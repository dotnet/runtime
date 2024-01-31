// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.macro NESTED_ENTRY Name, Section, Handler
    LEAF_ENTRY \Name, \Section
    .ifnc \Handler, NoHandler
        .cfi_personality 0x1B, C_FUNC(\Handler) // 0x1B == DW_EH_PE_pcrel | DW_EH_PE_sdata4
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
    lla  \HelperReg, \Name
.endm

.macro PROLOG_STACK_ALLOC Size
    addi sp, sp, -\Size
    .cfi_adjust_cfa_offset \Size
.endm

.macro EPILOG_STACK_FREE Size
    addi sp, sp, \Size
    .cfi_adjust_cfa_offset -\Size
.endm

.macro EPILOG_STACK_RESTORE
    ori  sp, fp, 0
    .cfi_restore sp
.endm

.macro PROLOG_SAVE_REG reg, ofs
    sd \reg, \ofs(sp)
    .cfi_rel_offset \reg, \ofs
.endm

.macro PROLOG_SAVE_REG_PAIR reg1, reg2, ofs, __def_cfa_save=0
    sd \reg1, \ofs(sp)
    sd \reg2, (\ofs+8)(sp)
    .cfi_rel_offset \reg1, \ofs
    .cfi_rel_offset \reg2, \ofs + 8
    .if (\__def_cfa_save ==  1)
        addi fp, sp, 0
        .cfi_def_cfa_register fp
    .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_INDEXED reg1, reg2, ssize, __def_cfa_save=1
    addi sp, sp, -\ssize
    .cfi_adjust_cfa_offset \ssize

    sd \reg1, 0(sp)
    sd \reg2, 8(sp)

    .cfi_rel_offset \reg1, 0
    .cfi_rel_offset \reg2, 8
    .if (\__def_cfa_save ==  1)
        addi fp, sp, 0
        .cfi_def_cfa_register fp
    .endif
.endm

.macro EPILOG_RESTORE_REG reg, ofs
    ld  \reg, (\ofs)(sp)
    .cfi_restore \reg
.endm

.macro EPILOG_RESTORE_REG_PAIR reg1, reg2, ofs
    ld  \reg2, (\ofs+8)(sp)
    ld  \reg1, (\ofs)(sp)
    .cfi_restore \reg2
    .cfi_restore \reg1
.endm

.macro EPILOG_RESTORE_REG_PAIR_INDEXED reg1, reg2, ssize
    ld  \reg2, 8(sp)
    ld  \reg1, 0(sp)
    .cfi_restore \reg2
    .cfi_restore \reg1

    addi  sp, sp, \ssize
    .cfi_adjust_cfa_offset -\ssize
.endm

.macro EPILOG_RETURN
    ret
.endm

.macro EMIT_BREAKPOINT
    ebreak
.endm

.macro EPILOG_BRANCH Target
    j \Target
.endm

.macro EPILOG_BRANCH_REG reg
    jr \reg
.endm

//-----------------------------------------------------------------------------
// The Following sets of SAVE_*_REGISTERS expect the memory to be reserved and
// base address to be passed in $reg
//

// Reserve 64 bytes of memory before calling  SAVE_CALLEESAVED_REGISTERS
.macro SAVE_CALLEESAVED_REGISTERS reg, ofs
    PROLOG_SAVE_REG_PAIR s1, s2, \ofs + 16
    PROLOG_SAVE_REG_PAIR s3, s4, \ofs + 32
    PROLOG_SAVE_REG_PAIR s5, s6, \ofs + 48
    PROLOG_SAVE_REG_PAIR s7, s8, \ofs + 64
    PROLOG_SAVE_REG_PAIR s9, s10, \ofs + 80
    PROLOG_SAVE_REG_PAIR s11, tp  \ofs + 96
    PROLOG_SAVE_REG      gp,  \ofs + 112
.endm

// Reserve 64 bytes of memory before calling  SAVE_ARGUMENT_REGISTERS
.macro SAVE_ARGUMENT_REGISTERS reg, ofs
    sd  a0, (\ofs)(\reg)
    sd  a1, (\ofs + 8)(\reg)
    sd  a2, (\ofs + 16)(\reg)
    sd  a3, (\ofs + 24)(\reg)
    sd  a4, (\ofs + 32)(\reg)
    sd  a5, (\ofs + 40)(\reg)
    sd  a6, (\ofs + 48)(\reg)
    sd  a7, (\ofs + 56)(\reg)
.endm

// Reserve 64 bytes of memory before calling  SAVE_FLOAT_ARGUMENT_REGISTERS
.macro SAVE_FLOAT_ARGUMENT_REGISTERS reg, ofs
    fsd  fa0, (\ofs)(\reg)
    fsd  fa1, (\ofs + 8)(\reg)
    fsd  fa2, (\ofs + 16)(\reg)
    fsd  fa3, (\ofs + 24)(\reg)
    fsd  fa4, (\ofs + 32)(\reg)
    fsd  fa5, (\ofs + 40)(\reg)
    fsd  fa6, (\ofs + 48)(\reg)
    fsd  fa7, (\ofs + 56)(\reg)
.endm

// Reserve 64 bytes of memory before calling  SAVE_FLOAT_CALLEESAVED_REGISTERS
.macro SAVE_FLOAT_CALLEESAVED_REGISTERS reg, ofs
// TODO RISCV NYI
    sw  ra, 0(zero)
.endm

.macro RESTORE_CALLEESAVED_REGISTERS reg, ofs
    EPILOG_RESTORE_REG      gp  \ofs + 112
    EPILOG_RESTORE_REG_PAIR s11, tp  \ofs + 96
    EPILOG_RESTORE_REG_PAIR s9, s10, \ofs + 80 
    EPILOG_RESTORE_REG_PAIR s7, s8,  \ofs + 64
    EPILOG_RESTORE_REG_PAIR s5, s6,  \ofs + 48
    EPILOG_RESTORE_REG_PAIR s3, s4,  \ofs + 32
    EPILOG_RESTORE_REG_PAIR s1, s2,  \ofs + 16
.endm

.macro RESTORE_ARGUMENT_REGISTERS reg, ofs
    ld  a0, (\ofs)(\reg)
    ld  a1, (\ofs + 8)(\reg)
    ld  a2, (\ofs + 16)(\reg)
    ld  a3, (\ofs + 24)(\reg)
    ld  a4, (\ofs + 32)(\reg)
    ld  a5, (\ofs + 40)(\reg)
    ld  a6, (\ofs + 48)(\reg)
    ld  a7, (\ofs + 56)(\reg)
.endm

.macro RESTORE_FLOAT_ARGUMENT_REGISTERS reg, ofs
    fld  fa0, (\ofs)(\reg)
    fld  fa1, (\ofs + 8)(\reg)
    fld  fa2, (\ofs + 16)(\reg)
    fld  fa3, (\ofs + 24)(\reg)
    fld  fa4, (\ofs + 32)(\reg)
    fld  fa5, (\ofs + 40)(\reg)
    fld  fa6, (\ofs + 48)(\reg)
    fld  fa7, (\ofs + 56)(\reg)
.endm

.macro RESTORE_FLOAT_CALLEESAVED_REGISTERS reg, ofs
// TODO RISCV NYI
    sw  ra, 0(zero)
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
.macro PROLOG_WITH_TRANSITION_BLOCK extraParameters = 0, extraLocals = 0, SaveFPRegs = 1
    __PWTB_SaveFPArgs = \SaveFPRegs

    __PWTB_FloatArgumentRegisters = \extraLocals

    // Note, stack (see __PWTB_StackAlloc variable) must be 16 byte aligned,
    // SIZEOF__FloatArgumentRegisters (0x40) is 16 byte aligned, that mean initial
    // __PWTB_FloatArgumentRegisters value must be not 16 byte aligned and
    // after add (120 + 64) provide 16 byte aligned result.
    .if ((__PWTB_FloatArgumentRegisters % 16) == 0)
        __PWTB_FloatArgumentRegisters = __PWTB_FloatArgumentRegisters + 8
    .endif

    __PWTB_TransitionBlock = __PWTB_FloatArgumentRegisters

    .if (__PWTB_SaveFPArgs == 1)
        __PWTB_TransitionBlock = __PWTB_TransitionBlock + SIZEOF__FloatArgumentRegisters
    .endif


    __PWTB_CalleeSavedRegisters = __PWTB_TransitionBlock
    __PWTB_ArgumentRegisters = __PWTB_TransitionBlock + 120

    // Including fp, ra, s1-s11, tp, gp, and (a0-a7)arguments. (1+1+11+1+1)*8 + 8*8.
    __PWTB_StackAlloc = __PWTB_TransitionBlock + 120 + 64
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

//-----------------------------------------------------------------------------
// Macro used to check (in debug builds only) whether the stack is 16-bytes aligned (a requirement before calling
// out into C++/OS code). Invoke this directly after your prolog (if the stack frame size is fixed) or directly
// before a call (if you have a frame pointer and a dynamic stack). A breakpoint will be invoked if the stack
// is misaligned.
//
.macro  CHECK_STACK_ALIGNMENT

#ifdef _DEBUG
        andi    t4, sp, 0xf
        beq     t4, zero, 0f
        EMIT_BREAKPOINT
0:
#endif
.endm
