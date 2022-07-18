;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

        TEXTAREA

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

        INLINE_GETTHREAD_CONSTANT_POOL
