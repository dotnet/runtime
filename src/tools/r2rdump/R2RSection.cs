using System;
using System.Collections.Generic;
using System.Text;

namespace R2RDump
{
    struct R2RSection
    {
        public enum SectionType
        {
            READYTORUN_SECTION_COMPILER_IDENTIFIER = 100,
            READYTORUN_SECTION_IMPORT_SECTIONS = 101,
            READYTORUN_SECTION_RUNTIME_FUNCTIONS = 102,
            READYTORUN_SECTION_METHODDEF_ENTRYPOINTS = 103,
            READYTORUN_SECTION_EXCEPTION_INFO = 104,
            READYTORUN_SECTION_DEBUG_INFO = 105,
            READYTORUN_SECTION_DELAYLOAD_METHODCALL_THUNKS = 106,
            READYTORUN_SECTION_AVAILABLE_TYPES = 108,
            READYTORUN_SECTION_INSTANCE_METHOD_ENTRYPOINTS = 109,
            READYTORUN_SECTION_INLINING_INFO = 110,
            READYTORUN_SECTION_PROFILEDATA_INFO = 111
        }

        /// <summary>
        /// The ReadyToRun section type
        /// </summary>
        public SectionType Type { get; }

        /// <summary>
        /// The RVA to the section
        /// </summary>
        public int RelativeVirtualAddress { get; }

        /// <summary>
        /// The size of the section
        /// </summary>
        public int Size { get; }

        public R2RSection(SectionType type, int rva, int size)
        {
            Type = type;
            RelativeVirtualAddress = rva;
            Size = size;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat($"Type:  {Enum.GetName(typeof(SectionType), Type)} ({Type:D})\n");
            sb.AppendFormat($"RelativeVirtualAddress: 0x{RelativeVirtualAddress:X8}\n");
            sb.AppendFormat($"Size: {Size} bytes\n");
            return sb.ToString();
        }
    }
}
