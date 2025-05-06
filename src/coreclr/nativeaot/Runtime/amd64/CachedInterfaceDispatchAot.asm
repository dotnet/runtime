;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

EXTERN RhpCidResolve : PROC
EXTERN RhpUniversalTransition_DebugStepTailCall : PROC

EXTERN RhpCidResolve : PROC
EXTERN RhpUniversalTransition_DebugStepTailCall : PROC

;; Cache miss case, call the runtime to resolve the target and update the cache.
;; Use universal transition helper to allow an exception to flow out of resolution
LEAF_ENTRY RhpInterfaceDispatchSlow, _TEXT
        ;; r11 contains indirection cell address
        lea r10, RhpCidResolve
        jmp RhpUniversalTransition_DebugStepTailCall

LEAF_END RhpInterfaceDispatchSlow, _TEXT

end
