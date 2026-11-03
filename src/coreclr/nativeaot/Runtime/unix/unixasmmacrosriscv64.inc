// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmOffsets.inc"

#include <riscv64/AsmMacros_Unix.inc>

// Inlined version of RhpGetThread. Target cannot be x0.
.macro INLINE_GETTHREAD target
    INLINE_GET_TLS_VAR \target, C_FUNC(tls_CurrentThread)
.endm

// Caller must have an established frame, trashes volatile registers
.macro INLINE_GET_ALLOC_CONTEXT_BASE
    // global dynamic TLS, see https://github.com/riscv-non-isa/riscv-elf-psabi-doc/blob/eb2b2962/riscv-elf.adoc#global-dynamic
    la.tls.gd a0, C_FUNC(tls_CurrentThread)
    call C_FUNC(__tls_get_addr)
.endm

.macro INLINE_THREAD_UNHIJACK threadReg, trashReg1, trashReg2
    //
    // Thread::Unhijack()
    //
    ld \trashReg1, OFFSETOF__Thread__m_pvHijackedReturnAddress(\threadReg)
    beqz \trashReg1, 0f

    ld \trashReg2, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(\threadReg)
    sd \trashReg1, 0(\trashReg2)
    sd zero, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(\threadReg)
    sd zero, OFFSETOF__Thread__m_pvHijackedReturnAddress(\threadReg)
0:
.endm

// Note: these must match the defs in PInvokeTransitionFrameFlags
#define PTFF_SAVE_SP            0x00000800
#define PTFF_SAVE_A0            0x00004000
#define PTFF_SAVE_A1            0x00008000
#define PTFF_SAVE_A2            0x00010000
#define PTFF_SAVE_ALL_PRESERVED 0x000007FF  // NOTE: S1-S11
#define PTFF_THREAD_HIJACK      0x80000000

#define DEFAULT_FRAME_SAVE_FLAGS PTFF_SAVE_ALL_PRESERVED + PTFF_SAVE_SP

.macro PUSH_COOP_PINVOKE_FRAME trashReg
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, ra, 128      // Push down stack pointer and store FP and RA

    // 16 bytes reserved for Thread* and flags

    // Save callee saved registers
    PROLOG_SAVE_REG_PAIR   s1, s2, 32
    PROLOG_SAVE_REG_PAIR   s3, s4, 48
    PROLOG_SAVE_REG_PAIR   s5, s6, 64
    PROLOG_SAVE_REG_PAIR   s7, s8, 80
    PROLOG_SAVE_REG_PAIR   s9, s10, 96
    PROLOG_SAVE_REG        s11, 112

    // Save the value of SP before stack allocation to the last slot in the frame (slot #15)
    add \trashReg, sp, 128
    sd \trashReg, 120(sp)

    // Record the bitmask of saved registers in the frame (slot #3)
    li \trashReg, DEFAULT_FRAME_SAVE_FLAGS
    sd \trashReg, 24(sp)

    mv \trashReg, sp
.endm

// Pop the frame and restore register state preserved by PUSH_COOP_PINVOKE_FRAME
.macro POP_COOP_PINVOKE_FRAME
    EPILOG_RESTORE_REG_PAIR   s1, s2, 32
    EPILOG_RESTORE_REG_PAIR   s3, s4, 48
    EPILOG_RESTORE_REG_PAIR   s5, s6, 64
    EPILOG_RESTORE_REG_PAIR   s7, s8, 80
    EPILOG_RESTORE_REG_PAIR   s9, s10, 96
    EPILOG_RESTORE_REG        s11, 112
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, ra, 128
.endm

//
// CONSTANTS -- INTEGER
//
#define TSF_Attached                    0x01
#define TSF_DoNotTriggerGc              0x10

// Bit position for the flags above, to be used with tbz / tbnz instructions
#define TrapThreadsFlags_TrapThreads_Bit     0

// These must match the TrapThreadsFlags enum
#define TrapThreadsFlags_None            0
#define TrapThreadsFlags_TrapThreads     1
