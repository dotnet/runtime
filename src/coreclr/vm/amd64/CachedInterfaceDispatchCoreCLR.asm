; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include <AsmMacros.inc>
include AsmConstants.inc

ifdef FEATURE_CACHED_INTERFACE_DISPATCH

        extern  CID_ResolveWorker:proc
        extern  CID_VirtualOpenDelegateDispatchWorker:proc

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