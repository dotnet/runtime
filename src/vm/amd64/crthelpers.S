//
// Copyright (c) Microsoft. All rights reserved.
// Copyright (c) Geoff Norton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

// JIT_MemSet/JIT_MemCpy
//
// It is IMPORANT that the exception handling code is able to find these guys
// on the stack, but on non-windows platforms we can just defer to the platform
// implementation.
// 

LEAF_ENTRY JIT_MemSet, _TEXT
        test rdx, rdx
        jz   Exit_MemSet

        cmp  byte ptr [rdi], 0

        jmp  C_FUNC(memset)

Exit_MemSet:
        ret

LEAF_END JIT_MemSet, _TEXT

LEAF_ENTRY JIT_MemCpy, _TEXT
        test rdx, rdx
        jz   Exit_MemCpy

        cmp  byte ptr [rdi], 0
        cmp  byte ptr [rsi], 0

        jmp  C_FUNC(memcpy)

Exit_MemCpy:
        ret

LEAF_END JIT_MemCpy, _TEXT
