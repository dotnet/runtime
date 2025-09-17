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
REGDEF(name, rnum,       mask, xname, wname) */
REGDEF(R0,      0,     0x0001, "x0")
REGDEF(R1,      1,     0x0002, "x1" )
REGDEF(R2,      2,     0x0004, "x2" )
REGDEF(R3,      3,     0x0008, "x3" )
REGDEF(R4,      4,     0x0010, "x4" )
REGDEF(R5,      5,     0x0020, "x5" )
REGDEF(R6,      6,     0x0040, "x6" )
REGDEF(R7,      7,     0x0080, "x7" )
REGDEF(R8,      8,     0x0100, "x8" )
REGDEF(R9,      9,     0x0200, "x9" )
REGDEF(R10,    10,     0x0400, "x10")
REGDEF(FP,     11,     0x0800, "x11")
REGDEF(R12,    12,     0x1000, "x12")
REGDEF(R13,    13,     0x2000, "x13")
REGDEF(LR,     14,     0x4000, "x14")
REGDEF(SP,     15,     0x8000, "x15")
REGDEF(IP0,    16,    0x10000, "xip0")
REGDEF(IP1,    17,    0x20000, "xip1")
REGDEF(R18,    18,    0x40000, "x18")
REGDEF(R19,    19,    0x80000, "x19")
REGDEF(R20,    20,   0x100000, "x20")
REGDEF(R21,    21,   0x200000, "x21")
REGDEF(R22,    22,   0x400000, "x22")
REGDEF(R23,    23,   0x800000, "x23")
REGDEF(R24,    24,  0x1000000, "x24")
REGDEF(R25,    25,  0x2000000, "x25")
REGDEF(R26,    26,  0x4000000, "x26")
REGDEF(R27,    27,  0x8000000, "x27")
REGDEF(R28,    28, 0x10000000, "x28")
REGDEF(ZR,     29, 0x20000000, "xzr")

// Allow us to call IP0,IP1,PR,FP,LR by their register number names
REGALIAS(R11, FP)
REGALIAS(R14, LR)
REGALIAS(R15, SP)
REGALIAS(R16, IP0)
REGALIAS(R17, IP1)


#define VBASE 30 
#define VMASK(x) (1ULL << (VBASE+(x)))

/*
REGDEF(name,  rnum,       mask,  xname,  wname) */
REGDEF(V0,    0+VBASE, VMASK(0),  "d0")
REGDEF(V1,    1+VBASE, VMASK(1),  "d1")
REGDEF(V2,    2+VBASE, VMASK(2),  "d2")
REGDEF(V3,    3+VBASE, VMASK(3),  "d3")
REGDEF(V4,    4+VBASE, VMASK(4),  "d4")
REGDEF(V5,    5+VBASE, VMASK(5),  "d5")
REGDEF(V6,    6+VBASE, VMASK(6),  "d6")
REGDEF(V7,    7+VBASE, VMASK(7),  "d7")
REGDEF(V8,    8+VBASE, VMASK(8),  "d8")
REGDEF(V9,    9+VBASE, VMASK(9),  "d9")
REGDEF(V10,  10+VBASE, VMASK(10), "d10")
REGDEF(V11,  11+VBASE, VMASK(11), "d11")
REGDEF(V12,  12+VBASE, VMASK(12), "d12")
REGDEF(V13,  13+VBASE, VMASK(13), "d13")
REGDEF(V14,  14+VBASE, VMASK(14), "d14")
REGDEF(V15,  15+VBASE, VMASK(15), "d15")
REGDEF(V16,  16+VBASE, VMASK(16), "d16")
REGDEF(V17,  17+VBASE, VMASK(17), "d17")
REGDEF(V18,  18+VBASE, VMASK(18), "d18")
REGDEF(V19,  19+VBASE, VMASK(19), "d19")
REGDEF(V20,  20+VBASE, VMASK(20), "d20")
REGDEF(V21,  21+VBASE, VMASK(21), "d21")
REGDEF(V22,  22+VBASE, VMASK(22), "d22")
REGDEF(V23,  23+VBASE, VMASK(23), "d23")
REGDEF(V24,  24+VBASE, VMASK(24), "d24")
REGDEF(V25,  25+VBASE, VMASK(25), "d25")
REGDEF(V26,  26+VBASE, VMASK(26), "d26")
REGDEF(V27,  27+VBASE, VMASK(27), "d27")
REGDEF(V28,  28+VBASE, VMASK(28), "d28")
REGDEF(V29,  29+VBASE, VMASK(29), "d29")
REGDEF(V30,  30+VBASE, VMASK(30), "d30")
REGDEF(V31,  31+VBASE, VMASK(31), "d31")


#define FPBASE 62 
#define VFPMASK(x) (1ULL << (VBASE+(x)))

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


#define PBASE 94 
#define PMASK(x) (1ULL << x)

/*
REGDEF(name,  rnum,         mask,  xname) */
REGDEF(P0,    0+PBASE,  PMASK(0),  "p0")
REGDEF(P1,    1+PBASE,  PMASK(1),  "p1")
REGDEF(P2,    2+PBASE,  PMASK(2),  "p2")
REGDEF(P3,    3+PBASE,  PMASK(3),  "p3")
REGDEF(P4,    4+PBASE,  PMASK(4),  "p4")
REGDEF(P5,    5+PBASE,  PMASK(5),  "p5")
REGDEF(P6,    6+PBASE,  PMASK(6),  "p6")
REGDEF(P7,    7+PBASE,  PMASK(7),  "p7")
REGDEF(P8,    8+PBASE,  PMASK(8),  "p8")
REGDEF(P9,    9+PBASE,  PMASK(9),  "p9")
REGDEF(P10,  10+PBASE, PMASK(10),  "p10")
REGDEF(P11,  11+PBASE, PMASK(11),  "p11")
REGDEF(P12,  12+PBASE, PMASK(12),  "p12")
REGDEF(P13,  13+PBASE, PMASK(13),  "p13")
REGDEF(P14,  14+PBASE, PMASK(14),  "p14")
REGDEF(P15,  15+PBASE, PMASK(15),  "p15")
REGDEF(STK,  16+PBASE, 0x0000,      "STK")
/*****************************************************************************/
#undef  RMASK
#undef  VMASK
#undef  VBASE
#undef  NBASE
#undef  REGDEF
#undef  REGALIAS
/*****************************************************************************/
// clang-format on
