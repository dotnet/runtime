// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// An encoder for the various Elf types that doesn't change LSB/MSB ordering from the current machine.
    /// </summary>
    internal readonly struct ElfEncoderDirect : IElfEncoder
    {
        public void Encode(out ElfNative.Elf32_Half dest, ushort value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf64_Half dest, ushort value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf32_Word dest, uint value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf64_Word dest, uint value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf32_Sword dest, int value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf64_Sword dest, int value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf32_Xword dest, ulong value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf32_Sxword dest, long value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf64_Xword dest, ulong value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf64_Sxword dest, long value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf32_Addr dest, uint value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf64_Addr dest, ulong value)
        {
            dest = value;
        }

        public void Encode(out ElfNative.Elf32_Off dest, uint offset)
        {
            dest = offset;
        }

        public void Encode(out ElfNative.Elf64_Off dest, ulong offset)
        {
            dest = offset;
        }

        public void Encode(out ElfNative.Elf32_Section dest, ushort index)
        {
            dest = index;
        }

        public void Encode(out ElfNative.Elf64_Section dest, ushort index)
        {
            dest = index;
        }

        public void Encode(out ElfNative.Elf32_Versym dest, ushort value)
        {
            dest = (ElfNative.Elf32_Half)value;
        }

        public void Encode(out ElfNative.Elf64_Versym dest, ushort value)
        {
            dest = (ElfNative.Elf64_Half)value;
        }
    }
}