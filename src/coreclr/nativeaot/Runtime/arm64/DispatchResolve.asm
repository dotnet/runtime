;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

    EXTERN RhpCidResolve
    EXTERN RhpUniversalTransitionTailCall
    EXTERN RhpUniversalTransitionGuardedTailCall

;; Macro that generates an interface dispatch stub.
;; DispatchName: the name of the dispatch entry point
;; TransitionTarget: the UniversalTransition variant to jump to
    MACRO
        INTERFACE_DISPATCH $DispatchName, $TransitionTarget

    LEAF_ENTRY $DispatchName, _TEXT

        ;; Load the MethodTable from the object instance in x0.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of the dispatch stub and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        ldr     x12, [x0]

        ldr     xip0, =RhpCidResolve
        mov     xip1, x11
        b       $TransitionTarget

    LEAF_END $DispatchName

    MEND

    INTERFACE_DISPATCH RhpInterfaceDispatch, RhpUniversalTransitionTailCall
    INTERFACE_DISPATCH RhpInterfaceDispatchGuarded, RhpUniversalTransitionGuardedTailCall

    END
