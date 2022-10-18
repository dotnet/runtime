// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public enum DwarfAccessibility : byte
    {
        Public = DwarfNative.DW_ACCESS_public,

        Private = DwarfNative.DW_ACCESS_private,

        Protected = DwarfNative.DW_ACCESS_protected,
    }
}