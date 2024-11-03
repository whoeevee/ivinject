using Claunia.PropertyList;
using ivinject.Features.Command.Models;
using Microsoft.Extensions.Logging;
using static ivinject.Features.Packaging.Models.InfoPlistDictionaryKeys;

namespace ivinject.Features.Packaging;

internal partial class PackageManager
{
    private void RemoveSupportedDevices()
    {
        if (_infoDictionary.Remove(UiKitSupportedDevicesKey))
            logger.LogInformation("Removed supported devices property");
        else
            logger.LogWarning("Unable to remove supported devices property. The key is likely not present.");
    }

    private void EnableDocumentSupport()
    {
        _infoDictionary[UiKitSupportsDocumentBrowserKey] = new NSNumber(true);
        _infoDictionary[UiKitFileSharingEnabledKey] = new NSNumber(true);
            
        logger.LogInformation("Enabled documents support for the application");
    }
    
    private void RemoveDirectories(IEnumerable<string> directories)
    {
        foreach (var directory in directories)
        {
            Directory.Delete(Path.Combine(_bundleDirectory, directory), true);
            logger.LogInformation("Removed {} directory from the app package", directory);
        }
    }
    
    private static void ReplaceWatchKitIdentifiers(
        NSDictionary dictionary,
        string customBundleId,
        string packageBundleId
    )
    {
        if (dictionary.WatchKitCompanionAppBundleIdentifier() is not null)
            dictionary[WatchKitCompanionAppBundleIdentifierKey] = new NSString(customBundleId);

        if (dictionary.Extension() is not { } extension
            || extension.ExtensionPointIdentifier() != "com.apple.watchkit") 
            return;
        
        var attributes = extension.ExtensionAttributes();
        var watchKitAppBundleId = attributes.WatchKitAppBundleIdentifier();

        attributes[WatchKitAppBundleIdentifierKey] = new NSString(
            watchKitAppBundleId.Replace(packageBundleId, customBundleId)
        );
    }
    
    private async Task ReplaceBundleIdentifiers(string customBundleId)
    {
        var packageBundleId = PackageInfo.BundleIdentifier;
        var replacedCount = 0;

        var infoPlistFiles = Directory.EnumerateFiles(
            _bundleDirectory,
            "Info.plist",
            SearchOption.AllDirectories
        );

        foreach (var file in infoPlistFiles)
        {
            var dictionary = (NSDictionary)PropertyListParser.Parse(file);

            ReplaceWatchKitIdentifiers(dictionary, customBundleId, packageBundleId);

            if (dictionary.BundleIdentifier() is not { } bundleId
                || !bundleId.Contains(packageBundleId))
                continue;

            var newBundleId = bundleId.Replace(packageBundleId, customBundleId);
            dictionary[CoreFoundationBundleIdentifierKey] = new NSString(newBundleId);

            await dictionary.SaveToFile(file);
            replacedCount++;
        }

        logger.LogInformation("Replaced bundle identifier of {} bundles", replacedCount);
    }
    
    internal async Task PerformPackageModifications(IviPackagingInfo packagingInfo)
    {
        RemoveDirectories(packagingInfo.DirectoriesToRemove);
        
        if (packagingInfo.RemoveSupportedDevices)
            RemoveSupportedDevices();
        
        if (packagingInfo.EnableDocumentsSupport)
            EnableDocumentSupport();
        
        await _infoDictionary.SaveToFile(_infoDictionaryFile.FullName);

        if (packagingInfo.CustomBundleId is not { } customBundleId)
            return;

        await ReplaceBundleIdentifiers(customBundleId);
    }
}