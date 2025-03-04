// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_THREAD_H
#define HAVE_MINIPAL_THREAD_H

#include <pthread.h>
#include <stdint.h>
#include <string.h>
#include <stdlib.h>

#if defined(__linux__)
#include <unistd.h>
#include <sys/syscall.h>
#elif defined(__FreeBSD__)
#include <pthread_np.h>
#elif defined(__NetBSD__)
#include <lwp.h>
#elif defined(__HAIKU__)
#include <kernel/OS.h>
#endif

#ifdef PTHREAD_MAX_NAMELEN_NP
#define MINIPAL_MAX_THREAD_NAME_LENGTH (PTHREAD_MAX_NAMELEN_NP - 1)
#elif defined(__APPLE__)
#define MINIPAL_MAX_THREAD_NAME_LENGTH 63
#elif defined(__FreeBSD__)
#define MINIPAL_MAX_THREAD_NAME_LENGTH MAXCOMLEN
#elif defined(__HAIKU__)
#define MINIPAL_MAX_THREAD_NAME_LENGTH (B_OS_NAME_LENGTH - 1)
#else
#define MINIPAL_MAX_THREAD_NAME_LENGTH 15
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Get the current thread ID.
 *
 * @return The current thread ID as a size_t value.
 */
static inline size_t minipal_get_current_thread_id(void)
{
#if defined(__wasm) && defined(MONO_WASM_MT)
    return 0;
#else
#if defined(__GNUC__) && !defined(__clang__) && defined(__cplusplus)
    // gcc doesn't like _Thread_local when __cplusplus is defined.
    // although thread_local is C2x, which other compilers don't allow with C11.
    static thread_local size_t tid = 0;
#else
    static _Thread_local size_t tid = 0;
#endif

    if (!tid)
#if defined(__linux__)
        tid = (size_t)syscall(SYS_gettid);
#elif defined(__APPLE__)
    {
        uint64_t thread_id;
        pthread_threadid_np(pthread_self(), &thread_id);
        tid = (size_t)thread_id;  // Cast the uint64_t thread ID to size_t
    }
#elif defined(__FreeBSD__)
        tid = (size_t)pthread_getthreadid_np();
#elif defined(__NetBSD__)
        tid = (size_t)_lwp_self();
#elif defined(__HAIKU__)
        tid = (size_t)find_thread(NULL);
#else
        tid = (size_t)(void*)pthread_self();
#endif

    return tid;
#endif
}

/**
 * Set the name of the specified thread.
 *
 * @param thread The thread for which to set the name.
 * @param name The desired name for the thread.
 * @return 0 on success, or an error code if the operation fails.
 */
static inline int minipal_set_thread_name(pthread_t thread, const char* name)
{
#ifdef __wasm
    // WASM does not support pthread_setname_np yet: https://github.com/emscripten-core/emscripten/pull/18751
    return 0;
#else
    const char* threadName = name;
    char truncatedName[MINIPAL_MAX_THREAD_NAME_LENGTH + 1];

    if (strlen(name) > MINIPAL_MAX_THREAD_NAME_LENGTH)
    {
        strncpy(truncatedName, name, MINIPAL_MAX_THREAD_NAME_LENGTH);
        truncatedName[MINIPAL_MAX_THREAD_NAME_LENGTH] = '\0';
        threadName = truncatedName;
    }

#if defined(__APPLE__)
    // On Apple OSes, pthread_setname_np only works for the calling thread.
    if (thread != pthread_self()) return 0;

    return pthread_setname_np(threadName);
#elif defined(__HAIKU__)
    return rename_thread(get_pthread_thread_id(thread), threadName);
#else
    return pthread_setname_np(thread, threadName);
#endif
#endif
}

#ifdef __cplusplus
}
#endif // extern "C"

#endif // HAVE_MINIPAL_THREAD_H
