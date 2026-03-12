// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <AsmOffsets.inc>
#include <unixasmmacros.inc>

.syntax unified
.thumb

//
// RhpPInvoke
//
// IN:  R0: address of pinvoke frame
//
// This helper assumes that its callsite is as good to start the stackwalk as the actual PInvoke callsite.
// The codegenerator must treat the callsite of this helper as GC triggering and generate the GC info for it.
// Also, the codegenerator must ensure that there are no live GC references in callee saved registers.
//

NESTED_ENTRY RhpPInvoke, _TEXT, NoHandler
        str     lr, [r0, #OFFSETOF__PInvokeTransitionFrame__m_RIP]
        str     r11, [r0, #OFFSETOF__PInvokeTransitionFrame__m_FramePointer]
        // We need to save R9 which could be frame pointer if the caller method uses stackalloc (REG_SAVED_LOCALLOC_SP)
        str     r9, [r0, #OFFSETOF__PInvokeTransitionFrame__m_PreservedRegs]
        str     sp, [r0, #OFFSETOF__PInvokeTransitionFrame__m_PreservedRegs + 4]
        mov     r3, #(PTFF_SAVE_R9 + PTFF_SAVE_SP)
        str     r3, [r0, #OFFSETOF__PInvokeTransitionFrame__m_Flags]

        PROLOG_PUSH "{r5,lr}"

        mov     r5, r0
        // get TLS global variable address
        // r0 = GetThread()
        INLINE_GETTHREAD
        str     r0, [r5, #OFFSETOF__PInvokeTransitionFrame__m_pThread]
        str     r5, [r0, #OFFSETOF__Thread__m_pTransitionFrame]

        EPILOG_POP "{r5,pc}"

NESTED_END RhpPInvoke, _TEXT


//
// RhpPInvokeReturn
//
// IN:  R0: address of pinvoke frame
//
LEAF_ENTRY RhpPInvokeReturn, _TEXT
        ldr     r3, [r0, #OFFSETOF__PInvokeTransitionFrame__m_pThread]

        mov     r2, #0
        str     r2, [r3, #OFFSETOF__Thread__m_pTransitionFrame]

        PREPARE_EXTERNAL_VAR_INDIRECT RhpTrapThreads, r3
        cbnz    r3, LOCAL_LABEL(ReturnRareTrapThread)  // TrapThreadsFlags_None = 0

        bx      lr
LOCAL_LABEL(ReturnRareTrapThread):
        // passing transition frame pointer in r0
        b       C_FUNC(RhpWaitForGC2)
LEAF_END RhpPInvokeReturn, _TEXT
