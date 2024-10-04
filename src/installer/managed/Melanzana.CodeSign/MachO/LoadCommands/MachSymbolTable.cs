// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
