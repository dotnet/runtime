namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class MainCommandHeader
    {
        public ulong FileOffset { get; set; }
        public ulong StackSize { get; set; }
    }
}