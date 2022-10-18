// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Gets the type of a <see cref="ElfNoteType"/>.
    /// </summary>
    public readonly partial struct ElfNoteTypeEx : IEquatable<ElfNoteTypeEx>
    {
        public ElfNoteTypeEx(uint value)
        {
            Value = (ElfNoteType)value;
        }

        public ElfNoteTypeEx(ElfNoteType value)
        {
            Value = value;
        }

        /// <summary>
        /// The value of this note type.
        /// </summary>
        public readonly ElfNoteType Value;

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(ElfNoteTypeEx)} (0x{(uint)Value:X4})";
        }

        public bool Equals(ElfNoteTypeEx other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfNoteTypeEx other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Value;
        }

        public static bool operator ==(ElfNoteTypeEx left, ElfNoteTypeEx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfNoteTypeEx left, ElfNoteTypeEx right)
        {
            return !left.Equals(right);
        }

        public static explicit operator byte(ElfNoteTypeEx noteType) => (byte)noteType.Value;

        public static implicit operator ElfNoteTypeEx(ElfNoteType noteType) => new ElfNoteTypeEx(noteType);

        public static implicit operator ElfNoteType(ElfNoteTypeEx noteType) => noteType.Value;
    }
}