namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class SectionHeader
    {
        public MachFixedName SectionName { get; set; } = MachFixedName.Empty;
        public MachFixedName SegmentName { get; set; } = MachFixedName.Empty;
        public uint Address { get; set; }
        public uint Size { get; set; }
        public uint FileOffset { get; set; }
        public uint Log2Alignment { get; set; }
        public uint RelocationOffset { get; set; }
        public uint NumberOfReloationEntries { get; set; }
        public uint Flags { get; set; }
        public uint Reserved1 { get; set; }
        public uint Reserved2 { get; set; }
    }
}