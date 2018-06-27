// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace R2RDump
{
    public struct R2RImportSection
    {
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

        public enum CorCompileImportFlags
        {
            CORCOMPILE_IMPORT_FLAGS_UNKNOWN = 0x0000,
            CORCOMPILE_IMPORT_FLAGS_EAGER = 0x0001,   // Section at module load time.
            CORCOMPILE_IMPORT_FLAGS_CODE = 0x0002,   // Section contains code.
            CORCOMPILE_IMPORT_FLAGS_PCODE = 0x0004,   // Section contains pointers to code.
        };

        public struct ImportSectionEntry
        {
            public long Section { get; set; }
            public uint SignatureRVA { get; set; }
            public uint Signature { get; set; }
            public ImportSectionEntry(long section, uint signatureRVA, uint signature)
            {
                Section = section;
                SignatureRVA = signatureRVA;
                Signature = signature;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"\tSection: 0x{Section:X8} ({Section})");
                sb.AppendLine($"\tSignatureRVA: 0x{SignatureRVA:X8} ({SignatureRVA})");
                sb.AppendLine($"\tSection: 0x{Signature:X8} ({Signature})");
                return sb.ToString();
            }
        }

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
        public GcInfo AuxiliaryData { get; set; }

        public R2RImportSection(byte[] image, int rva, int size, CorCompileImportFlags flags, byte type, byte entrySize, int signatureRVA, List<ImportSectionEntry> entries, int auxDataRVA, int auxDataOffset, Machine machine, ushort majorVersion)
        {
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
                AuxiliaryData = new GcInfo(image, auxDataOffset, machine, majorVersion);
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
