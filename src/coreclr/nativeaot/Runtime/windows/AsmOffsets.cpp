// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if defined(HOST_ARM) || defined(HOST_ARM64)

#define HASH_DEFINE #define
#define PLAT_ASM_OFFSET(offset, cls, member) HASH_DEFINE OFFSETOF__##cls##__##member   0x##offset
#define PLAT_ASM_SIZEOF(size,   cls        ) HASH_DEFINE SIZEOF__##cls   0x##size
#define PLAT_ASM_CONST(constant, expr)       HASH_DEFINE expr   0x##constant

#else

#define PLAT_ASM_OFFSET(offset, cls, member) OFFSETOF__##cls##__##member  equ  0##offset##h
#define PLAT_ASM_SIZEOF(size,   cls        ) SIZEOF__##cls  equ  0##size##h
#define PLAT_ASM_CONST(constant, expr)       expr  equ  0##constant##h

#endif

#include "AsmOffsets.h"
