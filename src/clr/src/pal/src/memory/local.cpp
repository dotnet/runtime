//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
