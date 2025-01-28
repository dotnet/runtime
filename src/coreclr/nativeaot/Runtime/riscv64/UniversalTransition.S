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
#define RETURN_BLOCK_SIZE (32)

#define COUNT_FLOAT_ARG_REGISTERS (8)
#define FLOAT_REGISTER_SIZE (16)
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

// RhpUniversalTransition
//
// At input to this function, a0-a7, fa0-fa7, and the stack may contain any number of arguments.
//
// In addition, there are 2 extra arguments passed in the intra-procedure-call scratch registers:
//  a2 will contain the managed function that is to be called by this transition function
//  a1 will contain the pointer-sized extra argument to the managed function
//
// When invoking the callee:
//
//  a0 shall contain a pointer to the TransitionBlock
//  a1 shall contain the value that was in a1 t2 entry to this function
//
// Frame layout is:
//
//  {StackPassedArgs}                           ChildSP+0F0     CallerSP+000
//  {IntArgRegs (a0-a7) (0x40 bytes)}           ChildSP+0B0     CallerSP-040
//  {ReturnBlock (0x20 bytes)}                  ChildSP+090     CallerSP-060
//   -- The base address of the Return block is the TransitionBlock pointer, the floating point args are
//      in the neg space of the TransitionBlock pointer.  Note that the callee has knowledge of the exact
//      layout of all pieces of the frame that lie at or above the pushed floating point registers.
//  {FpArgRegs (fa0-fa7) (0x80 bytes)}          ChildSP+010     CallerSP-0E0
//  {PushedRA}                                  ChildSP+008     CallerSP-0E8
//  {PushedFP}                                  ChildSP+000     CallerSP-0F0
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

    .text

    .macro UNIVERSAL_TRANSITION FunctionName

    NESTED_ENTRY Rhp\FunctionName, _TEXT, NoHandler

        // Save FP and RA registers
        addi    sp, sp, -272            // Allocate stack space for saved registers and other data
        sd      ra, 264(sp)             // Save return address
        sd      fp, 256(sp)             // Save frame pointer
        mv      fp, sp                  // Set frame pointer to current stack pointer

        // Save floating-point registers (fa0-fa7)
        fsd     fa0, 0(sp)
        fsd     fa1, 8(sp)
        fsd     fa2, 16(sp)
        fsd     fa3, 24(sp)
        fsd     fa4, 32(sp)
        fsd     fa5, 40(sp)
        fsd     fa6, 48(sp)
        fsd     fa7, 56(sp)

        // Space for return buffer data (0x40 bytes)

        // Save argument registers (a0 - a7)
        sd      a0, 64(sp)
        sd      a1, 72(sp)
        sd      a2, 80(sp)
        sd      a3, 88(sp)
        sd      a4, 96(sp)
        sd      a5, 104(sp)
        sd      a6, 112(sp)
        sd      a7, 120(sp)

    #ifdef TRASH_SAVED_ARGUMENT_REGISTERS
        PREPARE_EXTERNAL_VAR RhpFpTrashValues, t1

        fld     fa0, 0(t1)
        fld     fa1, 8(t1)
        fld     fa2, 16(t1)
        fld     fa3, 24(t1)
        fld     fa4, 32(t1)
        fld     fa5, 40(t1)
        fld     fa6, 48(t1)
        fld     fa7, 56(t1)

        PREPARE_EXTERNAL_VAR RhpIntegerTrashValues, t1

        ld      t2, 0x10(t1)
        ld      t3, 0x18(t1)
        ld      t4, 0x20(t1)
    #endif // TRASH_SAVED_ARGUMENT_REGISTERS

        addi    a0, sp, 144             // First parameter to target function is a pointer to the return block
        mv      a1, t2                  // Second parameter to target function remains in a1
        jalr    a2                      // Jump to the managed target address in a2

    ALTERNATE_ENTRY ReturnFrom\FunctionName

        // Move the result (the target address) to a temporary register so it doesn't get overridden
        mv      t1, a0

        // Restore floating-point registers (fa0-fa7)
        fld     fa0, 0(sp)
        fld     fa1, 8(sp)
        fld     fa2, 16(sp)
        fld     fa3, 24(sp)
        fld     fa4, 32(sp)
        fld     fa5, 40(sp)
        fld     fa6, 48(sp)
        fld     fa7, 56(sp)

        // Restore argument registers (a0 - a7)
        ld      a0, 64(sp)
        ld      a1, 72(sp)
        ld      a2, 80(sp)
        ld      a3, 88(sp)
        ld      a4, 96(sp)
        ld      a5, 104(sp)
        ld      a6, 112(sp)
        ld      a7, 120(sp)

        // Restore FP and RA registers, and free the allocated stack block
        ld      ra, 264(sp)             // Restore return address
        ld      fp, 256(sp)             // Restore frame pointer
        addi    sp, sp, 272             // Adjust stack pointer back to original

        // Perform the return to caller (via jump)
        jr      t1                      // Jump to the target address

    NESTED_END Rhp\FunctionName, _TEXT

    .endm

    // To enable proper step-in behavior in the debugger, we need to have two instances
    // of the thunk. For the first one, the debugger steps into the call in the function,
    // for the other, it steps over it.
    UNIVERSAL_TRANSITION UniversalTransition
    UNIVERSAL_TRANSITION UniversalTransition_DebugStepTailCall
