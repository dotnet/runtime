// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Encoding of an <see cref="ElfObjectFile"/>.
    /// This is the value seen in the ident part of an Elf header at index <see cref="ElfNative.EI_DATA"/>
    /// It is associated with <see cref="ElfNative.ELFDATANONE"/>, <see cref="ElfNative.ELFDATA2LSB"/> and <see cref="ElfNative.ELFDATA2MSB"/>
    /// </summary>
    public enum ElfEncoding : byte
    {
        /// <summary>
        /// Invalid data encoding. Equivalent of <see cref="ElfNative.ELFDATANONE"/>
        /// </summary>
        None = ElfNative.ELFDATANONE,

        /// <summary>
        /// 2's complement, little endian. Equivalent of <see cref="ElfNative.ELFDATA2LSB"/>
        /// </summary>
        Lsb = ElfNative.ELFDATA2LSB,

        /// <summary>
        /// 2's complement, big endian. Equivalent of <see cref="ElfNative.ELFDATA2MSB"/>
        /// </summary>
        Msb = ElfNative.ELFDATA2MSB,
    }
}