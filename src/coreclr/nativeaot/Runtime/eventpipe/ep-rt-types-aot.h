// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Implementation of ep-rt-types.h targeting AOT runtime.
#ifndef __EVENTPIPE_RT_TYPES_AOT_H__
#define __EVENTPIPE_RT_TYPES_AOT_H__

#include <eventpipe/ep-rt-config.h>

#include <inttypes.h>

#ifdef ENABLE_PERFTRACING

#include "EmptyContainers.h"

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

template<typename T>
struct _rt_aot_list_internal_t {
    typedef struct SListElem_EP<T> element_type_t;
    typedef class SList_EP<element_type_t> list_type_t;
    list_type_t *list;
};

template<typename T>
struct _rt_aot_queue_internal_t {
    typedef struct SListElem_EP<T> element_type_t;
    typedef class SList_EP<element_type_t> queue_type_t;
    queue_type_t *queue;
};

template<typename T>
struct _rt_aot_array_internal_t {
    typedef T element_type_t;
    typedef class CQuickArrayList_EP<T> array_type_t;
    array_type_t *array;
};

template<typename T>
struct _rt_aot_array_iterator_internal_t {
    typedef typename _rt_aot_array_internal_t<T>::array_type_t array_iterator_type;
    array_iterator_type *array;
    size_t index;
};

typedef struct _rt_aot_table_callbacks_t {
    void (*key_free_func)(void *);
    void (*value_free_func)(void *);
} rt_aot_table_callbacks_t;

template<typename T1, typename T2>
struct _rt_aot_table_default_internal_t {
    typedef class SHash_EP<NoRemoveSHashTraits_EP< MapSHashTraits_EP <T1, T2> > > table_type_t;
    rt_aot_table_callbacks_t callbacks;
    table_type_t *table;
};

template<typename T1, typename T2>
struct _rt_aot_table_remove_internal_t {
    typedef class SHash_EP< MapSHashTraits_EP <T1, T2> > table_type_t;
    rt_aot_table_callbacks_t callbacks;
    table_type_t *table;
};

class EventPipeAotStackHashTraits : public NoRemoveSHashTraits_EP< MapSHashTraits_EP<StackHashKey *, StackHashEntry *> >
{
public:
    typedef typename MapSHashTraits_EP<StackHashKey *, StackHashEntry *>::element_t element_t;
    typedef typename MapSHashTraits_EP<StackHashKey *, StackHashEntry *>::count_t count_t;

    typedef StackHashKey * key_t;

    static key_t GetKey (element_t e)
    {
        extern StackHashKey * ep_stack_hash_entry_get_key (StackHashEntry *);
        return ep_stack_hash_entry_get_key (e.Value ());
    }

    static bool Equals (key_t k1, key_t k2)
    {
        extern bool ep_stack_hash_key_equal (const void *, const void *);
        return ep_stack_hash_key_equal (k1, k2);
    }

    static count_t Hash (key_t k)
    {
        extern uint32_t ep_stack_hash_key_hash (const void *);
        return (count_t)ep_stack_hash_key_hash (k);
    }

    static element_t Null ()
    {
        return element_t (NULL, NULL);
    }

    static bool IsNull (const element_t &e)
    {
        return (e.Key () == NULL|| e.Value () == NULL);
    }
};

template<typename T1>
struct _rt_aot_table_custom_internal_t {
    typedef class SHash_EP<T1> table_type_t;
    rt_aot_table_callbacks_t callbacks;
    table_type_t *table;
};

class CLREventStatic;
struct _rt_aot_event_internal_t {
    CLREventStatic *event;
};

class CrstStatic;
struct _rt_aot_lock_internal_t {
    CrstStatic *lock;
};

class SpinLock;
struct _rt_aot_spin_lock_internal_t {
    SpinLock *lock;
};

/*
 * EventPipeBuffer.
 */

#undef ep_rt_buffer_array_t
typedef struct _rt_aot_array_internal_t<EventPipeBuffer *> ep_rt_buffer_array_t;

#undef ep_rt_buffer_array_iterator_t
typedef struct _rt_aot_array_iterator_internal_t<EventPipeBuffer *> ep_rt_buffer_array_iterator_t;

/*
 * EventPipeBufferList.
 */

#undef ep_rt_buffer_list_array_t
typedef struct _rt_aot_array_internal_t<EventPipeBufferList *> ep_rt_buffer_list_array_t;

#undef ep_rt_buffer_list_array_iterator_t
typedef struct _rt_aot_array_iterator_internal_t<EventPipeBufferList *> ep_rt_buffer_list_array_iterator_t;

/*
 * EventPipeEvent.
 */

#undef ep_rt_event_list_t
typedef struct _rt_aot_list_internal_t<EventPipeEvent *> ep_rt_event_list_t;

#undef ep_rt_event_list_iterator_t
typedef class _rt_aot_list_internal_t<EventPipeEvent *>::list_type_t::Iterator ep_rt_event_list_iterator_t;

/*
 * EventPipeFile.
 */

#undef ep_rt_metadata_labels_hash_map_t
typedef struct _rt_aot_table_remove_internal_t<EventPipeEvent *, uint32_t> ep_rt_metadata_labels_hash_map_t;

#undef ep_rt_metadata_labels_hash_map_iterator_t
typedef class _rt_aot_table_remove_internal_t<EventPipeEvent *, uint32_t>::table_type_t::Iterator ep_rt_metadata_labels_hash_map_iterator_t;

#undef ep_rt_stack_hash_map_t
typedef struct _rt_aot_table_custom_internal_t<EventPipeAotStackHashTraits> ep_rt_stack_hash_map_t;

#undef ep_rt_stack_hash_map_iterator_t
typedef class _rt_aot_table_custom_internal_t<EventPipeAotStackHashTraits>::table_type_t::Iterator ep_rt_stack_hash_map_iterator_t;

/*
 * EventPipeProvider.
 */

#undef ep_rt_provider_list_t
typedef struct _rt_aot_list_internal_t<EventPipeProvider *> ep_rt_provider_list_t;

#undef ep_rt_provider_list_iterator_t
typedef class _rt_aot_list_internal_t<EventPipeProvider *>::list_type_t::Iterator ep_rt_provider_list_iterator_t;

#undef ep_rt_provider_callback_data_queue_t
typedef struct _rt_aot_queue_internal_t<EventPipeProviderCallbackData *> ep_rt_provider_callback_data_queue_t;

/*
 * EventPipeProviderConfiguration.
 */

#undef ep_rt_provider_config_array_t
typedef struct _rt_aot_array_internal_t<EventPipeProviderConfiguration> ep_rt_provider_config_array_t;

#undef ep_rt_provider_config_array_iterator_t
typedef struct _rt_aot_array_iterator_internal_t<EventPipeProviderConfiguration> ep_rt_provider_config_array_iterator_t;

/*
 * EventPipeSessionProvider.
 */

#undef ep_rt_session_provider_list_t
typedef struct _rt_aot_list_internal_t<EventPipeSessionProvider *> ep_rt_session_provider_list_t;

#undef ep_rt_session_provider_list_iterator_t
typedef class _rt_aot_list_internal_t<EventPipeSessionProvider *>::list_type_t::Iterator ep_rt_session_provider_list_iterator_t;

/*
 * EventPipeSequencePoint.
 */

#undef ep_rt_sequence_point_list_t
typedef struct _rt_aot_list_internal_t<EventPipeSequencePoint *> ep_rt_sequence_point_list_t;

#undef ep_rt_sequence_point_list_iterator_t
typedef class _rt_aot_list_internal_t<EventPipeSequencePoint *>::list_type_t::Iterator ep_rt_sequence_point_list_iterator_t;

/*
 * EventPipeThread.
 */

#undef ep_rt_thread_list_t
typedef struct _rt_aot_list_internal_t<EventPipeThread *> ep_rt_thread_list_t;

#undef ep_rt_thread_list_iterator_t
typedef class _rt_aot_list_internal_t<EventPipeThread *>::list_type_t::Iterator ep_rt_thread_list_iterator_t;

#undef ep_rt_thread_array_t
typedef struct _rt_aot_array_internal_t<EventPipeThread *> ep_rt_thread_array_t;

#undef ep_rt_thread_array_iterator_t
typedef struct _rt_aot_array_iterator_internal_t<EventPipeThread *> ep_rt_thread_array_iterator_t;

/*
 * EventPipeThreadSessionState.
 */

#undef ep_rt_thread_session_state_list_t
typedef struct _rt_aot_list_internal_t<EventPipeThreadSessionState *> ep_rt_thread_session_state_list_t;

#undef ep_rt_thread_session_state_list_iterator_t
typedef class _rt_aot_list_internal_t<EventPipeThreadSessionState *>::list_type_t::Iterator ep_rt_thread_session_state_list_iterator_t;

#undef ep_rt_thread_session_state_array_t
typedef struct _rt_aot_array_internal_t<EventPipeThreadSessionState *> ep_rt_thread_session_state_array_t;

#undef ep_rt_thread_session_state_array_iterator_t
typedef struct _rt_aot_array_iterator_internal_t<EventPipeThreadSessionState *> ep_rt_thread_session_state_array_iterator_t;

/*
 * EventPipe.
 */

#undef ep_rt_session_id_array_t
typedef struct _rt_aot_array_internal_t<EventPipeSessionID> ep_rt_session_id_array_t;

#undef ep_rt_session_id_array_iterator_t
typedef struct _rt_aot_array_iterator_internal_t<EventPipeSessionID> ep_rt_session_id_array_iterator_t;

#undef ep_rt_method_desc_t
typedef class MethodDesc ep_rt_method_desc_t;

#undef ep_rt_execution_checkpoint_array_t
typedef struct _rt_aot_array_internal_t<EventPipeExecutionCheckpoint *> ep_rt_execution_checkpoint_array_t;

#undef ep_rt_execution_checkpoint_array_iterator_t
typedef struct _rt_aot_array_iterator_internal_t<EventPipeExecutionCheckpoint *> ep_rt_execution_checkpoint_array_iterator_t;

/*
 * PAL.
 */

#undef ep_rt_env_array_utf16_t
typedef struct _rt_aot_array_internal_t<ep_char16_t *> ep_rt_env_array_utf16_t;

#undef ep_rt_env_array_utf16_iterator_t
typedef struct _rt_aot_array_iterator_internal_t<ep_char16_t *> ep_rt_env_array_utf16_iterator_t;

#undef ep_rt_file_handle_t
typedef class CFileStream * ep_rt_file_handle_t;

#undef ep_rt_wait_event_handle_t
typedef struct _rt_aot_event_internal_t ep_rt_wait_event_handle_t;

#undef ep_rt_lock_handle_t
typedef struct _rt_aot_lock_internal_t ep_rt_lock_handle_t;

#undef ep_rt_spin_lock_handle_t
typedef _rt_aot_spin_lock_internal_t ep_rt_spin_lock_handle_t;

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

/*
 * ThreadSequenceNumberMap.
 */

#undef ep_rt_thread_sequence_number_hash_map_t
typedef struct _rt_aot_table_remove_internal_t<EventPipeThreadSessionState *, uint32_t> ep_rt_thread_sequence_number_hash_map_t;

#undef ep_rt_thread_sequence_number_hash_map_iterator_t
typedef class _rt_aot_table_remove_internal_t<EventPipeThreadSessionState *, uint32_t>::table_type_t::Iterator ep_rt_thread_sequence_number_hash_map_iterator_t;

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_TYPES_AOT_H__ */
