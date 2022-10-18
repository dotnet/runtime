// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines a symbol type.
    /// This is the value seen compressed in <see cref="ElfNative.Elf32_Sym.st_info"/> or <see cref="ElfNative.Elf64_Sym.st_info"/>
    /// as well as the various defines (e.g <see cref="ElfNative.STT_NOTYPE"/>).
    /// </summary>
    public enum ElfSymbolType : byte
    {
        /// <summary>
        /// Symbol type is unspecified
        /// </summary>
        NoType = ElfNative.STT_NOTYPE,

        /// <summary>
        /// Symbol is a data object
        /// </summary>
        Object = ElfNative.STT_OBJECT,

        /// <summary>
        /// Symbol is a code object
        /// </summary>
        Function = ElfNative.STT_FUNC,

        /// <summary>
        /// Symbol associated with a section
        /// </summary>
        Section = ElfNative.STT_SECTION,

        /// <summary>
        /// Symbol's name is file name
        /// </summary>
        File = ElfNative.STT_FILE,

        /// <summary>
        /// Symbol is a common data object
        /// </summary>
        Common = ElfNative.STT_COMMON,

        /// <summary>
        /// Symbol is thread-local data object
        /// </summary>
        Tls = ElfNative.STT_TLS,

        /// <summary>
        /// Symbol is indirect code object
        /// </summary>
        GnuIndirectFunction = ElfNative.STT_GNU_IFUNC,

        /// <summary>
        /// OS-specific 0
        /// </summary>
        SpecificOS0 = ElfNative.STT_GNU_IFUNC,

        /// <summary>
        /// OS-specific 1
        /// </summary>
        SpecificOS1 = ElfNative.STT_GNU_IFUNC + 1,

        /// <summary>
        /// OS-specific 2
        /// </summary>
        SpecificOS2 = ElfNative.STT_GNU_IFUNC + 2,

        /// <summary>
        /// Processor-specific 0
        /// </summary>
        SpecificProcessor0 = ElfNative.STT_LOPROC,

        /// <summary>
        /// Processor-specific 1
        /// </summary>
        SpecificProcessor1 = ElfNative.STT_LOPROC + 1,

        /// <summary>
        /// Processor-specific 2
        /// </summary>
        SpecificProcessor2 = ElfNative.STT_LOPROC + 2,
    }
}