// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines a link to a section, special section or an index (used by <see cref="ElfSection.Info"/> and <see cref="ElfSection.Link"/>)
    /// </summary>
    public readonly struct ElfSectionLink : IEquatable<ElfSectionLink>
    {
        public static readonly ElfSectionLink Empty = new ElfSectionLink(ElfNative.SHN_UNDEF);

        public static readonly ElfSectionLink SectionAbsolute = new ElfSectionLink(ElfNative.SHN_ABS);

        public static readonly ElfSectionLink SectionCommon = new ElfSectionLink(ElfNative.SHN_COMMON);
        
        public ElfSectionLink(uint index)
        {
            Section = null;
            SpecialIndex = index;
        }

        public ElfSectionLink(ElfSection section)
        {
            Section = section;
            SpecialIndex = 0;
        }

        public readonly ElfSection Section;

        public readonly uint SpecialIndex;

        public bool IsEmpty => Section == null && SpecialIndex == 0;

        /// <summary>
        /// <c>true</c> if this link to a section is a special section.
        /// </summary>
        public bool IsSpecial => Section == null && (SpecialIndex == ElfNative.SHN_UNDEF || SpecialIndex >= ElfNative.SHN_LORESERVE);
        
        public uint GetIndex()
        {
            return Section?.SectionIndex ?? SpecialIndex;
        }

        public bool Equals(ElfSectionLink other)
        {
            return Equals(Section, other.Section) && SpecialIndex == other.SpecialIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfSectionLink other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Section != null ? Section.GetHashCode() : 0) * 397) ^ SpecialIndex.GetHashCode();
            }
        }

        public static bool operator ==(ElfSectionLink left, ElfSectionLink right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfSectionLink left, ElfSectionLink right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            if (Section != null)
            {
                return Section.ToString();
            }

            if (SpecialIndex == 0) return "Special Section Undefined";

            if (SpecialIndex > ElfNative.SHN_BEFORE)
            {
                if (SpecialIndex == ElfNative.SHN_ABS)
                {
                    return "Special Section Absolute";
                }
                
                if (SpecialIndex == ElfNative.SHN_COMMON)
                {
                    return "Special Section Common";
                }

                if (SpecialIndex == ElfNative.SHN_XINDEX)
                {
                    return "Special Section XIndex";
                }
            }

            return $"Unknown Section Value 0x{SpecialIndex:X8}";
        }

        public static implicit operator ElfSectionLink(ElfSection section)
        {
            return new ElfSectionLink(section);
        }
        
        
        public bool TryGetSectionSafe<TSection>(string className, string propertyName, object context, DiagnosticBag diagnostics, out TSection section, params ElfSectionType[] sectionTypes) where TSection : ElfSection
        {
            section = null;

            if (Section == null)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_LinkOrInfoSectionNull, $"`{className}.{propertyName}` cannot be null for this instance", context);
                return false;
            }

            bool foundValid = false;
            foreach (var elfSectionType in sectionTypes)
            {
                if (Section.Type == elfSectionType)
                {
                    foundValid = true;
                    break;
                }
            }

            if (!foundValid)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_LinkOrInfoInvalidSectionType, $"The type `{Section.Type}` of `{className}.{propertyName}` must be a {string.Join(" or ", sectionTypes)}", context);
                return false;
            }
            section = Section as TSection;

            if (section == null)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_LinkOrInfoInvalidSectionInstance, $"The `{className}.{propertyName}` must be an instance of {typeof(TSection).Name}");
                return false;
            }
            return true;
        }
    }
}