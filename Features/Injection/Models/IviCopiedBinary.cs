using System.Text;
using ivinject.Common.Models;
using ivinject.Features.Packaging.Models;

namespace ivinject.Features.Injection.Models;

internal class IviCopiedBinary
{
    internal required IviInjectionEntryType Type { get; init; }
    internal required IviMachOBinary Binary { get; init; }

    internal string Name => Binary.Name;

    internal string GetRunPath(IviDirectoriesInfo directoriesInfo)
    {
        var builder = new StringBuilder("@rpath/");

        builder.Append(
            Type switch
            {
                IviInjectionEntryType.Framework => Path.GetRelativePath(
                    directoriesInfo.FrameworksDirectory,
                    Binary.FullName
                ),
                IviInjectionEntryType.PlugIn => Path.GetRelativePath(
                    directoriesInfo.PlugInsDirectory,
                    Binary.FullName
                ),
                _ => Binary.Name
            }
        );
                
        return builder.ToString();
    }
}