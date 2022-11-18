// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmOffsets.inc"

.macro NESTED_ENTRY Name, Section, Handler
        LEAF_ENTRY \Name, \Section
        .ifnc \Handler, NoHandler
#if defined(__APPLE__)
        .cfi_personality 0x9b, C_FUNC(\Handler) // 0x9b == DW_EH_PE_indirect | DW_EH_PE_pcrel | DW_EH_PE_sdata4
#else
        .cfi_personality 0x1b, C_FUNC(\Handler) // 0x1b == DW_EH_PE_pcrel | DW_EH_PE_sdata4
#endif
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
#if !defined(__APPLE__)
        .hidden C_FUNC(\Name)
#endif
C_FUNC(\Name):
.endm

.macro LABELED_RETURN_ADDRESS Name
        .global C_FUNC(\Name)
#if !defined(__APPLE__)
        .hidden C_FUNC(\Name)
#endif
C_FUNC(\Name):
.endm

.macro LEAF_ENTRY Name, Section
        .global C_FUNC(\Name)
#if defined(__APPLE__)
        .text
        .p2align        2
#else
        .hidden C_FUNC(\Name)
        .type \Name, %function
#endif
C_FUNC(\Name):
        .cfi_startproc
.endm

.macro LEAF_END Name, Section
#if !defined(__APPLE__)
        .size \Name, .-\Name
#endif
        .cfi_endproc
.endm

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
#if defined(__APPLE__)
        adrp \HelperReg, C_FUNC(\Name)@GOTPAGE
        ldr  \HelperReg, [\HelperReg, C_FUNC(\Name)@GOTPAGEOFF]
#else
        adrp \HelperReg, C_FUNC(\Name)
        add  \HelperReg, \HelperReg, :lo12:C_FUNC(\Name)
#endif
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT Name, HelperReg
#if defined(__APPLE__)
        adrp \HelperReg, C_FUNC(\Name)@GOTPAGE
        ldr  \HelperReg, [\HelperReg, C_FUNC(\Name)@GOTPAGEOFF]
        ldr  \HelperReg, [\HelperReg]
#else
        adrp \HelperReg, C_FUNC(\Name)
        ldr  \HelperReg, [\HelperReg, :lo12:C_FUNC(\Name)]
#endif
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT_W Name, HelperReg
#if defined(__APPLE__)
        adrp x\HelperReg, C_FUNC(\Name)@GOTPAGE
        ldr  x\HelperReg, [x\HelperReg, C_FUNC(\Name)@GOTPAGEOFF]
        ldr  w\HelperReg, [x\HelperReg]
#else
        adrp x\HelperReg, C_FUNC(\Name)
        ldr  w\HelperReg, [x\HelperReg, :lo12:C_FUNC(\Name)]
#endif
.endm


.macro PROLOG_STACK_ALLOC Size
        sub sp, sp, \Size
.endm

.macro EPILOG_STACK_FREE Size
        add sp, sp, \Size
        .cfi_adjust_cfa_offset -\Size
.endm

.macro EPILOG_STACK_RESTORE
        mov sp, fp
        .cfi_restore sp
.endm

.macro PROLOG_SAVE_REG reg, ofs
        str \reg, [sp, \ofs]
        .cfi_rel_offset \reg, \ofs
.endm

.macro PROLOG_SAVE_REG_PAIR reg1, reg2, ofs
        stp \reg1, \reg2, [sp, \ofs]
        .cfi_rel_offset \reg1, \ofs
        .cfi_rel_offset \reg2, \ofs + 8
        .ifc \reg1, fp
        mov fp, sp
        .cfi_def_cfa_register fp
        .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_INDEXED reg1, reg2, ofs
        stp \reg1, \reg2, [sp, \ofs]!
        .cfi_adjust_cfa_offset -\ofs
        .cfi_rel_offset \reg1, 0
        .cfi_rel_offset \reg2, 8
        .ifc \reg1, fp
        mov fp, sp
        .cfi_def_cfa_register fp
        .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED reg1, reg2, ofs
        stp \reg1, \reg2, [sp, \ofs]!
        .cfi_adjust_cfa_offset -\ofs
        .cfi_rel_offset \reg1, 0
        .cfi_rel_offset \reg2, 8
.endm


.macro EPILOG_RESTORE_REG reg, ofs
        ldr \reg, [sp, \ofs]
        .cfi_restore \reg
.endm

.macro EPILOG_RESTORE_REG_PAIR reg1, reg2, ofs
        ldp \reg1, \reg2, [sp, \ofs]
        .cfi_restore \reg1
        .cfi_restore \reg2
.endm

.macro EPILOG_RESTORE_REG_PAIR_INDEXED reg1, reg2, ofs
        ldp \reg1, \reg2, [sp], \ofs
        .cfi_restore \reg1
        .cfi_restore \reg2
        .cfi_adjust_cfa_offset -\ofs
.endm

.macro EPILOG_RETURN
        ret
.endm

.macro EMIT_BREAKPOINT
        brk #0
.endm

.macro EPILOG_BRANCH_REG reg

        br \reg

.endm

#define xip0 x16
#define xip1 x17
#define xpr x18

// Loads the address of a thread-local variable into the target register,
// which cannot be x0. Preserves all other registers.
.macro INLINE_GET_TLS_VAR target, var
    .ifc \target, x0
        .error "target cannot be x0"
    .endif

    stp     x0, lr, [sp,#-0x10]!
#if defined(__APPLE__)
    // Apple's tls_get_var trashes xip0 (and possibly xip1)
    stp     xip0, xip1, [sp,#-0x10]!
#endif

    // This sequence of instructions is recognized and potentially patched
    // by the linker (GD->IE/LE relaxation).
#if defined(__APPLE__)
    adrp    x0, \var@TLVPPAGE
    ldr     x0, [x0, \var@TLVPPAGEOFF]
    ldr     \target, [x0]

    blr     \target
    // End of the sequence

    mov     \target, x0
#else
    adrp    x0, :tlsdesc:\var
    ldr     \target, [x0, #:tlsdesc_lo12:\var]
    add     x0, x0, :tlsdesc_lo12:\var
    .tlsdesccall \var
    blr     \target
    // End of the sequence

    mrs     \target, tpidr_el0
    add     \target, \target, x0
#endif

#if defined(__APPLE__)
    ldp     xip0, xip1, [sp],#0x10
#endif
    ldp     x0, lr, [sp],#0x10
.endm

// Inlined version of RhpGetThread. Target cannot be x0.
.macro INLINE_GETTHREAD target
    INLINE_GET_TLS_VAR \target, C_FUNC(tls_CurrentThread)
.endm

// Do not use these ETLS macros in functions that already create a stack frame.
// Creating two stack frames in one function can confuse the unwinder/debugger

.macro GETTHREAD_ETLS_1
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -32           // ;; Push down stack pointer and store FP and LR
    str         x0,  [sp, #0x10]

    bl C_FUNC(RhpGetThread)
    mov x1, x0

    ldr         x0,  [sp, #0x10]
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr, 32
.endm

.macro GETTHREAD_ETLS_2
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -32           // ;; Push down stack pointer and store FP and LR
    stp         x0, x1, [sp, #0x10]

    bl C_FUNC(RhpGetThread)
    mov x2, x0

    ldp         x0, x1, [sp, #0x10]
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr, 32
.endm

.macro GETTHREAD_ETLS_3
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -48           // ;; Push down stack pointer and store FP and LR
    stp         x0, x1, [sp, #0x10]
    str         x2,  [sp, #0x20]

    bl C_FUNC(RhpGetThread)
    mov x3, x0

    ldp         x0, x1, [sp, #0x10]
    ldr         x2,  [sp, #0x20]
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr, 48
.endm

.macro GETTHUNKDATA_ETLS_9
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -96           // ;; Push down stack pointer and store FP and LR
    stp         x0, x1, [sp, #0x10]
    stp         x2, x3, [sp, #0x20]
    stp         x4, x5, [sp, #0x30]
    stp         x6, x7, [sp, #0x40]
    stp         x8, xip0, [sp, #0x50]

    bl RhpGetThunkData
    mov x9, x0

    ldp         x0, x1, [sp, #0x10]
    ldp         x2, x3, [sp, #0x20]
    ldp         x4, x5, [sp, #0x30]
    ldp         x6, x7, [sp, #0x40]
    ldp         x8, xip0, [sp, #0x50]
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr, 96
.endm

.macro  InterlockedOperationBarrier
    dmb ish
.endm

.macro INLINE_THREAD_UNHIJACK threadReg, trashReg1, trashReg2
    //
    // Thread::Unhijack()
    //
    ldr         \trashReg1, [\threadReg, #OFFSETOF__Thread__m_pvHijackedReturnAddress]
    cbz         \trashReg1, 0f

    ldr         \trashReg2, [\threadReg, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
    str         \trashReg1, [\trashReg2]
    str         xzr, [\threadReg, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
    str         xzr, [\threadReg, #OFFSETOF__Thread__m_pvHijackedReturnAddress]
0:
.endm


.macro EXPORT_POINTER_TO_ADDRESS Name

1:

        .data
        .align      8
C_FUNC(\Name):
        .quad       1b
        .global     C_FUNC(\Name)
#if !defined(__APPLE__)
        .hidden     C_FUNC(\Name)
#endif
        .text
.endm

// Note: these must match the defs in PInvokeTransitionFrameFlags
PTFF_SAVE_SP            = 0x00000400
PTFF_SAVE_X0            = 0x00000800
PTFF_SAVE_X1            = 0x00001000
PTFF_SAVE_ALL_PRESERVED = 0x000003FF  // NOTE: x19-x28

DEFAULT_FRAME_SAVE_FLAGS = PTFF_SAVE_ALL_PRESERVED + PTFF_SAVE_SP

.macro PUSH_COOP_PINVOKE_FRAME trashReg

    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -0x80      // Push down stack pointer and store FP and LR

    // 0x10 bytes reserved for Thread* and flags

    // Save callee saved registers
    PROLOG_SAVE_REG_PAIR   x19, x20, 0x20
    PROLOG_SAVE_REG_PAIR   x21, x22, 0x30
    PROLOG_SAVE_REG_PAIR   x23, x24, 0x40
    PROLOG_SAVE_REG_PAIR   x25, x26, 0x50
    PROLOG_SAVE_REG_PAIR   x27, x28, 0x60

    // Save the value of SP before stack allocation to the last slot in the frame (slot #15)
    add                    \trashReg, sp, #0x80
    str                    \trashReg, [sp, #0x70]

    // Record the bitmask of saved registers in the frame (slot #3)
    mov                    \trashReg, #DEFAULT_FRAME_SAVE_FLAGS
    str                    \trashReg, [sp, #0x18]

    mov  \trashReg, sp
.endm

// Pop the frame and restore register state preserved by PUSH_COOP_PINVOKE_FRAME
.macro POP_COOP_PINVOKE_FRAME

    EPILOG_RESTORE_REG_PAIR   x19, x20, 0x20
    EPILOG_RESTORE_REG_PAIR   x21, x22, 0x30
    EPILOG_RESTORE_REG_PAIR   x23, x24, 0x40
    EPILOG_RESTORE_REG_PAIR   x25, x26, 0x50
    EPILOG_RESTORE_REG_PAIR   x27, x28, 0x60
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr, 0x80
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

// This must match HwExceptionCode.STATUS_REDHAWK_THREAD_ABORT
#define STATUS_REDHAWK_THREAD_ABORT     0x43

// These must match the TrapThreadsFlags enum
#define TrapThreadsFlags_None            0
#define TrapThreadsFlags_AbortInProgress 1
#define TrapThreadsFlags_TrapThreads     2
