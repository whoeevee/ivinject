using System.Diagnostics;
using System.Text;
using ivinject.Common.Models;

namespace ivinject.Features.Codesigning;

internal static class CodesigningMachOExtensions
{
    internal static async Task<bool> SignAsync(
        this IviMachOBinary binary,
        string identity,
        bool force,
        FileInfo? entitlements,
        bool preserveEntitlements = false
    )
    {
        var arguments = new StringBuilder($"-s {identity}");
        
        if (entitlements is not null)
            arguments.Append($" --entitlements {entitlements.FullName}");
        else if (preserveEntitlements)
            arguments.Append(" --preserve-metadata=entitlements");

        if (force)
            arguments.Append(" -f");
        
        arguments.Append($" {binary.FullName}");
        
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "codesign",
                Arguments = arguments.ToString(),
                RedirectStandardOutput = true
            }
        );
        
        await process!.WaitForExitAsync();
        return process.ExitCode == 0;
    }
    
    internal static async Task<bool> RemoveSignatureAsync(this IviMachOBinary binary)
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "codesign",
                Arguments = $"--remove-signature \"{binary.FullName}\""
            }
        );
        
        await process!.WaitForExitAsync();
        return process.ExitCode == 0;
    }
    
    internal static async Task<bool> DumpEntitlementsAsync(this IviMachOBinary binary, string outputFilePath)
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "codesign",
                Arguments = $"-d --entitlements {outputFilePath} --xml \"{binary.FullName}\"",
                RedirectStandardError = true
            }
        );
        
        await process!.WaitForExitAsync();
        return process.ExitCode == 0;
    }
}