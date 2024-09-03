namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class SymbolHeader
    {
        public uint NameIndex { get; set; }
        public byte Type { get; set; }
        public byte Section { get; set; }
        public ushort Descriptor { get; set; }
    }
}