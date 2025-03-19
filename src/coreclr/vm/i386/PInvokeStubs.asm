; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: PInvokeStubs.asm
;
; ***********************************************************************
;
;  *** NOTE:  If you make changes to this file, propagate the changes to
;             PInvokeStubs.s in this directory

; This contains JITinterface routines that are 100% x86 assembly

        .586
        .model  flat

        include asmconstants.inc
        include asmmacros.inc

        assume fs: nothing
        option  casemap:none
        .code

extern _g_TrapReturningThreads:DWORD

extern _JIT_PInvokeEndRarePath@0:proc
ifdef FEATURE_EH_FUNCLETS
extern _ProcessCLRException:PROC
endif ; FEATURE_EH_FUNCLETS

.686P
.XMM

;
; in:
; InlinedCallFrame (ecx)   = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
;                            before actual InlinedCallFrame data)
; StackArgumentsSize (edx) = Number of argument bytes pushed on the stack, which will be popped by the callee
;
_JIT_PInvokeBegin@4 PROC public

        ;; set first slot to the value of InlinedCallFrame identifier (checked by runtime code)
        mov             dword ptr [ecx], FRAMETYPE_InlinedCallFrame

        mov             dword ptr [ecx + InlinedCallFrame__m_Datum], edx


        mov             eax, esp
        add             eax, 4
        mov             dword ptr [ecx + InlinedCallFrame__m_pCallSiteSP], eax
        mov             dword ptr [ecx + InlinedCallFrame__m_pCalleeSavedFP], ebp

        mov             eax, [esp]
        mov             dword ptr [ecx + InlinedCallFrame__m_pCallerReturnAddress], eax

ifdef FEATURE_EH_FUNCLETS
        ;; Link SEH exception registration
        mov             eax, dword ptr fs:[0]
        mov             dword ptr [ecx + InlinedCallFrame__m_ExceptionRecord], eax
        mov             dword ptr [ecx + InlinedCallFrame__m_ExceptionRecord + 4], _ProcessCLRException
        lea             eax, [ecx + InlinedCallFrame__m_ExceptionRecord]
        mov             dword ptr fs:[0], eax
endif

        ;; edx = GetThread(). Trashes eax
        INLINE_GETTHREAD edx, eax

        ;; pFrame->m_Next = pThread->m_pFrame;
        mov             eax, dword ptr [edx + Thread_m_pFrame]
        mov             dword ptr [ecx + Frame__m_Next], eax

        ;; pThread->m_pFrame = pFrame;
        mov             dword ptr [edx + Thread_m_pFrame], ecx

        ;; pThread->m_fPreemptiveGCDisabled = 0
        mov             dword ptr [edx + Thread_m_fPreemptiveGCDisabled], 0

        ret

_JIT_PInvokeBegin@4 ENDP

;
; in:
; InlinedCallFrame (ecx) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
;                          before actual InlinedCallFrame data)
;
;
_JIT_PInvokeEnd@4 PROC public

        ;; edx = GetThread(). Trashes eax
        INLINE_GETTHREAD edx, eax

        ;; ecx = pFrame
        ;; edx = pThread

        ;; pThread->m_fPreemptiveGCDisabled = 1
        mov             dword ptr [edx + Thread_m_fPreemptiveGCDisabled], 1

        ;; Check return trap
        cmp             [_g_TrapReturningThreads], 0
        jnz             RarePath

        ;; pThread->m_pFrame = pFrame->m_Next
        mov             eax, dword ptr [ecx + Frame__m_Next]
        mov             dword ptr [edx + Thread_m_pFrame], eax

ifdef FEATURE_EH_FUNCLETS
        ;; Unlink SEH exception registration
        mov             eax, dword ptr [ecx + InlinedCallFrame__m_ExceptionRecord]
        mov             dword ptr fs:[0], eax
endif

        ret

RarePath:
        jmp             _JIT_PInvokeEndRarePath@0

_JIT_PInvokeEnd@4 ENDP

        end
