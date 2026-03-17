;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc


EXTERN RhpCidResolve : PROC
EXTERN RhpUniversalTransitionTailCall : PROC
EXTERN RhpUniversalTransitionGuardedTailCall : PROC

EXTERN g_pDispatchCache : QWORD

EXTERN __guard_dispatch_icall_fptr : QWORD

;; Macro that generates an interface dispatch stub.
;; DispatchName: the name of the dispatch entry point
;; Guarded: if non-zero, validate indirect call targets using Control Flow Guard
INTERFACE_DISPATCH macro DispatchName, Guarded

LEAF_ENTRY DispatchName, _TEXT

        ;; Load the MethodTable from the object instance in rcx.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of the dispatch stub and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        mov     r10, [rcx]

        ;; r11 currently contains the indirection cell address.
        cmp     qword ptr [r11], r10 ;; is this the monomorhpic MethodTable?
        jne     @F

        mov     rax, [r11 + 8] ;; load the cached monomorphic resolved code address into rax
if Guarded ne 0
        jmp     [__guard_dispatch_icall_fptr]
else
        jmp     rax
endif

      @@:

        ;; r11 contains indirection cell address
        lea     r10, RhpCidResolve
if Guarded ne 0
        jmp     RhpUniversalTransitionGuardedTailCall
else
        jmp     RhpUniversalTransitionTailCall
endif

LEAF_END DispatchName, _TEXT

        endm

        INTERFACE_DISPATCH RhpInterfaceDispatch, 0
        INTERFACE_DISPATCH RhpInterfaceDispatchGuarded, 1

end
