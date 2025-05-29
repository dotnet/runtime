// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

#define PROBE_FRAME_SIZE 0xB0  // 4 * 8  for fixed part of PInvokeTransitionFrame (fp, ra, m_pThread, m_Flags) +
                               // 11 * 8 for callee saved registers +
                               // 1 * 8  for caller SP +
                               // 2 * 8  for int returns +
                               // 4 * 8  for FP returns

// Define the prolog for setting up the PInvokeTransitionFrame
.macro PUSH_PROBE_FRAME threadReg, trashReg, BITMASK

    # Define the method prologue, allocating enough stack space for the PInvokeTransitionFrame and saving
    # incoming register values into it.

    # First create PInvokeTransitionFrame
    PROLOG_SAVE_REG_PAIR_INDEXED  fp, ra, PROBE_FRAME_SIZE  # Push down stack pointer and store FP (s10) and RA (ra)

    # Slot at sp+0x10 is reserved for Thread *
    # Slot at sp+0x18 is reserved for bitmask of saved registers

    # Save callee-saved registers
    PROLOG_SAVE_REG_PAIR  s1, s2,  0x20
    PROLOG_SAVE_REG_PAIR  s3, s4,  0x30
    PROLOG_SAVE_REG_PAIR  s5, s6,  0x40
    PROLOG_SAVE_REG_PAIR  s7, s8,  0x50
    PROLOG_SAVE_REG_PAIR  s9, s10, 0x60
    PROLOG_SAVE_REG       s11,     0x70

    # Slot at sp+0x78 is reserved for caller sp

    # Save the integer return registers
    sd  a0, 0x80(sp)
    sd  a1, 0x88(sp)

    # Save the FP return registers
    fsd  fa0, 0x90(sp)
    fsd  fa1, 0x98(sp)
    fsd  fa2, 0xa0(sp)
    fsd  fa3, 0xa8(sp)

    # Perform the rest of the PInvokeTransitionFrame initialization.
    sd  \threadReg, OFFSETOF__PInvokeTransitionFrame__m_pThread(sp)        # Thread * (unused by stackwalker)
    sd  \BITMASK, OFFSETOF__PInvokeTransitionFrame__m_Flags(sp)            # Save the register bitmask passed in by caller

    addi  \trashReg, sp, PROBE_FRAME_SIZE                                  # Recover value of caller's SP
    sd  \trashReg, 0x78(sp)                                                # Save caller's SP

    # Link the frame into the Thread
    sd  sp, OFFSETOF__Thread__m_pDeferredTransitionFrame(\threadReg)

.endm

// Define the prolog for removing the PInvokeTransitionFrame
.macro POP_PROBE_FRAME

    // Restore the integer return registers
    ld  a0, 0x80(sp)
    ld  a1, 0x88(sp)

    // Restore the FP return registers
    fld  fa0, 0x90(sp)
    fld  fa1, 0x98(sp)
    fld  fa2, 0xa0(sp)
    fld  fa3, 0xa8(sp)

    // Restore callee saved registers
    EPILOG_RESTORE_REG_PAIR s1, s2,  0x20
    EPILOG_RESTORE_REG_PAIR s3, s4,  0x30
    EPILOG_RESTORE_REG_PAIR s5, s6,  0x40
    EPILOG_RESTORE_REG_PAIR s7, s8,  0x50
    EPILOG_RESTORE_REG_PAIR s9, s10, 0x60
    EPILOG_RESTORE_REG      s11,     0x70

    // Restore the frame pointer and return address
    EPILOG_RESTORE_REG_PAIR_INDEXED  fp, ra, PROBE_FRAME_SIZE
.endm

// Fix up the hijacked callstack
.macro FixupHijackedCallstack

    // a2 <- GetThread()
    mv  t1, a0
    INLINE_GETTHREAD a2
    mv  a0, t1

    // Fix the stack by restoring the original return address
    ld  ra, OFFSETOF__Thread__m_pvHijackedReturnAddress(a2)

    // Clear hijack state
    sd  zero, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(a2)
    sd  zero, OFFSETOF__Thread__m_pvHijackedReturnAddress(a2)
.endm

//
// GC Probe Hijack target
//
NESTED_ENTRY RhpGcProbeHijack, _TEXT, NoHandler
    FixupHijackedCallstack

    PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, t3
    andi  t3, t3, 1 << TrapThreadsFlags_TrapThreads_Bit
    bnez  t3, LOCAL_LABEL(WaitForGC)
    jr  ra

LOCAL_LABEL(WaitForGC):
    li  t3, (DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_A0 + PTFF_SAVE_A1 + (PTFF_THREAD_HIJACK_HI << 32))
    tail  C_FUNC(RhpWaitForGC)
NESTED_END RhpGcProbeHijack

.global C_FUNC(RhpThrowHwEx)

// Wait for GC function
NESTED_ENTRY RhpWaitForGC, _TEXT, NoHandler
    PUSH_PROBE_FRAME a2, a3, t3

    ld  a0, OFFSETOF__Thread__m_pDeferredTransitionFrame(a2)
    call  C_FUNC(RhpWaitForGC2)

    ld  a2, OFFSETOF__PInvokeTransitionFrame__m_Flags(sp)

    # Load PTFF_THREAD_ABORT_BIT into a register, using t2 if needed
    slli  t2, a2, (63-PTFF_THREAD_ABORT_BIT)
    srli  t2, t2, 63
    bnez  t2, LOCAL_LABEL(ThrowThreadAbort)

    .cfi_remember_state
    POP_PROBE_FRAME
    EPILOG_RETURN

    .cfi_restore_state
LOCAL_LABEL(ThrowThreadAbort):
    POP_PROBE_FRAME
    li  a0, STATUS_REDHAWK_THREAD_ABORT
    mv  a1, ra      # Set return address as exception PC
    call  C_FUNC(RhpThrowHwEx)
NESTED_END RhpWaitForGC

.global C_FUNC(RhpGcPoll2)

// GC Poll function
LEAF_ENTRY RhpGcPoll
    PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, a0
    bne   a0, zero, C_FUNC(RhpGcPollRare)
    jr  ra
LEAF_END RhpGcPoll

// Rare GC Poll function
NESTED_ENTRY RhpGcPollRare, _TEXT, NoHandler
    PUSH_COOP_PINVOKE_FRAME a0
    call  RhpGcPoll2
    POP_COOP_PINVOKE_FRAME
    jr  ra
NESTED_END RhpGcPollRare

#ifdef FEATURE_GC_STRESS

// GC Stress Hijack targets
LEAF_ENTRY RhpGcStressHijack, _TEXT
    // Not Yet Implemented (NYI)
    EMIT_BREAKPOINT
LEAF_END RhpGcStressHijack, _TEXT

#endif  // FEATURE_GC_STRESS
