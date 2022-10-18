// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public enum DwarfAddressSize : byte
    {
        None = 0,

        Bit8 = 1,

        Bit16 = 2,

        Bit32 = 4,

        Bit64 = 8,
    }
}