//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test1.c 
**
** Purpose: Test for CreateEvent.  Create an event, ensure the 
** HANDLE is valid.  Then check to ensure that the object is in the 
** signaled state.  Close the HANDLE and done.
**
**
**=========================================================*/

/*
  Note:  From the rotor_pal documentation:

  lpEventAttributes will always be NULL, bManualReset can be either
  TRUE or FALSE, bInitialState can be either TRUE or FALSE, the lpName
  may be non-NULL

*/


#include <palsuite.h>

BOOL CreateEventTest()
{
    BOOL bRet = FALSE;
    DWORD dwRet = 0;

    LPSECURITY_ATTRIBUTES lpEventAttributes = NULL;
    BOOL bManualReset = TRUE; 
    BOOL bInitialState = TRUE;

    /* Call CreateEvent, and check to ensure the returned HANDLE is a
       valid event HANDLE
    */
    
    HANDLE hEvent = CreateEvent( lpEventAttributes, 
                                 bManualReset, 
                                 bInitialState, 
                                 NULL); 
 
    if (hEvent != NULL)
    {
        /* Wait for the Object (for 0 time) and ensure that it returns
           the value indicating that the event is signaled.
        */
        dwRet = WaitForSingleObject(hEvent,0);

        if (dwRet != WAIT_OBJECT_0)
        {
            Trace("CreateEventTest:WaitForSingleObject failed (%x)\n", GetLastError());
        }
        else
        {
            /* If we make it here, and CloseHandle succeeds, then the
               entire test has passed.  Otherwise bRet will still show
               failure
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
