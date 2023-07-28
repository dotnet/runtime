// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// File machine generated. See gen_amd64InstrDecode/README.md


namespace Amd64InstrDecode
{
    // The enumeration below encodes the various amd64 instruction forms
    // Each enumeration is an '_' separated set of flags
    //      None     // No flags set
    //      MOp      // Instruction supports modrm RIP memory operations
    //      M1st     // Memory op is first operand normally src/dst
    //      MOnly    // Memory op is only operand.  May not be a write...
    //      MUnknown // Memory op size is unknown.  Size not included in disassembly
    //      MAddr    // Memory op is address load effective address
    //      M1B      // Memory op is 1  byte
    //      M2B      // Memory op is 2  bytes
    //      M4B      // Memory op is 4  bytes
    //      M8B      // Memory op is 8  bytes
    //      M16B     // Memory op is 16 bytes
    //      M32B     // Memory op is 32 bytes
    //      M6B      // Memory op is 6  bytes
    //      M10B     // Memory op is 10 bytes
    //      I1B      // Instruction includes 1  byte  of immediates
    //      I2B      // Instruction includes 2  bytes of immediates
    //      I3B      // Instruction includes 3  bytes of immediates
    //      I4B      // Instruction includes 4  bytes of immediates
    //      I8B      // Instruction includes 8  bytes of immediates
    //      Unknown  // Instruction samples did not include a modrm configured to produce RIP addressing
    //      L        // Flags depend on L bit in encoding.  L_<flagsLTrue>_or_<flagsLFalse>
    //      W        // Flags depend on W bit in encoding.  W_<flagsWTrue>_or_<flagsWFalse>
    //      P        // Flags depend on OpSize prefix for encoding.  P_<flagsNoOpSizePrefix>_or_<flagsOpSizePrefix>
    //      WP       // Flags depend on W bit in encoding and OpSize prefix.  WP_<flagsWTrue>_or__<flagsNoOpSizePrefix>_or_<flagsOpSizePrefix>
    //      or       // Flag option separator used in W, L, P, and WP above
    enum InstrForm : uint8_t
    {
       None,
       I1B,
       I1B_W_None_or_MOp_M16B,
       I2B,
       I3B,
       I4B,
       I8B,
       M1st_I1B_L_M16B_or_M8B,
       M1st_I1B_W_M8B_or_M4B,
       M1st_I1B_WP_M8B_or_M4B_or_M2B,
       M1st_L_M32B_or_M16B,
       M1st_M16B,
       M1st_M16B_I1B,
       M1st_M1B,
       M1st_M1B_I1B,
       M1st_M2B,
       M1st_M2B_I1B,
       M1st_M4B,
       M1st_M4B_I1B,
       M1st_M8B,
       M1st_MUnknown,
       M1st_W_M4B_or_M1B,
       M1st_W_M8B_or_M2B,
       M1st_W_M8B_or_M4B,
       M1st_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B,
       M1st_WP_M8B_or_M4B_or_M2B,
       MOnly_M10B,
       MOnly_M1B,
       MOnly_M2B,
       MOnly_M4B,
       MOnly_M8B,
       MOnly_MUnknown,
       MOnly_P_M6B_or_M4B,
       MOnly_W_M16B_or_M8B,
       MOnly_W_M8B_or_M4B,
       MOnly_WP_M8B_or_M4B_or_M2B,
       MOnly_WP_M8B_or_M8B_or_M2B,
       MOp_I1B_L_M32B_or_M16B,
       MOp_I1B_W_M8B_or_M4B,
       MOp_I1B_WP_M8B_or_M4B_or_M2B,
       MOp_I4B_W_M8B_or_M4B,
       MOp_L_M16B_or_M8B,
       MOp_L_M32B_or_M16B,
       MOp_L_M32B_or_M8B,
       MOp_L_M4B_or_M2B,
       MOp_L_M8B_or_M4B,
       MOp_M16B,
       MOp_M16B_I1B,
       MOp_M1B,
       MOp_M1B_I1B,
       MOp_M2B,
       MOp_M2B_I1B,
       MOp_M32B,
       MOp_M32B_I1B,
       MOp_M4B,
       MOp_M4B_I1B,
       MOp_M4B_I4B,
       MOp_M6B,
       MOp_M8B,
       MOp_M8B_I1B,
       MOp_MAddr,
       MOp_MUnknown,
       MOp_W_M4B_or_M1B,
       MOp_W_M8B_or_M2B,
       MOp_W_M8B_or_M4B,
       MOp_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B,
       MOp_WP_M8B_or_M4B_or_M2B,
       WP_I4B_or_I4B_or_I2B,
       WP_I8B_or_I4B_or_I2B,
       Extension = 0x80, // The instruction encoding form depends on the modrm.reg field. Extension table location in encoded in lower bits
    };

    // The following instrForm maps correspond to the amd64 instr maps
    // The comments are for debugging convenience.  The comments use a packed opcode followed by a list of observed mnemonics
    // The opcode is packed to be human readable.  PackedOpcode = opcode << 4 + pp
    //   - For Vex* and Xop* the pp is directly included in the encoding
    //   - For the Secondary, F38, and F3A pages the pp is not defined in the encoding, but affects instr form.
    //          - pp = 0 implies no prefix.
    //          - pp = 1 implies 0x66 OpSize prefix only.
    //          - pp = 2 implies 0xF3 prefix.
    //          - pp = 3 implies 0xF2 prefix.
    //   - For the primary and 3DNow pp is not used. And is always 0 in the comments


    // Instruction which change forms based on modrm.reg are encoded in this extension table.
    // Since there are 8 modrm.reg values, they occur is groups of 8.
    // Each group is referenced from the other tables below using Extension|(index >> 3).
    static const InstrForm instrFormExtension[153]
    {
        MOnly_M4B,                               // Primary:0xd90/0 fld
        None,
        MOnly_M4B,                               // Primary:0xd90/2 fst
        MOnly_M4B,                               // Primary:0xd90/3 fstp
        MOnly_MUnknown,                          // Primary:0xd90/4 fldenv,fldenvw
        MOnly_M2B,                               // Primary:0xd90/5 fldcw
        MOnly_MUnknown,                          // Primary:0xd90/6 fnstenv,fnstenvw
        MOnly_M2B,                               // Primary:0xd90/7 fnstcw
        MOnly_M4B,                               // Primary:0xdb0/0 fild
        MOnly_M4B,                               // Primary:0xdb0/1 fisttp
        MOnly_M4B,                               // Primary:0xdb0/2 fist
        MOnly_M4B,                               // Primary:0xdb0/3 fistp
        None,
        MOnly_M10B,                              // Primary:0xdb0/5 fld
        None,
        MOnly_M10B,                              // Primary:0xdb0/7 fstp
        MOnly_M8B,                               // Primary:0xdd0/0 fld
        MOnly_M8B,                               // Primary:0xdd0/1 fisttp
        MOnly_M8B,                               // Primary:0xdd0/2 fst
        MOnly_M8B,                               // Primary:0xdd0/3 fstp
        MOnly_MUnknown,                          // Primary:0xdd0/4 frstor,frstorw
        None,
        MOnly_MUnknown,                          // Primary:0xdd0/6 fnsave,fnsavew
        MOnly_M2B,                               // Primary:0xdd0/7 fnstsw
        MOnly_M2B,                               // Primary:0xdf0/0 fild
        MOnly_M2B,                               // Primary:0xdf0/1 fisttp
        MOnly_M2B,                               // Primary:0xdf0/2 fist
        MOnly_M2B,                               // Primary:0xdf0/3 fistp
        MOnly_M10B,                              // Primary:0xdf0/4 fbld
        MOnly_M8B,                               // Primary:0xdf0/5 fild
        MOnly_M10B,                              // Primary:0xdf0/6 fbstp
        MOnly_M8B,                               // Primary:0xdf0/7 fistp
        M1st_M1B_I1B,                            // Primary:0xf60/0 test
        None,
        MOnly_M1B,                               // Primary:0xf60/2 not
        MOnly_M1B,                               // Primary:0xf60/3 neg
        MOnly_M1B,                               // Primary:0xf60/4 mul
        MOnly_M1B,                               // Primary:0xf60/5 imul
        MOnly_M1B,                               // Primary:0xf60/6 div
        MOnly_M1B,                               // Primary:0xf60/7 idiv
        M1st_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B,   // Primary:0xf70/0 test
        None,
        MOnly_WP_M8B_or_M4B_or_M2B,              // Primary:0xf70/2 not
        MOnly_WP_M8B_or_M4B_or_M2B,              // Primary:0xf70/3 neg
        MOnly_WP_M8B_or_M4B_or_M2B,              // Primary:0xf70/4 mul
        MOnly_WP_M8B_or_M4B_or_M2B,              // Primary:0xf70/5 imul
        MOnly_WP_M8B_or_M4B_or_M2B,              // Primary:0xf70/6 div
        MOnly_WP_M8B_or_M4B_or_M2B,              // Primary:0xf70/7 idiv
        MOnly_WP_M8B_or_M4B_or_M2B,              // Primary:0xff0/0 inc
        MOnly_WP_M8B_or_M4B_or_M2B,              // Primary:0xff0/1 dec
        MOnly_WP_M8B_or_M8B_or_M2B,              // Primary:0xff0/2 call
        MOnly_P_M6B_or_M4B,                      // Primary:0xff0/3 call
        MOnly_WP_M8B_or_M8B_or_M2B,              // Primary:0xff0/4 jmp
        MOnly_P_M6B_or_M4B,                      // Primary:0xff0/5 jmp
        MOnly_WP_M8B_or_M8B_or_M2B,              // Primary:0xff0/6 push
        None,
        MOnly_M10B,                              // Secondary:0x010/0 sgdt
        MOnly_M10B,                              // Secondary:0x010/1 sidt
        MOnly_M10B,                              // Secondary:0x010/2 lgdt
        MOnly_M10B,                              // Secondary:0x010/3 lidt
        MOnly_M2B,                               // Secondary:0x010/4 smsw
        None,
        MOnly_M2B,                               // Secondary:0x010/6 lmsw
        MOnly_M1B,                               // Secondary:0x010/7 invlpg
        MOnly_M10B,                              // Secondary:0x011/0 sgdt
        MOnly_M10B,                              // Secondary:0x011/1 sidt
        MOnly_M10B,                              // Secondary:0x011/2 lgdt
        MOnly_M10B,                              // Secondary:0x011/3 lidt
        MOnly_M2B,                               // Secondary:0x011/4 smsw
        None,
        MOnly_M2B,                               // Secondary:0x011/6 lmsw
        MOnly_M1B,                               // Secondary:0x011/7 invlpg
        MOnly_M10B,                              // Secondary:0x012/0 sgdt
        MOnly_M10B,                              // Secondary:0x012/1 sidt
        MOnly_M10B,                              // Secondary:0x012/2 lgdt
        MOnly_M10B,                              // Secondary:0x012/3 lidt
        MOnly_M2B,                               // Secondary:0x012/4 smsw
        None,
        MOnly_M2B,                               // Secondary:0x012/6 lmsw
        MOnly_M1B,                               // Secondary:0x012/7 invlpg
        MOnly_M10B,                              // Secondary:0x013/0 sgdt
        MOnly_M10B,                              // Secondary:0x013/1 sidt
        MOnly_M10B,                              // Secondary:0x013/2 lgdt
        MOnly_M10B,                              // Secondary:0x013/3 lidt
        MOnly_M2B,                               // Secondary:0x013/4 smsw
        None,
        MOnly_M2B,                               // Secondary:0x013/6 lmsw
        MOnly_M1B,                               // Secondary:0x013/7 invlpg
        MOnly_MUnknown,                          // Secondary:0xae0/0 fxsave,fxsave64
        MOnly_MUnknown,                          // Secondary:0xae0/1 fxrstor,fxrstor64
        MOnly_M4B,                               // Secondary:0xae0/2 ldmxcsr
        MOnly_M4B,                               // Secondary:0xae0/3 stmxcsr
        MOnly_MUnknown,                          // Secondary:0xae0/4 xsave,xsave64
        MOnly_MUnknown,                          // Secondary:0xae0/5 xrstor,xrstor64
        MOnly_MUnknown,                          // Secondary:0xae0/6 xsaveopt,xsaveopt64
        MOnly_M1B,                               // Secondary:0xae0/7 clflush
        MOnly_MUnknown,                          // Secondary:0xae1/0 fxsave
        MOnly_MUnknown,                          // Secondary:0xae1/1 fxrstor
        MOnly_M4B,                               // Secondary:0xae1/2 ldmxcsr
        MOnly_M4B,                               // Secondary:0xae1/3 stmxcsr
        MOnly_MUnknown,                          // Secondary:0xae1/4 xsave
        MOnly_MUnknown,                          // Secondary:0xae1/5 xrstor
        MOnly_M1B,                               // Secondary:0xae1/6 clwb
        MOnly_M1B,                               // Secondary:0xae1/7 clflushopt
        MOnly_MUnknown,                          // Secondary:0xae2/0 fxsave
        MOnly_MUnknown,                          // Secondary:0xae2/1 fxrstor
        MOnly_M4B,                               // Secondary:0xae2/2 ldmxcsr
        MOnly_M4B,                               // Secondary:0xae2/3 stmxcsr
        MOnly_MUnknown,                          // Secondary:0xae2/4 xsave
        MOnly_MUnknown,                          // Secondary:0xae2/5 xrstor
        None,
        None,
        MOnly_MUnknown,                          // Secondary:0xae3/0 fxsave
        MOnly_MUnknown,                          // Secondary:0xae3/1 fxrstor
        MOnly_M4B,                               // Secondary:0xae3/2 ldmxcsr
        MOnly_M4B,                               // Secondary:0xae3/3 stmxcsr
        MOnly_MUnknown,                          // Secondary:0xae3/4 xsave
        MOnly_MUnknown,                          // Secondary:0xae3/5 xrstor
        None,
        None,
        None,
        MOnly_W_M16B_or_M8B,                     // Secondary:0xc70/1 cmpxchg16b,cmpxchg8b
        None,
        MOnly_MUnknown,                          // Secondary:0xc70/3 xrstors,xrstors64
        MOnly_MUnknown,                          // Secondary:0xc70/4 xsavec,xsavec64
        MOnly_MUnknown,                          // Secondary:0xc70/5 xsaves,xsaves64
        MOnly_M8B,                               // Secondary:0xc70/6 vmptrld
        MOnly_M8B,                               // Secondary:0xc70/7 vmptrst
        None,
        MOnly_M8B,                               // Secondary:0xc71/1 cmpxchg8b
        None,
        MOnly_MUnknown,                          // Secondary:0xc71/3 xrstors
        MOnly_MUnknown,                          // Secondary:0xc71/4 xsavec
        MOnly_MUnknown,                          // Secondary:0xc71/5 xsaves
        MOnly_M8B,                               // Secondary:0xc71/6 vmclear
        MOnly_M8B,                               // Secondary:0xc71/7 vmptrst
        None,
        MOnly_M8B,                               // Secondary:0xc72/1 cmpxchg8b
        None,
        MOnly_MUnknown,                          // Secondary:0xc72/3 xrstors
        MOnly_MUnknown,                          // Secondary:0xc72/4 xsavec
        MOnly_MUnknown,                          // Secondary:0xc72/5 xsaves
        MOnly_M8B,                               // Secondary:0xc72/6 vmxon
        MOnly_M8B,                               // Secondary:0xc72/7 vmptrst
        None,
        MOnly_M8B,                               // Secondary:0xc73/1 cmpxchg8b
        None,
        MOnly_MUnknown,                          // Secondary:0xc73/3 xrstors
        MOnly_MUnknown,                          // Secondary:0xc73/4 xsavec
        MOnly_MUnknown,                          // Secondary:0xc73/5 xsaves
        None,
        MOnly_M8B,                               // Secondary:0xc73/7 vmptrst
    };

    static const InstrForm instrFormPrimary[256]
    {
        M1st_M1B,                                // 0x000 add
        M1st_WP_M8B_or_M4B_or_M2B,               // 0x010 add
        MOp_M1B,                                 // 0x020 add
        MOp_WP_M8B_or_M4B_or_M2B,                // 0x030 add
        I1B,                                     // 0x040 add
        WP_I4B_or_I4B_or_I2B,                    // 0x050 add
        None,                                    // 0x060
        None,                                    // 0x070
        M1st_M1B,                                // 0x080 or
        M1st_WP_M8B_or_M4B_or_M2B,               // 0x090 or
        MOp_M1B,                                 // 0x0a0 or
        MOp_WP_M8B_or_M4B_or_M2B,                // 0x0b0 or
        I1B,                                     // 0x0c0 or
        WP_I4B_or_I4B_or_I2B,                    // 0x0d0 or
        None,                                    // 0x0e0
        None,                                    // 0x0f0
        M1st_M1B,                                // 0x100 adc
        M1st_WP_M8B_or_M4B_or_M2B,               // 0x110 adc
        MOp_M1B,                                 // 0x120 adc
        MOp_WP_M8B_or_M4B_or_M2B,                // 0x130 adc
        I1B,                                     // 0x140 adc
        WP_I4B_or_I4B_or_I2B,                    // 0x150 adc
        None,                                    // 0x160
        None,                                    // 0x170
        M1st_M1B,                                // 0x180 sbb
        M1st_WP_M8B_or_M4B_or_M2B,               // 0x190 sbb
        MOp_M1B,                                 // 0x1a0 sbb
        MOp_WP_M8B_or_M4B_or_M2B,                // 0x1b0 sbb
        I1B,                                     // 0x1c0 sbb
        WP_I4B_or_I4B_or_I2B,                    // 0x1d0 sbb
        None,                                    // 0x1e0
        None,                                    // 0x1f0
        M1st_M1B,                                // 0x200 and
        M1st_WP_M8B_or_M4B_or_M2B,               // 0x210 and
        MOp_M1B,                                 // 0x220 and
        MOp_WP_M8B_or_M4B_or_M2B,                // 0x230 and
        I1B,                                     // 0x240 and
        WP_I4B_or_I4B_or_I2B,                    // 0x250 and
        None,                                    // 0x260
        None,                                    // 0x270
        M1st_M1B,                                // 0x280 sub
        M1st_WP_M8B_or_M4B_or_M2B,               // 0x290 sub
        MOp_M1B,                                 // 0x2a0 sub
        MOp_WP_M8B_or_M4B_or_M2B,                // 0x2b0 sub
        I1B,                                     // 0x2c0 sub
        WP_I4B_or_I4B_or_I2B,                    // 0x2d0 sub
        None,                                    // 0x2e0
        None,                                    // 0x2f0
        M1st_M1B,                                // 0x300 xor
        M1st_WP_M8B_or_M4B_or_M2B,               // 0x310 xor
        MOp_M1B,                                 // 0x320 xor
        MOp_WP_M8B_or_M4B_or_M2B,                // 0x330 xor
        I1B,                                     // 0x340 xor
        WP_I4B_or_I4B_or_I2B,                    // 0x350 xor
        None,                                    // 0x360
        None,                                    // 0x370
        M1st_M1B,                                // 0x380 cmp
        M1st_WP_M8B_or_M4B_or_M2B,               // 0x390 cmp
        MOp_M1B,                                 // 0x3a0 cmp
        MOp_WP_M8B_or_M4B_or_M2B,                // 0x3b0 cmp
        I1B,                                     // 0x3c0 cmp
        WP_I4B_or_I4B_or_I2B,                    // 0x3d0 cmp
        None,                                    // 0x3e0
        None,                                    // 0x3f0
        None,                                    // 0x400
        None,                                    // 0x410
        None,                                    // 0x420
        None,                                    // 0x430
        None,                                    // 0x440
        None,                                    // 0x450
        None,                                    // 0x460
        None,                                    // 0x470
        None,                                    // 0x480
        None,                                    // 0x490
        None,                                    // 0x4a0
        None,                                    // 0x4b0
        None,                                    // 0x4c0
        None,                                    // 0x4d0
        None,                                    // 0x4e0
        None,                                    // 0x4f0
        None,                                    // 0x500 push
        None,                                    // 0x510 push
        None,                                    // 0x520 push
        None,                                    // 0x530 push
        None,                                    // 0x540 push
        None,                                    // 0x550 push
        None,                                    // 0x560 push
        None,                                    // 0x570 push
        None,                                    // 0x580 pop
        None,                                    // 0x590 pop
        None,                                    // 0x5a0 pop
        None,                                    // 0x5b0 pop
        None,                                    // 0x5c0 pop
        None,                                    // 0x5d0 pop
        None,                                    // 0x5e0 pop
        None,                                    // 0x5f0 pop
        None,                                    // 0x600
        None,                                    // 0x610
        None,                                    // 0x620
        MOp_M4B,                                 // 0x630 movsxd
        None,                                    // 0x640
        None,                                    // 0x650
        None,                                    // 0x660
        None,                                    // 0x670
        WP_I4B_or_I4B_or_I2B,                    // 0x680 push,pushw
        MOp_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B,    // 0x690 imul
        I1B,                                     // 0x6a0 push,pushw
        MOp_I1B_WP_M8B_or_M4B_or_M2B,            // 0x6b0 imul
        None,                                    // 0x6c0 ins
        None,                                    // 0x6d0 ins
        None,                                    // 0x6e0 outs
        None,                                    // 0x6f0 outs
        I1B,                                     // 0x700 jo
        I1B,                                     // 0x710 jno
        I1B,                                     // 0x720 jb
        I1B,                                     // 0x730 jae
        I1B,                                     // 0x740 je
        I1B,                                     // 0x750 jne
        I1B,                                     // 0x760 jbe
        I1B,                                     // 0x770 ja
        I1B,                                     // 0x780 js
        I1B,                                     // 0x790 jns
        I1B,                                     // 0x7a0 jp
        I1B,                                     // 0x7b0 jnp
        I1B,                                     // 0x7c0 jl
        I1B,                                     // 0x7d0 jge
        I1B,                                     // 0x7e0 jle
        I1B,                                     // 0x7f0 jg
        M1st_M1B_I1B,                            // 0x800 adc,add,and,cmp,or,sbb,sub,xor
        M1st_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B,   // 0x810 adc,add,and,cmp,or,sbb,sub,xor
        None,                                    // 0x820
        M1st_I1B_WP_M8B_or_M4B_or_M2B,           // 0x830 adc,add,and,cmp,or,sbb,sub,xor
        M1st_M1B,                                // 0x840 test
        M1st_WP_M8B_or_M4B_or_M2B,               // 0x850 test
        M1st_M1B,                                // 0x860 xchg
        M1st_WP_M8B_or_M4B_or_M2B,               // 0x870 xchg
        M1st_M1B,                                // 0x880 mov
        M1st_WP_M8B_or_M4B_or_M2B,               // 0x890 mov
        MOp_M1B,                                 // 0x8a0 mov
        MOp_WP_M8B_or_M4B_or_M2B,                // 0x8b0 mov
        M1st_M2B,                                // 0x8c0 mov
        MOp_MAddr,                               // 0x8d0 lea
        MOp_M2B,                                 // 0x8e0 mov
        MOnly_WP_M8B_or_M8B_or_M2B,              // 0x8f0 pop
        None,                                    // 0x900 nop,xchg
        None,                                    // 0x910 xchg
        None,                                    // 0x920 xchg
        None,                                    // 0x930 xchg
        None,                                    // 0x940 xchg
        None,                                    // 0x950 xchg
        None,                                    // 0x960 xchg
        None,                                    // 0x970 xchg
        None,                                    // 0x980 cbw,cdqe,cwde
        None,                                    // 0x990 cdq,cqo,cwd
        None,                                    // 0x9a0
        None,                                    // 0x9b0 fwait
        None,                                    // 0x9c0 pushf,pushfw
        None,                                    // 0x9d0 popf,popfw
        None,                                    // 0x9e0 sahf
        None,                                    // 0x9f0 lahf
        I8B,                                     // 0xa00 movabs
        I8B,                                     // 0xa10 movabs
        I8B,                                     // 0xa20 movabs
        I8B,                                     // 0xa30 movabs
        None,                                    // 0xa40 movs
        None,                                    // 0xa50 movs
        None,                                    // 0xa60 cmps
        None,                                    // 0xa70 cmps
        I1B,                                     // 0xa80 test
        WP_I4B_or_I4B_or_I2B,                    // 0xa90 test
        None,                                    // 0xaa0 stos
        None,                                    // 0xab0 stos
        None,                                    // 0xac0 lods
        None,                                    // 0xad0 lods
        None,                                    // 0xae0 scas
        None,                                    // 0xaf0 scas
        I1B,                                     // 0xb00 mov
        I1B,                                     // 0xb10 mov
        I1B,                                     // 0xb20 mov
        I1B,                                     // 0xb30 mov
        I1B,                                     // 0xb40 mov
        I1B,                                     // 0xb50 mov
        I1B,                                     // 0xb60 mov
        I1B,                                     // 0xb70 mov
        WP_I8B_or_I4B_or_I2B,                    // 0xb80 mov,movabs
        WP_I8B_or_I4B_or_I2B,                    // 0xb90 mov,movabs
        WP_I8B_or_I4B_or_I2B,                    // 0xba0 mov,movabs
        WP_I8B_or_I4B_or_I2B,                    // 0xbb0 mov,movabs
        WP_I8B_or_I4B_or_I2B,                    // 0xbc0 mov,movabs
        WP_I8B_or_I4B_or_I2B,                    // 0xbd0 mov,movabs
        WP_I8B_or_I4B_or_I2B,                    // 0xbe0 mov,movabs
        WP_I8B_or_I4B_or_I2B,                    // 0xbf0 mov,movabs
        M1st_M1B_I1B,                            // 0xc00 rcl,rcr,rol,ror,sar,shl,shr
        M1st_I1B_WP_M8B_or_M4B_or_M2B,           // 0xc10 rcl,rcr,rol,ror,sar,shl,shr
        I2B,                                     // 0xc20 ret,retw
        None,                                    // 0xc30 ret,retw
        None,                                    // 0xc40
        None,                                    // 0xc50
        M1st_M1B_I1B,                            // 0xc60 mov
        M1st_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B,   // 0xc70 mov
        I3B,                                     // 0xc80 enter,enterw
        None,                                    // 0xc90 leave,leavew
        I2B,                                     // 0xca0 retf,retfw
        None,                                    // 0xcb0 retf,retfw
        None,                                    // 0xcc0 int3
        I1B,                                     // 0xcd0 int
        None,                                    // 0xce0
        None,                                    // 0xcf0 iret,iretq,iretw
        M1st_M1B,                                // 0xd00 rcl,rcr,rol,ror,sar,shl,shr
        M1st_WP_M8B_or_M4B_or_M2B,               // 0xd10 rcl,rcr,rol,ror,sar,shl,shr
        M1st_M1B,                                // 0xd20 rcl,rcr,rol,ror,sar,shl,shr
        M1st_WP_M8B_or_M4B_or_M2B,               // 0xd30 rcl,rcr,rol,ror,sar,shl,shr
        None,                                    // 0xd40
        None,                                    // 0xd50
        None,                                    // 0xd60
        None,                                    // 0xd70 xlat
        MOnly_M4B,                               // 0xd80 fadd,fcom,fcomp,fdiv,fdivr,fmul,fsub,fsubr
        InstrForm(int(Extension)|0x00),          // 0xd90
        MOnly_M4B,                               // 0xda0 fiadd,ficom,ficomp,fidiv,fidivr,fimul,fisub,fisubr
        InstrForm(int(Extension)|0x01),          // 0xdb0
        MOnly_M8B,                               // 0xdc0 fadd,fcom,fcomp,fdiv,fdivr,fmul,fsub,fsubr
        InstrForm(int(Extension)|0x02),          // 0xdd0
        MOnly_M2B,                               // 0xde0 fiadd,ficom,ficomp,fidiv,fidivr,fimul,fisub,fisubr
        InstrForm(int(Extension)|0x03),          // 0xdf0
        I1B,                                     // 0xe00 loopne
        I1B,                                     // 0xe10 loope
        I1B,                                     // 0xe20 loop
        I1B,                                     // 0xe30 jrcxz
        I1B,                                     // 0xe40 in
        I1B,                                     // 0xe50 in
        I1B,                                     // 0xe60 out
        I1B,                                     // 0xe70 out
        WP_I4B_or_I4B_or_I2B,                    // 0xe80 call
        WP_I4B_or_I4B_or_I2B,                    // 0xe90 jmp
        None,                                    // 0xea0
        I1B,                                     // 0xeb0 jmp
        None,                                    // 0xec0 in
        None,                                    // 0xed0 in
        None,                                    // 0xee0 out
        None,                                    // 0xef0 out
        None,                                    // 0xf00
        None,                                    // 0xf10 icebp
        None,                                    // 0xf20
        None,                                    // 0xf30
        None,                                    // 0xf40 hlt
        None,                                    // 0xf50 cmc
        InstrForm(int(Extension)|0x04),          // 0xf60
        InstrForm(int(Extension)|0x05),          // 0xf70
        None,                                    // 0xf80 clc
        None,                                    // 0xf90 stc
        None,                                    // 0xfa0 cli
        None,                                    // 0xfb0 sti
        None,                                    // 0xfc0 cld
        None,                                    // 0xfd0 std
        MOnly_M1B,                               // 0xfe0 dec,inc
        InstrForm(int(Extension)|0x06),          // 0xff0
    };

    static const InstrForm instrForm3DNow[256]
    {
        MOp_M8B_I1B,                             // 0x000
        MOp_M8B_I1B,                             // 0x010
        MOp_M8B_I1B,                             // 0x020
        MOp_M8B_I1B,                             // 0x030
        MOp_M8B_I1B,                             // 0x040
        MOp_M8B_I1B,                             // 0x050
        MOp_M8B_I1B,                             // 0x060
        MOp_M8B_I1B,                             // 0x070
        MOp_M8B_I1B,                             // 0x080
        MOp_M8B_I1B,                             // 0x090
        MOp_M8B_I1B,                             // 0x0a0
        MOp_M8B_I1B,                             // 0x0b0
        MOp_M8B_I1B,                             // 0x0c0 pi2fw
        MOp_M8B_I1B,                             // 0x0d0 pi2fd
        MOp_M8B_I1B,                             // 0x0e0
        MOp_M8B_I1B,                             // 0x0f0
        MOp_M8B_I1B,                             // 0x100
        MOp_M8B_I1B,                             // 0x110
        MOp_M8B_I1B,                             // 0x120
        MOp_M8B_I1B,                             // 0x130
        MOp_M8B_I1B,                             // 0x140
        MOp_M8B_I1B,                             // 0x150
        MOp_M8B_I1B,                             // 0x160
        MOp_M8B_I1B,                             // 0x170
        MOp_M8B_I1B,                             // 0x180
        MOp_M8B_I1B,                             // 0x190
        MOp_M8B_I1B,                             // 0x1a0
        MOp_M8B_I1B,                             // 0x1b0
        MOp_M8B_I1B,                             // 0x1c0 pf2iw
        MOp_M8B_I1B,                             // 0x1d0 pf2id
        MOp_M8B_I1B,                             // 0x1e0
        MOp_M8B_I1B,                             // 0x1f0
        MOp_M8B_I1B,                             // 0x200
        MOp_M8B_I1B,                             // 0x210
        MOp_M8B_I1B,                             // 0x220
        MOp_M8B_I1B,                             // 0x230
        MOp_M8B_I1B,                             // 0x240
        MOp_M8B_I1B,                             // 0x250
        MOp_M8B_I1B,                             // 0x260
        MOp_M8B_I1B,                             // 0x270
        MOp_M8B_I1B,                             // 0x280
        MOp_M8B_I1B,                             // 0x290
        MOp_M8B_I1B,                             // 0x2a0
        MOp_M8B_I1B,                             // 0x2b0
        MOp_M8B_I1B,                             // 0x2c0
        MOp_M8B_I1B,                             // 0x2d0
        MOp_M8B_I1B,                             // 0x2e0
        MOp_M8B_I1B,                             // 0x2f0
        MOp_M8B_I1B,                             // 0x300
        MOp_M8B_I1B,                             // 0x310
        MOp_M8B_I1B,                             // 0x320
        MOp_M8B_I1B,                             // 0x330
        MOp_M8B_I1B,                             // 0x340
        MOp_M8B_I1B,                             // 0x350
        MOp_M8B_I1B,                             // 0x360
        MOp_M8B_I1B,                             // 0x370
        MOp_M8B_I1B,                             // 0x380
        MOp_M8B_I1B,                             // 0x390
        MOp_M8B_I1B,                             // 0x3a0
        MOp_M8B_I1B,                             // 0x3b0
        MOp_M8B_I1B,                             // 0x3c0
        MOp_M8B_I1B,                             // 0x3d0
        MOp_M8B_I1B,                             // 0x3e0
        MOp_M8B_I1B,                             // 0x3f0
        MOp_M8B_I1B,                             // 0x400
        MOp_M8B_I1B,                             // 0x410
        MOp_M8B_I1B,                             // 0x420
        MOp_M8B_I1B,                             // 0x430
        MOp_M8B_I1B,                             // 0x440
        MOp_M8B_I1B,                             // 0x450
        MOp_M8B_I1B,                             // 0x460
        MOp_M8B_I1B,                             // 0x470
        MOp_M8B_I1B,                             // 0x480
        MOp_M8B_I1B,                             // 0x490
        MOp_M8B_I1B,                             // 0x4a0
        MOp_M8B_I1B,                             // 0x4b0
        MOp_M8B_I1B,                             // 0x4c0
        MOp_M8B_I1B,                             // 0x4d0
        MOp_M8B_I1B,                             // 0x4e0
        MOp_M8B_I1B,                             // 0x4f0
        MOp_M8B_I1B,                             // 0x500
        MOp_M8B_I1B,                             // 0x510
        MOp_M8B_I1B,                             // 0x520
        MOp_M8B_I1B,                             // 0x530
        MOp_M8B_I1B,                             // 0x540
        MOp_M8B_I1B,                             // 0x550
        MOp_M8B_I1B,                             // 0x560
        MOp_M8B_I1B,                             // 0x570
        MOp_M8B_I1B,                             // 0x580
        MOp_M8B_I1B,                             // 0x590
        MOp_M8B_I1B,                             // 0x5a0
        MOp_M8B_I1B,                             // 0x5b0
        MOp_M8B_I1B,                             // 0x5c0
        MOp_M8B_I1B,                             // 0x5d0
        MOp_M8B_I1B,                             // 0x5e0
        MOp_M8B_I1B,                             // 0x5f0
        MOp_M8B_I1B,                             // 0x600
        MOp_M8B_I1B,                             // 0x610
        MOp_M8B_I1B,                             // 0x620
        MOp_M8B_I1B,                             // 0x630
        MOp_M8B_I1B,                             // 0x640
        MOp_M8B_I1B,                             // 0x650
        MOp_M8B_I1B,                             // 0x660
        MOp_M8B_I1B,                             // 0x670
        MOp_M8B_I1B,                             // 0x680
        MOp_M8B_I1B,                             // 0x690
        MOp_M8B_I1B,                             // 0x6a0
        MOp_M8B_I1B,                             // 0x6b0
        MOp_M8B_I1B,                             // 0x6c0
        MOp_M8B_I1B,                             // 0x6d0
        MOp_M8B_I1B,                             // 0x6e0
        MOp_M8B_I1B,                             // 0x6f0
        MOp_M8B_I1B,                             // 0x700
        MOp_M8B_I1B,                             // 0x710
        MOp_M8B_I1B,                             // 0x720
        MOp_M8B_I1B,                             // 0x730
        MOp_M8B_I1B,                             // 0x740
        MOp_M8B_I1B,                             // 0x750
        MOp_M8B_I1B,                             // 0x760
        MOp_M8B_I1B,                             // 0x770
        MOp_M8B_I1B,                             // 0x780
        MOp_M8B_I1B,                             // 0x790
        MOp_M8B_I1B,                             // 0x7a0
        MOp_M8B_I1B,                             // 0x7b0
        MOp_M8B_I1B,                             // 0x7c0
        MOp_M8B_I1B,                             // 0x7d0
        MOp_M8B_I1B,                             // 0x7e0
        MOp_M8B_I1B,                             // 0x7f0
        MOp_M8B_I1B,                             // 0x800
        MOp_M8B_I1B,                             // 0x810
        MOp_M8B_I1B,                             // 0x820
        MOp_M8B_I1B,                             // 0x830
        MOp_M8B_I1B,                             // 0x840
        MOp_M8B_I1B,                             // 0x850
        MOp_M8B_I1B,                             // 0x860
        MOp_M8B_I1B,                             // 0x870
        MOp_M8B_I1B,                             // 0x880
        MOp_M8B_I1B,                             // 0x890
        MOp_M8B_I1B,                             // 0x8a0 pfnacc
        MOp_M8B_I1B,                             // 0x8b0
        MOp_M8B_I1B,                             // 0x8c0
        MOp_M8B_I1B,                             // 0x8d0
        MOp_M8B_I1B,                             // 0x8e0 pfpnacc
        MOp_M8B_I1B,                             // 0x8f0
        MOp_M8B_I1B,                             // 0x900 pfcmpge
        MOp_M8B_I1B,                             // 0x910
        MOp_M8B_I1B,                             // 0x920
        MOp_M8B_I1B,                             // 0x930
        MOp_M8B_I1B,                             // 0x940 pfmin
        MOp_M8B_I1B,                             // 0x950
        MOp_M8B_I1B,                             // 0x960 pfrcp
        MOp_M8B_I1B,                             // 0x970 pfrsqrt
        MOp_M8B_I1B,                             // 0x980
        MOp_M8B_I1B,                             // 0x990
        MOp_M8B_I1B,                             // 0x9a0 pfsub
        MOp_M8B_I1B,                             // 0x9b0
        MOp_M8B_I1B,                             // 0x9c0
        MOp_M8B_I1B,                             // 0x9d0
        MOp_M8B_I1B,                             // 0x9e0 pfadd
        MOp_M8B_I1B,                             // 0x9f0
        MOp_M8B_I1B,                             // 0xa00 pfcmpgt
        MOp_M8B_I1B,                             // 0xa10
        MOp_M8B_I1B,                             // 0xa20
        MOp_M8B_I1B,                             // 0xa30
        MOp_M8B_I1B,                             // 0xa40 pfmax
        MOp_M8B_I1B,                             // 0xa50
        MOp_M8B_I1B,                             // 0xa60 pfrcpit1
        MOp_M8B_I1B,                             // 0xa70 pfrsqit1
        MOp_M8B_I1B,                             // 0xa80
        MOp_M8B_I1B,                             // 0xa90
        MOp_M8B_I1B,                             // 0xaa0 pfsubr
        MOp_M8B_I1B,                             // 0xab0
        MOp_M8B_I1B,                             // 0xac0
        MOp_M8B_I1B,                             // 0xad0
        MOp_M8B_I1B,                             // 0xae0 pfacc
        MOp_M8B_I1B,                             // 0xaf0
        MOp_M8B_I1B,                             // 0xb00 pfcmpeq
        MOp_M8B_I1B,                             // 0xb10
        MOp_M8B_I1B,                             // 0xb20
        MOp_M8B_I1B,                             // 0xb30
        MOp_M8B_I1B,                             // 0xb40 pfmul
        MOp_M8B_I1B,                             // 0xb50
        MOp_M8B_I1B,                             // 0xb60 pfrcpit2
        MOp_M8B_I1B,                             // 0xb70 pmulhrw
        MOp_M8B_I1B,                             // 0xb80
        MOp_M8B_I1B,                             // 0xb90
        MOp_M8B_I1B,                             // 0xba0
        MOp_M8B_I1B,                             // 0xbb0 pswapd
        MOp_M8B_I1B,                             // 0xbc0
        MOp_M8B_I1B,                             // 0xbd0
        MOp_M8B_I1B,                             // 0xbe0
        MOp_M8B_I1B,                             // 0xbf0 pavgusb
        MOp_M8B_I1B,                             // 0xc00
        MOp_M8B_I1B,                             // 0xc10
        MOp_M8B_I1B,                             // 0xc20
        MOp_M8B_I1B,                             // 0xc30
        MOp_M8B_I1B,                             // 0xc40
        MOp_M8B_I1B,                             // 0xc50
        MOp_M8B_I1B,                             // 0xc60
        MOp_M8B_I1B,                             // 0xc70
        MOp_M8B_I1B,                             // 0xc80
        MOp_M8B_I1B,                             // 0xc90
        MOp_M8B_I1B,                             // 0xca0
        MOp_M8B_I1B,                             // 0xcb0
        MOp_M8B_I1B,                             // 0xcc0
        MOp_M8B_I1B,                             // 0xcd0
        MOp_M8B_I1B,                             // 0xce0
        MOp_M8B_I1B,                             // 0xcf0
        MOp_M8B_I1B,                             // 0xd00
        MOp_M8B_I1B,                             // 0xd10
        MOp_M8B_I1B,                             // 0xd20
        MOp_M8B_I1B,                             // 0xd30
        MOp_M8B_I1B,                             // 0xd40
        MOp_M8B_I1B,                             // 0xd50
        MOp_M8B_I1B,                             // 0xd60
        MOp_M8B_I1B,                             // 0xd70
        MOp_M8B_I1B,                             // 0xd80
        MOp_M8B_I1B,                             // 0xd90
        MOp_M8B_I1B,                             // 0xda0
        MOp_M8B_I1B,                             // 0xdb0
        MOp_M8B_I1B,                             // 0xdc0
        MOp_M8B_I1B,                             // 0xdd0
        MOp_M8B_I1B,                             // 0xde0
        MOp_M8B_I1B,                             // 0xdf0
        MOp_M8B_I1B,                             // 0xe00
        MOp_M8B_I1B,                             // 0xe10
        MOp_M8B_I1B,                             // 0xe20
        MOp_M8B_I1B,                             // 0xe30
        MOp_M8B_I1B,                             // 0xe40
        MOp_M8B_I1B,                             // 0xe50
        MOp_M8B_I1B,                             // 0xe60
        MOp_M8B_I1B,                             // 0xe70
        MOp_M8B_I1B,                             // 0xe80
        MOp_M8B_I1B,                             // 0xe90
        MOp_M8B_I1B,                             // 0xea0
        MOp_M8B_I1B,                             // 0xeb0
        MOp_M8B_I1B,                             // 0xec0
        MOp_M8B_I1B,                             // 0xed0
        MOp_M8B_I1B,                             // 0xee0
        MOp_M8B_I1B,                             // 0xef0
        MOp_M8B_I1B,                             // 0xf00
        MOp_M8B_I1B,                             // 0xf10
        MOp_M8B_I1B,                             // 0xf20
        MOp_M8B_I1B,                             // 0xf30
        MOp_M8B_I1B,                             // 0xf40
        MOp_M8B_I1B,                             // 0xf50
        MOp_M8B_I1B,                             // 0xf60
        MOp_M8B_I1B,                             // 0xf70
        MOp_M8B_I1B,                             // 0xf80
        MOp_M8B_I1B,                             // 0xf90
        MOp_M8B_I1B,                             // 0xfa0
        MOp_M8B_I1B,                             // 0xfb0
        MOp_M8B_I1B,                             // 0xfc0
        MOp_M8B_I1B,                             // 0xfd0
        MOp_M8B_I1B,                             // 0xfe0
        MOp_M8B_I1B,                             // 0xff0
    };

    static const InstrForm instrFormSecondary[1024]
    {
        MOnly_M2B,                               // 0x000 lldt,ltr,sldt,str,verr,verw
        MOnly_M2B,                               // 0x001 lldt,ltr,sldt,str,verr,verw
        MOnly_M2B,                               // 0x002 lldt,ltr,sldt,str,verr,verw
        MOnly_M2B,                               // 0x003 lldt,ltr,sldt,str,verr,verw
        InstrForm(int(Extension)|0x07),          // 0x010
        InstrForm(int(Extension)|0x08),          // 0x011
        InstrForm(int(Extension)|0x09),          // 0x012
        InstrForm(int(Extension)|0x0a),          // 0x013
        MOp_M2B,                                 // 0x020 lar
        MOp_M2B,                                 // 0x021 lar
        MOp_M2B,                                 // 0x022 lar
        MOp_M2B,                                 // 0x023 lar
        MOp_M2B,                                 // 0x030 lsl
        MOp_M2B,                                 // 0x031 lsl
        MOp_M2B,                                 // 0x032 lsl
        MOp_M2B,                                 // 0x033 lsl
        None,                                    // 0x040
        None,                                    // 0x041
        None,                                    // 0x042
        None,                                    // 0x043
        None,                                    // 0x050 syscall
        None,                                    // 0x051 syscall
        None,                                    // 0x052 syscall
        None,                                    // 0x053 syscall
        None,                                    // 0x060 clts
        None,                                    // 0x061 clts
        None,                                    // 0x062 clts
        None,                                    // 0x063 clts
        None,                                    // 0x070 sysret,sysretq
        None,                                    // 0x071 sysretw
        None,                                    // 0x072 sysret
        None,                                    // 0x073 sysret
        None,                                    // 0x080 invd
        None,                                    // 0x081 invd
        None,                                    // 0x082 invd
        None,                                    // 0x083 invd
        None,                                    // 0x090 wbinvd
        None,                                    // 0x091 wbinvd
        None,                                    // 0x092 wbinvd
        None,                                    // 0x093 wbinvd
        None,                                    // 0x0a0
        None,                                    // 0x0a1
        None,                                    // 0x0a2
        None,                                    // 0x0a3
        None,                                    // 0x0b0 ud2
        None,                                    // 0x0b1 ud2
        None,                                    // 0x0b2 ud2
        None,                                    // 0x0b3 ud2
        None,                                    // 0x0c0
        None,                                    // 0x0c1
        None,                                    // 0x0c2
        None,                                    // 0x0c3
        MOnly_M1B,                               // 0x0d0 prefetch,prefetchw,prefetchwt1
        MOnly_M1B,                               // 0x0d1 prefetch,prefetchw,prefetchwt1
        MOnly_M1B,                               // 0x0d2 prefetch,prefetchw,prefetchwt1
        MOnly_M1B,                               // 0x0d3 prefetch,prefetchw,prefetchwt1
        None,                                    // 0x0e0 femms
        None,                                    // 0x0e1 femms
        None,                                    // 0x0e2 femms
        None,                                    // 0x0e3 femms
        None,                                    // 0x0f0
        None,                                    // 0x0f1
        None,                                    // 0x0f2
        None,                                    // 0x0f3
        MOp_M16B,                                // 0x100 movups
        MOp_M16B,                                // 0x101 movupd
        MOp_M4B,                                 // 0x102 movss
        MOp_M8B,                                 // 0x103 movsd
        M1st_M16B,                               // 0x110 movups
        M1st_M16B,                               // 0x111 movupd
        M1st_M4B,                                // 0x112 movss
        M1st_M8B,                                // 0x113 movsd
        MOp_M8B,                                 // 0x120 movlps
        MOp_M8B,                                 // 0x121 movlpd
        MOp_M16B,                                // 0x122 movsldup
        MOp_M8B,                                 // 0x123 movddup
        M1st_M8B,                                // 0x130 movlps
        M1st_M8B,                                // 0x131 movlpd
        None,                                    // 0x132
        None,                                    // 0x133
        MOp_M16B,                                // 0x140 unpcklps
        MOp_M16B,                                // 0x141 unpcklpd
        None,                                    // 0x142
        None,                                    // 0x143
        MOp_M16B,                                // 0x150 unpckhps
        MOp_M16B,                                // 0x151 unpckhpd
        None,                                    // 0x152
        None,                                    // 0x153
        MOp_M8B,                                 // 0x160 movhps
        MOp_M8B,                                 // 0x161 movhpd
        MOp_M16B,                                // 0x162 movshdup
        None,                                    // 0x163
        M1st_M8B,                                // 0x170 movhps
        M1st_M8B,                                // 0x171 movhpd
        None,                                    // 0x172
        None,                                    // 0x173
        MOnly_M1B,                               // 0x180 nop/reserved,prefetchnta,prefetcht0,prefetcht1,prefetcht2
        MOnly_M1B,                               // 0x181 nop/reserved,prefetchnta,prefetcht0,prefetcht1,prefetcht2
        MOnly_M1B,                               // 0x182 nop/reserved,prefetchnta,prefetcht0,prefetcht1,prefetcht2
        MOnly_M1B,                               // 0x183 nop/reserved,prefetchnta,prefetcht0,prefetcht1,prefetcht2
        MOnly_W_M8B_or_M4B,                      // 0x190 nop
        MOnly_M2B,                               // 0x191 nop
        MOnly_M4B,                               // 0x192 nop
        MOnly_M4B,                               // 0x193 nop
        MOp_MUnknown,                            // 0x1a0 bndldx
        MOp_MUnknown,                            // 0x1a1 bndmov
        MOp_MUnknown,                            // 0x1a2 bndcl
        MOp_MUnknown,                            // 0x1a3 bndcu
        M1st_MUnknown,                           // 0x1b0 bndstx
        M1st_MUnknown,                           // 0x1b1 bndmov
        MOp_MUnknown,                            // 0x1b2 bndmk
        MOp_MUnknown,                            // 0x1b3 bndcn
        MOnly_W_M8B_or_M4B,                      // 0x1c0 nop
        MOnly_M2B,                               // 0x1c1 nop
        MOnly_M4B,                               // 0x1c2 nop
        MOnly_M4B,                               // 0x1c3 nop
        MOnly_W_M8B_or_M4B,                      // 0x1d0 nop
        MOnly_M2B,                               // 0x1d1 nop
        MOnly_M4B,                               // 0x1d2 nop
        MOnly_M4B,                               // 0x1d3 nop
        MOnly_W_M8B_or_M4B,                      // 0x1e0 nop
        MOnly_M2B,                               // 0x1e1 nop
        MOnly_M4B,                               // 0x1e2 nop
        MOnly_M4B,                               // 0x1e3 nop
        MOnly_W_M8B_or_M4B,                      // 0x1f0 nop
        MOnly_M2B,                               // 0x1f1 nop
        MOnly_M4B,                               // 0x1f2 nop
        MOnly_M4B,                               // 0x1f3 nop
        I1B,                                     // 0x200 mov
        I1B,                                     // 0x201 mov
        I1B,                                     // 0x202 mov
        I1B,                                     // 0x203 mov
        I1B,                                     // 0x210 mov
        I1B,                                     // 0x211 mov
        I1B,                                     // 0x212 mov
        I1B,                                     // 0x213 mov
        I1B,                                     // 0x220 mov
        I1B,                                     // 0x221 mov
        I1B,                                     // 0x222 mov
        I1B,                                     // 0x223 mov
        I1B,                                     // 0x230 mov
        I1B,                                     // 0x231 mov
        I1B,                                     // 0x232 mov
        I1B,                                     // 0x233 mov
        None,                                    // 0x240
        None,                                    // 0x241
        None,                                    // 0x242
        None,                                    // 0x243
        None,                                    // 0x250
        None,                                    // 0x251
        None,                                    // 0x252
        None,                                    // 0x253
        None,                                    // 0x260
        None,                                    // 0x261
        None,                                    // 0x262
        None,                                    // 0x263
        None,                                    // 0x270
        None,                                    // 0x271
        None,                                    // 0x272
        None,                                    // 0x273
        MOp_M16B,                                // 0x280 movaps
        MOp_M16B,                                // 0x281 movapd
        None,                                    // 0x282
        None,                                    // 0x283
        M1st_M16B,                               // 0x290 movaps
        M1st_M16B,                               // 0x291 movapd
        None,                                    // 0x292
        None,                                    // 0x293
        MOp_M8B,                                 // 0x2a0 cvtpi2ps
        MOp_M8B,                                 // 0x2a1 cvtpi2pd
        MOp_M4B,                                 // 0x2a2 cvtsi2ss
        MOp_M4B,                                 // 0x2a3 cvtsi2sd
        M1st_M16B,                               // 0x2b0 movntps
        M1st_M16B,                               // 0x2b1 movntpd
        M1st_M4B,                                // 0x2b2 movntss
        M1st_M8B,                                // 0x2b3 movntsd
        MOp_M8B,                                 // 0x2c0 cvttps2pi
        MOp_M16B,                                // 0x2c1 cvttpd2pi
        MOp_M4B,                                 // 0x2c2 cvttss2si
        MOp_M8B,                                 // 0x2c3 cvttsd2si
        MOp_M8B,                                 // 0x2d0 cvtps2pi
        MOp_M16B,                                // 0x2d1 cvtpd2pi
        MOp_M4B,                                 // 0x2d2 cvtss2si
        MOp_M8B,                                 // 0x2d3 cvtsd2si
        MOp_M4B,                                 // 0x2e0 ucomiss
        MOp_M8B,                                 // 0x2e1 ucomisd
        None,                                    // 0x2e2
        None,                                    // 0x2e3
        MOp_M4B,                                 // 0x2f0 comiss
        MOp_M8B,                                 // 0x2f1 comisd
        None,                                    // 0x2f2
        None,                                    // 0x2f3
        None,                                    // 0x300 wrmsr
        None,                                    // 0x301 wrmsr
        None,                                    // 0x302 wrmsr
        None,                                    // 0x303 wrmsr
        None,                                    // 0x310 rdtsc
        None,                                    // 0x311 rdtsc
        None,                                    // 0x312 rdtsc
        None,                                    // 0x313 rdtsc
        None,                                    // 0x320 rdmsr
        None,                                    // 0x321 rdmsr
        None,                                    // 0x322 rdmsr
        None,                                    // 0x323 rdmsr
        None,                                    // 0x330 rdpmc
        None,                                    // 0x331 rdpmc
        None,                                    // 0x332 rdpmc
        None,                                    // 0x333 rdpmc
        None,                                    // 0x340 sysenter
        None,                                    // 0x341 sysenter
        None,                                    // 0x342 sysenter
        None,                                    // 0x343 sysenter
        None,                                    // 0x350 sysexit
        None,                                    // 0x351 sysexit
        None,                                    // 0x352 sysexit
        None,                                    // 0x353 sysexit
        None,                                    // 0x360
        None,                                    // 0x361
        None,                                    // 0x362
        None,                                    // 0x363
        None,                                    // 0x370 getsec
        None,                                    // 0x371 getsec
        None,                                    // 0x372 getsec
        None,                                    // 0x373 getsec
        None,                                    // 0x380
        None,                                    // 0x381
        None,                                    // 0x382
        None,                                    // 0x383
        None,                                    // 0x390
        None,                                    // 0x391
        None,                                    // 0x392
        None,                                    // 0x393
        None,                                    // 0x3a0
        None,                                    // 0x3a1
        None,                                    // 0x3a2
        None,                                    // 0x3a3
        None,                                    // 0x3b0
        None,                                    // 0x3b1
        None,                                    // 0x3b2
        None,                                    // 0x3b3
        None,                                    // 0x3c0
        None,                                    // 0x3c1
        None,                                    // 0x3c2
        None,                                    // 0x3c3
        None,                                    // 0x3d0
        None,                                    // 0x3d1
        None,                                    // 0x3d2
        None,                                    // 0x3d3
        None,                                    // 0x3e0
        None,                                    // 0x3e1
        None,                                    // 0x3e2
        None,                                    // 0x3e3
        None,                                    // 0x3f0
        None,                                    // 0x3f1
        None,                                    // 0x3f2
        None,                                    // 0x3f3
        MOp_W_M8B_or_M4B,                        // 0x400 cmovo
        MOp_M2B,                                 // 0x401 cmovo
        MOp_M4B,                                 // 0x402 cmovo
        MOp_M4B,                                 // 0x403 cmovo
        MOp_W_M8B_or_M4B,                        // 0x410 cmovno
        MOp_M2B,                                 // 0x411 cmovno
        MOp_M4B,                                 // 0x412 cmovno
        MOp_M4B,                                 // 0x413 cmovno
        MOp_W_M8B_or_M4B,                        // 0x420 cmovb
        MOp_M2B,                                 // 0x421 cmovb
        MOp_M4B,                                 // 0x422 cmovb
        MOp_M4B,                                 // 0x423 cmovb
        MOp_W_M8B_or_M4B,                        // 0x430 cmovae
        MOp_M2B,                                 // 0x431 cmovae
        MOp_M4B,                                 // 0x432 cmovae
        MOp_M4B,                                 // 0x433 cmovae
        MOp_W_M8B_or_M4B,                        // 0x440 cmove
        MOp_M2B,                                 // 0x441 cmove
        MOp_M4B,                                 // 0x442 cmove
        MOp_M4B,                                 // 0x443 cmove
        MOp_W_M8B_or_M4B,                        // 0x450 cmovne
        MOp_M2B,                                 // 0x451 cmovne
        MOp_M4B,                                 // 0x452 cmovne
        MOp_M4B,                                 // 0x453 cmovne
        MOp_W_M8B_or_M4B,                        // 0x460 cmovbe
        MOp_M2B,                                 // 0x461 cmovbe
        MOp_M4B,                                 // 0x462 cmovbe
        MOp_M4B,                                 // 0x463 cmovbe
        MOp_W_M8B_or_M4B,                        // 0x470 cmova
        MOp_M2B,                                 // 0x471 cmova
        MOp_M4B,                                 // 0x472 cmova
        MOp_M4B,                                 // 0x473 cmova
        MOp_W_M8B_or_M4B,                        // 0x480 cmovs
        MOp_M2B,                                 // 0x481 cmovs
        MOp_M4B,                                 // 0x482 cmovs
        MOp_M4B,                                 // 0x483 cmovs
        MOp_W_M8B_or_M4B,                        // 0x490 cmovns
        MOp_M2B,                                 // 0x491 cmovns
        MOp_M4B,                                 // 0x492 cmovns
        MOp_M4B,                                 // 0x493 cmovns
        MOp_W_M8B_or_M4B,                        // 0x4a0 cmovp
        MOp_M2B,                                 // 0x4a1 cmovp
        MOp_M4B,                                 // 0x4a2 cmovp
        MOp_M4B,                                 // 0x4a3 cmovp
        MOp_W_M8B_or_M4B,                        // 0x4b0 cmovnp
        MOp_M2B,                                 // 0x4b1 cmovnp
        MOp_M4B,                                 // 0x4b2 cmovnp
        MOp_M4B,                                 // 0x4b3 cmovnp
        MOp_W_M8B_or_M4B,                        // 0x4c0 cmovl
        MOp_M2B,                                 // 0x4c1 cmovl
        MOp_M4B,                                 // 0x4c2 cmovl
        MOp_M4B,                                 // 0x4c3 cmovl
        MOp_W_M8B_or_M4B,                        // 0x4d0 cmovge
        MOp_M2B,                                 // 0x4d1 cmovge
        MOp_M4B,                                 // 0x4d2 cmovge
        MOp_M4B,                                 // 0x4d3 cmovge
        MOp_W_M8B_or_M4B,                        // 0x4e0 cmovle
        MOp_M2B,                                 // 0x4e1 cmovle
        MOp_M4B,                                 // 0x4e2 cmovle
        MOp_M4B,                                 // 0x4e3 cmovle
        MOp_W_M8B_or_M4B,                        // 0x4f0 cmovg
        MOp_M2B,                                 // 0x4f1 cmovg
        MOp_M4B,                                 // 0x4f2 cmovg
        MOp_M4B,                                 // 0x4f3 cmovg
        None,                                    // 0x500
        None,                                    // 0x501
        None,                                    // 0x502
        None,                                    // 0x503
        MOp_M16B,                                // 0x510 sqrtps
        MOp_M16B,                                // 0x511 sqrtpd
        MOp_M4B,                                 // 0x512 sqrtss
        MOp_M8B,                                 // 0x513 sqrtsd
        MOp_M16B,                                // 0x520 rsqrtps
        None,                                    // 0x521
        MOp_M4B,                                 // 0x522 rsqrtss
        None,                                    // 0x523
        MOp_M16B,                                // 0x530 rcpps
        None,                                    // 0x531
        MOp_M4B,                                 // 0x532 rcpss
        None,                                    // 0x533
        MOp_M16B,                                // 0x540 andps
        MOp_M16B,                                // 0x541 andpd
        None,                                    // 0x542
        None,                                    // 0x543
        MOp_M16B,                                // 0x550 andnps
        MOp_M16B,                                // 0x551 andnpd
        None,                                    // 0x552
        None,                                    // 0x553
        MOp_M16B,                                // 0x560 orps
        MOp_M16B,                                // 0x561 orpd
        None,                                    // 0x562
        None,                                    // 0x563
        MOp_M16B,                                // 0x570 xorps
        MOp_M16B,                                // 0x571 xorpd
        None,                                    // 0x572
        None,                                    // 0x573
        MOp_M16B,                                // 0x580 addps
        MOp_M16B,                                // 0x581 addpd
        MOp_M4B,                                 // 0x582 addss
        MOp_M8B,                                 // 0x583 addsd
        MOp_M16B,                                // 0x590 mulps
        MOp_M16B,                                // 0x591 mulpd
        MOp_M4B,                                 // 0x592 mulss
        MOp_M8B,                                 // 0x593 mulsd
        MOp_M8B,                                 // 0x5a0 cvtps2pd
        MOp_M16B,                                // 0x5a1 cvtpd2ps
        MOp_M4B,                                 // 0x5a2 cvtss2sd
        MOp_M8B,                                 // 0x5a3 cvtsd2ss
        MOp_M16B,                                // 0x5b0 cvtdq2ps
        MOp_M16B,                                // 0x5b1 cvtps2dq
        MOp_M16B,                                // 0x5b2 cvttps2dq
        None,                                    // 0x5b3
        MOp_M16B,                                // 0x5c0 subps
        MOp_M16B,                                // 0x5c1 subpd
        MOp_M4B,                                 // 0x5c2 subss
        MOp_M8B,                                 // 0x5c3 subsd
        MOp_M16B,                                // 0x5d0 minps
        MOp_M16B,                                // 0x5d1 minpd
        MOp_M4B,                                 // 0x5d2 minss
        MOp_M8B,                                 // 0x5d3 minsd
        MOp_M16B,                                // 0x5e0 divps
        MOp_M16B,                                // 0x5e1 divpd
        MOp_M4B,                                 // 0x5e2 divss
        MOp_M8B,                                 // 0x5e3 divsd
        MOp_M16B,                                // 0x5f0 maxps
        MOp_M16B,                                // 0x5f1 maxpd
        MOp_M4B,                                 // 0x5f2 maxss
        MOp_M8B,                                 // 0x5f3 maxsd
        MOp_M4B,                                 // 0x600 punpcklbw
        MOp_M16B,                                // 0x601 punpcklbw
        None,                                    // 0x602
        None,                                    // 0x603
        MOp_M4B,                                 // 0x610 punpcklwd
        MOp_M16B,                                // 0x611 punpcklwd
        None,                                    // 0x612
        None,                                    // 0x613
        MOp_M4B,                                 // 0x620 punpckldq
        MOp_M16B,                                // 0x621 punpckldq
        None,                                    // 0x622
        None,                                    // 0x623
        MOp_M8B,                                 // 0x630 packsswb
        MOp_M16B,                                // 0x631 packsswb
        None,                                    // 0x632
        None,                                    // 0x633
        MOp_M8B,                                 // 0x640 pcmpgtb
        MOp_M16B,                                // 0x641 pcmpgtb
        None,                                    // 0x642
        None,                                    // 0x643
        MOp_M8B,                                 // 0x650 pcmpgtw
        MOp_M16B,                                // 0x651 pcmpgtw
        None,                                    // 0x652
        None,                                    // 0x653
        MOp_M8B,                                 // 0x660 pcmpgtd
        MOp_M16B,                                // 0x661 pcmpgtd
        None,                                    // 0x662
        None,                                    // 0x663
        MOp_M8B,                                 // 0x670 packuswb
        MOp_M16B,                                // 0x671 packuswb
        None,                                    // 0x672
        None,                                    // 0x673
        MOp_M8B,                                 // 0x680 punpckhbw
        MOp_M16B,                                // 0x681 punpckhbw
        None,                                    // 0x682
        None,                                    // 0x683
        MOp_M8B,                                 // 0x690 punpckhwd
        MOp_M16B,                                // 0x691 punpckhwd
        None,                                    // 0x692
        None,                                    // 0x693
        MOp_M8B,                                 // 0x6a0 punpckhdq
        MOp_M16B,                                // 0x6a1 punpckhdq
        None,                                    // 0x6a2
        None,                                    // 0x6a3
        MOp_M8B,                                 // 0x6b0 packssdw
        MOp_M16B,                                // 0x6b1 packssdw
        None,                                    // 0x6b2
        None,                                    // 0x6b3
        None,                                    // 0x6c0
        MOp_M16B,                                // 0x6c1 punpcklqdq
        None,                                    // 0x6c2
        None,                                    // 0x6c3
        None,                                    // 0x6d0
        MOp_M16B,                                // 0x6d1 punpckhqdq
        None,                                    // 0x6d2
        None,                                    // 0x6d3
        MOp_W_M8B_or_M4B,                        // 0x6e0 movd,movq
        MOp_M4B,                                 // 0x6e1 movd
        None,                                    // 0x6e2
        None,                                    // 0x6e3
        MOp_M8B,                                 // 0x6f0 movq
        MOp_M16B,                                // 0x6f1 movdqa
        MOp_M16B,                                // 0x6f2 movdqu
        None,                                    // 0x6f3
        MOp_M8B_I1B,                             // 0x700 pshufw
        MOp_M16B_I1B,                            // 0x701 pshufd
        MOp_M16B_I1B,                            // 0x702 pshufhw
        MOp_M16B_I1B,                            // 0x703 pshuflw
        None,                                    // 0x710
        None,                                    // 0x711
        None,                                    // 0x712
        None,                                    // 0x713
        None,                                    // 0x720
        None,                                    // 0x721
        None,                                    // 0x722
        None,                                    // 0x723
        None,                                    // 0x730
        None,                                    // 0x731
        None,                                    // 0x732
        None,                                    // 0x733
        MOp_M8B,                                 // 0x740 pcmpeqb
        MOp_M16B,                                // 0x741 pcmpeqb
        None,                                    // 0x742
        None,                                    // 0x743
        MOp_M8B,                                 // 0x750 pcmpeqw
        MOp_M16B,                                // 0x751 pcmpeqw
        None,                                    // 0x752
        None,                                    // 0x753
        MOp_M8B,                                 // 0x760 pcmpeqd
        MOp_M16B,                                // 0x761 pcmpeqd
        None,                                    // 0x762
        None,                                    // 0x763
        None,                                    // 0x770 emms
        None,                                    // 0x771
        None,                                    // 0x772
        None,                                    // 0x773
        M1st_M8B,                                // 0x780 vmread
        None,                                    // 0x781
        None,                                    // 0x782
        None,                                    // 0x783
        MOp_M8B,                                 // 0x790 vmwrite
        None,                                    // 0x791
        None,                                    // 0x792
        None,                                    // 0x793
        None,                                    // 0x7a0
        None,                                    // 0x7a1
        None,                                    // 0x7a2
        None,                                    // 0x7a3
        None,                                    // 0x7b0
        None,                                    // 0x7b1
        None,                                    // 0x7b2
        None,                                    // 0x7b3
        None,                                    // 0x7c0
        MOp_M16B,                                // 0x7c1 haddpd
        None,                                    // 0x7c2
        MOp_M16B,                                // 0x7c3 haddps
        None,                                    // 0x7d0
        MOp_M16B,                                // 0x7d1 hsubpd
        None,                                    // 0x7d2
        MOp_M16B,                                // 0x7d3 hsubps
        M1st_W_M8B_or_M4B,                       // 0x7e0 movd,movq
        M1st_M4B,                                // 0x7e1 movd
        MOp_M8B,                                 // 0x7e2 movq
        None,                                    // 0x7e3
        M1st_M8B,                                // 0x7f0 movq
        M1st_M16B,                               // 0x7f1 movdqa
        M1st_M16B,                               // 0x7f2 movdqu
        None,                                    // 0x7f3
        I4B,                                     // 0x800 jo
        I2B,                                     // 0x801 jo
        I4B,                                     // 0x802 jo
        I4B,                                     // 0x803 jo
        I4B,                                     // 0x810 jno
        I2B,                                     // 0x811 jno
        I4B,                                     // 0x812 jno
        I4B,                                     // 0x813 jno
        I4B,                                     // 0x820 jb
        I2B,                                     // 0x821 jb
        I4B,                                     // 0x822 jb
        I4B,                                     // 0x823 jb
        I4B,                                     // 0x830 jae
        I2B,                                     // 0x831 jae
        I4B,                                     // 0x832 jae
        I4B,                                     // 0x833 jae
        I4B,                                     // 0x840 je
        I2B,                                     // 0x841 je
        I4B,                                     // 0x842 je
        I4B,                                     // 0x843 je
        I4B,                                     // 0x850 jne
        I2B,                                     // 0x851 jne
        I4B,                                     // 0x852 jne
        I4B,                                     // 0x853 jne
        I4B,                                     // 0x860 jbe
        I2B,                                     // 0x861 jbe
        I4B,                                     // 0x862 jbe
        I4B,                                     // 0x863 jbe
        I4B,                                     // 0x870 ja
        I2B,                                     // 0x871 ja
        I4B,                                     // 0x872 ja
        I4B,                                     // 0x873 ja
        I4B,                                     // 0x880 js
        I2B,                                     // 0x881 js
        I4B,                                     // 0x882 js
        I4B,                                     // 0x883 js
        I4B,                                     // 0x890 jns
        I2B,                                     // 0x891 jns
        I4B,                                     // 0x892 jns
        I4B,                                     // 0x893 jns
        I4B,                                     // 0x8a0 jp
        I2B,                                     // 0x8a1 jp
        I4B,                                     // 0x8a2 jp
        I4B,                                     // 0x8a3 jp
        I4B,                                     // 0x8b0 jnp
        I2B,                                     // 0x8b1 jnp
        I4B,                                     // 0x8b2 jnp
        I4B,                                     // 0x8b3 jnp
        I4B,                                     // 0x8c0 jl
        I2B,                                     // 0x8c1 jl
        I4B,                                     // 0x8c2 jl
        I4B,                                     // 0x8c3 jl
        I4B,                                     // 0x8d0 jge
        I2B,                                     // 0x8d1 jge
        I4B,                                     // 0x8d2 jge
        I4B,                                     // 0x8d3 jge
        I4B,                                     // 0x8e0 jle
        I2B,                                     // 0x8e1 jle
        I4B,                                     // 0x8e2 jle
        I4B,                                     // 0x8e3 jle
        I4B,                                     // 0x8f0 jg
        I2B,                                     // 0x8f1 jg
        I4B,                                     // 0x8f2 jg
        I4B,                                     // 0x8f3 jg
        MOnly_M1B,                               // 0x900 seto
        MOnly_M1B,                               // 0x901 seto
        MOnly_M1B,                               // 0x902 seto
        MOnly_M1B,                               // 0x903 seto
        MOnly_M1B,                               // 0x910 setno
        MOnly_M1B,                               // 0x911 setno
        MOnly_M1B,                               // 0x912 setno
        MOnly_M1B,                               // 0x913 setno
        MOnly_M1B,                               // 0x920 setb
        MOnly_M1B,                               // 0x921 setb
        MOnly_M1B,                               // 0x922 setb
        MOnly_M1B,                               // 0x923 setb
        MOnly_M1B,                               // 0x930 setae
        MOnly_M1B,                               // 0x931 setae
        MOnly_M1B,                               // 0x932 setae
        MOnly_M1B,                               // 0x933 setae
        MOnly_M1B,                               // 0x940 sete
        MOnly_M1B,                               // 0x941 sete
        MOnly_M1B,                               // 0x942 sete
        MOnly_M1B,                               // 0x943 sete
        MOnly_M1B,                               // 0x950 setne
        MOnly_M1B,                               // 0x951 setne
        MOnly_M1B,                               // 0x952 setne
        MOnly_M1B,                               // 0x953 setne
        MOnly_M1B,                               // 0x960 setbe
        MOnly_M1B,                               // 0x961 setbe
        MOnly_M1B,                               // 0x962 setbe
        MOnly_M1B,                               // 0x963 setbe
        MOnly_M1B,                               // 0x970 seta
        MOnly_M1B,                               // 0x971 seta
        MOnly_M1B,                               // 0x972 seta
        MOnly_M1B,                               // 0x973 seta
        MOnly_M1B,                               // 0x980 sets
        MOnly_M1B,                               // 0x981 sets
        MOnly_M1B,                               // 0x982 sets
        MOnly_M1B,                               // 0x983 sets
        MOnly_M1B,                               // 0x990 setns
        MOnly_M1B,                               // 0x991 setns
        MOnly_M1B,                               // 0x992 setns
        MOnly_M1B,                               // 0x993 setns
        MOnly_M1B,                               // 0x9a0 setp
        MOnly_M1B,                               // 0x9a1 setp
        MOnly_M1B,                               // 0x9a2 setp
        MOnly_M1B,                               // 0x9a3 setp
        MOnly_M1B,                               // 0x9b0 setnp
        MOnly_M1B,                               // 0x9b1 setnp
        MOnly_M1B,                               // 0x9b2 setnp
        MOnly_M1B,                               // 0x9b3 setnp
        MOnly_M1B,                               // 0x9c0 setl
        MOnly_M1B,                               // 0x9c1 setl
        MOnly_M1B,                               // 0x9c2 setl
        MOnly_M1B,                               // 0x9c3 setl
        MOnly_M1B,                               // 0x9d0 setge
        MOnly_M1B,                               // 0x9d1 setge
        MOnly_M1B,                               // 0x9d2 setge
        MOnly_M1B,                               // 0x9d3 setge
        MOnly_M1B,                               // 0x9e0 setle
        MOnly_M1B,                               // 0x9e1 setle
        MOnly_M1B,                               // 0x9e2 setle
        MOnly_M1B,                               // 0x9e3 setle
        MOnly_M1B,                               // 0x9f0 setg
        MOnly_M1B,                               // 0x9f1 setg
        MOnly_M1B,                               // 0x9f2 setg
        MOnly_M1B,                               // 0x9f3 setg
        None,                                    // 0xa00 push
        None,                                    // 0xa01 pushw
        None,                                    // 0xa02 push
        None,                                    // 0xa03 push
        None,                                    // 0xa10 pop
        None,                                    // 0xa11 popw
        None,                                    // 0xa12 pop
        None,                                    // 0xa13 pop
        None,                                    // 0xa20 cpuid
        None,                                    // 0xa21 cpuid
        None,                                    // 0xa22 cpuid
        None,                                    // 0xa23 cpuid
        M1st_W_M8B_or_M4B,                       // 0xa30 bt
        M1st_M2B,                                // 0xa31 bt
        M1st_M4B,                                // 0xa32 bt
        M1st_M4B,                                // 0xa33 bt
        M1st_I1B_W_M8B_or_M4B,                   // 0xa40 shld
        M1st_M2B_I1B,                            // 0xa41 shld
        M1st_M4B_I1B,                            // 0xa42 shld
        M1st_M4B_I1B,                            // 0xa43 shld
        M1st_W_M8B_or_M4B,                       // 0xa50 shld
        M1st_M2B,                                // 0xa51 shld
        M1st_M4B,                                // 0xa52 shld
        M1st_M4B,                                // 0xa53 shld
        None,                                    // 0xa60
        None,                                    // 0xa61
        None,                                    // 0xa62
        None,                                    // 0xa63
        None,                                    // 0xa70
        None,                                    // 0xa71
        None,                                    // 0xa72
        None,                                    // 0xa73
        None,                                    // 0xa80 push
        None,                                    // 0xa81 pushw
        None,                                    // 0xa82 push
        None,                                    // 0xa83 push
        None,                                    // 0xa90 pop
        None,                                    // 0xa91 popw
        None,                                    // 0xa92 pop
        None,                                    // 0xa93 pop
        None,                                    // 0xaa0 rsm
        None,                                    // 0xaa1 rsm
        None,                                    // 0xaa2 rsm
        None,                                    // 0xaa3 rsm
        M1st_W_M8B_or_M4B,                       // 0xab0 bts
        M1st_M2B,                                // 0xab1 bts
        M1st_M4B,                                // 0xab2 bts
        M1st_M4B,                                // 0xab3 bts
        M1st_I1B_W_M8B_or_M4B,                   // 0xac0 shrd
        M1st_M2B_I1B,                            // 0xac1 shrd
        M1st_M4B_I1B,                            // 0xac2 shrd
        M1st_M4B_I1B,                            // 0xac3 shrd
        M1st_W_M8B_or_M4B,                       // 0xad0 shrd
        M1st_M2B,                                // 0xad1 shrd
        M1st_M4B,                                // 0xad2 shrd
        M1st_M4B,                                // 0xad3 shrd
        InstrForm(int(Extension)|0x0b),          // 0xae0
        InstrForm(int(Extension)|0x0c),          // 0xae1
        InstrForm(int(Extension)|0x0d),          // 0xae2
        InstrForm(int(Extension)|0x0e),          // 0xae3
        MOp_W_M8B_or_M4B,                        // 0xaf0 imul
        MOp_M2B,                                 // 0xaf1 imul
        MOp_M4B,                                 // 0xaf2 imul
        MOp_M4B,                                 // 0xaf3 imul
        M1st_M1B,                                // 0xb00 cmpxchg
        M1st_M1B,                                // 0xb01 cmpxchg
        M1st_M1B,                                // 0xb02 cmpxchg
        M1st_M1B,                                // 0xb03 cmpxchg
        M1st_W_M8B_or_M4B,                       // 0xb10 cmpxchg
        M1st_M2B,                                // 0xb11 cmpxchg
        M1st_M4B,                                // 0xb12 cmpxchg
        M1st_M4B,                                // 0xb13 cmpxchg
        MOp_M6B,                                 // 0xb20 lss
        MOp_M4B,                                 // 0xb21 lss
        MOp_M6B,                                 // 0xb22 lss
        MOp_M6B,                                 // 0xb23 lss
        M1st_W_M8B_or_M4B,                       // 0xb30 btr
        M1st_M2B,                                // 0xb31 btr
        M1st_M4B,                                // 0xb32 btr
        M1st_M4B,                                // 0xb33 btr
        MOp_M6B,                                 // 0xb40 lfs
        MOp_M4B,                                 // 0xb41 lfs
        MOp_M6B,                                 // 0xb42 lfs
        MOp_M6B,                                 // 0xb43 lfs
        MOp_M6B,                                 // 0xb50 lgs
        MOp_M4B,                                 // 0xb51 lgs
        MOp_M6B,                                 // 0xb52 lgs
        MOp_M6B,                                 // 0xb53 lgs
        MOp_M1B,                                 // 0xb60 movzx
        MOp_M1B,                                 // 0xb61 movzx
        MOp_M1B,                                 // 0xb62 movzx
        MOp_M1B,                                 // 0xb63 movzx
        MOp_M2B,                                 // 0xb70 movzx
        MOp_M2B,                                 // 0xb71 movzx
        MOp_M2B,                                 // 0xb72 movzx
        MOp_M2B,                                 // 0xb73 movzx
        None,                                    // 0xb80
        None,                                    // 0xb81
        MOp_M4B,                                 // 0xb82 popcnt
        None,                                    // 0xb83
        None,                                    // 0xb90 ud1
        None,                                    // 0xb91 ud1
        None,                                    // 0xb92 ud1
        None,                                    // 0xb93 ud1
        M1st_I1B_W_M8B_or_M4B,                   // 0xba0 bt,btc,btr,bts
        M1st_M2B_I1B,                            // 0xba1 bt,btc,btr,bts
        M1st_M4B_I1B,                            // 0xba2 bt,btc,btr,bts
        M1st_M4B_I1B,                            // 0xba3 bt,btc,btr,bts
        M1st_W_M8B_or_M4B,                       // 0xbb0 btc
        M1st_M2B,                                // 0xbb1 btc
        M1st_M4B,                                // 0xbb2 btc
        M1st_M4B,                                // 0xbb3 btc
        MOp_W_M8B_or_M4B,                        // 0xbc0 bsf
        MOp_M2B,                                 // 0xbc1 bsf
        MOp_M4B,                                 // 0xbc2 tzcnt
        None,                                    // 0xbc3
        MOp_W_M8B_or_M4B,                        // 0xbd0 bsr
        MOp_M2B,                                 // 0xbd1 bsr
        MOp_M4B,                                 // 0xbd2 lzcnt
        None,                                    // 0xbd3
        MOp_M1B,                                 // 0xbe0 movsx
        MOp_M1B,                                 // 0xbe1 movsx
        MOp_M1B,                                 // 0xbe2 movsx
        MOp_M1B,                                 // 0xbe3 movsx
        MOp_M2B,                                 // 0xbf0 movsx
        MOp_M2B,                                 // 0xbf1 movsx
        MOp_M2B,                                 // 0xbf2 movsx
        MOp_M2B,                                 // 0xbf3 movsx
        M1st_M1B,                                // 0xc00 xadd
        M1st_M1B,                                // 0xc01 xadd
        M1st_M1B,                                // 0xc02 xadd
        M1st_M1B,                                // 0xc03 xadd
        M1st_W_M8B_or_M4B,                       // 0xc10 xadd
        M1st_M2B,                                // 0xc11 xadd
        M1st_M4B,                                // 0xc12 xadd
        M1st_M4B,                                // 0xc13 xadd
        MOp_M16B_I1B,                            // 0xc20 cmpps
        MOp_M16B_I1B,                            // 0xc21 cmppd
        MOp_M4B_I1B,                             // 0xc22 cmpss
        MOp_M8B_I1B,                             // 0xc23 cmpsd
        M1st_W_M8B_or_M4B,                       // 0xc30 movnti
        None,                                    // 0xc31
        None,                                    // 0xc32
        None,                                    // 0xc33
        MOp_M2B_I1B,                             // 0xc40 pinsrw
        MOp_M2B_I1B,                             // 0xc41 pinsrw
        None,                                    // 0xc42
        None,                                    // 0xc43
        None,                                    // 0xc50
        None,                                    // 0xc51
        None,                                    // 0xc52
        None,                                    // 0xc53
        MOp_M16B_I1B,                            // 0xc60 shufps
        MOp_M16B_I1B,                            // 0xc61 shufpd
        None,                                    // 0xc62
        None,                                    // 0xc63
        InstrForm(int(Extension)|0x0f),          // 0xc70
        InstrForm(int(Extension)|0x10),          // 0xc71
        InstrForm(int(Extension)|0x11),          // 0xc72
        InstrForm(int(Extension)|0x12),          // 0xc73
        None,                                    // 0xc80 bswap
        None,                                    // 0xc81 bswap
        None,                                    // 0xc82 bswap
        None,                                    // 0xc83 bswap
        None,                                    // 0xc90 bswap
        None,                                    // 0xc91 bswap
        None,                                    // 0xc92 bswap
        None,                                    // 0xc93 bswap
        None,                                    // 0xca0 bswap
        None,                                    // 0xca1 bswap
        None,                                    // 0xca2 bswap
        None,                                    // 0xca3 bswap
        None,                                    // 0xcb0 bswap
        None,                                    // 0xcb1 bswap
        None,                                    // 0xcb2 bswap
        None,                                    // 0xcb3 bswap
        None,                                    // 0xcc0 bswap
        None,                                    // 0xcc1 bswap
        None,                                    // 0xcc2 bswap
        None,                                    // 0xcc3 bswap
        None,                                    // 0xcd0 bswap
        None,                                    // 0xcd1 bswap
        None,                                    // 0xcd2 bswap
        None,                                    // 0xcd3 bswap
        None,                                    // 0xce0 bswap
        None,                                    // 0xce1 bswap
        None,                                    // 0xce2 bswap
        None,                                    // 0xce3 bswap
        None,                                    // 0xcf0 bswap
        None,                                    // 0xcf1 bswap
        None,                                    // 0xcf2 bswap
        None,                                    // 0xcf3 bswap
        None,                                    // 0xd00
        MOp_M16B,                                // 0xd01 addsubpd
        None,                                    // 0xd02
        MOp_M16B,                                // 0xd03 addsubps
        MOp_M8B,                                 // 0xd10 psrlw
        MOp_M16B,                                // 0xd11 psrlw
        None,                                    // 0xd12
        None,                                    // 0xd13
        MOp_M8B,                                 // 0xd20 psrld
        MOp_M16B,                                // 0xd21 psrld
        None,                                    // 0xd22
        None,                                    // 0xd23
        MOp_M8B,                                 // 0xd30 psrlq
        MOp_M16B,                                // 0xd31 psrlq
        None,                                    // 0xd32
        None,                                    // 0xd33
        MOp_M8B,                                 // 0xd40 paddq
        MOp_M16B,                                // 0xd41 paddq
        None,                                    // 0xd42
        None,                                    // 0xd43
        MOp_M8B,                                 // 0xd50 pmullw
        MOp_M16B,                                // 0xd51 pmullw
        None,                                    // 0xd52
        None,                                    // 0xd53
        None,                                    // 0xd60
        M1st_M8B,                                // 0xd61 movq
        None,                                    // 0xd62
        None,                                    // 0xd63
        None,                                    // 0xd70
        None,                                    // 0xd71
        None,                                    // 0xd72
        None,                                    // 0xd73
        MOp_M8B,                                 // 0xd80 psubusb
        MOp_M16B,                                // 0xd81 psubusb
        None,                                    // 0xd82
        None,                                    // 0xd83
        MOp_M8B,                                 // 0xd90 psubusw
        MOp_M16B,                                // 0xd91 psubusw
        None,                                    // 0xd92
        None,                                    // 0xd93
        MOp_M8B,                                 // 0xda0 pminub
        MOp_M16B,                                // 0xda1 pminub
        None,                                    // 0xda2
        None,                                    // 0xda3
        MOp_M8B,                                 // 0xdb0 pand
        MOp_M16B,                                // 0xdb1 pand
        None,                                    // 0xdb2
        None,                                    // 0xdb3
        MOp_M8B,                                 // 0xdc0 paddusb
        MOp_M16B,                                // 0xdc1 paddusb
        None,                                    // 0xdc2
        None,                                    // 0xdc3
        MOp_M8B,                                 // 0xdd0 paddusw
        MOp_M16B,                                // 0xdd1 paddusw
        None,                                    // 0xdd2
        None,                                    // 0xdd3
        MOp_M8B,                                 // 0xde0 pmaxub
        MOp_M16B,                                // 0xde1 pmaxub
        None,                                    // 0xde2
        None,                                    // 0xde3
        MOp_M8B,                                 // 0xdf0 pandn
        MOp_M16B,                                // 0xdf1 pandn
        None,                                    // 0xdf2
        None,                                    // 0xdf3
        MOp_M8B,                                 // 0xe00 pavgb
        MOp_M16B,                                // 0xe01 pavgb
        None,                                    // 0xe02
        None,                                    // 0xe03
        MOp_M8B,                                 // 0xe10 psraw
        MOp_M16B,                                // 0xe11 psraw
        None,                                    // 0xe12
        None,                                    // 0xe13
        MOp_M8B,                                 // 0xe20 psrad
        MOp_M16B,                                // 0xe21 psrad
        None,                                    // 0xe22
        None,                                    // 0xe23
        MOp_M8B,                                 // 0xe30 pavgw
        MOp_M16B,                                // 0xe31 pavgw
        None,                                    // 0xe32
        None,                                    // 0xe33
        MOp_M8B,                                 // 0xe40 pmulhuw
        MOp_M16B,                                // 0xe41 pmulhuw
        None,                                    // 0xe42
        None,                                    // 0xe43
        MOp_M8B,                                 // 0xe50 pmulhw
        MOp_M16B,                                // 0xe51 pmulhw
        None,                                    // 0xe52
        None,                                    // 0xe53
        None,                                    // 0xe60
        MOp_M16B,                                // 0xe61 cvttpd2dq
        MOp_M8B,                                 // 0xe62 cvtdq2pd
        MOp_M16B,                                // 0xe63 cvtpd2dq
        M1st_M8B,                                // 0xe70 movntq
        M1st_M16B,                               // 0xe71 movntdq
        None,                                    // 0xe72
        None,                                    // 0xe73
        MOp_M8B,                                 // 0xe80 psubsb
        MOp_M16B,                                // 0xe81 psubsb
        None,                                    // 0xe82
        None,                                    // 0xe83
        MOp_M8B,                                 // 0xe90 psubsw
        MOp_M16B,                                // 0xe91 psubsw
        None,                                    // 0xe92
        None,                                    // 0xe93
        MOp_M8B,                                 // 0xea0 pminsw
        MOp_M16B,                                // 0xea1 pminsw
        None,                                    // 0xea2
        None,                                    // 0xea3
        MOp_M8B,                                 // 0xeb0 por
        MOp_M16B,                                // 0xeb1 por
        None,                                    // 0xeb2
        None,                                    // 0xeb3
        MOp_M8B,                                 // 0xec0 paddsb
        MOp_M16B,                                // 0xec1 paddsb
        None,                                    // 0xec2
        None,                                    // 0xec3
        MOp_M8B,                                 // 0xed0 paddsw
        MOp_M16B,                                // 0xed1 paddsw
        None,                                    // 0xed2
        None,                                    // 0xed3
        MOp_M8B,                                 // 0xee0 pmaxsw
        MOp_M16B,                                // 0xee1 pmaxsw
        None,                                    // 0xee2
        None,                                    // 0xee3
        MOp_M8B,                                 // 0xef0 pxor
        MOp_M16B,                                // 0xef1 pxor
        None,                                    // 0xef2
        None,                                    // 0xef3
        None,                                    // 0xf00
        None,                                    // 0xf01
        None,                                    // 0xf02
        MOp_M16B,                                // 0xf03 lddqu
        MOp_M8B,                                 // 0xf10 psllw
        MOp_M16B,                                // 0xf11 psllw
        None,                                    // 0xf12
        None,                                    // 0xf13
        MOp_M8B,                                 // 0xf20 pslld
        MOp_M16B,                                // 0xf21 pslld
        None,                                    // 0xf22
        None,                                    // 0xf23
        MOp_M8B,                                 // 0xf30 psllq
        MOp_M16B,                                // 0xf31 psllq
        None,                                    // 0xf32
        None,                                    // 0xf33
        MOp_M8B,                                 // 0xf40 pmuludq
        MOp_M16B,                                // 0xf41 pmuludq
        None,                                    // 0xf42
        None,                                    // 0xf43
        MOp_M8B,                                 // 0xf50 pmaddwd
        MOp_M16B,                                // 0xf51 pmaddwd
        None,                                    // 0xf52
        None,                                    // 0xf53
        MOp_M8B,                                 // 0xf60 psadbw
        MOp_M16B,                                // 0xf61 psadbw
        None,                                    // 0xf62
        None,                                    // 0xf63
        None,                                    // 0xf70
        None,                                    // 0xf71
        None,                                    // 0xf72
        None,                                    // 0xf73
        MOp_M8B,                                 // 0xf80 psubb
        MOp_M16B,                                // 0xf81 psubb
        None,                                    // 0xf82
        None,                                    // 0xf83
        MOp_M8B,                                 // 0xf90 psubw
        MOp_M16B,                                // 0xf91 psubw
        None,                                    // 0xf92
        None,                                    // 0xf93
        MOp_M8B,                                 // 0xfa0 psubd
        MOp_M16B,                                // 0xfa1 psubd
        None,                                    // 0xfa2
        None,                                    // 0xfa3
        MOp_M8B,                                 // 0xfb0 psubq
        MOp_M16B,                                // 0xfb1 psubq
        None,                                    // 0xfb2
        None,                                    // 0xfb3
        MOp_M8B,                                 // 0xfc0 paddb
        MOp_M16B,                                // 0xfc1 paddb
        None,                                    // 0xfc2
        None,                                    // 0xfc3
        MOp_M8B,                                 // 0xfd0 paddw
        MOp_M16B,                                // 0xfd1 paddw
        None,                                    // 0xfd2
        None,                                    // 0xfd3
        MOp_M8B,                                 // 0xfe0 paddd
        MOp_M16B,                                // 0xfe1 paddd
        None,                                    // 0xfe2
        None,                                    // 0xfe3
        None,                                    // 0xff0
        None,                                    // 0xff1
        None,                                    // 0xff2
        None,                                    // 0xff3
    };

    static const InstrForm instrFormF38[1024]
    {
        MOp_M8B,                                 // 0x000 pshufb
        MOp_M16B,                                // 0x001 pshufb
        None,                                    // 0x002
        None,                                    // 0x003
        MOp_M8B,                                 // 0x010 phaddw
        MOp_M16B,                                // 0x011 phaddw
        None,                                    // 0x012
        None,                                    // 0x013
        MOp_M8B,                                 // 0x020 phaddd
        MOp_M16B,                                // 0x021 phaddd
        None,                                    // 0x022
        None,                                    // 0x023
        MOp_M8B,                                 // 0x030 phaddsw
        MOp_M16B,                                // 0x031 phaddsw
        None,                                    // 0x032
        None,                                    // 0x033
        MOp_M8B,                                 // 0x040 pmaddubsw
        MOp_M16B,                                // 0x041 pmaddubsw
        None,                                    // 0x042
        None,                                    // 0x043
        MOp_M8B,                                 // 0x050 phsubw
        MOp_M16B,                                // 0x051 phsubw
        None,                                    // 0x052
        None,                                    // 0x053
        MOp_M8B,                                 // 0x060 phsubd
        MOp_M16B,                                // 0x061 phsubd
        None,                                    // 0x062
        None,                                    // 0x063
        MOp_M8B,                                 // 0x070 phsubsw
        MOp_M16B,                                // 0x071 phsubsw
        None,                                    // 0x072
        None,                                    // 0x073
        MOp_M8B,                                 // 0x080 psignb
        MOp_M16B,                                // 0x081 psignb
        None,                                    // 0x082
        None,                                    // 0x083
        MOp_M8B,                                 // 0x090 psignw
        MOp_M16B,                                // 0x091 psignw
        None,                                    // 0x092
        None,                                    // 0x093
        MOp_M8B,                                 // 0x0a0 psignd
        MOp_M16B,                                // 0x0a1 psignd
        None,                                    // 0x0a2
        None,                                    // 0x0a3
        MOp_M8B,                                 // 0x0b0 pmulhrsw
        MOp_M16B,                                // 0x0b1 pmulhrsw
        None,                                    // 0x0b2
        None,                                    // 0x0b3
        None,                                    // 0x0c0
        None,                                    // 0x0c1
        None,                                    // 0x0c2
        None,                                    // 0x0c3
        None,                                    // 0x0d0
        None,                                    // 0x0d1
        None,                                    // 0x0d2
        None,                                    // 0x0d3
        None,                                    // 0x0e0
        None,                                    // 0x0e1
        None,                                    // 0x0e2
        None,                                    // 0x0e3
        None,                                    // 0x0f0
        None,                                    // 0x0f1
        None,                                    // 0x0f2
        None,                                    // 0x0f3
        None,                                    // 0x100
        MOp_M16B,                                // 0x101 pblendvb
        None,                                    // 0x102
        None,                                    // 0x103
        None,                                    // 0x110
        None,                                    // 0x111
        None,                                    // 0x112
        None,                                    // 0x113
        None,                                    // 0x120
        None,                                    // 0x121
        None,                                    // 0x122
        None,                                    // 0x123
        None,                                    // 0x130
        None,                                    // 0x131
        None,                                    // 0x132
        None,                                    // 0x133
        None,                                    // 0x140
        MOp_M16B,                                // 0x141 blendvps
        None,                                    // 0x142
        None,                                    // 0x143
        None,                                    // 0x150
        MOp_M16B,                                // 0x151 blendvpd
        None,                                    // 0x152
        None,                                    // 0x153
        None,                                    // 0x160
        None,                                    // 0x161
        None,                                    // 0x162
        None,                                    // 0x163
        None,                                    // 0x170
        MOp_M16B,                                // 0x171 ptest
        None,                                    // 0x172
        None,                                    // 0x173
        None,                                    // 0x180
        None,                                    // 0x181
        None,                                    // 0x182
        None,                                    // 0x183
        None,                                    // 0x190
        None,                                    // 0x191
        None,                                    // 0x192
        None,                                    // 0x193
        None,                                    // 0x1a0
        None,                                    // 0x1a1
        None,                                    // 0x1a2
        None,                                    // 0x1a3
        None,                                    // 0x1b0
        None,                                    // 0x1b1
        None,                                    // 0x1b2
        None,                                    // 0x1b3
        MOp_M8B,                                 // 0x1c0 pabsb
        MOp_M16B,                                // 0x1c1 pabsb
        None,                                    // 0x1c2
        None,                                    // 0x1c3
        MOp_M8B,                                 // 0x1d0 pabsw
        MOp_M16B,                                // 0x1d1 pabsw
        None,                                    // 0x1d2
        None,                                    // 0x1d3
        MOp_M8B,                                 // 0x1e0 pabsd
        MOp_M16B,                                // 0x1e1 pabsd
        None,                                    // 0x1e2
        None,                                    // 0x1e3
        None,                                    // 0x1f0
        None,                                    // 0x1f1
        None,                                    // 0x1f2
        None,                                    // 0x1f3
        None,                                    // 0x200
        MOp_M8B,                                 // 0x201 pmovsxbw
        None,                                    // 0x202
        None,                                    // 0x203
        None,                                    // 0x210
        MOp_M4B,                                 // 0x211 pmovsxbd
        None,                                    // 0x212
        None,                                    // 0x213
        None,                                    // 0x220
        MOp_M2B,                                 // 0x221 pmovsxbq
        None,                                    // 0x222
        None,                                    // 0x223
        None,                                    // 0x230
        MOp_M8B,                                 // 0x231 pmovsxwd
        None,                                    // 0x232
        None,                                    // 0x233
        None,                                    // 0x240
        MOp_M4B,                                 // 0x241 pmovsxwq
        None,                                    // 0x242
        None,                                    // 0x243
        None,                                    // 0x250
        MOp_M8B,                                 // 0x251 pmovsxdq
        None,                                    // 0x252
        None,                                    // 0x253
        None,                                    // 0x260
        None,                                    // 0x261
        None,                                    // 0x262
        None,                                    // 0x263
        None,                                    // 0x270
        None,                                    // 0x271
        None,                                    // 0x272
        None,                                    // 0x273
        None,                                    // 0x280
        MOp_M16B,                                // 0x281 pmuldq
        None,                                    // 0x282
        None,                                    // 0x283
        None,                                    // 0x290
        MOp_M16B,                                // 0x291 pcmpeqq
        None,                                    // 0x292
        None,                                    // 0x293
        None,                                    // 0x2a0
        MOp_M16B,                                // 0x2a1 movntdqa
        None,                                    // 0x2a2
        None,                                    // 0x2a3
        None,                                    // 0x2b0
        MOp_M16B,                                // 0x2b1 packusdw
        None,                                    // 0x2b2
        None,                                    // 0x2b3
        None,                                    // 0x2c0
        None,                                    // 0x2c1
        None,                                    // 0x2c2
        None,                                    // 0x2c3
        None,                                    // 0x2d0
        None,                                    // 0x2d1
        None,                                    // 0x2d2
        None,                                    // 0x2d3
        None,                                    // 0x2e0
        None,                                    // 0x2e1
        None,                                    // 0x2e2
        None,                                    // 0x2e3
        None,                                    // 0x2f0
        None,                                    // 0x2f1
        None,                                    // 0x2f2
        None,                                    // 0x2f3
        None,                                    // 0x300
        MOp_M8B,                                 // 0x301 pmovzxbw
        None,                                    // 0x302
        None,                                    // 0x303
        None,                                    // 0x310
        MOp_M4B,                                 // 0x311 pmovzxbd
        None,                                    // 0x312
        None,                                    // 0x313
        None,                                    // 0x320
        MOp_M2B,                                 // 0x321 pmovzxbq
        None,                                    // 0x322
        None,                                    // 0x323
        None,                                    // 0x330
        MOp_M8B,                                 // 0x331 pmovzxwd
        None,                                    // 0x332
        None,                                    // 0x333
        None,                                    // 0x340
        MOp_M4B,                                 // 0x341 pmovzxwq
        None,                                    // 0x342
        None,                                    // 0x343
        None,                                    // 0x350
        MOp_M8B,                                 // 0x351 pmovzxdq
        None,                                    // 0x352
        None,                                    // 0x353
        None,                                    // 0x360
        None,                                    // 0x361
        None,                                    // 0x362
        None,                                    // 0x363
        None,                                    // 0x370
        MOp_M16B,                                // 0x371 pcmpgtq
        None,                                    // 0x372
        None,                                    // 0x373
        None,                                    // 0x380
        MOp_M16B,                                // 0x381 pminsb
        None,                                    // 0x382
        None,                                    // 0x383
        None,                                    // 0x390
        MOp_M16B,                                // 0x391 pminsd
        None,                                    // 0x392
        None,                                    // 0x393
        None,                                    // 0x3a0
        MOp_M16B,                                // 0x3a1 pminuw
        None,                                    // 0x3a2
        None,                                    // 0x3a3
        None,                                    // 0x3b0
        MOp_M16B,                                // 0x3b1 pminud
        None,                                    // 0x3b2
        None,                                    // 0x3b3
        None,                                    // 0x3c0
        MOp_M16B,                                // 0x3c1 pmaxsb
        None,                                    // 0x3c2
        None,                                    // 0x3c3
        None,                                    // 0x3d0
        MOp_M16B,                                // 0x3d1 pmaxsd
        None,                                    // 0x3d2
        None,                                    // 0x3d3
        None,                                    // 0x3e0
        MOp_M16B,                                // 0x3e1 pmaxuw
        None,                                    // 0x3e2
        None,                                    // 0x3e3
        None,                                    // 0x3f0
        MOp_M16B,                                // 0x3f1 pmaxud
        None,                                    // 0x3f2
        None,                                    // 0x3f3
        None,                                    // 0x400
        MOp_M16B,                                // 0x401 pmulld
        None,                                    // 0x402
        None,                                    // 0x403
        None,                                    // 0x410
        MOp_M16B,                                // 0x411 phminposuw
        None,                                    // 0x412
        None,                                    // 0x413
        None,                                    // 0x420
        None,                                    // 0x421
        None,                                    // 0x422
        None,                                    // 0x423
        None,                                    // 0x430
        None,                                    // 0x431
        None,                                    // 0x432
        None,                                    // 0x433
        None,                                    // 0x440
        None,                                    // 0x441
        None,                                    // 0x442
        None,                                    // 0x443
        None,                                    // 0x450
        None,                                    // 0x451
        None,                                    // 0x452
        None,                                    // 0x453
        None,                                    // 0x460
        None,                                    // 0x461
        None,                                    // 0x462
        None,                                    // 0x463
        None,                                    // 0x470
        None,                                    // 0x471
        None,                                    // 0x472
        None,                                    // 0x473
        None,                                    // 0x480
        None,                                    // 0x481
        None,                                    // 0x482
        None,                                    // 0x483
        None,                                    // 0x490
        None,                                    // 0x491
        None,                                    // 0x492
        None,                                    // 0x493
        None,                                    // 0x4a0
        None,                                    // 0x4a1
        None,                                    // 0x4a2
        None,                                    // 0x4a3
        None,                                    // 0x4b0
        None,                                    // 0x4b1
        None,                                    // 0x4b2
        None,                                    // 0x4b3
        None,                                    // 0x4c0
        None,                                    // 0x4c1
        None,                                    // 0x4c2
        None,                                    // 0x4c3
        None,                                    // 0x4d0
        None,                                    // 0x4d1
        None,                                    // 0x4d2
        None,                                    // 0x4d3
        None,                                    // 0x4e0
        None,                                    // 0x4e1
        None,                                    // 0x4e2
        None,                                    // 0x4e3
        None,                                    // 0x4f0
        None,                                    // 0x4f1
        None,                                    // 0x4f2
        None,                                    // 0x4f3
        None,                                    // 0x500
        None,                                    // 0x501
        None,                                    // 0x502
        None,                                    // 0x503
        None,                                    // 0x510
        None,                                    // 0x511
        None,                                    // 0x512
        None,                                    // 0x513
        None,                                    // 0x520
        None,                                    // 0x521
        None,                                    // 0x522
        None,                                    // 0x523
        None,                                    // 0x530
        None,                                    // 0x531
        None,                                    // 0x532
        None,                                    // 0x533
        None,                                    // 0x540
        None,                                    // 0x541
        None,                                    // 0x542
        None,                                    // 0x543
        None,                                    // 0x550
        None,                                    // 0x551
        None,                                    // 0x552
        None,                                    // 0x553
        None,                                    // 0x560
        None,                                    // 0x561
        None,                                    // 0x562
        None,                                    // 0x563
        None,                                    // 0x570
        None,                                    // 0x571
        None,                                    // 0x572
        None,                                    // 0x573
        None,                                    // 0x580
        None,                                    // 0x581
        None,                                    // 0x582
        None,                                    // 0x583
        None,                                    // 0x590
        None,                                    // 0x591
        None,                                    // 0x592
        None,                                    // 0x593
        None,                                    // 0x5a0
        None,                                    // 0x5a1
        None,                                    // 0x5a2
        None,                                    // 0x5a3
        None,                                    // 0x5b0
        None,                                    // 0x5b1
        None,                                    // 0x5b2
        None,                                    // 0x5b3
        None,                                    // 0x5c0
        None,                                    // 0x5c1
        None,                                    // 0x5c2
        None,                                    // 0x5c3
        None,                                    // 0x5d0
        None,                                    // 0x5d1
        None,                                    // 0x5d2
        None,                                    // 0x5d3
        None,                                    // 0x5e0
        None,                                    // 0x5e1
        None,                                    // 0x5e2
        None,                                    // 0x5e3
        None,                                    // 0x5f0
        None,                                    // 0x5f1
        None,                                    // 0x5f2
        None,                                    // 0x5f3
        None,                                    // 0x600
        None,                                    // 0x601
        None,                                    // 0x602
        None,                                    // 0x603
        None,                                    // 0x610
        None,                                    // 0x611
        None,                                    // 0x612
        None,                                    // 0x613
        None,                                    // 0x620
        None,                                    // 0x621
        None,                                    // 0x622
        None,                                    // 0x623
        None,                                    // 0x630
        None,                                    // 0x631
        None,                                    // 0x632
        None,                                    // 0x633
        None,                                    // 0x640
        None,                                    // 0x641
        None,                                    // 0x642
        None,                                    // 0x643
        None,                                    // 0x650
        None,                                    // 0x651
        None,                                    // 0x652
        None,                                    // 0x653
        None,                                    // 0x660
        None,                                    // 0x661
        None,                                    // 0x662
        None,                                    // 0x663
        None,                                    // 0x670
        None,                                    // 0x671
        None,                                    // 0x672
        None,                                    // 0x673
        None,                                    // 0x680
        None,                                    // 0x681
        None,                                    // 0x682
        None,                                    // 0x683
        None,                                    // 0x690
        None,                                    // 0x691
        None,                                    // 0x692
        None,                                    // 0x693
        None,                                    // 0x6a0
        None,                                    // 0x6a1
        None,                                    // 0x6a2
        None,                                    // 0x6a3
        None,                                    // 0x6b0
        None,                                    // 0x6b1
        None,                                    // 0x6b2
        None,                                    // 0x6b3
        None,                                    // 0x6c0
        None,                                    // 0x6c1
        None,                                    // 0x6c2
        None,                                    // 0x6c3
        None,                                    // 0x6d0
        None,                                    // 0x6d1
        None,                                    // 0x6d2
        None,                                    // 0x6d3
        None,                                    // 0x6e0
        None,                                    // 0x6e1
        None,                                    // 0x6e2
        None,                                    // 0x6e3
        None,                                    // 0x6f0
        None,                                    // 0x6f1
        None,                                    // 0x6f2
        None,                                    // 0x6f3
        None,                                    // 0x700
        None,                                    // 0x701
        None,                                    // 0x702
        None,                                    // 0x703
        None,                                    // 0x710
        None,                                    // 0x711
        None,                                    // 0x712
        None,                                    // 0x713
        None,                                    // 0x720
        None,                                    // 0x721
        None,                                    // 0x722
        None,                                    // 0x723
        None,                                    // 0x730
        None,                                    // 0x731
        None,                                    // 0x732
        None,                                    // 0x733
        None,                                    // 0x740
        None,                                    // 0x741
        None,                                    // 0x742
        None,                                    // 0x743
        None,                                    // 0x750
        None,                                    // 0x751
        None,                                    // 0x752
        None,                                    // 0x753
        None,                                    // 0x760
        None,                                    // 0x761
        None,                                    // 0x762
        None,                                    // 0x763
        None,                                    // 0x770
        None,                                    // 0x771
        None,                                    // 0x772
        None,                                    // 0x773
        None,                                    // 0x780
        None,                                    // 0x781
        None,                                    // 0x782
        None,                                    // 0x783
        None,                                    // 0x790
        None,                                    // 0x791
        None,                                    // 0x792
        None,                                    // 0x793
        None,                                    // 0x7a0
        None,                                    // 0x7a1
        None,                                    // 0x7a2
        None,                                    // 0x7a3
        None,                                    // 0x7b0
        None,                                    // 0x7b1
        None,                                    // 0x7b2
        None,                                    // 0x7b3
        None,                                    // 0x7c0
        None,                                    // 0x7c1
        None,                                    // 0x7c2
        None,                                    // 0x7c3
        None,                                    // 0x7d0
        None,                                    // 0x7d1
        None,                                    // 0x7d2
        None,                                    // 0x7d3
        None,                                    // 0x7e0
        None,                                    // 0x7e1
        None,                                    // 0x7e2
        None,                                    // 0x7e3
        None,                                    // 0x7f0
        None,                                    // 0x7f1
        None,                                    // 0x7f2
        None,                                    // 0x7f3
        None,                                    // 0x800
        MOp_M16B,                                // 0x801 invept
        None,                                    // 0x802
        None,                                    // 0x803
        None,                                    // 0x810
        MOp_M16B,                                // 0x811 invvpid
        None,                                    // 0x812
        None,                                    // 0x813
        None,                                    // 0x820
        MOp_MUnknown,                            // 0x821 invpcid
        None,                                    // 0x822
        None,                                    // 0x823
        None,                                    // 0x830
        None,                                    // 0x831
        None,                                    // 0x832
        None,                                    // 0x833
        None,                                    // 0x840
        None,                                    // 0x841
        None,                                    // 0x842
        None,                                    // 0x843
        None,                                    // 0x850
        None,                                    // 0x851
        None,                                    // 0x852
        None,                                    // 0x853
        None,                                    // 0x860
        None,                                    // 0x861
        None,                                    // 0x862
        None,                                    // 0x863
        None,                                    // 0x870
        None,                                    // 0x871
        None,                                    // 0x872
        None,                                    // 0x873
        None,                                    // 0x880
        None,                                    // 0x881
        None,                                    // 0x882
        None,                                    // 0x883
        None,                                    // 0x890
        None,                                    // 0x891
        None,                                    // 0x892
        None,                                    // 0x893
        None,                                    // 0x8a0
        None,                                    // 0x8a1
        None,                                    // 0x8a2
        None,                                    // 0x8a3
        None,                                    // 0x8b0
        None,                                    // 0x8b1
        None,                                    // 0x8b2
        None,                                    // 0x8b3
        None,                                    // 0x8c0
        None,                                    // 0x8c1
        None,                                    // 0x8c2
        None,                                    // 0x8c3
        None,                                    // 0x8d0
        None,                                    // 0x8d1
        None,                                    // 0x8d2
        None,                                    // 0x8d3
        None,                                    // 0x8e0
        None,                                    // 0x8e1
        None,                                    // 0x8e2
        None,                                    // 0x8e3
        None,                                    // 0x8f0
        None,                                    // 0x8f1
        None,                                    // 0x8f2
        None,                                    // 0x8f3
        None,                                    // 0x900
        None,                                    // 0x901
        None,                                    // 0x902
        None,                                    // 0x903
        None,                                    // 0x910
        None,                                    // 0x911
        None,                                    // 0x912
        None,                                    // 0x913
        None,                                    // 0x920
        None,                                    // 0x921
        None,                                    // 0x922
        None,                                    // 0x923
        None,                                    // 0x930
        None,                                    // 0x931
        None,                                    // 0x932
        None,                                    // 0x933
        None,                                    // 0x940
        None,                                    // 0x941
        None,                                    // 0x942
        None,                                    // 0x943
        None,                                    // 0x950
        None,                                    // 0x951
        None,                                    // 0x952
        None,                                    // 0x953
        None,                                    // 0x960
        None,                                    // 0x961
        None,                                    // 0x962
        None,                                    // 0x963
        None,                                    // 0x970
        None,                                    // 0x971
        None,                                    // 0x972
        None,                                    // 0x973
        None,                                    // 0x980
        None,                                    // 0x981
        None,                                    // 0x982
        None,                                    // 0x983
        None,                                    // 0x990
        None,                                    // 0x991
        None,                                    // 0x992
        None,                                    // 0x993
        None,                                    // 0x9a0
        None,                                    // 0x9a1
        None,                                    // 0x9a2
        None,                                    // 0x9a3
        None,                                    // 0x9b0
        None,                                    // 0x9b1
        None,                                    // 0x9b2
        None,                                    // 0x9b3
        None,                                    // 0x9c0
        None,                                    // 0x9c1
        None,                                    // 0x9c2
        None,                                    // 0x9c3
        None,                                    // 0x9d0
        None,                                    // 0x9d1
        None,                                    // 0x9d2
        None,                                    // 0x9d3
        None,                                    // 0x9e0
        None,                                    // 0x9e1
        None,                                    // 0x9e2
        None,                                    // 0x9e3
        None,                                    // 0x9f0
        None,                                    // 0x9f1
        None,                                    // 0x9f2
        None,                                    // 0x9f3
        None,                                    // 0xa00
        None,                                    // 0xa01
        None,                                    // 0xa02
        None,                                    // 0xa03
        None,                                    // 0xa10
        None,                                    // 0xa11
        None,                                    // 0xa12
        None,                                    // 0xa13
        None,                                    // 0xa20
        None,                                    // 0xa21
        None,                                    // 0xa22
        None,                                    // 0xa23
        None,                                    // 0xa30
        None,                                    // 0xa31
        None,                                    // 0xa32
        None,                                    // 0xa33
        None,                                    // 0xa40
        None,                                    // 0xa41
        None,                                    // 0xa42
        None,                                    // 0xa43
        None,                                    // 0xa50
        None,                                    // 0xa51
        None,                                    // 0xa52
        None,                                    // 0xa53
        None,                                    // 0xa60
        None,                                    // 0xa61
        None,                                    // 0xa62
        None,                                    // 0xa63
        None,                                    // 0xa70
        None,                                    // 0xa71
        None,                                    // 0xa72
        None,                                    // 0xa73
        None,                                    // 0xa80
        None,                                    // 0xa81
        None,                                    // 0xa82
        None,                                    // 0xa83
        None,                                    // 0xa90
        None,                                    // 0xa91
        None,                                    // 0xa92
        None,                                    // 0xa93
        None,                                    // 0xaa0
        None,                                    // 0xaa1
        None,                                    // 0xaa2
        None,                                    // 0xaa3
        None,                                    // 0xab0
        None,                                    // 0xab1
        None,                                    // 0xab2
        None,                                    // 0xab3
        None,                                    // 0xac0
        None,                                    // 0xac1
        None,                                    // 0xac2
        None,                                    // 0xac3
        None,                                    // 0xad0
        None,                                    // 0xad1
        None,                                    // 0xad2
        None,                                    // 0xad3
        None,                                    // 0xae0
        None,                                    // 0xae1
        None,                                    // 0xae2
        None,                                    // 0xae3
        None,                                    // 0xaf0
        None,                                    // 0xaf1
        None,                                    // 0xaf2
        None,                                    // 0xaf3
        None,                                    // 0xb00
        None,                                    // 0xb01
        None,                                    // 0xb02
        None,                                    // 0xb03
        None,                                    // 0xb10
        None,                                    // 0xb11
        None,                                    // 0xb12
        None,                                    // 0xb13
        None,                                    // 0xb20
        None,                                    // 0xb21
        None,                                    // 0xb22
        None,                                    // 0xb23
        None,                                    // 0xb30
        None,                                    // 0xb31
        None,                                    // 0xb32
        None,                                    // 0xb33
        None,                                    // 0xb40
        None,                                    // 0xb41
        None,                                    // 0xb42
        None,                                    // 0xb43
        None,                                    // 0xb50
        None,                                    // 0xb51
        None,                                    // 0xb52
        None,                                    // 0xb53
        None,                                    // 0xb60
        None,                                    // 0xb61
        None,                                    // 0xb62
        None,                                    // 0xb63
        None,                                    // 0xb70
        None,                                    // 0xb71
        None,                                    // 0xb72
        None,                                    // 0xb73
        None,                                    // 0xb80
        None,                                    // 0xb81
        None,                                    // 0xb82
        None,                                    // 0xb83
        None,                                    // 0xb90
        None,                                    // 0xb91
        None,                                    // 0xb92
        None,                                    // 0xb93
        None,                                    // 0xba0
        None,                                    // 0xba1
        None,                                    // 0xba2
        None,                                    // 0xba3
        None,                                    // 0xbb0
        None,                                    // 0xbb1
        None,                                    // 0xbb2
        None,                                    // 0xbb3
        None,                                    // 0xbc0
        None,                                    // 0xbc1
        None,                                    // 0xbc2
        None,                                    // 0xbc3
        None,                                    // 0xbd0
        None,                                    // 0xbd1
        None,                                    // 0xbd2
        None,                                    // 0xbd3
        None,                                    // 0xbe0
        None,                                    // 0xbe1
        None,                                    // 0xbe2
        None,                                    // 0xbe3
        None,                                    // 0xbf0
        None,                                    // 0xbf1
        None,                                    // 0xbf2
        None,                                    // 0xbf3
        None,                                    // 0xc00
        None,                                    // 0xc01
        None,                                    // 0xc02
        None,                                    // 0xc03
        None,                                    // 0xc10
        None,                                    // 0xc11
        None,                                    // 0xc12
        None,                                    // 0xc13
        None,                                    // 0xc20
        None,                                    // 0xc21
        None,                                    // 0xc22
        None,                                    // 0xc23
        None,                                    // 0xc30
        None,                                    // 0xc31
        None,                                    // 0xc32
        None,                                    // 0xc33
        None,                                    // 0xc40
        None,                                    // 0xc41
        None,                                    // 0xc42
        None,                                    // 0xc43
        None,                                    // 0xc50
        None,                                    // 0xc51
        None,                                    // 0xc52
        None,                                    // 0xc53
        None,                                    // 0xc60
        None,                                    // 0xc61
        None,                                    // 0xc62
        None,                                    // 0xc63
        None,                                    // 0xc70
        None,                                    // 0xc71
        None,                                    // 0xc72
        None,                                    // 0xc73
        MOp_M16B,                                // 0xc80 sha1nexte
        None,                                    // 0xc81
        None,                                    // 0xc82
        None,                                    // 0xc83
        MOp_M16B,                                // 0xc90 sha1msg1
        None,                                    // 0xc91
        None,                                    // 0xc92
        None,                                    // 0xc93
        MOp_M16B,                                // 0xca0 sha1msg2
        None,                                    // 0xca1
        None,                                    // 0xca2
        None,                                    // 0xca3
        MOp_M16B,                                // 0xcb0 sha256rnds2
        None,                                    // 0xcb1
        None,                                    // 0xcb2
        None,                                    // 0xcb3
        MOp_M16B,                                // 0xcc0 sha256msg1
        None,                                    // 0xcc1
        None,                                    // 0xcc2
        None,                                    // 0xcc3
        MOp_M16B,                                // 0xcd0 sha256msg2
        None,                                    // 0xcd1
        None,                                    // 0xcd2
        None,                                    // 0xcd3
        None,                                    // 0xce0
        None,                                    // 0xce1
        None,                                    // 0xce2
        None,                                    // 0xce3
        None,                                    // 0xcf0
        None,                                    // 0xcf1
        None,                                    // 0xcf2
        None,                                    // 0xcf3
        None,                                    // 0xd00
        None,                                    // 0xd01
        None,                                    // 0xd02
        None,                                    // 0xd03
        None,                                    // 0xd10
        None,                                    // 0xd11
        None,                                    // 0xd12
        None,                                    // 0xd13
        None,                                    // 0xd20
        None,                                    // 0xd21
        None,                                    // 0xd22
        None,                                    // 0xd23
        None,                                    // 0xd30
        None,                                    // 0xd31
        None,                                    // 0xd32
        None,                                    // 0xd33
        None,                                    // 0xd40
        None,                                    // 0xd41
        None,                                    // 0xd42
        None,                                    // 0xd43
        None,                                    // 0xd50
        None,                                    // 0xd51
        None,                                    // 0xd52
        None,                                    // 0xd53
        None,                                    // 0xd60
        None,                                    // 0xd61
        None,                                    // 0xd62
        None,                                    // 0xd63
        None,                                    // 0xd70
        None,                                    // 0xd71
        None,                                    // 0xd72
        None,                                    // 0xd73
        None,                                    // 0xd80
        None,                                    // 0xd81
        None,                                    // 0xd82
        None,                                    // 0xd83
        None,                                    // 0xd90
        None,                                    // 0xd91
        None,                                    // 0xd92
        None,                                    // 0xd93
        None,                                    // 0xda0
        None,                                    // 0xda1
        None,                                    // 0xda2
        None,                                    // 0xda3
        None,                                    // 0xdb0
        MOp_M16B,                                // 0xdb1 aesimc
        None,                                    // 0xdb2
        None,                                    // 0xdb3
        None,                                    // 0xdc0
        MOp_M16B,                                // 0xdc1 aesenc
        None,                                    // 0xdc2
        None,                                    // 0xdc3
        None,                                    // 0xdd0
        MOp_M16B,                                // 0xdd1 aesenclast
        None,                                    // 0xdd2
        None,                                    // 0xdd3
        None,                                    // 0xde0
        MOp_M16B,                                // 0xde1 aesdec
        None,                                    // 0xde2
        None,                                    // 0xde3
        None,                                    // 0xdf0
        MOp_M16B,                                // 0xdf1 aesdeclast
        None,                                    // 0xdf2
        None,                                    // 0xdf3
        None,                                    // 0xe00
        None,                                    // 0xe01
        None,                                    // 0xe02
        None,                                    // 0xe03
        None,                                    // 0xe10
        None,                                    // 0xe11
        None,                                    // 0xe12
        None,                                    // 0xe13
        None,                                    // 0xe20
        None,                                    // 0xe21
        None,                                    // 0xe22
        None,                                    // 0xe23
        None,                                    // 0xe30
        None,                                    // 0xe31
        None,                                    // 0xe32
        None,                                    // 0xe33
        None,                                    // 0xe40
        None,                                    // 0xe41
        None,                                    // 0xe42
        None,                                    // 0xe43
        None,                                    // 0xe50
        None,                                    // 0xe51
        None,                                    // 0xe52
        None,                                    // 0xe53
        None,                                    // 0xe60
        None,                                    // 0xe61
        None,                                    // 0xe62
        None,                                    // 0xe63
        None,                                    // 0xe70
        None,                                    // 0xe71
        None,                                    // 0xe72
        None,                                    // 0xe73
        None,                                    // 0xe80
        None,                                    // 0xe81
        None,                                    // 0xe82
        None,                                    // 0xe83
        None,                                    // 0xe90
        None,                                    // 0xe91
        None,                                    // 0xe92
        None,                                    // 0xe93
        None,                                    // 0xea0
        None,                                    // 0xea1
        None,                                    // 0xea2
        None,                                    // 0xea3
        None,                                    // 0xeb0
        None,                                    // 0xeb1
        None,                                    // 0xeb2
        None,                                    // 0xeb3
        None,                                    // 0xec0
        None,                                    // 0xec1
        None,                                    // 0xec2
        None,                                    // 0xec3
        None,                                    // 0xed0
        None,                                    // 0xed1
        None,                                    // 0xed2
        None,                                    // 0xed3
        None,                                    // 0xee0
        None,                                    // 0xee1
        None,                                    // 0xee2
        None,                                    // 0xee3
        None,                                    // 0xef0
        None,                                    // 0xef1
        None,                                    // 0xef2
        None,                                    // 0xef3
        MOp_W_M8B_or_M4B,                        // 0xf00 movbe
        MOp_W_M8B_or_M2B,                        // 0xf01 movbe
        None,                                    // 0xf02
        None,                                    // 0xf03
        M1st_W_M8B_or_M4B,                       // 0xf10 movbe
        M1st_W_M8B_or_M2B,                       // 0xf11 movbe
        None,                                    // 0xf12
        None,                                    // 0xf13
        None,                                    // 0xf20
        None,                                    // 0xf21
        None,                                    // 0xf22
        None,                                    // 0xf23
        None,                                    // 0xf30
        None,                                    // 0xf31
        None,                                    // 0xf32
        None,                                    // 0xf33
        None,                                    // 0xf40
        None,                                    // 0xf41
        None,                                    // 0xf42
        None,                                    // 0xf43
        None,                                    // 0xf50
        None,                                    // 0xf51
        None,                                    // 0xf52
        None,                                    // 0xf53
        None,                                    // 0xf60
        MOp_W_M8B_or_M4B,                        // 0xf61 adcx
        None,                                    // 0xf62
        None,                                    // 0xf63
        None,                                    // 0xf70
        None,                                    // 0xf71
        None,                                    // 0xf72
        None,                                    // 0xf73
        None,                                    // 0xf80
        None,                                    // 0xf81
        None,                                    // 0xf82
        None,                                    // 0xf83
        None,                                    // 0xf90
        None,                                    // 0xf91
        None,                                    // 0xf92
        None,                                    // 0xf93
        None,                                    // 0xfa0
        None,                                    // 0xfa1
        None,                                    // 0xfa2
        None,                                    // 0xfa3
        None,                                    // 0xfb0
        None,                                    // 0xfb1
        None,                                    // 0xfb2
        None,                                    // 0xfb3
        None,                                    // 0xfc0
        None,                                    // 0xfc1
        None,                                    // 0xfc2
        None,                                    // 0xfc3
        None,                                    // 0xfd0
        None,                                    // 0xfd1
        None,                                    // 0xfd2
        None,                                    // 0xfd3
        None,                                    // 0xfe0
        None,                                    // 0xfe1
        None,                                    // 0xfe2
        None,                                    // 0xfe3
        None,                                    // 0xff0
        None,                                    // 0xff1
        None,                                    // 0xff2
        None,                                    // 0xff3
    };

    static const InstrForm instrFormF3A[1024]
    {
        None,                                    // 0x000
        None,                                    // 0x001
        None,                                    // 0x002
        None,                                    // 0x003
        None,                                    // 0x010
        None,                                    // 0x011
        None,                                    // 0x012
        None,                                    // 0x013
        None,                                    // 0x020
        None,                                    // 0x021
        None,                                    // 0x022
        None,                                    // 0x023
        None,                                    // 0x030
        None,                                    // 0x031
        None,                                    // 0x032
        None,                                    // 0x033
        None,                                    // 0x040
        None,                                    // 0x041
        None,                                    // 0x042
        None,                                    // 0x043
        None,                                    // 0x050
        None,                                    // 0x051
        None,                                    // 0x052
        None,                                    // 0x053
        None,                                    // 0x060
        None,                                    // 0x061
        None,                                    // 0x062
        None,                                    // 0x063
        None,                                    // 0x070
        None,                                    // 0x071
        None,                                    // 0x072
        None,                                    // 0x073
        None,                                    // 0x080
        MOp_M16B_I1B,                            // 0x081 roundps
        None,                                    // 0x082
        None,                                    // 0x083
        None,                                    // 0x090
        MOp_M16B_I1B,                            // 0x091 roundpd
        None,                                    // 0x092
        None,                                    // 0x093
        None,                                    // 0x0a0
        MOp_M4B_I1B,                             // 0x0a1 roundss
        None,                                    // 0x0a2
        None,                                    // 0x0a3
        None,                                    // 0x0b0
        MOp_M8B_I1B,                             // 0x0b1 roundsd
        None,                                    // 0x0b2
        None,                                    // 0x0b3
        None,                                    // 0x0c0
        MOp_M16B_I1B,                            // 0x0c1 blendps
        None,                                    // 0x0c2
        None,                                    // 0x0c3
        None,                                    // 0x0d0
        MOp_M16B_I1B,                            // 0x0d1 blendpd
        None,                                    // 0x0d2
        None,                                    // 0x0d3
        None,                                    // 0x0e0
        MOp_M16B_I1B,                            // 0x0e1 pblendw
        None,                                    // 0x0e2
        None,                                    // 0x0e3
        MOp_M8B_I1B,                             // 0x0f0 palignr
        MOp_M16B_I1B,                            // 0x0f1 palignr
        None,                                    // 0x0f2
        None,                                    // 0x0f3
        None,                                    // 0x100
        None,                                    // 0x101
        None,                                    // 0x102
        None,                                    // 0x103
        None,                                    // 0x110
        None,                                    // 0x111
        None,                                    // 0x112
        None,                                    // 0x113
        None,                                    // 0x120
        None,                                    // 0x121
        None,                                    // 0x122
        None,                                    // 0x123
        None,                                    // 0x130
        None,                                    // 0x131
        None,                                    // 0x132
        None,                                    // 0x133
        None,                                    // 0x140
        M1st_M1B_I1B,                            // 0x141 pextrb
        None,                                    // 0x142
        None,                                    // 0x143
        None,                                    // 0x150
        M1st_M2B_I1B,                            // 0x151 pextrw
        None,                                    // 0x152
        None,                                    // 0x153
        None,                                    // 0x160
        M1st_I1B_W_M8B_or_M4B,                   // 0x161 pextrd,pextrq
        None,                                    // 0x162
        None,                                    // 0x163
        None,                                    // 0x170
        M1st_M4B_I1B,                            // 0x171 extractps
        None,                                    // 0x172
        None,                                    // 0x173
        None,                                    // 0x180
        None,                                    // 0x181
        None,                                    // 0x182
        None,                                    // 0x183
        None,                                    // 0x190
        None,                                    // 0x191
        None,                                    // 0x192
        None,                                    // 0x193
        None,                                    // 0x1a0
        None,                                    // 0x1a1
        None,                                    // 0x1a2
        None,                                    // 0x1a3
        None,                                    // 0x1b0
        None,                                    // 0x1b1
        None,                                    // 0x1b2
        None,                                    // 0x1b3
        None,                                    // 0x1c0
        None,                                    // 0x1c1
        None,                                    // 0x1c2
        None,                                    // 0x1c3
        None,                                    // 0x1d0
        None,                                    // 0x1d1
        None,                                    // 0x1d2
        None,                                    // 0x1d3
        None,                                    // 0x1e0
        None,                                    // 0x1e1
        None,                                    // 0x1e2
        None,                                    // 0x1e3
        None,                                    // 0x1f0
        None,                                    // 0x1f1
        None,                                    // 0x1f2
        None,                                    // 0x1f3
        None,                                    // 0x200
        MOp_M1B_I1B,                             // 0x201 pinsrb
        None,                                    // 0x202
        None,                                    // 0x203
        None,                                    // 0x210
        MOp_M4B_I1B,                             // 0x211 insertps
        None,                                    // 0x212
        None,                                    // 0x213
        None,                                    // 0x220
        MOp_I1B_W_M8B_or_M4B,                    // 0x221 pinsrd,pinsrq
        None,                                    // 0x222
        None,                                    // 0x223
        None,                                    // 0x230
        None,                                    // 0x231
        None,                                    // 0x232
        None,                                    // 0x233
        None,                                    // 0x240
        None,                                    // 0x241
        None,                                    // 0x242
        None,                                    // 0x243
        None,                                    // 0x250
        None,                                    // 0x251
        None,                                    // 0x252
        None,                                    // 0x253
        None,                                    // 0x260
        None,                                    // 0x261
        None,                                    // 0x262
        None,                                    // 0x263
        None,                                    // 0x270
        None,                                    // 0x271
        None,                                    // 0x272
        None,                                    // 0x273
        None,                                    // 0x280
        None,                                    // 0x281
        None,                                    // 0x282
        None,                                    // 0x283
        None,                                    // 0x290
        None,                                    // 0x291
        None,                                    // 0x292
        None,                                    // 0x293
        None,                                    // 0x2a0
        None,                                    // 0x2a1
        None,                                    // 0x2a2
        None,                                    // 0x2a3
        None,                                    // 0x2b0
        None,                                    // 0x2b1
        None,                                    // 0x2b2
        None,                                    // 0x2b3
        None,                                    // 0x2c0
        None,                                    // 0x2c1
        None,                                    // 0x2c2
        None,                                    // 0x2c3
        None,                                    // 0x2d0
        None,                                    // 0x2d1
        None,                                    // 0x2d2
        None,                                    // 0x2d3
        None,                                    // 0x2e0
        None,                                    // 0x2e1
        None,                                    // 0x2e2
        None,                                    // 0x2e3
        None,                                    // 0x2f0
        None,                                    // 0x2f1
        None,                                    // 0x2f2
        None,                                    // 0x2f3
        None,                                    // 0x300
        None,                                    // 0x301
        None,                                    // 0x302
        None,                                    // 0x303
        None,                                    // 0x310
        None,                                    // 0x311
        None,                                    // 0x312
        None,                                    // 0x313
        None,                                    // 0x320
        None,                                    // 0x321
        None,                                    // 0x322
        None,                                    // 0x323
        None,                                    // 0x330
        None,                                    // 0x331
        None,                                    // 0x332
        None,                                    // 0x333
        None,                                    // 0x340
        None,                                    // 0x341
        None,                                    // 0x342
        None,                                    // 0x343
        None,                                    // 0x350
        None,                                    // 0x351
        None,                                    // 0x352
        None,                                    // 0x353
        None,                                    // 0x360
        None,                                    // 0x361
        None,                                    // 0x362
        None,                                    // 0x363
        None,                                    // 0x370
        None,                                    // 0x371
        None,                                    // 0x372
        None,                                    // 0x373
        None,                                    // 0x380
        None,                                    // 0x381
        None,                                    // 0x382
        None,                                    // 0x383
        None,                                    // 0x390
        None,                                    // 0x391
        None,                                    // 0x392
        None,                                    // 0x393
        None,                                    // 0x3a0
        None,                                    // 0x3a1
        None,                                    // 0x3a2
        None,                                    // 0x3a3
        None,                                    // 0x3b0
        None,                                    // 0x3b1
        None,                                    // 0x3b2
        None,                                    // 0x3b3
        None,                                    // 0x3c0
        None,                                    // 0x3c1
        None,                                    // 0x3c2
        None,                                    // 0x3c3
        None,                                    // 0x3d0
        None,                                    // 0x3d1
        None,                                    // 0x3d2
        None,                                    // 0x3d3
        None,                                    // 0x3e0
        None,                                    // 0x3e1
        None,                                    // 0x3e2
        None,                                    // 0x3e3
        None,                                    // 0x3f0
        None,                                    // 0x3f1
        None,                                    // 0x3f2
        None,                                    // 0x3f3
        None,                                    // 0x400
        MOp_M16B_I1B,                            // 0x401 dpps
        None,                                    // 0x402
        None,                                    // 0x403
        None,                                    // 0x410
        MOp_M16B_I1B,                            // 0x411 dppd
        None,                                    // 0x412
        None,                                    // 0x413
        None,                                    // 0x420
        MOp_M16B_I1B,                            // 0x421 mpsadbw
        None,                                    // 0x422
        None,                                    // 0x423
        None,                                    // 0x430
        None,                                    // 0x431
        None,                                    // 0x432
        None,                                    // 0x433
        None,                                    // 0x440
        MOp_M16B_I1B,                            // 0x441 pclmulqdq
        None,                                    // 0x442
        None,                                    // 0x443
        None,                                    // 0x450
        None,                                    // 0x451
        None,                                    // 0x452
        None,                                    // 0x453
        None,                                    // 0x460
        None,                                    // 0x461
        None,                                    // 0x462
        None,                                    // 0x463
        None,                                    // 0x470
        None,                                    // 0x471
        None,                                    // 0x472
        None,                                    // 0x473
        None,                                    // 0x480
        None,                                    // 0x481
        None,                                    // 0x482
        None,                                    // 0x483
        None,                                    // 0x490
        None,                                    // 0x491
        None,                                    // 0x492
        None,                                    // 0x493
        None,                                    // 0x4a0
        None,                                    // 0x4a1
        None,                                    // 0x4a2
        None,                                    // 0x4a3
        None,                                    // 0x4b0
        None,                                    // 0x4b1
        None,                                    // 0x4b2
        None,                                    // 0x4b3
        None,                                    // 0x4c0
        None,                                    // 0x4c1
        None,                                    // 0x4c2
        None,                                    // 0x4c3
        None,                                    // 0x4d0
        None,                                    // 0x4d1
        None,                                    // 0x4d2
        None,                                    // 0x4d3
        None,                                    // 0x4e0
        None,                                    // 0x4e1
        None,                                    // 0x4e2
        None,                                    // 0x4e3
        None,                                    // 0x4f0
        None,                                    // 0x4f1
        None,                                    // 0x4f2
        None,                                    // 0x4f3
        None,                                    // 0x500
        None,                                    // 0x501
        None,                                    // 0x502
        None,                                    // 0x503
        None,                                    // 0x510
        None,                                    // 0x511
        None,                                    // 0x512
        None,                                    // 0x513
        None,                                    // 0x520
        None,                                    // 0x521
        None,                                    // 0x522
        None,                                    // 0x523
        None,                                    // 0x530
        None,                                    // 0x531
        None,                                    // 0x532
        None,                                    // 0x533
        None,                                    // 0x540
        None,                                    // 0x541
        None,                                    // 0x542
        None,                                    // 0x543
        None,                                    // 0x550
        None,                                    // 0x551
        None,                                    // 0x552
        None,                                    // 0x553
        None,                                    // 0x560
        None,                                    // 0x561
        None,                                    // 0x562
        None,                                    // 0x563
        None,                                    // 0x570
        None,                                    // 0x571
        None,                                    // 0x572
        None,                                    // 0x573
        None,                                    // 0x580
        None,                                    // 0x581
        None,                                    // 0x582
        None,                                    // 0x583
        None,                                    // 0x590
        None,                                    // 0x591
        None,                                    // 0x592
        None,                                    // 0x593
        None,                                    // 0x5a0
        None,                                    // 0x5a1
        None,                                    // 0x5a2
        None,                                    // 0x5a3
        None,                                    // 0x5b0
        None,                                    // 0x5b1
        None,                                    // 0x5b2
        None,                                    // 0x5b3
        None,                                    // 0x5c0
        None,                                    // 0x5c1
        None,                                    // 0x5c2
        None,                                    // 0x5c3
        None,                                    // 0x5d0
        None,                                    // 0x5d1
        None,                                    // 0x5d2
        None,                                    // 0x5d3
        None,                                    // 0x5e0
        None,                                    // 0x5e1
        None,                                    // 0x5e2
        None,                                    // 0x5e3
        None,                                    // 0x5f0
        None,                                    // 0x5f1
        None,                                    // 0x5f2
        None,                                    // 0x5f3
        None,                                    // 0x600
        MOp_M16B_I1B,                            // 0x601 pcmpestrm
        None,                                    // 0x602
        None,                                    // 0x603
        None,                                    // 0x610
        MOp_M16B_I1B,                            // 0x611 pcmpestri
        None,                                    // 0x612
        None,                                    // 0x613
        None,                                    // 0x620
        MOp_M16B_I1B,                            // 0x621 pcmpistrm
        None,                                    // 0x622
        None,                                    // 0x623
        None,                                    // 0x630
        MOp_M16B_I1B,                            // 0x631 pcmpistri
        None,                                    // 0x632
        None,                                    // 0x633
        None,                                    // 0x640
        None,                                    // 0x641
        None,                                    // 0x642
        None,                                    // 0x643
        None,                                    // 0x650
        None,                                    // 0x651
        None,                                    // 0x652
        None,                                    // 0x653
        None,                                    // 0x660
        None,                                    // 0x661
        None,                                    // 0x662
        None,                                    // 0x663
        None,                                    // 0x670
        None,                                    // 0x671
        None,                                    // 0x672
        None,                                    // 0x673
        None,                                    // 0x680
        None,                                    // 0x681
        None,                                    // 0x682
        None,                                    // 0x683
        None,                                    // 0x690
        None,                                    // 0x691
        None,                                    // 0x692
        None,                                    // 0x693
        None,                                    // 0x6a0
        None,                                    // 0x6a1
        None,                                    // 0x6a2
        None,                                    // 0x6a3
        None,                                    // 0x6b0
        None,                                    // 0x6b1
        None,                                    // 0x6b2
        None,                                    // 0x6b3
        None,                                    // 0x6c0
        None,                                    // 0x6c1
        None,                                    // 0x6c2
        None,                                    // 0x6c3
        None,                                    // 0x6d0
        None,                                    // 0x6d1
        None,                                    // 0x6d2
        None,                                    // 0x6d3
        None,                                    // 0x6e0
        None,                                    // 0x6e1
        None,                                    // 0x6e2
        None,                                    // 0x6e3
        None,                                    // 0x6f0
        None,                                    // 0x6f1
        None,                                    // 0x6f2
        None,                                    // 0x6f3
        None,                                    // 0x700
        None,                                    // 0x701
        None,                                    // 0x702
        None,                                    // 0x703
        None,                                    // 0x710
        None,                                    // 0x711
        None,                                    // 0x712
        None,                                    // 0x713
        None,                                    // 0x720
        None,                                    // 0x721
        None,                                    // 0x722
        None,                                    // 0x723
        None,                                    // 0x730
        None,                                    // 0x731
        None,                                    // 0x732
        None,                                    // 0x733
        None,                                    // 0x740
        None,                                    // 0x741
        None,                                    // 0x742
        None,                                    // 0x743
        None,                                    // 0x750
        None,                                    // 0x751
        None,                                    // 0x752
        None,                                    // 0x753
        None,                                    // 0x760
        None,                                    // 0x761
        None,                                    // 0x762
        None,                                    // 0x763
        None,                                    // 0x770
        None,                                    // 0x771
        None,                                    // 0x772
        None,                                    // 0x773
        None,                                    // 0x780
        None,                                    // 0x781
        None,                                    // 0x782
        None,                                    // 0x783
        None,                                    // 0x790
        None,                                    // 0x791
        None,                                    // 0x792
        None,                                    // 0x793
        None,                                    // 0x7a0
        None,                                    // 0x7a1
        None,                                    // 0x7a2
        None,                                    // 0x7a3
        None,                                    // 0x7b0
        None,                                    // 0x7b1
        None,                                    // 0x7b2
        None,                                    // 0x7b3
        None,                                    // 0x7c0
        None,                                    // 0x7c1
        None,                                    // 0x7c2
        None,                                    // 0x7c3
        None,                                    // 0x7d0
        None,                                    // 0x7d1
        None,                                    // 0x7d2
        None,                                    // 0x7d3
        None,                                    // 0x7e0
        None,                                    // 0x7e1
        None,                                    // 0x7e2
        None,                                    // 0x7e3
        None,                                    // 0x7f0
        None,                                    // 0x7f1
        None,                                    // 0x7f2
        None,                                    // 0x7f3
        None,                                    // 0x800
        None,                                    // 0x801
        None,                                    // 0x802
        None,                                    // 0x803
        None,                                    // 0x810
        None,                                    // 0x811
        None,                                    // 0x812
        None,                                    // 0x813
        None,                                    // 0x820
        None,                                    // 0x821
        None,                                    // 0x822
        None,                                    // 0x823
        None,                                    // 0x830
        None,                                    // 0x831
        None,                                    // 0x832
        None,                                    // 0x833
        None,                                    // 0x840
        None,                                    // 0x841
        None,                                    // 0x842
        None,                                    // 0x843
        None,                                    // 0x850
        None,                                    // 0x851
        None,                                    // 0x852
        None,                                    // 0x853
        None,                                    // 0x860
        None,                                    // 0x861
        None,                                    // 0x862
        None,                                    // 0x863
        None,                                    // 0x870
        None,                                    // 0x871
        None,                                    // 0x872
        None,                                    // 0x873
        None,                                    // 0x880
        None,                                    // 0x881
        None,                                    // 0x882
        None,                                    // 0x883
        None,                                    // 0x890
        None,                                    // 0x891
        None,                                    // 0x892
        None,                                    // 0x893
        None,                                    // 0x8a0
        None,                                    // 0x8a1
        None,                                    // 0x8a2
        None,                                    // 0x8a3
        None,                                    // 0x8b0
        None,                                    // 0x8b1
        None,                                    // 0x8b2
        None,                                    // 0x8b3
        None,                                    // 0x8c0
        None,                                    // 0x8c1
        None,                                    // 0x8c2
        None,                                    // 0x8c3
        None,                                    // 0x8d0
        None,                                    // 0x8d1
        None,                                    // 0x8d2
        None,                                    // 0x8d3
        None,                                    // 0x8e0
        None,                                    // 0x8e1
        None,                                    // 0x8e2
        None,                                    // 0x8e3
        None,                                    // 0x8f0
        None,                                    // 0x8f1
        None,                                    // 0x8f2
        None,                                    // 0x8f3
        None,                                    // 0x900
        None,                                    // 0x901
        None,                                    // 0x902
        None,                                    // 0x903
        None,                                    // 0x910
        None,                                    // 0x911
        None,                                    // 0x912
        None,                                    // 0x913
        None,                                    // 0x920
        None,                                    // 0x921
        None,                                    // 0x922
        None,                                    // 0x923
        None,                                    // 0x930
        None,                                    // 0x931
        None,                                    // 0x932
        None,                                    // 0x933
        None,                                    // 0x940
        None,                                    // 0x941
        None,                                    // 0x942
        None,                                    // 0x943
        None,                                    // 0x950
        None,                                    // 0x951
        None,                                    // 0x952
        None,                                    // 0x953
        None,                                    // 0x960
        None,                                    // 0x961
        None,                                    // 0x962
        None,                                    // 0x963
        None,                                    // 0x970
        None,                                    // 0x971
        None,                                    // 0x972
        None,                                    // 0x973
        None,                                    // 0x980
        None,                                    // 0x981
        None,                                    // 0x982
        None,                                    // 0x983
        None,                                    // 0x990
        None,                                    // 0x991
        None,                                    // 0x992
        None,                                    // 0x993
        None,                                    // 0x9a0
        None,                                    // 0x9a1
        None,                                    // 0x9a2
        None,                                    // 0x9a3
        None,                                    // 0x9b0
        None,                                    // 0x9b1
        None,                                    // 0x9b2
        None,                                    // 0x9b3
        None,                                    // 0x9c0
        None,                                    // 0x9c1
        None,                                    // 0x9c2
        None,                                    // 0x9c3
        None,                                    // 0x9d0
        None,                                    // 0x9d1
        None,                                    // 0x9d2
        None,                                    // 0x9d3
        None,                                    // 0x9e0
        None,                                    // 0x9e1
        None,                                    // 0x9e2
        None,                                    // 0x9e3
        None,                                    // 0x9f0
        None,                                    // 0x9f1
        None,                                    // 0x9f2
        None,                                    // 0x9f3
        None,                                    // 0xa00
        None,                                    // 0xa01
        None,                                    // 0xa02
        None,                                    // 0xa03
        None,                                    // 0xa10
        None,                                    // 0xa11
        None,                                    // 0xa12
        None,                                    // 0xa13
        None,                                    // 0xa20
        None,                                    // 0xa21
        None,                                    // 0xa22
        None,                                    // 0xa23
        None,                                    // 0xa30
        None,                                    // 0xa31
        None,                                    // 0xa32
        None,                                    // 0xa33
        None,                                    // 0xa40
        None,                                    // 0xa41
        None,                                    // 0xa42
        None,                                    // 0xa43
        None,                                    // 0xa50
        None,                                    // 0xa51
        None,                                    // 0xa52
        None,                                    // 0xa53
        None,                                    // 0xa60
        None,                                    // 0xa61
        None,                                    // 0xa62
        None,                                    // 0xa63
        None,                                    // 0xa70
        None,                                    // 0xa71
        None,                                    // 0xa72
        None,                                    // 0xa73
        None,                                    // 0xa80
        None,                                    // 0xa81
        None,                                    // 0xa82
        None,                                    // 0xa83
        None,                                    // 0xa90
        None,                                    // 0xa91
        None,                                    // 0xa92
        None,                                    // 0xa93
        None,                                    // 0xaa0
        None,                                    // 0xaa1
        None,                                    // 0xaa2
        None,                                    // 0xaa3
        None,                                    // 0xab0
        None,                                    // 0xab1
        None,                                    // 0xab2
        None,                                    // 0xab3
        None,                                    // 0xac0
        None,                                    // 0xac1
        None,                                    // 0xac2
        None,                                    // 0xac3
        None,                                    // 0xad0
        None,                                    // 0xad1
        None,                                    // 0xad2
        None,                                    // 0xad3
        None,                                    // 0xae0
        None,                                    // 0xae1
        None,                                    // 0xae2
        None,                                    // 0xae3
        None,                                    // 0xaf0
        None,                                    // 0xaf1
        None,                                    // 0xaf2
        None,                                    // 0xaf3
        None,                                    // 0xb00
        None,                                    // 0xb01
        None,                                    // 0xb02
        None,                                    // 0xb03
        None,                                    // 0xb10
        None,                                    // 0xb11
        None,                                    // 0xb12
        None,                                    // 0xb13
        None,                                    // 0xb20
        None,                                    // 0xb21
        None,                                    // 0xb22
        None,                                    // 0xb23
        None,                                    // 0xb30
        None,                                    // 0xb31
        None,                                    // 0xb32
        None,                                    // 0xb33
        None,                                    // 0xb40
        None,                                    // 0xb41
        None,                                    // 0xb42
        None,                                    // 0xb43
        None,                                    // 0xb50
        None,                                    // 0xb51
        None,                                    // 0xb52
        None,                                    // 0xb53
        None,                                    // 0xb60
        None,                                    // 0xb61
        None,                                    // 0xb62
        None,                                    // 0xb63
        None,                                    // 0xb70
        None,                                    // 0xb71
        None,                                    // 0xb72
        None,                                    // 0xb73
        None,                                    // 0xb80
        None,                                    // 0xb81
        None,                                    // 0xb82
        None,                                    // 0xb83
        None,                                    // 0xb90
        None,                                    // 0xb91
        None,                                    // 0xb92
        None,                                    // 0xb93
        None,                                    // 0xba0
        None,                                    // 0xba1
        None,                                    // 0xba2
        None,                                    // 0xba3
        None,                                    // 0xbb0
        None,                                    // 0xbb1
        None,                                    // 0xbb2
        None,                                    // 0xbb3
        None,                                    // 0xbc0
        None,                                    // 0xbc1
        None,                                    // 0xbc2
        None,                                    // 0xbc3
        None,                                    // 0xbd0
        None,                                    // 0xbd1
        None,                                    // 0xbd2
        None,                                    // 0xbd3
        None,                                    // 0xbe0
        None,                                    // 0xbe1
        None,                                    // 0xbe2
        None,                                    // 0xbe3
        None,                                    // 0xbf0
        None,                                    // 0xbf1
        None,                                    // 0xbf2
        None,                                    // 0xbf3
        None,                                    // 0xc00
        None,                                    // 0xc01
        None,                                    // 0xc02
        None,                                    // 0xc03
        None,                                    // 0xc10
        None,                                    // 0xc11
        None,                                    // 0xc12
        None,                                    // 0xc13
        None,                                    // 0xc20
        None,                                    // 0xc21
        None,                                    // 0xc22
        None,                                    // 0xc23
        None,                                    // 0xc30
        None,                                    // 0xc31
        None,                                    // 0xc32
        None,                                    // 0xc33
        None,                                    // 0xc40
        None,                                    // 0xc41
        None,                                    // 0xc42
        None,                                    // 0xc43
        None,                                    // 0xc50
        None,                                    // 0xc51
        None,                                    // 0xc52
        None,                                    // 0xc53
        None,                                    // 0xc60
        None,                                    // 0xc61
        None,                                    // 0xc62
        None,                                    // 0xc63
        None,                                    // 0xc70
        None,                                    // 0xc71
        None,                                    // 0xc72
        None,                                    // 0xc73
        None,                                    // 0xc80
        None,                                    // 0xc81
        None,                                    // 0xc82
        None,                                    // 0xc83
        None,                                    // 0xc90
        None,                                    // 0xc91
        None,                                    // 0xc92
        None,                                    // 0xc93
        None,                                    // 0xca0
        None,                                    // 0xca1
        None,                                    // 0xca2
        None,                                    // 0xca3
        None,                                    // 0xcb0
        None,                                    // 0xcb1
        None,                                    // 0xcb2
        None,                                    // 0xcb3
        MOp_M16B_I1B,                            // 0xcc0 sha1rnds4
        None,                                    // 0xcc1
        None,                                    // 0xcc2
        None,                                    // 0xcc3
        None,                                    // 0xcd0
        None,                                    // 0xcd1
        None,                                    // 0xcd2
        None,                                    // 0xcd3
        None,                                    // 0xce0
        None,                                    // 0xce1
        None,                                    // 0xce2
        None,                                    // 0xce3
        None,                                    // 0xcf0
        None,                                    // 0xcf1
        None,                                    // 0xcf2
        None,                                    // 0xcf3
        None,                                    // 0xd00
        None,                                    // 0xd01
        None,                                    // 0xd02
        None,                                    // 0xd03
        None,                                    // 0xd10
        None,                                    // 0xd11
        None,                                    // 0xd12
        None,                                    // 0xd13
        None,                                    // 0xd20
        None,                                    // 0xd21
        None,                                    // 0xd22
        None,                                    // 0xd23
        None,                                    // 0xd30
        None,                                    // 0xd31
        None,                                    // 0xd32
        None,                                    // 0xd33
        None,                                    // 0xd40
        None,                                    // 0xd41
        None,                                    // 0xd42
        None,                                    // 0xd43
        None,                                    // 0xd50
        None,                                    // 0xd51
        None,                                    // 0xd52
        None,                                    // 0xd53
        None,                                    // 0xd60
        None,                                    // 0xd61
        None,                                    // 0xd62
        None,                                    // 0xd63
        None,                                    // 0xd70
        None,                                    // 0xd71
        None,                                    // 0xd72
        None,                                    // 0xd73
        None,                                    // 0xd80
        None,                                    // 0xd81
        None,                                    // 0xd82
        None,                                    // 0xd83
        None,                                    // 0xd90
        None,                                    // 0xd91
        None,                                    // 0xd92
        None,                                    // 0xd93
        None,                                    // 0xda0
        None,                                    // 0xda1
        None,                                    // 0xda2
        None,                                    // 0xda3
        None,                                    // 0xdb0
        None,                                    // 0xdb1
        None,                                    // 0xdb2
        None,                                    // 0xdb3
        None,                                    // 0xdc0
        None,                                    // 0xdc1
        None,                                    // 0xdc2
        None,                                    // 0xdc3
        None,                                    // 0xdd0
        None,                                    // 0xdd1
        None,                                    // 0xdd2
        None,                                    // 0xdd3
        None,                                    // 0xde0
        None,                                    // 0xde1
        None,                                    // 0xde2
        None,                                    // 0xde3
        None,                                    // 0xdf0
        MOp_M16B_I1B,                            // 0xdf1 aeskeygenassist
        None,                                    // 0xdf2
        None,                                    // 0xdf3
        None,                                    // 0xe00
        None,                                    // 0xe01
        None,                                    // 0xe02
        None,                                    // 0xe03
        None,                                    // 0xe10
        None,                                    // 0xe11
        None,                                    // 0xe12
        None,                                    // 0xe13
        None,                                    // 0xe20
        None,                                    // 0xe21
        None,                                    // 0xe22
        None,                                    // 0xe23
        None,                                    // 0xe30
        None,                                    // 0xe31
        None,                                    // 0xe32
        None,                                    // 0xe33
        None,                                    // 0xe40
        None,                                    // 0xe41
        None,                                    // 0xe42
        None,                                    // 0xe43
        None,                                    // 0xe50
        None,                                    // 0xe51
        None,                                    // 0xe52
        None,                                    // 0xe53
        None,                                    // 0xe60
        None,                                    // 0xe61
        None,                                    // 0xe62
        None,                                    // 0xe63
        None,                                    // 0xe70
        None,                                    // 0xe71
        None,                                    // 0xe72
        None,                                    // 0xe73
        None,                                    // 0xe80
        None,                                    // 0xe81
        None,                                    // 0xe82
        None,                                    // 0xe83
        None,                                    // 0xe90
        None,                                    // 0xe91
        None,                                    // 0xe92
        None,                                    // 0xe93
        None,                                    // 0xea0
        None,                                    // 0xea1
        None,                                    // 0xea2
        None,                                    // 0xea3
        None,                                    // 0xeb0
        None,                                    // 0xeb1
        None,                                    // 0xeb2
        None,                                    // 0xeb3
        None,                                    // 0xec0
        None,                                    // 0xec1
        None,                                    // 0xec2
        None,                                    // 0xec3
        None,                                    // 0xed0
        None,                                    // 0xed1
        None,                                    // 0xed2
        None,                                    // 0xed3
        None,                                    // 0xee0
        None,                                    // 0xee1
        None,                                    // 0xee2
        None,                                    // 0xee3
        None,                                    // 0xef0
        None,                                    // 0xef1
        None,                                    // 0xef2
        None,                                    // 0xef3
        None,                                    // 0xf00
        None,                                    // 0xf01
        None,                                    // 0xf02
        None,                                    // 0xf03
        None,                                    // 0xf10
        None,                                    // 0xf11
        None,                                    // 0xf12
        None,                                    // 0xf13
        None,                                    // 0xf20
        None,                                    // 0xf21
        None,                                    // 0xf22
        None,                                    // 0xf23
        None,                                    // 0xf30
        None,                                    // 0xf31
        None,                                    // 0xf32
        None,                                    // 0xf33
        None,                                    // 0xf40
        None,                                    // 0xf41
        None,                                    // 0xf42
        None,                                    // 0xf43
        None,                                    // 0xf50
        None,                                    // 0xf51
        None,                                    // 0xf52
        None,                                    // 0xf53
        None,                                    // 0xf60
        None,                                    // 0xf61
        None,                                    // 0xf62
        None,                                    // 0xf63
        None,                                    // 0xf70
        None,                                    // 0xf71
        None,                                    // 0xf72
        None,                                    // 0xf73
        None,                                    // 0xf80
        None,                                    // 0xf81
        None,                                    // 0xf82
        None,                                    // 0xf83
        None,                                    // 0xf90
        None,                                    // 0xf91
        None,                                    // 0xf92
        None,                                    // 0xf93
        None,                                    // 0xfa0
        None,                                    // 0xfa1
        None,                                    // 0xfa2
        None,                                    // 0xfa3
        None,                                    // 0xfb0
        None,                                    // 0xfb1
        None,                                    // 0xfb2
        None,                                    // 0xfb3
        None,                                    // 0xfc0
        None,                                    // 0xfc1
        None,                                    // 0xfc2
        None,                                    // 0xfc3
        None,                                    // 0xfd0
        None,                                    // 0xfd1
        None,                                    // 0xfd2
        None,                                    // 0xfd3
        None,                                    // 0xfe0
        None,                                    // 0xfe1
        None,                                    // 0xfe2
        None,                                    // 0xfe3
        None,                                    // 0xff0
        None,                                    // 0xff1
        None,                                    // 0xff2
        None,                                    // 0xff3
    };

    static const InstrForm instrFormVex1[1024]
    {
        None,                                    // 0x000
        None,                                    // 0x001
        None,                                    // 0x002
        None,                                    // 0x003
        None,                                    // 0x010
        None,                                    // 0x011
        None,                                    // 0x012
        None,                                    // 0x013
        None,                                    // 0x020
        None,                                    // 0x021
        None,                                    // 0x022
        None,                                    // 0x023
        None,                                    // 0x030
        None,                                    // 0x031
        None,                                    // 0x032
        None,                                    // 0x033
        None,                                    // 0x040
        None,                                    // 0x041
        None,                                    // 0x042
        None,                                    // 0x043
        None,                                    // 0x050
        None,                                    // 0x051
        None,                                    // 0x052
        None,                                    // 0x053
        None,                                    // 0x060
        None,                                    // 0x061
        None,                                    // 0x062
        None,                                    // 0x063
        None,                                    // 0x070
        None,                                    // 0x071
        None,                                    // 0x072
        None,                                    // 0x073
        None,                                    // 0x080
        None,                                    // 0x081
        None,                                    // 0x082
        None,                                    // 0x083
        None,                                    // 0x090
        None,                                    // 0x091
        None,                                    // 0x092
        None,                                    // 0x093
        None,                                    // 0x0a0
        None,                                    // 0x0a1
        None,                                    // 0x0a2
        None,                                    // 0x0a3
        None,                                    // 0x0b0
        None,                                    // 0x0b1
        None,                                    // 0x0b2
        None,                                    // 0x0b3
        None,                                    // 0x0c0
        None,                                    // 0x0c1
        None,                                    // 0x0c2
        None,                                    // 0x0c3
        None,                                    // 0x0d0
        None,                                    // 0x0d1
        None,                                    // 0x0d2
        None,                                    // 0x0d3
        None,                                    // 0x0e0
        None,                                    // 0x0e1
        None,                                    // 0x0e2
        None,                                    // 0x0e3
        None,                                    // 0x0f0
        None,                                    // 0x0f1
        None,                                    // 0x0f2
        None,                                    // 0x0f3
        MOp_L_M32B_or_M16B,                      // 0x100 vmovups
        MOp_L_M32B_or_M16B,                      // 0x101 vmovupd
        MOp_M4B,                                 // 0x102 vmovss
        MOp_M8B,                                 // 0x103 vmovsd
        M1st_L_M32B_or_M16B,                     // 0x110 vmovups
        M1st_L_M32B_or_M16B,                     // 0x111 vmovupd
        M1st_M4B,                                // 0x112 vmovss
        M1st_M8B,                                // 0x113 vmovsd
        MOp_M8B,                                 // 0x120 vmovlps
        MOp_M8B,                                 // 0x121 vmovlpd
        MOp_L_M32B_or_M16B,                      // 0x122 vmovsldup
        MOp_L_M32B_or_M8B,                       // 0x123 vmovddup
        M1st_M8B,                                // 0x130 vmovlps
        M1st_M8B,                                // 0x131 vmovlpd
        M1st_M8B,                                // 0x132 vmovlps
        M1st_M8B,                                // 0x133 vmovlps
        MOp_L_M32B_or_M16B,                      // 0x140 vunpcklps
        MOp_L_M32B_or_M16B,                      // 0x141 vunpcklpd
        MOp_L_M32B_or_M16B,                      // 0x142 vunpcklps
        MOp_L_M32B_or_M16B,                      // 0x143 vunpcklps
        MOp_L_M32B_or_M16B,                      // 0x150 vunpckhps
        MOp_L_M32B_or_M16B,                      // 0x151 vunpckhpd
        MOp_L_M32B_or_M16B,                      // 0x152 vunpckhps
        MOp_L_M32B_or_M16B,                      // 0x153 vunpckhps
        MOp_M8B,                                 // 0x160 vmovhps
        MOp_M8B,                                 // 0x161 vmovhpd
        MOp_L_M32B_or_M16B,                      // 0x162 vmovshdup
        None,                                    // 0x163
        M1st_M8B,                                // 0x170 vmovhps
        M1st_M8B,                                // 0x171 vmovhpd
        M1st_M8B,                                // 0x172 vmovhps
        M1st_M8B,                                // 0x173 vmovhps
        None,                                    // 0x180
        None,                                    // 0x181
        None,                                    // 0x182
        None,                                    // 0x183
        None,                                    // 0x190
        None,                                    // 0x191
        None,                                    // 0x192
        None,                                    // 0x193
        None,                                    // 0x1a0
        None,                                    // 0x1a1
        None,                                    // 0x1a2
        None,                                    // 0x1a3
        None,                                    // 0x1b0
        None,                                    // 0x1b1
        None,                                    // 0x1b2
        None,                                    // 0x1b3
        None,                                    // 0x1c0
        None,                                    // 0x1c1
        None,                                    // 0x1c2
        None,                                    // 0x1c3
        None,                                    // 0x1d0
        None,                                    // 0x1d1
        None,                                    // 0x1d2
        None,                                    // 0x1d3
        None,                                    // 0x1e0
        None,                                    // 0x1e1
        None,                                    // 0x1e2
        None,                                    // 0x1e3
        None,                                    // 0x1f0
        None,                                    // 0x1f1
        None,                                    // 0x1f2
        None,                                    // 0x1f3
        None,                                    // 0x200
        None,                                    // 0x201
        None,                                    // 0x202
        None,                                    // 0x203
        None,                                    // 0x210
        None,                                    // 0x211
        None,                                    // 0x212
        None,                                    // 0x213
        None,                                    // 0x220
        None,                                    // 0x221
        None,                                    // 0x222
        None,                                    // 0x223
        None,                                    // 0x230
        None,                                    // 0x231
        None,                                    // 0x232
        None,                                    // 0x233
        None,                                    // 0x240
        None,                                    // 0x241
        None,                                    // 0x242
        None,                                    // 0x243
        None,                                    // 0x250
        None,                                    // 0x251
        None,                                    // 0x252
        None,                                    // 0x253
        None,                                    // 0x260
        None,                                    // 0x261
        None,                                    // 0x262
        None,                                    // 0x263
        None,                                    // 0x270
        None,                                    // 0x271
        None,                                    // 0x272
        None,                                    // 0x273
        MOp_L_M32B_or_M16B,                      // 0x280 vmovaps
        MOp_L_M32B_or_M16B,                      // 0x281 vmovapd
        MOp_L_M32B_or_M16B,                      // 0x282 vmovaps
        MOp_L_M32B_or_M16B,                      // 0x283 vmovaps
        M1st_L_M32B_or_M16B,                     // 0x290 vmovaps
        M1st_L_M32B_or_M16B,                     // 0x291 vmovapd
        M1st_L_M32B_or_M16B,                     // 0x292 vmovaps
        M1st_L_M32B_or_M16B,                     // 0x293 vmovaps
        None,                                    // 0x2a0
        None,                                    // 0x2a1
        MOp_W_M8B_or_M4B,                        // 0x2a2 vcvtsi2ss
        MOp_W_M8B_or_M4B,                        // 0x2a3 vcvtsi2sd
        M1st_L_M32B_or_M16B,                     // 0x2b0 vmovntps
        M1st_L_M32B_or_M16B,                     // 0x2b1 vmovntpd
        M1st_L_M32B_or_M16B,                     // 0x2b2 vmovntps
        M1st_L_M32B_or_M16B,                     // 0x2b3 vmovntps
        None,                                    // 0x2c0
        None,                                    // 0x2c1
        MOp_M4B,                                 // 0x2c2 vcvttss2si
        MOp_M8B,                                 // 0x2c3 vcvttsd2si
        None,                                    // 0x2d0
        None,                                    // 0x2d1
        MOp_M4B,                                 // 0x2d2 vcvtss2si
        MOp_M8B,                                 // 0x2d3 vcvtsd2si
        MOp_M4B,                                 // 0x2e0 vucomiss
        MOp_M8B,                                 // 0x2e1 vucomisd
        None,                                    // 0x2e2
        None,                                    // 0x2e3
        MOp_M4B,                                 // 0x2f0 vcomiss
        MOp_M8B,                                 // 0x2f1 vcomisd
        None,                                    // 0x2f2
        None,                                    // 0x2f3
        None,                                    // 0x300
        None,                                    // 0x301
        None,                                    // 0x302
        None,                                    // 0x303
        None,                                    // 0x310
        None,                                    // 0x311
        None,                                    // 0x312
        None,                                    // 0x313
        None,                                    // 0x320
        None,                                    // 0x321
        None,                                    // 0x322
        None,                                    // 0x323
        None,                                    // 0x330
        None,                                    // 0x331
        None,                                    // 0x332
        None,                                    // 0x333
        None,                                    // 0x340
        None,                                    // 0x341
        None,                                    // 0x342
        None,                                    // 0x343
        None,                                    // 0x350
        None,                                    // 0x351
        None,                                    // 0x352
        None,                                    // 0x353
        None,                                    // 0x360
        None,                                    // 0x361
        None,                                    // 0x362
        None,                                    // 0x363
        None,                                    // 0x370
        None,                                    // 0x371
        None,                                    // 0x372
        None,                                    // 0x373
        None,                                    // 0x380
        None,                                    // 0x381
        None,                                    // 0x382
        None,                                    // 0x383
        None,                                    // 0x390
        None,                                    // 0x391
        None,                                    // 0x392
        None,                                    // 0x393
        None,                                    // 0x3a0
        None,                                    // 0x3a1
        None,                                    // 0x3a2
        None,                                    // 0x3a3
        None,                                    // 0x3b0
        None,                                    // 0x3b1
        None,                                    // 0x3b2
        None,                                    // 0x3b3
        None,                                    // 0x3c0
        None,                                    // 0x3c1
        None,                                    // 0x3c2
        None,                                    // 0x3c3
        None,                                    // 0x3d0
        None,                                    // 0x3d1
        None,                                    // 0x3d2
        None,                                    // 0x3d3
        None,                                    // 0x3e0
        None,                                    // 0x3e1
        None,                                    // 0x3e2
        None,                                    // 0x3e3
        None,                                    // 0x3f0
        None,                                    // 0x3f1
        None,                                    // 0x3f2
        None,                                    // 0x3f3
        None,                                    // 0x400
        None,                                    // 0x401
        None,                                    // 0x402
        None,                                    // 0x403
        None,                                    // 0x410
        None,                                    // 0x411
        None,                                    // 0x412
        None,                                    // 0x413
        None,                                    // 0x420
        None,                                    // 0x421
        None,                                    // 0x422
        None,                                    // 0x423
        None,                                    // 0x430
        None,                                    // 0x431
        None,                                    // 0x432
        None,                                    // 0x433
        None,                                    // 0x440
        None,                                    // 0x441
        None,                                    // 0x442
        None,                                    // 0x443
        None,                                    // 0x450
        None,                                    // 0x451
        None,                                    // 0x452
        None,                                    // 0x453
        None,                                    // 0x460
        None,                                    // 0x461
        None,                                    // 0x462
        None,                                    // 0x463
        None,                                    // 0x470
        None,                                    // 0x471
        None,                                    // 0x472
        None,                                    // 0x473
        None,                                    // 0x480
        None,                                    // 0x481
        None,                                    // 0x482
        None,                                    // 0x483
        None,                                    // 0x490
        None,                                    // 0x491
        None,                                    // 0x492
        None,                                    // 0x493
        None,                                    // 0x4a0
        None,                                    // 0x4a1
        None,                                    // 0x4a2
        None,                                    // 0x4a3
        None,                                    // 0x4b0
        None,                                    // 0x4b1
        None,                                    // 0x4b2
        None,                                    // 0x4b3
        None,                                    // 0x4c0
        None,                                    // 0x4c1
        None,                                    // 0x4c2
        None,                                    // 0x4c3
        None,                                    // 0x4d0
        None,                                    // 0x4d1
        None,                                    // 0x4d2
        None,                                    // 0x4d3
        None,                                    // 0x4e0
        None,                                    // 0x4e1
        None,                                    // 0x4e2
        None,                                    // 0x4e3
        None,                                    // 0x4f0
        None,                                    // 0x4f1
        None,                                    // 0x4f2
        None,                                    // 0x4f3
        None,                                    // 0x500
        None,                                    // 0x501
        None,                                    // 0x502
        None,                                    // 0x503
        MOp_L_M32B_or_M16B,                      // 0x510 vsqrtps
        MOp_L_M32B_or_M16B,                      // 0x511 vsqrtpd
        MOp_M4B,                                 // 0x512 vsqrtss
        MOp_M8B,                                 // 0x513 vsqrtsd
        MOp_L_M32B_or_M16B,                      // 0x520 vrsqrtps
        None,                                    // 0x521
        MOp_M4B,                                 // 0x522 vrsqrtss
        None,                                    // 0x523
        MOp_L_M32B_or_M16B,                      // 0x530 vrcpps
        None,                                    // 0x531
        MOp_M4B,                                 // 0x532 vrcpss
        None,                                    // 0x533
        MOp_L_M32B_or_M16B,                      // 0x540 vandps
        MOp_L_M32B_or_M16B,                      // 0x541 vandpd
        MOp_L_M32B_or_M16B,                      // 0x542 vandps
        MOp_L_M32B_or_M16B,                      // 0x543 vandps
        MOp_L_M32B_or_M16B,                      // 0x550 vandnps
        MOp_L_M32B_or_M16B,                      // 0x551 vandnpd
        MOp_L_M32B_or_M16B,                      // 0x552 vandnps
        MOp_L_M32B_or_M16B,                      // 0x553 vandnps
        MOp_L_M32B_or_M16B,                      // 0x560 vorps
        MOp_L_M32B_or_M16B,                      // 0x561 vorpd
        MOp_L_M32B_or_M16B,                      // 0x562 vorps
        MOp_L_M32B_or_M16B,                      // 0x563 vorps
        MOp_L_M32B_or_M16B,                      // 0x570 vxorps
        MOp_L_M32B_or_M16B,                      // 0x571 vxorpd
        MOp_L_M32B_or_M16B,                      // 0x572 vxorps
        MOp_L_M32B_or_M16B,                      // 0x573 vxorps
        MOp_L_M32B_or_M16B,                      // 0x580 vaddps
        MOp_L_M32B_or_M16B,                      // 0x581 vaddpd
        MOp_M4B,                                 // 0x582 vaddss
        MOp_M8B,                                 // 0x583 vaddsd
        MOp_L_M32B_or_M16B,                      // 0x590 vmulps
        MOp_L_M32B_or_M16B,                      // 0x591 vmulpd
        MOp_M4B,                                 // 0x592 vmulss
        MOp_M8B,                                 // 0x593 vmulsd
        MOp_L_M16B_or_M8B,                       // 0x5a0 vcvtps2pd
        MOp_L_M32B_or_M16B,                      // 0x5a1 vcvtpd2ps
        MOp_M4B,                                 // 0x5a2 vcvtss2sd
        MOp_M8B,                                 // 0x5a3 vcvtsd2ss
        MOp_L_M32B_or_M16B,                      // 0x5b0 vcvtdq2ps
        MOp_L_M32B_or_M16B,                      // 0x5b1 vcvtps2dq
        MOp_L_M32B_or_M16B,                      // 0x5b2 vcvttps2dq
        None,                                    // 0x5b3
        MOp_L_M32B_or_M16B,                      // 0x5c0 vsubps
        MOp_L_M32B_or_M16B,                      // 0x5c1 vsubpd
        MOp_M4B,                                 // 0x5c2 vsubss
        MOp_M8B,                                 // 0x5c3 vsubsd
        MOp_L_M32B_or_M16B,                      // 0x5d0 vminps
        MOp_L_M32B_or_M16B,                      // 0x5d1 vminpd
        MOp_M4B,                                 // 0x5d2 vminss
        MOp_M8B,                                 // 0x5d3 vminsd
        MOp_L_M32B_or_M16B,                      // 0x5e0 vdivps
        MOp_L_M32B_or_M16B,                      // 0x5e1 vdivpd
        MOp_M4B,                                 // 0x5e2 vdivss
        MOp_M8B,                                 // 0x5e3 vdivsd
        MOp_L_M32B_or_M16B,                      // 0x5f0 vmaxps
        MOp_L_M32B_or_M16B,                      // 0x5f1 vmaxpd
        MOp_M4B,                                 // 0x5f2 vmaxss
        MOp_M8B,                                 // 0x5f3 vmaxsd
        None,                                    // 0x600
        MOp_L_M32B_or_M16B,                      // 0x601 vpunpcklbw
        None,                                    // 0x602
        None,                                    // 0x603
        None,                                    // 0x610
        MOp_L_M32B_or_M16B,                      // 0x611 vpunpcklwd
        None,                                    // 0x612
        None,                                    // 0x613
        None,                                    // 0x620
        MOp_L_M32B_or_M16B,                      // 0x621 vpunpckldq
        None,                                    // 0x622
        None,                                    // 0x623
        None,                                    // 0x630
        MOp_L_M32B_or_M16B,                      // 0x631 vpacksswb
        None,                                    // 0x632
        None,                                    // 0x633
        None,                                    // 0x640
        MOp_L_M32B_or_M16B,                      // 0x641 vpcmpgtb
        None,                                    // 0x642
        None,                                    // 0x643
        None,                                    // 0x650
        MOp_L_M32B_or_M16B,                      // 0x651 vpcmpgtw
        None,                                    // 0x652
        None,                                    // 0x653
        None,                                    // 0x660
        MOp_L_M32B_or_M16B,                      // 0x661 vpcmpgtd
        None,                                    // 0x662
        None,                                    // 0x663
        None,                                    // 0x670
        MOp_L_M32B_or_M16B,                      // 0x671 vpackuswb
        None,                                    // 0x672
        None,                                    // 0x673
        None,                                    // 0x680
        MOp_L_M32B_or_M16B,                      // 0x681 vpunpckhbw
        None,                                    // 0x682
        None,                                    // 0x683
        None,                                    // 0x690
        MOp_L_M32B_or_M16B,                      // 0x691 vpunpckhwd
        None,                                    // 0x692
        None,                                    // 0x693
        None,                                    // 0x6a0
        MOp_L_M32B_or_M16B,                      // 0x6a1 vpunpckhdq
        None,                                    // 0x6a2
        None,                                    // 0x6a3
        None,                                    // 0x6b0
        MOp_L_M32B_or_M16B,                      // 0x6b1 vpackssdw
        None,                                    // 0x6b2
        None,                                    // 0x6b3
        None,                                    // 0x6c0
        MOp_L_M32B_or_M16B,                      // 0x6c1 vpunpcklqdq
        None,                                    // 0x6c2
        None,                                    // 0x6c3
        None,                                    // 0x6d0
        MOp_L_M32B_or_M16B,                      // 0x6d1 vpunpckhqdq
        None,                                    // 0x6d2
        None,                                    // 0x6d3
        None,                                    // 0x6e0
        MOp_W_M8B_or_M4B,                        // 0x6e1 vmovd,vmovq
        None,                                    // 0x6e2
        None,                                    // 0x6e3
        None,                                    // 0x6f0
        MOp_L_M32B_or_M16B,                      // 0x6f1 vmovdqa
        MOp_L_M32B_or_M16B,                      // 0x6f2 vmovdqu
        None,                                    // 0x6f3
        None,                                    // 0x700
        MOp_I1B_L_M32B_or_M16B,                  // 0x701 vpshufd
        MOp_I1B_L_M32B_or_M16B,                  // 0x702 vpshufhw
        MOp_I1B_L_M32B_or_M16B,                  // 0x703 vpshuflw
        None,                                    // 0x710
        None,                                    // 0x711
        None,                                    // 0x712
        None,                                    // 0x713
        None,                                    // 0x720
        None,                                    // 0x721
        None,                                    // 0x722
        None,                                    // 0x723
        None,                                    // 0x730
        None,                                    // 0x731
        None,                                    // 0x732
        None,                                    // 0x733
        None,                                    // 0x740
        MOp_L_M32B_or_M16B,                      // 0x741 vpcmpeqb
        None,                                    // 0x742
        None,                                    // 0x743
        None,                                    // 0x750
        MOp_L_M32B_or_M16B,                      // 0x751 vpcmpeqw
        None,                                    // 0x752
        None,                                    // 0x753
        None,                                    // 0x760
        MOp_L_M32B_or_M16B,                      // 0x761 vpcmpeqd
        None,                                    // 0x762
        None,                                    // 0x763
        None,                                    // 0x770 vzeroall,vzeroupper
        None,                                    // 0x771
        None,                                    // 0x772
        None,                                    // 0x773
        None,                                    // 0x780
        None,                                    // 0x781
        None,                                    // 0x782
        None,                                    // 0x783
        None,                                    // 0x790
        None,                                    // 0x791
        None,                                    // 0x792
        None,                                    // 0x793
        None,                                    // 0x7a0
        None,                                    // 0x7a1
        None,                                    // 0x7a2
        None,                                    // 0x7a3
        None,                                    // 0x7b0
        None,                                    // 0x7b1
        None,                                    // 0x7b2
        None,                                    // 0x7b3
        None,                                    // 0x7c0
        MOp_L_M32B_or_M16B,                      // 0x7c1 vhaddpd
        None,                                    // 0x7c2
        MOp_L_M32B_or_M16B,                      // 0x7c3 vhaddps
        None,                                    // 0x7d0
        MOp_L_M32B_or_M16B,                      // 0x7d1 vhsubpd
        None,                                    // 0x7d2
        MOp_L_M32B_or_M16B,                      // 0x7d3 vhsubps
        None,                                    // 0x7e0
        M1st_W_M8B_or_M4B,                       // 0x7e1 vmovd,vmovq
        MOp_M8B,                                 // 0x7e2 vmovq
        None,                                    // 0x7e3
        None,                                    // 0x7f0
        M1st_L_M32B_or_M16B,                     // 0x7f1 vmovdqa
        M1st_L_M32B_or_M16B,                     // 0x7f2 vmovdqu
        None,                                    // 0x7f3
        None,                                    // 0x800
        None,                                    // 0x801
        None,                                    // 0x802
        None,                                    // 0x803
        None,                                    // 0x810
        None,                                    // 0x811
        None,                                    // 0x812
        None,                                    // 0x813
        None,                                    // 0x820
        None,                                    // 0x821
        None,                                    // 0x822
        None,                                    // 0x823
        None,                                    // 0x830
        None,                                    // 0x831
        None,                                    // 0x832
        None,                                    // 0x833
        None,                                    // 0x840
        None,                                    // 0x841
        None,                                    // 0x842
        None,                                    // 0x843
        None,                                    // 0x850
        None,                                    // 0x851
        None,                                    // 0x852
        None,                                    // 0x853
        None,                                    // 0x860
        None,                                    // 0x861
        None,                                    // 0x862
        None,                                    // 0x863
        None,                                    // 0x870
        None,                                    // 0x871
        None,                                    // 0x872
        None,                                    // 0x873
        None,                                    // 0x880
        None,                                    // 0x881
        None,                                    // 0x882
        None,                                    // 0x883
        None,                                    // 0x890
        None,                                    // 0x891
        None,                                    // 0x892
        None,                                    // 0x893
        None,                                    // 0x8a0
        None,                                    // 0x8a1
        None,                                    // 0x8a2
        None,                                    // 0x8a3
        None,                                    // 0x8b0
        None,                                    // 0x8b1
        None,                                    // 0x8b2
        None,                                    // 0x8b3
        None,                                    // 0x8c0
        None,                                    // 0x8c1
        None,                                    // 0x8c2
        None,                                    // 0x8c3
        None,                                    // 0x8d0
        None,                                    // 0x8d1
        None,                                    // 0x8d2
        None,                                    // 0x8d3
        None,                                    // 0x8e0
        None,                                    // 0x8e1
        None,                                    // 0x8e2
        None,                                    // 0x8e3
        None,                                    // 0x8f0
        None,                                    // 0x8f1
        None,                                    // 0x8f2
        None,                                    // 0x8f3
        MOp_W_M8B_or_M2B,                        // 0x900 kmovq,kmovw
        MOp_W_M4B_or_M1B,                        // 0x901 kmovb,kmovd
        None,                                    // 0x902
        None,                                    // 0x903
        M1st_W_M8B_or_M2B,                       // 0x910 kmovq,kmovw
        M1st_W_M4B_or_M1B,                       // 0x911 kmovb,kmovd
        None,                                    // 0x912
        None,                                    // 0x913
        None,                                    // 0x920
        None,                                    // 0x921
        None,                                    // 0x922
        None,                                    // 0x923
        None,                                    // 0x930
        None,                                    // 0x931
        None,                                    // 0x932
        None,                                    // 0x933
        None,                                    // 0x940
        None,                                    // 0x941
        None,                                    // 0x942
        None,                                    // 0x943
        None,                                    // 0x950
        None,                                    // 0x951
        None,                                    // 0x952
        None,                                    // 0x953
        None,                                    // 0x960
        None,                                    // 0x961
        None,                                    // 0x962
        None,                                    // 0x963
        None,                                    // 0x970
        None,                                    // 0x971
        None,                                    // 0x972
        None,                                    // 0x973
        None,                                    // 0x980
        None,                                    // 0x981
        None,                                    // 0x982
        None,                                    // 0x983
        None,                                    // 0x990
        None,                                    // 0x991
        None,                                    // 0x992
        None,                                    // 0x993
        None,                                    // 0x9a0
        None,                                    // 0x9a1
        None,                                    // 0x9a2
        None,                                    // 0x9a3
        None,                                    // 0x9b0
        None,                                    // 0x9b1
        None,                                    // 0x9b2
        None,                                    // 0x9b3
        None,                                    // 0x9c0
        None,                                    // 0x9c1
        None,                                    // 0x9c2
        None,                                    // 0x9c3
        None,                                    // 0x9d0
        None,                                    // 0x9d1
        None,                                    // 0x9d2
        None,                                    // 0x9d3
        None,                                    // 0x9e0
        None,                                    // 0x9e1
        None,                                    // 0x9e2
        None,                                    // 0x9e3
        None,                                    // 0x9f0
        None,                                    // 0x9f1
        None,                                    // 0x9f2
        None,                                    // 0x9f3
        None,                                    // 0xa00
        None,                                    // 0xa01
        None,                                    // 0xa02
        None,                                    // 0xa03
        None,                                    // 0xa10
        None,                                    // 0xa11
        None,                                    // 0xa12
        None,                                    // 0xa13
        None,                                    // 0xa20
        None,                                    // 0xa21
        None,                                    // 0xa22
        None,                                    // 0xa23
        None,                                    // 0xa30
        None,                                    // 0xa31
        None,                                    // 0xa32
        None,                                    // 0xa33
        None,                                    // 0xa40
        None,                                    // 0xa41
        None,                                    // 0xa42
        None,                                    // 0xa43
        None,                                    // 0xa50
        None,                                    // 0xa51
        None,                                    // 0xa52
        None,                                    // 0xa53
        None,                                    // 0xa60
        None,                                    // 0xa61
        None,                                    // 0xa62
        None,                                    // 0xa63
        None,                                    // 0xa70
        None,                                    // 0xa71
        None,                                    // 0xa72
        None,                                    // 0xa73
        None,                                    // 0xa80
        None,                                    // 0xa81
        None,                                    // 0xa82
        None,                                    // 0xa83
        None,                                    // 0xa90
        None,                                    // 0xa91
        None,                                    // 0xa92
        None,                                    // 0xa93
        None,                                    // 0xaa0
        None,                                    // 0xaa1
        None,                                    // 0xaa2
        None,                                    // 0xaa3
        None,                                    // 0xab0
        None,                                    // 0xab1
        None,                                    // 0xab2
        None,                                    // 0xab3
        None,                                    // 0xac0
        None,                                    // 0xac1
        None,                                    // 0xac2
        None,                                    // 0xac3
        None,                                    // 0xad0
        None,                                    // 0xad1
        None,                                    // 0xad2
        None,                                    // 0xad3
        MOnly_M4B,                               // 0xae0 vldmxcsr,vstmxcsr
        MOnly_M4B,                               // 0xae1 vldmxcsr,vstmxcsr
        MOnly_M4B,                               // 0xae2 vldmxcsr,vstmxcsr
        MOnly_M4B,                               // 0xae3 vldmxcsr,vstmxcsr
        None,                                    // 0xaf0
        None,                                    // 0xaf1
        None,                                    // 0xaf2
        None,                                    // 0xaf3
        None,                                    // 0xb00
        None,                                    // 0xb01
        None,                                    // 0xb02
        None,                                    // 0xb03
        None,                                    // 0xb10
        None,                                    // 0xb11
        None,                                    // 0xb12
        None,                                    // 0xb13
        None,                                    // 0xb20
        None,                                    // 0xb21
        None,                                    // 0xb22
        None,                                    // 0xb23
        None,                                    // 0xb30
        None,                                    // 0xb31
        None,                                    // 0xb32
        None,                                    // 0xb33
        None,                                    // 0xb40
        None,                                    // 0xb41
        None,                                    // 0xb42
        None,                                    // 0xb43
        None,                                    // 0xb50
        None,                                    // 0xb51
        None,                                    // 0xb52
        None,                                    // 0xb53
        None,                                    // 0xb60
        None,                                    // 0xb61
        None,                                    // 0xb62
        None,                                    // 0xb63
        None,                                    // 0xb70
        None,                                    // 0xb71
        None,                                    // 0xb72
        None,                                    // 0xb73
        None,                                    // 0xb80
        None,                                    // 0xb81
        None,                                    // 0xb82
        None,                                    // 0xb83
        None,                                    // 0xb90
        None,                                    // 0xb91
        None,                                    // 0xb92
        None,                                    // 0xb93
        None,                                    // 0xba0
        None,                                    // 0xba1
        None,                                    // 0xba2
        None,                                    // 0xba3
        None,                                    // 0xbb0
        None,                                    // 0xbb1
        None,                                    // 0xbb2
        None,                                    // 0xbb3
        None,                                    // 0xbc0
        None,                                    // 0xbc1
        None,                                    // 0xbc2
        None,                                    // 0xbc3
        None,                                    // 0xbd0
        None,                                    // 0xbd1
        None,                                    // 0xbd2
        None,                                    // 0xbd3
        None,                                    // 0xbe0
        None,                                    // 0xbe1
        None,                                    // 0xbe2
        None,                                    // 0xbe3
        None,                                    // 0xbf0
        None,                                    // 0xbf1
        None,                                    // 0xbf2
        None,                                    // 0xbf3
        None,                                    // 0xc00
        None,                                    // 0xc01
        None,                                    // 0xc02
        None,                                    // 0xc03
        None,                                    // 0xc10
        None,                                    // 0xc11
        None,                                    // 0xc12
        None,                                    // 0xc13
        MOp_I1B_L_M32B_or_M16B,                  // 0xc20 vcmpps
        MOp_I1B_L_M32B_or_M16B,                  // 0xc21 vcmppd
        MOp_M4B_I1B,                             // 0xc22 vcmpss
        MOp_M8B_I1B,                             // 0xc23 vcmpsd
        None,                                    // 0xc30
        None,                                    // 0xc31
        None,                                    // 0xc32
        None,                                    // 0xc33
        None,                                    // 0xc40
        MOp_M2B_I1B,                             // 0xc41 vpinsrw
        None,                                    // 0xc42
        None,                                    // 0xc43
        None,                                    // 0xc50
        None,                                    // 0xc51
        None,                                    // 0xc52
        None,                                    // 0xc53
        MOp_I1B_L_M32B_or_M16B,                  // 0xc60 vshufps
        MOp_I1B_L_M32B_or_M16B,                  // 0xc61 vshufpd
        MOp_I1B_L_M32B_or_M16B,                  // 0xc62 vshufps
        MOp_I1B_L_M32B_or_M16B,                  // 0xc63 vshufps
        None,                                    // 0xc70
        None,                                    // 0xc71
        None,                                    // 0xc72
        None,                                    // 0xc73
        None,                                    // 0xc80
        None,                                    // 0xc81
        None,                                    // 0xc82
        None,                                    // 0xc83
        None,                                    // 0xc90
        None,                                    // 0xc91
        None,                                    // 0xc92
        None,                                    // 0xc93
        None,                                    // 0xca0
        None,                                    // 0xca1
        None,                                    // 0xca2
        None,                                    // 0xca3
        None,                                    // 0xcb0
        None,                                    // 0xcb1
        None,                                    // 0xcb2
        None,                                    // 0xcb3
        None,                                    // 0xcc0
        None,                                    // 0xcc1
        None,                                    // 0xcc2
        None,                                    // 0xcc3
        None,                                    // 0xcd0
        None,                                    // 0xcd1
        None,                                    // 0xcd2
        None,                                    // 0xcd3
        None,                                    // 0xce0
        None,                                    // 0xce1
        None,                                    // 0xce2
        None,                                    // 0xce3
        None,                                    // 0xcf0
        None,                                    // 0xcf1
        None,                                    // 0xcf2
        None,                                    // 0xcf3
        None,                                    // 0xd00
        MOp_L_M32B_or_M16B,                      // 0xd01 vaddsubpd
        None,                                    // 0xd02
        MOp_L_M32B_or_M16B,                      // 0xd03 vaddsubps
        None,                                    // 0xd10
        MOp_M16B,                                // 0xd11 vpsrlw
        None,                                    // 0xd12
        None,                                    // 0xd13
        None,                                    // 0xd20
        MOp_M16B,                                // 0xd21 vpsrld
        None,                                    // 0xd22
        None,                                    // 0xd23
        None,                                    // 0xd30
        MOp_M16B,                                // 0xd31 vpsrlq
        None,                                    // 0xd32
        None,                                    // 0xd33
        None,                                    // 0xd40
        MOp_L_M32B_or_M16B,                      // 0xd41 vpaddq
        None,                                    // 0xd42
        None,                                    // 0xd43
        None,                                    // 0xd50
        MOp_L_M32B_or_M16B,                      // 0xd51 vpmullw
        None,                                    // 0xd52
        None,                                    // 0xd53
        None,                                    // 0xd60
        M1st_M8B,                                // 0xd61 vmovq
        None,                                    // 0xd62
        None,                                    // 0xd63
        None,                                    // 0xd70
        None,                                    // 0xd71
        None,                                    // 0xd72
        None,                                    // 0xd73
        None,                                    // 0xd80
        MOp_L_M32B_or_M16B,                      // 0xd81 vpsubusb
        None,                                    // 0xd82
        None,                                    // 0xd83
        None,                                    // 0xd90
        MOp_L_M32B_or_M16B,                      // 0xd91 vpsubusw
        None,                                    // 0xd92
        None,                                    // 0xd93
        None,                                    // 0xda0
        MOp_L_M32B_or_M16B,                      // 0xda1 vpminub
        None,                                    // 0xda2
        None,                                    // 0xda3
        None,                                    // 0xdb0
        MOp_L_M32B_or_M16B,                      // 0xdb1 vpand
        None,                                    // 0xdb2
        None,                                    // 0xdb3
        None,                                    // 0xdc0
        MOp_L_M32B_or_M16B,                      // 0xdc1 vpaddusb
        None,                                    // 0xdc2
        None,                                    // 0xdc3
        None,                                    // 0xdd0
        MOp_L_M32B_or_M16B,                      // 0xdd1 vpaddusw
        None,                                    // 0xdd2
        None,                                    // 0xdd3
        None,                                    // 0xde0
        MOp_L_M32B_or_M16B,                      // 0xde1 vpmaxub
        None,                                    // 0xde2
        None,                                    // 0xde3
        None,                                    // 0xdf0
        MOp_L_M32B_or_M16B,                      // 0xdf1 vpandn
        None,                                    // 0xdf2
        None,                                    // 0xdf3
        None,                                    // 0xe00
        MOp_L_M32B_or_M16B,                      // 0xe01 vpavgb
        None,                                    // 0xe02
        None,                                    // 0xe03
        None,                                    // 0xe10
        MOp_M16B,                                // 0xe11 vpsraw
        None,                                    // 0xe12
        None,                                    // 0xe13
        None,                                    // 0xe20
        MOp_M16B,                                // 0xe21 vpsrad
        None,                                    // 0xe22
        None,                                    // 0xe23
        None,                                    // 0xe30
        MOp_L_M32B_or_M16B,                      // 0xe31 vpavgw
        None,                                    // 0xe32
        None,                                    // 0xe33
        None,                                    // 0xe40
        MOp_L_M32B_or_M16B,                      // 0xe41 vpmulhuw
        None,                                    // 0xe42
        None,                                    // 0xe43
        None,                                    // 0xe50
        MOp_L_M32B_or_M16B,                      // 0xe51 vpmulhw
        None,                                    // 0xe52
        None,                                    // 0xe53
        None,                                    // 0xe60
        MOp_L_M32B_or_M16B,                      // 0xe61 vcvttpd2dq
        MOp_L_M16B_or_M8B,                       // 0xe62 vcvtdq2pd
        MOp_L_M32B_or_M16B,                      // 0xe63 vcvtpd2dq
        None,                                    // 0xe70
        M1st_L_M32B_or_M16B,                     // 0xe71 vmovntdq
        None,                                    // 0xe72
        None,                                    // 0xe73
        None,                                    // 0xe80
        MOp_L_M32B_or_M16B,                      // 0xe81 vpsubsb
        None,                                    // 0xe82
        None,                                    // 0xe83
        None,                                    // 0xe90
        MOp_L_M32B_or_M16B,                      // 0xe91 vpsubsw
        None,                                    // 0xe92
        None,                                    // 0xe93
        None,                                    // 0xea0
        MOp_L_M32B_or_M16B,                      // 0xea1 vpminsw
        None,                                    // 0xea2
        None,                                    // 0xea3
        None,                                    // 0xeb0
        MOp_L_M32B_or_M16B,                      // 0xeb1 vpor
        None,                                    // 0xeb2
        None,                                    // 0xeb3
        None,                                    // 0xec0
        MOp_L_M32B_or_M16B,                      // 0xec1 vpaddsb
        None,                                    // 0xec2
        None,                                    // 0xec3
        None,                                    // 0xed0
        MOp_L_M32B_or_M16B,                      // 0xed1 vpaddsw
        None,                                    // 0xed2
        None,                                    // 0xed3
        None,                                    // 0xee0
        MOp_L_M32B_or_M16B,                      // 0xee1 vpmaxsw
        None,                                    // 0xee2
        None,                                    // 0xee3
        None,                                    // 0xef0
        MOp_L_M32B_or_M16B,                      // 0xef1 vpxor
        None,                                    // 0xef2
        None,                                    // 0xef3
        None,                                    // 0xf00
        None,                                    // 0xf01
        None,                                    // 0xf02
        MOp_L_M32B_or_M16B,                      // 0xf03 vlddqu
        None,                                    // 0xf10
        MOp_M16B,                                // 0xf11 vpsllw
        None,                                    // 0xf12
        None,                                    // 0xf13
        None,                                    // 0xf20
        MOp_M16B,                                // 0xf21 vpslld
        None,                                    // 0xf22
        None,                                    // 0xf23
        None,                                    // 0xf30
        MOp_M16B,                                // 0xf31 vpsllq
        None,                                    // 0xf32
        None,                                    // 0xf33
        None,                                    // 0xf40
        MOp_L_M32B_or_M16B,                      // 0xf41 vpmuludq
        None,                                    // 0xf42
        None,                                    // 0xf43
        None,                                    // 0xf50
        MOp_L_M32B_or_M16B,                      // 0xf51 vpmaddwd
        None,                                    // 0xf52
        None,                                    // 0xf53
        None,                                    // 0xf60
        MOp_L_M32B_or_M16B,                      // 0xf61 vpsadbw
        None,                                    // 0xf62
        None,                                    // 0xf63
        None,                                    // 0xf70
        None,                                    // 0xf71
        None,                                    // 0xf72
        None,                                    // 0xf73
        None,                                    // 0xf80
        MOp_L_M32B_or_M16B,                      // 0xf81 vpsubb
        None,                                    // 0xf82
        None,                                    // 0xf83
        None,                                    // 0xf90
        MOp_L_M32B_or_M16B,                      // 0xf91 vpsubw
        None,                                    // 0xf92
        None,                                    // 0xf93
        None,                                    // 0xfa0
        MOp_L_M32B_or_M16B,                      // 0xfa1 vpsubd
        None,                                    // 0xfa2
        None,                                    // 0xfa3
        None,                                    // 0xfb0
        MOp_L_M32B_or_M16B,                      // 0xfb1 vpsubq
        None,                                    // 0xfb2
        None,                                    // 0xfb3
        None,                                    // 0xfc0
        MOp_L_M32B_or_M16B,                      // 0xfc1 vpaddb
        None,                                    // 0xfc2
        None,                                    // 0xfc3
        None,                                    // 0xfd0
        MOp_L_M32B_or_M16B,                      // 0xfd1 vpaddw
        None,                                    // 0xfd2
        None,                                    // 0xfd3
        None,                                    // 0xfe0
        MOp_L_M32B_or_M16B,                      // 0xfe1 vpaddd
        None,                                    // 0xfe2
        None,                                    // 0xfe3
        None,                                    // 0xff0
        None,                                    // 0xff1
        None,                                    // 0xff2
        None,                                    // 0xff3
    };

    static const InstrForm instrFormVex2[1024]
    {
        None,                                    // 0x000
        MOp_L_M32B_or_M16B,                      // 0x001 vpshufb
        None,                                    // 0x002
        None,                                    // 0x003
        None,                                    // 0x010
        MOp_L_M32B_or_M16B,                      // 0x011 vphaddw
        None,                                    // 0x012
        None,                                    // 0x013
        None,                                    // 0x020
        MOp_L_M32B_or_M16B,                      // 0x021 vphaddd
        None,                                    // 0x022
        None,                                    // 0x023
        None,                                    // 0x030
        MOp_L_M32B_or_M16B,                      // 0x031 vphaddsw
        None,                                    // 0x032
        None,                                    // 0x033
        None,                                    // 0x040
        MOp_L_M32B_or_M16B,                      // 0x041 vpmaddubsw
        None,                                    // 0x042
        None,                                    // 0x043
        None,                                    // 0x050
        MOp_L_M32B_or_M16B,                      // 0x051 vphsubw
        None,                                    // 0x052
        None,                                    // 0x053
        None,                                    // 0x060
        MOp_L_M32B_or_M16B,                      // 0x061 vphsubd
        None,                                    // 0x062
        None,                                    // 0x063
        None,                                    // 0x070
        MOp_L_M32B_or_M16B,                      // 0x071 vphsubsw
        None,                                    // 0x072
        None,                                    // 0x073
        None,                                    // 0x080
        MOp_L_M32B_or_M16B,                      // 0x081 vpsignb
        None,                                    // 0x082
        None,                                    // 0x083
        None,                                    // 0x090
        MOp_L_M32B_or_M16B,                      // 0x091 vpsignw
        None,                                    // 0x092
        None,                                    // 0x093
        None,                                    // 0x0a0
        MOp_L_M32B_or_M16B,                      // 0x0a1 vpsignd
        None,                                    // 0x0a2
        None,                                    // 0x0a3
        None,                                    // 0x0b0
        MOp_L_M32B_or_M16B,                      // 0x0b1 vpmulhrsw
        None,                                    // 0x0b2
        None,                                    // 0x0b3
        None,                                    // 0x0c0
        MOp_L_M32B_or_M16B,                      // 0x0c1 vpermilps
        None,                                    // 0x0c2
        None,                                    // 0x0c3
        None,                                    // 0x0d0
        MOp_L_M32B_or_M16B,                      // 0x0d1 vpermilpd
        None,                                    // 0x0d2
        None,                                    // 0x0d3
        None,                                    // 0x0e0
        MOp_L_M32B_or_M16B,                      // 0x0e1 vtestps
        None,                                    // 0x0e2
        None,                                    // 0x0e3
        None,                                    // 0x0f0
        MOp_L_M32B_or_M16B,                      // 0x0f1 vtestpd
        None,                                    // 0x0f2
        None,                                    // 0x0f3
        None,                                    // 0x100
        None,                                    // 0x101
        None,                                    // 0x102
        None,                                    // 0x103
        None,                                    // 0x110
        None,                                    // 0x111
        None,                                    // 0x112
        None,                                    // 0x113
        None,                                    // 0x120
        None,                                    // 0x121
        None,                                    // 0x122
        None,                                    // 0x123
        None,                                    // 0x130
        MOp_L_M16B_or_M8B,                       // 0x131 vcvtph2ps
        None,                                    // 0x132
        None,                                    // 0x133
        None,                                    // 0x140
        None,                                    // 0x141
        None,                                    // 0x142
        None,                                    // 0x143
        None,                                    // 0x150
        None,                                    // 0x151
        None,                                    // 0x152
        None,                                    // 0x153
        None,                                    // 0x160
        MOp_M32B,                                // 0x161 vpermps
        None,                                    // 0x162
        None,                                    // 0x163
        None,                                    // 0x170
        MOp_L_M32B_or_M16B,                      // 0x171 vptest
        None,                                    // 0x172
        None,                                    // 0x173
        None,                                    // 0x180
        MOp_M4B,                                 // 0x181 vbroadcastss
        None,                                    // 0x182
        None,                                    // 0x183
        None,                                    // 0x190
        MOp_M8B,                                 // 0x191 vbroadcastsd
        None,                                    // 0x192
        None,                                    // 0x193
        None,                                    // 0x1a0
        MOp_M16B,                                // 0x1a1 vbroadcastf128
        None,                                    // 0x1a2
        None,                                    // 0x1a3
        None,                                    // 0x1b0
        None,                                    // 0x1b1
        None,                                    // 0x1b2
        None,                                    // 0x1b3
        None,                                    // 0x1c0
        MOp_L_M32B_or_M16B,                      // 0x1c1 vpabsb
        None,                                    // 0x1c2
        None,                                    // 0x1c3
        None,                                    // 0x1d0
        MOp_L_M32B_or_M16B,                      // 0x1d1 vpabsw
        None,                                    // 0x1d2
        None,                                    // 0x1d3
        None,                                    // 0x1e0
        MOp_L_M32B_or_M16B,                      // 0x1e1 vpabsd
        None,                                    // 0x1e2
        None,                                    // 0x1e3
        None,                                    // 0x1f0
        None,                                    // 0x1f1
        None,                                    // 0x1f2
        None,                                    // 0x1f3
        None,                                    // 0x200
        MOp_L_M16B_or_M8B,                       // 0x201 vpmovsxbw
        None,                                    // 0x202
        None,                                    // 0x203
        None,                                    // 0x210
        MOp_L_M8B_or_M4B,                        // 0x211 vpmovsxbd
        None,                                    // 0x212
        None,                                    // 0x213
        None,                                    // 0x220
        MOp_L_M4B_or_M2B,                        // 0x221 vpmovsxbq
        None,                                    // 0x222
        None,                                    // 0x223
        None,                                    // 0x230
        MOp_L_M16B_or_M8B,                       // 0x231 vpmovsxwd
        None,                                    // 0x232
        None,                                    // 0x233
        None,                                    // 0x240
        MOp_L_M8B_or_M4B,                        // 0x241 vpmovsxwq
        None,                                    // 0x242
        None,                                    // 0x243
        None,                                    // 0x250
        MOp_L_M16B_or_M8B,                       // 0x251 vpmovsxdq
        None,                                    // 0x252
        None,                                    // 0x253
        None,                                    // 0x260
        None,                                    // 0x261
        None,                                    // 0x262
        None,                                    // 0x263
        None,                                    // 0x270
        None,                                    // 0x271
        None,                                    // 0x272
        None,                                    // 0x273
        None,                                    // 0x280
        MOp_L_M32B_or_M16B,                      // 0x281 vpmuldq
        None,                                    // 0x282
        None,                                    // 0x283
        None,                                    // 0x290
        MOp_L_M32B_or_M16B,                      // 0x291 vpcmpeqq
        None,                                    // 0x292
        None,                                    // 0x293
        None,                                    // 0x2a0
        MOp_L_M32B_or_M16B,                      // 0x2a1 vmovntdqa
        None,                                    // 0x2a2
        None,                                    // 0x2a3
        None,                                    // 0x2b0
        MOp_L_M32B_or_M16B,                      // 0x2b1 vpackusdw
        None,                                    // 0x2b2
        None,                                    // 0x2b3
        None,                                    // 0x2c0
        MOp_L_M32B_or_M16B,                      // 0x2c1 vmaskmovps
        None,                                    // 0x2c2
        None,                                    // 0x2c3
        None,                                    // 0x2d0
        MOp_L_M32B_or_M16B,                      // 0x2d1 vmaskmovpd
        None,                                    // 0x2d2
        None,                                    // 0x2d3
        None,                                    // 0x2e0
        M1st_L_M32B_or_M16B,                     // 0x2e1 vmaskmovps
        None,                                    // 0x2e2
        None,                                    // 0x2e3
        None,                                    // 0x2f0
        M1st_L_M32B_or_M16B,                     // 0x2f1 vmaskmovpd
        None,                                    // 0x2f2
        None,                                    // 0x2f3
        None,                                    // 0x300
        MOp_L_M16B_or_M8B,                       // 0x301 vpmovzxbw
        None,                                    // 0x302
        None,                                    // 0x303
        None,                                    // 0x310
        MOp_L_M8B_or_M4B,                        // 0x311 vpmovzxbd
        None,                                    // 0x312
        None,                                    // 0x313
        None,                                    // 0x320
        MOp_L_M4B_or_M2B,                        // 0x321 vpmovzxbq
        None,                                    // 0x322
        None,                                    // 0x323
        None,                                    // 0x330
        MOp_L_M16B_or_M8B,                       // 0x331 vpmovzxwd
        None,                                    // 0x332
        None,                                    // 0x333
        None,                                    // 0x340
        MOp_L_M8B_or_M4B,                        // 0x341 vpmovzxwq
        None,                                    // 0x342
        None,                                    // 0x343
        None,                                    // 0x350
        MOp_L_M16B_or_M8B,                       // 0x351 vpmovzxdq
        None,                                    // 0x352
        None,                                    // 0x353
        None,                                    // 0x360
        MOp_M32B,                                // 0x361 vpermd
        None,                                    // 0x362
        None,                                    // 0x363
        None,                                    // 0x370
        MOp_L_M32B_or_M16B,                      // 0x371 vpcmpgtq
        None,                                    // 0x372
        None,                                    // 0x373
        None,                                    // 0x380
        MOp_L_M32B_or_M16B,                      // 0x381 vpminsb
        None,                                    // 0x382
        None,                                    // 0x383
        None,                                    // 0x390
        MOp_L_M32B_or_M16B,                      // 0x391 vpminsd
        None,                                    // 0x392
        None,                                    // 0x393
        None,                                    // 0x3a0
        MOp_L_M32B_or_M16B,                      // 0x3a1 vpminuw
        None,                                    // 0x3a2
        None,                                    // 0x3a3
        None,                                    // 0x3b0
        MOp_L_M32B_or_M16B,                      // 0x3b1 vpminud
        None,                                    // 0x3b2
        None,                                    // 0x3b3
        None,                                    // 0x3c0
        MOp_L_M32B_or_M16B,                      // 0x3c1 vpmaxsb
        None,                                    // 0x3c2
        None,                                    // 0x3c3
        None,                                    // 0x3d0
        MOp_L_M32B_or_M16B,                      // 0x3d1 vpmaxsd
        None,                                    // 0x3d2
        None,                                    // 0x3d3
        None,                                    // 0x3e0
        MOp_L_M32B_or_M16B,                      // 0x3e1 vpmaxuw
        None,                                    // 0x3e2
        None,                                    // 0x3e3
        None,                                    // 0x3f0
        MOp_L_M32B_or_M16B,                      // 0x3f1 vpmaxud
        None,                                    // 0x3f2
        None,                                    // 0x3f3
        None,                                    // 0x400
        MOp_L_M32B_or_M16B,                      // 0x401 vpmulld
        None,                                    // 0x402
        None,                                    // 0x403
        None,                                    // 0x410
        MOp_M16B,                                // 0x411 vphminposuw
        None,                                    // 0x412
        None,                                    // 0x413
        None,                                    // 0x420
        None,                                    // 0x421
        None,                                    // 0x422
        None,                                    // 0x423
        None,                                    // 0x430
        None,                                    // 0x431
        None,                                    // 0x432
        None,                                    // 0x433
        None,                                    // 0x440
        None,                                    // 0x441
        None,                                    // 0x442
        None,                                    // 0x443
        None,                                    // 0x450
        MOp_L_M32B_or_M16B,                      // 0x451 vpsrlvd,vpsrlvq
        None,                                    // 0x452
        None,                                    // 0x453
        None,                                    // 0x460
        MOp_L_M32B_or_M16B,                      // 0x461 vpsravd
        None,                                    // 0x462
        None,                                    // 0x463
        None,                                    // 0x470
        MOp_L_M32B_or_M16B,                      // 0x471 vpsllvd,vpsllvq
        None,                                    // 0x472
        None,                                    // 0x473
        None,                                    // 0x480
        None,                                    // 0x481
        None,                                    // 0x482
        None,                                    // 0x483
        None,                                    // 0x490
        None,                                    // 0x491
        None,                                    // 0x492
        None,                                    // 0x493
        None,                                    // 0x4a0
        None,                                    // 0x4a1
        None,                                    // 0x4a2
        None,                                    // 0x4a3
        None,                                    // 0x4b0
        None,                                    // 0x4b1
        None,                                    // 0x4b2
        None,                                    // 0x4b3
        None,                                    // 0x4c0
        None,                                    // 0x4c1
        None,                                    // 0x4c2
        None,                                    // 0x4c3
        None,                                    // 0x4d0
        None,                                    // 0x4d1
        None,                                    // 0x4d2
        None,                                    // 0x4d3
        None,                                    // 0x4e0
        None,                                    // 0x4e1
        None,                                    // 0x4e2
        None,                                    // 0x4e3
        None,                                    // 0x4f0
        None,                                    // 0x4f1
        None,                                    // 0x4f2
        None,                                    // 0x4f3
        None,                                    // 0x500
        None,                                    // 0x501
        None,                                    // 0x502
        None,                                    // 0x503
        None,                                    // 0x510
        None,                                    // 0x511
        None,                                    // 0x512
        None,                                    // 0x513
        None,                                    // 0x520
        None,                                    // 0x521
        None,                                    // 0x522
        None,                                    // 0x523
        None,                                    // 0x530
        None,                                    // 0x531
        None,                                    // 0x532
        None,                                    // 0x533
        None,                                    // 0x540
        None,                                    // 0x541
        None,                                    // 0x542
        None,                                    // 0x543
        None,                                    // 0x550
        None,                                    // 0x551
        None,                                    // 0x552
        None,                                    // 0x553
        None,                                    // 0x560
        None,                                    // 0x561
        None,                                    // 0x562
        None,                                    // 0x563
        None,                                    // 0x570
        None,                                    // 0x571
        None,                                    // 0x572
        None,                                    // 0x573
        None,                                    // 0x580
        MOp_M4B,                                 // 0x581 vpbroadcastd
        None,                                    // 0x582
        None,                                    // 0x583
        None,                                    // 0x590
        MOp_M8B,                                 // 0x591 vpbroadcastq
        None,                                    // 0x592
        None,                                    // 0x593
        None,                                    // 0x5a0
        MOp_M16B,                                // 0x5a1 vbroadcasti128
        None,                                    // 0x5a2
        None,                                    // 0x5a3
        None,                                    // 0x5b0
        None,                                    // 0x5b1
        None,                                    // 0x5b2
        None,                                    // 0x5b3
        None,                                    // 0x5c0
        None,                                    // 0x5c1
        None,                                    // 0x5c2
        None,                                    // 0x5c3
        None,                                    // 0x5d0
        None,                                    // 0x5d1
        None,                                    // 0x5d2
        None,                                    // 0x5d3
        None,                                    // 0x5e0
        None,                                    // 0x5e1
        None,                                    // 0x5e2
        None,                                    // 0x5e3
        None,                                    // 0x5f0
        None,                                    // 0x5f1
        None,                                    // 0x5f2
        None,                                    // 0x5f3
        None,                                    // 0x600
        None,                                    // 0x601
        None,                                    // 0x602
        None,                                    // 0x603
        None,                                    // 0x610
        None,                                    // 0x611
        None,                                    // 0x612
        None,                                    // 0x613
        None,                                    // 0x620
        None,                                    // 0x621
        None,                                    // 0x622
        None,                                    // 0x623
        None,                                    // 0x630
        None,                                    // 0x631
        None,                                    // 0x632
        None,                                    // 0x633
        None,                                    // 0x640
        None,                                    // 0x641
        None,                                    // 0x642
        None,                                    // 0x643
        None,                                    // 0x650
        None,                                    // 0x651
        None,                                    // 0x652
        None,                                    // 0x653
        None,                                    // 0x660
        None,                                    // 0x661
        None,                                    // 0x662
        None,                                    // 0x663
        None,                                    // 0x670
        None,                                    // 0x671
        None,                                    // 0x672
        None,                                    // 0x673
        None,                                    // 0x680
        None,                                    // 0x681
        None,                                    // 0x682
        None,                                    // 0x683
        None,                                    // 0x690
        None,                                    // 0x691
        None,                                    // 0x692
        None,                                    // 0x693
        None,                                    // 0x6a0
        None,                                    // 0x6a1
        None,                                    // 0x6a2
        None,                                    // 0x6a3
        None,                                    // 0x6b0
        None,                                    // 0x6b1
        None,                                    // 0x6b2
        None,                                    // 0x6b3
        None,                                    // 0x6c0
        None,                                    // 0x6c1
        None,                                    // 0x6c2
        None,                                    // 0x6c3
        None,                                    // 0x6d0
        None,                                    // 0x6d1
        None,                                    // 0x6d2
        None,                                    // 0x6d3
        None,                                    // 0x6e0
        None,                                    // 0x6e1
        None,                                    // 0x6e2
        None,                                    // 0x6e3
        None,                                    // 0x6f0
        None,                                    // 0x6f1
        None,                                    // 0x6f2
        None,                                    // 0x6f3
        None,                                    // 0x700
        None,                                    // 0x701
        None,                                    // 0x702
        None,                                    // 0x703
        None,                                    // 0x710
        None,                                    // 0x711
        None,                                    // 0x712
        None,                                    // 0x713
        None,                                    // 0x720
        None,                                    // 0x721
        None,                                    // 0x722
        None,                                    // 0x723
        None,                                    // 0x730
        None,                                    // 0x731
        None,                                    // 0x732
        None,                                    // 0x733
        None,                                    // 0x740
        None,                                    // 0x741
        None,                                    // 0x742
        None,                                    // 0x743
        None,                                    // 0x750
        None,                                    // 0x751
        None,                                    // 0x752
        None,                                    // 0x753
        None,                                    // 0x760
        None,                                    // 0x761
        None,                                    // 0x762
        None,                                    // 0x763
        None,                                    // 0x770
        None,                                    // 0x771
        None,                                    // 0x772
        None,                                    // 0x773
        None,                                    // 0x780
        MOp_M1B,                                 // 0x781 vpbroadcastb
        None,                                    // 0x782
        None,                                    // 0x783
        None,                                    // 0x790
        MOp_M2B,                                 // 0x791 vpbroadcastw
        None,                                    // 0x792
        None,                                    // 0x793
        None,                                    // 0x7a0
        None,                                    // 0x7a1
        None,                                    // 0x7a2
        None,                                    // 0x7a3
        None,                                    // 0x7b0
        None,                                    // 0x7b1
        None,                                    // 0x7b2
        None,                                    // 0x7b3
        None,                                    // 0x7c0
        None,                                    // 0x7c1
        None,                                    // 0x7c2
        None,                                    // 0x7c3
        None,                                    // 0x7d0
        None,                                    // 0x7d1
        None,                                    // 0x7d2
        None,                                    // 0x7d3
        None,                                    // 0x7e0
        None,                                    // 0x7e1
        None,                                    // 0x7e2
        None,                                    // 0x7e3
        None,                                    // 0x7f0
        None,                                    // 0x7f1
        None,                                    // 0x7f2
        None,                                    // 0x7f3
        None,                                    // 0x800
        None,                                    // 0x801
        None,                                    // 0x802
        None,                                    // 0x803
        None,                                    // 0x810
        None,                                    // 0x811
        None,                                    // 0x812
        None,                                    // 0x813
        None,                                    // 0x820
        None,                                    // 0x821
        None,                                    // 0x822
        None,                                    // 0x823
        None,                                    // 0x830
        None,                                    // 0x831
        None,                                    // 0x832
        None,                                    // 0x833
        None,                                    // 0x840
        None,                                    // 0x841
        None,                                    // 0x842
        None,                                    // 0x843
        None,                                    // 0x850
        None,                                    // 0x851
        None,                                    // 0x852
        None,                                    // 0x853
        None,                                    // 0x860
        None,                                    // 0x861
        None,                                    // 0x862
        None,                                    // 0x863
        None,                                    // 0x870
        None,                                    // 0x871
        None,                                    // 0x872
        None,                                    // 0x873
        None,                                    // 0x880
        None,                                    // 0x881
        None,                                    // 0x882
        None,                                    // 0x883
        None,                                    // 0x890
        None,                                    // 0x891
        None,                                    // 0x892
        None,                                    // 0x893
        None,                                    // 0x8a0
        None,                                    // 0x8a1
        None,                                    // 0x8a2
        None,                                    // 0x8a3
        None,                                    // 0x8b0
        None,                                    // 0x8b1
        None,                                    // 0x8b2
        None,                                    // 0x8b3
        None,                                    // 0x8c0
        MOp_L_M32B_or_M16B,                      // 0x8c1 vpmaskmovd,vpmaskmovq
        None,                                    // 0x8c2
        None,                                    // 0x8c3
        None,                                    // 0x8d0
        None,                                    // 0x8d1
        None,                                    // 0x8d2
        None,                                    // 0x8d3
        None,                                    // 0x8e0
        M1st_L_M32B_or_M16B,                     // 0x8e1 vpmaskmovd,vpmaskmovq
        None,                                    // 0x8e2
        None,                                    // 0x8e3
        None,                                    // 0x8f0
        None,                                    // 0x8f1
        None,                                    // 0x8f2
        None,                                    // 0x8f3
        None,                                    // 0x900
        MOp_W_M8B_or_M4B,                        // 0x901 vpgatherdd,vpgatherdq
        None,                                    // 0x902
        None,                                    // 0x903
        None,                                    // 0x910
        MOp_W_M8B_or_M4B,                        // 0x911 vpgatherqd,vpgatherqq
        None,                                    // 0x912
        None,                                    // 0x913
        None,                                    // 0x920
        MOp_W_M8B_or_M4B,                        // 0x921 vgatherdpd,vgatherdps
        None,                                    // 0x922
        None,                                    // 0x923
        None,                                    // 0x930
        MOp_W_M8B_or_M4B,                        // 0x931 vgatherqpd,vgatherqps
        None,                                    // 0x932
        None,                                    // 0x933
        None,                                    // 0x940
        None,                                    // 0x941
        None,                                    // 0x942
        None,                                    // 0x943
        None,                                    // 0x950
        None,                                    // 0x951
        None,                                    // 0x952
        None,                                    // 0x953
        None,                                    // 0x960
        MOp_L_M32B_or_M16B,                      // 0x961 vfmaddsub132pd,vfmaddsub132ps
        None,                                    // 0x962
        None,                                    // 0x963
        None,                                    // 0x970
        MOp_L_M32B_or_M16B,                      // 0x971 vfmsubadd132pd,vfmsubadd132ps
        None,                                    // 0x972
        None,                                    // 0x973
        None,                                    // 0x980
        MOp_L_M32B_or_M16B,                      // 0x981 vfmadd132pd,vfmadd132ps
        None,                                    // 0x982
        None,                                    // 0x983
        None,                                    // 0x990
        MOp_W_M8B_or_M4B,                        // 0x991 vfmadd132sd,vfmadd132ss
        None,                                    // 0x992
        None,                                    // 0x993
        None,                                    // 0x9a0
        MOp_L_M32B_or_M16B,                      // 0x9a1 vfmsub132pd,vfmsub132ps
        None,                                    // 0x9a2
        None,                                    // 0x9a3
        None,                                    // 0x9b0
        MOp_W_M8B_or_M4B,                        // 0x9b1 vfmsub132sd,vfmsub132ss
        None,                                    // 0x9b2
        None,                                    // 0x9b3
        None,                                    // 0x9c0
        MOp_L_M32B_or_M16B,                      // 0x9c1 vfnmadd132pd,vfnmadd132ps
        None,                                    // 0x9c2
        None,                                    // 0x9c3
        None,                                    // 0x9d0
        MOp_W_M8B_or_M4B,                        // 0x9d1 vfnmadd132sd,vfnmadd132ss
        None,                                    // 0x9d2
        None,                                    // 0x9d3
        None,                                    // 0x9e0
        MOp_L_M32B_or_M16B,                      // 0x9e1 vfnmsub132pd,vfnmsub132ps
        None,                                    // 0x9e2
        None,                                    // 0x9e3
        None,                                    // 0x9f0
        MOp_W_M8B_or_M4B,                        // 0x9f1 vfnmsub132sd,vfnmsub132ss
        None,                                    // 0x9f2
        None,                                    // 0x9f3
        None,                                    // 0xa00
        None,                                    // 0xa01
        None,                                    // 0xa02
        None,                                    // 0xa03
        None,                                    // 0xa10
        None,                                    // 0xa11
        None,                                    // 0xa12
        None,                                    // 0xa13
        None,                                    // 0xa20
        None,                                    // 0xa21
        None,                                    // 0xa22
        None,                                    // 0xa23
        None,                                    // 0xa30
        None,                                    // 0xa31
        None,                                    // 0xa32
        None,                                    // 0xa33
        None,                                    // 0xa40
        None,                                    // 0xa41
        None,                                    // 0xa42
        None,                                    // 0xa43
        None,                                    // 0xa50
        None,                                    // 0xa51
        None,                                    // 0xa52
        None,                                    // 0xa53
        None,                                    // 0xa60
        MOp_L_M32B_or_M16B,                      // 0xa61 vfmaddsub213pd,vfmaddsub213ps
        None,                                    // 0xa62
        None,                                    // 0xa63
        None,                                    // 0xa70
        MOp_L_M32B_or_M16B,                      // 0xa71 vfmsubadd213pd,vfmsubadd213ps
        None,                                    // 0xa72
        None,                                    // 0xa73
        None,                                    // 0xa80
        MOp_L_M32B_or_M16B,                      // 0xa81 vfmadd213pd,vfmadd213ps
        None,                                    // 0xa82
        None,                                    // 0xa83
        None,                                    // 0xa90
        MOp_W_M8B_or_M4B,                        // 0xa91 vfmadd213sd,vfmadd213ss
        None,                                    // 0xa92
        None,                                    // 0xa93
        None,                                    // 0xaa0
        MOp_L_M32B_or_M16B,                      // 0xaa1 vfmsub213pd,vfmsub213ps
        None,                                    // 0xaa2
        None,                                    // 0xaa3
        None,                                    // 0xab0
        MOp_W_M8B_or_M4B,                        // 0xab1 vfmsub213sd,vfmsub213ss
        None,                                    // 0xab2
        None,                                    // 0xab3
        None,                                    // 0xac0
        MOp_L_M32B_or_M16B,                      // 0xac1 vfnmadd213pd,vfnmadd213ps
        None,                                    // 0xac2
        None,                                    // 0xac3
        None,                                    // 0xad0
        MOp_W_M8B_or_M4B,                        // 0xad1 vfnmadd213sd,vfnmadd213ss
        None,                                    // 0xad2
        None,                                    // 0xad3
        None,                                    // 0xae0
        MOp_L_M32B_or_M16B,                      // 0xae1 vfnmsub213pd,vfnmsub213ps
        None,                                    // 0xae2
        None,                                    // 0xae3
        None,                                    // 0xaf0
        MOp_W_M8B_or_M4B,                        // 0xaf1 vfnmsub213sd,vfnmsub213ss
        None,                                    // 0xaf2
        None,                                    // 0xaf3
        None,                                    // 0xb00
        None,                                    // 0xb01
        None,                                    // 0xb02
        None,                                    // 0xb03
        None,                                    // 0xb10
        None,                                    // 0xb11
        None,                                    // 0xb12
        None,                                    // 0xb13
        None,                                    // 0xb20
        None,                                    // 0xb21
        None,                                    // 0xb22
        None,                                    // 0xb23
        None,                                    // 0xb30
        None,                                    // 0xb31
        None,                                    // 0xb32
        None,                                    // 0xb33
        None,                                    // 0xb40
        None,                                    // 0xb41
        None,                                    // 0xb42
        None,                                    // 0xb43
        None,                                    // 0xb50
        None,                                    // 0xb51
        None,                                    // 0xb52
        None,                                    // 0xb53
        None,                                    // 0xb60
        MOp_L_M32B_or_M16B,                      // 0xb61 vfmaddsub231pd,vfmaddsub231ps
        None,                                    // 0xb62
        None,                                    // 0xb63
        None,                                    // 0xb70
        MOp_L_M32B_or_M16B,                      // 0xb71 vfmsubadd231pd,vfmsubadd231ps
        None,                                    // 0xb72
        None,                                    // 0xb73
        None,                                    // 0xb80
        MOp_L_M32B_or_M16B,                      // 0xb81 vfmadd231pd,vfmadd231ps
        None,                                    // 0xb82
        None,                                    // 0xb83
        None,                                    // 0xb90
        MOp_W_M8B_or_M4B,                        // 0xb91 vfmadd231sd,vfmadd231ss
        None,                                    // 0xb92
        None,                                    // 0xb93
        None,                                    // 0xba0
        MOp_L_M32B_or_M16B,                      // 0xba1 vfmsub231pd,vfmsub231ps
        None,                                    // 0xba2
        None,                                    // 0xba3
        None,                                    // 0xbb0
        MOp_W_M8B_or_M4B,                        // 0xbb1 vfmsub231sd,vfmsub231ss
        None,                                    // 0xbb2
        None,                                    // 0xbb3
        None,                                    // 0xbc0
        MOp_L_M32B_or_M16B,                      // 0xbc1 vfnmadd231pd,vfnmadd231ps
        None,                                    // 0xbc2
        None,                                    // 0xbc3
        None,                                    // 0xbd0
        MOp_W_M8B_or_M4B,                        // 0xbd1 vfnmadd231sd,vfnmadd231ss
        None,                                    // 0xbd2
        None,                                    // 0xbd3
        None,                                    // 0xbe0
        MOp_L_M32B_or_M16B,                      // 0xbe1 vfnmsub231pd,vfnmsub231ps
        None,                                    // 0xbe2
        None,                                    // 0xbe3
        None,                                    // 0xbf0
        MOp_W_M8B_or_M4B,                        // 0xbf1 vfnmsub231sd,vfnmsub231ss
        None,                                    // 0xbf2
        None,                                    // 0xbf3
        None,                                    // 0xc00
        None,                                    // 0xc01
        None,                                    // 0xc02
        None,                                    // 0xc03
        None,                                    // 0xc10
        None,                                    // 0xc11
        None,                                    // 0xc12
        None,                                    // 0xc13
        None,                                    // 0xc20
        None,                                    // 0xc21
        None,                                    // 0xc22
        None,                                    // 0xc23
        None,                                    // 0xc30
        None,                                    // 0xc31
        None,                                    // 0xc32
        None,                                    // 0xc33
        None,                                    // 0xc40
        None,                                    // 0xc41
        None,                                    // 0xc42
        None,                                    // 0xc43
        None,                                    // 0xc50
        None,                                    // 0xc51
        None,                                    // 0xc52
        None,                                    // 0xc53
        None,                                    // 0xc60
        None,                                    // 0xc61
        None,                                    // 0xc62
        None,                                    // 0xc63
        None,                                    // 0xc70
        None,                                    // 0xc71
        None,                                    // 0xc72
        None,                                    // 0xc73
        None,                                    // 0xc80
        None,                                    // 0xc81
        None,                                    // 0xc82
        None,                                    // 0xc83
        None,                                    // 0xc90
        None,                                    // 0xc91
        None,                                    // 0xc92
        None,                                    // 0xc93
        None,                                    // 0xca0
        None,                                    // 0xca1
        None,                                    // 0xca2
        None,                                    // 0xca3
        None,                                    // 0xcb0
        None,                                    // 0xcb1
        None,                                    // 0xcb2
        None,                                    // 0xcb3
        None,                                    // 0xcc0
        None,                                    // 0xcc1
        None,                                    // 0xcc2
        None,                                    // 0xcc3
        None,                                    // 0xcd0
        None,                                    // 0xcd1
        None,                                    // 0xcd2
        None,                                    // 0xcd3
        None,                                    // 0xce0
        None,                                    // 0xce1
        None,                                    // 0xce2
        None,                                    // 0xce3
        None,                                    // 0xcf0
        None,                                    // 0xcf1
        None,                                    // 0xcf2
        None,                                    // 0xcf3
        None,                                    // 0xd00
        None,                                    // 0xd01
        None,                                    // 0xd02
        None,                                    // 0xd03
        None,                                    // 0xd10
        None,                                    // 0xd11
        None,                                    // 0xd12
        None,                                    // 0xd13
        None,                                    // 0xd20
        None,                                    // 0xd21
        None,                                    // 0xd22
        None,                                    // 0xd23
        None,                                    // 0xd30
        None,                                    // 0xd31
        None,                                    // 0xd32
        None,                                    // 0xd33
        None,                                    // 0xd40
        None,                                    // 0xd41
        None,                                    // 0xd42
        None,                                    // 0xd43
        None,                                    // 0xd50
        None,                                    // 0xd51
        None,                                    // 0xd52
        None,                                    // 0xd53
        None,                                    // 0xd60
        None,                                    // 0xd61
        None,                                    // 0xd62
        None,                                    // 0xd63
        None,                                    // 0xd70
        None,                                    // 0xd71
        None,                                    // 0xd72
        None,                                    // 0xd73
        None,                                    // 0xd80
        None,                                    // 0xd81
        None,                                    // 0xd82
        None,                                    // 0xd83
        None,                                    // 0xd90
        None,                                    // 0xd91
        None,                                    // 0xd92
        None,                                    // 0xd93
        None,                                    // 0xda0
        None,                                    // 0xda1
        None,                                    // 0xda2
        None,                                    // 0xda3
        None,                                    // 0xdb0
        MOp_M16B,                                // 0xdb1 vaesimc
        None,                                    // 0xdb2
        None,                                    // 0xdb3
        None,                                    // 0xdc0
        MOp_M16B,                                // 0xdc1 vaesenc
        None,                                    // 0xdc2
        None,                                    // 0xdc3
        None,                                    // 0xdd0
        MOp_M16B,                                // 0xdd1 vaesenclast
        None,                                    // 0xdd2
        None,                                    // 0xdd3
        None,                                    // 0xde0
        MOp_M16B,                                // 0xde1 vaesdec
        None,                                    // 0xde2
        None,                                    // 0xde3
        None,                                    // 0xdf0
        MOp_M16B,                                // 0xdf1 vaesdeclast
        None,                                    // 0xdf2
        None,                                    // 0xdf3
        None,                                    // 0xe00
        None,                                    // 0xe01
        None,                                    // 0xe02
        None,                                    // 0xe03
        None,                                    // 0xe10
        None,                                    // 0xe11
        None,                                    // 0xe12
        None,                                    // 0xe13
        None,                                    // 0xe20
        None,                                    // 0xe21
        None,                                    // 0xe22
        None,                                    // 0xe23
        None,                                    // 0xe30
        None,                                    // 0xe31
        None,                                    // 0xe32
        None,                                    // 0xe33
        None,                                    // 0xe40
        None,                                    // 0xe41
        None,                                    // 0xe42
        None,                                    // 0xe43
        None,                                    // 0xe50
        None,                                    // 0xe51
        None,                                    // 0xe52
        None,                                    // 0xe53
        None,                                    // 0xe60
        None,                                    // 0xe61
        None,                                    // 0xe62
        None,                                    // 0xe63
        None,                                    // 0xe70
        None,                                    // 0xe71
        None,                                    // 0xe72
        None,                                    // 0xe73
        None,                                    // 0xe80
        None,                                    // 0xe81
        None,                                    // 0xe82
        None,                                    // 0xe83
        None,                                    // 0xe90
        None,                                    // 0xe91
        None,                                    // 0xe92
        None,                                    // 0xe93
        None,                                    // 0xea0
        None,                                    // 0xea1
        None,                                    // 0xea2
        None,                                    // 0xea3
        None,                                    // 0xeb0
        None,                                    // 0xeb1
        None,                                    // 0xeb2
        None,                                    // 0xeb3
        None,                                    // 0xec0
        None,                                    // 0xec1
        None,                                    // 0xec2
        None,                                    // 0xec3
        None,                                    // 0xed0
        None,                                    // 0xed1
        None,                                    // 0xed2
        None,                                    // 0xed3
        None,                                    // 0xee0
        None,                                    // 0xee1
        None,                                    // 0xee2
        None,                                    // 0xee3
        None,                                    // 0xef0
        None,                                    // 0xef1
        None,                                    // 0xef2
        None,                                    // 0xef3
        None,                                    // 0xf00
        None,                                    // 0xf01
        None,                                    // 0xf02
        None,                                    // 0xf03
        None,                                    // 0xf10
        None,                                    // 0xf11
        None,                                    // 0xf12
        None,                                    // 0xf13
        MOp_W_M8B_or_M4B,                        // 0xf20 andn
        None,                                    // 0xf21
        None,                                    // 0xf22
        None,                                    // 0xf23
        MOp_W_M8B_or_M4B,                        // 0xf30 blsi,blsmsk,blsr
        None,                                    // 0xf31
        None,                                    // 0xf32
        None,                                    // 0xf33
        None,                                    // 0xf40
        None,                                    // 0xf41
        None,                                    // 0xf42
        None,                                    // 0xf43
        MOp_W_M8B_or_M4B,                        // 0xf50 bzhi
        None,                                    // 0xf51
        MOp_W_M8B_or_M4B,                        // 0xf52 pext
        MOp_W_M8B_or_M4B,                        // 0xf53 pdep
        None,                                    // 0xf60
        None,                                    // 0xf61
        None,                                    // 0xf62
        MOp_W_M8B_or_M4B,                        // 0xf63 mulx
        MOp_W_M8B_or_M4B,                        // 0xf70 bextr
        MOp_W_M8B_or_M4B,                        // 0xf71 shlx
        MOp_W_M8B_or_M4B,                        // 0xf72 sarx
        MOp_W_M8B_or_M4B,                        // 0xf73 shrx
        None,                                    // 0xf80
        None,                                    // 0xf81
        None,                                    // 0xf82
        None,                                    // 0xf83
        None,                                    // 0xf90
        None,                                    // 0xf91
        None,                                    // 0xf92
        None,                                    // 0xf93
        None,                                    // 0xfa0
        None,                                    // 0xfa1
        None,                                    // 0xfa2
        None,                                    // 0xfa3
        None,                                    // 0xfb0
        None,                                    // 0xfb1
        None,                                    // 0xfb2
        None,                                    // 0xfb3
        None,                                    // 0xfc0
        None,                                    // 0xfc1
        None,                                    // 0xfc2
        None,                                    // 0xfc3
        None,                                    // 0xfd0
        None,                                    // 0xfd1
        None,                                    // 0xfd2
        None,                                    // 0xfd3
        None,                                    // 0xfe0
        None,                                    // 0xfe1
        None,                                    // 0xfe2
        None,                                    // 0xfe3
        None,                                    // 0xff0
        None,                                    // 0xff1
        None,                                    // 0xff2
        None,                                    // 0xff3
    };

    static const InstrForm instrFormVex3[1024]
    {
        None,                                    // 0x000
        MOp_M32B_I1B,                            // 0x001 vpermq
        None,                                    // 0x002
        None,                                    // 0x003
        None,                                    // 0x010
        MOp_M32B_I1B,                            // 0x011 vpermpd
        None,                                    // 0x012
        None,                                    // 0x013
        None,                                    // 0x020
        MOp_I1B_L_M32B_or_M16B,                  // 0x021 vpblendd
        None,                                    // 0x022
        None,                                    // 0x023
        None,                                    // 0x030
        None,                                    // 0x031
        None,                                    // 0x032
        None,                                    // 0x033
        None,                                    // 0x040
        MOp_I1B_L_M32B_or_M16B,                  // 0x041 vpermilps
        None,                                    // 0x042
        None,                                    // 0x043
        None,                                    // 0x050
        MOp_I1B_L_M32B_or_M16B,                  // 0x051 vpermilpd
        None,                                    // 0x052
        None,                                    // 0x053
        None,                                    // 0x060
        MOp_M32B_I1B,                            // 0x061 vperm2f128
        None,                                    // 0x062
        None,                                    // 0x063
        None,                                    // 0x070
        None,                                    // 0x071
        None,                                    // 0x072
        None,                                    // 0x073
        None,                                    // 0x080
        MOp_I1B_L_M32B_or_M16B,                  // 0x081 vroundps
        None,                                    // 0x082
        None,                                    // 0x083
        None,                                    // 0x090
        MOp_I1B_L_M32B_or_M16B,                  // 0x091 vroundpd
        None,                                    // 0x092
        None,                                    // 0x093
        None,                                    // 0x0a0
        MOp_M4B_I1B,                             // 0x0a1 vroundss
        None,                                    // 0x0a2
        None,                                    // 0x0a3
        None,                                    // 0x0b0
        MOp_M8B_I1B,                             // 0x0b1 vroundsd
        None,                                    // 0x0b2
        None,                                    // 0x0b3
        None,                                    // 0x0c0
        MOp_I1B_L_M32B_or_M16B,                  // 0x0c1 vblendps
        None,                                    // 0x0c2
        None,                                    // 0x0c3
        None,                                    // 0x0d0
        MOp_I1B_L_M32B_or_M16B,                  // 0x0d1 vblendpd
        None,                                    // 0x0d2
        None,                                    // 0x0d3
        None,                                    // 0x0e0
        MOp_I1B_L_M32B_or_M16B,                  // 0x0e1 vpblendw
        None,                                    // 0x0e2
        None,                                    // 0x0e3
        None,                                    // 0x0f0
        MOp_I1B_L_M32B_or_M16B,                  // 0x0f1 vpalignr
        None,                                    // 0x0f2
        None,                                    // 0x0f3
        None,                                    // 0x100
        None,                                    // 0x101
        None,                                    // 0x102
        None,                                    // 0x103
        None,                                    // 0x110
        None,                                    // 0x111
        None,                                    // 0x112
        None,                                    // 0x113
        None,                                    // 0x120
        None,                                    // 0x121
        None,                                    // 0x122
        None,                                    // 0x123
        None,                                    // 0x130
        None,                                    // 0x131
        None,                                    // 0x132
        None,                                    // 0x133
        None,                                    // 0x140
        M1st_M1B_I1B,                            // 0x141 vpextrb
        None,                                    // 0x142
        None,                                    // 0x143
        None,                                    // 0x150
        M1st_M2B_I1B,                            // 0x151 vpextrw
        None,                                    // 0x152
        None,                                    // 0x153
        None,                                    // 0x160
        M1st_I1B_W_M8B_or_M4B,                   // 0x161 vpextrd,vpextrq
        None,                                    // 0x162
        None,                                    // 0x163
        None,                                    // 0x170
        M1st_M4B_I1B,                            // 0x171 vextractps
        None,                                    // 0x172
        None,                                    // 0x173
        None,                                    // 0x180
        MOp_M16B_I1B,                            // 0x181 vinsertf128
        None,                                    // 0x182
        None,                                    // 0x183
        None,                                    // 0x190
        M1st_M16B_I1B,                           // 0x191 vextractf128
        None,                                    // 0x192
        None,                                    // 0x193
        None,                                    // 0x1a0
        None,                                    // 0x1a1
        None,                                    // 0x1a2
        None,                                    // 0x1a3
        None,                                    // 0x1b0
        None,                                    // 0x1b1
        None,                                    // 0x1b2
        None,                                    // 0x1b3
        None,                                    // 0x1c0
        None,                                    // 0x1c1
        None,                                    // 0x1c2
        None,                                    // 0x1c3
        None,                                    // 0x1d0
        M1st_I1B_L_M16B_or_M8B,                  // 0x1d1 vcvtps2ph
        None,                                    // 0x1d2
        None,                                    // 0x1d3
        None,                                    // 0x1e0
        None,                                    // 0x1e1
        None,                                    // 0x1e2
        None,                                    // 0x1e3
        None,                                    // 0x1f0
        None,                                    // 0x1f1
        None,                                    // 0x1f2
        None,                                    // 0x1f3
        None,                                    // 0x200
        MOp_M1B_I1B,                             // 0x201 vpinsrb
        None,                                    // 0x202
        None,                                    // 0x203
        None,                                    // 0x210
        MOp_M4B_I1B,                             // 0x211 vinsertps
        None,                                    // 0x212
        None,                                    // 0x213
        None,                                    // 0x220
        MOp_I1B_W_M8B_or_M4B,                    // 0x221 vpinsrd,vpinsrq
        None,                                    // 0x222
        None,                                    // 0x223
        None,                                    // 0x230
        None,                                    // 0x231
        None,                                    // 0x232
        None,                                    // 0x233
        None,                                    // 0x240
        None,                                    // 0x241
        None,                                    // 0x242
        None,                                    // 0x243
        None,                                    // 0x250
        None,                                    // 0x251
        None,                                    // 0x252
        None,                                    // 0x253
        None,                                    // 0x260
        None,                                    // 0x261
        None,                                    // 0x262
        None,                                    // 0x263
        None,                                    // 0x270
        None,                                    // 0x271
        None,                                    // 0x272
        None,                                    // 0x273
        None,                                    // 0x280
        None,                                    // 0x281
        None,                                    // 0x282
        None,                                    // 0x283
        None,                                    // 0x290
        None,                                    // 0x291
        None,                                    // 0x292
        None,                                    // 0x293
        None,                                    // 0x2a0
        None,                                    // 0x2a1
        None,                                    // 0x2a2
        None,                                    // 0x2a3
        None,                                    // 0x2b0
        None,                                    // 0x2b1
        None,                                    // 0x2b2
        None,                                    // 0x2b3
        None,                                    // 0x2c0
        None,                                    // 0x2c1
        None,                                    // 0x2c2
        None,                                    // 0x2c3
        None,                                    // 0x2d0
        None,                                    // 0x2d1
        None,                                    // 0x2d2
        None,                                    // 0x2d3
        None,                                    // 0x2e0
        None,                                    // 0x2e1
        None,                                    // 0x2e2
        None,                                    // 0x2e3
        None,                                    // 0x2f0
        None,                                    // 0x2f1
        None,                                    // 0x2f2
        None,                                    // 0x2f3
        None,                                    // 0x300
        None,                                    // 0x301
        None,                                    // 0x302
        None,                                    // 0x303
        None,                                    // 0x310
        None,                                    // 0x311
        None,                                    // 0x312
        None,                                    // 0x313
        None,                                    // 0x320
        None,                                    // 0x321
        None,                                    // 0x322
        None,                                    // 0x323
        None,                                    // 0x330
        None,                                    // 0x331
        None,                                    // 0x332
        None,                                    // 0x333
        None,                                    // 0x340
        None,                                    // 0x341
        None,                                    // 0x342
        None,                                    // 0x343
        None,                                    // 0x350
        None,                                    // 0x351
        None,                                    // 0x352
        None,                                    // 0x353
        None,                                    // 0x360
        None,                                    // 0x361
        None,                                    // 0x362
        None,                                    // 0x363
        None,                                    // 0x370
        None,                                    // 0x371
        None,                                    // 0x372
        None,                                    // 0x373
        None,                                    // 0x380
        MOp_M16B_I1B,                            // 0x381 vinserti128
        None,                                    // 0x382
        None,                                    // 0x383
        None,                                    // 0x390
        M1st_M16B_I1B,                           // 0x391 vextracti128
        None,                                    // 0x392
        None,                                    // 0x393
        None,                                    // 0x3a0
        None,                                    // 0x3a1
        None,                                    // 0x3a2
        None,                                    // 0x3a3
        None,                                    // 0x3b0
        None,                                    // 0x3b1
        None,                                    // 0x3b2
        None,                                    // 0x3b3
        None,                                    // 0x3c0
        None,                                    // 0x3c1
        None,                                    // 0x3c2
        None,                                    // 0x3c3
        None,                                    // 0x3d0
        None,                                    // 0x3d1
        None,                                    // 0x3d2
        None,                                    // 0x3d3
        None,                                    // 0x3e0
        None,                                    // 0x3e1
        None,                                    // 0x3e2
        None,                                    // 0x3e3
        None,                                    // 0x3f0
        None,                                    // 0x3f1
        None,                                    // 0x3f2
        None,                                    // 0x3f3
        None,                                    // 0x400
        MOp_I1B_L_M32B_or_M16B,                  // 0x401 vdpps
        None,                                    // 0x402
        None,                                    // 0x403
        None,                                    // 0x410
        MOp_M16B_I1B,                            // 0x411 vdppd
        None,                                    // 0x412
        None,                                    // 0x413
        None,                                    // 0x420
        MOp_I1B_L_M32B_or_M16B,                  // 0x421 vmpsadbw
        None,                                    // 0x422
        None,                                    // 0x423
        None,                                    // 0x430
        None,                                    // 0x431
        None,                                    // 0x432
        None,                                    // 0x433
        None,                                    // 0x440
        MOp_M16B_I1B,                            // 0x441 vpclmulqdq
        None,                                    // 0x442
        None,                                    // 0x443
        None,                                    // 0x450
        None,                                    // 0x451
        None,                                    // 0x452
        None,                                    // 0x453
        None,                                    // 0x460
        MOp_M32B_I1B,                            // 0x461 vperm2i128
        None,                                    // 0x462
        None,                                    // 0x463
        None,                                    // 0x470
        None,                                    // 0x471
        None,                                    // 0x472
        None,                                    // 0x473
        None,                                    // 0x480
        MOp_I1B_L_M32B_or_M16B,                  // 0x481 vpermil2ps
        None,                                    // 0x482
        None,                                    // 0x483
        None,                                    // 0x490
        MOp_I1B_L_M32B_or_M16B,                  // 0x491 vpermil2pd
        None,                                    // 0x492
        None,                                    // 0x493
        None,                                    // 0x4a0
        None,                                    // 0x4a1
        None,                                    // 0x4a2
        None,                                    // 0x4a3
        None,                                    // 0x4b0
        None,                                    // 0x4b1
        None,                                    // 0x4b2
        None,                                    // 0x4b3
        None,                                    // 0x4c0
        None,                                    // 0x4c1
        None,                                    // 0x4c2
        None,                                    // 0x4c3
        None,                                    // 0x4d0
        None,                                    // 0x4d1
        None,                                    // 0x4d2
        None,                                    // 0x4d3
        None,                                    // 0x4e0
        None,                                    // 0x4e1
        None,                                    // 0x4e2
        None,                                    // 0x4e3
        None,                                    // 0x4f0
        None,                                    // 0x4f1
        None,                                    // 0x4f2
        None,                                    // 0x4f3
        None,                                    // 0x500
        None,                                    // 0x501
        None,                                    // 0x502
        None,                                    // 0x503
        None,                                    // 0x510
        None,                                    // 0x511
        None,                                    // 0x512
        None,                                    // 0x513
        None,                                    // 0x520
        None,                                    // 0x521
        None,                                    // 0x522
        None,                                    // 0x523
        None,                                    // 0x530
        None,                                    // 0x531
        None,                                    // 0x532
        None,                                    // 0x533
        None,                                    // 0x540
        None,                                    // 0x541
        None,                                    // 0x542
        None,                                    // 0x543
        None,                                    // 0x550
        None,                                    // 0x551
        None,                                    // 0x552
        None,                                    // 0x553
        None,                                    // 0x560
        None,                                    // 0x561
        None,                                    // 0x562
        None,                                    // 0x563
        None,                                    // 0x570
        None,                                    // 0x571
        None,                                    // 0x572
        None,                                    // 0x573
        None,                                    // 0x580
        None,                                    // 0x581
        None,                                    // 0x582
        None,                                    // 0x583
        None,                                    // 0x590
        None,                                    // 0x591
        None,                                    // 0x592
        None,                                    // 0x593
        None,                                    // 0x5a0
        None,                                    // 0x5a1
        None,                                    // 0x5a2
        None,                                    // 0x5a3
        None,                                    // 0x5b0
        None,                                    // 0x5b1
        None,                                    // 0x5b2
        None,                                    // 0x5b3
        None,                                    // 0x5c0
        None,                                    // 0x5c1
        None,                                    // 0x5c2
        None,                                    // 0x5c3
        None,                                    // 0x5d0
        None,                                    // 0x5d1
        None,                                    // 0x5d2
        None,                                    // 0x5d3
        None,                                    // 0x5e0
        None,                                    // 0x5e1
        None,                                    // 0x5e2
        None,                                    // 0x5e3
        None,                                    // 0x5f0
        None,                                    // 0x5f1
        None,                                    // 0x5f2
        None,                                    // 0x5f3
        None,                                    // 0x600
        MOp_M16B_I1B,                            // 0x601 vpcmpestrm
        None,                                    // 0x602
        None,                                    // 0x603
        None,                                    // 0x610
        MOp_M16B_I1B,                            // 0x611 vpcmpestri
        None,                                    // 0x612
        None,                                    // 0x613
        None,                                    // 0x620
        MOp_M16B_I1B,                            // 0x621 vpcmpistrm
        None,                                    // 0x622
        None,                                    // 0x623
        None,                                    // 0x630
        MOp_M16B_I1B,                            // 0x631 vpcmpistri
        None,                                    // 0x632
        None,                                    // 0x633
        None,                                    // 0x640
        None,                                    // 0x641
        None,                                    // 0x642
        None,                                    // 0x643
        None,                                    // 0x650
        None,                                    // 0x651
        None,                                    // 0x652
        None,                                    // 0x653
        None,                                    // 0x660
        None,                                    // 0x661
        None,                                    // 0x662
        None,                                    // 0x663
        None,                                    // 0x670
        None,                                    // 0x671
        None,                                    // 0x672
        None,                                    // 0x673
        None,                                    // 0x680
        None,                                    // 0x681
        None,                                    // 0x682
        None,                                    // 0x683
        None,                                    // 0x690
        None,                                    // 0x691
        None,                                    // 0x692
        None,                                    // 0x693
        None,                                    // 0x6a0
        None,                                    // 0x6a1
        None,                                    // 0x6a2
        None,                                    // 0x6a3
        None,                                    // 0x6b0
        None,                                    // 0x6b1
        None,                                    // 0x6b2
        None,                                    // 0x6b3
        None,                                    // 0x6c0
        None,                                    // 0x6c1
        None,                                    // 0x6c2
        None,                                    // 0x6c3
        None,                                    // 0x6d0
        None,                                    // 0x6d1
        None,                                    // 0x6d2
        None,                                    // 0x6d3
        None,                                    // 0x6e0
        None,                                    // 0x6e1
        None,                                    // 0x6e2
        None,                                    // 0x6e3
        None,                                    // 0x6f0
        None,                                    // 0x6f1
        None,                                    // 0x6f2
        None,                                    // 0x6f3
        None,                                    // 0x700
        None,                                    // 0x701
        None,                                    // 0x702
        None,                                    // 0x703
        None,                                    // 0x710
        None,                                    // 0x711
        None,                                    // 0x712
        None,                                    // 0x713
        None,                                    // 0x720
        None,                                    // 0x721
        None,                                    // 0x722
        None,                                    // 0x723
        None,                                    // 0x730
        None,                                    // 0x731
        None,                                    // 0x732
        None,                                    // 0x733
        None,                                    // 0x740
        None,                                    // 0x741
        None,                                    // 0x742
        None,                                    // 0x743
        None,                                    // 0x750
        None,                                    // 0x751
        None,                                    // 0x752
        None,                                    // 0x753
        None,                                    // 0x760
        None,                                    // 0x761
        None,                                    // 0x762
        None,                                    // 0x763
        None,                                    // 0x770
        None,                                    // 0x771
        None,                                    // 0x772
        None,                                    // 0x773
        None,                                    // 0x780
        None,                                    // 0x781
        None,                                    // 0x782
        None,                                    // 0x783
        None,                                    // 0x790
        None,                                    // 0x791
        None,                                    // 0x792
        None,                                    // 0x793
        None,                                    // 0x7a0
        None,                                    // 0x7a1
        None,                                    // 0x7a2
        None,                                    // 0x7a3
        None,                                    // 0x7b0
        None,                                    // 0x7b1
        None,                                    // 0x7b2
        None,                                    // 0x7b3
        None,                                    // 0x7c0
        None,                                    // 0x7c1
        None,                                    // 0x7c2
        None,                                    // 0x7c3
        None,                                    // 0x7d0
        None,                                    // 0x7d1
        None,                                    // 0x7d2
        None,                                    // 0x7d3
        None,                                    // 0x7e0
        None,                                    // 0x7e1
        None,                                    // 0x7e2
        None,                                    // 0x7e3
        None,                                    // 0x7f0
        None,                                    // 0x7f1
        None,                                    // 0x7f2
        None,                                    // 0x7f3
        None,                                    // 0x800
        None,                                    // 0x801
        None,                                    // 0x802
        None,                                    // 0x803
        None,                                    // 0x810
        None,                                    // 0x811
        None,                                    // 0x812
        None,                                    // 0x813
        None,                                    // 0x820
        None,                                    // 0x821
        None,                                    // 0x822
        None,                                    // 0x823
        None,                                    // 0x830
        None,                                    // 0x831
        None,                                    // 0x832
        None,                                    // 0x833
        None,                                    // 0x840
        None,                                    // 0x841
        None,                                    // 0x842
        None,                                    // 0x843
        None,                                    // 0x850
        None,                                    // 0x851
        None,                                    // 0x852
        None,                                    // 0x853
        None,                                    // 0x860
        None,                                    // 0x861
        None,                                    // 0x862
        None,                                    // 0x863
        None,                                    // 0x870
        None,                                    // 0x871
        None,                                    // 0x872
        None,                                    // 0x873
        None,                                    // 0x880
        None,                                    // 0x881
        None,                                    // 0x882
        None,                                    // 0x883
        None,                                    // 0x890
        None,                                    // 0x891
        None,                                    // 0x892
        None,                                    // 0x893
        None,                                    // 0x8a0
        None,                                    // 0x8a1
        None,                                    // 0x8a2
        None,                                    // 0x8a3
        None,                                    // 0x8b0
        None,                                    // 0x8b1
        None,                                    // 0x8b2
        None,                                    // 0x8b3
        None,                                    // 0x8c0
        None,                                    // 0x8c1
        None,                                    // 0x8c2
        None,                                    // 0x8c3
        None,                                    // 0x8d0
        None,                                    // 0x8d1
        None,                                    // 0x8d2
        None,                                    // 0x8d3
        None,                                    // 0x8e0
        None,                                    // 0x8e1
        None,                                    // 0x8e2
        None,                                    // 0x8e3
        None,                                    // 0x8f0
        None,                                    // 0x8f1
        None,                                    // 0x8f2
        None,                                    // 0x8f3
        None,                                    // 0x900
        None,                                    // 0x901
        None,                                    // 0x902
        None,                                    // 0x903
        None,                                    // 0x910
        None,                                    // 0x911
        None,                                    // 0x912
        None,                                    // 0x913
        None,                                    // 0x920
        None,                                    // 0x921
        None,                                    // 0x922
        None,                                    // 0x923
        None,                                    // 0x930
        None,                                    // 0x931
        None,                                    // 0x932
        None,                                    // 0x933
        None,                                    // 0x940
        None,                                    // 0x941
        None,                                    // 0x942
        None,                                    // 0x943
        None,                                    // 0x950
        None,                                    // 0x951
        None,                                    // 0x952
        None,                                    // 0x953
        None,                                    // 0x960
        None,                                    // 0x961
        None,                                    // 0x962
        None,                                    // 0x963
        None,                                    // 0x970
        None,                                    // 0x971
        None,                                    // 0x972
        None,                                    // 0x973
        None,                                    // 0x980
        None,                                    // 0x981
        None,                                    // 0x982
        None,                                    // 0x983
        None,                                    // 0x990
        None,                                    // 0x991
        None,                                    // 0x992
        None,                                    // 0x993
        None,                                    // 0x9a0
        None,                                    // 0x9a1
        None,                                    // 0x9a2
        None,                                    // 0x9a3
        None,                                    // 0x9b0
        None,                                    // 0x9b1
        None,                                    // 0x9b2
        None,                                    // 0x9b3
        None,                                    // 0x9c0
        None,                                    // 0x9c1
        None,                                    // 0x9c2
        None,                                    // 0x9c3
        None,                                    // 0x9d0
        None,                                    // 0x9d1
        None,                                    // 0x9d2
        None,                                    // 0x9d3
        None,                                    // 0x9e0
        None,                                    // 0x9e1
        None,                                    // 0x9e2
        None,                                    // 0x9e3
        None,                                    // 0x9f0
        None,                                    // 0x9f1
        None,                                    // 0x9f2
        None,                                    // 0x9f3
        None,                                    // 0xa00
        None,                                    // 0xa01
        None,                                    // 0xa02
        None,                                    // 0xa03
        None,                                    // 0xa10
        None,                                    // 0xa11
        None,                                    // 0xa12
        None,                                    // 0xa13
        None,                                    // 0xa20
        None,                                    // 0xa21
        None,                                    // 0xa22
        None,                                    // 0xa23
        None,                                    // 0xa30
        None,                                    // 0xa31
        None,                                    // 0xa32
        None,                                    // 0xa33
        None,                                    // 0xa40
        None,                                    // 0xa41
        None,                                    // 0xa42
        None,                                    // 0xa43
        None,                                    // 0xa50
        None,                                    // 0xa51
        None,                                    // 0xa52
        None,                                    // 0xa53
        None,                                    // 0xa60
        None,                                    // 0xa61
        None,                                    // 0xa62
        None,                                    // 0xa63
        None,                                    // 0xa70
        None,                                    // 0xa71
        None,                                    // 0xa72
        None,                                    // 0xa73
        None,                                    // 0xa80
        None,                                    // 0xa81
        None,                                    // 0xa82
        None,                                    // 0xa83
        None,                                    // 0xa90
        None,                                    // 0xa91
        None,                                    // 0xa92
        None,                                    // 0xa93
        None,                                    // 0xaa0
        None,                                    // 0xaa1
        None,                                    // 0xaa2
        None,                                    // 0xaa3
        None,                                    // 0xab0
        None,                                    // 0xab1
        None,                                    // 0xab2
        None,                                    // 0xab3
        None,                                    // 0xac0
        None,                                    // 0xac1
        None,                                    // 0xac2
        None,                                    // 0xac3
        None,                                    // 0xad0
        None,                                    // 0xad1
        None,                                    // 0xad2
        None,                                    // 0xad3
        None,                                    // 0xae0
        None,                                    // 0xae1
        None,                                    // 0xae2
        None,                                    // 0xae3
        None,                                    // 0xaf0
        None,                                    // 0xaf1
        None,                                    // 0xaf2
        None,                                    // 0xaf3
        None,                                    // 0xb00
        None,                                    // 0xb01
        None,                                    // 0xb02
        None,                                    // 0xb03
        None,                                    // 0xb10
        None,                                    // 0xb11
        None,                                    // 0xb12
        None,                                    // 0xb13
        None,                                    // 0xb20
        None,                                    // 0xb21
        None,                                    // 0xb22
        None,                                    // 0xb23
        None,                                    // 0xb30
        None,                                    // 0xb31
        None,                                    // 0xb32
        None,                                    // 0xb33
        None,                                    // 0xb40
        None,                                    // 0xb41
        None,                                    // 0xb42
        None,                                    // 0xb43
        None,                                    // 0xb50
        None,                                    // 0xb51
        None,                                    // 0xb52
        None,                                    // 0xb53
        None,                                    // 0xb60
        None,                                    // 0xb61
        None,                                    // 0xb62
        None,                                    // 0xb63
        None,                                    // 0xb70
        None,                                    // 0xb71
        None,                                    // 0xb72
        None,                                    // 0xb73
        None,                                    // 0xb80
        None,                                    // 0xb81
        None,                                    // 0xb82
        None,                                    // 0xb83
        None,                                    // 0xb90
        None,                                    // 0xb91
        None,                                    // 0xb92
        None,                                    // 0xb93
        None,                                    // 0xba0
        None,                                    // 0xba1
        None,                                    // 0xba2
        None,                                    // 0xba3
        None,                                    // 0xbb0
        None,                                    // 0xbb1
        None,                                    // 0xbb2
        None,                                    // 0xbb3
        None,                                    // 0xbc0
        None,                                    // 0xbc1
        None,                                    // 0xbc2
        None,                                    // 0xbc3
        None,                                    // 0xbd0
        None,                                    // 0xbd1
        None,                                    // 0xbd2
        None,                                    // 0xbd3
        None,                                    // 0xbe0
        None,                                    // 0xbe1
        None,                                    // 0xbe2
        None,                                    // 0xbe3
        None,                                    // 0xbf0
        None,                                    // 0xbf1
        None,                                    // 0xbf2
        None,                                    // 0xbf3
        None,                                    // 0xc00
        None,                                    // 0xc01
        None,                                    // 0xc02
        None,                                    // 0xc03
        None,                                    // 0xc10
        None,                                    // 0xc11
        None,                                    // 0xc12
        None,                                    // 0xc13
        None,                                    // 0xc20
        None,                                    // 0xc21
        None,                                    // 0xc22
        None,                                    // 0xc23
        None,                                    // 0xc30
        None,                                    // 0xc31
        None,                                    // 0xc32
        None,                                    // 0xc33
        None,                                    // 0xc40
        None,                                    // 0xc41
        None,                                    // 0xc42
        None,                                    // 0xc43
        None,                                    // 0xc50
        None,                                    // 0xc51
        None,                                    // 0xc52
        None,                                    // 0xc53
        None,                                    // 0xc60
        None,                                    // 0xc61
        None,                                    // 0xc62
        None,                                    // 0xc63
        None,                                    // 0xc70
        None,                                    // 0xc71
        None,                                    // 0xc72
        None,                                    // 0xc73
        None,                                    // 0xc80
        None,                                    // 0xc81
        None,                                    // 0xc82
        None,                                    // 0xc83
        None,                                    // 0xc90
        None,                                    // 0xc91
        None,                                    // 0xc92
        None,                                    // 0xc93
        None,                                    // 0xca0
        None,                                    // 0xca1
        None,                                    // 0xca2
        None,                                    // 0xca3
        None,                                    // 0xcb0
        None,                                    // 0xcb1
        None,                                    // 0xcb2
        None,                                    // 0xcb3
        None,                                    // 0xcc0
        None,                                    // 0xcc1
        None,                                    // 0xcc2
        None,                                    // 0xcc3
        None,                                    // 0xcd0
        None,                                    // 0xcd1
        None,                                    // 0xcd2
        None,                                    // 0xcd3
        None,                                    // 0xce0
        None,                                    // 0xce1
        None,                                    // 0xce2
        None,                                    // 0xce3
        None,                                    // 0xcf0
        None,                                    // 0xcf1
        None,                                    // 0xcf2
        None,                                    // 0xcf3
        None,                                    // 0xd00
        None,                                    // 0xd01
        None,                                    // 0xd02
        None,                                    // 0xd03
        None,                                    // 0xd10
        None,                                    // 0xd11
        None,                                    // 0xd12
        None,                                    // 0xd13
        None,                                    // 0xd20
        None,                                    // 0xd21
        None,                                    // 0xd22
        None,                                    // 0xd23
        None,                                    // 0xd30
        None,                                    // 0xd31
        None,                                    // 0xd32
        None,                                    // 0xd33
        None,                                    // 0xd40
        None,                                    // 0xd41
        None,                                    // 0xd42
        None,                                    // 0xd43
        None,                                    // 0xd50
        None,                                    // 0xd51
        None,                                    // 0xd52
        None,                                    // 0xd53
        None,                                    // 0xd60
        None,                                    // 0xd61
        None,                                    // 0xd62
        None,                                    // 0xd63
        None,                                    // 0xd70
        None,                                    // 0xd71
        None,                                    // 0xd72
        None,                                    // 0xd73
        None,                                    // 0xd80
        None,                                    // 0xd81
        None,                                    // 0xd82
        None,                                    // 0xd83
        None,                                    // 0xd90
        None,                                    // 0xd91
        None,                                    // 0xd92
        None,                                    // 0xd93
        None,                                    // 0xda0
        None,                                    // 0xda1
        None,                                    // 0xda2
        None,                                    // 0xda3
        None,                                    // 0xdb0
        None,                                    // 0xdb1
        None,                                    // 0xdb2
        None,                                    // 0xdb3
        None,                                    // 0xdc0
        None,                                    // 0xdc1
        None,                                    // 0xdc2
        None,                                    // 0xdc3
        None,                                    // 0xdd0
        None,                                    // 0xdd1
        None,                                    // 0xdd2
        None,                                    // 0xdd3
        None,                                    // 0xde0
        None,                                    // 0xde1
        None,                                    // 0xde2
        None,                                    // 0xde3
        None,                                    // 0xdf0
        MOp_M16B_I1B,                            // 0xdf1 vaeskeygenassist
        None,                                    // 0xdf2
        None,                                    // 0xdf3
        None,                                    // 0xe00
        None,                                    // 0xe01
        None,                                    // 0xe02
        None,                                    // 0xe03
        None,                                    // 0xe10
        None,                                    // 0xe11
        None,                                    // 0xe12
        None,                                    // 0xe13
        None,                                    // 0xe20
        None,                                    // 0xe21
        None,                                    // 0xe22
        None,                                    // 0xe23
        None,                                    // 0xe30
        None,                                    // 0xe31
        None,                                    // 0xe32
        None,                                    // 0xe33
        None,                                    // 0xe40
        None,                                    // 0xe41
        None,                                    // 0xe42
        None,                                    // 0xe43
        None,                                    // 0xe50
        None,                                    // 0xe51
        None,                                    // 0xe52
        None,                                    // 0xe53
        None,                                    // 0xe60
        None,                                    // 0xe61
        None,                                    // 0xe62
        None,                                    // 0xe63
        None,                                    // 0xe70
        None,                                    // 0xe71
        None,                                    // 0xe72
        None,                                    // 0xe73
        None,                                    // 0xe80
        None,                                    // 0xe81
        None,                                    // 0xe82
        None,                                    // 0xe83
        None,                                    // 0xe90
        None,                                    // 0xe91
        None,                                    // 0xe92
        None,                                    // 0xe93
        None,                                    // 0xea0
        None,                                    // 0xea1
        None,                                    // 0xea2
        None,                                    // 0xea3
        None,                                    // 0xeb0
        None,                                    // 0xeb1
        None,                                    // 0xeb2
        None,                                    // 0xeb3
        None,                                    // 0xec0
        None,                                    // 0xec1
        None,                                    // 0xec2
        None,                                    // 0xec3
        None,                                    // 0xed0
        None,                                    // 0xed1
        None,                                    // 0xed2
        None,                                    // 0xed3
        None,                                    // 0xee0
        None,                                    // 0xee1
        None,                                    // 0xee2
        None,                                    // 0xee3
        None,                                    // 0xef0
        None,                                    // 0xef1
        None,                                    // 0xef2
        None,                                    // 0xef3
        None,                                    // 0xf00
        None,                                    // 0xf01
        None,                                    // 0xf02
        MOp_I1B_W_M8B_or_M4B,                    // 0xf03 rorx
        None,                                    // 0xf10
        None,                                    // 0xf11
        None,                                    // 0xf12
        None,                                    // 0xf13
        None,                                    // 0xf20
        None,                                    // 0xf21
        None,                                    // 0xf22
        None,                                    // 0xf23
        None,                                    // 0xf30
        None,                                    // 0xf31
        None,                                    // 0xf32
        None,                                    // 0xf33
        None,                                    // 0xf40
        None,                                    // 0xf41
        None,                                    // 0xf42
        None,                                    // 0xf43
        None,                                    // 0xf50
        None,                                    // 0xf51
        None,                                    // 0xf52
        None,                                    // 0xf53
        None,                                    // 0xf60
        None,                                    // 0xf61
        None,                                    // 0xf62
        None,                                    // 0xf63
        None,                                    // 0xf70
        None,                                    // 0xf71
        None,                                    // 0xf72
        None,                                    // 0xf73
        None,                                    // 0xf80
        None,                                    // 0xf81
        None,                                    // 0xf82
        None,                                    // 0xf83
        None,                                    // 0xf90
        None,                                    // 0xf91
        None,                                    // 0xf92
        None,                                    // 0xf93
        None,                                    // 0xfa0
        None,                                    // 0xfa1
        None,                                    // 0xfa2
        None,                                    // 0xfa3
        None,                                    // 0xfb0
        None,                                    // 0xfb1
        None,                                    // 0xfb2
        None,                                    // 0xfb3
        None,                                    // 0xfc0
        None,                                    // 0xfc1
        None,                                    // 0xfc2
        None,                                    // 0xfc3
        None,                                    // 0xfd0
        None,                                    // 0xfd1
        None,                                    // 0xfd2
        None,                                    // 0xfd3
        None,                                    // 0xfe0
        None,                                    // 0xfe1
        None,                                    // 0xfe2
        None,                                    // 0xfe3
        None,                                    // 0xff0
        None,                                    // 0xff1
        None,                                    // 0xff2
        None,                                    // 0xff3
    };

    static const InstrForm instrFormXOP8[1024]
    {
        None,                                    // 0x000
        None,                                    // 0x001
        None,                                    // 0x002
        None,                                    // 0x003
        None,                                    // 0x010
        None,                                    // 0x011
        None,                                    // 0x012
        None,                                    // 0x013
        None,                                    // 0x020
        None,                                    // 0x021
        None,                                    // 0x022
        None,                                    // 0x023
        None,                                    // 0x030
        None,                                    // 0x031
        None,                                    // 0x032
        None,                                    // 0x033
        None,                                    // 0x040
        None,                                    // 0x041
        None,                                    // 0x042
        None,                                    // 0x043
        None,                                    // 0x050
        None,                                    // 0x051
        None,                                    // 0x052
        None,                                    // 0x053
        None,                                    // 0x060
        None,                                    // 0x061
        None,                                    // 0x062
        None,                                    // 0x063
        None,                                    // 0x070
        None,                                    // 0x071
        None,                                    // 0x072
        None,                                    // 0x073
        None,                                    // 0x080
        None,                                    // 0x081
        None,                                    // 0x082
        None,                                    // 0x083
        None,                                    // 0x090
        None,                                    // 0x091
        None,                                    // 0x092
        None,                                    // 0x093
        None,                                    // 0x0a0
        None,                                    // 0x0a1
        None,                                    // 0x0a2
        None,                                    // 0x0a3
        None,                                    // 0x0b0
        None,                                    // 0x0b1
        None,                                    // 0x0b2
        None,                                    // 0x0b3
        None,                                    // 0x0c0
        None,                                    // 0x0c1
        None,                                    // 0x0c2
        None,                                    // 0x0c3
        None,                                    // 0x0d0
        None,                                    // 0x0d1
        None,                                    // 0x0d2
        None,                                    // 0x0d3
        None,                                    // 0x0e0
        None,                                    // 0x0e1
        None,                                    // 0x0e2
        None,                                    // 0x0e3
        None,                                    // 0x0f0
        None,                                    // 0x0f1
        None,                                    // 0x0f2
        None,                                    // 0x0f3
        None,                                    // 0x100
        None,                                    // 0x101
        None,                                    // 0x102
        None,                                    // 0x103
        None,                                    // 0x110
        None,                                    // 0x111
        None,                                    // 0x112
        None,                                    // 0x113
        None,                                    // 0x120
        None,                                    // 0x121
        None,                                    // 0x122
        None,                                    // 0x123
        None,                                    // 0x130
        None,                                    // 0x131
        None,                                    // 0x132
        None,                                    // 0x133
        None,                                    // 0x140
        None,                                    // 0x141
        None,                                    // 0x142
        None,                                    // 0x143
        None,                                    // 0x150
        None,                                    // 0x151
        None,                                    // 0x152
        None,                                    // 0x153
        None,                                    // 0x160
        None,                                    // 0x161
        None,                                    // 0x162
        None,                                    // 0x163
        None,                                    // 0x170
        None,                                    // 0x171
        None,                                    // 0x172
        None,                                    // 0x173
        None,                                    // 0x180
        None,                                    // 0x181
        None,                                    // 0x182
        None,                                    // 0x183
        None,                                    // 0x190
        None,                                    // 0x191
        None,                                    // 0x192
        None,                                    // 0x193
        None,                                    // 0x1a0
        None,                                    // 0x1a1
        None,                                    // 0x1a2
        None,                                    // 0x1a3
        None,                                    // 0x1b0
        None,                                    // 0x1b1
        None,                                    // 0x1b2
        None,                                    // 0x1b3
        None,                                    // 0x1c0
        None,                                    // 0x1c1
        None,                                    // 0x1c2
        None,                                    // 0x1c3
        None,                                    // 0x1d0
        None,                                    // 0x1d1
        None,                                    // 0x1d2
        None,                                    // 0x1d3
        None,                                    // 0x1e0
        None,                                    // 0x1e1
        None,                                    // 0x1e2
        None,                                    // 0x1e3
        None,                                    // 0x1f0
        None,                                    // 0x1f1
        None,                                    // 0x1f2
        None,                                    // 0x1f3
        None,                                    // 0x200
        None,                                    // 0x201
        None,                                    // 0x202
        None,                                    // 0x203
        None,                                    // 0x210
        None,                                    // 0x211
        None,                                    // 0x212
        None,                                    // 0x213
        None,                                    // 0x220
        None,                                    // 0x221
        None,                                    // 0x222
        None,                                    // 0x223
        None,                                    // 0x230
        None,                                    // 0x231
        None,                                    // 0x232
        None,                                    // 0x233
        None,                                    // 0x240
        None,                                    // 0x241
        None,                                    // 0x242
        None,                                    // 0x243
        None,                                    // 0x250
        None,                                    // 0x251
        None,                                    // 0x252
        None,                                    // 0x253
        None,                                    // 0x260
        None,                                    // 0x261
        None,                                    // 0x262
        None,                                    // 0x263
        None,                                    // 0x270
        None,                                    // 0x271
        None,                                    // 0x272
        None,                                    // 0x273
        None,                                    // 0x280
        None,                                    // 0x281
        None,                                    // 0x282
        None,                                    // 0x283
        None,                                    // 0x290
        None,                                    // 0x291
        None,                                    // 0x292
        None,                                    // 0x293
        None,                                    // 0x2a0
        None,                                    // 0x2a1
        None,                                    // 0x2a2
        None,                                    // 0x2a3
        None,                                    // 0x2b0
        None,                                    // 0x2b1
        None,                                    // 0x2b2
        None,                                    // 0x2b3
        None,                                    // 0x2c0
        None,                                    // 0x2c1
        None,                                    // 0x2c2
        None,                                    // 0x2c3
        None,                                    // 0x2d0
        None,                                    // 0x2d1
        None,                                    // 0x2d2
        None,                                    // 0x2d3
        None,                                    // 0x2e0
        None,                                    // 0x2e1
        None,                                    // 0x2e2
        None,                                    // 0x2e3
        None,                                    // 0x2f0
        None,                                    // 0x2f1
        None,                                    // 0x2f2
        None,                                    // 0x2f3
        None,                                    // 0x300
        None,                                    // 0x301
        None,                                    // 0x302
        None,                                    // 0x303
        None,                                    // 0x310
        None,                                    // 0x311
        None,                                    // 0x312
        None,                                    // 0x313
        None,                                    // 0x320
        None,                                    // 0x321
        None,                                    // 0x322
        None,                                    // 0x323
        None,                                    // 0x330
        None,                                    // 0x331
        None,                                    // 0x332
        None,                                    // 0x333
        None,                                    // 0x340
        None,                                    // 0x341
        None,                                    // 0x342
        None,                                    // 0x343
        None,                                    // 0x350
        None,                                    // 0x351
        None,                                    // 0x352
        None,                                    // 0x353
        None,                                    // 0x360
        None,                                    // 0x361
        None,                                    // 0x362
        None,                                    // 0x363
        None,                                    // 0x370
        None,                                    // 0x371
        None,                                    // 0x372
        None,                                    // 0x373
        None,                                    // 0x380
        None,                                    // 0x381
        None,                                    // 0x382
        None,                                    // 0x383
        None,                                    // 0x390
        None,                                    // 0x391
        None,                                    // 0x392
        None,                                    // 0x393
        None,                                    // 0x3a0
        None,                                    // 0x3a1
        None,                                    // 0x3a2
        None,                                    // 0x3a3
        None,                                    // 0x3b0
        None,                                    // 0x3b1
        None,                                    // 0x3b2
        None,                                    // 0x3b3
        None,                                    // 0x3c0
        None,                                    // 0x3c1
        None,                                    // 0x3c2
        None,                                    // 0x3c3
        None,                                    // 0x3d0
        None,                                    // 0x3d1
        None,                                    // 0x3d2
        None,                                    // 0x3d3
        None,                                    // 0x3e0
        None,                                    // 0x3e1
        None,                                    // 0x3e2
        None,                                    // 0x3e3
        None,                                    // 0x3f0
        None,                                    // 0x3f1
        None,                                    // 0x3f2
        None,                                    // 0x3f3
        None,                                    // 0x400
        None,                                    // 0x401
        None,                                    // 0x402
        None,                                    // 0x403
        None,                                    // 0x410
        None,                                    // 0x411
        None,                                    // 0x412
        None,                                    // 0x413
        None,                                    // 0x420
        None,                                    // 0x421
        None,                                    // 0x422
        None,                                    // 0x423
        None,                                    // 0x430
        None,                                    // 0x431
        None,                                    // 0x432
        None,                                    // 0x433
        None,                                    // 0x440
        None,                                    // 0x441
        None,                                    // 0x442
        None,                                    // 0x443
        None,                                    // 0x450
        None,                                    // 0x451
        None,                                    // 0x452
        None,                                    // 0x453
        None,                                    // 0x460
        None,                                    // 0x461
        None,                                    // 0x462
        None,                                    // 0x463
        None,                                    // 0x470
        None,                                    // 0x471
        None,                                    // 0x472
        None,                                    // 0x473
        None,                                    // 0x480
        None,                                    // 0x481
        None,                                    // 0x482
        None,                                    // 0x483
        None,                                    // 0x490
        None,                                    // 0x491
        None,                                    // 0x492
        None,                                    // 0x493
        None,                                    // 0x4a0
        None,                                    // 0x4a1
        None,                                    // 0x4a2
        None,                                    // 0x4a3
        None,                                    // 0x4b0
        None,                                    // 0x4b1
        None,                                    // 0x4b2
        None,                                    // 0x4b3
        None,                                    // 0x4c0
        None,                                    // 0x4c1
        None,                                    // 0x4c2
        None,                                    // 0x4c3
        None,                                    // 0x4d0
        None,                                    // 0x4d1
        None,                                    // 0x4d2
        None,                                    // 0x4d3
        None,                                    // 0x4e0
        None,                                    // 0x4e1
        None,                                    // 0x4e2
        None,                                    // 0x4e3
        None,                                    // 0x4f0
        None,                                    // 0x4f1
        None,                                    // 0x4f2
        None,                                    // 0x4f3
        None,                                    // 0x500
        None,                                    // 0x501
        None,                                    // 0x502
        None,                                    // 0x503
        None,                                    // 0x510
        None,                                    // 0x511
        None,                                    // 0x512
        None,                                    // 0x513
        None,                                    // 0x520
        None,                                    // 0x521
        None,                                    // 0x522
        None,                                    // 0x523
        None,                                    // 0x530
        None,                                    // 0x531
        None,                                    // 0x532
        None,                                    // 0x533
        None,                                    // 0x540
        None,                                    // 0x541
        None,                                    // 0x542
        None,                                    // 0x543
        None,                                    // 0x550
        None,                                    // 0x551
        None,                                    // 0x552
        None,                                    // 0x553
        None,                                    // 0x560
        None,                                    // 0x561
        None,                                    // 0x562
        None,                                    // 0x563
        None,                                    // 0x570
        None,                                    // 0x571
        None,                                    // 0x572
        None,                                    // 0x573
        None,                                    // 0x580
        None,                                    // 0x581
        None,                                    // 0x582
        None,                                    // 0x583
        None,                                    // 0x590
        None,                                    // 0x591
        None,                                    // 0x592
        None,                                    // 0x593
        None,                                    // 0x5a0
        None,                                    // 0x5a1
        None,                                    // 0x5a2
        None,                                    // 0x5a3
        None,                                    // 0x5b0
        None,                                    // 0x5b1
        None,                                    // 0x5b2
        None,                                    // 0x5b3
        None,                                    // 0x5c0
        None,                                    // 0x5c1
        None,                                    // 0x5c2
        None,                                    // 0x5c3
        None,                                    // 0x5d0
        None,                                    // 0x5d1
        None,                                    // 0x5d2
        None,                                    // 0x5d3
        None,                                    // 0x5e0
        None,                                    // 0x5e1
        None,                                    // 0x5e2
        None,                                    // 0x5e3
        None,                                    // 0x5f0
        None,                                    // 0x5f1
        None,                                    // 0x5f2
        None,                                    // 0x5f3
        None,                                    // 0x600
        None,                                    // 0x601
        None,                                    // 0x602
        None,                                    // 0x603
        None,                                    // 0x610
        None,                                    // 0x611
        None,                                    // 0x612
        None,                                    // 0x613
        None,                                    // 0x620
        None,                                    // 0x621
        None,                                    // 0x622
        None,                                    // 0x623
        None,                                    // 0x630
        None,                                    // 0x631
        None,                                    // 0x632
        None,                                    // 0x633
        None,                                    // 0x640
        None,                                    // 0x641
        None,                                    // 0x642
        None,                                    // 0x643
        None,                                    // 0x650
        None,                                    // 0x651
        None,                                    // 0x652
        None,                                    // 0x653
        None,                                    // 0x660
        None,                                    // 0x661
        None,                                    // 0x662
        None,                                    // 0x663
        None,                                    // 0x670
        None,                                    // 0x671
        None,                                    // 0x672
        None,                                    // 0x673
        None,                                    // 0x680
        None,                                    // 0x681
        None,                                    // 0x682
        None,                                    // 0x683
        None,                                    // 0x690
        None,                                    // 0x691
        None,                                    // 0x692
        None,                                    // 0x693
        None,                                    // 0x6a0
        None,                                    // 0x6a1
        None,                                    // 0x6a2
        None,                                    // 0x6a3
        None,                                    // 0x6b0
        None,                                    // 0x6b1
        None,                                    // 0x6b2
        None,                                    // 0x6b3
        None,                                    // 0x6c0
        None,                                    // 0x6c1
        None,                                    // 0x6c2
        None,                                    // 0x6c3
        None,                                    // 0x6d0
        None,                                    // 0x6d1
        None,                                    // 0x6d2
        None,                                    // 0x6d3
        None,                                    // 0x6e0
        None,                                    // 0x6e1
        None,                                    // 0x6e2
        None,                                    // 0x6e3
        None,                                    // 0x6f0
        None,                                    // 0x6f1
        None,                                    // 0x6f2
        None,                                    // 0x6f3
        None,                                    // 0x700
        None,                                    // 0x701
        None,                                    // 0x702
        None,                                    // 0x703
        None,                                    // 0x710
        None,                                    // 0x711
        None,                                    // 0x712
        None,                                    // 0x713
        None,                                    // 0x720
        None,                                    // 0x721
        None,                                    // 0x722
        None,                                    // 0x723
        None,                                    // 0x730
        None,                                    // 0x731
        None,                                    // 0x732
        None,                                    // 0x733
        None,                                    // 0x740
        None,                                    // 0x741
        None,                                    // 0x742
        None,                                    // 0x743
        None,                                    // 0x750
        None,                                    // 0x751
        None,                                    // 0x752
        None,                                    // 0x753
        None,                                    // 0x760
        None,                                    // 0x761
        None,                                    // 0x762
        None,                                    // 0x763
        None,                                    // 0x770
        None,                                    // 0x771
        None,                                    // 0x772
        None,                                    // 0x773
        None,                                    // 0x780
        None,                                    // 0x781
        None,                                    // 0x782
        None,                                    // 0x783
        None,                                    // 0x790
        None,                                    // 0x791
        None,                                    // 0x792
        None,                                    // 0x793
        None,                                    // 0x7a0
        None,                                    // 0x7a1
        None,                                    // 0x7a2
        None,                                    // 0x7a3
        None,                                    // 0x7b0
        None,                                    // 0x7b1
        None,                                    // 0x7b2
        None,                                    // 0x7b3
        None,                                    // 0x7c0
        None,                                    // 0x7c1
        None,                                    // 0x7c2
        None,                                    // 0x7c3
        None,                                    // 0x7d0
        None,                                    // 0x7d1
        None,                                    // 0x7d2
        None,                                    // 0x7d3
        None,                                    // 0x7e0
        None,                                    // 0x7e1
        None,                                    // 0x7e2
        None,                                    // 0x7e3
        None,                                    // 0x7f0
        None,                                    // 0x7f1
        None,                                    // 0x7f2
        None,                                    // 0x7f3
        None,                                    // 0x800
        None,                                    // 0x801
        None,                                    // 0x802
        None,                                    // 0x803
        None,                                    // 0x810
        None,                                    // 0x811
        None,                                    // 0x812
        None,                                    // 0x813
        None,                                    // 0x820
        None,                                    // 0x821
        None,                                    // 0x822
        None,                                    // 0x823
        None,                                    // 0x830
        None,                                    // 0x831
        None,                                    // 0x832
        None,                                    // 0x833
        None,                                    // 0x840
        None,                                    // 0x841
        None,                                    // 0x842
        None,                                    // 0x843
        None,                                    // 0x850
        None,                                    // 0x851
        None,                                    // 0x852
        None,                                    // 0x853
        None,                                    // 0x860
        None,                                    // 0x861
        None,                                    // 0x862
        None,                                    // 0x863
        None,                                    // 0x870
        None,                                    // 0x871
        None,                                    // 0x872
        None,                                    // 0x873
        None,                                    // 0x880
        None,                                    // 0x881
        None,                                    // 0x882
        None,                                    // 0x883
        None,                                    // 0x890
        None,                                    // 0x891
        None,                                    // 0x892
        None,                                    // 0x893
        None,                                    // 0x8a0
        None,                                    // 0x8a1
        None,                                    // 0x8a2
        None,                                    // 0x8a3
        None,                                    // 0x8b0
        None,                                    // 0x8b1
        None,                                    // 0x8b2
        None,                                    // 0x8b3
        None,                                    // 0x8c0
        None,                                    // 0x8c1
        None,                                    // 0x8c2
        None,                                    // 0x8c3
        None,                                    // 0x8d0
        None,                                    // 0x8d1
        None,                                    // 0x8d2
        None,                                    // 0x8d3
        None,                                    // 0x8e0
        None,                                    // 0x8e1
        None,                                    // 0x8e2
        None,                                    // 0x8e3
        None,                                    // 0x8f0
        None,                                    // 0x8f1
        None,                                    // 0x8f2
        None,                                    // 0x8f3
        None,                                    // 0x900
        None,                                    // 0x901
        None,                                    // 0x902
        None,                                    // 0x903
        None,                                    // 0x910
        None,                                    // 0x911
        None,                                    // 0x912
        None,                                    // 0x913
        None,                                    // 0x920
        None,                                    // 0x921
        None,                                    // 0x922
        None,                                    // 0x923
        None,                                    // 0x930
        None,                                    // 0x931
        None,                                    // 0x932
        None,                                    // 0x933
        None,                                    // 0x940
        None,                                    // 0x941
        None,                                    // 0x942
        None,                                    // 0x943
        None,                                    // 0x950
        None,                                    // 0x951
        None,                                    // 0x952
        None,                                    // 0x953
        None,                                    // 0x960
        None,                                    // 0x961
        None,                                    // 0x962
        None,                                    // 0x963
        None,                                    // 0x970
        None,                                    // 0x971
        None,                                    // 0x972
        None,                                    // 0x973
        None,                                    // 0x980
        None,                                    // 0x981
        None,                                    // 0x982
        None,                                    // 0x983
        None,                                    // 0x990
        None,                                    // 0x991
        None,                                    // 0x992
        None,                                    // 0x993
        None,                                    // 0x9a0
        None,                                    // 0x9a1
        None,                                    // 0x9a2
        None,                                    // 0x9a3
        None,                                    // 0x9b0
        None,                                    // 0x9b1
        None,                                    // 0x9b2
        None,                                    // 0x9b3
        None,                                    // 0x9c0
        None,                                    // 0x9c1
        None,                                    // 0x9c2
        None,                                    // 0x9c3
        None,                                    // 0x9d0
        None,                                    // 0x9d1
        None,                                    // 0x9d2
        None,                                    // 0x9d3
        None,                                    // 0x9e0
        None,                                    // 0x9e1
        None,                                    // 0x9e2
        None,                                    // 0x9e3
        None,                                    // 0x9f0
        None,                                    // 0x9f1
        None,                                    // 0x9f2
        None,                                    // 0x9f3
        None,                                    // 0xa00
        None,                                    // 0xa01
        None,                                    // 0xa02
        None,                                    // 0xa03
        None,                                    // 0xa10
        None,                                    // 0xa11
        None,                                    // 0xa12
        None,                                    // 0xa13
        None,                                    // 0xa20
        None,                                    // 0xa21
        None,                                    // 0xa22
        None,                                    // 0xa23
        None,                                    // 0xa30
        None,                                    // 0xa31
        None,                                    // 0xa32
        None,                                    // 0xa33
        None,                                    // 0xa40
        None,                                    // 0xa41
        None,                                    // 0xa42
        None,                                    // 0xa43
        None,                                    // 0xa50
        None,                                    // 0xa51
        None,                                    // 0xa52
        None,                                    // 0xa53
        None,                                    // 0xa60
        None,                                    // 0xa61
        None,                                    // 0xa62
        None,                                    // 0xa63
        None,                                    // 0xa70
        None,                                    // 0xa71
        None,                                    // 0xa72
        None,                                    // 0xa73
        None,                                    // 0xa80
        None,                                    // 0xa81
        None,                                    // 0xa82
        None,                                    // 0xa83
        None,                                    // 0xa90
        None,                                    // 0xa91
        None,                                    // 0xa92
        None,                                    // 0xa93
        None,                                    // 0xaa0
        None,                                    // 0xaa1
        None,                                    // 0xaa2
        None,                                    // 0xaa3
        None,                                    // 0xab0
        None,                                    // 0xab1
        None,                                    // 0xab2
        None,                                    // 0xab3
        None,                                    // 0xac0
        None,                                    // 0xac1
        None,                                    // 0xac2
        None,                                    // 0xac3
        None,                                    // 0xad0
        None,                                    // 0xad1
        None,                                    // 0xad2
        None,                                    // 0xad3
        None,                                    // 0xae0
        None,                                    // 0xae1
        None,                                    // 0xae2
        None,                                    // 0xae3
        None,                                    // 0xaf0
        None,                                    // 0xaf1
        None,                                    // 0xaf2
        None,                                    // 0xaf3
        None,                                    // 0xb00
        None,                                    // 0xb01
        None,                                    // 0xb02
        None,                                    // 0xb03
        None,                                    // 0xb10
        None,                                    // 0xb11
        None,                                    // 0xb12
        None,                                    // 0xb13
        None,                                    // 0xb20
        None,                                    // 0xb21
        None,                                    // 0xb22
        None,                                    // 0xb23
        None,                                    // 0xb30
        None,                                    // 0xb31
        None,                                    // 0xb32
        None,                                    // 0xb33
        None,                                    // 0xb40
        None,                                    // 0xb41
        None,                                    // 0xb42
        None,                                    // 0xb43
        None,                                    // 0xb50
        None,                                    // 0xb51
        None,                                    // 0xb52
        None,                                    // 0xb53
        None,                                    // 0xb60
        None,                                    // 0xb61
        None,                                    // 0xb62
        None,                                    // 0xb63
        None,                                    // 0xb70
        None,                                    // 0xb71
        None,                                    // 0xb72
        None,                                    // 0xb73
        None,                                    // 0xb80
        None,                                    // 0xb81
        None,                                    // 0xb82
        None,                                    // 0xb83
        None,                                    // 0xb90
        None,                                    // 0xb91
        None,                                    // 0xb92
        None,                                    // 0xb93
        None,                                    // 0xba0
        None,                                    // 0xba1
        None,                                    // 0xba2
        None,                                    // 0xba3
        None,                                    // 0xbb0
        None,                                    // 0xbb1
        None,                                    // 0xbb2
        None,                                    // 0xbb3
        None,                                    // 0xbc0
        None,                                    // 0xbc1
        None,                                    // 0xbc2
        None,                                    // 0xbc3
        None,                                    // 0xbd0
        None,                                    // 0xbd1
        None,                                    // 0xbd2
        None,                                    // 0xbd3
        None,                                    // 0xbe0
        None,                                    // 0xbe1
        None,                                    // 0xbe2
        None,                                    // 0xbe3
        None,                                    // 0xbf0
        None,                                    // 0xbf1
        None,                                    // 0xbf2
        None,                                    // 0xbf3
        I1B_W_None_or_MOp_M16B,                  // 0xc00 vprotb
        I1B_W_None_or_MOp_M16B,                  // 0xc01 vprotb
        I1B_W_None_or_MOp_M16B,                  // 0xc02 vprotb
        I1B_W_None_or_MOp_M16B,                  // 0xc03 vprotb
        I1B_W_None_or_MOp_M16B,                  // 0xc10 vprotw
        I1B_W_None_or_MOp_M16B,                  // 0xc11 vprotw
        I1B_W_None_or_MOp_M16B,                  // 0xc12 vprotw
        I1B_W_None_or_MOp_M16B,                  // 0xc13 vprotw
        I1B_W_None_or_MOp_M16B,                  // 0xc20 vprotd
        I1B_W_None_or_MOp_M16B,                  // 0xc21 vprotd
        I1B_W_None_or_MOp_M16B,                  // 0xc22 vprotd
        I1B_W_None_or_MOp_M16B,                  // 0xc23 vprotd
        I1B_W_None_or_MOp_M16B,                  // 0xc30 vprotq
        I1B_W_None_or_MOp_M16B,                  // 0xc31 vprotq
        I1B_W_None_or_MOp_M16B,                  // 0xc32 vprotq
        I1B_W_None_or_MOp_M16B,                  // 0xc33 vprotq
        None,                                    // 0xc40
        None,                                    // 0xc41
        None,                                    // 0xc42
        None,                                    // 0xc43
        None,                                    // 0xc50
        None,                                    // 0xc51
        None,                                    // 0xc52
        None,                                    // 0xc53
        None,                                    // 0xc60
        None,                                    // 0xc61
        None,                                    // 0xc62
        None,                                    // 0xc63
        None,                                    // 0xc70
        None,                                    // 0xc71
        None,                                    // 0xc72
        None,                                    // 0xc73
        None,                                    // 0xc80
        None,                                    // 0xc81
        None,                                    // 0xc82
        None,                                    // 0xc83
        None,                                    // 0xc90
        None,                                    // 0xc91
        None,                                    // 0xc92
        None,                                    // 0xc93
        None,                                    // 0xca0
        None,                                    // 0xca1
        None,                                    // 0xca2
        None,                                    // 0xca3
        None,                                    // 0xcb0
        None,                                    // 0xcb1
        None,                                    // 0xcb2
        None,                                    // 0xcb3
        MOp_M16B_I1B,                            // 0xcc0 vpcomb
        MOp_M16B_I1B,                            // 0xcc1 vpcomb
        MOp_M16B_I1B,                            // 0xcc2 vpcomb
        MOp_M16B_I1B,                            // 0xcc3 vpcomb
        MOp_M16B_I1B,                            // 0xcd0 vpcomw
        MOp_M16B_I1B,                            // 0xcd1 vpcomw
        MOp_M16B_I1B,                            // 0xcd2 vpcomw
        MOp_M16B_I1B,                            // 0xcd3 vpcomw
        MOp_M16B_I1B,                            // 0xce0 vpcomd
        MOp_M16B_I1B,                            // 0xce1 vpcomd
        MOp_M16B_I1B,                            // 0xce2 vpcomd
        MOp_M16B_I1B,                            // 0xce3 vpcomd
        MOp_M16B_I1B,                            // 0xcf0 vpcomq
        MOp_M16B_I1B,                            // 0xcf1 vpcomq
        MOp_M16B_I1B,                            // 0xcf2 vpcomq
        MOp_M16B_I1B,                            // 0xcf3 vpcomq
        None,                                    // 0xd00
        None,                                    // 0xd01
        None,                                    // 0xd02
        None,                                    // 0xd03
        None,                                    // 0xd10
        None,                                    // 0xd11
        None,                                    // 0xd12
        None,                                    // 0xd13
        None,                                    // 0xd20
        None,                                    // 0xd21
        None,                                    // 0xd22
        None,                                    // 0xd23
        None,                                    // 0xd30
        None,                                    // 0xd31
        None,                                    // 0xd32
        None,                                    // 0xd33
        None,                                    // 0xd40
        None,                                    // 0xd41
        None,                                    // 0xd42
        None,                                    // 0xd43
        None,                                    // 0xd50
        None,                                    // 0xd51
        None,                                    // 0xd52
        None,                                    // 0xd53
        None,                                    // 0xd60
        None,                                    // 0xd61
        None,                                    // 0xd62
        None,                                    // 0xd63
        None,                                    // 0xd70
        None,                                    // 0xd71
        None,                                    // 0xd72
        None,                                    // 0xd73
        None,                                    // 0xd80
        None,                                    // 0xd81
        None,                                    // 0xd82
        None,                                    // 0xd83
        None,                                    // 0xd90
        None,                                    // 0xd91
        None,                                    // 0xd92
        None,                                    // 0xd93
        None,                                    // 0xda0
        None,                                    // 0xda1
        None,                                    // 0xda2
        None,                                    // 0xda3
        None,                                    // 0xdb0
        None,                                    // 0xdb1
        None,                                    // 0xdb2
        None,                                    // 0xdb3
        None,                                    // 0xdc0
        None,                                    // 0xdc1
        None,                                    // 0xdc2
        None,                                    // 0xdc3
        None,                                    // 0xdd0
        None,                                    // 0xdd1
        None,                                    // 0xdd2
        None,                                    // 0xdd3
        None,                                    // 0xde0
        None,                                    // 0xde1
        None,                                    // 0xde2
        None,                                    // 0xde3
        None,                                    // 0xdf0
        None,                                    // 0xdf1
        None,                                    // 0xdf2
        None,                                    // 0xdf3
        None,                                    // 0xe00
        None,                                    // 0xe01
        None,                                    // 0xe02
        None,                                    // 0xe03
        None,                                    // 0xe10
        None,                                    // 0xe11
        None,                                    // 0xe12
        None,                                    // 0xe13
        None,                                    // 0xe20
        None,                                    // 0xe21
        None,                                    // 0xe22
        None,                                    // 0xe23
        None,                                    // 0xe30
        None,                                    // 0xe31
        None,                                    // 0xe32
        None,                                    // 0xe33
        None,                                    // 0xe40
        None,                                    // 0xe41
        None,                                    // 0xe42
        None,                                    // 0xe43
        None,                                    // 0xe50
        None,                                    // 0xe51
        None,                                    // 0xe52
        None,                                    // 0xe53
        None,                                    // 0xe60
        None,                                    // 0xe61
        None,                                    // 0xe62
        None,                                    // 0xe63
        None,                                    // 0xe70
        None,                                    // 0xe71
        None,                                    // 0xe72
        None,                                    // 0xe73
        None,                                    // 0xe80
        None,                                    // 0xe81
        None,                                    // 0xe82
        None,                                    // 0xe83
        None,                                    // 0xe90
        None,                                    // 0xe91
        None,                                    // 0xe92
        None,                                    // 0xe93
        None,                                    // 0xea0
        None,                                    // 0xea1
        None,                                    // 0xea2
        None,                                    // 0xea3
        None,                                    // 0xeb0
        None,                                    // 0xeb1
        None,                                    // 0xeb2
        None,                                    // 0xeb3
        MOp_M16B_I1B,                            // 0xec0 vpcomub
        MOp_M16B_I1B,                            // 0xec1 vpcomub
        MOp_M16B_I1B,                            // 0xec2 vpcomub
        MOp_M16B_I1B,                            // 0xec3 vpcomub
        MOp_M16B_I1B,                            // 0xed0 vpcomuw
        MOp_M16B_I1B,                            // 0xed1 vpcomuw
        MOp_M16B_I1B,                            // 0xed2 vpcomuw
        MOp_M16B_I1B,                            // 0xed3 vpcomuw
        MOp_M16B_I1B,                            // 0xee0 vpcomud
        MOp_M16B_I1B,                            // 0xee1 vpcomud
        MOp_M16B_I1B,                            // 0xee2 vpcomud
        MOp_M16B_I1B,                            // 0xee3 vpcomud
        MOp_M16B_I1B,                            // 0xef0 vpcomuq
        MOp_M16B_I1B,                            // 0xef1 vpcomuq
        MOp_M16B_I1B,                            // 0xef2 vpcomuq
        MOp_M16B_I1B,                            // 0xef3 vpcomuq
        None,                                    // 0xf00
        None,                                    // 0xf01
        None,                                    // 0xf02
        None,                                    // 0xf03
        None,                                    // 0xf10
        None,                                    // 0xf11
        None,                                    // 0xf12
        None,                                    // 0xf13
        None,                                    // 0xf20
        None,                                    // 0xf21
        None,                                    // 0xf22
        None,                                    // 0xf23
        None,                                    // 0xf30
        None,                                    // 0xf31
        None,                                    // 0xf32
        None,                                    // 0xf33
        None,                                    // 0xf40
        None,                                    // 0xf41
        None,                                    // 0xf42
        None,                                    // 0xf43
        None,                                    // 0xf50
        None,                                    // 0xf51
        None,                                    // 0xf52
        None,                                    // 0xf53
        None,                                    // 0xf60
        None,                                    // 0xf61
        None,                                    // 0xf62
        None,                                    // 0xf63
        None,                                    // 0xf70
        None,                                    // 0xf71
        None,                                    // 0xf72
        None,                                    // 0xf73
        None,                                    // 0xf80
        None,                                    // 0xf81
        None,                                    // 0xf82
        None,                                    // 0xf83
        None,                                    // 0xf90
        None,                                    // 0xf91
        None,                                    // 0xf92
        None,                                    // 0xf93
        None,                                    // 0xfa0
        None,                                    // 0xfa1
        None,                                    // 0xfa2
        None,                                    // 0xfa3
        None,                                    // 0xfb0
        None,                                    // 0xfb1
        None,                                    // 0xfb2
        None,                                    // 0xfb3
        None,                                    // 0xfc0
        None,                                    // 0xfc1
        None,                                    // 0xfc2
        None,                                    // 0xfc3
        None,                                    // 0xfd0
        None,                                    // 0xfd1
        None,                                    // 0xfd2
        None,                                    // 0xfd3
        None,                                    // 0xfe0
        None,                                    // 0xfe1
        None,                                    // 0xfe2
        None,                                    // 0xfe3
        None,                                    // 0xff0
        None,                                    // 0xff1
        None,                                    // 0xff2
        None,                                    // 0xff3
    };

    static const InstrForm instrFormXOP9[1024]
    {
        None,                                    // 0x000
        None,                                    // 0x001
        None,                                    // 0x002
        None,                                    // 0x003
        MOp_W_M8B_or_M4B,                        // 0x010 blcfill,blcic,blcs,blsfill,blsic,t1mskc,tzmsk
        MOp_W_M8B_or_M4B,                        // 0x011 blcfill,blcic,blcs,blsfill,blsic,t1mskc,tzmsk
        MOp_W_M8B_or_M4B,                        // 0x012 blcfill,blcic,blcs,blsfill,blsic,t1mskc,tzmsk
        MOp_W_M8B_or_M4B,                        // 0x013 blcfill,blcic,blcs,blsfill,blsic,t1mskc,tzmsk
        MOp_W_M8B_or_M4B,                        // 0x020 blci,blcmsk
        MOp_W_M8B_or_M4B,                        // 0x021 blci,blcmsk
        MOp_W_M8B_or_M4B,                        // 0x022 blci,blcmsk
        MOp_W_M8B_or_M4B,                        // 0x023 blci,blcmsk
        None,                                    // 0x030
        None,                                    // 0x031
        None,                                    // 0x032
        None,                                    // 0x033
        None,                                    // 0x040
        None,                                    // 0x041
        None,                                    // 0x042
        None,                                    // 0x043
        None,                                    // 0x050
        None,                                    // 0x051
        None,                                    // 0x052
        None,                                    // 0x053
        None,                                    // 0x060
        None,                                    // 0x061
        None,                                    // 0x062
        None,                                    // 0x063
        None,                                    // 0x070
        None,                                    // 0x071
        None,                                    // 0x072
        None,                                    // 0x073
        None,                                    // 0x080
        None,                                    // 0x081
        None,                                    // 0x082
        None,                                    // 0x083
        None,                                    // 0x090
        None,                                    // 0x091
        None,                                    // 0x092
        None,                                    // 0x093
        None,                                    // 0x0a0
        None,                                    // 0x0a1
        None,                                    // 0x0a2
        None,                                    // 0x0a3
        None,                                    // 0x0b0
        None,                                    // 0x0b1
        None,                                    // 0x0b2
        None,                                    // 0x0b3
        None,                                    // 0x0c0
        None,                                    // 0x0c1
        None,                                    // 0x0c2
        None,                                    // 0x0c3
        None,                                    // 0x0d0
        None,                                    // 0x0d1
        None,                                    // 0x0d2
        None,                                    // 0x0d3
        None,                                    // 0x0e0
        None,                                    // 0x0e1
        None,                                    // 0x0e2
        None,                                    // 0x0e3
        None,                                    // 0x0f0
        None,                                    // 0x0f1
        None,                                    // 0x0f2
        None,                                    // 0x0f3
        None,                                    // 0x100
        None,                                    // 0x101
        None,                                    // 0x102
        None,                                    // 0x103
        None,                                    // 0x110
        None,                                    // 0x111
        None,                                    // 0x112
        None,                                    // 0x113
        I1B,                                     // 0x120 llwpcb,slwpcb
        I1B,                                     // 0x121 llwpcb,slwpcb
        I1B,                                     // 0x122 llwpcb,slwpcb
        I1B,                                     // 0x123 llwpcb,slwpcb
        None,                                    // 0x130
        None,                                    // 0x131
        None,                                    // 0x132
        None,                                    // 0x133
        None,                                    // 0x140
        None,                                    // 0x141
        None,                                    // 0x142
        None,                                    // 0x143
        None,                                    // 0x150
        None,                                    // 0x151
        None,                                    // 0x152
        None,                                    // 0x153
        None,                                    // 0x160
        None,                                    // 0x161
        None,                                    // 0x162
        None,                                    // 0x163
        None,                                    // 0x170
        None,                                    // 0x171
        None,                                    // 0x172
        None,                                    // 0x173
        None,                                    // 0x180
        None,                                    // 0x181
        None,                                    // 0x182
        None,                                    // 0x183
        None,                                    // 0x190
        None,                                    // 0x191
        None,                                    // 0x192
        None,                                    // 0x193
        None,                                    // 0x1a0
        None,                                    // 0x1a1
        None,                                    // 0x1a2
        None,                                    // 0x1a3
        None,                                    // 0x1b0
        None,                                    // 0x1b1
        None,                                    // 0x1b2
        None,                                    // 0x1b3
        None,                                    // 0x1c0
        None,                                    // 0x1c1
        None,                                    // 0x1c2
        None,                                    // 0x1c3
        None,                                    // 0x1d0
        None,                                    // 0x1d1
        None,                                    // 0x1d2
        None,                                    // 0x1d3
        None,                                    // 0x1e0
        None,                                    // 0x1e1
        None,                                    // 0x1e2
        None,                                    // 0x1e3
        None,                                    // 0x1f0
        None,                                    // 0x1f1
        None,                                    // 0x1f2
        None,                                    // 0x1f3
        None,                                    // 0x200
        None,                                    // 0x201
        None,                                    // 0x202
        None,                                    // 0x203
        None,                                    // 0x210
        None,                                    // 0x211
        None,                                    // 0x212
        None,                                    // 0x213
        None,                                    // 0x220
        None,                                    // 0x221
        None,                                    // 0x222
        None,                                    // 0x223
        None,                                    // 0x230
        None,                                    // 0x231
        None,                                    // 0x232
        None,                                    // 0x233
        None,                                    // 0x240
        None,                                    // 0x241
        None,                                    // 0x242
        None,                                    // 0x243
        None,                                    // 0x250
        None,                                    // 0x251
        None,                                    // 0x252
        None,                                    // 0x253
        None,                                    // 0x260
        None,                                    // 0x261
        None,                                    // 0x262
        None,                                    // 0x263
        None,                                    // 0x270
        None,                                    // 0x271
        None,                                    // 0x272
        None,                                    // 0x273
        None,                                    // 0x280
        None,                                    // 0x281
        None,                                    // 0x282
        None,                                    // 0x283
        None,                                    // 0x290
        None,                                    // 0x291
        None,                                    // 0x292
        None,                                    // 0x293
        None,                                    // 0x2a0
        None,                                    // 0x2a1
        None,                                    // 0x2a2
        None,                                    // 0x2a3
        None,                                    // 0x2b0
        None,                                    // 0x2b1
        None,                                    // 0x2b2
        None,                                    // 0x2b3
        None,                                    // 0x2c0
        None,                                    // 0x2c1
        None,                                    // 0x2c2
        None,                                    // 0x2c3
        None,                                    // 0x2d0
        None,                                    // 0x2d1
        None,                                    // 0x2d2
        None,                                    // 0x2d3
        None,                                    // 0x2e0
        None,                                    // 0x2e1
        None,                                    // 0x2e2
        None,                                    // 0x2e3
        None,                                    // 0x2f0
        None,                                    // 0x2f1
        None,                                    // 0x2f2
        None,                                    // 0x2f3
        None,                                    // 0x300
        None,                                    // 0x301
        None,                                    // 0x302
        None,                                    // 0x303
        None,                                    // 0x310
        None,                                    // 0x311
        None,                                    // 0x312
        None,                                    // 0x313
        None,                                    // 0x320
        None,                                    // 0x321
        None,                                    // 0x322
        None,                                    // 0x323
        None,                                    // 0x330
        None,                                    // 0x331
        None,                                    // 0x332
        None,                                    // 0x333
        None,                                    // 0x340
        None,                                    // 0x341
        None,                                    // 0x342
        None,                                    // 0x343
        None,                                    // 0x350
        None,                                    // 0x351
        None,                                    // 0x352
        None,                                    // 0x353
        None,                                    // 0x360
        None,                                    // 0x361
        None,                                    // 0x362
        None,                                    // 0x363
        None,                                    // 0x370
        None,                                    // 0x371
        None,                                    // 0x372
        None,                                    // 0x373
        None,                                    // 0x380
        None,                                    // 0x381
        None,                                    // 0x382
        None,                                    // 0x383
        None,                                    // 0x390
        None,                                    // 0x391
        None,                                    // 0x392
        None,                                    // 0x393
        None,                                    // 0x3a0
        None,                                    // 0x3a1
        None,                                    // 0x3a2
        None,                                    // 0x3a3
        None,                                    // 0x3b0
        None,                                    // 0x3b1
        None,                                    // 0x3b2
        None,                                    // 0x3b3
        None,                                    // 0x3c0
        None,                                    // 0x3c1
        None,                                    // 0x3c2
        None,                                    // 0x3c3
        None,                                    // 0x3d0
        None,                                    // 0x3d1
        None,                                    // 0x3d2
        None,                                    // 0x3d3
        None,                                    // 0x3e0
        None,                                    // 0x3e1
        None,                                    // 0x3e2
        None,                                    // 0x3e3
        None,                                    // 0x3f0
        None,                                    // 0x3f1
        None,                                    // 0x3f2
        None,                                    // 0x3f3
        None,                                    // 0x400
        None,                                    // 0x401
        None,                                    // 0x402
        None,                                    // 0x403
        None,                                    // 0x410
        None,                                    // 0x411
        None,                                    // 0x412
        None,                                    // 0x413
        None,                                    // 0x420
        None,                                    // 0x421
        None,                                    // 0x422
        None,                                    // 0x423
        None,                                    // 0x430
        None,                                    // 0x431
        None,                                    // 0x432
        None,                                    // 0x433
        None,                                    // 0x440
        None,                                    // 0x441
        None,                                    // 0x442
        None,                                    // 0x443
        None,                                    // 0x450
        None,                                    // 0x451
        None,                                    // 0x452
        None,                                    // 0x453
        None,                                    // 0x460
        None,                                    // 0x461
        None,                                    // 0x462
        None,                                    // 0x463
        None,                                    // 0x470
        None,                                    // 0x471
        None,                                    // 0x472
        None,                                    // 0x473
        None,                                    // 0x480
        None,                                    // 0x481
        None,                                    // 0x482
        None,                                    // 0x483
        None,                                    // 0x490
        None,                                    // 0x491
        None,                                    // 0x492
        None,                                    // 0x493
        None,                                    // 0x4a0
        None,                                    // 0x4a1
        None,                                    // 0x4a2
        None,                                    // 0x4a3
        None,                                    // 0x4b0
        None,                                    // 0x4b1
        None,                                    // 0x4b2
        None,                                    // 0x4b3
        None,                                    // 0x4c0
        None,                                    // 0x4c1
        None,                                    // 0x4c2
        None,                                    // 0x4c3
        None,                                    // 0x4d0
        None,                                    // 0x4d1
        None,                                    // 0x4d2
        None,                                    // 0x4d3
        None,                                    // 0x4e0
        None,                                    // 0x4e1
        None,                                    // 0x4e2
        None,                                    // 0x4e3
        None,                                    // 0x4f0
        None,                                    // 0x4f1
        None,                                    // 0x4f2
        None,                                    // 0x4f3
        None,                                    // 0x500
        None,                                    // 0x501
        None,                                    // 0x502
        None,                                    // 0x503
        None,                                    // 0x510
        None,                                    // 0x511
        None,                                    // 0x512
        None,                                    // 0x513
        None,                                    // 0x520
        None,                                    // 0x521
        None,                                    // 0x522
        None,                                    // 0x523
        None,                                    // 0x530
        None,                                    // 0x531
        None,                                    // 0x532
        None,                                    // 0x533
        None,                                    // 0x540
        None,                                    // 0x541
        None,                                    // 0x542
        None,                                    // 0x543
        None,                                    // 0x550
        None,                                    // 0x551
        None,                                    // 0x552
        None,                                    // 0x553
        None,                                    // 0x560
        None,                                    // 0x561
        None,                                    // 0x562
        None,                                    // 0x563
        None,                                    // 0x570
        None,                                    // 0x571
        None,                                    // 0x572
        None,                                    // 0x573
        None,                                    // 0x580
        None,                                    // 0x581
        None,                                    // 0x582
        None,                                    // 0x583
        None,                                    // 0x590
        None,                                    // 0x591
        None,                                    // 0x592
        None,                                    // 0x593
        None,                                    // 0x5a0
        None,                                    // 0x5a1
        None,                                    // 0x5a2
        None,                                    // 0x5a3
        None,                                    // 0x5b0
        None,                                    // 0x5b1
        None,                                    // 0x5b2
        None,                                    // 0x5b3
        None,                                    // 0x5c0
        None,                                    // 0x5c1
        None,                                    // 0x5c2
        None,                                    // 0x5c3
        None,                                    // 0x5d0
        None,                                    // 0x5d1
        None,                                    // 0x5d2
        None,                                    // 0x5d3
        None,                                    // 0x5e0
        None,                                    // 0x5e1
        None,                                    // 0x5e2
        None,                                    // 0x5e3
        None,                                    // 0x5f0
        None,                                    // 0x5f1
        None,                                    // 0x5f2
        None,                                    // 0x5f3
        None,                                    // 0x600
        None,                                    // 0x601
        None,                                    // 0x602
        None,                                    // 0x603
        None,                                    // 0x610
        None,                                    // 0x611
        None,                                    // 0x612
        None,                                    // 0x613
        None,                                    // 0x620
        None,                                    // 0x621
        None,                                    // 0x622
        None,                                    // 0x623
        None,                                    // 0x630
        None,                                    // 0x631
        None,                                    // 0x632
        None,                                    // 0x633
        None,                                    // 0x640
        None,                                    // 0x641
        None,                                    // 0x642
        None,                                    // 0x643
        None,                                    // 0x650
        None,                                    // 0x651
        None,                                    // 0x652
        None,                                    // 0x653
        None,                                    // 0x660
        None,                                    // 0x661
        None,                                    // 0x662
        None,                                    // 0x663
        None,                                    // 0x670
        None,                                    // 0x671
        None,                                    // 0x672
        None,                                    // 0x673
        None,                                    // 0x680
        None,                                    // 0x681
        None,                                    // 0x682
        None,                                    // 0x683
        None,                                    // 0x690
        None,                                    // 0x691
        None,                                    // 0x692
        None,                                    // 0x693
        None,                                    // 0x6a0
        None,                                    // 0x6a1
        None,                                    // 0x6a2
        None,                                    // 0x6a3
        None,                                    // 0x6b0
        None,                                    // 0x6b1
        None,                                    // 0x6b2
        None,                                    // 0x6b3
        None,                                    // 0x6c0
        None,                                    // 0x6c1
        None,                                    // 0x6c2
        None,                                    // 0x6c3
        None,                                    // 0x6d0
        None,                                    // 0x6d1
        None,                                    // 0x6d2
        None,                                    // 0x6d3
        None,                                    // 0x6e0
        None,                                    // 0x6e1
        None,                                    // 0x6e2
        None,                                    // 0x6e3
        None,                                    // 0x6f0
        None,                                    // 0x6f1
        None,                                    // 0x6f2
        None,                                    // 0x6f3
        None,                                    // 0x700
        None,                                    // 0x701
        None,                                    // 0x702
        None,                                    // 0x703
        None,                                    // 0x710
        None,                                    // 0x711
        None,                                    // 0x712
        None,                                    // 0x713
        None,                                    // 0x720
        None,                                    // 0x721
        None,                                    // 0x722
        None,                                    // 0x723
        None,                                    // 0x730
        None,                                    // 0x731
        None,                                    // 0x732
        None,                                    // 0x733
        None,                                    // 0x740
        None,                                    // 0x741
        None,                                    // 0x742
        None,                                    // 0x743
        None,                                    // 0x750
        None,                                    // 0x751
        None,                                    // 0x752
        None,                                    // 0x753
        None,                                    // 0x760
        None,                                    // 0x761
        None,                                    // 0x762
        None,                                    // 0x763
        None,                                    // 0x770
        None,                                    // 0x771
        None,                                    // 0x772
        None,                                    // 0x773
        None,                                    // 0x780
        None,                                    // 0x781
        None,                                    // 0x782
        None,                                    // 0x783
        None,                                    // 0x790
        None,                                    // 0x791
        None,                                    // 0x792
        None,                                    // 0x793
        None,                                    // 0x7a0
        None,                                    // 0x7a1
        None,                                    // 0x7a2
        None,                                    // 0x7a3
        None,                                    // 0x7b0
        None,                                    // 0x7b1
        None,                                    // 0x7b2
        None,                                    // 0x7b3
        None,                                    // 0x7c0
        None,                                    // 0x7c1
        None,                                    // 0x7c2
        None,                                    // 0x7c3
        None,                                    // 0x7d0
        None,                                    // 0x7d1
        None,                                    // 0x7d2
        None,                                    // 0x7d3
        None,                                    // 0x7e0
        None,                                    // 0x7e1
        None,                                    // 0x7e2
        None,                                    // 0x7e3
        None,                                    // 0x7f0
        None,                                    // 0x7f1
        None,                                    // 0x7f2
        None,                                    // 0x7f3
        MOp_L_M32B_or_M16B,                      // 0x800 vfrczps
        MOp_L_M32B_or_M16B,                      // 0x801 vfrczps
        MOp_L_M32B_or_M16B,                      // 0x802 vfrczps
        MOp_L_M32B_or_M16B,                      // 0x803 vfrczps
        MOp_L_M32B_or_M16B,                      // 0x810 vfrczpd
        MOp_L_M32B_or_M16B,                      // 0x811 vfrczpd
        MOp_L_M32B_or_M16B,                      // 0x812 vfrczpd
        MOp_L_M32B_or_M16B,                      // 0x813 vfrczpd
        MOp_M4B,                                 // 0x820 vfrczss
        MOp_M4B,                                 // 0x821 vfrczss
        MOp_M4B,                                 // 0x822 vfrczss
        MOp_M4B,                                 // 0x823 vfrczss
        MOp_M8B,                                 // 0x830 vfrczsd
        MOp_M8B,                                 // 0x831 vfrczsd
        MOp_M8B,                                 // 0x832 vfrczsd
        MOp_M8B,                                 // 0x833 vfrczsd
        None,                                    // 0x840
        None,                                    // 0x841
        None,                                    // 0x842
        None,                                    // 0x843
        None,                                    // 0x850
        None,                                    // 0x851
        None,                                    // 0x852
        None,                                    // 0x853
        None,                                    // 0x860
        None,                                    // 0x861
        None,                                    // 0x862
        None,                                    // 0x863
        None,                                    // 0x870
        None,                                    // 0x871
        None,                                    // 0x872
        None,                                    // 0x873
        None,                                    // 0x880
        None,                                    // 0x881
        None,                                    // 0x882
        None,                                    // 0x883
        None,                                    // 0x890
        None,                                    // 0x891
        None,                                    // 0x892
        None,                                    // 0x893
        None,                                    // 0x8a0
        None,                                    // 0x8a1
        None,                                    // 0x8a2
        None,                                    // 0x8a3
        None,                                    // 0x8b0
        None,                                    // 0x8b1
        None,                                    // 0x8b2
        None,                                    // 0x8b3
        None,                                    // 0x8c0
        None,                                    // 0x8c1
        None,                                    // 0x8c2
        None,                                    // 0x8c3
        None,                                    // 0x8d0
        None,                                    // 0x8d1
        None,                                    // 0x8d2
        None,                                    // 0x8d3
        None,                                    // 0x8e0
        None,                                    // 0x8e1
        None,                                    // 0x8e2
        None,                                    // 0x8e3
        None,                                    // 0x8f0
        None,                                    // 0x8f1
        None,                                    // 0x8f2
        None,                                    // 0x8f3
        MOp_M16B,                                // 0x900 vprotb
        MOp_M16B,                                // 0x901 vprotb
        MOp_M16B,                                // 0x902 vprotb
        MOp_M16B,                                // 0x903 vprotb
        MOp_M16B,                                // 0x910 vprotw
        MOp_M16B,                                // 0x911 vprotw
        MOp_M16B,                                // 0x912 vprotw
        MOp_M16B,                                // 0x913 vprotw
        MOp_M16B,                                // 0x920 vprotd
        MOp_M16B,                                // 0x921 vprotd
        MOp_M16B,                                // 0x922 vprotd
        MOp_M16B,                                // 0x923 vprotd
        MOp_M16B,                                // 0x930 vprotq
        MOp_M16B,                                // 0x931 vprotq
        MOp_M16B,                                // 0x932 vprotq
        MOp_M16B,                                // 0x933 vprotq
        MOp_M16B,                                // 0x940 vpshlb
        MOp_M16B,                                // 0x941 vpshlb
        MOp_M16B,                                // 0x942 vpshlb
        MOp_M16B,                                // 0x943 vpshlb
        MOp_M16B,                                // 0x950 vpshlw
        MOp_M16B,                                // 0x951 vpshlw
        MOp_M16B,                                // 0x952 vpshlw
        MOp_M16B,                                // 0x953 vpshlw
        MOp_M16B,                                // 0x960 vpshld
        MOp_M16B,                                // 0x961 vpshld
        MOp_M16B,                                // 0x962 vpshld
        MOp_M16B,                                // 0x963 vpshld
        MOp_M16B,                                // 0x970 vpshlq
        MOp_M16B,                                // 0x971 vpshlq
        MOp_M16B,                                // 0x972 vpshlq
        MOp_M16B,                                // 0x973 vpshlq
        MOp_M16B,                                // 0x980 vpshab
        MOp_M16B,                                // 0x981 vpshab
        MOp_M16B,                                // 0x982 vpshab
        MOp_M16B,                                // 0x983 vpshab
        MOp_M16B,                                // 0x990 vpshaw
        MOp_M16B,                                // 0x991 vpshaw
        MOp_M16B,                                // 0x992 vpshaw
        MOp_M16B,                                // 0x993 vpshaw
        MOp_M16B,                                // 0x9a0 vpshad
        MOp_M16B,                                // 0x9a1 vpshad
        MOp_M16B,                                // 0x9a2 vpshad
        MOp_M16B,                                // 0x9a3 vpshad
        MOp_M16B,                                // 0x9b0 vpshaq
        MOp_M16B,                                // 0x9b1 vpshaq
        MOp_M16B,                                // 0x9b2 vpshaq
        MOp_M16B,                                // 0x9b3 vpshaq
        None,                                    // 0x9c0
        None,                                    // 0x9c1
        None,                                    // 0x9c2
        None,                                    // 0x9c3
        None,                                    // 0x9d0
        None,                                    // 0x9d1
        None,                                    // 0x9d2
        None,                                    // 0x9d3
        None,                                    // 0x9e0
        None,                                    // 0x9e1
        None,                                    // 0x9e2
        None,                                    // 0x9e3
        None,                                    // 0x9f0
        None,                                    // 0x9f1
        None,                                    // 0x9f2
        None,                                    // 0x9f3
        None,                                    // 0xa00
        None,                                    // 0xa01
        None,                                    // 0xa02
        None,                                    // 0xa03
        None,                                    // 0xa10
        None,                                    // 0xa11
        None,                                    // 0xa12
        None,                                    // 0xa13
        None,                                    // 0xa20
        None,                                    // 0xa21
        None,                                    // 0xa22
        None,                                    // 0xa23
        None,                                    // 0xa30
        None,                                    // 0xa31
        None,                                    // 0xa32
        None,                                    // 0xa33
        None,                                    // 0xa40
        None,                                    // 0xa41
        None,                                    // 0xa42
        None,                                    // 0xa43
        None,                                    // 0xa50
        None,                                    // 0xa51
        None,                                    // 0xa52
        None,                                    // 0xa53
        None,                                    // 0xa60
        None,                                    // 0xa61
        None,                                    // 0xa62
        None,                                    // 0xa63
        None,                                    // 0xa70
        None,                                    // 0xa71
        None,                                    // 0xa72
        None,                                    // 0xa73
        None,                                    // 0xa80
        None,                                    // 0xa81
        None,                                    // 0xa82
        None,                                    // 0xa83
        None,                                    // 0xa90
        None,                                    // 0xa91
        None,                                    // 0xa92
        None,                                    // 0xa93
        None,                                    // 0xaa0
        None,                                    // 0xaa1
        None,                                    // 0xaa2
        None,                                    // 0xaa3
        None,                                    // 0xab0
        None,                                    // 0xab1
        None,                                    // 0xab2
        None,                                    // 0xab3
        None,                                    // 0xac0
        None,                                    // 0xac1
        None,                                    // 0xac2
        None,                                    // 0xac3
        None,                                    // 0xad0
        None,                                    // 0xad1
        None,                                    // 0xad2
        None,                                    // 0xad3
        None,                                    // 0xae0
        None,                                    // 0xae1
        None,                                    // 0xae2
        None,                                    // 0xae3
        None,                                    // 0xaf0
        None,                                    // 0xaf1
        None,                                    // 0xaf2
        None,                                    // 0xaf3
        None,                                    // 0xb00
        None,                                    // 0xb01
        None,                                    // 0xb02
        None,                                    // 0xb03
        None,                                    // 0xb10
        None,                                    // 0xb11
        None,                                    // 0xb12
        None,                                    // 0xb13
        None,                                    // 0xb20
        None,                                    // 0xb21
        None,                                    // 0xb22
        None,                                    // 0xb23
        None,                                    // 0xb30
        None,                                    // 0xb31
        None,                                    // 0xb32
        None,                                    // 0xb33
        None,                                    // 0xb40
        None,                                    // 0xb41
        None,                                    // 0xb42
        None,                                    // 0xb43
        None,                                    // 0xb50
        None,                                    // 0xb51
        None,                                    // 0xb52
        None,                                    // 0xb53
        None,                                    // 0xb60
        None,                                    // 0xb61
        None,                                    // 0xb62
        None,                                    // 0xb63
        None,                                    // 0xb70
        None,                                    // 0xb71
        None,                                    // 0xb72
        None,                                    // 0xb73
        None,                                    // 0xb80
        None,                                    // 0xb81
        None,                                    // 0xb82
        None,                                    // 0xb83
        None,                                    // 0xb90
        None,                                    // 0xb91
        None,                                    // 0xb92
        None,                                    // 0xb93
        None,                                    // 0xba0
        None,                                    // 0xba1
        None,                                    // 0xba2
        None,                                    // 0xba3
        None,                                    // 0xbb0
        None,                                    // 0xbb1
        None,                                    // 0xbb2
        None,                                    // 0xbb3
        None,                                    // 0xbc0
        None,                                    // 0xbc1
        None,                                    // 0xbc2
        None,                                    // 0xbc3
        None,                                    // 0xbd0
        None,                                    // 0xbd1
        None,                                    // 0xbd2
        None,                                    // 0xbd3
        None,                                    // 0xbe0
        None,                                    // 0xbe1
        None,                                    // 0xbe2
        None,                                    // 0xbe3
        None,                                    // 0xbf0
        None,                                    // 0xbf1
        None,                                    // 0xbf2
        None,                                    // 0xbf3
        None,                                    // 0xc00
        None,                                    // 0xc01
        None,                                    // 0xc02
        None,                                    // 0xc03
        MOp_M16B,                                // 0xc10 vphaddbw
        MOp_M16B,                                // 0xc11 vphaddbw
        MOp_M16B,                                // 0xc12 vphaddbw
        MOp_M16B,                                // 0xc13 vphaddbw
        MOp_M16B,                                // 0xc20 vphaddbd
        MOp_M16B,                                // 0xc21 vphaddbd
        MOp_M16B,                                // 0xc22 vphaddbd
        MOp_M16B,                                // 0xc23 vphaddbd
        MOp_M16B,                                // 0xc30 vphaddbq
        MOp_M16B,                                // 0xc31 vphaddbq
        MOp_M16B,                                // 0xc32 vphaddbq
        MOp_M16B,                                // 0xc33 vphaddbq
        None,                                    // 0xc40
        None,                                    // 0xc41
        None,                                    // 0xc42
        None,                                    // 0xc43
        None,                                    // 0xc50
        None,                                    // 0xc51
        None,                                    // 0xc52
        None,                                    // 0xc53
        MOp_M16B,                                // 0xc60 vphaddwd
        MOp_M16B,                                // 0xc61 vphaddwd
        MOp_M16B,                                // 0xc62 vphaddwd
        MOp_M16B,                                // 0xc63 vphaddwd
        MOp_M16B,                                // 0xc70 vphaddwq
        MOp_M16B,                                // 0xc71 vphaddwq
        MOp_M16B,                                // 0xc72 vphaddwq
        MOp_M16B,                                // 0xc73 vphaddwq
        None,                                    // 0xc80
        None,                                    // 0xc81
        None,                                    // 0xc82
        None,                                    // 0xc83
        None,                                    // 0xc90
        None,                                    // 0xc91
        None,                                    // 0xc92
        None,                                    // 0xc93
        None,                                    // 0xca0
        None,                                    // 0xca1
        None,                                    // 0xca2
        None,                                    // 0xca3
        MOp_M16B,                                // 0xcb0 vphadddq
        MOp_M16B,                                // 0xcb1 vphadddq
        MOp_M16B,                                // 0xcb2 vphadddq
        MOp_M16B,                                // 0xcb3 vphadddq
        None,                                    // 0xcc0
        None,                                    // 0xcc1
        None,                                    // 0xcc2
        None,                                    // 0xcc3
        None,                                    // 0xcd0
        None,                                    // 0xcd1
        None,                                    // 0xcd2
        None,                                    // 0xcd3
        None,                                    // 0xce0
        None,                                    // 0xce1
        None,                                    // 0xce2
        None,                                    // 0xce3
        None,                                    // 0xcf0
        None,                                    // 0xcf1
        None,                                    // 0xcf2
        None,                                    // 0xcf3
        None,                                    // 0xd00
        None,                                    // 0xd01
        None,                                    // 0xd02
        None,                                    // 0xd03
        MOp_M16B,                                // 0xd10 vphaddubw
        MOp_M16B,                                // 0xd11 vphaddubw
        MOp_M16B,                                // 0xd12 vphaddubw
        MOp_M16B,                                // 0xd13 vphaddubw
        MOp_M16B,                                // 0xd20 vphaddubd
        MOp_M16B,                                // 0xd21 vphaddubd
        MOp_M16B,                                // 0xd22 vphaddubd
        MOp_M16B,                                // 0xd23 vphaddubd
        MOp_M16B,                                // 0xd30 vphaddubq
        MOp_M16B,                                // 0xd31 vphaddubq
        MOp_M16B,                                // 0xd32 vphaddubq
        MOp_M16B,                                // 0xd33 vphaddubq
        None,                                    // 0xd40
        None,                                    // 0xd41
        None,                                    // 0xd42
        None,                                    // 0xd43
        None,                                    // 0xd50
        None,                                    // 0xd51
        None,                                    // 0xd52
        None,                                    // 0xd53
        MOp_M16B,                                // 0xd60 vphadduwd
        MOp_M16B,                                // 0xd61 vphadduwd
        MOp_M16B,                                // 0xd62 vphadduwd
        MOp_M16B,                                // 0xd63 vphadduwd
        MOp_M16B,                                // 0xd70 vphadduwq
        MOp_M16B,                                // 0xd71 vphadduwq
        MOp_M16B,                                // 0xd72 vphadduwq
        MOp_M16B,                                // 0xd73 vphadduwq
        None,                                    // 0xd80
        None,                                    // 0xd81
        None,                                    // 0xd82
        None,                                    // 0xd83
        None,                                    // 0xd90
        None,                                    // 0xd91
        None,                                    // 0xd92
        None,                                    // 0xd93
        None,                                    // 0xda0
        None,                                    // 0xda1
        None,                                    // 0xda2
        None,                                    // 0xda3
        MOp_M16B,                                // 0xdb0 vphaddudq
        MOp_M16B,                                // 0xdb1 vphaddudq
        MOp_M16B,                                // 0xdb2 vphaddudq
        MOp_M16B,                                // 0xdb3 vphaddudq
        None,                                    // 0xdc0
        None,                                    // 0xdc1
        None,                                    // 0xdc2
        None,                                    // 0xdc3
        None,                                    // 0xdd0
        None,                                    // 0xdd1
        None,                                    // 0xdd2
        None,                                    // 0xdd3
        None,                                    // 0xde0
        None,                                    // 0xde1
        None,                                    // 0xde2
        None,                                    // 0xde3
        None,                                    // 0xdf0
        None,                                    // 0xdf1
        None,                                    // 0xdf2
        None,                                    // 0xdf3
        None,                                    // 0xe00
        None,                                    // 0xe01
        None,                                    // 0xe02
        None,                                    // 0xe03
        MOp_M16B,                                // 0xe10 vphsubbw
        MOp_M16B,                                // 0xe11 vphsubbw
        MOp_M16B,                                // 0xe12 vphsubbw
        MOp_M16B,                                // 0xe13 vphsubbw
        MOp_M16B,                                // 0xe20 vphsubwd
        MOp_M16B,                                // 0xe21 vphsubwd
        MOp_M16B,                                // 0xe22 vphsubwd
        MOp_M16B,                                // 0xe23 vphsubwd
        MOp_M16B,                                // 0xe30 vphsubdq
        MOp_M16B,                                // 0xe31 vphsubdq
        MOp_M16B,                                // 0xe32 vphsubdq
        MOp_M16B,                                // 0xe33 vphsubdq
        None,                                    // 0xe40
        None,                                    // 0xe41
        None,                                    // 0xe42
        None,                                    // 0xe43
        None,                                    // 0xe50
        None,                                    // 0xe51
        None,                                    // 0xe52
        None,                                    // 0xe53
        None,                                    // 0xe60
        None,                                    // 0xe61
        None,                                    // 0xe62
        None,                                    // 0xe63
        None,                                    // 0xe70
        None,                                    // 0xe71
        None,                                    // 0xe72
        None,                                    // 0xe73
        None,                                    // 0xe80
        None,                                    // 0xe81
        None,                                    // 0xe82
        None,                                    // 0xe83
        None,                                    // 0xe90
        None,                                    // 0xe91
        None,                                    // 0xe92
        None,                                    // 0xe93
        None,                                    // 0xea0
        None,                                    // 0xea1
        None,                                    // 0xea2
        None,                                    // 0xea3
        None,                                    // 0xeb0
        None,                                    // 0xeb1
        None,                                    // 0xeb2
        None,                                    // 0xeb3
        None,                                    // 0xec0
        None,                                    // 0xec1
        None,                                    // 0xec2
        None,                                    // 0xec3
        None,                                    // 0xed0
        None,                                    // 0xed1
        None,                                    // 0xed2
        None,                                    // 0xed3
        None,                                    // 0xee0
        None,                                    // 0xee1
        None,                                    // 0xee2
        None,                                    // 0xee3
        None,                                    // 0xef0
        None,                                    // 0xef1
        None,                                    // 0xef2
        None,                                    // 0xef3
        None,                                    // 0xf00
        None,                                    // 0xf01
        None,                                    // 0xf02
        None,                                    // 0xf03
        None,                                    // 0xf10
        None,                                    // 0xf11
        None,                                    // 0xf12
        None,                                    // 0xf13
        None,                                    // 0xf20
        None,                                    // 0xf21
        None,                                    // 0xf22
        None,                                    // 0xf23
        None,                                    // 0xf30
        None,                                    // 0xf31
        None,                                    // 0xf32
        None,                                    // 0xf33
        None,                                    // 0xf40
        None,                                    // 0xf41
        None,                                    // 0xf42
        None,                                    // 0xf43
        None,                                    // 0xf50
        None,                                    // 0xf51
        None,                                    // 0xf52
        None,                                    // 0xf53
        None,                                    // 0xf60
        None,                                    // 0xf61
        None,                                    // 0xf62
        None,                                    // 0xf63
        None,                                    // 0xf70
        None,                                    // 0xf71
        None,                                    // 0xf72
        None,                                    // 0xf73
        None,                                    // 0xf80
        None,                                    // 0xf81
        None,                                    // 0xf82
        None,                                    // 0xf83
        None,                                    // 0xf90
        None,                                    // 0xf91
        None,                                    // 0xf92
        None,                                    // 0xf93
        None,                                    // 0xfa0
        None,                                    // 0xfa1
        None,                                    // 0xfa2
        None,                                    // 0xfa3
        None,                                    // 0xfb0
        None,                                    // 0xfb1
        None,                                    // 0xfb2
        None,                                    // 0xfb3
        None,                                    // 0xfc0
        None,                                    // 0xfc1
        None,                                    // 0xfc2
        None,                                    // 0xfc3
        None,                                    // 0xfd0
        None,                                    // 0xfd1
        None,                                    // 0xfd2
        None,                                    // 0xfd3
        None,                                    // 0xfe0
        None,                                    // 0xfe1
        None,                                    // 0xfe2
        None,                                    // 0xfe3
        None,                                    // 0xff0
        None,                                    // 0xff1
        None,                                    // 0xff2
        None,                                    // 0xff3
    };

    static const InstrForm instrFormXOPA[1024]
    {
        None,                                    // 0x000
        None,                                    // 0x001
        None,                                    // 0x002
        None,                                    // 0x003
        None,                                    // 0x010
        None,                                    // 0x011
        None,                                    // 0x012
        None,                                    // 0x013
        None,                                    // 0x020
        None,                                    // 0x021
        None,                                    // 0x022
        None,                                    // 0x023
        None,                                    // 0x030
        None,                                    // 0x031
        None,                                    // 0x032
        None,                                    // 0x033
        None,                                    // 0x040
        None,                                    // 0x041
        None,                                    // 0x042
        None,                                    // 0x043
        None,                                    // 0x050
        None,                                    // 0x051
        None,                                    // 0x052
        None,                                    // 0x053
        None,                                    // 0x060
        None,                                    // 0x061
        None,                                    // 0x062
        None,                                    // 0x063
        None,                                    // 0x070
        None,                                    // 0x071
        None,                                    // 0x072
        None,                                    // 0x073
        None,                                    // 0x080
        None,                                    // 0x081
        None,                                    // 0x082
        None,                                    // 0x083
        None,                                    // 0x090
        None,                                    // 0x091
        None,                                    // 0x092
        None,                                    // 0x093
        None,                                    // 0x0a0
        None,                                    // 0x0a1
        None,                                    // 0x0a2
        None,                                    // 0x0a3
        None,                                    // 0x0b0
        None,                                    // 0x0b1
        None,                                    // 0x0b2
        None,                                    // 0x0b3
        None,                                    // 0x0c0
        None,                                    // 0x0c1
        None,                                    // 0x0c2
        None,                                    // 0x0c3
        None,                                    // 0x0d0
        None,                                    // 0x0d1
        None,                                    // 0x0d2
        None,                                    // 0x0d3
        None,                                    // 0x0e0
        None,                                    // 0x0e1
        None,                                    // 0x0e2
        None,                                    // 0x0e3
        None,                                    // 0x0f0
        None,                                    // 0x0f1
        None,                                    // 0x0f2
        None,                                    // 0x0f3
        MOp_I4B_W_M8B_or_M4B,                    // 0x100 bextr
        MOp_I4B_W_M8B_or_M4B,                    // 0x101 bextr
        MOp_I4B_W_M8B_or_M4B,                    // 0x102 bextr
        MOp_I4B_W_M8B_or_M4B,                    // 0x103 bextr
        None,                                    // 0x110
        None,                                    // 0x111
        None,                                    // 0x112
        None,                                    // 0x113
        MOp_M4B_I4B,                             // 0x120 lwpins,lwpval
        MOp_M4B_I4B,                             // 0x121 lwpins,lwpval
        MOp_M4B_I4B,                             // 0x122 lwpins,lwpval
        None,                                    // 0x123
        None,                                    // 0x130
        None,                                    // 0x131
        None,                                    // 0x132
        None,                                    // 0x133
        None,                                    // 0x140
        None,                                    // 0x141
        None,                                    // 0x142
        None,                                    // 0x143
        None,                                    // 0x150
        None,                                    // 0x151
        None,                                    // 0x152
        None,                                    // 0x153
        None,                                    // 0x160
        None,                                    // 0x161
        None,                                    // 0x162
        None,                                    // 0x163
        None,                                    // 0x170
        None,                                    // 0x171
        None,                                    // 0x172
        None,                                    // 0x173
        None,                                    // 0x180
        None,                                    // 0x181
        None,                                    // 0x182
        None,                                    // 0x183
        None,                                    // 0x190
        None,                                    // 0x191
        None,                                    // 0x192
        None,                                    // 0x193
        None,                                    // 0x1a0
        None,                                    // 0x1a1
        None,                                    // 0x1a2
        None,                                    // 0x1a3
        None,                                    // 0x1b0
        None,                                    // 0x1b1
        None,                                    // 0x1b2
        None,                                    // 0x1b3
        None,                                    // 0x1c0
        None,                                    // 0x1c1
        None,                                    // 0x1c2
        None,                                    // 0x1c3
        None,                                    // 0x1d0
        None,                                    // 0x1d1
        None,                                    // 0x1d2
        None,                                    // 0x1d3
        None,                                    // 0x1e0
        None,                                    // 0x1e1
        None,                                    // 0x1e2
        None,                                    // 0x1e3
        None,                                    // 0x1f0
        None,                                    // 0x1f1
        None,                                    // 0x1f2
        None,                                    // 0x1f3
        None,                                    // 0x200
        None,                                    // 0x201
        None,                                    // 0x202
        None,                                    // 0x203
        None,                                    // 0x210
        None,                                    // 0x211
        None,                                    // 0x212
        None,                                    // 0x213
        None,                                    // 0x220
        None,                                    // 0x221
        None,                                    // 0x222
        None,                                    // 0x223
        None,                                    // 0x230
        None,                                    // 0x231
        None,                                    // 0x232
        None,                                    // 0x233
        None,                                    // 0x240
        None,                                    // 0x241
        None,                                    // 0x242
        None,                                    // 0x243
        None,                                    // 0x250
        None,                                    // 0x251
        None,                                    // 0x252
        None,                                    // 0x253
        None,                                    // 0x260
        None,                                    // 0x261
        None,                                    // 0x262
        None,                                    // 0x263
        None,                                    // 0x270
        None,                                    // 0x271
        None,                                    // 0x272
        None,                                    // 0x273
        None,                                    // 0x280
        None,                                    // 0x281
        None,                                    // 0x282
        None,                                    // 0x283
        None,                                    // 0x290
        None,                                    // 0x291
        None,                                    // 0x292
        None,                                    // 0x293
        None,                                    // 0x2a0
        None,                                    // 0x2a1
        None,                                    // 0x2a2
        None,                                    // 0x2a3
        None,                                    // 0x2b0
        None,                                    // 0x2b1
        None,                                    // 0x2b2
        None,                                    // 0x2b3
        None,                                    // 0x2c0
        None,                                    // 0x2c1
        None,                                    // 0x2c2
        None,                                    // 0x2c3
        None,                                    // 0x2d0
        None,                                    // 0x2d1
        None,                                    // 0x2d2
        None,                                    // 0x2d3
        None,                                    // 0x2e0
        None,                                    // 0x2e1
        None,                                    // 0x2e2
        None,                                    // 0x2e3
        None,                                    // 0x2f0
        None,                                    // 0x2f1
        None,                                    // 0x2f2
        None,                                    // 0x2f3
        None,                                    // 0x300
        None,                                    // 0x301
        None,                                    // 0x302
        None,                                    // 0x303
        None,                                    // 0x310
        None,                                    // 0x311
        None,                                    // 0x312
        None,                                    // 0x313
        None,                                    // 0x320
        None,                                    // 0x321
        None,                                    // 0x322
        None,                                    // 0x323
        None,                                    // 0x330
        None,                                    // 0x331
        None,                                    // 0x332
        None,                                    // 0x333
        None,                                    // 0x340
        None,                                    // 0x341
        None,                                    // 0x342
        None,                                    // 0x343
        None,                                    // 0x350
        None,                                    // 0x351
        None,                                    // 0x352
        None,                                    // 0x353
        None,                                    // 0x360
        None,                                    // 0x361
        None,                                    // 0x362
        None,                                    // 0x363
        None,                                    // 0x370
        None,                                    // 0x371
        None,                                    // 0x372
        None,                                    // 0x373
        None,                                    // 0x380
        None,                                    // 0x381
        None,                                    // 0x382
        None,                                    // 0x383
        None,                                    // 0x390
        None,                                    // 0x391
        None,                                    // 0x392
        None,                                    // 0x393
        None,                                    // 0x3a0
        None,                                    // 0x3a1
        None,                                    // 0x3a2
        None,                                    // 0x3a3
        None,                                    // 0x3b0
        None,                                    // 0x3b1
        None,                                    // 0x3b2
        None,                                    // 0x3b3
        None,                                    // 0x3c0
        None,                                    // 0x3c1
        None,                                    // 0x3c2
        None,                                    // 0x3c3
        None,                                    // 0x3d0
        None,                                    // 0x3d1
        None,                                    // 0x3d2
        None,                                    // 0x3d3
        None,                                    // 0x3e0
        None,                                    // 0x3e1
        None,                                    // 0x3e2
        None,                                    // 0x3e3
        None,                                    // 0x3f0
        None,                                    // 0x3f1
        None,                                    // 0x3f2
        None,                                    // 0x3f3
        None,                                    // 0x400
        None,                                    // 0x401
        None,                                    // 0x402
        None,                                    // 0x403
        None,                                    // 0x410
        None,                                    // 0x411
        None,                                    // 0x412
        None,                                    // 0x413
        None,                                    // 0x420
        None,                                    // 0x421
        None,                                    // 0x422
        None,                                    // 0x423
        None,                                    // 0x430
        None,                                    // 0x431
        None,                                    // 0x432
        None,                                    // 0x433
        None,                                    // 0x440
        None,                                    // 0x441
        None,                                    // 0x442
        None,                                    // 0x443
        None,                                    // 0x450
        None,                                    // 0x451
        None,                                    // 0x452
        None,                                    // 0x453
        None,                                    // 0x460
        None,                                    // 0x461
        None,                                    // 0x462
        None,                                    // 0x463
        None,                                    // 0x470
        None,                                    // 0x471
        None,                                    // 0x472
        None,                                    // 0x473
        None,                                    // 0x480
        None,                                    // 0x481
        None,                                    // 0x482
        None,                                    // 0x483
        None,                                    // 0x490
        None,                                    // 0x491
        None,                                    // 0x492
        None,                                    // 0x493
        None,                                    // 0x4a0
        None,                                    // 0x4a1
        None,                                    // 0x4a2
        None,                                    // 0x4a3
        None,                                    // 0x4b0
        None,                                    // 0x4b1
        None,                                    // 0x4b2
        None,                                    // 0x4b3
        None,                                    // 0x4c0
        None,                                    // 0x4c1
        None,                                    // 0x4c2
        None,                                    // 0x4c3
        None,                                    // 0x4d0
        None,                                    // 0x4d1
        None,                                    // 0x4d2
        None,                                    // 0x4d3
        None,                                    // 0x4e0
        None,                                    // 0x4e1
        None,                                    // 0x4e2
        None,                                    // 0x4e3
        None,                                    // 0x4f0
        None,                                    // 0x4f1
        None,                                    // 0x4f2
        None,                                    // 0x4f3
        None,                                    // 0x500
        None,                                    // 0x501
        None,                                    // 0x502
        None,                                    // 0x503
        None,                                    // 0x510
        None,                                    // 0x511
        None,                                    // 0x512
        None,                                    // 0x513
        None,                                    // 0x520
        None,                                    // 0x521
        None,                                    // 0x522
        None,                                    // 0x523
        None,                                    // 0x530
        None,                                    // 0x531
        None,                                    // 0x532
        None,                                    // 0x533
        None,                                    // 0x540
        None,                                    // 0x541
        None,                                    // 0x542
        None,                                    // 0x543
        None,                                    // 0x550
        None,                                    // 0x551
        None,                                    // 0x552
        None,                                    // 0x553
        None,                                    // 0x560
        None,                                    // 0x561
        None,                                    // 0x562
        None,                                    // 0x563
        None,                                    // 0x570
        None,                                    // 0x571
        None,                                    // 0x572
        None,                                    // 0x573
        None,                                    // 0x580
        None,                                    // 0x581
        None,                                    // 0x582
        None,                                    // 0x583
        None,                                    // 0x590
        None,                                    // 0x591
        None,                                    // 0x592
        None,                                    // 0x593
        None,                                    // 0x5a0
        None,                                    // 0x5a1
        None,                                    // 0x5a2
        None,                                    // 0x5a3
        None,                                    // 0x5b0
        None,                                    // 0x5b1
        None,                                    // 0x5b2
        None,                                    // 0x5b3
        None,                                    // 0x5c0
        None,                                    // 0x5c1
        None,                                    // 0x5c2
        None,                                    // 0x5c3
        None,                                    // 0x5d0
        None,                                    // 0x5d1
        None,                                    // 0x5d2
        None,                                    // 0x5d3
        None,                                    // 0x5e0
        None,                                    // 0x5e1
        None,                                    // 0x5e2
        None,                                    // 0x5e3
        None,                                    // 0x5f0
        None,                                    // 0x5f1
        None,                                    // 0x5f2
        None,                                    // 0x5f3
        None,                                    // 0x600
        None,                                    // 0x601
        None,                                    // 0x602
        None,                                    // 0x603
        None,                                    // 0x610
        None,                                    // 0x611
        None,                                    // 0x612
        None,                                    // 0x613
        None,                                    // 0x620
        None,                                    // 0x621
        None,                                    // 0x622
        None,                                    // 0x623
        None,                                    // 0x630
        None,                                    // 0x631
        None,                                    // 0x632
        None,                                    // 0x633
        None,                                    // 0x640
        None,                                    // 0x641
        None,                                    // 0x642
        None,                                    // 0x643
        None,                                    // 0x650
        None,                                    // 0x651
        None,                                    // 0x652
        None,                                    // 0x653
        None,                                    // 0x660
        None,                                    // 0x661
        None,                                    // 0x662
        None,                                    // 0x663
        None,                                    // 0x670
        None,                                    // 0x671
        None,                                    // 0x672
        None,                                    // 0x673
        None,                                    // 0x680
        None,                                    // 0x681
        None,                                    // 0x682
        None,                                    // 0x683
        None,                                    // 0x690
        None,                                    // 0x691
        None,                                    // 0x692
        None,                                    // 0x693
        None,                                    // 0x6a0
        None,                                    // 0x6a1
        None,                                    // 0x6a2
        None,                                    // 0x6a3
        None,                                    // 0x6b0
        None,                                    // 0x6b1
        None,                                    // 0x6b2
        None,                                    // 0x6b3
        None,                                    // 0x6c0
        None,                                    // 0x6c1
        None,                                    // 0x6c2
        None,                                    // 0x6c3
        None,                                    // 0x6d0
        None,                                    // 0x6d1
        None,                                    // 0x6d2
        None,                                    // 0x6d3
        None,                                    // 0x6e0
        None,                                    // 0x6e1
        None,                                    // 0x6e2
        None,                                    // 0x6e3
        None,                                    // 0x6f0
        None,                                    // 0x6f1
        None,                                    // 0x6f2
        None,                                    // 0x6f3
        None,                                    // 0x700
        None,                                    // 0x701
        None,                                    // 0x702
        None,                                    // 0x703
        None,                                    // 0x710
        None,                                    // 0x711
        None,                                    // 0x712
        None,                                    // 0x713
        None,                                    // 0x720
        None,                                    // 0x721
        None,                                    // 0x722
        None,                                    // 0x723
        None,                                    // 0x730
        None,                                    // 0x731
        None,                                    // 0x732
        None,                                    // 0x733
        None,                                    // 0x740
        None,                                    // 0x741
        None,                                    // 0x742
        None,                                    // 0x743
        None,                                    // 0x750
        None,                                    // 0x751
        None,                                    // 0x752
        None,                                    // 0x753
        None,                                    // 0x760
        None,                                    // 0x761
        None,                                    // 0x762
        None,                                    // 0x763
        None,                                    // 0x770
        None,                                    // 0x771
        None,                                    // 0x772
        None,                                    // 0x773
        None,                                    // 0x780
        None,                                    // 0x781
        None,                                    // 0x782
        None,                                    // 0x783
        None,                                    // 0x790
        None,                                    // 0x791
        None,                                    // 0x792
        None,                                    // 0x793
        None,                                    // 0x7a0
        None,                                    // 0x7a1
        None,                                    // 0x7a2
        None,                                    // 0x7a3
        None,                                    // 0x7b0
        None,                                    // 0x7b1
        None,                                    // 0x7b2
        None,                                    // 0x7b3
        None,                                    // 0x7c0
        None,                                    // 0x7c1
        None,                                    // 0x7c2
        None,                                    // 0x7c3
        None,                                    // 0x7d0
        None,                                    // 0x7d1
        None,                                    // 0x7d2
        None,                                    // 0x7d3
        None,                                    // 0x7e0
        None,                                    // 0x7e1
        None,                                    // 0x7e2
        None,                                    // 0x7e3
        None,                                    // 0x7f0
        None,                                    // 0x7f1
        None,                                    // 0x7f2
        None,                                    // 0x7f3
        None,                                    // 0x800
        None,                                    // 0x801
        None,                                    // 0x802
        None,                                    // 0x803
        None,                                    // 0x810
        None,                                    // 0x811
        None,                                    // 0x812
        None,                                    // 0x813
        None,                                    // 0x820
        None,                                    // 0x821
        None,                                    // 0x822
        None,                                    // 0x823
        None,                                    // 0x830
        None,                                    // 0x831
        None,                                    // 0x832
        None,                                    // 0x833
        None,                                    // 0x840
        None,                                    // 0x841
        None,                                    // 0x842
        None,                                    // 0x843
        None,                                    // 0x850
        None,                                    // 0x851
        None,                                    // 0x852
        None,                                    // 0x853
        None,                                    // 0x860
        None,                                    // 0x861
        None,                                    // 0x862
        None,                                    // 0x863
        None,                                    // 0x870
        None,                                    // 0x871
        None,                                    // 0x872
        None,                                    // 0x873
        None,                                    // 0x880
        None,                                    // 0x881
        None,                                    // 0x882
        None,                                    // 0x883
        None,                                    // 0x890
        None,                                    // 0x891
        None,                                    // 0x892
        None,                                    // 0x893
        None,                                    // 0x8a0
        None,                                    // 0x8a1
        None,                                    // 0x8a2
        None,                                    // 0x8a3
        None,                                    // 0x8b0
        None,                                    // 0x8b1
        None,                                    // 0x8b2
        None,                                    // 0x8b3
        None,                                    // 0x8c0
        None,                                    // 0x8c1
        None,                                    // 0x8c2
        None,                                    // 0x8c3
        None,                                    // 0x8d0
        None,                                    // 0x8d1
        None,                                    // 0x8d2
        None,                                    // 0x8d3
        None,                                    // 0x8e0
        None,                                    // 0x8e1
        None,                                    // 0x8e2
        None,                                    // 0x8e3
        None,                                    // 0x8f0
        None,                                    // 0x8f1
        None,                                    // 0x8f2
        None,                                    // 0x8f3
        None,                                    // 0x900
        None,                                    // 0x901
        None,                                    // 0x902
        None,                                    // 0x903
        None,                                    // 0x910
        None,                                    // 0x911
        None,                                    // 0x912
        None,                                    // 0x913
        None,                                    // 0x920
        None,                                    // 0x921
        None,                                    // 0x922
        None,                                    // 0x923
        None,                                    // 0x930
        None,                                    // 0x931
        None,                                    // 0x932
        None,                                    // 0x933
        None,                                    // 0x940
        None,                                    // 0x941
        None,                                    // 0x942
        None,                                    // 0x943
        None,                                    // 0x950
        None,                                    // 0x951
        None,                                    // 0x952
        None,                                    // 0x953
        None,                                    // 0x960
        None,                                    // 0x961
        None,                                    // 0x962
        None,                                    // 0x963
        None,                                    // 0x970
        None,                                    // 0x971
        None,                                    // 0x972
        None,                                    // 0x973
        None,                                    // 0x980
        None,                                    // 0x981
        None,                                    // 0x982
        None,                                    // 0x983
        None,                                    // 0x990
        None,                                    // 0x991
        None,                                    // 0x992
        None,                                    // 0x993
        None,                                    // 0x9a0
        None,                                    // 0x9a1
        None,                                    // 0x9a2
        None,                                    // 0x9a3
        None,                                    // 0x9b0
        None,                                    // 0x9b1
        None,                                    // 0x9b2
        None,                                    // 0x9b3
        None,                                    // 0x9c0
        None,                                    // 0x9c1
        None,                                    // 0x9c2
        None,                                    // 0x9c3
        None,                                    // 0x9d0
        None,                                    // 0x9d1
        None,                                    // 0x9d2
        None,                                    // 0x9d3
        None,                                    // 0x9e0
        None,                                    // 0x9e1
        None,                                    // 0x9e2
        None,                                    // 0x9e3
        None,                                    // 0x9f0
        None,                                    // 0x9f1
        None,                                    // 0x9f2
        None,                                    // 0x9f3
        None,                                    // 0xa00
        None,                                    // 0xa01
        None,                                    // 0xa02
        None,                                    // 0xa03
        None,                                    // 0xa10
        None,                                    // 0xa11
        None,                                    // 0xa12
        None,                                    // 0xa13
        None,                                    // 0xa20
        None,                                    // 0xa21
        None,                                    // 0xa22
        None,                                    // 0xa23
        None,                                    // 0xa30
        None,                                    // 0xa31
        None,                                    // 0xa32
        None,                                    // 0xa33
        None,                                    // 0xa40
        None,                                    // 0xa41
        None,                                    // 0xa42
        None,                                    // 0xa43
        None,                                    // 0xa50
        None,                                    // 0xa51
        None,                                    // 0xa52
        None,                                    // 0xa53
        None,                                    // 0xa60
        None,                                    // 0xa61
        None,                                    // 0xa62
        None,                                    // 0xa63
        None,                                    // 0xa70
        None,                                    // 0xa71
        None,                                    // 0xa72
        None,                                    // 0xa73
        None,                                    // 0xa80
        None,                                    // 0xa81
        None,                                    // 0xa82
        None,                                    // 0xa83
        None,                                    // 0xa90
        None,                                    // 0xa91
        None,                                    // 0xa92
        None,                                    // 0xa93
        None,                                    // 0xaa0
        None,                                    // 0xaa1
        None,                                    // 0xaa2
        None,                                    // 0xaa3
        None,                                    // 0xab0
        None,                                    // 0xab1
        None,                                    // 0xab2
        None,                                    // 0xab3
        None,                                    // 0xac0
        None,                                    // 0xac1
        None,                                    // 0xac2
        None,                                    // 0xac3
        None,                                    // 0xad0
        None,                                    // 0xad1
        None,                                    // 0xad2
        None,                                    // 0xad3
        None,                                    // 0xae0
        None,                                    // 0xae1
        None,                                    // 0xae2
        None,                                    // 0xae3
        None,                                    // 0xaf0
        None,                                    // 0xaf1
        None,                                    // 0xaf2
        None,                                    // 0xaf3
        None,                                    // 0xb00
        None,                                    // 0xb01
        None,                                    // 0xb02
        None,                                    // 0xb03
        None,                                    // 0xb10
        None,                                    // 0xb11
        None,                                    // 0xb12
        None,                                    // 0xb13
        None,                                    // 0xb20
        None,                                    // 0xb21
        None,                                    // 0xb22
        None,                                    // 0xb23
        None,                                    // 0xb30
        None,                                    // 0xb31
        None,                                    // 0xb32
        None,                                    // 0xb33
        None,                                    // 0xb40
        None,                                    // 0xb41
        None,                                    // 0xb42
        None,                                    // 0xb43
        None,                                    // 0xb50
        None,                                    // 0xb51
        None,                                    // 0xb52
        None,                                    // 0xb53
        None,                                    // 0xb60
        None,                                    // 0xb61
        None,                                    // 0xb62
        None,                                    // 0xb63
        None,                                    // 0xb70
        None,                                    // 0xb71
        None,                                    // 0xb72
        None,                                    // 0xb73
        None,                                    // 0xb80
        None,                                    // 0xb81
        None,                                    // 0xb82
        None,                                    // 0xb83
        None,                                    // 0xb90
        None,                                    // 0xb91
        None,                                    // 0xb92
        None,                                    // 0xb93
        None,                                    // 0xba0
        None,                                    // 0xba1
        None,                                    // 0xba2
        None,                                    // 0xba3
        None,                                    // 0xbb0
        None,                                    // 0xbb1
        None,                                    // 0xbb2
        None,                                    // 0xbb3
        None,                                    // 0xbc0
        None,                                    // 0xbc1
        None,                                    // 0xbc2
        None,                                    // 0xbc3
        None,                                    // 0xbd0
        None,                                    // 0xbd1
        None,                                    // 0xbd2
        None,                                    // 0xbd3
        None,                                    // 0xbe0
        None,                                    // 0xbe1
        None,                                    // 0xbe2
        None,                                    // 0xbe3
        None,                                    // 0xbf0
        None,                                    // 0xbf1
        None,                                    // 0xbf2
        None,                                    // 0xbf3
        None,                                    // 0xc00
        None,                                    // 0xc01
        None,                                    // 0xc02
        None,                                    // 0xc03
        None,                                    // 0xc10
        None,                                    // 0xc11
        None,                                    // 0xc12
        None,                                    // 0xc13
        None,                                    // 0xc20
        None,                                    // 0xc21
        None,                                    // 0xc22
        None,                                    // 0xc23
        None,                                    // 0xc30
        None,                                    // 0xc31
        None,                                    // 0xc32
        None,                                    // 0xc33
        None,                                    // 0xc40
        None,                                    // 0xc41
        None,                                    // 0xc42
        None,                                    // 0xc43
        None,                                    // 0xc50
        None,                                    // 0xc51
        None,                                    // 0xc52
        None,                                    // 0xc53
        None,                                    // 0xc60
        None,                                    // 0xc61
        None,                                    // 0xc62
        None,                                    // 0xc63
        None,                                    // 0xc70
        None,                                    // 0xc71
        None,                                    // 0xc72
        None,                                    // 0xc73
        None,                                    // 0xc80
        None,                                    // 0xc81
        None,                                    // 0xc82
        None,                                    // 0xc83
        None,                                    // 0xc90
        None,                                    // 0xc91
        None,                                    // 0xc92
        None,                                    // 0xc93
        None,                                    // 0xca0
        None,                                    // 0xca1
        None,                                    // 0xca2
        None,                                    // 0xca3
        None,                                    // 0xcb0
        None,                                    // 0xcb1
        None,                                    // 0xcb2
        None,                                    // 0xcb3
        None,                                    // 0xcc0
        None,                                    // 0xcc1
        None,                                    // 0xcc2
        None,                                    // 0xcc3
        None,                                    // 0xcd0
        None,                                    // 0xcd1
        None,                                    // 0xcd2
        None,                                    // 0xcd3
        None,                                    // 0xce0
        None,                                    // 0xce1
        None,                                    // 0xce2
        None,                                    // 0xce3
        None,                                    // 0xcf0
        None,                                    // 0xcf1
        None,                                    // 0xcf2
        None,                                    // 0xcf3
        None,                                    // 0xd00
        None,                                    // 0xd01
        None,                                    // 0xd02
        None,                                    // 0xd03
        None,                                    // 0xd10
        None,                                    // 0xd11
        None,                                    // 0xd12
        None,                                    // 0xd13
        None,                                    // 0xd20
        None,                                    // 0xd21
        None,                                    // 0xd22
        None,                                    // 0xd23
        None,                                    // 0xd30
        None,                                    // 0xd31
        None,                                    // 0xd32
        None,                                    // 0xd33
        None,                                    // 0xd40
        None,                                    // 0xd41
        None,                                    // 0xd42
        None,                                    // 0xd43
        None,                                    // 0xd50
        None,                                    // 0xd51
        None,                                    // 0xd52
        None,                                    // 0xd53
        None,                                    // 0xd60
        None,                                    // 0xd61
        None,                                    // 0xd62
        None,                                    // 0xd63
        None,                                    // 0xd70
        None,                                    // 0xd71
        None,                                    // 0xd72
        None,                                    // 0xd73
        None,                                    // 0xd80
        None,                                    // 0xd81
        None,                                    // 0xd82
        None,                                    // 0xd83
        None,                                    // 0xd90
        None,                                    // 0xd91
        None,                                    // 0xd92
        None,                                    // 0xd93
        None,                                    // 0xda0
        None,                                    // 0xda1
        None,                                    // 0xda2
        None,                                    // 0xda3
        None,                                    // 0xdb0
        None,                                    // 0xdb1
        None,                                    // 0xdb2
        None,                                    // 0xdb3
        None,                                    // 0xdc0
        None,                                    // 0xdc1
        None,                                    // 0xdc2
        None,                                    // 0xdc3
        None,                                    // 0xdd0
        None,                                    // 0xdd1
        None,                                    // 0xdd2
        None,                                    // 0xdd3
        None,                                    // 0xde0
        None,                                    // 0xde1
        None,                                    // 0xde2
        None,                                    // 0xde3
        None,                                    // 0xdf0
        None,                                    // 0xdf1
        None,                                    // 0xdf2
        None,                                    // 0xdf3
        None,                                    // 0xe00
        None,                                    // 0xe01
        None,                                    // 0xe02
        None,                                    // 0xe03
        None,                                    // 0xe10
        None,                                    // 0xe11
        None,                                    // 0xe12
        None,                                    // 0xe13
        None,                                    // 0xe20
        None,                                    // 0xe21
        None,                                    // 0xe22
        None,                                    // 0xe23
        None,                                    // 0xe30
        None,                                    // 0xe31
        None,                                    // 0xe32
        None,                                    // 0xe33
        None,                                    // 0xe40
        None,                                    // 0xe41
        None,                                    // 0xe42
        None,                                    // 0xe43
        None,                                    // 0xe50
        None,                                    // 0xe51
        None,                                    // 0xe52
        None,                                    // 0xe53
        None,                                    // 0xe60
        None,                                    // 0xe61
        None,                                    // 0xe62
        None,                                    // 0xe63
        None,                                    // 0xe70
        None,                                    // 0xe71
        None,                                    // 0xe72
        None,                                    // 0xe73
        None,                                    // 0xe80
        None,                                    // 0xe81
        None,                                    // 0xe82
        None,                                    // 0xe83
        None,                                    // 0xe90
        None,                                    // 0xe91
        None,                                    // 0xe92
        None,                                    // 0xe93
        None,                                    // 0xea0
        None,                                    // 0xea1
        None,                                    // 0xea2
        None,                                    // 0xea3
        None,                                    // 0xeb0
        None,                                    // 0xeb1
        None,                                    // 0xeb2
        None,                                    // 0xeb3
        None,                                    // 0xec0
        None,                                    // 0xec1
        None,                                    // 0xec2
        None,                                    // 0xec3
        None,                                    // 0xed0
        None,                                    // 0xed1
        None,                                    // 0xed2
        None,                                    // 0xed3
        None,                                    // 0xee0
        None,                                    // 0xee1
        None,                                    // 0xee2
        None,                                    // 0xee3
        None,                                    // 0xef0
        None,                                    // 0xef1
        None,                                    // 0xef2
        None,                                    // 0xef3
        None,                                    // 0xf00
        None,                                    // 0xf01
        None,                                    // 0xf02
        None,                                    // 0xf03
        None,                                    // 0xf10
        None,                                    // 0xf11
        None,                                    // 0xf12
        None,                                    // 0xf13
        None,                                    // 0xf20
        None,                                    // 0xf21
        None,                                    // 0xf22
        None,                                    // 0xf23
        None,                                    // 0xf30
        None,                                    // 0xf31
        None,                                    // 0xf32
        None,                                    // 0xf33
        None,                                    // 0xf40
        None,                                    // 0xf41
        None,                                    // 0xf42
        None,                                    // 0xf43
        None,                                    // 0xf50
        None,                                    // 0xf51
        None,                                    // 0xf52
        None,                                    // 0xf53
        None,                                    // 0xf60
        None,                                    // 0xf61
        None,                                    // 0xf62
        None,                                    // 0xf63
        None,                                    // 0xf70
        None,                                    // 0xf71
        None,                                    // 0xf72
        None,                                    // 0xf73
        None,                                    // 0xf80
        None,                                    // 0xf81
        None,                                    // 0xf82
        None,                                    // 0xf83
        None,                                    // 0xf90
        None,                                    // 0xf91
        None,                                    // 0xf92
        None,                                    // 0xf93
        None,                                    // 0xfa0
        None,                                    // 0xfa1
        None,                                    // 0xfa2
        None,                                    // 0xfa3
        None,                                    // 0xfb0
        None,                                    // 0xfb1
        None,                                    // 0xfb2
        None,                                    // 0xfb3
        None,                                    // 0xfc0
        None,                                    // 0xfc1
        None,                                    // 0xfc2
        None,                                    // 0xfc3
        None,                                    // 0xfd0
        None,                                    // 0xfd1
        None,                                    // 0xfd2
        None,                                    // 0xfd3
        None,                                    // 0xfe0
        None,                                    // 0xfe1
        None,                                    // 0xfe2
        None,                                    // 0xfe3
        None,                                    // 0xff0
        None,                                    // 0xff1
        None,                                    // 0xff2
        None,                                    // 0xff3
    };
}
