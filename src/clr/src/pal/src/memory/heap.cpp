//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
#define HEAP_SIZEINFO_SIZE (2 * sizeof(DWORD))
using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(MEM);

// In safemath.h, Template SafeInt uses macro _ASSERTE, which need to use variable
// defdbgchan defined by SET_DEFAULT_DEBUG_CHANNEL. Therefore, the include statement
// should be placed after the SET_DEFAULT_DEBUG_CHANNEL(MEM)
#include <safemath.h>

#define HEAP_MAGIC 0xEAFDC9BB
#ifndef __APPLE__
#define DUMMY_HEAP 0x01020304
#endif // __APPLE__


#ifdef __APPLE__
#define CACHE_HEAP_ZONE
#endif // __APPLE__

#ifdef CACHE_HEAP_ZONE
/* This is a kludge.
 *
 * We need to know whether an instruction pointer fault is in our executable
 * heap, but the intersection between the HeapX functions on Windows and the
 * malloc_zone functions on Mac OS X are somewhat at odds and we'd have to 
 * implement an unnecessarily complicated HeapWalk. Instead, we cache the only
 * "heap" we create, knowing it's the executable heap, and use that instead
 * with the much simpler malloc_zone_from_ptr.
 */
extern malloc_zone_t *s_pExecutableHeap;
malloc_zone_t *s_pExecutableHeap = NULL;
#endif // CACHE_HEAP_ZONE

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
  RtlZeroMemory

See MSDN doc.
--*/
VOID
PALAPI
RtlZeroMemory(
    PVOID Destination,
    SIZE_T Length
)
{
    PERF_ENTRY(RtlZeroMemory);
    ENTRY("RtlZeroMemory(Destination:%p, Length:%x)\n", Destination, Length);
    
    memset(Destination, 0, Length);
    
    LOGEXIT("RtlZeroMemory returning.\n");
    PERF_EXIT(RtlZeroMemory);
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
        malloc_zone_t *pZone = malloc_create_zone(dwInitialSize, 0 /* flags */);
        ret = (HANDLE)pZone;
#ifdef CACHE_HEAP_ZONE
        _ASSERT_MSG(s_pExecutableHeap == NULL, "PAL currently only handles the creation of one executable heap.");
        s_pExecutableHeap = pZone;
        TRACE("s_pExecutableHeap is %p.\n", s_pExecutableHeap);
#endif // CACHE_HEAP_ZONE
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
    ret = (HANDLE) malloc_default_zone();
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
HeapSize

See MSDN doc.
--*/
SIZE_T
PALAPI
HeapSize(
    HANDLE hHeap,
    DWORD dwFlags,
    LPCVOID lpMem)
{
    SIZE_T ret = (SIZE_T)-1;
    PERF_ENTRY(HeapSize);
    ENTRY("HeapSize(hHeap=%p, dwFlags=%#x, lpMem=%p)\n",
            hHeap, dwFlags, lpMem);

    // First four bytes contain magic
    // Second four bytes size.
    ret = ((DWORD *)(static_cast<LPCBYTE>(lpMem) - HEAP_SIZEINFO_SIZE))[1];

    LOGEXIT("HeapSize returning %d\n", ret);
    PERF_EXIT(HeapSize);
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
	  IN SIZE_T dwBytes)
{
    BYTE *pMem;

    PERF_ENTRY(HeapAlloc);
    ENTRY("HeapAlloc (hHeap=%p, dwFlags=%#x, dwBytes=%u)\n",
          hHeap, dwFlags, dwBytes);

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

    
    size_t fullsize;
    if (!ClrSafeInt<size_t>::addition(dwBytes, HEAP_SIZEINFO_SIZE,fullsize))
    {
        ERROR("Integer Overflow\n");
        SetLastError(ERROR_ARITHMETIC_OVERFLOW);
        LOGEXIT("HeapAlloc returning NULL\n");
        PERF_EXIT(HeapAlloc);
        return NULL;
    }
#ifdef __APPLE__
    // This is patterned off of InternalMalloc in malloc.cpp.
    {
        CPalThread *pthrCurrent = InternalGetCurrentThread();
        pthrCurrent->suspensionInfo.EnterUnsafeRegion();
        pMem = (BYTE *)malloc_zone_malloc((malloc_zone_t *)hHeap, fullsize);
        pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
    }
#else // __APPLE__
    pMem = (BYTE *) PAL_malloc(fullsize);
#endif // __APPLE__ else

    if (pMem == NULL)
    {
        ERROR("Not enough memory\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        LOGEXIT("HeapAlloc returning NULL\n");
        PERF_EXIT(HeapAlloc);
        return NULL;
    }

    /* use a magic number, to know it has been allocated with HeapAlloc
       when doing HeapFree */
    ((DWORD *) pMem)[0] = HEAP_MAGIC;
    /* Store the size in the second word */
    ((DWORD *)pMem)[1] = dwBytes;

    /*If the Heap Zero memory flag is set initialize to zero*/
    if (dwFlags == HEAP_ZERO_MEMORY)
    {
        memset(pMem+ HEAP_SIZEINFO_SIZE, 0, dwBytes);
    }
    LOGEXIT("HeapAlloc returning LPVOID %p\n", pMem + HEAP_SIZEINFO_SIZE);
    PERF_EXIT(HeapAlloc);
    return (pMem + HEAP_SIZEINFO_SIZE);
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

    if ( !lpMem )
    {
        bRetVal = TRUE;
        goto done;
    }
    /*HEAP_SIZEINFO_SIZE + nMemAlloc is the size of Magic Number plus
     *size of the int to store value of Memory allocated */
	lpMem = static_cast<LPVOID>(static_cast<LPBYTE>(lpMem) - HEAP_SIZEINFO_SIZE);
    
    /* check if the memory has been allocated by HeapAlloc */
    if (*((DWORD *) lpMem) != HEAP_MAGIC)
    {
        ERROR("Pointer hasn't been allocated with HeapAlloc (%p)\n",
            static_cast<LPBYTE>(lpMem) + HEAP_SIZEINFO_SIZE);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }
    *((DWORD *) lpMem) = 0;

    bRetVal = TRUE;
#ifdef __APPLE__
    // This is patterned off of InternalFree in malloc.cpp.
    {
        CPalThread *pthrCurrent = InternalGetCurrentThread();
        pthrCurrent->suspensionInfo.EnterUnsafeRegion();
        malloc_zone_free((malloc_zone_t *)hHeap, lpMem);
        pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
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
	  IN SIZE_T dwBytes)
{
    BYTE *pMem = NULL;

    PERF_ENTRY(HeapReAlloc);
    ENTRY("HeapReAlloc (hHeap=%p, dwFlags=%#x, lpmem=%p, dwBytes=%u)\n",
          hHeap, dwFlags, lpmem, dwBytes);

#ifdef __APPLE__
    if (hHeap == NULL)
#else // __APPLE__
    if (hHeap != (HANDLE) DUMMY_HEAP)
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

   /*HEAP_SIZEINFO_SIZE + nMemAlloc is the size of Magic Number plus
     *size of the int to store value of Memory allocated */
   lpmem = static_cast<LPVOID>(static_cast<LPBYTE>(lpmem) - HEAP_SIZEINFO_SIZE);

    /* check if the memory has been allocated by HeapAlloc */
    if (*((DWORD *) lpmem) != HEAP_MAGIC)
    {
        ERROR("Pointer hasn't been allocated with HeapAlloc (%p)\n",
            static_cast<LPBYTE>(lpmem) + HEAP_SIZEINFO_SIZE);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }


    size_t fullsize;
    if (!ClrSafeInt<size_t>::addition(dwBytes, HEAP_SIZEINFO_SIZE, fullsize))
    {
        ERROR("Integer Overflow\n");
        SetLastError(ERROR_ARITHMETIC_OVERFLOW);
        goto done;
    }

#ifdef __APPLE__
    // This is patterned off of InternalRealloc in malloc.cpp.
    {
        CPalThread *pthrCurrent = InternalGetCurrentThread();
        pthrCurrent->suspensionInfo.EnterUnsafeRegion();
        pMem = (BYTE *) malloc_zone_realloc((malloc_zone_t *)hHeap, lpmem, fullsize);
        pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
    }
#else // __APPLE__
    pMem = (BYTE *) PAL_realloc(lpmem,fullsize);
#endif // __APPLE__ else

    if (pMem == NULL)
    {
        ERROR("Not enough memory\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }

    /* use a magic number, to know it has been allocated with HeapAlloc
       when doing HeapFree */
    *((DWORD *) pMem) = HEAP_MAGIC;

done:
    LOGEXIT("HeapReAlloc returns LPVOID %p\n", pMem ? (pMem + HEAP_SIZEINFO_SIZE) : pMem);
    PERF_EXIT(HeapReAlloc);
    return pMem ? (pMem + HEAP_SIZEINFO_SIZE) : pMem;
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
