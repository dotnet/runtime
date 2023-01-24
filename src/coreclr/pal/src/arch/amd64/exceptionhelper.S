// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//////////////////////////////////////////////////////////////////////////
//
// This function creates a stack frame right below the target frame, restores all callee
// saved registers from the passed in context, sets the RSP to that frame and sets the
// return address to the target frame's RIP.
// Then it uses the ThrowExceptionHelper to throw the passed in exception from that context.
// EXTERN_C void ThrowExceptionFromContextInternal(CONTEXT* context, PAL_SEHException* ex);
LEAF_ENTRY ThrowExceptionFromContextInternal, _TEXT
#ifdef HAS_ASAN
        // Need to call __asan_handle_no_return explicitly here because we re-initialize RSP before
        // throwing exception in ThrowExceptionHelper
        push_nonvol_reg rdi
        push_nonvol_reg rsi
        call    EXTERNAL_C_FUNC(__asan_handle_no_return)
        pop_nonvol_reg rsi
        pop_nonvol_reg rdi
#endif

        // Save the RBP to the stack so that the unwind can work at the instruction after
        // loading the RBP from the context, but before loading the RSP from the context.
        push_nonvol_reg rbp
        mov     r12, [rdi + CONTEXT_R12]
        mov     r13, [rdi + CONTEXT_R13]
        mov     r14, [rdi + CONTEXT_R14]
        mov     r15, [rdi + CONTEXT_R15]
        mov     rbx, [rdi + CONTEXT_Rbx]
        mov     rbp, [rdi + CONTEXT_Rbp]
        mov     rsp, [rdi + CONTEXT_Rsp]
        // The RSP was set to the target frame's value, so the current function's
        // CFA is now right at the RSP.
        .cfi_def_cfa_offset 0

        // Indicate that now that we have moved the RSP to the target address,
        // the RBP is no longer saved in the current stack frame.
        .cfi_restore rbp

        mov     rax, [rdi + CONTEXT_Rip]

        // Store return address to the stack
        push_register rax
        // The PAL_SEHException pointer
        mov     rdi, rsi
        jmp     EXTERNAL_C_FUNC(ThrowExceptionHelper)
LEAF_END ThrowExceptionFromContextInternal, _TEXT
