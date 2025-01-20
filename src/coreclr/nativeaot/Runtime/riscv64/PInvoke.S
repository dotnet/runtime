// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

.global RhpTrapThreads

//
// RhpPInvoke
//
// IN:  a0: address of pinvoke frame
//
// This helper assumes that its callsite is as good to start the stackwalk as the actual PInvoke callsite.
// The code generator must treat the callsite of this helper as GC triggering and generate the GC info for it.
// Also, the code generator must ensure that there are no live GC references in callee saved registers.
//

NESTED_ENTRY RhpPInvoke, _TEXT, NoHandler
        sd    fp, OFFSETOF__PInvokeTransitionFrame__m_FramePointer(a0)
        sd    ra, OFFSETOF__PInvokeTransitionFrame__m_RIP(a0)
        sd    sp, OFFSETOF__PInvokeTransitionFrame__m_PreservedRegs(a0)
        li    t0, PTFF_SAVE_SP
        sd    t0, OFFSETOF__PInvokeTransitionFrame__m_Flags(a0)

        // get TLS global variable address
#ifdef FEATURE_EMULATED_TLS
        GETTHREAD_ETLS_1
#else
        INLINE_GETTHREAD a1
#endif

        sd    a1, OFFSETOF__PInvokeTransitionFrame__m_pThread(a0)
        sd    a0, OFFSETOF__Thread__m_pTransitionFrame(a1)
        jr  ra
NESTED_END RhpPInvoke, _TEXT

LEAF_ENTRY RhpPInvokeReturn, _TEXT
        ld    t0, OFFSETOF__PInvokeTransitionFrame__m_pThread(a0)
        sd    zero, OFFSETOF__Thread__m_pTransitionFrame(t0)

        PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, a5

        bne   t0, zero, 1f   // TrapThreadsFlags_None = 0
        jr  ra
1:
        // passing transition frame pointer in a0
        tail  C_FUNC(RhpWaitForGC2)
LEAF_END RhpPInvokeReturn, _TEXT
