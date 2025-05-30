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

static
uint32_t
event_reg(uint32_t fd, const char *command, uint32_t *write, uint32_t *enabled)
{
#if HAVE_LINUX_USER_EVENTS_H
	struct user_reg reg = {0};

	reg.size = sizeof(reg); // uint32_t
	reg.enable_bit = 31; // uint8_t
	reg.enable_size = sizeof(*enabled); // uint8_t
	// reg.flags //uint16_t
	reg.enable_addr = (uint64_t)enabled; // uint64_t

	// Hard-coded format as per the documentation
	const char *format = "u8 version; u16 event_id; __rel_loc u8[] extension; __rel_loc u8[] payload; __rel_loc u8[] meta";

	// Dynamically allocate space for name_args
	size_t name_args_size = strlen(command) + strlen(format) + 2; // +2 for space and null terminator
	char *name_args = (char *)malloc(name_args_size);
	if (!name_args)
		return -1; // Memory allocation failed

	if (snprintf(name_args, name_args_size, "%s %s", command, format) >= name_args_size) {
		free(name_args);
		return -1; // Name and format combination is too long
	}

	reg.name_args = (uint64_t)name_args; // uint64_t

	if (ioctl(fd, DIAG_IOCSREG, &reg) == -1) {
		free(name_args);
		return -1;
	}

	*write = reg.write_index; // uint32_t

	free(name_args);
	return 0;
#else // HAVE_LINUX_USER_EVENTS_H
	// Not Supported
	return -1;
#endif // HAVE_LINUX_USER_EVENTS_H
}

// Could bump this to when we are deserializing the payload
// And then the ProviderConfigurations would instead own the actual
// Tracepoint info like write_index, enable bit, mapping
static
void
ep_session_user_events_tracepoints_init (
	EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	EP_ASSERT (session->session_type == EP_SESSION_TYPE_USEREVENTS);

	EP_ASSERT (session->user_events_data_fd != 0);

	EventPipeSessionProviderList *providers = ep_session_get_providers (session);
	EP_ASSERT (providers != NULL);

	// Create a mapping of event_id to Tracepoint that we generate, for writing.
	for (dn_list_it_t it = dn_list_begin (ep_session_provider_list_get_providers (providers)); !dn_list_it_end (it); it = dn_list_it_next (it)) {
		EventPipeSessionProvider *session_provider = *dn_list_it_data_t (it, EventPipeSessionProvider *);
		EP_ASSERT (session_provider != NULL);

		// Should this be owned by each EventPipeSession ProviderConfiguration
		dn_umap_custom_alloc_params_t params = {0, };
		params.hash_func = dn_int_hash;
		params.equal_func = dn_int_equal;
		dn_umap_t *provider_event_id_to_tracepoint_map = dn_umap_custom_alloc (&params);

		// Should we bother to register tracepoints whos events don't pass the event_filter?
		ProviderTracepointConfiguration *tracepoint_config = ep_session_provider_get_tracepoint_config (session_provider);

		dn_vector_t *tracepoints = tracepoint_config->tracepoints;
		if (tracepoints != NULL) {
			for (int32_t i = 0; i < tracepoints->size; ++i) {
				// Get the tracepoint set
				ProviderTracepointSet *tracepoint_set = *dn_vector_index_t (tracepoints, ProviderTracepointSet *, i);
				EP_ASSERT (tracepoint_set != NULL);

				// Get the tracepoint name for this set
				const ep_char8_t *tracepoint_name = tracepoint_set->tracepoint_name;
				EP_ASSERT (tracepoint_name != NULL);

				EventPipeTracepoint *tracepoint = ep_rt_object_alloc (EventPipeTracepoint);
				EP_ASSERT(tracepoint != NULL);
				if (event_reg (session->user_events_data_fd, tracepoint_name, &tracepoint->write_index, &tracepoint->enabled) == -1) {
					ep_raise_error ();
				}

				dn_vector_t *event_ids = tracepoint_set->event_ids;
				if (event_ids != NULL) {
					for (int32_t j = 0; j < event_ids->size; ++j) {
						uint32_t *event_id = dn_vector_index_t (event_ids, uint32_t, j);
						dn_umap_result_t result = dn_umap_insert (provider_event_id_to_tracepoint_map, event_id, tracepoint);
						EP_ASSERT (result.result);
					}
				}
			}
		}
		session_provider->event_id_to_tracepoint_map = provider_event_id_to_tracepoint_map;

		const ep_char8_t *default_tracepoint_name = tracepoint_config->default_tracepoint_name;
		if (default_tracepoint_name != NULL) {
			EventPipeTracepoint *default_tracepoint = ep_rt_object_alloc (EventPipeTracepoint);
			EP_ASSERT(default_tracepoint != NULL);
			if (event_reg (session->user_events_data_fd, default_tracepoint_name, &default_tracepoint->write_index, &default_tracepoint->enabled) == -1) {
				ep_raise_error ();
			}

			session_provider->default_tracepoint = default_tracepoint;
		}
	}

ep_on_exit:
	return;

ep_on_error:
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
	uint32_t user_events_data_fd)
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
		ep_raise_error_if_nok (user_events_data_fd != 0);
		// Transfer ownership of the user_events_data file descriptor to the EventPipe Session.
		instance->user_events_data_fd = user_events_data_fd;
		// With the user_events_data file, register tracepoints for each provider's tracepoint configurations
		ep_session_user_events_tracepoints_init (instance);
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
		ep_provider_config_get_filter_data (&rundown_provider));

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

	bool ignored;
	ep_session_write_all_buffers_to_file (session, &ignored);
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

bool
ep_tracepoint_write (
	EventPipeSession *session,
	ep_rt_thread_handle_t thread,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *ep_event_payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	ep_rt_thread_handle_t event_thread,
	EventPipeStackContents *stack)
{
#if HAVE_SYS_UIO_H
	EventPipeProvider *provider = ep_event_get_provider (ep_event);

	EventPipeSessionProviderList *session_provider_list = ep_session_get_providers (session);
	EventPipeSessionProvider *session_provider = ep_session_provider_list_find_by_name (ep_session_provider_list_get_providers (session_provider_list), ep_provider_get_provider_name (provider));

	uint32_t event_id = ep_event_get_event_id (ep_event);

	dn_umap_t *event_id_to_tracepoint_map = ep_session_provider_get_event_id_to_tracepoint_map (session_provider);
	dn_umap_it_t found1 = dn_umap_find (event_id_to_tracepoint_map, &event_id);
	EventPipeTracepoint *tracepoint = NULL;
	if (dn_umap_it_end (found1)) {
		// If we don't have a tracepoint for this event_id, use the default tracepoint
		tracepoint = session_provider->default_tracepoint;
	} else {
		// We have a tracepoint for this event_id
		tracepoint = dn_umap_it_value_t (found1, EventPipeTracepoint *);
	}
	if (tracepoint == NULL) {
		// No tracepoint for this event_id and no default tracepoint, so we can't write the event.
		return false;
	}

	if (tracepoint->enabled == 0) {
		// No listeners
		return false;
	}

	struct iovec io[9];

	io[0].iov_base = &tracepoint->write_index;      // __u32 from event_reg
	io[0].iov_len = sizeof(tracepoint->write_index);

	uint8_t version = 0x01; // hardcoded for the first tracepoint format version
	io[1].iov_base = &version;
	io[1].iov_len = sizeof(version);

	uint16_t truncated_event_id = event_id & 0xFFFF;
	io[2].iov_base = &truncated_event_id;
	io[2].iov_len = sizeof(truncated_event_id);

	// The data transmitted in version 1 is
	// extension - a NetTrace V6 LabelList
	// payload - the EventPipe Event Payload
	// meta - the EventPipe Event metadata

	bool activity_id_is_empty = true;
	if (activity_id != NULL) {
		// If the activity_id is not empty, then we don't consider it empty.
		for (int i = 0; i < EP_ACTIVITY_ID_SIZE; ++i) {
			if (activity_id[i] != 0) {
				activity_id_is_empty = false;
				break;
			}
		}
	}
	bool related_activity_id_is_empty = true;
	if (related_activity_id != NULL) {
		// If the related_activity_id is not empty, then we don't consider it empty.
		for (int i = 0; i < EP_ACTIVITY_ID_SIZE; ++i) {
			if (related_activity_id[i] != 0) {
				related_activity_id_is_empty = false;
				break;
			}
		}
	}
	// extension generation helper
	uint16_t extension_len = 0;
	if (activity_id != NULL && !activity_id_is_empty)
		extension_len += 1 + EP_ACTIVITY_ID_SIZE; // ActivityId kind + value
	if (related_activity_id != NULL && !related_activity_id_is_empty)
		extension_len += 1 + EP_ACTIVITY_ID_SIZE; // RelatedActivityId kind + value

	uint8_t *extension = NULL;
	if (extension_len > 0) {
		extension = (uint8_t *)malloc(extension_len);
		EP_ASSERT(extension != NULL);
		uint16_t offset = 0;
		if (activity_id != NULL && !activity_id_is_empty) {
			// If there is a related_activity_id, use 0x81 (more follows), else 0x01 (no more follows)
			extension[offset] = (related_activity_id != NULL && !related_activity_id_is_empty) ? 0x81 : 0x01;
			memcpy(extension + offset + 1, activity_id, EP_ACTIVITY_ID_SIZE);
			offset += 1 + EP_ACTIVITY_ID_SIZE;
		}
		if (related_activity_id != NULL && !related_activity_id_is_empty) {
			// RelatedActivityId: 0x02 (no more follows)
			extension[offset] = 0x02;
			memcpy(extension + offset + 1, related_activity_id, EP_ACTIVITY_ID_SIZE);
			offset += 1 + EP_ACTIVITY_ID_SIZE;
		}
	}

	uint32_t payload_len = ep_event_payload_get_size (ep_event_payload);
	if ((payload_len & 0xFFFF0000) != 0) {
		// Payload is too large, we can't write it.
		return false;
	}
	uint8_t *payload = (uint8_t *)malloc (payload_len);
	EP_ASSERT (payload != NULL);
	ep_event_payload_copy_data (ep_event_payload, payload);

	// meta
	const uint8_t *metadata = ep_event_get_metadata (ep_event);
	uint32_t metadata_len = ep_event_get_metadata_len (ep_event);

	// calculated __rel_loc values
	uint32_t meta_rel_loc = metadata_len << 16 | ((extension_len + payload_len) & 0xFFFF);
	uint32_t payload_rel_loc = payload_len << 16 | ((sizeof(meta_rel_loc) + extension_len) & 0xFFFF);
	uint32_t extension_rel_loc = extension_len << 16 | ((sizeof(payload_rel_loc) + sizeof(meta_rel_loc)) & 0xFFFF);
	io[3].iov_base = &extension_rel_loc;
	io[3].iov_len = sizeof(extension_rel_loc);

	io[4].iov_base = &payload_rel_loc;
	io[4].iov_len = sizeof(payload_rel_loc);

	io[5].iov_base = &meta_rel_loc;
	io[5].iov_len = sizeof(meta_rel_loc);

	// Actual data buffers
	io[6].iov_base = extension;
	io[6].iov_len = extension_len;

	io[7].iov_base = payload;
	io[7].iov_len = payload_len;

	io[8].iov_base = (void *)metadata;
	io[8].iov_len = metadata_len;
	int32_t result = writev(session->user_events_data_fd, (const struct iovec *)io, 9);
	if (result == -1) {
		// Failed to write the event, return false.
		// return false;
		return false;
	}

	return true;
#else // HAVE_SYS_UIO_H
	// Not Supported
	return false;
#endif // HAVE_SYS_UIO_H
}

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
		if (session->synchronous_callback) {
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
		} else if (session->session_type == EP_SESSION_TYPE_USEREVENTS) {
			EP_ASSERT (session->user_events_data_fd != 0);
			result = ep_tracepoint_write (
				session,
				thread,
				ep_event,
				payload,
				activity_id,
				related_activity_id,
				event_thread,
				stack);
		} else {
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
	if (session_type == EP_SESSION_TYPE_SYNCHRONOUS || session_type == EP_SESSION_TYPE_USEREVENTS)
		return false;
	
	return true;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_session;
const char quiet_linker_empty_file_warning_eventpipe_session = 0;
#endif
