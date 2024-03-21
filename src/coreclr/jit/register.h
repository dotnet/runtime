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
REGDEF(name, rnum,   mask, sname, regTypeTag) */
REGDEF(EAX,     0,   0x01, "eax", 0)
REGDEF(ECX,     1,   0x02, "ecx", 0)
REGDEF(EDX,     2,   0x04, "edx", 0)
REGDEF(EBX,     3,   0x08, "ebx", 0)
REGDEF(ESP,     4,   0x10, "esp", 0)
REGDEF(EBP,     5,   0x20, "ebp", 0)
REGDEF(ESI,     6,   0x40, "esi", 0)
REGDEF(EDI,     7,   0x80, "edi", 0)
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
REGDEF(name, rnum,   mask, sname, regTypeTag) */
REGDEF(RAX,     0, 0x0001, "rax", 0)
REGDEF(RCX,     1, 0x0002, "rcx", 0)
REGDEF(RDX,     2, 0x0004, "rdx", 0)
REGDEF(RBX,     3, 0x0008, "rbx", 0)
REGDEF(RSP,     4, 0x0010, "rsp", 0)
REGDEF(RBP,     5, 0x0020, "rbp", 0)
REGDEF(RSI,     6, 0x0040, "rsi", 0)
REGDEF(RDI,     7, 0x0080, "rdi", 0)
REGDEF(R8,      8, 0x0100, "r8" , 0)
REGDEF(R9,      9, 0x0200, "r9" , 0)
REGDEF(R10,    10, 0x0400, "r10", 0)
REGDEF(R11,    11, 0x0800, "r11", 0)
REGDEF(R12,    12, 0x1000, "r12", 0)
REGDEF(R13,    13, 0x2000, "r13", 0)
REGDEF(R14,    14, 0x4000, "r14", 0)
REGDEF(R15,    15, 0x8000, "r15", 0)

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
#define XMMMASK(x) ((__int64)(1) << ((x)+XMMBASE))

#define KBASE 48
#define KMASK(x) ((__int64)(1) << ((x)+KBASE))

#else // !TARGET_AMD64
#define XMMBASE 8
#define XMMMASK(x) ((__int32)(1) << ((x)+XMMBASE))

#define KBASE 16
#define KMASK(x) ((__int32)(1) << ((x)+KBASE))


#endif // !TARGET_AMD64

REGDEF(XMM0,    0+XMMBASE,  XMMMASK(0),   "mm0",  1)
REGDEF(XMM1,    1+XMMBASE,  XMMMASK(1),   "mm1",  1)
REGDEF(XMM2,    2+XMMBASE,  XMMMASK(2),   "mm2",  1)
REGDEF(XMM3,    3+XMMBASE,  XMMMASK(3),   "mm3",  1)
REGDEF(XMM4,    4+XMMBASE,  XMMMASK(4),   "mm4",  1)
REGDEF(XMM5,    5+XMMBASE,  XMMMASK(5),   "mm5",  1)
REGDEF(XMM6,    6+XMMBASE,  XMMMASK(6),   "mm6",  1)
REGDEF(XMM7,    7+XMMBASE,  XMMMASK(7),   "mm7",  1)

#ifdef TARGET_AMD64
REGDEF(XMM8,    8+XMMBASE,  XMMMASK(8),   "mm8",  1)
REGDEF(XMM9,    9+XMMBASE,  XMMMASK(9),   "mm9",  1)
REGDEF(XMM10,  10+XMMBASE,  XMMMASK(10),  "mm10", 1)
REGDEF(XMM11,  11+XMMBASE,  XMMMASK(11),  "mm11", 1)
REGDEF(XMM12,  12+XMMBASE,  XMMMASK(12),  "mm12", 1)
REGDEF(XMM13,  13+XMMBASE,  XMMMASK(13),  "mm13", 1)
REGDEF(XMM14,  14+XMMBASE,  XMMMASK(14),  "mm14", 1)
REGDEF(XMM15,  15+XMMBASE,  XMMMASK(15),  "mm15", 1)

REGDEF(XMM16,  16+XMMBASE,  XMMMASK(16),  "mm16", 1)
REGDEF(XMM17,  17+XMMBASE,  XMMMASK(17),  "mm17", 1)
REGDEF(XMM18,  18+XMMBASE,  XMMMASK(18),  "mm18", 1)
REGDEF(XMM19,  19+XMMBASE,  XMMMASK(19),  "mm19", 1)
REGDEF(XMM20,  20+XMMBASE,  XMMMASK(20),  "mm20", 1)
REGDEF(XMM21,  21+XMMBASE,  XMMMASK(21),  "mm21", 1)
REGDEF(XMM22,  22+XMMBASE,  XMMMASK(22),  "mm22", 1)
REGDEF(XMM23,  23+XMMBASE,  XMMMASK(23),  "mm23", 1)

REGDEF(XMM24,  24+XMMBASE,  XMMMASK(24),  "mm24", 1)
REGDEF(XMM25,  25+XMMBASE,  XMMMASK(25),  "mm25", 1)
REGDEF(XMM26,  26+XMMBASE,  XMMMASK(26),  "mm26", 1)
REGDEF(XMM27,  27+XMMBASE,  XMMMASK(27),  "mm27", 1)
REGDEF(XMM28,  28+XMMBASE,  XMMMASK(28),  "mm28", 1)
REGDEF(XMM29,  29+XMMBASE,  XMMMASK(29),  "mm29", 1)
REGDEF(XMM30,  30+XMMBASE,  XMMMASK(30),  "mm30", 1)
REGDEF(XMM31,  31+XMMBASE,  XMMMASK(31),  "mm31", 1)

#endif // !TARGET_AMD64

REGDEF(K0,     0+KBASE,    KMASK(0),     "k0", 2)
REGDEF(K1,     1+KBASE,    KMASK(1),     "k1", 2)
REGDEF(K2,     2+KBASE,    KMASK(2),     "k2", 2)
REGDEF(K3,     3+KBASE,    KMASK(3),     "k3", 2)
REGDEF(K4,     4+KBASE,    KMASK(4),     "k4", 2)
REGDEF(K5,     5+KBASE,    KMASK(5),     "k5", 2)
REGDEF(K6,     6+KBASE,    KMASK(6),     "k6", 2)
REGDEF(K7,     7+KBASE,    KMASK(7),     "k7", 2)

REGDEF(STK,    8+KBASE,    0x0000,       "STK", -1)

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
