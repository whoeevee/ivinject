using System.Diagnostics;
using System.Text;
using ivinject.Common.Models;
using ivinject.Features.Packaging.Models;
using static ivinject.Features.Packaging.Models.DirectoryNames;

namespace ivinject.Features.Codesigning;

internal static class CodesigningMachOExtensions
{
    internal static bool IsMainExecutable(this IviMachOBinary binary, IviDirectoriesInfo directoriesInfo)
    {
        return !Path.GetRelativePath(
            directoriesInfo.BundleDirectory,
            binary.FullName
        ).Contains(FrameworksDirectoryName);
    }
    
    internal static async Task<bool> SignAsync(
        this IviMachOBinary binary,
        string identity,
        FileInfo? entitlements = null
    )
    {
        var arguments = new StringBuilder($"-s {identity}");
        
        if (entitlements is not null)
            arguments.Append($" --entitlements {entitlements.FullName}");
        
        arguments.Append($" \"{binary.FullName}\"");
        
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
                ArgumentList = { "--remove-signature", binary.FullName }
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
                ArgumentList = { "-d", "--entitlements", outputFilePath, "--xml", binary.FullName },
                RedirectStandardError = true
            }
        );

        var error = await process!.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        return process.ExitCode == 0 && error.Count(c => c.Equals('\n')) == 1;
    }
}