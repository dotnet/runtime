// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: test5.c
**
** Purpose: Test the functionality of simultaneously waiting
** on multiple processes. Create the same number of helper
** processes and helper threads.
** Helper threads wait on helper processes to finish.
** Helper processes wait on the event signal from test
** thread before exit.
** The test thread can wake up one helper
** thread at a time by signaling the corresponding helper
** process to finish.
** The test thread can also wake up all helper threads at once
** by signaling help process 0 to exit.
**
**
**============================================================*/

#define UNICODE

#include "commonconsts.h"

#include <palsuite.h>

/* The maximum number of objects a thread can wait is MAXIMUM_WAIT_OBJECTS.
   The last helper thread in this test case will wait on all helper processes
   plus a thread finish event so the maximum number of helper processes
   can be created in this test case is (MAXIMUM_WAIT_OBJECTS-1). */
#define MAX_HELPER_PROCESS (MAXIMUM_WAIT_OBJECTS-1)

int MaxNumHelperProcess = MAX_HELPER_PROCESS;

/* indicate how the test thread wake up helper thread. */
typedef enum _TestCaseType {
    WakeUpOneThread, /* wake up one helper thread at a time. */
    WakeUpAllThread  /* wake up all helper threads at once */
} TestCaseType;

TestCaseType TestCase = WakeUpOneThread;

/* When the test thread wakes up one thread at a time,
   ThreadIndexOfThreadFinishEvent specifies the index of the thread that
   should be waked up using hThreadFinishEvent instead of helper process. */
DWORD ThreadIndexOfThreadFinishEvent = 0;

struct helper_process_t
{
    PROCESS_INFORMATION pi;
    HANDLE hProcessReadyEvent;
    HANDLE hProcessFinishEvent;
} helper_process[MAX_HELPER_PROCESS];

HANDLE hProcessStartEvent_WFMO_test5;

struct helper_thread_t
{
    HANDLE hThread;
    DWORD dwThreadId;
    HANDLE hThreadReadyEvent;
    HANDLE hThreadFinishEvent;
} helper_thread[MAX_HELPER_PROCESS];

/*
 * Entry Point for helper thread.
 */
DWORD PALAPI WaitForProcess(LPVOID lpParameter)
{
    DWORD index, i;
    DWORD dwRet;
    HANDLE handles[MAX_HELPER_PROCESS+1];

    index = (DWORD)(SIZE_T) lpParameter;

    /* The helper thread 0 will wait for helper process 0, helper thread 1 will
       wait for helper process 0 and 1, helper thread 2 will wait for helper
       process 0, 1, and 2, and so on ..., and the last helper thread will wait
       on all helper processes.
       Each helper thread also waits on hThreadFinishEvent so that
       it can exit without waiting on any process to finish. */

    for (i = 0; i <= index; i++)
    {
        handles[i] = helper_process[i].pi.hProcess;
    }

    handles[index+1] = helper_thread[index].hThreadFinishEvent;

    if(!SetEvent(helper_thread[index].hThreadReadyEvent))
    {
        Fail("test5.WaitProcess: SetEvent of hThreadReadyEvent failed for thread %d. "
            "GetLastError() returned %d.\n", index,
            GetLastError());
    }

    dwRet = WaitForMultipleObjectsEx(index+2, &handles[0], FALSE, TIMEOUT, TRUE);
    if (WakeUpAllThread == TestCase)
    {
        /* If the test thread signals helper process 0 to exit, all threads will be waked up,
           and the return value must be (WAIT_OBJECT_0+0) because the handle of helper process 0
           is in handle[0]. */
        if (dwRet != (WAIT_OBJECT_0+0))
        {
            Fail("test5.WaitForProcess: invalid return value %d for WakupAllThread from WaitForMultipleObjectsEx for thread %d\n"
                    "LastError:(%u)\n",
                    dwRet, index,
                    GetLastError());
        }
    }
    else if (WakeUpOneThread == TestCase)
    {
        /* If the test thread wakes up one helper thread at a time,
           the return value must be either (WAIT_OBJECT_0+index) if the helper thread
           wakes up because the corresponding help process exits,
           or (index+1) if the helper thread wakes up because of hThreadReadyEvent. */
        if ((index != ThreadIndexOfThreadFinishEvent && dwRet != (WAIT_OBJECT_0+index)) ||
            (index == ThreadIndexOfThreadFinishEvent && dwRet != (index+1)))
        {
            Fail("test5.WaitForProcess: invalid return value %d for WakupOneThread from WaitForMultipleObjectsEx for thread %d\n"
                    "LastError:(%u)\n",
                    dwRet, index,
                    GetLastError());
        }
    }
    else
    {
        Fail("Unknown TestCase %d\n", TestCase);
    }
    return 0;
}

/*
 * Setup the helper processes and helper threads.
 */
void
Setup()
{

    STARTUPINFO si;
    DWORD dwRet;
    int i;

    char szEventName[MAX_PATH];
    PWCHAR uniStringHelper;
    PWCHAR uniString;

    /* Create the event to start helper process after it was created. */
    uniString = convert(szcHelperProcessStartEvName);
    hProcessStartEvent_WFMO_test5 = CreateEvent(NULL, TRUE, FALSE, uniString);
    free(uniString);
    if (!hProcessStartEvent_WFMO_test5)
    {
        Fail("test5.Setup: CreateEvent of '%s' failed. "
             "GetLastError() returned %d.\n", szcHelperProcessStartEvName,
             GetLastError());
    }

    /* Create the helper processes. */
    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof(si);
    uniStringHelper = convert("helper");
    for (i = 0; i < MaxNumHelperProcess; i++)
    {
        ZeroMemory( &helper_process[i].pi, sizeof(PROCESS_INFORMATION));

        if(!CreateProcess( NULL, uniStringHelper, NULL, NULL,
                            FALSE, 0, NULL, NULL, &si, &helper_process[i].pi))
        {
            Fail("test5.Setup: CreateProcess failed to load executable for helper process %d.  "
                "GetLastError() returned %u.\n",
                i, GetLastError());
        }

        /* Create the event to let helper process tell us it is ready. */
        if (sprintf_s(szEventName, MAX_PATH-1, "%s%d",
            szcHelperProcessReadyEvName, helper_process[i].pi.dwProcessId) < 0)
        {
            Fail ("test5.Setup: Insufficient event name string length for %s\n", szcHelperProcessReadyEvName);
        }

        uniString = convert(szEventName);

        helper_process[i].hProcessReadyEvent = CreateEvent(NULL, FALSE, FALSE, uniString);
        free(uniString);
        if (!helper_process[i].hProcessReadyEvent)
        {
            Fail("test5.Setup: CreateEvent of '%s' failed. "
                "GetLastError() returned %d.\n", szEventName,
                GetLastError());
        }

        /* Create the event to tell helper process to exit. */
        if (sprintf_s(szEventName, MAX_PATH-1, "%s%d",
            szcHelperProcessFinishEvName, helper_process[i].pi.dwProcessId) < 0)
        {
            Fail ("test5.Setup: Insufficient event name string length for %s\n", szcHelperProcessFinishEvName);
        }

        uniString = convert(szEventName);

        helper_process[i].hProcessFinishEvent = CreateEvent(NULL, TRUE, FALSE, uniString);
        free(uniString);
        if (!helper_process[i].hProcessFinishEvent)
        {
            Fail("test5.Setup: CreateEvent of '%s' failed. "
                "GetLastError() returned %d.\n", szEventName,
                GetLastError());
        }

    }
    free(uniStringHelper);

    /* Signal all helper processes to start. */
    if (!SetEvent(hProcessStartEvent_WFMO_test5))
    {
        Fail("test5.Setup: SetEvent '%s' failed\n",
            "LastError:(%u)\n",
            szcHelperProcessStartEvName, GetLastError());
    }

    /* Wait for ready signals from all helper processes. */
    for (i = 0; i < MaxNumHelperProcess; i++)
    {
        dwRet = WaitForSingleObject(helper_process[i].hProcessReadyEvent, TIMEOUT);
        if (dwRet != WAIT_OBJECT_0)
        {
            Fail("test5.Setup: WaitForSingleObject %s failed for helper process %d\n"
                    "LastError:(%u)\n",
                    szcHelperProcessReadyEvName, i, GetLastError());
        }
    }

    /* Create the same number of helper threads as helper processes. */
    for (i = 0; i < MaxNumHelperProcess; i++)
    {
        /* Create the event to let helper thread tell us it is ready. */
        helper_thread[i].hThreadReadyEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
        if (!helper_thread[i].hThreadReadyEvent)
        {
            Fail("test5.Setup: CreateEvent of hThreadReadyEvent failed for thread %d\n"
                "LastError:(%u)\n", i, GetLastError());
        }

        /* Create the event to tell helper thread to exit without waiting for helper process. */
        helper_thread[i].hThreadFinishEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
        if (!helper_thread[i].hThreadFinishEvent)
        {
            Fail("test5.Setup: CreateEvent of hThreadFinishEvent failed for thread %d\n"
                "LastError:(%u)\n", i, GetLastError());
        }

        /* Create the helper thread. */
        helper_thread[i].hThread = CreateThread( NULL,
                                0,
                                (LPTHREAD_START_ROUTINE)WaitForProcess,
                                (LPVOID)i,
                                0,
                                &helper_thread[i].dwThreadId);
        if (NULL == helper_thread[i].hThread)
        {
            Fail("test5.Setup: Unable to create the helper thread %d\n"
                "LastError:(%u)\n", i, GetLastError());
        }
    }

    /* Wait for ready signals from all helper threads. */
    for (i = 0; i < MaxNumHelperProcess; i++)
    {
        dwRet = WaitForSingleObject(helper_thread[i].hThreadReadyEvent, TIMEOUT);
        if (dwRet != WAIT_OBJECT_0)
        {
            Fail("test5.Setup: WaitForSingleObject hThreadReadyEvent for thread %d\n"
                    "LastError:(%u)\n", i, GetLastError());
        }
    }
}

/*
 * Cleanup the helper processes and helper threads.
 */
DWORD
Cleanup_WFMO_test5()
{
    DWORD dwExitCode;
    DWORD dwRet;
    int i;

    /* Wait for all helper process to finish and close their handles
       and associated events. */
    for (i = 0; i < MaxNumHelperProcess; i++)
    {

        /* wait for the child process to complete */
        dwRet = WaitForSingleObject ( helper_process[i].pi.hProcess, TIMEOUT );
        if (WAIT_OBJECT_0 != dwRet)
        {
            Fail("test5.Cleanup: WaitForSingleObject hThreadReadyEvent failed for thread %d\n"
                    "LastError:(%u)\n", i, GetLastError());
        }

        /* check the exit code from the process */
        if (!GetExitCodeProcess(helper_process[i].pi.hProcess, &dwExitCode))
        {
            Trace( "test5.Cleanup: GetExitCodeProcess %d call failed LastError:(%u)\n",
                i, GetLastError());
            dwExitCode = FAIL;
        }
        PEDANTIC(CloseHandle, (helper_process[i].pi.hThread));
        PEDANTIC(CloseHandle, (helper_process[i].pi.hProcess));
        PEDANTIC(CloseHandle, (helper_process[i].hProcessReadyEvent));
        PEDANTIC(CloseHandle, (helper_process[i].hProcessFinishEvent));
    }

    /* Close all helper threads' handles */
    for (i = 0; i < MaxNumHelperProcess; i++)
    {
        PEDANTIC(CloseHandle, (helper_thread[i].hThread));
        PEDANTIC(CloseHandle, (helper_thread[i].hThreadReadyEvent));
        PEDANTIC(CloseHandle, (helper_thread[i].hThreadFinishEvent));
    }

    /* Close all process start event. */
    PEDANTIC(CloseHandle, (hProcessStartEvent_WFMO_test5));

    return dwExitCode;
}

/*
 * In this test case, the test thread will signal one helper
 * process to exit at a time starting from the last helper
 * process and then wait for the corresponding helper thread to exit.
 * The ThreadIndexOfThreadFinishEvent specifies the index of the thread that
 * should be waked up using hThreadFinishEvent instead of helper process.
 */
void
TestWakeupOneThread()
{
    DWORD dwRet;
    int i;

    TestCase = WakeUpOneThread;

    if (((LONG)ThreadIndexOfThreadFinishEvent) < 0 ||
        ThreadIndexOfThreadFinishEvent >= MAX_HELPER_PROCESS)
        Fail("test5.TestWaitOnOneThread: Invalid ThreadIndexOfThreadFinishEvent %d\n", ThreadIndexOfThreadFinishEvent);

    /* Since helper thread 0 waits on helper process 0,
       thread 1 waits on process 0, and 1,
       thread 2 waits on process 0, 1, and 2, and so on ...,
       and the last helper thread will wait on all helper processes,
       the helper thread can be waked up one at a time by
       waking up the help process one at a time starting from the
       last helper process. */
    for (i = MaxNumHelperProcess-1; i >= 0; i--)
    {
        /* make sure the helper thread has not exited yet. */
        dwRet = WaitForSingleObject(helper_thread[i].hThread, 0);
        if (WAIT_TIMEOUT != dwRet)
        {
            Fail("test5.TestWaitOnOneThread: helper thread %d already exited %d\n", i);
        }

        /* Decide how to wakeup the helper thread:
           using event or using helper process. */
        if (i == ThreadIndexOfThreadFinishEvent)
        {
           if (!SetEvent(helper_thread[i].hThreadFinishEvent))
            {
                Fail("test5.TestWaitOnOneThread: SetEvent hThreadFinishEvent failed for thread %d\n",
                    "LastError:(%u)\n", i, GetLastError());
            }
        }
        else
        {
            if (!SetEvent(helper_process[i].hProcessFinishEvent))
            {
                Fail("test5.TestWaitOnOneThread: SetEvent %s%d failed for helper process %d\n",
                    "LastError:(%u)\n",
                    szcHelperProcessFinishEvName, helper_process[i].pi.dwProcessId, i,
                    GetLastError());
            }
        }

        dwRet = WaitForSingleObject(helper_thread[i].hThread, TIMEOUT);
        if (WAIT_OBJECT_0 != dwRet)
        {
            Fail("test5.TestWaitOnOneThread: WaitForSingleObject helper thread %d"
                    "LastError:(%u)\n",
                    i, GetLastError());
        }
    }

    /* Finally, need to wake up the helper process which the test thread
       skips waking up in the last loop. */
    if (!SetEvent(helper_process[ThreadIndexOfThreadFinishEvent].hProcessFinishEvent))
    {
        Fail("test5.TestWaitOnOneThread: SetEvent %s%d failed\n",
            "LastError:(%u)\n",
            szcHelperProcessFinishEvName, helper_process[ThreadIndexOfThreadFinishEvent].pi.dwProcessId,
            GetLastError());
    }
}

/*
 * In this test case, the test thread will signal the helper
 * process 0 to exit. Since all helper threads wait on process 0,
 * all helper threads will wake up and exit, and the test thread
 * will wait for all of them to exit.
 */
void
TestWakeupAllThread()
{
    DWORD dwRet;
    int i;

    TestCase = WakeUpAllThread;

    /* make sure none of the helper thread exits. */
    for (i = 0; i < MaxNumHelperProcess; i++)
    {
        dwRet = WaitForSingleObject(helper_thread[i].hThread, 0);
        if (WAIT_TIMEOUT != dwRet)
        {
            Fail("test5.TestWaitOnAllThread: helper thread %d already exited %d\n", i);
        }
    }

    /* Signal helper process 0 to exit. */
    if (!SetEvent(helper_process[0].hProcessFinishEvent))
    {
        Fail("test5.TestWaitOnAllThread: SetEvent %s%d failed\n",
            "LastError:(%u)\n",
            szcHelperProcessFinishEvName, helper_process[0].pi.dwProcessId,
            GetLastError());
    }

    /* Wait for all helper threads to exit. */
    for (i = 0; i < MaxNumHelperProcess; i++)
    {

        dwRet = WaitForSingleObject(helper_thread[i].hThread, TIMEOUT);
        if (WAIT_OBJECT_0 != dwRet)
        {
            Fail("test5.TestWaitOnAllThread: WaitForSingleObject failed for helper thread %d\n"
                    "LastError:(%u)\n",
                    i, GetLastError());
        }
    }

    /* Signal the rest of helper processes to exit. */
    for (i = 1; i < MaxNumHelperProcess; i++)
    {
        if (!SetEvent(helper_process[i].hProcessFinishEvent))
        {
            Fail("test5.TestWaitOnAllThread: SetEvent %s%d failed\n",
                "LastError:(%u)\n",
                szcHelperProcessFinishEvName, helper_process[i].pi.dwProcessId,
                GetLastError());
        }
    }
}

PALTEST(threading_WaitForMultipleObjectsEx_test5_paltest_waitformultipleobjectsex_test5, "threading/WaitForMultipleObjectsEx/test5/paltest_waitformultipleobjectsex_test5")
{
    DWORD dwExitCode;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    switch (argc)
    {
        case 1:
            MaxNumHelperProcess = MAX_HELPER_PROCESS;
            break;
        case 2:
            MaxNumHelperProcess = atoi(argv[1]);
            break;
        default:
            Fail("Invalid number of arguments\n");
    }

    if (MaxNumHelperProcess < 1 ||
        MaxNumHelperProcess > MAX_HELPER_PROCESS)
        Fail("test5.main: Invalid MaxNumHelperProcess %d\n", MaxNumHelperProcess);

    Setup();
    ThreadIndexOfThreadFinishEvent = 3;
    TestWakeupOneThread();
    dwExitCode = Cleanup_WFMO_test5();

    if (PASS == dwExitCode)
    {
        Setup();
        TestWakeupAllThread();
        dwExitCode = Cleanup_WFMO_test5();
    }

    PAL_TerminateEx(dwExitCode);
    return dwExitCode;
}
