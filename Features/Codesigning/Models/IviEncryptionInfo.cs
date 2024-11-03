using ivinject.Common.Models;

namespace ivinject.Features.Codesigning.Models;

internal class IviEncryptionInfo
{
    internal bool IsMainBinaryEncrypted { get; init; }
    internal IEnumerable<IviMachOBinary> EncryptedBinaries { get; init; } = [];
}