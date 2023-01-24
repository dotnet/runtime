// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test1.c
**
** Purpose: Test for WaitForSingleObjectTest. Create two events, one
** with a TRUE and one with FALSE initial state.  Ensure that WaitForSingle
** returns correct values for each of these.
**
**
**=========================================================*/

#include <palsuite.h>

BOOL WaitForSingleObjectTest()
{

    BOOL bRet = FALSE;
    DWORD dwRet = 0;

    LPSECURITY_ATTRIBUTES lpEventAttributes = 0;
    BOOL bManualReset = TRUE;
    BOOL bInitialState = TRUE;

    HANDLE hEvent;

    /* Create an event, and ensure the HANDLE is valid */
    hEvent  = CreateEvent(lpEventAttributes, bManualReset,
                          bInitialState, NULL);

    if (hEvent != INVALID_HANDLE_VALUE)
    {

        /* Call WaitForSingleObject with 0 time on the event.  It
           should return WAIT_OBJECT_0
        */

        dwRet = WaitForSingleObject(hEvent,0);

        if (dwRet != WAIT_OBJECT_0)
        {
            Trace("WaitForSingleObjectTest:WaitForSingleObject failed (%x)\n", GetLastError());
        }
        else
        {
            bRet = CloseHandle(hEvent);

            if (!bRet)
            {
                Trace("WaitForSingleObjectTest:CloseHandle failed (%x)\n", GetLastError());
            }
        }
    }
    else
    {
        Trace("WaitForSingleObjectTest:CreateEvent failed (%x)\n", GetLastError());
    }

    /* If the first section passed, Create another event, with the
       initial state being FALSE this time.
    */

    if (bRet)
    {
        bRet = FALSE;

        bInitialState = FALSE;

        hEvent = CreateEvent( lpEventAttributes,
                              bManualReset, bInitialState, NULL);

        if (hEvent != INVALID_HANDLE_VALUE)
        {

            /* Test WaitForSingleObject and ensure that it returns
               WAIT_TIMEOUT in this case.
            */

            dwRet = WaitForSingleObject(hEvent,0);

            if (dwRet != WAIT_TIMEOUT)
            {
                Trace("WaitForSingleObjectTest:WaitForSingleObject failed (%x)\n", GetLastError());
            }
            else
            {
                bRet = CloseHandle(hEvent);

                if (!bRet)
                {
                    Trace("WaitForSingleObjectTest:CloseHandle failed (%x)\n", GetLastError());
                }
            }
        }
        else
        {
            Trace("WaitForSingleObjectTest::CreateEvent failed (%x)\n", GetLastError());
        }
    }
    return bRet;
}

PALTEST(threading_WaitForSingleObject_test1_paltest_waitforsingleobject_test1, "threading/WaitForSingleObject/test1/paltest_waitforsingleobject_test1")
{
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    if(!WaitForSingleObjectTest())
    {
        Fail ("Test failed\n");
    }

    PAL_Terminate();
    return ( PASS );

}
