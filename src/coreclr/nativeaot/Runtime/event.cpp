// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "daccess.h"
#include "event.h"
#include "Pal.h"
#include "thread.h"
#include "threadstore.h"

// CLR wrapper around native events.

uint32_t CLREventBase::Wait(uint32_t milliseconds, bool alertable, bool allowReentrantWait)
{
    uint32_t result = WAIT_FAILED;

    if (IsValid())
    {
        bool disablePreemptive = false;
        Thread* currentThread = ThreadStore::GetCurrentThreadIfAvailable();

        if (currentThread != NULL && currentThread->IsCurrentThreadInCooperativeMode())
        {
            currentThread->EnablePreemptiveMode();
            disablePreemptive = true;
        }

#ifdef TARGET_WINDOWS
        HANDLE event = GetOSEvent();
        result = allowReentrantWait
            ? PalCompatibleWaitAny(alertable, milliseconds, 1, &event, TRUE)
            : CLREventBase::Wait(event, milliseconds, alertable);
#else
        result = CLREventBase::Wait(GetOSEvent(), milliseconds);
#endif // TARGET_WINDOWS

        if (disablePreemptive)
        {
            currentThread->DisablePreemptiveMode();
        }
    }

    return result;
}
