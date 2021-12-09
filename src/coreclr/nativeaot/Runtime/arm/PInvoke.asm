;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

        TEXTAREA

        IMPORT RhpReversePInvokeBadTransition

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
        NESTED_ENTRY RhpWaitForSuspend

        PROLOG_PUSH {r0-r4,lr}     ; Need to save argument registers r0-r3 and lr, r4 is just for alignment
        PROLOG_VPUSH {d0-d7}       ; Save float argument registers as well since they're volatile

        bl          RhpWaitForSuspend2

        EPILOG_VPOP {d0-d7}
        EPILOG_POP  {r0-r4,pc}

        NESTED_END RhpWaitForSuspend


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpWaitForGCNoAbort
;;
;;
;; INPUT: r2: transition frame
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
        NESTED_ENTRY RhpWaitForGCNoAbort

        PROLOG_PUSH {r0-r6,lr}  ; Even number of registers to maintain 8-byte stack alignment
        PROLOG_VPUSH {d0-d3}    ; Save float return value registers as well

        ldr         r5, [r2, #OFFSETOF__PInvokeTransitionFrame__m_pThread]

        ldr         r0, [r5, #OFFSETOF__Thread__m_ThreadStateFlags]
        tst         r0, #TSF_DoNotTriggerGc
        bne         Done

        mov         r0, r2      ; passing transition frame in r0
        bl          RhpWaitForGC2

Done
        EPILOG_VPOP {d0-d3}
        EPILOG_POP  {r0-r6,pc}

        NESTED_END RhpWaitForGCNoAbort

        EXTERN RhpThrowHwEx

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpWaitForGC
;;
;;
;; INPUT: r2: transition frame
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
        NESTED_ENTRY RhpWaitForGC
        PROLOG_PUSH  {r0,lr}

        ldr         r0, =RhpTrapThreads
        ldr         r0, [r0]
        tst         r0, #TrapThreadsFlags_TrapThreads
        beq         NoWait
        bl          RhpWaitForGCNoAbort
NoWait
        tst         r0, #TrapThreadsFlags_AbortInProgress
        beq         NoAbort
        ldr         r0, [r2, #OFFSETOF__PInvokeTransitionFrame__m_Flags]
        tst         r0, #PTFF_THREAD_ABORT
        beq         NoAbort
        EPILOG_POP  {r0,r1}         ; hijack target address as exception PC
        EPILOG_NOP  mov r0, #STATUS_REDHAWK_THREAD_ABORT
        EPILOG_BRANCH RhpThrowHwEx
NoAbort
        EPILOG_POP  {r0,pc}
        NESTED_END RhpWaitForGC

        INLINE_GETTHREAD_CONSTANT_POOL


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpReversePInvokeAttachOrTrapThread -- rare path for RhpPInvoke
;;
;;
;; INPUT: r4: address of reverse pinvoke frame
;;
;; TRASHES: none
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
        NESTED_ENTRY RhpReversePInvokeAttachOrTrapThread

        PROLOG_PUSH {r0-r4,lr}     ; Need to save argument registers r0-r3 and lr, r4 is just for alignment
        PROLOG_VPUSH {d0-d7}       ; Save float argument registers as well since they're volatile

        mov         r0, r4         ; passing reverse pinvoke frame pointer in r0
        bl          RhpReversePInvokeAttachOrTrapThread2

        EPILOG_VPOP {d0-d7}
        EPILOG_POP  {r0-r4,pc}

        NESTED_END RhpReversePInvokeTrapThread


        end
