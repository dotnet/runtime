namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class LinkEditHeader
    {
        public uint FileOffset { get; set; }
        public uint FileSize { get; set; }
    }
}