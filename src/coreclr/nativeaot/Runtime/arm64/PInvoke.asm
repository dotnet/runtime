;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

        TEXTAREA

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpPInvoke
;;
;; IN: x0: address of pinvoke frame
;;
;; TRASHES: x9
;;
;; This helper assumes that its callsite is as good to start the stackwalk as the actual PInvoke callsite.
;; The codegenerator must treat the callsite of this helper as GC triggering and generate the GC info for it.
;; Also, the codegenerator must ensure that there are no live GC references in callee saved registers.
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY RhpPInvoke, _TEXT

        str     fp, [x0, #OFFSETOF__PInvokeTransitionFrame__m_FramePointer]
        str     lr, [x0, #OFFSETOF__PInvokeTransitionFrame__m_RIP]
        mov     x9, sp
        str     x9, [x0, #OFFSETOF__PInvokeTransitionFrame__m_PreservedRegs]
        mov     x9, #PTFF_SAVE_SP
        str     x9, [x0, #OFFSETOF__PInvokeTransitionFrame__m_Flags]

        INLINE_GETTHREAD x1, x9

        str     x1, [x0, #OFFSETOF__PInvokeTransitionFrame__m_pThread]
        str     x0, [x1, #OFFSETOF__Thread__m_pTransitionFrame]
        ret
    NESTED_END RhpPInvoke


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpPInvokeReturn
;;
;; IN: x0: address of pinvoke frame
;;
;; TRASHES: x9, x10
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    LEAF_ENTRY RhpPInvokeReturn, _TEXT
        ldr     x9, [x0, #OFFSETOF__PInvokeTransitionFrame__m_pThread]
        mov     x10, 0
        str     x10, [x9, #OFFSETOF__Thread__m_pTransitionFrame]

        ldr     x9, =RhpTrapThreads
        ldr     w9, [x9]
        cbnz    w9, %ft0 ;; TrapThreadsFlags_None = 0
        ret
0
        ;; passing transition frame pointer in x0
        b       RhpWaitForGC2
    LEAF_END RhpPInvokeReturn

    end
