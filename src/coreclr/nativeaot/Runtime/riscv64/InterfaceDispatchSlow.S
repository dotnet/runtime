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
// At input to this function, a0-a7, fa0-fa7 and the stack may contain any number of arguments, and t5 contains the
// interface dispatch cell.
//
// NOTE: StackFrameIterator::UnwindInterfaceDispatchSlowThunk hard-codes the frame layout. The stack walker reports
// everything between the base of the TransitionBlock and the top of the stack passed arguments conservatively.
//
    NESTED_ENTRY RhpInterfaceDispatchSlow, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

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

        addi        a0, sp, __PWTB_TransitionBlock
        mv          a1, t5
        call        C_FUNC(RhpCidResolve)

ALTERNATE_ENTRY ReturnFromInterfaceDispatchSlow

        # Move the result (the target address) to t2 so it doesn't get overridden when we restore the
        # argument registers.
        mv          t2, a0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        jr          t2

    NESTED_END RhpInterfaceDispatchSlow, _TEXT
