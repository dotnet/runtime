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
// At input to this function, a0-a7, f0-f7 and the stack may contain any number of arguments, and t8 contains the
// interface dispatch cell.
//
// NOTE: StackFrameIterator::UnwindInterfaceDispatchSlowThunk hard-codes the frame layout. The stack walker reports
// everything between the base of the TransitionBlock and the top of the stack passed arguments conservatively.
//
    NESTED_ENTRY RhpInterfaceDispatchSlow, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

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

        addi.d  $a0, $sp, __PWTB_TransitionBlock
        ori     $a1, $t8, 0
        bl      C_FUNC(RhpCidResolve)

    ALTERNATE_ENTRY ReturnFromInterfaceDispatchSlow

        // Move the result (the target address) to t3 so it doesn't get overridden when we restore the
        // argument registers.
        ori     $t3, $a0, 0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        jirl    $r0, $t3, 0

    NESTED_END RhpInterfaceDispatchSlow, _TEXT
