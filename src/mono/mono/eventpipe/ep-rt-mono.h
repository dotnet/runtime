// Implementation of ep-rt.h targeting Mono runtime.
#ifndef __EVENTPIPE_RT_MONO_H__
#define __EVENTPIPE_RT_MONO_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include <glib.h>
#include <mono/utils/checked-build.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-rand.h>
#include <mono/metadata/w32file.h>
#include "ep.h"

#undef EP_ARRAY_SIZE
#define EP_ARRAY_SIZE(expr) G_N_ELEMENTS(expr)

#undef EP_INFINITE_WAIT
#define EP_INFINITE_WAIT MONO_INFINITE_WAIT

//TODO: Should make sure block is executed in safe mode.
#undef EP_GCX_PREEMP_ENTER
#define EP_GCX_PREEMP_ENTER {

//TODO: Should make sure block is returned back to previous mode.
#undef EP_GCX_PREEMP_EXIT
#define EP_GCX_PREEMP_EXIT }

#define EP_RT_DEFINE_LIST(list_name, list_type, item_type) \
	static inline void ep_rt_ ## list_name ## _free (list_type *list, void (*callback)(void *)) { \
		for (GSList *l = list->list; l; l = l->next) { \
			if (callback != NULL) \
				callback (l->data); \
		} \
		g_slist_free (list->list); \
		list->list = NULL; \
	} \
	static inline void ep_rt_ ## list_name ## _clear (list_type *list, void (*callback)(void *)) { ep_rt_ ## list_name ## _free (list, callback); } \
	static inline void ep_rt_ ## list_name ## _append (list_type *list, item_type item) { list->list = g_slist_append (list->list, ((gpointer)(gsize)item)); } \
	static inline void ep_rt_ ## list_name ## _remove (list_type *list, const item_type item) { list->list = g_slist_remove (list->list, ((gconstpointer)(const gsize)item)); } \
	static inline bool ep_rt_ ## list_name ## _find (const list_type *list, const item_type item_to_find, item_type *found_item) { \
		GSList *found_glist_item = g_slist_find (list->list, ((gconstpointer)(const gsize)item_to_find)); \
		*found_item = (found_glist_item != NULL) ? ((item_type)(gsize)(found_glist_item->data)) : ((item_type)(gsize)NULL); \
		return *found_item != NULL; \
	} \
	static inline bool ep_rt_ ## list_name ## _is_empty (const list_type *list) { return list->list == NULL; }

#define EP_RT_DEFINE_LIST_ITERATOR(list_name, list_type, iterator_type, item_type) \
	static inline void ep_rt_ ## list_name ## _iterator_begin (const list_type *list, iterator_type *iterator) { iterator->iterator = list->list; } \
	static inline bool ep_rt_ ## list_name ## _iterator_end (const list_type *list, const iterator_type *iterator) { return iterator->iterator == NULL; } \
	static inline void ep_rt_ ## list_name ## _iterator_next (const list_type *list, iterator_type *iterator) { iterator->iterator = iterator->iterator->next; } \
	static inline item_type ep_rt_ ## list_name ## _iterator_value (const iterator_type *iterator) { return ((item_type)(gsize)(iterator->iterator->data)); }

#define EP_RT_DEFINE_QUEUE(queue_name, queue_type, item_type) \
	static inline void ep_rt_ ## queue_name ## _alloc (queue_type *queue) { \
		queue->queue = g_queue_new ();\
	} \
	static inline void ep_rt_ ## queue_name ## _free (queue_type *queue) { \
		g_queue_free (queue->queue); \
		queue->queue = NULL; \
	} \
	static inline void ep_rt_ ## queue_name ## _pop_head (queue_type *queue, item_type *item) { \
		*item = ((item_type)(gsize)g_queue_pop_head (queue->queue)); \
	} \
	static inline void ep_rt_ ## queue_name ## _push_head (queue_type *queue, item_type item) { \
		g_queue_push_head (queue->queue, ((gpointer)(gsize)item)); \
	} \
	static inline void ep_rt_ ## queue_name ## _push_tail (queue_type *queue, item_type item) { \
		g_queue_push_tail (queue->queue, ((gpointer)(gsize)item)); \
	} \
	static inline bool ep_rt_ ## queue_name ## _is_empty (const queue_type *queue) { \
		return g_queue_is_empty (queue->queue); \
	}

#define EP_RT_DEFINE_HASH_MAP(hash_map_name, hash_map_type, key_type, value_type) \
	static inline void ep_rt_ ## hash_map_name ## _alloc (hash_map_type *hash_map, uint32_t (*hash_callback)(const void *), bool (*eq_callback)(const void *, const void *), void (*key_free_callback)(void *), void (*value_free_callback)(void *)) { \
		hash_map->table = g_hash_table_new_full ((GHashFunc)hash_callback, (GEqualFunc)eq_callback, (GDestroyNotify)key_free_callback, (GDestroyNotify)value_free_callback); \
		hash_map->count = 0;\
	} \
	static inline void ep_rt_ ## hash_map_name ## _free (hash_map_type *hash_map) { \
		g_hash_table_destroy (hash_map->table); \
		hash_map->table = NULL; \
		hash_map->count = 0; \
	} \
	static inline void ep_rt_ ## hash_map_name ## _add (hash_map_type *hash_map, key_type key, value_type value) { \
		g_hash_table_replace (hash_map->table, (gpointer)key, ((gpointer)(gsize)value)); \
		hash_map->count++; \
	} \
	static inline void ep_rt_ ## hash_map_name ## _remove (hash_map_type *hash_map, const key_type key) { \
		if (g_hash_table_remove (hash_map->table, (gconstpointer)key)) \
			hash_map->count--; \
	} \
	static inline void ep_rt_ ## hash_map_name ## _remove_all (hash_map_type *hash_map) { \
		g_hash_table_remove_all (hash_map->table); \
		hash_map->count = 0; \
	} \
	static inline bool ep_rt_ ## hash_map_name ## _lookup (const hash_map_type *hash_map, const key_type key, value_type *value) { \
		gpointer _value = NULL; \
		bool result = g_hash_table_lookup_extended (hash_map->table, (gconstpointer)key, NULL, &_value); \
		*value = ((value_type)(gsize)_value); \
		return result; \
	} \
	static inline uint32_t ep_rt_ ## hash_map_name ## _count (const hash_map_type *hash_map) { \
		return hash_map->count; \
	}

#define EP_RT_DEFINE_HASH_MAP_ITERATOR(hash_map_name, hash_map_type, iterator_type, key_type, value_type) \
	static inline void ep_rt_ ## hash_map_name ## _iterator_begin (const hash_map_type *hash_map, iterator_type *iterator) { \
		g_hash_table_iter_init (&iterator->iterator, hash_map->table); \
		if (hash_map->table) \
			iterator->end = g_hash_table_iter_next (&iterator->iterator, &iterator->key, &iterator->value); \
		else \
			iterator->end = true; \
	} \
	static inline bool ep_rt_ ## hash_map_name ## _iterator_end (const hash_map_type *hash_map, const iterator_type *iterator) { \
		return iterator->end; \
	} \
	static inline void ep_rt_ ## hash_map_name ## _iterator_next (const hash_map_type *hash_map, iterator_type *iterator) { \
		iterator->end = g_hash_table_iter_next (&iterator->iterator, &iterator->key, &iterator->value); \
	} \
	static inline key_type ep_rt_ ## hash_map_name ## _iterator_key (const iterator_type *iterator) { \
			return ((key_type)(gsize)iterator->key); \
	} \
	static inline value_type ep_rt_ ## hash_map_name ## _iterator_value (const iterator_type *iterator) { \
		return ((value_type)(gsize)iterator->value); \
	}

typedef gint64 (*ep_rt_mono_100ns_ticks_func)(void);
typedef gint64 (*ep_rt_mono_100ns_datetime_func)(void);
typedef int (*ep_rt_mono_cpu_count_func)(void);
typedef int (*ep_rt_mono_process_current_pid_func)(void);
typedef MonoNativeThreadId (*ep_rt_mono_native_thread_id_get_func)(void);
typedef gboolean (*ep_rt_mono_native_thread_id_equals_func)(MonoNativeThreadId, MonoNativeThreadId);
typedef mono_bool (*ep_rt_mono_runtime_is_shutting_down_func)(void);
typedef gboolean (*ep_rt_mono_rand_try_get_bytes_func)(guchar *buffer, gssize buffer_size, MonoError *error);
typedef EventPipeThread * (*ep_rt_mono_thread_get_func)(void);
typedef EventPipeThread * (*ep_rt_mono_thread_get_or_create_func)(void);
typedef void (*ep_rt_mono_thread_exited_func)(void);
typedef gpointer (*ep_rt_mono_w32file_create_func)(const gunichar2 *name, guint32 fileaccess, guint32 sharemode, guint32 createmode, guint32 attrs);
typedef gboolean (*ep_rt_mono_w32file_write_func)(gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten, gint32 *win32error);
typedef gboolean (*ep_rt_mono_w32file_close_func)(gpointer handle);

typedef struct _EventPipeMonoFuncTable {
	ep_rt_mono_100ns_ticks_func ep_rt_mono_100ns_ticks;
	ep_rt_mono_100ns_datetime_func ep_rt_mono_100ns_datetime;
	ep_rt_mono_process_current_pid_func ep_rt_mono_process_current_pid;
	ep_rt_mono_cpu_count_func ep_rt_mono_cpu_count;
	ep_rt_mono_native_thread_id_get_func ep_rt_mono_native_thread_id_get;
	ep_rt_mono_native_thread_id_equals_func ep_rt_mono_native_thread_id_equals;
	ep_rt_mono_runtime_is_shutting_down_func ep_rt_mono_runtime_is_shutting_down;
	ep_rt_mono_rand_try_get_bytes_func ep_rt_mono_rand_try_get_bytes;
	ep_rt_mono_thread_get_func ep_rt_mono_thread_get;
	ep_rt_mono_thread_get_or_create_func ep_rt_mono_thread_get_or_create;
	ep_rt_mono_thread_exited_func ep_rt_mono_thread_exited;
	ep_rt_mono_w32file_create_func ep_rt_mono_w32file_create;
	ep_rt_mono_w32file_write_func ep_rt_mono_w32file_write;
	ep_rt_mono_w32file_close_func ep_rt_mono_w32file_close;
} EventPipeMonoFuncTable;

typedef EventPipeThreadHolder * (*ep_rt_thread_holder_alloc_func)(void);
typedef void (*ep_rt_thread_holder_free_func)(EventPipeThreadHolder *thread_holder);

static
inline
ep_rt_spin_lock_handle_t *
ep_rt_mono_config_lock_get (void)
{
	extern ep_rt_spin_lock_handle_t _ep_rt_mono_config_lock;
	return &_ep_rt_mono_config_lock;
}

static
inline
EventPipeMonoFuncTable *
ep_rt_mono_func_table_get (void)
{
	extern EventPipeMonoFuncTable _ep_rt_mono_func_table;
	return &_ep_rt_mono_func_table;
}

MONO_PROFILER_API
void
mono_eventpipe_init (
	EventPipeMonoFuncTable *table,
	ep_rt_thread_holder_alloc_func thread_holder_alloc_func,
	ep_rt_thread_holder_free_func thread_holder_free_func);

MONO_PROFILER_API
void
mono_eventpipe_fini (void);

/*
* Helpers
*/

static
inline
MonoNativeThreadId
ep_rt_mono_native_thread_id_get (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return mono_native_thread_id_get ();
#else
	return ep_rt_mono_func_table_get ()->ep_rt_mono_native_thread_id_get ();
#endif
}

static
inline
gboolean
ep_rt_mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return mono_native_thread_id_equals (id1, id2);
#else
	return ep_rt_mono_func_table_get ()->ep_rt_mono_native_thread_id_equals (id1, id2);
#endif
}

static
inline
gboolean
ep_rt_mono_rand_try_get_bytes (guchar *buffer, gssize buffer_size, MonoError *error)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	extern gpointer ep_rt_mono_rand_provider;
	g_assert (ep_rt_mono_rand_provider != NULL);
	return mono_rand_try_get_bytes (&ep_rt_mono_rand_provider, buffer, buffer_size, error);
#else
	return ep_rt_mono_func_table_get ()->ep_rt_mono_rand_try_get_bytes (buffer, buffer_size, error);
#endif
}

static
inline
void
ep_rt_mono_thread_exited (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	extern gboolean ep_rt_mono_initialized;
	extern MonoNativeTlsKey ep_rt_mono_thread_holder_tls_id;
	if (ep_rt_mono_initialized) {
		EventPipeThreadHolder *thread_holder = (EventPipeThreadHolder *)mono_native_tls_get_value (ep_rt_mono_thread_holder_tls_id);
		if (thread_holder)
			ep_thread_holder_free (thread_holder);
		mono_native_tls_set_value (ep_rt_mono_thread_holder_tls_id, NULL);
	}
#else
	ep_rt_mono_func_table_get ()->ep_rt_mono_thread_exited ();
#endif
}

static
inline
gpointer
ep_rt_mono_w32file_create (const gunichar2 *name, guint32 fileaccess, guint32 sharemode, guint32 createmode, guint32 attrs)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return mono_w32file_create (name, fileaccess, sharemode, createmode, attrs);
#else
	return ep_rt_mono_func_table_get ()->ep_rt_mono_w32file_create (name, fileaccess, sharemode, createmode, attrs);
#endif
}

static
inline
gboolean
ep_rt_mono_w32file_write (gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten, gint32 *win32error)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return mono_w32file_write (handle, buffer, numbytes, byteswritten, win32error);
#else
	return ep_rt_mono_func_table_get ()->ep_rt_mono_w32file_write (handle, buffer, numbytes, byteswritten, win32error);
#endif
}

static
inline
gboolean
ep_rt_mono_w32file_close (gpointer handle)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return mono_w32file_close (handle);
#else
	return ep_rt_mono_func_table_get ()->ep_rt_mono_w32file_close (handle);
#endif
}

/*
* Atomics.
*/

static
inline
uint32_t
ep_rt_atomic_inc_uint32_t (volatile uint32_t *value)
{
	return (uint32_t)mono_atomic_inc_i32 ((volatile gint32 *)value);
}

static
inline
uint32_t
ep_rt_atomic_dec_uint32_t (volatile uint32_t *value)
{
	return (uint32_t)mono_atomic_dec_i32 ((volatile gint32 *)value);
}

static
inline
int32_t
ep_rt_atomic_inc_int32_t (volatile int32_t *value)
{
	return (int32_t)mono_atomic_inc_i32 ((volatile gint32 *)value);
}

static
inline
int32_t
ep_rt_atomic_dec_int32_t (volatile int32_t *value)
{
	return (int32_t)mono_atomic_dec_i32 ((volatile gint32 *)value);
}

/*
 * EventPipe.
 */

static
EventPipeThreadHolder *
thread_holder_alloc_func (void)
{
	return ep_thread_holder_alloc (ep_thread_alloc());
}

static
void
thread_holder_free_func (EventPipeThreadHolder * thread_holder)
{
	ep_thread_holder_free (thread_holder);
}

static
inline
void
ep_rt_init (void)
{
	mono_eventpipe_init (ep_rt_mono_func_table_get (), thread_holder_alloc_func, thread_holder_free_func);
	ep_rt_spin_lock_alloc (ep_rt_mono_config_lock_get ());
}

static
inline
void
ep_rt_shutdown (void)
{
	ep_rt_spin_lock_free (ep_rt_mono_config_lock_get ());
	mono_eventpipe_fini ();
}

static
inline
bool
ep_rt_config_aquire (void)
{
	ep_rt_spin_lock_aquire (ep_rt_mono_config_lock_get ());
	return true;
}

static
inline
void
ep_rt_config_release (void)
{
	ep_rt_spin_lock_release (ep_rt_mono_config_lock_get ());
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_config_requires_lock_held (void)
{
	ep_rt_spin_lock_requires_lock_held (ep_rt_mono_config_lock_get ());
}

static
inline
void
ep_rt_config_requires_lock_not_held (void)
{
	ep_rt_spin_lock_requires_lock_not_held (ep_rt_mono_config_lock_get ());
}
#endif

/*
 * EventPipeEvent.
 */

EP_RT_DEFINE_LIST (event_list, ep_rt_event_list_t, EventPipeEvent *)
EP_RT_DEFINE_LIST_ITERATOR (event_list, ep_rt_event_list_t, ep_rt_event_list_iterator_t, EventPipeEvent *)

/*
 * EventPipeFile.
 */

EP_RT_DEFINE_HASH_MAP(metadata_labels, ep_rt_metadata_labels_hash_map_t, EventPipeEvent *, uint32_t)
EP_RT_DEFINE_HASH_MAP(stack_hash, ep_rt_stack_hash_map_t, StackHashKey *, StackHashEntry *)
EP_RT_DEFINE_HASH_MAP_ITERATOR(stack_hash, ep_rt_stack_hash_map_t, ep_rt_stack_hash_map_iterator_t, StackHashKey *, StackHashEntry *)

/*
 * EventPipeProvider.
 */

EP_RT_DEFINE_LIST (provider_list, ep_rt_provider_list_t, EventPipeProvider *)
EP_RT_DEFINE_LIST_ITERATOR (provider_list, ep_rt_provider_list_t, ep_rt_provider_list_iterator_t, EventPipeProvider *)

EP_RT_DEFINE_QUEUE (provider_callback_data_queue, ep_rt_provider_callback_data_queue_t, EventPipeProviderCallbackData *)

static
inline
int
compare_provider_name (
	gconstpointer a,
	gconstpointer b)
{
	return (a) ? ep_rt_utf8_string_compare (ep_provider_get_provider_name ((EventPipeProvider *)a), (const ep_char8_t *)b) : 1;
}

static
inline
EventPipeProvider *
ep_rt_provider_list_find_by_name (
	const ep_rt_provider_list_t *list,
	const ep_char8_t *name)
{
	GSList *item = g_slist_find_custom (list->list, name, compare_provider_name);
	return (item != NULL) ? (EventPipeProvider *)item->data : NULL;
}

/*
 * EventPipeSampleProfiler.
 */

static
inline
void
ep_rt_sample_profiler_init (EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	//TODO: Not supported.
}

static
inline
void
ep_rt_sample_profiler_enable (void)
{
	//TODO: Not supported.
}

static
inline
void
ep_rt_sample_profiler_disable (void)
{
	//TODO: Not supported.
}

static
uint32_t
ep_rt_sample_profiler_get_sampling_rate (void)
{
	//TODO: Not supported.
	return 0;
}

/*
 * EvetPipeSessionProvider.
 */

EP_RT_DEFINE_LIST (session_provider_list, ep_rt_session_provider_list_t, EventPipeSessionProvider *)
EP_RT_DEFINE_LIST_ITERATOR (session_provider_list, ep_rt_session_provider_list_t, ep_rt_session_provider_list_iterator_t, EventPipeSessionProvider *)

static
inline
int
compare_session_provider_name (
	gconstpointer a,
	gconstpointer b)
{
	return (a) ? ep_rt_utf8_string_compare (ep_session_provider_get_provider_name ((EventPipeSessionProvider *)a), (const ep_char8_t *)b) : 1;
}

static
inline
EventPipeSessionProvider *
ep_rt_session_provider_list_find_by_name (
	const ep_rt_session_provider_list_t *list,
	const ep_char8_t *name)
{
	GSList *item = g_slist_find_custom (list->list, name, compare_provider_name);
	return (item != NULL) ? (EventPipeSessionProvider *)item->data : NULL;
}

/*
 * Arrays.
 */

static
inline
uint8_t *
ep_rt_byte_array_alloc (size_t len)
{
	return g_new(uint8_t, len);
}

static
inline
void
ep_rt_byte_array_free (uint8_t *ptr)
{
	g_free (ptr);
}

/*
 * Event.
 */

static
inline
void
ep_rt_wait_event_alloc (ep_rt_wait_event_handle_t *wait_event)
{
	EP_ASSERT (wait_event != NULL);
	wait_event->event = g_new0 (MonoOSEvent, 1);
	if (wait_event->event)
		mono_os_event_init (wait_event->event, false);
}

static
inline
void
ep_rt_wait_event_free (ep_rt_wait_event_handle_t *wait_event)
{
	if (wait_event != NULL && wait_event->event != NULL) {
		mono_os_event_destroy (wait_event->event);
		g_free (wait_event->event);
		wait_event->event = NULL;
	}
}

static
inline
bool
ep_rt_wait_event_set (ep_rt_wait_event_handle_t *wait_event)
{
	EP_ASSERT (wait_event != NULL && wait_event->event != NULL);
	mono_os_event_set (wait_event->event);
	return true;
}

static
inline
int32_t
ep_rt_wait_event_wait (
	ep_rt_wait_event_handle_t *wait_event,
	uint32_t timeout,
	bool alertable)
{
	EP_ASSERT (wait_event != NULL && wait_event->event != NULL);
	return mono_os_event_wait_one (wait_event->event, timeout, alertable);
}

static
inline
EventPipeWaitHandle
ep_rt_wait_event_get_wait_handle (ep_rt_wait_event_handle_t *wait_event)
{
	EP_ASSERT (wait_event != NULL);
	return (EventPipeWaitHandle)wait_event;
}

/*
 * Misc.
 */

static
inline
bool
ep_rt_process_detach (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return (bool)mono_runtime_is_shutting_down ();
#else
	return (bool)ep_rt_mono_func_table_get ()->ep_rt_mono_runtime_is_shutting_down ();
#endif
}

static
inline
void
ep_rt_create_activity_id (
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	ERROR_DECL (error);
	ep_rt_mono_rand_try_get_bytes ((guchar *)activity_id, EP_ACTIVITY_ID_SIZE, error);

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

/*
 * Objects.
 */

#undef ep_rt_object_alloc
#define ep_rt_object_alloc(obj_type) (g_new0 (obj_type, 1))

static
inline
void
ep_rt_object_free (void *ptr)
{
	g_free (ptr);
}

/*
 * PAL.
 */

static
inline
uint32_t
ep_rt_current_process_get_id (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return (uint32_t)mono_process_current_pid ();
#else
	return (uint32_t)ep_rt_mono_func_table_get ()->ep_rt_mono_process_current_pid ();
#endif
}

static
inline
uint32_t
ep_rt_current_processor_get_number (void)
{
	return 0xFFFFFFFF;
}

static
inline
uint32_t
ep_rt_processors_get_count (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return (uint32_t)mono_cpu_count ();
#else
	return (uint32_t)ep_rt_mono_func_table_get ()->ep_rt_mono_cpu_count ();
#endif
}

static
inline
size_t
ep_rt_current_thread_get_id (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ());
#else
	return MONO_NATIVE_THREAD_ID_TO_UINT (ep_rt_mono_func_table_get ()->ep_rt_mono_native_thread_id_get ());
#endif
}

static
inline
uint64_t
ep_rt_perf_counter_query (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return (uint64_t)mono_100ns_ticks ();
#else
	return (uint64_t)ep_rt_mono_func_table_get ()->ep_rt_mono_100ns_ticks ();
#endif
}

static
inline
uint64_t
ep_rt_perf_frequency_query (void)
{
	//Counter uses resolution of 100ns ticks.
	return 100 * 1000 * 1000;
}

static
inline
uint64_t
ep_rt_system_time_get (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return (uint64_t)mono_100ns_datetime ();
#else
	return (uint64_t)ep_rt_mono_func_table_get ()->ep_rt_mono_100ns_datetime ();
#endif
}

static
inline
const ep_char8_t *
ep_rt_command_line_get (void)
{
	//TODO: Implement.
	return "";
}

static
inline
ep_rt_file_handle_t
ep_rt_file_open_write (const ep_char8_t *path)
{
	ep_char16_t *path_utf16 = ep_rt_utf8_to_utf16_string (path, -1);
	ep_return_null_if_nok (path_utf16 != NULL);

	gpointer file_handle = ep_rt_mono_w32file_create (path_utf16, GENERIC_WRITE, FILE_SHARE_READ, CREATE_ALWAYS, FileAttributes_Normal);
	ep_rt_utf16_string_free (path_utf16);

	return file_handle;
}

static
bool
ep_rt_file_close (ep_rt_file_handle_t file_handle)
{
	ep_return_false_if_nok (file_handle != NULL);
	return ep_rt_mono_w32file_close (file_handle);
}

static
bool
ep_rt_file_write (
	ep_rt_file_handle_t file_handle,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	ep_return_false_if_nok (file_handle != NULL);
	EP_ASSERT (buffer != NULL);

	gint32 win32_error;
	bool result = ep_rt_mono_w32file_write (file_handle, buffer, bytes_to_write, bytes_written, &win32_error);
	if (result)
		*bytes_written = bytes_to_write;

	return result;
}

/*
* SpinLock.
*/

static
inline
void
ep_rt_spin_lock_alloc (ep_rt_spin_lock_handle_t *spin_lock)
{
#ifdef EP_CHECKED_BUILD
	spin_lock->lock_is_held = false;
#endif
	spin_lock->lock = g_new0 (MonoCoopMutex, 1);
	if (spin_lock->lock)
		mono_coop_mutex_init (spin_lock->lock);
}

static
inline
void
ep_rt_spin_lock_free (ep_rt_spin_lock_handle_t *spin_lock)
{
	if (spin_lock && spin_lock->lock) {
		mono_coop_mutex_destroy (spin_lock->lock);
		g_free (spin_lock->lock);
		spin_lock->lock = NULL;
	}
}

static
inline
void
ep_rt_spin_lock_aquire (ep_rt_spin_lock_handle_t *spin_lock)
{
	if (spin_lock && spin_lock->lock) {
		mono_coop_mutex_lock (spin_lock->lock);
#ifdef EP_CHECKED_BUILD
		spin_lock->owning_thread_id = ep_rt_mono_native_thread_id_get ();
		spin_lock->lock_is_held = true;
#endif
	}
}

static
inline
void
ep_rt_spin_lock_release (ep_rt_spin_lock_handle_t *spin_lock)
{
	if (spin_lock && spin_lock->lock) {
#ifdef EP_CHECKED_BUILD
		spin_lock->lock_is_held = false;
#endif
		mono_coop_mutex_unlock (spin_lock->lock);
	}
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_spin_lock_requires_lock_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
	g_assert (spin_lock->lock_is_held && ep_rt_mono_native_thread_id_equals (spin_lock->owning_thread_id, ep_rt_mono_native_thread_id_get ()));
}

static
inline
void
ep_rt_spin_lock_requires_lock_not_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
	g_assert (!spin_lock->lock_is_held || (spin_lock->lock_is_held && !ep_rt_mono_native_thread_id_equals (spin_lock->owning_thread_id, ep_rt_mono_native_thread_id_get ())));
}
#endif

/*
 * String.
 */

static
inline
size_t
ep_rt_utf8_string_len (const ep_char8_t *str)
{
	return g_utf8_strlen ((const gchar *)str, -1);
}

static
inline
int
ep_rt_utf8_string_compare (
	const ep_char8_t *str1,
	const ep_char8_t *str2)
{
	return strcmp ((const char *)str1, (const char *)str2);
}

static
inline
ep_char16_t *
ep_rt_utf8_to_utf16_string (
	const ep_char8_t *str,
	size_t len)
{
	return (ep_char16_t *)(g_utf8_to_utf16 ((const gchar *)str, len, NULL, NULL, NULL));
}

static
inline
ep_char8_t *
ep_rt_utf8_string_dup (const ep_char8_t *str)
{
	return g_strdup (str);
}

static
inline
void
ep_rt_utf8_string_free (ep_char8_t *str)
{
	g_free (str);
}

static
inline
size_t
ep_rt_utf16_string_len (const ep_char16_t *str)
{
	return g_utf16_len ((const gunichar2 *)str);
}

static
inline
ep_char8_t *
ep_rt_utf16_to_utf8_string (
	const ep_char16_t *str,
	size_t len)
{
	return g_utf16_to_utf8 ((const gunichar2 *)str, len, NULL, NULL, NULL);
}

static
inline
void
ep_rt_utf16_string_free (ep_char16_t *str)
{
	g_free (str);
}

static
inline
const ep_char8_t *
ep_rt_managed_command_line_get (void)
{
	//TODO: Implement.
	return "";
}

/*
 * Thread.
 */
static
inline
void
ep_rt_thread_setup (void)
{
	//TODO: Is this needed on Mono runtime? Looks like a thread attach, making sure thread is attached to runtime.
}

static
inline
EventPipeThread *
ep_rt_thread_get (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	extern MonoNativeTlsKey ep_rt_mono_thread_holder_tls_id;
	EventPipeThreadHolder *thread_holder = (EventPipeThreadHolder *)mono_native_tls_get_value (ep_rt_mono_thread_holder_tls_id);
	return thread_holder ? ep_thread_holder_get_thread (thread_holder) : NULL;
#else
	return ep_rt_mono_func_table_get ()->ep_rt_mono_thread_get ();
#endif
}

static
inline
EventPipeThread *
ep_rt_thread_get_or_create (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	extern MonoNativeTlsKey ep_rt_mono_thread_holder_tls_id;
	EventPipeThreadHolder *thread_holder = (EventPipeThreadHolder *)mono_native_tls_get_value (ep_rt_mono_thread_holder_tls_id);
	if (!thread_holder) {
		thread_holder = thread_holder_alloc_func ();
		mono_native_tls_set_value (ep_rt_mono_thread_holder_tls_id, thread_holder);
	}
	return ep_thread_holder_get_thread (thread_holder);
#else
	return ep_rt_mono_func_table_get ()->ep_rt_mono_thread_get_or_create ();
#endif
}

/*
 * ThreadSequenceNumberMap.
 */

EP_RT_DEFINE_HASH_MAP(thread_sequence_number_map, ep_rt_thread_sequence_number_hash_map_t, EventPipeThreadSessionState *, uint32_t)
EP_RT_DEFINE_HASH_MAP_ITERATOR(thread_sequence_number_map, ep_rt_thread_sequence_number_hash_map_t, ep_rt_thread_sequence_number_hash_map_iterator_t, EventPipeThreadSessionState *, uint32_t)

/*
 * Volatile.
 */

static
inline
uint32_t
ep_rt_volatile_load_uint32_t (const volatile uint32_t *ptr)
{
	return mono_atomic_load_i32 ((volatile gint32 *)ptr);
}

static
inline
uint32_t
ep_rt_volatile_load_uint32_t_without_barrier (const volatile uint32_t *ptr)
{
	uint32_t value = *ptr;
	return value;
}

static
inline
void
ep_rt_volatile_store_uint32_t (
	volatile uint32_t *ptr,
	uint32_t value)
{
	mono_atomic_store_i32 ((volatile gint32 *)ptr, (gint32)value);
}

static
inline
void
ep_rt_volatile_store_uint32_t_without_barrier (
	volatile uint32_t *ptr,
	uint32_t value)
{
	*ptr = value;
}

static
inline
uint64_t
ep_rt_volatile_load_uint64_t (const volatile uint64_t *ptr)
{
	return mono_atomic_load_i64 ((volatile gint64 *)ptr);
}

static
inline
uint64_t
ep_rt_volatile_load_uint64_t_without_barrier (const volatile uint64_t *ptr)
{
	uint64_t value = *ptr;
	return value;
}

static
inline
void
ep_rt_volatile_store_uint64_t (
	volatile uint64_t *ptr,
	uint64_t value)
{
	mono_atomic_store_i64 ((volatile gint64 *)ptr, (gint64)value);
}

static
inline
void
ep_rt_volatile_store_uint64_t_without_barrier (
	volatile uint64_t *ptr,
	uint64_t value)
{
	*ptr = value;
}

static
inline
void *
ep_rt_volatile_load_ptr (volatile void **ptr)
{
	return mono_atomic_load_ptr ((volatile gpointer *)ptr);
}

static
inline
void *
ep_rt_volatile_load_ptr_without_barrier (volatile void **ptr)
{
	void *value = (void *)(*ptr);
	return value;
}

static
inline
void
ep_rt_volatile_store_ptr (
	volatile void **ptr,
	void *value)
{
	mono_atomic_store_ptr ((volatile gpointer *)ptr, (gpointer)value);
}

static
inline
void
ep_rt_volatile_store_ptr_without_barrier (
	volatile void **ptr,
	void *value)
{
	*ptr = value;
}

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_MONO_H__ */
