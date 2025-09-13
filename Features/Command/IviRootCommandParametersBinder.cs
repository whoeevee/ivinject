using System.CommandLine;
using System.CommandLine.Binding;
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
) : BinderBase<IviParameters>
{
    protected override IviParameters GetBoundValue(BindingContext bindingContext)
    {
        var targetAppPackage = 
            bindingContext.ParseResult.GetValueForArgument(targetArgument);
        var outputAppPackage = 
            bindingContext.ParseResult.GetValueForArgument(outputArgument);
        var overwriteOutput = 
            bindingContext.ParseResult.GetValueForOption(overwriteOutputOption);
        var compressionLevel = 
            bindingContext.ParseResult.GetValueForOption(compressionLevelOption);
        
        var items = 
            bindingContext.ParseResult.GetValueForOption(itemsOption);
        var provisioningProfile =
            bindingContext.ParseResult.GetValueForOption(provisioningProfileOption);
        var profileBundleId =
            bindingContext.ParseResult.GetValueForOption(profileBundleIdOption);
        var codesignIdentity =
            bindingContext.ParseResult.GetValueForOption(codesignIdentityOption);
        var codesignEntitlements = 
            bindingContext.ParseResult.GetValueForOption(codesignEntitlementsOption);
        
        var customBundleId = 
            bindingContext.ParseResult.GetValueForOption(customBundleIdOption);
        var enableDocumentsSupport = 
            bindingContext.ParseResult.GetValueForOption(enableDocumentsSupportOption);
        var removeSupportedDevices = 
            bindingContext.ParseResult.GetValueForOption(removeSupportedDevicesOption);
        var directoriesToRemove = 
            bindingContext.ParseResult.GetValueForOption(directoriesToRemoveOption);

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