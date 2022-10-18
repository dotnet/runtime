// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A decoder interface for the various Elf types that provides decoding of data based on LSB/MSB.
    /// </summary>
    /// <seealso cref="IElfEncoder"/>
    public interface IElfDecoder
    {
        ushort Decode(ElfNative.Elf32_Half src);

        ushort Decode(ElfNative.Elf64_Half src);

        uint Decode(ElfNative.Elf32_Word src);

        uint Decode(ElfNative.Elf64_Word src);

        int Decode(ElfNative.Elf32_Sword src);

        int Decode(ElfNative.Elf64_Sword src);

        ulong Decode(ElfNative.Elf32_Xword src);

        long Decode(ElfNative.Elf32_Sxword src);

        ulong Decode(ElfNative.Elf64_Xword src);

        long Decode(ElfNative.Elf64_Sxword src);

        uint Decode(ElfNative.Elf32_Addr src);

        ulong Decode(ElfNative.Elf64_Addr src);

        uint Decode(ElfNative.Elf32_Off src);

        ulong Decode(ElfNative.Elf64_Off src);

        ushort Decode(ElfNative.Elf32_Section src);

        ushort Decode(ElfNative.Elf64_Section src);

        ushort Decode(ElfNative.Elf32_Versym src);

        ushort Decode(ElfNative.Elf64_Versym src);
    }
}