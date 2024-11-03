namespace ivinject.Common.Models;

internal class BinaryHeaders
{
    private const uint FatMagic = 0xcafebabe;
    private const uint FatMagic64 = 0xcafebabf;
    private const uint FatCigam = 0xbebafeca;
    private const uint FatCigam64 = 0xbfbafeca;
    
    private const uint MhMagic = 0xfeedface;
    private const uint MhMagic64 = 0xfeedfacf;
    private const uint MhCigam = 0xcefaedfe;
    private const uint MhCigam64 = 0xcffaedfe;

    internal static readonly uint[] FatHeaders =
    [
        FatMagic,
        FatMagic64,
        FatCigam,
        FatCigam64
    ];
    
    internal static readonly uint[] MhHeaders =
    [
        MhMagic,
        MhMagic64,
        MhCigam,
        MhCigam64
    ];
}