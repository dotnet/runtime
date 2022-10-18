// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public enum DwarfVirtuality : byte
    {
        None = DwarfNative.DW_VIRTUALITY_none,

        Virtual = DwarfNative.DW_VIRTUALITY_virtual,

        PureVirtual = DwarfNative.DW_VIRTUALITY_pure_virtual,
    }
}