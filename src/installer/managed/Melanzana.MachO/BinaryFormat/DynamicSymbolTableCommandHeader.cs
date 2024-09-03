namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class DynamicSymbolTableCommandHeader
    {
        public uint LocalSymbolsIndex { get; set; }
        public uint LocalSymbolsCount { get; set; }
        public uint ExternalSymbolsIndex { get; set; }
        public uint ExternalSymbolsCount { get; set; }
        public uint UndefinedSymbolsIndex { get; set; }
        public uint UndefinedSymbolsCount { get; set; }
        public uint TableOfContentsOffset { get; set; }
        public uint TableOfContentsCount { get; set; }
        public uint ModuleTableOffset { get; set; }
        public uint ModuleTableCount { get; set; }
        public uint ExternalReferenceTableOffset { get; set; }
        public uint ExternalReferenceTableCount { get; set; }
        public uint IndirectSymbolTableOffset { get; set; }
        public uint IndirectSymbolTableCount { get; set; }
        public uint ExternalRelocationTableOffset { get; set; }
        public uint ExternalRelocationTableCount { get; set; }
        public uint LocalRelocationTableOffset { get; set; }
        public uint LocalRelocationTableCount { get; set; }
    }
}