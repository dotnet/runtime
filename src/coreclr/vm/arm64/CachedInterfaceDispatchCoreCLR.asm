; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

    TEXTAREA

    EXTERN CID_ResolveWorker
    EXTERN CID_VirtualOpenDelegateDispatchWorker

;;
;; Cache miss case, call the runtime to resolve the target and update the cache.
;; x11 contains the interface dispatch cell address.
;;
    NESTED_ENTRY RhpInterfaceDispatchSlow

        PROLOG_WITH_TRANSITION_BLOCK

        add         x0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
        mov         x1, x11                         ; indirection cell

        bl          CID_ResolveWorker

        mov         x9, x0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG  x9
    NESTED_END RhpInterfaceDispatchSlow

;; x11 contains the address of the indirection cell (which is the MethodPtrAux field of the delegate)
    NESTED_ENTRY CID_VirtualOpenDelegateDispatch

        PROLOG_WITH_TRANSITION_BLOCK

        add         x0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
        mov         x1, x11                         ; indirection cell

        bl          CID_VirtualOpenDelegateDispatchWorker

        mov         x9, x0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG  x9
    NESTED_END CID_VirtualOpenDelegateDispatch

#endif ;; FEATURE_CACHED_INTERFACE_DISPATCH

    END
