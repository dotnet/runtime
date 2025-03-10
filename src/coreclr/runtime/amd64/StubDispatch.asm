;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros_Shared.inc


ifdef FEATURE_CACHED_INTERFACE_DISPATCH

EXTERN RhpInterfaceDispatchSlow : PROC

;; Macro that generates code to check a single cache entry.
CHECK_CACHE_ENTRY macro entry
NextLabel textequ @CatStr( Attempt, %entry+1 )
        cmp     rax, [r10 + OFFSETOF__InterfaceDispatchCache__m_rgEntries + (entry * 16)]
        jne     NextLabel
        jmp     qword ptr [r10 + OFFSETOF__InterfaceDispatchCache__m_rgEntries + (entry * 16) + 8]
NextLabel:
endm


;; Macro that generates a stub consuming a cache with the given number of entries.
DEFINE_INTERFACE_DISPATCH_STUB macro entries

StubName textequ @CatStr( RhpInterfaceDispatch, entries )
StubAVLocation textequ @CatStr( RhpInterfaceDispatchAVLocation, entries )

LEAF_ENTRY StubName, _TEXT

;EXTERN CID_g_cInterfaceDispatches : DWORD
        ;inc     [CID_g_cInterfaceDispatches]

        ;; r11 currently contains the indirection cell address.
        ;; load r10 to point to the cache block.
        mov     r10, [r11 + OFFSETOF__InterfaceDispatchCell__m_pCache]

        ;; Load the MethodTable from the object instance in rcx.
        ALTERNATE_ENTRY StubAVLocation
        mov     rax, [rcx]

CurrentEntry = 0
    while CurrentEntry lt entries
        CHECK_CACHE_ENTRY %CurrentEntry
CurrentEntry = CurrentEntry + 1
    endm

        ;; r11 still contains the indirection cell address.

        jmp RhpInterfaceDispatchSlow

LEAF_END StubName, _TEXT

    endm ;; DEFINE_INTERFACE_DISPATCH_STUB


;; Define all the stub routines we currently need.
;;
;; The mrt100dbi requires these be exported to identify mrt100 code that dispatches back into managed.
;; If you change or add any new dispatch stubs, please also change slr.def and dbi\process.cpp CordbProcess::GetExportStepInfo
;;
;; If you change or add any new dispatch stubs, exception handling might need to be aware because it refers to the
;; *AVLocation symbols defined by the dispatch stubs to be able to unwind and blame user code if a NullRef happens
;; during the interface dispatch.
;;
DEFINE_INTERFACE_DISPATCH_STUB 1
DEFINE_INTERFACE_DISPATCH_STUB 2
DEFINE_INTERFACE_DISPATCH_STUB 4
DEFINE_INTERFACE_DISPATCH_STUB 8
DEFINE_INTERFACE_DISPATCH_STUB 16
DEFINE_INTERFACE_DISPATCH_STUB 32
DEFINE_INTERFACE_DISPATCH_STUB 64

;; Initial dispatch on an interface when we don't have a cache yet.
LEAF_ENTRY RhpInitialInterfaceDispatch, _TEXT
ALTERNATE_ENTRY RhpInitialDynamicInterfaceDispatch
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of RhpInitialInterfaceDispatch and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        cmp     byte ptr [rcx], 0

        ;; Just tail call to the cache miss helper.
        jmp RhpInterfaceDispatchSlow

LEAF_END RhpInitialInterfaceDispatch, _TEXT

endif ;; FEATURE_CACHED_INTERFACE_DISPATCH

end
