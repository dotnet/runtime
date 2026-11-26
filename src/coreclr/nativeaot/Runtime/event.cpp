// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "daccess.h"
#include "event.h"
#include "Pal.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

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
            : Wait(milliseconds, alertable);
#else
        result = Wait(milliseconds, false);
#endif // TARGET_WINDOWS

        if (disablePreemptive)
        {
            currentThread->DisablePreemptiveMode();
        }
    }

    return result;
}
