// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Native constants for the Mach-O file format.
    /// </summary>
    internal static class MachNative
    {
        public const uint MH_MAGIC_64 = 0xFEEDFACF;

        // File type
        public const uint MH_OBJECT = 0x1;

        // File header flags
        public const uint MH_SUBSECTIONS_VIA_SYMBOLS = 0x2000;

        // CPU type/subtype
        public const uint CPU_TYPE_ARM64 = 0x1000000 | 12;
        public const uint CPU_TYPE_X86_64 = 0x1000000 | 7;
        public const uint CPU_SUBTYPE_ARM64_ALL = 0;
        public const uint CPU_SUBTYPE_X86_64_ALL = 3;

        // Load command types
        public const uint LC_SYMTAB = 0x2;
        public const uint LC_DYSYMTAB = 0xB;
        public const uint LC_SEGMENT_64 = 0x19;
        public const uint LC_BUILD_VERSION = 0x32;

        // Memory protection
        public const uint VM_PROT_NONE = 0;
        public const uint VM_PROT_READ = 1;
        public const uint VM_PROT_WRITE = 2;
        public const uint VM_PROT_EXECUTE = 4;

        // Section types
        public const uint S_REGULAR = 0;
        public const uint S_ZEROFILL = 1;
        public const uint S_CSTRING_LITERALS = 2;
        public const uint S_4BYTE_LITERALS = 3;
        public const uint S_LITERAL_POINTERS = 5;
        public const uint S_NON_LAZY_SYMBOL_POINTERS = 6;
        public const uint S_LAZY_SYMBOL_POINTERS = 7;
        public const uint S_SYMBOL_STUBS = 8;
        public const uint S_MOD_INIT_FUNC_POINTERS = 9;
        public const uint S_MOD_TERM_FUNC_POINTERS = 10;
        public const uint S_COALESCED = 11;
        public const uint S_GB_ZEROFILL = 12;
        public const uint S_INTERPOSING = 13;
        public const uint S_16BYTE_LITERALS = 14;
        public const uint S_DTRACE_DOF = 15;
        public const uint S_LAZY_DYLIB_SYMBOL_POINTERS = 16;
        public const uint S_THREAD_LOCAL_REGULAR = 17;
        public const uint S_THREAD_LOCAL_ZEROFILL = 18;
        public const uint S_THREAD_LOCAL_VARIABLES = 19;
        public const uint S_THREAD_LOCAL_VARIABLE_POINTERS = 20;
        public const uint S_THREAD_LOCAL_INIT_FUNCTION_POINTERS = 21;
        public const uint S_INIT_FUNC_OFFSETS = 22;

        // Section flags
        public const uint S_ATTR_SOME_INSTRUCTIONS = 0x400;
        public const uint S_ATTR_DEBUG = 0x2000000;
        public const uint S_ATTR_LIVE_SUPPORT = 0x8000000;
        public const uint S_ATTR_NO_DEAD_STRIP = 0x10000000;
        public const uint S_ATTR_STRIP_STATIC_SYMS = 0x20000000;
        public const uint S_ATTR_NO_TOC = 0x40000000;
        public const uint S_ATTR_PURE_INSTRUCTIONS = 0x80000000;

        // Relocation types
        public const byte X86_64_RELOC_UNSIGNED = 0;
        public const byte X86_64_RELOC_SIGNED = 1;
        public const byte X86_64_RELOC_BRANCH = 2;
        public const byte X86_64_RELOC_GOT_LOAD = 3;
        public const byte X86_64_RELOC_GOT = 4;
        public const byte X86_64_RELOC_SUBTRACTOR = 5;
        public const byte X86_64_RELOC_SIGNED_1 = 6;
        public const byte X86_64_RELOC_SIGNED_2 = 7;
        public const byte X86_64_RELOC_SIGNED_4 = 8;
        public const byte X86_64_RELOC_TLV = 9;
        public const byte ARM64_RELOC_UNSIGNED = 0;
        public const byte ARM64_RELOC_SUBTRACTOR = 1;
        public const byte ARM64_RELOC_BRANCH26 = 2;
        public const byte ARM64_RELOC_PAGE21 = 3;
        public const byte ARM64_RELOC_PAGEOFF12 = 4;
        public const byte ARM64_RELOC_GOT_LOAD_PAGE21 = 5;
        public const byte ARM64_RELOC_GOT_LOAD_PAGEOFF12 = 6;
        public const byte ARM64_RELOC_POINTER_TO_GOT = 7;
        public const byte ARM64_RELOC_TLVP_LOAD_PAGE21 = 8;
        public const byte ARM64_RELOC_TLVP_LOAD_PAGEOFF12 = 9;
        public const byte ARM64_RELOC_ADDEND = 10;

        // Symbol type
        public const byte N_UNDF = 0;
        public const byte N_EXT = 1;
        public const byte N_ABS = 2;
        public const byte N_INDR = 0xA;
        public const byte N_SECT = 0xE;
        public const byte N_PBUD = 0xC;

        // Symbol descriptor flags
        public const ushort REFERENCE_FLAG_UNDEFINED_NON_LAZY = 0;
        public const ushort REFERENCE_FLAG_UNDEFINED_LAZY = 1;
        public const ushort REFERENCE_FLAG_DEFINED = 2;
        public const ushort REFERENCE_FLAG_PRIVATE_DEFINED = 3;
        public const ushort REFERENCE_FLAG_PRIVATE_UNDEFINED_NON_LAZY = 4;
        public const ushort REFERENCE_FLAG_PRIVATE_UNDEFINED_LAZY = 5;
        public const ushort REFERENCED_DYNAMICALLY = 0x10;
        public const ushort N_NO_DEAD_STRIP = 0x20;
        public const ushort N_WEAK_REF = 0x40;
        public const ushort N_WEAK_DEF = 0x80;

        public const uint PLATFORM_MACOS = 1;
        public const uint PLATFORM_IOS = 2;
        public const uint PLATFORM_TVOS = 3;
        public const uint PLATFORM_WATCHOS = 4;
        public const uint PLATFORM_BRIDGEOS = 5;
        public const uint PLATFORM_MACCATALYST = 6;
        public const uint PLATFORM_IOSSIMULATOR = 7;
        public const uint PLATFORM_TVOSSIMULATOR = 8;
        public const uint PLATFORM_WATCHOSSIMULATOR = 9;
        public const uint PLATFORM_DRIVERKIT = 10;
    }
}
