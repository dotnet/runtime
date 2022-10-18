// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Internal struct used to identify which part of the file is attached to a section or not.
    /// It is used while reading back an ELF file from the disk to create <see cref="ElfShadowSection"/>
    /// </summary>
    [DebuggerDisplay("{StartOffset,nq} - {EndOffset,nq} : {Section,nq}")]
    internal readonly struct ElfFilePart : IComparable<ElfFilePart>, IEquatable<ElfFilePart>
    {
        /// <summary>
        /// Creates an instance that is not yet bound to a section for which an
        /// <see cref="ElfShadowSection"/> will be created
        /// </summary>
        /// <param name="startOffset">Start of the offset in the file</param>
        /// <param name="endOffset">End of the offset in the file (inclusive)</param>
        public ElfFilePart(ulong startOffset, ulong endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            Section = null;
        }

        /// <summary>
        /// Creates an instance that is bound to a section 
        /// </summary>
        /// <param name="section">A section of the file</param>
        public ElfFilePart(ElfSection section)
        {
            Section = section ?? throw new ArgumentNullException(nameof(section));
            Debug.Assert(section.Size > 0);
            StartOffset = section.Offset;
            EndOffset = StartOffset + Section.Size - 1;
        }
        
        public readonly ulong StartOffset;

        public readonly ulong EndOffset;
        
        public readonly ElfSection Section;

        public int CompareTo(ElfFilePart other)
        {
            if (EndOffset < other.StartOffset)
            {
                return -1;
            }

            if (StartOffset > other.EndOffset)
            {
                return 1;
            }

            // May overlap or not
            return 0;
        }


        public bool Equals(ElfFilePart other)
        {
            return StartOffset == other.StartOffset && EndOffset == other.EndOffset;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfFilePart other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StartOffset.GetHashCode() * 397) ^ EndOffset.GetHashCode();
            }
        }

        public static bool operator ==(ElfFilePart left, ElfFilePart right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfFilePart left, ElfFilePart right)
        {
            return !left.Equals(right);
        }
    }
}