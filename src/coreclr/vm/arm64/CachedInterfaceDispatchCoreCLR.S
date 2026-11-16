// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

//
// Cache miss case, call the runtime to resolve the target and update the cache.
// x11 contains the interface dispatch cell address.
//
    NESTED_ENTRY RhpInterfaceDispatchSlow, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

        add         x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
        mov         x1, x11                         // indirection cell

        bl          C_FUNC(CID_ResolveWorker)

        mov         x9, x0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG  x9
    NESTED_END RhpInterfaceDispatchSlow, _TEXT

// x11 contains the address of the indirection cell (which is the MethodPtrAux field of the delegate)
    NESTED_ENTRY CID_VirtualOpenDelegateDispatch, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

        add         x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
        mov         x1, x11                         // indirection cell

        bl          C_FUNC(CID_VirtualOpenDelegateDispatchWorker)

        mov         x9, x0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG  x9
    NESTED_END CID_VirtualOpenDelegateDispatch, _TEXT

#endif // FEATURE_CACHED_INTERFACE_DISPATCH
