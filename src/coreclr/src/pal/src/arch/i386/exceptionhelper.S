// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//////////////////////////////////////////////////////////////////////////
//
// EXTERN_C void ThrowExceptionFromContextInternal(CONTEXT* context, PAL_SEHException* ex);
//
// This function creates a stack frame right below the target frame, restores all callee
// saved registers from the passed in context, sets the SP to that frame and sets the
// return address to the target frame's IP.
// Then it uses the ThrowExceptionHelper to throw the passed in exception from that context.
//
//////////////////////////////////////////////////////////////////////////

LEAF_ENTRY ThrowExceptionFromContextInternal, _TEXT
        push  ebp
        mov   eax, [esp + 12] // ebx: PAL_SEHException *
        mov   ebx, [esp + 8]  // eax: CONTEXT *

        mov   ebp, [ebx + CONTEXT_Ebp]
        mov   esp, [ebx + CONTEXT_Esp]

        // The ESP is re-initialized as the target frame's value, so the current function's
        // CFA is now right at the ESP.
        .cfi_def_cfa_offset 0

        // Indicate that now that we have moved the RSP to the target address,
        // the EBP is no longer saved in the current stack frame.
        .cfi_restore ebp

        // Store PAL_SEHException as the first argument
        push    eax

        // Store return address to the stack
        mov     ebx, [ebx + CONTEXT_Eip]
        push    ebx
        jmp     EXTERNAL_C_FUNC(ThrowExceptionHelper)
LEAF_END ThrowExceptionFromContextInternal, _TEXT
