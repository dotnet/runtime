; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include <AsmMacros.inc>
include AsmConstants.inc

ifdef FEATURE_CACHED_INTERFACE_DISPATCH

        extern  CID_ResolveWorker:proc
        extern  CID_VirtualOpenDelegateDispatchWorker:proc

;; Stub dispatch routine for dispatch to a vtable slot
LEAF_ENTRY RhpVTableOffsetDispatch, _TEXT
        ;; r11 currently contains the indirection cell address.
        ;; load r11 to point to the vtable offset (which is stored in the m_pCache field).
        mov     r11, [r11 + OFFSETOF__InterfaceDispatchCell__m_pCache]

        ;; r11 now contains the VTableOffset where the upper 32 bits are the offset to adjust
        ;; to get to the VTable chunk
        mov     rax, r11
        shr     rax, 32

        ;; Load the MethodTable from the object instance in rcx, and add it to the vtable offset
        ;; to get the address in the vtable chunk list of what we want to dereference
ALTERNATE_ENTRY RhpVTableOffsetDispatchAVLocation
        add     rax, [rcx]

        ;; Load the target address of the vtable chunk into rax
        mov     rax, [rax]

        ;; Compute the chunk offset
        shr     r11d, 16

        ;; Load the target address of the virtual function into rax
        mov     rax, [rax + r11]

        TAILJMP_RAX
LEAF_END RhpVTableOffsetDispatch, _TEXT

;; On Input:
;;    r11                    contains the address of the indirection cell
;;  [rsp+0] m_ReturnAddress: contains the return address of caller to stub
NESTED_ENTRY RhpInterfaceDispatchSlow, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK

        lea             rcx, [rsp + __PWTB_TransitionBlock]         ; pTransitionBlock
        mov             rdx, r11                                    ; indirection cell

        call            CID_ResolveWorker

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        TAILJMP_RAX

NESTED_END RhpInterfaceDispatchSlow, _TEXT

;; On Input:
;;    r11                    contains the address of the indirection cell (which is the MethodPtrAux field of the delegate)
;;  [rsp+0] m_ReturnAddress: contains the return address of caller to stub
NESTED_ENTRY CID_VirtualOpenDelegateDispatch, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK

        lea             rcx, [rsp + __PWTB_TransitionBlock]         ; pTransitionBlock
        mov             rdx, r11                                    ; indirection cell

        call            CID_VirtualOpenDelegateDispatchWorker

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        TAILJMP_RAX

NESTED_END CID_VirtualOpenDelegateDispatch, _TEXT

endif ;; FEATURE_CACHED_INTERFACE_DISPATCH

        end