// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define HASH_DEFINE #define
#define PLAT_ASM_OFFSET(offset, cls, member) HASH_DEFINE OFFSETOF__##cls##__##member 0x##offset
#define PLAT_ASM_SIZEOF(size,   cls        ) HASH_DEFINE SIZEOF__##cls 0x##size
#define PLAT_ASM_CONST(constant, expr)       HASH_DEFINE expr 0x##constant

#include <AsmOffsets.h>
