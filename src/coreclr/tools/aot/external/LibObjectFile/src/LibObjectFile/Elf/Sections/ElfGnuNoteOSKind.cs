// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Type of Operating System for a <see cref="ElfGnuNoteABITag"/>
    /// </summary>
    public enum ElfGnuNoteOSKind : uint
    {
        /// <summary>
        /// Linux operating system.
        /// </summary>
        Linux = ElfNative.ELF_NOTE_OS_LINUX,

        /// <summary>
        /// A Gnu operating system.
        /// </summary>
        Gnu = ElfNative.ELF_NOTE_OS_GNU,

        /// <summary>
        /// Solaris operating system.
        /// </summary>
        Solaris = ElfNative.ELF_NOTE_OS_SOLARIS2,

        /// <summary>
        /// FreeBSD operating system.
        /// </summary>
        FreeBSD = ElfNative.ELF_NOTE_OS_FREEBSD,
    }
}