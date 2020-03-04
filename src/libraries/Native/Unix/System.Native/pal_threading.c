// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_threading.h"
#include "pal_compiler.h"

#include <limits.h>
#include <sched.h>

bool LowLevelMonitor_TryInitialize(LowLevelMonitor *monitor)
{
    assert(monitor != NULL);

    if (!LowLevelMutex_TryInitialize(&monitor->m_mutex))
    {
        return false;
    }

    int initError = pthread_cond_init(&monitor->m_condition, NULL);
    if (initError != 0)
    {
        LowLevelMutex_Destroy(&monitor->m_mutex);
        return false;
    }

    return true;
}

PALEXPORT LowLevelMonitor *SystemNative_LowLevelMonitor_New(void);
PALEXPORT LowLevelMonitor *SystemNative_LowLevelMonitor_New()
{
    LowLevelMonitor *monitor = (LowLevelMonitor *)malloc(sizeof(LowLevelMonitor));
    if (monitor == NULL)
    {
        return NULL;
    }

    if (!LowLevelMonitor_TryInitialize(monitor))
    {
        free(monitor);
        return NULL;
    }
    return monitor;
}

PALEXPORT void SystemNative_LowLevelMonitor_Delete(LowLevelMonitor *monitor);
PALEXPORT void SystemNative_LowLevelMonitor_Delete(LowLevelMonitor *monitor)
{
    assert(monitor != NULL);

    LowLevelMonitor_Destroy(monitor);
    free(monitor);
}

PALEXPORT void SystemNative_LowLevelMonitor_Acquire(LowLevelMonitor *monitor);
PALEXPORT void SystemNative_LowLevelMonitor_Acquire(LowLevelMonitor *monitor)
{
    assert(monitor != NULL);
    LowLevelMutex_Acquire(&monitor->m_mutex);
}

PALEXPORT void SystemNative_LowLevelMonitor_Release(LowLevelMonitor *monitor);
PALEXPORT void SystemNative_LowLevelMonitor_Release(LowLevelMonitor *monitor)
{
    assert(monitor != NULL);
    LowLevelMutex_Release(&monitor->m_mutex);
}

PALEXPORT void SystemNative_LowLevelMonitor_Wait(LowLevelMonitor *monitor);
PALEXPORT void SystemNative_LowLevelMonitor_Wait(LowLevelMonitor *monitor)
{
    assert(monitor != NULL);
    LowLevelMonitor_Wait(monitor);
}

PALEXPORT void SystemNative_LowLevelMonitor_Signal_Release(LowLevelMonitor *monitor);
PALEXPORT void SystemNative_LowLevelMonitor_Signal_Release(LowLevelMonitor *monitor)
{
    assert(monitor != NULL);

    LowLevelMonitor_Signal(monitor);
    LowLevelMutex_Release(&monitor->m_mutex);
}
