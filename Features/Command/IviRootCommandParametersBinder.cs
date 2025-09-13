using System.CommandLine;
using System.IO.Compression;
using ivinject.Features.Command.Models;
using ivinject.Features.Injection.Models;

namespace ivinject.Features.Command;

internal class IviRootCommandParametersBinder(
    Argument<string> targetArgument,
    Argument<string> outputArgument,
    Option<bool> overwriteOutputOption,
    Option<CompressionLevel> compressionLevelOption,
    Option<IEnumerable<FileInfo>> itemsOption,
    Option<FileInfo> provisioningProfileOption,
    Option<bool> profileBundleIdOption,
    Option<string> codesignIdentityOption,
    Option<FileInfo> codesignEntitlementsOption,
    Option<string> customBundleIdOption,
    Option<bool> enableDocumentsSupportOption,
    Option<bool> removeSupportedDevicesOption,
    Option<IEnumerable<string>> directoriesToRemoveOption
)
{
    public IviParameters GetBoundValue(ParseResult parseResult)
    {
        var targetAppPackage = 
            parseResult.GetValue(targetArgument)!;
        var outputAppPackage = 
            parseResult.GetValue(outputArgument)!;
        
        var overwriteOutput = 
            parseResult.GetValue(overwriteOutputOption);
        var compressionLevel = 
            parseResult.GetValue(compressionLevelOption);
        
        var items = 
            parseResult.GetValue(itemsOption);
        var provisioningProfile =
            parseResult.GetValue(provisioningProfileOption);
        var profileBundleId =
            parseResult.GetValue(profileBundleIdOption);
        var codesignIdentity =
            parseResult.GetValue(codesignIdentityOption);
        var codesignEntitlements = 
            parseResult.GetValue(codesignEntitlementsOption);
        
        var customBundleId = 
            parseResult.GetValue(customBundleIdOption);
        var enableDocumentsSupport = 
            parseResult.GetValue(enableDocumentsSupportOption);
        var removeSupportedDevices = 
            parseResult.GetValue(removeSupportedDevicesOption);
        var directoriesToRemove = 
            parseResult.GetValue(directoriesToRemoveOption);

        IviSigningInfo? signingInfo = null;
        IviPackagingInfo? packagingInfo = null;
        
        var profileInfo = provisioningProfile is not null 
            ? ProvisioningProfileParser.Parse(provisioningProfile)
            : null;
        
        if (profileInfo is not null)
        {
            signingInfo = new IviSigningInfo
            {
                Identity = profileInfo.Identity,
                Entitlements = profileInfo.Entitlements,
                IsFromProvisioningProfile = true
            };
        }
        else if (codesignIdentity is not null)
        {
            signingInfo = new IviSigningInfo
            {
                Identity = codesignIdentity,
                Entitlements = codesignEntitlements
            };
        }

        var bundleId = customBundleId;
        
        if (profileInfo is not null && profileBundleId)
            bundleId = profileInfo.BundleId;

        if (bundleId is not null
            || directoriesToRemove is not null
            || enableDocumentsSupport
            || removeSupportedDevices
        )
        {
            packagingInfo = new IviPackagingInfo
            {
                CustomBundleId = bundleId,
                EnableDocumentsSupport = enableDocumentsSupport,
                RemoveSupportedDevices = removeSupportedDevices,
                DirectoriesToRemove = directoriesToRemove ?? []
            };
        }
        
        return new IviParameters
        {
            TargetAppPackage = targetAppPackage,
            OutputAppPackage = outputAppPackage,
            OverwriteOutput = overwriteOutput,
            CompressionLevel = compressionLevel,
            InjectionEntries = items?.Select(item => new IviInjectionEntry(item)) ?? [],
            SigningInfo = signingInfo,
            PackagingInfo = packagingInfo
        };
    }
}