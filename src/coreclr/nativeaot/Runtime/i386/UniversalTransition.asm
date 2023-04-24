;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros.inc

ifdef FEATURE_DYNAMIC_CODE

;;
;; Defines an assembly thunk used to make a transition from managed code to a callee,
;; then (based on the return value from the callee), either returning or jumping to
;; a new location while preserving the input arguments.  The usage of this thunk also
;; ensures arguments passed are properly reported.
;;
;; TODO: This code currently only tailcalls, and does not return.
;;
;; Inputs:
;;      ecx, edx, stack space three pops down: arguments as normal
;;       first register sized fields on the stack is the location of the target code
;;       the UniversalTransitionThunk will call
;;       second register sized field on the stack is the parameter to the target function
;;       followed by the return address of the whole method. (This method cannot be called
;;       via a call instruction, it must be jumped to.) The fake entrypoint is in place to
;;       convince the stack walker this is a normal framed function.
;;
;;  NOTE! FOR CORRECTNESS THIS FUNCTION REQUIRES THAT ALL NON-LEAF MANAGED FUNCTIONS HAVE
;;        FRAME POINTERS, OR THE STACK WALKER CAN'T STACKWALK OUT OF HERE
;;

;
; Frame layout is:
;
;   {StackPassedArgs}                           ChildSP+018     CallerSP+000
;   {CallerRetaddr}                             ChildSP+014     CallerSP-004
;   {CallerEBP}                                 ChildSP+010     CallerSP-008
;   {ReturnBlock (0x8 bytes)}                   ChildSP+008     CallerSP-010
;    -- On input (i.e., when control jumps to RhpUniversalTransition), the low 4 bytes of
;       the ReturnBlock area holds the address of the callee and the high 4 bytes holds the
;       extra argument to pass to the callee.
;   {IntArgRegs (edx,ecx) (0x8 bytes)}          ChildSP+000     CallerSP-018
;   {CalleeRetaddr}                             ChildSP-004     CallerSP-01c
;
; NOTE: If the frame layout ever changes, the C++ UniversalTransitionStackFrame structure
; must be updated as well.
;
; NOTE: The callee receives a pointer to the base of the pushed IntArgRegs, and the callee
; has knowledge of the exact layout of the entire frame.
;
; NOTE: The stack walker guarantees that conservative GC reporting will be applied to
; everything between the base of the IntArgRegs and the top of the StackPassedArgs.
;

UNIVERSAL_TRANSITION macro FunctionName

FASTCALL_FUNC Rhp&FunctionName&_FAKE_ENTRY, 0
        ; Set up an ebp frame
        push        ebp
        mov         ebp, esp
        push eax
        push eax
ALTERNATE_ENTRY Rhp&FunctionName&@0
        push ecx
        push edx

        ;
        ; Call out to the target, while storing and reporting arguments to the GC.
        ;
        mov  eax, [ebp-8]    ; Get the address of the callee
        mov  edx, [ebp-4]    ; Get the extra argument to pass to the callee
        lea  ecx, [ebp-10h]  ; Get pointer to edx value pushed above
        call eax

        EXPORT_POINTER_TO_ADDRESS _PointerToReturnFrom&FunctionName

        ; We cannot make the label public as that tricks DIA stackwalker into thinking
        ; it's the beginning of a method. For this reason we export an auxiliary variable
        ; holding the address instead.

        pop edx
        pop ecx
        add esp, 8
        pop ebp
        jmp eax

FASTCALL_ENDFUNC

        endm

        ; To enable proper step-in behavior in the debugger, we need to have two instances
        ; of the thunk. For the first one, the debugger steps into the call in the function,
        ; for the other, it steps over it.
        UNIVERSAL_TRANSITION UniversalTransition
        UNIVERSAL_TRANSITION UniversalTransition_DebugStepTailCall

endif

end
