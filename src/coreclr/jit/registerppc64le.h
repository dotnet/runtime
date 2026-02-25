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
REGDEF(R0,      0,       0x0001, "r0")
REGDEF(SP,      1,       0x0002, "r1")
REGDEF(R2,      2,       0x0004, "r2")
REGDEF(R3,      3,       0x0008, "r3")
REGDEF(R4,      4,       0x0010, "r4")
REGDEF(R5,      5,       0x0020, "r5")
REGDEF(R6,      6,       0x0040, "r6")
REGDEF(R7,      7,       0x0080, "r7")
REGDEF(R8,      8,       0x0100, "r8")
REGDEF(R9,      9,       0x0200, "r9")
REGDEF(R10,	10,      0x0400, "r10")
REGDEF(R11,	11,      0x0800, "r11")
REGDEF(R12,	12,      0x1000, "r12")
REGDEF(R13,	13,      0x2000, "r13")
REGDEF(R14,     14,      0x4000, "r14")
REGDEF(R15,     15,      0x8000, "r15")
REGDEF(R16,     16,     0x10000, "r16")
REGDEF(R17,     17,     0x20000, "r17")
REGDEF(R18,     18,     0x40000, "r18")
REGDEF(R19,     19,     0x80000, "r19")
REGDEF(R20,     20,    0x100000, "r20")
REGDEF(R21,     21,    0x200000, "r21")
REGDEF(R22,     22,    0x400000, "r22")
REGDEF(R23,     23,    0x800000, "r23")
REGDEF(R24,     24,   0x1000000, "r24")
REGDEF(R25,     25,   0x2000000, "r25")
REGDEF(R26,     26,   0x4000000, "r26")
REGDEF(R27,     27,   0x8000000, "r27")
REGDEF(R28,     28,  0x10000000, "r28")
REGDEF(R29,     29,  0x20000000, "r29")
REGDEF(R30,     30,  0x40000000, "r30")
REGDEF(FP,      31,  0x80000000, "r31")
//REGDEF(LR,      32, 0x100000000, "lr")

// Allow us to call IP0,IP1,PR,FP,LR by their register number names
REGALIAS(R1, SP)
REGALIAS(R31, FP)

#define FBASE 32
#define FMASK(x) (1ULL << (FBASE+(x)))

/*
REGDEF(name,  rnum,       mask,  sname) */
REGDEF(F0,    0+FBASE, FMASK(0),   "f0")
REGDEF(F1,    1+FBASE, FMASK(1),   "f1")
REGDEF(F2,    2+FBASE, FMASK(2),   "f2")
REGDEF(F3,    3+FBASE, FMASK(3),   "f3")
REGDEF(F4,    4+FBASE, FMASK(4),   "f4")
REGDEF(F5,    5+FBASE, FMASK(5),   "f5")
REGDEF(F6,    6+FBASE, FMASK(6),   "f6")
REGDEF(F7,    7+FBASE, FMASK(7),   "f7")
REGDEF(F8,    8+FBASE, FMASK(8),   "f8")
REGDEF(F9,    9+FBASE, FMASK(9),   "f9")
REGDEF(F10,  10+FBASE, FMASK(10), "f10")
REGDEF(F11,  11+FBASE, FMASK(11), "f11")
REGDEF(F12,  12+FBASE, FMASK(12), "f12")
REGDEF(F13,  13+FBASE, FMASK(13), "f13")
REGDEF(F14,  14+FBASE, FMASK(14), "f14")
REGDEF(F15,  15+FBASE, FMASK(15), "f15")
REGDEF(F16,  16+FBASE, FMASK(16), "f16")
REGDEF(F17,  17+FBASE, FMASK(17), "f17")
REGDEF(F18,  18+FBASE, FMASK(18), "f18")
REGDEF(F19,  19+FBASE, FMASK(19), "f19")
REGDEF(F20,  20+FBASE, FMASK(20), "f20")
REGDEF(F21,  21+FBASE, FMASK(21), "f21")
REGDEF(F22,  22+FBASE, FMASK(22), "f22")
REGDEF(F23,  23+FBASE, FMASK(23), "f23")
REGDEF(F24,  24+FBASE, FMASK(24), "f24")
REGDEF(F25,  25+FBASE, FMASK(25), "f25")
REGDEF(F26,  26+FBASE, FMASK(26), "f26")
REGDEF(F27,  27+FBASE, FMASK(27), "f27")
REGDEF(F28,  28+FBASE, FMASK(28), "f28")
REGDEF(F29,  29+FBASE, FMASK(29), "f29")
REGDEF(F30,  30+FBASE, FMASK(30), "f30")
REGDEF(F31,  31+FBASE, FMASK(31), "f31")

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
