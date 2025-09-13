using System.Collections.Concurrent;
using Claunia.PropertyList;
using ivinject.Common;
using ivinject.Common.Models;
using ivinject.Features.Codesigning.Models;
using ivinject.Features.Command.Models;
using ivinject.Features.Packaging;
using ivinject.Features.Packaging.Models;
using Microsoft.Extensions.Logging;
using static ivinject.Common.Models.BinaryHeaders;

namespace ivinject.Features.Codesigning;

internal class CodesigningManager(ILogger logger) : IDisposable
{
    private IviPackageInfo _packageInfo = null!;
    private IviDirectoriesInfo DirectoriesInfo => _packageInfo.DirectoriesInfo;
    
    //
    
    private readonly List<IviMachOBinary> _allBinaries = [];
    private IviMachOBinary MainBinary => _allBinaries[0];
    private List<IviMachOBinary> Binaries => _allBinaries[1..];
    private List<IviMachOBinary> MainExecutables =>
        _allBinaries.Where(binary => binary.IsMainExecutable(DirectoriesInfo)).ToList();
    
    //
    
    private readonly ConcurrentDictionary<IviMachOBinary, FileInfo> _savedEntitlements = [];
    private readonly List<FileInfo> _mergedEntitlementFiles = [];
    
    internal void UpdateWithPackage(IviPackageInfo packageInfo)
    {
        _packageInfo = packageInfo;
        ProcessBinaries(packageInfo);
    }

    private void ProcessBinaries(IviPackageInfo packageInfo)
    {
        foreach (var file in Directory.EnumerateFiles(
                     packageInfo.DirectoriesInfo.BundleDirectory,
                     "*",
                     SearchOption.AllDirectories))
        {
            var header = File.OpenRead(file).FileHeader();
            
            if (!MhHeaders.Contains(header) && !FatHeaders.Contains(header))
                continue;

            if (file.Contains("Stub"))
            {
                var relativePath = Path.GetRelativePath(
                    packageInfo.DirectoriesInfo.BundleDirectory,
                    file
                );
                logger.LogWarning(
                    "Skipping stub executable {}, its signature may not be modified",
                    relativePath
                );
                continue;
            }

            _allBinaries.Add(new IviMachOBinary(file));
        }
    }

    internal async Task<IviEncryptionInfo> GetEncryptionStateAsync()
    {
        var results = await Task.WhenAll(
            Binaries.Select(
                async binary => new
                {
                    Binary = binary,
                    IsEncrypted = await binary.IsEncrypted()
                }
            ));

        return new IviEncryptionInfo
        {
            IsMainBinaryEncrypted = await MainBinary.IsEncrypted(),
            EncryptedBinaries = results
                .Where(result => result.IsEncrypted)
                .Select(result => result.Binary)
        };
    }
    
    internal async Task<bool> SaveMainExecutablesEntitlementsAsync()
    {
        var dumpingResults = await Task.WhenAll(
            MainExecutables.Select(async binary =>
                {
                    var tempFile = Path.GetTempFileName();
                    var tempFileInfo = new FileInfo(tempFile);

                    if (!await binary.DumpEntitlementsAsync(tempFile))
                    {
                        tempFileInfo.Delete();
                        return false;
                    }
                    
                    _savedEntitlements[binary] = tempFileInfo;
                    return true;
                }
            )
        );

        return dumpingResults.All(result => result);
    }

    private async Task<FileInfo> MergeEntitlementsAsync(
        string binaryName,
        FileInfo binaryEntitlements,
        FileInfo profileEntitlements
    )
    {
        var binaryEntitlementsDictionary = (NSDictionary)PropertyListParser.Parse(binaryEntitlements);
        var profileEntitlementsDictionary = (NSDictionary)PropertyListParser.Parse(profileEntitlements);

        var profileTeamId = ((NSString)profileEntitlementsDictionary["com.apple.developer.team-identifier"]).Content;
        
        var finalEntitlements = new NSDictionary();
        
        foreach (var entitlementKey in binaryEntitlementsDictionary.Keys)
        {
            if (!profileEntitlementsDictionary.TryGetValue(entitlementKey, out var profileEntitlement))
            {
                logger.LogWarning(
                    "Matching entitlement for {} ({}) was not found",
                    entitlementKey,
                    binaryName
                );
                
                continue;
            }

            if (entitlementKey == "keychain-access-groups")
            {
                var binaryAccessGroups = (NSArray)binaryEntitlementsDictionary[entitlementKey];
                var finalAccessGroups = new NSArray();
                
                foreach (var accessGroup in binaryAccessGroups)
                {
                    var group = profileTeamId + ((NSString)accessGroup).Content[10..];
                    finalAccessGroups.Add(new NSString(group));
                }
                
                finalEntitlements[entitlementKey] = finalAccessGroups;
                logger.LogInformation("Mapped keychain access groups for {}: {}", binaryName, finalAccessGroups);
                
                continue;
            }
            
            finalEntitlements[entitlementKey] = profileEntitlement;
        }
        
        var finalEntitlementsPath = Path.GetTempFileName();
        await finalEntitlements.SaveToFileAsync(finalEntitlementsPath);

        var fileInfo = new FileInfo(finalEntitlementsPath);
        _mergedEntitlementFiles.Add(fileInfo);
        
        return fileInfo;
    }

    private async Task<bool> SignBinary(IviMachOBinary binary, IviSigningInfo signingInfo)
    {
        var identity = signingInfo.Identity;
        var entitlements = signingInfo.Entitlements;
        
        if (!binary.IsMainExecutable(DirectoriesInfo))
            return await binary.SignAsync(identity);

        if (!_savedEntitlements.TryGetValue(binary, out var signingEntitlements))
            return await binary.SignAsync(identity, entitlements);

        if (!signingInfo.IsAdHocSigning)
            signingEntitlements = await MergeEntitlementsAsync(
                binary.Name,
                signingEntitlements,
                entitlements!
            );
        
        return await binary.SignAsync(identity, signingEntitlements);
    }

    internal async Task<bool> SignPackageAsync(IviSigningInfo signingInfo)
    {
        var signingResults = await Task.WhenAll(
            Binaries.Select(async binary => await SignBinary(binary, signingInfo))
        );

        if (signingResults.Any(result => !result))
            return false;

        if (!await SignBinary(MainBinary, signingInfo))
            return false;
        
        if (signingInfo.IsFromProvisioningProfile)
            signingInfo.Entitlements!.Delete();
        
        logger.LogInformation(
            "Signed {} binaries ({} main executables) with the specified identity",
            _allBinaries.Count,
            MainExecutables.Count
        );
        
        return true;
    }
    
    internal async Task<bool> RemoveSignatureAsync()
    {
        var removingResults = await Task.WhenAll(
            _allBinaries.Select(binary => binary.RemoveSignatureAsync())
        );
        
        if (removingResults.Any(result => !result))
            return false;
        
        logger.LogInformation("Signature removed from {} binaries", _allBinaries.Count);
        return true;
    }

    public void Dispose()
    {
        foreach (var fileInfo in _savedEntitlements.Values)
            fileInfo.Delete();
        
        foreach (var fileInfo in _mergedEntitlementFiles)
            fileInfo.Delete();
    } 
}