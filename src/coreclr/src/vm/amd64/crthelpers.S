// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

// JIT_MemSet/JIT_MemCpy
//
// It is IMPORTANT that the exception handling code is able to find these guys
// on the stack, but on non-windows platforms we can just defer to the platform
// implementation.
//

// void JIT_MemSet(void* dest, int c, size_t count)
//
// Purpose:
//    Sets the first "count" bytes of the block of memory pointed byte
//    "dest" to the specified value (interpreted as an unsigned char).
//
// Entry:
//    RDI: void* dest    - Pointer to the block of memory to fill.
//    RSI: int c         - Value to be set.
//    RDX: size_t count  - Number of bytes to be set to the value.
//
// Exit:
//
// Uses:
//
// Exceptions:
//
LEAF_ENTRY JIT_MemSet, _TEXT
        test    rdx, rdx                // check if count is zero
        jz      Exit_MemSet             // if zero, no bytes to set

        cmp     byte ptr [rdi], 0       // check dest for null

        jmp     C_PLTFUNC(memset)       // forward to the CRT implementation

Exit_MemSet:
        ret

LEAF_END_MARKED JIT_MemSet, _TEXT

// void JIT_MemCpy(void* dest, const void* src, size_t count)
//
// Purpose:
//    Copies the values of "count" bytes from the location pointed to
//    by "src" to the memory block pointed by "dest".
//
// Entry:
//    RDI: void* dest             - Pointer to the destination array where content is to be copied.
//    RSI: const void* src        - Pointer to the source of the data to be copied.
//    RDX:  size_t count           - Number of bytes to copy.
//
// Exit:
//
// Uses:
//
// Exceptions:
//
LEAF_ENTRY JIT_MemCpy, _TEXT
        test    rdx, rdx                // check if count is zero
        jz      Exit_MemCpy             // if zero, no bytes to set

        cmp     byte ptr [rdi], 0       // check dest for null
        cmp     byte ptr [rsi], 0       // check src for null

        jmp     C_PLTFUNC(memcpy)      // forward to the CRT implementation

Exit_MemCpy:
        ret

LEAF_END_MARKED JIT_MemCpy, _TEXT
