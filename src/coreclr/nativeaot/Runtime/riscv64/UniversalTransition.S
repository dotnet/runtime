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

        // Save FP and RA registers
        PROLOG_SAVE_REG_PAIR_INDEXED s10, ra, STACK_SIZE

        // Save Floating Point registers
        fsd f0, 0x100(sp)         // Save f0
        fsd f1, 0x108(sp)         // Save f1
        fsd f2, 0x110(sp)         // Save f2
        fsd f3, 0x118(sp)         // Save f3
        fsd f4, 0x120(sp)         // Save f4
        fsd f5, 0x128(sp)         // Save f5
        fsd f6, 0x130(sp)         // Save f6
        fsd f7, 0x138(sp)         // Save f7

        // Space for return buffer data (0x40 bytes)

        // Save argument registers
        sd a0, 0x140(sp)         // Save a0
        sd a1, 0x148(sp)         // Save a1
        sd a2, 0x150(sp)         // Save a2
        sd a3, 0x158(sp)         // Save a3
        sd a4, 0x160(sp)         // Save a4
        sd a5, 0x168(sp)         // Save a5
        sd a6, 0x170(sp)         // Save a6
        sd a7, 0x178(sp)         // Save a7
        sd tp, 0x180(sp)         // Save tp

        // Optionally prepare the values to trash saved argument registers
    #ifdef TRASH_SAVED_ARGUMENT_REGISTERS
        PREPARE_EXTERNAL_VAR RhpFpTrashValues, t0

        fld f0, 0(t0)              // Load f0 from t0
        fld f1, 0x08(t0)           // Load f1 from t0
        fld f2, 0x10(t0)           // Load f2 from t0
        fld f3, 0x18(t0)           // Load f3 from t0
        fld f4, 0x20(t0)           // Load f4 from t0
        fld f5, 0x28(t0)           // Load f5 from t0
        fld f6, 0x30(t0)           // Load f6 from t0
        fld f7, 0x38(t0)           // Load f7 from t0

        PREPARE_EXTERNAL_VAR RhpIntegerTrashValues, t0

        ld a0, 0x10(t0)           // Load a0 from t0
        ld a1, 0x18(t0)           // Load a1 from t0
        ld a2, 0x20(t0)           // Load a2 from t0
        ld a3, 0x28(t0)           // Load a3 from t0
        ld a4, 0x30(t0)           // Load a4 from t0
        ld a5, 0x38(t0)           // Load a5 from t0
        ld a6, 0x40(t0)           // Load a6 from t0
        ld a7, 0x48(t0)           // Load a7 from t0
    #endif // TRASH_SAVED_ARGUMENT_REGISTERS

        addi a0, sp, DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK  // First parameter to target function
        mv a1, t1                                         // Second parameter to target function
        jalr t0, t1, 0                                    // Jump to the function in t1

        // Restore the result address from t2
        mv t2, a0                                       // Move result to t2

        // Restore Floating Point registers
        fld f0, 0x100(sp)         // Restore f0
        fld f1, 0x108(sp)         // Restore f1
        fld f2, 0x110(sp)         // Restore f2
        fld f3, 0x118(sp)         // Restore f3
        fld f4, 0x120(sp)         // Restore f4
        fld f5, 0x128(sp)         // Restore f5
        fld f6, 0x130(sp)         // Restore f6
        fld f7, 0x138(sp)         // Restore f7

        // Restore argument registers
        ld a0, 0x140(sp)         // Restore a0
        ld a1, 0x148(sp)         // Restore a1
        ld a2, 0x150(sp)         // Restore a2
        ld a3, 0x158(sp)         // Restore a3
        ld a4, 0x160(sp)         // Restore a4
        ld a5, 0x168(sp)         // Restore a5
        ld a6, 0x170(sp)         // Restore a6
        ld a7, 0x178(sp)         // Restore a7
        ld tp, 0x180(sp)         // Restore tp

        // Restore FP and RA registers, and free the allocated stack block
        EPILOG_RESTORE_REG_PAIR_INDEXED s10, ra, STACK_SIZE

        // Tailcall to the target address in t2
        jalr t2, 0

    NESTED_END Rhp\FunctionName, _TEXT

    .endm


    // To enable proper step-in behavior in the debugger, we need to have two instances
    // of the thunk. For the first one, the debugger steps into the call in the function,
    // for the other, it steps over it.
    UNIVERSAL_TRANSITION UniversalTransition
    UNIVERSAL_TRANSITION UniversalTransition_DebugStepTailCall
