using ivinject.Common.Models;

namespace ivinject.Features.Packaging.Models;

internal class IviPackageInfo(string mainBinary, string bundleIdentifier, IviDirectoriesInfo directoriesInfo)
{
    internal IviMachOBinary MainBinary { get; } = new(
        Path.Combine(directoriesInfo.BundleDirectory, mainBinary)
    );
    internal string BundleIdentifier { get; } = bundleIdentifier;
    internal IviDirectoriesInfo DirectoriesInfo { get; } = directoriesInfo;
}