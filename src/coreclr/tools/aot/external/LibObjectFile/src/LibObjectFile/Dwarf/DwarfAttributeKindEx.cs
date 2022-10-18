// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    /// <summary>
    /// Defines the kind of an <see cref="DwarfAttribute"/>.
    /// This is the value seen in <see cref="DwarfNative.DW_AT_ALTIUM_loclist"/>
    /// </summary>
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly partial struct DwarfAttributeKindEx : IEquatable<DwarfAttributeKindEx>
    {
        public DwarfAttributeKindEx(uint value)
        {
            Value = (DwarfAttributeKind)value;
        }

        public DwarfAttributeKindEx(DwarfAttributeKind value)
        {
            Value = value;
        }

        public readonly DwarfAttributeKind Value;


        public bool Equals(DwarfAttributeKindEx other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfAttributeKindEx other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Value;
        }

        public static bool operator ==(DwarfAttributeKindEx left, DwarfAttributeKindEx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfAttributeKindEx left, DwarfAttributeKindEx right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(DwarfAttributeKindEx)} (0x{Value:X4})";
        }

        public static explicit operator uint(DwarfAttributeKindEx kind) => (uint)kind.Value;

        public static implicit operator DwarfAttributeKindEx(DwarfAttributeKind kind) => new DwarfAttributeKindEx(kind);

        public static implicit operator DwarfAttributeKind(DwarfAttributeKindEx kind) => kind.Value;
    }
}