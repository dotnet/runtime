// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// The Section Header String Table used by <see cref="ElfObjectFile.SectionHeaderStringTable"/>.
    /// </summary>
    public sealed class ElfSectionHeaderStringTable : ElfStringTable
    {
        public new const string DefaultName = ".shstrtab";

        public ElfSectionHeaderStringTable()
        {
            Name = DefaultName;
        }
    }
}