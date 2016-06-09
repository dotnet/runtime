// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "unixasmmacros.inc"
#include "asmconstants.h"

.syntax unified
.thumb

// EXTERN_C void ThrowExceptionFromContextInternal(CONTEXT* context, PAL_SEHException* ex);
LEAF_ENTRY ThrowExceptionFromContextInternal, _TEXT
    // Ported from src/pal/src/arch/i386/exceptionhelper.S
    push_nonvol_reg {r7} /* FP. x64-RBP */

    ldr	r4,	[r0, #(CONTEXT_R4)]
    ldr	r5,	[r0, #(CONTEXT_R5)]
    ldr	r6,	[r0, #(CONTEXT_R6)]
    ldr	r7,	[r0, #(CONTEXT_R7)]
    ldr	r8,	[r0, #(CONTEXT_R8)]
    ldr	r9,	[r0, #(CONTEXT_R9)]
    ldr	r10,	[r0, #(CONTEXT_R10)]
    ldr	r11,	[r0, #(CONTEXT_R11)]
    ldr	sp,	[r0, #(CONTEXT_Sp)]
    ldr	lr,	[r0, #(CONTEXT_Pc)]

    // The PAL_SEHException pointer
    mov	r0,	r1
    b	EXTERNAL_C_FUNC(ThrowExceptionHelper)
LEAF_END ThrowExceptionFromContextInternal, _TEXT
