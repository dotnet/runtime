// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

#ifdef _DEBUG
#define TRASH_SAVED_ARGUMENT_REGISTERS
#endif

#ifdef TRASH_SAVED_ARGUMENT_REGISTERS
    .global RhpIntegerTrashValues
    .global RhpFpTrashValues
#endif // TRASH_SAVED_ARGUMENT_REGISTERS

// Padding to account for the odd number of saved integer registers
#define ALIGNMENT_PADDING_SIZE (8)

#define COUNT_ARG_REGISTERS (9)
#define INTEGER_REGISTER_SIZE (8)
#define ARGUMENT_REGISTERS_SIZE (COUNT_ARG_REGISTERS * INTEGER_REGISTER_SIZE)

// Largest return block is 4 doubles
#define RETURN_BLOCK_SIZE (32)

#define COUNT_FLOAT_ARG_REGISTERS (8)
#define FLOAT_REGISTER_SIZE (16)
#define FLOAT_ARG_REGISTERS_SIZE (COUNT_FLOAT_ARG_REGISTERS * FLOAT_REGISTER_SIZE)

#define PUSHED_RA_SIZE (8)
#define PUSHED_FP_SIZE (8)

//
// From CallerSP to ChildSP, the stack frame is composed of the following adjacent regions:
//
//      ALIGNMENT_PADDING_SIZE
//      ARGUMENT_REGISTERS_SIZE
//      RETURN_BLOCK_SIZE
//      FLOAT_ARG_REGISTERS_SIZE
//      PUSHED_RA_SIZE
//      PUSHED_FP_SIZE
//

#define DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK (PUSHED_FP_SIZE + PUSHED_RA_SIZE + FLOAT_ARG_REGISTERS_SIZE)

#define STACK_SIZE (ALIGNMENT_PADDING_SIZE + ARGUMENT_REGISTERS_SIZE + RETURN_BLOCK_SIZE + FLOAT_ARG_REGISTERS_SIZE + PUSHED_RA_SIZE + PUSHED_FP_SIZE)

#define FLOAT_ARG_OFFSET (PUSHED_FP_SIZE + PUSHED_RA_SIZE)
#define ARGUMENT_REGISTERS_OFFSET (FLOAT_ARG_OFFSET + FLOAT_ARG_REGISTERS_SIZE + RETURN_BLOCK_SIZE)

//
// RhpUniversalTransition
//
// At input to this function, a0-7/tp, f0-7 and the stack may contain any number of arguments.
//
// In addition, there are 2 extra arguments passed in the intra-procedure-call scratch register:
//  t7 will contain the managed function that is to be called by this transition function
//  t8 will contain the pointer sized extra argument to the managed function
//
// When invoking the callee:
//
//  a0 shall contain a pointer to the TransitionBlock
//  a1 shall contain the value that was in t8 at entry to this function
//
// Frame layout is:
//
//  {StackPassedArgs}                           ChildSP+100     CallerSP+000
//  {AlignmentPad (0x8 bytes)}                  ChildSP+0F8     CallerSP-008
//  {IntArgRegs (a0-a7/tp) (0x48 bytes)}        ChildSP+0B0     CallerSP-050
//  {ReturnBlock (0x20 bytes)}                  ChildSP+090     CallerSP-070
//   -- The base address of the Return block is the TransitionBlock pointer, the floating point args are
//      in the neg space of the TransitionBlock pointer.  Note that the callee has knowledge of the exact
//      layout of all pieces of the frame that lie at or above the pushed floating point registers.
//  {FpArgRegs (f0-f7) (0x80 bytes)}            ChildSP+010     CallerSP-0F0
//  {PushedRA}                                  ChildSP+008     CallerSP-0F8
//  {PushedFP}                                  ChildSP+000     CallerSP-100
//
// NOTE: If the frame layout ever changes, the C++ UniversalTransitionStackFrame structure
// must be updated as well.
//
// NOTE: The callee receives a pointer to the base of the ReturnBlock, and the callee has
// knowledge of the exact layout of all pieces of the frame that lie at or above the pushed
// FpArgRegs.
//
// NOTE: The stack walker guarantees that conservative GC reporting will be applied to
// everything between the base of the ReturnBlock and the top of the StackPassedArgs.
//

    .text

    .macro UNIVERSAL_TRANSITION FunctionName

    NESTED_ENTRY Rhp\FunctionName, _TEXT, NoHandler

        // FP and RA registers
        PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, STACK_SIZE // ;; Push down stack pointer and store FP and RA

        // Floating point registers
        fst.d  $f0, $sp, FLOAT_ARG_OFFSET
        fst.d  $f1, $sp, FLOAT_ARG_OFFSET + 0x08
        fst.d  $f2, $sp, FLOAT_ARG_OFFSET + 0x10
        fst.d  $f3, $sp, FLOAT_ARG_OFFSET + 0x18
        fst.d  $f4, $sp, FLOAT_ARG_OFFSET + 0x20
        fst.d  $f5, $sp, FLOAT_ARG_OFFSET + 0x28
        fst.d  $f6, $sp, FLOAT_ARG_OFFSET + 0x30
        fst.d  $f7, $sp, FLOAT_ARG_OFFSET + 0x38

        // Space for return buffer data (0x40 bytes)

        // Save argument registers
        st.d  $a0, $sp, ARGUMENT_REGISTERS_OFFSET
        st.d  $a1, $sp, ARGUMENT_REGISTERS_OFFSET + 0x08
        st.d  $a2, $sp, ARGUMENT_REGISTERS_OFFSET + 0x10
        st.d  $a3, $sp, ARGUMENT_REGISTERS_OFFSET + 0x18
        st.d  $a4, $sp, ARGUMENT_REGISTERS_OFFSET + 0x20
        st.d  $a5, $sp, ARGUMENT_REGISTERS_OFFSET + 0x28
        st.d  $a6, $sp, ARGUMENT_REGISTERS_OFFSET + 0x30
        st.d  $a7, $sp, ARGUMENT_REGISTERS_OFFSET + 0x38
        st.d  $tp, $sp, ARGUMENT_REGISTERS_OFFSET + 0x40
        st.d  $r0, $sp, ARGUMENT_REGISTERS_OFFSET + 0x48

#ifdef TRASH_SAVED_ARGUMENT_REGISTERS
        PREPARE_EXTERNAL_VAR RhpFpTrashValues, $a1

        fld.d  $f0, $a1, 0
        fld.d  $f1, $a1, 0x08
        fld.d  $f2, $a1, 0x10
        fld.d  $f3, $a1, 0x18
        fld.d  $f4, $a1, 0x20
        fld.d  $f5, $a1, 0x28
        fld.d  $f6, $a1, 0x30
        fld.d  $f7, $a1, 0x38

        PREPARE_EXTERNAL_VAR RhpIntegerTrashValues, $a1

        ld.d  $a2, $a1, 0x10
        ld.d  $a3, $a1, 0x18
        ld.d  $a4, $a1, 0x20
        ld.d  $a5, $a1, 0x28
        ld.d  $a6, $a1, 0x30
        ld.d  $a7, $a1, 0x38
#endif // TRASH_SAVED_ARGUMENT_REGISTERS

        addi.d  $a0, $sp, DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK  // First parameter to target function is a pointer to the return block
        ori  $a1, $t8, 0                                         // Second parameter to target function
        jirl  $ra, $t7, 0

        // We cannot make the label public as that tricks DIA stackwalker into thinking
        // it's the beginning of a method. For this reason we export an auxiliary variable
        // holding the address instead.
    ALTERNATE_ENTRY ReturnFrom\FunctionName

        // Move the result (the target address) to t3 so it doesn't get overridden when we restore the
        // argument registers.
        ori  $t3, $a0, 0

        // Restore floating point registers
        fld.d  $f0, $sp, FLOAT_ARG_OFFSET
        fld.d  $f1, $sp, FLOAT_ARG_OFFSET + 0x08
        fld.d  $f2, $sp, FLOAT_ARG_OFFSET + 0x10
        fld.d  $f3, $sp, FLOAT_ARG_OFFSET + 0x18
        fld.d  $f4, $sp, FLOAT_ARG_OFFSET + 0x20
        fld.d  $f5, $sp, FLOAT_ARG_OFFSET + 0x28
        fld.d  $f6, $sp, FLOAT_ARG_OFFSET + 0x30
        fld.d  $f7, $sp, FLOAT_ARG_OFFSET + 0x38

        // Restore the argument registers
        ld.d  $a0, $sp, ARGUMENT_REGISTERS_OFFSET
        ld.d  $a1, $sp, ARGUMENT_REGISTERS_OFFSET + 0x08
        ld.d  $a2, $sp, ARGUMENT_REGISTERS_OFFSET + 0x10
        ld.d  $a3, $sp, ARGUMENT_REGISTERS_OFFSET + 0x18
        ld.d  $a4, $sp, ARGUMENT_REGISTERS_OFFSET + 0x20
        ld.d  $a5, $sp, ARGUMENT_REGISTERS_OFFSET + 0x28
        ld.d  $a6, $sp, ARGUMENT_REGISTERS_OFFSET + 0x30
        ld.d  $a7, $sp, ARGUMENT_REGISTERS_OFFSET + 0x38
        ld.d  $tp, $sp, ARGUMENT_REGISTERS_OFFSET + 0x40

        // Restore FP and RA registers, and free the allocated stack block
        EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, STACK_SIZE

        // Tailcall to the target address.
        jirl  $r0, $t3, 0

    NESTED_END Rhp\FunctionName, _TEXT

    .endm

    // To enable proper step-in behavior in the debugger, we need to have two instances
    // of the thunk. For the first one, the debugger steps into the call in the function,
    // for the other, it steps over it.
    UNIVERSAL_TRANSITION UniversalTransition
    UNIVERSAL_TRANSITION UniversalTransition_DebugStepTailCall
