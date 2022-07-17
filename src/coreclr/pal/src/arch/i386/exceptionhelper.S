// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#ifdef HAS_ASAN
        // Need to call __asan_handle_no_return explicitly here because we re-initialize ESP before
        // throwing exception in ThrowExceptionHelper
        call  EXTERNAL_C_FUNC(__asan_handle_no_return)
#endif

        push  ebp
        mov   ecx, [esp + 12] // ecx: PAL_SEHException * (first argument for ThrowExceptionHelper)
        mov   eax, [esp + 8]  // ebx: CONTEXT *

        mov   ebp, [eax + CONTEXT_Ebp]
        mov   esp, [eax + CONTEXT_Esp]
        mov   ebx, [eax + CONTEXT_Ebx]
        mov   esi, [eax + CONTEXT_Esi]
        mov   edi, [eax + CONTEXT_Edi]

        // The ESP is re-initialized as the target frame's value, so the current function's
        // CFA is now right at the ESP.
        .cfi_def_cfa_offset 0

        // Indicate that now that we have moved the RSP to the target address,
        // the EBP is no longer saved in the current stack frame.
        .cfi_restore ebp

        // Store return address to the stack
        mov     eax, [eax + CONTEXT_Eip]
        push    eax
        jmp     EXTERNAL_C_FUNC(ThrowExceptionHelper)

LEAF_END ThrowExceptionFromContextInternal, _TEXT
