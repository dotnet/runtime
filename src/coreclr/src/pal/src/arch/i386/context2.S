//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// Implementation of _CONTEXT_CaptureContext for the Intel x86 platform.
// This function is processor dependent.  It is used by exception handling,
// and is always apply to the current thread.
//

#include "macros.inc"

#ifdef BIT64

#define CONTEXT_CONTROL 1 // SegSs, Rsp, SegCs, Rip, and EFlags
#define CONTEXT_INTEGER 2 // Rax, Rcx, Rdx, Rbx, Rbp, Rsi, Rdi, R8-R15
#define CONTEXT_SEGMENTS 4 // SegDs, SegEs, SegFs, SegGs
#define CONTEXT_FLOATING_POINT 8
#define CONTEXT_DEBUG_REGISTERS 16 // Dr0-Dr3 and Dr6-Dr7

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

// Incoming:
//  RDI: Context*
//
    .globl C_FUNC(CONTEXT_CaptureContext)
C_FUNC(CONTEXT_CaptureContext):
    testb $CONTEXT_INTEGER, CONTEXT_ContextFlags(%rdi)
    je 0f
    mov %rdi, CONTEXT_Rdi(%rdi)
    mov %rsi, CONTEXT_Rsi(%rdi)
    mov %rbx, CONTEXT_Rbx(%rdi)
    mov %rdx, CONTEXT_Rdx(%rdi)
    mov %rcx, CONTEXT_Rcx(%rdi)
    mov %rax, CONTEXT_Rax(%rdi)
    mov %rbp, CONTEXT_Rbp(%rdi)
    mov %r8, CONTEXT_R8(%rdi)
    mov %r9, CONTEXT_R9(%rdi)
    mov %r10, CONTEXT_R10(%rdi)
    mov %r11, CONTEXT_R11(%rdi)
    mov %r12, CONTEXT_R12(%rdi)
    mov %r13, CONTEXT_R13(%rdi)
    mov %r14, CONTEXT_R14(%rdi)
    mov %r15, CONTEXT_R15(%rdi)
    jmp 1f
0:
    nop
1:
    testb $CONTEXT_CONTROL, CONTEXT_ContextFlags(%rdi)
    je 2f
    
    // Return address is @ RSP
    mov (%rsp), %rdx
    mov %rdx, CONTEXT_Rip(%rdi)
    mov %cs, CONTEXT_SegCs(%rdi)
    pushfq
    pop %rdx
    mov %edx, CONTEXT_EFlags(%rdi)
    lea 8(%rsp), %rdx
    mov %rdx, CONTEXT_Rsp(%rdi)
    mov %ss, CONTEXT_SegSs(%rdi)
2:
    // Need to double check this is producing the right result
    // also that FFSXR (fast save/restore) is not turned on
    // otherwise it omits the xmm registers.
    testb $CONTEXT_FLOATING_POINT, CONTEXT_ContextFlags(%rdi)
    je 3f
    fxsave CONTEXT_FltSave(%rdi)
3:
    testb $CONTEXT_DEBUG_REGISTERS, CONTEXT_ContextFlags(%rdi)
    je 4f
    mov %dr0, %rdx
    mov %rdx, CONTEXT_Dr0(%rdi)
    mov %dr1, %rdx
    mov %rdx, CONTEXT_Dr1(%rdi)
    mov %dr2, %rdx
    mov %rdx, CONTEXT_Dr2(%rdi)
    mov %dr3, %rdx
    mov %rdx, CONTEXT_Dr3(%rdi)
    mov %dr6, %rdx
    mov %rdx, CONTEXT_Dr6(%rdi)
    mov %dr7, %rdx
    mov %rdx, CONTEXT_Dr7(%rdi)
4:
    ret

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
