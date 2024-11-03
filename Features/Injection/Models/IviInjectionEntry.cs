using ivinject.Features.Packaging.Models;

namespace ivinject.Features.Injection.Models;

internal class IviInjectionEntry
{
    private readonly FileInfo _fileInfo;
    internal string FullName => _fileInfo.FullName;
    internal string Name => _fileInfo.Name;
    internal IviInjectionEntryType Type => _fileInfo.Extension switch
    {
        ".dylib" => IviInjectionEntryType.DynamicLibrary,
        ".deb" => IviInjectionEntryType.DebianPackage,
        ".bundle" => IviInjectionEntryType.Bundle,
        ".framework" => IviInjectionEntryType.Framework,
        ".appex" => IviInjectionEntryType.PlugIn,
        _ => IviInjectionEntryType.Unknown
    };

    internal IviInjectionEntry(FileInfo fileInfo) 
        => _fileInfo = fileInfo;
    
    internal IviInjectionEntry(string filePath)
        => _fileInfo = new FileInfo(filePath);
    
    internal string GetPathInBundle(IviDirectoriesInfo directoriesInfo) =>
        Type switch
        {
            IviInjectionEntryType.DynamicLibrary or IviInjectionEntryType.Unknown => 
                Path.Combine(
                    Type is IviInjectionEntryType.DynamicLibrary
                        ? directoriesInfo.FrameworksDirectory 
                        : directoriesInfo.BundleDirectory,
                    Name
                ),
            IviInjectionEntryType.Framework => Path.Combine(
                directoriesInfo.FrameworksDirectory,
                Name
            ),
            IviInjectionEntryType.PlugIn => Path.Combine(
                directoriesInfo.PlugInsDirectory,
                Name
            ),
            _ => Path.Combine(directoriesInfo.BundleDirectory, Name)
        };
}
