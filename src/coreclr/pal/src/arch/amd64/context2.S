// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Implementation of _CONTEXT_CaptureContext for the Intel x86 platform.
// This function is processor dependent.  It is used by exception handling,
// and is always apply to the current thread.
//

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

#ifdef HOST_64BIT

#define IRETFRAME_Rip 0
#define IRETFRAME_SegCs IRETFRAME_Rip+8
#define IRETFRAME_EFlags IRETFRAME_SegCs+8
#define IRETFRAME_Rsp IRETFRAME_EFlags+8
#define IRETFRAME_SegSs IRETFRAME_Rsp+8
#define IRetFrameLength IRETFRAME_SegSs+8
#define IRetFrameLengthAligned 16*((IRetFrameLength+8)/16)

// Incoming:
//  RDI: Context*
//
LEAF_ENTRY CONTEXT_CaptureContext, _TEXT
    // Save processor flags before calling any of the following 'test' instructions
    // because they will modify state of some flags
    push_eflags
    END_PROLOGUE

    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_INTEGER
    je      LOCAL_LABEL(Done_CONTEXT_INTEGER)
    mov     [rdi + CONTEXT_Rdi], rdi
    mov     [rdi + CONTEXT_Rsi], rsi
    mov     [rdi + CONTEXT_Rbx], rbx
    mov     [rdi + CONTEXT_Rdx], rdx
    mov     [rdi + CONTEXT_Rcx], rcx
    mov     [rdi + CONTEXT_Rax], rax
    mov     [rdi + CONTEXT_Rbp], rbp
    mov     [rdi + CONTEXT_R8], r8
    mov     [rdi + CONTEXT_R9], r9
    mov     [rdi + CONTEXT_R10], r10
    mov     [rdi + CONTEXT_R11], r11
    mov     [rdi + CONTEXT_R12], r12
    mov     [rdi + CONTEXT_R13], r13
    mov     [rdi + CONTEXT_R14], r14
    mov     [rdi + CONTEXT_R15], r15
LOCAL_LABEL(Done_CONTEXT_INTEGER):

    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_CONTROL
    je      LOCAL_LABEL(Done_CONTEXT_CONTROL)

    // Return address is @ (RSP + 8)
    mov     rdx, [rsp + 8]
    mov     [rdi + CONTEXT_Rip], rdx
.att_syntax
    mov     %cs, CONTEXT_SegCs(%rdi)
.intel_syntax noprefix
    // Get the value of EFlags that was pushed on stack at the beginning of the function
    mov     rdx, [rsp]
    mov     [rdi + CONTEXT_EFlags], edx
    lea     rdx, [rsp + 16]
    mov     [rdi + CONTEXT_Rsp], rdx
.att_syntax
    mov     %ss, CONTEXT_SegSs(%rdi)
.intel_syntax noprefix
LOCAL_LABEL(Done_CONTEXT_CONTROL):

    // Need to double check this is producing the right result
    // also that FFSXR (fast save/restore) is not turned on
    // otherwise it omits the xmm registers.
    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_FLOATING_POINT
    je      LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT)
    fxsave  [rdi + CONTEXT_FltSave]
LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT):

    free_stack 8
    ret
LEAF_END CONTEXT_CaptureContext, _TEXT

LEAF_ENTRY RtlCaptureContext, _TEXT
    mov     DWORD PTR [rdi + CONTEXT_ContextFlags], (CONTEXT_AMD64 | CONTEXT_FULL | CONTEXT_SEGMENTS)
    jmp     C_FUNC(CONTEXT_CaptureContext)
LEAF_END RtlCaptureContext, _TEXT

LEAF_ENTRY RtlRestoreContext, _TEXT
    push_nonvol_reg rbp
    alloc_stack (IRetFrameLengthAligned)

#ifdef HAS_ASAN
    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_CONTROL
    je      LOCAL_LABEL(Restore_CONTEXT_FLOATING_POINT)

    push_nonvol_reg rdi
    push_nonvol_reg rsi
    call    EXTERNAL_C_FUNC(__asan_handle_no_return)
    pop_nonvol_reg rsi
    pop_nonvol_reg rdi
LOCAL_LABEL(Restore_CONTEXT_FLOATING_POINT):
#endif
    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_FLOATING_POINT
    je      LOCAL_LABEL(Done_Restore_CONTEXT_FLOATING_POINT)
    fxrstor [rdi + CONTEXT_FltSave]
LOCAL_LABEL(Done_Restore_CONTEXT_FLOATING_POINT):

    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_XSTATE
    je      LOCAL_LABEL(Done_Restore_CONTEXT_XSTATE)

    // Restore the extended state (for now, this is just the upper halves of YMM registers)
    vinsertf128 ymm0, ymm0, xmmword ptr [rdi + (CONTEXT_VectorRegister + 0 * 16)], 1
    vinsertf128 ymm1, ymm1, xmmword ptr [rdi + (CONTEXT_VectorRegister + 1 * 16)], 1
    vinsertf128 ymm2, ymm2, xmmword ptr [rdi + (CONTEXT_VectorRegister + 2 * 16)], 1
    vinsertf128 ymm3, ymm3, xmmword ptr [rdi + (CONTEXT_VectorRegister + 3 * 16)], 1
    vinsertf128 ymm4, ymm4, xmmword ptr [rdi + (CONTEXT_VectorRegister + 4 * 16)], 1
    vinsertf128 ymm5, ymm5, xmmword ptr [rdi + (CONTEXT_VectorRegister + 5 * 16)], 1
    vinsertf128 ymm6, ymm6, xmmword ptr [rdi + (CONTEXT_VectorRegister + 6 * 16)], 1
    vinsertf128 ymm7, ymm7, xmmword ptr [rdi + (CONTEXT_VectorRegister + 7 * 16)], 1
    vinsertf128 ymm8, ymm8, xmmword ptr [rdi + (CONTEXT_VectorRegister + 8 * 16)], 1
    vinsertf128 ymm9, ymm9, xmmword ptr [rdi + (CONTEXT_VectorRegister + 9 * 16)], 1
    vinsertf128 ymm10, ymm10, xmmword ptr [rdi + (CONTEXT_VectorRegister + 10 * 16)], 1
    vinsertf128 ymm11, ymm11, xmmword ptr [rdi + (CONTEXT_VectorRegister + 11 * 16)], 1
    vinsertf128 ymm12, ymm12, xmmword ptr [rdi + (CONTEXT_VectorRegister + 12 * 16)], 1
    vinsertf128 ymm13, ymm13, xmmword ptr [rdi + (CONTEXT_VectorRegister + 13 * 16)], 1
    vinsertf128 ymm14, ymm14, xmmword ptr [rdi + (CONTEXT_VectorRegister + 14 * 16)], 1
    vinsertf128 ymm15, ymm15, xmmword ptr [rdi + (CONTEXT_VectorRegister + 15 * 16)], 1
LOCAL_LABEL(Done_Restore_CONTEXT_XSTATE):

    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_CONTROL
    je      LOCAL_LABEL(Done_Restore_CONTEXT_CONTROL)

    // The control registers are restored via the iret instruction
    // so we build the frame for the iret on the stack.
#ifdef __APPLE__
.att_syntax
    // On OSX, we cannot read SS via the thread_get_context and RtlRestoreContext
    // needs to be used on context extracted by thread_get_context. So we
    // don't change the SS.
    mov     %ss, %ax
.intel_syntax noprefix
#else
    mov     ax, [rdi + CONTEXT_SegSs]
#endif
    mov     [rsp + IRETFRAME_SegSs], ax
    mov     rax, [rdi + CONTEXT_Rsp]
    mov     [rsp + IRETFRAME_Rsp], rax
    mov     eax, [rdi + CONTEXT_EFlags]
    mov     [rsp + IRETFRAME_EFlags], eax
    mov     ax, [rdi + CONTEXT_SegCs]
    mov     [rsp + IRETFRAME_SegCs], ax
    mov     rax, [rdi + CONTEXT_Rip]
    mov     [rsp + IRETFRAME_Rip], rax

LOCAL_LABEL(Done_Restore_CONTEXT_CONTROL):
    // Remember the result of the test for the CONTEXT_CONTROL
    push_eflags
    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_INTEGER
    je      LOCAL_LABEL(Done_Restore_CONTEXT_INTEGER)
    mov     rsi, [rdi + CONTEXT_Rsi]
    mov     rbx, [rdi + CONTEXT_Rbx]
    mov     rdx, [rdi + CONTEXT_Rdx]
    mov     rcx, [rdi + CONTEXT_Rcx]
    mov     rax, [rdi + CONTEXT_Rax]
    mov     rbp, [rdi + CONTEXT_Rbp]
    mov     r8, [rdi + CONTEXT_R8]
    mov     r9, [rdi + CONTEXT_R9]
    mov     r10, [rdi + CONTEXT_R10]
    mov     r11, [rdi + CONTEXT_R11]
    mov     r12, [rdi + CONTEXT_R12]
    mov     r13, [rdi + CONTEXT_R13]
    mov     r14, [rdi + CONTEXT_R14]
    mov     r15, [rdi + CONTEXT_R15]
    mov     rdi, [rdi + CONTEXT_Rdi]
LOCAL_LABEL(Done_Restore_CONTEXT_INTEGER):

    // Restore the result of the test for the CONTEXT_CONTROL
    pop_eflags
    je      LOCAL_LABEL(No_Restore_CONTEXT_CONTROL)
    // The function was asked to restore the control registers, so
    // we perform iretq that restores them all.
    // We don't return to the caller in this case.
    iretq
LOCAL_LABEL(No_Restore_CONTEXT_CONTROL):

    // The function was not asked to restore the control registers
    // so we return back to the caller.
    free_stack (IRetFrameLengthAligned)
    pop_nonvol_reg rbp
    ret
LEAF_END RtlRestoreContext, _TEXT

#else

    .globl C_FUNC(CONTEXT_CaptureContext)
C_FUNC(CONTEXT_CaptureContext):
    push %eax
    mov 8(%esp), %eax
    mov %edi, CONTEXT_Edi(%eax)
    mov %esi, CONTEXT_Esi(%eax)
    mov %ebx, CONTEXT_Ebx(%eax)
    mov %edx, CONTEXT_Edx(%eax)
    mov %ecx, CONTEXT_Ecx(%eax)
    pop %ecx
    mov %ecx, CONTEXT_Eax(%eax)
    mov %ebp, CONTEXT_Ebp(%eax)
    mov (%esp), %edx
    mov %edx, CONTEXT_Eip(%eax)
    push %cs
    pop %edx
    mov %edx, CONTEXT_SegCs(%eax)
    pushf
    pop %edx
    mov %edx, CONTEXT_EFlags(%eax)
    lea 4(%esp), %edx
    mov %edx, CONTEXT_Esp(%eax)
    push %ss
    pop %edx
    mov %edx, CONTEXT_SegSs(%eax)
    testb $CONTEXT_FLOATING_POINT, CONTEXT_ContextFlags(%eax)
    je 0f
    fnsave CONTEXT_FloatSave(%eax)
    frstor CONTEXT_FloatSave(%eax)
0:
    testb $CONTEXT_EXTENDED_REGISTERS, CONTEXT_ContextFlags(%eax)
    je 2f
    movdqu %xmm0, CONTEXT_Xmm0(%eax)
    movdqu %xmm1, CONTEXT_Xmm1(%eax)
    movdqu %xmm2, CONTEXT_Xmm2(%eax)
    movdqu %xmm3, CONTEXT_Xmm3(%eax)
    movdqu %xmm4, CONTEXT_Xmm4(%eax)
    movdqu %xmm5, CONTEXT_Xmm5(%eax)
    movdqu %xmm6, CONTEXT_Xmm6(%eax)
    movdqu %xmm7, CONTEXT_Xmm7(%eax)
2:
    ret

#endif
