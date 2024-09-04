using System.Diagnostics;

namespace Melanzana.MachO
{
    public class MachSymbolTable : MachLoadCommand
    {
        private readonly MachObjectFile objectFile;
        private readonly MachLinkEditData symbolTableData;
        private readonly MachLinkEditData stringTableData;
        private readonly Dictionary<byte, MachSection> sectionMap;
        private MachSymbolTableCollection? symbolTableCollection;

        public MachSymbolTable(MachObjectFile objectFile)
        {
            if (objectFile is null) throw new ArgumentNullException(nameof(objectFile));

            this.objectFile = objectFile;
            this.symbolTableData = new MachLinkEditData();
            this.stringTableData = new MachLinkEditData();
            this.sectionMap = new Dictionary<byte, MachSection>();
        }

        internal MachSymbolTable(
            MachObjectFile objectFile,
            MachLinkEditData symbolTableData,
            MachLinkEditData stringTableData)
        {
            if (objectFile is null) throw new ArgumentNullException(nameof(objectFile));
            if (symbolTableData is null) throw new ArgumentNullException(nameof(symbolTableData));
            if (stringTableData is null) throw new ArgumentNullException(nameof(stringTableData));

            this.objectFile = objectFile;
            this.symbolTableData = symbolTableData;
            this.stringTableData = stringTableData;

            // Create a section map now since the section indexes may change later
            sectionMap = new Dictionary<byte, MachSection>();
            byte sectionIndex = 1;
            foreach (var section in objectFile.Segments.SelectMany(segment => segment.Sections))
            {
                sectionMap.Add(sectionIndex++, section);
                Debug.Assert(sectionIndex != 0);
            }
        }

        public MachLinkEditData SymbolTableData
        {
            get
            {
                symbolTableCollection?.FlushIfDirty();
                return symbolTableData;
            }
        }

        public MachLinkEditData StringTableData
        {
            get
            {
                symbolTableCollection?.FlushIfDirty();
                return stringTableData;
            }
        }

        public IList<MachSymbol> Symbols
        {
            get
            {
                symbolTableCollection ??= new MachSymbolTableCollection(objectFile, symbolTableData, stringTableData, sectionMap);
                return symbolTableCollection;
            }
        }

        internal override IEnumerable<MachLinkEditData> LinkEditData
        {
            get
            {
                yield return SymbolTableData;
                yield return StringTableData;
            }
        }
    }
}
