// Implementation of ep-rt-types.h targeting Mono runtime.
#ifndef __EVENTPIPE_RT_TYPES_MONO_H__
#define __EVENTPIPE_RT_TYPES_MONO_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include <glib.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/os-event.h>
#include <mono/utils/mono-coop-mutex.h>

//#ifdef ENABLE_CHECKED_BUILD
#define EP_CHECKED_BUILD
//#endif

#undef EP_ASSERT
//#define EP_ASSERT(expr) g_assert_checked(expr)
#define EP_ASSERT(expr) g_assert(expr)

#undef EP_LIKELY
#define EP_LIKELY(expr) G_LIKELY(expr)

#undef EP_UNLIKELY
#define EP_UNLIKELY(expr) G_UNLIKELY(expr)

struct _rt_mono_list_internal_t {
	GSList *list;
};

struct _rt_mono_list_iterator_internal_t {
	GSList *iterator;
};

typedef struct _rt_mono_list_internal_t ep_rt_provider_list_t;
typedef struct _rt_mono_list_iterator_internal_t ep_rt_provider_list_iterator_t;

typedef struct _rt_mono_list_internal_t ep_rt_event_list_t;
typedef struct _rt_mono_list_iterator_internal_t ep_rt_event_list_iterator_t;

typedef struct _rt_mono_list_internal_t ep_rt_session_provider_list_t;
typedef struct _rt_mono_list_iterator_internal_t ep_rt_session_provider_list_iterator_t;

struct _rt_mono_queue_internal_t {
	GQueue *queue;
};

typedef struct _rt_mono_queue_internal_t ep_rt_provider_callback_data_queue_t;
typedef struct _rt_mono_queue_iterator_internal_t ep_rt_provider_callback_data_queue_iterator_t;

struct _rt_mono_table_internal_t {
	GHashTable *table;
	uint32_t count;
};

struct _rt_mono_table_iterator_internal_t {
	GHashTableIter iterator;
	gpointer key;
	gpointer value;
	bool end;
};

typedef struct _rt_mono_table_internal_t ep_rt_metadata_labels_hash_map_t;
typedef struct _rt_mono_iterator_table_internal_t ep_rt_metadata_labels_hash_map_iterator_t;

typedef struct _rt_mono_table_internal_t ep_rt_stack_hash_map_t;
typedef struct _rt_mono_table_iterator_internal_t ep_rt_stack_hash_map_iterator_t;

typedef struct _rt_mono_table_internal_t ep_rt_thread_sequence_number_hash_map_t;
typedef struct _rt_mono_table_iterator_internal_t ep_rt_thread_sequence_number_hash_map_iterator_t;

typedef MonoThreadHandle ep_rt_thread_handle_t;
typedef gpointer ep_rt_file_handle_t;
typedef gpointer ep_rt_ipc_handle_t;
typedef MonoMethod ep_rt_method_desc_t;

struct _rt_mono_event_internal_t {
	MonoOSEvent *event;
};

typedef struct _rt_mono_event_internal_t ep_rt_wait_event_handle_t;

struct _rt_mono_lock_internal_t {
	MonoCoopMutex *lock;
#ifdef EP_CHECKED_BUILD
	MonoNativeThreadId owning_thread_id;
	bool lock_is_held;
#endif
};

typedef struct _rt_mono_lock_internal_t ep_rt_lock_handle_t;
typedef ep_rt_lock_handle_t ep_rt_spin_lock_handle_t;

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_TYPES_MONO_H__ */
