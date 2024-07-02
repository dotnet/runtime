// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmOffsets.inc"

.macro NESTED_ENTRY Name, Section, Handler
        LEAF_ENTRY \Name, \Section
        .ifnc \Handler, NoHandler
        .cfi_personality 0x1b, C_FUNC(\Handler) // 0x1b == DW_EH_PE_pcrel | DW_EH_PE_sdata4
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

.macro LEAF_ENTRY Name, Section
        .global C_FUNC(\Name)
        .hidden C_FUNC(\Name)
        .type \Name, %function
C_FUNC(\Name):
        .cfi_startproc
.endm

.macro LEAF_END Name, Section
        .size \Name, .-\Name
        .cfi_endproc
.endm

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
        la.local  \HelperReg, \Name
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT Name, HelperReg
        la.local  \HelperReg, \Name
        ld.d  \HelperReg, \HelperReg, 0
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT_W Name, HelperReg
        la.local  \HelperReg, \Name
        ld.w  \HelperReg, \HelperReg, 0
.endm


.macro PROLOG_STACK_ALLOC Size
        addi.d  $sp, $sp, -\Size
.endm

.macro EPILOG_STACK_FREE Size
        addi.d  $sp, $sp, \Size
        .cfi_adjust_cfa_offset -\Size
.endm

.macro EPILOG_STACK_RESTORE
        ori  $sp, $fp, 0
        .cfi_restore  3
.endm

.macro PROLOG_SAVE_REG reg, ofs
        st.d  $r\reg, $sp, \ofs
        .cfi_rel_offset \reg, \ofs
.endm

.macro PROLOG_SAVE_REG_PAIR reg1, reg2, ofs
        st.d  $r\reg1, $sp, \ofs
        st.d  $r\reg2, $sp, \ofs + 8
        .cfi_rel_offset  \reg1, \ofs
        .cfi_rel_offset  \reg2, \ofs + 8
        .ifc  \reg1, $fp
        ori  $fp, $sp, 0
        .cfi_def_cfa_register  22
        .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_INDEXED reg1, reg2, ssize, __def_cfa_save=1
        addi.d  $sp, $sp, -\ssize
        //.cfi_adjust_cfa_offset  \ssize
        .cfi_def_cfa  3, \ssize

        st.d  $r\reg1, $sp, 0
        st.d  $r\reg2, $sp, 8

        .cfi_rel_offset  \reg1, 0
        .cfi_rel_offset  \reg2, 8
        .if (\__def_cfa_save ==  1)
          ori  $fp, $sp, 0
          .cfi_def_cfa_register  22
        .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED reg1, reg2, ssize
        addi.d  $sp, $sp, -\ssize
        //.cfi_adjust_cfa_offset  \ssize
        .cfi_def_cfa  3, \ssize

        st.d  $r\reg1, $sp, 0
        st.d  $r\reg2, $sp, 8

        .cfi_rel_offset  \reg1, 0
        .cfi_rel_offset  \reg2, 8
.endm


.macro EPILOG_RESTORE_REG reg, ofs
        ld.d  $r\reg, $sp, \ofs
        .cfi_restore  \reg
.endm

.macro EPILOG_RESTORE_REG_PAIR reg1, reg2, ofs
        ld.d  $r\reg1, $sp, \ofs
        ld.d  $r\reg2, $sp, \ofs + 8
        .cfi_restore  \reg1
        .cfi_restore  \reg2
.endm

.macro EPILOG_RESTORE_REG_PAIR_INDEXED reg1, reg2, ofs
        ld.d  $r\reg1, $sp, 0
        ld.d  $r\reg2, $sp, 8
        addi.d  $sp, $sp, \ofs
        .cfi_restore  \reg1
        .cfi_restore  \reg2
        .cfi_adjust_cfa_offset  -\ofs
.endm

.macro EPILOG_RETURN
        jirl  $r0, $ra, 0
.endm

.macro EMIT_BREAKPOINT
        break  0
.endm

.macro EPILOG_BRANCH_REG reg

        jirl  $r0, \reg, 0

.endm

// Loads the address of a thread-local variable into the target register,
// which cannot be a0. Preserves all other registers.
.macro INLINE_GET_TLS_VAR target, var
    .ifc  \target, $a0
        .error "target cannot be a0"
    .endif

    st.d  $a0, $sp, -0x10
    st.d  $ra, $sp, -0x8
    addi.d  $sp, $sp, -16

    // This sequence of instructions is recognized and potentially patched
    // by the linker (GD->IE/LE relaxation).
    //la.local  $a0, \var                      //
    //ld.d  \target, $a0, 0                    //
    //.tlsdesccall \var                        //TODO-LOONGARCH64
    la.tls.ie   $a0, \var
    //jirl  $ra, \target, 0
    // End of the sequence

    ori  \target, $tp, 0
    add.d  \target, \target, $a0

    ld.d  $a0, $sp, 0
    ld.d  $ra, $sp, 8
    addi.d $sp, $sp, 16
.endm

// Inlined version of RhpGetThread. Target cannot be a0.
.macro INLINE_GETTHREAD target
    INLINE_GET_TLS_VAR  \target, C_FUNC(tls_CurrentThread)
.endm

// Do not use these ETLS macros in functions that already create a stack frame.
// Creating two stack frames in one function can confuse the unwinder/debugger

.macro GETTHREAD_ETLS_1
    PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, 32          // ;; Push down stack pointer and store FP and RA
    st.d  $a0, $sp, 0x10

    bl  C_FUNC(RhpGetThread)
    ori  $a1, $a0, 0

    ld.d  $a0, $sp, 0x10
    EPILOG_RESTORE_REG_PAIR_INDEXED   22, 1, 32
.endm

.macro GETTHREAD_ETLS_2
    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 32           // ;; Push down stack pointer and store FP and RA
    st.d  $a0, $sp, 0x10
    st.d  $a1, $sp, 0x18

    bl  C_FUNC(RhpGetThread)
    ori  $a2, $a0, 0

    ld.d  $a0, $sp, 0x10
    ld.d  $a1, $sp, 0x18
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 32
.endm

.macro GETTHREAD_ETLS_3
    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 48           // ;; Push down stack pointer and store FP and RA
    st.d  $a0, $sp, 0x10
    st.d  $a1, $sp, 0x18
    st.d  $a2, $sp, 0x20

    bl  C_FUNC(RhpGetThread)
    ori  $a3, $a0, 0

    ld.d  $a0, $sp, 0x10
    ld.d  $a1, $sp, 0x18
    ld.d  $a2, $sp, 0x20
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 48
.endm

.macro GETTHUNKDATA_ETLS_9
    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 96           // ;; Push down stack pointer and store FP and RA
    st.d  $a0, $sp, 0x10
    st.d  $a1, $sp, 0x18
    st.d  $a2, $sp, 0x20
    st.d  $a3, $sp, 0x28
    st.d  $a4, $sp, 0x30
    st.d  $a5, $sp, 0x38
    st.d  $a6, $sp, 0x40
    st.d  $a7, $sp, 0x48
    st.d  $t6, $sp, 0x50
    st.d  $t7, $sp, 0x58

    bl  RhpGetThunkData
    ori  $t0, $a0, 0

    ld.d  $a0, $sp, 0x10
    ld.d  $a1, $sp, 0x18
    ld.d  $a2, $sp, 0x20
    ld.d  $a3, $sp, 0x28
    ld.d  $a4, $sp, 0x30
    ld.d  $a5, $sp, 0x38
    ld.d  $a6, $sp, 0x40
    ld.d  $a7, $sp, 0x48
    ld.d  $t6, $sp, 0x50
    ld.d  $t7, $sp, 0x58
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 96
.endm

.macro  InterlockedOperationBarrier
    dbar  0
.endm

.macro INLINE_THREAD_UNHIJACK threadReg, trashReg1, trashReg2
    //
    // Thread::Unhijack()
    //
    ld.d  \trashReg1, \threadReg, OFFSETOF__Thread__m_pvHijackedReturnAddress
    beq  \trashReg1, $zero, 0f

    ld.d  \trashReg2, \threadReg, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation
    st.d  \trashReg1, \trashReg2, 0
    st.d  $zero, \threadReg, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation
    st.d  $zero, \threadReg, OFFSETOF__Thread__m_pvHijackedReturnAddress
0:
.endm

// Note: these must match the defs in PInvokeTransitionFrameFlags
PTFF_SAVE_SP            = 0x00000200
PTFF_SAVE_R4            = 0x00001000
PTFF_SAVE_R5            = 0x00002000
PTFF_SAVE_ALL_PRESERVED = 0x000001FF // NOTE: r23-r31

DEFAULT_FRAME_SAVE_FLAGS = PTFF_SAVE_ALL_PRESERVED + PTFF_SAVE_SP

.macro PUSH_COOP_PINVOKE_FRAME trashReg

    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 0x80 // Push down stack pointer and store FP and RA

    // 0x10 bytes reserved for Thread* and flags

    // Save callee saved registers
    PROLOG_SAVE_REG_PAIR   23, 24, 0x20
    PROLOG_SAVE_REG_PAIR   25, 26, 0x30
    PROLOG_SAVE_REG_PAIR   27, 28, 0x40
    PROLOG_SAVE_REG_PAIR   29, 30, 0x50
    PROLOG_SAVE_REG_PAIR   31,  2, 0x60

    // Save the value of SP before stack allocation to the last slot in the frame (slot #15)
    addi.d  \trashReg, $sp, 0x80
    st.d  \trashReg, $sp, 0x70

    // Record the bitmask of saved registers in the frame (slot #3)
    ori  \trashReg, $zero, DEFAULT_FRAME_SAVE_FLAGS
    st.d  \trashReg, $sp, 0x18

    ori  \trashReg, $sp, 0
.endm

// Pop the frame and restore register state preserved by PUSH_COOP_PINVOKE_FRAME
.macro POP_COOP_PINVOKE_FRAME

    //                      $s0,$s1
    EPILOG_RESTORE_REG_PAIR  23, 24, 0x20
    EPILOG_RESTORE_REG_PAIR  25, 26, 0x30
    EPILOG_RESTORE_REG_PAIR  27, 28, 0x40
    EPILOG_RESTORE_REG_PAIR  29, 30, 0x50
    EPILOG_RESTORE_REG_PAIR  31,  2, 0x60
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0x80
.endm

// Bit position for the flags above, to be used with andi+beq/bne instructions
PTFF_THREAD_ABORT_BIT = 36

//
// CONSTANTS -- INTEGER
//
#define TSF_Attached                    0x01
#define TSF_SuppressGcStress            0x08
#define TSF_DoNotTriggerGc              0x10
#define TSF_SuppressGcStress__OR__TSF_DoNotTriggerGC 0x18

// Bit position for the flags above, to be used with andi+beq/bne instructions
TrapThreadsFlags_AbortInProgress_Bit = 0
TrapThreadsFlags_TrapThreads_Bit     = 1

// These must match the TrapThreadsFlags enum
#define TrapThreadsFlags_None            0
#define TrapThreadsFlags_AbortInProgress 1
#define TrapThreadsFlags_TrapThreads     2
