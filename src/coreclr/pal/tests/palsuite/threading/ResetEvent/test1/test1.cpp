// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test1.c
**
** Purpose: Test for ResetEvent.  Create an event with an initial
** state signaled.  Then reset that signal, and check to see that
** the event is now not signaled.
**
**
**=========================================================*/

#include <palsuite.h>

BOOL ResetEventTest()
{
    BOOL bRet = FALSE;
    DWORD dwRet = 0;

    LPSECURITY_ATTRIBUTES lpEventAttributes = 0;
    BOOL bManualReset = TRUE;
    BOOL bInitialState = TRUE;

    /* Create an Event, ensure it is valid */
    HANDLE hEvent = CreateEvent( lpEventAttributes,
                                 bManualReset, bInitialState, NULL);

    if (hEvent != INVALID_HANDLE_VALUE)
    {
        /* Check that WaitFor returns WAIT_OBJECT_0, indicating that
           the event is signaled.
        */

        dwRet = WaitForSingleObject(hEvent,0);

        if (dwRet != WAIT_OBJECT_0)
        {
            Fail("ResetEventTest:WaitForSingleObject failed (%x)\n", GetLastError());
        }
        else
        {
            /* Call ResetEvent, which will reset the signal */
            bRet = ResetEvent(hEvent);

            if (!bRet)
            {
                Fail("ResetEventTest:ResetEvent failed (%x)\n", GetLastError());
            }
            else
            {
                /* Call WaitFor again, and since it has been reset,
                   the return value should now be WAIT_TIMEOUT
                */
                dwRet = WaitForSingleObject(hEvent,0);

                if (dwRet != WAIT_TIMEOUT)
                {
                    Fail("ResetEventTest:WaitForSingleObject %s failed (%x)\n", GetLastError());
                }
                else
                {
                    bRet = CloseHandle(hEvent);

                    if (!bRet)
                    {
                        Fail("ResetEventTest:CloseHandle failed (%x)\n", GetLastError());
                    }
                }
            }
        }
    }
    else
    {
        Fail("ResetEventTest:CreateEvent failed (%x)\n", GetLastError());
    }

    return bRet;
}

PALTEST(threading_ResetEvent_test1_paltest_resetevent_test1, "threading/ResetEvent/test1/paltest_resetevent_test1")
{

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    if(!ResetEventTest())
    {
        Fail ("Test failed\n");
    }

    PAL_Terminate();
    return ( PASS );

}
