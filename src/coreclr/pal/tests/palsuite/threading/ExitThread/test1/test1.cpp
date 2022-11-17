// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test1.c
**
** Purpose: Test for ExitThread.  Create a thread and then call
** exit thread within the threading function.  Ensure that it exits
** immediately.
**
**
**=========================================================*/

#include <palsuite.h>

DWORD dwExitThreadTestParameter = 0;

DWORD PALAPI ExitThreadTestThread( LPVOID lpParameter)
{
    DWORD dwRet = 0;

    /* Save parameter for test */
    dwExitThreadTestParameter = (DWORD)(SIZE_T)lpParameter;

    /* Call the ExitThread function */
    ExitThread(dwRet);

    /* If we didn't exit, get caught in this loop.  But, the
       program will exit.
    */
    while (!dwRet)
    {
        Fail("ERROR: Entered an infinite loop because ExitThread "
               "failed to exit from the thread.  Forcing exit from "
               "the test now.");
    }

    return dwRet;
}

BOOL ExitThreadTest()
{
    BOOL bRet = FALSE;
    DWORD dwRet = 0;

    LPSECURITY_ATTRIBUTES lpThreadAttributes = NULL;
    DWORD dwStackSize = 0;
    LPTHREAD_START_ROUTINE lpStartAddress =  &ExitThreadTestThread;
    LPVOID lpParameter = (LPVOID)lpStartAddress;
    DWORD dwCreationFlags = 0;  //run immediately
    DWORD dwThreadId = 0;

    HANDLE hThread = 0;

    dwExitThreadTestParameter = 0;

    /* Create a Thread.  We'll need this to test that we're able
       to exit the thread.
    */
    hThread = CreateThread( lpThreadAttributes,
                            dwStackSize, lpStartAddress, lpParameter,
                            dwCreationFlags, &dwThreadId );

    if (hThread != INVALID_HANDLE_VALUE)
    {
        dwRet = WaitForSingleObject(hThread,INFINITE);

        if (dwRet != WAIT_OBJECT_0)
        {
            Trace("ExitThreadTest:WaitForSingleObject failed "
                   "(%x)\n",GetLastError());
        }
        else
        {
            /* Check to ensure that the parameter set in the Thread
               function is correct.
            */
            if (dwExitThreadTestParameter != (DWORD)(SIZE_T)lpParameter)
            {
                Trace("ERROR: The parameter passed should have been "
                       "%d but turned up as %d.",
                       dwExitThreadTestParameter, lpParameter);
            }
            else
            {
                bRet = TRUE;
            }
        }
    }
    else
    {
        Trace("ExitThreadTest:CreateThread failed (%x)\n",GetLastError());
    }

    return bRet;
}

PALTEST(threading_ExitThread_test1_paltest_exitthread_test1, "threading/ExitThread/test1/paltest_exitthread_test1")
{
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    if(!ExitThreadTest())
    {
        Fail ("Test failed\n");
    }

    PAL_Terminate();
    return ( PASS );
}
