#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_SESSION_GETTER_SETTER
#include "ep.h"
#include "ep-buffer-manager.h"
#include "ep-config.h"
#include "ep-event.h"
#include "ep-file.h"
#include "ep-session.h"
#include "ep-event-payload.h"
#include "ep-rt.h"

#if HAVE_UNISTD_H
#include <unistd.h> // close
#endif // HAVE_UNISTD_H

#if HAVE_SYS_UIO_H
#include <sys/uio.h> // iovec
#endif // HAVE_SYS_UIO_H

#if HAVE_ERRNO_H
#include <errno.h> // errno
#endif // HAVE_ERRNO_H

/*
 * Forward declares of all static functions.
 */

static
void
session_disable_streaming_thread (EventPipeSession *session);

// _Requires_lock_held (ep)
static
void
session_create_streaming_thread (EventPipeSession *session);

static
void
ep_session_remove_dangling_session_states (EventPipeSession *session);

static
bool
session_user_events_tracepoints_init (
	EventPipeSession *session,
	int user_events_data_fd);

static
void
session_disable_user_events (EventPipeSession *session);

static
bool
is_guid_empty(const uint8_t *guid);

static
uint16_t
construct_extension_activity_ids_buffer (
	uint8_t *extension,
	size_t extension_size,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static
bool
session_tracepoint_write_event (
	EventPipeSession *session,
	ep_rt_thread_handle_t thread,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *ep_event_payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	ep_rt_thread_handle_t event_thread,
	EventPipeStackContents *stack);

/*
 * EventPipeSession.
 */

#ifndef PERFTRACING_DISABLE_THREADS

EP_RT_DEFINE_THREAD_FUNC (streaming_thread)
{
	EP_ASSERT (data != NULL);
	if (data == NULL)
		return 1;

	ep_rt_thread_params_t *thread_params = (ep_rt_thread_params_t *)data;

	EventPipeSession *const session = (EventPipeSession *)thread_params->thread_params;
	if (session->session_type != EP_SESSION_TYPE_IPCSTREAM && session->session_type != EP_SESSION_TYPE_FILESTREAM)
		return 1;

	if (!thread_params->thread || !ep_rt_thread_has_started (thread_params->thread))
		return 1;

	session->streaming_thread = thread_params->thread;

	bool success = true;
	ep_rt_wait_event_handle_t *wait_event = ep_session_get_wait_event (session);

	ep_rt_volatile_store_uint32_t (&session->started, 1);

	EP_GCX_PREEMP_ENTER
		while (ep_session_get_streaming_enabled (session)) {
			bool events_written = false;
			if (!ep_session_write_all_buffers_to_file (session, &events_written)) {
				success = false;
				break;
			}

			if (!events_written) {
				// No events were available, sleep until more are available
				ep_rt_wait_event_wait (wait_event, EP_INFINITE_WAIT, false);
			}

			// Wait until it's time to sample again.
			const uint32_t timeout_ns = 100000000; // 100 msec.
			ep_rt_thread_sleep (timeout_ns);
		}

		session->streaming_thread = NULL;
		ep_rt_wait_event_set (&session->rt_thread_shutdown_event);
	EP_GCX_PREEMP_EXIT

	if (!success)
		ep_disable ((EventPipeSessionID)session);

	return (ep_rt_thread_start_func_return_t)0;
}

#else // PERFTRACING_DISABLE_THREADS

static size_t streaming_loop_tick(EventPipeSession *const session) {
	bool events_written = false;
	bool ok;
	if (!ep_session_get_streaming_enabled (session)){
		session->streaming_thread = NULL;
		ep_session_dec_ref (session);
		return 1; // done
	}
	EP_GCX_PREEMP_ENTER
	ok = ep_session_write_all_buffers_to_file (session, &events_written);
	EP_GCX_PREEMP_EXIT
	if (!ok) {
		ep_disable ((EventPipeSessionID)session);
		return 1; // done
	}
	return 0; // continue
}

#endif // PERFTRACING_DISABLE_THREADS

static
void
session_create_streaming_thread (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	EP_ASSERT (session->session_type == EP_SESSION_TYPE_IPCSTREAM || session->session_type == EP_SESSION_TYPE_FILESTREAM);

	ep_requires_lock_held ();

	ep_session_set_streaming_enabled (session, true);
	ep_rt_wait_event_alloc (&session->rt_thread_shutdown_event, true, false);
	if (!ep_rt_wait_event_is_valid (&session->rt_thread_shutdown_event))
		EP_UNREACHABLE ("Unable to create stream flushing thread shutdown event.");

#ifndef PERFTRACING_DISABLE_THREADS
	ep_rt_thread_id_t thread_id = ep_rt_uint64_t_to_thread_id_t (0);
	if (!ep_rt_thread_create ((void *)streaming_thread, (void *)session, EP_THREAD_TYPE_SESSION, &thread_id))
		EP_UNREACHABLE ("Unable to create stream flushing thread.");
#else
	ep_session_inc_ref (session);
	ep_rt_volatile_store_uint32_t (&session->started, 1);
	ep_rt_queue_job ((void *)streaming_loop_tick, (void *)session);
#endif
}

static
void
session_disable_streaming_thread (EventPipeSession *session)
{
	EP_ASSERT (session->session_type == EP_SESSION_TYPE_IPCSTREAM || session->session_type == EP_SESSION_TYPE_FILESTREAM);
	EP_ASSERT (ep_session_get_streaming_enabled (session));

	EP_ASSERT (!ep_rt_process_detach ());
	EP_ASSERT (session->buffer_manager != NULL);

	// The streaming thread will watch this value and exit
	// when profiling is disabled.
	ep_session_set_streaming_enabled (session, false);

	// Thread could be waiting on the event that there is new data to read.
	ep_rt_wait_event_set (ep_buffer_manager_get_rt_wait_event_ref (session->buffer_manager));

	// Wait for the streaming thread to clean itself up.
	ep_rt_wait_event_handle_t *rt_thread_shutdown_event = &session->rt_thread_shutdown_event;
	ep_rt_wait_event_wait (rt_thread_shutdown_event, EP_INFINITE_WAIT, false /* bAlertable */);
	ep_rt_wait_event_free (rt_thread_shutdown_event);
}

/*
 *  session_user_events_tracepoints_init
 *
 *  Registers all configured tracepoints for the user_events eventpipe session.
 */
static
bool
session_user_events_tracepoints_init (
	EventPipeSession *session,
	int user_events_data_fd)
{
	EP_ASSERT (session != NULL);
	EP_ASSERT (session->session_type == EP_SESSION_TYPE_USEREVENTS);

	bool result = false;

	EventPipeSessionProviderList *providers = session->providers;
	EP_ASSERT (providers != NULL);

	ep_raise_error_if_nok (user_events_data_fd != -1);
	session->user_events_data_fd = user_events_data_fd;

	DN_LIST_FOREACH_BEGIN (EventPipeSessionProvider *, session_provider, ep_session_provider_list_get_providers (providers)) {
		EP_ASSERT (session_provider != NULL);
		ep_raise_error_if_nok (ep_session_provider_register_tracepoints (session_provider, session->user_events_data_fd));
	} DN_LIST_FOREACH_END;
	
	result = true;

ep_on_exit:
	return result;

ep_on_error:
	session_disable_user_events (session);
	ep_exit_error_handler ();
}

EventPipeSession *
ep_session_alloc (
	uint32_t index,
	const ep_char8_t *output_path,
	IpcStream *stream,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	uint64_t rundown_keyword,
	bool stackwalk_requested,
	uint32_t circular_buffer_size_in_mb,
	const EventPipeProviderConfiguration *providers,
	uint32_t providers_len,
	EventPipeSessionSynchronousCallback sync_callback,
	void *callback_additional_data,
	int user_events_data_fd)
{
	EP_ASSERT (index < EP_MAX_NUMBER_OF_SESSIONS);
	EP_ASSERT (format < EP_SERIALIZATION_FORMAT_COUNT);
	EP_ASSERT (!ep_session_type_uses_buffer_manager (session_type) || circular_buffer_size_in_mb > 0);
	EP_ASSERT (providers_len > 0);
	EP_ASSERT (providers != NULL);
	EP_ASSERT ((sync_callback != NULL) == (session_type == EP_SESSION_TYPE_SYNCHRONOUS));

	ep_requires_lock_held ();

	FileStreamWriter *file_stream_writer = NULL;
	IpcStreamWriter *ipc_stream_writer = NULL;
	size_t sequence_point_alloc_budget = 0;

	EventPipeSession *instance = ep_rt_object_alloc (EventPipeSession);
	ep_raise_error_if_nok (instance != NULL);
	ep_session_inc_ref (instance);

	instance->providers = ep_session_provider_list_alloc (providers, providers_len);
	ep_raise_error_if_nok (instance->providers != NULL);

	instance->index = index;
	instance->rundown_enabled = 0;
	instance->session_type = session_type;
	instance->format = format;
	instance->rundown_keyword = rundown_keyword;
	instance->synchronous_callback = sync_callback;
	instance->callback_additional_data = callback_additional_data;

	// Hard coded 10MB for now, we'll probably want to make
	// this configurable later.
	if (instance->session_type != EP_SESSION_TYPE_LISTENER && instance->format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4) {
		sequence_point_alloc_budget = 10 * 1024 * 1024;
	}

	if (ep_session_type_uses_buffer_manager (session_type)) {
		instance->buffer_manager = ep_buffer_manager_alloc (instance, ((size_t)circular_buffer_size_in_mb) << 20, sequence_point_alloc_budget);
		ep_raise_error_if_nok (instance->buffer_manager != NULL);
	}

	// Create the event pipe file.
	// A NULL output path means that we should not write the results to a file.
	// This is used in the EventListener case.
	switch (session_type) {
	case EP_SESSION_TYPE_FILE :
	case EP_SESSION_TYPE_FILESTREAM :
		if (output_path) {
			file_stream_writer = ep_file_stream_writer_alloc (output_path);
			ep_raise_error_if_nok (file_stream_writer != NULL);
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

	case EP_SESSION_TYPE_USEREVENTS:
		// With the user_events_data file, register tracepoints for each provider's tracepoint configurations
		ep_raise_error_if_nok (session_user_events_tracepoints_init (instance, user_events_data_fd));
		break;

	default:
		break;
	}

	instance->session_start_time = ep_system_timestamp_get ();
	instance->session_start_timestamp = ep_perf_timestamp_get ();
	instance->paused = false;
	instance->enable_stackwalk = ep_rt_config_value_get_enable_stackwalk () && stackwalk_requested;
	instance->started = 0;

ep_on_exit:
	ep_requires_lock_held ();
	return instance;

ep_on_error:
	ep_file_stream_writer_free (file_stream_writer);
	ep_ipc_stream_writer_free (ipc_stream_writer);
	ep_session_dec_ref (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_session_remove_dangling_session_states (EventPipeSession *session)
{
	ep_return_void_if_nok (session != NULL);

	DN_DEFAULT_LOCAL_ALLOCATOR (allocator, dn_vector_ptr_default_local_allocator_byte_size);

	dn_vector_ptr_custom_init_params_t params = {0, };
	params.allocator = (dn_allocator_t *)&allocator;
	params.capacity = dn_vector_ptr_default_local_allocator_capacity_size;

	dn_vector_ptr_t threads;

	if (dn_vector_ptr_custom_init (&threads, &params)) {
		ep_thread_get_threads (&threads);
		DN_VECTOR_PTR_FOREACH_BEGIN (EventPipeThread *, thread, &threads) {
			if (thread) {
				EP_SPIN_LOCK_ENTER (ep_thread_get_rt_lock_ref (thread), section1);
				EventPipeThreadSessionState *session_state = ep_thread_get_session_state(thread, session);
				if (session_state) {
					// If a buffer tries to write event(s) but never gets a buffer because the maximum total buffer size
					// has been exceeded, we can leak the EventPipeThreadSessionState* and crash later trying to access 
					// the session from the thread session state. Whenever we terminate a session we check to make sure
					// we haven't leaked any thread session states.
					ep_thread_delete_session_state(thread, session);
				}
				EP_SPIN_LOCK_EXIT (ep_thread_get_rt_lock_ref (thread), section1);

				ep_thread_release (thread);
			}
		} DN_VECTOR_PTR_FOREACH_END;

		dn_vector_ptr_dispose (&threads);
	}

ep_on_exit:
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_session_inc_ref (EventPipeSession *session)
{
	ep_rt_atomic_inc_uint32_t (&session->ref_count);
}

void
ep_session_dec_ref (EventPipeSession *session)
{
	ep_return_void_if_nok (session != NULL);

	EP_ASSERT (!ep_session_get_streaming_enabled (session));

	if (ep_rt_atomic_dec_uint32_t (&session->ref_count) != 0)
		return;

	ep_rt_wait_event_free (&session->rt_thread_shutdown_event);

	ep_session_provider_list_free (session->providers);

	ep_buffer_manager_free (session->buffer_manager);
	ep_file_free (session->file);

	ep_session_remove_dangling_session_states (session);

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

	EventPipeSessionProvider *session_provider = ep_session_provider_list_find_by_name (ep_session_provider_list_get_providers (providers), ep_provider_get_provider_name (provider));

	ep_requires_lock_held ();
	return session_provider;
}

bool
ep_session_enable_rundown (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);

	ep_requires_lock_held ();

	bool result = false;
	const uint64_t keywords = ep_session_get_rundown_keyword (session);
	const EventPipeEventLevel verbose_logging_level = EP_EVENT_LEVEL_VERBOSE;

	EventPipeProviderConfiguration rundown_provider;
	ep_provider_config_init (&rundown_provider, ep_config_get_rundown_provider_name_utf8 (), keywords, verbose_logging_level, NULL); // Rundown provider.

	EventPipeSessionProvider *session_provider = ep_session_provider_alloc (
		ep_provider_config_get_provider_name (&rundown_provider),
		ep_provider_config_get_keywords (&rundown_provider),
		ep_provider_config_get_logging_level (&rundown_provider),
		ep_provider_config_get_filter_data (&rundown_provider),
		NULL,
		NULL);

	ep_raise_error_if_nok (ep_session_add_session_provider (session, session_provider));

	ep_session_set_rundown_enabled (session, true);
	result = true;

ep_on_exit:
	ep_requires_lock_held ();
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ep_exit_error_handler ();
}

void
ep_session_execute_rundown (
	EventPipeSession *session,
	dn_vector_ptr_t *execution_checkpoints)
{
	EP_ASSERT (session != NULL);

	// Lock must be held by ep_disable.
	ep_requires_lock_held ();

	ep_return_void_if_nok (session->file != NULL);

	ep_rt_execute_rundown (execution_checkpoints);
}

void
ep_session_suspend_write_event (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);

	// Need to disable the session before calling this method.
	EP_ASSERT (!ep_is_session_enabled ((EventPipeSessionID)session));

	DN_DEFAULT_LOCAL_ALLOCATOR (allocator, dn_vector_ptr_default_local_allocator_byte_size);

	dn_vector_ptr_custom_init_params_t params = {0, };
	params.allocator = (dn_allocator_t *)&allocator;
	params.capacity = dn_vector_ptr_default_local_allocator_capacity_size;

	dn_vector_ptr_t threads;

	if (dn_vector_ptr_custom_init (&threads, &params)) {
		ep_thread_get_threads (&threads);
		DN_VECTOR_PTR_FOREACH_BEGIN (EventPipeThread *, thread, &threads) {
			if (thread) {
				// Wait for the thread to finish any writes to this session
				EP_YIELD_WHILE (ep_thread_get_session_write_in_progress (thread) == session->index);

				// Since we've already disabled the session, the thread won't call back in to this
				// session once its done with the current write
				ep_thread_release (thread);
			}
		} DN_VECTOR_PTR_FOREACH_END;

		dn_vector_ptr_dispose (&threads);
	}

	if (session->buffer_manager)
		// Convert all buffers to read only to ensure they get flushed
		ep_buffer_manager_suspend_write_event (session->buffer_manager, session->index);
}

void
ep_session_write_sequence_point_unbuffered (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);

	ep_return_void_if_nok (session->file != NULL && session->buffer_manager != NULL);

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

	if (session->session_type == EP_SESSION_TYPE_IPCSTREAM || session->session_type == EP_SESSION_TYPE_FILESTREAM)
		session_create_streaming_thread (session);

	if (session->session_type == EP_SESSION_TYPE_SYNCHRONOUS) {
		EP_ASSERT (session->file == NULL);
		EP_ASSERT (!ep_session_get_streaming_enabled (session));
	}

	if (session->session_type != EP_SESSION_TYPE_IPCSTREAM && session->session_type != EP_SESSION_TYPE_FILESTREAM)
		ep_rt_volatile_store_uint32_t_without_barrier (&session->started, 1);

	ep_requires_lock_held ();
	return;
}

bool
ep_session_is_valid (const EventPipeSession *session)
{
	EP_ASSERT (session != NULL);

	ep_requires_lock_held ();

	return !ep_session_provider_list_is_empty (session->providers);
}

bool
ep_session_add_session_provider (EventPipeSession *session, EventPipeSessionProvider *session_provider)
{
	EP_ASSERT (session != NULL);

	ep_requires_lock_held ();

	return ep_session_provider_list_add_session_provider (session->providers, session_provider);
}

void
ep_session_disable (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);

	if ((session->session_type == EP_SESSION_TYPE_IPCSTREAM || session->session_type == EP_SESSION_TYPE_FILESTREAM) && ep_session_get_streaming_enabled (session))
		session_disable_streaming_thread (session);

	if (session->session_type == EP_SESSION_TYPE_USEREVENTS)
		session_disable_user_events (session);

	if (session->file != NULL) {
		bool ignored;
		ep_session_write_all_buffers_to_file (session, &ignored);
	}

	ep_session_provider_list_clear (session->providers);
}

bool
ep_session_write_all_buffers_to_file (EventPipeSession *session, bool *events_written)
{
	EP_ASSERT (session != NULL);

	if (session->file == NULL || session->buffer_manager == NULL)
		return true;

	// Get the current time stamp.
	// ep_buffer_manager_write_all_buffer_to_file will use this to ensure that no events after
	// the current timestamp are written into the file.
	ep_timestamp_t stop_timestamp = ep_perf_timestamp_get ();
	ep_buffer_manager_write_all_buffers_to_file (session->buffer_manager, session->file, stop_timestamp, events_written);
	return !ep_file_has_errors (session->file);
}

static
void
session_disable_user_events (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	ep_return_void_if_nok (session->session_type == EP_SESSION_TYPE_USEREVENTS && session->user_events_data_fd != -1);

	ep_requires_lock_held ();

	EventPipeSessionProviderList *providers = session->providers;
	EP_ASSERT (providers != NULL);
	DN_LIST_FOREACH_BEGIN (EventPipeSessionProvider *, session_provider, ep_session_provider_list_get_providers (providers)) {
		ep_session_provider_unregister_tracepoints (session_provider, session->user_events_data_fd);
	} DN_LIST_FOREACH_END;

#if HAVE_UNISTD_H
	close (session->user_events_data_fd);
#endif // HAVE_UNISTD_H
	session->user_events_data_fd = -1;
}

static
bool
is_guid_empty(const uint8_t *guid)
{
	if (guid == NULL)
		return true;

	for (size_t i = 0; i < EP_ACTIVITY_ID_SIZE; ++i) {
		if (guid[i] != 0)
			return false;
	}

	return true;
}

/*
 *  construct_extension_activity_ids_buffer
 *
 *  Encodes non-empty activity ID and related activity ID.
 */
static
uint16_t
construct_extension_activity_ids_buffer (
	uint8_t *extension,
	size_t extension_size,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (extension != NULL);
	EP_ASSERT (extension_size >= 2 * (1 + EP_ACTIVITY_ID_SIZE));
	uint16_t offset = 0;

	memset (extension, 0, extension_size);

	bool activity_id_is_empty = is_guid_empty (activity_id);
	bool related_activity_id_is_empty = is_guid_empty (related_activity_id);

	EP_ASSERT ((size_t)(offset + 1 + EP_ACTIVITY_ID_SIZE) <= extension_size);
	if (!activity_id_is_empty) {
		extension[offset] = 0x02;
		++offset;
		memcpy (extension + offset, activity_id, EP_ACTIVITY_ID_SIZE);
		offset += EP_ACTIVITY_ID_SIZE;
	}
	
	EP_ASSERT ((size_t)(offset + 1 + EP_ACTIVITY_ID_SIZE) <= extension_size);
	if (!related_activity_id_is_empty) {
		extension[offset] = 0x03;
		++offset;
		memcpy (extension + offset, related_activity_id, EP_ACTIVITY_ID_SIZE);
		offset += EP_ACTIVITY_ID_SIZE;
	}

	return offset;
}

#if HAVE_SYS_UIO_H && HAVE_ERRNO_H
/*
 *  session_tracepoint_write_event
 *
 *  Writes the event data to the corresponding tracepoint, as configured by the session provider.
 *  For minimum performance overhead, this function prefers static buffers over dynamic allocations.
 */
static
bool
session_tracepoint_write_event (
	EventPipeSession *session,
	ep_rt_thread_handle_t thread,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *ep_event_payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	ep_rt_thread_handle_t event_thread,
	EventPipeStackContents *stack)
{
	EP_ASSERT (session != NULL);
	EP_ASSERT (ep_event != NULL);

	EventPipeProvider *provider = ep_event_get_provider (ep_event);
	EventPipeSessionProviderList *session_provider_list = session->providers;
	EventPipeSessionProvider *session_provider = ep_session_provider_list_find_by_name (ep_session_provider_list_get_providers (session_provider_list), ep_provider_get_provider_name (provider));
	const EventPipeSessionProviderTracepoint *tracepoint = ep_session_provider_get_tracepoint_for_event (session_provider, ep_event);
	if (tracepoint == NULL)
		return false;

	if (ep_session_provider_tracepoint_get_enabled (tracepoint) == 0)
		return false;

	// Setup iovec array
	const int max_non_parameter_iov = 8;
	const int max_static_io_capacity = 30; // Should account for most events that use EventData structs
	struct iovec static_io[max_static_io_capacity];
	struct iovec *io = static_io;

	uint8_t *ep_event_data = ep_event_payload_get_data (ep_event_payload);
	uint32_t ep_event_data_len = ep_event_payload_get_event_data_len (ep_event_payload);
	int param_iov = ep_event_data != NULL ? 1 : ep_event_data_len;
	int io_elem_capacity = param_iov + max_non_parameter_iov;
	if (io_elem_capacity > max_static_io_capacity) {
		io = (struct iovec *)malloc (sizeof (struct iovec) * io_elem_capacity);
		if (io == NULL)
			return false;
	}

	int io_index = 0;

	// Write Index
	uint32_t write_index = ep_session_provider_tracepoint_get_write_index (tracepoint);
	io[io_index].iov_base = (void *)&write_index;
	io[io_index].iov_len = sizeof(write_index);
	io_index++;

	// Version
	uint8_t version = 0x01; // Format V1
	io[io_index].iov_base = &version;
	io[io_index].iov_len = sizeof(version);
	io_index++;

	// Truncated event id
	// For parity with EventSource, there shouldn't be any that need more than 16 bits.
	uint16_t truncated_event_id = ep_event_get_event_id (ep_event) & 0xFFFF;
	io[io_index].iov_base = &truncated_event_id;
	io[io_index].iov_len = sizeof(truncated_event_id);
	io_index++;

	// Extension and payload relative locations (to be fixed up later)
	uint32_t extension_rel_loc = 0;
	io[io_index].iov_base = &extension_rel_loc;
	io[io_index].iov_len = sizeof(extension_rel_loc);
	io_index++;

	uint32_t payload_rel_loc = 0;
	io[io_index].iov_base = &payload_rel_loc;
	io[io_index].iov_len = sizeof(payload_rel_loc);
	io_index++;

	// Extension
	uint32_t extension_len = 0;

	// Extension Event Metadata
	uint32_t metadata_len = ep_event_get_metadata_len (ep_event);
	uint8_t extension_metadata[1 + sizeof(uint32_t)];
	extension_metadata[0] = 0x01; // label
	*(uint32_t*)&extension_metadata[1] = metadata_len;
	io[io_index].iov_base = extension_metadata;
	io[io_index].iov_len = sizeof(extension_metadata);
	io_index++;
	extension_len += sizeof(extension_metadata);

	io[io_index].iov_base = (void *)ep_event_get_metadata (ep_event);
	io[io_index].iov_len = metadata_len;
	io_index++;
	extension_len += metadata_len;

	// Extension Activity IDs
	const int extension_activity_ids_max_len = 2 * (1 + EP_ACTIVITY_ID_SIZE);
	uint8_t extension_activity_ids[extension_activity_ids_max_len];
	uint16_t extension_activity_ids_len = construct_extension_activity_ids_buffer (extension_activity_ids, extension_activity_ids_max_len, activity_id, related_activity_id);
	EP_ASSERT (extension_activity_ids_len <= extension_activity_ids_max_len);
	io[io_index].iov_base = extension_activity_ids;
	io[io_index].iov_len = extension_activity_ids_len;
	io_index++;
	extension_len += extension_activity_ids_len;

	EP_ASSERT (io_index <= max_non_parameter_iov);

	uint32_t ep_event_payload_total_size = 0;
	// EventPipeEventPayload
	if (ep_event_data != NULL) {
		uint32_t ep_event_payload_size = ep_event_payload_get_size (ep_event_payload);
		io[io_index].iov_base = ep_event_data;
		io[io_index].iov_len = ep_event_payload_size;
		io_index++;
		ep_event_payload_total_size += ep_event_payload_size;
	} else {
		const EventData *event_data = ep_event_payload_get_event_data (ep_event_payload);
		for (uint32_t i = 0; i < ep_event_data_len; ++i) {
			uint32_t ep_event_data_size = ep_event_data_get_size (&event_data[i]);
			io[io_index].iov_base = (void *)ep_event_data_get_ptr (&event_data[i]);
			io[io_index].iov_len = ep_event_data_size;
			io_index++;
			ep_event_payload_total_size += ep_event_data_size;
		}
	}

	if (((extension_len & 0xFFFF0000) != 0) || ((ep_event_payload_total_size & 0xFFFF0000) != 0)) {
		if (io != static_io)
			free (io);
		return false;
	}

	// Calculate the relative locations for extension and payload.
	extension_rel_loc = extension_len << 16 | (sizeof(payload_rel_loc) & 0xFFFF);
	payload_rel_loc = ep_event_payload_total_size << 16 | (extension_len & 0xFFFF);

	ssize_t bytes_written;
	while ((bytes_written = writev(session->user_events_data_fd, (const struct iovec *)io, io_index) < 0) && errno == EINTR);

	if (io != static_io)
		free (io);

	return bytes_written != -1;
}
#else // HAVE_SYS_UIO_H && HAVE_ERRNO_H
static
bool
session_tracepoint_write_event (
	EventPipeSession *session,
	ep_rt_thread_handle_t thread,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *ep_event_payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	ep_rt_thread_handle_t event_thread,
	EventPipeStackContents *stack)
{
	// Not Supported
	return false;
}
#endif // HAVE_SYS_UIO_H && HAVE_ERRNO_H

bool
ep_session_write_event (
	EventPipeSession *session,
	ep_rt_thread_handle_t thread,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	ep_rt_thread_handle_t event_thread,
	EventPipeStackContents *stack)
{
	EP_ASSERT (session != NULL);
	EP_ASSERT (ep_event != NULL);

	if (session->paused)
		return true;

	bool result = false;

	// Filter events specific to "this" session based on precomputed flag on provider/events.
	if (ep_event_is_enabled_by_mask (ep_event, ep_session_get_mask (session))) {
		switch (session->session_type) {
		case EP_SESSION_TYPE_FILE:
		case EP_SESSION_TYPE_LISTENER:
		case EP_SESSION_TYPE_IPCSTREAM:
		case EP_SESSION_TYPE_FILESTREAM:
			EP_ASSERT (session->buffer_manager != NULL);
			result = ep_buffer_manager_write_event (
				session->buffer_manager,
				thread,
				session,
				ep_event,
				payload,
				activity_id,
				related_activity_id,
				event_thread,
				stack);
			break;
		case EP_SESSION_TYPE_SYNCHRONOUS:
			EP_ASSERT (session->synchronous_callback != NULL);
			session->synchronous_callback (
				ep_event_get_provider (ep_event),
				ep_event_get_event_id (ep_event),
				ep_event_get_event_version (ep_event),
				ep_event_get_metadata_len (ep_event),
				ep_event_get_metadata (ep_event),
				ep_event_payload_get_size (payload),
				ep_event_payload_get_flat_data (payload),
				activity_id,
				related_activity_id,
				event_thread,
				stack == NULL ? 0 : ep_stack_contents_get_size (stack),
				stack == NULL ? NULL : (uintptr_t *)ep_stack_contents_get_pointer (stack),
				session->callback_additional_data);
			result = true;
			break;
		case EP_SESSION_TYPE_USEREVENTS:
			EP_ASSERT (session->user_events_data_fd != -1);
			result = session_tracepoint_write_event (
				session,
				thread,
				ep_event,
				payload,
				activity_id,
				related_activity_id,
				event_thread,
				stack);
			break;
		default:
			EP_UNREACHABLE ("Unknown session type.");
			break;
		}
	}

	return result;
}

EventPipeEventInstance *
ep_session_get_next_event (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	ep_requires_lock_not_held ();

	if (!session->buffer_manager) {
		EP_ASSERT (!"Shouldn't call get_next_event on a synchronous session.");
		return NULL;
	}

	return ep_buffer_manager_get_next_event (session->buffer_manager);
}

ep_rt_wait_event_handle_t *
ep_session_get_wait_event (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);

	if (!session->buffer_manager) {
		EP_ASSERT (!"Shouldn't call get_wait_event on a synchronous session.");
		return NULL;
	}

	return ep_buffer_manager_get_rt_wait_event_ref (session->buffer_manager);
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
ep_session_get_streaming_enabled (const EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	return (ep_rt_volatile_load_uint32_t(&session->streaming_enabled) != 0 ? true : false);
}

void
ep_session_set_streaming_enabled (
	EventPipeSession *session,
	bool enabled)
{
	EP_ASSERT (session != NULL);
	ep_rt_volatile_store_uint32_t (&session->streaming_enabled, (enabled) ? 1 : 0);
}

void
ep_session_pause (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	session->paused = true;
}

void
ep_session_resume (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	session->paused = false;
}

bool
ep_session_has_started (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	return ep_rt_volatile_load_uint32_t (&session->started) == 1 ? true : false;
}

bool
ep_session_type_uses_buffer_manager (EventPipeSessionType session_type)
{
	return (session_type != EP_SESSION_TYPE_SYNCHRONOUS && session_type != EP_SESSION_TYPE_USEREVENTS);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_session;
const char quiet_linker_empty_file_warning_eventpipe_session = 0;
#endif
