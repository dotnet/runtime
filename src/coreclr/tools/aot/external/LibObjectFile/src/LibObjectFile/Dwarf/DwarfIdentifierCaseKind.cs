// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public enum DwarfIdentifierCaseKind : byte
    {
        Sensitive = DwarfNative.DW_ID_case_sensitive,

        UpCase = DwarfNative.DW_ID_up_case,

        DownCase = DwarfNative.DW_ID_down_case,

        Insensitive = DwarfNative.DW_ID_case_insensitive,
    }
}