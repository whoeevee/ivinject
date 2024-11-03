namespace ivinject.Features.Command.Models;

internal class IviPackagingInfo
{
    internal string? CustomBundleId { get; init; }
    internal bool RemoveSupportedDevices { get; init; }
    internal bool EnableDocumentsSupport { get; init; }
    internal required IEnumerable<string> DirectoriesToRemove { get; init; }
}