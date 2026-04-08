// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// WASI pthread.h wrapper — adds missing declarations that WASI hides.

#ifndef _WASI_PTHREAD_WRAPPER_H
#define _WASI_PTHREAD_WRAPPER_H

#include_next <pthread.h>
#include <sched.h>

#ifdef __wasi__

// These are declared in upstream musl but hidden on WASI behind
// __wasilibc_unmodified_upstream. Provide declarations here;
// link-time stubs are in pal/src/arch/wasm/stubs.cpp.
#ifdef __cplusplus
extern "C" {
#endif

#ifndef _WASI_PTHREAD_EXIT_DECLARED
#define _WASI_PTHREAD_EXIT_DECLARED
_Noreturn void pthread_exit(void *);
int pthread_setschedparam(pthread_t, int, const struct sched_param *);
int pthread_getschedparam(pthread_t, int *__restrict, struct sched_param *__restrict);
#endif

#ifdef __cplusplus
}
#endif

#ifndef SCHED_OTHER
#define SCHED_OTHER 0
#endif

#endif // __wasi__
#endif // _WASI_PTHREAD_WRAPPER_H
