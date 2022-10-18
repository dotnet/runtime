// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public enum DwarfInlineKind : byte
    {
        NotInlined = DwarfNative.DW_INL_not_inlined,

        Inlined = DwarfNative.DW_INL_inlined,

        DeclaredNotInlined = DwarfNative.DW_INL_declared_not_inlined,

        DeclaredInlined = DwarfNative.DW_INL_declared_inlined,
    }
}