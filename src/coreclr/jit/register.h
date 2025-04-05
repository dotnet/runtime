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

#if defined(TARGET_XARCH)

#if defined(TARGET_X86)
/*
REGDEF(name, rnum,   mask, sname) */
REGDEF(EAX,     0,   0x01, "eax"   )
REGDEF(ECX,     1,   0x02, "ecx"   )
REGDEF(EDX,     2,   0x04, "edx"   )
REGDEF(EBX,     3,   0x08, "ebx"   )
REGDEF(ESP,     4,   0x10, "esp"   )
REGDEF(EBP,     5,   0x20, "ebp"   )
REGDEF(ESI,     6,   0x40, "esi"   )
REGDEF(EDI,     7,   0x80, "edi"   )
REGALIAS(RAX, EAX)
REGALIAS(RCX, ECX)
REGALIAS(RDX, EDX)
REGALIAS(RBX, EBX)
REGALIAS(RSP, ESP)
REGALIAS(RBP, EBP)
REGALIAS(RSI, ESI)
REGALIAS(RDI, EDI)

#else // !defined(TARGET_X86)

#define GPRMASK(x) (1ULL << (x))
/*
REGDEF(name, rnum,   mask, sname) */
REGDEF(RAX,     0, GPRMASK(0), "rax"   )
REGDEF(RCX,     1, GPRMASK(1), "rcx"   )
REGDEF(RDX,     2, GPRMASK(2), "rdx"   )
REGDEF(RBX,     3, GPRMASK(3), "rbx"   )
REGDEF(RSP,     4, GPRMASK(4), "rsp"   )
REGDEF(RBP,     5, GPRMASK(5), "rbp"   )
REGDEF(RSI,     6, GPRMASK(6), "rsi"   )
REGDEF(RDI,     7, GPRMASK(7), "rdi"   )
REGDEF(R8,      8, GPRMASK(8), "r8"    )
REGDEF(R9,      9, GPRMASK(9), "r9"    )
REGDEF(R10,    10, GPRMASK(10), "r10"   )
REGDEF(R11,    11, GPRMASK(11), "r11"   )
REGDEF(R12,    12, GPRMASK(12), "r12"   )
REGDEF(R13,    13, GPRMASK(13), "r13"   )
REGDEF(R14,    14, GPRMASK(14), "r14"   )
REGDEF(R15,    15, GPRMASK(15), "r15"   )
REGDEF(R16,    16, GPRMASK(16), "r16"   )
REGDEF(R17,    17, GPRMASK(17), "r17"   )
REGDEF(R18,    18, GPRMASK(18), "r18"   )
REGDEF(R19,    19, GPRMASK(19), "r19"   )
REGDEF(R20,    20, GPRMASK(20), "r20"   )
REGDEF(R21,    21, GPRMASK(21), "r21"   )
REGDEF(R22,    22, GPRMASK(22), "r22"   )
REGDEF(R23,    23, GPRMASK(23), "r23"   )
REGDEF(R24,    24, GPRMASK(24), "r24"   )
REGDEF(R25,    25, GPRMASK(25), "r25"   )
REGDEF(R26,    26, GPRMASK(26), "r26"   )
REGDEF(R27,    27, GPRMASK(27), "r27"   )
REGDEF(R28,    28, GPRMASK(28), "r28"   )
REGDEF(R29,    29, GPRMASK(29), "r29"   )
REGDEF(R30,    30, GPRMASK(30), "r30"   )
REGDEF(R31,    31, GPRMASK(31), "r31"   )

REGALIAS(EAX, RAX)
REGALIAS(ECX, RCX)
REGALIAS(EDX, RDX)
REGALIAS(EBX, RBX)
REGALIAS(ESP, RSP)
REGALIAS(EBP, RBP)
REGALIAS(ESI, RSI)
REGALIAS(EDI, RDI)

#endif // !defined(TARGET_X86)

#ifdef TARGET_AMD64
#define XMMBASE 32
#define XMMMASK(x) (1ULL << ((x)+XMMBASE))

#define KBASE 64
#define KMASK(x) (1ULL << ((x)))

#else // !TARGET_AMD64
#define XMMBASE 8
#define XMMMASK(x) ((int32_t)(1) << ((x)+XMMBASE))

#define KBASE 16
#define KMASK(x) ((int32_t)(1) << ((x)+KBASE))


#endif // !TARGET_AMD64

REGDEF(XMM0,    0+XMMBASE,  XMMMASK(0),   "mm0"  )
REGDEF(XMM1,    1+XMMBASE,  XMMMASK(1),   "mm1"  )
REGDEF(XMM2,    2+XMMBASE,  XMMMASK(2),   "mm2"  )
REGDEF(XMM3,    3+XMMBASE,  XMMMASK(3),   "mm3"  )
REGDEF(XMM4,    4+XMMBASE,  XMMMASK(4),   "mm4"  )
REGDEF(XMM5,    5+XMMBASE,  XMMMASK(5),   "mm5"  )
REGDEF(XMM6,    6+XMMBASE,  XMMMASK(6),   "mm6"  )
REGDEF(XMM7,    7+XMMBASE,  XMMMASK(7),   "mm7"  )

#ifdef TARGET_AMD64
REGDEF(XMM8,    8+XMMBASE,  XMMMASK(8),   "mm8"  )
REGDEF(XMM9,    9+XMMBASE,  XMMMASK(9),   "mm9"  )
REGDEF(XMM10,  10+XMMBASE,  XMMMASK(10),  "mm10" )
REGDEF(XMM11,  11+XMMBASE,  XMMMASK(11),  "mm11" )
REGDEF(XMM12,  12+XMMBASE,  XMMMASK(12),  "mm12" )
REGDEF(XMM13,  13+XMMBASE,  XMMMASK(13),  "mm13" )
REGDEF(XMM14,  14+XMMBASE,  XMMMASK(14),  "mm14" )
REGDEF(XMM15,  15+XMMBASE,  XMMMASK(15),  "mm15" )

REGDEF(XMM16,  16+XMMBASE,  XMMMASK(16),  "mm16" )
REGDEF(XMM17,  17+XMMBASE,  XMMMASK(17),  "mm17" )
REGDEF(XMM18,  18+XMMBASE,  XMMMASK(18),  "mm18" )
REGDEF(XMM19,  19+XMMBASE,  XMMMASK(19),  "mm19" )
REGDEF(XMM20,  20+XMMBASE,  XMMMASK(20),  "mm20" )
REGDEF(XMM21,  21+XMMBASE,  XMMMASK(21),  "mm21" )
REGDEF(XMM22,  22+XMMBASE,  XMMMASK(22),  "mm22" )
REGDEF(XMM23,  23+XMMBASE,  XMMMASK(23),  "mm23" )

REGDEF(XMM24,  24+XMMBASE,  XMMMASK(24),  "mm24" )
REGDEF(XMM25,  25+XMMBASE,  XMMMASK(25),  "mm25" )
REGDEF(XMM26,  26+XMMBASE,  XMMMASK(26),  "mm26" )
REGDEF(XMM27,  27+XMMBASE,  XMMMASK(27),  "mm27" )
REGDEF(XMM28,  28+XMMBASE,  XMMMASK(28),  "mm28" )
REGDEF(XMM29,  29+XMMBASE,  XMMMASK(29),  "mm29" )
REGDEF(XMM30,  30+XMMBASE,  XMMMASK(30),  "mm30" )
REGDEF(XMM31,  31+XMMBASE,  XMMMASK(31),  "mm31" )

#endif // !TARGET_AMD64

REGDEF(K0,     0+KBASE,    KMASK(0),     "k0"   )
REGDEF(K1,     1+KBASE,    KMASK(1),     "k1"   )
REGDEF(K2,     2+KBASE,    KMASK(2),     "k2"   )
REGDEF(K3,     3+KBASE,    KMASK(3),     "k3"   )
REGDEF(K4,     4+KBASE,    KMASK(4),     "k4"   )
REGDEF(K5,     5+KBASE,    KMASK(5),     "k5"   )
REGDEF(K6,     6+KBASE,    KMASK(6),     "k6"   )
REGDEF(K7,     7+KBASE,    KMASK(7),     "k7"   )

REGDEF(STK,    8+KBASE,    0x0000,       "STK"  )

// Ignore REG_* symbols defined in Android NDK
#if defined(TARGET_X86)
#undef REG_EAX
#define REG_EAX JITREG_EAX
#undef REG_ECX
#define REG_ECX JITREG_ECX
#undef REG_EDX
#define REG_EDX JITREG_EDX
#undef REG_EBX
#define REG_EBX JITREG_EBX
#undef REG_ESP
#define REG_ESP JITREG_ESP
#undef REG_EBP
#define REG_EBP JITREG_EBP
#undef REG_ESI
#define REG_ESI JITREG_ESI
#undef REG_EDI
#define REG_EDI JITREG_EDI
#undef REG_RAX
#define REG_RAX JITREG_RAX
#undef REG_RCX
#define REG_RCX JITREG_RCX
#undef REG_RDX
#define REG_RDX JITREG_RDX
#undef REG_RBX
#define REG_RBX JITREG_RBX
#undef REG_RSP
#define REG_RSP JITREG_RSP
#undef REG_RBP
#define REG_RBP JITREG_RBP
#undef REG_RSI
#define REG_RSI JITREG_RSI
#undef REG_RDI
#define REG_RDI JITREG_RDI
#else // defined(TARGET_X86)
#undef REG_RAX
#define REG_RAX JITREG_RAX
#undef REG_RCX
#define REG_RCX JITREG_RCX
#undef REG_RDX
#define REG_RDX JITREG_RDX
#undef REG_RBX
#define REG_RBX JITREG_RBX
#undef REG_RSP
#define REG_RSP JITREG_RSP
#undef REG_RBP
#define REG_RBP JITREG_RBP
#undef REG_RSI
#define REG_RSI JITREG_RSI
#undef REG_RDI
#define REG_RDI JITREG_RDI
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
#undef REG_R16
#define REG_R16 JITREG_R16
#undef REG_R17
#define REG_R17 JITREG_R17
#undef REG_R18
#define REG_R18 JITREG_R18
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
#undef REG_R29
#define REG_R29 JITREG_R29
#undef REG_R30
#define REG_R30 JITREG_R30
#undef REG_R31
#define REG_R31 JITREG_R31
#undef REG_EAX
#define REG_EAX JITREG_EAX
#undef REG_ECX
#define REG_ECX JITREG_ECX
#undef REG_EDX
#define REG_EDX JITREG_EDX
#undef REG_EBX
#define REG_EBX JITREG_EBX
#undef REG_ESP
#define REG_ESP JITREG_ESP
#undef REG_EBP
#define REG_EBP JITREG_EBP
#undef REG_ESI
#define REG_ESI JITREG_ESI
#undef REG_EDI
#define REG_EDI JITREG_EDI
#endif // !defined(TARGET_X86)

#undef REG_XMM0
#define REG_XMM0 JITREG_XMM0
#undef REG_XMM1
#define REG_XMM1 JITREG_XMM1
#undef REG_XMM2
#define REG_XMM2 JITREG_XMM2
#undef REG_XMM3
#define REG_XMM3 JITREG_XMM3
#undef REG_XMM4
#define REG_XMM4 JITREG_XMM4
#undef REG_XMM5
#define REG_XMM5 JITREG_XMM5
#undef REG_XMM6
#define REG_XMM6 JITREG_XMM6
#undef REG_XMM7
#define REG_XMM7 JITREG_XMM7

#ifdef TARGET_AMD64
#undef REG_XMM8
#define REG_XMM8 JITREG_XMM8
#undef REG_XMM9
#define REG_XMM9 JITREG_XMM9
#undef REG_XMM10
#define REG_XMM10 JITREG_XMM10
#undef REG_XMM11
#define REG_XMM11 JITREG_XMM11
#undef REG_XMM12
#define REG_XMM12 JITREG_XMM12
#undef REG_XMM13
#define REG_XMM13 JITREG_XMM13
#undef REG_XMM14
#define REG_XMM14 JITREG_XMM14
#undef REG_XMM15
#define REG_XMM15 JITREG_XMM15
#undef REG_XMM16
#define REG_XMM16 JITREG_XMM16
#undef REG_XMM17
#define REG_XMM17 JITREG_XMM17
#undef REG_XMM18
#define REG_XMM18 JITREG_XMM18
#undef REG_XMM19
#define REG_XMM19 JITREG_XMM19
#undef REG_XMM20
#define REG_XMM20 JITREG_XMM20
#undef REG_XMM21
#define REG_XMM21 JITREG_XMM21
#undef REG_XMM22
#define REG_XMM22 JITREG_XMM22
#undef REG_XMM23
#define REG_XMM23 JITREG_XMM23
#undef REG_XMM24
#define REG_XMM24 JITREG_XMM24
#undef REG_XMM25
#define REG_XMM25 JITREG_XMM25
#undef REG_XMM26
#define REG_XMM26 JITREG_XMM26
#undef REG_XMM27
#define REG_XMM27 JITREG_XMM27
#undef REG_XMM28
#define REG_XMM28 JITREG_XMM28
#undef REG_XMM29
#define REG_XMM29 JITREG_XMM29
#undef REG_XMM30
#define REG_XMM30 JITREG_XMM30
#undef REG_XMM31
#define REG_XMM31 JITREG_XMM31
#endif // TARGET_AMD64

#undef REG_K0
#define REG_K0 JITREG_K0
#undef REG_K1
#define REG_K1 JITREG_K1
#undef REG_K2
#define REG_K2 JITREG_K2
#undef REG_K3
#define REG_K3 JITREG_K3
#undef REG_K4
#define REG_K4 JITREG_K4
#undef REG_K5
#define REG_K5 JITREG_K5
#undef REG_K6
#define REG_K6 JITREG_K6
#undef REG_K7
#define REG_K7 JITREG_K7
#undef REG_STK
#define REG_STK JITREG_STK

#elif defined(TARGET_ARM)
 #include "registerarm.h"

#elif defined(TARGET_ARM64)
 #include "registerarm64.h"

#elif defined(TARGET_LOONGARCH64)
 #include "registerloongarch64.h"

#elif defined(TARGET_RISCV64)
 #include "registerriscv64.h"

#else
  #error Unsupported or unset target architecture
#endif // target type
/*****************************************************************************/
#undef  REGDEF
#undef  REGALIAS
#undef  XMMMASK
/*****************************************************************************/

// clang-format on
