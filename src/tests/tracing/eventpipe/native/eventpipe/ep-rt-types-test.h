// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef EP_RT_TYPES_TEST_H
#define EP_RT_TYPES_TEST_H

#include <assert.h>
#include <stdio.h>
#include <minipal/mutex.h>

#undef EP_ASSERT
#define EP_ASSERT(expr) assert(expr)
#undef EP_UNREACHABLE
#define EP_UNREACHABLE(msg) do { fputs(msg "\n", stderr); abort(); } while (0)
#undef EP_LIKELY
#define EP_LIKELY(expr) (expr)
#undef EP_UNLIKELY
#define EP_UNLIKELY(expr) (expr)

typedef struct
{
    minipal_mutex mutex;
    uint64_t owner;
    bool initialized;
} ep_rt_test_lock_t;

typedef struct ep_rt_test_wait_event ep_rt_test_wait_event_t;
typedef struct
{
    ep_rt_test_wait_event_t *event;
} ep_rt_test_wait_handle_t;

typedef struct
{
    EventPipeThreadHolder *holder;
    uint64_t id;
} ep_rt_test_thread_t;

#undef ep_rt_method_desc_t
typedef void ep_rt_method_desc_t;
#undef ep_rt_file_handle_t
typedef FILE *ep_rt_file_handle_t;
#undef ep_rt_wait_event_handle_t
typedef ep_rt_test_wait_handle_t ep_rt_wait_event_handle_t;
#undef ep_rt_lock_handle_t
typedef ep_rt_test_lock_t ep_rt_lock_handle_t;
#undef ep_rt_spin_lock_handle_t
typedef ep_rt_test_lock_t ep_rt_spin_lock_handle_t;
#undef ep_rt_thread_handle_t
typedef ep_rt_test_thread_t *ep_rt_thread_handle_t;
#undef ep_rt_thread_activity_id_handle_t
typedef EventPipeThread *ep_rt_thread_activity_id_handle_t;
#undef ep_rt_thread_id_t
typedef uint64_t ep_rt_thread_id_t;
#undef ep_rt_thread_start_func_return_t
typedef uintptr_t ep_rt_thread_start_func_return_t;
#undef ep_rt_thread_start_func
typedef ep_rt_thread_start_func_return_t (*ep_rt_thread_start_func)(void *);
#undef ep_rt_thread_params_t
typedef struct
{
    ep_rt_thread_handle_t thread;
    EventPipeThreadType thread_type;
    ep_rt_thread_start_func thread_func;
    void *thread_params;
} ep_rt_thread_params_t;

#endif
