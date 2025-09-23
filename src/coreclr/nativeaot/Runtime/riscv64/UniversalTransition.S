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

#define COUNT_ARG_REGISTERS (8)
#define INTEGER_REGISTER_SIZE (8)
#define ARGUMENT_REGISTERS_SIZE (COUNT_ARG_REGISTERS * INTEGER_REGISTER_SIZE)

// Largest return block is 4 doubles
#define RETURN_BLOCK_SIZE 16

#define COUNT_FLOAT_ARG_REGISTERS (8)
#define FLOAT_REGISTER_SIZE (8)
#define FLOAT_ARG_REGISTERS_SIZE (COUNT_FLOAT_ARG_REGISTERS * FLOAT_REGISTER_SIZE)

#define PUSHED_RA_SIZE (8)
#define PUSHED_FP_SIZE (8)

// From CallerSP to ChildSP, the stack frame is composed of the following adjacent regions:
//
//      ARGUMENT_REGISTERS_SIZE
//      RETURN_BLOCK_SIZE
//      FLOAT_ARG_REGISTERS_SIZE
//      PUSHED_RA_SIZE
//      PUSHED_FP_SIZE
//

#define DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK (PUSHED_FP_SIZE + PUSHED_RA_SIZE + FLOAT_ARG_REGISTERS_SIZE)

#define STACK_SIZE (ARGUMENT_REGISTERS_SIZE + RETURN_BLOCK_SIZE + FLOAT_ARG_REGISTERS_SIZE + PUSHED_RA_SIZE + PUSHED_FP_SIZE)

#define FLOAT_ARG_OFFSET (PUSHED_FP_SIZE + PUSHED_RA_SIZE)
#define ARGUMENT_REGISTERS_OFFSET (FLOAT_ARG_OFFSET + FLOAT_ARG_REGISTERS_SIZE + RETURN_BLOCK_SIZE)

//
// RhpUniversalTransition
//
// At input to this function, a0-a7, fa0-fa7, and the stack may contain any number of arguments.
//
// In addition, there are 2 extra arguments passed in the intra-procedure-call scratch register:
//  t0 will contain the managed function that is to be called by this transition function
//  t1 will contain the pointer-sized extra argument to the managed function
//
// When invoking the callee:
//
//  a0 shall contain a pointer to the TransitionBlock
//  a1 shall contain the value that was in t1 at entry to this function
//
// Frame layout is:
//
//  {StackPassedArgs}                           ChildSP+100     CallerSP+000
//  {IntArgRegs (a0-a7) (0x40 bytes)}           ChildSP+0B8     CallerSP-048
//  {ReturnBlock (0x10 bytes)}                  ChildSP+098     CallerSP-068
//   -- The base address of the Return block is the TransitionBlock pointer, the floating point args are
//      in the neg space of the TransitionBlock pointer.  Note that the callee has knowledge of the exact
//      layout of all pieces of the frame that lie at or above the pushed floating point registers.
//  {FpArgRegs (fa0-fa7) (0x40 bytes)}          ChildSP+058     CallerSP-0A8
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

        # FP and RA registers
        PROLOG_SAVE_REG_PAIR_INDEXED fp, ra, STACK_SIZE

        # Floating point registers
        fsd         fa0, FLOAT_ARG_OFFSET(sp)
        fsd         fa1, FLOAT_ARG_OFFSET + 0x08(sp)
        fsd         fa2, FLOAT_ARG_OFFSET + 0x10(sp)
        fsd         fa3, FLOAT_ARG_OFFSET + 0x18(sp)
        fsd         fa4, FLOAT_ARG_OFFSET + 0x20(sp)
        fsd         fa5, FLOAT_ARG_OFFSET + 0x28(sp)
        fsd         fa6, FLOAT_ARG_OFFSET + 0x30(sp)
        fsd         fa7, FLOAT_ARG_OFFSET + 0x38(sp)

        # Space for return block data (0x10 bytes)

        # Save argument registers
        sd          a0, ARGUMENT_REGISTERS_OFFSET(sp)
        sd          a1, ARGUMENT_REGISTERS_OFFSET + 0x08(sp)
        sd          a2, ARGUMENT_REGISTERS_OFFSET + 0x10(sp)
        sd          a3, ARGUMENT_REGISTERS_OFFSET + 0x18(sp)
        sd          a4, ARGUMENT_REGISTERS_OFFSET + 0x20(sp)
        sd          a5, ARGUMENT_REGISTERS_OFFSET + 0x28(sp)
        sd          a6, ARGUMENT_REGISTERS_OFFSET + 0x30(sp)
        sd          a7, ARGUMENT_REGISTERS_OFFSET + 0x38(sp)

#ifdef TRASH_SAVED_ARGUMENT_REGISTERS
        PREPARE_EXTERNAL_VAR RhpFpTrashValues, a1

        fld fa0, 0x00(a1)
        fld fa1, 0x08(a1)
        fld fa2, 0x10(a1)
        fld fa3, 0x18(a1)
        fld fa4, 0x20(a1)
        fld fa5, 0x28(a1)
        fld fa6, 0x30(a1)
        fld fa7, 0x38(a1)

        PREPARE_EXTERNAL_VAR RhpIntegerTrashValues, a1

        ld a2, 0x10(a1)
        ld a3, 0x18(a1)
        ld a4, 0x20(a1)
        ld a5, 0x28(a1)
        ld a6, 0x30(a1)
        ld a7, 0x38(a1)
#endif // TRASH_SAVED_ARGUMENT_REGISTERS

        add         a0, sp, DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK  # First parameter to target function is a pointer to the return block
        mv          a1, t1                                         # Second parameter to target function
        jalr        ra, t0, 0

ALTERNATE_ENTRY ReturnFrom\FunctionName

        # Move the result (the target address) to a2 so it doesn't get overridden when we restore the
        # argument registers.
        mv          t2, a0

        # Restore floating point registers
        fld         fa0, FLOAT_ARG_OFFSET(sp)
        fld         fa1, FLOAT_ARG_OFFSET + 0x08(sp)
        fld         fa2, FLOAT_ARG_OFFSET + 0x10(sp)
        fld         fa3, FLOAT_ARG_OFFSET + 0x18(sp)
        fld         fa4, FLOAT_ARG_OFFSET + 0x20(sp)
        fld         fa5, FLOAT_ARG_OFFSET + 0x28(sp)
        fld         fa6, FLOAT_ARG_OFFSET + 0x30(sp)
        fld         fa7, FLOAT_ARG_OFFSET + 0x38(sp)

        # Restore the argument registers
        ld          a0, ARGUMENT_REGISTERS_OFFSET(sp)
        ld          a1, ARGUMENT_REGISTERS_OFFSET + 0x08(sp)
        ld          a2, ARGUMENT_REGISTERS_OFFSET + 0x10(sp)
        ld          a3, ARGUMENT_REGISTERS_OFFSET + 0x18(sp)
        ld          a4, ARGUMENT_REGISTERS_OFFSET + 0x20(sp)
        ld          a5, ARGUMENT_REGISTERS_OFFSET + 0x28(sp)
        ld          a6, ARGUMENT_REGISTERS_OFFSET + 0x30(sp)
        ld          a7, ARGUMENT_REGISTERS_OFFSET + 0x38(sp)

        # Restore FP and RA registers, and free the allocated stack block
        ld          ra, 0x08(sp)
        ld          s0, 0x0(sp)
        addi        sp, sp, STACK_SIZE

        # Tailcall to the target address.
        jr          t2

    NESTED_END Rhp\FunctionName, _TEXT

    .endm

    // To enable proper step-in behavior in the debugger, we need to have two instances
    // of the thunk. For the first one, the debugger steps into the call in the function,
    // for the other, it steps over it.
    UNIVERSAL_TRANSITION UniversalTransition
    UNIVERSAL_TRANSITION UniversalTransition_DebugStepTailCall
