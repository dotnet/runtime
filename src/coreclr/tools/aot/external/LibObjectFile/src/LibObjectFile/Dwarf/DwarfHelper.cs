// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Text;
using System.Numerics;

namespace LibObjectFile.Dwarf
{
    public static partial class DwarfHelper
    {
        public static ulong SizeOfStringUTF8NullTerminated(string text)
        {
            if (text == null) return 0;
            return (ulong)Encoding.UTF8.GetByteCount(text) + 1;
        }
        
        public static uint SizeOfUnitLength(bool is64Bit)
        {
            return is64Bit ? 12U : 4U;
        }

        public static uint SizeOfUInt(bool is64Bit)
        {
            return is64Bit ? 8U : 4U;
        }

        public static uint SizeOfUInt(DwarfAddressSize addressSize)
        {
            return (uint)(addressSize);
        }

        public static uint SizeOfULEB128(ulong value)
        {
            // bits_to_encode = (data != 0) ? 64 - CLZ(x) : 1 = 64 - CLZ(data | 1)
            // bytes = ceil(bits_to_encode / 7.0);            = (6 + bits_to_encode) / 7
            uint x = 6 + 64 - (uint)BitOperations.LeadingZeroCount(value | 1UL);

            // Division by 7 is done by (x * 37) >> 8 where 37 = ceil(256 / 7).
            // This works for 0 <= x < 256 / (7 * 37 - 256), i.e. 0 <= x <= 85.
            return (x * 37) >> 8;
        }

        public static uint SizeOfILEB128(long value)
        {
            // The same as SizeOfULEB128 calculation but we have to account for the sign bit.
            value ^= value >> 63;
            uint x = 1 + 6 + 64 - (uint)BitOperations.LeadingZeroCount((ulong)value | 1UL);
            return (x * 37) >> 8;
        }

        public static DwarfAttributeEncoding GetAttributeEncoding(DwarfAttributeKindEx kind)
        {
            if ((uint)kind.Value >= AttributeToEncoding.Length) return DwarfAttributeEncoding.None;
            return AttributeToEncoding[(int) kind.Value];
        }

        private static readonly DwarfAttributeEncoding[] Encodings = new DwarfAttributeEncoding[]
        {
            DwarfAttributeEncoding.None               , // 0
            DwarfAttributeEncoding.Address            , // DW_FORM_addr  0x01 
            DwarfAttributeEncoding.None               , // Reserved  0x02
            DwarfAttributeEncoding.Block              , // DW_FORM_block2  0x03 
            DwarfAttributeEncoding.Block              , // DW_FORM_block4  0x04 
            DwarfAttributeEncoding.Constant           , // DW_FORM_data2  0x05 
            DwarfAttributeEncoding.Constant           , // DW_FORM_data4  0x06 
            DwarfAttributeEncoding.Constant           , // DW_FORM_data8  0x07 
            DwarfAttributeEncoding.String             , // DW_FORM_string  0x08 
            DwarfAttributeEncoding.Block              , // DW_FORM_block  0x09 
            DwarfAttributeEncoding.Block              , // DW_FORM_block1  0x0a 
            DwarfAttributeEncoding.Constant           , // DW_FORM_data1  0x0b 
            DwarfAttributeEncoding.Flag               , // DW_FORM_flag  0x0c 
            DwarfAttributeEncoding.Constant           , // DW_FORM_sdata  0x0d 
            DwarfAttributeEncoding.String             , // DW_FORM_strp  0x0e 
            DwarfAttributeEncoding.Constant           , // DW_FORM_udata  0x0f 
            DwarfAttributeEncoding.Reference          , // DW_FORM_ref_addr  0x10 
            DwarfAttributeEncoding.Reference          , // DW_FORM_ref1  0x11 
            DwarfAttributeEncoding.Reference          , // DW_FORM_ref2  0x12 
            DwarfAttributeEncoding.Reference          , // DW_FORM_ref4  0x13 
            DwarfAttributeEncoding.Reference          , // DW_FORM_ref8  0x14 
            DwarfAttributeEncoding.Reference          , // DW_FORM_ref_udata  0x15 
            DwarfAttributeEncoding.Indirect           , // DW_FORM_indirect  0x16
            DwarfAttributeEncoding.AddressPointer |
            DwarfAttributeEncoding.LinePointer |
            DwarfAttributeEncoding.LocationList |
            DwarfAttributeEncoding.LocationListsPointer |
            DwarfAttributeEncoding.MacroPointer |
            DwarfAttributeEncoding.RangeList |
            DwarfAttributeEncoding.RangeListsPointer |
            DwarfAttributeEncoding.StringOffsetPointer, // DW_FORM_sec_offset 	0x17 
            DwarfAttributeEncoding.ExpressionLocation , // DW_FORM_exprloc  0x18 
            DwarfAttributeEncoding.Flag               , // DW_FORM_flag_present  0x19 
            DwarfAttributeEncoding.String             , // DW_FORM_strx  0x1a 
            DwarfAttributeEncoding.Address            , // DW_FORM_addrx  0x1b 
            DwarfAttributeEncoding.Reference          , // DW_FORM_ref_sup4  0x1c 
            DwarfAttributeEncoding.String             , // DW_FORM_strp_sup  0x1d 
            DwarfAttributeEncoding.Constant           , // DW_FORM_data16 0x1e
            DwarfAttributeEncoding.String             , // DW_FORM_line_strp 0x1f
            DwarfAttributeEncoding.Reference          , // DW_FORM_ref_sig8 0x20
            DwarfAttributeEncoding.Constant           , // DW_FORM_implicit_const 0x21
            DwarfAttributeEncoding.LocationList       , // DW_FORM_loclistx 0x22
            DwarfAttributeEncoding.RangeList          , // DW_FORM_rnglistx 0x23
            DwarfAttributeEncoding.Reference          , // DW_FORM_ref_sup8 0x24
            DwarfAttributeEncoding.String             , // DW_FORM_strx1 0x25
            DwarfAttributeEncoding.String             , // DW_FORM_strx2 0x26
            DwarfAttributeEncoding.String             , // DW_FORM_strx3 0x27
            DwarfAttributeEncoding.String             , // DW_FORM_strx4 0x28
            DwarfAttributeEncoding.Address            , // DW_FORM_addrx1 0x29
            DwarfAttributeEncoding.Address            , // DW_FORM_addrx2 0x2a
            DwarfAttributeEncoding.Address            , // DW_FORM_addrx3 0x2b
            DwarfAttributeEncoding.Address            , // DW_FORM_addrx4 0x2c
        };
    }
}