using System.Diagnostics;
using ivinject.Features.Injection.Models;
using static ivinject.Common.DirectoryExtensions;

namespace ivinject.Features.Injection;

internal class DebManager(IviInjectionEntry debEntry) : IDisposable
{
    private readonly string _tempPath = TempDirectoryPath();
    
    internal async Task<IviInjectionEntry[]> ExtractDebEntries()
    {
        if (debEntry.Type is not IviInjectionEntryType.DebianPackage)
            throw new ArgumentException("Entry type is not DebianPackage");
        
        Directory.CreateDirectory(_tempPath);
        
        var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "tar",
                ArgumentList = { "-xf", debEntry.FullName, $"--directory={_tempPath}" }
            }
        );
        
        await process!.WaitForExitAsync();
        
        var dataArchive = Directory.GetFiles(_tempPath, "data*.*")[0];
        
        process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "tar",
                ArgumentList = { "-xf", dataArchive },
                WorkingDirectory = _tempPath
            }
        );
        
        await process!.WaitForExitAsync();
        
        var dataFiles = Directory.EnumerateFiles(
            _tempPath,
            "*",
            SearchOption.AllDirectories
        );
            
        var dataDirectories = Directory.EnumerateDirectories(
            _tempPath,
            "*",
            SearchOption.AllDirectories
        );
        
        return dataFiles.Concat(dataDirectories)
            .Select(entry => new IviInjectionEntry(entry))
            .Where(entry => entry.Type is not IviInjectionEntryType.Unknown)
            .ToArray();
    }

    public void Dispose() => Directory.Delete(_tempPath, true);
}