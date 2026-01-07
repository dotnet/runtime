;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

    EXTERN RhpCidResolve
    EXTERN RhpUniversalTransitionReturnResult

    NESTED_ENTRY RhpResolveInterfaceMethodFast, _TEXT

        ;; Load the MethodTable from the object instance in x0.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of RhpResolveInterfaceMethodFast and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        ldr     x12, [x0]

        ;; x11 currently contains the indirection cell address.
        ;; load x13 to point to the cache block.
        ldr     x13, [x11, #OFFSETOF__InterfaceDispatchCell__m_pCache]
        and     x14, x13, #IDC_CACHE_POINTER_MASK
        cbnz    x14, RhpResolveInterfaceMethodFast_SlowPath

        add     x14, x13, #OFFSETOF__InterfaceDispatchCache__m_rgEntries
        ldr     x15, [x14]
        cmp     x15, x12
        bne     RhpResolveInterfaceMethodFast_Polymorphic
        ldur    x15, [x14, #8]
        ret

RhpResolveInterfaceMethodFast_Polymorphic
        ldr     w13, [x13, #OFFSETOF__InterfaceDispatchCache__m_cEntries]

RhpResolveInterfaceMethodFast_NextEntry
        add     x14, x14, #SIZEOF__InterfaceDispatchCacheEntry
        sub     w13, w13, #1
        cmp     w13, #0
        beq     RhpResolveInterfaceMethodFast_SlowPath

        ldr     x15, [x14]
        cmp     x15, x12
        bne     RhpResolveInterfaceMethodFast_NextEntry

        ldur    x15, [x14, #8]
        ret

RhpResolveInterfaceMethodFast_SlowPath
        ldr     xip0, =RhpCidResolve
        mov     xip1, x11
        b       RhpUniversalTransitionReturnResult

    NESTED_END RhpResolveInterfaceMethodFast

#endif // FEATURE_CACHED_INTERFACE_DISPATCH

    END
