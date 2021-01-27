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

// The registers with values 64 (NBASE) and above are not real register numbers
#define NBASE 64

REGDEF(SP,    0+NBASE, 0x0000,    "sp",  "wsp?")
// This must be last!
REGDEF(STK,   1+NBASE, 0x0000,    "STK", "STK")

/*****************************************************************************/
#undef  RMASK
#undef  VMASK
#undef  VBASE
#undef  NBASE
#undef  REGDEF
#undef  REGALIAS
/*****************************************************************************/

// clang-format on
