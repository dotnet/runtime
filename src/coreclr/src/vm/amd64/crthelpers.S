// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

// JIT_MemSet/JIT_MemCpy
//
// It is IMPORTANT that the exception handling code is able to find these guys
// on the stack, but on non-windows platforms we can just defer to the platform
// implementation.
//

LEAF_ENTRY JIT_MemSet, _TEXT
        test rdx, rdx
        jz   Exit_MemSet

        cmp  byte ptr [rdi], 0

        jmp  C_PLTFUNC(memset)

Exit_MemSet:
        ret

LEAF_END_MARKED JIT_MemSet, _TEXT

LEAF_ENTRY JIT_MemCpy, _TEXT
        test rdx, rdx
        jz   Exit_MemCpy

        cmp  byte ptr [rdi], 0
        cmp  byte ptr [rsi], 0

        jmp  C_PLTFUNC(memcpy)

Exit_MemCpy:
        ret

LEAF_END_MARKED JIT_MemCpy, _TEXT
