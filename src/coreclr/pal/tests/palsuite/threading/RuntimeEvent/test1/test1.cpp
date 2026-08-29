// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <palsuite.h>

#include "RuntimeEvent.h"

static DWORD PALAPI SignalRuntimeEvent(LPVOID context)
{
    PAL_Sleep(10);
    return PAL_SetEvent(context) ? 0 : 1;
}

static void VerifyWait(void* event, uint32_t timeout, uint32_t expected)
{
    void* events[] = { event };
    uint32_t result = PAL_WaitForMultipleObjectsEx(1, events, false, timeout, false);
    if (result != expected)
    {
        Fail("PAL_WaitForMultipleObjectsEx returned %u instead of %u\n", result, expected);
    }
}

PALTEST(threading_RuntimeEvent_test1_paltest_runtimeevent_test1, "threading/RuntimeEvent/test1/paltest_runtimeevent_test1")
{
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    void* autoResetEvent = PAL_CreateEvent(nullptr, false, false);
    if (autoResetEvent == nullptr)
    {
        Fail("PAL_CreateEvent failed to create an auto-reset event\n");
    }

    VerifyWait(autoResetEvent, 0, WAIT_TIMEOUT);

    DWORD threadId;
    HANDLE thread = CreateThread(nullptr, 0, SignalRuntimeEvent, autoResetEvent, 0, &threadId);
    if (thread == nullptr)
    {
        Fail("CreateThread failed with error %u\n", GetLastError());
    }

    VerifyWait(autoResetEvent, 10000, WAIT_OBJECT_0);

    CloseHandle(thread);

    VerifyWait(autoResetEvent, 0, WAIT_TIMEOUT);

    if (!PAL_SetEvent(autoResetEvent))
    {
        Fail("PAL_SetEvent failed for an auto-reset event\n");
    }

    VerifyWait(autoResetEvent, 0, WAIT_OBJECT_0);
    VerifyWait(autoResetEvent, 0, WAIT_TIMEOUT);

    if (!PAL_CloseEvent(autoResetEvent))
    {
        Fail("PAL_CloseEvent failed for an auto-reset event\n");
    }

    void* manualResetEvent = PAL_CreateEvent(nullptr, true, false);
    if (manualResetEvent == nullptr)
    {
        Fail("PAL_CreateEvent failed to create a manual-reset event\n");
    }

    VerifyWait(manualResetEvent, 0, WAIT_TIMEOUT);

    if (!PAL_SetEvent(manualResetEvent))
    {
        Fail("PAL_SetEvent failed for a manual-reset event\n");
    }

    VerifyWait(manualResetEvent, 0, WAIT_OBJECT_0);
    VerifyWait(manualResetEvent, 0, WAIT_OBJECT_0);

    if (!PAL_ResetEvent(manualResetEvent))
    {
        Fail("PAL_ResetEvent failed for a manual-reset event\n");
    }

    VerifyWait(manualResetEvent, 0, WAIT_TIMEOUT);

    if (!PAL_CloseEvent(manualResetEvent))
    {
        Fail("PAL_CloseEvent failed for a manual-reset event\n");
    }

    PAL_Terminate();
    return PASS;
}
