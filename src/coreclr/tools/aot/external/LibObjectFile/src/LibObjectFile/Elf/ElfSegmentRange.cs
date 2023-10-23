// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines the range of section a segment is bound to.
    /// </summary>
    public readonly struct ElfSegmentRange : IEquatable<ElfSegmentRange>
    {
        public static readonly ElfSegmentRange Empty = new ElfSegmentRange();

        /// <summary>
        /// Creates a new instance that is bound to an entire section/
        /// </summary>
        /// <param name="section">The section to be bound to</param>
        public ElfSegmentRange(ElfSection section)
        {
            BeginSection = section ?? throw new ArgumentNullException(nameof(section));
            BeginOffset = 0;
            EndSection = section;
            EndOffset = -1;
        }

        /// <summary>
        /// Creates a new instance that is bound to a range of section.
        /// </summary>
        /// <param name="beginSection">The first section.</param>
        /// <param name="beginOffset">The offset inside the first section.</param>
        /// <param name="endSection">The last section.</param>
        /// <param name="endOffset">The offset in the last section</param>
        public ElfSegmentRange(ElfSection beginSection, ulong beginOffset, ElfSection endSection, long endOffset)
        {
            BeginSection = beginSection ?? throw new ArgumentNullException(nameof(beginSection));
            BeginOffset = beginOffset;
            EndSection = endSection ?? throw new ArgumentNullException(nameof(endSection));
            EndOffset = endOffset;
            if (BeginSection.Index > EndSection.Index)
            {
                throw new ArgumentOutOfRangeException(nameof(beginSection), $"The {nameof(beginSection)}.{nameof(ElfSection.Index)} = {BeginSection.Index} is > {nameof(endSection)}.{nameof(ElfSection.Index)} = {EndSection.Index}, while it must be <=");
            }
        }
        
        /// <summary>
        /// The first section.
        /// </summary>
        public readonly ElfSection BeginSection;

        /// <summary>
        /// The relative offset in <see cref="BeginSection"/>.
        /// </summary>
        public readonly ulong BeginOffset;

        /// <summary>
        /// The last section.
        /// </summary>
        public readonly ElfSection EndSection;

        /// <summary>
        /// The offset in the last section. If the offset is &lt; 0, then the actual offset starts from end of the section where finalEndOffset = section.Size + EndOffset.
        /// </summary>
        public readonly long EndOffset;

        /// <summary>
        /// Gets a boolean indicating if this section is empty.
        /// </summary>
        public bool IsEmpty => this == Empty;

        /// <summary>
        /// Returns the absolute offset of this range taking into account the <see cref="BeginSection"/>.<see cref="ElfObject.Offset"/>.
        /// </summary>
        public ulong Offset
        {
            get
            {
                // If this Begin/End section are not attached we can't calculate any meaningful size
                if (BeginSection?.Parent == null || EndSection?.Parent == null || BeginSection?.Parent != EndSection?.Parent)
                {
                    return 0;
                }

                return BeginSection.Offset + BeginOffset;
            }
        }

        /// <summary>
        /// Returns the size of this range taking into account the size of each section involved in this range.
        /// </summary>
        public ulong Size
        {
            get
            {
                // If this Begin/End section are not attached we can't calculate any meaningful size
                if (BeginSection?.Parent == null || EndSection?.Parent == null || BeginSection?.Parent != EndSection?.Parent)
                {
                    return 0;
                }

                ulong size = EndSection.Offset - BeginSection.Offset;
                size -= BeginOffset;
                size += EndOffset < 0 ? (ulong)((long)EndSection.Size + EndOffset + 1) : (ulong)(EndOffset + 1);
                return size;
            }
        }
        
        public bool Equals(ElfSegmentRange other)
        {
            return Equals(BeginSection, other.BeginSection) && BeginOffset == other.BeginOffset && Equals(EndSection, other.EndSection) && EndOffset == other.EndOffset;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfSegmentRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (BeginSection != null ? BeginSection.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ BeginOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ (EndSection != null ? EndSection.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ EndOffset.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ElfSegmentRange left, ElfSegmentRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfSegmentRange left, ElfSegmentRange right)
        {
            return !left.Equals(right);
        }

        public static implicit operator ElfSegmentRange(ElfSection section)
        {
            return section == null ? Empty : new ElfSegmentRange(section);
        }
    }
}