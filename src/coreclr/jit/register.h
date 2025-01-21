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
REGDEF(RAX,     0, 0x0001, "rax"   )
REGDEF(RCX,     1, 0x0002, "rcx"   )
REGDEF(RDX,     2, 0x0004, "rdx"   )
REGDEF(RBX,     3, 0x0008, "rbx"   )
REGDEF(RSP,     4, 0x0010, "rsp"   )
REGDEF(RBP,     5, 0x0020, "rbp"   )
REGDEF(RSI,     6, 0x0040, "rsi"   )
REGDEF(RDI,     7, 0x0080, "rdi"   )
REGDEF(R8,      8, 0x0100, "r8"    )
REGDEF(R9,      9, 0x0200, "r9"    )
REGDEF(R10,    10, 0x0400, "r10"   )
REGDEF(R11,    11, 0x0800, "r11"   )
REGDEF(R12,    12, 0x1000, "r12"   )
REGDEF(R13,    13, 0x2000, "r13"   )
REGDEF(R14,    14, 0x4000, "r14"   )
REGDEF(R15,    15, 0x8000, "r15"   )

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
#define XMMBASE 16
#define XMMMASK(x) ((int64_t)(1) << ((x)+XMMBASE))

#define KBASE 48
#define KMASK(x) ((int64_t)(1) << ((x)+KBASE))

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

// Temporary workaround to avoid changing all the code that uses REG_* enum values.
// as they conflict with symbols defined in Android NDK. This will be removed later
// when we figure out a better solution.

#if defined(TARGET_XARCH)
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
#else // !defined(TARGET_X86)
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
#endif // defined(TARGET_XARCH)

#if defined(TARGET_ARM)
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
#undef REG_SP
#define REG_SP JITREG_SP
#undef REG_LR
#define REG_LR JITREG_LR
#undef REG_PC
#define REG_PC JITREG_PC
#undef REG_F0
#define REG_F0 JITREG_F0
#undef REG_F1
#define REG_F1 JITREG_F1
#undef REG_F2
#define REG_F2 JITREG_F2
#undef REG_F3
#define REG_F3 JITREG_F3
#undef REG_F4
#define REG_F4 JITREG_F4
#undef REG_F5
#define REG_F5 JITREG_F5
#undef REG_F6
#define REG_F6 JITREG_F6
#undef REG_F7
#define REG_F7 JITREG_F7
#undef REG_F8
#define REG_F8 JITREG_F8
#undef REG_F9
#define REG_F9 JITREG_F9
#undef REG_F10
#define REG_F10 JITREG_F10
#undef REG_F11
#define REG_F11 JITREG_F11
#undef REG_F12
#define REG_F12 JITREG_F12
#undef REG_F13
#define REG_F13 JITREG_F13
#undef REG_F14
#define REG_F14 JITREG_F14
#undef REG_F15
#define REG_F15 JITREG_F15
#undef REG_F16
#define REG_F16 JITREG_F16
#undef REG_F17
#define REG_F17 JITREG_F17
#undef REG_F18
#define REG_F18 JITREG_F18
#undef REG_F19
#define REG_F19 JITREG_F19
#undef REG_F20
#define REG_F20 JITREG_F20
#undef REG_F21
#define REG_F21 JITREG_F21
#undef REG_F22
#define REG_F22 JITREG_F22
#undef REG_F23
#define REG_F23 JITREG_F23
#undef REG_F24
#define REG_F24 JITREG_F24
#undef REG_F25
#define REG_F25 JITREG_F25
#undef REG_F26
#define REG_F26 JITREG_F26
#undef REG_F27
#define REG_F27 JITREG_F27
#undef REG_F28
#define REG_F28 JITREG_F28
#undef REG_F29
#define REG_F29 JITREG_F29
#undef REG_F30
#define REG_F30 JITREG_F30
#undef REG_F31
#define REG_F31 JITREG_F31
#undef REG_FP
#define REG_FP JITREG_FP
#undef REG_R13
#define REG_R13 JITREG_R13
#undef REG_R14
#define REG_R14 JITREG_R14
#undef REG_R15
#define REG_R15 JITREG_R15
#undef REG_STK
#define REG_STK JITREG_STK
#endif // defined(TARGET_ARM)

#if defined(TARGET_ARM64)
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
#endif // defined(TARGET_ARM64)

#if defined(TARGET_LOONGARCH64)
#undef REG_R0
#define REG_R0 JITREG_R0
#undef REG_RA
#define REG_RA JITREG_RA
#undef REG_TP
#define REG_TP JITREG_TP
#undef REG_SP
#define REG_SP JITREG_SP
#undef REG_A0
#define REG_A0 JITREG_A0
#undef REG_A1
#define REG_A1 JITREG_A1
#undef REG_A2
#define REG_A2 JITREG_A2
#undef REG_A3
#define REG_A3 JITREG_A3
#undef REG_A4
#define REG_A4 JITREG_A4
#undef REG_A5
#define REG_A5 JITREG_A5
#undef REG_A6
#define REG_A6 JITREG_A6
#undef REG_A7
#define REG_A7 JITREG_A7
#undef REG_T0
#define REG_T0 JITREG_T0
#undef REG_T1
#define REG_T1 JITREG_T1
#undef REG_T2
#define REG_T2 JITREG_T2
#undef REG_T3
#define REG_T3 JITREG_T3
#undef REG_T4
#define REG_T4 JITREG_T4
#undef REG_T5
#define REG_T5 JITREG_T5
#undef REG_T6
#define REG_T6 JITREG_T6
#undef REG_T7
#define REG_T7 JITREG_T7
#undef REG_T8
#define REG_T8 JITREG_T8
#undef REG_X0
#define REG_X0 JITREG_X0
#undef REG_FP
#define REG_FP JITREG_FP
#undef REG_S0
#define REG_S0 JITREG_S0
#undef REG_S1
#define REG_S1 JITREG_S1
#undef REG_S2
#define REG_S2 JITREG_S2
#undef REG_S3
#define REG_S3 JITREG_S3
#undef REG_S4
#define REG_S4 JITREG_S4
#undef REG_S5
#define REG_S5 JITREG_S5
#undef REG_S6
#define REG_S6 JITREG_S6
#undef REG_S7
#define REG_S7 JITREG_S7
#undef REG_S8
#define REG_S8 JITREG_S8
#undef REG_R21
#define REG_R21 JITREG_R21
#undef REG_F0
#define REG_F0 JITREG_F0
#undef REG_F1
#define REG_F1 JITREG_F1
#undef REG_F2
#define REG_F2 JITREG_F2
#undef REG_F3
#define REG_F3 JITREG_F3
#undef REG_F4
#define REG_F4 JITREG_F4
#undef REG_F5
#define REG_F5 JITREG_F5
#undef REG_F6
#define REG_F6 JITREG_F6
#undef REG_F7
#define REG_F7 JITREG_F7
#undef REG_F8
#define REG_F8 JITREG_F8
#undef REG_F9
#define REG_F9 JITREG_F9
#undef REG_F10
#define REG_F10 JITREG_F10
#undef REG_F11
#define REG_F11 JITREG_F11
#undef REG_F12
#define REG_F12 JITREG_F12
#undef REG_F13
#define REG_F13 JITREG_F13
#undef REG_F14
#define REG_F14 JITREG_F14
#undef REG_F15
#define REG_F15 JITREG_F15
#undef REG_F16
#define REG_F16 JITREG_F16
#undef REG_F17
#define REG_F17 JITREG_F17
#undef REG_F18
#define REG_F18 JITREG_F18
#undef REG_F19
#define REG_F19 JITREG_F19
#undef REG_F20
#define REG_F20 JITREG_F20
#undef REG_F21
#define REG_F21 JITREG_F21
#undef REG_F22
#define REG_F22 JITREG_F22
#undef REG_F23
#define REG_F23 JITREG_F23
#undef REG_F24
#define REG_F24 JITREG_F24
#undef REG_F25
#define REG_F25 JITREG_F25
#undef REG_F26
#define REG_F26 JITREG_F26
#undef REG_F27
#define REG_F27 JITREG_F27
#undef REG_F28
#define REG_F28 JITREG_F28
#undef REG_F29
#define REG_F29 JITREG_F29
#undef REG_F30
#define REG_F30 JITREG_F30
#undef REG_F31
#define REG_F31 JITREG_F31
#undef REG_STK
#define REG_STK JITREG_STK
#endif // defined(TARGET_LOONGARCH64)

#if defined(TARGET_RISCV64)
#undef REG_R0
#define REG_R0 JITREG_R0
#undef REG_RA
#define REG_RA JITREG_RA
#undef REG_SP
#define REG_SP JITREG_SP
#undef REG_GP
#define REG_GP JITREG_GP
#undef REG_TP
#define REG_TP JITREG_TP
#undef REG_T0
#define REG_T0 JITREG_T0
#undef REG_T1
#define REG_T1 JITREG_T1
#undef REG_T2
#define REG_T2 JITREG_T2
#undef REG_FP
#define REG_FP JITREG_FP
#undef REG_S1
#define REG_S1 JITREG_S1
#undef REG_A0
#define REG_A0 JITREG_A0
#undef REG_A1
#define REG_A1 JITREG_A1
#undef REG_A2
#define REG_A2 JITREG_A2
#undef REG_A3
#define REG_A3 JITREG_A3
#undef REG_A4
#define REG_A4 JITREG_A4
#undef REG_A5
#define REG_A5 JITREG_A5
#undef REG_A6
#define REG_A6 JITREG_A6
#undef REG_A7
#define REG_A7 JITREG_A7
#undef REG_S2
#define REG_S2 JITREG_S2
#undef REG_S3
#define REG_S3 JITREG_S3
#undef REG_S4
#define REG_S4 JITREG_S4
#undef REG_S5
#define REG_S5 JITREG_S5
#undef REG_S6
#define REG_S6 JITREG_S6
#undef REG_S7
#define REG_S7 JITREG_S7
#undef REG_S8
#define REG_S8 JITREG_S8
#undef REG_S9
#define REG_S9 JITREG_S9
#undef REG_S10
#define REG_S10 JITREG_S10
#undef REG_S11
#define REG_S11 JITREG_S11
#undef REG_T3
#define REG_T3 JITREG_T3
#undef REG_T4
#define REG_T4 JITREG_T4
#undef REG_T5
#define REG_T5 JITREG_T5
#undef REG_T6
#define REG_T6 JITREG_T6
#undef REG_R8
#define REG_R8 JITREG_R8
#undef REG_ZERO
#define REG_ZERO JITREG_ZERO
#undef REG_FT0
#define REG_FT0 JITREG_FT0
#undef REG_FT1
#define REG_FT1 JITREG_FT1
#undef REG_FT2
#define REG_FT2 JITREG_FT2
#undef REG_FT3
#define REG_FT3 JITREG_FT3
#undef REG_FT4
#define REG_FT4 JITREG_FT4
#undef REG_FT5
#define REG_FT5 JITREG_FT5
#undef REG_FT6
#define REG_FT6 JITREG_FT6
#undef REG_FT7
#define REG_FT7 JITREG_FT7
#undef REG_FS0
#define REG_FS0 JITREG_FS0
#undef REG_FS1
#define REG_FS1 JITREG_FS1
#undef REG_FA0
#define REG_FA0 JITREG_FA0
#undef REG_FA1
#define REG_FA1 JITREG_FA1
#undef REG_FA2
#define REG_FA2 JITREG_FA2
#undef REG_FA3
#define REG_FA3 JITREG_FA3
#undef REG_FA4
#define REG_FA4 JITREG_FA4
#undef REG_FA5
#define REG_FA5 JITREG_FA5
#undef REG_FA6
#define REG_FA6 JITREG_FA6
#undef REG_FA7
#define REG_FA7 JITREG_FA7
#undef REG_FS2
#define REG_FS2 JITREG_FS2
#undef REG_FS3
#define REG_FS3 JITREG_FS3
#undef REG_FS4
#define REG_FS4 JITREG_FS4
#undef REG_FS5
#define REG_FS5 JITREG_FS5
#undef REG_FS6
#define REG_FS6 JITREG_FS6
#undef REG_FS7
#define REG_FS7 JITREG_FS7
#undef REG_FS8
#define REG_FS8 JITREG_FS8
#undef REG_FS9
#define REG_FS9 JITREG_FS9
#undef REG_FS10
#define REG_FS10 JITREG_FS10
#undef REG_FS11
#define REG_FS11 JITREG_FS11
#undef REG_FT8
#define REG_FT8 JITREG_FT8
#undef REG_FT9
#define REG_FT9 JITREG_FT9
#undef REG_FT10
#define REG_FT10 JITREG_FT10
#undef REG_FT11
#define REG_FT11 JITREG_FT11
#undef REG_STK
#define REG_STK JITREG_STK
#endif // defined(TARGET_RISCV64)





#if defined(TARGET_XARCH)
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
#else // !defined(TARGET_X86)
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
#endif // defined(TARGET_XARCH)

#if defined(TARGET_ARM)
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
#undef REG_SP
#define REG_SP JITREG_SP
#undef REG_LR
#define REG_LR JITREG_LR
#undef REG_PC
#define REG_PC JITREG_PC
#undef REG_F0
#define REG_F0 JITREG_F0
#undef REG_F1
#define REG_F1 JITREG_F1
#undef REG_F2
#define REG_F2 JITREG_F2
#undef REG_F3
#define REG_F3 JITREG_F3
#undef REG_F4
#define REG_F4 JITREG_F4
#undef REG_F5
#define REG_F5 JITREG_F5
#undef REG_F6
#define REG_F6 JITREG_F6
#undef REG_F7
#define REG_F7 JITREG_F7
#undef REG_F8
#define REG_F8 JITREG_F8
#undef REG_F9
#define REG_F9 JITREG_F9
#undef REG_F10
#define REG_F10 JITREG_F10
#undef REG_F11
#define REG_F11 JITREG_F11
#undef REG_F12
#define REG_F12 JITREG_F12
#undef REG_F13
#define REG_F13 JITREG_F13
#undef REG_F14
#define REG_F14 JITREG_F14
#undef REG_F15
#define REG_F15 JITREG_F15
#undef REG_F16
#define REG_F16 JITREG_F16
#undef REG_F17
#define REG_F17 JITREG_F17
#undef REG_F18
#define REG_F18 JITREG_F18
#undef REG_F19
#define REG_F19 JITREG_F19
#undef REG_F20
#define REG_F20 JITREG_F20
#undef REG_F21
#define REG_F21 JITREG_F21
#undef REG_F22
#define REG_F22 JITREG_F22
#undef REG_F23
#define REG_F23 JITREG_F23
#undef REG_F24
#define REG_F24 JITREG_F24
#undef REG_F25
#define REG_F25 JITREG_F25
#undef REG_F26
#define REG_F26 JITREG_F26
#undef REG_F27
#define REG_F27 JITREG_F27
#undef REG_F28
#define REG_F28 JITREG_F28
#undef REG_F29
#define REG_F29 JITREG_F29
#undef REG_F30
#define REG_F30 JITREG_F30
#undef REG_F31
#define REG_F31 JITREG_F31
#undef REG_FP
#define REG_FP JITREG_FP
#undef REG_R13
#define REG_R13 JITREG_R13
#undef REG_R14
#define REG_R14 JITREG_R14
#undef REG_R15
#define REG_R15 JITREG_R15
#undef REG_STK
#define REG_STK JITREG_STK
#endif // defined(TARGET_ARM)

#if defined(TARGET_ARM64)
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
#endif // defined(TARGET_ARM64)

#if defined(TARGET_LOONGARCH64)
#undef REG_R0
#define REG_R0 JITREG_R0
#undef REG_RA
#define REG_RA JITREG_RA
#undef REG_TP
#define REG_TP JITREG_TP
#undef REG_SP
#define REG_SP JITREG_SP
#undef REG_A0
#define REG_A0 JITREG_A0
#undef REG_A1
#define REG_A1 JITREG_A1
#undef REG_A2
#define REG_A2 JITREG_A2
#undef REG_A3
#define REG_A3 JITREG_A3
#undef REG_A4
#define REG_A4 JITREG_A4
#undef REG_A5
#define REG_A5 JITREG_A5
#undef REG_A6
#define REG_A6 JITREG_A6
#undef REG_A7
#define REG_A7 JITREG_A7
#undef REG_T0
#define REG_T0 JITREG_T0
#undef REG_T1
#define REG_T1 JITREG_T1
#undef REG_T2
#define REG_T2 JITREG_T2
#undef REG_T3
#define REG_T3 JITREG_T3
#undef REG_T4
#define REG_T4 JITREG_T4
#undef REG_T5
#define REG_T5 JITREG_T5
#undef REG_T6
#define REG_T6 JITREG_T6
#undef REG_T7
#define REG_T7 JITREG_T7
#undef REG_T8
#define REG_T8 JITREG_T8
#undef REG_X0
#define REG_X0 JITREG_X0
#undef REG_FP
#define REG_FP JITREG_FP
#undef REG_S0
#define REG_S0 JITREG_S0
#undef REG_S1
#define REG_S1 JITREG_S1
#undef REG_S2
#define REG_S2 JITREG_S2
#undef REG_S3
#define REG_S3 JITREG_S3
#undef REG_S4
#define REG_S4 JITREG_S4
#undef REG_S5
#define REG_S5 JITREG_S5
#undef REG_S6
#define REG_S6 JITREG_S6
#undef REG_S7
#define REG_S7 JITREG_S7
#undef REG_S8
#define REG_S8 JITREG_S8
#undef REG_R21
#define REG_R21 JITREG_R21
#undef REG_F0
#define REG_F0 JITREG_F0
#undef REG_F1
#define REG_F1 JITREG_F1
#undef REG_F2
#define REG_F2 JITREG_F2
#undef REG_F3
#define REG_F3 JITREG_F3
#undef REG_F4
#define REG_F4 JITREG_F4
#undef REG_F5
#define REG_F5 JITREG_F5
#undef REG_F6
#define REG_F6 JITREG_F6
#undef REG_F7
#define REG_F7 JITREG_F7
#undef REG_F8
#define REG_F8 JITREG_F8
#undef REG_F9
#define REG_F9 JITREG_F9
#undef REG_F10
#define REG_F10 JITREG_F10
#undef REG_F11
#define REG_F11 JITREG_F11
#undef REG_F12
#define REG_F12 JITREG_F12
#undef REG_F13
#define REG_F13 JITREG_F13
#undef REG_F14
#define REG_F14 JITREG_F14
#undef REG_F15
#define REG_F15 JITREG_F15
#undef REG_F16
#define REG_F16 JITREG_F16
#undef REG_F17
#define REG_F17 JITREG_F17
#undef REG_F18
#define REG_F18 JITREG_F18
#undef REG_F19
#define REG_F19 JITREG_F19
#undef REG_F20
#define REG_F20 JITREG_F20
#undef REG_F21
#define REG_F21 JITREG_F21
#undef REG_F22
#define REG_F22 JITREG_F22
#undef REG_F23
#define REG_F23 JITREG_F23
#undef REG_F24
#define REG_F24 JITREG_F24
#undef REG_F25
#define REG_F25 JITREG_F25
#undef REG_F26
#define REG_F26 JITREG_F26
#undef REG_F27
#define REG_F27 JITREG_F27
#undef REG_F28
#define REG_F28 JITREG_F28
#undef REG_F29
#define REG_F29 JITREG_F29
#undef REG_F30
#define REG_F30 JITREG_F30
#undef REG_F31
#define REG_F31 JITREG_F31
#undef REG_STK
#define REG_STK JITREG_STK
#endif // defined(TARGET_LOONGARCH64)

#if defined(TARGET_RISCV64)
#undef REG_R0
#define REG_R0 JITREG_R0
#undef REG_RA
#define REG_RA JITREG_RA
#undef REG_SP
#define REG_SP JITREG_SP
#undef REG_GP
#define REG_GP JITREG_GP
#undef REG_TP
#define REG_TP JITREG_TP
#undef REG_T0
#define REG_T0 JITREG_T0
#undef REG_T1
#define REG_T1 JITREG_T1
#undef REG_T2
#define REG_T2 JITREG_T2
#undef REG_FP
#define REG_FP JITREG_FP
#undef REG_S1
#define REG_S1 JITREG_S1
#undef REG_A0
#define REG_A0 JITREG_A0
#undef REG_A1
#define REG_A1 JITREG_A1
#undef REG_A2
#define REG_A2 JITREG_A2
#undef REG_A3
#define REG_A3 JITREG_A3
#undef REG_A4
#define REG_A4 JITREG_A4
#undef REG_A5
#define REG_A5 JITREG_A5
#undef REG_A6
#define REG_A6 JITREG_A6
#undef REG_A7
#define REG_A7 JITREG_A7
#undef REG_S2
#define REG_S2 JITREG_S2
#undef REG_S3
#define REG_S3 JITREG_S3
#undef REG_S4
#define REG_S4 JITREG_S4
#undef REG_S5
#define REG_S5 JITREG_S5
#undef REG_S6
#define REG_S6 JITREG_S6
#undef REG_S7
#define REG_S7 JITREG_S7
#undef REG_S8
#define REG_S8 JITREG_S8
#undef REG_S9
#define REG_S9 JITREG_S9
#undef REG_S10
#define REG_S10 JITREG_S10
#undef REG_S11
#define REG_S11 JITREG_S11
#undef REG_T3
#define REG_T3 JITREG_T3
#undef REG_T4
#define REG_T4 JITREG_T4
#undef REG_T5
#define REG_T5 JITREG_T5
#undef REG_T6
#define REG_T6 JITREG_T6
#undef REG_R8
#define REG_R8 JITREG_R8
#undef REG_ZERO
#define REG_ZERO JITREG_ZERO
#undef REG_FT0
#define REG_FT0 JITREG_FT0
#undef REG_FT1
#define REG_FT1 JITREG_FT1
#undef REG_FT2
#define REG_FT2 JITREG_FT2
#undef REG_FT3
#define REG_FT3 JITREG_FT3
#undef REG_FT4
#define REG_FT4 JITREG_FT4
#undef REG_FT5
#define REG_FT5 JITREG_FT5
#undef REG_FT6
#define REG_FT6 JITREG_FT6
#undef REG_FT7
#define REG_FT7 JITREG_FT7
#undef REG_FS0
#define REG_FS0 JITREG_FS0
#undef REG_FS1
#define REG_FS1 JITREG_FS1
#undef REG_FA0
#define REG_FA0 JITREG_FA0
#undef REG_FA1
#define REG_FA1 JITREG_FA1
#undef REG_FA2
#define REG_FA2 JITREG_FA2
#undef REG_FA3
#define REG_FA3 JITREG_FA3
#undef REG_FA4
#define REG_FA4 JITREG_FA4
#undef REG_FA5
#define REG_FA5 JITREG_FA5
#undef REG_FA6
#define REG_FA6 JITREG_FA6
#undef REG_FA7
#define REG_FA7 JITREG_FA7
#undef REG_FS2
#define REG_FS2 JITREG_FS2
#undef REG_FS3
#define REG_FS3 JITREG_FS3
#undef REG_FS4
#define REG_FS4 JITREG_FS4
#undef REG_FS5
#define REG_FS5 JITREG_FS5
#undef REG_FS6
#define REG_FS6 JITREG_FS6
#undef REG_FS7
#define REG_FS7 JITREG_FS7
#undef REG_FS8
#define REG_FS8 JITREG_FS8
#undef REG_FS9
#define REG_FS9 JITREG_FS9
#undef REG_FS10
#define REG_FS10 JITREG_FS10
#undef REG_FS11
#define REG_FS11 JITREG_FS11
#undef REG_FT8
#define REG_FT8 JITREG_FT8
#undef REG_FT9
#define REG_FT9 JITREG_FT9
#undef REG_FT10
#define REG_FT10 JITREG_FT10
#undef REG_FT11
#define REG_FT11 JITREG_FT11
#undef REG_STK
#define REG_STK JITREG_STK
#endif // defined(TARGET_RISCV64)

// clang-format on
