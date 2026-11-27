// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <riscv64/AsmMacros_Unix.inc>

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
