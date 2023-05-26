// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Implementation of ep-rt.h targeting NativeAOT runtime.
#ifndef __EVENTPIPE_RT_AOT_H__
#define __EVENTPIPE_RT_AOT_H__

#include <ctype.h>  // For isspace
#ifdef TARGET_UNIX
#include <sys/time.h>
#include <pthread.h>
#endif

#include <eventpipe/ep-rt-config.h>
#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-thread.h>
#include <eventpipe/ep-types.h>
#include <eventpipe/ep-provider.h>
#include <eventpipe/ep-session-provider.h>

#include "rhassert.h"
#include <RhConfig.h>

#ifdef TARGET_UNIX
#define sprintf_s snprintf
#define _stricmp strcasecmp
#define INFINITE            0xFFFFFFFF  // Infinite timeout
#endif

#define STATIC_CONTRACT_NOTHROW

#undef EP_INFINITE_WAIT
#define EP_INFINITE_WAIT INFINITE

#undef EP_GCX_PREEMP_ENTER
#define EP_GCX_PREEMP_ENTER { //GCX_PREEMP();

#undef EP_GCX_PREEMP_EXIT
#define EP_GCX_PREEMP_EXIT }

#undef EP_ALWAYS_INLINE
#define EP_ALWAYS_INLINE

#undef EP_NEVER_INLINE
#define EP_NEVER_INLINE

#undef EP_ALIGN_UP
#define EP_ALIGN_UP(val,align) _rt_aot_align_up(val,align)

#ifdef TARGET_UNIX
extern pthread_key_t eventpipe_tls_key;
extern __thread EventPipeThreadHolder* eventpipe_tls_instance;
#endif

// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// TODO: The NativeAOT ALIGN_UP is defined in a tangled manner that generates linker errors if
// it is used here; instead, define a version tailored to the existing usage in the shared
// EventPipe code.
static inline uint8_t* _rt_aot_align_up(uint8_t* val, uintptr_t alignment)
{
    // alignment must be a power of 2 for this implementation to work (need modulo otherwise)
    EP_ASSERT( 0 == (alignment & (alignment - 1)) );
    uintptr_t rawVal = reinterpret_cast<uintptr_t>(val);
    uintptr_t result = (rawVal + (alignment - 1)) & ~(alignment - 1);
    EP_ASSERT( result >= rawVal );      // check for overflow
    return reinterpret_cast<uint8_t*>(result);
}

static
inline
ep_rt_lock_handle_t *
ep_rt_aot_config_lock_get (void)
{
    extern ep_rt_lock_handle_t _ep_rt_aot_config_lock_handle;
    return &_ep_rt_aot_config_lock_handle;
}

static
inline
const ep_char8_t *
ep_rt_entrypoint_assembly_name_get_utf8 (void)
{ 
    STATIC_CONTRACT_NOTHROW;

    extern const ep_char8_t * ep_rt_aot_entrypoint_assembly_name_get_utf8 (void);
    return ep_rt_aot_entrypoint_assembly_name_get_utf8();
}

static
const ep_char8_t *
ep_rt_runtime_version_get_utf8 (void) { 
    STATIC_CONTRACT_NOTHROW;

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Find a way to use CoreCLR runtime_version.h here if a more exact version is needed
    return reinterpret_cast<const ep_char8_t*>("8.0.0");
}

/*
 * Little-Endian Conversion.
 */

static
inline
uint16_t
ep_rt_val_uint16_t (uint16_t value)
{
    return value;
}

static
inline
uint32_t
ep_rt_val_uint32_t (uint32_t value)
{
    return value;
}

static
inline
uint64_t
ep_rt_val_uint64_t (uint64_t value)
{
    return value;
}

static
inline
int16_t
ep_rt_val_int16_t (int16_t value)
{
    return value;
}

static
inline
int32_t
ep_rt_val_int32_t (int32_t value)
{
    return value;
}

static
inline
int64_t
ep_rt_val_int64_t (int64_t value)
{
    return value;
}

static
inline
uintptr_t
ep_rt_val_uintptr_t (uintptr_t value)
{
    return value;
}

/*
* Atomics.
*/

static
inline
uint32_t
ep_rt_atomic_inc_uint32_t (volatile uint32_t *value)
{
    STATIC_CONTRACT_NOTHROW;
    extern uint32_t ep_rt_aot_atomic_inc_uint32_t (volatile uint32_t *value);
    return ep_rt_aot_atomic_inc_uint32_t (value);
}

static
inline
uint32_t
ep_rt_atomic_dec_uint32_t (volatile uint32_t *value)
{
    STATIC_CONTRACT_NOTHROW;
    extern uint32_t ep_rt_aot_atomic_dec_uint32_t (volatile uint32_t *value);
    return ep_rt_aot_atomic_dec_uint32_t (value);
}

static
inline
int32_t
ep_rt_atomic_inc_int32_t (volatile int32_t *value)
{
    STATIC_CONTRACT_NOTHROW;
    extern int32_t ep_rt_aot_atomic_inc_int32_t (volatile int32_t *value);

    return ep_rt_aot_atomic_inc_int32_t (value);
}

static
inline
int32_t
ep_rt_atomic_dec_int32_t (volatile int32_t *value)
{
    STATIC_CONTRACT_NOTHROW;
    extern int32_t ep_rt_aot_atomic_dec_int32_t (volatile int32_t *value);
    return ep_rt_aot_atomic_dec_int32_t (value);
}

static
inline
int64_t
ep_rt_atomic_inc_int64_t (volatile int64_t *value)
{
    STATIC_CONTRACT_NOTHROW;

    extern int64_t ep_rt_aot_atomic_inc_int64_t (volatile int64_t *value);
    return ep_rt_aot_atomic_inc_int64_t (value);
}

static
inline
int64_t
ep_rt_atomic_dec_int64_t (volatile int64_t *value) 
{ 
    STATIC_CONTRACT_NOTHROW;

    extern int64_t ep_rt_aot_atomic_dec_int64_t (volatile int64_t *value);
    return ep_rt_aot_atomic_dec_int64_t (value);
}

static
inline
size_t
ep_rt_atomic_compare_exchange_size_t (volatile size_t *target, size_t expected, size_t value) 
{
    STATIC_CONTRACT_NOTHROW;
    extern size_t ep_rt_aot_atomic_compare_exchange_size_t (volatile size_t *target, size_t expected, size_t value);
    return ep_rt_aot_atomic_compare_exchange_size_t (target, expected, value);
}

static
inline
ep_char8_t *
ep_rt_atomic_compare_exchange_utf8_string (ep_char8_t *volatile *target, ep_char8_t *expected, ep_char8_t *value) 
{ 
    STATIC_CONTRACT_NOTHROW;
    extern ep_char8_t * ep_rt_aot_atomic_compare_exchange_utf8_string (ep_char8_t *volatile *target, ep_char8_t *expected, ep_char8_t *value);
    return ep_rt_aot_atomic_compare_exchange_utf8_string (target, expected, value);
}

static
void
ep_rt_init (void) 
{
    extern void ep_rt_aot_init (void);
    ep_rt_aot_init();
}

static
inline
void
ep_rt_init_finish (void)
{
    STATIC_CONTRACT_NOTHROW;
}

static
inline
void
ep_rt_shutdown (void)
{
    STATIC_CONTRACT_NOTHROW;
}

static
inline
bool
ep_rt_config_acquire (void)
{
    return ep_rt_lock_acquire (ep_rt_aot_config_lock_get ());
}

static
inline
bool
ep_rt_config_release (void)
{
    return ep_rt_lock_release (ep_rt_aot_config_lock_get ());
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_config_requires_lock_held (void)
{
    ep_rt_lock_requires_lock_held (ep_rt_aot_config_lock_get ());
}

static
inline
void
ep_rt_config_requires_lock_not_held (void)
{
    ep_rt_lock_requires_lock_not_held (ep_rt_aot_config_lock_get ());
}
#endif

static
inline
bool
ep_rt_walk_managed_stack_for_thread (
    ep_rt_thread_handle_t thread,
    EventPipeStackContents *stack_contents)
{
    STATIC_CONTRACT_NOTHROW;
    extern bool ep_rt_aot_walk_managed_stack_for_thread (ep_rt_thread_handle_t thread, EventPipeStackContents *stack_contents);
    return ep_rt_aot_walk_managed_stack_for_thread (thread, stack_contents);
}

static
inline
bool
ep_rt_method_get_simple_assembly_name (
    ep_rt_method_desc_t *method,
    ep_char8_t *name,
    size_t name_len)
{
    STATIC_CONTRACT_NOTHROW;

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Design MethodDesc and method name services if/when needed
    //PalDebugBreak();

    return false;

}

static
bool
ep_rt_method_get_full_name (
    ep_rt_method_desc_t *method,
    ep_char8_t *name,
    size_t name_len)
{
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Design MethodDesc and method name services if/when needed
    //PalDebugBreak();

    return false;
}

static
inline
void
ep_rt_provider_config_init (EventPipeProviderConfiguration *provider_config)
{
    STATIC_CONTRACT_NOTHROW;
}

// This function is auto-generated from /src/scripts/genEventPipe.py
#ifdef TARGET_UNIX
extern "C" void InitProvidersAndEvents ();
#else
extern void InitProvidersAndEvents ();
#endif

static
void
ep_rt_init_providers_and_events (void)
{
    InitProvidersAndEvents ();
}

static
inline
bool
ep_rt_providers_validate_all_disabled (void)
{
    STATIC_CONTRACT_NOTHROW;
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context and MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context are not available in NativeAOT
    return true;
}

static
inline
void
ep_rt_prepare_provider_invoke_callback (EventPipeProviderCallbackData *provider_callback_data)
{
    STATIC_CONTRACT_NOTHROW;
}

static
void
ep_rt_provider_invoke_callback (
    EventPipeCallback callback_func,
    const uint8_t *source_id,
    unsigned long is_enabled,
    uint8_t level,
    uint64_t match_any_keywords,
    uint64_t match_all_keywords,
    EventFilterDescriptor *filter_data,
    void *callback_data)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (callback_func != NULL);

    (*callback_func)(
        source_id,
        is_enabled,
        level,
        match_any_keywords,
        match_all_keywords,
        filter_data,
        callback_data);
}

static
inline
bool
ep_rt_config_value_get_enable (void)
{
    // See https://learn.microsoft.com/dotnet/core/diagnostics/eventpipe#trace-using-environment-variables
    bool value;
    if (RhConfig::Environment::TryGetBooleanValue("EnableEventPipe", &value))
        return value;

    return false;
}

static
inline
ep_char8_t *
ep_rt_config_value_get_config (void)
{
    STATIC_CONTRACT_NOTHROW;

    // See https://learn.microsoft.com/dotnet/core/diagnostics/eventpipe#trace-using-environment-variables
    char* value;
    if (RhConfig::Environment::TryGetStringValue("EventPipeConfig", &value))
        return (ep_char8_t*)value;

    return nullptr;
}

static
inline
ep_char8_t *
ep_rt_config_value_get_output_path (void)
{
    STATIC_CONTRACT_NOTHROW;

    // See https://learn.microsoft.com/dotnet/core/diagnostics/eventpipe#trace-using-environment-variables
    char* value;
    if (RhConfig::Environment::TryGetStringValue("EventPipeOutputPath", &value))
        return (ep_char8_t*)value;

    return nullptr;
}

static
inline
uint32_t
ep_rt_config_value_get_circular_mb (void)
{
    STATIC_CONTRACT_NOTHROW;

    // See https://learn.microsoft.com/dotnet/core/diagnostics/eventpipe#trace-using-environment-variables
    uint64_t value;
    if (RhConfig::Environment::TryGetIntegerValue("EventPipeCircularMB", &value))
    {
        EP_ASSERT(value <= UINT32_MAX);
        return static_cast<uint32_t>(value);
    }

    return 0;
}

static
inline
bool
ep_rt_config_value_get_output_streaming (void)
{
    STATIC_CONTRACT_NOTHROW;

    bool value;
    if (RhConfig::Environment::TryGetBooleanValue("EventPipeOutputStreaming", &value))
        return value;

    return false;
}

static
inline
bool
ep_rt_config_value_get_enable_stackwalk (void)
{
    STATIC_CONTRACT_NOTHROW;

    bool value;
    if (RhConfig::Environment::TryGetBooleanValue("EventPipeEnableStackwalk", &value))
        return value;

    return true;
}

/*
 * EventPipeSampleProfiler.
 */

static
inline
void
ep_rt_sample_profiler_write_sampling_event_for_threads (
    ep_rt_thread_handle_t sampling_thread,
    EventPipeEvent *sampling_event)
{
    STATIC_CONTRACT_NOTHROW;

    extern void ep_rt_aot_sample_profiler_write_sampling_event_for_threads (ep_rt_thread_handle_t sampling_thread, EventPipeEvent *sampling_event);
    ep_rt_aot_sample_profiler_write_sampling_event_for_threads (sampling_thread, sampling_event);
}

static
inline
void
ep_rt_notify_profiler_provider_created (EventPipeProvider *provider)
{
    // Following mono's path of no-op
}

/*
 * Arrays.
 */

static
inline
uint8_t *
ep_rt_byte_array_alloc (size_t len)
{
    STATIC_CONTRACT_NOTHROW;
    return new (nothrow) uint8_t [len];
}

static
inline
void
ep_rt_byte_array_free (uint8_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;

    if (ptr)
        delete [] ptr;
}

/*
 * Event.
 */

static
void
ep_rt_wait_event_alloc (
    ep_rt_wait_event_handle_t *wait_event,
    bool manual,
    bool initial)
{
    STATIC_CONTRACT_NOTHROW;

    extern void ep_rt_aot_wait_event_alloc (
    ep_rt_wait_event_handle_t *wait_event,
    bool manual,
    bool initial);
    ep_rt_aot_wait_event_alloc(wait_event, manual, initial);
}

static
inline
void
ep_rt_wait_event_free (ep_rt_wait_event_handle_t *wait_event)
{
    STATIC_CONTRACT_NOTHROW;
    extern void ep_rt_aot_wait_event_free (ep_rt_wait_event_handle_t *wait_event);
    ep_rt_aot_wait_event_free(wait_event);
}

static
inline
bool
ep_rt_wait_event_set (ep_rt_wait_event_handle_t *wait_event) 
{ 
    STATIC_CONTRACT_NOTHROW;
    extern bool ep_rt_aot_wait_event_set (ep_rt_wait_event_handle_t *wait_event);
    return ep_rt_aot_wait_event_set (wait_event);
}

static
int32_t
ep_rt_wait_event_wait (
    ep_rt_wait_event_handle_t *wait_event,
    uint32_t timeout,
    bool alertable) 
{ 
    STATIC_CONTRACT_NOTHROW;
    extern int32_t
ep_rt_aot_wait_event_wait (
    ep_rt_wait_event_handle_t *wait_event,
    uint32_t timeout,
    bool alertable);

    return ep_rt_aot_wait_event_wait(wait_event, timeout, alertable);
}

static
inline
EventPipeWaitHandle
ep_rt_wait_event_get_wait_handle (ep_rt_wait_event_handle_t *wait_event) 
{ 
    STATIC_CONTRACT_NOTHROW;
    // EP_ASSERT (wait_event != NULL && wait_event->event != NULL);

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: NativeAOT CLREventStatic doesn't have GetHandleUNHOSTED
    // PalDebugBreak();
    return 0;
}

static
inline
bool
ep_rt_wait_event_is_valid (ep_rt_wait_event_handle_t *wait_event) 
{ 
    STATIC_CONTRACT_NOTHROW;
    extern bool
    ep_rt_aot_wait_event_is_valid (ep_rt_wait_event_handle_t *wait_event);

    return ep_rt_aot_wait_event_is_valid (wait_event);
}

/*
 * Misc.
 */

static
inline
int
ep_rt_get_last_error (void)
{
    STATIC_CONTRACT_NOTHROW;
    extern int
    ep_rt_aot_get_last_error (void);
    return ep_rt_aot_get_last_error ();
}

static
inline
bool
ep_rt_process_detach (void)
{
    STATIC_CONTRACT_NOTHROW;

    return false;
}

static
inline
bool
ep_rt_process_shutdown (void)
{
    STATIC_CONTRACT_NOTHROW;

    return false;
}

static
inline
void
ep_rt_create_activity_id (
    uint8_t *activity_id,
    uint32_t activity_id_len)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (activity_id != NULL);
    EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Implement a way to generate a real Guid
    // CoCreateGuid (reinterpret_cast<GUID *>(activity_id));

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Using roughly Mono's implementation but Mono randomly generates this, hardcoding for now
    uint8_t data1[] = {0x67,0xac,0x33,0xf1,0x8d,0xed,0x41,0x01,0xb4,0x26,0xc9,0xb7,0x94,0x35,0xf7,0x8a};
    memcpy (activity_id, data1, EP_ACTIVITY_ID_SIZE);

    const uint16_t version_mask = 0xF000;
    const uint16_t random_guid_version = 0x4000;
    const uint8_t clock_seq_hi_and_reserved_mask = 0xC0;
    const uint8_t clock_seq_hi_and_reserved_value = 0x80;

    // Modify bits indicating the type of the GUID
    uint8_t *activity_id_c = activity_id + sizeof (uint32_t) + sizeof (uint16_t);
    uint8_t *activity_id_d = activity_id + sizeof (uint32_t) + sizeof (uint16_t) + sizeof (uint16_t);

    uint16_t c;
    memcpy (&c, activity_id_c, sizeof (c));

    uint8_t d;
    memcpy (&d, activity_id_d, sizeof (d));

    // time_hi_and_version
    c = ((c & ~version_mask) | random_guid_version);
    // clock_seq_hi_and_reserved
    d = ((d & ~clock_seq_hi_and_reserved_mask) | clock_seq_hi_and_reserved_value);

    memcpy (activity_id_c, &c, sizeof (c));
    memcpy (activity_id_d, &d, sizeof (d));
}

static
inline
bool
ep_rt_is_running (void)
{
    STATIC_CONTRACT_NOTHROW;
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Does NativeAot have the concept of EEStarted
    // PalDebugBreak();

    return false;
}

static
inline
void
ep_rt_execute_rundown (dn_vector_ptr_t *execution_checkpoints)
{
    STATIC_CONTRACT_NOTHROW;

    //TODO: Write execution checkpoint rundown events.
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: EventPipe Configuration values - RhConfig?
    // (CLRConfig::INTERNAL_EventPipeCircularMB)
    // PalDebugBreak();
}

/*
 * Objects.
 */

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_alloc
#define ep_rt_object_alloc(obj_type) (new (nothrow) obj_type())

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_array_alloc
#define ep_rt_object_array_alloc(obj_type,size) (new (nothrow) obj_type [size]())

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_array_free
#define ep_rt_object_array_free(obj_ptr) do { if (obj_ptr) delete [] obj_ptr; } while(0)

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_free
#define ep_rt_object_free(obj_ptr) do { if (obj_ptr) delete obj_ptr; } while(0)

/*
 * PAL.
 */

#undef EP_RT_DEFINE_THREAD_FUNC
#define EP_RT_DEFINE_THREAD_FUNC(name) static ep_rt_thread_start_func_return_t __stdcall name (void *data)

EP_RT_DEFINE_THREAD_FUNC (ep_rt_thread_aot_start_session_or_sampling_thread)
{
    STATIC_CONTRACT_NOTHROW;

    ep_rt_thread_params_t* thread_params = reinterpret_cast<ep_rt_thread_params_t *>(data);

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Implement thread creation/management if needed.
    // The session and sampling threads both assert that the incoming thread handle is
    // non-null, but do not necessarily rely on it otherwise; just pass a meaningless non-null
    // value until testing shows that a meaningful value is needed.
    thread_params->thread = reinterpret_cast<ep_rt_thread_handle_t>(1);

    size_t result = thread_params->thread_func (thread_params);
    delete thread_params;
    return result;
}

static
bool
ep_rt_thread_create (
    void *thread_func,
    void *params,
    EventPipeThreadType thread_type,
    void *id)
{
    STATIC_CONTRACT_NOTHROW;
    extern bool
    ep_rt_aot_thread_create (
    void *thread_func,
    void *params,
    EventPipeThreadType thread_type,
    void *id);

    return ep_rt_aot_thread_create(thread_func, params, thread_type, id);
}

static
inline
void
ep_rt_set_server_name(void)
{
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Need to set name for  the thread
    // ::SetThreadName(GetCurrentThread(), W(".NET EventPipe"));
}


static
inline
void
ep_rt_thread_sleep (uint64_t ns)
{
    STATIC_CONTRACT_NOTHROW;
    extern void
    ep_rt_aot_thread_sleep (uint64_t ns);
    ep_rt_aot_thread_sleep(ns);
}

static
inline
uint32_t
ep_rt_current_process_get_id (void)
{
    STATIC_CONTRACT_NOTHROW;
    extern uint32_t
    ep_rt_aot_current_process_get_id (void);
    return ep_rt_aot_current_process_get_id();
}

static
inline
uint32_t
ep_rt_current_processor_get_number (void)
{
    STATIC_CONTRACT_NOTHROW;

#ifndef TARGET_UNIX
    extern uint32_t *_ep_rt_aot_proc_group_offsets;
    if (_ep_rt_aot_proc_group_offsets) {
        // PROCESSOR_NUMBER proc;
        // GetCurrentProcessorNumberEx (&proc);
        // return _ep_rt_aot_proc_group_offsets [proc.Group] + proc.Number;
        // PalDebugBreak();
    }
#endif
    return 0xFFFFFFFF;
}

static
inline
uint32_t
ep_rt_processors_get_count (void)
{
    STATIC_CONTRACT_NOTHROW;
#ifdef _INC_WINDOWS
    SYSTEM_INFO sys_info = {};
    GetSystemInfo (&sys_info);
    return static_cast<uint32_t>(sys_info.dwNumberOfProcessors);
#else    
    // PalDebugBreak();
    return 0xffff;
#endif
}

static
inline
ep_rt_thread_id_t
ep_rt_current_thread_get_id (void)
{
    STATIC_CONTRACT_NOTHROW;
    extern ep_rt_thread_id_t
    ep_rt_aot_current_thread_get_id (void);
    return ep_rt_aot_current_thread_get_id();
}

static
inline
int64_t
ep_rt_perf_counter_query (void)
{
    STATIC_CONTRACT_NOTHROW;
    extern int64_t
    ep_rt_aot_perf_counter_query (void);

    return ep_rt_aot_perf_counter_query();
}

static
inline
int64_t
ep_rt_perf_frequency_query (void)
{
    STATIC_CONTRACT_NOTHROW;
    extern int64_t
    ep_rt_aot_perf_frequency_query (void);

    return ep_rt_aot_perf_frequency_query();
}

static
inline
void
ep_rt_system_time_get (EventPipeSystemTime *system_time)
{
    STATIC_CONTRACT_NOTHROW;

#ifdef _INC_WINDOWS
    SYSTEMTIME value;
    GetSystemTime (&value);

    EP_ASSERT(system_time != NULL);
    ep_system_time_set (
        system_time,
        value.wYear,
        value.wMonth,
        value.wDayOfWeek,
        value.wDay,
        value.wHour,
        value.wMinute,
        value.wSecond,
        value.wMilliseconds);
#elif TARGET_UNIX
    time_t tt;
    struct tm *ut_ptr;
    struct timeval time_val;
    int timeofday_retval;

    EP_ASSERT (system_time != NULL);

    tt = time (NULL);

    timeofday_retval = gettimeofday (&time_val, NULL);

    ut_ptr = gmtime (&tt);

    uint16_t milliseconds = 0;
    if (timeofday_retval != -1) {
        int old_seconds;
        int new_seconds;

        milliseconds = (uint16_t)(time_val.tv_usec / 1000);

        old_seconds = ut_ptr->tm_sec;
        new_seconds = time_val.tv_sec % 60;

        /* just in case we reached the next second in the interval between time () and gettimeofday () */
        if (old_seconds != new_seconds)
            milliseconds = 999;
    }

    ep_system_time_set (
        system_time,
        (uint16_t)(1900 + ut_ptr->tm_year),
        (uint16_t)ut_ptr->tm_mon + 1,
        (uint16_t)ut_ptr->tm_wday,
        (uint16_t)ut_ptr->tm_mday,
        (uint16_t)ut_ptr->tm_hour,
        (uint16_t)ut_ptr->tm_min,
        (uint16_t)ut_ptr->tm_sec,
        milliseconds);
#endif

}

static
inline
int64_t
ep_rt_system_timestamp_get (void)
{
    STATIC_CONTRACT_NOTHROW;
    extern int64_t ep_rt_aot_system_timestamp_get (void);
    return ep_rt_aot_system_timestamp_get();
}

static
inline
int32_t
ep_rt_system_get_alloc_granularity (void)
{
    STATIC_CONTRACT_NOTHROW;
    // return static_cast<int32_t>(g_SystemInfo.dwAllocationGranularity);
    return 0x10000;
}

static
inline
const ep_char8_t *
ep_rt_os_command_line_get (void)
{
    STATIC_CONTRACT_NOTHROW;
    //EP_UNREACHABLE ("Can not reach here");

    return NULL;
}

static
ep_rt_file_handle_t
ep_rt_file_open_write (const ep_char8_t *path)
{
    STATIC_CONTRACT_NOTHROW;

    extern ep_rt_file_handle_t ep_rt_aot_file_open_write (const ep_char8_t *);
    return ep_rt_aot_file_open_write (path);
}

static
inline
bool
ep_rt_file_close (ep_rt_file_handle_t file_handle)
{
    STATIC_CONTRACT_NOTHROW;

    extern bool ep_rt_aot_file_close (ep_rt_file_handle_t);
    return ep_rt_aot_file_close (file_handle);
}

static
inline
bool
ep_rt_file_write (
    ep_rt_file_handle_t file_handle,
    const uint8_t *buffer,
    uint32_t bytes_to_write,
    uint32_t *bytes_written)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (buffer != NULL);

    extern bool ep_rt_aot_file_write (ep_rt_file_handle_t, const uint8_t*, uint32_t, uint32_t*);
    return ep_rt_aot_file_write (file_handle, buffer, bytes_to_write, bytes_written);
}

static
inline
uint8_t *
ep_rt_valloc0 (size_t buffer_size)
{
    STATIC_CONTRACT_NOTHROW;
    extern uint8_t *
    ep_rt_aot_valloc0 (size_t buffer_size);

    return ep_rt_aot_valloc0(buffer_size);
}

static
inline
void
ep_rt_vfree (
    uint8_t *buffer,
    size_t buffer_size)
{
    STATIC_CONTRACT_NOTHROW;
    extern void
    ep_rt_aot_vfree (
    uint8_t *buffer,
    size_t buffer_size);

    return ep_rt_aot_vfree(buffer, buffer_size);
}

static
inline
uint32_t
ep_rt_temp_path_get (
    ep_char8_t *buffer,
    uint32_t buffer_len)
{
    STATIC_CONTRACT_NOTHROW;

#ifdef TARGET_UNIX

    EP_ASSERT (buffer != NULL);
    EP_ASSERT (buffer_len > 0);

    const ep_char8_t *path = getenv ("TMPDIR");
    if (path == NULL){
        path = getenv ("TMP");
        if (path == NULL){
            path = getenv ("TEMP");
            if (path == NULL)
                path = "/tmp/";
        }
    }

    int32_t result = snprintf (buffer, buffer_len, path[strlen(path) - 1] == '/' ? "%s" : "%s/", path);
    if (result <= 0 || (uint32_t)result >= buffer_len)
        ep_raise_error ();


ep_on_exit:
    return result;

ep_on_error:
    result = 0;
    ep_exit_error_handler ();

#else
    return 0;
#endif
}

static
void
ep_rt_os_environment_get_utf16 (dn_vector_ptr_t *env_array)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (env_array != NULL);

    // PalDebugBreak();
}

/*
* Lock.
*/

static
bool
ep_rt_lock_acquire (ep_rt_lock_handle_t *lock)
{
    STATIC_CONTRACT_NOTHROW;
    extern bool ep_rt_aot_lock_acquire (ep_rt_lock_handle_t *lock);
    return ep_rt_aot_lock_acquire(lock);
}

static
bool
ep_rt_lock_release (ep_rt_lock_handle_t *lock)
{
    STATIC_CONTRACT_NOTHROW;
    extern bool ep_rt_aot_lock_release (ep_rt_lock_handle_t *lock);
    return ep_rt_aot_lock_release(lock);
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_lock_requires_lock_held (const ep_rt_lock_handle_t *lock)
{

    STATIC_CONTRACT_NOTHROW;
    extern void ep_rt_aot_lock_requires_lock_held (const ep_rt_lock_handle_t *lock);
    ep_rt_aot_lock_requires_lock_held(lock);
}

static
inline
void
ep_rt_lock_requires_lock_not_held (const ep_rt_lock_handle_t *lock)
{
    STATIC_CONTRACT_NOTHROW;
    extern void ep_rt_aot_lock_requires_lock_not_held (const ep_rt_lock_handle_t *lock);
    ep_rt_aot_lock_requires_lock_not_held(lock);
}
#endif

/*
* SpinLock.
*/

static
void
ep_rt_spin_lock_alloc (ep_rt_spin_lock_handle_t *spin_lock)
{
    STATIC_CONTRACT_NOTHROW;
    extern void ep_rt_aot_spin_lock_alloc (ep_rt_spin_lock_handle_t *spin_lock);
    ep_rt_aot_spin_lock_alloc(spin_lock);
}

static
inline
void
ep_rt_spin_lock_free (ep_rt_spin_lock_handle_t *spin_lock)
{
    STATIC_CONTRACT_NOTHROW;
    extern void ep_rt_aot_spin_lock_free (ep_rt_spin_lock_handle_t *spin_lock);
    ep_rt_aot_spin_lock_free(spin_lock);
}

static
inline
bool
ep_rt_spin_lock_acquire (ep_rt_spin_lock_handle_t *spin_lock)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (ep_rt_spin_lock_is_valid (spin_lock));
    extern bool ep_rt_aot_spin_lock_acquire (ep_rt_spin_lock_handle_t *spin_lock);
    return ep_rt_aot_spin_lock_acquire(spin_lock);
}

static
inline
bool
ep_rt_spin_lock_release (ep_rt_spin_lock_handle_t *spin_lock)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (ep_rt_spin_lock_is_valid (spin_lock));
    extern bool ep_rt_aot_spin_lock_release (ep_rt_spin_lock_handle_t *spin_lock);
    return ep_rt_aot_spin_lock_release(spin_lock);
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_spin_lock_requires_lock_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
    STATIC_CONTRACT_NOTHROW;
    extern void ep_rt_aot_spin_lock_requires_lock_held (const ep_rt_spin_lock_handle_t *spin_lock);
    ep_rt_aot_spin_lock_requires_lock_held(spin_lock);
}

static
inline
void
ep_rt_spin_lock_requires_lock_not_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
    STATIC_CONTRACT_NOTHROW;
    extern void ep_rt_aot_spin_lock_requires_lock_not_held (const ep_rt_spin_lock_handle_t *spin_lock);
    ep_rt_aot_spin_lock_requires_lock_not_held(spin_lock);
}
#endif

static
inline
bool
ep_rt_spin_lock_is_valid (const ep_rt_spin_lock_handle_t *spin_lock)
{
    STATIC_CONTRACT_NOTHROW;
    return (spin_lock != NULL && spin_lock->lock != NULL);
}

/*
 * String.
 */

static
inline
int
ep_rt_utf8_string_compare (
    const ep_char8_t *str1,
    const ep_char8_t *str2)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (str1 != NULL && str2 != NULL);

    return strcmp (reinterpret_cast<const char *>(str1), reinterpret_cast<const char *>(str2));
}

static
inline
int
ep_rt_utf8_string_compare_ignore_case (
    const ep_char8_t *str1,
    const ep_char8_t *str2)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (str1 != NULL && str2 != NULL);

    return _stricmp (reinterpret_cast<const char *>(str1), reinterpret_cast<const char *>(str2));
}

static
inline
bool
ep_rt_utf8_string_is_null_or_empty (const ep_char8_t *str)
{
    STATIC_CONTRACT_NOTHROW;

    if (str == NULL)
        return true;

    while (*str) {
        if (!isspace (*str))
            return false;
        str++;
    }
    return true;
}

static
inline
ep_char8_t *
ep_rt_utf8_string_dup (const ep_char8_t *str)
{
    STATIC_CONTRACT_NOTHROW;

    if (!str)
        return NULL;

#ifdef TARGET_UNIX
    return strdup (str);
#else
    return _strdup (str);
#endif
}

static
inline
ep_char8_t *
ep_rt_utf8_string_dup_range (const ep_char8_t *str, const ep_char8_t *strEnd)
{
    ptrdiff_t byte_len = strEnd - str;
    ep_char8_t *buffer = reinterpret_cast<ep_char8_t *>(malloc(byte_len + 1));
    if (buffer != NULL)
    {
        memcpy (buffer, str, byte_len);
        buffer [byte_len] = '\0';
    }
    return buffer;
}

static
inline
ep_char8_t *
ep_rt_utf8_string_strtok (
    ep_char8_t *str,
    const ep_char8_t *delimiter,
    ep_char8_t **context)
{
    STATIC_CONTRACT_NOTHROW;
#ifdef TARGET_UNIX
    return strtok_r (str, delimiter, context);
#else
    return strtok_s (str, delimiter, context);
#endif    
}

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_utf8_string_snprintf
#define ep_rt_utf8_string_snprintf( \
    str, \
    str_len, \
    format, ...) \
sprintf_s (reinterpret_cast<char *>(str), static_cast<size_t>(str_len), reinterpret_cast<const char *>(format), __VA_ARGS__)

static
inline
bool
ep_rt_utf8_string_replace (
    ep_char8_t **str,
    const ep_char8_t *strSearch,
    const ep_char8_t *strReplacement
)
{
    STATIC_CONTRACT_NOTHROW;
    if ((*str) == NULL)
        return false;

    ep_char8_t* strFound = strstr(*str, strSearch);
    if (strFound != NULL)
    {
        size_t strSearchLen = strlen(strSearch);
        size_t newStrSize = strlen(*str) + strlen(strReplacement) - strSearchLen + 1;
        ep_char8_t *newStr =  reinterpret_cast<ep_char8_t *>(malloc(newStrSize));
        if (newStr == NULL)
        {
            *str = NULL;
            return false;
        }
        ep_rt_utf8_string_snprintf(newStr, newStrSize, "%.*s%s%s", (int)(strFound - (*str)), *str, strReplacement, strFound + strSearchLen);
        ep_rt_utf8_string_free(*str);
        *str = newStr;
        return true;
    }
    return false;
}

static
ep_char16_t *
ep_rt_utf8_to_utf16le_string (
    const ep_char8_t *str,
    size_t len)
{
    STATIC_CONTRACT_NOTHROW;

    if (!str)
        return NULL;

    // Shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // Implementation would just use strlen and malloc to make a new buffer, and would then copy the string chars one by one.
    // Assumes that only ASCII is used for ep_char8_t
    size_t len_utf8 = strlen(str);        
    if (len_utf8 == 0)
        return NULL;

    ep_char16_t *str_utf16 = reinterpret_cast<ep_char16_t *>(malloc ((len_utf8 + 1) * sizeof (ep_char16_t)));
    if (!str_utf16)
        return NULL;

    for (size_t i = 0; i < len_utf8; i++)
    {
        EP_ASSERT(isascii(str[i]));
         str_utf16[i] = str[i];
    }

    str_utf16[len_utf8] = 0;
    return str_utf16;
}

static
inline
ep_char16_t *
ep_rt_utf16_string_dup (const ep_char16_t *str)
{
    STATIC_CONTRACT_NOTHROW;

    if (!str)
        return NULL;

    size_t str_size = (ep_rt_utf16_string_len (str) + 1) * sizeof (ep_char16_t);
    ep_char16_t *str_dup = reinterpret_cast<ep_char16_t *>(malloc (str_size));
    if (str_dup)
        memcpy (str_dup, str, str_size);
    return str_dup;
}

static
inline
void
ep_rt_utf8_string_free (ep_char8_t *str)
{
    STATIC_CONTRACT_NOTHROW;

    if (str)
        free (str);
}

static
inline
size_t
ep_rt_utf16_string_len (const ep_char16_t *str)
{
    STATIC_CONTRACT_NOTHROW;
    extern size_t
    ep_rt_aot_utf16_string_len (const ep_char16_t *str);
    return ep_rt_aot_utf16_string_len(str);
}

static
ep_char8_t *
ep_rt_utf16_to_utf8_string (
    const ep_char16_t *str,
    size_t len)
{
    STATIC_CONTRACT_NOTHROW;

    if (!str)
        return NULL;
    
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // Simple implementation to create a utf8 string from a utf16 one
    size_t len_utf16 = len;
    if(len_utf16 == (size_t)-1)
        len_utf16 = ep_rt_utf16_string_len (str);

    ep_char8_t *str_utf8 = reinterpret_cast<ep_char8_t *>(malloc ((len_utf16 + 1) * sizeof (ep_char8_t)));
    if (!str_utf8)
        return NULL;

    for (size_t i = 0; i < len_utf16; i++)
    {
         str_utf8[i] = (char)str[i];
    }

    str_utf8[len_utf16] = 0;
    return str_utf8;
}

static
inline
ep_char8_t *
ep_rt_utf16le_to_utf8_string (
    const ep_char16_t *str,
    size_t len)
{
    return ep_rt_utf16_to_utf8_string (str, len);
}

static
inline
void
ep_rt_utf16_string_free (ep_char16_t *str)
{
    STATIC_CONTRACT_NOTHROW;

    if (str)
        free (str);
}

static
inline
const ep_char8_t *
ep_rt_managed_command_line_get (void)
{
    STATIC_CONTRACT_NOTHROW;
    //EP_UNREACHABLE ("Can not reach here");

    return NULL;
}

static
const ep_char8_t *
ep_rt_diagnostics_command_line_get (void)
{

    STATIC_CONTRACT_NOTHROW;

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: revisit commandline for AOT
    // return reinterpret_cast<const ep_char8_t *>(::GetCommandLineA());

    extern ep_char8_t *volatile _ep_rt_aot_diagnostics_cmd_line;
    ep_char8_t *old_cmd_line = _ep_rt_aot_diagnostics_cmd_line;
    return _ep_rt_aot_diagnostics_cmd_line;
}

/*
 * Thread.
 */

static
inline
EventPipeThreadHolder *
thread_holder_alloc_func (void)
{
    STATIC_CONTRACT_NOTHROW;
    EventPipeThreadHolder *instance = ep_thread_holder_alloc (ep_thread_alloc());
    if (instance)
        ep_thread_register (ep_thread_holder_get_thread (instance));
    return instance;
}

static
inline
void
thread_holder_free_func (EventPipeThreadHolder * thread_holder)
{
    STATIC_CONTRACT_NOTHROW;
    if (thread_holder) {
        ep_thread_unregister (ep_thread_holder_get_thread (thread_holder));
        ep_thread_holder_free (thread_holder);
    }
}

class EventPipeAotThreadHolderTLS {
public:
    EventPipeAotThreadHolderTLS ()
    {
        STATIC_CONTRACT_NOTHROW;
    }

    ~EventPipeAotThreadHolderTLS ()
    {
        STATIC_CONTRACT_NOTHROW;

        if (m_threadHolder) {
            thread_holder_free_func (m_threadHolder);
            m_threadHolder = NULL;
        }
    }

    static inline EventPipeThreadHolder * getThreadHolder ()
    {
        STATIC_CONTRACT_NOTHROW;
        return g_threadHolderTLS.m_threadHolder;
    }

    static inline EventPipeThreadHolder * createThreadHolder ()
    {
        STATIC_CONTRACT_NOTHROW;

        if (g_threadHolderTLS.m_threadHolder) {
            thread_holder_free_func (g_threadHolderTLS.m_threadHolder);
            g_threadHolderTLS.m_threadHolder = NULL;
        }
        g_threadHolderTLS.m_threadHolder = thread_holder_alloc_func ();
        return g_threadHolderTLS.m_threadHolder;
    }

private:
    EventPipeThreadHolder *m_threadHolder;
    static thread_local EventPipeAotThreadHolderTLS g_threadHolderTLS;
};

static
void
ep_rt_thread_setup (void)
{
    STATIC_CONTRACT_NOTHROW;

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Implement thread creation/management if needed
    // Thread* thread_handle = SetupThreadNoThrow ();
    // EP_ASSERT (thread_handle != NULL);
}

#ifdef TARGET_UNIX
static
inline
EventPipeThreadHolder *
pthread_getThreadHolder (void)
{
    void *value = eventpipe_tls_instance;
    if (value) {
        EventPipeThreadHolder *thread_holder = static_cast<EventPipeThreadHolder*>(value);    
        return thread_holder;
    }
    return NULL;
}

static
inline
EventPipeThreadHolder *
pthread_createThreadHolder (void)
{
    void *value = eventpipe_tls_instance;
    if (value) {
        // we need to do the unallocation here
        EventPipeThreadHolder *thread_holder_old = static_cast<EventPipeThreadHolder*>(value);
        thread_holder_free_func(thread_holder_old);
        eventpipe_tls_instance = NULL;

        value = NULL;
    }
    EventPipeThreadHolder *instance = thread_holder_alloc_func();
    if (instance){
        // We need to know when the thread is no longer in use to clean up EventPipeThreadHolder instance and will use pthread destructor function to get notification when that happens.
        pthread_setspecific(eventpipe_tls_key, instance);
        eventpipe_tls_instance = instance;
    }
    return instance;
}
#endif

static
inline
EventPipeThread *
ep_rt_thread_get (void)
{
    STATIC_CONTRACT_NOTHROW;

#ifdef TARGET_UNIX
    EventPipeThreadHolder *thread_holder = pthread_getThreadHolder ();
#else
    EventPipeThreadHolder *thread_holder = EventPipeAotThreadHolderTLS::getThreadHolder ();
#endif    
    return thread_holder ? ep_thread_holder_get_thread (thread_holder) : NULL;
}

static
inline
EventPipeThread *
ep_rt_thread_get_or_create (void)
{
    STATIC_CONTRACT_NOTHROW;

#ifdef TARGET_UNIX
    EventPipeThreadHolder *thread_holder = pthread_getThreadHolder ();
    if (!thread_holder)
        thread_holder = pthread_createThreadHolder ();
#else
    EventPipeThreadHolder *thread_holder = EventPipeAotThreadHolderTLS::getThreadHolder ();    
    if (!thread_holder)
        thread_holder = EventPipeAotThreadHolderTLS::createThreadHolder ();
#endif        

    return ep_thread_holder_get_thread (thread_holder);
}

static
inline
ep_rt_thread_handle_t
ep_rt_thread_get_handle (void)
{
    STATIC_CONTRACT_NOTHROW;
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Implement thread creation/management if needed
    // return GetThreadNULLOk ();
    return NULL;
}

static
inline
ep_rt_thread_id_t
ep_rt_thread_get_id (ep_rt_thread_handle_t thread_handle)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (thread_handle != NULL);

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Implement thread creation/management if needed
    // return ep_rt_uint64_t_to_thread_id_t (thread_handle->GetOSThreadId64 ());
    // PalDebugBreak();
    return 0;
}

static
inline
uint64_t
ep_rt_thread_id_t_to_uint64_t (ep_rt_thread_id_t thread_id)
{
    return static_cast<uint64_t>(thread_id);
}

static
inline
ep_rt_thread_id_t
ep_rt_uint64_t_to_thread_id_t (uint64_t thread_id)
{
    return static_cast<ep_rt_thread_id_t>(thread_id);
}

static
inline
bool
ep_rt_thread_has_started (ep_rt_thread_handle_t thread_handle)
{
    STATIC_CONTRACT_NOTHROW;
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Implement thread creation/management if needed
    // return thread_handle != NULL && thread_handle->HasStarted ();
    return true;
}

static
inline
ep_rt_thread_activity_id_handle_t
ep_rt_thread_get_activity_id_handle (void)
{
    STATIC_CONTRACT_NOTHROW;
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Implement thread creation/management if needed
    // return GetThread ();
    // PalDebugBreak();
    return NULL;
}

static
inline
const uint8_t *
ep_rt_thread_get_activity_id_cref (ep_rt_thread_activity_id_handle_t activity_id_handle)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (activity_id_handle != NULL);

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Implement thread creation/management if needed
    // return reinterpret_cast<const uint8_t *>(activity_id_handle->GetActivityId ());
    // PalDebugBreak();
    return NULL;
}

static
inline
void
ep_rt_thread_get_activity_id (
    ep_rt_thread_activity_id_handle_t activity_id_handle,
    uint8_t *activity_id,
    uint32_t activity_id_len)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (activity_id_handle != NULL);
    EP_ASSERT (activity_id != NULL);
    EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

    memcpy (activity_id, ep_rt_thread_get_activity_id_cref (activity_id_handle), EP_ACTIVITY_ID_SIZE);
}

static
inline
void
ep_rt_thread_set_activity_id (
    ep_rt_thread_activity_id_handle_t activity_id_handle,
    const uint8_t *activity_id,
    uint32_t activity_id_len)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (activity_id_handle != NULL);
    EP_ASSERT (activity_id != NULL);
    EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Implement thread creation/management if needed
    // activity_id_handle->SetActivityId (reinterpret_cast<LPCGUID>(activity_id));
    // PalDebugBreak();
}

#undef EP_YIELD_WHILE
#define EP_YIELD_WHILE(condition) {}//YIELD_WHILE(condition)

/*
 * Volatile.
 */

static
inline
uint32_t
ep_rt_volatile_load_uint32_t (const volatile uint32_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    extern uint32_t
    ep_rt_aot_volatile_load_uint32_t (const volatile uint32_t *ptr);
    return ep_rt_aot_volatile_load_uint32_t(ptr);
}

static
inline
uint32_t
ep_rt_volatile_load_uint32_t_without_barrier (const volatile uint32_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    extern uint32_t
    ep_rt_aot_volatile_load_uint32_t_without_barrier (const volatile uint32_t *ptr);

    return ep_rt_aot_volatile_load_uint32_t_without_barrier(ptr);
}

static
inline
void
ep_rt_volatile_store_uint32_t (
    volatile uint32_t *ptr,
    uint32_t value)
{
    STATIC_CONTRACT_NOTHROW;
    extern void
    ep_rt_aot_volatile_store_uint32_t (
    volatile uint32_t *ptr,
    uint32_t value);

    ep_rt_aot_volatile_store_uint32_t(ptr, value);
}

static
inline
void
ep_rt_volatile_store_uint32_t_without_barrier (
    volatile uint32_t *ptr,
    uint32_t value)
{
    STATIC_CONTRACT_NOTHROW;
    extern void
    ep_rt_aot_volatile_store_uint32_t_without_barrier (
    volatile uint32_t *ptr,
    uint32_t value);

    ep_rt_aot_volatile_store_uint32_t_without_barrier(ptr, value);
}

static
inline
uint64_t
ep_rt_volatile_load_uint64_t (const volatile uint64_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    extern uint64_t
    ep_rt_aot_volatile_load_uint64_t (const volatile uint64_t *ptr);

    return ep_rt_aot_volatile_load_uint64_t(ptr);
}

static
inline
uint64_t
ep_rt_volatile_load_uint64_t_without_barrier (const volatile uint64_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    extern uint64_t
    ep_rt_aot_volatile_load_uint64_t_without_barrier (const volatile uint64_t *ptr);

    return ep_rt_aot_volatile_load_uint64_t_without_barrier(ptr);
}

static
inline
void
ep_rt_volatile_store_uint64_t (
    volatile uint64_t *ptr,
    uint64_t value)
{
    STATIC_CONTRACT_NOTHROW;
    extern void
    ep_rt_aot_volatile_store_uint64_t (
    volatile uint64_t *ptr,
    uint64_t value);

    ep_rt_aot_volatile_store_uint64_t(ptr, value);
}

static
inline
void
ep_rt_volatile_store_uint64_t_without_barrier (
    volatile uint64_t *ptr,
    uint64_t value)
{
    STATIC_CONTRACT_NOTHROW;
    extern void
    ep_rt_aot_volatile_store_uint64_t_without_barrier (
    volatile uint64_t *ptr,
    uint64_t value);
    ep_rt_aot_volatile_store_uint64_t_without_barrier(ptr, value);
}

static
inline
int64_t
ep_rt_volatile_load_int64_t (const volatile int64_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    extern int64_t
    ep_rt_aot_volatile_load_int64_t (const volatile int64_t *ptr);
    return ep_rt_aot_volatile_load_int64_t(ptr);
}

static
inline
int64_t
ep_rt_volatile_load_int64_t_without_barrier (const volatile int64_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    extern int64_t
    ep_rt_aot_volatile_load_int64_t_without_barrier (const volatile int64_t *ptr);
    return ep_rt_aot_volatile_load_int64_t_without_barrier(ptr);
}

static
inline
void
ep_rt_volatile_store_int64_t (
    volatile int64_t *ptr,
    int64_t value)
{
    STATIC_CONTRACT_NOTHROW;
    extern void
    ep_rt_aot_volatile_store_int64_t (
    volatile int64_t *ptr,
    int64_t value);
    ep_rt_aot_volatile_store_int64_t(ptr, value);
}

static
inline
void
ep_rt_volatile_store_int64_t_without_barrier (
    volatile int64_t *ptr,
    int64_t value)
{
    STATIC_CONTRACT_NOTHROW;
    extern void
    ep_rt_aot_volatile_store_int64_t_without_barrier (
    volatile int64_t *ptr,
    int64_t value);
    ep_rt_aot_volatile_store_int64_t_without_barrier(ptr, value);
}

static
inline
void *
ep_rt_volatile_load_ptr (volatile void **ptr)
{
    STATIC_CONTRACT_NOTHROW;
    extern void *
    ep_rt_aot_volatile_load_ptr (volatile void **ptr);
    return ep_rt_aot_volatile_load_ptr(ptr);
}

static
inline
void *
ep_rt_volatile_load_ptr_without_barrier (volatile void **ptr)
{
    STATIC_CONTRACT_NOTHROW;
    extern void *
    ep_rt_aot_volatile_load_ptr_without_barrier (volatile void **ptr);
    return ep_rt_aot_volatile_load_ptr_without_barrier(ptr);
}

static
inline
void
ep_rt_volatile_store_ptr (
    volatile void **ptr,
    void *value)
{
    STATIC_CONTRACT_NOTHROW;
    extern void
    ep_rt_aot_volatile_store_ptr (
    volatile void **ptr,
    void *value);
    ep_rt_aot_volatile_store_ptr(ptr, value);
}

static
inline
void
ep_rt_volatile_store_ptr_without_barrier (
    volatile void **ptr,
    void *value)
{
    STATIC_CONTRACT_NOTHROW;
    extern void
    ep_rt_aot_volatile_store_ptr_without_barrier (
    volatile void **ptr,
    void *value);
    ep_rt_aot_volatile_store_ptr_without_barrier(ptr, value);
}

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_AOT_H__ */
