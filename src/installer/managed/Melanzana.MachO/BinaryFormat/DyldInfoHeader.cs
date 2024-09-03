namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class DyldInfoHeader
    {
        public uint RebaseOffset { get; set; }
        public uint RebaseSize { get; set; }
        public uint BindOffset { get; set; }
        public uint BindSize { get; set; }
        public uint WeakBindOffset { get; set; }
        public uint WeakBindSize { get; set; }
        public uint LazyBindOffset { get; set; }
        public uint LazyBindSize { get; set; }
        public uint ExportOffset { get; set; }
        public uint ExportSize { get; set; }
    }
}