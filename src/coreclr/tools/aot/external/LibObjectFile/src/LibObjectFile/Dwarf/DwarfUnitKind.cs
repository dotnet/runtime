// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public readonly partial struct DwarfUnitKindEx : IEquatable<DwarfUnitKindEx>
    {
        public DwarfUnitKindEx(byte value)
        {
            Value = (DwarfUnitKind)value;
        }

        public DwarfUnitKindEx(DwarfUnitKind value)
        {
            Value = value;
        }

        public readonly DwarfUnitKind Value;

        public override string ToString()
        {
            if ((byte)Value >= DwarfNative.DW_UT_lo_user)
            {
                return $"User {nameof(DwarfUnitKindEx)} (0x{Value:x2})";
            }
            return ToStringInternal() ?? $"Unknown {nameof(DwarfUnitKindEx)} (0x{Value:x2})";
        }

        public bool Equals(DwarfUnitKindEx other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfUnitKindEx other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(DwarfUnitKindEx left, DwarfUnitKindEx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfUnitKindEx left, DwarfUnitKindEx right)
        {
            return !left.Equals(right);
        }

        public static explicit operator uint(DwarfUnitKindEx kind) => (uint)kind.Value;

        public static implicit operator DwarfUnitKindEx(DwarfUnitKind kind) => new DwarfUnitKindEx(kind);

        public static implicit operator DwarfUnitKind(DwarfUnitKindEx kind) => kind.Value;
    }
}