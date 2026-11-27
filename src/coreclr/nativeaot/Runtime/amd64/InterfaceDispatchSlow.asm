;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

ifdef _DEBUG
TRASH_SAVED_ARGUMENT_REGISTERS equ 1
else
TRASH_SAVED_ARGUMENT_REGISTERS equ 0
endif

if TRASH_SAVED_ARGUMENT_REGISTERS ne 0
EXTERN RhpIntegerTrashValues    : QWORD
EXTERN RhpFpTrashValues         : QWORD
endif ;; TRASH_SAVED_ARGUMENT_REGISTERS

EXTERN RhpCidResolve : PROC
EXTERN __guard_check_icall_fptr : QWORD

;
; RhpInterfaceDispatchSlow / RhpInterfaceDispatchSlowGuarded
;
; Saves the arguments in a TransitionBlock (see PROLOG_WITH_TRANSITION_BLOCK), calls RhpCidResolve to resolve the
; interface dispatch cell and then jumps to the resolved target with the original arguments. The guarded variant
; validates the target with Control Flow Guard first.
;
; Inputs:
;      rcx, rdx, r8, r9, xmm0-xmm3, stack space: arguments as normal
;      r11: The interface dispatch cell
;
; NOTE: StackFrameIterator::UnwindInterfaceDispatchSlowThunk hard-codes the frame layout. The stack walker reports
; the argument registers homed by the TransitionBlock prolog and the stack passed arguments conservatively.
;
INTERFACE_DISPATCH_SLOW macro FunctionName, ValidateTarget

NESTED_ENTRY Rhp&FunctionName, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK

if TRASH_SAVED_ARGUMENT_REGISTERS ne 0

        ; Before calling out, trash all of the argument registers except the ones (rcx, rdx) that
        ; hold outgoing arguments.  All of these registers have been saved to the transition
        ; frame, and the code at the call target is required to use only the transition frame
        ; copies when dispatching this call to the eventual callee.

        movsd           xmm0, mmword ptr [RhpFpTrashValues + 0h]
        movsd           xmm1, mmword ptr [RhpFpTrashValues + 8h]
        movsd           xmm2, mmword ptr [RhpFpTrashValues + 10h]
        movsd           xmm3, mmword ptr [RhpFpTrashValues + 18h]

        mov             r8, qword ptr [RhpIntegerTrashValues + 10h]
        mov             r9, qword ptr [RhpIntegerTrashValues + 18h]

endif ; TRASH_SAVED_ARGUMENT_REGISTERS

        lea             rcx, [rsp + __PWTB_TransitionBlock]
        mov             rdx, r11
        call            RhpCidResolve

ALTERNATE_ENTRY ReturnFrom&FunctionName

if ValidateTarget ne 0
        ; Validate the target address using Control Flow Guard before tail-calling it.
        ; The validator takes the target in rcx and preserves rcx, r10, and all float registers.
        mov             rcx, rax
        call            [__guard_check_icall_fptr]
        mov             rax, rcx
endif ; ValidateTarget

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        TAILJMP_RAX

NESTED_END Rhp&FunctionName, _TEXT

        endm

        INTERFACE_DISPATCH_SLOW InterfaceDispatchSlow, 0
        INTERFACE_DISPATCH_SLOW InterfaceDispatchSlowGuarded, 1

end
