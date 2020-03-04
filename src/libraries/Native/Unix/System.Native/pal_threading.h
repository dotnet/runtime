// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <assert.h>
#include <stdbool.h>
#include <stdlib.h>

#include <pthread.h>

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// LowLevelMutex - Represents a non-recursive mutex

typedef struct
{
    pthread_mutex_t m_mutex;

#ifdef DEBUG
    bool m_isLocked;
#endif
} LowLevelMutex;

bool LowLevelMutex_TryInitialize(LowLevelMutex *mutex);
bool LowLevelMutex_TryInitialize(LowLevelMutex *mutex)
{
    assert(mutex != NULL);

#ifdef DEBUG
    mutex->m_isLocked = false;
#endif

    int error = pthread_mutex_init(&mutex->m_mutex, NULL);
    return error == 0;
}

void LowLevelMutex_Destroy(LowLevelMutex *mutex);
void LowLevelMutex_Destroy(LowLevelMutex *mutex)
{
    assert(mutex != NULL);

    int error = pthread_mutex_destroy(&mutex->m_mutex);
    assert(error == 0);

    (void)error; // unused in release build
}

void LowLevelMutex_SetIsLocked(LowLevelMutex *mutex, bool isLocked);
void LowLevelMutex_SetIsLocked(LowLevelMutex *mutex, bool isLocked)
{
    assert(mutex != NULL);

#ifdef DEBUG
    assert(mutex->m_isLocked != isLocked);
    mutex->m_isLocked = isLocked;
#endif

    (void)mutex; // unused in release build
    (void)isLocked; // unused in release build
}

void LowLevelMutex_Acquire(LowLevelMutex *mutex);
void LowLevelMutex_Acquire(LowLevelMutex *mutex)
{
    assert(mutex != NULL);

    int error = pthread_mutex_lock(&mutex->m_mutex);
    assert(error == 0);
    LowLevelMutex_SetIsLocked(mutex, true);

    (void)error; // unused in release build
}

void LowLevelMutex_Release(LowLevelMutex *mutex);
void LowLevelMutex_Release(LowLevelMutex *mutex)
{
    assert(mutex != NULL);

    LowLevelMutex_SetIsLocked(mutex, false);
    int error = pthread_mutex_unlock(&mutex->m_mutex);
    assert(error == 0);

    (void)error; // unused in release build
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// LowLevelMonitor - Represents a non-recursive mutex and condition

typedef struct
{
    LowLevelMutex m_mutex;
    pthread_cond_t m_condition;
} LowLevelMonitor;

bool LowLevelMonitor_TryInitialize(LowLevelMonitor *monitor);

void LowLevelMonitor_Destroy(LowLevelMonitor *monitor);
void LowLevelMonitor_Destroy(LowLevelMonitor *monitor)
{
    assert(monitor != NULL);

    int error = pthread_cond_destroy(&monitor->m_condition);
    assert(error == 0);

    LowLevelMutex_Destroy(&monitor->m_mutex);

    (void)error; // unused in release build
}

void LowLevelMonitor_Wait(LowLevelMonitor *monitor);
void LowLevelMonitor_Wait(LowLevelMonitor *monitor)
{
    assert(monitor != NULL);

    LowLevelMutex_SetIsLocked(&monitor->m_mutex, false);
    int error = pthread_cond_wait(&monitor->m_condition, &monitor->m_mutex.m_mutex);
    assert(error == 0);
    LowLevelMutex_SetIsLocked(&monitor->m_mutex, true);

    (void)error; // unused in release build
}

void LowLevelMonitor_Signal(LowLevelMonitor *monitor);
void LowLevelMonitor_Signal(LowLevelMonitor *monitor)
{
    assert(monitor != NULL);

    int error = pthread_cond_signal(&monitor->m_condition);
    assert(error == 0);

    (void)error; // unused in release build
}
