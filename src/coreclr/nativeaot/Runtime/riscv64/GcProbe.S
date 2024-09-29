// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

#define PROBE_FRAME_SIZE 0xD0  // 4 * 8 for fixed part of PInvokeTransitionFrame (fp, ra, m_pThread, m_Flags) +
                                // 10 * 8 for callee saved registers +
                                // 1 * 8 for caller SP +
                                // 2 * 8 for int returns +
                                // 1 * 8 for alignment padding +
                                // 4 * 16 for FP returns

// Define the prolog for setting up the PInvokeTransitionFrame
.macro PUSH_PROBE_FRAME threadReg, trashReg, BITMASK

    # Define the method prologue, allocating enough stack space for the PInvokeTransitionFrame and saving
    # incoming register values into it.

    # First create PInvokeTransitionFrame
    PROLOG_SAVE_REG_PAIR_INDEXED  s10, ra, PROBE_FRAME_SIZE  # Push down stack pointer and store FP (s10) and RA (ra)

    # Slot at sp+0x10 is reserved for Thread *
    # Slot at sp+0x18 is reserved for bitmask of saved registers

    # Save callee-saved registers
    PROLOG_SAVE_REG_PAIR  s11, s0, 0x20
    PROLOG_SAVE_REG_PAIR  s1, s2, 0x30
    PROLOG_SAVE_REG_PAIR  s3, s4, 0x40
    PROLOG_SAVE_REG_PAIR  s5, s6, 0x50
    PROLOG_SAVE_REG_PAIR  s7, s8, 0x60

    # Slot at sp+0x70 is reserved for caller sp

    # Save the integer return registers
    sd a0, 0x78(sp)
    sd a1, 0x80(sp)

    # Slot at sp+0x88 is alignment padding

    # Save the FP return registers
    fsd  f0, 0x90(sp)
    fsd  f1, 0x98(sp)
    fsd  f2, 0xa0(sp)
    fsd  f3, 0xa8(sp)

    # Perform the rest of the PInvokeTransitionFrame initialization.
    sd \threadReg, OFFSETOF__PInvokeTransitionFrame__m_pThread(sp)        # Thread * (unused by stackwalker)
    sd \BITMASK, OFFSETOF__PInvokeTransitionFrame__m_pThread + 8(sp)      # Save the register bitmask passed in by caller

    addi \trashReg, sp, PROBE_FRAME_SIZE                                  # Recover value of caller's SP
    sd \trashReg, 0x70(sp)                                                # Save caller's SP

    # Link the frame into the Thread
    mv \trashReg, sp
    sd \trashReg, OFFSETOF__Thread__m_pDeferredTransitionFrame(\threadReg)

.endm

// Define the prolog for removing the PInvokeTransitionFrame
.macro POP_PROBE_FRAME

    // Restore the integer return registers
    ld a0, 0x78(sp)
    ld a1, 0x80(sp)

    // Restore the FP return registers
    fld f0, 0x90(sp)
    fld f1, 0x98(sp)
    fld f2, 0xA0(sp)
    fld f3, 0xA8(sp)

    // Restore callee saved registers
    EPILOG_RESTORE_REG_PAIR t0, t1, 0x20
    EPILOG_RESTORE_REG_PAIR t2, t3, 0x30
    EPILOG_RESTORE_REG_PAIR t4, t5, 0x40
    EPILOG_RESTORE_REG_PAIR t6, s0, 0x50
    EPILOG_RESTORE_REG_PAIR s1, s2, 0x60

    // Restore the frame pointer and return address
    EPILOG_RESTORE_REG_PAIR_INDEXED s3, ra, PROBE_FRAME_SIZE
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

//
// GC Probe Hijack target
//
NESTED_ENTRY RhpGcProbeHijack, _TEXT, NoHandler
    FixupHijackedCallstack

    PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, a3
    andi  t3, a3, TrapThreadsFlags_TrapThreads_Bit
    bnez  t3, WaitForGC
    jr  ra

WaitForGC:
    lui  t6, %hi((DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_A4 + PTFF_SAVE_A5))
    addi  t6, t6, %lo((DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_A4 + PTFF_SAVE_A5))
    or  t3, t3, t6
    j  C_FUNC(RhpWaitForGC)
NESTED_END RhpGcProbeHijack

.global C_FUNC(RhpThrowHwEx)

// Wait for GC function
NESTED_ENTRY RhpWaitForGC, _TEXT, NoHandler
    PUSH_PROBE_FRAME a2, a3, t3

    ld t0, OFFSETOF__Thread__m_pDeferredTransitionFrame(a2)
    call C_FUNC(RhpWaitForGC2)

    ld t1, OFFSETOF__PInvokeTransitionFrame__m_Flags(sp)

    # Load PTFF_THREAD_ABORT_BIT into a register, using t2 if needed
    li t2, PTFF_THREAD_ABORT_BIT
    and t3, t1, t2
    bnez t3, ThrowThreadAbort

    .cfi_remember_state
    POP_PROBE_FRAME
    EPILOG_RETURN

    .cfi_restore_state
ThrowThreadAbort:
    POP_PROBE_FRAME
    li a0, STATUS_REDHAWK_THREAD_ABORT
    mv a1, ra      # Set return address as exception PC
    j RhpThrowHwEx
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
