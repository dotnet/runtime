// Implementation of ep-rt-types.h targeting Mono runtime.
#ifndef __EVENTPIPE_RT_TYPES_MONO_H__
#define __EVENTPIPE_RT_TYPES_MONO_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-rt-config.h>
#include <glib.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/os-event.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/checked-build.h>

#ifdef ENABLE_CHECKED_BUILD
#define EP_CHECKED_BUILD
#endif

#undef EP_ASSERT
#define EP_ASSERT(expr) g_assert_checked(expr)
//#define EP_ASSERT(expr) g_assert(expr)

#undef EP_UNREACHABLE
#define EP_UNREACHABLE(msg) g_assert_not_reached()

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

struct _rt_mono_queue_internal_t {
	GQueue *queue;
};

struct _rt_mono_array_internal_t {
	GArray *array;
};

struct _rt_mono_array_iterator_internal_t {
	GArray *array;
	int32_t index;
};

#ifdef EP_RT_USE_CUSTOM_HASH_MAP_CALLBACKS
typedef GHashFunc ep_rt_hash_map_hash_callback_t;
typedef GEqualFunc ep_rt_hash_map_equal_callback_t;
#endif

struct _rt_mono_table_internal_t {
	GHashTable *table;
};

struct _rt_mono_table_iterator_internal_t {
	GHashTableIter iterator;
	gpointer key;
	gpointer value;
	bool end;
};

struct _rt_mono_event_internal_t {
	gpointer event;
};

struct _rt_mono_lock_internal_t {
	MonoCoopMutex *lock;
#ifdef EP_CHECKED_BUILD
	volatile MonoNativeThreadId owning_thread_id;
#endif
};

/*
 * EventPipeBuffer.
 */

#undef ep_rt_buffer_array_t
typedef struct _rt_mono_array_internal_t ep_rt_buffer_array_t;

#undef ep_rt_buffer_array_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ep_rt_buffer_array_iterator_t;

/*
 * EventPipeBufferList.
 */

#undef ep_rt_buffer_list_array_t
typedef struct _rt_mono_array_internal_t ep_rt_buffer_list_array_t;

#undef ep_rt_buffer_list_array_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ep_rt_buffer_list_array_iterator_t;

/*
 * EventPipeEvent.
 */

#undef ep_rt_event_list_t
typedef struct _rt_mono_list_internal_t ep_rt_event_list_t;

#undef ep_rt_event_list_iterator_t
typedef struct _rt_mono_list_iterator_internal_t ep_rt_event_list_iterator_t;

/*
 * EventPipeFile.
 */

#undef ep_rt_metadata_labels_hash_map_t
typedef struct _rt_mono_table_internal_t ep_rt_metadata_labels_hash_map_t;

#undef ep_rt_metadata_labels_hash_map_iterator_t
typedef struct _rt_mono_iterator_table_internal_t ep_rt_metadata_labels_hash_map_iterator_t;

#undef ep_rt_stack_hash_map_t
typedef struct _rt_mono_table_internal_t ep_rt_stack_hash_map_t;

#undef ep_rt_stack_hash_map_iterator_t
typedef struct _rt_mono_table_iterator_internal_t ep_rt_stack_hash_map_iterator_t;

/*
 * EventPipeProvider.
 */

#undef ep_rt_provider_list_t
typedef struct _rt_mono_list_internal_t ep_rt_provider_list_t;

#undef ep_rt_provider_list_iterator_t
typedef struct _rt_mono_list_iterator_internal_t ep_rt_provider_list_iterator_t;

#undef ep_rt_provider_callback_data_queue_t
typedef struct _rt_mono_queue_internal_t ep_rt_provider_callback_data_queue_t;

/*
 * EventPipeProviderConfiguration.
 */

#undef ep_rt_provider_config_array_t
typedef struct _rt_mono_array_internal_t ep_rt_provider_config_array_t;

#undef ep_rt_provider_config_array_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ep_rt_provider_config_array_iterator_t;

/*
 * EventPipeSessionProvider.
 */

#undef ep_rt_session_provider_list_t
typedef struct _rt_mono_list_internal_t ep_rt_session_provider_list_t;

#undef ep_rt_session_provider_list_iterator_t
typedef struct _rt_mono_list_iterator_internal_t ep_rt_session_provider_list_iterator_t;

/*
 * EventPipeSequencePoint.
 */

#undef ep_rt_sequence_point_list_t
typedef struct _rt_mono_list_internal_t ep_rt_sequence_point_list_t;

#undef ep_rt_sequence_point_list_iterator_t
typedef struct _rt_mono_list_iterator_internal_t ep_rt_sequence_point_list_iterator_t;

/*
 * EventPipeThread.
 */

#undef ep_rt_thread_list_t
typedef struct _rt_mono_list_internal_t ep_rt_thread_list_t;

#undef ep_rt_thread_list_iterator_t
typedef struct _rt_mono_list_iterator_internal_t ep_rt_thread_list_iterator_t;

#undef ep_rt_thread_array_t
typedef struct _rt_mono_array_internal_t ep_rt_thread_array_t;

#undef ep_rt_thread_array_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ep_rt_thread_array_iterator_t;

/*
 * EventPipeThreadSessionState.
 */

#undef ep_rt_thread_session_state_list_t
typedef struct _rt_mono_list_internal_t ep_rt_thread_session_state_list_t;

#undef ep_rt_thread_session_state_list_iterator_t
typedef struct _rt_mono_list_iterator_internal_t ep_rt_thread_session_state_list_iterator_t;

#undef ep_rt_thread_session_state_array_t
typedef struct _rt_mono_array_internal_t ep_rt_thread_session_state_array_t;

#undef ep_rt_thread_session_state_array_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ep_rt_thread_session_state_array_iterator_t;

/*
 * EventPipe.
 */

#undef ep_rt_session_id_array_t
typedef struct _rt_mono_array_internal_t ep_rt_session_id_array_t;

#undef ep_rt_session_id_array_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ep_rt_session_id_array_iterator_t;

#undef ep_rt_method_desc_t
typedef MonoMethod ep_rt_method_desc_t;

#undef ep_rt_execution_checkpoint_array_t
typedef struct _rt_mono_array_internal_t ep_rt_execution_checkpoint_array_t;

#undef ep_rt_execution_checkpoint_array_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ep_rt_execution_checkpoint_array_iterator_t;

/*
 * PAL.
 */

#undef ep_rt_env_array_utf16_t
typedef struct _rt_mono_array_internal_t ep_rt_env_array_utf16_t;

#undef ep_rt_env_array_utf16_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ep_rt_env_array_utf16_iterator_t;

#undef ep_rt_file_handle_t
typedef gpointer ep_rt_file_handle_t;

#undef ep_rt_wait_event_handle_t
typedef struct _rt_mono_event_internal_t ep_rt_wait_event_handle_t;

#undef ep_rt_lock_handle_t
typedef struct _rt_mono_lock_internal_t ep_rt_lock_handle_t;

#undef ep_rt_spin_lock_handle_t
typedef ep_rt_lock_handle_t ep_rt_spin_lock_handle_t;

/*
 * Thread.
 */

#undef ep_rt_thread_handle_t
typedef THREAD_INFO_TYPE * ep_rt_thread_handle_t;

#undef ep_rt_thread_activity_id_handle_t
typedef EventPipeThread * ep_rt_thread_activity_id_handle_t;

#undef ep_rt_thread_id_t
typedef MonoNativeThreadId ep_rt_thread_id_t;

#undef ep_rt_thread_start_func
typedef MonoThreadStart ep_rt_thread_start_func;

#undef ep_rt_thread_start_func_return_t
typedef mono_thread_start_return_t ep_rt_thread_start_func_return_t;

#undef ep_rt_thread_params_t
typedef struct _rt_mono_thread_params_t {
	ep_rt_thread_handle_t thread;
	EventPipeThreadType thread_type;
	ep_rt_thread_start_func thread_func;
	void *thread_params;
} ep_rt_thread_params_t;

/*
 * ThreadSequenceNumberMap.
 */

#undef ep_rt_thread_sequence_number_hash_map_t
typedef struct _rt_mono_table_internal_t ep_rt_thread_sequence_number_hash_map_t;

#undef ep_rt_thread_sequence_number_hash_map_iterator_t
typedef struct _rt_mono_table_iterator_internal_t ep_rt_thread_sequence_number_hash_map_iterator_t;

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_TYPES_MONO_H__ */
