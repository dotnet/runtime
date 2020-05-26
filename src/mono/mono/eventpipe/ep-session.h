#ifndef __EVENTPIPE_SESSION_H__
#define __EVENTPIPE_SESSION_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

/*
 * EventPipeSession.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeSession {
#else
struct _EventPipeSession_Internal {
#endif
	uint32_t index;
	EventPipeSessionProviderList *providers;
	EventPipeBufferManager *buffer_manager;
	volatile uint32_t rundown_enabled;
	EventPipeSessionType session_type;
	EventPipeSerializationFormat format;
	bool rundown_requested;
	uint64_t session_start_time;
	uint64_t session_start_timestamp;
	EventPipeFile *file;
	volatile uint32_t ipc_streaming_enabled;
	EventPipeThread ipc_streaming_thread;
	ep_rt_wait_event_handle_t rt_thread_shutdown_event;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeSession {
	uint8_t _internal [sizeof (struct _EventPipeSession_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeSession *, session, uint32_t, index)
EP_DEFINE_GETTER(EventPipeSession *, session, EventPipeSessionProviderList *, providers)
EP_DEFINE_GETTER(EventPipeSession *, session, EventPipeBufferManager *, buffer_manager)
EP_DEFINE_GETTER_REF(EventPipeSession *, session, volatile uint32_t *, rundown_enabled)
EP_DEFINE_GETTER(EventPipeSession *, session, EventPipeSessionType, session_type)
EP_DEFINE_GETTER(EventPipeSession *, session, EventPipeSerializationFormat, format)
EP_DEFINE_GETTER(EventPipeSession *, session, bool, rundown_requested)
EP_DEFINE_GETTER(EventPipeSession *, session, uint64_t, session_start_time)
EP_DEFINE_GETTER(EventPipeSession *, session, uint64_t, session_start_timestamp)
EP_DEFINE_GETTER(EventPipeSession *, session, EventPipeFile *, file)
EP_DEFINE_GETTER_REF(EventPipeSession *, session, volatile uint32_t *, ipc_streaming_enabled)
EP_DEFINE_GETTER_REF(EventPipeSession *, session, EventPipeThread *, ipc_streaming_thread)
EP_DEFINE_GETTER_REF(EventPipeSession *, session, ep_rt_wait_event_handle_t *, rt_thread_shutdown_event)

EventPipeSession *
ep_session_alloc (
	uint32_t index,
	const ep_char8_t *output_path,
	IpcStream *stream,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	bool rundown_requested,
	uint32_t circular_buffer_size_in_mb,
	const EventPipeProviderConfiguration *providers,
	uint32_t providers_len,
	bool rundown_enabled);

void
ep_session_free (EventPipeSession *session);

EventPipeSessionProvider *
ep_session_get_session_provider_lock_held (
	const EventPipeSession *session,
	const EventPipeProvider *provider);

void
ep_session_enable_rundown_lock_held (EventPipeSession *session);

void
ep_session_execute_rundown_lock_held (EventPipeSession *session);

void
ep_session_suspend_write_event_lock_held (EventPipeSession *session);

void
ep_session_write_sequence_point_unbuffered_lock_held (EventPipeSession *session);

void
ep_session_start_streaming_lock_held (EventPipeSession *session);

bool
ep_session_is_valid (const EventPipeSession *session);

void
ep_session_add_session_provider (
	EventPipeSession *session,
	EventPipeSessionProvider *session_provider);

void
ep_session_disable (EventPipeSession *session);

bool
ep_session_write_all_buffers_to_file (EventPipeSession *session, bool *events_written);

EventPipeEventInstance *
ep_session_get_next_event (EventPipeSession *session);

EventPipeWaitHandle
ep_session_get_wait_event (EventPipeSession *session);

uint64_t
ep_session_get_mask (const EventPipeSession *session);

bool
ep_session_get_rundown_enabled (const EventPipeSession *session);

void
ep_session_set_rundown_enabled (
	EventPipeSession *session,
	bool enabled);

bool
ep_session_get_ipc_streaming_enabled (const EventPipeSession *session);

void
ep_session_set_ipc_streaming_enabled (
	EventPipeSession *session,
	bool enabled);

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_SESSION_H__ **/
