// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*===========================================================
**
** Source: test3.c 
**
** Purpose: Check to see that the handle CreateThread returns
** can be closed while the thread is still running.
**
**
**=========================================================*/

#include <palsuite.h>

HANDLE hThread_CreateThread_test3;
HANDLE hEvent_CreateThread_test3;

DWORD PALAPI Thread_CreateThread_test3( LPVOID lpParameter)
{
    DWORD dwRet;
    dwRet = WaitForSingleObject(hEvent_CreateThread_test3, INFINITE);
    /* if this thread continues beyond here, fail */
    Fail("");
    
    return 0;
}

PALTEST(threading_CreateThread_test3_paltest_createthread_test3, "threading/CreateThread/test3/paltest_createthread_test3")
{
    DWORD dwThreadId;
    DWORD dwRet;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return (FAIL);
    }

    hEvent_CreateThread_test3 = CreateEvent(NULL, TRUE, FALSE, NULL);

    if (hEvent_CreateThread_test3 == NULL)
    {
        Fail("PALSUITE ERROR: CreateEvent call #0 failed.  GetLastError "
             "returned %u.\n", GetLastError());
    }

    /* pass the index as the thread argument */
    hThread_CreateThread_test3 = CreateThread( NULL,
                            0,
                            &Thread_CreateThread_test3,
                            (LPVOID) 0,
                            0,
                            &dwThreadId);
    if (hThread_CreateThread_test3 == NULL)
    {
        Trace("PALSUITE ERROR: CreateThread('%p' '%d' '%p' '%p' '%d' '%p') "
              "call failed.\nGetLastError returned '%u'.\n", NULL,
              0, &Thread_CreateThread_test3, (LPVOID) 0, 0, &dwThreadId, GetLastError());
        if (0 == CloseHandle(hEvent_CreateThread_test3))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%u'.\n", hEvent_CreateThread_test3);
        }
        Fail("");
    } 

    dwRet = WaitForSingleObject(hThread_CreateThread_test3, 10000);
    if (dwRet != WAIT_TIMEOUT)
    {
        Trace ("PALSUITE ERROR: WaitForSingleObject('%p' '%d') "
               "call returned %d instead of WAIT_TIMEOUT ('%d').\n"
               "GetLastError returned '%u'.\n", hThread_CreateThread_test3, 10000, 
               dwRet, WAIT_TIMEOUT, GetLastError());
        Fail("");
    }

    if (0 == CloseHandle(hThread_CreateThread_test3))
    {
        Trace("PALSUITE ERROR: Unable to CloseHandle(%p) on a running thread."
              "\nGetLastError returned '%u'.\n", hThread_CreateThread_test3, GetLastError());
        if (0 == CloseHandle(hEvent_CreateThread_test3))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "cleanup.\nGetLastError returned '%u'.\n", hEvent_CreateThread_test3, 
                  GetLastError());
        }
        Fail("");
    }
    if (0 == CloseHandle(hEvent_CreateThread_test3))
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "cleanup.\nGetLastError returned '%u'.\n", hEvent_CreateThread_test3, 
              GetLastError());
        Fail("");
    }
 
    PAL_Terminate();
    return (PASS);
}

