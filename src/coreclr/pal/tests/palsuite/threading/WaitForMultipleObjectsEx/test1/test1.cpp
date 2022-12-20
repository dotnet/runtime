// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source:  test1.c
**
** Purpose: Test for WaitForMultipleObjectsEx. Call the function
**          on an array of 4 events, and ensure that it returns correct
**          results when we do so.
**
**
**=========================================================*/

#include <palsuite.h>

/* Originally written as WaitForMultipleObjects/test1 */


/* Number of events in array */
#define MAX_EVENTS 4

BOOL WaitForMultipleObjectsExTest()
{
    BOOL bRet = TRUE;
    DWORD dwRet = 0;
    DWORD i = 0, j = 0;

    LPSECURITY_ATTRIBUTES lpEventAttributes = NULL;
    BOOL bManualReset = TRUE;
    BOOL bInitialState = TRUE;

    HANDLE hEvent[MAX_EVENTS];

    /* Run through this for loop and create 4 events */
    for (i = 0; i < MAX_EVENTS; i++)
    {
        hEvent[i] = CreateEvent( lpEventAttributes,
                                 bManualReset, bInitialState, NULL);

        if (hEvent[i] == INVALID_HANDLE_VALUE)
        {
            Trace("WaitForMultipleObjectsExTest:CreateEvent %u failed (%x)\n", i, GetLastError());
            bRet = FALSE;
            break;
        }

        /* Set the current event */
        bRet = SetEvent(hEvent[i]);

        if (!bRet)
        {
            Trace("WaitForMultipleObjectsExTest:SetEvent %u failed (%x)\n", i, GetLastError());
            bRet = FALSE;
            break;
        }

        /* Ensure that this returns the correct value */
        dwRet = WaitForSingleObject(hEvent[i],0);

        if (dwRet != WAIT_OBJECT_0)
        {
            Trace("WaitForMultipleObjectsExTest:WaitForSingleObject %u failed (%x)\n", i, GetLastError());
            bRet = FALSE;
            break;
        }

        /* Reset the event, and again ensure that the return value of
           WaitForSingle is correct.
        */
        bRet = ResetEvent(hEvent[i]);

        if (!bRet)
        {
            Trace("WaitForMultipleObjectsExTest:ResetEvent %u failed (%x)\n", i, GetLastError());
            bRet = FALSE;
            break;
        }

        dwRet = WaitForSingleObject(hEvent[i],0);

        if (dwRet != WAIT_TIMEOUT)
        {
            Trace("WaitForMultipleObjectsExTest:WaitForSingleObject %u failed (%x)\n", i, GetLastError());
            bRet = FALSE;
            break;
        }
    }

    /*
     * If the first section of the test passed, move on to the
     * second.
    */

    if (bRet)
    {
        BOOL bWaitAll = TRUE;
        DWORD nCount = MAX_EVENTS;
        CONST HANDLE *lpHandles = &hEvent[0];

        /* Call WaitForMultipleObjectsEx on all the events, the return
           should be WAIT_TIMEOUT
        */
        dwRet = WaitForMultipleObjectsEx(nCount,
                                        lpHandles,
                                        bWaitAll,
                                        0,
                                        FALSE);

        if (dwRet != WAIT_TIMEOUT)
        {
            Trace("WaitForMultipleObjectsExTest: WaitForMultipleObjectsEx failed (%x)\n", GetLastError());
        }
        else
        {
            /* Step through each event and one at a time, set the
               current test, while resetting all the other tests
            */

            for (i = 0; i < MAX_EVENTS; i++)
            {
                for (j = 0; j < MAX_EVENTS; j++)
                {
                    if (j == i)
                    {

                        bRet = SetEvent(hEvent[j]);

                        if (!bRet)
                        {
                            Trace("WaitForMultipleObjectsExTest:SetEvent %j failed (%x)\n", j, GetLastError());
                            break;
                        }
                    }
                    else
                    {
                        bRet = ResetEvent(hEvent[j]);

                        if (!bRet)
                        {
                            Trace("WaitForMultipleObjectsExTest:ResetEvent %u failed (%x)\n", j, GetLastError());
                        }
                    }
                }

                bWaitAll = FALSE;

                /* Check that WaitFor returns WAIT_OBJECT + i */
                dwRet = WaitForMultipleObjectsEx( nCount,
                                                lpHandles, bWaitAll, 0, FALSE);

                if (dwRet != WAIT_OBJECT_0+i)
                {
                    Trace("WaitForMultipleObjectsExTest: WaitForMultipleObjectsEx failed (%x)\n", GetLastError());
                    bRet = FALSE;
                    break;
                }
            }
        }

        for (i = 0; i < MAX_EVENTS; i++)
        {
            bRet = CloseHandle(hEvent[i]);

            if (!bRet)
            {
                Trace("WaitForMultipleObjectsExTest:CloseHandle %u failed (%x)\n", i, GetLastError());
            }
        }
    }

    return bRet;
}

BOOL WaitMultipleDuplicateHandleTest_WFMOEx_test1()
{
    BOOL testResult = TRUE;
    const HANDLE eventHandle = CreateEvent(NULL, TRUE, TRUE, NULL);
    HANDLE eventHandles[] = {eventHandle, eventHandle};

    // WaitAny - Wait for any of the events (no error expected)
    DWORD result = WaitForMultipleObjects(sizeof(eventHandles) / sizeof(eventHandles[0]), eventHandles, FALSE, 0);
    if (result != WAIT_OBJECT_0)
    {
        Trace("WaitMultipleDuplicateHandleTest:WaitAny failed (%x)\n", GetLastError());
        testResult = FALSE;
    }

    // WaitAll - Wait for all of the events (error expected)
    result = WaitForMultipleObjects(sizeof(eventHandles) / sizeof(eventHandles[0]), eventHandles, TRUE, 0);
    if (result != WAIT_FAILED)
    {
        Trace("WaitMultipleDuplicateHandleTest:WaitAll failed: call unexpectedly succeeded\n");
        testResult = FALSE;
    }
    else if (GetLastError() != ERROR_INVALID_PARAMETER)
    {
        Trace("WaitMultipleDuplicateHandleTest:WaitAll failed: unexpected last error (%x)\n");
        testResult = FALSE;
    }

    return testResult;
}

PALTEST(threading_WaitForMultipleObjectsEx_test1_paltest_waitformultipleobjectsex_test1, "threading/WaitForMultipleObjectsEx/test1/paltest_waitformultipleobjectsex_test1")
{

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    if(!WaitForMultipleObjectsExTest())
    {
        Fail ("Test failed\n");
    }

    if (!WaitMultipleDuplicateHandleTest_WFMOEx_test1())
    {
        Fail("Test failed\n");
    }

    PAL_Terminate();
    return PASS;
}
