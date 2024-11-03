using ivinject.Common;
using ivinject.Common.Models;
using ivinject.Features.Codesigning.Models;
using ivinject.Features.Packaging.Models;
using Microsoft.Extensions.Logging;
using static ivinject.Features.Packaging.Models.DirectoryNames;
using static ivinject.Common.Models.BinaryHeaders;

namespace ivinject.Features.Codesigning;

internal class CodesigningManager(ILogger logger) : IDisposable
{
    private IviPackageInfo _packageInfo = null!;
    
    private readonly List<IviMachOBinary> _allBinaries = [];
    private List<IviMachOBinary> Binaries => _allBinaries[1..];
    private IviMachOBinary MainBinary => _allBinaries[0];
    
    private FileInfo? _savedMainEntitlements;
    
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
    
    internal async Task SaveMainBinaryEntitlementsAsync()
    {
        var tempFile = Path.GetTempFileName();

        if (await MainBinary.DumpEntitlementsAsync(tempFile))
        {
            logger.LogInformation("Saved {} entitlements", MainBinary.Name);
            _savedMainEntitlements = new FileInfo(tempFile);
            
            return;
        }
        
        logger.LogWarning(
            "Unable to save {} entitlements. The binary is likely unsigned.",
            MainBinary.Name
        );
    }

    internal async Task<bool> SignAsync(string identity, bool isAdHocSigning, FileInfo? entitlements)
    {
        var mainExecutablesCount = 0;

        var signingResults = await Task.WhenAll(
            Binaries.Select(async binary =>
                {
                    var isMainExecutable = !Path.GetRelativePath(
                        _packageInfo.DirectoriesInfo.BundleDirectory,
                        binary.FullName
                    ).Contains(FrameworksDirectoryName);

                    if (isMainExecutable)
                        mainExecutablesCount++;

                    return await binary.SignAsync(
                        identity,
                        isAdHocSigning,
                        isMainExecutable
                            ? entitlements 
                            : null, 
                        isMainExecutable && entitlements is null 
                    );
                }
            )
        );

        if (signingResults.Any(result => !result))
            return false;

        if (!await MainBinary.SignAsync(identity, false, _savedMainEntitlements ?? entitlements))
            return false;
        
        logger.LogInformation(
            "Signed {} binaries ({} main executables) with the specified identity",
            _allBinaries.Count,
            mainExecutablesCount + 1 // App main executable
        );
        
        return true;
    }
    
    internal async Task<bool> RemoveSignatureAsync(bool allBinaries)
    {
        if (allBinaries)
        {
            var removingResults = await Task.WhenAll(
                _allBinaries.Select(binary => binary.RemoveSignatureAsync())
            );
            
            if (removingResults.Any(result => !result))
                return false;
            
            logger.LogInformation("Signature removed from {} binaries", _allBinaries.Count);
            return true;
        }

        if (!await MainBinary.RemoveSignatureAsync())
            return false;
        
        logger.LogInformation("Removed {} signature", MainBinary.Name);
        return true;
    }

    public void Dispose() => _savedMainEntitlements?.Delete();
}