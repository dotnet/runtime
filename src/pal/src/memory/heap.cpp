// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    heap.c

Abstract:

    Implementation of heap memory management functions.

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/handlemgr.hpp"
#include "pal/corunix.hpp"
#include <errno.h>
using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(MEM);

// In safemath.h, Template SafeInt uses macro _ASSERTE, which need to use variable
// defdbgchan defined by SET_DEFAULT_DEBUG_CHANNEL. Therefore, the include statement
// should be placed after the SET_DEFAULT_DEBUG_CHANNEL(MEM)
#include <safemath.h>

#ifndef __APPLE__
#define DUMMY_HEAP 0x01020304
#endif // __APPLE__

/*++
Function:
  RtlMoveMemory

See MSDN doc.
--*/
VOID
PALAPI
RtlMoveMemory(
          IN PVOID Destination,
          IN CONST VOID *Source,
          IN SIZE_T Length)
{
    PERF_ENTRY(RtlMoveMemory);
    ENTRY("RtlMoveMemory(Destination:%p, Source:%p, Length:%d)\n", 
          Destination, Source, Length);
    
    memmove(Destination, Source, Length);
    
    LOGEXIT("RtlMoveMemory returning\n");
    PERF_EXIT(RtlMoveMemory);
}

/*++
Function:
  HeapCreate

See MSDN doc.
--*/
HANDLE
PALAPI
HeapCreate(
	       IN DWORD flOptions,
	       IN SIZE_T dwInitialSize,
	       IN SIZE_T dwMaximumSize)
{
    HANDLE ret = INVALID_HANDLE_VALUE;
    PERF_ENTRY(HeapCreate);
    ENTRY("HeapCreate(flOptions=%#x, dwInitialSize=%u, dwMaximumSize=%u)\n",
        flOptions, dwInitialSize, dwMaximumSize);
#ifdef __APPLE__
    if ((flOptions & 0x40005) != 0)
    {
        ERROR("Invalid flOptions\n");
        SetLastError(ERROR_INVALID_PARAMETER);
    }
    else if (flOptions != 0)
    {
        ERROR("No support for flOptions\n");
        SetLastError(ERROR_INVALID_PARAMETER);
    }
    else if (dwMaximumSize)
    {
        ERROR("Zone implementation does not support a max size\n");
        SetLastError(ERROR_INVALID_PARAMETER);
    }
    else
    {
        ret = (HANDLE)malloc_create_zone(dwInitialSize, 0 /* flags */);
    }
    
#else // __APPLE__
    ret = (HANDLE)DUMMY_HEAP;
#endif // __APPLE__

    LOGEXIT("HeapCreate returning HANDLE %p\n", ret);
    PERF_EXIT(HeapCreate);
    return ret;
}


/*++
Function:
  GetProcessHeap

See MSDN doc.
--*/
HANDLE
PALAPI
GetProcessHeap(
	       VOID)
{
    HANDLE ret;

    PERF_ENTRY(GetProcessHeap);
    ENTRY("GetProcessHeap()\n");

#ifdef __APPLE__
#if HEAP_HANDLES_ARE_REAL_HANDLES
#error
#else
    malloc_zone_t *pZone = malloc_default_zone();
    ret = (HANDLE)pZone;
#endif // HEAP_HANDLES_ARE_REAL_HANDLES
#else
    ret = (HANDLE) DUMMY_HEAP;
#endif
  
    LOGEXIT("GetProcessHeap returning HANDLE %p\n", ret);
    PERF_EXIT(GetProcessHeap);
    return ret;
}

/*++
Function:
  HeapAlloc

Abstract
  Implemented as wrapper over malloc

See MSDN doc.
--*/
LPVOID
PALAPI
HeapAlloc(
    IN HANDLE hHeap,
    IN DWORD dwFlags,
    IN SIZE_T numberOfBytes)
{
    BYTE *pMem;

    PERF_ENTRY(HeapAlloc);
    ENTRY("HeapAlloc (hHeap=%p, dwFlags=%#x, numberOfBytes=%u)\n",
          hHeap, dwFlags, numberOfBytes);

#ifdef __APPLE__
    if (hHeap == NULL)
#else // __APPLE__
    if (hHeap != (HANDLE) DUMMY_HEAP)
#endif // __APPLE__ else
    {
        ERROR("Invalid heap handle\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        LOGEXIT("HeapAlloc returning NULL\n");
        PERF_EXIT(HeapAlloc);
        return NULL;
    }

    if ((dwFlags != 0) && (dwFlags != HEAP_ZERO_MEMORY))
    {
        ASSERT("Invalid parameter dwFlags=%#x\n", dwFlags);
        SetLastError(ERROR_INVALID_PARAMETER);
        LOGEXIT("HeapAlloc returning NULL\n");
        PERF_EXIT(HeapAlloc);
        return NULL;
    }

#ifdef __APPLE__
    // This is patterned off of InternalMalloc in malloc.cpp.
    {
        pMem = (BYTE *)malloc_zone_malloc((malloc_zone_t *)hHeap, numberOfBytes);
    }
#else // __APPLE__
    pMem = (BYTE *) PAL_malloc(numberOfBytes);
#endif // __APPLE__ else

    if (pMem == NULL)
    {
        ERROR("Not enough memory\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        LOGEXIT("HeapAlloc returning NULL\n");
        PERF_EXIT(HeapAlloc);
        return NULL;
    }

    /* If the HEAP_ZERO_MEMORY flag is set initialize to zero */
    if (dwFlags == HEAP_ZERO_MEMORY)
    {
        memset(pMem, 0, numberOfBytes);
    }

    LOGEXIT("HeapAlloc returning LPVOID %p\n", pMem);
    PERF_EXIT(HeapAlloc);
    return (pMem);
}


/*++
Function:
  HeapFree

Abstract
  Implemented as wrapper over free

See MSDN doc.
--*/
BOOL
PALAPI
HeapFree(
    IN HANDLE hHeap,
    IN DWORD dwFlags,
    IN LPVOID lpMem)
{
    BOOL bRetVal = FALSE;

    PERF_ENTRY(HeapFree);
    ENTRY("HeapFree (hHeap=%p, dwFlags = %#x, lpMem=%p)\n", 
          hHeap, dwFlags, lpMem);

#ifdef __APPLE__
    if (hHeap == NULL)
#else // __APPLE__
    if (hHeap != (HANDLE) DUMMY_HEAP)
#endif // __APPLE__ else
    {
        ERROR("Invalid heap handle\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    if (dwFlags != 0)
    {
        ASSERT("Invalid parameter dwFlags=%#x\n", dwFlags);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    if (!lpMem)
    {
        bRetVal = TRUE;
        goto done;
    }

    bRetVal = TRUE;
#ifdef __APPLE__
    {
        malloc_zone_free((malloc_zone_t *)hHeap, lpMem);
    }
#else // __APPLE__
    PAL_free (lpMem);
#endif // __APPLE__ else

done:
    LOGEXIT( "HeapFree returning BOOL %d\n", bRetVal );
    PERF_EXIT(HeapFree);
    return bRetVal;
}


/*++
Function:
  HeapReAlloc

Abstract
  Implemented as wrapper over realloc

See MSDN doc.
--*/
LPVOID
PALAPI
HeapReAlloc(
    IN HANDLE hHeap,
    IN DWORD dwFlags,
    IN LPVOID lpmem,
    IN SIZE_T numberOfBytes)
{
    BYTE *pMem = NULL;

    PERF_ENTRY(HeapReAlloc);
    ENTRY("HeapReAlloc (hHeap=%p, dwFlags=%#x, lpmem=%p, numberOfBytes=%u)\n",
          hHeap, dwFlags, lpmem, numberOfBytes);

#ifdef __APPLE__
    if (hHeap == NULL)
#else // __APPLE__
    if (hHeap != (HANDLE)DUMMY_HEAP)
#endif // __APPLE__ else
    {
        ASSERT("Invalid heap handle\n");
        SetLastError(ERROR_INVALID_HANDLE);
        goto done;
    }

    if ((dwFlags != 0))
    {
        ASSERT("Invalid parameter dwFlags=%#x\n", dwFlags);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    if (lpmem == NULL)
    {
        WARN("NULL memory pointer to realloc. Do not do anything.\n");
        /* set LastError back to zero. this appears to be an undocumented
        behavior in Windows, in doesn't cost much to match it */
        SetLastError(0);
        goto done;
    }

    if(numberOfBytes == 0)
    {
        // PAL's realloc behaves like free for a requested size of zero bytes. Force a nonzero size to get a valid pointer.
        numberOfBytes = 1;
    }

#ifdef __APPLE__
    // This is patterned off of InternalRealloc in malloc.cpp.
    {
        pMem = (BYTE *) malloc_zone_realloc((malloc_zone_t *)hHeap, lpmem, numberOfBytes);
    }
#else // __APPLE__
    pMem = (BYTE *) PAL_realloc(lpmem, numberOfBytes);
#endif // __APPLE__ else

    if (pMem == NULL)
    {
        ERROR("Not enough memory\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }

done:
    LOGEXIT("HeapReAlloc returns LPVOID %p\n", pMem);
    PERF_EXIT(HeapReAlloc);
    return pMem;
}

BOOL
PALAPI
HeapSetInformation(
        IN OPTIONAL HANDLE HeapHandle,
        IN HEAP_INFORMATION_CLASS HeapInformationClass,
        IN PVOID HeapInformation,
        IN SIZE_T HeapInformationLength)
{
    return TRUE;
}
