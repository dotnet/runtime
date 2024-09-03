namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class Section64Header
    {
        public MachFixedName SectionName { get; set; } = MachFixedName.Empty;
        public MachFixedName SegmentName { get; set; } = MachFixedName.Empty;
        public ulong Address { get; set; }
        public ulong Size { get; set; }
        public uint FileOffset { get; set; }
        public uint Log2Alignment { get; set; }
        public uint RelocationOffset { get; set; }
        public uint NumberOfReloationEntries { get; set; }
        public uint Flags { get; set; }
        public uint Reserved1 { get; set; }
        public uint Reserved2 { get; set; }
        public uint Reserved3 { get; set; }
    }
}