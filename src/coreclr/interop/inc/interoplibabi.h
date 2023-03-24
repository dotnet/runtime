// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTEROP_INC_INTEROPLIBABI_H_
#define _INTEROP_INC_INTEROPLIBABI_H_

#include <stddef.h>

namespace InteropLib
{
    namespace ABI
    {
        // Updating this also requires updating ComInterfaceDispatch::GetInstance<T>.
#ifdef HOST_64BIT
        const size_t DispatchAlignmentThisPtr = 64; // Should be a power of 2.
#else
        const size_t DispatchAlignmentThisPtr = 16; // Should be a power of 2.
#endif

        const intptr_t DispatchThisPtrMask = ~(DispatchAlignmentThisPtr - 1);

        // Managed object wrapper layout.
        // This is designed to codify the binary layout.
        struct ManagedObjectWrapperLayout
        {
            PTR_VOID ManagedObject;
            long long RefCount;
        };
    }
}

#endif // _INTEROP_INC_INTEROPLIBABI_H_
