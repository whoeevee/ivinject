using System.IO.Compression;
using ivinject.Features.Injection.Models;

namespace ivinject.Features.Command.Models;

internal class IviParameters
{
    internal required string TargetAppPackage { get; init; }
    internal required string OutputAppPackage { get; init; }
    internal bool OverwriteOutput { get; init; }
    internal CompressionLevel CompressionLevel { get; init; }
    internal required IEnumerable<IviInjectionEntry> InjectionEntries { get; init; }
    internal IviSigningInfo? SigningInfo { get; init; }
    internal IviPackagingInfo? PackagingInfo { get; init; }
}