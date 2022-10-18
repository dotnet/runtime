// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Gets the type of a <see cref="ElfRelocation"/>.
    /// </summary>
    public readonly partial struct ElfRelocationType : IEquatable<ElfRelocationType>
    {
        public ElfRelocationType(ElfArchEx arch, uint value)
        {
            Arch = arch;
            Value = value;
        }

        /// <summary>
        /// The associated <see cref="ElfArch"/> the <see cref="Value"/> applies to.
        /// </summary>
        public readonly ElfArchEx Arch;

        /// <summary>
        /// The value of this relocation type.
        /// </summary>
        public readonly uint Value;

        public bool Equals(ElfRelocationType other)
        {
            return Arch.Equals(other.Arch) && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfRelocationType other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Arch.GetHashCode() * 397) ^ (int) Value;
            }
        }

        public static bool operator ==(ElfRelocationType left, ElfRelocationType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfRelocationType left, ElfRelocationType right)
        {
            return !left.Equals(right);
        }

        public string Name => ToStringInternal();

        public override string ToString()
        {
            if (Arch.Value == 0 && Value == 0) return "Empty ElfRelocationType";
            return ToStringInternal() ?? $"Unknown {nameof(ElfRelocationType)} (0x{Value:X4})";
        }
    }
}