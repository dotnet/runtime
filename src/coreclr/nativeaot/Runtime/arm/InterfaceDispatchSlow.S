// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include <unixasmmacros.inc>

#ifdef _DEBUG
#define TRASH_SAVED_ARGUMENT_REGISTERS
#endif

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// RhpInterfaceDispatchSlow
//
// Saves the arguments in a TransitionBlock (see PROLOG_WITH_TRANSITION_BLOCK), calls RhpCidResolve to resolve the
// interface dispatch cell and then jumps to the resolved target with the original arguments.
//
// At input to this function, r0-3, d0-7 and the stack may contain any number of arguments, and r12 contains the
// interface dispatch cell.
//
// NOTE: StackFrameIterator::UnwindInterfaceDispatchSlowThunk hard-codes the frame layout. The stack walker reports
// everything between the base of the TransitionBlock and the top of the stack passed arguments conservatively.
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY RhpInterfaceDispatchSlow, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

#ifdef TRASH_SAVED_ARGUMENT_REGISTERS

        // Before calling out, trash all of the argument registers except the ones (r0, r1) that
        // hold outgoing arguments.  All of these registers have been saved to the transition
        // frame, and the code at the call target is required to use only the transition frame
        // copies when dispatching this call to the eventual callee.

        PREPARE_EXTERNAL_VAR RhpFpTrashValues, r3
        vldr        d0, [r3, #(0 * 8)]
        vldr        d1, [r3, #(1 * 8)]
        vldr        d2, [r3, #(2 * 8)]
        vldr        d3, [r3, #(3 * 8)]
        vldr        d4, [r3, #(4 * 8)]
        vldr        d5, [r3, #(5 * 8)]
        vldr        d6, [r3, #(6 * 8)]
        vldr        d7, [r3, #(7 * 8)]

        PREPARE_EXTERNAL_VAR RhpIntegerTrashValues, r3
        ldr         r2, [r3, #(2 * 4)]
        ldr         r3, [r3, #(3 * 4)]

#endif // TRASH_SAVED_ARGUMENT_REGISTERS

        add         r0, sp, #__PWTB_TransitionBlock
        mov         r1, r12

        // Make the ReturnFromInterfaceDispatchSlow label 4 byte aligned
        .balign 4
        bl          C_FUNC(RhpCidResolve)

GLOBAL_LABEL ReturnFromInterfaceDispatchSlow

        // Move the result (the target address) to r12 so it doesn't get overridden when we restore the
        // argument registers. Additionally make sure the thumb2 bit is set.
        orr         r12, r0, #1

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG r12

NESTED_END RhpInterfaceDispatchSlow, _TEXT
