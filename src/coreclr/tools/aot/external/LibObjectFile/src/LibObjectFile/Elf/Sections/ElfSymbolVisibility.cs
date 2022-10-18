// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines the visibility of a symbol
    /// This is the value seen compressed in <see cref="ElfNative.Elf32_Sym.st_other"/> or <see cref="ElfNative.Elf64_Sym.st_other"/>
    /// as well as the various defines (e.g <see cref="ElfNative.STV_INTERNAL"/>).
    /// </summary>
    public enum ElfSymbolVisibility : byte
    {
        /// <summary>
        /// Default symbol visibility rules 
        /// </summary>
        Default = ElfNative.STV_DEFAULT,

        /// <summary>
        /// Processor specific hidden class
        /// </summary>
        Internal = ElfNative.STV_INTERNAL,

        /// <summary>
        /// Sym unavailable in other modules
        /// </summary>
        Hidden = ElfNative.STV_HIDDEN,

        /// <summary>
        /// Not preemptible, not exported 
        /// </summary>
        Protected = ElfNative.STV_PROTECTED,
    }
}