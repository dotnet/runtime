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
        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/corcompile.h">src/inc/corcompile.h</a> CorCompileImportType
        /// </summary>
        public enum CorCompileImportType
        {
            CORCOMPILE_IMPORT_TYPE_UNKNOWN = 0,
            CORCOMPILE_IMPORT_TYPE_EXTERNAL_METHOD = 1,
            CORCOMPILE_IMPORT_TYPE_STUB_DISPATCH = 2,
            CORCOMPILE_IMPORT_TYPE_STRING_HANDLE = 3,
            CORCOMPILE_IMPORT_TYPE_TYPE_HANDLE = 4,
            CORCOMPILE_IMPORT_TYPE_METHOD_HANDLE = 5,
            CORCOMPILE_IMPORT_TYPE_VIRTUAL_METHOD = 6,
        };

        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/corcompile.h">src/inc/corcompile.h</a> CorCompileImportFlags
        /// </summary>
        public enum CorCompileImportFlags
        {
            CORCOMPILE_IMPORT_FLAGS_UNKNOWN = 0x0000,
            CORCOMPILE_IMPORT_FLAGS_EAGER = 0x0001,   // Section at module load time.
            CORCOMPILE_IMPORT_FLAGS_CODE = 0x0002,   // Section contains code.
            CORCOMPILE_IMPORT_FLAGS_PCODE = 0x0004,   // Section contains pointers to code.
        };

        public struct ImportSectionEntry
        {
            [XmlAttribute("Index")]
            public int Index { get; set; }
            public int StartOffset { get; set; }
            public long Section { get; set; }
            public uint SignatureRVA { get; set; }
            public byte[] SignatureSample { get; set; }
            public ImportSectionEntry(int index, int startOffset, long section, uint signatureRVA, byte[] signatureSample)
            {
                Index = index;
                StartOffset = startOffset;
                Section = section;
                SignatureRVA = signatureRVA;
                SignatureSample = signatureSample;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($@"+{StartOffset:X4}  Section: 0x{Section:X8}  SignatureRVA: 0x{SignatureRVA:X8}  ");
                foreach (byte b in SignatureSample)
                {
                    sb.AppendFormat("{0:X2} ", b);
                }
                sb.Append("...");
                return sb.ToString();
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
            if (AuxiliaryDataRVA != 0)
            {
                sb.AppendLine("AuxiliaryData:");
                sb.AppendLine(AuxiliaryData.ToString());
            }
            return sb.ToString();
        }
    }
}
