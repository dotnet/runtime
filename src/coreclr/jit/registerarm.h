// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off

/*****************************************************************************/
/*****************************************************************************/
#ifndef REGDEF
#error  Must define REGDEF macro before including this file
#endif
#ifndef REGALIAS
#define REGALIAS(alias, realname)
#endif

/*
REGDEF(name, rnum,   mask, sname) */
REGDEF(R0,      0, 0x0001, "r0"   )
REGDEF(R1,      1, 0x0002, "r1"   )
REGDEF(R2,      2, 0x0004, "r2"   )
REGDEF(R3,      3, 0x0008, "r3"   )
REGDEF(R4,      4, 0x0010, "r4"   )
REGDEF(R5,      5, 0x0020, "r5"   )
REGDEF(R6,      6, 0x0040, "r6"   )
REGDEF(R7,      7, 0x0080, "r7"   )
REGDEF(R8,      8, 0x0100, "r8"   )
REGDEF(R9,      9, 0x0200, "r9"   )
REGDEF(R10,    10, 0x0400, "r10"  )
REGDEF(R11,    11, 0x0800, "r11"  )
REGDEF(R12,    12, 0x1000, "r12"  )
REGDEF(SP,     13, 0x2000, "sp"   )
REGDEF(LR,     14, 0x4000, "lr"   )
REGDEF(PC,     15, 0x8000, "pc"   )

#define FPBASE 16
#define VFPMASK(x) (((int64_t)1) << (x+FPBASE))

REGDEF(F0,    0+FPBASE, VFPMASK(0),  "f0")
REGDEF(F1,    1+FPBASE, VFPMASK(1),  "f1")
REGDEF(F2,    2+FPBASE, VFPMASK(2),  "f2")
REGDEF(F3,    3+FPBASE, VFPMASK(3),  "f3")
REGDEF(F4,    4+FPBASE, VFPMASK(4),  "f4")
REGDEF(F5,    5+FPBASE, VFPMASK(5),  "f5")
REGDEF(F6,    6+FPBASE, VFPMASK(6),  "f6")
REGDEF(F7,    7+FPBASE, VFPMASK(7),  "f7")
REGDEF(F8,    8+FPBASE, VFPMASK(8),  "f8")
REGDEF(F9,    9+FPBASE, VFPMASK(9),  "f9")
REGDEF(F10,  10+FPBASE, VFPMASK(10), "f10")
REGDEF(F11,  11+FPBASE, VFPMASK(11), "f11")
REGDEF(F12,  12+FPBASE, VFPMASK(12), "f12")
REGDEF(F13,  13+FPBASE, VFPMASK(13), "f13")
REGDEF(F14,  14+FPBASE, VFPMASK(14), "f14")
REGDEF(F15,  15+FPBASE, VFPMASK(15), "f15")
REGDEF(F16,  16+FPBASE, VFPMASK(16), "f16")
REGDEF(F17,  17+FPBASE, VFPMASK(17), "f17")
REGDEF(F18,  18+FPBASE, VFPMASK(18), "f18")
REGDEF(F19,  19+FPBASE, VFPMASK(19), "f19")
REGDEF(F20,  20+FPBASE, VFPMASK(20), "f20")
REGDEF(F21,  21+FPBASE, VFPMASK(21), "f21")
REGDEF(F22,  22+FPBASE, VFPMASK(22), "f22")
REGDEF(F23,  23+FPBASE, VFPMASK(23), "f23")
REGDEF(F24,  24+FPBASE, VFPMASK(24), "f24")
REGDEF(F25,  25+FPBASE, VFPMASK(25), "f25")
REGDEF(F26,  26+FPBASE, VFPMASK(26), "f26")
REGDEF(F27,  27+FPBASE, VFPMASK(27), "f27")
REGDEF(F28,  28+FPBASE, VFPMASK(28), "f28")
REGDEF(F29,  29+FPBASE, VFPMASK(29), "f29")
REGDEF(F30,  30+FPBASE, VFPMASK(30), "f30")
REGDEF(F31,  31+FPBASE, VFPMASK(31), "f31")


// Allow us to call R11/FP, SP, LR and PC by their register number names
REGALIAS(FP,  R11)
REGALIAS(R13, SP)
REGALIAS(R14, LR)
REGALIAS(R15, PC)

// Ignore REG_* symbols defined in Android NDK
#undef REG_R0
#define REG_R0 JITREG_R0
#undef REG_R1
#define REG_R1 JITREG_R1
#undef REG_R2
#define REG_R2 JITREG_R2
#undef REG_R3
#define REG_R3 JITREG_R3
#undef REG_R4
#define REG_R4 JITREG_R4
#undef REG_R5
#define REG_R5 JITREG_R5
#undef REG_R6
#define REG_R6 JITREG_R6
#undef REG_R7
#define REG_R7 JITREG_R7
#undef REG_R8
#define REG_R8 JITREG_R8
#undef REG_R9
#define REG_R9 JITREG_R9
#undef REG_R10
#define REG_R10 JITREG_R10
#undef REG_R11
#define REG_R11 JITREG_R11
#undef REG_R12
#define REG_R12 JITREG_R12
#undef REG_SP
#define REG_SP JITREG_SP
#undef REG_LR
#define REG_LR JITREG_LR
#undef REG_PC
#define REG_PC JITREG_PC
#undef REG_F0
#define REG_F0 JITREG_F0
#undef REG_F1
#define REG_F1 JITREG_F1
#undef REG_F2
#define REG_F2 JITREG_F2
#undef REG_F3
#define REG_F3 JITREG_F3
#undef REG_F4
#define REG_F4 JITREG_F4
#undef REG_F5
#define REG_F5 JITREG_F5
#undef REG_F6
#define REG_F6 JITREG_F6
#undef REG_F7
#define REG_F7 JITREG_F7
#undef REG_F8
#define REG_F8 JITREG_F8
#undef REG_F9
#define REG_F9 JITREG_F9
#undef REG_F10
#define REG_F10 JITREG_F10
#undef REG_F11
#define REG_F11 JITREG_F11
#undef REG_F12
#define REG_F12 JITREG_F12
#undef REG_F13
#define REG_F13 JITREG_F13
#undef REG_F14
#define REG_F14 JITREG_F14
#undef REG_F15
#define REG_F15 JITREG_F15
#undef REG_F16
#define REG_F16 JITREG_F16
#undef REG_F17
#define REG_F17 JITREG_F17
#undef REG_F18
#define REG_F18 JITREG_F18
#undef REG_F19
#define REG_F19 JITREG_F19
#undef REG_F20
#define REG_F20 JITREG_F20
#undef REG_F21
#define REG_F21 JITREG_F21
#undef REG_F22
#define REG_F22 JITREG_F22
#undef REG_F23
#define REG_F23 JITREG_F23
#undef REG_F24
#define REG_F24 JITREG_F24
#undef REG_F25
#define REG_F25 JITREG_F25
#undef REG_F26
#define REG_F26 JITREG_F26
#undef REG_F27
#define REG_F27 JITREG_F27
#undef REG_F28
#define REG_F28 JITREG_F28
#undef REG_F29
#define REG_F29 JITREG_F29
#undef REG_F30
#define REG_F30 JITREG_F30
#undef REG_F31
#define REG_F31 JITREG_F31
#undef REG_FP
#define REG_FP JITREG_FP
#undef REG_R13
#define REG_R13 JITREG_R13
#undef REG_R14
#define REG_R14 JITREG_R14
#undef REG_R15
#define REG_R15 JITREG_R15
#undef REG_STK
#define REG_STK JITREG_STK

// This must be last!
REGDEF(STK,  32+FPBASE, 0x0000,      "STK")

/*****************************************************************************/
#undef  REGDEF
#undef  REGALIAS
/*****************************************************************************/

// clang-format on
