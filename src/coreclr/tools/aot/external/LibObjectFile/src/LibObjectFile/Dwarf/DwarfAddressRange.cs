// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public struct DwarfAddressRange
    {
        public DwarfAddressRange(ulong segment, ulong address, ulong length)
        {
            Segment = segment;
            Address = address;
            Length = length;
        }
        
        public ulong Segment { get; set; }

        public ulong Address { get; set; }

        public ulong Length { get; set; }

        public override string ToString()
        {
            return $"{nameof(Segment)}: 0x{Segment:x16}, {nameof(Address)}: 0x{Address:x16}, {nameof(Length)}: 0x{Length:x16}";
        }
    }
}