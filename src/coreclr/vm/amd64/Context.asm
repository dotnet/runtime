; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;
; Some implementations of operations done on the CONTEXT structure, like capturing and restoring the thread's context.
;

include AsmMacros.inc
include AsmConstants.inc

; The arch bit is normally set in the flag constants below. Since this is already arch-specific code and the arch bit is not
; relevant, the arch bit is excluded from the flag constants below for simpler tests.
CONTEXT_AMD64 equ 100000h

; Some constants for CONTEXT.ContextFlags
CONTEXT_CONTROL equ 1h
CONTEXT_INTEGER equ 2h
CONTEXT_FLOATING_POINT equ 8h
CONTEXT_XSTATE equ 40h

; Signature: EXTERN_C void STDCALL ClrRestoreContext(PCONTEXT ContextRecord);
NESTED_ENTRY ClrRestoreContext, _TEXT
        push_nonvol_reg rbp
        set_frame rbp, 0
        END_PROLOGUE
    
        test    byte ptr [rcx + OFFSETOF__CONTEXT__ContextFlags], CONTEXT_FLOATING_POINT
        je      Done_Restore_CONTEXT_FLOATING_POINT
        fxrstor [rcx + OFFSETOF__CONTEXT__FltSave]
    Done_Restore_CONTEXT_FLOATING_POINT:
    
        test    byte ptr [rcx + OFFSETOF__CONTEXT__ContextFlags], CONTEXT_XSTATE
        je      Done_Restore_CONTEXT_XSTATE
    
        ; Restore the extended state (for now, this is just the upper halves of YMM registers)
        vinsertf128 ymm0, ymm0, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 0 * 16)], 1
        vinsertf128 ymm1, ymm1, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 1 * 16)], 1
        vinsertf128 ymm2, ymm2, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 2 * 16)], 1
        vinsertf128 ymm3, ymm3, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 3 * 16)], 1
        vinsertf128 ymm4, ymm4, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 4 * 16)], 1
        vinsertf128 ymm5, ymm5, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 5 * 16)], 1
        vinsertf128 ymm6, ymm6, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 6 * 16)], 1
        vinsertf128 ymm7, ymm7, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 7 * 16)], 1
        vinsertf128 ymm8, ymm8, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 8 * 16)], 1
        vinsertf128 ymm9, ymm9, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 9 * 16)], 1
        vinsertf128 ymm10, ymm10, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 10 * 16)], 1
        vinsertf128 ymm11, ymm11, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 11 * 16)], 1
        vinsertf128 ymm12, ymm12, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 12 * 16)], 1
        vinsertf128 ymm13, ymm13, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 13 * 16)], 1
        vinsertf128 ymm14, ymm14, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 14 * 16)], 1
        vinsertf128 ymm15, ymm15, xmmword ptr [rcx + (OFFSETOF__CONTEXT__VectorRegister + 15 * 16)], 1
    Done_Restore_CONTEXT_XSTATE:
    
        test    byte ptr [rcx + OFFSETOF__CONTEXT__ContextFlags], CONTEXT_INTEGER
        je      Done_Restore_CONTEXT_INTEGER_0
    
        ; Restore the integer registers except for rax and rcx, which may still be needed and will be restored later
        mov     rdx, [rcx + OFFSETOF__CONTEXT__Rdx]
        mov     rbx, [rcx + OFFSETOF__CONTEXT__Rbx]
        mov     rbp, [rcx + OFFSETOF__CONTEXT__Rbp]
        mov     rsi, [rcx + OFFSETOF__CONTEXT__Rsi]
        mov     rdi, [rcx + OFFSETOF__CONTEXT__Rdi]
        mov     r8, [rcx + OFFSETOF__CONTEXT__R8]
        mov     r9, [rcx + OFFSETOF__CONTEXT__R9]
        mov     r10, [rcx + OFFSETOF__CONTEXT__R10]
        mov     r11, [rcx + OFFSETOF__CONTEXT__R11]
        mov     r12, [rcx + OFFSETOF__CONTEXT__R12]
        mov     r13, [rcx + OFFSETOF__CONTEXT__R13]
        mov     r14, [rcx + OFFSETOF__CONTEXT__R14]
        mov     r15, [rcx + OFFSETOF__CONTEXT__R15]
    Done_Restore_CONTEXT_INTEGER_0:
    
        test    byte ptr [rcx + OFFSETOF__CONTEXT__ContextFlags], CONTEXT_CONTROL
        je      Done_Restore_CONTEXT_CONTROL
    
        ; When user-mode shadow stacks are enabled, and for example the intent is to continue execution in managed code after
        ; exception handling, iret and ret can't be used because their shadow stack enforcement would not allow that transition,
        ; and using them would require writing to the shadow stack, which is not preferable. Instead, iret is partially
        ; simulated. SS and CS shouldn't be different from their current values in cases where this function is used.
        mov     rsp, [rcx + OFFSETOF__CONTEXT__Rsp]     ; restore rsp
        mov     eax, [rcx + OFFSETOF__CONTEXT__EFlags]  ; push RFlags onto the stack
        push    rax
        mov     rax, [rcx + OFFSETOF__CONTEXT__Rip]     ; store the target IP on the stack below RFlags
        mov     [rsp - 8h], rax
        test    byte ptr [rcx + OFFSETOF__CONTEXT__ContextFlags], CONTEXT_INTEGER
        je      Done_Restore_CONTEXT_INTEGER_1
        mov     rax, [rcx + OFFSETOF__CONTEXT__Rax]     ; restore the remaining integer registers
        mov     rcx, [rcx + OFFSETOF__CONTEXT__Rcx]
    Done_Restore_CONTEXT_INTEGER_1:
        popfq                                           ; pop RFlags, which also restores rsp, and resume at the target IP
        jmp     qword ptr [rsp - 10h]
    Done_Restore_CONTEXT_CONTROL:
    
        ; The function was not asked to restore the control registers, so complete the restore and return to the caller
    
        test    byte ptr [rcx + OFFSETOF__CONTEXT__ContextFlags], CONTEXT_INTEGER
        je      Done_Restore_CONTEXT_INTEGER_2
        mov     rax, [rcx + OFFSETOF__CONTEXT__Rax]
        mov     rcx, [rcx + OFFSETOF__CONTEXT__Rcx]
    Done_Restore_CONTEXT_INTEGER_2:
    
        pop     rbp
        ret
NESTED_END ClrRestoreContext, _TEXT

end
