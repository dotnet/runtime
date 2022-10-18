// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public enum DwarfDiscriminantListKind : byte
    {
        Label = DwarfNative.DW_DSC_label,

        Range = DwarfNative.DW_DSC_range,
    }
}