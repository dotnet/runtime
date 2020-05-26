#ifndef __EVENTPIPE_RT_H__
#define __EVENTPIPE_RT_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

#define EP_ARRAY_SIZE(expr) ep_rt_redefine
#define EP_INFINITE_WAIT ep_rt_redefine

#define EP_GCX_PREEMP_ENTER ep_rt_redefine
#define EP_GCX_PREEMP_EXIT ep_rt_redefine

#define EP_RT_DECLARE_LIST(list_name, list_type, item_type) \
	static void ep_rt_ ## list_name ## _free (list_type *list, void (*callback)(void *)); \
	static void ep_rt_ ## list_name ## _clear (list_type *list, void (*callback)(void *)); \
	static void ep_rt_ ## list_name ## _append (list_type *list, item_type item); \
	static void ep_rt_ ## list_name ## _remove (list_type *list, const item_type item); \
	static bool ep_rt_ ## list_name ## _find (const list_type *list, const item_type item_to_find, item_type *found_item); \
	static bool ep_rt_ ## list_name ## _is_empty (const list_type *list);

#define EP_RT_DECLARE_LIST_ITERATOR(list_name, list_type, iterator_type, item_type) \
	static void ep_rt_ ## list_name ## _iterator_begin (const list_type *list, iterator_type *iterator); \
	static bool ep_rt_ ## list_name ## _iterator_end (const list_type *list, const iterator_type *iterator); \
	static void ep_rt_ ## list_name ## _iterator_next (const list_type *list, iterator_type *iterator); \
	static item_type ep_rt_ ## list_name ## _iterator_value (const iterator_type *iterator);

#define EP_RT_DECLARE_QUEUE(queue_name, queue_type, item_type) \
	static void ep_rt_ ## queue_name ## _alloc (queue_type *queue); \
	static void ep_rt_ ## queue_name ## _free (queue_type *queue); \
	static void ep_rt_ ## queue_name ## _pop_head (queue_type *queue, item_type *item); \
	static void ep_rt_ ## queue_name ## _push_head (queue_type *queue, item_type item); \
	static void ep_rt_ ## queue_name ## _push_tail (queue_type *queue, item_type item); \
	static bool ep_rt_ ## queue_name ## _is_empty (const queue_type *queue);

#define EP_RT_DECLARE_HASH_MAP(hash_map_name, hash_map_type, key_type, value_type) \
	static void ep_rt_ ## hash_map_name ## _alloc (hash_map_type *hash_map, uint32_t (*hash_callback)(const void *), bool (*eq_callback)(const void *, const void *), void (*key_free_callback)(void *), void (*value_free_callback)(void *)); \
	static void ep_rt_ ## hash_map_name ## _free (hash_map_type *hash_map); \
	static void ep_rt_ ## hash_map_name ## _add (hash_map_type *hash_map, key_type key, value_type value); \
	static void ep_rt_ ## hash_map_name ## _remove (hash_map_type *hash_map, const key_type key); \
	static void ep_rt_ ## hash_map_name ## _remove_all (hash_map_type *hash_map); \
	static bool ep_rt_ ## hash_map_name ## _lookup (const hash_map_type *hash_map, const key_type key, value_type *value); \
	static uint32_t ep_rt_ ## hash_map_name ## _count (const hash_map_type *hash_map);

#define EP_RT_DECLARE_HASH_MAP_ITERATOR(hash_map_name, hash_map_type, iterator_type, key_type, value_type) \
	static void ep_rt_ ## hash_map_name ## _iterator_begin (const hash_map_type *hash_map, iterator_type *iterator); \
	static bool ep_rt_ ## hash_map_name ## _iterator_end (const hash_map_type *hash_map, const iterator_type *iterator); \
	static void ep_rt_ ## hash_map_name ## _iterator_next (const hash_map_type *hash_map, iterator_type *iterator); \
	static key_type ep_rt_ ## hash_map_name ## _iterator_key (const iterator_type *iterator); \
	static value_type ep_rt_ ## hash_map_name ## _iterator_value (const iterator_type *iterator);

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

/*
 * EventPipe.
 */

static
void
ep_rt_init (void);

static
void
ep_rt_shutdown (void);

static
bool
ep_rt_config_aquire (void);

static
void
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

/*
 * EventPipeEvent.
 */

EP_RT_DECLARE_LIST (event_list, ep_rt_event_list_t, EventPipeEvent *)
EP_RT_DECLARE_LIST_ITERATOR (event_list, ep_rt_event_list_t, ep_rt_event_list_iterator_t, EventPipeEvent *)

/*
 * EventPipeFile.
 */

EP_RT_DECLARE_HASH_MAP(metadata_labels, ep_rt_metadata_labels_hash_map_t, EventPipeEvent *, uint32_t)
EP_RT_DECLARE_HASH_MAP(stack_hash, ep_rt_stack_hash_map_t, StackHashKey *, StackHashEntry *)
EP_RT_DECLARE_HASH_MAP_ITERATOR(stack_hash, ep_rt_stack_hash_map_t, ep_rt_stack_hash_map_iterator_t, StackHashKey *, StackHashEntry *)

/*
 * EventPipeProvider.
 */

EP_RT_DECLARE_LIST (provider_list, ep_rt_provider_list_t, EventPipeProvider *)
EP_RT_DECLARE_LIST_ITERATOR (provider_list, ep_rt_provider_list_t, ep_rt_provider_list_iterator_t, EventPipeProvider *)

EP_RT_DECLARE_QUEUE (provider_callback_data_queue, ep_rt_provider_callback_data_queue_t, EventPipeProviderCallbackData *)

static
EventPipeProvider *
ep_rt_provider_list_find_by_name (
	const ep_rt_provider_list_t *list,
	const ep_char8_t *name);

/*
 * EventPipeSampleProfiler.
 */

static
void
ep_rt_sample_profiler_init (EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

static
void
ep_rt_sample_profiler_enable (void);

static
void
ep_rt_sample_profiler_disable (void);

static
uint32_t
ep_rt_sample_profiler_get_sampling_rate (void);

/*
 * EventPipeSessionProvider.
 */

EP_RT_DECLARE_LIST (session_provider_list, ep_rt_session_provider_list_t, EventPipeSessionProvider *)
EP_RT_DECLARE_LIST_ITERATOR (session_provider_list, ep_rt_session_provider_list_t, ep_rt_session_provider_list_iterator_t, EventPipeSessionProvider *)

static
EventPipeSessionProvider *
ep_rt_session_provider_list_find_by_name (
	const ep_rt_session_provider_list_t *list,
	const ep_char8_t *name);

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
ep_rt_wait_event_alloc (ep_rt_wait_event_handle_t *wait_event);

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
ep_rt_wait_event_get_handle (ep_rt_wait_event_handle_t *wait_event);

/*
 * Misc.
 */

static
bool
ep_rt_process_detach (void);

static
void
ep_rt_create_activity_id (
	uint8_t *activity_id,
	uint32_t activity_id_len);

/*
 * Objects.
 */

#define ep_rt_object_alloc(obj_type) ep_rt_redefine

static
void
ep_rt_object_free (void *ptr);

/*
 * PAL.
 */

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
size_t
ep_rt_current_thread_get_id (void);

static
uint64_t
ep_rt_perf_counter_query (void);

static
uint64_t
ep_rt_perf_frequency_query (void);

static
uint64_t
ep_rt_system_time_get (void);

static
const ep_char8_t *
ep_rt_command_line_get (void);

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
void
ep_rt_spin_lock_aquire (ep_rt_spin_lock_handle_t *spin_lock);

static
void
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

/*
 * String.
 */

static
size_t
ep_rt_utf8_string_len (const ep_char8_t *str);

static
int
ep_rt_utf8_string_compare (
	const ep_char8_t *str1,
	const ep_char8_t *str2);

static
ep_char8_t *
ep_rt_utf8_string_dup (const ep_char8_t *str);

static
ep_char16_t *
ep_rt_utf8_to_utf16_string (
	const ep_char8_t *str,
	size_t len);

static
void
ep_rt_utf8_string_free (ep_char8_t *str);

static
size_t
ep_rt_utf16_string_len (const ep_char16_t *str);

static
ep_char8_t *
ep_rt_utf16_to_utf8_string (
	const ep_char16_t *str,
	size_t len);

static
void
ep_rt_utf16_string_free (ep_char16_t *str);

static
const ep_char8_t *
ep_rt_managed_command_line_get (void);

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


/*
 * ThreadSequenceNumberMap.
 */

EP_RT_DECLARE_HASH_MAP(thread_sequence_number_map, ep_rt_thread_sequence_number_hash_map_t, EventPipeThreadSessionState *, uint32_t)
EP_RT_DECLARE_HASH_MAP_ITERATOR(thread_sequence_number_map, ep_rt_thread_sequence_number_hash_map_t, ep_rt_thread_sequence_number_hash_map_iterator_t, EventPipeThreadSessionState *, uint32_t)


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
 * Enter/Exit config lock helper used with error handling macros.
 */

#define EP_CONFIG_LOCK_ENTER \
{ \
	ep_rt_config_requires_lock_not_held (); \
	bool _owns_lock = ep_rt_config_aquire (); \
	bool _no_error = false; \
	if (EP_UNLIKELY((!_owns_lock))) \
		goto _ep_on_lock_exit;

#define EP_CONFIG_LOCK_EXIT \
	_no_error = true; \
_ep_on_lock_exit: \
	if (EP_UNLIKELY((!_owns_lock))) \
		goto ep_on_error; \
	ep_rt_config_requires_lock_held (); \
	ep_rt_config_release (); \
	if (EP_UNLIKELY((!_no_error))) \
		goto ep_on_error; \
	ep_rt_config_requires_lock_not_held (); \
}

#define ep_raise_error_if_nok_holding_lock(expr) do { if (EP_UNLIKELY(!(expr))) goto _ep_on_lock_exit; } while (0)
#define ep_raise_error_holding_lock() do { goto _ep_on_lock_exit; } while (0)

#include "ep-rt-mono.h"

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_H__ */
