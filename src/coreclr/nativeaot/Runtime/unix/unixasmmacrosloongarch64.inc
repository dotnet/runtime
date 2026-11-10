// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmOffsets.inc"

#include <loongarch64/AsmMacros_Unix.inc>

// Inlined version of RhpGetThread. Target cannot be a0.
.macro INLINE_GETTHREAD target
    INLINE_GET_TLS_VAR  \target, C_FUNC(tls_CurrentThread)
.endm

// Target cannot be a0.
.macro INLINE_GET_ALLOC_CONTEXT_BASE target
    INLINE_GET_TLS_VAR  \target, C_FUNC(tls_CurrentThread)
.endm

.macro INLINE_THREAD_UNHIJACK threadReg, trashReg1, trashReg2
    //
    // Thread::Unhijack()
    //
    ld.d  \trashReg1, \threadReg, OFFSETOF__Thread__m_pvHijackedReturnAddress
    beqz  \trashReg1, 0f

    ld.d  \trashReg2, \threadReg, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation
    st.d  \trashReg1, \trashReg2, 0
    st.d  $zero, \threadReg, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation
    st.d  $zero, \threadReg, OFFSETOF__Thread__m_pvHijackedReturnAddress
0:
.endm

// Note: these must match the defs in PInvokeTransitionFrameFlags
#define PTFF_SAVE_SP            0x00000200
#define PTFF_SAVE_R4            0x00000800
#define PTFF_SAVE_R5            0x00001000
#define PTFF_SAVE_R6            0x00002000
#define PTFF_SAVE_ALL_PRESERVED 0x000001FF // NOTE: r23-r31
#define PTFF_THREAD_HIJACK      0x80000000

#define DEFAULT_FRAME_SAVE_FLAGS (PTFF_SAVE_ALL_PRESERVED + PTFF_SAVE_SP)

.macro PUSH_COOP_PINVOKE_FRAME trashReg

    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 0x70 // Push down stack pointer and store FP and RA

    // 0x10 bytes reserved for Thread* and flags

    // Save callee saved registers
    PROLOG_SAVE_REG_PAIR   23, 24, 0x20
    PROLOG_SAVE_REG_PAIR   25, 26, 0x30
    PROLOG_SAVE_REG_PAIR   27, 28, 0x40
    PROLOG_SAVE_REG_PAIR   29, 30, 0x50
    PROLOG_SAVE_REG        31,     0x60

    // Save the value of SP before stack allocation to the last slot in the frame (slot #13)
    addi.d  \trashReg, $sp, 0x70
    st.d  \trashReg, $sp, 0x68

    // Record the bitmask of saved registers in the frame (slot #3)
    ori  \trashReg, $zero, DEFAULT_FRAME_SAVE_FLAGS
    st.d  \trashReg, $sp, 0x18

    ori  \trashReg, $sp, 0
.endm

// Pop the frame and restore register state preserved by PUSH_COOP_PINVOKE_FRAME
.macro POP_COOP_PINVOKE_FRAME
    // Restore callee saved registers
    EPILOG_RESTORE_REG_PAIR  23, 24, 0x20
    EPILOG_RESTORE_REG_PAIR  25, 26, 0x30
    EPILOG_RESTORE_REG_PAIR  27, 28, 0x40
    EPILOG_RESTORE_REG_PAIR  29, 30, 0x50
    EPILOG_RESTORE_REG       31,     0x60
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0x70
.endm

//
// CONSTANTS -- INTEGER
//
#define TSF_Attached                    0x01
#define TSF_DoNotTriggerGc              0x10

// Bit position for the flags above, to be used with bstrpick.d+beq/bne instructions
#define TrapThreadsFlags_TrapThreads_Bit     0

// These must match the TrapThreadsFlags enum
#define TrapThreadsFlags_None            0
#define TrapThreadsFlags_TrapThreads     1
