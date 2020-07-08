#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_SESSION_GETTER_SETTER
#include "ep.h"
#include "ep-buffer-manager.h"
#include "ep-config.h"
#include "ep-event.h"
#include "ep-file.h"
#include "ep-session.h"
#include "ep-rt.h"

/*
 * Forward declares of all static functions.
 */

static
void
session_disable_ipc_streaming_thread (EventPipeSession *session);

// _Requires_lock_held (ep)
static
void
session_create_ipc_streaming_thread (EventPipeSession *session);

/*
 * EventPipeSession.
 */

static
void
session_create_ipc_streaming_thread (EventPipeSession *session)
{
	//TODO: Implement.
}

static
void
session_disable_ipc_streaming_thread (EventPipeSession *session)
{
	EP_ASSERT (session->session_type == EP_SESSION_TYPE_IPCSTREAM);
	EP_ASSERT (ep_session_get_ipc_streaming_enabled (session));

	EP_ASSERT (!ep_rt_process_detach ());

	// The IPC streaming thread will watch this value and exit
	// when profiling is disabled.
	ep_session_set_ipc_streaming_enabled (session, false);

	// Thread could be waiting on the event that there is new data to read.
	ep_rt_wait_event_set (ep_buffer_manager_get_rt_wait_event_ref (session->buffer_manager));

	// Wait for the sampling thread to clean itself up.
	ep_rt_wait_event_handle_t *rt_thread_shutdown_event = &session->rt_thread_shutdown_event;
	ep_rt_wait_event_wait (rt_thread_shutdown_event, EP_INFINITE_WAIT, false /* bAlertable */);
	ep_rt_wait_event_free (rt_thread_shutdown_event);
}

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
	bool rundown_enabled)
{
	EP_ASSERT (index < EP_MAX_NUMBER_OF_SESSIONS);
	EP_ASSERT (format < EP_SERIALIZATION_FORMAT_COUNT);
	EP_ASSERT (circular_buffer_size_in_mb > 0);
	EP_ASSERT (providers_len > 0);
	EP_ASSERT (providers != NULL);

	ep_requires_lock_held ();

	FileStreamWriter *file_stream_writer = NULL;
	IpcStreamWriter *ipc_stream_writer = NULL;
	size_t sequence_point_alloc_budget = 0;

	EventPipeSession *instance = ep_rt_object_alloc (EventPipeSession);
	ep_raise_error_if_nok (instance != NULL);

	instance->providers = ep_session_provider_list_alloc (providers, providers_len);
	ep_raise_error_if_nok (instance->providers != NULL);

	instance->index = index;
	instance->rundown_enabled = rundown_enabled ? 1 : 0;
	instance->session_type = session_type;
	instance->format = format;
	instance->rundown_requested = rundown_requested;

	// Hard coded 10MB for now, we'll probably want to make
	// this configurable later.
	if (instance->session_type != EP_SESSION_TYPE_LISTENER && instance->format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4) {
		sequence_point_alloc_budget = 10 * 1024 * 1024;
	}

	instance->buffer_manager = ep_buffer_manager_alloc (instance, ((size_t)circular_buffer_size_in_mb) << 20, sequence_point_alloc_budget);
	ep_raise_error_if_nok (instance->buffer_manager != NULL);

	// Create the event pipe file.
	// A NULL output path means that we should not write the results to a file.
	// This is used in the EventListener case.
	switch (session_type) {
	case EP_SESSION_TYPE_FILE :
		if (output_path) {
			file_stream_writer = ep_file_stream_writer_alloc (output_path);
			instance->file = ep_file_alloc (ep_file_stream_writer_get_stream_writer_ref (file_stream_writer), format);
			ep_raise_error_if_nok (instance->file != NULL);
			file_stream_writer = NULL;
		}
		break;

	case EP_SESSION_TYPE_IPCSTREAM:
		ipc_stream_writer = ep_ipc_stream_writer_alloc ((uint64_t)instance, stream);
		ep_raise_error_if_nok (ipc_stream_writer != NULL);
		instance->file = ep_file_alloc (ep_ipc_stream_writer_get_stream_writer_ref (ipc_stream_writer), format);
		ep_raise_error_if_nok (instance->file != NULL);
		ipc_stream_writer = NULL;
		break;

	default:
		break;
	}

	instance->session_start_time = ep_rt_system_time_get ();
	instance->session_start_timestamp = ep_perf_counter_query ();

	ep_rt_wait_event_alloc (&instance->rt_thread_shutdown_event, true, false);

ep_on_exit:
	ep_requires_lock_held ();
	return instance;

ep_on_error:
	ep_file_stream_writer_free (file_stream_writer);
	ep_ipc_stream_writer_free (ipc_stream_writer);
	ep_session_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_session_free (EventPipeSession *session)
{
	ep_return_void_if_nok (session != NULL);

	EP_ASSERT (ep_session_get_ipc_streaming_enabled (session) == false);

	ep_rt_wait_event_free (&session->rt_thread_shutdown_event);

	ep_session_provider_list_free (session->providers);

	ep_buffer_manager_free (session->buffer_manager);
	ep_file_free (session->file);

	ep_rt_object_free (session);
}

EventPipeSessionProvider *
ep_session_get_session_provider (
	const EventPipeSession *session,
	const EventPipeProvider *provider)
{
	EP_ASSERT (session != NULL);
	EP_ASSERT (provider != NULL);

	ep_requires_lock_held ();

	EventPipeSessionProviderList *providers = session->providers;
	ep_return_null_if_nok (providers != NULL);

	EventPipeSessionProvider *catch_all = ep_session_provider_list_get_catch_all_provider (providers);
	if (catch_all)
		return catch_all;

	EventPipeSessionProvider *session_provider = ep_rt_session_provider_list_find_by_name (ep_session_provider_list_get_providers_ref (providers), ep_provider_get_provider_name (provider));

	ep_requires_lock_held ();
	return session_provider;
}

void
ep_session_enable_rundown (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);

	ep_requires_lock_held ();

	//! This is CoreCLR specific keywords for native ETW events (ending up in event pipe).
	//! The keywords below seems to correspond to:
	//!  LoaderKeyword                      (0x00000008)
	//!  JitKeyword                         (0x00000010)
	//!  NgenKeyword                        (0x00000020)
	//!  unused_keyword                     (0x00000100)
	//!  JittedMethodILToNativeMapKeyword   (0x00020000)
	//!  ThreadTransferKeyword              (0x80000000)
	const uint64_t keywords = 0x80020138;
	const EventPipeEventLevel verbose_logging_level = EP_EVENT_LEVEL_VERBOSE;

	EventPipeProviderConfiguration rundown_providers [2];
	uint32_t rundown_providers_len = EP_ARRAY_SIZE (rundown_providers);

	ep_provider_config_init (&rundown_providers [0], ep_config_get_public_provider_name_utf8 (), keywords, verbose_logging_level, NULL); // Public provider.
	ep_provider_config_init (&rundown_providers [1], ep_config_get_rundown_provider_name_utf8 (), keywords, verbose_logging_level, NULL); // Rundown provider.

	// Update provider list with rundown configuration.
	for (uint32_t i = 0; i < rundown_providers_len; ++i) {
		const EventPipeProviderConfiguration *config = &rundown_providers [i];

		EventPipeSessionProvider *session_provider = ep_session_provider_alloc (
			ep_provider_config_get_provider_name (config),
			ep_provider_config_get_keywords (config),
			ep_provider_config_get_logging_level (config),
			ep_provider_config_get_filter_data (config));

		ep_session_add_session_provider (session, session_provider);
	}

	ep_session_set_rundown_enabled (session, true);

	ep_requires_lock_held ();
	return;
}

void
ep_session_execute_rundown (EventPipeSession *session)
{
	//TODO: Implement. This is mainly runtime specific implementation
	//since it will emit native trace events into the pipe (using CoreCLR's ETW support).
}

void
ep_session_suspend_write_event (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);

	// Force all in-progress writes to either finish or cancel
	// This is required to ensure we can safely flush and delete the buffers
	ep_buffer_manager_suspend_write_event (session->buffer_manager, session->index);
}

void
ep_session_write_sequence_point_unbuffered (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);

	ep_return_void_if_nok (session->file != NULL);

	EventPipeSequencePoint sequence_point;
	ep_sequence_point_init (&sequence_point);
	ep_buffer_manager_init_sequence_point_thread_list (session->buffer_manager, &sequence_point);
	ep_file_write_sequence_point (session->file, &sequence_point);
	ep_sequence_point_fini (&sequence_point);
}

void
ep_session_start_streaming (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);

	ep_requires_lock_held ();

	if (session->file != NULL)
		ep_file_initialize_file (session->file);

	if (session->session_type == EP_SESSION_TYPE_IPCSTREAM)
		session_create_ipc_streaming_thread (session);

	ep_requires_lock_held ();
	return;
}

bool
ep_session_is_valid (const EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	return !ep_session_provider_list_is_empty (session->providers);
}

void
ep_session_add_session_provider (EventPipeSession *session, EventPipeSessionProvider *session_provider)
{
	EP_ASSERT (session != NULL);
	ep_session_provider_list_add_session_provider (session->providers, session_provider);
}

void
ep_session_disable (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);

	if (session->session_type == EP_SESSION_TYPE_IPCSTREAM && ep_session_get_ipc_streaming_enabled (session))
		session_disable_ipc_streaming_thread (session);

	bool ignored;
	ep_session_write_all_buffers_to_file (session, &ignored);
	ep_session_provider_list_clear (session->providers);
}

bool
ep_session_write_all_buffers_to_file (EventPipeSession *session, bool *events_written)
{
	EP_ASSERT (session != NULL);

	if (session->file == NULL)
		return true;

	// Get the current time stamp.
	// ep_buffer_manager_write_all_buffer_to_file will use this to ensure that no events after
	// the current timestamp are written into the file.
	ep_timestamp_t stop_timestamp = ep_rt_perf_counter_query ();
	ep_buffer_manager_write_all_buffers_to_file (session->buffer_manager, session->file, stop_timestamp, events_written);
	return ep_file_has_errors (session->file);
}

bool
ep_session_write_event_buffered (
	EventPipeSession *session,
	EventPipeThread *thread,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	EventPipeThread *event_thread,
	EventPipeStackContents *stack)
{
	EP_ASSERT (session != NULL);
	EP_ASSERT (ep_event != NULL);

	return ep_event_is_enabled_by_mask (ep_event, ep_session_get_mask (session)) ?
		ep_buffer_manager_write_event (
			session->buffer_manager,
			thread,
			session,
			ep_event,
			payload,
			activity_id,
			related_activity_id,
			event_thread,
			stack) :
		false;
}

EventPipeEventInstance *
ep_session_get_next_event (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	ep_requires_lock_not_held ();
	return ep_buffer_manager_get_next_event (session->buffer_manager);
}

EventPipeWaitHandle
ep_session_get_wait_event (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	EP_ASSERT (session->buffer_manager != NULL);
	return ep_rt_wait_event_get_wait_handle (ep_buffer_manager_get_rt_wait_event_ref (session->buffer_manager));
}

uint64_t
ep_session_get_mask (const EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	return ((uint64_t)1 << session->index);
}

bool
ep_session_get_rundown_enabled (const EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	return (ep_rt_volatile_load_uint32_t (&session->rundown_enabled) != 0 ? true : false);
}

void
ep_session_set_rundown_enabled (
	EventPipeSession *session,
	bool enabled)
{
	EP_ASSERT (session != NULL);
	ep_rt_volatile_store_uint32_t (&session->rundown_enabled, (enabled) ? 1 : 0);
}

bool
ep_session_get_ipc_streaming_enabled (const EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	return (ep_rt_volatile_load_uint32_t(&session->ipc_streaming_enabled) != 0 ? true : false);
}

void
ep_session_set_ipc_streaming_enabled (
	EventPipeSession *session,
	bool enabled)
{
	EP_ASSERT (session != NULL);
	ep_rt_volatile_store_uint32_t (&session->ipc_streaming_enabled, (enabled) ? 1 : 0);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_session;
const char quiet_linker_empty_file_warning_eventpipe_session = 0;
#endif
