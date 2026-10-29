// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <palsuite.h>

#include "CLREventBase.h"

static DWORD PALAPI SignalRuntimeEvent(LPVOID context)
{
    minipal_sleep(10);
    return static_cast<CLREventBase*>(context)->Set() ? 0 : 1;
}

static void VerifyWait(CLREventBase& event, uint32_t timeout, uint32_t expected)
{
    uint32_t result = event.Wait(timeout);
    if (result != expected)
    {
        Fail("CLREventBase::Wait returned %u instead of %u\n", result, expected);
    }
}

PALTEST(threading_RuntimeEvent_test1_paltest_runtimeevent_test1, "threading/RuntimeEvent/test1/paltest_runtimeevent_test1")
{
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    CLREventBase autoResetEvent;
    if (!autoResetEvent.CreateAutoEventNoThrow(false))
    {
        Fail("CLREventBase failed to create an auto-reset event\n");
    }

    VerifyWait(autoResetEvent, 0, WAIT_TIMEOUT);

    DWORD threadId;
    HANDLE thread = CreateThread(nullptr, 0, SignalRuntimeEvent, &autoResetEvent, 0, &threadId);
    if (thread == nullptr)
    {
        Fail("CreateThread failed with error %u\n", GetLastError());
    }

    VerifyWait(autoResetEvent, 10000, WAIT_OBJECT_0);

    CloseHandle(thread);

    VerifyWait(autoResetEvent, 0, WAIT_TIMEOUT);

    if (!autoResetEvent.Set())
    {
        Fail("CLREventBase::Set failed for an auto-reset event\n");
    }

    VerifyWait(autoResetEvent, 0, WAIT_OBJECT_0);
    VerifyWait(autoResetEvent, 0, WAIT_TIMEOUT);

    autoResetEvent.CloseEvent();

    CLREventBase manualResetEvent;
    if (!manualResetEvent.CreateManualEventNoThrow(false))
    {
        Fail("CLREventBase failed to create a manual-reset event\n");
    }

    VerifyWait(manualResetEvent, 0, WAIT_TIMEOUT);

    if (!manualResetEvent.Set())
    {
        Fail("CLREventBase::Set failed for a manual-reset event\n");
    }

    VerifyWait(manualResetEvent, 0, WAIT_OBJECT_0);
    VerifyWait(manualResetEvent, 0, WAIT_OBJECT_0);

    if (!manualResetEvent.Reset())
    {
        Fail("CLREventBase::Reset failed for a manual-reset event\n");
    }

    VerifyWait(manualResetEvent, 0, WAIT_TIMEOUT);

    manualResetEvent.CloseEvent();

    PAL_Terminate();
    return PASS;
}
