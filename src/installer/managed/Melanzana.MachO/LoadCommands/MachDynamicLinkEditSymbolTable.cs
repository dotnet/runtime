using Melanzana.MachO.BinaryFormat;
using System.Diagnostics;

namespace Melanzana.MachO
{
    public class MachDynamicLinkEditSymbolTable : MachLoadCommand
    {
        private readonly Stream stream;
        private readonly MachObjectFile objectFile;
        internal DynamicSymbolTableCommandHeader Header;

        public MachDynamicLinkEditSymbolTable(MachObjectFile objectFile, Stream stream)
        {
            this.objectFile = objectFile ?? throw new ArgumentNullException(nameof(objectFile));
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.Header = new DynamicSymbolTableCommandHeader();
        }

        public MachDynamicLinkEditSymbolTable(MachObjectFile objectFile, Stream stream, MachSymbolTable symbolTable)
            : this(objectFile, stream)
        {
            Span<bool> bucketUsed = stackalloc bool[3];
            var symbols = symbolTable.Symbols;
            int lastBucket = -1;
            bool needsSort = false;

            for (int i = 0; i < symbols.Count; i++)
            {
                int bucket =
                    symbols[i].IsUndefined ? 2 :
                    (symbols[i].IsExternal ? 1 : 0);

                if (bucket != lastBucket)
                {
                    switch (lastBucket)
                    {
                        case 0: LocalSymbolsCount = (uint)i - LocalSymbolsIndex; break;
                        case 1: ExternalSymbolsCount = (uint)i - ExternalSymbolsIndex; break;
                        case 2: UndefinedSymbolsCount = (uint)i - UndefinedSymbolsIndex; break;
                    }

                    if (bucketUsed[bucket])
                    {
                        // Same types of symbols have to be next to each other
                        throw new InvalidOperationException("Symbol table is not in correct order");
                    }
                    bucketUsed[bucket] = true;

                    switch (bucket)
                    {
                        case 0: LocalSymbolsIndex = (uint)i; needsSort = false; break;
                        case 1: ExternalSymbolsIndex = (uint)i; needsSort = true; break;
                        case 2: UndefinedSymbolsIndex = (uint)i; needsSort = true; break;
                    }
                    lastBucket = bucket;
                }
                else if (needsSort && string.CompareOrdinal(symbols[i - 1].Name, symbols[i].Name) > 0)
                {
                    // External and undefined symbols have to be lexicographically sorted
                    throw new InvalidOperationException("Symbol table is not sorted");
                }
            }

            switch (lastBucket)
            {
                case 0: LocalSymbolsCount = (uint)symbols.Count - LocalSymbolsIndex; break;
                case 1: ExternalSymbolsCount = (uint)symbols.Count - ExternalSymbolsIndex; break;
                case 2: UndefinedSymbolsCount = (uint)symbols.Count - UndefinedSymbolsIndex; break;
            }
        }

        internal MachDynamicLinkEditSymbolTable(MachObjectFile objectFile, Stream stream, DynamicSymbolTableCommandHeader header)
        {
            this.objectFile = objectFile ?? throw new ArgumentNullException(nameof(objectFile));
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.Header = header;
        }

        public uint LocalSymbolsIndex
        {
            get => Header.LocalSymbolsIndex;
            set => Header.LocalSymbolsIndex = value;
        }

        public uint LocalSymbolsCount
        {
            get => Header.LocalSymbolsCount;
            set => Header.LocalSymbolsCount = value;
        }

        public uint ExternalSymbolsIndex
        {
            get => Header.ExternalSymbolsIndex;
            set => Header.ExternalSymbolsIndex = value;
        }

        public uint ExternalSymbolsCount
        {
            get => Header.ExternalSymbolsCount;
            set => Header.ExternalSymbolsCount = value;
        }

        public uint UndefinedSymbolsIndex
        {
            get => Header.UndefinedSymbolsIndex;
            set => Header.UndefinedSymbolsIndex = value;
        }

        public uint UndefinedSymbolsCount
        {
            get => Header.UndefinedSymbolsCount;
            set => Header.UndefinedSymbolsCount = value;
        }

        public uint TableOfContentsOffset
        {
            get => Header.TableOfContentsOffset;
            set => Header.TableOfContentsOffset = value;
        }

        public uint TableOfContentsCount
        {
            get => Header.TableOfContentsCount;
            set => Header.TableOfContentsCount = value;
        }

        public uint ModuleTableOffset
        {
            get => Header.ModuleTableOffset;
            set => Header.ModuleTableOffset = value;
        }

        public uint ModuleTableCount
        {
            get => Header.ModuleTableCount;
            set => Header.ModuleTableCount = value;
        }

        public uint ExternalReferenceTableOffset
        {
            get => Header.ExternalReferenceTableOffset;
            set => Header.ExternalReferenceTableOffset = value;
        }

        public uint ExternalReferenceTableCount
        {
            get => Header.ExternalReferenceTableCount;
            set => Header.ExternalReferenceTableCount = value;
        }

        public uint IndirectSymbolTableOffset
        {
            get => Header.IndirectSymbolTableOffset;
            set => Header.IndirectSymbolTableOffset = value;
        }

        public uint IndirectSymbolTableCount
        {
            get => Header.IndirectSymbolTableCount;
            set => Header.IndirectSymbolTableCount = value;
        }

        public uint ExternalRelocationTableOffset
        {
            get => Header.ExternalRelocationTableOffset;
            set => Header.ExternalRelocationTableOffset = value;
        }

        public uint ExternalRelocationTableCount
        {
            get => Header.ExternalRelocationTableCount;
            set => Header.ExternalRelocationTableCount = value;
        }

        public uint LocalRelocationTableOffset
        {
            get => Header.LocalRelocationTableOffset;
            set => Header.LocalRelocationTableOffset = value;
        }

        public uint LocalRelocationTableCount
        {
            get => Header.LocalRelocationTableCount;
            set => Header.LocalRelocationTableCount = value;
        }

        internal override IEnumerable<MachLinkEditData> LinkEditData
        {
            get
            {
                // The sizes of the entries in the various tables are defined in loader.h and reloc.h, e.g.
                // - https://opensource.apple.com/source/xnu/xnu-2050.18.24/EXTERNAL_HEADERS/mach-o/loader.h
                // - https://opensource.apple.com/source/xnu/xnu-2050.18.24/EXTERNAL_HEADERS/mach-o/reloc.h

                // dylib_table_of_contents in loader.h
                const int TableOfContentsEntrySize = 8;

                // dylib_module[_64] in loader.h
                var moduleTableEntrySize = this.objectFile.Is64Bit ? 56u : 52u;

                // dylib_reference in loader.h
                const int ReferenceTableEntrySize = 4;

                const int IndirectSymbolTableEntrySize = 4;

                // relocation_info in reloc.h
                const int RelocationTableEntrySize = 8;

                yield return new MachLinkEditData(stream, TableOfContentsOffset, TableOfContentsCount * TableOfContentsEntrySize);

                yield return new MachLinkEditData(stream, ModuleTableOffset, ModuleTableCount * moduleTableEntrySize);

                yield return new MachLinkEditData(stream, ExternalReferenceTableOffset, ExternalReferenceTableCount * ReferenceTableEntrySize);

                yield return new MachLinkEditData(stream, IndirectSymbolTableOffset, IndirectSymbolTableCount * IndirectSymbolTableEntrySize);

                yield return new MachLinkEditData(stream, ExternalRelocationTableOffset, ExternalRelocationTableCount * ReferenceTableEntrySize);

                yield return new MachLinkEditData(stream, LocalRelocationTableOffset, LocalRelocationTableCount * RelocationTableEntrySize);
            }
        }
    }
}