namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class TwoLevelHintsHeader
    {
        public uint FileOffset { get; set; }
        public uint NumberOfHints { get; set; }
    }
}