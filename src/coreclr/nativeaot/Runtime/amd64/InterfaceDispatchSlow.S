// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include <unixasmmacros.inc>

#ifdef _DEBUG
#define TRASH_SAVED_ARGUMENT_REGISTERS 1
#endif

//
// RhpInterfaceDispatchSlow
//
// Saves the arguments in a TransitionBlock (see PROLOG_WITH_TRANSITION_BLOCK), calls RhpCidResolve to resolve the
// interface dispatch cell and then jumps to the resolved target with the original arguments.
//
// Inputs:
//      rdi, rsi, rdx, rcx, r8, r9, xmm0-xmm7, stack space: arguments as normal
//      r11: The interface dispatch cell
//
// NOTE: StackFrameIterator::UnwindInterfaceDispatchSlowThunk hard-codes the frame layout. The stack walker reports
// everything between the base of the TransitionBlock and the top of the stack passed arguments conservatively.
//
NESTED_ENTRY RhpInterfaceDispatchSlow, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

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

        lea             rdi, [rsp + __PWTB_TransitionBlock]
        mov             rsi, r11
        call            C_FUNC(RhpCidResolve)

ALTERNATE_ENTRY ReturnFromInterfaceDispatchSlow

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        TAILJMP_RAX

NESTED_END RhpInterfaceDispatchSlow, _TEXT
