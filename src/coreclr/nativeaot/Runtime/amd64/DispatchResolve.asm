;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc


ifdef FEATURE_CACHED_INTERFACE_DISPATCH

EXTERN RhpCidResolve : PROC
EXTERN RhpUniversalTransitionReturnResult : PROC

;; Fast version of RhpResolveInterfaceMethod
LEAF_ENTRY RhpResolveInterfaceMethodFast, _TEXT

        ;; Load the MethodTable from the object instance in rcx.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of RhpResolveInterfaceMethodFast and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        mov     rax, [rcx]

        ;; r11 currently contains the indirection cell address.
        ;; load r10 to point to the cache block.
        mov     r10, [r11 + OFFSETOF__InterfaceDispatchCell__m_pCache]
        test    r10b, IDC_CACHE_POINTER_MASK
        jne     RhpResolveInterfaceMethodFast_SlowPath

        lea     r10, [r10 + OFFSETOF__InterfaceDispatchCache__m_rgEntries]
        cmp     qword ptr [r10], rax
        jne     RhpResolveInterfaceMethodFast_Polymorphic
        mov     rax, qword ptr [r10 + 8]
        ret

      RhpResolveInterfaceMethodFast_Polymorphic:
        ;; load the count of cache entries into edx
        ;; r11 points to the first cache entry so to get to m_cEntries, we need to subtract m_rgEntries first
        push    rdx
        mov     edx, dword ptr [r10 - OFFSETOF__InterfaceDispatchCache__m_rgEntries + OFFSETOF__InterfaceDispatchCache__m_cEntries]

      RhpResolveInterfaceMethodFast_NextEntry:
        add     r10, SIZEOF__InterfaceDispatchCacheEntry
        dec     edx
        jz      RhpResolveInterfaceMethodFast_SlowPath_Pop

        cmp     qword ptr [r10], rax
        jne     RhpResolveInterfaceMethodFast_NextEntry

        mov     rax, qword ptr [r10 + 8]
        pop     rdx
        ret

      RhpResolveInterfaceMethodFast_SlowPath_Pop:
        pop     rdx
      RhpResolveInterfaceMethodFast_SlowPath:
        ;; r11 contains indirection cell address
        lea     r10, RhpCidResolve
        jmp     RhpUniversalTransitionReturnResult

LEAF_END RhpResolveInterfaceMethodFast, _TEXT

endif ;; FEATURE_CACHED_INTERFACE_DISPATCH

end
