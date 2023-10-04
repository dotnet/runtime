// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A null section with the type <see cref="ElfSectionType.Null"/>.
    /// </summary>
    public sealed class ElfNullSection : ElfSection
    {
        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            if (Type != ElfSectionType.Null ||
                Flags != ElfSectionFlags.None ||
                !Name.IsEmpty ||
                VirtualAddress != 0 ||
                Alignment != 0 ||
                !Info.IsEmpty ||
                Offset != 0)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidNullSection, "Invalid Null section. This section should not be modified and all properties must be null");
            }

            if (Size != 0 && Parent.VisibleSectionCount < ElfNative.SHN_LORESERVE)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidNullSection, "Invalid Null section. Size is non-zero but number of sections is lower than SHN_LORESERVE");
            }
            else if (Size == 0 && Parent.VisibleSectionCount >= ElfNative.SHN_LORESERVE)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidNullSection, "Invalid Null section. Size is zero but number of sections is higher or equal to SHN_LORESERVE");
            }

            if (!Link.IsEmpty && (Parent.SectionHeaderStringTable?.SectionIndex ?? 0) < ElfNative.SHN_LORESERVE)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidNullSection, "Invalid Null section. Link is non-zero but index of section header string section is lower than SHN_LORESERVE");
            }
            else if (Link.IsEmpty && (Parent.SectionHeaderStringTable?.SectionIndex ?? 0) >= ElfNative.SHN_LORESERVE)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidNullSection, "Invalid Null section. Link is non-zero but index of section header string section is higher or equal to SHN_LORESERVE");
            }
        }

        public override void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
        }
        
        protected override void Read(ElfReader reader)
        {
        }

        protected override void Write(ElfWriter writer)
        {
        }
    }
}