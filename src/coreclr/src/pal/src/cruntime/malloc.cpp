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
    return InternalRealloc(InternalGetCurrentThread(), pvMemblock, szSize);
}

void *
CorUnix::InternalRealloc(
    CPalThread *pthrCurrent,
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
            InternalFree(pthrCurrent, pvMemblock);
        }
        pvMem = NULL;
    }
    else
    {
        pthrCurrent->suspensionInfo.EnterUnsafeRegion();
        pvMem = realloc(pvMemblock, szSize);
        pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
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
    InternalFree(InternalGetCurrentThread(), pvMem);
}

void
CorUnix::InternalFree(
    CPalThread *pthrCurrent,
    void *pvMem
    )
{
    pthrCurrent->suspensionInfo.EnterUnsafeRegion();
    free(pvMem);
    pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
}

void * 
__cdecl
PAL_malloc(
    size_t szSize
    )
{
    return InternalMalloc(InternalGetCurrentThread(), szSize);
}

void *
CorUnix::InternalMalloc(
    CPalThread *pthrCurrent,
    size_t szSize
    )
{
    void *pvMem;
    pthrCurrent->suspensionInfo.EnterUnsafeRegion();

    if (szSize == 0)
    {
        // malloc may return null for a requested size of zero bytes. Force a nonzero size to get a valid pointer.
        szSize = 1;
    }

    pvMem = (void*)malloc(szSize);
    pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
    return pvMem;
}

char *
__cdecl
PAL__strdup(
    const char *c_szStr
    )
{
    return InternalStrdup(InternalGetCurrentThread(), c_szStr);
}

char *
CorUnix::InternalStrdup(
    CPalThread *pthrCurrent,
    const char *c_szStr
    )
{
    char *pszStrCopy;
    pthrCurrent->suspensionInfo.EnterUnsafeRegion();
    pszStrCopy = strdup(c_szStr);
    pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
    return pszStrCopy;
}
