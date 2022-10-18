// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines a symbol binding 
    /// This is the value seen compressed in <see cref="ElfNative.Elf32_Sym.st_info"/> or <see cref="ElfNative.Elf64_Sym.st_info"/>
    /// as well as the various defines (e.g <see cref="ElfNative.STB_LOCAL"/>).
    /// </summary>
    public enum ElfSymbolBind : byte
    {
        /// <summary>
        /// Local symbol
        /// </summary>
        Local = ElfNative.STB_LOCAL,
        
        /// <summary>
        /// Global symbol
        /// </summary>
        Global = ElfNative.STB_GLOBAL,

        /// <summary>
        /// Weak symbol
        /// </summary>
        Weak = ElfNative.STB_WEAK,

        /// <summary>
        /// Unique symbol
        /// </summary>
        GnuUnique = ElfNative.STB_GNU_UNIQUE,

        /// <summary>
        /// OS-specific 0
        /// </summary>
        SpecificOS0 = ElfNative.STB_GNU_UNIQUE,

        /// <summary>
        /// OS-specific 1
        /// </summary>
        SpecificOS1 = ElfNative.STB_GNU_UNIQUE + 1,

        /// <summary>
        /// OS-specific 2
        /// </summary>
        SpecificOS2 = ElfNative.STB_GNU_UNIQUE + 2,

        /// <summary>
        /// Processor-specific 0
        /// </summary>
        SpecificProcessor0 = ElfNative.STB_LOPROC,

        /// <summary>
        /// Processor-specific 1
        /// </summary>
        SpecificProcessor1 = ElfNative.STB_LOPROC + 1,

        /// <summary>
        /// Processor-specific 2
        /// </summary>
        SpecificProcessor2 = ElfNative.STB_LOPROC + 2,
    }
}