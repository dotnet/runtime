#ifndef __EVENTPIPE_SESSION_H__
#define __EVENTPIPE_SESSION_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"
#include "ep-thread.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_SESSION_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeSession.
 */

//! Encapsulates an EventPipe session information and memory management.
#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_SESSION_GETTER_SETTER)
struct _EventPipeSession {
#else
struct _EventPipeSession_Internal {
#endif
	// When the session is of IPC or FILE stream type, this becomes a reference to the streaming thread.
	ep_rt_thread_handle_t streaming_thread;
	// Event object used to signal Disable that the streaming thread is done.
	ep_rt_wait_event_handle_t rt_thread_shutdown_event;
	// The set of configurations for each provider in the session.
	EventPipeSessionProviderList *providers;
	// Session buffer manager.
	EventPipeBufferManager *buffer_manager;
	// Object used to flush event data (File, IPC stream, etc.).
	EventPipeFile *file;
	// For synchoronous sessions.
	EventPipeSessionSynchronousCallback synchronous_callback;
	// Start date and time in UTC.
	ep_system_timestamp_t session_start_time;
	// Start timestamp.
	ep_timestamp_t session_start_timestamp;
	uint32_t index;
	// True if rundown is enabled.
	volatile uint32_t rundown_enabled;
	// Data members used when an streaming thread is used.
	volatile uint32_t streaming_enabled;
	// The type of the session.
	// This determines behavior within the system (e.g. policies around which events to drop, etc.)
	EventPipeSessionType session_type;
	// For file/IPC sessions this controls the format emitted. For in-proc EventListener it is
	// irrelevant.
	EventPipeSerializationFormat format;
	// For determininig if a particular session needs rundown events.
	bool rundown_requested;
	// Note - access to this field is NOT synchronized
	// This functionality is a workaround because we couldn't safely enable/disable the session where we wanted to due to lock-leveling.
	// we expect to remove it in the future once that limitation is resolved other scenarios are discouraged from using this given that
	// we plan to make it go away
	bool paused;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_SESSION_GETTER_SETTER)
struct _EventPipeSession {
	uint8_t _internal [sizeof (struct _EventPipeSession_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeSession *, session, uint32_t, index)
EP_DEFINE_GETTER(EventPipeSession *, session, EventPipeSessionProviderList *, providers)
EP_DEFINE_GETTER(EventPipeSession *, session, EventPipeBufferManager *, buffer_manager)
EP_DEFINE_GETTER_REF(EventPipeSession *, session, volatile uint32_t *, rundown_enabled)
EP_DEFINE_GETTER(EventPipeSession *, session, bool, rundown_requested)
EP_DEFINE_GETTER(EventPipeSession *, session, ep_timestamp_t, session_start_time)
EP_DEFINE_GETTER(EventPipeSession *, session, ep_timestamp_t, session_start_timestamp)
EP_DEFINE_GETTER(EventPipeSession *, session, EventPipeFile *, file)

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
	EventPipeSessionSynchronousCallback sync_callback);

void
ep_session_free (EventPipeSession *session);

// _Requires_lock_held (ep)
EventPipeSessionProvider *
ep_session_get_session_provider (
	const EventPipeSession *session,
	const EventPipeProvider *provider);

// _Requires_lock_held (ep)
bool
ep_session_enable_rundown (EventPipeSession *session);

// _Requires_lock_held (ep)
void
ep_session_execute_rundown (EventPipeSession *session);

// Force all in-progress writes to either finish or cancel
// This is required to ensure we can safely flush and delete the buffers
// _Requires_lock_held (ep)
void
ep_session_suspend_write_event (EventPipeSession *session);

// Write a sequence point into the output stream synchronously.
void
ep_session_write_sequence_point_unbuffered (EventPipeSession *session);

// Enable a session in the event pipe.
// MUST be called AFTER sending the IPC response
// Side effects:
// - sends file header information for nettrace format
// - turns on streaming thread which flushes events to stream
// _Requires_lock_held (ep)
void
ep_session_start_streaming (EventPipeSession *session);

// Determine if the session is valid or not.
// Invalid sessions can be detected before they are enabled.
bool
ep_session_is_valid (const EventPipeSession *session);

bool
ep_session_add_session_provider (
	EventPipeSession *session,
	EventPipeSessionProvider *session_provider);

// Disable a session in the event pipe.
// side-effects: writes all buffers to stream/file
void
ep_session_disable (EventPipeSession *session);

bool
ep_session_write_all_buffers_to_file (
	EventPipeSession *session,
	bool *events_written);

// If a session is non-synchronous (i.e. a file, pipe, etc) WriteEvent will
// put the event in a buffer and return as quick as possible. If a session is
// synchronous (callback to the profiler) then this method will block until the
// profiler is done parsing and reacting to it.
bool
ep_session_write_event (
	EventPipeSession *session,
	ep_rt_thread_handle_t thread,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	ep_rt_thread_handle_t event_thread,
	EventPipeStackContents *stack);

EventPipeEventInstance *
ep_session_get_next_event (EventPipeSession *session);

ep_rt_wait_event_handle_t *
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
ep_session_get_streaming_enabled (const EventPipeSession *session);

void
ep_session_set_streaming_enabled (
	EventPipeSession *session,
	bool enabled);

// Please do not use this function, see EventPipeSession paused field for more information.
void
ep_session_pause (EventPipeSession *session);

// Please do not use this function, see EventPipeSession paused field for more information.
void
ep_session_resume (EventPipeSession *session);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_SESSION_H__ */
