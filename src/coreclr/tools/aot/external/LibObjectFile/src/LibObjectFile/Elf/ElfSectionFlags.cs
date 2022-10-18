// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines the flag of a section.
    /// </summary>
    [Flags]
    public enum ElfSectionFlags : uint
    {
        None = 0,

        /// <summary>
        /// Writable
        /// </summary>
        Write = ElfNative.SHF_WRITE,

        /// <summary>
        /// Occupies memory during execution
        /// </summary>
        Alloc = ElfNative.SHF_ALLOC,

        /// <summary>
        /// Executable
        /// </summary>
        Executable = ElfNative.SHF_EXECINSTR,

        /// <summary>
        /// Might be merged
        /// </summary>
        Merge = ElfNative.SHF_MERGE,

        /// <summary>
        /// Contains nul-terminated strings
        /// </summary>
        Strings = ElfNative.SHF_STRINGS,

        /// <summary>
        /// `sh_info' contains SHT index
        /// </summary>
        InfoLink = ElfNative.SHF_INFO_LINK,

        /// <summary>
        /// Preserve order after combining
        /// </summary>
        LinkOrder = ElfNative.SHF_LINK_ORDER,

        /// <summary>
        /// Non-standard OS specific handling required
        /// </summary>
        OsNonConforming = ElfNative.SHF_OS_NONCONFORMING,

        /// <summary>
        /// Section is member of a group. 
        /// </summary>
        Group = ElfNative.SHF_GROUP,

        /// <summary>
        /// Section hold thread-local data. 
        /// </summary>
        Tls = ElfNative.SHF_TLS,

        /// <summary>
        /// Section with compressed data.
        /// </summary>
        Compressed = ElfNative.SHF_COMPRESSED,
    }
}