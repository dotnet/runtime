// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <amd64/AsmMacros_Unix.inc>

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
