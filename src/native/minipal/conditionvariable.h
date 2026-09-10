// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_CONDITION_VARIABLE_H
#define HAVE_MINIPAL_CONDITION_VARIABLE_H

#include <stdbool.h>
#include <stdint.h>

#include "mutex.h"

#ifdef HOST_WINDOWS
#include <windows.h>
typedef CONDITION_VARIABLE MINIPAL_CONDITION_VARIABLE_IMPL;
#else
#include <pthread.h>
typedef pthread_cond_t MINIPAL_CONDITION_VARIABLE_IMPL;
#endif // HOST_WINDOWS

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef struct _minipal_condition_variable
{
    MINIPAL_CONDITION_VARIABLE_IMPL _impl;
} minipal_condition_variable;

typedef enum _minipal_condition_variable_result
{
    MINIPAL_CONDITION_VARIABLE_SIGNALED,
    MINIPAL_CONDITION_VARIABLE_TIMED_OUT,
    MINIPAL_CONDITION_VARIABLE_FAILED,
} minipal_condition_variable_result;

#define MINIPAL_CONDITION_VARIABLE_INFINITE UINT32_MAX

// Initialize the condition variable.
bool minipal_condition_variable_init(minipal_condition_variable* condition);

// Destroy the condition variable. No threads may be waiting.
void minipal_condition_variable_destroy(minipal_condition_variable* condition);

// Wake all threads waiting on the condition variable.
bool minipal_condition_variable_broadcast(minipal_condition_variable* condition);

// Wake one thread waiting on the condition variable.
bool minipal_condition_variable_signal(minipal_condition_variable* condition);

// Atomically release the entered mutex and wait, then reacquire it before returning.
// The calling thread must have entered the mutex exactly once.
// The caller must recheck its predicate after every signaled result because wakes may be spurious.
minipal_condition_variable_result minipal_condition_variable_wait(
    minipal_condition_variable* condition,
    minipal_mutex* mutex,
    uint32_t timeoutMilliseconds);

// Atomically release the entered non-recursive mutex and wait, then reacquire it before returning.
// The caller must recheck its predicate after every signaled result because wakes may be spurious.
minipal_condition_variable_result minipal_condition_variable_wait_nonrecursive(
    minipal_condition_variable* condition,
    minipal_nonrecursive_mutex* mutex,
    uint32_t timeoutMilliseconds);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_CONDITION_VARIABLE_H
