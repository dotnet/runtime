// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

//////////////////////////////////////////////////////////////////////////
//
// This function creates a stack frame right below the target frame, restores all callee
// saved registers from the passed in context, sets R15 to that frame and sets the
// return address to the target frame's PSW address.
// Then it uses the ThrowExceptionHelper to throw the passed in exception from that context.
// EXTERN_C void ThrowExceptionFromContextInternal(CONTEXT* context, PAL_SEHException* ex);
LEAF_ENTRY ThrowExceptionFromContextInternal, _TEXT
        // Save callee-saved registers to the stack so that unwinding can work at
        // any intermediate step while loading up target registers.
        stmg    %r6, %r15, 48(%r15)
        .cfi_offset 6, -112
        .cfi_offset 7, -104
        .cfi_offset 8, -96
        .cfi_offset 9, -88
        .cfi_offset 10, -80
        .cfi_offset 11, -72
        .cfi_offset 12, -64
        .cfi_offset 13, -56
        .cfi_offset 14, -48
        .cfi_offset 15, -40

        lg      %r6, CONTEXT_R6(%r2)
        lg      %r7, CONTEXT_R7(%r2)
        lg      %r8, CONTEXT_R8(%r2)
        lg      %r9, CONTEXT_R9(%r2)
        lg      %r10, CONTEXT_R10(%r2)
        lg      %r11, CONTEXT_R11(%r2)
        lg      %r12, CONTEXT_R12(%r2)
        lg      %r13, CONTEXT_R13(%r2)
        lg      %r14, CONTEXT_PSWAddr(%r2)
        lg      %r15, CONTEXT_R15(%r2)
        // After changing %r15, unwinding no longer finds the registers
        // saved above.  Switch to the target frame at this point.
        .cfi_restore 6
        .cfi_restore 7
        .cfi_restore 8
        .cfi_restore 9
        .cfi_restore 10
        .cfi_restore 11
        .cfi_restore 12
        .cfi_restore 13
        .cfi_restore 14
        .cfi_restore 15

        // The PAL_SEHException pointer
        lgr    %r2, %r3
        jg     EXTERNAL_C_FUNC(ThrowExceptionHelper)
LEAF_END ThrowExceptionFromContextInternal, _TEXT
