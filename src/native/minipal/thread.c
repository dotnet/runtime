// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(__linux__) && !defined(_GNU_SOURCE)
// glibc declares pthread_setname_np only when _GNU_SOURCE is defined before <pthread.h>.
#define _GNU_SOURCE
#endif

#include "thread.h"

#if !defined(__wasm) || defined(_REENTRANT)
PLATFORM_THREAD_LOCAL size_t minipal_cached_thread_id;
#endif

extern size_t minipal_get_current_thread_id_no_cache(void);
extern size_t minipal_get_current_thread_id(void);
extern int minipal_set_thread_name(pthread_t thread, const char* name);
