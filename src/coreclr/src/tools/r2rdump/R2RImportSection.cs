// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace R2RDump
{
    struct R2RImportSection
    {
        public enum ImportSectionType
        {
            READYTORUN_IMPORT_SECTION_TYPE_UNKNOWN = 0,
        };

        public enum ImportSectionFlags
        {
            READYTORUN_IMPORT_SECTION_FLAGS_EAGER = 0x0001,
        };

        /// <summary>
        /// Section containing values to be fixed up
        /// </summary>
        public int SectionRVA { get; }
        public int SectionSize { get; }

        /// <summary>
        /// One or more of ImportSectionFlags
        /// </summary>
        public ushort Flags { get; }

        /// <summary>
        /// One of ImportSectionType
        /// </summary>
        public ImportSectionType Type { get; }

        /// <summary>
        /// 
        /// </summary>
        public byte EntrySize { get; }

        /// <summary>
        /// RVA of optional signature descriptors
        /// </summary>
        public int Signatures { get; }

        /// <summary>
        /// RVA of optional auxiliary data (typically GC info)
        /// </summary>
        public int AuxiliaryData { get; }

        public R2RImportSection(int rva, int size, ushort flags, byte type, byte entrySize, int sig, int data)
        {
            SectionRVA = rva;
            SectionSize = size;
            Flags = flags;
            Type = (ImportSectionType)type;
            EntrySize = entrySize;
            Signatures = sig;
            AuxiliaryData = data;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"SectionRVA: 0x{SectionRVA:X8}");
            sb.AppendLine($"SectionSize: {SectionSize} bytes");
            sb.AppendLine($"Flags: {Enum.GetName(typeof(ImportSectionFlags), Flags)}({Flags})");
            sb.AppendLine($"Type: {Type}");
            sb.AppendLine($"EntrySize: {EntrySize}");
            sb.AppendLine($"Signatures: 0x{Signatures:X8}");
            sb.AppendLine($"AuxiliaryData: {AuxiliaryData}");
            return sb.ToString();
        }
    }
}
