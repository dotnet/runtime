// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines special sections that can be configured via <see cref="ElfSectionExtension.ConfigureAs{TElfSection}"/>
    /// </summary>
    public enum ElfSectionSpecialType
    {
        None,
        Bss,
        Comment,
        Data,
        Data1,
        Debug,
        Dynamic,
        DynamicStringTable,
        DynamicSymbolTable,
        Fini,
        Got,
        Hash,
        Init,
        Interp,
        Line,
        Note,
        Plt,
        Relocation,
        RelocationAddends,
        ReadOnlyData,
        ReadOnlyData1,
        SectionHeaderStringTable,
        StringTable,
        SymbolTable,
        Text,
    }
}