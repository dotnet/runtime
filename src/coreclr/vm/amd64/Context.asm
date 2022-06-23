; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;
; Some implementations of operations done on the CONTEXT structure, like capturing and restoring the thread's context.
;

include AsmMacros.inc
include AsmConstants.inc

; Some constants for CONTEXT.ContextFlags. The arch bit (CONTEXT_AMD64) is normally set in these flag constants below. Since
; this is already arch-specific code and the arch bit is not relevant, the arch bit is excluded from the flag constants below
; for simpler tests.
CONTEXT_CONTROL equ 1h
CONTEXT_INTEGER equ 2h
CONTEXT_FLOATING_POINT equ 8h

; Signature: EXTERN_C void STDCALL ClrRestoreNonvolatileContext(PCONTEXT ContextRecord);
NESTED_ENTRY ClrRestoreNonvolatileContext, _TEXT
        push_nonvol_reg rbp
        set_frame rbp, 0
        END_PROLOGUE
    
        test    byte ptr [rcx + OFFSETOF__CONTEXT__ContextFlags], CONTEXT_FLOATING_POINT
        je      Done_Restore_CONTEXT_FLOATING_POINT
        fxrstor [rcx + OFFSETOF__CONTEXT__FltSave]
    Done_Restore_CONTEXT_FLOATING_POINT:
    
        test    byte ptr [rcx + OFFSETOF__CONTEXT__ContextFlags], CONTEXT_INTEGER
        je      Done_Restore_CONTEXT_INTEGER
        mov     rbx, [rcx + OFFSETOF__CONTEXT__Rbx]
        mov     rbp, [rcx + OFFSETOF__CONTEXT__Rbp]
        mov     rsi, [rcx + OFFSETOF__CONTEXT__Rsi]
        mov     rdi, [rcx + OFFSETOF__CONTEXT__Rdi]
        mov     r12, [rcx + OFFSETOF__CONTEXT__R12]
        mov     r13, [rcx + OFFSETOF__CONTEXT__R13]
        mov     r14, [rcx + OFFSETOF__CONTEXT__R14]
        mov     r15, [rcx + OFFSETOF__CONTEXT__R15]
    Done_Restore_CONTEXT_INTEGER:
    
        test    byte ptr [rcx + OFFSETOF__CONTEXT__ContextFlags], CONTEXT_CONTROL
        je      Done_Restore_CONTEXT_CONTROL
    
        ; When user-mode shadow stacks are enabled, and for example the intent is to continue execution in managed code after
        ; exception handling, iret and ret can't be used because their shadow stack enforcement would not allow that transition,
        ; and using them would require writing to the shadow stack, which is not preferable. Instead, iret is partially
        ; simulated.
        mov     eax, [rcx + OFFSETOF__CONTEXT__EFlags]
        push    rax
        popfq
        mov     rsp, [rcx + OFFSETOF__CONTEXT__Rsp]
        jmp     qword ptr [rcx + OFFSETOF__CONTEXT__Rip]
    Done_Restore_CONTEXT_CONTROL:
    
        ; The function was not asked to restore the control registers so we return back to the caller
        pop     rbp
        ret
NESTED_END ClrRestoreNonvolatileContext, _TEXT

end
