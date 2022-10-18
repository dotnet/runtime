// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public enum DwarfVisibility : byte
    {
        Local = DwarfNative.DW_VIS_local,

        Exported = DwarfNative.DW_VIS_exported,

        Qualified = DwarfNative.DW_VIS_qualified,
    }
}