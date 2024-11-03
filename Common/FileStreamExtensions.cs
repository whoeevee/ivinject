namespace ivinject.Common;

internal static class FileStreamExtensions
{
    internal static uint FileHeader(this FileStream stream)
    {
        using var reader = new BinaryReader(stream);
        var bytes = reader.ReadBytes(4);

        return bytes.Length != 4 
            ? uint.MinValue 
            : BitConverter.ToUInt32(bytes);
    }
}