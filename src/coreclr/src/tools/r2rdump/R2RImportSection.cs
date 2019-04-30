// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Serialization;

namespace R2RDump
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/readytorun.h">src/inc/readytorun.h</a> READYTORUN_IMPORT_SECTION
    /// </summary>
    public struct R2RImportSection
    {
        public class ImportSectionEntry
        {
            [XmlAttribute("Index")]
            public int Index { get; set; }
            public int StartOffset { get; set; }
            public int StartRVA { get; set; }
            public long Section { get; set; }
            public uint SignatureRVA { get; set; }
            public string Signature { get; set; }
            public GCRefMap GCRefMap { get; set; }

            public ImportSectionEntry()
            {
            }

            public ImportSectionEntry(int index, int startOffset, int startRVA, long section, uint signatureRVA, string signature)
            {
                Index = index;
                StartOffset = startOffset;
                StartRVA = startRVA;
                Section = section;
                SignatureRVA = signatureRVA;
                Signature = signature;
            }

            public void WriteTo(TextWriter writer, DumpOptions options)
            {
                if (!options.Naked)
                {
                    writer.Write($"+{StartOffset:X4}");
                    writer.Write($" ({StartRVA:X4})");
                    writer.Write($"  Section: 0x{Section:X8}");
                    writer.Write($"  SignatureRVA: 0x{SignatureRVA:X8}");
                    writer.Write("   ");
                }
                writer.Write(Signature);
                if (GCRefMap != null)
                {
                    writer.Write(" -- ");
                    GCRefMap.WriteTo(writer);
                }
            }
        }

        [XmlAttribute("Index")]
        public int Index { get; set; }

        /// <summary>
        /// Section containing values to be fixed up
        /// </summary>
        public int SectionRVA { get; set; }
        public int SectionSize { get; set; }

        /// <summary>
        /// One or more of ImportSectionFlags
        /// </summary>
        public CorCompileImportFlags Flags { get; set; }

        /// <summary>
        /// One of ImportSectionType
        /// </summary>
        public CorCompileImportType Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public byte EntrySize { get; set; }

        /// <summary>
        /// RVA of optional signature descriptors
        /// </summary>
        public int SignatureRVA { get; set; }
        public List<ImportSectionEntry> Entries { get; set; }

        /// <summary>
        /// RVA of optional auxiliary data (typically GC info)
        /// </summary>
        public int AuxiliaryDataRVA { get; set; }

        public int AuxiliaryDataSize { get; set; }

        public R2RImportSection(
            int index, 
            R2RReader reader, 
            int rva, 
            int size, 
            CorCompileImportFlags flags, 
            byte type, 
            byte entrySize, 
            int signatureRVA, 
            List<ImportSectionEntry> entries, 
            int auxDataRVA, 
            int auxDataOffset, 
            Machine machine, 
            ushort majorVersion)
        {
            Index = index;
            SectionRVA = rva;
            SectionSize = size;
            Flags = flags;
            Type = (CorCompileImportType)type;
            EntrySize = entrySize;

            SignatureRVA = signatureRVA;
            Entries = entries;

            AuxiliaryDataRVA = auxDataRVA;
            AuxiliaryDataSize = 0;
            if (AuxiliaryDataRVA != 0)
            {
                int startOffset = auxDataOffset + BitConverter.ToInt32(reader.Image, auxDataOffset);

                for (int i = 0; i < Entries.Count; i++)
                {
                    GCRefMapDecoder decoder = new GCRefMapDecoder(reader, startOffset);
                    Entries[i].GCRefMap = decoder.ReadMap();
                    startOffset = decoder.GetOffset();
                }

                AuxiliaryDataSize = startOffset - auxDataOffset;
            }
        }

        public void WriteTo(TextWriter writer)
        {
            writer.WriteLine($"SectionRVA: 0x{SectionRVA:X8} ({SectionRVA})");
            writer.WriteLine($"SectionSize: {SectionSize} bytes");
            writer.WriteLine($"Flags: {Flags}");
            writer.WriteLine($"Type: {Type}");
            writer.WriteLine($"EntrySize: {EntrySize}");
            writer.WriteLine($"SignatureRVA: 0x{SignatureRVA:X8} ({SignatureRVA})");
            writer.WriteLine($"AuxiliaryDataRVA: 0x{AuxiliaryDataRVA:X8} ({AuxiliaryDataRVA})");
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
