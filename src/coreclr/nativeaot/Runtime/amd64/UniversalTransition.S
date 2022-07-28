// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include <unixasmmacros.inc>

#ifdef FEATURE_DYNAMIC_CODE

#ifdef _DEBUG
#define TRASH_SAVED_ARGUMENT_REGISTERS 1
#endif

#define SIZEOF_RETADDR              8

#define SIZEOF_RETURN_BLOCK         0x10    // for 16 bytes of conservatively reported space that the callee can
                                            // use to manage the return value that the call eventually generates

#define SIZEOF_ARGUMENT_REGISTERS   0x30    // Callee register spill

//
// From CallerSP to ChildSP, the stack frame is composed of the following adjacent regions:
//
//      SIZEOF_RETADDR
//      SIZEOF_ARGUMENT_REGISTERS
//      SIZEOF_RETURN_BLOCK
//      SIZEOF_FP_REGS
//

#define DISTANCE_FROM_CHILDSP_TO_FP_REGS            0

#define DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK       SIZEOF_FP_REGS

#define DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS (SIZEOF_FP_REGS + SIZEOF_RETURN_BLOCK)

#define DISTANCE_FROM_CHILDSP_TO_RETADDR            (SIZEOF_FP_REGS + SIZEOF_RETURN_BLOCK + SIZEOF_ARGUMENT_REGISTERS + 8)

//
// Defines an assembly thunk used to make a transition from managed code to a callee,
// then (based on the return value from the callee), either returning or jumping to
// a new location while preserving the input arguments.  The usage of this thunk also
// ensures arguments passed are properly reported.
//
// TODO: This code currently only tailcalls, and does not return.
//
// Inputs:
//      rdi, esi, rcx, rdx, r8, r9, stack space: arguments as normal
//      r10: The location of the target code the UniversalTransition thunk will call
//      r11: The only parameter to the target function (passed in rdx to callee)
//

//
// Frame layout is:
//
//  {StackPassedArgs}                           ChildSP+0D0     CallerSP+000
//  {CallerRetaddr}                             ChildSP+0C8     CallerSP-008
//  {AlignmentPad (0x8 bytes)}                  ChildSP+0C0     CallerSP-010
//  {IntArgRegs (0x30 bytes)}                   ChildSP+090     CallerSP-040
//  {ReturnBlock (0x10 bytes)}                  ChildSP+080     CallerSP-050
//  {FpArgRegs (xmm0-xmm7) (0x80 bytes)}        ChildSP+000     CallerSP-0D0
//  {CalleeRetaddr}                             ChildSP-008     CallerSP-0D8
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

.macro UNIVERSAL_TRANSITION FunctionName

NESTED_ENTRY Rhp\FunctionName, _TEXT, NoHandler

        alloc_stack DISTANCE_FROM_CHILDSP_TO_RETADDR

        // save integer argument registers
        mov             [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x00], rdi
        mov             [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x08], rsi
        mov             [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x10], rcx
        mov             [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x18], rdx
        mov             [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x20], r8
        mov             [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x28], r9

        // save fp argument registers
        movdqa          [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x00], xmm0
        movdqa          [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x10], xmm1
        movdqa          [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x20], xmm2
        movdqa          [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x30], xmm3
        movdqa          [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x40], xmm4
        movdqa          [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x50], xmm5
        movdqa          [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x60], xmm6
        movdqa          [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x70], xmm7

#ifdef TRASH_SAVED_ARGUMENT_REGISTERS

        // Before calling out, trash all of the argument registers except the ones (rdi, rsi) that
        // hold outgoing arguments.  All of these registers have been saved to the transition
        // frame, and the code at the call target is required to use only the transition frame
        // copies when dispatching this call to the eventual callee.

        movsd           xmm0, [C_VAR(RhpFpTrashValues) + 0x0]
        movsd           xmm1, [C_VAR(RhpFpTrashValues) + 0x8]
        movsd           xmm2, [C_VAR(RhpFpTrashValues) + 0x10]
        movsd           xmm3, [C_VAR(RhpFpTrashValues) + 0x18]
        movsd           xmm4, [C_VAR(RhpFpTrashValues) + 0x20]
        movsd           xmm5, [C_VAR(RhpFpTrashValues) + 0x28]
        movsd           xmm6, [C_VAR(RhpFpTrashValues) + 0x30]
        movsd           xmm7, [C_VAR(RhpFpTrashValues) + 0x38]

        mov             rcx, qword ptr [C_VAR(RhpIntegerTrashValues) + 0x10]
        mov             rdx, qword ptr [C_VAR(RhpIntegerTrashValues) + 0x18]
        mov             r8,  qword ptr [C_VAR(RhpIntegerTrashValues) + 0x20]
        mov             r9,  qword ptr [C_VAR(RhpIntegerTrashValues) + 0x28]

#endif // TRASH_SAVED_ARGUMENT_REGISTERS

        //
        // Call out to the target, while storing and reporting arguments to the GC.
        //
        mov  rsi, r11
        lea  rdi, [rsp + DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK]
        call r10

        EXPORT_POINTER_TO_ADDRESS PointerToReturnFrom\FunctionName

        // restore fp argument registers
        movdqa          xmm0, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x00]
        movdqa          xmm1, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x10]
        movdqa          xmm2, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x20]
        movdqa          xmm3, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x30]
        movdqa          xmm4, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x40]
        movdqa          xmm5, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x50]
        movdqa          xmm6, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x60]
        movdqa          xmm7, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 0x70]

        // restore integer argument registers
        mov             rdi, [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x00]
        mov             rsi, [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x08]
        mov             rcx, [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x10]
        mov             rdx, [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x18]
        mov             r8,  [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x20]
        mov             r9,  [rsp + DISTANCE_FROM_CHILDSP_TO_ARGUMENT_REGISTERS + 0x28]

        // Pop the space that was allocated between the ChildSP and the caller return address.
        free_stack      DISTANCE_FROM_CHILDSP_TO_RETADDR

        jmp             rax

NESTED_END Rhp\FunctionName, _TEXT

.endm // UNIVERSAL_TRANSITION

        // To enable proper step-in behavior in the debugger, we need to have two instances
        // of the thunk. For the first one, the debugger steps into the call in the function,
        // for the other, it steps over it.
        UNIVERSAL_TRANSITION UniversalTransition
        UNIVERSAL_TRANSITION UniversalTransition_DebugStepTailCall

#endif // FEATURE_DYNAMIC_CODE
