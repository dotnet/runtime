// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "unixasmmacros.inc"
#include "asmconstants.h"

.syntax unified
.thumb

//
// void *memcpy(void *dst, const void *src, size_t length)
//
// Copy a block of memory in a forward direction.
//

    NESTED_ENTRY FCallMemcpy, _TEXT, NoHandler
        cmp r2, #0
        
        beq LOCAL_LABEL(GC_POLL)

        PROLOG_PUSH  "{r7, lr}"
        PROLOG_STACK_SAVE r7
        
        ldrb r3, [r0]
        ldrb r3, [r1]
        
        blx C_FUNC(memcpy)

        EPILOG_POP   "{r7, pc}"

LOCAL_LABEL(GC_POLL):
        ldr r0, =g_TrapReturningThreads
        ldr r0, [r0]
        cmp r0, #0
        it ne
        bne C_FUNC(FCallMemCpy_GCPoll)
        bx lr
    NESTED_END_MARKED FCallMemcpy, _TEXT
