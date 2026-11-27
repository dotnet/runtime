// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <amd64/AsmMacros_Unix.inc>

// Stack layout:
//
// (stack parameters)
// ...
// return address
// CalleeSavedRegisters::rbp
// CalleeSavedRegisters::rbx
// CalleeSavedRegisters::r15
// CalleeSavedRegisters::r14
// CalleeSavedRegisters::r13
// CalleeSavedRegisters::r12
// ArgumentRegisters::r9
// ArgumentRegisters::r8
// ArgumentRegisters::rcx
// ArgumentRegisters::rdx
// ArgumentRegisters::rsi
// ArgumentRegisters::rdi    <- __PWTB_StackAlloc, __PWTB_TransitionBlock
// padding to align xmm save area
// xmm7
// xmm6
// xmm5
// xmm4
// xmm3
// xmm2
// xmm1
// xmm0                      <- __PWTB_FloatArgumentRegisters
// extra locals + padding to qword align
.macro PROLOG_WITH_TRANSITION_BLOCK extraLocals = 0, stackAllocOnEntry = 0, stackAllocSpill1, stackAllocSpill2, stackAllocSpill3

        set_cfa_register rsp, 8

        __PWTB_FloatArgumentRegisters = \extraLocals

        .if ((__PWTB_FloatArgumentRegisters % 16) != 0)
        __PWTB_FloatArgumentRegisters = __PWTB_FloatArgumentRegisters + 8
        .endif

        __PWTB_StackAlloc = __PWTB_FloatArgumentRegisters + 8 * 16 + 8 // 8 floating point registers
        __PWTB_TransitionBlock = __PWTB_StackAlloc
        __PWTB_ArgumentRegisters = __PWTB_TransitionBlock

        .if \stackAllocOnEntry >= 4*8
        .error "Max supported stackAllocOnEntry is 3*8"
        .endif

        .if \stackAllocOnEntry > 0
        .cfi_adjust_cfa_offset \stackAllocOnEntry
        .endif

        // PUSH_CALLEE_SAVED_REGISTERS expanded here

        .if \stackAllocOnEntry < 8
        push_nonvol_reg rbp
        mov rbp, rsp
        .endif

        .if \stackAllocOnEntry < 2*8
        push_nonvol_reg rbx
        .endif

        .if \stackAllocOnEntry < 3*8
        push_nonvol_reg r15
        .endif

        push_nonvol_reg r14
        push_nonvol_reg r13
        push_nonvol_reg r12

        // ArgumentRegisters
        PUSH_ARGUMENT_REGISTERS

        .if \stackAllocOnEntry >= 3*8
        mov \stackAllocSpill3, [rsp + 0x48]
        save_reg_postrsp    r15, 0x48
        .endif

        .if \stackAllocOnEntry >= 2*8
        mov \stackAllocSpill2, [rsp + 0x50]
        save_reg_postrsp    rbx, 0x50
        .endif

        .if \stackAllocOnEntry >= 8
        mov \stackAllocSpill1, [rsp + 0x58]
        save_reg_postrsp    rbp, 0x58
        lea rbp, [rsp + 0x58]
        .endif

        alloc_stack     __PWTB_StackAlloc
        SAVE_FLOAT_ARGUMENT_REGISTERS __PWTB_FloatArgumentRegisters

        END_PROLOGUE

.endm

.macro EPILOG_WITH_TRANSITION_BLOCK_RETURN

        free_stack      __PWTB_StackAlloc
        SKIP_ARGUMENT_REGISTERS
        POP_CALLEE_SAVED_REGISTERS
        ret

.endm

.macro EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        RESTORE_FLOAT_ARGUMENT_REGISTERS __PWTB_FloatArgumentRegisters
        free_stack      __PWTB_StackAlloc
        POP_ARGUMENT_REGISTERS
        POP_CALLEE_SAVED_REGISTERS

.endm

// Inlined version of GetThreadEEAllocContext. Trashes volatile registers.
.macro INLINE_GET_ALLOC_CONTEXT_BASE
#ifdef FEATURE_EMULATED_TLS
        call    C_FUNC(GetThreadEEAllocContext)
#else
        INLINE_GET_TLS_VAR t_runtime_thread_locals

        .ifnc OFFSETOF__RuntimeThreadLocals__ee_alloc_context, 0
                lea     rax, [rax + OFFSETOF__RuntimeThreadLocals__ee_alloc_context]
        .endif
#endif
.endm

// Pushes a TransitionBlock on the stack without saving the argument registers.
// See the PROLOG_WITH_TRANSITION_BLOCK macro for the stack layout.
.macro PUSH_COOP_PINVOKE_FRAME target
        set_cfa_register rsp, 8

        PUSH_CALLEE_SAVED_REGISTERS
        // 6 * 8 for argument register space in TransitionBlock + alignment of the stack to 16b
        alloc_stack     56
        END_PROLOGUE

        lea     \target, [rsp + 8]
.endm

.macro POP_COOP_PINVOKE_FRAME
        free_stack      56
        POP_CALLEE_SAVED_REGISTERS
.endm

.macro INLINE_GETTHREAD
        // Inlined version of call C_FUNC(RhpGetThread)
#ifdef FEATURE_EMULATED_TLS
        // Emulated TLS (e.g. OpenBSD, which has no __tls_get_addr): call a C helper instead
        // of inlining the native thread-local access.
        call    C_FUNC(GetThreadHelper)
#else
        INLINE_GET_TLS_VAR t_CurrentThreadInfo
        mov rax, [rax + OFFSETOF__ThreadLocalInfo__m_pThread]
#endif
.endm
