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

        option  casemap:none
        .code

extern _s_gsCookie:DWORD
extern ??_7InlinedCallFrame@@6B@:DWORD
extern _g_TrapReturningThreads:DWORD

extern @JIT_PInvokeEndRarePath@0:proc

.686P
.XMM

;
; in:
; InlinedCallFrame (ecx)   = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
;                            before actual InlinedCallFrame data)
; StackArgumentsSize (edx) = Number of argument bytes pushed on the stack, which will be popped by the callee
;
_JIT_PInvokeBegin@4 PROC public

        mov             eax, dword ptr [_s_gsCookie]
        mov             dword ptr [ecx], eax
        add             ecx, SIZEOF_GSCookie

        ;; set first slot to the value of InlinedCallFrame::`vftable' (checked by runtime code)
        lea             eax,[??_7InlinedCallFrame@@6B@]
        mov             dword ptr [ecx], eax

        mov             dword ptr [ecx + InlinedCallFrame__m_Datum], edx


        mov             eax, esp
        add             eax, 4
        mov             dword ptr [ecx + InlinedCallFrame__m_pCallSiteSP], eax
        mov             dword ptr [ecx + InlinedCallFrame__m_pCalleeSavedFP], ebp

        mov             eax, [esp]
        mov             dword ptr [ecx + InlinedCallFrame__m_pCallerReturnAddress], eax

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

        add             ecx, SIZEOF_GSCookie

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

        ret

RarePath:
        jmp             @JIT_PInvokeEndRarePath@0

_JIT_PInvokeEnd@4 ENDP

        end
