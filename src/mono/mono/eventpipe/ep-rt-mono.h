// Implementation of ep-rt.h targeting Mono runtime.
#ifndef __EVENTPIPE_RT_MONO_H__
#define __EVENTPIPE_RT_MONO_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-rt-config.h>
#include <eventpipe/ep-thread.h>
#include <eventpipe/ep-types.h>
#include <eventpipe/ep-provider.h>
#include <eventpipe/ep-session-provider.h>
#include <glib.h>
#include <mono/utils/checked-build.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-rand.h>
#include <mono/metadata/w32file.h>
#include <mono/metadata/w32event.h>
#include <mono/metadata/environment-internals.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/w32api.h>

#undef EP_ARRAY_SIZE
#define EP_ARRAY_SIZE(expr) G_N_ELEMENTS(expr)

#undef EP_INFINITE_WAIT
#define EP_INFINITE_WAIT MONO_INFINITE_WAIT

#undef EP_GCX_PREEMP_ENTER
#define EP_GCX_PREEMP_ENTER {

#undef EP_GCX_PREEMP_EXIT
#define EP_GCX_PREEMP_EXIT }

#undef EP_ALWAYS_INLINE
#define EP_ALWAYS_INLINE MONO_ALWAYS_INLINE

#undef EP_NEVER_INLINE
#define EP_NEVER_INLINE MONO_NEVER_INLINE

#undef EP_ALIGN_UP
#define EP_ALIGN_UP(val,align) ALIGN_TO(val,align)

#ifndef EP_RT_BUILD_TYPE_FUNC_NAME
#define EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, type_name, func_name) \
prefix_name ## _rt_ ## type_name ## _ ## func_name
#endif

#define EP_RT_DEFINE_LIST_PREFIX(prefix_name, list_name, list_type, item_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, alloc) (list_type *list) { ; } \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, free) (list_type *list, void (*callback)(void *)) { \
		if (list && list->list) { \
			if (callback) { \
				for (GSList *l = list->list; l; l = l->next) { \
					callback (l->data); \
				} \
			} \
			g_slist_free (list->list); \
			list->list = NULL; \
		} \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, clear) (list_type *list, void (*callback)(void *)) { \
		EP_ASSERT (list != NULL); \
		ep_rt_ ## list_name ## _free (list, callback); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, append) (list_type *list, item_type item) { \
		EP_ASSERT (list != NULL); \
		list->list = g_slist_append (list->list, ((gpointer)(gsize)item)); \
		return list->list != NULL; \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, remove) (list_type *list, const item_type item) { \
		EP_ASSERT (list != NULL); \
		list->list = g_slist_remove (list->list, ((gconstpointer)(const gsize)item)); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, find) (const list_type *list, const item_type item_to_find, item_type *found_item) { \
		EP_ASSERT (list != NULL && found_item != NULL); \
		GSList *found_glist_item = g_slist_find (list->list, ((gconstpointer)(const gsize)item_to_find)); \
		*found_item = (found_glist_item != NULL) ? ((item_type)(gsize)(found_glist_item->data)) : ((item_type)(gsize)NULL); \
		return *found_item != NULL; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, is_empty) (const list_type *list) { \
		EP_ASSERT (list != NULL); \
		return list->list == NULL; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, is_valid) (const list_type *list) { return (list != NULL && list->list == NULL); }

#undef EP_RT_DEFINE_LIST
#define EP_RT_DEFINE_LIST(list_name, list_type, item_type) \
	EP_RT_DEFINE_LIST_PREFIX(ep, list_name, list_type, item_type)

#define EP_RT_DEFINE_LIST_ITERATOR_PREFIX(prefix_name, list_name, list_type, iterator_type, item_type) \
	static inline iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_begin) (const list_type *list) { \
		EP_ASSERT (list != NULL); \
		iterator_type temp; \
		temp.iterator = list->list; \
		return temp;\
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_end) (const list_type *list, const iterator_type *iterator) { \
		EP_ASSERT (list != NULL && iterator != NULL); \
		return iterator->iterator == NULL; \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_next) (iterator_type *iterator) { \
		EP_ASSERT (iterator != NULL); \
		iterator->iterator = iterator->iterator->next; \
	} \
	static inline item_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_value) (const iterator_type *iterator) { \
		EP_ASSERT (iterator != NULL); \
		return ((item_type)(gsize)(iterator->iterator->data)); \
	}

#undef EP_RT_DEFINE_LIST_ITERATOR
#define EP_RT_DEFINE_LIST_ITERATOR(list_name, list_type, iterator_type, item_type) \
	EP_RT_DEFINE_LIST_ITERATOR_PREFIX(ep, list_name, list_type, iterator_type, item_type)

#define EP_RT_DEFINE_QUEUE_PREFIX(prefix_name, queue_name, queue_type, item_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, alloc) (queue_type *queue) { queue->queue = g_queue_new (); } \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, free) (queue_type *queue) { \
		EP_ASSERT (queue != NULL); \
		g_queue_free (queue->queue); \
		queue->queue = NULL; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, pop_head) (queue_type *queue, item_type *item) { \
		EP_ASSERT (queue != NULL && item != NULL); \
		*item = ((item_type)(gsize)g_queue_pop_head (queue->queue)); \
		return true; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, push_head) (queue_type *queue, item_type item) { \
		EP_ASSERT (queue != NULL); \
		g_queue_push_head (queue->queue, ((gpointer)(gsize)item)); \
		return true; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, push_tail) (queue_type *queue, item_type item) { \
		EP_ASSERT (queue != NULL); \
		g_queue_push_tail (queue->queue, ((gpointer)(gsize)item)); \
		return true; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, is_empty) (const queue_type *queue) { \
		EP_ASSERT (queue != NULL); \
		return g_queue_is_empty (queue->queue); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, is_valid) (const queue_type *queue) { return (queue != NULL && queue->queue != NULL); }

#undef EP_RT_DEFINE_QUEUE
#define EP_RT_DEFINE_QUEUE(queue_name, queue_type, item_type) \
	EP_RT_DEFINE_QUEUE_PREFIX(ep, queue_name, queue_type, item_type)

#define EP_RT_DEFINE_ARRAY_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, alloc) (array_type *ep_array) { \
		EP_ASSERT (ep_array != NULL); \
		ep_array->array = g_array_new (FALSE, FALSE, sizeof (item_type)); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, alloc_capacity) (array_type *ep_array, size_t capacity) { \
		EP_ASSERT (ep_array != NULL); \
		ep_array->array = g_array_sized_new (FALSE, FALSE, sizeof (item_type), capacity); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, free) (array_type *ep_array) { \
		EP_ASSERT (ep_array != NULL); \
		g_array_free (ep_array->array, TRUE); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, append) (array_type *ep_array, item_type item) { \
		EP_ASSERT (ep_array != NULL); \
		return g_array_append_val (ep_array->array, item) != NULL; \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, clear) (array_type *ep_array) { \
		EP_ASSERT (ep_array != NULL); \
		g_array_set_size (ep_array->array, 0); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, remove) (array_type *ep_array, iterator_type *pos) { \
		EP_ASSERT (ep_array != NULL && pos != NULL); \
		EP_ASSERT (pos->index < ep_array->array->len); \
		ep_array->array = g_array_remove_index_fast (ep_array->array, pos->index); \
	} \
	static inline size_t EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, size) (const array_type *ep_array) { \
		EP_ASSERT (ep_array != NULL); \
		return ep_array->array->len; \
	} \
	static inline item_type * EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, data) (const array_type *ep_array) { \
		EP_ASSERT (ep_array != NULL); \
		return (item_type *)ep_array->array->data; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, is_valid) (const array_type *ep_array) { return (ep_array != NULL && ep_array->array != NULL); }

#define EP_RT_DEFINE_LOCAL_ARRAY_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, init) (array_type *ep_array) { \
		EP_ASSERT (ep_array != NULL); \
		ep_array->array = g_array_new (FALSE, FALSE, sizeof (item_type)); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, init_capacity) (array_type *ep_array, size_t capacity) { \
		EP_ASSERT (ep_array != NULL); \
		ep_array->array = g_array_sized_new (FALSE, FALSE, sizeof (item_type), capacity); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, fini) (array_type *ep_array) { \
		EP_ASSERT (ep_array != NULL); \
		g_array_free (ep_array->array, TRUE); \
	}

#undef EP_RT_DEFINE_ARRAY
#define EP_RT_DEFINE_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#undef EP_RT_DEFINE_LOCAL_ARRAY
#define EP_RT_DEFINE_LOCAL_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_LOCAL_ARRAY_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#define EP_RT_DEFINE_ARRAY_ITERATOR_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static inline iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_begin) (const array_type *ep_array) { \
		EP_ASSERT (ep_array != NULL); \
		iterator_type temp; \
		temp.array = ep_array->array; \
		temp.index = 0; \
		return temp; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_end) (const array_type *ep_array, const iterator_type *iterator) { \
		EP_ASSERT (ep_array != NULL && iterator != NULL && iterator->array == ep_array->array); \
		return iterator->index >= iterator->array->len; \
	} \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_next) (iterator_type *iterator) { \
		EP_ASSERT (iterator != NULL); \
		iterator->index++; \
	} \
	static item_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_value) (const iterator_type *iterator) { \
		EP_ASSERT (iterator != NULL); \
		return g_array_index(iterator->array, item_type, iterator->index); \
	}

#define EP_RT_DEFINE_ARRAY_REVERSE_ITERATOR_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static inline iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_begin) (const array_type *ep_array) { \
		EP_ASSERT (ep_array != NULL); \
		iterator_type temp; \
		temp.array = ep_array->array; \
		temp.index = ep_array->array->len - 1; \
		return temp; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_end) (const array_type *ep_array, const iterator_type *iterator) { \
		EP_ASSERT (ep_array != NULL && iterator != NULL && iterator->array == ep_array->array); \
		return iterator->index < 0; \
	} \
	static void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_next) (iterator_type *iterator) { \
		EP_ASSERT (iterator != NULL && iterator->array != NULL); \
		iterator->index--; \
	} \
	static item_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_value) (const iterator_type *iterator) { \
		EP_ASSERT (iterator != NULL && iterator->array != NULL); \
		EP_ASSERT (iterator->index >= 0); \
		return g_array_index(iterator->array, item_type, iterator->index); \
	}

#undef EP_RT_DEFINE_ARRAY_ITERATOR
#define EP_RT_DEFINE_ARRAY_ITERATOR(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_ITERATOR_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#undef EP_RT_DEFINE_ARRAY_REVERSE_ITERATOR
#define EP_RT_DEFINE_ARRAY_REVERSE_ITERATOR(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_REVERSE_ITERATOR_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#define EP_RT_DEFINE_HASH_MAP_BASE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, alloc) (hash_map_type *hash_map, uint32_t (*hash_callback)(const void *), bool (*eq_callback)(const void *, const void *), void (*key_free_callback)(void *), void (*value_free_callback)(void *)) { \
		EP_ASSERT (hash_map != NULL); \
		EP_ASSERT (key_free_callback == NULL); \
		hash_map->table = g_hash_table_new_full ((GHashFunc)hash_callback, (GEqualFunc)eq_callback, (GDestroyNotify)key_free_callback, (GDestroyNotify)value_free_callback); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, free) (hash_map_type *hash_map) { \
		EP_ASSERT (hash_map != NULL); \
		g_hash_table_destroy (hash_map->table); \
		hash_map->table = NULL; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, add) (hash_map_type *hash_map, key_type key, value_type value) { \
		EP_ASSERT (hash_map != NULL); \
		g_hash_table_insert (hash_map->table, (gpointer)key, ((gpointer)(gsize)value)); \
		return true; \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, remove_all) (hash_map_type *hash_map) { \
		EP_ASSERT (hash_map != NULL); \
		g_hash_table_remove_all (hash_map->table); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, lookup) (const hash_map_type *hash_map, const key_type key, value_type *value) { \
		EP_ASSERT (hash_map != NULL && value != NULL); \
		gpointer _value = NULL; \
		bool result = g_hash_table_lookup_extended (hash_map->table, (gconstpointer)key, NULL, &_value); \
		*value = ((value_type)(gsize)_value); \
		return result; \
	} \
	static inline uint32_t EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, count) (const hash_map_type *hash_map) { \
		EP_ASSERT (hash_map != NULL); \
		return (hash_map->table != NULL) ? g_hash_table_size (hash_map->table) : 0; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, is_valid) (const hash_map_type *hash_map) { \
		EP_ASSERT (hash_map != NULL); \
		return (hash_map != NULL && hash_map->table != NULL); \
	}

#define EP_RT_DEFINE_HASH_MAP_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DEFINE_HASH_MAP_BASE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, add_or_replace) (hash_map_type *hash_map, key_type key, value_type value) { \
		EP_ASSERT (hash_map != NULL); \
		g_hash_table_replace (hash_map->table, (gpointer)key, ((gpointer)(gsize)value)); \
		return true; \
	}

#define EP_RT_DEFINE_HASH_MAP_REMOVE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DEFINE_HASH_MAP_BASE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, remove) (hash_map_type *hash_map, const key_type key) { \
		EP_ASSERT (hash_map != NULL); \
		g_hash_table_remove (hash_map->table, (gconstpointer)key); \
	}

#undef EP_RT_DEFINE_HASH_MAP
#define EP_RT_DEFINE_HASH_MAP(hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DEFINE_HASH_MAP_PREFIX(ep, hash_map_name, hash_map_type, key_type, value_type)

#undef EP_RT_DEFINE_HASH_MAP_REMOVE
#define EP_RT_DEFINE_HASH_MAP_REMOVE(hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DEFINE_HASH_MAP_REMOVE_PREFIX(ep, hash_map_name, hash_map_type, key_type, value_type)

#define EP_RT_DEFINE_HASH_MAP_ITERATOR_PREFIX(prefix_name, hash_map_name, hash_map_type, iterator_type, key_type, value_type) \
	static inline iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_begin) (const hash_map_type *hash_map) { \
		EP_ASSERT (hash_map != NULL); \
		iterator_type temp; \
		g_hash_table_iter_init (&temp.iterator, hash_map->table); \
		if (hash_map->table && g_hash_table_size (hash_map->table) > 0) \
			temp.end = !g_hash_table_iter_next (&temp.iterator, &temp.key, &temp.value); \
		else \
			temp.end = true; \
		return temp; \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_end) (const hash_map_type *hash_map, const iterator_type *iterator) { \
		EP_ASSERT (hash_map != NULL && iterator != NULL); \
		return iterator->end; \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_next) (iterator_type *iterator) { \
		EP_ASSERT (iterator != NULL); \
		iterator->end = !g_hash_table_iter_next (&iterator->iterator, &iterator->key, &iterator->value); \
	} \
	static inline key_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_key) (const iterator_type *iterator) { \
		EP_ASSERT (iterator != NULL); \
		return ((key_type)(gsize)iterator->key); \
	} \
	static inline value_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_value) (const iterator_type *iterator) { \
		EP_ASSERT (iterator != NULL); \
		return ((value_type)(gsize)iterator->value); \
	}

#undef EP_RT_DEFINE_HASH_MAP_ITERATOR
#define EP_RT_DEFINE_HASH_MAP_ITERATOR(hash_map_name, hash_map_type, iterator_type, key_type, value_type) \
	EP_RT_DEFINE_HASH_MAP_ITERATOR_PREFIX(ep, hash_map_name, hash_map_type, iterator_type, key_type, value_type)

// Rundown callbacks.
typedef
bool
(*ep_rt_mono_fire_method_rundown_events_func)(
	const uint64_t method_id,
	const uint64_t module_id,
	const uint64_t method_start_address,
	const uint32_t method_size,
	const uint32_t method_token,
	const uint32_t method_flags,
	const ep_char8_t *method_namespace,
	const ep_char8_t *method_name,
	const ep_char8_t *method_signature,
	const uint16_t count_of_map_entries,
	const uint32_t *il_offsets,
	const uint32_t *native_offsets,
	void *user_data);

typedef
bool
(*ep_rt_mono_fire_assembly_rundown_events_func)(
	const uint64_t domain_id,
	const uint64_t assembly_id,
	const uint32_t assembly_flags,
	const uint32_t binding_id,
	const ep_char8_t *assembly_name,
	const uint64_t module_id,
	const uint32_t module_flags,
	const uint32_t reserved_flags,
	const ep_char8_t *module_il_path,
	const ep_char8_t *module_native_path,
	const uint8_t *managed_pdb_signature,
	const uint32_t managed_pdb_age,
	const ep_char8_t *managed_pdb_build_path,
	const uint8_t *native_pdb_signature,
	const uint32_t native_pdb_age,
	const ep_char8_t *native_pdb_build_path,
	void *user_data);

typedef
bool
(*ep_rt_mono_fire_domain_rundown_events_func)(
	const uint64_t domain_id,
	const uint32_t domain_flags,
	const ep_char8_t *domain_name,
	const uint32_t domain_index,
	void *user_data);

typedef EventPipeThreadHolder * (*ep_rt_thread_holder_alloc_func)(void);
typedef void (*ep_rt_thread_holder_free_func)(EventPipeThreadHolder *thread_holder);

typedef int (*ep_rt_mono_cpu_count_func)(void);
typedef int (*ep_rt_mono_process_current_pid_func)(void);
typedef MonoNativeThreadId (*ep_rt_mono_native_thread_id_get_func)(void);
typedef gboolean (*ep_rt_mono_native_thread_id_equals_func)(MonoNativeThreadId, MonoNativeThreadId);
typedef gboolean (*ep_rt_mono_runtime_is_shutting_down_func)(void);
typedef gboolean (*ep_rt_mono_rand_try_get_bytes_func)(guchar *buffer, gssize buffer_size, MonoError *error);
typedef EventPipeThread * (*ep_rt_mono_thread_get_func)(void);
typedef EventPipeThread * (*ep_rt_mono_thread_get_or_create_func)(void);
typedef void (*ep_rt_mono_thread_exited_func)(void);
typedef gint (*ep_rt_mono_thread_info_sleep_func)(guint32 ms, gboolean *alerted);
typedef gboolean (*ep_rt_mono_thread_info_yield_func)(void);
typedef gpointer (*ep_rt_mono_w32file_create_func)(const gunichar2 *name, guint32 fileaccess, guint32 sharemode, guint32 createmode, guint32 attrs);
typedef gboolean (*ep_rt_mono_w32file_write_func)(gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten, gint32 *win32error);
typedef gboolean (*ep_rt_mono_w32file_close_func)(gpointer handle);
typedef gpointer (*ep_rt_mono_w32event_create_func)(gboolean manual, gboolean initial);
typedef gboolean (*ep_rt_mono_w32event_close_func)(gpointer handle);
typedef void (*ep_rt_mono_w32event_set_func)(gpointer handle);
typedef MonoW32HandleWaitRet (*ep_rt_mono_w32handle_wait_one_func)(gpointer handle, guint32 timeout, gboolean alertable);
typedef void* (*ep_rt_mono_valloc_func)(void *addr, size_t length, int flags, MonoMemAccountType type);
typedef int (*ep_rt_mono_vfree_func)(void *addr, size_t length, MonoMemAccountType type);
typedef int (*ep_rt_mono_valloc_granule_func)(void);
typedef gboolean (*ep_rt_mono_thread_platform_create_thread_func)(ep_rt_thread_start_func thread_func, gpointer thread_data, gsize * const stack_size, ep_rt_thread_id_t *thread_id);
typedef gpointer (*ep_rt_mono_thread_attach_func)(gboolean);
typedef void (*ep_rt_mono_thread_detach_func)(void);
typedef char* (*ep_rt_mono_get_os_cmd_line_func)(void);
typedef char* (*ep_rt_mono_get_managed_cmd_line_func)(void);
typedef gboolean (*ep_rt_mono_execute_rundown_func)(ep_rt_mono_fire_domain_rundown_events_func domain_events_func, ep_rt_mono_fire_assembly_rundown_events_func assembly_events_func, ep_rt_mono_fire_method_rundown_events_func methods_events_func);
typedef gboolean (*ep_rt_mono_walk_managed_stack_for_thread_func)(ep_rt_thread_handle_t thread, EventPipeStackContents *stack_contents);
typedef gboolean (*ep_rt_mono_sample_profiler_write_sampling_event_for_threads_func)(ep_rt_thread_handle_t sampling_thread, EventPipeEvent *sampling_event);
typedef gboolean (*ep_rt_mono_method_get_simple_assembly_name_func)(ep_rt_method_desc_t *method, ep_char8_t *name, size_t name_len);
typedef gboolean (*ep_rt_mono_method_get_full_name_func)(ep_rt_method_desc_t *method, ep_char8_t *name, size_t name_len);

typedef struct _EventPipeMonoFuncTable {
	ep_rt_mono_process_current_pid_func ep_rt_mono_process_current_pid;
	ep_rt_mono_cpu_count_func ep_rt_mono_cpu_count;
	ep_rt_mono_native_thread_id_get_func ep_rt_mono_native_thread_id_get;
	ep_rt_mono_native_thread_id_equals_func ep_rt_mono_native_thread_id_equals;
	ep_rt_mono_runtime_is_shutting_down_func ep_rt_mono_runtime_is_shutting_down;
	ep_rt_mono_rand_try_get_bytes_func ep_rt_mono_rand_try_get_bytes;
	ep_rt_mono_thread_get_func ep_rt_mono_thread_get;
	ep_rt_mono_thread_get_or_create_func ep_rt_mono_thread_get_or_create;
	ep_rt_mono_thread_exited_func ep_rt_mono_thread_exited;
	ep_rt_mono_thread_info_sleep_func ep_rt_mono_thread_info_sleep;
	ep_rt_mono_thread_info_yield_func ep_rt_mono_thread_info_yield;
	ep_rt_mono_w32file_create_func ep_rt_mono_w32file_create;
	ep_rt_mono_w32file_write_func ep_rt_mono_w32file_write;
	ep_rt_mono_w32file_close_func ep_rt_mono_w32file_close;
	ep_rt_mono_w32event_create_func ep_rt_mono_w32event_create;
	ep_rt_mono_w32event_close_func ep_rt_mono_w32event_close;
	ep_rt_mono_w32event_set_func ep_rt_mono_w32event_set;
	ep_rt_mono_w32handle_wait_one_func ep_rt_mono_w32hadle_wait_one;
	ep_rt_mono_valloc_func ep_rt_mono_valloc;
	ep_rt_mono_vfree_func ep_rt_mono_vfree;
	ep_rt_mono_valloc_granule_func ep_rt_mono_valloc_granule;
	ep_rt_mono_thread_platform_create_thread_func ep_rt_mono_thread_platform_create_thread;
	ep_rt_mono_thread_attach_func ep_rt_mono_thread_attach;
	ep_rt_mono_thread_detach_func ep_rt_mono_thread_detach;
	ep_rt_mono_get_os_cmd_line_func ep_rt_mono_get_os_cmd_line;
	ep_rt_mono_get_managed_cmd_line_func ep_rt_mono_get_managed_cmd_line;
	ep_rt_mono_execute_rundown_func ep_rt_mono_execute_rundown;
	ep_rt_mono_walk_managed_stack_for_thread_func ep_rt_mono_walk_managed_stack_for_thread;
	ep_rt_mono_sample_profiler_write_sampling_event_for_threads_func ep_rt_mono_sample_profiler_write_sampling_event_for_threads;
	ep_rt_mono_method_get_simple_assembly_name_func ep_rt_mono_method_get_simple_assembly_name;
	ep_rt_mono_method_get_full_name_func ep_rt_mono_method_get_full_name;
} EventPipeMonoFuncTable;

int64_t
ep_rt_mono_perf_counter_query (void);

int64_t
ep_rt_mono_perf_frequency_query (void);

void
ep_rt_mono_system_time_get (EventPipeSystemTime *system_time);

int64_t
ep_rt_mono_system_timestamp_get (void);

void
ep_rt_mono_os_environment_get_utf16 (ep_rt_env_array_utf16_t *env_array);

void
ep_rt_mono_init_providers_and_events (void);

void
ep_rt_mono_fini_providers_and_events (void);

void
ep_rt_mono_execute_rundown (void);

static
inline
EventPipeMonoFuncTable *
ep_rt_mono_func_table_get (void)
{
	extern EventPipeMonoFuncTable _ep_rt_mono_func_table;
	return &_ep_rt_mono_func_table;
}

static
inline
char *
os_command_line_get (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return mono_get_os_cmd_line ();
#else
	return ep_rt_mono_func_table_get ()->ep_rt_mono_get_os_cmd_line ();
#endif
}

static
inline
char **
os_command_line_get_ref (void)
{
	extern char *_ep_rt_mono_os_cmd_line;
	return &_ep_rt_mono_os_cmd_line;
}

static
inline
mono_lazy_init_t *
os_command_line_get_init (void)
{
	extern mono_lazy_init_t _ep_rt_mono_os_cmd_line_init;
	return &_ep_rt_mono_os_cmd_line_init;
}

static
inline
void
os_command_line_lazy_init (void)
{
	if (!*os_command_line_get_ref ())
		*os_command_line_get_ref () = os_command_line_get ();
}

static
inline
void
os_command_line_lazy_clean (void)
{
	g_free (*os_command_line_get_ref ());
	*os_command_line_get_ref () = NULL;
}

static
inline
char *
managed_command_line_get (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return mono_runtime_get_managed_cmd_line ();
#else
	return ep_rt_mono_func_table_get ()->ep_rt_mono_get_managed_cmd_line ();
#endif
}

static
inline
char **
managed_command_line_get_ref (void)
{
	extern char *_ep_rt_mono_managed_cmd_line;
	return &_ep_rt_mono_managed_cmd_line;
}

static
inline
mono_lazy_init_t *
managed_command_line_get_init (void)
{
	extern mono_lazy_init_t _ep_rt_mono_managed_cmd_line_init;
	return &_ep_rt_mono_managed_cmd_line_init;
}

static
inline
void
managed_command_line_lazy_init (void)
{
	if (!*managed_command_line_get_ref ())
		*managed_command_line_get_ref () = managed_command_line_get ();
}

static
inline
void
managed_command_line_lazy_clean (void)
{
	g_free (*managed_command_line_get_ref ());
	*managed_command_line_get_ref () = NULL;
}

static
inline
ep_rt_spin_lock_handle_t *
ep_rt_mono_config_lock_get (void)
{
	extern ep_rt_spin_lock_handle_t _ep_rt_mono_config_lock;
	return &_ep_rt_mono_config_lock;
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
EventPipeThreadHolder *
thread_holder_alloc_func (void)
{
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
	if (thread_holder) {
		ep_thread_unregister (ep_thread_holder_get_thread (thread_holder));
		ep_thread_holder_free (thread_holder);
	}
}

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
	return ep_rt_mono_func_table_get ()->ep_rt_mono_rand_try_get_bytes (buffer, buffer_size, error);
}

static
inline
void
ep_rt_mono_thread_exited (void)
{
	ep_rt_mono_func_table_get ()->ep_rt_mono_thread_exited ();
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

static
inline
void
ep_rt_mono_thread_setup (bool background_thread)
{
	ep_rt_mono_func_table_get ()->ep_rt_mono_thread_attach (background_thread);
}

static
inline
void
ep_rt_mono_thread_teardown (void)
{
	ep_rt_mono_func_table_get ()->ep_rt_mono_thread_detach ();
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

static
inline
int64_t
ep_rt_atomic_inc_int64_t (volatile int64_t *value)
{
	return (int64_t)mono_atomic_inc_i64 ((volatile gint64 *)value);
}

static
inline
int64_t
ep_rt_atomic_dec_int64_t (volatile int64_t *value)
{
	return (int64_t)mono_atomic_dec_i64 ((volatile gint64 *)value);
}

/*
 * EventPipe.
 */

EP_RT_DEFINE_ARRAY (session_id_array, ep_rt_session_id_array_t, ep_rt_session_id_array_iterator_t, EventPipeSessionID)
EP_RT_DEFINE_ARRAY_ITERATOR (session_id_array, ep_rt_session_id_array_t, ep_rt_session_id_array_iterator_t, EventPipeSessionID)

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
	mono_lazy_cleanup (managed_command_line_get_init (), managed_command_line_lazy_clean);
	mono_lazy_cleanup (os_command_line_get_init (), os_command_line_lazy_clean);

	ep_rt_spin_lock_free (ep_rt_mono_config_lock_get ());
	mono_eventpipe_fini ();
}

static
inline
bool
ep_rt_config_aquire (void)
{
	return ep_rt_spin_lock_aquire (ep_rt_mono_config_lock_get ());
}

static
inline
bool
ep_rt_config_release (void)
{
	return ep_rt_spin_lock_release (ep_rt_mono_config_lock_get ());
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

static
inline
bool
ep_rt_walk_managed_stack_for_thread (
	ep_rt_thread_handle_t thread,
	EventPipeStackContents *stack_contents)
{
	return (ep_rt_mono_func_table_get ()->ep_rt_mono_walk_managed_stack_for_thread (thread, stack_contents) == TRUE) ? true : false;
}

static
inline
bool
ep_rt_method_get_simple_assembly_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	return (ep_rt_mono_func_table_get ()->ep_rt_mono_method_get_simple_assembly_name (method, name, name_len) == TRUE) ? true : false;
}

static
inline
bool
ep_rt_method_get_full_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	return (ep_rt_mono_func_table_get ()->ep_rt_mono_method_get_full_name (method, name, name_len) == TRUE) ? true : false;
}

static
inline
void
ep_rt_provider_config_init (EventPipeProviderConfiguration *provider_config)
{
	;
}

static
inline
void
ep_rt_init_providers_and_events (void)
{
	ep_rt_mono_init_providers_and_events ();
}

static
inline
bool
ep_rt_providers_validate_all_disabled (void)
{
	return true;
}

static
inline
void
ep_rt_prepare_provider_invoke_callback (EventPipeProviderCallbackData *provider_callback_data)
{
	;
}

static
inline
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

/*
 * EventPipeBuffer.
 */

EP_RT_DEFINE_ARRAY (buffer_array, ep_rt_buffer_array_t, ep_rt_buffer_array_iterator_t, EventPipeBuffer *)
EP_RT_DEFINE_LOCAL_ARRAY (buffer_array, ep_rt_buffer_array_t, ep_rt_buffer_array_iterator_t, EventPipeBuffer *)
EP_RT_DEFINE_ARRAY_ITERATOR (buffer_array, ep_rt_buffer_array_t, ep_rt_buffer_array_iterator_t, EventPipeBuffer *)

#undef EP_RT_DECLARE_LOCAL_BUFFER_ARRAY
#define EP_RT_DECLARE_LOCAL_BUFFER_ARRAY(var_name) \
	ep_rt_buffer_array_t var_name

/*
 * EventPipeBufferList.
 */

EP_RT_DEFINE_ARRAY (buffer_list_array, ep_rt_buffer_list_array_t, ep_rt_buffer_list_array_iterator_t, EventPipeBufferList *)
EP_RT_DEFINE_LOCAL_ARRAY (buffer_list_array, ep_rt_buffer_list_array_t, ep_rt_buffer_list_array_iterator_t, EventPipeBufferList *)
EP_RT_DEFINE_ARRAY_ITERATOR (buffer_list_array, ep_rt_buffer_list_array_t, ep_rt_buffer_list_array_iterator_t, EventPipeBufferList *)

#undef EP_RT_DECLARE_LOCAL_BUFFER_LIST_ARRAY
#define EP_RT_DECLARE_LOCAL_BUFFER_LIST_ARRAY(var_name) \
	ep_rt_buffer_list_array_t var_name

/*
 * EventPipeEvent.
 */

EP_RT_DEFINE_LIST (event_list, ep_rt_event_list_t, EventPipeEvent *)
EP_RT_DEFINE_LIST_ITERATOR (event_list, ep_rt_event_list_t, ep_rt_event_list_iterator_t, EventPipeEvent *)

/*
 * EventPipeFile.
 */

EP_RT_DEFINE_HASH_MAP_REMOVE(metadata_labels_hash, ep_rt_metadata_labels_hash_map_t, EventPipeEvent *, uint32_t)
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
 * EventPipeProviderConfiguration.
 */

EP_RT_DEFINE_ARRAY (provider_config_array, ep_rt_provider_config_array_t, ep_rt_provider_config_array_iterator_t, EventPipeProviderConfiguration)
EP_RT_DEFINE_ARRAY_ITERATOR (provider_config_array, ep_rt_provider_config_array_t, ep_rt_provider_config_array_iterator_t, EventPipeProviderConfiguration)

static
inline
bool
ep_rt_config_value_get_enable (void)
{
	bool enable = false;
	gchar *value = g_getenv ("COMPlus_EnableEventPipe");
	if (value && atoi (value) == 1)
		enable = true;
	g_free (value);
	return enable;
}

static
inline
ep_char8_t *
ep_rt_config_value_get_config (void)
{
	return g_getenv ("COMPlus_EventPipeConfig");
}

static
inline
ep_char8_t *
ep_rt_config_value_get_output_path (void)
{
	return g_getenv ("COMPlus_EventPipeOutputPath");
}

static
inline
uint32_t
ep_rt_config_value_get_circular_mb (void)
{
	uint32_t circular_mb = 0;
	gchar *value = g_getenv ("COMPlus_EventPipeCircularMB");
	if (value)
		circular_mb = strtoul (value, NULL, 9);
	g_free (value);
	return circular_mb;
}

static
inline
bool
ep_rt_config_value_get_use_portable_thread_pool (void)
{
	// Only supports portable thread pool.
	return true;
}

static
inline
uint32_t
ep_rt_config_value_get_rundown (void)
{
	uint32_t value_uint32_t = 1;
	gchar *value = g_getenv ("COMPlus_EventPipeRundown");
	if (value)
		value_uint32_t = (uint32_t)atoi (value);
	g_free (value);
	return value_uint32_t;
}

/*
 * EventPipeSampleProfiler.
 */

static
void
ep_rt_sample_profiler_write_sampling_event_for_threads (ep_rt_thread_handle_t sampling_thread, EventPipeEvent *sampling_event)
{
	ep_rt_mono_func_table_get ()->ep_rt_mono_sample_profiler_write_sampling_event_for_threads (sampling_thread, sampling_event);
}

static
void
ep_rt_notify_profiler_provider_created (EventPipeProvider *provider)
{
	;
}

/*
 * EventPipeSessionProvider.
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
	GSList *item = g_slist_find_custom (list->list, name, compare_session_provider_name);
	return (item != NULL) ? (EventPipeSessionProvider *)item->data : NULL;
}

/*
 * EventPipeSequencePoint.
 */

EP_RT_DEFINE_LIST (sequence_point_list, ep_rt_sequence_point_list_t, EventPipeSequencePoint *)
EP_RT_DEFINE_LIST_ITERATOR (sequence_point_list, ep_rt_sequence_point_list_t, ep_rt_sequence_point_list_iterator_t, EventPipeSequencePoint *)

/*
 * EventPipeThread.
 */

EP_RT_DEFINE_LIST (thread_list, ep_rt_thread_list_t, EventPipeThread *)
EP_RT_DEFINE_LIST_ITERATOR (thread_list, ep_rt_thread_list_t, ep_rt_thread_list_iterator_t, EventPipeThread *)

EP_RT_DEFINE_ARRAY (thread_array, ep_rt_thread_array_t, ep_rt_thread_array_iterator_t, EventPipeThread *)
EP_RT_DEFINE_LOCAL_ARRAY (thread_array, ep_rt_thread_array_t, ep_rt_thread_array_iterator_t, EventPipeThread *)
EP_RT_DEFINE_ARRAY_ITERATOR (thread_array, ep_rt_thread_array_t, ep_rt_thread_array_iterator_t, EventPipeThread *)

#undef EP_RT_DECLARE_LOCAL_THREAD_ARRAY
#define EP_RT_DECLARE_LOCAL_THREAD_ARRAY(var_name) \
	ep_rt_thread_array_t var_name

/*
 * EventPipeThreadSessionState.
 */

EP_RT_DEFINE_LIST (thread_session_state_list, ep_rt_thread_session_state_list_t, EventPipeThreadSessionState *)
EP_RT_DEFINE_LIST_ITERATOR (thread_session_state_list, ep_rt_thread_session_state_list_t, ep_rt_thread_session_state_list_iterator_t, EventPipeThreadSessionState *)

EP_RT_DEFINE_ARRAY (thread_session_state_array, ep_rt_thread_session_state_array_t, ep_rt_thread_session_state_array_iterator_t, EventPipeThreadSessionState *)
EP_RT_DEFINE_LOCAL_ARRAY (thread_session_state_array, ep_rt_thread_session_state_array_t, ep_rt_thread_session_state_array_iterator_t, EventPipeThreadSessionState *)
EP_RT_DEFINE_ARRAY_ITERATOR (thread_session_state_array, ep_rt_thread_session_state_array_t, ep_rt_thread_session_state_array_iterator_t, EventPipeThreadSessionState *)

#undef EP_RT_DECLARE_LOCAL_THREAD_SESSION_STATE_ARRAY
#define EP_RT_DECLARE_LOCAL_THREAD_SESSION_STATE_ARRAY(var_name) \
	ep_rt_thread_session_state_array_t var_name

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
ep_rt_wait_event_alloc (
	ep_rt_wait_event_handle_t *wait_event,
	bool manual,
	bool initial)
{
	EP_ASSERT (wait_event != NULL);
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	wait_event->event = mono_w32event_create (manual, initial);
#else
	wait_event->event = ep_rt_mono_func_table_get ()->ep_rt_mono_w32event_create (manual, initial);
#endif
}

static
inline
void
ep_rt_wait_event_free (ep_rt_wait_event_handle_t *wait_event)
{
	if (wait_event != NULL && wait_event->event != NULL) {
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
		mono_w32event_close (wait_event->event);
#else
		ep_rt_mono_func_table_get ()->ep_rt_mono_w32event_close (wait_event->event);
#endif
		wait_event->event = NULL;
	}
}

static
inline
bool
ep_rt_wait_event_set (ep_rt_wait_event_handle_t *wait_event)
{
	EP_ASSERT (wait_event != NULL && wait_event->event != NULL);
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	mono_w32event_set (wait_event->event);
#else
	ep_rt_mono_func_table_get ()->ep_rt_mono_w32event_set (wait_event->event);
#endif
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
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return (int32_t)mono_w32handle_wait_one (wait_event->event, timeout, alertable);
#else
	return (int32_t)ep_rt_mono_func_table_get ()->ep_rt_mono_w32hadle_wait_one (wait_event->event, timeout, alertable);
#endif
}

static
inline
EventPipeWaitHandle
ep_rt_wait_event_get_wait_handle (ep_rt_wait_event_handle_t *wait_event)
{
	EP_ASSERT (wait_event != NULL);
	return (EventPipeWaitHandle)wait_event->event;
}

static
inline
bool
ep_rt_wait_event_is_valid (ep_rt_wait_event_handle_t *wait_event)
{
	if (wait_event == NULL || wait_event->event == NULL || wait_event->event == INVALID_HANDLE_VALUE)
		return false;
	else
		return true;
}

/*
 * Misc.
 */

static
inline
int
ep_rt_get_last_error (void)
{
#ifdef HOST_WIN32
	return GetLastError ();
#else
	return errno;
#endif
}

static
inline
bool
ep_rt_process_detach (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return (mono_runtime_is_shutting_down () == TRUE) ? true : false;
#else
	return (ep_rt_mono_func_table_get ()->ep_rt_mono_runtime_is_shutting_down () == TRUE) ? true : false;
#endif
}

static
inline
bool
ep_rt_process_shutdown (void)
{
	return ep_rt_process_detach ();
}

static
inline
void
ep_rt_create_activity_id (
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	EP_ASSERT (activity_id != NULL);
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

static
inline
bool
ep_rt_is_running (void)
{
	return ep_rt_process_detach ();
}

static
inline
void
ep_rt_execute_rundown (void)
{
	if (ep_rt_config_value_get_rundown () > 0) {
		// Ask the runtime to emit rundown events.
		if (/*is_running &&*/ !ep_rt_process_shutdown ())
			ep_rt_mono_execute_rundown ();
	}
}

/*
 * Objects.
 */

#undef ep_rt_object_alloc
#define ep_rt_object_alloc(obj_type) (g_new0 (obj_type, 1))

#undef ep_rt_object_array_alloc
#define ep_rt_object_array_alloc(obj_type,size) (g_new0 (obj_type, size))

static
inline
void
ep_rt_object_array_free (void *ptr)
{
	g_free (ptr);
}

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

typedef struct _rt_mono_thread_params_internal_t {
	ep_rt_thread_params_t thread_params;
	bool background_thread;
} rt_mono_thread_params_internal_t;

#undef EP_RT_DEFINE_THREAD_FUNC
#define EP_RT_DEFINE_THREAD_FUNC(name) static mono_thread_start_return_t WINAPI name (gpointer data)

EP_RT_DEFINE_THREAD_FUNC (ep_rt_thread_mono_start_func)
{
	rt_mono_thread_params_internal_t *thread_params = (rt_mono_thread_params_internal_t *)data;

	ep_rt_mono_thread_setup (thread_params->background_thread);

	thread_params->thread_params.thread = ep_rt_thread_get_handle ();
	mono_thread_start_return_t result = thread_params->thread_params.thread_func (thread_params);

	ep_rt_mono_thread_teardown ();

	g_free (thread_params);

	return result;
}

static
inline
bool
ep_rt_thread_create (
	void *thread_func,
	void *params,
	EventPipeThreadType thread_type,
	void *id)
{
	rt_mono_thread_params_internal_t *thread_params = g_new0 (rt_mono_thread_params_internal_t, 1);
	if (thread_params) {
		thread_params->thread_params.thread_type = thread_type;
		thread_params->thread_params.thread_func = thread_func;
		thread_params->thread_params.thread_params = params;
		thread_params->background_thread = true;
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
		return (mono_thread_platform_create_thread (ep_rt_thread_mono_start_func, thread_params, NULL, (ep_rt_thread_id_t *)id) == TRUE) ? true : false;
#else
		return (ep_rt_mono_func_table_get ()->ep_rt_mono_thread_platform_create_thread (ep_rt_thread_mono_start_func, thread_params, NULL, (ep_rt_thread_id_t *)id) == TRUE) ? true : false;
#endif
	}

	return false;
}

static
inline
void
ep_rt_thread_sleep (uint64_t ns)
{
	g_usleep (ns / 1000);
}

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
ep_rt_thread_id_t
ep_rt_current_thread_get_id (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return mono_native_thread_id_get ();
#else
	return ep_rt_mono_func_table_get ()->ep_rt_mono_native_thread_id_get ();
#endif
}

static
inline
int64_t
ep_rt_perf_counter_query (void)
{
	return ep_rt_mono_perf_counter_query ();
}

static
inline
int64_t
ep_rt_perf_frequency_query (void)
{
	return ep_rt_mono_perf_frequency_query ();
}

static
inline
void
ep_rt_system_time_get (EventPipeSystemTime *system_time)
{
	ep_rt_mono_system_time_get (system_time);
}

static
inline
int64_t
ep_rt_system_timestamp_get (void)
{
	return ep_rt_mono_system_timestamp_get ();
}

static
inline
int32_t
ep_rt_system_get_alloc_granularity (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return (int32_t)mono_valloc_granule ();
#else
	return (int32_t)ep_rt_mono_func_table_get ()->ep_rt_mono_valloc_granule ();
#endif
}

static
inline
const ep_char8_t *
ep_rt_os_command_line_get (void)
{
	if (!mono_lazy_is_initialized (os_command_line_get_init ())) {
		char *cmd_line = os_command_line_get ();
		if (!cmd_line)
			return NULL;
		g_free (cmd_line);
	}

	mono_lazy_initialize (os_command_line_get_init (), os_command_line_lazy_init);
	EP_ASSERT (*os_command_line_get_ref () != NULL);
	return *os_command_line_get_ref ();
}

static
inline
ep_rt_file_handle_t
ep_rt_file_open_write (const ep_char8_t *path)
{
	ep_char16_t *path_utf16 = ep_rt_utf8_to_utf16_string (path, -1);
	ep_return_null_if_nok (path_utf16 != NULL);

	gpointer file_handle = ep_rt_mono_w32file_create ((gunichar2 *)path_utf16, GENERIC_WRITE, FILE_SHARE_READ, CREATE_ALWAYS, FileAttributes_Normal);
	ep_rt_utf16_string_free (path_utf16);

	return file_handle;
}

static
inline
bool
ep_rt_file_close (ep_rt_file_handle_t file_handle)
{
	ep_return_false_if_nok (file_handle != NULL);
	return ep_rt_mono_w32file_close (file_handle);
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
	ep_return_false_if_nok (file_handle != NULL);
	EP_ASSERT (buffer != NULL);

	gint32 win32_error;
	bool result = ep_rt_mono_w32file_write (file_handle, buffer, bytes_to_write, bytes_written, &win32_error);
	if (result)
		*bytes_written = bytes_to_write;

	return result;
}

static
inline
uint8_t *
ep_rt_valloc0 (size_t buffer_size)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	uint8_t *buffer = (uint8_t *)mono_valloc (NULL, buffer_size, MONO_MMAP_READ | MONO_MMAP_WRITE, MONO_MEM_ACCOUNT_PROFILER);
#else
	uint8_t *buffer = (uint8_t *)ep_rt_mono_func_table_get ()->ep_rt_mono_valloc (NULL, buffer_size, MONO_MMAP_READ | MONO_MMAP_WRITE, MONO_MEM_ACCOUNT_PROFILER);
#endif
	if (buffer)
		memset (buffer, 0, buffer_size);
	return buffer;
}

static
inline
void
ep_rt_vfree (
	uint8_t *buffer,
	size_t buffer_size)
{
	if (buffer)
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
		mono_vfree (buffer, buffer_size, MONO_MEM_ACCOUNT_PROFILER);
#else
		ep_rt_mono_func_table_get ()->ep_rt_mono_vfree (buffer, buffer_size, MONO_MEM_ACCOUNT_PROFILER);
#endif
}

static
inline
uint32_t
ep_rt_temp_path_get (
	ep_char8_t *buffer,
	uint32_t buffer_len)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len > 0);

	const ep_char8_t *path = g_get_tmp_dir ();
	int32_t result = snprintf (buffer, buffer_len, "%s", path);
	if (result <= 0 || result > buffer_len)
		ep_raise_error ();

	if (buffer [result - 1] != G_DIR_SEPARATOR) {
		buffer [result++] = G_DIR_SEPARATOR;
		buffer [result] = '\0';
	}

ep_on_exit:
	return result;

ep_on_error:
	result = 0;
	ep_exit_error_handler ();
}

EP_RT_DEFINE_ARRAY (env_array_utf16, ep_rt_env_array_utf16_t, ep_rt_env_array_utf16_iterator_t, ep_char16_t *)
EP_RT_DEFINE_ARRAY_ITERATOR (env_array_utf16, ep_rt_env_array_utf16_t, ep_rt_env_array_utf16_iterator_t, ep_char16_t *)

static
inline
void
ep_rt_os_environment_get_utf16 (ep_rt_env_array_utf16_t *env_array)
{
	ep_rt_mono_os_environment_get_utf16 (env_array);
}

/*
* Lock.
*/

static
bool
ep_rt_lock_aquire (ep_rt_lock_handle_t *lock)
{
	EP_UNREACHABLE ("Not implemented on Mono.");
}

static
bool
ep_rt_lock_release (ep_rt_lock_handle_t *lock)
{
	EP_UNREACHABLE ("Not implemented on Mono.");
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_lock_requires_lock_held (const ep_rt_lock_handle_t *lock)
{
	EP_UNREACHABLE ("Not implemented on Mono.");
}

static
inline
void
ep_rt_lock_requires_lock_not_held (const ep_rt_lock_handle_t *lock)
{
	EP_UNREACHABLE ("Not implemented on Mono.");
}
#endif

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
bool
ep_rt_spin_lock_aquire (ep_rt_spin_lock_handle_t *spin_lock)
{
	if (spin_lock && spin_lock->lock) {
		mono_coop_mutex_lock (spin_lock->lock);
#ifdef EP_CHECKED_BUILD
		spin_lock->owning_thread_id = ep_rt_mono_native_thread_id_get ();
		spin_lock->lock_is_held = true;
#endif
	}
	return true;
}

static
inline
bool
ep_rt_spin_lock_release (ep_rt_spin_lock_handle_t *spin_lock)
{
	if (spin_lock && spin_lock->lock) {
#ifdef EP_CHECKED_BUILD
		spin_lock->lock_is_held = false;
		spin_lock->owning_thread_id = MONO_UINT_TO_NATIVE_THREAD_ID (0);
#endif
		mono_coop_mutex_unlock (spin_lock->lock);
	}
	return true;
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

static
bool
ep_rt_spin_lock_is_valid (const ep_rt_spin_lock_handle_t *spin_lock)
{
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
	return strcmp ((const char *)str1, (const char *)str2);
}

static
inline
int
ep_rt_utf8_string_compare_ignore_case (
	const ep_char8_t *str1,
	const ep_char8_t *str2)
{
	return g_strcasecmp ((const char *)str1, (const char *)str2);
}

static
inline
bool
ep_rt_utf8_string_is_null_or_empty (const ep_char8_t *str)
{
	if (str == NULL)
		return true;

	while (*str) {
		if (!isspace(*str))
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
	return g_strdup (str);
}

static
inline
ep_char8_t *
ep_rt_utf8_string_strtok (
	ep_char8_t *str,
	const ep_char8_t *delimiter,
	ep_char8_t **context)
{
	return strtok_r (str, delimiter, context);
}

#undef ep_rt_utf8_string_snprintf
#define ep_rt_utf8_string_snprintf( \
	str, \
	str_len, \
	format, ...) \
g_snprintf ((gchar *)str, (gulong)str_len, (const gchar *)format, __VA_ARGS__)

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
ep_char16_t *
ep_rt_utf16_string_dup (const ep_char16_t *str)
{
	size_t str_size = (ep_rt_utf16_string_len (str) + 1) * sizeof (ep_char16_t);
	ep_char16_t *str_dup = (ep_char16_t *)malloc (str_size);
	if (str_dup)
		memcpy (str_dup, str, str_size);
	return str_dup;
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
	if (!mono_lazy_is_initialized (managed_command_line_get_init ())) {
		char *cmd_line = managed_command_line_get ();
		if (!cmd_line)
			return NULL;
		g_free (cmd_line);
	}

	mono_lazy_initialize (managed_command_line_get_init (), managed_command_line_lazy_init);
	EP_ASSERT (*managed_command_line_get_ref () != NULL);
	return *managed_command_line_get_ref ();
}

static
const ep_char8_t *
ep_rt_diagnostics_command_line_get (void)
{
	const ep_char8_t * cmd_line = ep_rt_managed_command_line_get ();

	// Checkout https://github.com/dotnet/coreclr/pull/24433 for more information about this fall back.
	if (cmd_line == NULL)
		cmd_line = ep_rt_os_command_line_get ();

	return cmd_line;
}

/*
 * Thread.
 */
static
inline
void
ep_rt_thread_setup ()
{
	ep_rt_mono_thread_setup (false);
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
	EventPipeThread *thread = ep_rt_thread_get ();
	if (!thread)
		thread = ep_rt_mono_func_table_get ()->ep_rt_mono_thread_get_or_create ();
	return thread;
}

static
inline
ep_rt_thread_handle_t
ep_rt_thread_get_handle (void)
{
	return mono_thread_info_current ();
}

static
inline
ep_rt_thread_id_t
ep_rt_thread_get_id (ep_rt_thread_handle_t thread_handle)
{
	return mono_thread_info_get_tid (thread_handle);
}

static
inline
uint64_t
ep_rt_thread_id_t_to_uint64_t (ep_rt_thread_id_t thread_id)
{
	return (uint64_t)MONO_NATIVE_THREAD_ID_TO_UINT (thread_id);
}

static
inline
ep_rt_thread_id_t
ep_rt_uint64_t_to_thread_id_t (uint64_t thread_id)
{
	return MONO_UINT_TO_NATIVE_THREAD_ID (thread_id);
}

static
inline
bool
ep_rt_thread_has_started (ep_rt_thread_handle_t thread_handle)
{
	return thread_handle == ep_rt_thread_get_handle ();
}

static
inline
ep_rt_thread_activity_id_handle_t
ep_rt_thread_get_activity_id_handle (void)
{
	return ep_rt_thread_get_or_create ();
}

static
inline
const uint8_t *
ep_rt_thread_get_activity_id_cref (ep_rt_thread_activity_id_handle_t activity_id_handle)
{
	EP_UNREACHABLE ("EP_THREAD_INCLUDE_ACTIVITY_ID should have been defined on Mono");
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
	EP_ASSERT (activity_id_handle != NULL);
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	memcpy (activity_id, ep_thread_get_activity_id_cref (activity_id_handle), EP_ACTIVITY_ID_SIZE);
}

static
inline
void
ep_rt_thread_set_activity_id (
	ep_rt_thread_activity_id_handle_t activity_id_handle,
	const uint8_t *activity_id,
	uint32_t activity_id_len)
{
	EP_ASSERT (activity_id_handle != NULL);
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	memcpy (ep_thread_get_activity_id_ref (activity_id_handle), activity_id, EP_ACTIVITY_ID_SIZE);
}

static
inline
int32_t
ep_rt_mono_thread_sleep (uint32_t ms, bool alertable)
{
	gboolean alerted = false;
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	if (alertable)
		return (int32_t)mono_thread_info_sleep (ms, &alerted);
	else
		return (int32_t)mono_thread_info_sleep (ms, NULL);
#else
	if (alertable)
		return (int32_t)ep_rt_mono_func_table_get ()->ep_rt_mono_thread_info_sleep (ms, &alerted);
	else
		return (int32_t)ep_rt_mono_func_table_get ()->ep_rt_mono_thread_info_sleep (ms, NULL);
#endif
}

static
inline
bool
ep_rt_mono_thread_yield (void)
{
#ifdef EP_RT_MONO_USE_STATIC_RUNTIME
	return (mono_thread_info_yield () == TRUE) ? true : false;
#else
	return (ep_rt_mono_func_table_get ()->ep_rt_mono_thread_info_yield () == TRUE) ? true : false;
#endif
}

// See src/coreclr/vm/spinlock.h for details.
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
	#define EP_SLEEP_START_THRESHOLD (5 * 1024)
#else
	#define EP_SLEEP_START_THRESHOLD (32 * 1024)
#endif

#undef EP_YIELD_WHILE
#define EP_YIELD_WHILE(condition) { \
	int32_t __switch_count = 0; \
	while (condition) { \
		if (++__switch_count >= EP_SLEEP_START_THRESHOLD) { \
			ep_rt_mono_thread_sleep (1, false); \
		} \
	} \
	{ \
		ep_rt_mono_thread_yield (); \
	} \
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
	return (uint32_t)mono_atomic_load_i32 ((volatile gint32 *)ptr);
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
	return (uint64_t)mono_atomic_load_i64 ((volatile gint64 *)ptr);
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
int64_t
ep_rt_volatile_load_int64_t (const volatile int64_t *ptr)
{
	return mono_atomic_load_i64 ((volatile gint64 *)ptr);
}

static
inline
int64_t
ep_rt_volatile_load_int64_t_without_barrier (const volatile int64_t *ptr)
{
	return *ptr;
}

static
inline
void
ep_rt_volatile_store_int64_t (
	volatile int64_t *ptr,
	int64_t value)
{
	mono_atomic_store_i64 ((volatile gint64 *)ptr, (gint64)value);
}

static
inline
void
ep_rt_volatile_store_int64_t_without_barrier (
	volatile int64_t *ptr,
	int64_t value)
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
