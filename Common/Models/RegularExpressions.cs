using System.Text.RegularExpressions;

namespace ivinject.Common.Models;

internal static partial class RegularExpressions
{
    [GeneratedRegex("cryptid 1", RegexOptions.Compiled)]
    internal static partial Regex OToolEncryptedBinary();
    
    [GeneratedRegex(@"\.(?:app|\w*ipa)$", RegexOptions.Compiled)]
    internal static partial Regex ApplicationPackage();
}