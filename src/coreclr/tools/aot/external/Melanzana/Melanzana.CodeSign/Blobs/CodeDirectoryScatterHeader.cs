namespace Melanzana.CodeSign.Blobs
{
    [GenerateReaderWriter]
    [BigEndian]
    public partial class CodeDirectoryScatterHeader
    {
        public uint ScatterOffset;
    }
}