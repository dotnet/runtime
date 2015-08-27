//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
               currect test, while reseting all the other tests
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


int __cdecl main(int argc, char **argv)
{
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }
    
    if(!WaitForMultipleObjectsExTest())
    {
        Fail ("Test failed\n");
    }

    PAL_Terminate();
    return PASS;
}
