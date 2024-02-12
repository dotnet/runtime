// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    malloc.cpp

Abstract:

    Implementation of suspension safe memory allocation functions.

Revision History:



--*/

#include "pal/corunix.hpp"
#include "pal/thread.hpp"
#include "pal/malloc.hpp"
#include "pal/dbgmsg.h"

#include <string.h>

SET_DEFAULT_DEBUG_CHANNEL(CRT);

using namespace CorUnix;

void *
__cdecl
PAL_malloc(
    size_t szSize
    )
{
    return InternalMalloc(szSize);
}

void *
CorUnix::InternalMalloc(
    size_t szSize
    )
{
    void *pvMem;

    if (szSize == 0)
    {
        // malloc may return null for a requested size of zero bytes. Force a nonzero size to get a valid pointer.
        szSize = 1;
    }

    pvMem = (void*)malloc(szSize);
    return pvMem;
}
