using System.Diagnostics.CodeAnalysis;
using ivinject.Features.Codesigning;
using ivinject.Features.Command.Models;
using ivinject.Features.Injection;
using ivinject.Features.Injection.Models;
using ivinject.Features.Packaging;
using ivinject.Features.Packaging.Models;
using Microsoft.Extensions.Logging;

namespace ivinject.Features.Command;

internal class IviRootCommandProcessor
{
    private readonly ILogger _logger;
    private readonly PackageManager _packageManager;
    private readonly InjectionManager _injectionManager;
    private readonly CodesigningManager _codesigningManager;
    
    internal IviRootCommandProcessor(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("Main");
        _packageManager = new PackageManager(
            loggerFactory.CreateLogger("PackageManager")
        );
        _injectionManager = new InjectionManager(
            loggerFactory.CreateLogger("InjectionManager")
        );
        _codesigningManager = new CodesigningManager(
            loggerFactory.CreateLogger("CodesigningManager")
        );
    }
    
    [SuppressMessage("Usage", "CA2254")]
    private void CriticalError(string? message, params object?[] args)
    {
        _logger.LogCritical(message, args);
        Environment.Exit(1);
    }

    private async Task InjectEntries(IEnumerable<IviInjectionEntry> injectionEntries)
    {
        await _injectionManager.AddEntriesAsync(injectionEntries);

        if (!await _injectionManager.ThinCopiedBinariesAsync())
            CriticalError("Unable to thin one or more binaries.");

        await _injectionManager.CopyKnownFrameworksAsync();
        await _injectionManager.FixCopiedDependenciesAsync();
    }
    
    private async Task CheckForEncryptedBinaries(IviPackageInfo packageInfo)
    {
        var encryptionInfo = await _codesigningManager.GetEncryptionStateAsync();

        if (encryptionInfo.IsMainBinaryEncrypted)
            CriticalError("The main application binary, {}, is encrypted.", packageInfo.MainBinary.Name);

        if (encryptionInfo.EncryptedBinaries.Any())
        {
            var encryptedPaths = encryptionInfo.EncryptedBinaries.Select(binary =>
                Path.GetRelativePath(packageInfo.DirectoriesInfo.BundleDirectory, binary.FullName)
            );
            
            _logger.LogError(
                "The app package contains encrypted binaries. Consider removing them: \n{}",
                string.Join("\n", encryptedPaths)
            );
        }
    }

    private async Task ProcessSigning(IviSigningInfo? signingInfo)
    {
        var hasIdentity = signingInfo is not null;
        var isAdHocSigning = signingInfo?.IsAdHocSigning ?? false;
        var hasEntitlements = signingInfo?.Entitlements is not null;

        if (hasIdentity && !isAdHocSigning && !hasEntitlements)
            CriticalError("Entitlements are required for non ad hoc identity signing.");

        if (hasIdentity)
        {
            if (!await _codesigningManager.SaveMainExecutablesEntitlementsAsync())
            {
                _logger.LogError(
                    "Unable to save entitlements for one or more binaries. The package is likely unsigned, and all specified entitlements will be applied."
                );
            }
        }

        if (!await _codesigningManager.RemoveSignatureAsync())
            CriticalError("Unable to remove signature from one or more binaries.");
        
        await _injectionManager.InsertLoadCommandsAsync();

        if (hasIdentity)
        {
            if (!await _codesigningManager.SignPackageAsync(signingInfo!))
                CriticalError("Unable to sign one or more binaries.");
        }
    }
    
    internal async Task ProcessRootCommand(IviParameters parameters)
    {
        _packageManager.LoadAppPackage(parameters.TargetAppPackage);
        _logger.LogInformation("Loaded app package");
        
        var packageInfo = _packageManager.PackageInfo;
        
        if (parameters.PackagingInfo is { } packagingInfo)
            await _packageManager.PerformPackageModifications(packagingInfo);
        
        //
        
        _injectionManager.UpdateWithPackage(packageInfo);
        await InjectEntries(parameters.InjectionEntries);
        
        _codesigningManager.UpdateWithPackage(packageInfo);
        await CheckForEncryptedBinaries(packageInfo);
        
        await ProcessSigning(parameters.SigningInfo);

        if (!_packageManager.CreateAppPackage(
                parameters.OutputAppPackage,
                parameters.OverwriteOutput,
                parameters.CompressionLevel
            ))
            CriticalError(
                "The app package couldn't be created. If it already exists, use --overwrite to replace."
            );
        
        _codesigningManager.Dispose();
        _packageManager.Dispose();
    }
}