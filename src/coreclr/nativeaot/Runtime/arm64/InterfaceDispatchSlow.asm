;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    EXTERN RhpCidResolve
    EXTERN __guard_check_icall_fptr

    TEXTAREA

;;
;; RhpInterfaceDispatchSlow / RhpInterfaceDispatchSlowGuarded
;;
;; Saves the arguments in a TransitionBlock (see PROLOG_WITH_TRANSITION_BLOCK), calls RhpCidResolve to resolve the
;; interface dispatch cell and then jumps to the resolved target with the original arguments. The guarded variant
;; validates the target with Control Flow Guard first.
;;
;; At input to this function, x0-8, q0-7 and the stack may contain any number of arguments, and x11 contains the
;; interface dispatch cell.
;;
;; NOTE: StackFrameIterator::UnwindInterfaceDispatchSlowThunk hard-codes the frame layout. The stack walker reports
;; everything between the base of the TransitionBlock and the top of the stack passed arguments conservatively.
;;
    MACRO
        INTERFACE_DISPATCH_SLOW $FunctionName, $ValidateTarget

    NESTED_ENTRY Rhp$FunctionName

        PROLOG_WITH_TRANSITION_BLOCK

        add         x0, sp, #__PWTB_TransitionBlock
        mov         x1, x11
        bl          RhpCidResolve

    ALTERNATE_ENTRY ReturnFrom$FunctionName

    IF "$ValidateTarget" != ""
        ;; Validate the target address using Control Flow Guard before tail-calling it.
        ;; The validator takes the target in x15 and preserves x0-x8, x15, and all float registers.
        mov         x15, x0
        adrp        x16, __guard_check_icall_fptr
        ldr         x16, [x16, __guard_check_icall_fptr]
        blr         x16
        mov         x0, x15
    ENDIF

        mov         x9, x0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG x9

    NESTED_END Rhp$FunctionName

    MEND

    INTERFACE_DISPATCH_SLOW InterfaceDispatchSlow
    INTERFACE_DISPATCH_SLOW InterfaceDispatchSlowGuarded, ValidateTarget

    END
