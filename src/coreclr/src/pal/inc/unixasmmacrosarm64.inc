// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

.macro LEAF_ENTRY Name, Section
        .global C_FUNC(\Name)
        .type \Name, %function
C_FUNC(\Name):
        .cfi_startproc
.endm

.macro LEAF_END Name, Section
        .size \Name, .-\Name
        .cfi_endproc
.endm

.macro LEAF_END_MARKED Name, Section
C_FUNC(\Name\()_End):
        .global C_FUNC(\Name\()_End)
        LEAF_END \Name, \Section
.endm

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
        adrp    \HelperReg, \Name
        add     \HelperReg, \HelperReg, :lo12:\Name
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

.macro EPILOG_RESTORE_REG reg, ofs
        ldr \reg, [sp, \ofs]
        .cfi_restore \reg1
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
// FloatRegisters::q7
// FloatRegisters::q6
// FloatRegisters::q5
// FloatRegisters::q4
// FloatRegisters::q3
// FloatRegisters::q2
// FloatRegisters::q1
// FloatRegisters::q0
.macro PROLOG_WITH_TRANSITION_BLOCK extraLocals = 0, SaveFPArgs = 1

        __PWTB_FloatArgumentRegisters = \extraLocals
        __PWTB_SaveFPArgs = \SaveFPArgs

        .if ((__PWTB_FloatArgumentRegisters % 16) != 0)
                __PWTB_FloatArgumentRegisters = __PWTB_FloatArgumentRegisters + 8
        .endif

        __PWTB_TransitionBlock = __PWTB_FloatArgumentRegisters

        .if (__PWTB_SaveFPArgs == 1)
            __PWTB_TransitionBlock = __PWTB_TransitionBlock + SIZEOF__FloatArgumentRegisters
        .endif

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

        // Spill argument registers.
        SAVE_ARGUMENT_REGISTERS        sp, __PWTB_ArgumentRegisters

        .if (__PWTB_SaveFPArgs == 1)
                SAVE_FLOAT_ARGUMENT_REGISTERS sp, \extraLocals
        .endif

.endm

//-----------------------------------------------------------------------------
// The Following sets of SAVE_*_REGISTERS expect the memory to be reserved and
// base address to be passed in $reg
//

// Reserve 64 bytes of memory before calling  SAVE_ARGUMENT_REGISTERS
.macro SAVE_ARGUMENT_REGISTERS reg, ofs

        str                    x8, [\reg, #(\ofs)]
        stp                    x0, x1, [\reg, #(\ofs + 8)]
        stp                    x2, x3, [\reg, #(\ofs + 24)]
        stp                    x4, x5, [\reg, #(\ofs + 40)]
        stp                    x6, x7, [\reg, #(\ofs + 56)]

.endm

// Reserve 128 bytes of memory before calling  SAVE_FLOAT_ARGUMENT_REGISTERS
.macro SAVE_FLOAT_ARGUMENT_REGISTERS reg, ofs

        stp                    q0, q1, [\reg, #(\ofs)]
        stp                    q2, q3, [\reg, #(\ofs + 32)]
        stp                    q4, q5, [\reg, #(\ofs + 64)]
        stp                    q6, q7, [\reg, #(\ofs + 96)]

.endm

.macro RESTORE_ARGUMENT_REGISTERS reg, ofs

        ldr                    x8, [\reg, #(\ofs)]
        ldp                    x0, x1, [\reg, #(\ofs + 8)]
        ldp                    x2, x3, [\reg, #(\ofs + 24)]
        ldp                    x4, x5, [\reg, #(\ofs + 40)]
        ldp                    x6, x7, [\reg, #(\ofs + 56)]

.endm

.macro RESTORE_FLOAT_ARGUMENT_REGISTERS reg, ofs

        ldp                    q0, q1, [\reg, #(\ofs)]
        ldp                    q2, q3, [\reg, #(\ofs + 32)]
        ldp                    q4, q5, [\reg, #(\ofs + 64)]
        ldp                    q6, q7, [\reg, #(\ofs + 96)]

.endm

.macro EPILOG_BRANCH Target
        b \Target
.endm

.macro EPILOG_BRANCH_REG reg

        br \reg

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
