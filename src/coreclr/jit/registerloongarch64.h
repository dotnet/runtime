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
REGDEF(TP,      2,     0x0004, "tp"  )
REGDEF(SP,      3,     0x0008, "sp"  )
REGDEF(A0,      4,     0x0010, "a0"  )
REGDEF(A1,      5,     0x0020, "a1"  )
REGDEF(A2,      6,     0x0040, "a2"  )
REGDEF(A3,      7,     0x0080, "a3"  )
REGDEF(A4,      8,     0x0100, "a4"  )
REGDEF(A5,      9,     0x0200, "a5"  )
REGDEF(A6,     10,     0x0400, "a6"  )
REGDEF(A7,     11,     0x0800, "a7"  )
REGDEF(T0,     12,     0x1000, "t0"  )
REGDEF(T1,     13,     0x2000, "t1"  )
REGDEF(T2,     14,     0x4000, "t2"  )
REGDEF(T3,     15,     0x8000, "t3"  )
REGDEF(T4,     16,    0x10000, "t4"  )
REGDEF(T5,     17,    0x20000, "t5"  )
REGDEF(T6,     18,    0x40000, "t6"  )
REGDEF(T7,     19,    0x80000, "t7"  )
REGDEF(T8,     20,   0x100000, "t8"  )
REGDEF(X0,     21,   0x200000, "x0"  )
REGDEF(FP,     22,   0x400000, "fp"  )
REGDEF(S0,     23,   0x800000, "s0"  )
REGDEF(S1,     24,  0x1000000, "s1"  )
REGDEF(S2,     25,  0x2000000, "s2"  )
REGDEF(S3,     26,  0x4000000, "s3"  )
REGDEF(S4,     27,  0x8000000, "s4"  )
REGDEF(S5,     28, 0x10000000, "s5"  )
REGDEF(S6,     29, 0x20000000, "s6"  )
REGDEF(S7,     30, 0x40000000, "s7"  )
REGDEF(S8,     31, 0x80000000, "s8"  )

//NOTE for LoongArch64:
//  The `REG_R21` which alias `REG_X0` is specially reserved !!!
//  It should be only used with hand written assembly code and should be very careful!!!
//  e.g. right now LoongArch64's backend-codegen/emit, there is usually
//  a need for an extra register for cases like
//  constructing a large imm or offset, saving some intermediate result
//  of the overflowing check and integer-comparing result.
//  Using the a specially reserved register maybe more efficient.
REGALIAS(R21, X0)

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

// The registers with values 64 (NBASE) and above are not real register numbers
#define NBASE 64

// This must be last!
REGDEF(STK,   0+NBASE, 0x0000,    "STK")

/*****************************************************************************/
#undef  RMASK
#undef  FMASK
#undef  FBASE
#undef  NBASE
#undef  REGDEF
#undef  REGALIAS
/*****************************************************************************/

// clang-format on
