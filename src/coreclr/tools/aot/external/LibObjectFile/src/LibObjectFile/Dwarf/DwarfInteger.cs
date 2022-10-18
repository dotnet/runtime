// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.Dwarf
{
    [StructLayout(LayoutKind.Explicit)]
    public struct DwarfInteger
    {
        [FieldOffset(0)]
        public ulong U64;

        [FieldOffset(0)]
        public long I64;

        [FieldOffset(0)]
        public sbyte I8;

        [FieldOffset(0)]
        public byte U8;

        [FieldOffset(0)]
        public short I16;

        [FieldOffset(0)]
        public ushort U16;

        [FieldOffset(0)]
        public int I32;

        [FieldOffset(0)]
        public uint U32;

        public override string ToString()
        {
            return $"0x{U64:x16}";
        }
    }
}