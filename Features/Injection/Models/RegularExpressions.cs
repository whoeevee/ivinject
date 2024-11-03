using System.Text.RegularExpressions;

namespace ivinject.Features.Injection.Models;

internal static partial class RegularExpressions
{
    [GeneratedRegex(@"([\/@].*) \(.*\)", RegexOptions.Compiled)]
    internal static partial Regex OToolSharedLibrary();
}