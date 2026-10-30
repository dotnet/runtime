// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <arm64/AsmMacros_Unix.inc>

//-----------------------------------------------------------------------------
// Define the prolog for a TransitionFrame-based method. This macro should be called first in the method and
// comprises the entire prolog (i.e. don't modify SP after calling this).The locals must be 8 byte aligned
//
// Stack layout:
//
// (stack parameters)
// ...
// fp
// lr
// CalleeSavedRegisters::x28
// CalleeSavedRegisters::x27
// CalleeSavedRegisters::x26
// CalleeSavedRegisters::x25
// CalleeSavedRegisters::x24
// CalleeSavedRegisters::x23
// CalleeSavedRegisters::x22
// CalleeSavedRegisters::x21
// CalleeSavedRegisters::x20
// CalleeSavedRegisters::x19
// CalleeSavedRegisters::x30 (Lr)
// CalleeSavedRegisters::x29 (Fp)
// ArgumentRegisters::x7
// ArgumentRegisters::x6
// ArgumentRegisters::x5
// ArgumentRegisters::x4
// ArgumentRegisters::x3
// ArgumentRegisters::x2
// ArgumentRegisters::x1
// ArgumentRegisters::x0
// ArgumentRegisters::x8
// FloatRegisters::q7
// FloatRegisters::q6
// FloatRegisters::q5
// FloatRegisters::q4
// FloatRegisters::q3
// FloatRegisters::q2
// FloatRegisters::q1
// FloatRegisters::q0
// Optional: Callee saved floating point registers (if pushCalleeSavedFloatRegs=1)
//   d15
//   d14
//   d13
//   d12
//   d11
//   d10
//   d9
//   d8
//
// pushCalleeSavedFloatRegs - Optional parameter. If set to 1, the macro will also save
//                            the callee-saved floating point registers (d8-d15) to the stack.
//                            These registers are NOT restored by the EPILOG_WITH_TRANSITION_BLOCK variants.
//
.macro PROLOG_WITH_TRANSITION_BLOCK extraLocals = 0, SaveFPArgs = 1, SaveGPArgs = 1, pushCalleeSavedFloatRegs = 0

        __PWTB_FloatCalleeSavedRegisters = \extraLocals
        __PWTB_SaveFPArgs = \SaveFPArgs
        __PWTB_PushCalleeSavedFloatRegs = \pushCalleeSavedFloatRegs

        .if ((__PWTB_FloatCalleeSavedRegisters % 16) != 0)
                __PWTB_FloatCalleeSavedRegisters = __PWTB_FloatCalleeSavedRegisters + 8
        .endif

        // If pushCalleeSavedFloatRegs is specified, reserve space for d8-d15 (8 registers * 8 bytes = 64 bytes)
        .if (__PWTB_PushCalleeSavedFloatRegs == 1)
                __PWTB_FloatArgumentRegisters = __PWTB_FloatCalleeSavedRegisters + 64
        .else
                __PWTB_FloatArgumentRegisters = __PWTB_FloatCalleeSavedRegisters
        .endif

        __PWTB_TransitionBlock = __PWTB_FloatArgumentRegisters

        // Always reserve space for the FP arg regs, even if they are not saved in the PROLOG_WITH_TRANSITION_BLOCK
        // Some code using this macro saves them optionally
        __PWTB_TransitionBlock = __PWTB_TransitionBlock + SIZEOF__FloatArgumentRegisters

        __PWTB_StackAlloc = __PWTB_TransitionBlock
        __PWTB_ArgumentRegisters = __PWTB_StackAlloc + 104
        __PWTB_ArgumentRegister_FirstArg = __PWTB_ArgumentRegisters + 8

        PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -176
        // Spill callee saved registers
        PROLOG_SAVE_REG_PAIR   x19, x20, 16
        PROLOG_SAVE_REG_PAIR   x21, x22, 32
        PROLOG_SAVE_REG_PAIR   x23, x24, 48
        PROLOG_SAVE_REG_PAIR   x25, x26, 64
        PROLOG_SAVE_REG_PAIR   x27, x28, 80

        // Allocate space for the rest of the frame
        PROLOG_STACK_ALLOC  __PWTB_StackAlloc

        .if (\SaveGPArgs == 1)
                // Spill argument registers.
                SAVE_ARGUMENT_REGISTERS        sp, __PWTB_ArgumentRegisters
        .endif

        .if (__PWTB_SaveFPArgs == 1)
                SAVE_FLOAT_ARGUMENT_REGISTERS sp, __PWTB_FloatArgumentRegisters
        .endif

        // Save callee-saved floating point registers if requested
        .if (__PWTB_PushCalleeSavedFloatRegs == 1)
                PROLOG_SAVE_REG_PAIR d8, d9, __PWTB_FloatCalleeSavedRegisters
                PROLOG_SAVE_REG_PAIR d10, d11, __PWTB_FloatCalleeSavedRegisters + 16
                PROLOG_SAVE_REG_PAIR d12, d13, __PWTB_FloatCalleeSavedRegisters + 32
                PROLOG_SAVE_REG_PAIR d14, d15, __PWTB_FloatCalleeSavedRegisters + 48
        .endif

.endm

.macro EPILOG_WITH_TRANSITION_BLOCK_RETURN

        EPILOG_STACK_FREE                 __PWTB_StackAlloc

        EPILOG_RESTORE_REG_PAIR   x19, x20, 16
        EPILOG_RESTORE_REG_PAIR   x21, x22, 32
        EPILOG_RESTORE_REG_PAIR   x23, x24, 48
        EPILOG_RESTORE_REG_PAIR   x25, x26, 64
        EPILOG_RESTORE_REG_PAIR   x27, x28, 80
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 176
        ret

.endm

// Same as EPILOG_WITH_TRANSITION_BLOCK_RETURN but skips restoring x21 (SwiftError)
.macro EPILOG_WITH_TRANSITION_BLOCK_RETURN_SKIP_SWIFT_ERROR

        EPILOG_STACK_FREE                 __PWTB_StackAlloc

        EPILOG_RESTORE_REG_PAIR   x19, x20, 16
        EPILOG_RESTORE_REG   x22, 40
        EPILOG_RESTORE_REG_PAIR   x23, x24, 48
        EPILOG_RESTORE_REG_PAIR   x25, x26, 64
        EPILOG_RESTORE_REG_PAIR   x27, x28, 80
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 176
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

        RESTORE_ARGUMENT_REGISTERS        sp, __PWTB_ArgumentRegisters

        EPILOG_STACK_FREE                 __PWTB_StackAlloc

        EPILOG_RESTORE_REG_PAIR   x19, x20, 16
        EPILOG_RESTORE_REG_PAIR   x21, x22, 32
        EPILOG_RESTORE_REG_PAIR   x23, x24, 48
        EPILOG_RESTORE_REG_PAIR   x25, x26, 64
        EPILOG_RESTORE_REG_PAIR   x27, x28, 80
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 176

.endm

// Loads the address of a thread-local variable into the target register,
// which cannot be x0.
// Preserves registers except for xip0 and xip1 on Apple
.macro INLINE_GET_TLS_VAR target, var
    .ifc \target, x0
        .error "target cannot be x0"
    .endif

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

.endm


// Inlined version of GetThreadEEAllocContext. Target cannot be x0 or x1.
.macro INLINE_GET_ALLOC_CONTEXT_BASE target
        .ifc  \target, x0
                .error "target cannot be x0"
        .endif
        .ifc  \target, x1
                .error "target cannot be x1"
        .endif

#if defined(FEATURE_EMULATED_TLS) || defined(__APPLE__)
        PROLOG_SAVE_REG_PAIR_INDEXED      fp, lr, -0x20
        PROLOG_SAVE_REG_PAIR              x0, x1, 0x10

        bl      C_FUNC(GetThreadEEAllocContext)
        mov     \target, x0

        EPILOG_RESTORE_REG_PAIR           x0, x1, 0x10
        EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr, 0x20
#else
        PROLOG_SAVE_REG_PAIR_INDEXED      x0, lr, -0x10
        INLINE_GET_TLS_VAR \target, t_runtime_thread_locals

        .ifnc OFFSETOF__RuntimeThreadLocals__ee_alloc_context, 0
                add     \target, x0, OFFSETOF__RuntimeThreadLocals__ee_alloc_context
        .endif

        EPILOG_RESTORE_REG_PAIR_INDEXED   x0, lr, 0x10
#endif
.endm

// Pushes a TransitionBlock on the stack without saving the argument registers.
// See the PROLOG_WITH_TRANSITION_BLOCK macro for the stack layout.
.macro PUSH_COOP_PINVOKE_FRAME target
        PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -176

        // Spill callee saved registers
        PROLOG_SAVE_REG_PAIR   x19, x20, 16
        PROLOG_SAVE_REG_PAIR   x21, x22, 32
        PROLOG_SAVE_REG_PAIR   x23, x24, 48
        PROLOG_SAVE_REG_PAIR   x25, x26, 64
        PROLOG_SAVE_REG_PAIR   x27, x28, 80

        mov     \target, sp
.endm

.macro POP_COOP_PINVOKE_FRAME
        EPILOG_RESTORE_REG_PAIR   x19, x20, 16
        EPILOG_RESTORE_REG_PAIR   x21, x22, 32
        EPILOG_RESTORE_REG_PAIR   x23, x24, 48
        EPILOG_RESTORE_REG_PAIR   x25, x26, 64
        EPILOG_RESTORE_REG_PAIR   x27, x28, 80
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 176
.endm

// Pushes a full TransitionBlock on the stack including argument registers and
// floating point argument registers. Used for exception throw helpers where we
// need to capture the complete register state including FP callee-saved registers.
//
// Stack layout (from low to high address):
//   sp+0:     FP callee-saved registers (d8-d15, 64 bytes)
//   sp+64:    FloatArgumentRegisters (q0-q7, 128 bytes)
//   sp+192:   TransitionBlock start (176 bytes)
//             - CalleeSavedRegisters (fp, lr, x19-x28 - 96 bytes)
//             - padding (8 bytes)
//             - x8 (8 bytes)
//             - ArgumentRegisters (x0-x7, 64 bytes)
//
// On exit, \target contains the TransitionBlock pointer (sp+192).
.macro PUSH_COOP_PINVOKE_FRAME_WITH_FLOATS target
        PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -176

        // Spill callee saved registers
        PROLOG_SAVE_REG_PAIR   x19, x20, 16
        PROLOG_SAVE_REG_PAIR   x21, x22, 32
        PROLOG_SAVE_REG_PAIR   x23, x24, 48
        PROLOG_SAVE_REG_PAIR   x25, x26, 64
        PROLOG_SAVE_REG_PAIR   x27, x28, 80

        // Allocate space for FloatArgumentRegisters (128) + FP callee-saved (64) = 192 bytes
        PROLOG_STACK_ALLOC 192

        // Save argument registers (x8, x0-x7) at offset 296 from sp (192 + 104)
        SAVE_ARGUMENT_REGISTERS sp, 296

        // Save floating point argument registers (q0-q7) at sp+64
        SAVE_FLOAT_ARGUMENT_REGISTERS sp, 64

        // Save FP callee-saved registers (d8-d15) at sp+0
        str     d8,  [sp, #0]
        str     d9,  [sp, #8]
        str     d10, [sp, #16]
        str     d11, [sp, #24]
        str     d12, [sp, #32]
        str     d13, [sp, #40]
        str     d14, [sp, #48]
        str     d15, [sp, #56]

        // Set target to TransitionBlock pointer
        add     \target, sp, #192
.endm

// ------------------------------------------------------------------
// Macro to generate Redirection Stubs
//
// $reason : reason for redirection
//                     Eg. GCThreadControl
// NOTE: If you edit this macro, make sure you update GetCONTEXTFromRedirectedStubStackFrame.
// This function is used by both the personality routine and the debugger to retrieve the original CONTEXT.
.macro GenerateRedirectedHandledJITCaseStub reason

#if NOTYET
        GBLS __RedirectionStubFuncName
        GBLS __RedirectionStubEndFuncName
        GBLS __RedirectionFuncName
__RedirectionStubFuncName SETS "RedirectedHandledJITCaseFor":CC:"$reason":CC:"_Stub"
__RedirectionStubEndFuncName SETS "RedirectedHandledJITCaseFor":CC:"$reason":CC:"_StubEnd"
__RedirectionFuncName SETS "|?RedirectedHandledJITCaseFor":CC:"$reason":CC:"@Thread@@CAXXZ|"

        IMPORT $__RedirectionFuncName

        NESTED_ENTRY $__RedirectionStubFuncName
        PROLOG_SAVE_REG_PAIR    fp, lr, -16
        sub sp, sp, #16                          // stack slot for CONTEXT * and padding

        //REDIRECTSTUB_SP_OFFSET_CONTEXT is defined in asmconstants.h and is used in GetCONTEXTFromRedirectedStubStackFrame
        //If CONTEXT is not saved at 0 offset from SP it must be changed as well.
        ASSERT REDIRECTSTUB_SP_OFFSET_CONTEXT == 0

        // Stack alignment. This check is necessary as this function can be
        // entered before complete execution of the prolog of another function.
        and x8, fp, #15
        sub sp, sp, x8


        //
        // Save a copy of the redirect CONTEXT*.
        // This is needed for the debugger to unwind the stack.
        //
        bl GetCurrentSavedRedirectContext
        str x0, [sp]

        //
        // Fetch the interrupted pc and save it as our return address.
        //
        ldr x1, [x0, #CONTEXT_Pc]
        str x1, [fp, #8]

        //
        // Call target, which will do whatever we needed to do in the context
        // of the target thread, and will RtlRestoreContext when it is done.
        //
        bl $__RedirectionFuncName

        EMIT_BREAKPOINT // Unreachable

// Put a label here to tell the debugger where the end of this function is.
$__RedirectionStubEndFuncName
        EXPORT $__RedirectionStubEndFuncName

        NESTED_END
#else
        EMIT_BREAKPOINT
#endif

.endm

// Inlined version of RhpGetThread. Target cannot be x0.
.macro INLINE_GETTHREAD target
    INLINE_GET_TLS_VAR \target, C_FUNC(t_CurrentThreadInfo)
    ldr     \target, [\target, #OFFSETOF__ThreadLocalInfo__m_pThread]
.endm
