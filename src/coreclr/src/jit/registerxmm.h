// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// clang-format off
/*****************************************************************************/
/*****************************************************************************/
#ifndef REGDEF
#error  Must define REGDEF macro before including this file
#endif

#ifndef LEGACY_BACKEND
#error This file is only used for the LEGACY_BACKEND build.
#endif

#if defined(_TARGET_XARCH_)

#define XMMMASK(x) (unsigned(1) << (x-1))

/*
REGDEF(name, rnum,         mask,  sname) */
REGDEF(XMM0,    0,   XMMMASK(1),  "xmm0"  )
REGDEF(XMM1,    1,   XMMMASK(2),  "xmm1"  )
REGDEF(XMM2,    2,   XMMMASK(3),  "xmm2"  )
REGDEF(XMM3,    3,   XMMMASK(4),  "xmm3"  )
REGDEF(XMM4,    4,   XMMMASK(5),  "xmm4"  )
REGDEF(XMM5,    5,   XMMMASK(6),  "xmm5"  )
REGDEF(XMM6,    6,   XMMMASK(7),  "xmm6"  )
REGDEF(XMM7,    7,   XMMMASK(8),  "xmm7"  )

#ifdef _TARGET_AMD64_
REGDEF(XMM8,    8,   XMMMASK(9),  "xmm8"  )
REGDEF(XMM9,    9,   XMMMASK(10), "xmm9"  )
REGDEF(XMM10,   10,  XMMMASK(11), "xmm10" )
REGDEF(XMM11,   11,  XMMMASK(12), "xmm11" )
REGDEF(XMM12,   12,  XMMMASK(13), "xmm12" )
REGDEF(XMM13,   13,  XMMMASK(14), "xmm13" )
REGDEF(XMM14,   14,  XMMMASK(15), "xmm14" )
REGDEF(XMM15,   15,  XMMMASK(16), "xmm15" )
#endif

#endif // _TARGET_*

/*****************************************************************************/
#undef  REGDEF
/*****************************************************************************/

// clang-format on
