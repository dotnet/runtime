// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test2.c 
**
** Purpose: Test for CreateEventW.  Create the event with the
** initial state being not signaled.  Check to ensure that it
** times out when the event is triggered.
**
**
**=========================================================*/
#define UNICODE
#include <palsuite.h>

BOOL CreateEventTest()
{
    BOOL bRet = FALSE;
    DWORD dwRet = 0;

    LPSECURITY_ATTRIBUTES lpEventAttributes = 0;
    BOOL bManualReset = TRUE; 
    BOOL bInitialState = FALSE;
  

    /* Create an event with the Initial State set to FALSE */

    HANDLE hEvent = CreateEventW(lpEventAttributes, 
                                 bManualReset, 
                                 bInitialState, 
                                 NULL); 
 
    if (hEvent != NULL)
    {
        /* This should ensure that the object is reset, or
           non-signaled.
        */
        
        dwRet = WaitForSingleObject(hEvent,0);

        if (dwRet != WAIT_TIMEOUT)
        {
            Trace("CloseEventTest:WaitForSingleObject failed (%x)\n", GetLastError());
        }
        else
        {
            /* At this point, we've tested the function with success.
               So long as the HANDLE can be closed, this test should
               pass.
            */
            
            bRet = CloseHandle(hEvent);

            if (!bRet)
            {
                Trace("CloseEventTest:CloseHandle failed (%x)\n", GetLastError());
            }
        }
    }
    else
    {
        Trace("CloseEventTest:CreateEvent failed (%x)\n", GetLastError());
    }
    
    return bRet;
}

int __cdecl main(int argc, char **argv)
{
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    if(!CreateEventTest())
    {
        Fail ("Test failed\n");
    }

    PAL_Terminate();
    return ( PASS );

}
