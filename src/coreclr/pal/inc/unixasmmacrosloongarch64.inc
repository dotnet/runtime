// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <loongarch64/AsmMacros_Unix.inc>

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
// General Registers:
//            GPR_s8
//            GPR_s7
//            GPR_s6
//            GPR_s5
//            GPR_s4
//            GPR_s3
//            GPR_s2
//            GPR_s1
//            GPR_s0
//            GPR_ra
//            GPR_fp
//
// Float Point:
//            FPR_f31 / fs7
//            FPR_f30 / fs6
//            FPR_f29 / fs5
//            FPR_f28 / fs4
//            FPR_f27 / fs3
//            FPR_f26 / fs2
//            FPR_f25 / fs1
//            FPR_f24 / fs0
// Extra:
//
.macro PROLOG_WITH_TRANSITION_BLOCK extraParameters = 0, extraLocals = 0, SaveFPRegs = 1

        __PWTB_SaveFPArgs = \SaveFPRegs

        __PWTB_FloatArgumentRegisters = \extraLocals

        .if ((__PWTB_FloatArgumentRegisters % 16) != 0)
            __PWTB_FloatArgumentRegisters = __PWTB_FloatArgumentRegisters + 8
        .endif

        __PWTB_TransitionBlock = __PWTB_FloatArgumentRegisters

        .if (__PWTB_SaveFPArgs == 1)
            __PWTB_TransitionBlock = __PWTB_TransitionBlock + SIZEOF__FloatArgumentRegisters
        .endif


        __PWTB_CalleeSavedRegisters = __PWTB_TransitionBlock
        __PWTB_ArgumentRegisters = __PWTB_TransitionBlock + 96

        // Including fp, ra, s0-s8, padding, and (a0-a7)arguments. (1+1+9+1)*8 + 8*8.
        __PWTB_StackAlloc = __PWTB_TransitionBlock + 96 + 64
        PROLOG_STACK_ALLOC __PWTB_StackAlloc
        //                   $fp,$ra
        PROLOG_SAVE_REG_PAIR  22, 1, __PWTB_CalleeSavedRegisters, 1

        // First, Spill argument registers.
        SAVE_ARGUMENT_REGISTERS        $sp, __PWTB_ArgumentRegisters

        // Then, Spill callee saved registers.  $sp=$r3.
        SAVE_CALLEESAVED_REGISTERS     3, __PWTB_CalleeSavedRegisters

        // saving is f12-19.
        .if (__PWTB_SaveFPArgs == 1)
            SAVE_FLOAT_ARGUMENT_REGISTERS $sp, __PWTB_FloatArgumentRegisters
        .endif

.endm

.macro EPILOG_WITH_TRANSITION_BLOCK_RETURN

        RESTORE_CALLEESAVED_REGISTERS 3, __PWTB_CalleeSavedRegisters

        EPILOG_RESTORE_REG_PAIR 22, 1, __PWTB_CalleeSavedRegisters

        EPILOG_STACK_FREE                 __PWTB_StackAlloc

        jirl  $r0, $ra, 0

.endm


//-----------------------------------------------------------------------------
// Provides a matching epilog to PROLOG_WITH_TRANSITION_BLOCK and ends by preparing for tail-calling.
// Since this is a tail call argument registers are restored.
//
.macro EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        .if (__PWTB_SaveFPArgs == 1)
            RESTORE_FLOAT_ARGUMENT_REGISTERS  $sp, __PWTB_FloatArgumentRegisters
        .endif

        RESTORE_CALLEESAVED_REGISTERS     3, __PWTB_CalleeSavedRegisters

        RESTORE_ARGUMENT_REGISTERS        $sp, __PWTB_ArgumentRegisters

        EPILOG_RESTORE_REG_PAIR 22, 1, __PWTB_CalleeSavedRegisters

        EPILOG_STACK_FREE                 __PWTB_StackAlloc

.endm

// Inlined version of GetThreadEEAllocContext. Target cannot be a0.
.macro INLINE_GET_ALLOC_CONTEXT_BASE target
        .ifc  \target, $a0
                .error "target cannot be a0"
        .endif

        // Save $a0, $ra
        PROLOG_SAVE_REG_PAIR_INDEXED   4, 1, 16, 0

        // This instruction is recognized and potentially patched
        // by the linker (GD->IE/LE relaxation).
        la.tls.desc  $a0, t_runtime_thread_locals

        addi.d  \target, $tp, OFFSETOF__RuntimeThreadLocals__ee_alloc_context
        add.d  \target, \target, $a0

        // Restore $a0, $ra
        EPILOG_RESTORE_REG_PAIR_INDEXED 4, 1, 16
.endm

// Pushes a TransitionBlock on the stack without saving the argument registers.
// See the PROLOG_WITH_TRANSITION_BLOCK macro for the stack layout.
.macro PUSH_COOP_PINVOKE_FRAME target
        // Including fp, ra, s0-s8, padding, and (a0-a7)arguments. (1+1+9+1)*8 + 8*8 == 96.
        PROLOG_STACK_ALLOC  160
        //                   $fp,$ra
        PROLOG_SAVE_REG_PAIR  22, 1, 0, 1
        // Spill callee saved registers.  $sp=$r3.
        SAVE_CALLEESAVED_REGISTERS  3, 0
        move  \target, $sp
.endm

.macro POP_COOP_PINVOKE_FRAME
        RESTORE_CALLEESAVED_REGISTERS  3, 0
        EPILOG_RESTORE_REG_PAIR  22, 1, 0
        EPILOG_STACK_FREE  160
.endm

// Pushes a full TransitionBlock on the stack including argument registers and
// floating point argument registers. Used for exception throw helpers where we
// need to capture the complete register state.
//
// Stack layout (from low to high address):
//   sp+0:     FloatArgumentRegisters (fa0-fa7, 64 bytes)
//   sp+64:    TransitionBlock start
//             - CalleeSavedRegisters (fp, ra, s0-s8 - 88 bytes)
//             - padding (8 bytes)
//             - ArgumentRegisters (a0-a7, 64 bytes)
//
// On exit, \target contains the TransitionBlock pointer (sp+128).
.macro PUSH_COOP_PINVOKE_FRAME_WITH_FLOATS target
        // Stack: FPCalleeSaved(64) + FloatArgs(64) + CalleeSaved(88) + pad(8) + Args(64) = 288 bytes
        PROLOG_STACK_ALLOC 288
        PROLOG_SAVE_REG_PAIR  22, 1, 128, 1

        // Save callee-saved registers at offset 128 (after FP callee-saved and FloatArgumentRegisters)
        SAVE_CALLEESAVED_REGISTERS $sp, 128

        // Save argument registers (a0-a7) at offset 224
        SAVE_ARGUMENT_REGISTERS $sp, 224

        // Save floating-point argument registers (fa0-fa7) at offset 64
        SAVE_FLOAT_ARGUMENT_REGISTERS $sp, 64

        // Save FP callee-saved registers (f24-f31) at offset 0
        fst.d   $f24, $sp, 0
        fst.d   $f25, $sp, 8
        fst.d   $f26, $sp, 16
        fst.d   $f27, $sp, 24
        fst.d   $f28, $sp, 32
        fst.d   $f29, $sp, 40
        fst.d   $f30, $sp, 48
        fst.d   $f31, $sp, 56

        // Set target to TransitionBlock pointer
        addi.d  \target, $sp, 128
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
        addi.d $sp, $sp, -32                          // stack slot for CONTEXT * and padding
        PROLOG_SAVE_REG_PAIR    22, 1, 16, 1

        //REDIRECTSTUB_SP_OFFSET_CONTEXT is defined in asmconstants.h and is used in GetCONTEXTFromRedirectedStubStackFrame
        //If CONTEXT is not saved at 0 offset from SP it must be changed as well.
        ASSERT REDIRECTSTUB_SP_OFFSET_CONTEXT == 0

        // Stack alignment. This check is necessary as this function can be
        // entered before complete execution of the prolog of another function.
        andi $t4, $fp, 0xf
        sub.d  $sp, $sp, $t4


        //
        // Save a copy of the redirect CONTEXT*.
        // This is needed for the debugger to unwind the stack.
        //
        bl  GetCurrentSavedRedirectContext

        st.d  $v0, $sp, 0

        //
        // Fetch the interrupted pc and save it as our return address.
        //
        ld.d  $a1, $a0, CONTEXT_PC
        st.d  $a1, $fp, 8

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

// Inlined version of RhpGetThread. Target cannot be a0.
.macro INLINE_GETTHREAD target
        INLINE_GET_TLS_VAR \target, C_FUNC(t_CurrentThreadInfo)
        ld.d  \target, \target, OFFSETOF__ThreadLocalInfo__m_pThread
.endm
