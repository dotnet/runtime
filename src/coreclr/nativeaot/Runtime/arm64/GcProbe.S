// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

PROBE_FRAME_SIZE = 0xD0  // 4 * 8  for fixed part of PInvokeTransitionFrame (fp, lr, m_pThread, m_Flags) +
                         // 10 * 8 for callee saved registers +
                         // 1 * 8  for caller SP +
                         // 2 * 8  for int returns +
                         // 1 * 8  for alignment padding +
                         // 4 * 16 for FP/HFA/HVA returns

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

    // First create PInvokeTransitionFrame
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -PROBE_FRAME_SIZE      // Push down stack pointer and store FP and LR

    // Slot at [sp, #0x10] is reserved for Thread *
    // Slot at [sp, #0x18] is reserved for bitmask of saved registers

    // Save callee saved registers
    PROLOG_SAVE_REG_PAIR   x19, x20, 0x20
    PROLOG_SAVE_REG_PAIR   x21, x22, 0x30
    PROLOG_SAVE_REG_PAIR   x23, x24, 0x40
    PROLOG_SAVE_REG_PAIR   x25, x26, 0x50
    PROLOG_SAVE_REG_PAIR   x27, x28, 0x60

    // Slot at [sp, #0x70] is reserved for caller sp

    // Save the integer return registers
    stp         x0, x1,   [sp, #0x78]

    // Slot at [sp, #0x88] is alignment padding

    // Save the FP/HFA/HVA return registers
    stp         q0, q1,   [sp, #0x90]
    stp         q2, q3,   [sp, #0xB0]

    // Perform the rest of the PInvokeTransitionFrame initialization.
    //   str         \threadReg,[sp, #OFFSETOF__PInvokeTransitionFrame__m_pThread]       // Thread * (unused by stackwalker)
    //   str         \BITMASK,  [sp, #OFFSETOF__PInvokeTransitionFrame__m_Flags]         // save the register bitmask passed in by caller
    stp         \threadReg, \BITMASK, [sp, #OFFSETOF__PInvokeTransitionFrame__m_pThread]

    add         \trashReg, sp,  #PROBE_FRAME_SIZE                                   // recover value of caller's SP
    str         \trashReg, [sp, #0x70]                                              // save caller's SP

    // link the frame into the Thread
    mov         \trashReg, sp
    str         \trashReg, [\threadReg, #OFFSETOF__Thread__m_pDeferredTransitionFrame]
.endm

//
// Remove the frame from a previous call to PUSH_PROBE_FRAME from the top of the stack and restore preserved
// registers and return value to their values from before the probe was called (while also updating any
// object refs or byrefs).
//
.macro POP_PROBE_FRAME

    // Restore the integer return registers
    ldp          x0, x1,   [sp, #0x78]

    // Restore the FP/HFA/HVA return registers
    ldp          q0, q1,   [sp, #0x90]
    ldp          q2, q3,   [sp, #0xB0]

    // Restore callee saved registers
    EPILOG_RESTORE_REG_PAIR x19, x20, 0x20
    EPILOG_RESTORE_REG_PAIR x21, x22, 0x30
    EPILOG_RESTORE_REG_PAIR x23, x24, 0x40
    EPILOG_RESTORE_REG_PAIR x25, x26, 0x50
    EPILOG_RESTORE_REG_PAIR x27, x28, 0x60

    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr, PROBE_FRAME_SIZE
.endm

//
// The prolog for all GC suspension hijacks (normal and stress). Fixes up the hijacked return address, and
// clears the hijack state.
//
// Register state on entry:
//  All registers correct for return to the original return address.
//
// Register state on exit:
//  x2: thread pointer
//  x12: transition frame flags for the return registers x0 and x1
//
.macro FixupHijackedCallstack

    // x2 <- GetThread()
#ifdef FEATURE_EMULATED_TLS
    GETTHREAD_ETLS_2
#else
    INLINE_GETTHREAD x2
#endif

    //
    // Fix the stack by restoring the original return address
    //
    // Load m_pvHijackedReturnAddress and m_uHijackedReturnValueFlags
    ldp         lr, x12, [x2, #OFFSETOF__Thread__m_pvHijackedReturnAddress]

    //
    // Clear hijack state
    //
    // Clear m_ppvHijackedReturnAddressLocation and m_pvHijackedReturnAddress
    stp         xzr, xzr, [x2, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
    // Clear m_uHijackedReturnValueFlags
    str         xzr, [x2, #OFFSETOF__Thread__m_uHijackedReturnValueFlags]

.endm

//
// GC Probe Hijack target
//
NESTED_ENTRY RhpGcProbeHijack, _TEXT, NoHandler
    FixupHijackedCallstack

    PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, 3
    tbnz        x3, #TrapThreadsFlags_TrapThreads_Bit, LOCAL_LABEL(WaitForGC)
    ret

LOCAL_LABEL(WaitForGC):
    orr         x12, x12, DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_X0 + PTFF_SAVE_X1
    b           C_FUNC(RhpWaitForGC)
NESTED_END RhpGcProbeHijack

.global C_FUNC(RhpThrowHwEx)

NESTED_ENTRY RhpWaitForGC, _TEXT, NoHandler
    PUSH_PROBE_FRAME x2, x3, x12

    ldr         x0, [x2, #OFFSETOF__Thread__m_pDeferredTransitionFrame]
    bl          C_FUNC(RhpWaitForGC2)

    ldr         x2, [sp, #OFFSETOF__PInvokeTransitionFrame__m_Flags]
    tbnz        x2, #PTFF_THREAD_ABORT_BIT, LOCAL_LABEL(ThrowThreadAbort)

    .cfi_remember_state
    POP_PROBE_FRAME
    EPILOG_RETURN

    .cfi_restore_state
LOCAL_LABEL(ThrowThreadAbort):
    POP_PROBE_FRAME
    mov w0, #STATUS_REDHAWK_THREAD_ABORT
    mov x1, lr // return address as exception PC
    b C_FUNC(RhpThrowHwEx)
NESTED_END RhpWaitForGC

.global C_FUNC(RhpGcPoll2)

LEAF_ENTRY RhpGcPoll
    PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, 0
    cbnz    w0, LOCAL_LABEL(RhpGcPoll_Rare) // TrapThreadsFlags_None = 0
    ret
LOCAL_LABEL(RhpGcPoll_Rare):
    b C_FUNC(RhpGcPollRare)
LEAF_END RhpGcPoll

NESTED_ENTRY RhpGcPollRare, _TEXT, NoHandler
    PUSH_COOP_PINVOKE_FRAME x0
    bl C_FUNC(RhpGcPoll2)
    POP_COOP_PINVOKE_FRAME
    ret
NESTED_END RhpGcPollRare


#ifdef FEATURE_GC_STRESS

//
// GC Stress Hijack targets
//
LEAF_ENTRY RhpGcStressHijack, _TEXT
    // NYI
    EMIT_BREAKPOINT
LEAF_END RhpGcStressHijack, _TEXT

#endif  // FEATURE_GC_STRESS
