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

//
// RhpInterfaceDispatchSlow
//
// Saves the arguments in a TransitionBlock (see PROLOG_WITH_TRANSITION_BLOCK), calls RhpCidResolve to resolve the
// interface dispatch cell and then jumps to the resolved target with the original arguments.
//
// At input to this function, x0-8, q0-7 and the stack may contain any number of arguments, and x11 contains the
// interface dispatch cell.
//
// NOTE: StackFrameIterator::UnwindInterfaceDispatchSlowThunk hard-codes the frame layout. The stack walker reports
// everything between the base of the TransitionBlock and the top of the stack passed arguments conservatively.
//
    NESTED_ENTRY RhpInterfaceDispatchSlow, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

#ifdef TRASH_SAVED_ARGUMENT_REGISTERS
        PREPARE_EXTERNAL_VAR RhpFpTrashValues, x1

        ldp q0,q1, [x1, 0x0]
        ldp q2,q3, [x1, 0x20]
        ldp q4,q5, [x1, 0x40]
        ldp q6,q7, [x1, 0x60]

        PREPARE_EXTERNAL_VAR RhpIntegerTrashValues, x1

        ldp x2,x3, [x1, 0x10]
        ldp x4,x5, [x1, 0x20]
        ldp x6,x7, [x1, 0x30]
#endif // TRASH_SAVED_ARGUMENT_REGISTERS

        add         x0, sp, #__PWTB_TransitionBlock
        mov         x1, x11
        bl          C_FUNC(RhpCidResolve)

ALTERNATE_ENTRY ReturnFromInterfaceDispatchSlow

        mov         x9, x0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG x9

    NESTED_END RhpInterfaceDispatchSlow, _TEXT
