// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

// On Input:
//    r11                    contains the address of the indirection cell
//  [rsp+0] m_ReturnAddress: contains the return address of caller to stub
NESTED_ENTRY RhpInterfaceDispatchSlow, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

        lea             rdi, [rsp + __PWTB_TransitionBlock]         // pTransitionBlock
        mov             rsi, r11                                    // indirection cell

        call            C_FUNC(CID_ResolveWorker)

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        TAILJMP_RAX

NESTED_END RhpInterfaceDispatchSlow, _TEXT

// On Input:
//    r11                    contains the address of the indirection cell (which is the MethodPtrAux field of the delegate)
NESTED_ENTRY CID_VirtualOpenDelegateDispatch, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

        lea             rdi, [rsp + __PWTB_TransitionBlock]         // pTransitionBlock
        mov             rsi, r11                                    // indirection cell

        call            C_FUNC(CID_VirtualOpenDelegateDispatchWorker)

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        TAILJMP_RAX

NESTED_END CID_VirtualOpenDelegateDispatch, _TEXT

#endif // FEATURE_CACHED_INTERFACE_DISPATCH
