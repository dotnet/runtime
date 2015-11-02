//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
PAL_realloc(
    void* pvMemblock,
    size_t szSize
    )
{
    return InternalRealloc(pvMemblock, szSize);
}

void *
CorUnix::InternalRealloc(
    void* pvMemblock,
    size_t szSize
    )
{
    void *pvMem;

    PERF_ENTRY(InternalRealloc);
    ENTRY("realloc (memblock:%p size=%d)\n", pvMemblock, szSize);    
       
    if (szSize == 0)
    {
        // If pvMemblock is NULL, there's no reason to call free.
        if (pvMemblock != NULL)
        {
            InternalFree(pvMemblock);
        }
        pvMem = NULL;
    }
    else
    {
        pvMem = realloc(pvMemblock, szSize);
    }

    LOGEXIT("realloc returns void * %p\n", pvMem);
    PERF_EXIT(InternalRealloc);
    return pvMem;
}

void
__cdecl
PAL_free(
    void *pvMem
    )
{
    InternalFree(pvMem);
}

void
CorUnix::InternalFree(
    void *pvMem
    )
{
    free(pvMem);
}

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

char *
__cdecl
PAL__strdup(
    const char *c_szStr
    )
{
    return InternalStrdup(c_szStr);
}

char *
CorUnix::InternalStrdup(
    const char *c_szStr
    )
{
    char *pszStrCopy;
    pszStrCopy = strdup(c_szStr);
    return pszStrCopy;
}
