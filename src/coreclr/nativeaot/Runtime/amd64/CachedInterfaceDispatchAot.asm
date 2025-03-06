;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

EXTERN RhpCidResolve : PROC
EXTERN RhpUniversalTransition_DebugStepTailCall : PROC

EXTERN RhpCidResolve : PROC
EXTERN RhpUniversalTransition_DebugStepTailCall : PROC


;; Stub dispatch routine for dispatch to a vtable slot
LEAF_ENTRY RhpVTableOffsetDispatch, _TEXT
        ;; r11 currently contains the indirection cell address.
        ;; load rax to point to the vtable offset (which is stored in the m_pCache field).
        mov     rax, [r11 + OFFSETOF__InterfaceDispatchCell__m_pCache]

        ;; Load the MethodTable from the object instance in rcx, and add it to the vtable offset
        ;; to get the address in the vtable of what we want to dereference
    ALTERNATE_ENTRY RhpVTableOffsetDispatchAVLocation
        add     rax, [rcx]

        ;; Load the target address of the vtable into rax
        mov     rax, [rax]

        TAILJMP_RAX
LEAF_END RhpVTableOffsetDispatch, _TEXT

;; Cache miss case, call the runtime to resolve the target and update the cache.
;; Use universal transition helper to allow an exception to flow out of resolution
LEAF_ENTRY RhpInterfaceDispatchSlow, _TEXT
        ;; r11 contains indirection cell address
        lea r10, RhpCidResolve
        jmp RhpUniversalTransition_DebugStepTailCall

LEAF_END RhpInterfaceDispatchSlow, _TEXT

end
