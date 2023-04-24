// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.macro NESTED_ENTRY Name, Section, Handler
        LEAF_ENTRY \Name, \Section
        .ifnc \Handler, NoHandler
        .cfi_personality 0x1c, C_FUNC(\Handler) // 0x1c == DW_EH_PE_pcrel | DW_EH_PE_sdata8
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
        // make sure this symbol gets its own address
        nop
.endm

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
    la.local  \HelperReg, \Name
.endm

.macro PROLOG_STACK_ALLOC Size
        addi.d $sp, $sp, -\Size
        //.cfi_adjust_cfa_offset \Size
        .cfi_def_cfa 3,\Size
.endm

.macro EPILOG_STACK_FREE Size
        addi.d $sp, $sp, \Size
        //.cfi_adjust_cfa_offset -\Size
        .cfi_def_cfa 3,-\Size
.endm

.macro EPILOG_STACK_RESTORE
        ori  $sp, $fp, 0
        .cfi_restore 3
.endm

////NOTE: reg must be the number and GPR type !!!
.macro PROLOG_SAVE_REG reg, ofs
        st.d $r\reg, $sp, \ofs
        .cfi_rel_offset \reg, \ofs
.endm

////NOTE: reg1 and reg2 must be the number and GPR type !!!
.macro PROLOG_SAVE_REG_PAIR reg1, reg2, ofs, __def_cfa_save=0
//#ifdef FEATURE_LOONGSONISA
//        //NOTE:The offset of gssq/gslq must be 16-bytes aligned.
//        //     here ofs must be 16-bytes aligned.
//        gssq \reg2, \reg1, \ofs(sp)
//#else
        st.d $r\reg1, $sp, \ofs
        st.d $r\reg2, $sp, \ofs+8
//#endif

        .cfi_rel_offset \reg1, \ofs
        .cfi_rel_offset \reg2, \ofs + 8
        .if (\__def_cfa_save ==  1)
           ori $fp, $sp, 0
           .cfi_def_cfa_register 22
        .endif
.endm

////NOTE: reg1 and reg2 must be the number and GPR type !!!
.macro PROLOG_SAVE_REG_PAIR_INDEXED reg1, reg2, ssize, __def_cfa_save=1
        addi.d  $sp, $sp, -\ssize
        //.cfi_adjust_cfa_offset \ssize
        .cfi_def_cfa 3,\ssize

        st.d $r\reg1, $sp, 0
        st.d $r\reg2, $sp, 8

        .cfi_rel_offset \reg1, 0
        .cfi_rel_offset \reg2, 8
        .if (\__def_cfa_save ==  1)
          ori $fp, $sp, 0
          .cfi_def_cfa_register 22
        .endif
.endm

.macro EPILOG_RESTORE_REG reg, ofs
        ld.d $r\reg, $sp, \ofs
        .cfi_restore \reg
       .cfi_def_cfa_register 3
.endm

.macro EPILOG_RESTORE_REG_PAIR reg1, reg2, ofs
//#ifdef FEATURE_LOONGSONISA
//        gslq \reg2, \reg1, \ofs(sp)
//#else
        ld.d  $r\reg2, $sp, \ofs+8
        ld.d  $r\reg1, $sp, \ofs
//#endif
        .cfi_restore \reg2
        .cfi_restore \reg1
.endm

.macro EPILOG_RESTORE_REG_PAIR_INDEXED reg1, reg2, ssize
//#ifdef FEATURE_LOONGSONISA
//        gslq \reg2, \reg1, 0(sp)
//#else
        ld.d  $r\reg2, $sp, 8
        ld.d  $r\reg1, $sp, 0
//#endif
        .cfi_restore \reg2
        .cfi_restore \reg1

        addi.d  $sp, $sp, \ssize
        //.cfi_adjust_cfa_offset -\ssize
        .cfi_def_cfa 3,-\ssize
.endm

.macro EPILOG_RETURN
    jirl  $r0, $ra, 0
.endm

.macro EMIT_BREAKPOINT
        break
.endm

.macro EPILOG_BRANCH Target
        b \Target
        //break
.endm

.macro EPILOG_BRANCH_REG reg
    jirl  $r0, \reg, 0
.endm

//-----------------------------------------------------------------------------
// The Following sets of SAVE_*_REGISTERS expect the memory to be reserved and
// base address to be passed in $reg
//

// Reserve 64 bytes of memory before calling  SAVE_CALLEESAVED_REGISTERS
.macro SAVE_CALLEESAVED_REGISTERS reg, ofs

        PROLOG_SAVE_REG_PAIR 23, 24, \ofs + 16
        PROLOG_SAVE_REG_PAIR 25, 26, \ofs + 32
        PROLOG_SAVE_REG_PAIR 27, 28, \ofs + 48
        PROLOG_SAVE_REG_PAIR 29, 30, \ofs + 64
        PROLOG_SAVE_REG_PAIR 31, 2,  \ofs + 80

.endm

// Reserve 64 bytes of memory before calling  SAVE_ARGUMENT_REGISTERS
.macro SAVE_ARGUMENT_REGISTERS reg, ofs

//#ifdef FEATURE_LOONGSONISA
//        //NOTE:The offset of gssq/gslq must be 16-bytes aligned.
//        //     here ofs must be 16-bytes aligned.
//        gssq  a1, a0, \ofs(\reg)
//        gssq  a3, a2, \ofs+16(\reg)
//        gssq  a5, a4, \ofs+32(\reg)
//        gssq  a7, a6, \ofs+48(\reg)
//#else
        st.d  $a0, \reg, \ofs
        st.d  $a1, \reg, \ofs+8
        st.d  $a2, \reg, \ofs+16
        st.d  $a3, \reg, \ofs+24
        st.d  $a4, \reg, \ofs+32
        st.d  $a5, \reg, \ofs+40
        st.d  $a6, \reg, \ofs+48
        st.d  $a7, \reg, \ofs+56
//#endif

.endm

// Reserve 64 bytes of memory before calling  SAVE_FLOAT_ARGUMENT_REGISTERS
.macro SAVE_FLOAT_ARGUMENT_REGISTERS reg, ofs

//#ifdef FEATURE_LOONGSONISA
//        //NOTE:The offset of gssqc1/gslqc1 must be 16-bytes aligned.
//        //     here ofs must be 16-bytes aligned.
//        gssqc1  $f13, $f12, \ofs(\reg)
//        gssqc1  $f15, $f14, \ofs+16(\reg)
//        gssqc1  $f17, $f16, \ofs+32(\reg)
//        gssqc1  $f19, $f18, \ofs+48(\reg)
//#else
        fst.d $f0, \reg, \ofs
        fst.d $f1, \reg, \ofs+8
        fst.d $f2, \reg, \ofs+16
        fst.d $f3, \reg, \ofs+24
        fst.d $f4, \reg, \ofs+32
        fst.d $f5, \reg, \ofs+40
        fst.d $f6, \reg, \ofs+48
        fst.d $f7, \reg, \ofs+56
//#endif

.endm

// Reserve 64 bytes of memory before calling  SAVE_FLOAT_CALLEESAVED_REGISTERS
.macro SAVE_FLOAT_CALLEESAVED_REGISTERS reg, ofs

//#ifdef FEATURE_LOONGSONISA
//        //NOTE:The offset of gssqc1/gslqc1 must be 16-bytes aligned.
//        //     here ofs must be 16-bytes aligned.
//        gssqc1  $f25, $f24, \ofs(\reg)
//        gssqc1  $f27, $f26, \ofs+16(\reg)
//        gssqc1  $f29, $f28, \ofs+32(\reg)
//        gssqc1  $f31, $f30, \ofs+48(\reg)
//#else
        fst.d  $f24, \reg, \ofs
        fst.d  $f25, \reg, \ofs+8
        fst.d  $f26, \reg, \ofs+16
        fst.d  $f27, \reg, \ofs+24
        fst.d  $f28, \reg, \ofs+32
        fst.d  $f29, \reg, \ofs+40
        fst.d  $f30, \reg, \ofs+48
        fst.d  $f31, \reg, \ofs+56
//#endif

.endm

.macro RESTORE_CALLEESAVED_REGISTERS reg, ofs

        EPILOG_RESTORE_REG_PAIR 31, 2   \ofs + 80

        EPILOG_RESTORE_REG_PAIR 29, 30, \ofs + 64
        EPILOG_RESTORE_REG_PAIR 27, 28, \ofs + 48
        EPILOG_RESTORE_REG_PAIR 25, 26, \ofs + 32
        EPILOG_RESTORE_REG_PAIR 23, 24, \ofs + 16
.endm

.macro RESTORE_ARGUMENT_REGISTERS reg, ofs

//#ifdef FEATURE_LOONGSONISA
//        //NOTE:The offset of gssq/gslq must be 16-bytes aligned.
//        //     here ofs must be 16-bytes aligned.
//        gslq  a7, a6, \ofs+48(\reg)
//        gslq  a5, a4, \ofs+32(\reg)
//        gslq  a3, a2, \ofs+16(\reg)
//        gslq  a1, a0, \ofs(\reg)
//#else
        ld.d $a7, \reg, \ofs+56
        ld.d $a6, \reg, \ofs+48
        ld.d $a5, \reg, \ofs+40
        ld.d $a4, \reg, \ofs+32
        ld.d $a3, \reg, \ofs+24
        ld.d $a2, \reg, \ofs+16
        ld.d $a1, \reg, \ofs+8
        ld.d $a0, \reg, \ofs
//#endif

.endm

.macro RESTORE_FLOAT_ARGUMENT_REGISTERS reg, ofs

//#ifdef FEATURE_LOONGSONISA
//        gslqc1  $f19, $f18, \ofs+48(\reg)
//        gslqc1  $f17, $f16, \ofs+32(\reg)
//        gslqc1  $f15, $f14, \ofs+16(\reg)
//        gslqc1  $f13, $f12, \ofs(\reg)
//#else
        fld.d  $f7, \reg, \ofs+56
        fld.d  $f6, \reg, \ofs+48
        fld.d  $f5, \reg, \ofs+40
        fld.d  $f4, \reg, \ofs+32
        fld.d  $f3, \reg, \ofs+24
        fld.d  $f2, \reg, \ofs+16
        fld.d  $f1, \reg, \ofs+8
        fld.d  $f0, \reg, \ofs
//#endif

.endm

.macro RESTORE_FLOAT_CALLEESAVED_REGISTERS reg, ofs

//#ifdef FEATURE_LOONGSONISA
//        //NOTE:The offset of gssqc1/gslqc1 must be 16-bytes aligned.
//        //     here ofs must be 16-bytes aligned.
//        gslqc1  $f25, $f24, \ofs(\reg)
//        gslqc1  $f27, $f26, \ofs+16(\reg)
//        gslqc1  $f29, $f28, \ofs+32(\reg)
//        gslqc1  $f31, $f30, \ofs+48(\reg)
//#else
        fld.d  $f24, $r\reg, \ofs
        fld.d  $f25, $r\reg, \ofs+8
        fld.d  $f26, $r\reg, \ofs+16
        fld.d  $f27, $r\reg, \ofs+24
        fld.d  $f28, $r\reg, \ofs+32
        fld.d  $f29, $r\reg, \ofs+40
        fld.d  $f30, $r\reg, \ofs+48
        fld.d  $f31, $r\reg, \ofs+56
//#endif

.endm

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
//            GPR_tp
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

        // Including fp, ra, s0-s8, tp, and (a0-a7)arguments. (1+1+9+1)*8 + 8*8.
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
        andi $t4, $fp, 15
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
