// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_THREAD_H
#define HAVE_MINIPAL_THREAD_H

#include <stdbool.h>
#include <stdint.h>

#ifndef HOST_WINDOWS
#include <stddef.h>
#include <pthread.h>
#include <minipal/utils.h>
#endif // !HOST_WINDOWS

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Yield to another thread, periodically sleeping after repeated yields.
 * A nonzero sleepMilliseconds sleeps explicitly instead of yielding.
 *
 * @return Whether the OS yield succeeded, or true for an explicit sleep.
 */
bool minipal_switch_to_thread(uint32_t sleepMilliseconds, uint32_t switchCount);

#ifndef HOST_WINDOWS

/**
 * Get the current thread ID without caching in a TLS variable.
 *
 * @return The current thread ID as a size_t value.
 */
size_t minipal_get_current_thread_id_no_cache(void);

#if !defined(__wasm) || defined(_REENTRANT)
extern PLATFORM_THREAD_LOCAL size_t minipal_cached_thread_id;
#endif

/**
 * Get the current thread ID.
 *
 * @return The current thread ID as a size_t value.
 */
inline size_t minipal_get_current_thread_id(void)
{
#if defined(__wasm) && !defined(_REENTRANT)
    return minipal_get_current_thread_id_no_cache();

#else // !__wasm || _REENTRANT
    if (!minipal_cached_thread_id)
    {
        minipal_cached_thread_id = minipal_get_current_thread_id_no_cache();
    }

    return minipal_cached_thread_id;
#endif // __wasm && !_REENTRANT
}

/**
 * Set the name of the specified thread.
 *
 * @param thread The thread for which to set the name.
 * @param name The desired name for the thread.
 * @return 0 on success, or an error code if the operation fails.
 */
int minipal_set_thread_name(pthread_t thread, const char* name);

#endif // !HOST_WINDOWS

#ifdef __cplusplus
}
#endif // extern "C"

#endif // HAVE_MINIPAL_THREAD_H
