using Claunia.PropertyList;
using static ivinject.Features.Packaging.Models.InfoPlistDictionaryKeys;

namespace ivinject.Features.Packaging;

internal static class InfoPlistDictionaryExtensions
{
    internal static string? BundleIdentifier(this NSDictionary dictionary) =>
        dictionary.TryGetValue(CoreFoundationBundleIdentifierKey, out var bundleIdObject)
            ? ((NSString)bundleIdObject).Content
            : null;
    
    internal static string? WatchKitCompanionAppBundleIdentifier(this NSDictionary dictionary) =>
        dictionary.TryGetValue(WatchKitCompanionAppBundleIdentifierKey, out var bundleIdObject)
            ? ((NSString)bundleIdObject).Content
            : null;
    internal static NSDictionary? Extension(this NSDictionary dictionary) =>
        dictionary.TryGetValue(NextStepExtensionKey, out var extension)
            ? (NSDictionary)extension
            : null;

    internal static string ExtensionPointIdentifier(this NSDictionary dictionary) =>
        ((NSString)dictionary[NextStepExtensionPointIdentifierKey]).Content;
    
    internal static NSDictionary ExtensionAttributes(this NSDictionary dictionary) =>
        (NSDictionary)dictionary[NextStepExtensionAttributesKey];
    
    internal static string WatchKitAppBundleIdentifier(this NSDictionary dictionary) =>
        ((NSString)dictionary[WatchKitAppBundleIdentifierKey]).Content;
    
    internal static string BundleExecutable(this NSDictionary dictionary) =>
        ((NSString)dictionary[CoreFoundationBundleExecutableKey]).Content;

    internal static async Task SaveToFile(this NSDictionary dictionary, string filePath) =>
        await File.WriteAllTextAsync(filePath, dictionary.ToXmlPropertyList());
}