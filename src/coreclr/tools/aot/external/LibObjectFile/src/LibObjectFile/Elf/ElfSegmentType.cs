// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines a segment type
    /// </summary>
    public readonly struct ElfSegmentType : IEquatable<ElfSegmentType>
    {
        public ElfSegmentType(uint value)
        {
            Value = value;
        }

        public ElfSegmentType(ElfSegmentTypeCore value)
        {
            Value = (uint)value;
        }
        
        public readonly uint Value;

        public bool Equals(ElfSegmentType other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfSegmentType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Value;
        }

        public static bool operator ==(ElfSegmentType left, ElfSegmentType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfSegmentType left, ElfSegmentType right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return Value < ElfNative.PT_NUM ? $"SegmentType {((ElfSegmentTypeCore) Value)}" : $"SegmentType 0x{Value:X8}";
        }
        
        public static implicit operator ElfSegmentType(ElfSegmentTypeCore segmentTypeCore)
        {
            return new ElfSegmentType(segmentTypeCore);
        }
    }
}