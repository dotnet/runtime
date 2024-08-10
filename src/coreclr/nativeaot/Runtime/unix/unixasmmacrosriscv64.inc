// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmOffsets.inc"

.macro NESTED_ENTRY Name, Section, Handler
        LEAF_ENTRY \Name, \Section
        .ifnc \Handler, NoHandler
        .cfi_personality 0x1b, C_FUNC(\Handler) // 0x1b == DW_EH_PE_pcrel | DW_EH_PE_sdata4 (standard across most platforms)
        .endif
.endm

.macro NESTED_END Name, Section
        LEAF_END \Name, \Section
.endm

.macro PATCH_LABEL Name
        .global C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro ALTERNATE_ENTRY Name
        .global C_FUNC(\Name)
        .hidden C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro LABELED_RETURN_ADDRESS Name
        .global C_FUNC(\Name)
        .hidden C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro LEAF_ENTRY Name, Section
        .global C_FUNC(\Name)
        .hidden C_FUNC(\Name)
        .type \Name, @function
        .section \Section
        .align 2
C_FUNC(\Name):
        .cfi_startproc
.endm

.macro LEAF_END Name, Section
        .size \Name, .-\Name
        .cfi_endproc
.endm

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
        lui \HelperReg, %hi(C_FUNC(\Name))
        addi \HelperReg, \HelperReg, %lo(C_FUNC(\Name))
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT Name, HelperReg
        lui \HelperReg, %hi(C_FUNC(\Name))
        ld  \HelperReg, %lo(C_FUNC(\Name))(\HelperReg)
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT_W Name, HelperReg
        lui \HelperReg, %hi(C_FUNC(\Name))
        lw  \HelperReg, %lo(C_FUNC(\Name))(\HelperReg)
.endm

.macro PROLOG_STACK_ALLOC Size
        addi sp, sp, -\Size
.endm

.macro EPILOG_STACK_FREE Size
        addi sp, sp, \Size
        .cfi_adjust_cfa_offset -\Size
.endm

.macro EPILOG_STACK_RESTORE
        mv sp, s0
        .cfi_restore sp
.endm

.macro PROLOG_SAVE_REG reg, ofs
        sd \reg, \ofs(sp)
        .cfi_rel_offset \reg, \ofs
.endm

.macro PROLOG_SAVE_REG_PAIR reg1, reg2, ofs
        sd \reg1, \ofs(sp)
        sd \reg2, \ofs+8(sp)
        .cfi_rel_offset \reg1, \ofs
        .cfi_rel_offset \reg2, \ofs+8
        .ifc \reg1, s0
        mv s0, sp
        .cfi_def_cfa_register s0
        .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_INDEXED reg1, reg2, ofs
        sd \reg1, -\ofs(sp)
        sd \reg2, -\ofs+8(sp)
        addi sp, sp, -\ofs
        .cfi_adjust_cfa_offset \ofs
        .cfi_rel_offset \reg1, 0
        .cfi_rel_offset \reg2, 8
        .ifc \reg1, s0
        mv s0, sp
        .cfi_def_cfa_register s0
        .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED reg1, reg2, ofs
        sd \reg1, \ofs(sp)
        sd \reg2, \ofs+8(sp)
        addi sp, sp, -\ofs
        .cfi_adjust_cfa_offset \ofs
        .cfi_rel_offset \reg1, 0
        .cfi_rel_offset \reg2, 8
.endm

.macro EPILOG_RESTORE_REG reg, ofs
        ld \reg, \ofs(sp)
        .cfi_restore \reg
.endm

.macro EPILOG_RESTORE_REG_PAIR reg1, reg2, ofs
        ld \reg1, \ofs(sp)
        ld \reg2, \ofs+8(sp)
        .cfi_restore \reg1
        .cfi_restore \reg2
.endm

.macro EPILOG_RESTORE_REG_PAIR_INDEXED reg1, reg2, ofs
        ld \reg1, (sp)
        ld \reg2, 8(sp)
        addi sp, sp, \ofs
        .cfi_restore \reg1
        .cfi_restore \reg2
        .cfi_adjust_cfa_offset -\ofs
.endm

.macro EPILOG_RETURN
        jalr x0, ra
.endm

.macro EMIT_BREAKPOINT
        ebreak
.endm

.macro EPILOG_BRANCH_REG reg
        jalr \reg
.endm

// Loads the address of a thread-local variable into the target register,
// which cannot be x0. Preserves all other registers.
.macro INLINE_GET_TLS_VAR target, var
    .ifc \target, x0
        .error "target cannot be x0"
    .endif

    sd    x0, -16(sp)
    sd    ra, -8(sp)
    
    // RISC-V does not have a direct equivalent to Apple's or GNU's TLS
    // handling, so we'll use an indirect approach and inline assembly 
    // if needed.
    lui   t0, %hi(\var)
    addi  t0, t0, %lo(\var)
    ld    \target, 0(t0)
    
    // This sequence is a placeholder; actual TLS handling may require
    // platform-specific instructions or further customization.
    
    ld    ra, -8(sp)
    ld    x0, -16(sp)
.endm
// Inlined version of RhpGetThread. Target cannot be x0.
.macro INLINE_GETTHREAD target
    INLINE_GET_TLS_VAR \target, C_FUNC(tls_CurrentThread)
.endm

// Do not use these ETLS macros in functions that already create a stack frame.
// Creating two stack frames in one function can confuse the unwinder/debugger

.macro GETTHREAD_ETLS_1
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, ra, -32           // Push down stack pointer and store FP and RA
    sd x0,  16(sp)

    call C_FUNC(RhpGetThread)
    mv x1, x0

    ld x0,  16(sp)
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, ra, 32
.endm

.macro GETTHREAD_ETLS_2
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, ra, -32           // Push down stack pointer and store FP and RA
    sd x0,  16(sp)
    sd x1,  24(sp)

    call C_FUNC(RhpGetThread)
    mv x2, x0

    ld x0,  16(sp)
    ld x1,  24(sp)
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, ra, 32
.endm

.macro GETTHREAD_ETLS_3
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, ra, -48           // Push down stack pointer and store FP and RA
    sd x0,  16(sp)
    sd x1,  24(sp)
    sd x2,  32(sp)

    call C_FUNC(RhpGetThread)
    mv x3, x0

    ld x0,  16(sp)
    ld x1,  24(sp)
    ld x2,  32(sp)
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, ra, 48
.endm

.macro GETTHUNKDATA_ETLS_9
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, ra, -96           // Push down stack pointer and store FP and RA
    sd x0,  16(sp)
    sd x1,  24(sp)
    sd x2,  32(sp)
    sd x3,  40(sp)
    sd x4,  48(sp)
    sd x5,  56(sp)
    sd x6,  64(sp)
    sd x7,  72(sp)
    sd x8,  80(sp)
    sd x9,  88(sp)

    call C_FUNC(RhpGetThunkData)
    mv x10, x0

    ld x0,  16(sp)
    ld x1,  24(sp)
    ld x2,  32(sp)
    ld x3,  40(sp)
    ld x4,  48(sp)
    ld x5,  56(sp)
    ld x6,  64(sp)
    ld x7,  72(sp)
    ld x8,  80(sp)
    ld x9,  88(sp)
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, ra, 96
.endm

.macro InterlockedOperationBarrier
    fence rw, rw
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
PTFF_SAVE_SP            = 0x00000400
PTFF_SAVE_X0            = 0x00000800
PTFF_SAVE_X1            = 0x00001000
PTFF_SAVE_ALL_PRESERVED = 0x000003FF  // NOTE: x19-x28

DEFAULT_FRAME_SAVE_FLAGS = PTFF_SAVE_ALL_PRESERVED + PTFF_SAVE_SP

.macro PUSH_COOP_PINVOKE_FRAME trashReg
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, ra, -0x80      // Push down stack pointer and store FP and RA

    // 0x10 bytes reserved for Thread* and flags

    // Save callee saved registers
    PROLOG_SAVE_REG_PAIR   x19, x20, 0x20
    PROLOG_SAVE_REG_PAIR   x21, x22, 0x30
    PROLOG_SAVE_REG_PAIR   x23, x24, 0x40
    PROLOG_SAVE_REG_PAIR   x25, x26, 0x50
    PROLOG_SAVE_REG_PAIR   x27, x28, 0x60

    // Save the value of SP before stack allocation to the last slot in the frame (slot #15)
    add \trashReg, sp, 0x80
    sd \trashReg, 0x70(sp)

    // Record the bitmask of saved registers in the frame (slot #3)
    li \trashReg, DEFAULT_FRAME_SAVE_FLAGS
    sd \trashReg, 0x18(sp)

    mv \trashReg, sp
.endm

// Pop the frame and restore register state preserved by PUSH_COOP_PINVOKE_FRAME
.macro POP_COOP_PINVOKE_FRAME
    EPILOG_RESTORE_REG_PAIR   x19, x20, 0x20
    EPILOG_RESTORE_REG_PAIR   x21, x22, 0x30
    EPILOG_RESTORE_REG_PAIR   x23, x24, 0x40
    EPILOG_RESTORE_REG_PAIR   x25, x26, 0x50
    EPILOG_RESTORE_REG_PAIR   x27, x28, 0x60
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, ra, 0x80
.endm

// Bit position for the flags above, to be used with tbz / tbnz instructions
PTFF_THREAD_ABORT_BIT = 36

//
// CONSTANTS -- INTEGER
//
#define TSF_Attached                    0x01
#define TSF_SuppressGcStress            0x08
#define TSF_DoNotTriggerGc              0x10
#define TSF_SuppressGcStress__OR__TSF_DoNotTriggerGC 0x18

// Bit position for the flags above, to be used with tbz / tbnz instructions
TrapThreadsFlags_AbortInProgress_Bit = 0
TrapThreadsFlags_TrapThreads_Bit     = 1

// These must match the TrapThreadsFlags enum
#define TrapThreadsFlags_None            0
#define TrapThreadsFlags_AbortInProgress 1
#define TrapThreadsFlags_TrapThreads     2
