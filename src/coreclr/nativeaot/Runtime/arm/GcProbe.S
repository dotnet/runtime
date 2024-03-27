// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

.global RhpGcPoll2
.global RhpThrowHwEx

// See PUSH_COOP_PINVOKE_FRAME, this macro is very similar, but also saves return registers
// and accepts the register bitmask
// Call this macro first in the method (no further prolog instructions can be added after this).
//
//  threadReg     : register containing the Thread* (this will be preserved).
//  trashReg      : register that can be trashed by this macro
//  BITMASK       : value to initialize m_dwFlags field with (register or #constant)
.macro PUSH_PROBE_FRAME threadReg, trashReg, BITMASK
    // Define the method prolog, allocating enough stack space for the PInvokeTransitionFrame and saving
    // incoming register values into it.
    PROLOG_VPUSH        "{d0-d3}"           // Save d0-d3 which can have the floating point return value
    PROLOG_PUSH         "{r0,r1}"           // Save return registers
    PROLOG_STACK_ALLOC  4                   // Space for caller's SP
    PROLOG_PUSH         "{r4-r10}"          // Save non-volatile registers
    PROLOG_STACK_ALLOC  8                   // Space for flags and Thread*
    PROLOG_PUSH         "{r11,lr}"          // Save caller's frame pointer and return address

    str         \threadReg, [sp, #OFFSETOF__PInvokeTransitionFrame__m_pThread]
    mov         \trashReg, \BITMASK
    str         \trashReg, [sp, #OFFSETOF__PInvokeTransitionFrame__m_Flags]

    // Compute SP value at entry to this method and save it in slot of the frame.
    add         \trashReg, sp, #(14 * 4 + 4 * 8)
    str         \trashReg, [sp, #(11 * 4)]

    // Link the frame into the Thread
    str         sp, [\threadReg, #OFFSETOF__Thread__m_pDeferredTransitionFrame]
.endm

//
// Remove the frame from a previous call to PUSH_PROBE_FRAME from the top of the stack and restore preserved
// registers and return value to their values from before the probe was called (while also updating any
// object refs or byrefs).
//
.macro POP_PROBE_FRAME
    EPILOG_POP          "{r11,lr}"          // Restore caller's frame pointer and return address
    EPILOG_STACK_FREE   8                   // Discard flags and Thread*
    EPILOG_POP          "{r4-r10}"          // Restore non-volatile registers
    EPILOG_STACK_FREE   4                   // Discard caller's SP
    EPILOG_POP          "{r0,r1}"           // Restore return registers
    EPILOG_VPOP         "{d0-d3}"           // Restore d0-d3 which can have the floating point return value
.endm

//
// The prolog for all GC suspension hijacks (normal and stress). Fixes up the hijacked return address, and
// clears the hijack state.
//
// Register state on entry:
//  All registers correct for return to the original return address.
//
// Register state on exit:
//  r2: thread pointer
//  r3: trashed
//
.macro FixupHijackedCallstack
    push        {r0, r1}

    // r0 <- GetThread()
    INLINE_GETTHREAD

    mov         r2, r0
    pop         {r0, r1}

    // Fix the stack by restoring the original return address
    ldr         lr, [r2, #OFFSETOF__Thread__m_pvHijackedReturnAddress]
    ldr         r12, [r2, #OFFSETOF__Thread__m_uHijackedReturnValueFlags]

    // Clear hijack state
    mov         r3, #0
    str         r3, [r2, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
    str         r3, [r2, #OFFSETOF__Thread__m_pvHijackedReturnAddress]
    str         r3, [r2, #OFFSETOF__Thread__m_uHijackedReturnValueFlags]
.endm

NESTED_ENTRY RhpWaitForGC, _TEXT, NoHandler
    PUSH_PROBE_FRAME r2, r3, r12

    ldr         r0, [r2, #OFFSETOF__Thread__m_pDeferredTransitionFrame] 
    bl          RhpWaitForGC2

    ldr         r2, [sp, #OFFSETOF__PInvokeTransitionFrame__m_Flags]
    tst         r2, #PTFF_THREAD_ABORT
    bne         LOCAL_LABEL(ThrowThreadAbort)

    POP_PROBE_FRAME
    bx          lr

LOCAL_LABEL(ThrowThreadAbort):
    POP_PROBE_FRAME
    mov         r0, #STATUS_REDHAWK_THREAD_ABORT
    mov         r1, lr // return address as exception PC
    b           C_FUNC(RhpThrowHwEx)
NESTED_END RhpWaitForGC

LEAF_ENTRY RhpGcPoll
    PREPARE_EXTERNAL_VAR_INDIRECT RhpTrapThreads, r0
    cmp         r0, #TrapThreadsFlags_None
    bne         RhpGcPollRare
    bx          lr
LEAF_END RhpGcPoll

NESTED_ENTRY RhpGcPollRare, _TEXT, NoHandler
    PUSH_COOP_PINVOKE_FRAME r0
    bl RhpGcPoll2
    POP_COOP_PINVOKE_FRAME
    bx           lr
NESTED_END RhpGcPollRare

NESTED_ENTRY RhpGcProbeHijack, _TEXT, NoHandler
    FixupHijackedCallstack

    PREPARE_EXTERNAL_VAR_INDIRECT RhpTrapThreads, r3
    tst         r3, #TrapThreadsFlags_TrapThreads
    bne         LOCAL_LABEL(WaitForGC)
    bx          lr
LOCAL_LABEL(WaitForGC):
    mov         r3, #(DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_R0)
    orr         r12, r3
    b           RhpWaitForGC
NESTED_END RhpGcProbeHijack

#ifdef FEATURE_GC_STRESS
LEAF_ENTRY RhpGcStressHijack
    // Not implemented
    EMIT_BREAKPOINT
LEAF_END RhpGcStressHijack
#endif
