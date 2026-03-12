;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros.inc

EXTERN RhpCidResolve : PROC
EXTERN _RhpUniversalTransitionTailCall@0 : PROC

;; Dispatching version of RhpResolveInterfaceMethod
FASTCALL_FUNC RhpInterfaceDispatch, 0
ALTERNATE_ENTRY _RhpInterfaceDispatch

        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of RhpInterfaceDispatch and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        cmp     dword ptr [ecx], ecx

        ;; eax currently contains the indirection cell address.
        ;; Setup call to Universal Transition thunk
        push    ebp
        mov     ebp, esp
        push    eax
        lea     eax, [RhpCidResolve]
        push    eax

        jmp     _RhpUniversalTransitionTailCall@0

FASTCALL_ENDFUNC

end
