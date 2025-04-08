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

/*
REGDEF(name, rnum,   mask, sname) */
REGDEF(RAX,     0, 0x00000001, "rax"   )
REGDEF(RCX,     1, 0x00000002, "rcx"   )
REGDEF(RDX,     2, 0x00000004, "rdx"   )
REGDEF(RBX,     3, 0x00000008, "rbx"   )
REGDEF(RSP,     4, 0x00000010, "rsp"   )
REGDEF(RBP,     5, 0x00000020, "rbp"   )
REGDEF(RSI,     6, 0x00000040, "rsi"   )
REGDEF(RDI,     7, 0x00000080, "rdi"   )
REGDEF(R8,      8, 0x00000100, "r8"    )
REGDEF(R9,      9, 0x00000200, "r9"    )
REGDEF(R10,    10, 0x00000400, "r10"   )
REGDEF(R11,    11, 0x00000800, "r11"   )
REGDEF(R12,    12, 0x00001000, "r12"   )
REGDEF(R13,    13, 0x00002000, "r13"   )
REGDEF(R14,    14, 0x00004000, "r14"   )
REGDEF(R15,    15, 0x00008000, "r15"   )
REGDEF(R16,    16, 0x00010000, "r16"   )
REGDEF(R17,    17, 0x00020000, "r17"   )
REGDEF(R18,    18, 0x00040000, "r18"   )
REGDEF(R19,    19, 0x00080000, "r19"   )
REGDEF(R20,    20, 0x00100000, "r20"   )
REGDEF(R21,    21, 0x00200000, "r21"   )
REGDEF(R22,    22, 0x00400000, "r22"   )
REGDEF(R23,    23, 0x00800000, "r23"   )

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
#define XMMBASE 24
#define XMMMASK(x) (1ULL << ((x)+XMMBASE))

#define KBASE 56
#define KMASK(x) (1ULL << ((x)+KBASE))

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
