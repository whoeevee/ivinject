namespace ivinject.Features.Command.Models;

internal class ProfileInfo
{
    internal required FileInfo Entitlements { get; init; }
    internal required string Identity { get; set; }
    internal required string BundleId { get; set; }
}