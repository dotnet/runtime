// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines a machine architecture.
    /// This is the value seen in <see cref="ElfNative.Elf32_Ehdr.e_machine"/> or <see cref="ElfNative.Elf64_Ehdr.e_machine"/>
    /// as well as the various machine defines (e.g <see cref="ElfNative.EM_386"/>).
    /// </summary>
    public readonly partial struct ElfArchEx : IEquatable<ElfArchEx>
    {
        public ElfArchEx(ushort value)
        {
            Value = (ElfArch)value;
        }

        public ElfArchEx(ElfArch value)
        {
            Value = value;
        }

        /// <summary>
        /// Raw value.
        /// </summary>
        public readonly ElfArch Value;

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(ElfArchEx)} (0x{Value:X4})";
        }

        public bool Equals(ElfArchEx other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfArchEx other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(ElfArchEx left, ElfArchEx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfArchEx left, ElfArchEx right)
        {
            return !left.Equals(right);
        }

        public static explicit operator uint(ElfArchEx arch) => (uint)arch.Value;

        public static implicit operator ElfArchEx(ElfArch arch) => new ElfArchEx(arch);

        public static implicit operator ElfArch(ElfArchEx arch) => arch.Value;
    }
}