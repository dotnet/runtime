// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: helper.c
**
** Purpose: This helper process sets up signals to communicate
** with the test thread in the parent process, and let the test
** thread signal this process when to exit.
**
**
**============================================================*/

#include "commonconsts.h"

#include <palsuite.h>

HANDLE hProcessStartEvent_WFMO_test5_helper;
HANDLE hProcessReadyEvent;
HANDLE hProcessFinishEvent;
HANDLE hProcessCleanupEvent;


PALTEST(threading_WaitForMultipleObjectsEx_test5_paltest_waitformultipleobjectsex_test5_helper, "threading/WaitForMultipleObjectsEx/test5/paltest_waitformultipleobjectsex_test5_helper")
{
     
    BOOL  success = TRUE;  /* assume success */
    DWORD dwRet;
    DWORD dwProcessId;
    char szEventName[MAX_LONGPATH];
    PWCHAR uniString;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Open the event to let test thread tell us to get started. */
    uniString = convert(szcHelperProcessStartEvName);
    hProcessStartEvent_WFMO_test5_helper = OpenEventW(EVENT_ALL_ACCESS, 0, uniString);
    free(uniString);
    if (!hProcessStartEvent_WFMO_test5_helper) 
    {
        Fail("helper.main: OpenEvent of '%S' failed (%u). "
             "(the event should already exist!)\n", 
             szcHelperProcessStartEvName, GetLastError());
    }

    /* Wait for signal from test thread. */
    dwRet = WaitForSingleObject(hProcessStartEvent_WFMO_test5_helper, TIMEOUT);
    if (dwRet != WAIT_OBJECT_0)
    {
        Fail("helper.main: WaitForSingleObject '%s' failed\n"
                "LastError:(%u)\n", szcHelperProcessStartEvName, GetLastError());
    }

    dwProcessId = GetCurrentProcessId();
    
    if ( 0 >= dwProcessId ) 
    {
        Fail ("helper.main: %s has invalid pid %d\n", argv[0], dwProcessId );
    }

    /* Open the event to tell test thread we are ready. */
    if (sprintf_s(szEventName, MAX_LONGPATH-1, "%s%d", szcHelperProcessReadyEvName, dwProcessId) < 0)
    {
        Fail ("helper.main: Insufficient event name string length for pid=%d\n", dwProcessId);
    }

    uniString = convert(szEventName);

    hProcessReadyEvent = OpenEventW(EVENT_ALL_ACCESS, 0, uniString);
    free(uniString);
    if (!hProcessReadyEvent) 
    {
        Fail("helper.main: OpenEvent of '%s' failed (%u). "
             "(the event should already exist!)\n", 
             szEventName, GetLastError());
    }

    /* Open the event to let test thread tell us to exit. */
    if (sprintf_s(szEventName, MAX_LONGPATH-1, "%s%d", szcHelperProcessFinishEvName, dwProcessId) < 0)
    {
        Fail ("helper.main: Insufficient event name string length for pid=%d\n", dwProcessId);
    }

    uniString = convert(szEventName);

    hProcessFinishEvent = OpenEventW(EVENT_ALL_ACCESS, 0, uniString);
    free(uniString);
    if (!hProcessFinishEvent) 
    {
        Fail("helper.main: OpenEvent of '%s' failed LastError:(%u).\n",
             szEventName, GetLastError());
    }

    /* Tell the test thread we are ready. */
    if (!SetEvent(hProcessReadyEvent))
    {
        Fail("helper.main: SetEvent '%s' failed LastError:(%u)\n",
            hProcessReadyEvent, GetLastError());
    }

    /* Wait for signal from test thread before exit. */
    dwRet = WaitForSingleObject(hProcessFinishEvent, TIMEOUT);
    if (WAIT_OBJECT_0 != dwRet)
    {
        Fail("helper.main: WaitForSingleObject '%s' failed pid=%d\n"
            "LastError:(%u)\n", 
            szcHelperProcessFinishEvName, dwProcessId, GetLastError());
    }

    PEDANTIC(CloseHandle, (hProcessStartEvent_WFMO_test5_helper));
    PEDANTIC(CloseHandle, (hProcessReadyEvent));
    PEDANTIC(CloseHandle, (hProcessFinishEvent));

    PAL_TerminateEx(success ? PASS : FAIL);

    return success ? PASS : FAIL;
}
