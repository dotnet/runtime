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
REGDEF(R0,      0,     0x0001, "x0" , "w0"   )
REGDEF(R1,      1,     0x0002, "x1" , "w1"   )
REGDEF(R2,      2,     0x0004, "x2" , "w2"   )
REGDEF(R3,      3,     0x0008, "x3" , "w3"   )
REGDEF(R4,      4,     0x0010, "x4" , "w4"   )
REGDEF(R5,      5,     0x0020, "x5" , "w5"   )
REGDEF(R6,      6,     0x0040, "x6" , "w6"   )
REGDEF(R7,      7,     0x0080, "x7" , "w7"   )
REGDEF(R8,      8,     0x0100, "x8" , "w8"   )
REGDEF(R9,      9,     0x0200, "x9" , "w9"   )
REGDEF(R10,    10,     0x0400, "x10", "w10"  )
REGDEF(R11,    11,     0x0800, "x11", "w11"  )
REGDEF(R12,    12,     0x1000, "x12", "w12"  )
REGDEF(R13,    13,     0x2000, "x13", "w13"  )
REGDEF(R14,    14,     0x4000, "x14", "w14"  )
REGDEF(R15,    15,     0x8000, "x15", "w15"  )
REGDEF(IP0,    16,    0x10000, "xip0","wip0" )
REGDEF(IP1,    17,    0x20000, "xip1","wip1" )
REGDEF(PR,     18,    0x40000, "xpr", "wpr"  )
REGDEF(R19,    19,    0x80000, "x19", "w19"  )
REGDEF(R20,    20,   0x100000, "x20", "w20"  )
REGDEF(R21,    21,   0x200000, "x21", "w21"  )
REGDEF(R22,    22,   0x400000, "x22", "w22"  )
REGDEF(R23,    23,   0x800000, "x23", "w23"  )
REGDEF(R24,    24,  0x1000000, "x24", "w24"  )
REGDEF(R25,    25,  0x2000000, "x25", "w25"  )
REGDEF(R26,    26,  0x4000000, "x26", "w26"  )
REGDEF(R27,    27,  0x8000000, "x27", "w27"  )
REGDEF(R28,    28, 0x10000000, "x28", "w28"  )
REGDEF(FP,     29, 0x20000000, "fp" , "w29"  )
REGDEF(LR,     30, 0x40000000, "lr" , "w30"  )
REGDEF(ZR,     31, 0x80000000, "xzr", "wzr"  )

// Allow us to call IP0,IP1,PR,FP,LR by their register number names
REGALIAS(R16, IP0)
REGALIAS(R17, IP1)
REGALIAS(R18, PR)
REGALIAS(R29, FP)
REGALIAS(R30, LR)

#define VBASE 32
#define VMASK(x) (1ULL << (VBASE+(x)))

/*
REGDEF(name,  rnum,       mask,  xname,  wname) */
REGDEF(V0,    0+VBASE, VMASK(0),  "d0",  "s0")
REGDEF(V1,    1+VBASE, VMASK(1),  "d1",  "s1")
REGDEF(V2,    2+VBASE, VMASK(2),  "d2",  "s2")
REGDEF(V3,    3+VBASE, VMASK(3),  "d3",  "s3")
REGDEF(V4,    4+VBASE, VMASK(4),  "d4",  "s4")
REGDEF(V5,    5+VBASE, VMASK(5),  "d5",  "s5")
REGDEF(V6,    6+VBASE, VMASK(6),  "d6",  "s6")
REGDEF(V7,    7+VBASE, VMASK(7),  "d7",  "s7")
REGDEF(V8,    8+VBASE, VMASK(8),  "d8",  "s8")
REGDEF(V9,    9+VBASE, VMASK(9),  "d9",  "s9")
REGDEF(V10,  10+VBASE, VMASK(10), "d10", "s10")
REGDEF(V11,  11+VBASE, VMASK(11), "d11", "s11")
REGDEF(V12,  12+VBASE, VMASK(12), "d12", "s12")
REGDEF(V13,  13+VBASE, VMASK(13), "d13", "s13")
REGDEF(V14,  14+VBASE, VMASK(14), "d14", "s14")
REGDEF(V15,  15+VBASE, VMASK(15), "d15", "s15")
REGDEF(V16,  16+VBASE, VMASK(16), "d16", "s16")
REGDEF(V17,  17+VBASE, VMASK(17), "d17", "s17")
REGDEF(V18,  18+VBASE, VMASK(18), "d18", "s18")
REGDEF(V19,  19+VBASE, VMASK(19), "d19", "s19")
REGDEF(V20,  20+VBASE, VMASK(20), "d20", "s20")
REGDEF(V21,  21+VBASE, VMASK(21), "d21", "s21")
REGDEF(V22,  22+VBASE, VMASK(22), "d22", "s22")
REGDEF(V23,  23+VBASE, VMASK(23), "d23", "s23")
REGDEF(V24,  24+VBASE, VMASK(24), "d24", "s24")
REGDEF(V25,  25+VBASE, VMASK(25), "d25", "s25")
REGDEF(V26,  26+VBASE, VMASK(26), "d26", "s26")
REGDEF(V27,  27+VBASE, VMASK(27), "d27", "s27")
REGDEF(V28,  28+VBASE, VMASK(28), "d28", "s28")
REGDEF(V29,  29+VBASE, VMASK(29), "d29", "s29")
REGDEF(V30,  30+VBASE, VMASK(30), "d30", "s30")
REGDEF(V31,  31+VBASE, VMASK(31), "d31", "s31")

#define PBASE 64
#define PMASK(x) (1ULL << x)

/*
REGDEF(name,  rnum,         mask,  xname,  wname) */
REGDEF(P0,    0+PBASE,  PMASK(0),  "p0" ,  "na")
REGDEF(P1,    1+PBASE,  PMASK(1),  "p1" ,  "na")
REGDEF(P2,    2+PBASE,  PMASK(2),  "p2" ,  "na")
REGDEF(P3,    3+PBASE,  PMASK(3),  "p3" ,  "na")
REGDEF(P4,    4+PBASE,  PMASK(4),  "p4" ,  "na")
REGDEF(P5,    5+PBASE,  PMASK(5),  "p5" ,  "na")
REGDEF(P6,    6+PBASE,  PMASK(6),  "p6" ,  "na")
REGDEF(P7,    7+PBASE,  PMASK(7),  "p7" ,  "na")
REGDEF(P8,    8+PBASE,  PMASK(8),  "p8" ,  "na")
REGDEF(P9,    9+PBASE,  PMASK(9),  "p9" ,  "na")
REGDEF(P10,  10+PBASE, PMASK(10),  "p10",  "na")
REGDEF(P11,  11+PBASE, PMASK(11),  "p11",  "na")
REGDEF(P12,  12+PBASE, PMASK(12),  "p12",  "na")
REGDEF(P13,  13+PBASE, PMASK(13),  "p13",  "na")
REGDEF(P14,  14+PBASE, PMASK(14),  "p14",  "na")
REGDEF(P15,  15+PBASE, PMASK(15),  "p15",  "na")

// The registers with values 80 (NBASE) and above are not real register numbers
#define NBASE 80

REGDEF(SP,    0+NBASE, 0x0000,    "sp",  "wsp?")
REGDEF(FFR,   1+NBASE, 0x0000,    "ffr",  "na")
// This must be last!
REGDEF(STK,   2+NBASE, 0x0000,    "STK", "STK")

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
#undef REG_R13
#define REG_R13 JITREG_R13
#undef REG_R14
#define REG_R14 JITREG_R14
#undef REG_R15
#define REG_R15 JITREG_R15
#undef REG_IP0
#define REG_IP0 JITREG_IP0
#undef REG_IP1
#define REG_IP1 JITREG_IP1
#undef REG_PR
#define REG_PR JITREG_PR
#undef REG_R19
#define REG_R19 JITREG_R19
#undef REG_R20
#define REG_R20 JITREG_R20
#undef REG_R21
#define REG_R21 JITREG_R21
#undef REG_R22
#define REG_R22 JITREG_R22
#undef REG_R23
#define REG_R23 JITREG_R23
#undef REG_R24
#define REG_R24 JITREG_R24
#undef REG_R25
#define REG_R25 JITREG_R25
#undef REG_R26
#define REG_R26 JITREG_R26
#undef REG_R27
#define REG_R27 JITREG_R27
#undef REG_R28
#define REG_R28 JITREG_R28
#undef REG_FP
#define REG_FP JITREG_FP
#undef REG_LR
#define REG_LR JITREG_LR
#undef REG_ZR
#define REG_ZR JITREG_ZR
#undef REG_R16
#define REG_R16 JITREG_R16
#undef REG_R17
#define REG_R17 JITREG_R17
#undef REG_R18
#define REG_R18 JITREG_R18
#undef REG_R29
#define REG_R29 JITREG_R29
#undef REG_R30
#define REG_R30 JITREG_R30
#undef REG_V0
#define REG_V0 JITREG_V0
#undef REG_V1
#define REG_V1 JITREG_V1
#undef REG_V2
#define REG_V2 JITREG_V2
#undef REG_V3
#define REG_V3 JITREG_V3
#undef REG_V4
#define REG_V4 JITREG_V4
#undef REG_V5
#define REG_V5 JITREG_V5
#undef REG_V6
#define REG_V6 JITREG_V6
#undef REG_V7
#define REG_V7 JITREG_V7
#undef REG_V8
#define REG_V8 JITREG_V8
#undef REG_V9
#define REG_V9 JITREG_V9
#undef REG_V10
#define REG_V10 JITREG_V10
#undef REG_V11
#define REG_V11 JITREG_V11
#undef REG_V12
#define REG_V12 JITREG_V12
#undef REG_V13
#define REG_V13 JITREG_V13
#undef REG_V14
#define REG_V14 JITREG_V14
#undef REG_V15
#define REG_V15 JITREG_V15
#undef REG_V16
#define REG_V16 JITREG_V16
#undef REG_V17
#define REG_V17 JITREG_V17
#undef REG_V18
#define REG_V18 JITREG_V18
#undef REG_V19
#define REG_V19 JITREG_V19
#undef REG_V20
#define REG_V20 JITREG_V20
#undef REG_V21
#define REG_V21 JITREG_V21
#undef REG_V22
#define REG_V22 JITREG_V22
#undef REG_V23
#define REG_V23 JITREG_V23
#undef REG_V24
#define REG_V24 JITREG_V24
#undef REG_V25
#define REG_V25 JITREG_V25
#undef REG_V26
#define REG_V26 JITREG_V26
#undef REG_V27
#define REG_V27 JITREG_V27
#undef REG_V28
#define REG_V28 JITREG_V28
#undef REG_V29
#define REG_V29 JITREG_V29
#undef REG_V30
#define REG_V30 JITREG_V30
#undef REG_V31
#define REG_V31 JITREG_V31
#undef REG_P0
#define REG_P0 JITREG_P0
#undef REG_P1
#define REG_P1 JITREG_P1
#undef REG_P2
#define REG_P2 JITREG_P2
#undef REG_P3
#define REG_P3 JITREG_P3
#undef REG_P4
#define REG_P4 JITREG_P4
#undef REG_P5
#define REG_P5 JITREG_P5
#undef REG_P6
#define REG_P6 JITREG_P6
#undef REG_P7
#define REG_P7 JITREG_P7
#undef REG_P8
#define REG_P8 JITREG_P8
#undef REG_P9
#define REG_P9 JITREG_P9
#undef REG_P10
#define REG_P10 JITREG_P10
#undef REG_P11
#define REG_P11 JITREG_P11
#undef REG_P12
#define REG_P12 JITREG_P12
#undef REG_P13
#define REG_P13 JITREG_P13
#undef REG_P14
#define REG_P14 JITREG_P14
#undef REG_P15
#define REG_P15 JITREG_P15
#undef REG_SP
#define REG_SP JITREG_SP
#undef REG_FFR
#define REG_FFR JITREG_FFR
#undef REG_STK
#define REG_STK JITREG_STK

/*****************************************************************************/
#undef  RMASK
#undef  VMASK
#undef  VBASE
#undef  NBASE
#undef  REGDEF
#undef  REGALIAS
/*****************************************************************************/

// clang-format on
