;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

    EXTERN RhpCidResolve
    EXTERN RhpUniversalTransitionTailCall

    LEAF_ENTRY RhpResolveInterfaceMethodFast, _TEXT

        ;; Load the MethodTable from the object instance in x0.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of RhpResolveInterfaceMethodFast and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        ldr     x12, [x0]

RhpResolveInterfaceMethodFast_SlowPath
        ldr     xip0, =RhpCidResolve
        mov     xip1, x11
        b       RhpUniversalTransitionTailCall

    LEAF_END RhpResolveInterfaceMethodFast

    END
