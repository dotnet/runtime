//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// Implementation of _CONTEXT_CaptureContext for the Intel x86 platform.
// This function is processor dependent.  It is used by exception handling,
// and is always apply to the current thread.
//

.intel_syntax noprefix
#include "unixasmmacros.inc"

#ifdef BIT64

#define CONTEXT_AMD64   0x100000

#define CONTEXT_CONTROL 1 // SegSs, Rsp, SegCs, Rip, and EFlags
#define CONTEXT_INTEGER 2 // Rax, Rcx, Rdx, Rbx, Rbp, Rsi, Rdi, R8-R15
#define CONTEXT_SEGMENTS 4 // SegDs, SegEs, SegFs, SegGs
#define CONTEXT_FLOATING_POINT 8
#define CONTEXT_DEBUG_REGISTERS 16 // Dr0-Dr3 and Dr6-Dr7

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_ContextFlags 6*8
#define CONTEXT_SegCs CONTEXT_ContextFlags+8
#define CONTEXT_SegDs CONTEXT_SegCs+2
#define CONTEXT_SegEs CONTEXT_SegDs+2
#define CONTEXT_SegFs CONTEXT_SegEs+2
#define CONTEXT_SegGs CONTEXT_SegFs+2
#define CONTEXT_SegSs CONTEXT_SegGs+2
#define CONTEXT_EFlags CONTEXT_SegSs+2
#define CONTEXT_Dr0 CONTEXT_EFlags+4
#define CONTEXT_Dr1 CONTEXT_Dr0+8
#define CONTEXT_Dr2 CONTEXT_Dr1+8
#define CONTEXT_Dr3 CONTEXT_Dr2+8
#define CONTEXT_Dr6 CONTEXT_Dr3+8
#define CONTEXT_Dr7 CONTEXT_Dr6+8
#define CONTEXT_Rax CONTEXT_Dr7+8
#define CONTEXT_Rcx CONTEXT_Rax+8
#define CONTEXT_Rdx CONTEXT_Rcx+8
#define CONTEXT_Rbx CONTEXT_Rdx+8
#define CONTEXT_Rsp CONTEXT_Rbx+8
#define CONTEXT_Rbp CONTEXT_Rsp+8
#define CONTEXT_Rsi CONTEXT_Rbp+8
#define CONTEXT_Rdi CONTEXT_Rsi+8
#define CONTEXT_R8 CONTEXT_Rdi+8
#define CONTEXT_R9 CONTEXT_R8+8
#define CONTEXT_R10 CONTEXT_R9+8
#define CONTEXT_R11 CONTEXT_R10+8
#define CONTEXT_R12 CONTEXT_R11+8
#define CONTEXT_R13 CONTEXT_R12+8
#define CONTEXT_R14 CONTEXT_R13+8
#define CONTEXT_R15 CONTEXT_R14+8
#define CONTEXT_Rip CONTEXT_R15+8
#define CONTEXT_FltSave CONTEXT_Rip+8
#define FLOATING_SAVE_AREA_SIZE 4*8+24*16+96
#define CONTEXT_Xmm0 CONTEXT_FltSave+10*16
#define CONTEXT_Xmm1 CONTEXT_Xmm0+16
#define CONTEXT_Xmm2 CONTEXT_Xmm1+16
#define CONTEXT_Xmm3 CONTEXT_Xmm2+16
#define CONTEXT_Xmm4 CONTEXT_Xmm3+16
#define CONTEXT_Xmm5 CONTEXT_Xmm4+16
#define CONTEXT_Xmm6 CONTEXT_Xmm5+16
#define CONTEXT_Xmm7 CONTEXT_Xmm6+16
#define CONTEXT_Xmm8 CONTEXT_Xmm7+16
#define CONTEXT_Xmm9 CONTEXT_Xmm8+16
#define CONTEXT_Xmm10 CONTEXT_Xmm9+16
#define CONTEXT_Xmm11 CONTEXT_Xmm10+16
#define CONTEXT_Xmm12 CONTEXT_Xmm11+16
#define CONTEXT_Xmm13 CONTEXT_Xmm12+16
#define CONTEXT_Xmm14 CONTEXT_Xmm13+16
#define CONTEXT_Xmm15 CONTEXT_Xmm14+16
#define CONTEXT_VectorRegister CONTEXT_Xmm15+16
#define CONTEXT_VectorControl CONTEXT_VectorRegister+16*26
#define CONTEXT_DebugControl CONTEXT_VectorControl+8
#define CONTEXT_LastBranchToRip CONTEXT_DebugControl+8
#define CONTEXT_LastBranchFromRip CONTEXT_LastBranchToRip+8
#define CONTEXT_LastExceptionToRip CONTEXT_LastBranchFromRip+8
#define CONTEXT_LastExceptionFromRip CONTEXT_LastExceptionToRip+8

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

    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_DEBUG_REGISTERS
    je      LOCAL_LABEL(Done_CONTEXT_DEBUG_REGISTERS)
    mov     rdx, dr0
    mov     [rdi + CONTEXT_Dr0], rdx
    mov     rdx, dr1
    mov     [rdi + CONTEXT_Dr1], rdx
    mov     rdx, dr2
    mov     [rdi + CONTEXT_Dr2], rdx
    mov     rdx, dr3
    mov     [rdi + CONTEXT_Dr3], rdx
    mov     rdx, dr6
    mov     [rdi + CONTEXT_Dr6], rdx
    mov     rdx, dr7
    mov     [rdi + CONTEXT_Dr7], rdx
LOCAL_LABEL(Done_CONTEXT_DEBUG_REGISTERS):

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
    
    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_DEBUG_REGISTERS
    je      LOCAL_LABEL(Done_Restore_CONTEXT_DEBUG_REGISTERS)
    mov     rdx, [rdi + CONTEXT_Dr0]
    mov     dr0, rdx
    mov     rdx, [rdi + CONTEXT_Dr1]
    mov     dr1, rdx
    mov     rdx, [rdi + CONTEXT_Dr2]
    mov     dr2, rdx
    mov     rdx, [rdi + CONTEXT_Dr3]
    mov     dr3, rdx
    mov     rdx, [rdi + CONTEXT_Dr6]
    mov     dr6, rdx
    mov     rdx, [rdi + CONTEXT_Dr7]
    mov     dr7, rdx
LOCAL_LABEL(Done_Restore_CONTEXT_DEBUG_REGISTERS):

    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_FLOATING_POINT
    je      LOCAL_LABEL(Done_Restore_CONTEXT_FLOATING_POINT)
    fxrstor [rdi + CONTEXT_FltSave]
LOCAL_LABEL(Done_Restore_CONTEXT_FLOATING_POINT):

    test    BYTE PTR [rdi + CONTEXT_ContextFlags], CONTEXT_CONTROL
    je      LOCAL_LABEL(Done_Restore_CONTEXT_CONTROL)

    // The control registers are restored via the iret instruction
    // so we build the frame for the iret on the stack.
    mov     ax, [rdi + CONTEXT_SegSs]
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

#define CONTEXT_ContextFlags 0
#define CONTEXT_FLOATING_POINT 8
#define CONTEXT_FloatSave 7*4
#define FLOATING_SAVE_AREA_SIZE 8*4+80
#define CONTEXT_Edi CONTEXT_FloatSave + FLOATING_SAVE_AREA_SIZE + 4*4
#define CONTEXT_Esi CONTEXT_Edi+4
#define CONTEXT_Ebx CONTEXT_Esi+4
#define CONTEXT_Edx CONTEXT_Ebx+4
#define CONTEXT_Ecx CONTEXT_Edx+4
#define CONTEXT_Eax CONTEXT_Ecx+4
#define CONTEXT_Ebp CONTEXT_Eax+4
#define CONTEXT_Eip CONTEXT_Ebp+4
#define CONTEXT_SegCs CONTEXT_Eip+4
#define CONTEXT_EFlags CONTEXT_SegCs+4
#define CONTEXT_Esp CONTEXT_EFlags+4
#define CONTEXT_SegSs CONTEXT_Esp+4
#define CONTEXT_EXTENDED_REGISTERS 32
#define CONTEXT_ExtendedRegisters CONTEXT_SegSs+4
#define CONTEXT_Xmm0 CONTEXT_ExtendedRegisters+160
#define CONTEXT_Xmm1 CONTEXT_Xmm0+16
#define CONTEXT_Xmm2 CONTEXT_Xmm1+16
#define CONTEXT_Xmm3 CONTEXT_Xmm2+16
#define CONTEXT_Xmm4 CONTEXT_Xmm3+16
#define CONTEXT_Xmm5 CONTEXT_Xmm4+16
#define CONTEXT_Xmm6 CONTEXT_Xmm5+16
#define CONTEXT_Xmm7 CONTEXT_Xmm6+16

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
