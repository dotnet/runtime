// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A decoder for the various Elf types that doesn't change LSB/MSB ordering from the current machine.
    /// </summary>
    public struct ElfDecoderDirect : IElfDecoder
    {
        public ushort Decode(ElfNative.Elf32_Half src)
        {
            return src.Value;
        }

        public ushort Decode(ElfNative.Elf64_Half src)
        {
            return src.Value;
        }

        public uint Decode(ElfNative.Elf32_Word src)
        {
            return src.Value;
        }

        public uint Decode(ElfNative.Elf64_Word src)
        {
            return src.Value;
        }

        public int Decode(ElfNative.Elf32_Sword src)
        {
            return src.Value;
        }

        public int Decode(ElfNative.Elf64_Sword src)
        {
            return src.Value;
        }

        public ulong Decode(ElfNative.Elf32_Xword src)
        {
            return src.Value;
        }

        public long Decode(ElfNative.Elf32_Sxword src)
        {
            return src.Value;
        }

        public ulong Decode(ElfNative.Elf64_Xword src)
        {
            return src.Value;
        }

        public long Decode(ElfNative.Elf64_Sxword src)
        {
            return src.Value;
        }

        public uint Decode(ElfNative.Elf32_Addr src)
        {
            return src.Value;
        }

        public ulong Decode(ElfNative.Elf64_Addr src)
        {
            return src.Value;
        }

        public uint Decode(ElfNative.Elf32_Off src)
        {
            return src.Value;
        }

        public ulong Decode(ElfNative.Elf64_Off src)
        {
            return src.Value;
        }

        public ushort Decode(ElfNative.Elf32_Section src)
        {
            return src.Value;
        }

        public ushort Decode(ElfNative.Elf64_Section src)
        {
            return src.Value;
        }

        public ushort Decode(ElfNative.Elf32_Versym src)
        {
            return src.Value;
        }

        public ushort Decode(ElfNative.Elf64_Versym src)
        {
            return src.Value;
        }
    }
}