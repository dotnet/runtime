// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Buffers.Binary;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// An encoder for the various Elf types that swap LSB/MSB ordering based on a mismatch between the current machine and file ordering.
    /// </summary>
    internal readonly struct ElfEncoderSwap : IElfEncoder
    {
        public void Encode(out ElfNative.Elf32_Half dest, ushort value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf64_Half dest, ushort value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf32_Word dest, uint value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf64_Word dest, uint value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf32_Sword dest, int value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf64_Sword dest, int value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf32_Xword dest, ulong value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf32_Sxword dest, long value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf64_Xword dest, ulong value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf64_Sxword dest, long value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf32_Addr dest, uint value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf64_Addr dest, ulong value)
        {
            dest = BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf32_Off dest, uint offset)
        {
            dest = BinaryPrimitives.ReverseEndianness(offset);
        }

        public void Encode(out ElfNative.Elf64_Off dest, ulong offset)
        {
            dest = BinaryPrimitives.ReverseEndianness(offset);
        }

        public void Encode(out ElfNative.Elf32_Section dest, ushort index)
        {
            dest = BinaryPrimitives.ReverseEndianness(index);
        }

        public void Encode(out ElfNative.Elf64_Section dest, ushort index)
        {
            dest = BinaryPrimitives.ReverseEndianness(index);
        }

        public void Encode(out ElfNative.Elf32_Versym dest, ushort value)
        {
            dest = (ElfNative.Elf32_Half)BinaryPrimitives.ReverseEndianness(value);
        }

        public void Encode(out ElfNative.Elf64_Versym dest, ushort value)
        {
            dest = (ElfNative.Elf64_Half)BinaryPrimitives.ReverseEndianness(value);
        }
    }
}