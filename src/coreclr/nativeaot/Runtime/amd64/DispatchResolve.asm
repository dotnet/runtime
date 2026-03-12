;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc


EXTERN RhpCidResolve : PROC
EXTERN RhpUniversalTransitionTailCall : PROC

EXTERN g_pDispatchCache : QWORD

;; Dispatching version of RhpResolveInterfaceMethod
LEAF_ENTRY RhpInterfaceDispatch, _TEXT

        ;; Load the MethodTable from the object instance in rcx.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of RhpInterfaceDispatch and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        mov     r10, [rcx]

      RhpInterfaceDispatch_SlowPath:
        ;; r11 contains indirection cell address
        lea     r10, RhpCidResolve
        jmp     RhpUniversalTransitionTailCall

LEAF_END RhpInterfaceDispatch, _TEXT

end
