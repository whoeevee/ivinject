using static ivinject.Features.Packaging.Models.DirectoryNames;

namespace ivinject.Features.Packaging.Models;

internal class IviDirectoriesInfo(string bundleDirectory)
{
    internal string BundleDirectory { get; } = bundleDirectory;
    internal string FrameworksDirectory { get; } = Path.Combine(bundleDirectory, FrameworksDirectoryName);
    internal string PlugInsDirectory { get; } = Path.Combine(bundleDirectory, PlugInsDirectoryName);
}