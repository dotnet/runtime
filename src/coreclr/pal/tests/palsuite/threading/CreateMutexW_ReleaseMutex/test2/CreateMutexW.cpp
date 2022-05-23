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
**          - Calling RelaseMutex with invalid Mutex handles and
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
BOOL TestNamedMutex_CreateMutexW_ReleaseMutex_test2(const char *szMutexName);
DWORD NamedMutexThread_CreateMutexW_ReleaseMutex_test2(LPVOID lpParam);
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
     * Test named Mutexes with ordinary string
     */

    if (!TestNamedMutex_CreateMutexW_ReleaseMutex_test2(szMutex))
    {
        bFailures = TRUE;
    }


    /*
     * Test named Mutexes with empty ("") string
     */

    if (!TestNamedMutex_CreateMutexW_ReleaseMutex_test2(szEmpty))
    {
        bFailures = TRUE;
    }


    /*
     * Test named Mutexes with string of length MAX_LONGPATH
     */

    szMaxPath = (char *)malloc(MAX_LONGPATH+2);
    memset(szMaxPath, 'A', MAX_LONGPATH-60);
    szMaxPath[MAX_LONGPATH-60] = 0;

    if (!TestNamedMutex_CreateMutexW_ReleaseMutex_test2(szMaxPath))
    {
        bFailures = TRUE;
    }

    free(szMaxPath);


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
 * Try to get multiple handles to a named Mutex and test
 * to make sure they actually refer to same Mutex object.
 */
BOOL TestNamedMutex_CreateMutexW_ReleaseMutex_test2(const char *szMutexName)
{
    DWORD dwData;
    HANDLE hMutex1;
    HANDLE hMutex2;
    HANDLE hThread;
    WCHAR *swzMutexName;
    THREADDATA threadData;

    /* Convert the Mutex name to wide characters */
    swzMutexName = convert((char *)szMutexName);

    /* Create a mutex and take ownership immediately */
    hMutex1 = CreateMutexW (NULL, TRUE, swzMutexName);

    if (NULL == hMutex1)
    {
        Trace("ERROR: CreateMutex #1 failed. GetLastError returned %u\n",
              GetLastError());
        free(swzMutexName);
        return FALSE;
    }

    /* Try to wait on the Mutex we just created. We should not block. */
    if (WaitForSingleObject(hMutex1, 1000) == WAIT_TIMEOUT)
    {
        Trace("WaitForSingleObject blocked on a Mutex that we owned.\n");
        free(swzMutexName);
        return FALSE;
    }
    /* We have to call ReleaseMutex here because of the Wait */
    if (ReleaseMutex(hMutex1) == FALSE)
    {
        Trace("ReleaseMutex Failed.\n");
        return FALSE;
    }

    /* Get a second handle to the same mutex */
    hMutex2 = CreateMutexW (NULL, FALSE, swzMutexName);

    if (NULL == hMutex2)
    {
        Trace("ERROR: CreateMutex #2 failed. GetLastError returned %u\n",
              GetLastError());
        free(swzMutexName);
        return FALSE;
    }

    /* Get rid of the wide character string */
    free(swzMutexName);

    /*
     * Create a thread that will Wait on the second handle.
     */
    threadData.hMutex = hMutex2;
    hThread = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)NamedMutexThread_CreateMutexW_ReleaseMutex_test2,
                          (LPVOID)&threadData, 0, &dwData);

    if (NULL == hThread)
    {
        Trace("ERROR: CreateThread failed. GetLastError returned %u\n",
              GetLastError());
        return FALSE;
    }

    /* Give the thread a little time to execute & wait*/
    Sleep(500);

    /* Signal the first handle */
    if (ReleaseMutex(hMutex1) == FALSE)
    {
        Trace("ReleaseMutex Failed.\n");
        return FALSE;
    }

    /* Give the thread some time to finish */
    Sleep(2000);

    /* Clean Up */
    if (CloseHandle(hMutex1) == FALSE ||
        CloseHandle(hMutex2) == FALSE ||
        CloseHandle(hThread) == FALSE)
    {
        Trace("ERROR: CloseHandle failed.\n");
        return FALSE;
    }

    /* Check the return code to see if signalling the first */
    /* Mutex handle woke up the thread which was Waiting on */
    /* the second handle.                                   */
    if (threadData.bReturnCode != FALSE)
    {
        Trace("ERROR: The handles did not refer to the same Mutex object.\n");
        return FALSE;
    }

    return TRUE;
}


/*
 * Thread function used with above testing function.
 */
DWORD NamedMutexThread_CreateMutexW_ReleaseMutex_test2(LPVOID lpParam)
{
    BOOL bTimedOut =  FALSE;
    THREADDATA *lpThreadData = (THREADDATA *)lpParam;

    /* Wait on the Mutex that was passed to us */
    if (WaitForSingleObject(lpThreadData->hMutex, 10000) == WAIT_TIMEOUT)
    {
        /* The Mutex was not signaled in the allotted time */
        bTimedOut = TRUE;
    }
    if (ReleaseMutex(lpThreadData->hMutex) == FALSE)
    {
        Trace("ERROR: ReleaseMutex failed.\n");
        lpThreadData->bReturnCode = FALSE;
        return  0;
    }

    /* Indicate whether we timed out Waiting on the Mutex */
    lpThreadData->bReturnCode = bTimedOut;

    return 0;
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
