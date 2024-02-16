;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

ifdef FEATURE_DYNAMIC_CODE

ifdef _DEBUG
TRASH_SAVED_ARGUMENT_REGISTERS equ 1
else
TRASH_SAVED_ARGUMENT_REGISTERS equ 0
endif

if TRASH_SAVED_ARGUMENT_REGISTERS ne 0
EXTERN RhpIntegerTrashValues    : QWORD
EXTERN RhpFpTrashValues         : QWORD
endif ;; TRASH_SAVED_ARGUMENT_REGISTERS

SIZEOF_RETADDR                  equ 8h

SIZEOF_ALIGNMENT_PADDING        equ 8h

SIZEOF_RETURN_BLOCK             equ 10h    ; for 16 bytes of conservatively reported space that the callee can
                                           ; use to manage the return value that the call eventually generates

SIZEOF_FP_REGS                  equ 40h    ; xmm0-3

SIZEOF_OUT_REG_HOMES            equ 20h    ; Callee register spill

;
; From CallerSP to ChildSP, the stack frame is composed of the following adjacent regions:
;
;       SIZEOF_RETADDR
;       SIZEOF_ALIGNMENT_PADDING
;       SIZEOF_RETURN_BLOCK
;       SIZEOF_FP_REGS
;       SIZEOF_OUT_REG_HOMES
;

DISTANCE_FROM_CHILDSP_TO_FP_REGS                equ SIZEOF_OUT_REG_HOMES

DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK           equ DISTANCE_FROM_CHILDSP_TO_FP_REGS + SIZEOF_FP_REGS

DISTANCE_FROM_CHILDSP_TO_RETADDR                equ DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK + SIZEOF_RETURN_BLOCK + SIZEOF_ALIGNMENT_PADDING

DISTANCE_FROM_CHILDSP_TO_CALLERSP               equ DISTANCE_FROM_CHILDSP_TO_RETADDR + SIZEOF_RETADDR

.errnz DISTANCE_FROM_CHILDSP_TO_CALLERSP mod 16

;;
;; Defines an assembly thunk used to make a transition from managed code to a callee,
;; then (based on the return value from the callee), either returning or jumping to
;; a new location while preserving the input arguments.  The usage of this thunk also
;; ensures arguments passed are properly reported.
;;
;; TODO: This code currently only tailcalls, and does not return.
;;
;; Inputs:
;;      rcx, rdx, r8, r9, stack space: arguments as normal
;;      r10: The location of the target code the UniversalTransition thunk will call
;;      r11: The only parameter to the target function (passed in rdx to callee)
;;

;
; Frame layout is:
;
;   {StackPassedArgs}                           ChildSP+0a0     CallerSP+020
;   {IntArgRegs (rcx,rdx,r8,r9) (0x20 bytes)}   ChildSP+080     CallerSP+000
;   {CallerRetaddr}                             ChildSP+078     CallerSP-008
;   {AlignmentPad (0x8 bytes)}                  ChildSP+070     CallerSP-010
;   {ReturnBlock (0x10 bytes)}                  ChildSP+060     CallerSP-020
;   {FpArgRegs (xmm0-xmm3) (0x40 bytes)}        ChildSP+020     CallerSP-060
;   {CalleeArgumentHomes (0x20 bytes)}          ChildSP+000     CallerSP-080
;   {CalleeRetaddr}                             ChildSP-008     CallerSP-088
;
; NOTE: If the frame layout ever changes, the C++ UniversalTransitionStackFrame structure
; must be updated as well.
;
; NOTE: The callee receives a pointer to the base of the ReturnBlock, and the callee has
; knowledge of the exact layout of all pieces of the frame that lie at or above the pushed
; FpArgRegs.
;
; NOTE: The stack walker guarantees that conservative GC reporting will be applied to
; everything between the base of the ReturnBlock and the top of the StackPassedArgs.
;

UNIVERSAL_TRANSITION macro FunctionName

NESTED_ENTRY Rhp&FunctionName, _TEXT

        alloc_stack DISTANCE_FROM_CHILDSP_TO_RETADDR

        save_reg_postrsp    rcx,   0h + DISTANCE_FROM_CHILDSP_TO_CALLERSP
        save_reg_postrsp    rdx,   8h + DISTANCE_FROM_CHILDSP_TO_CALLERSP
        save_reg_postrsp    r8,   10h + DISTANCE_FROM_CHILDSP_TO_CALLERSP
        save_reg_postrsp    r9,   18h + DISTANCE_FROM_CHILDSP_TO_CALLERSP

        save_xmm128_postrsp xmm0, DISTANCE_FROM_CHILDSP_TO_FP_REGS
        save_xmm128_postrsp xmm1, DISTANCE_FROM_CHILDSP_TO_FP_REGS + 10h
        save_xmm128_postrsp xmm2, DISTANCE_FROM_CHILDSP_TO_FP_REGS + 20h
        save_xmm128_postrsp xmm3, DISTANCE_FROM_CHILDSP_TO_FP_REGS + 30h

        END_PROLOGUE

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

        ;
        ; Call out to the target, while storing and reporting arguments to the GC.
        ;
        mov  rdx, r11
        lea  rcx, [rsp + DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK]
        call r10

ALTERNATE_ENTRY ReturnFrom&FunctionName

        ; We cannot make the label public as that tricks DIA stackwalker into thinking
        ; it's the beginning of a method. For this reason we export the address
        ; by means of an auxiliary variable.

        ; restore fp argument registers
        movdqa          xmm0, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS      ]
        movdqa          xmm1, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 10h]
        movdqa          xmm2, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 20h]
        movdqa          xmm3, [rsp + DISTANCE_FROM_CHILDSP_TO_FP_REGS + 30h]

        ; restore integer argument registers
        mov             rcx, [rsp +  0h + DISTANCE_FROM_CHILDSP_TO_CALLERSP]
        mov             rdx, [rsp +  8h + DISTANCE_FROM_CHILDSP_TO_CALLERSP]
        mov             r8,  [rsp + 10h + DISTANCE_FROM_CHILDSP_TO_CALLERSP]
        mov             r9,  [rsp + 18h + DISTANCE_FROM_CHILDSP_TO_CALLERSP]

        ; epilog
        nop

        ; Pop the space that was allocated between the ChildSP and the caller return address.
        add             rsp, DISTANCE_FROM_CHILDSP_TO_RETADDR

        TAILJMP_RAX

NESTED_END Rhp&FunctionName, _TEXT

        endm

        ; To enable proper step-in behavior in the debugger, we need to have two instances
        ; of the thunk. For the first one, the debugger steps into the call in the function,
        ; for the other, it steps over it.
        UNIVERSAL_TRANSITION UniversalTransition
        UNIVERSAL_TRANSITION UniversalTransition_DebugStepTailCall

endif

end
