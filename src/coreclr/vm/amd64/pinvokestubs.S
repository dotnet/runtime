// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"


//
// in:
// PINVOKE_CALLI_TARGET_REGISTER (r10) = unmanaged target
// PINVOKE_CALLI_SIGTOKEN_REGNUM (r11) = sig token
//
// out:
// METHODDESC_REGISTER           (r10) = unmanaged target
//
LEAF_ENTRY GenericPInvokeCalliHelper, _TEXT

        //
        // check for existing IL stub
        //
        mov             rax, [PINVOKE_CALLI_SIGTOKEN_REGISTER + OFFSETOF__VASigCookie__pNDirectILStub]
        test            rax, rax
        jz              C_FUNC(GenericPInvokeCalliGenILStub)

        //
        // We need to distinguish between a MethodDesc* and an unmanaged target.
        // The way we do this is to shift the managed target to the left by one bit and then set the
        // least significant bit to 1.  This works because MethodDesc* are always 8-byte aligned.
        //
        shl             PINVOKE_CALLI_TARGET_REGISTER, 1
        or              PINVOKE_CALLI_TARGET_REGISTER, 1

        //
        // jump to existing IL stub
        //
        jmp             rax

LEAF_END GenericPInvokeCalliHelper, _TEXT

NESTED_ENTRY GenericPInvokeCalliGenILStub, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

        //
        // save target
        //
        mov             r12, METHODDESC_REGISTER
        mov             r13, PINVOKE_CALLI_SIGTOKEN_REGISTER

        //
        // GenericPInvokeCalliStubWorker(TransitionBlock * pTransitionBlock, VASigCookie * pVASigCookie, PCODE pUnmanagedTarget)
        //
        lea             rdi, [rsp + __PWTB_TransitionBlock]     // pTransitionBlock*
        mov             rsi, PINVOKE_CALLI_SIGTOKEN_REGISTER    // pVASigCookie
        mov             rdx, METHODDESC_REGISTER                // pUnmanagedTarget
        call            C_FUNC(GenericPInvokeCalliStubWorker)

        //
        // restore target
        //
        mov             METHODDESC_REGISTER, r12
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, r13

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        jmp             C_FUNC(GenericPInvokeCalliHelper)

NESTED_END GenericPInvokeCalliGenILStub, _TEXT

LEAF_ENTRY VarargPInvokeStub, _TEXT
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, rdi
        jmp             C_FUNC(VarargPInvokeStubHelper)
LEAF_END VarargPInvokeStub, _TEXT

LEAF_ENTRY VarargPInvokeStub_RetBuffArg, _TEXT
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, rsi
        jmp             C_FUNC(VarargPInvokeStubHelper)
LEAF_END VarargPInvokeStub_RetBuffArg, _TEXT

LEAF_ENTRY VarargPInvokeStubHelper, _TEXT
        //
        // check for existing IL stub
        //
        mov             rax, [PINVOKE_CALLI_SIGTOKEN_REGISTER + OFFSETOF__VASigCookie__pNDirectILStub]
        test            rax, rax
        jz              C_FUNC(VarargPInvokeGenILStub)

        //
        // jump to existing IL stub
        //
        jmp             rax

LEAF_END VarargPInvokeStubHelper, _TEXT

//
// IN: METHODDESC_REGISTER (R10) stub secret param
//     PINVOKE_CALLI_SIGTOKEN_REGISTER (R11) VASigCookie*
//
// ASSUMES: we already checked for an existing stub to use
//
NESTED_ENTRY VarargPInvokeGenILStub, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

        //
        // save target
        //
        mov             r12, METHODDESC_REGISTER
        mov             r13, PINVOKE_CALLI_SIGTOKEN_REGISTER

        //
        // VarargPInvokeStubWorker(TransitionBlock * pTransitionBlock, VASigCookie *pVASigCookie, MethodDesc *pMD)
        //
        lea             rdi, [rsp + __PWTB_TransitionBlock]     // pTransitionBlock*
        mov             rsi, PINVOKE_CALLI_SIGTOKEN_REGISTER    // pVASigCookie
        mov             rdx, METHODDESC_REGISTER                // pMD
        call            C_FUNC(VarargPInvokeStubWorker)

        //
        // restore target
        //
        mov             METHODDESC_REGISTER, r12
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, r13

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        jmp             C_FUNC(VarargPInvokeStubHelper)

NESTED_END VarargPInvokeGenILStub, _TEXT

//
// IN:
// InlinedCallFrame (rdi) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
NESTED_ENTRY JIT_PInvokeBegin, _TEXT, NoHandler

        push_nonvol_reg r14
        END_PROLOGUE

        PREPARE_EXTERNAL_VAR s_gsCookie, rax
        mov             rax, qword ptr [rax]
        mov             qword ptr [rdi], rax
        add             rdi, SIZEOF_GSCookie

        // set first slot to the value of InlinedCallFrame vftable (checked by runtime code)
        PREPARE_EXTERNAL_VAR _ZTV16InlinedCallFrame, rax
        add             rax, 0x10
        mov             qword ptr [rdi], rax

        mov             qword ptr [rdi + OFFSETOF__InlinedCallFrame__m_Datum], 0

        lea             rax, [rsp + 0x10]
        mov             qword ptr [rdi + OFFSETOF__InlinedCallFrame__m_pCallSiteSP], rax
        mov             qword ptr [rdi + OFFSETOF__InlinedCallFrame__m_pCalleeSavedFP], rbp

        mov             rax, qword ptr [rsp + 8]
        mov             qword ptr [rdi + OFFSETOF__InlinedCallFrame__m_pCallerReturnAddress], rax

        // Save pFrame in callee saved register
        mov             r14, rdi

        // Get current thread and cache it in current frame
        call            C_FUNC(GetThreadHelper)
        mov             qword ptr [r14 + OFFSETOF__InlinedCallFrame__m_pThread], rax

        // pFrame->m_Next = pThread->m_pFrame;
        mov             rdx, qword ptr [rax + OFFSETOF__Thread__m_pFrame]
        mov             qword ptr [r14 + OFFSETOF__Frame__m_Next], rdx

        // pThread->m_pFrame = pFrame;
        mov             qword ptr [rax + OFFSETOF__Thread__m_pFrame], r14

        // pThread->m_fPreemptiveGCDisabled = 0
        mov             dword ptr [rax + OFFSETOF__Thread__m_fPreemptiveGCDisabled], 0

        pop_nonvol_reg  r14
        ret

NESTED_END JIT_PInvokeBegin, _TEXT

//
// IN:
// InlinedCallFrame (rdi) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
LEAF_ENTRY JIT_PInvokeEnd, _TEXT

        add             rdi, SIZEOF_GSCookie
        mov             rsi, qword ptr [rdi + OFFSETOF__InlinedCallFrame__m_pThread]

        // rdi = pFrame
        // rsi = pThread

        // pThread->m_fPreemptiveGCDisabled = 1
        mov             dword ptr [rsi + OFFSETOF__Thread__m_fPreemptiveGCDisabled], 1

        // Check return trap
        PREPARE_EXTERNAL_VAR g_TrapReturningThreads, rax
        cmp             dword ptr [rax], 0
        jnz             RarePath

        // pThread->m_pFrame = pFrame->m_Next
        mov             rax, qword ptr [rdi + OFFSETOF__Frame__m_Next]
        mov             qword ptr [rsi + OFFSETOF__Thread__m_pFrame], rax

        ret

RarePath:
        jmp             C_FUNC(JIT_PInvokeEndRarePath)

LEAF_END JIT_PInvokeEnd, _TEXT
