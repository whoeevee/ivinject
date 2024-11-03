using System.IO.Compression;
using Claunia.PropertyList;
using ivinject.Features.Packaging.Models;
using Microsoft.Extensions.Logging;
using static ivinject.Common.DirectoryExtensions;

namespace ivinject.Features.Packaging;

internal partial class PackageManager(ILogger logger) : IDisposable
{
    private readonly string _tempDirectory = TempDirectoryPath();
    private string TempPayloadDirectory => Path.Combine(_tempDirectory, "Payload");
    private string _bundleDirectory = null!;
    
    private FileInfo _infoDictionaryFile = null!;
    private NSDictionary _infoDictionary = null!;
    
    internal IviPackageInfo PackageInfo { get; private set; } = null!;
    
    private void LoadPackageInfo()
    {
        _infoDictionaryFile = new FileInfo(
            Path.Combine(_bundleDirectory, "Info.plist")
        );
        
        _infoDictionary = (NSDictionary)PropertyListParser.Parse(_infoDictionaryFile);
        
        PackageInfo = new IviPackageInfo(
            _infoDictionary.BundleExecutable(),
            ((NSString)_infoDictionary["CFBundleIdentifier"]).Content,
            new IviDirectoriesInfo(_bundleDirectory)
        );
    }
    
    private void ProcessAppPackage(string targetAppPackage)
    {
        var directoryInfo = new DirectoryInfo(targetAppPackage);
        
        if (directoryInfo.Exists)
        {
            var packageName = directoryInfo.Name;

            _bundleDirectory = Path.Combine(TempPayloadDirectory, packageName);
            
            CopyDirectory(targetAppPackage, _bundleDirectory, true);
            logger.LogInformation("Copied {}", packageName);
            
            return;
        }
        
        var fileInfo = new FileInfo(targetAppPackage);
        var fileName = fileInfo.Name;
        
        ZipFile.ExtractToDirectory(targetAppPackage, _tempDirectory);
        logger.LogInformation("Extracted {}", fileName);
        
        _bundleDirectory = Directory.GetDirectories(TempPayloadDirectory)[0];
    }

    internal void LoadAppPackage(string targetAppPackage)
    {
        ProcessAppPackage(targetAppPackage);
        LoadPackageInfo();
    }

    public void Dispose() => Directory.Delete(_tempDirectory, true);
}