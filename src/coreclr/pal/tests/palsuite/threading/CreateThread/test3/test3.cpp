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

DWORD PALAPI Thread_CreateThread_test3( LPVOID lpParameter)
{
    minipal_sleep(INFINITE);
    /* if this thread continues beyond here, fail */
    Fail("");
    
    return 0;
}

PALTEST(threading_CreateThread_test3_paltest_createthread_test3, "threading/CreateThread/test3/paltest_createthread_test3")
{
    DWORD dwThreadId;
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return (FAIL);
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
        Fail("");
    } 

    minipal_sleep(10);

    if (0 == CloseHandle(hThread_CreateThread_test3))
    {
        Trace("PALSUITE ERROR: Unable to CloseHandle(%p) on a running thread."
              "\nGetLastError returned '%u'.\n", hThread_CreateThread_test3, GetLastError());
        Fail("");
    }
    PAL_Terminate();
    return (PASS);
}
