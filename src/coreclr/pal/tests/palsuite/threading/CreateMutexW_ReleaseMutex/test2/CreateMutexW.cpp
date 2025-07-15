// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: CreateMutexW_ReleaseMutex/test2/CreateMutexW.c
**
** Purpose: This test case tests the following things
**          - Creation of named Mutexes
**          - Creating multiple handles to a single named Mutex
**          - Ensuring that these handles work interchangeably
**          - Setting bInitialOwnerFlag to TRUE will cause the
**            initial call to a Wait function on the same Mutex
**            to actually wait.
**          - Waiting on a Mutex that a thread already owns does
**            not block.
**          - Create Named mutex with empty string ("")
**          - Create Named mutex with string of MAX_LONGPATH length
**          - Calling ReleaseMutex with invalid Mutex handles and
**            valid but unowned Mutexes.
**
** Dependencies: CreateThread
**               ReleaseMutex
**               WaitForSingleObject
**               CloseHandle
**               Sleep
**               memset
**

**
**=========================================================*/

#define UNICODE
#include <palsuite.h>

#define szMutex "MyMutex"
#define szEmpty ""

/* Function Prototypes */
BOOL NegativeReleaseMutexTests_CreateMutexW_ReleaseMutex_test2();

struct ThreadData
{
    HANDLE hMutex;
    BOOL bReturnCode;
};
typedef struct ThreadData THREADDATA;


PALTEST(threading_CreateMutexW_ReleaseMutex_test2_paltest_createmutexw_releasemutex_test2, "threading/CreateMutexW_ReleaseMutex/test2/paltest_createmutexw_releasemutex_test2")
{
    BOOL bFailures = FALSE;
    char *szMaxPath;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    /*
     * Run some negative tests on ReleaseMutex
     */

    if (!NegativeReleaseMutexTests_CreateMutexW_ReleaseMutex_test2())
    {
        bFailures = TRUE;
    }


    /*
     * If there were any failures, then abort with a call to Fail
     */

    if (bFailures == TRUE)
    {
        Fail("ERROR: There some failures in the Mutex tests.\n");
    }

    PAL_Terminate();
    return ( PASS );
}

/*
 * Testing Function
 *
 * Try some negative tests on ReleaseMutex
 */
BOOL NegativeReleaseMutexTests_CreateMutexW_ReleaseMutex_test2()
{
    HANDLE hMutex;
    BOOL bRet;
    BOOL bResults = TRUE;


    /*
     * Try calling ReleaseMutex on a null handle
     */
    hMutex = 0;
    bRet = ReleaseMutex(hMutex);

    if (bRet != 0)
    {
        Trace("Error: ReleaseMutex accepted null handle.\n");
        bResults =  FALSE;
    }


    /*
     * Try calling ReleaseMutex on an handle that we don't own
     */
    hMutex = CreateMutexW (NULL, TRUE, NULL);
    if (hMutex == 0)
    {
        Trace("Error: CreateMutex failed.\n");
        bResults =  FALSE;
    }

    bRet = ReleaseMutex(hMutex);
    bRet = ReleaseMutex(hMutex);

    if (bRet != FALSE)
    {
        Trace("Error: ReleaseMutex accepted unowned handle.\n");
        bResults =  FALSE;
    }

    if (CloseHandle(hMutex) == FALSE)
    {
        Trace("Error: CloseHandle failed.\n");
        bResults =  FALSE;
    }



    /*
     * Try calling ReleaseMutex on an handle that has been closed
     */
    hMutex = CreateMutexW (NULL, TRUE, NULL);
    if (hMutex == 0)
    {
        Trace("Error: CreateMutex failed.\n");
        bResults =  FALSE;
    }

    if (ReleaseMutex(hMutex) == FALSE)
    {
        Trace("Error: ReleaseMutex failed.\n");
        bResults =  FALSE;
    }
    if (CloseHandle(hMutex) == FALSE)
    {
        Trace("Error: CloseHandle failed.\n");
        bResults =  FALSE;
    }

    bRet = ReleaseMutex(hMutex);

    if (bRet != FALSE)
    {
        Trace("Error: ReleaseMutex accepted invalid handle.\n");
        bResults =  FALSE;
    }

    return bResults;
}
