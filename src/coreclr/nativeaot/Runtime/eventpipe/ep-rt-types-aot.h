// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Implementation of ep-rt-types.h targeting AOT runtime.
#ifndef __EVENTPIPE_RT_TYPES_AOT_H__
#define __EVENTPIPE_RT_TYPES_AOT_H__

#include <eventpipe/ep-rt-config.h>

#include <inttypes.h>
#include "rhassert.h"

#ifdef ENABLE_PERFTRACING

#ifdef TARGET_UNIX
#define __stdcall
#endif

#ifdef DEBUG
#define EP_CHECKED_BUILD
#endif

#undef EP_ASSERT
#ifdef EP_CHECKED_BUILD
#define EP_ASSERT(expr) _ASSERTE(expr)
#else
#define EP_ASSERT(expr)
#endif

#undef EP_UNREACHABLE
#define EP_UNREACHABLE(msg) do { UNREACHABLE_MSG(msg); } while (0)

#undef EP_LIKELY
#define EP_LIKELY(expr) expr

#undef EP_UNLIKELY
#define EP_UNLIKELY(expr) expr

class CLREventStatic;
struct _rt_aot_event_internal_t {
    CLREventStatic *event;
};

class CrstStatic;
struct _rt_aot_lock_internal_t {
    CrstStatic *lock;
};

/*
 * EventPipe.
 */

#undef ep_rt_method_desc_t
typedef class MethodDesc ep_rt_method_desc_t;

/*
 * PAL.
 */

#undef ep_rt_file_handle_t
typedef void * ep_rt_file_handle_t;

#undef ep_rt_wait_event_handle_t
typedef struct _rt_aot_event_internal_t ep_rt_wait_event_handle_t;

#undef ep_rt_lock_handle_t
typedef struct _rt_aot_lock_internal_t ep_rt_lock_handle_t;

#undef ep_rt_spin_lock_handle_t
// NativeAOT will start with CrstStatic instead of a SpinLock and change as needed if performance is an issue
typedef struct _rt_aot_lock_internal_t ep_rt_spin_lock_handle_t;

/*
 * Thread.
 */

#undef ep_rt_thread_handle_t
typedef class Thread * ep_rt_thread_handle_t;

#undef ep_rt_thread_activity_id_handle_t
typedef class Thread * ep_rt_thread_activity_id_handle_t;

#undef ep_rt_thread_id_t
// #ifndef TARGET_UNIX
// typedef DWORD ep_rt_thread_id_t;
// #else
typedef size_t ep_rt_thread_id_t;
//#endif

#undef ep_rt_thread_start_func
typedef size_t (__stdcall *ep_rt_thread_start_func)(void *lpThreadParameter);

#undef ep_rt_thread_start_func_return_t
typedef size_t ep_rt_thread_start_func_return_t;

#undef ep_rt_thread_params_t
typedef struct _rt_aot_thread_params_t {
    ep_rt_thread_handle_t thread;
    EventPipeThreadType thread_type;
    ep_rt_thread_start_func thread_func;
    void *thread_params;
} ep_rt_thread_params_t;

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_TYPES_AOT_H__ */
