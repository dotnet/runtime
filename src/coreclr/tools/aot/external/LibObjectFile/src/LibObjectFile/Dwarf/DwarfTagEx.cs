// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    /// <summary>
    /// Defines the tag of an <see cref="DwarfDIE"/>.
    /// </summary>
    public readonly partial struct DwarfTagEx : IEquatable<DwarfTagEx>
    {
        public DwarfTagEx(uint value)
        {
            Value = (DwarfTag)value;
        }

        public DwarfTagEx(DwarfTag value)
        {
            Value = value;
        }

        public readonly DwarfTag Value;
        
        public bool Equals(DwarfTagEx other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfTagEx other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Value;
        }

        public static bool operator ==(DwarfTagEx left, DwarfTagEx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfTagEx left, DwarfTagEx right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(DwarfTagEx)} (0x{Value:X4})";
        }

        public static explicit operator uint(DwarfTagEx tag) => (uint)tag.Value;

        public static implicit operator DwarfTagEx(DwarfTag tag) => new DwarfTagEx(tag);

        public static implicit operator DwarfTag(DwarfTagEx tag) => tag.Value;
    }
}