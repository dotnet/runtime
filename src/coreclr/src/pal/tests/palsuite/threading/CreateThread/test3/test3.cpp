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

HANDLE hThread;
HANDLE hEvent;

DWORD PALAPI Thread( LPVOID lpParameter)
{
    DWORD dwRet;
    dwRet = WaitForSingleObject(hEvent, INFINITE);
    /* if this thread continues beyond here, fail */
    Fail("");
    
    return 0;
}

int __cdecl main(int argc, char **argv)
{
    DWORD dwThreadId;
    DWORD dwRet;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return (FAIL);
    }

    hEvent = CreateEvent(NULL, TRUE, FALSE, NULL);

    if (hEvent == NULL)
    {
        Fail("PALSUITE ERROR: CreateEvent call #0 failed.  GetLastError "
             "returned %u.\n", GetLastError());
    }

    /* pass the index as the thread argument */
    hThread = CreateThread( NULL,
                            0,
                            &Thread,
                            (LPVOID) 0,
                            0,
                            &dwThreadId);
    if (hThread == NULL)
    {
        Trace("PALSUITE ERROR: CreateThread('%p' '%d' '%p' '%p' '%d' '%p') "
              "call failed.\nGetLastError returned '%u'.\n", NULL,
              0, &Thread, (LPVOID) 0, 0, &dwThreadId, GetLastError());
        if (0 == CloseHandle(hEvent))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%u'.\n", hEvent);
        }
        Fail("");
    } 

    dwRet = WaitForSingleObject(hThread, 10000);
    if (dwRet != WAIT_TIMEOUT)
    {
        Trace ("PALSUITE ERROR: WaitForSingleObject('%p' '%d') "
               "call returned %d instead of WAIT_TIMEOUT ('%d').\n"
               "GetLastError returned '%u'.\n", hThread, 10000, 
               dwRet, WAIT_TIMEOUT, GetLastError());
        Fail("");
    }

    if (0 == CloseHandle(hThread))
    {
        Trace("PALSUITE ERROR: Unable to CloseHandle(%p) on a running thread."
              "\nGetLastError returned '%u'.\n", hThread, GetLastError());
        if (0 == CloseHandle(hEvent))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "cleanup.\nGetLastError returned '%u'.\n", hEvent, 
                  GetLastError());
        }
        Fail("");
    }
    if (0 == CloseHandle(hEvent))
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "cleanup.\nGetLastError returned '%u'.\n", hEvent, 
              GetLastError());
        Fail("");
    }
 
    PAL_Terminate();
    return (PASS);
}

