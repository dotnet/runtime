// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    local.c

Abstract:

    Implementation of local memory management functions.

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"


SET_DEFAULT_DEBUG_CHANNEL(MEM);

static
int
AllocFlagsToHeapAllocFlags (IN  UINT  AllocFlags,
                       OUT PUINT pHeapallocFlags)
{
    int success = 1;
    UINT newFlags = 0, flags = AllocFlags;
    if (flags & LMEM_ZEROINIT) {
        newFlags |= HEAP_ZERO_MEMORY;
        flags &= ~LMEM_ZEROINIT;
    }
    if (flags != 0) {
        ASSERT("Invalid parameter AllocFlags=0x%x\n", AllocFlags);
        SetLastError(ERROR_INVALID_PARAMETER);
        success = 0;
    }
    if (success) {
        *pHeapallocFlags = newFlags;
    }
    return success;
}



/*++
Function:
  LocalAlloc

See MSDN doc.
--*/
HLOCAL
PALAPI
LocalAlloc(
	   IN UINT uFlags,
	   IN SIZE_T uBytes)
{
    LPVOID lpRetVal = NULL;
    PERF_ENTRY(LocalAlloc);
    ENTRY("LocalAlloc (uFlags=%#x, uBytes=%u)\n", uFlags, uBytes);

    if (!AllocFlagsToHeapAllocFlags (uFlags, &uFlags)) {
        goto done;
    }

    lpRetVal = HeapAlloc( GetProcessHeap(), uFlags, uBytes );

done:
    LOGEXIT( "LocalAlloc returning %p.\n", lpRetVal );
    PERF_EXIT(LocalAlloc);
    return (HLOCAL) lpRetVal;
}

/*++
Function:
LocalReAlloc

See MSDN doc.
--*/
HLOCAL
PALAPI
LocalReAlloc(
       IN HLOCAL hMem,
       IN SIZE_T uBytes,
       IN UINT   uFlags)
{
    LPVOID lpRetVal = NULL;
    PERF_ENTRY(LocalReAlloc);
    ENTRY("LocalReAlloc (hMem=%p, uBytes=%u, uFlags=%#x)\n", hMem, uBytes, uFlags);

    if (uFlags != LMEM_MOVEABLE) {
        // Currently valid iff uFlags is LMEM_MOVEABLE
        ASSERT("Invalid parameter uFlags=0x%x\n", uFlags);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }
    uFlags = 0;

    lpRetVal = HeapReAlloc(GetProcessHeap(), uFlags, hMem, uBytes);

done:
    LOGEXIT("LocalReAlloc returning %p.\n", lpRetVal);
    PERF_EXIT(LocalReAlloc);
    return (HLOCAL)lpRetVal;
}

/*++
Function:
  LocalFree

See MSDN doc.
--*/
HLOCAL
PALAPI
LocalFree(
	  IN HLOCAL hMem)
{
    BOOL bRetVal = FALSE;
    PERF_ENTRY(LocalFree);
    ENTRY("LocalFree (hmem=%p)\n", hMem);

    if ( hMem )
    {
        bRetVal = HeapFree( GetProcessHeap(), 0, hMem );
    }
    else
    {
        bRetVal = TRUE;
    }

    LOGEXIT( "LocalFree returning %p.\n", bRetVal == TRUE ? (HLOCAL)NULL : hMem );
    PERF_EXIT(LocalFree);
    return bRetVal == TRUE ? (HLOCAL)NULL : hMem;
}
