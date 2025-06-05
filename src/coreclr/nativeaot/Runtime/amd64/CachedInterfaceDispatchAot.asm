;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

EXTERN CidResolve : PROC
EXTERN UniversalTransition_DebugStepTailCall : PROC

EXTERN CidResolve : PROC
EXTERN UniversalTransition_DebugStepTailCall : PROC

;; Cache miss case, call the runtime to resolve the target and update the cache.
;; Use universal transition helper to allow an exception to flow out of resolution
LEAF_ENTRY InterfaceDispatchSlow, _TEXT
        ;; r11 contains indirection cell address
        lea r10, CidResolve
        jmp UniversalTransition_DebugStepTailCall

LEAF_END InterfaceDispatchSlow, _TEXT

end
