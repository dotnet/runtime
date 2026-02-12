// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "asyncsafethreadmap.h"

// Async safe lock free thread map for use in signal handlers

struct ThreadEntry
{
    size_t osThread;
    void* pThread;
};

#define MAX_THREADS_IN_SEGMENT 256

struct ThreadSegment
{
    ThreadEntry entries[MAX_THREADS_IN_SEGMENT];
    ThreadSegment* pNext;
};

static ThreadSegment *s_pAsyncSafeThreadMapHead = NULL;

bool InsertThreadIntoAsyncSafeMap(size_t osThread, void* pThread)
{
    size_t startIndex = osThread % MAX_THREADS_IN_SEGMENT;

    ThreadSegment* pSegment = s_pAsyncSafeThreadMapHead;
    ThreadSegment** ppSegment = &s_pAsyncSafeThreadMapHead;
    while (true)
    {
        if (pSegment == NULL)
        {
            // Need to add a new segment
            ThreadSegment* pNewSegment = new (nothrow) ThreadSegment();
            if (pNewSegment == NULL)
            {
                // Memory allocation failed
                return false;
            }

            memset(pNewSegment, 0, sizeof(ThreadSegment));
            ThreadSegment* pExpected = NULL;           
            if (!__atomic_compare_exchange_n(
                ppSegment,
                &pExpected,
                pNewSegment,
                false /* weak */,
                __ATOMIC_RELEASE  /* success_memorder */,
                __ATOMIC_RELAXED /* failure_memorder */))
            {
                // Another thread added the segment first
                delete pNewSegment;
                pNewSegment = pExpected;
            }

            pSegment = pNewSegment;
        }
        for (size_t i = 0; i < MAX_THREADS_IN_SEGMENT; i++)
        {
            size_t index = (startIndex + i) % MAX_THREADS_IN_SEGMENT;

            size_t expected = 0;
            if (__atomic_compare_exchange_n(
                    &pSegment->entries[index].osThread,
                    &expected,
                    osThread,
                    false /* weak */,
                    __ATOMIC_RELEASE  /* success_memorder */,
                    __ATOMIC_RELAXED /* failure_memorder */))
            {
                // Successfully inserted
                // Use atomic store with release to ensure proper ordering
                __atomic_store_n(&pSegment->entries[index].pThread, pThread, __ATOMIC_RELEASE);
                return true;
            }
        }

        ppSegment = &pSegment->pNext;
        pSegment = __atomic_load_n(&pSegment->pNext, __ATOMIC_ACQUIRE);
    }
}

void RemoveThreadFromAsyncSafeMap(size_t osThread, void* pThread)
{
    size_t startIndex = osThread % MAX_THREADS_IN_SEGMENT;

    ThreadSegment* pSegment = s_pAsyncSafeThreadMapHead;
    while (pSegment)
    {
        for (size_t i = 0; i < MAX_THREADS_IN_SEGMENT; i++)
        {
            size_t index = (startIndex + i) % MAX_THREADS_IN_SEGMENT;
            if (pSegment->entries[index].pThread == pThread)
            {
                // Found the entry, remove it
                pSegment->entries[index].pThread = NULL;
                __atomic_exchange_n(&pSegment->entries[index].osThread, (size_t)0, __ATOMIC_RELEASE);
                return;
            }
        }
        pSegment = __atomic_load_n(&pSegment->pNext, __ATOMIC_ACQUIRE);
    }
}

void *FindThreadInAsyncSafeMap(size_t osThread)
{
    size_t startIndex = osThread % MAX_THREADS_IN_SEGMENT;
    ThreadSegment* pSegment = s_pAsyncSafeThreadMapHead;
    while (pSegment)
    {
        for (size_t i = 0; i < MAX_THREADS_IN_SEGMENT; i++)
        {
            size_t index = (startIndex + i) % MAX_THREADS_IN_SEGMENT;
            // Use acquire to synchronize with release in InsertThreadIntoAsyncSafeMap
            if (__atomic_load_n(&pSegment->entries[index].osThread, __ATOMIC_ACQUIRE) == osThread)
            {
                return pSegment->entries[index].pThread;
            }
        }
        pSegment = __atomic_load_n(&pSegment->pNext, __ATOMIC_ACQUIRE);
    }
    return NULL;
}
