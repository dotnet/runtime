// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <riscv64/AsmMacros_Unix.inc>

//-----------------------------------------------------------------------------
// Define the prolog for a TransitionBlock-based method. This macro should be called first in the method and
// comprises the entire prolog.The locals must be 8 byte aligned
//
// Save_argument_registers:
//            GPR_a7
//            GPR_a6
//            GPR_a5
//            GPR_a4
//            GPR_a3
//            GPR_a2
//            GPR_a1
//            GPR_a0
//
// Callee Saved Registers:
//            GPR_gp
//            GPR_tp
//            GPR_s11
//            GPR_s10
//            GPR_s9
//            GPR_s8
//            GPR_s7
//            GPR_s6
//            GPR_s5
//            GPR_s4
//            GPR_s3
//            GPR_s2
//            GPR_s1
//            GPR_ra
//            GPR_fp
//
// Float Point:
//            FPR_f27 / fs11
//            FPR_f26 / fs10
//            FPR_f25 / fs9
//            FPR_f24 / fs8
//            FPR_f23 / fs7
//            FPR_f22 / fs6
//            FPR_f21 / fs5
//            FPR_f20 / fs4
//            FPR_f19 / fs3
//            FPR_f18 / fs2
//            FPR_f9 / fs1
//            FPR_f8 / fs0
// Extra:
//
// pushCalleeSavedFloatRegs - Optional parameter. If set to 1, the macro will also save
//                            the callee-saved floating point registers (fs0-fs11) to the stack.
//                            These registers are NOT restored by the EPILOG_WITH_TRANSITION_BLOCK variants.
//
.macro PROLOG_WITH_TRANSITION_BLOCK extraParameters = 0, extraLocals = 0, SaveFPRegs = 1, pushCalleeSavedFloatRegs = 0
    __PWTB_SaveFPArgs = \SaveFPRegs
    __PWTB_PushCalleeSavedFloatRegs = \pushCalleeSavedFloatRegs

    __PWTB_FloatCalleeSavedRegisters = \extraLocals

    // Note, stack (see __PWTB_StackAlloc variable) must be 16 byte aligned.
    .if ((__PWTB_FloatCalleeSavedRegisters % 16) != 0)
        __PWTB_FloatCalleeSavedRegisters = __PWTB_FloatCalleeSavedRegisters + 8
    .endif

    // If pushCalleeSavedFloatRegs is specified, reserve space for fs0-fs11 (12 registers * 8 bytes = 96 bytes)
    .if (__PWTB_PushCalleeSavedFloatRegs == 1)
        __PWTB_FloatArgumentRegisters = __PWTB_FloatCalleeSavedRegisters + 96
    .else
        __PWTB_FloatArgumentRegisters = __PWTB_FloatCalleeSavedRegisters
    .endif

    __PWTB_TransitionBlock = __PWTB_FloatArgumentRegisters

    .if (__PWTB_SaveFPArgs == 1)
        __PWTB_TransitionBlock = __PWTB_TransitionBlock + SIZEOF__FloatArgumentRegisters
    .endif


    __PWTB_CalleeSavedRegisters = __PWTB_TransitionBlock
    __PWTB_ArgumentRegisters = __PWTB_TransitionBlock + 120 + 8

    // Including fp, ra, s1-s11, tp, gp, padding and (a0-a7)arguments. (1+1+11+1+1)*8 + 8 + 8*8.
    __PWTB_StackAlloc = __PWTB_TransitionBlock + 120 + 8 + 64
    PROLOG_STACK_ALLOC __PWTB_StackAlloc
    PROLOG_SAVE_REG_PAIR  fp, ra, __PWTB_CalleeSavedRegisters, 1

    // First, Spill argument registers.
    SAVE_ARGUMENT_REGISTERS        sp, __PWTB_ArgumentRegisters

    // Then, Spill callee saved registers.  sp=r2.
    SAVE_CALLEESAVED_REGISTERS     sp, __PWTB_CalleeSavedRegisters

    // saving is f10-17.
    .if (__PWTB_SaveFPArgs == 1)
        SAVE_FLOAT_ARGUMENT_REGISTERS sp, __PWTB_FloatArgumentRegisters
    .endif

    // Save callee-saved floating point registers if requested (fs0-fs11)
    .if (__PWTB_PushCalleeSavedFloatRegs == 1)
        fsd     fs0, (__PWTB_FloatCalleeSavedRegisters)(sp)
        fsd     fs1, (__PWTB_FloatCalleeSavedRegisters + 8)(sp)
        fsd     fs2, (__PWTB_FloatCalleeSavedRegisters + 16)(sp)
        fsd     fs3, (__PWTB_FloatCalleeSavedRegisters + 24)(sp)
        fsd     fs4, (__PWTB_FloatCalleeSavedRegisters + 32)(sp)
        fsd     fs5, (__PWTB_FloatCalleeSavedRegisters + 40)(sp)
        fsd     fs6, (__PWTB_FloatCalleeSavedRegisters + 48)(sp)
        fsd     fs7, (__PWTB_FloatCalleeSavedRegisters + 56)(sp)
        fsd     fs8, (__PWTB_FloatCalleeSavedRegisters + 64)(sp)
        fsd     fs9, (__PWTB_FloatCalleeSavedRegisters + 72)(sp)
        fsd     fs10, (__PWTB_FloatCalleeSavedRegisters + 80)(sp)
        fsd     fs11, (__PWTB_FloatCalleeSavedRegisters + 88)(sp)
    .endif

.endm

.macro EPILOG_WITH_TRANSITION_BLOCK_RETURN
    RESTORE_CALLEESAVED_REGISTERS sp, __PWTB_CalleeSavedRegisters

    EPILOG_RESTORE_REG_PAIR       fp, ra, __PWTB_CalleeSavedRegisters

    EPILOG_STACK_FREE             __PWTB_StackAlloc

    ret
.endm


//-----------------------------------------------------------------------------
// Provides a matching epilog to PROLOG_WITH_TRANSITION_BLOCK and ends by preparing for tail-calling.
// Since this is a tail call argument registers are restored.
//
.macro EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    .if (__PWTB_SaveFPArgs == 1)
        RESTORE_FLOAT_ARGUMENT_REGISTERS  sp, __PWTB_FloatArgumentRegisters
    .endif

    RESTORE_CALLEESAVED_REGISTERS     sp, __PWTB_CalleeSavedRegisters

    RESTORE_ARGUMENT_REGISTERS        sp, __PWTB_ArgumentRegisters

    EPILOG_RESTORE_REG_PAIR fp, ra, __PWTB_CalleeSavedRegisters

    EPILOG_STACK_FREE                 __PWTB_StackAlloc
.endm

// Inlined version of GetThreadEEAllocContext
.macro INLINE_GET_ALLOC_CONTEXT_BASE
    call    C_FUNC(GetThreadEEAllocContext)
.endm

// Pushes a TransitionBlock on the stack without saving the argument registers.
// See the PROLOG_WITH_TRANSITION_BLOCK macro for the stack layout.
.macro PUSH_COOP_PINVOKE_FRAME target
    // Including fp, ra, s1-s11, tp, gp, padding and (a0-a7)arguments. (1+1+11+1+1)*8 + 8 + 8*8.
    PROLOG_STACK_ALLOC          192
    PROLOG_SAVE_REG_PAIR        fp, ra, 0, 1
    // Spill callee saved registers.  sp=r2.
    SAVE_CALLEESAVED_REGISTERS  sp, 0
    mv    \target, sp
.endm

.macro POP_COOP_PINVOKE_FRAME
    RESTORE_CALLEESAVED_REGISTERS sp, 0
    EPILOG_RESTORE_REG_PAIR       fp, ra, 0
    EPILOG_STACK_FREE             192
.endm

// Pushes a full TransitionBlock on the stack including argument registers and
// floating point argument registers. Used for exception throw helpers where we
// need to capture the complete register state.
//
// Stack layout (from low to high address):
//   sp+0:     FloatArgumentRegisters (fa0-fa7, 64 bytes)
//   sp+64:    TransitionBlock start
//             - CalleeSavedRegisters (fp, ra, s1-s11, tp, gp - 120 bytes)
//             - padding (8 bytes)
//             - ArgumentRegisters (a0-a7, 64 bytes)
//
// On exit, \target contains the TransitionBlock pointer (sp+160).
.macro PUSH_COOP_PINVOKE_FRAME_WITH_FLOATS target
    // Stack: FPCalleeSaved(96) + FloatArgs(64) + CalleeSaved(120) + pad(8) + Args(64) = 352 bytes
    PROLOG_STACK_ALLOC 352
    PROLOG_SAVE_REG_PAIR   fp, ra, 160, 1

    // Save callee-saved registers at offset 160 (after FP callee-saved and FloatArgumentRegisters)
    SAVE_CALLEESAVED_REGISTERS sp, 160

    // Save argument registers (a0-a7) at offset 288
    SAVE_ARGUMENT_REGISTERS sp, 288

    // Save floating-point argument registers (fa0-fa7) at offset 96
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, 96

    // Save FP callee-saved registers (fs0-fs11 = f8,f9,f18-f27) at offset 0
    // RISC-V FP callee-saved: fs0=f8, fs1=f9, fs2-fs11=f18-f27
    fsd   fs0,  0(sp)   // f8
    fsd   fs1,  8(sp)   // f9
    fsd   fs2,  16(sp)  // f18
    fsd   fs3,  24(sp)  // f19
    fsd   fs4,  32(sp)  // f20
    fsd   fs5,  40(sp)  // f21
    fsd   fs6,  48(sp)  // f22
    fsd   fs7,  56(sp)  // f23
    fsd   fs8,  64(sp)  // f24
    fsd   fs9,  72(sp)  // f25
    fsd   fs10, 80(sp)  // f26
    fsd   fs11, 88(sp)  // f27

    // Set target to TransitionBlock pointer
    addi  \target, sp, 160
.endm

// ------------------------------------------------------------------
// Macro to generate Redirection Stubs
//
// $reason : reason for redirection
//                     Eg. GCThreadControl
// NOTE: If you edit this macro, make sure you update GetCONTEXTFromRedirectedStubStackFrame.
// This function is used by both the personality routine and the debugger to retrieve the original CONTEXT.
.macro GenerateRedirectedHandledJITCaseStub reason
// TODO RISCV NYI
    sw  ra, 0(zero)
.endm

// Inlined version of RhpGetThread
.macro INLINE_GETTHREAD target
    INLINE_GET_TLS_VAR \target, C_FUNC(t_CurrentThreadInfo)
    ld \target, OFFSETOF__ThreadLocalInfo__m_pThread(\target)
.endm
