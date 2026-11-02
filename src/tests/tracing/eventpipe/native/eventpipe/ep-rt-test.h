// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef EP_RT_TEST_H
#define EP_RT_TEST_H

#include "ep-rt-test-atomic.h"
#include <errno.h>
#include <minipal/strings.h>
#include <minipal/time.h>
#include <eventpipe/ep-thread.h>
#include <eventpipe/ep-provider.h>
#include <eventpipe/ep-session-provider.h>
#include <eventpipe/ep-string.h>

#ifndef HOST_WINDOWS
#include <strings.h>
#include <unistd.h>
#endif

extern ep_rt_spin_lock_handle_t ep_rt_test_config_lock;

void ep_rt_test_thread_exited(void);
ep_rt_thread_handle_t ep_rt_test_current_thread(void);
uint64_t ep_rt_test_current_thread_id(void);
EventPipeThread *ep_rt_test_thread_get_or_create(void);
bool ep_rt_test_thread_create(ep_rt_thread_start_func func, void *params, EventPipeThreadType type, uint64_t *id);
void ep_rt_test_threads_wait(void);
void ep_rt_test_wait_alloc(ep_rt_wait_event_handle_t *event, bool manual, bool initial);
void ep_rt_test_wait_free(ep_rt_wait_event_handle_t *event);
bool ep_rt_test_wait_set(ep_rt_wait_event_handle_t *event);
int32_t ep_rt_test_wait(ep_rt_wait_event_handle_t *event, uint32_t timeout);
EventPipeWaitHandle ep_rt_test_wait_handle(ep_rt_wait_event_handle_t *event);
uint32_t ep_rt_test_temp_path(ep_char8_t *buffer, uint32_t length);
uint32_t ep_rt_test_processor_count(void);
uint32_t ep_rt_test_current_processor(void);
void ep_rt_test_system_time(EventPipeSystemTime *time);
int32_t ep_rt_test_alloc_granularity(void);
uint8_t *ep_rt_test_valloc(size_t size);
void ep_rt_test_vfree(uint8_t *buffer, size_t size);
FILE *ep_rt_test_file_open(const ep_char8_t *path);
void ep_rt_test_environment(dn_vector_ptr_t *environment);
bool ep_rt_test_string_replace(ep_char8_t **str, const ep_char8_t *search, const ep_char8_t *replacement);

#undef EP_INFINITE_WAIT
#define EP_INFINITE_WAIT UINT32_MAX
#undef EP_GCX_PREEMP_ENTER
#define EP_GCX_PREEMP_ENTER {
#undef EP_GCX_PREEMP_EXIT
#define EP_GCX_PREEMP_EXIT }
#undef EP_ALWAYS_INLINE
#define EP_ALWAYS_INLINE inline
#undef EP_NEVER_INLINE
#ifdef _MSC_VER
#define EP_NEVER_INLINE __declspec(noinline)
#else
#define EP_NEVER_INLINE __attribute__((noinline))
#endif
#undef EP_ALIGN_UP
#define EP_ALIGN_UP(value, alignment) (((uintptr_t)(value) + (alignment) - 1) & ~((uintptr_t)(alignment) - 1))
#undef EP_YIELD_WHILE
#define EP_YIELD_WHILE(condition) do { uint32_t count = 0; while (condition) minipal_switch_to_thread(++count); } while (0)
#undef EP_RT_DEFINE_THREAD_FUNC
#define EP_RT_DEFINE_THREAD_FUNC(name) static ep_rt_thread_start_func_return_t name(void *data)
#undef ep_rt_object_alloc
#define ep_rt_object_alloc(type) ((type *)calloc(1, sizeof(type)))
#undef ep_rt_object_array_alloc
#define ep_rt_object_array_alloc(type, count) ((type *)calloc(count, sizeof(type)))
#undef ep_rt_utf8_string_snprintf
#define ep_rt_utf8_string_snprintf snprintf

#define EP_TEST_ENDIAN(type, suffix) \
    static inline type ep_rt_val_##suffix(type value) \
    { \
        const uint16_t one = 1; \
        if (*(const uint8_t *)&one) return value; \
        type result; \
        for (size_t i = 0; i < sizeof(type); ++i) ((uint8_t *)&result)[i] = ((const uint8_t *)&value)[sizeof(type) - 1 - i]; \
        return result; \
    }
EP_TEST_ENDIAN(uint16_t, uint16_t)
EP_TEST_ENDIAN(uint32_t, uint32_t)
EP_TEST_ENDIAN(uint64_t, uint64_t)
EP_TEST_ENDIAN(int16_t, int16_t)
EP_TEST_ENDIAN(int32_t, int32_t)
EP_TEST_ENDIAN(int64_t, int64_t)
EP_TEST_ENDIAN(uintptr_t, uintptr_t)
#undef EP_TEST_ENDIAN

static inline void ep_rt_spin_lock_alloc(ep_rt_spin_lock_handle_t *lock)
{
    lock->owner = 0;
    lock->initialized = minipal_mutex_init(&lock->mutex);
}

static inline void ep_rt_spin_lock_free(ep_rt_spin_lock_handle_t *lock)
{
    if (lock->initialized)
        minipal_mutex_destroy(&lock->mutex);
    lock->initialized = false;
}

static inline bool ep_rt_spin_lock_acquire(ep_rt_spin_lock_handle_t *lock)
{
    if (!lock->initialized)
        return false;
    minipal_mutex_enter(&lock->mutex);
    ep_rt_volatile_store_uint64_t(&lock->owner, ep_rt_test_current_thread_id());
    return true;
}

static inline bool ep_rt_spin_lock_release(ep_rt_spin_lock_handle_t *lock)
{
    EP_ASSERT(ep_rt_volatile_load_uint64_t(&lock->owner) == ep_rt_test_current_thread_id());
    ep_rt_volatile_store_uint64_t(&lock->owner, 0);
    minipal_mutex_leave(&lock->mutex);
    return true;
}

static inline bool ep_rt_spin_lock_is_valid(const ep_rt_spin_lock_handle_t *lock) { return lock->initialized; }
static inline bool ep_rt_lock_acquire(ep_rt_lock_handle_t *lock) { return ep_rt_spin_lock_acquire(lock); }
static inline bool ep_rt_lock_release(ep_rt_lock_handle_t *lock) { return ep_rt_spin_lock_release(lock); }
static inline bool ep_rt_config_acquire(void) { return ep_rt_spin_lock_acquire(&ep_rt_test_config_lock); }
static inline bool ep_rt_config_release(void) { return ep_rt_spin_lock_release(&ep_rt_test_config_lock); }
#ifdef EP_CHECKED_BUILD
static inline void ep_rt_spin_lock_requires_lock_held(const ep_rt_spin_lock_handle_t *lock) { EP_ASSERT(ep_rt_volatile_load_uint64_t(&lock->owner) == ep_rt_test_current_thread_id()); }
static inline void ep_rt_spin_lock_requires_lock_not_held(const ep_rt_spin_lock_handle_t *lock) { EP_ASSERT(ep_rt_volatile_load_uint64_t(&lock->owner) != ep_rt_test_current_thread_id()); }
static inline void ep_rt_lock_requires_lock_held(const ep_rt_lock_handle_t *lock) { ep_rt_spin_lock_requires_lock_held(lock); }
static inline void ep_rt_lock_requires_lock_not_held(const ep_rt_lock_handle_t *lock) { ep_rt_spin_lock_requires_lock_not_held(lock); }
static inline void ep_rt_config_requires_lock_held(void) { ep_rt_spin_lock_requires_lock_held(&ep_rt_test_config_lock); }
static inline void ep_rt_config_requires_lock_not_held(void) { ep_rt_spin_lock_requires_lock_not_held(&ep_rt_test_config_lock); }
#endif

static inline void ep_rt_init(void)
{
    ep_rt_spin_lock_alloc(&ep_rt_test_config_lock);
    if (!ep_rt_spin_lock_is_valid(&ep_rt_test_config_lock))
        EP_UNREACHABLE("Failed to initialize EventPipe configuration lock");
}

static inline void ep_rt_init_finish(void) { }
static inline void ep_rt_shutdown(void)
{
    ep_rt_test_threads_wait();
    ep_rt_spin_lock_free(&ep_rt_test_config_lock);
}

// This host has no managed frames, runtime providers, managed rundown, or managed sampling events.
static inline bool ep_rt_walk_managed_stack_for_thread(ep_rt_thread_handle_t thread, EventPipeStackContents *stack) { return true; }
static inline bool ep_rt_method_get_simple_assembly_name(ep_rt_method_desc_t *method, ep_char8_t *name, size_t length) { EP_UNREACHABLE("No managed methods in standalone EventPipe tests"); }
static inline bool ep_rt_method_get_full_name(ep_rt_method_desc_t *method, ep_char8_t *name, size_t length) { EP_UNREACHABLE("No managed methods in standalone EventPipe tests"); }
static inline void ep_rt_provider_config_init(EventPipeProviderConfiguration *config) { }
static inline void ep_rt_init_providers_and_events(void) { }
static inline bool ep_rt_providers_validate_all_disabled(void) { return true; }
static inline void ep_rt_prepare_provider_invoke_callback(EventPipeProviderCallbackData *data) { }
static inline void ep_rt_provider_invoke_callback(EventPipeCallback callback, const uint8_t *source, unsigned long enabled, uint8_t level, uint64_t any, uint64_t all, EventFilterDescriptor *filter, void *data)
{
    callback(source, enabled, level, any, all, filter, data);
}
static inline void ep_rt_sample_profiler_write_sampling_event_for_threads(ep_rt_thread_handle_t thread, EventPipeEvent *event) { }
static inline void ep_rt_sample_profiler_enabled(EventPipeEvent *event) { }
static inline void ep_rt_sample_profiler_session_enabled(void) { }
static inline void ep_rt_sample_profiler_disabled(void) { }
static inline void ep_rt_notify_profiler_provider_created(EventPipeProvider *provider) { }
static inline void ep_rt_session_stopping(void) { }
static inline void ep_rt_execute_rundown(dn_vector_ptr_t *checkpoints) { }

// Sessions are configured explicitly by the tests, not inherited from the parent runtime.
static inline bool ep_rt_config_value_get_enable(void) { return false; }
static inline ep_char8_t *ep_rt_config_value_get_config(void) { return NULL; }
static inline ep_char8_t *ep_rt_config_value_get_output_path(void) { return NULL; }
static inline uint32_t ep_rt_config_value_get_circular_mb(void) { return 1; }
static inline bool ep_rt_config_value_get_output_streaming(void) { return false; }
static inline bool ep_rt_config_value_get_enable_stackwalk(void) { return false; }
static inline uint32_t ep_rt_config_value_get_sampling_rate(void) { return 0; }
static inline uint32_t ep_rt_config_value_get_buffering_mode(void) { return 0; }
static inline bool ep_rt_process_detach(void) { return false; }
static inline bool ep_rt_process_shutdown(void) { return false; }
static inline bool ep_rt_is_running(void) { return true; }
static inline int ep_rt_get_last_error(void) { return errno; }
static inline uint8_t *ep_rt_byte_array_alloc(size_t size) { return (uint8_t *)malloc(size); }
static inline void ep_rt_byte_array_free(uint8_t *ptr) { free(ptr); }
static inline void ep_rt_object_free(void *ptr) { free(ptr); }
static inline void ep_rt_object_array_free(void *ptr) { free(ptr); }
static inline void ep_rt_wait_event_alloc(ep_rt_wait_event_handle_t *event, bool manual, bool initial) { ep_rt_test_wait_alloc(event, manual, initial); }
static inline void ep_rt_wait_event_free(ep_rt_wait_event_handle_t *event) { ep_rt_test_wait_free(event); }
static inline bool ep_rt_wait_event_set(ep_rt_wait_event_handle_t *event) { return ep_rt_test_wait_set(event); }
static inline int32_t ep_rt_wait_event_wait(ep_rt_wait_event_handle_t *event, uint32_t timeout, bool alertable) { return ep_rt_test_wait(event, timeout); }
static inline EventPipeWaitHandle ep_rt_wait_event_get_wait_handle(ep_rt_wait_event_handle_t *event) { return ep_rt_test_wait_handle(event); }
static inline bool ep_rt_wait_event_is_valid(ep_rt_wait_event_handle_t *event) { return event && event->event; }
static inline bool ep_rt_thread_create(void *func, void *params, EventPipeThreadType type, void *id) { return ep_rt_test_thread_create((ep_rt_thread_start_func)func, params, type, (uint64_t *)id); }
static inline bool ep_rt_queue_job(void *func, void *params) { EP_UNREACHABLE("Threaded standalone EventPipe host does not use queued jobs"); }
static inline void ep_rt_set_server_name(void) { }
static inline void ep_rt_thread_sleep(uint64_t ns)
{
    while (ns > (uint64_t)(UINT32_MAX - 1) * 1000000)
    {
        minipal_sleep(UINT32_MAX - 1);
        ns -= (uint64_t)(UINT32_MAX - 1) * 1000000;
    }
    minipal_sleep((uint32_t)((ns + 999999) / 1000000));
}
static inline uint32_t ep_rt_current_processor_get_number(void) { return ep_rt_test_current_processor(); }
static inline uint32_t ep_rt_processors_get_count(void) { return ep_rt_test_processor_count(); }
static inline ep_rt_thread_id_t ep_rt_current_thread_get_id(void) { return ep_rt_test_current_thread_id(); }
static inline int64_t ep_rt_perf_counter_query(void) { return minipal_hires_ticks(); }
static inline int64_t ep_rt_perf_frequency_query(void) { return minipal_hires_tick_frequency(); }
static inline void ep_rt_system_time_get(EventPipeSystemTime *time) { ep_rt_test_system_time(time); }
static inline int64_t ep_rt_system_timestamp_get(void) { return (int64_t)minipal_get_system_time(); }
static inline int32_t ep_rt_system_get_alloc_granularity(void) { return ep_rt_test_alloc_granularity(); }
static inline const ep_char8_t *ep_rt_os_command_line_get(void) { return "NativeEventPipeTestsDriver"; }
static inline const ep_char8_t *ep_rt_managed_command_line_get(void) { return ""; }
static inline const ep_char8_t *ep_rt_diagnostics_command_line_get(void) { return ep_rt_os_command_line_get(); }
static inline const ep_char8_t *ep_rt_entrypoint_assembly_name_get_utf8(void) { return ""; }
static inline const ep_char8_t *ep_rt_runtime_version_get_utf8(void) { return ""; }
static inline ep_rt_file_handle_t ep_rt_file_open_write(const ep_char8_t *path) { return ep_rt_test_file_open(path); }
static inline bool ep_rt_file_close(ep_rt_file_handle_t file) { return fclose(file) == 0; }
static inline bool ep_rt_file_write(ep_rt_file_handle_t file, const uint8_t *buffer, uint32_t count, uint32_t *written)
{
    *written = (uint32_t)fwrite(buffer, 1, count, file);
    return *written == count;
}
static inline uint8_t *ep_rt_valloc0(size_t size) { return ep_rt_test_valloc(size); }
static inline void ep_rt_vfree(uint8_t *buffer, size_t size) { ep_rt_test_vfree(buffer, size); }
static inline uint32_t ep_rt_temp_path_get(ep_char8_t *buffer, uint32_t length) { return ep_rt_test_temp_path(buffer, length); }
static inline void ep_rt_os_environment_get_utf16(dn_vector_ptr_t *environment) { ep_rt_test_environment(environment); }
static inline int ep_rt_utf8_string_compare(const ep_char8_t *a, const ep_char8_t *b) { return strcmp(a, b); }
static inline int ep_rt_utf8_string_compare_ignore_case(const ep_char8_t *a, const ep_char8_t *b)
{
#ifdef HOST_WINDOWS
    return _stricmp(a, b);
#else
    return strcasecmp(a, b);
#endif
}
static inline ep_char8_t *ep_rt_utf8_string_alloc(size_t length) { return (ep_char8_t *)calloc(length, 1); }
static inline void ep_rt_utf8_string_free(ep_char8_t *str) { free(str); }
static inline ep_char8_t *ep_rt_utf8_string_dup_range(const ep_char8_t *start, const ep_char8_t *end)
{
    size_t size = (size_t)(end - start);
    if (size == SIZE_MAX)
        return NULL;
    ep_char8_t *copy = ep_rt_utf8_string_alloc(size + 1);
    if (copy)
        memcpy(copy, start, size);
    return copy;
}
static inline ep_char8_t *ep_rt_utf8_string_dup(const ep_char8_t *str) { return str ? ep_rt_utf8_string_dup_range(str, str + strlen(str)) : NULL; }
static inline ep_char8_t *ep_rt_utf8_string_strtok(ep_char8_t *str, const ep_char8_t *delimiter, ep_char8_t **context)
{
#ifdef HOST_WINDOWS
    return strtok_s(str, delimiter, context);
#else
    return strtok_r(str, delimiter, context);
#endif
}
static inline bool ep_rt_utf8_string_replace(ep_char8_t **str, const ep_char8_t *search, const ep_char8_t *replacement) { return ep_rt_test_string_replace(str, search, replacement); }
static inline size_t ep_rt_utf16_string_len(const ep_char16_t *str) { return minipal_u16_strlen((const CHAR16_T *)str); }
static inline ep_char16_t *ep_rt_utf16_string_alloc(size_t length) { return (ep_char16_t *)calloc(length, sizeof(ep_char16_t)); }
static inline void ep_rt_utf16_string_free(ep_char16_t *str) { free(str); }
static inline ep_char16_t *ep_rt_utf16_string_dup(const ep_char16_t *str)
{
    size_t size = ep_rt_utf16_string_len(str) + 1;
    ep_char16_t *copy = ep_rt_utf16_string_alloc(size);
    if (copy)
        memcpy(copy, str, size * sizeof(ep_char16_t));
    return copy;
}
static inline void ep_rt_thread_setup(void) { }
static inline EventPipeThread *ep_rt_thread_get(void)
{
    EventPipeThreadHolder *holder = ep_rt_test_current_thread()->holder;
    return holder ? ep_thread_holder_get_thread(holder) : NULL;
}
static inline EventPipeThread *ep_rt_thread_get_or_create(void) { return ep_rt_test_thread_get_or_create(); }
static inline ep_rt_thread_handle_t ep_rt_thread_get_handle(void) { return ep_rt_test_current_thread(); }
static inline ep_rt_thread_id_t ep_rt_thread_get_id(ep_rt_thread_handle_t thread) { return thread->id; }
static inline uint64_t ep_rt_thread_id_t_to_uint64_t(ep_rt_thread_id_t id) { return id; }
static inline ep_rt_thread_id_t ep_rt_uint64_t_to_thread_id_t(uint64_t id) { return id; }
static inline bool ep_rt_thread_has_started(ep_rt_thread_handle_t thread) { return thread != NULL; }
static inline ep_rt_thread_activity_id_handle_t ep_rt_thread_get_activity_id_handle(void) { return ep_rt_thread_get_or_create(); }
static inline const uint8_t *ep_rt_thread_get_activity_id_cref(ep_rt_thread_activity_id_handle_t thread) { return ep_thread_get_activity_id_cref(thread); }
static inline void ep_rt_thread_get_activity_id(ep_rt_thread_activity_id_handle_t thread, uint8_t *id, uint32_t length)
{
    EP_ASSERT(length == EP_ACTIVITY_ID_SIZE);
    memcpy(id, ep_thread_get_activity_id_cref(thread), length);
}
static inline void ep_rt_thread_set_activity_id(ep_rt_thread_activity_id_handle_t thread, const uint8_t *id, uint32_t length)
{
    EP_ASSERT(length == EP_ACTIVITY_ID_SIZE);
    memcpy(ep_thread_get_activity_id_ref(thread), id, length);
}

#ifdef HOST_WINDOWS
static inline uint32_t ep_rt_current_process_get_id(void) { return GetCurrentProcessId(); }
#else
static inline uint32_t ep_rt_current_process_get_id(void) { return (uint32_t)getpid(); }
#endif

#endif
