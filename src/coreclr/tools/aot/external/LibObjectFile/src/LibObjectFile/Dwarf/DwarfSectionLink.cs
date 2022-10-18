// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public struct DwarfSectionLink : IEquatable<DwarfSectionLink>
    {
        public DwarfSectionLink(ulong offset)
        {
            Offset = offset;
        }
        
        public readonly ulong Offset;

        public override string ToString()
        {
            return $"SectionLink {nameof(Offset)}: 0x{Offset:x}";
        }

        public bool Equals(DwarfSectionLink other)
        {
            return Offset == other.Offset;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfSectionLink other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Offset.GetHashCode();
        }

        public static bool operator ==(DwarfSectionLink left, DwarfSectionLink right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfSectionLink left, DwarfSectionLink right)
        {
            return !left.Equals(right);
        }
    }
}