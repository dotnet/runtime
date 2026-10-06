// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(__linux__) && !defined(_GNU_SOURCE)
// glibc declares pthread_setname_np only when _GNU_SOURCE is defined before <pthread.h>.
#define _GNU_SOURCE
#elif defined(__APPLE__) && !defined(_DARWIN_C_SOURCE)
// pthread_threadid_np and pthread_setname_np are hidden by _XOPEN_SOURCE unless
// _DARWIN_C_SOURCE is defined before <pthread.h>.
#define _DARWIN_C_SOURCE
#endif

#include "thread.h"

#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#if defined(__linux__)
#include <sys/syscall.h>
#include <unistd.h>
#elif defined(__FreeBSD__)
#include <pthread_np.h>
#elif defined(__OpenBSD__)
#include <pthread_np.h>
#include <unistd.h>
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

#if !defined(__wasm) || defined(_REENTRANT)
PLATFORM_THREAD_LOCAL size_t minipal_cached_thread_id;
#endif

size_t minipal_get_current_thread_id_no_cache(void)
{
    size_t tid;
#if defined(__wasm) && !defined(_REENTRANT)
    tid = 1; // In non-reentrant WASM builds, we define a single thread with ID 1.
#else // !__wasm || _REENTRANT

#if defined(__linux__)
    tid = (size_t)syscall(SYS_gettid);
#elif defined(__APPLE__)
    uint64_t thread_id;
    pthread_threadid_np(pthread_self(), &thread_id);
    tid = (size_t)thread_id;  // Cast the uint64_t thread ID to size_t
#elif defined(__FreeBSD__)
    tid = (size_t)pthread_getthreadid_np();
#elif defined(__NetBSD__)
    tid = (size_t)_lwp_self();
#elif defined(__OpenBSD__)
    tid = (size_t)getthrid();
#elif defined(__HAIKU__)
    tid = (size_t)find_thread(NULL);
#elif defined(__sun)
    tid = (size_t)pthread_self();
#elif defined(__wasm)
    tid = (size_t)(void*)pthread_self();
#else
#error "Unsupported platform"
#endif

#endif // __wasm && !_REENTRANT
    return tid;
}

extern size_t minipal_get_current_thread_id(void);

int minipal_set_thread_name(pthread_t thread, const char* name)
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
#elif defined(__OpenBSD__)
    pthread_set_name_np(thread, threadName);
    return 0;
#elif defined(__HAIKU__)
    return rename_thread(get_pthread_thread_id(thread), threadName);
#else
    return pthread_setname_np(thread, threadName);
#endif
#endif
}
