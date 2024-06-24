// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

PROBE_FRAME_SIZE = 0xD0  // 4 * 8  for fixed part of PInvokeTransitionFrame (fp, ra, m_pThread, m_Flags) +
                         // 10 * 8 for callee saved registers +
                         // 1 * 8  for caller SP +
                         // 2 * 8  for int returns +
                         // 1 * 8  for alignment padding +
                         // 4 * 16 for FP returns

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
    PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, -PROBE_FRAME_SIZE // Push down stack pointer and store FP and RA

    // Slot at $sp+0x10 is reserved for Thread *
    // Slot at $sp+0x18 is reserved for bitmask of saved registers

    // Save callee saved registers
    PROLOG_SAVE_REG_PAIR   23, 24, 0x20
    PROLOG_SAVE_REG_PAIR   25, 26, 0x30
    PROLOG_SAVE_REG_PAIR   27, 28, 0x40
    PROLOG_SAVE_REG_PAIR   29, 30, 0x50
    PROLOG_SAVE_REG_PAIR   31,  2, 0x60

    // Slot at $sp+0x70 is reserved for caller sp

    // Save the integer return registers
    st.d  $a0, $sp, 0x78
    st.d  $a1, $sp, 0x80

    // Slot at $sp+0x88 is alignment padding

    // Save the FP return registers
    fst.d  $f0, $sp, 0x90
    fst.d  $f1, $sp, 0x98
    fst.d  $f2, $sp, 0xA0
    fst.d  $f3, $sp, 0xA8

    // Perform the rest of the PInvokeTransitionFrame initialization.
    st.d  \threadReg, $sp, OFFSETOF__PInvokeTransitionFrame__m_pThread   // Thread * (unused by stackwalker)
    st.d  \BITMASK, $sp, OFFSETOF__PInvokeTransitionFrame__m_pThread + 8 // save the register bitmask passed in by caller

    addi.d  \trashReg, $sp,  PROBE_FRAME_SIZE                            // recover value of caller's SP
    st.d  \trashReg, $sp, 0x70                                           // save caller's SP

    // link the frame into the Thread
    ori  \trashReg, $sp, 0
    st.d  \trashReg, \threadReg, OFFSETOF__Thread__m_pDeferredTransitionFrame
.endm

//
// Remove the frame from a previous call to PUSH_PROBE_FRAME from the top of the stack and restore preserved
// registers and return value to their values from before the probe was called (while also updating any
// object refs or byrefs).
//
.macro POP_PROBE_FRAME

    // Restore the integer return registers
    ld.d  $a0, $sp, 0x78
    ld.d  $a1, $sp, 0x80

    // Restore the FP return registers
    fld.d  $f0, $sp, 0x90
    fld.d  $f1, $sp, 0x98
    fld.d  $f2, $sp, 0xA0
    fld.d  $f3, $sp, 0xA8

    // Restore callee saved registers
    EPILOG_RESTORE_REG_PAIR 23, 24, 0x20
    EPILOG_RESTORE_REG_PAIR 25, 26, 0x30
    EPILOG_RESTORE_REG_PAIR 27, 28, 0x40
    EPILOG_RESTORE_REG_PAIR 29, 30, 0x50
    EPILOG_RESTORE_REG_PAIR 31,  2, 0x60

    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, PROBE_FRAME_SIZE
.endm

//
// The prolog for all GC suspension hijacks (normal and stress). Fixes up the hijacked return address, and
// clears the hijack state.
//
// Register state on entry:
//  All registers correct for return to the original return address.
//
// Register state on exit:
//  a2: thread pointer
//  t3: transition frame flags for the return registers a0 and a1
//
.macro FixupHijackedCallstack

    // a2 <- GetThread()
#ifdef FEATURE_EMULATED_TLS
    GETTHREAD_ETLS_2
#else
    INLINE_GETTHREAD  $a2
#endif

    //
    // Fix the stack by restoring the original return address
    //
    // Load m_pvHijackedReturnAddress and m_uHijackedReturnValueFlags
    ld.d  $ra, $a2, OFFSETOF__Thread__m_pvHijackedReturnAddress
    ld.d  $t3, $a2, OFFSETOF__Thread__m_pvHijackedReturnAddress + 8

    //
    // Clear hijack state
    //
    // Clear m_ppvHijackedReturnAddressLocation and m_pvHijackedReturnAddress
    st.d  $zero, $a2, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation
    st.d  $zero, $a2, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation + 8
    // Clear m_uHijackedReturnValueFlags
    st.d  $zero, $a2, OFFSETOF__Thread__m_uHijackedReturnValueFlags

.endm

//
// GC Probe Hijack target
//
NESTED_ENTRY RhpGcProbeHijack, _TEXT, NoHandler
    FixupHijackedCallstack

    PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, $a3
    andi  $t8, $a3, TrapThreadsFlags_TrapThreads_Bit
    bne  $t8, $zero, WaitForGC
    jirl  $r0, $ra, 0

WaitForGC:
    lu12i.w  $t7, ((DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_R4 + PTFF_SAVE_R5) >> 12) & 0xfffff
    ori  $t7, $t7, (DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_R4 + PTFF_SAVE_R5) & 0xfff
    or  $t3, $t3, $t7
    b  C_FUNC(RhpWaitForGC)
NESTED_END RhpGcProbeHijack

.global C_FUNC(RhpThrowHwEx)

NESTED_ENTRY RhpWaitForGC, _TEXT, NoHandler
    PUSH_PROBE_FRAME $a2, $a3, $t3

    ld.d        $a0, $a2, OFFSETOF__Thread__m_pDeferredTransitionFrame
    bl  C_FUNC(RhpWaitForGC2)

    ld.d  $a2,$sp, OFFSETOF__PInvokeTransitionFrame__m_Flags
    andi  $t8, $a2, PTFF_THREAD_ABORT_BIT
    bne  $t8, $zero, ThrowThreadAbort

    .cfi_remember_state
    POP_PROBE_FRAME
    EPILOG_RETURN

    .cfi_restore_state
ThrowThreadAbort:
    POP_PROBE_FRAME
    addi.w  $a0, $zero, STATUS_REDHAWK_THREAD_ABORT
    ori  $a1, $ra, 0 // return address as exception PC
    b  RhpThrowHwEx
NESTED_END RhpWaitForGC

.global C_FUNC(RhpGcPoll2)

LEAF_ENTRY RhpGcPoll
    PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, $a0
    bne  $a0, $zero, C_FUNC(RhpGcPollRare)
    jirl  $r0, $ra, 0
LEAF_END RhpGcPoll

NESTED_ENTRY RhpGcPollRare, _TEXT, NoHandler
    PUSH_COOP_PINVOKE_FRAME  $a0
    bl  RhpGcPoll2
    POP_COOP_PINVOKE_FRAME
    jirl  $r0, $ra, 0
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
