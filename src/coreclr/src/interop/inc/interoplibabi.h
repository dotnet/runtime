// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stddef.h>

namespace InteropLib
{
    namespace ABI
    {
        const size_t DispatchAlignmentThisPtr = 16; // Should be a power of 2.
        const intptr_t DispatchThisPtrMask = ~(DispatchAlignmentThisPtr - 1);
    }
}
