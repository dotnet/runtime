// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

//
// Stub dispatch routine for dispatch to a vtable slot
//
    LEAF_ENTRY RhpVTableOffsetDispatch, _TEXT

        // x11 currently contains the indirection cell address.
        // load x11 to point to the vtable offset (which is stored in the m_pCache field).
        ldr     x11, [x11, #OFFSETOF__InterfaceDispatchCell__m_pCache]

        // x11 now contains the VTableOffset where the upper 32 bits are the offset to adjust
        // to get to the VTable chunk
        lsr     x10, x11, #32

        // Load the MethodTable from the object instance in x0, and add it to the vtable offset
        // to get the address in the vtable chunk list of what we want to dereference
    ALTERNATE_ENTRY RhpVTableOffsetDispatchAVLocation
        ldr     x9, [x0]
        add     x9, x10, x9

        // Load the target address of the vtable chunk into x9
        ldr     x9, [x9]

        // Compute the chunk offset
        ubfx    x10, x11, #16, #16

        // Load the target address of the virtual function into x9
        ldr     x9, [x9, x10]

        EPILOG_BRANCH_REG  x9
    LEAF_END RhpVTableOffsetDispatch, _TEXT

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
