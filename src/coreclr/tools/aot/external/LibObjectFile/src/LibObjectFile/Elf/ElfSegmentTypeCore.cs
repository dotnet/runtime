// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines a core <see cref="ElfSegmentType"/>
    /// </summary>
    public enum ElfSegmentTypeCore : uint
    {
        /// <summary>
        /// Program header table entry unused
        /// </summary>
        Null = ElfNative.PT_NULL,

        /// <summary>
        /// Loadable program segment
        /// </summary>
        Load = ElfNative.PT_LOAD,

        /// <summary>
        /// Dynamic linking information
        /// </summary>
        Dynamic = ElfNative.PT_DYNAMIC,

        /// <summary>
        /// Program interpreter
        /// </summary>
        Interpreter = ElfNative.PT_INTERP,

        /// <summary>
        /// Auxiliary information
        /// </summary>
        Note = ElfNative.PT_NOTE,

        /// <summary>
        /// Reserved
        /// </summary>
        SectionHeaderLib = ElfNative.PT_SHLIB,

        /// <summary>
        /// Entry for header table itself
        /// </summary>
        ProgramHeader = ElfNative.PT_PHDR,

        /// <summary>
        /// Thread-local storage segment
        /// </summary>
        Tls = ElfNative.PT_TLS,
    }
}