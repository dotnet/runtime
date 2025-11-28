// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define _GNU_SOURCE

#include <stdbool.h>
#include "utils.h"
#include "thread.h"

#ifdef TARGET_UNIX

// Async safe lock free thread map for use in signal handlers

struct ThreadEntry
{
    size_t osThread;
    void* pThread;
};

#define MAX_THREADS_IN_SEGMENT 256

struct ThreadSegment
{
    struct ThreadEntry entries[MAX_THREADS_IN_SEGMENT];
    struct ThreadSegment* pNext;
};

static struct ThreadSegment *s_pAsyncSafeThreadMapHead = NULL;

bool minipal_insert_thread_into_async_safe_map(size_t osThread, void* pThread)
{
    size_t startIndex = osThread % MAX_THREADS_IN_SEGMENT;

    struct ThreadSegment* pSegment = s_pAsyncSafeThreadMapHead;
    struct ThreadSegment** ppSegment = &s_pAsyncSafeThreadMapHead;
    while (true)
    {
        if (pSegment == NULL)
        {
            // Need to add a new segment
            struct ThreadSegment* pNewSegment = (struct ThreadSegment*)malloc(sizeof(struct ThreadSegment));
            if (pNewSegment == NULL)
            {
                // Memory allocation failed
                return false;
            }

            memset(pNewSegment, 0, sizeof(struct ThreadSegment));
            struct ThreadSegment* pExpected = NULL;           
            if (!__atomic_compare_exchange_n(
                ppSegment,
                &pExpected,
                pNewSegment,
                false /* weak */,
                __ATOMIC_RELEASE  /* success_memorder */,
                __ATOMIC_RELAXED /* failure_memorder */))
            {
                // Another thread added the segment first
                free(pNewSegment);
                pNewSegment = *ppSegment;
            }

            pSegment = pNewSegment;
        }
        for (size_t i = 0; i < MAX_THREADS_IN_SEGMENT; i++)
        {
            size_t index = (startIndex + i) % MAX_THREADS_IN_SEGMENT;

            size_t expected = 0;
            if (__atomic_compare_exchange_n(
                    &pSegment->entries[index].osThread,
                    &expected, osThread,
                    false /* weak */,
                    __ATOMIC_RELEASE  /* success_memorder */,
                    __ATOMIC_RELAXED /* failure_memorder */))
            {
                // Successfully inserted
                pSegment->entries[index].pThread = pThread;
                return true;
            }
        }

        ppSegment = &pSegment->pNext;
        pSegment = pSegment->pNext;
    }
}

void minipal_remove_thread_from_async_safe_map(size_t osThread, void* pThread)
{
    size_t startIndex = osThread % MAX_THREADS_IN_SEGMENT;

    struct ThreadSegment* pSegment = s_pAsyncSafeThreadMapHead;
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
        pSegment = pSegment->pNext;
    }
}

void *minipal_find_thread_in_async_safe_map(size_t osThread)
{
    size_t startIndex = osThread % MAX_THREADS_IN_SEGMENT;
    struct ThreadSegment* pSegment = s_pAsyncSafeThreadMapHead;
    while (pSegment)
    {
        for (size_t i = 0; i < MAX_THREADS_IN_SEGMENT; i++)
        {
            size_t index = (startIndex + i) % MAX_THREADS_IN_SEGMENT;
            // Use acquire to synchronize with release in insert_thread_to_async_safe_map
            if (__atomic_load_n(&pSegment->entries[index].osThread, __ATOMIC_ACQUIRE) == osThread)
            {
                return pSegment->entries[index].pThread;
            }
        }
        pSegment = pSegment->pNext;
    }
    return NULL;
}

#endif // TARGET_UNIX
