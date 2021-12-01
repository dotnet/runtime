// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test1.c 
**
** Purpose: Test for CreateEventW
**
**
**=========================================================*/

/*
 * Note: From the rotor_pal documentation: lpEventAttributes will
 * always be NULL, bManualReset can be either TRUE or FALSE,
 * bInitialState can be either TRUE or FALSE, the lpName may be
 * non-NULL.
*/
#define UNICODE
#include <palsuite.h>

BOOL CreateEventTest_CreateEvent_test1()
{
    BOOL bRet = FALSE;
    DWORD dwRet = 0;

    LPSECURITY_ATTRIBUTES lpEventAttributes = NULL;
    BOOL bManualReset = TRUE; 
    BOOL bInitialState = TRUE;

    /* 
     * Call CreateEvent, and check to ensure the returned HANDLE is a
     * valid event HANDLE
    */
    
    HANDLE hEvent = CreateEventW(lpEventAttributes, 
                                 bManualReset, 
                                 bInitialState, 
                                 NULL); 
 
    if (hEvent != NULL)
    {
        /* 
         * Wait for the Object (for 0 time) and ensure that it returns
         * the value indicating that the event is signaled.
        */
        dwRet = WaitForSingleObject(hEvent,0);

        if (dwRet != WAIT_OBJECT_0)
        {
            Trace("CreateEventTest:WaitForSingleObject failed (%x)\n", GetLastError());
        }
        else
        {
            /* 
	     * If we make it here, and CloseHandle succeeds, then the
	     * entire test has passed.  Otherwise bRet will still show
	     * failure
            */
            bRet = CloseHandle(hEvent);

            if (!bRet)
            {
                Trace("CreateEventTest:CloseHandle failed (%x)\n", GetLastError());
            }           
        }
    }
    else
    {
        Trace("CreateEventTest:CreateEvent failed (%x)\n", GetLastError());
    }
    
    return bRet;
}

PALTEST(threading_CreateEventW_test1_paltest_createeventw_test1, "threading/CreateEventW/test1/paltest_createeventw_test1")
{
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }
    
    if(!CreateEventTest_CreateEvent_test1())
    {
        Fail ("Test failed\n");
    }
 
    PAL_Terminate();
    return ( PASS );

}
