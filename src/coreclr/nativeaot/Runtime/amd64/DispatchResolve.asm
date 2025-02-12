;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc


ifdef FEATURE_CACHED_INTERFACE_DISPATCH


EXTERN RhpResolveInterfaceMethod : PROC

;; Fast version of RhpResolveInterfaceMethod
LEAF_ENTRY RhpResolveInterfaceMethodFast, _TEXT

        ;; Load the MethodTable from the object instance in rcx.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of RhpResolveInterfaceMethodFast and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        mov     r9, [rcx]

        ;; r10 currently contains the indirection cell address.
        ;; load r10 to point to the cache block.
        mov     r10, [r10 + OFFSETOF__InterfaceDispatchCell__m_pCache]
        test    r10b, IDC_CACHE_POINTER_MASK
        jne     RhpResolveInterfaceMethodFast_SlowPath

        lea     rax, [r10 + OFFSETOF__InterfaceDispatchCache__m_rgEntries]
        cmp     qword ptr [rax], r9
        jne     RhpResolveInterfaceMethodFast_Polymorphic
        mov     rax, qword ptr [rax + 8]
        ret

      RhpResolveInterfaceMethodFast_Polymorphic:
        mov     r10d, dword ptr [r10 + OFFSETOF__InterfaceDispatchCache__m_cEntries]

      RhpResolveInterfaceMethodFast_NextEntry:
        add     rax, SIZEOF__InterfaceDispatchCacheEntry
        dec     r10d
        jz      RhpResolveInterfaceMethodFast_SlowPath

        cmp     qword ptr [rax], r9
        jne     RhpResolveInterfaceMethodFast_NextEntry

        mov     rax, qword ptr [rax + 8]
        ret

      RhpResolveInterfaceMethodFast_SlowPath:
        jmp     RhpResolveInterfaceMethod

LEAF_END RhpResolveInterfaceMethodFast, _TEXT

endif ;; FEATURE_CACHED_INTERFACE_DISPATCH

end
