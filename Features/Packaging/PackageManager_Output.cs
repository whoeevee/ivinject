using System.IO.Compression;
using Microsoft.Extensions.Logging;
using static ivinject.Common.DirectoryExtensions;

namespace ivinject.Features.Packaging;

internal partial class PackageManager
{
    private bool CopyAppPackage(string outputAppPackage, bool overwrite, ref bool isOverwritten)
    {
        var packageDirectory = new DirectoryInfo(outputAppPackage);

        if (packageDirectory.Exists)
        {
            if (overwrite)
            {
                packageDirectory.Delete(true);
                isOverwritten = true;
            }
            else
            {
                return false;
            }
        }

        CopyDirectory(_bundleDirectory, packageDirectory.FullName, true);
        logger.LogInformation("{} {}", isOverwritten ? "Replaced" : "Copied", packageDirectory.Name);
        
        return true;
    }
    
    private bool CreateAppArchive(
        string outputAppPackage,
        bool overwrite,
        CompressionLevel compressionLevel,
        ref bool isOverwritten
    )
    {
        var packageFile = new FileInfo(outputAppPackage);

        if (packageFile.Exists)
        {
            if (overwrite)
            {
                packageFile.Delete();
                isOverwritten = true;
            }
            else
            {
                return false;
            }
        }

        foreach (var dotFile in Directory.EnumerateFiles(TempPayloadDirectory, ".*"))
        {
            var fileInfo = new FileInfo(dotFile);
            
            File.Delete(fileInfo.FullName);
            logger.LogWarning("Removed {} from the app package", fileInfo.Name);
        }

        ZipFile.CreateFromDirectory(
            TempPayloadDirectory,
            packageFile.FullName,
            compressionLevel,
            true
        );
        logger.LogInformation("{} {}", isOverwritten ? "Replaced" : "Created", packageFile.Name);
        
        return true;
    }

    internal bool CreateAppPackage(string outputAppPackage, bool overwrite, CompressionLevel compressionLevel)
    {
        var isOverwritten = false;
        
        return outputAppPackage.EndsWith(".app") 
            ? CopyAppPackage(outputAppPackage, overwrite, ref isOverwritten) 
            : CreateAppArchive(outputAppPackage, overwrite, compressionLevel, ref isOverwritten);
    }
}