;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

    EXTERN RhpCidResolve
    EXTERN RhpUniversalTransitionReturnResult_DebugStepTailCall

    NESTED_ENTRY RhpResolveInterfaceMethodFast, _TEXT

        ldr     xip0, =RhpCidResolve
        mov     xip1, x11
        b       RhpUniversalTransitionReturnResult_DebugStepTailCall

    NESTED_END RhpResolveInterfaceMethodFast

#endif // FEATURE_CACHED_INTERFACE_DISPATCH

    END