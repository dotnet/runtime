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
REGDEF(name, rnum,       mask, sname) */
REGDEF(R0,      0,     0x0001, "zero")
REGDEF(RA,      1,     0x0002, "ra"  )
REGDEF(SP,      2,     0x0004, "sp"  )
REGDEF(GP,      3,     0x0008, "gp"  )
REGDEF(TP,      4,     0x0010, "tp"  )
REGDEF(T0,      5,     0x0020, "t0"  )
REGDEF(T1,      6,     0x0040, "t1"  )
REGDEF(T2,      7,     0x0080, "t2"  )
REGDEF(FP,      8,     0x0100, "fp"  )
REGDEF(S1,      9,     0x0200, "s1"  )
REGDEF(A0,     10,     0x0400, "a0"  )
REGDEF(A1,     11,     0x0800, "a1"  )
REGDEF(A2,     12,     0x1000, "a2"  )
REGDEF(A3,     13,     0x2000, "a3"  )
REGDEF(A4,     14,     0x4000, "a4"  )
REGDEF(A5,     15,     0x8000, "a5"  )
REGDEF(A6,     16,    0x10000, "a6"  )
REGDEF(A7,     17,    0x20000, "a7"  )
REGDEF(S2,     18,    0x40000, "s2"  )
REGDEF(S3,     19,    0x80000, "s3"  )
REGDEF(S4,     20,   0x100000, "s4"  )
REGDEF(S5,     21,   0x200000, "s5"  )
REGDEF(S6,     22,   0x400000, "s6"  )
REGDEF(S7,     23,   0x800000, "s7"  )
REGDEF(S8,     24,  0x1000000, "s8"  )
REGDEF(S9,     25,  0x2000000, "s9"  )
REGDEF(S10,    26,  0x4000000, "s10"  )
REGDEF(S11,    27,  0x8000000, "s11"  )
REGDEF(T3,     28, 0x10000000, "t3"  )
REGDEF(T4,     29, 0x20000000, "t4"  )
REGDEF(T5,     30, 0x40000000, "t5"  )
REGDEF(T6,     31, 0x80000000, "t6"  )

REGALIAS(R8, FP)
REGALIAS(ZERO, R0)

#define FBASE 32
#define FMASK(x) (1ULL << (FBASE+(x)))

/*
REGDEF(name,     rnum,     mask,  sname) */
REGDEF(FT0,   0+FBASE, FMASK(0),  "ft0")
REGDEF(FT1,   1+FBASE, FMASK(1),  "ft1")
REGDEF(FT2,   2+FBASE, FMASK(2),  "ft2")
REGDEF(FT3,   3+FBASE, FMASK(3),  "ft3")
REGDEF(FT4,   4+FBASE, FMASK(4),  "ft4")
REGDEF(FT5,   5+FBASE, FMASK(5),  "ft5")
REGDEF(FT6,   6+FBASE, FMASK(6),  "ft6")
REGDEF(FT7,   7+FBASE, FMASK(7),  "ft7")
REGDEF(FS0,   8+FBASE, FMASK(8),  "fs0")
REGDEF(FS1,   9+FBASE, FMASK(9),  "fs1")
REGDEF(FA0,  10+FBASE, FMASK(10), "fa0")
REGDEF(FA1,  11+FBASE, FMASK(11), "fa1")
REGDEF(FA2,  12+FBASE, FMASK(12), "fa2")
REGDEF(FA3,  13+FBASE, FMASK(13), "fa3")
REGDEF(FA4,  14+FBASE, FMASK(14), "fa4")
REGDEF(FA5,  15+FBASE, FMASK(15), "fa5")
REGDEF(FA6,  16+FBASE, FMASK(16), "fa6")
REGDEF(FA7,  17+FBASE, FMASK(17), "fa7")
REGDEF(FS2,  18+FBASE, FMASK(18), "fs2")
REGDEF(FS3,  19+FBASE, FMASK(19), "fs3")
REGDEF(FS4,  20+FBASE, FMASK(20), "fs4")
REGDEF(FS5,  21+FBASE, FMASK(21), "fs5")
REGDEF(FS6,  22+FBASE, FMASK(22), "fs6")
REGDEF(FS7,  23+FBASE, FMASK(23), "fs7")
REGDEF(FS8,  24+FBASE, FMASK(24), "fs8")
REGDEF(FS9,  25+FBASE, FMASK(25), "fs9")
REGDEF(FS10, 26+FBASE, FMASK(26), "fs10")
REGDEF(FS11, 27+FBASE, FMASK(27), "fs11")
REGDEF(FT8,  28+FBASE, FMASK(28), "ft8")
REGDEF(FT9,  29+FBASE, FMASK(29), "ft9")
REGDEF(FT10, 30+FBASE, FMASK(30), "ft10")
REGDEF(FT11, 31+FBASE, FMASK(31), "ft11")

// The registers with values 64 (NBASE) and above are not real register numbers
#define NBASE 64

REGDEF(STK,   0+NBASE, 0x0000,    "STK")

/*****************************************************************************/
#undef  RMASK
#undef  VMASK
#undef  VBASE
#undef  NBASE
#undef  REGDEF
#undef  REGALIAS
/*****************************************************************************/

// clang-format on
