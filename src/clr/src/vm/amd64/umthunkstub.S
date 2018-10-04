// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// METHODDESC_REGISTER: UMEntryThunk*
//
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix
    PUSH_ARGUMENT_REGISTERS
    // +8 for alignment
    alloc_stack (SIZEOF_MAX_FP_ARG_SPILL + 8)
    SAVE_FLOAT_ARGUMENT_REGISTERS 0
    END_PROLOGUE

    mov rdi, METHODDESC_REGISTER 
    call C_FUNC(TheUMEntryPrestubWorker)
    
    // we're going to tail call to the exec stub that we just setup 

    RESTORE_FLOAT_ARGUMENT_REGISTERS 0
    free_stack (SIZEOF_MAX_FP_ARG_SPILL + 8)
    POP_ARGUMENT_REGISTERS
    TAILJMP_RAX

NESTED_END TheUMEntryPrestub, _TEXT 

//
// METHODDESC_REGISTER: UMEntryThunk*
//
NESTED_ENTRY UMThunkStub, _TEXT, UnhandledExceptionHandlerUnix
#define UMThunkStubAMD64_FIXED_STACK_ALLOC_SIZE (SIZEOF_MAX_INT_ARG_SPILL + SIZEOF_MAX_FP_ARG_SPILL + 0x8)
#define UMThunkStubAMD64_XMM_SAVE_OFFSET 0x0
#define UMThunkStubAMD64_INT_ARG_OFFSET (SIZEOF_MAX_FP_ARG_SPILL + 0x8)
#define UMThunkStubAMD64_METHODDESC_OFFSET SIZEOF_MAX_FP_ARG_SPILL
#define UMThunkStubAMD64_RBP_OFFSET (UMThunkStubAMD64_FIXED_STACK_ALLOC_SIZE + 8)

// {optional stack args passed to callee}   <-- new RSP
// xmm0                                     <-- RBP
// xmm1
// xmm2
// xmm3
// xmm4
// xmm5
// xmm6
// xmm7
// METHODDESC_REGISTER
// rdi
// rsi
// rcx
// rdx
// r8
// r9
// r12
// rbp
// return address                           <-- entry RSP
        push_nonvol_reg rbp
        mov             rbp, rsp
        push_nonvol_reg r12                                                                     // stack_args
        alloc_stack     UMThunkStubAMD64_FIXED_STACK_ALLOC_SIZE
        save_reg_postrsp rdi, (UMThunkStubAMD64_INT_ARG_OFFSET)
        save_reg_postrsp rsi, (UMThunkStubAMD64_INT_ARG_OFFSET + 0x08)
        save_reg_postrsp rdx, (UMThunkStubAMD64_INT_ARG_OFFSET + 0x10)
        save_reg_postrsp rcx, (UMThunkStubAMD64_INT_ARG_OFFSET + 0x18)
        save_reg_postrsp r8,  (UMThunkStubAMD64_INT_ARG_OFFSET + 0x20)
        save_reg_postrsp r9,  (UMThunkStubAMD64_INT_ARG_OFFSET + 0x28)
        save_reg_postrsp METHODDESC_REGISTER, UMThunkStubAMD64_METHODDESC_OFFSET
        SAVE_FLOAT_ARGUMENT_REGISTERS UMThunkStubAMD64_XMM_SAVE_OFFSET
        set_cfa_register rbp, (2*8)
        END_PROLOGUE

        //
        // Call GetThread()
        //
        call            C_FUNC(GetThread)
        test            rax, rax
        jz              LOCAL_LABEL(DoThreadSetup)

LOCAL_LABEL(HaveThread):

        mov             r12, rax                // r12 <- Thread*

        //FailFast if a native callable method is invoked via ldftn and calli.
        cmp             dword ptr [r12 + OFFSETOF__Thread__m_fPreemptiveGCDisabled], 1
        jz              LOCAL_LABEL(InvalidTransition)

        //
        // disable preemptive GC
        //
        mov             dword ptr [r12 + OFFSETOF__Thread__m_fPreemptiveGCDisabled], 1

        //
        // catch returning thread here if a GC is in progress
        //
        PREPARE_EXTERNAL_VAR g_TrapReturningThreads, rax
        cmp                  dword ptr [rax], 0
        jnz                  LOCAL_LABEL(DoTrapReturningThreadsTHROW)

LOCAL_LABEL(InCooperativeMode):

        mov             METHODDESC_REGISTER, [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_METHODDESC_OFFSET]

#if _DEBUG
        mov             rax, [r12 + OFFSETOF__Thread__m_pDomain]
        mov             eax, [rax + OFFSETOF__AppDomain__m_dwId]

        mov             r11d, [METHODDESC_REGISTER + OFFSETOF__UMEntryThunk__m_dwDomainId]

        cmp             rax, r11
        jne             LOCAL_LABEL(WrongAppDomain)
#endif

        mov             r11, [METHODDESC_REGISTER + OFFSETOF__UMEntryThunk__m_pUMThunkMarshInfo]
        mov             eax, [r11 + OFFSETOF__UMThunkMarshInfo__m_cbActualArgSize]                      // stack_args
        test            rax, rax                                                                        // stack_args
        jnz             LOCAL_LABEL(UMThunkStub_CopyStackArgs)                                          // stack_args
        
LOCAL_LABEL(UMThunkStub_ArgumentsSetup):
        mov    rdi, [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_INT_ARG_OFFSET]
        mov    rsi, [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_INT_ARG_OFFSET + 0x08]
        mov    rdx, [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_INT_ARG_OFFSET + 0x10]
        mov    rcx, [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_INT_ARG_OFFSET + 0x18]
        mov    r8,  [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_INT_ARG_OFFSET + 0x20]
        mov    r9,  [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_INT_ARG_OFFSET + 0x28]
        movdqa xmm0, xmmword ptr [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_XMM_SAVE_OFFSET]
        movdqa xmm1, xmmword ptr [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_XMM_SAVE_OFFSET + 0x10]
        movdqa xmm2, xmmword ptr [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_XMM_SAVE_OFFSET + 0x20]
        movdqa xmm3, xmmword ptr [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_XMM_SAVE_OFFSET + 0x30]
        movdqa xmm4, xmmword ptr [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_XMM_SAVE_OFFSET + 0x40]
        movdqa xmm5, xmmword ptr [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_XMM_SAVE_OFFSET + 0x50]
        movdqa xmm6, xmmword ptr [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_XMM_SAVE_OFFSET + 0x60]
        movdqa xmm7, xmmword ptr [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_XMM_SAVE_OFFSET + 0x70]

        mov             rax, [r11 + OFFSETOF__UMThunkMarshInfo__m_pILStub]                              // rax <- Stub*
        call            rax

LOCAL_LABEL(PostCall):
        //
        // enable preemptive GC
        //
        mov             dword ptr [r12 + OFFSETOF__Thread__m_fPreemptiveGCDisabled], 0

        // epilog
        lea             rsp, [rbp - 8]         // deallocate arguments
        set_cfa_register rsp, (3*8)
        pop_nonvol_reg  r12
        pop_nonvol_reg  rbp
        ret


LOCAL_LABEL(DoThreadSetup):
        call            C_FUNC(CreateThreadBlockThrow)
        jmp             LOCAL_LABEL(HaveThread)
        
LOCAL_LABEL(InvalidTransition):
        //No arguments to setup , ReversePInvokeBadTransition will failfast
        call            C_FUNC(ReversePInvokeBadTransition)

LOCAL_LABEL(DoTrapReturningThreadsTHROW):
        mov             rdi, r12                                                                        // Thread* pThread
        mov             rsi, [rbp - UMThunkStubAMD64_RBP_OFFSET + UMThunkStubAMD64_METHODDESC_OFFSET]   // UMEntryThunk* pUMEntry
        call            C_FUNC(UMThunkStubRareDisableWorker)

        jmp             LOCAL_LABEL(InCooperativeMode)

LOCAL_LABEL(UMThunkStub_CopyStackArgs):
        // rax = cbStackArgs

        sub             rsp, rax
        and             rsp, -16

        // rax = number of bytes

        lea             rdi, [rbp + 0x10] // rbp + ra
        lea             rsi, [rsp]

LOCAL_LABEL(CopyLoop):
        // rax = number of bytes
        // rdi = src
        // rsi = dest
        // rdx = sratch

        add             rax, -8
        mov             rdx, [rdi + rax]
        mov             [rsi + rax], rdx
        jnz             LOCAL_LABEL(CopyLoop)

        jmp             LOCAL_LABEL(UMThunkStub_ArgumentsSetup)

#if _DEBUG
LOCAL_LABEL(WrongAppDomain):
        int3
#endif

NESTED_END UMThunkStub, _TEXT
