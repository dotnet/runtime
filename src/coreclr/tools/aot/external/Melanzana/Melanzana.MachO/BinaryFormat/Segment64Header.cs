namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class Segment64Header
    {
        public MachFixedName Name { get; set; } = MachFixedName.Empty;
        public ulong Address { get; set; }
        public ulong Size { get; set; }
        public ulong FileOffset { get; set; }
        public ulong FileSize { get; set; }
        public MachVmProtection MaximumProtection { get; set; }
        public MachVmProtection InitialProtection { get; set; }
        public uint NumberOfSections { get; set; }
        public MachSegmentFlags Flags { get; set; }
    }
}