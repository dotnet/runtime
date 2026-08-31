// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_MUTEX_H
#define HAVE_MINIPAL_MUTEX_H

#include <stdbool.h>

#ifdef HOST_WINDOWS
#include <windows.h>
typedef CRITICAL_SECTION MINIPAL_MUTEX_IMPL;
typedef SRWLOCK MINIPAL_NONRECURSIVE_MUTEX_IMPL;
#else // !HOST_WINDOWS
#include <pthread.h>
typedef pthread_mutex_t MINIPAL_MUTEX_IMPL;
typedef pthread_mutex_t MINIPAL_NONRECURSIVE_MUTEX_IMPL;
#endif // HOST_WINDOWS

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef struct _minipal_mutex
{
    MINIPAL_MUTEX_IMPL _impl;
} minipal_mutex;

typedef struct _minipal_nonrecursive_mutex
{
    MINIPAL_NONRECURSIVE_MUTEX_IMPL _impl;
} minipal_nonrecursive_mutex;

// Initialize the mutex.
bool minipal_mutex_init(minipal_mutex* mt);

// Initialize a non-recursive mutex.
bool minipal_nonrecursive_mutex_init(minipal_nonrecursive_mutex* mt);

// Destroy the mutex.
void minipal_mutex_destroy(minipal_mutex* mt);

// Destroy the non-recursive mutex.
void minipal_nonrecursive_mutex_destroy(minipal_nonrecursive_mutex* mt);

// Enter the mutex. Blocks until the mutex can be entered.
// Recursive enters are allowed.
void minipal_mutex_enter(minipal_mutex* mt);

// Enter the non-recursive mutex. Blocks until the mutex can be entered.
void minipal_nonrecursive_mutex_enter(minipal_nonrecursive_mutex* mt);

// Leave the mutex.
void minipal_mutex_leave(minipal_mutex* mt);

// Leave the non-recursive mutex.
void minipal_nonrecursive_mutex_leave(minipal_nonrecursive_mutex* mt);

#ifdef __cplusplus
}
#endif // __cplusplus

#ifdef __cplusplus
namespace minipal
{
    class MutexHolder final
    {
        minipal_mutex& _mtx;

    public:
        explicit MutexHolder(minipal_mutex& mtx)
            : _mtx{ mtx }
        {
            minipal_mutex_enter(&_mtx);
        }

        ~MutexHolder() noexcept
        {
            minipal_mutex_leave(&_mtx);
        }

        MutexHolder(MutexHolder const&) = delete;
        MutexHolder& operator=(MutexHolder const&) = delete;

        MutexHolder(MutexHolder&&) = delete;
        MutexHolder& operator=(MutexHolder&&) = delete;
    };
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_MUTEX_H
