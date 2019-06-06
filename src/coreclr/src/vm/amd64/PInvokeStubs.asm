; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
;

;
; ==--==

include AsmMacros.inc
include AsmConstants.inc

extern GenericPInvokeCalliStubWorker:proc
extern VarargPInvokeStubWorker:proc
extern JIT_PInvokeEndRarePath:proc

extern s_gsCookie:QWORD
extern ??_7InlinedCallFrame@@6B@:QWORD
extern g_TrapReturningThreads:DWORD

;
; in:
; PINVOKE_CALLI_TARGET_REGISTER (r10) = unmanaged target
; PINVOKE_CALLI_SIGTOKEN_REGNUM (r11) = sig token       
;
; out:
; METHODDESC_REGISTER           (r10) = unmanaged target
;
LEAF_ENTRY GenericPInvokeCalliHelper, _TEXT
        
        ;
        ; check for existing IL stub
        ;
        mov             rax, [PINVOKE_CALLI_SIGTOKEN_REGISTER + OFFSETOF__VASigCookie__pNDirectILStub]
        test            rax, rax
        jz              GenericPInvokeCalliGenILStub

        ;
        ; We need to distinguish between a MethodDesc* and an unmanaged target.
        ; The way we do this is to shift the managed target to the left by one bit and then set the
        ; least significant bit to 1.  This works because MethodDesc* are always 8-byte aligned.
        ;
        shl             PINVOKE_CALLI_TARGET_REGISTER, 1
        or              PINVOKE_CALLI_TARGET_REGISTER, 1

        ;
        ; jump to existing IL stub
        ;
        jmp             rax

LEAF_END GenericPInvokeCalliHelper, _TEXT

NESTED_ENTRY GenericPInvokeCalliGenILStub, _TEXT
        
        PROLOG_WITH_TRANSITION_BLOCK

        ;
        ; save target
        ;
        mov             r12, METHODDESC_REGISTER
        mov             r13, PINVOKE_CALLI_SIGTOKEN_REGISTER

        ;
        ; GenericPInvokeCalliStubWorker(TransitionBlock * pTransitionBlock, VASigCookie * pVASigCookie, PCODE pUnmanagedTarget)
        ;
        lea             rcx, [rsp + __PWTB_TransitionBlock]     ; pTransitionBlock*
        mov             rdx, PINVOKE_CALLI_SIGTOKEN_REGISTER    ; pVASigCookie
        mov             r8, METHODDESC_REGISTER                 ; pUnmanagedTarget
        call            GenericPInvokeCalliStubWorker

        ;
        ; restore target
        ;
        mov             METHODDESC_REGISTER, r12
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, r13

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        jmp             GenericPInvokeCalliHelper

NESTED_END GenericPInvokeCalliGenILStub, _TEXT

LEAF_ENTRY VarargPInvokeStub, _TEXT
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, rcx
        jmp             VarargPInvokeStubHelper
LEAF_END VarargPInvokeStub, _TEXT
        
LEAF_ENTRY VarargPInvokeStub_RetBuffArg, _TEXT
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, rdx
        jmp             VarargPInvokeStubHelper
LEAF_END VarargPInvokeStub_RetBuffArg, _TEXT

LEAF_ENTRY VarargPInvokeStubHelper, _TEXT
        ;
        ; check for existing IL stub
        ;
        mov             rax, [PINVOKE_CALLI_SIGTOKEN_REGISTER + OFFSETOF__VASigCookie__pNDirectILStub]
        test            rax, rax
        jz              VarargPInvokeGenILStub

        ;
        ; jump to existing IL stub
        ;
        jmp             rax
        
LEAF_END VarargPInvokeStubHelper, _TEXT

;
; IN: METHODDESC_REGISTER (R10) stub secret param
;     PINVOKE_CALLI_SIGTOKEN_REGISTER (R11) VASigCookie*
;
; ASSUMES: we already checked for an existing stub to use
;
NESTED_ENTRY VarargPInvokeGenILStub, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK 

        ;
        ; save target
        ;
        mov             r12, METHODDESC_REGISTER
        mov             r13, PINVOKE_CALLI_SIGTOKEN_REGISTER

        ;
        ; VarargPInvokeStubWorker(TransitionBlock * pTransitionBlock, VASigCookie *pVASigCookie, MethodDesc *pMD)
        ;
        lea             rcx, [rsp + __PWTB_TransitionBlock]     ; pTransitionBlock*
        mov             rdx, PINVOKE_CALLI_SIGTOKEN_REGISTER    ; pVASigCookie
        mov             r8, METHODDESC_REGISTER                 ; pMD
        call            VarargPInvokeStubWorker

        ;
        ; restore target
        ;
        mov             METHODDESC_REGISTER, r12
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, r13

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        jmp             VarargPInvokeStubHelper

NESTED_END VarargPInvokeGenILStub, _TEXT

;
; in:
; InlinedCallFrame (rcx) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right 
;                          before actual InlinedCallFrame data)
;
;
LEAF_ENTRY JIT_PInvokeBegin, _TEXT
        
        mov             rax, qword ptr [s_gsCookie]
        mov             qword ptr [rcx], rax
        add             rcx, SIZEOF_GSCookie

        ;; set first slot to the value of InlinedCallFrame::`vftable' (checked by runtime code)
        lea             rax,[??_7InlinedCallFrame@@6B@]
        mov             qword ptr [rcx], rax

        mov             qword ptr [rcx + OFFSETOF__InlinedCallFrame__m_Datum], 0

        mov             rax, rsp
        add             rax, 8
        mov             qword ptr [rcx + OFFSETOF__InlinedCallFrame__m_pCallSiteSP], rax
        mov             qword ptr [rcx + OFFSETOF__InlinedCallFrame__m_pCalleeSavedFP], rbp

        mov             rax, [rsp]
        mov             qword ptr [rcx + OFFSETOF__InlinedCallFrame__m_pCallerReturnAddress], rax

        INLINE_GETTHREAD rax 
        ;; pFrame->m_Next = pThread->m_pFrame;
        mov             rdx, qword ptr [rax + OFFSETOF__Thread__m_pFrame]
        mov             qword ptr [rcx + OFFSETOF__Frame__m_Next], rdx

        ;; pThread->m_pFrame = pFrame;
        mov             qword ptr [rax + OFFSETOF__Thread__m_pFrame], rcx

        ;; pThread->m_fPreemptiveGCDisabled = 0
        mov             dword ptr [rax + OFFSETOF__Thread__m_fPreemptiveGCDisabled], 0

        ret

LEAF_END JIT_PInvokeBegin, _TEXT

;
; in:
; InlinedCallFrame (rcx) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right 
;                          before actual InlinedCallFrame data)
;
;
LEAF_ENTRY JIT_PInvokeEnd, _TEXT

        add             rcx, SIZEOF_GSCookie

        INLINE_GETTHREAD rdx 

        ;; rcx = pFrame
        ;; rdx = pThread

        ;; pThread->m_fPreemptiveGCDisabled = 1
        mov             dword ptr [rdx + OFFSETOF__Thread__m_fPreemptiveGCDisabled], 1

        ;; Check return trap
        cmp             [g_TrapReturningThreads], 0
        jnz             RarePath

        ;; pThread->m_pFrame = pFrame->m_Next
        mov             rax, qword ptr [rcx + OFFSETOF__Frame__m_Next]
        mov             qword ptr [rdx + OFFSETOF__Thread__m_pFrame], rax

        ret

RarePath:
        jmp             JIT_PInvokeEndRarePath

LEAF_END JIT_PInvokeEnd, _TEXT

        end
