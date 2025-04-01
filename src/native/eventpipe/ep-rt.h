#ifndef __EVENTPIPE_RT_H__
#define __EVENTPIPE_RT_H__

#include "ep-rt-config.h"

#include <minipal/utils.h>

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

#define EP_INFINITE_WAIT ep_rt_redefine

#define EP_GCX_PREEMP_ENTER ep_rt_redefine
#define EP_GCX_PREEMP_EXIT ep_rt_redefine

#define EP_YIELD_WHILE(condition) ep_rt_redefine

#define EP_ALWAYS_INLINE ep_rt_redefine
#define EP_NEVER_INLINE ep_rt_redefine
#define EP_ALIGN_UP(val,align) ep_rt_redefine

/*
 * Little-Endian Conversion.
 */

static
uint16_t
ep_rt_val_uint16_t (uint16_t value);

static
uint32_t
ep_rt_val_uint32_t (uint32_t value);

static
uint64_t
ep_rt_val_uint64_t (uint64_t value);

static
int16_t
ep_rt_val_int16_t (int16_t value);

static
int32_t
ep_rt_val_int32_t (int32_t value);

static
int64_t
ep_rt_val_int64_t (int64_t value);

static
uintptr_t
ep_rt_val_uintptr_t (uintptr_t value);

/*
* Atomics.
*/

static
uint32_t
ep_rt_atomic_inc_uint32_t (volatile uint32_t *value);

static
uint32_t
ep_rt_atomic_dec_uint32_t (volatile uint32_t *value);

static
int32_t
ep_rt_atomic_inc_int32_t (volatile int32_t *value);

static
int32_t
ep_rt_atomic_dec_int32_t (volatile int32_t *value);

static
int64_t
ep_rt_atomic_inc_int64_t (volatile int64_t *value);

static
int64_t
ep_rt_atomic_dec_int64_t (volatile int64_t *value);

static
size_t
ep_rt_atomic_compare_exchange_size_t (volatile size_t *target, size_t expected, size_t value);

/*
 * EventPipe.
 */

static
void
ep_rt_init (void);

static
void
ep_rt_init_finish (void);

static
void
ep_rt_shutdown (void);

static
bool
ep_rt_config_acquire (void);

static
bool
ep_rt_config_release (void);

#ifdef EP_CHECKED_BUILD
static
void
ep_rt_config_requires_lock_held (void);

static
void
ep_rt_config_requires_lock_not_held (void);
#else
#define ep_rt_config_requires_lock_held()
#define ep_rt_config_requires_lock_not_held()
#endif

static
bool
ep_rt_walk_managed_stack_for_thread (
	ep_rt_thread_handle_t thread,
	EventPipeStackContents *stack_contents);

static
bool
ep_rt_method_get_simple_assembly_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name, size_t name_len);

static
bool
ep_rt_method_get_full_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name, size_t name_len);

static
void
ep_rt_provider_config_init (EventPipeProviderConfiguration *provider_config);

static
void
ep_rt_init_providers_and_events (void);

static
bool
ep_rt_providers_validate_all_disabled (void);

static
void
ep_rt_prepare_provider_invoke_callback (EventPipeProviderCallbackData *provider_callback_data);

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
	void *callback_data);

/*
 * EventPipeProviderConfiguration.
 */

static
bool
ep_rt_config_value_get_enable (void);

static
ep_char8_t *
ep_rt_config_value_get_config (void);

static
ep_char8_t *
ep_rt_config_value_get_output_path (void);

static
uint32_t
ep_rt_config_value_get_circular_mb (void);

static
inline
bool
ep_rt_config_value_get_output_streaming (void);

static
inline
bool
ep_rt_config_value_get_enable_stackwalk (void);

/*
 * EventPipeSampleProfiler.
 */

static
void
ep_rt_sample_profiler_write_sampling_event_for_threads (ep_rt_thread_handle_t sampling_thread, EventPipeEvent *sampling_event);

static
void
ep_rt_sample_profiler_enabled (EventPipeEvent *sampling_event);

static
void
ep_rt_sample_profiler_disabled (void);

static
void
ep_rt_notify_profiler_provider_created (EventPipeProvider *provider);

/*
 * Arrays.
 */

static
uint8_t *
ep_rt_byte_array_alloc (size_t len);

static
void
ep_rt_byte_array_free (uint8_t *ptr);

/*
 * Event.
 */

static
void
ep_rt_wait_event_alloc (
	ep_rt_wait_event_handle_t *wait_event,
	bool manual,
	bool initial);

static
void
ep_rt_wait_event_free (ep_rt_wait_event_handle_t *wait_event);

static
bool
ep_rt_wait_event_set (ep_rt_wait_event_handle_t *wait_event);

static
int32_t
ep_rt_wait_event_wait (
	ep_rt_wait_event_handle_t *wait_event,
	uint32_t timeout,
	bool alertable);

static
EventPipeWaitHandle
ep_rt_wait_event_get_wait_handle (ep_rt_wait_event_handle_t *wait_event);

static
bool
ep_rt_wait_event_is_valid (ep_rt_wait_event_handle_t *wait_event);

/*
 * Misc.
 */

static
int
ep_rt_get_last_error (void);

static
bool
ep_rt_process_detach (void);

static
bool
ep_rt_process_shutdown (void);

static
bool
ep_rt_is_running (void);

static
void
ep_rt_execute_rundown (dn_vector_ptr_t *execution_checkpoints);

/*
 * Objects.
 */

#define ep_rt_object_alloc(obj_type) ep_rt_redefine

#define ep_rt_object_array_alloc(obj_type,size) ep_rt_redefine

static
void
ep_rt_object_array_free (void *ptr);

static
void
ep_rt_object_free (void *ptr);

/*
 * PAL.
 */

#define EP_RT_DEFINE_THREAD_FUNC ep_rt_redefine

static
bool
ep_rt_thread_create (
	void *thread_func,
	void *params,
	EventPipeThreadType thread_type,
	void *id);

static
bool
ep_rt_queue_job (
	void *job_func,
	void *params);

static
void
ep_rt_set_server_name (void);

static
void
ep_rt_thread_sleep (uint64_t ns);

static
uint32_t
ep_rt_current_process_get_id (void);

static
uint32_t
ep_rt_current_processor_get_number (void);

static
uint32_t
ep_rt_processors_get_count (void);

static
ep_rt_thread_id_t
ep_rt_current_thread_get_id (void);

static
int64_t
ep_rt_perf_counter_query (void);

static
int64_t
ep_rt_perf_frequency_query (void);

static
void
ep_rt_system_time_get (EventPipeSystemTime *system_time);

static
int64_t
ep_rt_system_timestamp_get (void);

static
int32_t
ep_rt_system_get_alloc_granularity (void);

static
const ep_char8_t *
ep_rt_os_command_line_get (void);

static
ep_rt_file_handle_t
ep_rt_file_open_write (const ep_char8_t *path);

static
bool
ep_rt_file_close (ep_rt_file_handle_t file_handle);

static
bool
ep_rt_file_write (
	ep_rt_file_handle_t file_handle,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written);

static
uint8_t *
ep_rt_valloc0 (size_t buffer_size);

static
void
ep_rt_vfree (
	uint8_t *buffer,
	size_t buffer_size);

static
uint32_t
ep_rt_temp_path_get (
	ep_char8_t *buffer,
	uint32_t buffer_len);

static
void
ep_rt_os_environment_get_utf16 (dn_vector_ptr_t *os_env);

static
const ep_char8_t *
ep_rt_entrypoint_assembly_name_get_utf8 (void);

static
const ep_char8_t *
ep_rt_runtime_version_get_utf8 (void);

/*
* Lock
*/

static
bool
ep_rt_lock_acquire (ep_rt_lock_handle_t *lock);

static
bool
ep_rt_lock_release (ep_rt_lock_handle_t *lock);

#ifdef EP_CHECKED_BUILD
static
void
ep_rt_lock_requires_lock_held (const ep_rt_lock_handle_t *lock);

static
void
ep_rt_lock_requires_lock_not_held (const ep_rt_lock_handle_t *lock);
#else
#define ep_rt_lock_requires_lock_held(lock)
#define ep_rt_lock_requires_lock_not_held(lock)
#endif

/*
* SpinLock.
*/

static
void
ep_rt_spin_lock_alloc (ep_rt_spin_lock_handle_t *spin_lock);

static
void
ep_rt_spin_lock_free (ep_rt_spin_lock_handle_t *spin_lock);

static
bool
ep_rt_spin_lock_acquire (ep_rt_spin_lock_handle_t *spin_lock);

static
bool
ep_rt_spin_lock_release (ep_rt_spin_lock_handle_t *spin_lock);

#ifdef EP_CHECKED_BUILD
static
void
ep_rt_spin_lock_requires_lock_held (const ep_rt_spin_lock_handle_t *spin_lock);

static
void
ep_rt_spin_lock_requires_lock_not_held (const ep_rt_spin_lock_handle_t *spin_lock);
#else
#define ep_rt_spin_lock_requires_lock_held(spin_lock)
#define ep_rt_spin_lock_requires_lock_not_held(spin_lock)
#endif

static
bool
ep_rt_spin_lock_is_valid (const ep_rt_spin_lock_handle_t *spin_lock);

/*
 * String.
 */

static
int
ep_rt_utf8_string_compare (
	const ep_char8_t *str1,
	const ep_char8_t *str2);

static
int
ep_rt_utf8_string_compare_ignore_case (
	const ep_char8_t *str1,
	const ep_char8_t *str2);

static
ep_char8_t *
ep_rt_utf8_string_dup (const ep_char8_t *str);

static
ep_char8_t *
ep_rt_utf8_string_dup_range (const ep_char8_t *str, const ep_char8_t *strEnd);

static
ep_char8_t *
ep_rt_utf8_string_strtok (
	ep_char8_t *str,
	const ep_char8_t *delimiter,
	ep_char8_t **context);

#define ep_rt_utf8_string_snprintf( \
	str, \
	str_len, \
	format, ...) ep_redefine

static
inline bool
ep_rt_utf8_string_replace (
	ep_char8_t **str,
	const ep_char8_t *strSearch,
	const ep_char8_t *strReplacement
);

static
ep_char16_t *
ep_rt_utf16_string_dup (const ep_char16_t *str);

static
ep_char8_t *
ep_rt_utf8_string_alloc (size_t len);

static
void
ep_rt_utf8_string_free (ep_char8_t *str);

static
size_t
ep_rt_utf16_string_len (const ep_char16_t *str);

static
ep_char16_t *
ep_rt_utf16_string_alloc (size_t len);

static
void
ep_rt_utf16_string_free (ep_char16_t *str);

static
const ep_char8_t *
ep_rt_managed_command_line_get (void);

static
const ep_char8_t *
ep_rt_diagnostics_command_line_get (void);

/*
 * Thread.
 */

static
void
ep_rt_thread_setup (void);

static
EventPipeThread *
ep_rt_thread_get (void);

static
EventPipeThread *
ep_rt_thread_get_or_create (void);

static
ep_rt_thread_handle_t
ep_rt_thread_get_handle (void);

static
ep_rt_thread_id_t
ep_rt_thread_get_id (ep_rt_thread_handle_t thread_handle);

static
uint64_t
ep_rt_thread_id_t_to_uint64_t (ep_rt_thread_id_t thread_id);

static
ep_rt_thread_id_t
ep_rt_uint64_t_to_thread_id_t (uint64_t thread_id);

static
bool
ep_rt_thread_has_started (ep_rt_thread_handle_t thread_handle);

static
ep_rt_thread_activity_id_handle_t
ep_rt_thread_get_activity_id_handle (void);

static
const uint8_t *
ep_rt_thread_get_activity_id_cref (ep_rt_thread_activity_id_handle_t activity_id_handle);

static
void
ep_rt_thread_get_activity_id (
	ep_rt_thread_activity_id_handle_t activity_id_handle,
	uint8_t *activity_id,
	uint32_t activity_id_len);

static
void
ep_rt_thread_set_activity_id (
	ep_rt_thread_activity_id_handle_t activity_id_handle,
	const uint8_t *activity_id,
	uint32_t activity_id_len);

/*
 * Volatile.
 */

static
uint32_t
ep_rt_volatile_load_uint32_t (const volatile uint32_t *ptr);

static
uint32_t
ep_rt_volatile_load_uint32_t_without_barrier (const volatile uint32_t *ptr);

static
void
ep_rt_volatile_store_uint32_t (
	volatile uint32_t *ptr,
	uint32_t value);

static
void
ep_rt_volatile_store_uint32_t_without_barrier (
	volatile uint32_t *ptr,
	uint32_t value);

static
uint64_t
ep_rt_volatile_load_uint64_t (const volatile uint64_t *ptr);

static
uint64_t
ep_rt_volatile_load_uint64_t_without_barrier (const volatile uint64_t *ptr);

static
void
ep_rt_volatile_store_uint64_t (
	volatile uint64_t *ptr,
	uint64_t value);

static
void
ep_rt_volatile_store_uint64_t_without_barrier (
	volatile uint64_t *ptr,
	uint64_t value);

static
int64_t
ep_rt_volatile_load_int64_t (const volatile int64_t *ptr);

static
int64_t
ep_rt_volatile_load_int64_t_without_barrier (const volatile int64_t *ptr);

static
void
ep_rt_volatile_store_int64_t (
	volatile int64_t *ptr,
	int64_t value);

static
void
ep_rt_volatile_store_int64_t_without_barrier (
	volatile int64_t *ptr,
	int64_t value);

static
void *
ep_rt_volatile_load_ptr (volatile void **ptr);

static
void *
ep_rt_volatile_load_ptr_without_barrier (volatile void **ptr);

static
void
ep_rt_volatile_store_ptr (
	volatile void **ptr,
	void *value);

static
void
ep_rt_volatile_store_ptr_without_barrier (
	volatile void **ptr,
	void *value);

/*
 * Enter/Exit spin lock helper used with error handling macros.
 */

#define EP_SPIN_LOCK_ENTER(expr, section_name) \
{ \
	ep_rt_spin_lock_requires_lock_not_held (expr); \
	ep_rt_spin_lock_acquire (expr); \
	bool _no_error_ ##section_name = false;

#define EP_SPIN_LOCK_EXIT(expr, section_name) \
	_no_error_ ##section_name = true; \
	goto _ep_on_spinlock_exit_ ##section_name; \
_ep_on_spinlock_exit_ ##section_name : \
	ep_rt_spin_lock_requires_lock_held (expr); \
	ep_rt_spin_lock_release (expr); \
	if (EP_UNLIKELY((!_no_error_ ##section_name))) \
		goto ep_on_error; \
	ep_rt_spin_lock_requires_lock_not_held (expr); \
}

#define ep_raise_error_if_nok_holding_spin_lock(expr, section_name) do { if (EP_UNLIKELY(!(expr))) { _no_error_ ##section_name = false; goto _ep_on_spinlock_exit_ ##section_name; } } while (0)
#define ep_raise_error_holding_spin_lock(section_name) do { _no_error_ ##section_name = false; goto _ep_on_spinlock_exit_ ##section_name; } while (0)

/*
 * Enter/Exit config lock helper used with error handling macros.
 */

#define EP_LOCK_ENTER(section_name) \
{ \
	ep_requires_lock_not_held (); \
	bool _owns_config_lock_ ##section_name = ep_rt_config_acquire (); \
	bool _no_config_error_ ##section_name = false; \
	if (EP_UNLIKELY((!_owns_config_lock_ ##section_name))) \
		goto _ep_on_config_lock_exit_ ##section_name;

#define EP_LOCK_EXIT(section_name) \
	_no_config_error_ ##section_name = true; \
_ep_on_config_lock_exit_ ##section_name: \
	if (EP_UNLIKELY((!_owns_config_lock_ ##section_name))) \
		goto ep_on_error; \
	ep_requires_lock_held (); \
	ep_rt_config_release (); \
	if (EP_UNLIKELY((!_no_config_error_ ##section_name))) \
		goto ep_on_error; \
	ep_requires_lock_not_held (); \
}

#define ep_raise_error_if_nok_holding_lock(expr, section_name) do { if (EP_UNLIKELY(!(expr))) { _no_config_error_ ##section_name = false; goto _ep_on_config_lock_exit_ ##section_name; } } while (0)
#define ep_raise_error_holding_lock(section_name) do { _no_config_error_ ##section_name = false; goto _ep_on_config_lock_exit_ ##section_name; } while (0)

#ifndef EP_NO_RT_DEPENDENCY
#include EP_RT_H
#endif

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_H__ */
