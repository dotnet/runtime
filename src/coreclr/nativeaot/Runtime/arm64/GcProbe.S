// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

#define PROBE_FRAME_SIZE 0x140  // 4 * 8  for fixed part of PInvokeTransitionFrame (fp, lr, m_pThread, m_Flags) +
                                // 10 * 8 for callee saved registers +
                                // 1 * 8  for caller SP +
                                // 8 * 8  for int return/argument regs (x0..x7) +
                                // 1 * 8  for padding (16-byte alignment for q regs) +
                                // 8 * 16 for FP argument registers (q0..q7)

// See PUSH_COOP_PINVOKE_FRAME, this macro is very similar, but also saves return/argument registers
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

    // Save the integer return/argument registers
    stp         x0, x1,   [sp, #0x78]
    stp         x2, x3,   [sp, #0x88]
    stp         x4, x5,   [sp, #0x98]
    stp         x6, x7,   [sp, #0xA8]

    // Save the FP argument registers
    stp         q0, q1,   [sp, #0xC0]
    stp         q2, q3,   [sp, #0xE0]
    stp         q4, q5,   [sp, #0x100]
    stp         q6, q7,   [sp, #0x120]

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

    // Restore the integer return/argument registers
    ldp         x0, x1,   [sp, #0x78]
    ldp         x2, x3,   [sp, #0x88]
    ldp         x4, x5,   [sp, #0x98]
    ldp         x6, x7,   [sp, #0xA8]

    // Restore the FP argument registers
    ldp         q0, q1,   [sp, #0xC0]
    ldp         q2, q3,   [sp, #0xE0]
    ldp         q4, q5,   [sp, #0x100]
    ldp         q6, q7,   [sp, #0x120]

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
//  x9: thread pointer
//  x0-x7, q0-q7 preserved, x10 trashed
//
.macro FixupHijackedCallstack

    // x9 <- GetThread()

#ifdef FEATURE_EMULATED_TLS
    // Save x0-x7 and q0-q7 around RhpGetThread call (it trashes all scratch registers)
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -0xD0
    stp         x0, x1, [sp, #0x10]
    stp         x2, x3, [sp, #0x20]
    stp         x4, x5, [sp, #0x30]
    stp         x6, x7, [sp, #0x40]
    stp         q0, q1, [sp, #0x50]
    stp         q2, q3, [sp, #0x70]
    stp         q4, q5, [sp, #0x90]
    stp         q6, q7, [sp, #0xB0]

    bl C_FUNC(RhpGetThread)
    mov x9, x0

    ldp         q6, q7, [sp, #0xB0]
    ldp         q4, q5, [sp, #0x90]
    ldp         q2, q3, [sp, #0x70]
    ldp         q0, q1, [sp, #0x50]
    ldp         x0, x1, [sp, #0x10]
    ldp         x2, x3, [sp, #0x20]
    ldp         x4, x5, [sp, #0x30]
    ldp         x6, x7, [sp, #0x40]
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr, 0xD0
#else
    // INLINE_GETTHREAD makes blr calls that trash volatile FP registers.
    // Save and restore q0-q7 around the call.
    sub         sp, sp, #0x80
    stp         q0, q1, [sp, #0x00]
    stp         q2, q3, [sp, #0x20]
    stp         q4, q5, [sp, #0x40]
    stp         q6, q7, [sp, #0x60]

    INLINE_GETTHREAD x9

    ldp         q6, q7, [sp, #0x60]
    ldp         q4, q5, [sp, #0x40]
    ldp         q2, q3, [sp, #0x20]
    ldp         q0, q1, [sp, #0x00]
    add         sp, sp, #0x80
#endif

    //
    // Fix the stack by restoring the original return address
    //
    ldr         lr, [x9, #OFFSETOF__Thread__m_pvHijackedReturnAddress]

    //
    // Clear hijack state
    //
    // Clear m_ppvHijackedReturnAddressLocation and m_pvHijackedReturnAddress
    stp         xzr, xzr, [x9, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
.endm

//
// GC Probe Hijack target
//
NESTED_ENTRY RhpGcProbeHijack, _TEXT, NoHandler
    FixupHijackedCallstack

    PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, 10
    tbnz        x10, #TrapThreadsFlags_TrapThreads_Bit, LOCAL_LABEL(WaitForGC)
    ret

LOCAL_LABEL(WaitForGC):
    mov         x12, DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_X0 + PTFF_SAVE_X1 + PTFF_SAVE_X2 + PTFF_SAVE_X3 + PTFF_SAVE_X4 + PTFF_SAVE_X5 + PTFF_SAVE_X6 + PTFF_SAVE_X7
    movk        x12, PTFF_THREAD_HIJACK_HI, lsl #32
    b           C_FUNC(RhpWaitForGC)
NESTED_END RhpGcProbeHijack

.global C_FUNC(RhpThrowHwEx)

NESTED_ENTRY RhpWaitForGC, _TEXT, NoHandler
    PUSH_PROBE_FRAME x9, x10, x12

    ldr         x0, [x9, #OFFSETOF__Thread__m_pDeferredTransitionFrame]
    bl          C_FUNC(RhpWaitForGC2)

    POP_PROBE_FRAME
    EPILOG_RETURN
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
