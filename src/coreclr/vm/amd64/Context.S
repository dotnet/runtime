// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

// Some constants for CONTEXT.ContextFlags. The arch bit (CONTEXT_AMD64) is normally set in these flag constants below. Since
// this is already arch-specific code and the arch bit is not relevant, the arch bit is excluded from the flag constants below
// for simpler tests.
#define CONTEXT_CONTROL 1
#define CONTEXT_INTEGER 2

// Signature: EXTERN_C void STDCALL ClrRestoreNonvolatileContextWorker(PCONTEXT ContextRecord, DWORD64 ssp);
NESTED_ENTRY ClrRestoreNonvolatileContextWorker, _TEXT, NoHandler
        push_nonvol_reg rbp
        set_cfa_register rbp, 0
        END_PROLOGUE
    
        mov     r10, rdi
        mov     r11, rsi

        test    byte ptr [r10 + OFFSETOF__CONTEXT__ContextFlags], CONTEXT_INTEGER
        je      Done_Restore_CONTEXT_INTEGER
        mov     rbx, [r10 + OFFSETOF__CONTEXT__Rbx]
        mov     rcx, [r10 + OFFSETOF__CONTEXT__Rcx]
        mov     rdx, [r10 + OFFSETOF__CONTEXT__Rdx]
        mov     r8, [r10 + OFFSETOF__CONTEXT__R8]
        mov     r9, [r10 + OFFSETOF__CONTEXT__R9]
        mov     rbp, [r10 + OFFSETOF__CONTEXT__Rbp]
        mov     rsi, [r10 + OFFSETOF__CONTEXT__Rsi]
        mov     rdi, [r10 + OFFSETOF__CONTEXT__Rdi]
        mov     r12, [r10 + OFFSETOF__CONTEXT__R12]
        mov     r13, [r10 + OFFSETOF__CONTEXT__R13]
        mov     r14, [r10 + OFFSETOF__CONTEXT__R14]
        mov     r15, [r10 + OFFSETOF__CONTEXT__R15]
    Done_Restore_CONTEXT_INTEGER:
    
        test    byte ptr [r10 + OFFSETOF__CONTEXT__ContextFlags], CONTEXT_CONTROL
        je      Done_Restore_CONTEXT_CONTROL

        test    r11, r11
        je      No_Ssp_Update
        rdsspq  rax
        sub     r11, rax
        shr     r11, 3
        incsspq r11
    No_Ssp_Update:

        // When user-mode shadow stacks are enabled, and for example the intent is to continue execution in managed code after
        // exception handling, iret and ret can't be used because their shadow stack enforcement would not allow that transition,
        // and using them would require writing to the shadow stack, which is not preferable. Instead, iret is partially
        // simulated.
        mov     rax, [r10 + OFFSETOF__CONTEXT__Rip]
        mov     rsp, [r10 + OFFSETOF__CONTEXT__Rsp]
        jmp     rax
    Done_Restore_CONTEXT_CONTROL:
    
        // The function was not asked to restore the control registers so we return back to the caller
        pop     rbp
        ret
NESTED_END ClrRestoreNonvolatileContextWorker, _TEXT
