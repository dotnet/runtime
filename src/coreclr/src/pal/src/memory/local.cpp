// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    if ((uFlags & ~LMEM_ZEROINIT) != 0)
    {
        ASSERT("Invalid parameter AllocFlags=0x%x\n", uFlags);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    lpRetVal = PAL_malloc(uBytes);

    if (lpRetVal == NULL)
    {
        ERROR("Not enough memory\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }

    if ((uFlags & LMEM_ZEROINIT) != 0)
    {
        memset(lpRetVal, 0, uBytes);
    }

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

    if (uFlags != LMEM_MOVEABLE)
    {
        // Currently valid iff uFlags is LMEM_MOVEABLE
        ASSERT("Invalid parameter uFlags=0x%x\n", uFlags);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    if (uBytes == 0)
    {
        // PAL's realloc behaves like free for a requested size of zero bytes. Force a nonzero size to get a valid pointer.	
        uBytes = 1;
    }

    lpRetVal = PAL_realloc(hMem, uBytes);

    if (lpRetVal == NULL)
    {
        ERROR("Not enough memory\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }

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

    free(hMem);
    bRetVal = TRUE;

    LOGEXIT( "LocalFree returning %p.\n", bRetVal == TRUE ? (HLOCAL)NULL : hMem );
    PERF_EXIT(LocalFree);
    return bRetVal == TRUE ? (HLOCAL)NULL : hMem;
}
