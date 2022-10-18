// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines a segment flags
    /// </summary>
    public readonly struct ElfSegmentFlags : IEquatable<ElfSegmentFlags>
    {
        public ElfSegmentFlags(uint value)
        {
            Value = value;
        }

        public ElfSegmentFlags(ElfSegmentFlagsCore value)
        {
            Value = (uint)value;
        }
        
        public readonly uint Value;

        public bool Equals(ElfSegmentFlags other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfSegmentFlags other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Value;
        }

        public static bool operator ==(ElfSegmentFlags left, ElfSegmentFlags right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfSegmentFlags left, ElfSegmentFlags right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"SegmentFlags {((ElfSegmentFlagsCore)(Value&3))} 0x{Value:X8}";
        }

        public static implicit operator ElfSegmentFlags(ElfSegmentFlagsCore segmentFlagsCore)
        {
            return new ElfSegmentFlags(segmentFlagsCore);
        }
    }
}