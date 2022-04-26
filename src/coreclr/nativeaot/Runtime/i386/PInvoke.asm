;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code


include AsmMacros.inc

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpWaitForSuspend -- rare path for RhpPInvoke and RhpReversePInvokeReturn
;;
;;
;; INPUT: none
;;
;; TRASHES: none
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
_RhpWaitForSuspend proc public
        push        ebp
        mov         ebp, esp
        push        eax
        push        ecx
        push        edx

        call        RhpWaitForSuspend2

        pop         edx
        pop         ecx
        pop         eax
        pop         ebp
        ret
_RhpWaitForSuspend endp


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpWaitForGCNoAbort
;;
;;
;; INPUT: ECX: transition frame
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
_RhpWaitForGCNoAbort proc public
        push        ebp
        mov         ebp, esp
        push        eax
        push        edx
        push        ebx
        push        esi

        mov         esi, [ecx + OFFSETOF__PInvokeTransitionFrame__m_pThread]

        test        dword ptr [esi + OFFSETOF__Thread__m_ThreadStateFlags], TSF_DoNotTriggerGc
        jnz         Done

        ; passing transition frame pointer in ecx
        call        RhpWaitForGC2

Done:
        pop         esi
        pop         ebx
        pop         edx
        pop         eax
        pop         ebp
        ret
_RhpWaitForGCNoAbort endp

RhpThrowHwEx equ @RhpThrowHwEx@0
EXTERN RhpThrowHwEx : PROC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpWaitForGC
;;
;;
;; INPUT: ECX: transition frame
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
_RhpWaitForGC proc public
        push        ebp
        mov         ebp, esp
        push        ebx

        mov         ebx, ecx
        test        [RhpTrapThreads], TrapThreadsFlags_TrapThreads
        jz          NoWait

        call        _RhpWaitForGCNoAbort
NoWait:
        test        [RhpTrapThreads], TrapThreadsFlags_AbortInProgress
        jz          Done
        test        dword ptr [ebx + OFFSETOF__PInvokeTransitionFrame__m_Flags], PTFF_THREAD_ABORT
        jz          Done

        mov         ecx, STATUS_REDHAWK_THREAD_ABORT
        pop         ebx
        pop         ebp
        pop         edx                 ; return address as exception RIP
        jmp         RhpThrowHwEx        ; Throw the ThreadAbortException as a special kind of hardware exception
Done:
        pop         ebx
        pop         ebp
        ret
_RhpWaitForGC endp


        end
