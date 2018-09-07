// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
        public struct ImportSectionEntry
        {
            [XmlAttribute("Index")]
            public int Index { get; set; }
            public int StartOffset { get; set; }
            public long Section { get; set; }
            public uint SignatureRVA { get; set; }
            public string Signature { get; set; }
            public ImportSectionEntry(int index, int startOffset, long section, uint signatureRVA, string signature)
            {
                Index = index;
                StartOffset = startOffset;
                Section = section;
                SignatureRVA = signatureRVA;
                Signature = signature;
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat("+{0:X4}", StartOffset);
                builder.AppendFormat("  Section: 0x{0:X8}", Section);
                builder.AppendFormat("  SignatureRVA: 0x{0:X8}", SignatureRVA);
                builder.AppendFormat("  {0}", Signature);
                return builder.ToString();
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
        [XmlIgnore]
        public BaseGcInfo AuxiliaryData { get; set; }

        public R2RImportSection(int index, byte[] image, int rva, int size, CorCompileImportFlags flags, byte type, byte entrySize, int signatureRVA, List<ImportSectionEntry> entries, int auxDataRVA, int auxDataOffset, Machine machine, ushort majorVersion)
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
            AuxiliaryData = null;
            if (AuxiliaryDataRVA != 0)
            {
                if (machine == Machine.Amd64)
                {
                    AuxiliaryData = new Amd64.GcInfo(image, auxDataOffset, machine, majorVersion);
                }
                else if (machine == Machine.I386)
                {
                    AuxiliaryData = new x86.GcInfo(image, auxDataOffset, machine, majorVersion);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"SectionRVA: 0x{SectionRVA:X8} ({SectionRVA})");
            sb.AppendLine($"SectionSize: {SectionSize} bytes");
            sb.AppendLine($"Flags: {Flags}");
            sb.AppendLine($"Type: {Type}");
            sb.AppendLine($"EntrySize: {EntrySize}");
            sb.AppendLine($"SignatureRVA: 0x{SignatureRVA:X8} ({SignatureRVA})");
            sb.AppendLine($"AuxiliaryDataRVA: 0x{AuxiliaryDataRVA:X8} ({AuxiliaryDataRVA})");
            if (AuxiliaryDataRVA != 0 && AuxiliaryData != null)
            {
                sb.AppendLine("AuxiliaryData:");
                sb.AppendLine(AuxiliaryData.ToString());
            }
            return sb.ToString();
        }
    }
}
