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

#ifndef EP_RT_BUILD_TYPE_FUNC_NAME
#define EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, type_name, func_name) \
prefix_name ## _rt_ ## type_name ## _ ## func_name
#endif

#define EP_RT_DECLARE_LIST_PREFIX(prefix_name, list_name, list_type, item_type) \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, alloc) (list_type *list); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, free) (list_type *list, void (*callback)(void *)); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, clear) (list_type *list, void (*callback)(void *)); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, append) (list_type *list, item_type item); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, remove) (list_type *list, const item_type item); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, find) (const list_type *list, const item_type item_to_find, item_type *found_item); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, is_empty) (const list_type *list); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, is_valid) (const list_type *list);

#define EP_RT_DECLARE_LIST(list_name, list_type, item_type) \
	EP_RT_DECLARE_LIST_PREFIX(ep, list_name, list_type, item_type)

#define EP_RT_DEFINE_LIST ep_rt_redefine

#define EP_RT_DECLARE_LIST_ITERATOR_PREFIX(prefix_name, list_name, list_type, iterator_type, item_type) \
	static iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_begin) (const list_type *list); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_end) (const list_type *list, const iterator_type *iterator); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_next) (iterator_type *iterator); \
	static item_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_value) (const iterator_type *iterator);

#define EP_RT_DECLARE_LIST_ITERATOR(list_name, list_type, iterator_type, item_type) \
	EP_RT_DECLARE_LIST_ITERATOR_PREFIX(ep, list_name, list_type, iterator_type, item_type)

#define EP_RT_DEFINE_LIST_ITERATOR ep_rt_redefine

#define EP_RT_DECLARE_QUEUE_PREFIX(prefix_name, queue_name, queue_type, item_type) \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, alloc) (queue_type *queue); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, free) (queue_type *queue); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, pop_head) (queue_type *queue, item_type *item); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, push_head) (queue_type *queue, item_type item); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, push_tail) (queue_type *queue, item_type item); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, is_empty) (const queue_type *queue); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, is_valid) (const queue_type *queue);

#define EP_RT_DECLARE_QUEUE(queue_name, queue_type, item_type) \
	EP_RT_DECLARE_QUEUE_PREFIX(ep, queue_name, queue_type, item_type)

#define EP_RT_DEFINE_QUEUE ep_rt_redefine

#define EP_RT_DECLARE_ARRAY_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, alloc) (array_type *ep_array); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, alloc_capacity) (array_type *ep_array, size_t capacity); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, free) (array_type *ep_array); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, append) (array_type *ep_array, item_type item); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, clear) (array_type *ep_array); \
	static size_t EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, size) (const array_type *ep_array); \
	static item_type * EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, data) (const array_type *ep_array); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, is_valid) (const array_type *ep_array);

#define EP_RT_DECLARE_LOCAL_ARRAY_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, init) (array_type *ep_array); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, init_capacity) (array_type *ep_array, size_t capacity); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, fini) (array_type *ep_array);

#define EP_RT_DECLARE_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DECLARE_ARRAY_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#define EP_RT_DEFINE_ARRAY ep_rt_redefine

#define EP_RT_DECLARE_LOCAL_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DECLARE_LOCAL_ARRAY_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#define EP_RT_DEFINE_LOCAL_ARRAY ep_rt_redefine

#define EP_RT_DECLARE_ARRAY_ITERATOR_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_begin) (const array_type *ep_array); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_end) (const array_type *ep_array, const iterator_type *iterator); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_next) (iterator_type *iterator); \
	static item_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_value) (const iterator_type *iterator);

#define EP_RT_DECLARE_ARRAY_REVERSE_ITERATOR_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_begin) (const array_type *ep_array); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_end) (const array_type *ep_array, const iterator_type *iterator); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_next) (iterator_type *iterator); \
	static item_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_value) (const iterator_type *iterator);

#define EP_RT_DECLARE_ARRAY_ITERATOR(array_name, array_type, iterator_type, item_type) \
	EP_RT_DECLARE_ARRAY_ITERATOR_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#define EP_RT_DEFINE_ARRAY_ITERATOR ep_rt_redefine

#define EP_RT_DECLARE_ARRAY_REVERSE_ITERATOR(array_name, array_type, iterator_type, item_type) \
	EP_RT_DECLARE_ARRAY_REVERSE_ITERATOR_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#define EP_RT_DEFINE_ARRAY_REVERSE_ITERATOR ep_rt_redefine

#ifndef EP_RT_USE_CUSTOM_HASH_MAP_CALLBACKS
typedef uint32_t (*ep_rt_hash_map_hash_callback_t)(const void *);
typedef bool (*ep_rt_hash_map_equal_callback_t)(const void *, const void *);
#endif

#define EP_RT_DECLARE_HASH_MAP_BASE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, alloc) (hash_map_type *hash_map, ep_rt_hash_map_hash_callback_t hash_callback, ep_rt_hash_map_equal_callback_t eq_callback, void (*key_free_callback)(void *), void (*value_free_callback)(void *)); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, free) (hash_map_type *hash_map); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, add) (hash_map_type *hash_map, key_type key, value_type value); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, remove_all) (hash_map_type *hash_map); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, lookup) (const hash_map_type *hash_map, const key_type key, value_type *value); \
	static uint32_t EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, count) (const hash_map_type *hash_map); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, is_valid) (const hash_map_type *hash_map);

#define EP_RT_DECLARE_HASH_MAP_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DECLARE_HASH_MAP_BASE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, add_or_replace) (hash_map_type *hash_map, key_type key, value_type value);

#define EP_RT_DECLARE_HASH_MAP_REMOVE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DECLARE_HASH_MAP_BASE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, remove) (hash_map_type *hash_map, const key_type key);

#define EP_RT_DECLARE_HASH_MAP(hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DECLARE_HASH_MAP_PREFIX(ep, hash_map_name, hash_map_type, key_type, value_type)

#define EP_RT_DEFINE_HASH_MAP ep_rt_redefine

#define EP_RT_DECLARE_HASH_MAP_REMOVE(hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DECLARE_HASH_MAP_REMOVE_PREFIX(ep, hash_map_name, hash_map_type, key_type, value_type)

#define EP_RT_DEFINE_HASH_MAP_REMOVE ep_rt_redefine

#define EP_RT_DECLARE_HASH_MAP_ITERATOR_PREFIX(prefix_name, hash_map_name, hash_map_type, iterator_type, key_type, value_type) \
	static iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_begin) (const hash_map_type *hash_map); \
	static bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_end) (const hash_map_type *hash_map, const iterator_type *iterator); \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_next) (iterator_type *iterator); \
	static key_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_key) (const iterator_type *iterator); \
	static value_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_value) (const iterator_type *iterator);

#define EP_RT_DECLARE_HASH_MAP_ITERATOR(hash_map_name, hash_map_type, iterator_type, key_type, value_type) \
	EP_RT_DECLARE_HASH_MAP_ITERATOR_PREFIX(ep, hash_map_name, hash_map_type, iterator_type, key_type, value_type)

#define EP_RT_DEFINE_HASH_MAP_ITERATOR ep_rt_redefine

/*
 * Little-Endian Conversion.
 */

static
inline
uint16_t
ep_rt_val_uint16_t (uint16_t value);

static
inline
uint32_t
ep_rt_val_uint32_t (uint32_t value);

static
inline
uint64_t
ep_rt_val_uint64_t (uint64_t value);

static
inline
int16_t
ep_rt_val_int16_t (int16_t value);

static
inline
int32_t
ep_rt_val_int32_t (int32_t value);

static
inline
int64_t
ep_rt_val_int64_t (int64_t value);

static
inline
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

static
ep_char8_t *
eo_rt_atomic_compare_exchange_utf8_string (volatile ep_char8_t **target, ep_char8_t *expected, ep_char8_t *value);

/*
 * EventPipe.
 */

EP_RT_DECLARE_ARRAY (session_id_array, ep_rt_session_id_array_t, ep_rt_session_id_array_iterator_t, EventPipeSessionID)
EP_RT_DECLARE_ARRAY_ITERATOR (session_id_array, ep_rt_session_id_array_t, ep_rt_session_id_array_iterator_t, EventPipeSessionID)

EP_RT_DECLARE_ARRAY (execution_checkpoint_array, ep_rt_execution_checkpoint_array_t, ep_rt_execution_checkpoint_array_iterator_t, EventPipeExecutionCheckpoint *)
EP_RT_DECLARE_ARRAY_ITERATOR (execution_checkpoint_array, ep_rt_execution_checkpoint_array_t, ep_rt_execution_checkpoint_array_iterator_t, EventPipeExecutionCheckpoint *)

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
 * EventPipeBuffer.
 */

EP_RT_DECLARE_ARRAY (buffer_array, ep_rt_buffer_array_t, ep_rt_buffer_array_iterator_t, EventPipeBuffer *)
EP_RT_DECLARE_LOCAL_ARRAY (buffer_array, ep_rt_buffer_array_t, ep_rt_buffer_array_iterator_t, EventPipeBuffer *)
EP_RT_DECLARE_ARRAY_ITERATOR (buffer_array, ep_rt_buffer_array_t, ep_rt_buffer_array_iterator_t, EventPipeBuffer *)

#define EP_RT_DECLARE_LOCAL_BUFFER_ARRAY(var_name) ds_rt_redefine

/*
 * EventPipeBufferList.
 */

EP_RT_DECLARE_ARRAY (buffer_list_array, ep_rt_buffer_list_array_t, ep_rt_buffer_list_array_iterator_t, EventPipeBufferList *)
EP_RT_DECLARE_LOCAL_ARRAY (buffer_list_array, ep_rt_buffer_list_array_t, ep_rt_buffer_list_array_iterator_t, EventPipeBufferList *)
EP_RT_DECLARE_ARRAY_ITERATOR (buffer_list_array, ep_rt_buffer_list_array_t, ep_rt_buffer_list_array_iterator_t, EventPipeBufferList *)

#define EP_RT_DECLARE_LOCAL_BUFFER_LIST_ARRAY(var_name) ds_rt_redefine

/*
 * EventPipeEvent.
 */

EP_RT_DECLARE_LIST (event_list, ep_rt_event_list_t, EventPipeEvent *)
EP_RT_DECLARE_LIST_ITERATOR (event_list, ep_rt_event_list_t, ep_rt_event_list_iterator_t, EventPipeEvent *)

/*
 * EventPipeFile.
 */

EP_RT_DECLARE_HASH_MAP_REMOVE(metadata_labels_hash, ep_rt_metadata_labels_hash_map_t, EventPipeEvent *, uint32_t)
EP_RT_DECLARE_HASH_MAP(stack_hash, ep_rt_stack_hash_map_t, StackHashKey *, StackHashEntry *)
EP_RT_DECLARE_HASH_MAP_ITERATOR(stack_hash, ep_rt_stack_hash_map_t, ep_rt_stack_hash_map_iterator_t, StackHashKey *, StackHashEntry *)

#ifndef EP_RT_USE_CUSTOM_HASH_MAP_CALLBACKS
#define ep_rt_stack_hash_key_hash ep_stack_hash_key_hash
#define ep_rt_stack_hash_key_equal ep_stack_hash_key_equal
#endif

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
 * EventPipeProviderConfiguration.
 */

EP_RT_DECLARE_ARRAY (provider_config_array, ep_rt_provider_config_array_t, ep_rt_provider_config_array_iterator_t, EventPipeProviderConfiguration)
EP_RT_DECLARE_ARRAY_ITERATOR (provider_config_array, ep_rt_provider_config_array_t, ep_rt_provider_config_array_iterator_t, EventPipeProviderConfiguration)

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
bool
ep_rt_config_value_get_use_portable_thread_pool (void);

/*
 * EventPipeSampleProfiler.
 */

static
void
ep_rt_sample_profiler_write_sampling_event_for_threads (ep_rt_thread_handle_t sampling_thread, EventPipeEvent *sampling_event);

static
void
ep_rt_notify_profiler_provider_created (EventPipeProvider *provider);

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
 * EventPipeSequencePoint.
 */

EP_RT_DECLARE_LIST (sequence_point_list, ep_rt_sequence_point_list_t, EventPipeSequencePoint *)
EP_RT_DECLARE_LIST_ITERATOR (sequence_point_list, ep_rt_sequence_point_list_t, ep_rt_sequence_point_list_iterator_t, EventPipeSequencePoint *)

/*
 * EventPipeThread.
 */

EP_RT_DECLARE_LIST (thread_list, ep_rt_thread_list_t, EventPipeThread *)
EP_RT_DECLARE_LIST_ITERATOR (thread_list, ep_rt_thread_list_t, ep_rt_thread_list_iterator_t, EventPipeThread *)

EP_RT_DECLARE_ARRAY (thread_array, ep_rt_thread_array_t, ep_rt_thread_array_iterator_t, EventPipeThread *)
EP_RT_DECLARE_LOCAL_ARRAY (thread_array, ep_rt_thread_array_t, ep_rt_thread_array_iterator_t, EventPipeThread *)
EP_RT_DECLARE_ARRAY_ITERATOR (thread_array, ep_rt_thread_array_t, ep_rt_thread_array_iterator_t, EventPipeThread *)

#define EP_RT_DECLARE_LOCAL_THREAD_ARRAY(var_name) ds_rt_redefine

/*
 * EventPipeThreadSessionState.
 */

EP_RT_DECLARE_LIST (thread_session_state_list, ep_rt_thread_session_state_list_t, EventPipeThreadSessionState *)
EP_RT_DECLARE_LIST_ITERATOR (thread_session_state_list, ep_rt_thread_session_state_list_t, ep_rt_thread_session_state_list_iterator_t, EventPipeThreadSessionState *)

EP_RT_DECLARE_ARRAY (thread_session_state_array, ep_rt_thread_session_state_array_t, ep_rt_thread_session_state_array_iterator_t, EventPipeThreadSessionState *)
EP_RT_DECLARE_LOCAL_ARRAY (thread_session_state_array, ep_rt_thread_session_state_array_t, ep_rt_thread_session_state_array_iterator_t, EventPipeThreadSessionState *)
EP_RT_DECLARE_ARRAY_ITERATOR (thread_session_state_array, ep_rt_thread_session_state_array_t, ep_rt_thread_session_state_array_iterator_t, EventPipeThreadSessionState *)

#define EP_RT_DECLARE_LOCAL_THREAD_SESSION_STATE_ARRAY(var_name) ds_rt_redefine

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
void
ep_rt_create_activity_id (
	uint8_t *activity_id,
	uint32_t activity_id_len);

static
bool
ep_rt_is_running (void);

static
void
ep_rt_execute_rundown (ep_rt_execution_checkpoint_array_t *execution_checkpoints);

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


EP_RT_DECLARE_ARRAY (env_array_utf16_, ep_rt_env_array_utf16_t, ep_rt_env_array_utf16_iterator_t, ep_char16_t *)
EP_RT_DECLARE_ARRAY_ITERATOR (env_array_utf16, ep_rt_env_array_utf16_t, ep_rt_env_array_utf16_iterator_t, ep_char16_t *)

static
void
ep_rt_os_environment_get_utf16 (ep_rt_env_array_utf16_t *env_array);

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
bool
ep_rt_utf8_string_is_null_or_empty (const ep_char8_t *str);

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
ep_rt_utf8_to_utf16le_string (
	const ep_char8_t *str,
	size_t len);

static
ep_char16_t *
ep_rt_utf16_string_dup (const ep_char16_t *str);

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
ep_char8_t *
ep_rt_utf16le_to_utf8_string (
	const ep_char16_t *str,
	size_t len);

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
 * ThreadSequenceNumberMap.
 */

EP_RT_DECLARE_HASH_MAP_REMOVE(thread_sequence_number_map, ep_rt_thread_sequence_number_hash_map_t, EventPipeThreadSessionState *, uint32_t)
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
