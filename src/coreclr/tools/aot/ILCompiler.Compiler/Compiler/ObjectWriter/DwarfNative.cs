// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.ObjectWriter
{
    public static class DwarfNative
    {
        public const byte DW_EH_PE_absptr = 0x00;
        public const byte DW_EH_PE_omit = 0xff;
        public const byte DW_EH_PE_ptr = 0x00;
        public const byte DW_EH_PE_uleb128 = 0x01;
        public const byte DW_EH_PE_udata2 = 0x02;
        public const byte DW_EH_PE_udata4 = 0x03;
        public const byte DW_EH_PE_udata8 = 0x04;
        public const byte DW_EH_PE_sleb128 = 0x09;
        public const byte DW_EH_PE_sdata2 = 0x0a;
        public const byte DW_EH_PE_sdata4 = 0x0b;
        public const byte DW_EH_PE_sdata8 = 0x0c;
        public const byte DW_EH_PE_signed = 0x08;
        public const byte DW_EH_PE_pcrel = 0x10;
        public const byte DW_EH_PE_textrel = 0x20;
        public const byte DW_EH_PE_datarel = 0x30;
        public const byte DW_EH_PE_funcrel = 0x40;
        public const byte DW_EH_PE_aligned = 0x50;
        public const byte DW_EH_PE_indirect = 0x80;

        public const byte DW_CFA_nop = 0x0;
        public const byte DW_CFA_set_loc = 0x1;
        public const byte DW_CFA_advance_loc1 = 0x2;
        public const byte DW_CFA_advance_loc2 = 0x3;
        public const byte DW_CFA_advance_loc4 = 0x4;
        public const byte DW_CFA_offset_extended = 0x5;
        public const byte DW_CFA_restore_extended = 0x6;
        public const byte DW_CFA_undefined = 0x7;
        public const byte DW_CFA_same_value = 0x8;
        public const byte DW_CFA_register = 0x9;
        public const byte DW_CFA_remember_state = 0xa;
        public const byte DW_CFA_restore_state = 0xb;
        public const byte DW_CFA_def_cfa = 0xc;
        public const byte DW_CFA_def_cfa_register = 0xd;
        public const byte DW_CFA_def_cfa_offset = 0xe;
        public const byte DW_CFA_def_cfa_expression = 0xf;
        public const byte DW_CFA_expression = 0x10;
        public const byte DW_CFA_offset_extended_sf = 0x11;
        public const byte DW_CFA_def_cfa_sf = 0x12;
        public const byte DW_CFA_def_cfa_offset_sf = 0x13;
        public const byte DW_CFA_val_offset = 0x14;
        public const byte DW_CFA_val_offset_sf = 0x15;
        public const byte DW_CFA_val_expression = 0x16;
        public const byte DW_CFA_advance_loc = 0x40;
        public const byte DW_CFA_offset = 0x80;
        public const byte DW_CFA_restore = 0xc0;
        public const byte DW_CFA_GNU_window_save = 0x2d;
        public const byte DW_CFA_GNU_args_size = 0x2e;
        public const byte DW_CFA_GNU_negative_offset_extended = 0x2f;
        public const byte DW_CFA_AARCH64_negate_ra_state = 0x2d;

        public const int DW_ATE_address                  = 0x01;
        public const int DW_ATE_boolean                  = 0x02;
        public const int DW_ATE_complex_float            = 0x03;
        public const int DW_ATE_float                    = 0x04;
        public const int DW_ATE_signed                   = 0x05;
        public const int DW_ATE_signed_char              = 0x06;
        public const int DW_ATE_unsigned                 = 0x07;
        public const int DW_ATE_unsigned_char            = 0x08;
        public const int DW_ATE_imaginary_float          = 0x09;  /* DWARF3 */
        public const int DW_ATE_packed_decimal           = 0x0a;  /* DWARF3f */
        public const int DW_ATE_numeric_string           = 0x0b;  /* DWARF3f */
        public const int DW_ATE_edited                   = 0x0c;  /* DWARF3f */
        public const int DW_ATE_signed_fixed             = 0x0d;  /* DWARF3f */
        public const int DW_ATE_unsigned_fixed           = 0x0e;  /* DWARF3f */
        public const int DW_ATE_decimal_float            = 0x0f;  /* DWARF3f */
        public const int DW_ATE_UTF                      = 0x10;  /* DWARF4 */
        public const int DW_ATE_UCS                      = 0x11;  /* DWARF5 */
        public const int DW_ATE_ASCII                    = 0x12;  /* DWARF5 */
    }
}
