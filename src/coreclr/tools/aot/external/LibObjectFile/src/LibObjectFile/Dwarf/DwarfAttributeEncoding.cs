// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    [Flags]
    public enum DwarfAttributeEncoding
    {
        None,

        Address = 1,

        Block = 1 << 1,

        Constant = 1 << 2,

        ExpressionLocation = 1 << 3,

        Flag = 1 << 4,

        LinePointer = 1 << 5,

        LocationListPointer = 1 << 6,

        MacroPointer = 1 << 7,

        RangeListPointer = 1 << 8,

        Reference = 1 << 9,

        String = 1 << 10,

        RangeList = 1 << 11,

        Indirect = 1 << 12,

        LocationList = 1 << 13,

        AddressPointer = 1 << 14,

        LocationListsPointer = 1 << 15,

        RangeListsPointer = 1 << 16,

        StringOffsetPointer = 1 << 17,
    }
}