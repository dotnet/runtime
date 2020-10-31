// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: createsemaphorew_releasesemaphore/test3/createsemaphore.c
**
** Purpose: Test attributes of CreateSemaphoreExW and ReleaseSemaphore.
** Insure for CreateSemaphore that lInitialCount and lMaximumCount
** constraints are respected.  Validate that CreateSemaphore rejects
** conditions where initial count and / or maximum count are negative
** and conditions where the initial count is greater than the maximum
** count.  For ReleaseSemaphore validate that lpPreviousCount gets set
** to the previous semaphore count and lpPreviousCount can be NULL.
** Also establish ReleaseSemaphore fails when called in a semaphore
** with count equal to lMaximumCount.
**
**
**==========================================================================*/

#include <palsuite.h>

struct testcase
{
    LPSECURITY_ATTRIBUTES lpSemaphoreAttributes;
    LONG lInitialCount;
    LONG lMaximumCount;
    LPCWSTR lpName;
    BOOL bNegativeTest;
};

struct testcase testCases_CreateSemaphoreW_ReleaseSemaphore_test3[] =
{
    {NULL, -1, 1, NULL, TRUE},
    {NULL, 1, -1, NULL, TRUE},
    {NULL, -1, -1, NULL, TRUE},
    {NULL, 2, 1, NULL, TRUE},
    {NULL, 1, 2, NULL, FALSE},
    {NULL, 0, 10, NULL, FALSE},
    {NULL, INT_MAX - 1, INT_MAX, NULL, FALSE},
    {NULL, INT_MAX, INT_MAX, NULL, FALSE}
};

HANDLE hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[sizeof(testCases_CreateSemaphoreW_ReleaseSemaphore_test3)/sizeof(struct testcase)];

BOOL cleanup_ReleaseSemaphore_test3(int index)
{
    int i;
    BOOL bRet = TRUE;
    for (i = 0; i < index; i++)
    {
        if (!CloseHandle(hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i]))
        {
            bRet = FALSE;
            Trace("PALSUITE ERROR: CloseHandle(%p) call failed for index %d\n",
                  hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i], i);
        }
    }
    return(bRet);
}

PALTEST(threading_CreateSemaphoreW_ReleaseSemaphore_test3_paltest_createsemaphorew_releasesemaphore_test3, "threading/CreateSemaphoreW_ReleaseSemaphore/test3/paltest_createsemaphorew_releasesemaphore_test3")
{
    int i;
    int j;

    if(0 != (PAL_Initialize(argc, argv)))
    {
	return (FAIL);
    }
    /* create semaphores */
    testcase* testCases = testCases_CreateSemaphoreW_ReleaseSemaphore_test3;

    for (i = 0; i < sizeof(testCases_CreateSemaphoreW_ReleaseSemaphore_test3)/sizeof(struct testcase); i++)
    {
        hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i] = CreateSemaphoreExW (testCases[i].lpSemaphoreAttributes,
            testCases[i].lInitialCount,
            testCases[i].lMaximumCount,
            testCases[i].lpName,
            0,
            0);

        if (NULL == hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i])
        {
            if (!testCases[i].bNegativeTest)
            {
                Trace("PALSUITE ERROR: CreateSemaphoreExW('%p' '%ld' '%ld' "
                      "'%p' '0' '0') returned NULL at index %d.\nGetLastError "
                      "returned %d.\n", testCases[i].lpSemaphoreAttributes,
                      testCases[i].lInitialCount, testCases[i].lMaximumCount,
                      testCases[i].lpName, i, GetLastError());
                if (i > 0)
                {
                    cleanup_ReleaseSemaphore_test3(i - 1);
                }
                Fail("");
            }
            else
            {
                continue;
            }
        }

        /* increment semaphore count to lMaximumCount */
        for (j = testCases[i].lInitialCount; (ULONG)j <= (ULONG)testCases[i].lMaximumCount;
             j++)
        {
            if (testCases[i].lMaximumCount == j)
            {
                /* Call ReleaseSemaphore once more to ensure ReleaseSemaphore
                   fails */
                if(ReleaseSemaphore(hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i], 1, NULL))
                {
                    Trace("PALSUITE ERROR: ReleaseSemaphore('%p' '%ld' '%p') "
                          "call returned %d\nwhen it should have returned "
                          "%d.\nThe semaphore's count was %d.\nGetLastError "
                          "returned %d.\n", hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i], 1, NULL, TRUE,

                          FALSE, j, GetLastError());
                    cleanup_ReleaseSemaphore_test3(i);
                    Fail("");
                }
            }
            else
            {
                int previous;
                BOOL bRet = ReleaseSemaphore(hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i], 1, &previous);
                DWORD dwError = GetLastError();

                if(!bRet)
                {
                    Trace("PALSUITE ERROR: ReleaseSemaphore('%p' '%ld' '%p') "
                          "call returned %d\nwhen it should have returned "
                          "%d.\nThe semaphore count was %d and it's "
                          "lMaxCount was %d.\nGetLastError returned %d.\n",
                          hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i], 1, &previous, bRet, TRUE, j,
                          testCases[i].lMaximumCount, dwError);
                    cleanup_ReleaseSemaphore_test3(i);
                    Fail("");
                }
                if (previous != j)
                {
                    Trace("PALSUITE ERROR: ReleaseSemaphore('%p' '%ld' '%p') "
                          "call set %p to %d instead of %d.\n The semaphore "
                          "count was %d and GetLastError returned %d.\n",
                          hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i], 1, &previous, &previous, previous,
                          j, j, dwError);
                    cleanup_ReleaseSemaphore_test3(i);
                    Fail("");
                }
            }
        }

        // Skip exhaustive decrement tests for too large an initial count
        if(testCases[i].lInitialCount >= INT_MAX - 1)
        {
            continue;
        }

        /* decrement semaphore count to 0 */
        for (j = testCases[i].lMaximumCount; j >= 0; j--)
        {
            DWORD dwRet = WaitForSingleObject(hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i], 0);
            DWORD dwError = GetLastError();

            if (0 == j)
            {
                /* WaitForSingleObject should report that the
                   semaphore is nonsignaled */
                if (WAIT_TIMEOUT != dwRet)
                {
                    Trace("PALSUITE ERROR: WaitForSingleObject('%p' '%u') "
                          "call returned %d\nwhen it should have returned "
                          "%d.\nThe semaphore's count was %d.\nGetLastError "
                          "returned %d.\n", hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i], 0, dwRet,
                          WAIT_TIMEOUT, j, dwError);
                    cleanup_ReleaseSemaphore_test3(i);
                    Fail("");
                }
            }
            else
            {
                /* WaitForSingleObject should report that the
                   semaphore is signaled */
                if (WAIT_OBJECT_0 != dwRet)
                {
                    Trace("PALSUITE ERROR: WaitForSingleObject('%p' '%u') "
                          "call returned %d\nwhen it should have returned "
                          "%d.\nThe semaphore's count was %d.\nGetLastError "
                          "returned %d.\n", hSemaphore_CreateSemaphoreW_ReleaseSemaphore_test3[i], 0, dwRet,
                          WAIT_OBJECT_0, j, dwError);
                    cleanup_ReleaseSemaphore_test3(i);
                    Fail("");
                }
            }
        }
    }
    PAL_Terminate();
    return (PASS);
}





