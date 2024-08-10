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
        fsd    fa0, FLOAT_ARG_OFFSET($sp)
        fsd    fa1, FLOAT_ARG_OFFSET+8($sp)
        fsd    fa2, FLOAT_ARG_OFFSET+16($sp)
        fsd    fa3, FLOAT_ARG_OFFSET+24($sp)
        fsd    fa4, FLOAT_ARG_OFFSET+32($sp)
        fsd    fa5, FLOAT_ARG_OFFSET+40($sp)
        fsd    fa6, FLOAT_ARG_OFFSET+48($sp)
        fsd    fa7, FLOAT_ARG_OFFSET+56($sp)

        // Space for return buffer data (0x40 bytes)

        // Save argument registers
        sd     a0, ARGUMENT_REGISTERS_OFFSET($sp)
        sd     a1, ARGUMENT_REGISTERS_OFFSET+8($sp)
        sd     a2, ARGUMENT_REGISTERS_OFFSET+16($sp)
        sd     a3, ARGUMENT_REGISTERS_OFFSET+24($sp)
        sd     a4, ARGUMENT_REGISTERS_OFFSET+32($sp)
        sd     a5, ARGUMENT_REGISTERS_OFFSET+40($sp)
        sd     a6, ARGUMENT_REGISTERS_OFFSET+48($sp)
        sd     a7, ARGUMENT_REGISTERS_OFFSET+56($sp)
        sd     t0, ARGUMENT_REGISTERS_OFFSET+64($sp)
        sd     t1, ARGUMENT_REGISTERS_OFFSET+72($sp)

#ifdef TRASH_SAVED_ARGUMENT_REGISTERS
        PREPARE_EXTERNAL_VAR RhpFpTrashValues, a1

        fld    fa0, 0(a1)
        fld    fa1, 8(a1)
        fld    fa2, 16(a1)
        fld    fa3, 24(a1)
        fld    fa4, 32(a1)
        fld    fa5, 40(a1)
        fld    fa6, 48(a1)
        fld    fa7, 56(a1)

        PREPARE_EXTERNAL_VAR RhpIntegerTrashValues, a1

        ld     a2, 16(a1)
        ld     a3, 24(a1)
        ld     a4, 32(a1)
        ld     a5, 40(a1)
        ld     a6, 48(a1)
        ld     a7, 56(a1)
#endif // TRASH_SAVED_ARGUMENT_REGISTERS

        addi   a0, sp, DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK // First parameter to target function is a pointer to the return block
        mv     a1, t1                                         // Second parameter to target function
        jalr   t0, t1, 0

        // We cannot make the label public as that tricks DIA stackwalker into thinking
        // it's the beginning of a method. For this reason we export an auxiliary variable
        // holding the address instead.
    ALTERNATE_ENTRY ReturnFrom\FunctionName

        // Move the result (the target address) to t2 so it doesn't get overridden when we restore the
        // argument registers.
        mv     t2, a0

        // Restore floating point registers
        fld    fa0, FLOAT_ARG_OFFSET($sp)
        fld    fa1, FLOAT_ARG_OFFSET+8($sp)
        fld    fa2, FLOAT_ARG_OFFSET+16($sp)
        fld    fa3, FLOAT_ARG_OFFSET+24($sp)
        fld    fa4, FLOAT_ARG_OFFSET+32($sp)
        fld    fa5, FLOAT_ARG_OFFSET+40($sp)
        fld    fa6, FLOAT_ARG_OFFSET+48($sp)
        fld    fa7, FLOAT_ARG_OFFSET+56($sp)

        // Restore the argument registers
        ld     a0, ARGUMENT_REGISTERS_OFFSET($sp)
        ld     a1, ARGUMENT_REGISTERS_OFFSET+8($sp)
        ld     a2, ARGUMENT_REGISTERS_OFFSET+16($sp)
        ld     a3, ARGUMENT_REGISTERS_OFFSET+24($sp)
        ld     a4, ARGUMENT_REGISTERS_OFFSET+32($sp)
        ld     a5, ARGUMENT_REGISTERS_OFFSET+40($sp)
        ld     a6, ARGUMENT_REGISTERS_OFFSET+48($sp)
        ld     a7, ARGUMENT_REGISTERS_OFFSET+56($sp)
        ld     t0, ARGUMENT_REGISTERS_OFFSET+64($sp)

        // Restore FP and RA registers, and free the allocated stack block
        EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, STACK_SIZE

        // Tailcall to the target address.
        jalr   t2, t2, 0

    NESTED_END Rhp\FunctionName, _TEXT

    .endm

    // To enable proper step-in behavior in the debugger, we need to have two instances
    // of the thunk. For the first one, the debugger steps into the call in the function,
    // for the other, it steps over it.
    UNIVERSAL_TRANSITION UniversalTransition
    UNIVERSAL_TRANSITION UniversalTransition_DebugStepTailCall
