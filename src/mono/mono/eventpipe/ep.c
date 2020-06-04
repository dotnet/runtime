#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep.h"

// Option to include all internal source files into ep.c.
#ifdef EP_INCLUDE_SOURCE_FILES
#define EP_FORCE_INCLUDE_SOURCE_FILES
#include "ep-block.c"
#include "ep-buffer-manager.c"
#include "ep-config.c"
#include "ep-event-instance.c"
#include "ep-event-source.c"
#include "ep-file.c"
#include "ep-metadata-generator.c"
#include "ep-provider.c"
#include "ep-session.c"
#include "ep-session-provider.c"
#include "ep-stream.c"
#include "ep-thread.c"
#endif

/*
 * Forward declares of all static functions.
 */

static
bool
enabled_lock_held (void);

static
uint32_t
generate_session_index_lock_held (void);

static
bool
is_session_id_in_collection_lock_held (EventPipeSessionID id);

static
EventPipeSessionID
enable_lock_held (
	const ep_char8_t *output_path,
	uint32_t circular_buffer_size_in_mb,
	const EventPipeProviderConfiguration *providers,
	uint32_t providers_len,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	bool rundown_requested,
	IpcStream *stream,
	bool enable_sample_profiler,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

static
void
log_process_info_event (EventPipeEventSource *event_source);

static
void
disable_lock_held (
	EventPipeSessionID id,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

static
void
disable (
	EventPipeSessionID id,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

/*
 * Global volatile varaibles, only to be accessed through inlined volatile access functions.
 */

volatile EventPipeState _ep_state = EP_STATE_NOT_INITIALIZED;

volatile uint32_t _ep_number_of_sessions = 0;

volatile EventPipeSession *_ep_sessions [EP_MAX_NUMBER_OF_SESSIONS] = { 0 };

volatile uint64_t _ep_allow_write = 0;

/*
 * EventPipe.
 */

static
bool
enabled_lock_held (void)
{
	ep_rt_config_requires_lock_held ();
	return (ep_volatile_load_eventpipe_state_without_barrier () >= EP_STATE_INITIALIZED &&
			ep_volatile_load_number_of_sessions_without_barrier () > 0);
}

static
uint32_t
generate_session_index_lock_held (void)
{
	ep_rt_config_requires_lock_held ();

	for (uint32_t i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; ++i)
		if (ep_volatile_load_session_without_barrier (i) == NULL)
			return i;
	return EP_MAX_NUMBER_OF_SESSIONS;
}

static
bool
is_session_id_in_collection_lock_held (EventPipeSessionID session_id)
{
	ep_rt_config_requires_lock_held ();
	EP_ASSERT (session_id != 0);

	const EventPipeSession *const session = (EventPipeSession *)session_id;
	for (uint32_t i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; ++i) {
		if (ep_volatile_load_session (i) == session) {
			EP_ASSERT (i == ep_session_get_index (session));
			return true;
		}
	}

	return false;
}

static
EventPipeSessionID
enable_lock_held (
	const ep_char8_t *output_path,
	uint32_t circular_buffer_size_in_mb,
	const EventPipeProviderConfiguration *providers,
	uint32_t providers_len,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	bool rundown_requested,
	IpcStream *stream,
	bool enable_sample_profiler,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	ep_rt_config_requires_lock_held ();
	EP_ASSERT (format < EP_SERIALIZATION_FORMAT_COUNT);
	EP_ASSERT (circular_buffer_size_in_mb > 0);
	EP_ASSERT (providers_len > 0 && providers != NULL);
	EP_ASSERT ((session_type == EP_SESSION_TYPE_FILE && output_path != NULL) ||(session_type == EP_SESSION_TYPE_IPCSTREAM && stream != NULL));

	EventPipeSession *session = NULL;
	EventPipeSessionID session_id = 0;
	uint32_t session_index = 0;

	ep_raise_error_if_nok (ep_volatile_load_eventpipe_state () == EP_STATE_INITIALIZED);

	session_index = generate_session_index_lock_held ();
	ep_raise_error_if_nok (session_index < EP_MAX_NUMBER_OF_SESSIONS);

	session = ep_session_alloc (
		session_index,
		output_path,
		stream,
		session_type,
		format,
		rundown_requested,
		circular_buffer_size_in_mb,
		providers,
		providers_len,
		FALSE);

	ep_raise_error_if_nok (session != NULL && ep_session_is_valid (session) == true);

	session_id = (EventPipeSessionID)session;

	// Return if the index is invalid.
	if (ep_session_get_index (session) >= EP_MAX_NUMBER_OF_SESSIONS) {
		EP_ASSERT (!"Session index was out of range.");
		ep_raise_error ();
	}

	if (ep_volatile_load_number_of_sessions () >= EP_MAX_NUMBER_OF_SESSIONS) {
		EP_ASSERT (!"max number of sessions reached.");
		ep_raise_error ();
	}

	// Register the SampleProfiler the very first time (if supported).
	ep_rt_sample_profiler_init (provider_callback_data_queue);

	// Enable the EventPipe EventSource.
	ep_event_source_enable (ep_event_source_get (), session);

	// Save the session.
	if (ep_volatile_load_session_without_barrier (ep_session_get_index (session)) != NULL) {
		EP_ASSERT (!"Attempting to override an existing session.");
		ep_raise_error ();
	}

	ep_volatile_store_session (ep_session_get_index (session), session);

	ep_volatile_store_allow_write (ep_volatile_load_allow_write () | ep_session_get_mask (session));
	ep_volatile_store_number_of_sessions (ep_volatile_load_number_of_sessions () + 1);

	// Enable tracing.
	ep_config_enable_disable_lock_held (ep_config_get (), session, provider_callback_data_queue, true);

	session = NULL;

	// Enable the sample profiler (if supported).
	if (enable_sample_profiler)
		ep_rt_sample_profiler_enable ();

ep_on_exit:
	ep_rt_config_requires_lock_held ();
	return session_id;

ep_on_error:
	ep_session_free (session);

	session_id = 0;
	ep_exit_error_handler ();
}

static
void
log_process_info_event (EventPipeEventSource *event_source)
{
	// Get the managed command line.
	const ep_char8_t *cmd_line = ep_rt_managed_command_line_get ();

	if (cmd_line == NULL)
		cmd_line = ep_rt_command_line_get ();

	// Log the process information event.
	ep_char16_t *cmd_line_utf16 = ep_rt_utf8_to_utf16_string (cmd_line, -1);
	if (cmd_line_utf16 != NULL) {
		ep_event_source_send_process_info (event_source, cmd_line_utf16);
		ep_rt_utf16_string_free (cmd_line_utf16);
	}
}

static
void
disable_lock_held (
	EventPipeSessionID id,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	ep_rt_config_requires_lock_held ();
	EP_ASSERT (id != 0);
	EP_ASSERT (ep_volatile_load_number_of_sessions () > 0);

	if (is_session_id_in_collection_lock_held (id)) {
		EventPipeSession *const session = (EventPipeSession *)id;

		// Disable the profiler.
		ep_rt_sample_profiler_disable ();

		// Log the process information event.
		log_process_info_event (ep_event_source_get ());

		// Disable session tracing.
		ep_config_enable_disable_lock_held (ep_config_get (), session, provider_callback_data_queue, false);

		ep_session_disable (session); // WriteAllBuffersToFile, and remove providers.

		// Do rundown before fully stopping the session unless rundown wasn't requested
		if (ep_session_get_rundown_requested (session)) {
			ep_session_enable_rundown_lock_held (session); // Set Rundown provider.
			EventPipeThread *const thread = ep_thread_get ();
			if (thread != NULL) {
				ep_thread_set_as_rundown_thread (thread, session);
				{
					ep_config_enable_disable_lock_held (ep_config_get (), session, provider_callback_data_queue, true);
					{
						ep_session_execute_rundown_lock_held (session);
					}
					ep_config_enable_disable_lock_held (ep_config_get (), session, provider_callback_data_queue, false);
				}
				ep_thread_set_as_rundown_thread (thread, NULL);
			} else {
				EP_ASSERT (!"Failed to get or create the EventPipeThread for rundown events.");
			}
		}

		ep_volatile_store_allow_write (ep_volatile_load_allow_write () & ~(ep_session_get_mask (session)));
		ep_session_suspend_write_event_lock_held (session);

		bool ignored;
		ep_session_write_all_buffers_to_file (session, &ignored); // Flush the buffers to the stream/file

		ep_volatile_store_number_of_sessions (ep_volatile_load_number_of_sessions () - 1);

		// At this point, we should not be writing events to this session anymore
		// This is a good time to remove the session from the array.
		EP_ASSERT (ep_volatile_load_session (ep_session_get_index (session)) == session);

		// Remove the session from the array, and mask.
		ep_volatile_store_session (ep_session_get_index (session), NULL);

		// Write a final sequence point to the file now that all events have
		// been emitted.
		ep_session_write_sequence_point_unbuffered_lock_held (session);

		ep_session_free (session);

		// Providers can't be deleted during tracing because they may be needed when serializing the file.
		ep_config_delete_deferred_providers_lock_held (ep_config_get ());
	}

	ep_rt_config_requires_lock_held ();
	return;
}

static
void
disable (
	EventPipeSessionID id,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	ep_rt_config_requires_lock_not_held ();

	EP_CONFIG_LOCK_ENTER
		disable_lock_held (id, provider_callback_data_queue);
	EP_CONFIG_LOCK_EXIT

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

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
	bool enable_sample_profiler)
{
	ep_rt_config_requires_lock_not_held ();
	EP_ASSERT (format < EP_SERIALIZATION_FORMAT_COUNT);
	EP_ASSERT (circular_buffer_size_in_mb > 0);
	EP_ASSERT (providers_len > 0 && providers != NULL);

	// If the state or arguments are invalid, bail here.
	if (session_type == EP_SESSION_TYPE_FILE && output_path == NULL)
		return 0;
	if (session_type == EP_SESSION_TYPE_IPCSTREAM && stream == NULL)
		return 0;

	EventPipeSessionID session_id = 0;
	EventPipeProviderCallbackDataQueue callback_data_queue;
	EventPipeProviderCallbackData provider_callback_data;
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue = ep_provider_callback_data_queue_init (&callback_data_queue);

	EP_CONFIG_LOCK_ENTER
		session_id = enable_lock_held (
			output_path,
			circular_buffer_size_in_mb,
			providers,
			providers_len,
			session_type,
			format,
			rundown_requested,
			stream,
			enable_sample_profiler,
			provider_callback_data_queue);
	EP_CONFIG_LOCK_EXIT

	while (ep_provider_callback_data_queue_try_dequeue (provider_callback_data_queue, &provider_callback_data))
		ep_provider_invoke_callback (&provider_callback_data);

ep_on_exit:
	ep_provider_callback_data_queue_fini (provider_callback_data_queue);
	ep_rt_config_requires_lock_not_held ();
	return session_id;

ep_on_error:
	session_id = 0;
	ep_exit_error_handler ();
}

void
ep_disable (EventPipeSessionID id)
{
	ep_rt_config_requires_lock_not_held ();

	//TODO: Why is this needed? Just to make sure thread is attached to runtime for
	//EP_GCX_PREEMP_ENTER/EP_GCX_PREEMP_EXIT to work?
	ep_rt_thread_setup ();

	if (id == 0)
		return;

	// Don't block GC during clean-up.
	EP_GCX_PREEMP_ENTER

		EventPipeProviderCallbackDataQueue callback_data_queue;
		EventPipeProviderCallbackData provider_callback_data;
		EventPipeProviderCallbackDataQueue *provider_callback_data_queue = ep_provider_callback_data_queue_init (&callback_data_queue);

		disable (id, provider_callback_data_queue);

		while (ep_provider_callback_data_queue_try_dequeue (provider_callback_data_queue, &provider_callback_data))
			ep_provider_invoke_callback (&provider_callback_data);

		ep_provider_callback_data_queue_fini (provider_callback_data_queue);

	EP_GCX_PREEMP_EXIT

	ep_rt_config_requires_lock_not_held ();
	return;
}

EventPipeSession *
ep_get_session (EventPipeSessionID session_id)
{
	ep_rt_config_requires_lock_not_held ();

	EP_CONFIG_LOCK_ENTER

		if (ep_volatile_load_eventpipe_state () == EP_STATE_NOT_INITIALIZED) {
			EP_ASSERT (!"EventPipe::GetSession invoked before EventPipe was initialized.");
			ep_raise_error_holding_lock ();
		}

		ep_raise_error_if_nok_holding_lock (is_session_id_in_collection_lock_held (session_id) == true);

	EP_CONFIG_LOCK_EXIT

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return (EventPipeSession *)session_id;

ep_on_error:
	session_id = 0;
	ep_exit_error_handler ();
}

bool
ep_enabled (void)
{
	//TODO: Validate if ever called without holding lock, if so should check be atomic?
	return (ep_volatile_load_eventpipe_state () >= EP_STATE_INITIALIZED &&
			ep_volatile_load_number_of_sessions () > 0);
}

EventPipeProvider *
ep_create_provider (
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	void *callback_data)
{
	ep_rt_config_requires_lock_not_held ();

	ep_return_null_if_nok (provider_name != NULL);

	EventPipeProvider *provider = NULL;
	EventPipeProviderCallbackDataQueue data_queue;
	EventPipeProviderCallbackData provider_callback_data;
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue = ep_provider_callback_data_queue_init (&data_queue);

	EP_CONFIG_LOCK_ENTER
		provider = ep_config_create_provider_lock_held (ep_config_get (), provider_name, callback_func, callback_data, provider_callback_data_queue);
		ep_raise_error_if_nok_holding_lock (provider != NULL);
	EP_CONFIG_LOCK_EXIT

	while (ep_provider_callback_data_queue_try_dequeue (provider_callback_data_queue, &provider_callback_data))
		ep_provider_invoke_callback (&provider_callback_data);

ep_on_exit:
	ep_provider_callback_data_queue_fini (provider_callback_data_queue);
	ep_rt_config_requires_lock_not_held ();
	return provider;

ep_on_error:
	ep_delete_provider (provider);

	provider = NULL;
	ep_exit_error_handler ();
}

void
ep_delete_provider (EventPipeProvider *provider)
{
	ep_rt_config_requires_lock_not_held ();

	ep_return_void_if_nok (provider != NULL);

	// Take the lock to make sure that we don't have a race
	// between disabling tracing and deleting a provider
	// where we hold a provider after tracing has been disabled.
	EP_CONFIG_LOCK_ENTER
		if (enabled_lock_held ()) {
			// Save the provider until the end of the tracing session.
			ep_provider_set_delete_deferred (provider, true);
		} else {
			ep_config_delete_provider_lock_held (ep_config_get (), provider);
		}
	EP_CONFIG_LOCK_EXIT

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

EventPipeProvider *
ep_get_provider (const ep_char8_t *provider_name)
{
	ep_rt_config_requires_lock_not_held ();

	ep_return_null_if_nok (provider_name != NULL);

	EventPipeProvider *provider = NULL;

	EP_CONFIG_LOCK_ENTER
		provider = ep_config_get_provider_lock_held (ep_config_get (), provider_name);
		ep_raise_error_if_nok_holding_lock (provider != NULL);
	EP_CONFIG_LOCK_EXIT

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return provider;

ep_on_error:
	provider = NULL;
	ep_exit_error_handler ();
}

void
ep_init (void)
{
	ep_rt_init ();
	ep_rt_config_requires_lock_not_held ();

	//TODO: Implement. Locking pattern between init/shutdown is racy but same as CoreCLR. Needs to be revisited.
	if (ep_volatile_load_eventpipe_state () != EP_STATE_NOT_INITIALIZED) {
		EP_ASSERT (!"EventPipe already initialized.");
		return;
	}

	for (uint32_t i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; ++i)
		ep_volatile_store_session (i, NULL);

	if (ep_config_init (ep_config_get ())) {
		EP_CONFIG_LOCK_ENTER
			ep_volatile_store_eventpipe_state_without_barrier (EP_STATE_INITIALIZED);
		EP_CONFIG_LOCK_EXIT
	}

	//TODO: Implement.

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_finish_init (void)
{
	//TODO: Implement.
}

void
ep_shutdown (void)
{
	ep_rt_config_requires_lock_not_held ();

	//TODO: Locking pattern between init/shutdown is racy but same as CoreCLR. Needs to be revisited.
	ep_return_void_if_nok (ep_volatile_load_eventpipe_state () != EP_STATE_SHUTTING_DOWN);
	ep_return_void_if_nok (!ep_rt_process_detach ());
	ep_return_void_if_nok (ep_volatile_load_eventpipe_state () == EP_STATE_INITIALIZED);

	EP_CONFIG_LOCK_ENTER
		ep_volatile_store_eventpipe_state_without_barrier (EP_STATE_SHUTTING_DOWN);
	EP_CONFIG_LOCK_EXIT

	for (uint32_t i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; ++i) {
		EventPipeSession *session = ep_volatile_load_session (i);
		if (session)
			ep_disable ((EventPipeSessionID)session);
	}

	// dotnet/coreclr: issue 24850: EventPipe shutdown race conditions
	// Deallocating providers/events here might cause AV if a WriteEvent
	// was to occur. Thus, we are not doing this cleanup.

	// // Remove EventPipeEventSource first since it tries to use the data structures that we remove below.
	// // We need to do this after disabling sessions since those try to write to EventPipeEventSource.
	// delete s_pEventSource;
	// s_pEventSource = nullptr;
	// s_config.Shutdown();

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	ep_rt_shutdown ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

EventPipeEventMetadataEvent *
ep_build_event_metadata_event (
	EventPipeEventInstance *event_instance,
	uint32_t metadata_id)
{
	return ep_config_build_event_metadata_event (ep_config_get (), event_instance, metadata_id);
}

void
ep_write_event (
	EventPipeEvent *ep_event,
	EventData *event_data,
	uint32_t event_data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	//TODO: Implement.
}

EventPipeEventInstance *
ep_get_next_event (EventPipeSessionID session_id)
{
	ep_rt_config_requires_lock_not_held ();

	// Only fetch the next event if a tracing session exists.
	// The buffer manager is not disposed until the process is shutdown.
	EventPipeSession *const session = ep_get_session (session_id);
	return session ? ep_session_get_next_event (session) : NULL;
}

EventPipeWaitHandle
ep_get_wait_handle (EventPipeSessionID session_id)
{
	EventPipeSession *const session = ep_get_session (session_id);
	return session ? ep_session_get_wait_event (session) : 0;
}

void
ep_start_streaming (EventPipeSessionID session_id)
{
	ep_rt_config_requires_lock_not_held ();

	EP_CONFIG_LOCK_ENTER
		ep_raise_error_if_nok_holding_lock (is_session_id_in_collection_lock_held (session_id) == true);
		ep_session_start_streaming_lock_held ((EventPipeSession *)session_id);
	EP_CONFIG_LOCK_EXIT

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

/*
 * EventPipePerf.
 */

uint64_t
ep_perf_counter_query (void)
{
	return ep_rt_perf_counter_query ();
}

uint64_t
ep_perf_frequency_query (void)
{
	return ep_rt_perf_frequency_query ();
}

/*
 * EventPipeProviderCallbackDataQueue.
 */

void
ep_provider_callback_data_queue_enqueue (
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue,
	EventPipeProviderCallbackData *provider_callback_data)
{
	ep_return_void_if_nok (provider_callback_data_queue != NULL);
	ep_rt_provider_callback_data_queue_push_tail (ep_provider_callback_data_queue_get_queue_ref (provider_callback_data_queue), ep_provider_callback_data_alloc_copy (provider_callback_data));
}

bool
ep_provider_callback_data_queue_try_dequeue (
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue,
	EventPipeProviderCallbackData *provider_callback_data)
{
	ep_return_false_if_nok (provider_callback_data_queue != NULL && ep_rt_provider_callback_data_queue_is_empty (ep_provider_callback_data_queue_get_queue_ref (provider_callback_data_queue)) != true);

	EventPipeProviderCallbackData *value = NULL;
	ep_rt_provider_callback_data_queue_pop_head (ep_provider_callback_data_queue_get_queue_ref (provider_callback_data_queue), &value);
	ep_provider_callback_data_init_copy (provider_callback_data, value);
	ep_provider_callback_data_free (value);

	return true;
}

#endif /* ENABLE_PERFTRACING */

extern const char quiet_linker_empty_file_warning_eventpipe;
const char quiet_linker_empty_file_warning_eventpipe = 0;
