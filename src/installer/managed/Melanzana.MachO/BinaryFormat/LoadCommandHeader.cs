namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class LoadCommandHeader
    {
        public MachLoadCommandType CommandType { get; set; }
        public uint CommandSize { get; set; }
    }
}