namespace Melanzana.CodeSign.Blobs
{
    [GenerateReaderWriter]
    [BigEndian]
    public partial class CodeDirectoryBaselineHeader
    {
        public BlobMagic Magic;
        public uint Size;
        public CodeDirectoryVersion Version;
        public CodeDirectoryFlags Flags;
        public uint HashesOffset;
        public uint IdentifierOffset;
        public uint SpecialSlotCount;
        public uint CodeSlotCount;
        public uint ExecutableLength;
        public byte HashSize;
        public HashType HashType;
        public byte Platform;
        public byte Log2PageSize;
        public uint Reserved;
    }
}