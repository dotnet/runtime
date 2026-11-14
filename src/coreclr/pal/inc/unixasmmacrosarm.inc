// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <arm/AsmMacros_Unix.inc>

.macro INLINE_GET_ALLOC_CONTEXT_BASE
        bl      C_FUNC(GetThreadEEAllocContext)
.endm

.macro PUSH_COOP_PINVOKE_FRAME target
        // Reserve space for argument registers
        alloc_stack 16
        PUSH_CALLEE_SAVED_REGISTERS
        PROLOG_STACK_SAVE_OFFSET r7, #12
        // let r7 point the saved r7 in the stack (clang FP style)
        // align the stack
        alloc_stack 4
        CHECK_STACK_ALIGNMENT
        END_PROLOGUE
        add     \target, sp, 4
.endm

// Pushes a full TransitionBlock on the stack including float argument registers.
// On exit, \target contains the TransitionBlock pointer.
//
// Stack layout (from sp going up):
//   sp+0:   padding (4 bytes) - for 8-byte alignment
//   sp+4:   d8-d15 (64 bytes) - FP callee-saved
//   sp+68:  padding (4 bytes) - to make d0-d7 8-byte aligned at TransitionBlock-68
//   sp+72:  d0-d7 (64 bytes) - float argument registers (at TransitionBlock - 68)
//   sp+136: padding (4 bytes) - to keep total allocation 8-byte aligned
//   sp+140: TransitionBlock starts here (CalleeSavedRegisters + ArgumentRegisters pushed above)
//
// GetNegSpaceSize() for ARM32 = 64 (FloatArgumentRegisters) + 4 (padding) = 68
// GetOffsetOfFloatArgumentRegisters() = -68
//
// Total stack alloc: 4 + 64 + 4 + 64 + 4 = 140 bytes
// Stack: Arguments(16) + callee-saved(36) + alloc(140) = 192 bytes
// 192 % 8 = 0, properly aligned for ARM32 (8-byte alignment required)
.macro PUSH_COOP_PINVOKE_FRAME_WITH_FLOATS target
        // Push argument registers (r0-r3) - these will be at highest address in TransitionBlock
        PUSH_ARGUMENT_REGISTERS
        PUSH_CALLEE_SAVED_REGISTERS
        PROLOG_STACK_SAVE_OFFSET r7, #12
        // let r7 point the saved r7 in the stack (clang FP style)
        // Allocate space for: padding (4) + d8-d15 (64) + padding (4) + d0-d7 (64) + padding (4) = 140 bytes
        alloc_stack 140
        // Save floating point argument registers (d0-d7) at sp+72 (TransitionBlock - 68)
        add     r12, sp, #72
        vstm    r12, {d0-d7}
        // Save FP callee-saved registers (d8-d15) at sp+4
        add     r12, sp, #4
        vstm    r12, {d8-d15}
        CHECK_STACK_ALIGNMENT
        END_PROLOGUE
        // TransitionBlock is at sp + 140
        add     \target, sp, #140
.endm

.macro POP_COOP_PINVOKE_FRAME
        free_stack 4
        POP_CALLEE_SAVED_REGISTERS
        free_stack 16
.endm
.macro INLINE_GETTHREAD
#ifdef FEATURE_EMULATED_TLS
        bl      C_FUNC(GetThreadHelper)
#else
        INLINE_GET_TLS_VAR C_FUNC(t_CurrentThreadInfo)
        ldr     r0, [r0, #OFFSETOF__ThreadLocalInfo__m_pThread]
#endif
.endm

