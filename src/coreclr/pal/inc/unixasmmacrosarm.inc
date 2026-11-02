// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <arm/AsmMacros_Unix.inc>

// Stack layout:
//
// (stack parameters)
// ...
// ArgumentRegisters::r3
// ArgumentRegisters::r2
// ArgumentRegisters::r1
// ArgumentRegisters::r0
// CalleeSavedRegisters::lr
// CalleeSavedRegisters::r11
// CalleeSavedRegisters::r10
// CalleeSavedRegisters::r9
// CalleeSavedRegisters::r8
// CalleeSavedRegisters::r7    <- r7
// CalleeSavedRegisters::r6
// CalleeSavedRegisters::r5
// CalleeSavedRegisters::r4    <- __PWTB_StackAlloc, __PWTB_TransitionBlock
// padding to align float save area
// d7
// d6
// d5
// d4
// d3
// d2
// d1
// d0                          <- __PWTB_FloatArgumentRegisters
// Optional: Callee saved floating point registers (if pushCalleeSavedFloatRegs=1)
//   d15
//   d14
//   d13
//   d12
//   d11
//   d10
//   d9
//   d8                        <- __PWTB_FloatCalleeSavedRegisters
//
// pushCalleeSavedFloatRegs - Optional parameter. If set to 1, the macro will also save
//                            the callee-saved floating point registers (d8-d15) to the stack.
//                            These registers are NOT restored by the EPILOG_WITH_TRANSITION_BLOCK variants.
//
.macro PROLOG_WITH_TRANSITION_BLOCK extraLocals = 0, saveFpArgs = 1, pushArgRegs = 0, pushCalleeSavedFloatRegs = 0

        __PWTB_FloatCalleeSavedRegisters = \extraLocals
        __PWTB_SaveFPArgs = \saveFpArgs
        __PWTB_PushCalleeSavedFloatRegs = \pushCalleeSavedFloatRegs

        // If pushCalleeSavedFloatRegs is specified, reserve space for d8-d15 (8 registers * 8 bytes = 64 bytes)
        .if (__PWTB_PushCalleeSavedFloatRegs == 1)
                .if ((__PWTB_FloatCalleeSavedRegisters % 8) != 0)
                __PWTB_FloatCalleeSavedRegisters = __PWTB_FloatCalleeSavedRegisters + 4
                .endif
                __PWTB_FloatArgumentRegisters = __PWTB_FloatCalleeSavedRegisters + 64
        .else
                __PWTB_FloatArgumentRegisters = \extraLocals
        .endif

        .if (__PWTB_SaveFPArgs == 1)
                .if ((__PWTB_FloatArgumentRegisters % 8) != 0)
                __PWTB_FloatArgumentRegisters = __PWTB_FloatArgumentRegisters + 4
                .endif

                __PWTB_TransitionBlock = __PWTB_FloatArgumentRegisters + 8 * 8 + 4 // 8 floating point registers + padding
        .else
                .if ((__PWTB_FloatArgumentRegisters % 8) == 0)
                __PWTB_FloatArgumentRegisters = __PWTB_FloatArgumentRegisters + 4
                .endif

                __PWTB_TransitionBlock = __PWTB_FloatArgumentRegisters
        .endif

        __PWTB_StackAlloc = __PWTB_TransitionBlock
        __PWTB_ArgumentRegisters = __PWTB_StackAlloc + 36

        .ifnc \pushArgRegs, DoNotPushArgRegs
                PUSH_ARGUMENT_REGISTERS
        .endif

        PUSH_CALLEE_SAVED_REGISTERS
        PROLOG_STACK_SAVE_OFFSET r7, #12
        // let r7 point the saved r7 in the stack (clang FP style)

        alloc_stack     __PWTB_StackAlloc

        .if (__PWTB_SaveFPArgs == 1)
                add r6, sp, #(__PWTB_FloatArgumentRegisters)
                vstm r6, {d0-d7}
        .endif

        // Save callee-saved floating point registers if requested
        .if (__PWTB_PushCalleeSavedFloatRegs == 1)
                add r6, sp, #(__PWTB_FloatCalleeSavedRegisters)
                vstm r6, {d8-d15}
        .endif

        CHECK_STACK_ALIGNMENT

        END_PROLOGUE

.endm

.macro EPILOG_WITH_TRANSITION_BLOCK_RETURN

        free_stack __PWTB_StackAlloc
        POP_CALLEE_SAVED_REGISTERS
        free_stack 16
        bx lr

.endm

.macro EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        .if (__PWTB_SaveFPArgs == 1)
                add r6, sp, #(__PWTB_FloatArgumentRegisters)
                vldm r6, {d0-d7}
        .endif

        free_stack __PWTB_StackAlloc
        POP_CALLEE_SAVED_REGISTERS
        POP_ARGUMENT_REGISTERS

.endm

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

