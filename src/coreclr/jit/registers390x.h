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

#define RMASK(x) (1ULL << (x))

/*
REGDEF(name, rnum,       mask, xname) */
REGDEF(R0,      0,     0x0001, "r0")
REGDEF(R1,      1,     0x0002, "r1")
REGDEF(R2,      2,     0x0004, "r2")
REGDEF(R3,      3,     0x0008, "r3")
REGDEF(R4,      4,     0x0010, "r4")
REGDEF(R5,      5,     0x0020, "r5")
REGDEF(R6,      6,     0x0040, "r6")
REGDEF(R7,      7,     0x0080, "r7")
REGDEF(R8,      8,     0x0100, "r8")
REGDEF(R9,      9,     0x0200, "r9")
REGDEF(R10,	10,     0x0400, "r10")
REGDEF(FP,	11,     0x0800, "r11")
REGDEF(R12,	12,     0x1000, "r12")
REGDEF(R13,	13,     0x2000, "r13")
REGDEF(LR,     14,     0x4000, "r14")
REGDEF(SP,     15,     0x8000, "r15")

// Allow us to call IP0,IP1,PR,FP,LR by their register number names
REGALIAS(R11, FP)
REGALIAS(R14, LR)
REGALIAS(R15, SP)


#define FBASE 16 
#define FMASK(x) (1ULL << (FBASE+(x)))

/*
REGDEF(name,  rnum,       mask,  xname,  wname) */
REGDEF(F0,    0+FBASE, FMASK(0),  "f0")
REGDEF(F1,    1+FBASE, FMASK(1),  "f1")
REGDEF(F2,    2+FBASE, FMASK(2),  "f2")
REGDEF(F3,    3+FBASE, FMASK(3),  "f3")
REGDEF(F4,    4+FBASE, FMASK(4),  "f4")
REGDEF(F5,    5+FBASE, FMASK(5),  "f5")
REGDEF(F6,    6+FBASE, FMASK(6),  "f6")
REGDEF(F7,    7+FBASE, FMASK(7),  "f7")
REGDEF(F8,    8+FBASE, FMASK(8),  "f8")
REGDEF(F9,    9+FBASE, FMASK(9),  "f9")
REGDEF(F10,  10+FBASE, FMASK(10), "f10")
REGDEF(F11,  11+FBASE, FMASK(11), "f11")
REGDEF(F12,  12+FBASE, FMASK(12), "f12")
REGDEF(F13,  13+FBASE, FMASK(13), "f13")
REGDEF(F14,  14+FBASE, FMASK(14), "f14")
REGDEF(F15,  15+FBASE, FMASK(15), "f15")

#define VBASE 32 
#define VMASK(x) (1ULL << (VBASE+(x)))

REGDEF(V0,    0+VBASE, VMASK(0),  "v0")
REGDEF(V1,    1+VBASE, VMASK(1),  "v1")
REGDEF(V2,    2+VBASE, VMASK(2),  "v2")
REGDEF(V3,    3+VBASE, VMASK(3),  "v3")
REGDEF(V4,    4+VBASE, VMASK(4),  "v4")
REGDEF(V5,    5+VBASE, VMASK(5),  "v5")
REGDEF(V6,    6+VBASE, VMASK(6),  "v6")
REGDEF(V7,    7+VBASE, VMASK(7),  "v7")
REGDEF(V8,    8+VBASE, VMASK(8),  "v8")
REGDEF(V9,    9+VBASE, VMASK(9),  "v9")
REGDEF(V10,  10+VBASE, VMASK(10), "v10")
REGDEF(V11,  11+VBASE, VMASK(11), "v11")
REGDEF(V12,  12+VBASE, VMASK(12), "v12")
REGDEF(V13,  13+VBASE, VMASK(13), "v13")
REGDEF(V14,  14+VBASE, VMASK(14), "v14")
REGDEF(V15,  15+VBASE, VMASK(15), "v15")
REGDEF(V16,  16+VBASE, VMASK(16), "v16")
REGDEF(V17,  17+VBASE, VMASK(17), "v17")
REGDEF(V18,  18+VBASE, VMASK(18), "v18")
REGDEF(V19,  19+VBASE, VMASK(19), "v19")
REGDEF(V20,  20+VBASE, VMASK(20), "v20")
REGDEF(V21,  21+VBASE, VMASK(21), "v21")
REGDEF(V22,  22+VBASE, VMASK(22), "v22")
REGDEF(V23,  23+VBASE, VMASK(23), "v23")
REGDEF(V24,  24+VBASE, VMASK(24), "v24")
REGDEF(V25,  25+VBASE, VMASK(25), "v25")
REGDEF(V26,  26+VBASE, VMASK(26), "v26")
REGDEF(V27,  27+VBASE, VMASK(27), "v27")
REGDEF(V28,  28+VBASE, VMASK(28), "v28")
REGDEF(V29,  29+VBASE, VMASK(29), "v29")
REGDEF(V30,  30+VBASE, VMASK(30), "v30")
REGDEF(V31,  31+VBASE, VMASK(31), "v31")


// The registers with values 80 (NBASE) and above are not real register numbers
#define NBASE 64

REGDEF(FPC,    0+NBASE, 0x0000,    "fpc")
// This must be last!
REGDEF(STK,   1+NBASE, 0x0000,    "STK")

/*****************************************************************************/
#undef  RMASK
#undef  VMASK
#undef  VBASE
#undef  NBASE
#undef  REGDEF
#undef  REGALIAS
/*****************************************************************************/
// clang-format on
