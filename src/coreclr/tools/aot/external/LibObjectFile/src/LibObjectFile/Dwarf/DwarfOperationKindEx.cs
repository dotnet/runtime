// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public readonly partial struct DwarfOperationKindEx : IEquatable<DwarfOperationKindEx>
    {
        public DwarfOperationKindEx(byte value)
        {
            Value = (DwarfOperationKind)value;
        }

        public DwarfOperationKindEx(DwarfOperationKind value)
        {
            Value = value;
        }

        public readonly DwarfOperationKind Value;

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(DwarfOperationKindEx)} ({(uint)Value:x2})";
        }

        public bool Equals(DwarfOperationKindEx other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfOperationKindEx other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(DwarfOperationKindEx left, DwarfOperationKindEx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfOperationKindEx left, DwarfOperationKindEx right)
        {
            return !left.Equals(right);
        }

        public static explicit operator uint(DwarfOperationKindEx kind) => (uint)kind.Value;

        public static implicit operator DwarfOperationKindEx(DwarfOperationKind kind) => new DwarfOperationKindEx(kind);

        public static implicit operator DwarfOperationKind(DwarfOperationKindEx kind) => kind.Value;
    }
}