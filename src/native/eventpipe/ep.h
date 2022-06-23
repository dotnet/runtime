#ifndef __EVENTPIPE_H__
#define __EVENTPIPE_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"
#include "ep-stack-contents.h"
#include "ep-rt.h"

extern volatile EventPipeState _ep_state;
extern volatile EventPipeSession *_ep_sessions [EP_MAX_NUMBER_OF_SESSIONS];
extern volatile uint32_t _ep_number_of_sessions;
extern volatile uint64_t _ep_allow_write;

/*
 * Globals and volatile access functions.
 */

static
inline
EventPipeState
ep_volatile_load_eventpipe_state (void)
{
	return (EventPipeState)ep_rt_volatile_load_uint32_t ((const volatile uint32_t *)&_ep_state);
}

static
inline
EventPipeState
ep_volatile_load_eventpipe_state_without_barrier (void)
{
	return (EventPipeState)ep_rt_volatile_load_uint32_t_without_barrier ((const volatile uint32_t *)&_ep_state);
}

static
inline
void
ep_volatile_store_eventpipe_state (EventPipeState state)
{
	ep_rt_volatile_store_uint32_t ((volatile uint32_t *)&_ep_state, state);
}

static
inline
EventPipeSession *
ep_volatile_load_session (size_t index)
{
	return (EventPipeSession *)ep_rt_volatile_load_ptr ((volatile void **)(&_ep_sessions [index]));
}

static
inline
EventPipeSession *
ep_volatile_load_session_without_barrier (size_t index)
{
	return (EventPipeSession *)ep_rt_volatile_load_ptr_without_barrier ((volatile void **)(&_ep_sessions [index]));
}

static
inline
void
ep_volatile_store_session (size_t index, EventPipeSession *session)
{
	ep_rt_volatile_store_ptr ((volatile void **)(&_ep_sessions [index]), session);
}

static
inline
uint32_t
ep_volatile_load_number_of_sessions (void)
{
	return ep_rt_volatile_load_uint32_t (&_ep_number_of_sessions);
}

static
inline
uint32_t
ep_volatile_load_number_of_sessions_without_barrier (void)
{
	return ep_rt_volatile_load_uint32_t_without_barrier (&_ep_number_of_sessions);
}

static
inline
void
ep_volatile_store_number_of_sessions (uint32_t number_of_sessions)
{
	ep_rt_volatile_store_uint32_t (&_ep_number_of_sessions, number_of_sessions);
}

static
inline
uint64_t
ep_volatile_load_allow_write (void)
{
	return ep_rt_volatile_load_uint64_t (&_ep_allow_write);
}

static
inline
void
ep_volatile_store_allow_write (uint64_t allow_write)
{
	ep_rt_volatile_store_uint64_t (&_ep_allow_write, allow_write);
}

/*
 * EventPipe.
 */

#ifdef EP_CHECKED_BUILD
void
ep_requires_lock_held (void);

void
ep_requires_lock_not_held (void);
#else
#define ep_requires_lock_held()
#define ep_requires_lock_not_held()
#endif

EventPipeSessionID
ep_enable (
	const ep_char8_t *output_path,
	uint32_t circular_buffer_size_in_mb,
	const EventPipeProviderConfiguration *providers,
	uint32_t providers_len,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	bool rundown_requested,
	IpcStream *stream,
	EventPipeSessionSynchronousCallback sync_callback,
	void *callback_additional_data);

EventPipeSessionID
ep_enable_2 (
	const ep_char8_t *output_path,
	uint32_t circular_buffer_size_in_mb,
	const ep_char8_t *providers,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	bool rundown_requested,
	IpcStream *stream,
	EventPipeSessionSynchronousCallback sync_callback,
	void *callback_additional_data);

void
ep_disable (EventPipeSessionID id);

EventPipeSession *
ep_get_session (EventPipeSessionID session_id);

bool
ep_is_session_enabled (EventPipeSessionID session_id);

void
ep_start_streaming (EventPipeSessionID session_id);

bool
ep_enabled (void);

EventPipeProvider *
ep_create_provider (
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	EventPipeCallbackDataFree callback_data_free_func,
	void *callback_data);

void
ep_delete_provider (EventPipeProvider *provider);

EventPipeProvider *
ep_get_provider (const ep_char8_t *provider_name);

bool
ep_add_provider_to_session (
	EventPipeSessionProvider *provider,
	EventPipeSession *session);

void
ep_init (void);

void
ep_finish_init (void);

void
ep_shutdown (void);

EventPipeEventMetadataEvent *
ep_build_event_metadata_event (
	EventPipeEventInstance *event_instance,
	uint32_t metadata_id);

void
ep_write_event (
	EventPipeEvent *ep_event,
	uint8_t *data,
	uint32_t data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

void
ep_write_event_2 (
	EventPipeEvent *ep_event,
	EventData *event_data,
	uint32_t event_data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

void
ep_write_sample_profile_event (
	ep_rt_thread_handle_t sampling_thread,
	EventPipeEvent *ep_event,
	ep_rt_thread_handle_t target_thread,
	EventPipeStackContents *stack,
	uint8_t *event_data,
	uint32_t event_data_len);

EventPipeEventInstance *
ep_get_next_event (EventPipeSessionID session_id);

EventPipeWaitHandle
ep_get_wait_handle (EventPipeSessionID session_id);

static
inline
bool
ep_walk_managed_stack_for_current_thread (EventPipeStackContents *stack_contents)
{
	EP_ASSERT (stack_contents != NULL);

	ep_stack_contents_reset (stack_contents);

	ep_rt_thread_handle_t thread = ep_rt_thread_get_handle ();
	return (thread != NULL) ? ep_rt_walk_managed_stack_for_thread (thread, stack_contents) : false;
}

bool
ep_add_rundown_execution_checkpoint (
	const ep_char8_t *name,
	ep_timestamp_t timestamp);

/*
 * EventPipePerf.
 */

static
inline
ep_timestamp_t
ep_perf_timestamp_get (void)
{
	return (ep_timestamp_t)ep_rt_perf_counter_query ();
}

static
inline
int64_t
ep_perf_frequency_query (void)
{
	return ep_rt_perf_frequency_query ();
}

/*
 * EventPipeSystemTime.
 */

static
inline
ep_system_timestamp_t
ep_system_timestamp_get (void)
{
	return (ep_system_timestamp_t)ep_rt_system_timestamp_get ();
}

static
inline
void
ep_system_time_get (EventPipeSystemTime *system_time)
{
	ep_rt_system_time_get (system_time);
}

/*
 * EventPipeIpcStreamFactoryCallback.
 */

void
ep_ipc_stream_factory_callback_set (EventPipeIpcStreamFactorySuspendedPortsCallback suspended_ports_callback);

#else /* ENABLE_PERFTRACING */

static
inline
void
ep_init (void)
{
	;
}

static
inline
void
ep_finish_init (void)
{
	;
}

static
inline
void
ep_shutdown (void)
{
	;
}

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_H__ */
