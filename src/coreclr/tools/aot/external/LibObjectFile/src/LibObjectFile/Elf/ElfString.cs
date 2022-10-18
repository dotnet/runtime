// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines a string with the associated index in the string table.
    /// </summary>
    public readonly struct ElfString : IEquatable<ElfString>
    {
        private ElfString(string value, uint index)
        {
            Value = value;
            Index = index;
        }

        public ElfString(string value)
        {
            Value = value;
            Index = 0;
        }

        public ElfString(uint index)
        {
            Value = null;
            Index = index;
        }

        public readonly string Value;

        public readonly uint Index;

        public bool IsEmpty => Value == null && Index == 0;

        public bool Equals(ElfString other)
        {
            return (Value ?? string.Empty) == (other.Value ?? string.Empty) && Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfString other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Value ?? string.Empty).GetHashCode() * 397) ^ (int) Index;
            }
        }

        public static bool operator ==(ElfString left, ElfString right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfString left, ElfString right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(string left, ElfString right)
        {
            return string.Equals(left, right.Value);
        }

        public static bool operator !=(ElfString left, string right)
        {
            return !string.Equals(left.Value, right);
        }

        public static bool operator ==(ElfString right, string left)
        {
            return string.Equals(left, right.Value);
        }

        public static bool operator !=(string right, ElfString left)
        {
            return !string.Equals(left.Value, right);
        }

        public static implicit operator string(ElfString elfString)
        {
            return elfString.Value;
        }

        public static implicit operator ElfString(string text)
        {
            return new ElfString(text);
        }

        public ElfString WithName(string name)
        {
            return new ElfString(name, Index);
        }

        public ElfString WithIndex(uint index)
        {
            return new ElfString(Value, index);
        }

        public override string ToString()
        {
            return Value ?? $"0x{Index:x8}";
        }
    }
}