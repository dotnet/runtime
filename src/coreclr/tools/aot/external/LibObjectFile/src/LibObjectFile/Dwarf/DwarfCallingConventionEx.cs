// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public readonly partial struct DwarfCallingConventionEx : IEquatable<DwarfCallingConventionEx>
    {
        public DwarfCallingConventionEx(byte value)
        {
            Value = (DwarfCallingConvention)value;
        }

        public DwarfCallingConventionEx(DwarfCallingConvention value)
        {
            Value = value;
        }

        public readonly DwarfCallingConvention Value;

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(DwarfCallingConvention)} (0x{Value:x2})";
        }

        public bool Equals(DwarfCallingConventionEx other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfCallingConventionEx other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(DwarfCallingConventionEx left, DwarfCallingConventionEx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfCallingConventionEx left, DwarfCallingConventionEx right)
        {
            return !left.Equals(right);
        }
        
        public static explicit operator uint(DwarfCallingConventionEx callConv) => (uint)callConv.Value;

        public static implicit operator DwarfCallingConventionEx(DwarfCallingConvention callConv) => new DwarfCallingConventionEx(callConv);

        public static implicit operator DwarfCallingConvention(DwarfCallingConventionEx callConv) => callConv.Value;
    }
}