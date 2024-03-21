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
REGDEF(name, rnum,       mask, sname, regTypeTag) */
REGDEF(R0,      0,     0x0001, "zero",  0)
REGDEF(RA,      1,     0x0002, "ra"  ,  0)
REGDEF(SP,      2,     0x0004, "sp"  ,  0)
REGDEF(GP,      3,     0x0008, "gp"  ,  0)
REGDEF(TP,      4,     0x0010, "tp"  ,  0)
REGDEF(T0,      5,     0x0020, "t0"  ,  0)
REGDEF(T1,      6,     0x0040, "t1"  ,  0)
REGDEF(T2,      7,     0x0080, "t2",  0)
REGDEF(FP,      8,     0x0100, "fp",  0)
REGDEF(S1,      9,     0x0200, "s1",  0)
REGDEF(A0,     10,     0x0400, "a0",  0)
REGDEF(A1,     11,     0x0800, "a1",  0)
REGDEF(A2,     12,     0x1000, "a2",  0)
REGDEF(A3,     13,     0x2000, "a3",  0)
REGDEF(A4,     14,     0x4000, "a4",  0)
REGDEF(A5,     15,     0x8000, "a5",  0)
REGDEF(A6,     16,    0x10000, "a6",  0)
REGDEF(A7,     17,    0x20000, "a7",  0)
REGDEF(S2,     18,    0x40000, "s2",  0)
REGDEF(S3,     19,    0x80000, "s3",  0)
REGDEF(S4,     20,   0x100000, "s4",  0)
REGDEF(S5,     21,   0x200000, "s5",  0)
REGDEF(S6,     22,   0x400000, "s6",  0)
REGDEF(S7,     23,   0x800000, "s7",  0)
REGDEF(S8,     24,  0x1000000, "s8",  0)
REGDEF(S9,     25,  0x2000000, "s9",  0)
REGDEF(S10,    26,  0x4000000, "s10",  0)
REGDEF(S11,    27,  0x8000000, "s11",  0)
REGDEF(T3,     28, 0x10000000, "t3",  0)
REGDEF(T4,     29, 0x20000000, "t4",  0)
REGDEF(T5,     30, 0x40000000, "t5",  0)
REGDEF(T6,     31, 0x80000000, "t6",  0)

REGALIAS(R8, FP)
REGALIAS(ZERO, R0)

#define FBASE 32
#define FMASK(x) (1ULL << (FBASE+(x)))

/*
REGDEF(name,  rnum,       mask,  sname) */
REGDEF(F0,    0+FBASE, FMASK(0),  "f0",  1)
REGDEF(F1,    1+FBASE, FMASK(1),  "f1",  1)
REGDEF(F2,    2+FBASE, FMASK(2),  "f2",  1)
REGDEF(F3,    3+FBASE, FMASK(3),  "f3",  1)
REGDEF(F4,    4+FBASE, FMASK(4),  "f4",  1)
REGDEF(F5,    5+FBASE, FMASK(5),  "f5",  1)
REGDEF(F6,    6+FBASE, FMASK(6),  "f6",  1)
REGDEF(F7,    7+FBASE, FMASK(7),  "f7",  1)
REGDEF(F8,    8+FBASE, FMASK(8),  "f8",  1)
REGDEF(F9,    9+FBASE, FMASK(9),  "f9",  1)
REGDEF(F10,  10+FBASE, FMASK(10), "f10",  1)
REGDEF(F11,  11+FBASE, FMASK(11), "f11",  1)
REGDEF(F12,  12+FBASE, FMASK(12), "f12",  1)
REGDEF(F13,  13+FBASE, FMASK(13), "f13",  1)
REGDEF(F14,  14+FBASE, FMASK(14), "f14",  1)
REGDEF(F15,  15+FBASE, FMASK(15), "f15",  1)
REGDEF(F16,  16+FBASE, FMASK(16), "f16",  1)
REGDEF(F17,  17+FBASE, FMASK(17), "f17",  1)
REGDEF(F18,  18+FBASE, FMASK(18), "f18",  1)
REGDEF(F19,  19+FBASE, FMASK(19), "f19",  1)
REGDEF(F20,  20+FBASE, FMASK(20), "f20",  1)
REGDEF(F21,  21+FBASE, FMASK(21), "f21",  1)
REGDEF(F22,  22+FBASE, FMASK(22), "f22",  1)
REGDEF(F23,  23+FBASE, FMASK(23), "f23",  1)
REGDEF(F24,  24+FBASE, FMASK(24), "f24",  1)
REGDEF(F25,  25+FBASE, FMASK(25), "f25",  1)
REGDEF(F26,  26+FBASE, FMASK(26), "f26",  1)
REGDEF(F27,  27+FBASE, FMASK(27), "f27",  1)
REGDEF(F28,  28+FBASE, FMASK(28), "f28",  1)
REGDEF(F29,  29+FBASE, FMASK(29), "f29",  1)
REGDEF(F30,  30+FBASE, FMASK(30), "f30",  1)
REGDEF(F31,  31+FBASE, FMASK(31), "f31",  1)

// The registers with values 64 (NBASE) and above are not real register numbers
#define NBASE 64

REGDEF(STK,   0+NBASE, 0x0000,    "STK",  -1)

/*****************************************************************************/
#undef  RMASK
#undef  VMASK
#undef  VBASE
#undef  NBASE
#undef  REGDEF
#undef  REGALIAS
/*****************************************************************************/

// clang-format on
