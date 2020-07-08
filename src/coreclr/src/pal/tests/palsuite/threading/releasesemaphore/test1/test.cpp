// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: releasesemaphore/test1/createsemaphore.c
**
** Purpose: Check that ReleaseSemaphore fails when using a semaphore handle 
** which has been closed by a call to CloseHandle.  Check that
** ReleaseSemaphore fails when using a ReleaseCount of zero or less than 
** zero.
**
**
**==========================================================================*/

#include <palsuite.h>

HANDLE hSemaphore;

int __cdecl main (int argc, char **argv) 
{
    if(0 != (PAL_Initialize(argc, argv)))
    {
	return (FAIL);
    }
    hSemaphore = CreateSemaphoreA (NULL, 1, 2, NULL); 
        
    if (NULL == hSemaphore)
    {
        Fail("PALSUITE ERROR: CreateSemaphoreA ('%p' '%ld' '%ld' "
             "'%p') returned NULL.\nGetLastError returned %d.\n", 
             NULL, 1, 2, NULL, GetLastError()); 
    }

    if(ReleaseSemaphore(hSemaphore, 0, NULL))
    {
        Fail("PALSUITE ERROR: ReleaseSemaphore('%p' '%ld' '%p') "
             "call returned %d\nwhen it should have returned "
             "%d.\nGetLastError returned %d.\n", 
             hSemaphore, 0, NULL, FALSE, TRUE, GetLastError());
    }

    if(ReleaseSemaphore(hSemaphore, -1, NULL))
    {
        Fail("PALSUITE ERROR: ReleaseSemaphore('%p' '%ld' '%p') "
             "call returned %d\nwhen it should have returned "
             "%d.\nGetLastError returned %d.\n", 
             hSemaphore, -1, NULL, TRUE, FALSE, GetLastError());
    }

    if(!CloseHandle(hSemaphore))
    {
        Fail("PALSUITE ERROR: CloseHandle(%p) call failed.  GetLastError "
             "returned %d.\n", hSemaphore, GetLastError());
    }

    if(ReleaseSemaphore(hSemaphore, 1, NULL))
    {
        Fail("PALSUITE ERROR: ReleaseSemaphore('%p' '%ld' '%p') "
             "call incremented semaphore %p count\nafter the handle "
             "was closed by a call to CloseHandle.\n GetLastError returned "
             "%d.\n", hSemaphore, -1, NULL, hSemaphore, GetLastError());
    }
    
    PAL_Terminate();
    return (PASS);
}
