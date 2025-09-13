using System.Diagnostics;
using ivinject.Common.Models;
using ivinject.Features.Injection.Models;
using RegularExpressions = ivinject.Features.Injection.Models.RegularExpressions;

namespace ivinject.Features.Injection;

internal static class DependencyExtensions
{
    internal static async Task<string[]> GetSharedLibraries(this IviMachOBinary binary)
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "otool",
                ArgumentList = { "-L", binary.FullName },
                RedirectStandardOutput = true
            }
        );
        
        var output = await process!.StandardOutput.ReadToEndAsync();
        var matches = RegularExpressions.OToolSharedLibrary().Matches(output);
        
        // the first result is actually LC_ID_DYLIB, not LC_LOAD_DYLIB
        return matches.Select(match => match.Groups[1].Value).ToArray()[1..];
    }
    
    internal static async Task ChangeDependency(
        this IviMachOBinary binary,
        string oldPath,
        string newPath
    )
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "install_name_tool",
                ArgumentList = { "-change", oldPath, newPath, binary.FullName },
                RedirectStandardError = true
            }
        );
        
        await process!.WaitForExitAsync();
    }
    
    internal static async Task<bool> AddRunPath(
        this IviMachOBinary binary,
        string rPath
    )
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "install_name_tool",
                ArgumentList = { "-add_rpath", rPath, binary.FullName },
                RedirectStandardOutput = true
            }
        );
            
        await process!.WaitForExitAsync();
        return process.ExitCode == 0;
    }
    
    internal static async Task InsertDependency(
        this IviMachOBinary binary,
        string dependency
    )
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "insert-dylib",
                ArgumentList = { dependency, binary.FullName, "--all-yes", "--inplace" },
                RedirectStandardOutput = true
            }
        );
            
        await process!.WaitForExitAsync();
    }

    internal static async Task<IEnumerable<string>> AllDependencies(this List<IviCopiedBinary> copiedBinaries)
    {
        return (await Task.WhenAll(
            copiedBinaries.Select(async binary => await binary.Binary.GetSharedLibraries())
        ))
        .SelectMany(dependencies => dependencies)
        .Distinct();
    }
}