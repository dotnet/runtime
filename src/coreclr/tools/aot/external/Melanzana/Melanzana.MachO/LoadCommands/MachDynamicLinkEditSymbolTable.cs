using Melanzana.MachO.BinaryFormat;

namespace Melanzana.MachO
{
    public class MachDynamicLinkEditSymbolTable : MachLoadCommand
    {
        internal DynamicSymbolTableCommandHeader Header;

        public MachDynamicLinkEditSymbolTable()
        {
            this.Header = new DynamicSymbolTableCommandHeader();
        }

        public MachDynamicLinkEditSymbolTable(MachSymbolTable symbolTable)
            : this()
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

        internal MachDynamicLinkEditSymbolTable(DynamicSymbolTableCommandHeader header)
        {
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
    }
}