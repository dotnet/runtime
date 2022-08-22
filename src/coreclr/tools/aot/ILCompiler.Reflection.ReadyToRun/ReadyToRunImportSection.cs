// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using Internal.ReadyToRunConstants;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/readytorun.h">src/inc/readytorun.h</a> READYTORUN_IMPORT_SECTION
    /// </summary>
    public struct ReadyToRunImportSection
    {
        public class ImportSectionEntry
        {
            public int Index { get; set; }
            public int StartOffset { get; set; }
            public int StartRVA { get; set; }
            public long Section { get; set; }
            public uint SignatureRVA { get; set; }
            public ReadyToRunSignature Signature { get; set; }
            public GCRefMap GCRefMap { get; set; }

            public ImportSectionEntry()
            {
            }

            public ImportSectionEntry(int index, int startOffset, int startRVA, long section, uint signatureRVA, ReadyToRunSignature signature)
            {
                Index = index;
                StartOffset = startOffset;
                StartRVA = startRVA;
                Section = section;
                SignatureRVA = signatureRVA;
                Signature = signature;
            }
        }

        public int Index { get; set; }

        /// <summary>
        /// Section containing values to be fixed up
        /// </summary>
        public int SectionRVA { get; set; }
        public int SectionSize { get; set; }

        /// <summary>
        /// One or more of ImportSectionFlags
        /// </summary>
        public ReadyToRunImportSectionFlags Flags { get; set; }

        /// <summary>
        /// One of ImportSectionType
        /// </summary>
        public ReadyToRunImportSectionType Type { get; set; }

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

        public ReadyToRunImportSection(
            int index,
            ReadyToRunReader reader,
            int rva,
            int size,
            ReadyToRunImportSectionFlags flags,
            ReadyToRunImportSectionType type,
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
            Type = type;
            EntrySize = entrySize;

            SignatureRVA = signatureRVA;
            Entries = entries;

            AuxiliaryDataRVA = auxDataRVA;
            AuxiliaryDataSize = 0;
            if (AuxiliaryDataRVA != 0)
            {
                int endOffset = auxDataOffset + BitConverter.ToInt32(reader.Image, auxDataOffset);

                for (int i = 0; i < Entries.Count; i++)
                {
                    int entryStartOffset = auxDataOffset + BitConverter.ToInt32(reader.Image, auxDataOffset + sizeof(int) * (Entries[i].Index / GCRefMap.GCREFMAP_LOOKUP_STRIDE));
                    int remaining = Entries[i].Index % GCRefMap.GCREFMAP_LOOKUP_STRIDE;
                    while (remaining != 0)
                    {
                        while ((reader.Image[entryStartOffset] & 0x80) != 0)
                        {
                            entryStartOffset++;
                        }

                        entryStartOffset++;
                        remaining--;
                    }

                    GCRefMapDecoder decoder = new GCRefMapDecoder(reader, entryStartOffset);
                    Entries[i].GCRefMap = decoder.ReadMap();
                    endOffset = decoder.GetOffset();
                }

                AuxiliaryDataSize = endOffset - auxDataOffset;
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
