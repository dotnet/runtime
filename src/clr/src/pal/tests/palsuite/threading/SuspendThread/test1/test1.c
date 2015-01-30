//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test1.c 
**
** Purpose: Test for SuspendThread. Create a thread, which is a 
** function that is counting.  Then suspend it, and make sure that it
** has stopped increasing the counter.
**
**
**=========================================================*/

#include <palsuite.h>

volatile DWORD dwSuspendThreadTestParameter = 0;
volatile DWORD dwSuspendThreadTestCounter = 0;

DWORD PALAPI SuspendThreadTestThread( LPVOID lpParameter)
{
    DWORD dwRet = 0;
    
    /* save parameter for test */
    dwSuspendThreadTestParameter = (DWORD)lpParameter;

    while (dwSuspendThreadTestParameter)
    {
        dwSuspendThreadTestCounter++;
    }

    return dwRet;
}

BOOL SuspendThreadTest()
{
    BOOL bRet = FALSE;
    DWORD dwRet = 0;

    LPSECURITY_ATTRIBUTES lpThreadAttributes = NULL;
    DWORD dwStackSize = 0; 
    LPTHREAD_START_ROUTINE lpStartAddress =  &SuspendThreadTestThread;
    LPVOID lpParameter = (LPVOID)1;
    DWORD dwCreationFlags = 0;  /* run immediately */
    DWORD dwThreadId = 0;

    HANDLE hThread = 0;

    dwSuspendThreadTestParameter = 0;


    /* Create a thread and ensure it returned a valid handle */

    hThread = CreateThread( lpThreadAttributes, 
                            dwStackSize, lpStartAddress, lpParameter, 
                            dwCreationFlags, &dwThreadId ); 

    if (hThread != INVALID_HANDLE_VALUE)
    {
        /* Wait and ensure that WAIT_TIMEOUT is returned */
        dwRet = WaitForSingleObject(hThread,1000);
        
        if (dwRet != WAIT_TIMEOUT)
        {
            Trace("SuspendThreadTest:WaitForSingleObject "
                   "failed (%x)\n",GetLastError());
        }
        else
        {
            /* Suspend the thread */
            dwRet = SuspendThread(hThread);

            if (dwRet != 0)
            {
                Trace("SuspendThreadTest:SuspendThread "
                       "failed (%x)\n",GetLastError());
            }
            else
            {
                /* now check parameter, it should be greater than 0 */
                if (dwSuspendThreadTestCounter == 0)
                {
                    Trace("SuspendThreadTest:parameter error\n");
                }
                else
                {
                    /* Save the counter */
                    dwRet = dwSuspendThreadTestCounter;
                    
                    /* Wait a second */
                    Sleep(1000);

                    /* Ensure the counter hasn't changed becuase the
                       thread was suspended.
                    */
                    
                    if (dwSuspendThreadTestCounter != dwRet)
                    {
                        Trace("SuspendThreadTest:parameter error\n");
                    }
                    else
                    {
                        /* Resume the thread */
                        dwRet = ResumeThread(hThread);

                        if (dwRet != 1)
                        {
                            Trace("SuspendThreadTest:ResumeThread "
                                   "failed (%x)\n",GetLastError());
                        }
                        else
                        {
                            dwSuspendThreadTestParameter = 0;

                            /* set thread to exit and wait */
                            dwRet = WaitForSingleObject(hThread,INFINITE);

                            if (dwRet != WAIT_OBJECT_0)
                            {
                                Trace("SuspendThreadTest:WaitForSingleObject"
                                       " failed (%x)\n",GetLastError());
                            }
                            else
                            {
                                bRet = TRUE;
                            }
                        }
                    }
                }
            }
        }
    }
    else
    {
        Trace("SuspendThreadTest:CreateThread failed (%x)\n",GetLastError());
    }

    return bRet; 
}

int __cdecl main(int argc, char **argv)
{
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    if(!SuspendThreadTest())
    {
        Fail ("Test failed\n");
    }
    
    PAL_Terminate();
    return (PASS);

}
