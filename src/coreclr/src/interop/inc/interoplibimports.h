// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _INTEROP_INC_INTEROPLIBIMPORTS_H_
#define _INTEROP_INC_INTEROPLIBIMPORTS_H_

#include "interoplib.h"

namespace InteropLibImports
{
    // Allocate the given amount of memory.
    void* MemAlloc(_In_ size_t sizeInBytes);

    // Free the previously allocated memory.
    void MemFree(_In_ void* mem);
}

#endif // _INTEROP_INC_INTEROPLIBIMPORTS_H_

