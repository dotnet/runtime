// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Contains all the low-level structures used for reading/writing ELF data, automatically generated from C headers.
    /// </summary>
    public static partial class ElfNative
    {
        public partial struct Elf32_Shdr: IEquatable<Elf32_Shdr>
        {
            public static readonly Elf32_Shdr Null = new Elf32_Shdr();

            public bool IsNull => this == Null;

            public bool Equals(Elf32_Shdr other)
            {
                return sh_name.Equals(other.sh_name) && sh_type.Equals(other.sh_type) && sh_flags.Equals(other.sh_flags) && sh_addr.Equals(other.sh_addr) && sh_offset.Equals(other.sh_offset) && sh_size.Equals(other.sh_size) && sh_link.Equals(other.sh_link) && sh_info.Equals(other.sh_info) && sh_addralign.Equals(other.sh_addralign) && sh_entsize.Equals(other.sh_entsize);
            }

            public override bool Equals(object obj)
            {
                return obj is Elf32_Shdr other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = sh_name.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_type.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_flags.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_addr.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_offset.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_size.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_link.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_info.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_addralign.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_entsize.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(Elf32_Shdr left, Elf32_Shdr right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Elf32_Shdr left, Elf32_Shdr right)
            {
                return !left.Equals(right);
            }
        }

        public partial struct Elf64_Shdr : IEquatable<Elf64_Shdr>
        {
            public static readonly Elf64_Shdr Null = new Elf64_Shdr();
            
            public bool IsNull => this == Null;
            
            public bool Equals(Elf64_Shdr other)
            {
                return sh_name.Equals(other.sh_name) && sh_type.Equals(other.sh_type) && sh_flags.Equals(other.sh_flags) && sh_addr.Equals(other.sh_addr) && sh_offset.Equals(other.sh_offset) && sh_size.Equals(other.sh_size) && sh_link.Equals(other.sh_link) && sh_info.Equals(other.sh_info) && sh_addralign.Equals(other.sh_addralign) && sh_entsize.Equals(other.sh_entsize);
            }

            public override bool Equals(object obj)
            {
                return obj is Elf64_Shdr other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = sh_name.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_type.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_flags.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_addr.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_offset.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_size.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_link.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_info.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_addralign.GetHashCode();
                    hashCode = (hashCode * 397) ^ sh_entsize.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(Elf64_Shdr left, Elf64_Shdr right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Elf64_Shdr left, Elf64_Shdr right)
            {
                return !left.Equals(right);
            }
        }
    }
}