// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

PROBE_FRAME_SIZE = 0xD0  // 4 * 8 for fixed part of PInvokeTransitionFrame (fp, ra, m_pThread, m_Flags) +
                         // 10 * 8 for callee saved registers +
                         // 1 * 8 for caller SP +
                         // 2 * 8 for int returns +
                         // 1 * 8 for alignment padding +
                         // 4 * 16 for FP returns

// Define the prolog for setting up the PInvokeTransitionFrame
.macro PUSH_PROBE_FRAME threadReg, trashReg, BITMASK

    // Save the current stack frame and registers
    PROLOG_SAVE_REG_PAIR_INDEXED 22, 1, PROBE_FRAME_SIZE

    // Save callee-saved registers
    PROLOG_SAVE_REG_PAIR 23, 24, 0x20
    PROLOG_SAVE_REG_PAIR 25, 26, 0x30
    PROLOG_SAVE_REG_PAIR 27, 28, 0x40
    PROLOG_SAVE_REG_PAIR 29, 30, 0x50
    PROLOG_SAVE_REG_PAIR 31,  2, 0x60

    // Save caller's SP
    sd    sp, 0x70(sp)

    // Save integer return registers
    sd    a0, 0x78(sp)
    sd    a1, 0x80(sp)

    // Alignment padding
    // (No need to explicitly handle alignment in RISC-V assembly if stack size is a multiple of 16 bytes)

    // Save FP return registers
    fsd   f0, 0x90(sp)
    fsd   f1, 0x98(sp)
    fsd   f2, 0xA0(sp)
    fsd   f3, 0xA8(sp)

    // Initialize the PInvokeTransitionFrame
    sd    \threadReg, OFFSETOF__PInvokeTransitionFrame__m_pThread(sp)
    sd    \BITMASK, OFFSETOF__PInvokeTransitionFrame__m_Flags(sp)

    // Save caller's SP in the PInvokeTransitionFrame
    addi  \trashReg, sp, PROBE_FRAME_SIZE
    sd    \trashReg, 0x70(sp)

    // Link the frame into the Thread
    sd    zero, OFFSETOF__Thread__m_pDeferredTransitionFrame(\threadReg)
.endm

// Define the prolog for removing the PInvokeTransitionFrame
.macro POP_PROBE_FRAME

    // Restore integer return registers
    ld    a0, 0x78(sp)
    ld    a1, 0x80(sp)

    // Restore FP return registers
    fld   f0, 0x90(sp)
    fld   f1, 0x98(sp)
    fld   f2, 0xA0(sp)
    fld   f3, 0xA8(sp)

    // Restore callee-saved registers
    EPILOG_RESTORE_REG_PAIR 23, 24, 0x20
    EPILOG_RESTORE_REG_PAIR 25, 26, 0x30
    EPILOG_RESTORE_REG_PAIR 27, 28, 0x40
    EPILOG_RESTORE_REG_PAIR 29, 30, 0x50
    EPILOG_RESTORE_REG_PAIR 31,  2, 0x60

    // Restore stack frame
    EPILOG_RESTORE_REG_PAIR_INDEXED 22, 1, PROBE_FRAME_SIZE
.endm

// Fix up the hijacked callstack
.macro FixupHijackedCallstack

    // a2 <- GetThread()
#ifdef FEATURE_EMULATED_TLS
    GETTHREAD_ETLS_2
#else
    INLINE_GETTHREAD a2
#endif

    // Fix the stack by restoring the original return address
    ld    ra, OFFSETOF__Thread__m_pvHijackedReturnAddress(a2)
    ld    t3, OFFSETOF__Thread__m_pvHijackedReturnAddress + 8(a2)

    // Clear hijack state
    sd    zero, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(a2)
    sd    zero, OFFSETOF__Thread__m_pvHijackedReturnAddress(a2)
    sd    zero, OFFSETOF__Thread__m_uHijackedReturnValueFlags(a2)
.endm

// GC Probe Hijack target
NESTED_ENTRY RhpGcProbeHijack, _TEXT, NoHandler
    FixupHijackedCallstack

    PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, a3
    andi  t8, a3, TrapThreadsFlags_TrapThreads_Bit
    bne   t8, zero, WaitForGC
    jalr  ra

WaitForGC:
    lui   t7, ((DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_R4 + PTFF_SAVE_R5) >> 12) & 0xfffff
    ori   t7, t7, (DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_R4 + PTFF_SAVE_R5) & 0xfff
    or    t3, t3, t7
    jal   C_FUNC(RhpWaitForGC)
NESTED_END RhpGcProbeHijack

.global C_FUNC(RhpThrowHwEx)

// Wait for GC function
NESTED_ENTRY RhpWaitForGC, _TEXT, NoHandler
    PUSH_PROBE_FRAME a2, a3, t3

    ld    a0, OFFSETOF__Thread__m_pDeferredTransitionFrame(a2)
    jal   C_FUNC(RhpWaitForGC2)

    ld    a2, OFFSETOF__PInvokeTransitionFrame__m_Flags(sp)
    andi  t8, a2, PTFF_THREAD_ABORT_BIT
    bne   t8, zero, ThrowThreadAbort

    .cfi_remember_state
    POP_PROBE_FRAME
    EPILOG_RETURN

    .cfi_restore_state
ThrowThreadAbort:
    POP_PROBE_FRAME
    li    a0, STATUS_REDHAWK_THREAD_ABORT
    addi  a1, ra, 0 // return address as exception PC
    jal   RhpThrowHwEx
NESTED_END RhpWaitForGC

.global C_FUNC(RhpGcPoll2)

// GC Poll function
LEAF_ENTRY RhpGcPoll
    PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, a0
    bne   a0, zero, C_FUNC(RhpGcPollRare)
    jalr  ra
LEAF_END RhpGcPoll

// Rare GC Poll function
NESTED_ENTRY RhpGcPollRare, _TEXT, NoHandler
    PUSH_COOP_PINVOKE_FRAME a0
    jal   RhpGcPoll2
    POP_COOP_PINVOKE_FRAME
    jalr  ra
NESTED_END RhpGcPollRare

#ifdef FEATURE_GC_STRESS

// GC Stress Hijack targets
LEAF_ENTRY RhpGcStressHijack, _TEXT
    // Not Yet Implemented (NYI)
    EMIT_BREAKPOINT
LEAF_END RhpGcStressHijack, _TEXT

#endif  // FEATURE_GC_STRESS
