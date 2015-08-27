//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test1.c 
**
** Purpose: Test for SetEvent.  Create an Event and then set
** this event, checking the return value.  Ensure that it returns
** positive.
**
**
**=========================================================*/

#include <palsuite.h>

BOOL SetEventTest()
{
    int bRet = 0;
    DWORD dwRet = 0;

    LPSECURITY_ATTRIBUTES lpEventAttributes = 0;
    BOOL bManualReset = TRUE; 
    BOOL bInitialState = FALSE;

    /* Create an event which we can use with SetEvent */
    HANDLE hEvent = CreateEvent( lpEventAttributes, 
                                 bManualReset, bInitialState, NULL); 
 
    if (hEvent != INVALID_HANDLE_VALUE)
    {
        dwRet = WaitForSingleObject(hEvent,0);

        if (dwRet != WAIT_TIMEOUT)
        {
            Trace("SetEventTest:WaitForSingleObject failed (%x)\n", GetLastError());
        }
        else
        {
            /* Set the event to the previously created event and check
               the return value.
            */
            bRet = SetEvent(hEvent);
            
            if (!bRet)
            {
                Trace("SetEventTest:SetEvent failed (%x)\n", GetLastError());
            }
            else
            {
                dwRet = WaitForSingleObject(hEvent,0);

                if (dwRet != WAIT_OBJECT_0)
                {
                    Trace("SetEventTest:WaitForSingleObject failed (%x)\n", GetLastError());
                }
                else
                {
                    dwRet = CloseHandle(hEvent);

                    if (!dwRet)
                    {
                        Trace("SetEventTest:CloseHandle failed (%x)\n", GetLastError());
                    }
                }
            }
        }
    }
    else
    {
        Trace("SetEventTest:CreateEvent failed (%x)\n", GetLastError());
    }

    return bRet;
}


int __cdecl main(int argc, char **argv)
{
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }
   
    if(SetEventTest() == 0)
    {
        Fail ("Test failed\n");
    }

    PAL_Terminate();
    return ( PASS );

}
