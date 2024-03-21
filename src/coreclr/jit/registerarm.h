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
REGDEF(name, rnum,   mask, sname, regTypeTag) */
REGDEF(R0,      0, 0x0001, "r0", 0)
REGDEF(R1,      1, 0x0002, "r1", 0)
REGDEF(R2,      2, 0x0004, "r2", 0)
REGDEF(R3,      3, 0x0008, "r3", 0)
REGDEF(R4,      4, 0x0010, "r4", 0)
REGDEF(R5,      5, 0x0020, "r5", 0)
REGDEF(R6,      6, 0x0040, "r6", 0)
REGDEF(R7,      7, 0x0080, "r7", 0)
REGDEF(R8,      8, 0x0100, "r8", 0)
REGDEF(R9,      9, 0x0200, "r9", 0)
REGDEF(R10,    10, 0x0400, "r10",0)
REGDEF(R11,    11, 0x0800, "r11",0)
REGDEF(R12,    12, 0x1000, "r12",0)
REGDEF(SP,     13, 0x2000, "sp", 0)
REGDEF(LR,     14, 0x4000, "lr", 0)
REGDEF(PC,     15, 0x8000, "pc", 0)

#define FPBASE 16
#define VFPMASK(x) (((__int64)1) << (x+FPBASE))

REGDEF(F0,    0+FPBASE, VFPMASK(0),  "f0",  1)
REGDEF(F1,    1+FPBASE, VFPMASK(1),  "f1",  1)
REGDEF(F2,    2+FPBASE, VFPMASK(2),  "f2",  1)
REGDEF(F3,    3+FPBASE, VFPMASK(3),  "f3",  1)
REGDEF(F4,    4+FPBASE, VFPMASK(4),  "f4",  1)
REGDEF(F5,    5+FPBASE, VFPMASK(5),  "f5",  1)
REGDEF(F6,    6+FPBASE, VFPMASK(6),  "f6",  1)
REGDEF(F7,    7+FPBASE, VFPMASK(7),  "f7",  1)
REGDEF(F8,    8+FPBASE, VFPMASK(8),  "f8",  1)
REGDEF(F9,    9+FPBASE, VFPMASK(9),  "f9",  1)
REGDEF(F10,  10+FPBASE, VFPMASK(10), "f10", 1)
REGDEF(F11,  11+FPBASE, VFPMASK(11), "f11", 1)
REGDEF(F12,  12+FPBASE, VFPMASK(12), "f12", 1)
REGDEF(F13,  13+FPBASE, VFPMASK(13), "f13", 1)
REGDEF(F14,  14+FPBASE, VFPMASK(14), "f14", 1)
REGDEF(F15,  15+FPBASE, VFPMASK(15), "f15", 1)
REGDEF(F16,  16+FPBASE, VFPMASK(16), "f16", 1)
REGDEF(F17,  17+FPBASE, VFPMASK(17), "f17", 1)
REGDEF(F18,  18+FPBASE, VFPMASK(18), "f18", 1)
REGDEF(F19,  19+FPBASE, VFPMASK(19), "f19", 1)
REGDEF(F20,  20+FPBASE, VFPMASK(20), "f20", 1)
REGDEF(F21,  21+FPBASE, VFPMASK(21), "f21", 1)
REGDEF(F22,  22+FPBASE, VFPMASK(22), "f22", 1)
REGDEF(F23,  23+FPBASE, VFPMASK(23), "f23", 1)
REGDEF(F24,  24+FPBASE, VFPMASK(24), "f24", 1)
REGDEF(F25,  25+FPBASE, VFPMASK(25), "f25", 1)
REGDEF(F26,  26+FPBASE, VFPMASK(26), "f26", 1)
REGDEF(F27,  27+FPBASE, VFPMASK(27), "f27", 1)
REGDEF(F28,  28+FPBASE, VFPMASK(28), "f28", 1)
REGDEF(F29,  29+FPBASE, VFPMASK(29), "f29", 1)
REGDEF(F30,  30+FPBASE, VFPMASK(30), "f30", 1)
REGDEF(F31,  31+FPBASE, VFPMASK(31), "f31", 1)


// Allow us to call R11/FP, SP, LR and PC by their register number names
REGALIAS(FP,  R11)
REGALIAS(R13, SP)
REGALIAS(R14, LR)
REGALIAS(R15, PC)

// This must be last!
REGDEF(STK,  32+FPBASE, 0x0000,      "STK",  -1)

/*****************************************************************************/
#undef  REGDEF
#undef  REGALIAS
/*****************************************************************************/

// clang-format on
