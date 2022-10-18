// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public readonly struct DwarfAbbreviationItemKey : IEquatable<DwarfAbbreviationItemKey>
    {
        public DwarfAbbreviationItemKey(DwarfTagEx tag, bool hasChildren, DwarfAttributeDescriptors descriptors)
        {
            Tag = tag;
            HasChildren = hasChildren;
            Descriptors = descriptors;
        }


        public readonly DwarfTagEx Tag;

        public readonly bool HasChildren;

        public readonly DwarfAttributeDescriptors Descriptors;

        public bool Equals(DwarfAbbreviationItemKey other)
        {
            return Tag == other.Tag && HasChildren == other.HasChildren && Descriptors.Equals(other.Descriptors);
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfAbbreviationItemKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Tag.GetHashCode();
                hashCode = (hashCode * 397) ^ HasChildren.GetHashCode();
                hashCode = (hashCode * 397) ^ Descriptors.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(DwarfAbbreviationItemKey left, DwarfAbbreviationItemKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfAbbreviationItemKey left, DwarfAbbreviationItemKey right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{nameof(Tag)}: {Tag}, {nameof(HasChildren)}: {HasChildren}, {nameof(Descriptors)}: {Descriptors}";
        }
    }
}