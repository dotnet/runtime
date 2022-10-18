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
                !Link.IsEmpty ||
                !Info.IsEmpty ||
                Offset != 0 ||
                Size != 0)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidNullSection, "Invalid Null section. This section should not be modified and all properties must be null");
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