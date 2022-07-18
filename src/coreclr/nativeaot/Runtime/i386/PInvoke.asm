;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code


include AsmMacros.inc

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

        end
