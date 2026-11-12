;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros.inc

EXTERN RhpCidResolve : PROC

;;
;; RhpInterfaceDispatchSlow
;;
;; Saves the argument registers, calls RhpCidResolve to resolve the interface dispatch cell and then jumps to the
;; resolved target with the original arguments.
;;
;; Inputs:
;;      ecx, edx, stack space two pops down: arguments as normal
;;       first register sized field on the stack is the interface dispatch cell,
;;       followed by the caller's EBP and the return address of the whole method. (This method cannot be called
;;       via a call instruction, it must be jumped to.) The fake entrypoint is in place to
;;       convince the stack walker this is a normal framed function.
;;
;;  NOTE! FOR CORRECTNESS THIS FUNCTION REQUIRES THAT ALL NON-LEAF MANAGED FUNCTIONS HAVE
;;        FRAME POINTERS, OR THE STACK WALKER CAN'T STACKWALK OUT OF HERE
;;

;
; Frame layout is:
;
;   {StackPassedArgs}                           ChildSP+014     CallerSP+000
;   {CallerRetaddr}                             ChildSP+010     CallerSP-004
;   {CallerEBP}                                 ChildSP+00C     CallerSP-008
;   {DispatchCell}                              ChildSP+008     CallerSP-00C
;   {IntArgRegs (edx,ecx) (0x8 bytes)}          ChildSP+000     CallerSP-014
;   {CalleeRetaddr}                             ChildSP-004     CallerSP-018
;
; NOTE: If the frame layout ever changes, the C++ InterfaceDispatchSlowStackFrame structure
; must be updated as well.
;
; NOTE: RhpCidResolve receives a pointer to the base of the pushed IntArgRegs.
;
; NOTE: The stack walker guarantees that conservative GC reporting will be applied to
; everything between the base of the IntArgRegs and the top of the StackPassedArgs.
;

FASTCALL_FUNC RhpInterfaceDispatchSlow_FAKE_ENTRY, 0
        ; Set up an ebp frame
        push        ebp
        mov         ebp, esp
        push eax
ALTERNATE_ENTRY _RhpInterfaceDispatchSlow@0
        push ecx
        push edx

        ;
        ; Call out to RhpCidResolve, while storing and reporting arguments to the GC.
        ;
        mov  edx, [ebp-4]    ; Get the interface dispatch cell
        lea  ecx, [ebp-0Ch]  ; Get pointer to edx value pushed above
        call RhpCidResolve

ALTERNATE_ENTRY _ReturnFromInterfaceDispatchSlow

        pop edx
        pop ecx
        add esp, 4
        pop ebp
        jmp eax

FASTCALL_ENDFUNC

end
