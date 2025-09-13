namespace ivinject.Features.Command.Models;

internal class IviSigningInfo
{
    internal bool IsFromProvisioningProfile { get; init; }
    internal required string Identity { get; init; }
    internal bool IsAdHocSigning => Identity == "-";
    internal FileInfo? Entitlements { get; init; }
}