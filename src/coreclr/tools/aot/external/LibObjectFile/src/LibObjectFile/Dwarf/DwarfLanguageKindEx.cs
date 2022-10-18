// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public readonly partial struct DwarfLanguageKindEx : IEquatable<DwarfLanguageKindEx>
    {
        public DwarfLanguageKindEx(ushort value)
        {
            Value = (DwarfLanguageKind)value;
        }

        public DwarfLanguageKindEx(DwarfLanguageKind value)
        {
            Value = value;
        }

        public readonly DwarfLanguageKind Value;

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(DwarfLanguageKind)} (0x{Value:x4})";
        }

        public bool Equals(DwarfLanguageKindEx other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfLanguageKindEx other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(DwarfLanguageKindEx left, DwarfLanguageKindEx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfLanguageKindEx left, DwarfLanguageKindEx right)
        {
            return !left.Equals(right);
        }
        
        public static explicit operator uint(DwarfLanguageKindEx kind) => (uint)kind.Value;

        public static implicit operator DwarfLanguageKindEx(DwarfLanguageKind kind) => new DwarfLanguageKindEx(kind);

        public static implicit operator DwarfLanguageKind(DwarfLanguageKindEx kind) => kind.Value;
    }
}