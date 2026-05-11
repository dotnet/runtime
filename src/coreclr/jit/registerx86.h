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

#define XMMBASE 8
#define XMMMASK(x) ((int32_t)(1) << ((x)+XMMBASE))

REGDEF(XMM0,    0+XMMBASE,  XMMMASK(0),   "mm0"  )
REGDEF(XMM1,    1+XMMBASE,  XMMMASK(1),   "mm1"  )
REGDEF(XMM2,    2+XMMBASE,  XMMMASK(2),   "mm2"  )
REGDEF(XMM3,    3+XMMBASE,  XMMMASK(3),   "mm3"  )
REGDEF(XMM4,    4+XMMBASE,  XMMMASK(4),   "mm4"  )
REGDEF(XMM5,    5+XMMBASE,  XMMMASK(5),   "mm5"  )
REGDEF(XMM6,    6+XMMBASE,  XMMMASK(6),   "mm6"  )
REGDEF(XMM7,    7+XMMBASE,  XMMMASK(7),   "mm7"  )

#define KBASE 16
#define KMASK(x) ((int32_t)(1) << ((x)+KBASE))

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

/*****************************************************************************/
#undef  XMMMASK
#undef  KMASK
#undef  REGDEF
#undef  REGALIAS
/*****************************************************************************/

// clang-format on
