using System.Diagnostics;
using static ivinject.Common.Models.BinaryHeaders;

namespace ivinject.Common.Models;

internal class IviMachOBinary(string fileName)
{
    private FileInfo FileInfo { get; } = new(fileName);

    internal string Name => FileInfo.Name;
    internal string FullName => FileInfo.FullName;
    
    internal bool IsFatFile
    {
        get
        {
            var header = File.OpenRead(FullName).FileHeader();
            return FatHeaders.Contains(header);
        }
    }

    internal string FileSize
    {
        get
        {
            FileInfo.Refresh();
            var size = FileInfo.Length;

            return size switch
            {
                < 1024 => $"{size:F0} bytes",
                _ when size >> 10 < 1024 => $"{size / (float)1024:F1} KB",
                _ when size >> 20 < 1024 => $"{(size >> 10) / (float)1024:F1} MB",
                _ => $"{(size >> 30) / (float)1024:F1} GB"
            };
        }
    }
    
    internal async Task<bool> IsEncrypted()
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "otool",
                ArgumentList = { "-l", FullName },
                RedirectStandardOutput = true
            }
        );
        
        var output = await process!.StandardOutput.ReadToEndAsync();
        return RegularExpressions.OToolEncryptedBinary().IsMatch(output);
    }

    internal async Task<bool> Thin()
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "lipo",
                ArgumentList = { "-thin", "arm64", FullName, "-output", FullName }
            }
        );
        
        await process!.WaitForExitAsync();
        return process.ExitCode == 0;
    }
}