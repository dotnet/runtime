// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test3.c
**
** Purpose: Tests that waiting on an open mutex will a return 
**          WAIT_OBJECT_0.  Does this by creating a child thread that 
**          acquires the mutex, releases it, and exits.
**
**
**===================================================================*/

#include <palsuite.h>


const int ChildThreadWaitTime = 1000;
const int ParentDelayTime = 2000; 

DWORD PALAPI AcquiringProc(LPVOID lpParameter);

PALTEST(threading_WaitForMultipleObjectsEx_test3_paltest_waitformultipleobjectsex_test3, "threading/WaitForMultipleObjectsEx/test3/paltest_waitformultipleobjectsex_test3")
{
    HANDLE Mutex;
    HANDLE hThread = 0;
    DWORD dwThreadId = 0;
    int ret;

    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    Mutex = CreateMutexW(NULL, FALSE, NULL);
    if (Mutex == NULL)
    {
        Fail("Unable to create the mutex.  GetLastError returned %d\n", 
            GetLastError());
    }

    hThread = CreateThread( NULL, 
                            0, 
                            (LPTHREAD_START_ROUTINE)AcquiringProc,
                            (LPVOID) Mutex,
                            0,
                            &dwThreadId);

    if (hThread == NULL)
    {
        Fail("ERROR: Was not able to create the thread to test!\n"
            "GetLastError returned %d\n", GetLastError());
    }

    Sleep(ParentDelayTime);

    ret = WaitForMultipleObjectsEx(1, &Mutex, FALSE, INFINITE, FALSE);
    if (ret != WAIT_OBJECT_0)
    {
        Fail("Expected WaitForMultipleObjectsEx to return WAIT_OBJECT_0\n"
            "Got %d\n", ret);
    }

    if (!CloseHandle(Mutex))
    {
        Fail("CloseHandle on the mutex failed!\n");
    }

    if (!CloseHandle(hThread))
    {
        Fail("CloseHandle on the thread failed!\n");
    }

    PAL_Terminate();
    return PASS;
}

/* 
 * Entry Point for child thread. Acquries a mutex, releases it, and exits.
 */
DWORD PALAPI AcquiringProc(LPVOID lpParameter)
{
    HANDLE Mutex;
    DWORD ret;

    Mutex = (HANDLE) lpParameter;
    
    Sleep(ChildThreadWaitTime);

    ret = WaitForSingleObject(Mutex, 0);
    if (ret != WAIT_OBJECT_0)
    {
        Fail("Expected the WaitForSingleObject call on the mutex to succeed\n"
            "Expected return of WAIT_OBJECT_0, got %d\n", ret);
    }

    ret = ReleaseMutex(Mutex);
    if (!ret)
    {
        Fail("Unable to release mutex!  GetLastError returned %d\n", 
            GetLastError());
    }

    return 0;
}
