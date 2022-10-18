namespace Melanzana.CodeSign.Blobs
{
    [GenerateReaderWriter]
    [BigEndian]
    public partial class CodeDirectoryCodeLimit64Header
    {
        public uint Reserved;
        public ulong CodeLimit64;
    }
}