// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

.global RhpTrapThreads

// Note: these must match the defs in PInvokeTransitionFrameFlags defined in rhbinder.h
PTFF_SAVE_SP            = 0x00000400

// Bit position for the flags above, to be used with tbz / tbnz instructions
PTFF_THREAD_ABORT_BIT   = 36

// Bit position for the flags above, to be used with tbz/tbnz instructions
TSF_Attached_Bit                = 0
TSF_SuppressGcStress_Bit        = 3
TSF_DoNotTriggerGc_Bit          = 4

//
// RhpPInvoke
//
// IN:  X0: address of pinvoke frame
//
// This helper assumes that its callsite is as good to start the stackwalk as the actual PInvoke callsite.
// The codegenerator must treat the callsite of this helper as GC triggering and generate the GC info for it.
// Also, the codegenerator must ensure that there are no live GC references in callee saved registers.
//

NESTED_ENTRY RhpPInvoke, _TEXT, NoHandler
        str     fp, [x0, #OFFSETOF__PInvokeTransitionFrame__m_FramePointer]
        str     lr, [x0, #OFFSETOF__PInvokeTransitionFrame__m_RIP]
        mov     x9, SP
        str     x9, [x0, #OFFSETOF__PInvokeTransitionFrame__m_PreservedRegs]
        mov     x9, #PTFF_SAVE_SP
        str     x9, [x0, #OFFSETOF__PInvokeTransitionFrame__m_Flags]

        // get TLS global variable address

#ifdef FEATURE_EMULATED_TLS
        GETTHREAD_ETLS_1
#else
        INLINE_GETTHREAD x1
#endif

        str     x1, [x0, #OFFSETOF__PInvokeTransitionFrame__m_pThread]
        str     x0, [x1, #OFFSETOF__Thread__m_pTransitionFrame]
        ret
NESTED_END RhpPInvoke, _TEXT


LEAF_ENTRY RhpPInvokeReturn, _TEXT
        ldr         x9, [x0, #OFFSETOF__PInvokeTransitionFrame__m_pThread]
        mov         x10, 0
        str         x10, [x9, #OFFSETOF__Thread__m_pTransitionFrame]

        PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, 9

        cbnz    w9, 0f  // TrapThreadsFlags_None = 0
        ret
0:
        // passing transition frame pointer in x0
        b         C_FUNC(RhpWaitForGC2)
LEAF_END RhpPInvokeReturn, _TEXT

