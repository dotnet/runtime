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
    public readonly partial struct DwarfAttributeFormEx : IEquatable<DwarfAttributeFormEx>
    {
        public DwarfAttributeFormEx(uint value)
        {
            Value = (DwarfAttributeForm)value;
        }
        public DwarfAttributeFormEx(DwarfAttributeForm value)
        {
            Value = value;
        }

        public readonly DwarfAttributeForm Value;
        
        public bool Equals(DwarfAttributeFormEx other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfAttributeFormEx other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Value;
        }

        public static bool operator ==(DwarfAttributeFormEx left, DwarfAttributeFormEx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfAttributeFormEx left, DwarfAttributeFormEx right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(DwarfAttributeFormEx)} (0x{Value:X4})";
        }

        public static explicit operator uint(DwarfAttributeFormEx form) => (uint)form.Value;

        public static implicit operator DwarfAttributeFormEx(DwarfAttributeForm kind) => new DwarfAttributeFormEx(kind);

        public static implicit operator DwarfAttributeForm(DwarfAttributeFormEx kind) => kind.Value;
    }
}