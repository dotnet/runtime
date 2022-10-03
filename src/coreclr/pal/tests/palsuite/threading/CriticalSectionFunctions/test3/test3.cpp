// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:      CriticalSectionFunctions/test3/test3.c
**
** Purpose:     Create two threads to exercise TryEnterCriticalSection
**              and EnterCriticalSection.  TryEnterCriticalSection acquires
**              and holds a CRITICAL_SECTION object.  Another call to
**              TryEnterCriticalSection is made from a different thread, at
**              this time, to establish a call to TryEnterCriticalSection
**              will return immediately and to establish
**              TryEnterCriticalSection returns the proper value when it
**              attempts to lock a CRITICAL_SECTION that is already owned
**              by another thread.  The CRITICAL_SECTION object is then
**              released and held by a call to EnterCriticalSection.  A new
**              thread is invoked and attempts to acquire the held
**              CRITICAL_SECTION with a call to TryEnterCriticalSection.
**              TryEnterCriticalSection returns immediately and returns
**              with the value that states the CRITICAL_SECTION object is
**              held by another thread.  This establishes
**              TryEnterCriticalSection behaves the same way with
**              CriticalSections locked by TryEnterCriticalSection and
**              EnterCriticalSection.
**
**
**===================================================================*/
#include <palsuite.h>

#define NUM_THREADS 2

HANDLE hThread_CriticalSectionFunctions_test3[NUM_THREADS];
HANDLE hEvent_CriticalSectionFunctions_test3[NUM_THREADS];
BOOL bRet_CriticalSectionFunctions_test3 = FAIL;

DWORD PALAPI Thread_CriticalSectionFunctions_test3(LPVOID lpParam)
{
    DWORD dwRet;

    if (0 == TryEnterCriticalSection(&CriticalSection))
    {
        dwRet = WaitForMultipleObjects(NUM_THREADS, hEvent_CriticalSectionFunctions_test3, TRUE, 10000);
        if ((WAIT_OBJECT_0 > dwRet) ||
            ((WAIT_OBJECT_0 + NUM_THREADS - 1) < dwRet))
        {
#if 0
            if (0 == CloseHandle(hThread_CriticalSectionFunctions_test3[1]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up.\nGetLastError returned '%d'.\n",
                      hThread_CriticalSectionFunctions_test3[1], GetLastError());
            }
#endif
            Trace("PALSUITE ERROR: WaitForMultipleObjects(%d, %p, %d, %d) call"
                  "returned an unexpected value, '%d'.\nGetLastError returned "
                  "%d.\n", NUM_THREADS, hEvent_CriticalSectionFunctions_test3, TRUE, 10000, dwRet,
                  GetLastError());
        }
        else
        {
            bRet_CriticalSectionFunctions_test3 = PASS;
        }
    }
    else
    {
        /* signal thread 0 */
        if (0 == SetEvent(hEvent_CriticalSectionFunctions_test3[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute SetEvent(%p) during "
                 "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[0],
                 GetLastError());
            LeaveCriticalSection(&CriticalSection);
            if (0 == CloseHandle(hThread_CriticalSectionFunctions_test3[0]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up\nGetLastError returned '%d'.\n",
                      hThread_CriticalSectionFunctions_test3[0], GetLastError());
            }
            if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[0]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up\nGetLastError returned '%d'.\n",
                      hEvent_CriticalSectionFunctions_test3[0], GetLastError());
            }
            if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[1]))
        {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up\nGetLastError returned '%d'.\n",
                      hEvent_CriticalSectionFunctions_test3[1], GetLastError());
        }
            DeleteCriticalSection(&CriticalSection);
            Fail("");
        }

        /* wait to be signaled */
        dwRet = WaitForSingleObject(hEvent_CriticalSectionFunctions_test3[1], 10000);
        if (WAIT_OBJECT_0 != dwRet)
        {
            Trace("PALSUITE ERROR: WaitForSingleObject(%p,%d) should have "
                 "returned\nWAIT_OBJECT_0 ('%d'), instead it returned "
                 "('%d').\nGetLastError returned '%d'.\n",
                 hEvent_CriticalSectionFunctions_test3[0], 10000, WAIT_OBJECT_0, dwRet, GetLastError());
            if (0 == CloseHandle(hThread_CriticalSectionFunctions_test3[0]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up.\nGetLastError returned '%d'.\n",
                      hThread_CriticalSectionFunctions_test3[0], GetLastError());
            }
            if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[0]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up.\nGetLastError returned '%d'.\n",
                      hEvent_CriticalSectionFunctions_test3[0], GetLastError());
            }
            if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[1]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up.\nGetLastError returned '%d.'\n",
                      hEvent_CriticalSectionFunctions_test3[1], GetLastError());
            }
            DeleteCriticalSection(&CriticalSection);
            Fail("");
        }
        LeaveCriticalSection(&CriticalSection);
    }
    return FAIL;
}

PALTEST(threading_CriticalSectionFunctions_test3_paltest_criticalsectionfunctions_test3, "threading/CriticalSectionFunctions/test3/paltest_criticalsectionfunctions_test3")
{
    HANDLE hThread_CriticalSectionFunctions_test3[NUM_THREADS];
    DWORD dwThreadId[NUM_THREADS];
    DWORD dwRet;

    if ((PAL_Initialize(argc,argv)) != 0)
    {
        return(bRet_CriticalSectionFunctions_test3);
    }

    /* thread 0 event */
    hEvent_CriticalSectionFunctions_test3[0] = CreateEvent(NULL, TRUE, FALSE, NULL);

    if (hEvent_CriticalSectionFunctions_test3[0] == NULL)
    {
        Fail("PALSUITE ERROR: CreateEvent call #0 failed.  GetLastError "
             "returned %d.\n", GetLastError());
    }

    /* thread 1 event */
    hEvent_CriticalSectionFunctions_test3[1] = CreateEvent(NULL, TRUE, FALSE, NULL);

    if (hEvent_CriticalSectionFunctions_test3[1] == NULL)
    {
        Trace("PALSUITE ERROR: CreateEvent call #1 failed.  GetLastError "
             "returned %d.\n", GetLastError());
        if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[0]);
        }
        Fail("");
    }

    InitializeCriticalSection ( &CriticalSection );

    hThread_CriticalSectionFunctions_test3[0] = CreateThread(NULL,
                           0,
                              &Thread_CriticalSectionFunctions_test3,
                           (LPVOID) NULL,
                           0,
                              &dwThreadId[0]);

    if (hThread_CriticalSectionFunctions_test3[0] == NULL)
    {
        Trace("PALSUITE ERROR: CreateThread call #0 failed.  GetLastError "
             "returned %d.\n", GetLastError());
        if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[1]))
    {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[1],
                  GetLastError());
        }
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }


    /* wait for thread 0 to be signaled */
    dwRet = WaitForSingleObject(hEvent_CriticalSectionFunctions_test3[0], 10000);
    if (WAIT_OBJECT_0 != dwRet)
    {
        Trace("PALSUITE ERROR: WaitForSingleObject(%p,%d) should have "
             "returned\nWAIT_OBJECT_0 ('%d'), instead it returned "
             "('%d').\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[0], 10000,
             WAIT_OBJECT_0, dwRet, GetLastError());
        if (0 == CloseHandle(hThread_CriticalSectionFunctions_test3[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hThread_CriticalSectionFunctions_test3[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[0]))
    {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[0],
                  GetLastError());
    }
        if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[1],
                  GetLastError());
        }
        Fail("");
    }

    /*
     * Attempting to enter CRITICAL_SECTION object owned by the
     * created thread and locked with TryEnterCriticalSection
     */
    if (0 == TryEnterCriticalSection(&CriticalSection))
    {
        /* signal thread 1 */
        if (0 == SetEvent(hEvent_CriticalSectionFunctions_test3[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute SetEvent(%p) call.\n"
                  "GetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[1],
                  GetLastError());
            goto done;
        }
    }
    else
    {
        Trace("PALSUITE_ERROR: TryEnterCriticalSection was able to grab a"
             " CRITICAL_SECTION object\nwhich was already owned.\n");
        LeaveCriticalSection(&CriticalSection);
        if (0 == CloseHandle(hThread_CriticalSectionFunctions_test3[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hThread_CriticalSectionFunctions_test3[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[1],
                  GetLastError());
        }
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }
        /*
     * Enter the CRITICAL_SECTION and launch another thread to attempt
     * to access the CRITICAL_SECTION with a call to TryEnterCriticalSection.
         */
    EnterCriticalSection(&CriticalSection);

    hThread_CriticalSectionFunctions_test3[1] = CreateThread(NULL,
                              0,
                              &Thread_CriticalSectionFunctions_test3,
                              (LPVOID) NULL,
                              0,
                              &dwThreadId[1]);

    if (hThread_CriticalSectionFunctions_test3[1] == NULL)
    {
        Trace("PALSUITE ERROR: CreateThread call #1 failed.  GetLastError "
             "returned %d.\n", GetLastError());
        LeaveCriticalSection(&CriticalSection);
        if (0 == CloseHandle(hThread_CriticalSectionFunctions_test3[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hThread_CriticalSectionFunctions_test3[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[1],
                  GetLastError());
        }
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }

    dwRet = WaitForMultipleObjects(NUM_THREADS, hThread_CriticalSectionFunctions_test3, TRUE, 10000);
    if ((WAIT_OBJECT_0 > dwRet) ||
        ((WAIT_OBJECT_0 + NUM_THREADS - 1) < dwRet))
    {
        Trace("PALSUITE ERROR: WaitForMultipleObjects(%d, %p, %d, %d) call "
             "returned an unexpected value, '%d'.\nGetLastError returned "
             "%d.\n", NUM_THREADS, hThread_CriticalSectionFunctions_test3, TRUE, 10000, dwRet,
             GetLastError());
        LeaveCriticalSection(&CriticalSection);
        if (0 == CloseHandle(hThread_CriticalSectionFunctions_test3[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hThread_CriticalSectionFunctions_test3[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hThread_CriticalSectionFunctions_test3[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hThread_CriticalSectionFunctions_test3[1],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[1],
                  GetLastError());
        }
    DeleteCriticalSection(&CriticalSection);
        Fail("");
    }

    LeaveCriticalSection(&CriticalSection);
    if (0 == CloseHandle(hThread_CriticalSectionFunctions_test3[1]))
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%d'.\n", hThread_CriticalSectionFunctions_test3[1],
              GetLastError());
    }
done:
    if (0 == CloseHandle(hThread_CriticalSectionFunctions_test3[0]))
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%d'.\n", hThread_CriticalSectionFunctions_test3[0],
              GetLastError());
    }
    if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[0]))
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[0],
              GetLastError());
    }
    if (0 == CloseHandle(hEvent_CriticalSectionFunctions_test3[1]))
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%d'.\n", hEvent_CriticalSectionFunctions_test3[1],
              GetLastError());
    }
    DeleteCriticalSection(&CriticalSection);

    PAL_TerminateEx(bRet_CriticalSectionFunctions_test3);

    return (bRet_CriticalSectionFunctions_test3);
}

