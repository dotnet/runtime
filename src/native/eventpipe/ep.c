#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_EP_GETTER_SETTER
#include "ep.h"
#include "ep-config.h"
#include "ep-config-internals.h"
#include "ep-event.h"
#include "ep-event-payload.h"
#include "ep-event-source.h"
#include "ep-provider.h"
#include "ep-provider-internals.h"
#include "ep-session.h"
#include "ep-sample-profiler.h"

static bool _ep_can_start_threads = false;

static ep_rt_session_id_array_t _ep_deferred_enable_session_ids = { 0 };
static ep_rt_session_id_array_t _ep_deferred_disable_session_ids = { 0 };

static EventPipeIpcStreamFactorySuspendedPortsCallback _ep_ipc_stream_factory_suspended_ports_callback = NULL;

static ep_rt_execution_checkpoint_array_t _ep_rundown_execution_checkpoints = { 0 };

/*
 * Forward declares of all static functions.
 */

// _Requires_lock_held (ep)
static
bool
enabled (void);

// _Requires_lock_held (ep)
static
uint32_t
generate_session_index (void);

// _Requires_lock_held (ep)
static
bool
is_session_id_in_collection (EventPipeSessionID id);

// _Requires_lock_held (ep)
static
EventPipeSessionID
enable (
	const ep_char8_t *output_path,
	uint32_t circular_buffer_size_in_mb,
	const EventPipeProviderConfiguration *providers,
	uint32_t providers_len,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	bool rundown_requested,
	IpcStream *stream,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue,
	EventPipeSessionSynchronousCallback sync_callback,
	void *callback_additional_data);

static
void
log_process_info_event (EventPipeEventSource *event_source);

// _Requires_lock_held (ep)
static
void
disable_holding_lock (
	EventPipeSessionID id,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

static
void
disable_helper (EventPipeSessionID id);

static
void
write_event (
	EventPipeEvent *ep_event,
	EventPipeEventPayload *payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static
void
write_event_2 (
	ep_rt_thread_handle_t thread,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	ep_rt_thread_handle_t event_thread,
	EventPipeStackContents *stack);

static
const ep_char8_t *
get_next_config_value (const ep_char8_t *data, const ep_char8_t **start, const ep_char8_t **end);

static
ep_char8_t *
get_next_config_value_as_utf8_string (const ep_char8_t **data);

static
uint64_t
get_next_config_value_as_uint64_t (const ep_char8_t **data);

static
uint32_t
get_next_config_value_as_uint32_t (const ep_char8_t **data);

static
void
enable_default_session_via_env_variables (void);

static
bool
session_requested_sampling (EventPipeSession *session);

static
bool
ipc_stream_factory_any_suspended_ports (void);

/*
 * Global volatile varaibles, only to be accessed through inlined volatile access functions.
 */

volatile EventPipeState _ep_state = EP_STATE_NOT_INITIALIZED;

volatile uint32_t _ep_number_of_sessions = 0;

volatile EventPipeSession *_ep_sessions [EP_MAX_NUMBER_OF_SESSIONS] = { 0 };

volatile uint64_t _ep_allow_write = 0;

/*
 * EventFilterDescriptor.
 */

EventFilterDescriptor *
ep_event_filter_desc_alloc (
	uint64_t ptr,
	uint32_t size,
	uint32_t type)
{
	EventFilterDescriptor *instance = ep_rt_object_alloc (EventFilterDescriptor);
	ep_raise_error_if_nok (instance != NULL);
	ep_raise_error_if_nok (ep_event_filter_desc_init (instance, ptr, size, type) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_event_filter_desc_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventFilterDescriptor *
ep_event_filter_desc_init (
	EventFilterDescriptor *event_filter_desc,
	uint64_t ptr,
	uint32_t size,
	uint32_t type)
{
	EP_ASSERT (event_filter_desc != NULL);

	event_filter_desc->ptr = ptr;
	event_filter_desc->size = size;
	event_filter_desc->type = type;

	return event_filter_desc;
}

void
ep_event_filter_desc_fini (EventFilterDescriptor * filter_desc)
{
	;
}

void
ep_event_filter_desc_free (EventFilterDescriptor * filter_desc)
{
	ep_return_void_if_nok (filter_desc != NULL);

	ep_event_filter_desc_fini (filter_desc);
	ep_rt_object_free (filter_desc);
}

/*
 * EventPipeProviderCallbackDataQueue.
 */

EventPipeProviderCallbackDataQueue *
ep_provider_callback_data_queue_init (EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	EP_ASSERT (provider_callback_data_queue != NULL);
	ep_rt_provider_callback_data_queue_alloc (&provider_callback_data_queue->queue);
	return ep_rt_provider_callback_data_queue_is_valid (&provider_callback_data_queue->queue) ? provider_callback_data_queue : NULL;
}

void
ep_provider_callback_data_queue_fini (EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	ep_return_void_if_nok (provider_callback_data_queue != NULL);
	ep_rt_provider_callback_data_queue_free (&provider_callback_data_queue->queue);
}

/*
 * EventPipeProviderCallbackData.
 */

EventPipeProviderCallbackData *
ep_provider_callback_data_alloc (
	const ep_char8_t *filter_data,
	EventPipeCallback callback_function,
	void *callback_data,
	int64_t keywords,
	EventPipeEventLevel provider_level,
	bool enabled)
{
	EventPipeProviderCallbackData *instance = ep_rt_object_alloc (EventPipeProviderCallbackData);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_provider_callback_data_init (
		instance,
		filter_data,
		callback_function,
		callback_data,
		keywords,
		provider_level,
		enabled) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_provider_callback_data_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeProviderCallbackData *
ep_provider_callback_data_alloc_copy (EventPipeProviderCallbackData *provider_callback_data_src)
{
	EventPipeProviderCallbackData *instance = ep_rt_object_alloc (EventPipeProviderCallbackData);
	ep_raise_error_if_nok (instance != NULL);

	if (provider_callback_data_src) {
		*instance = *provider_callback_data_src;
		instance->filter_data = ep_rt_utf8_string_dup (provider_callback_data_src->filter_data);
	}

ep_on_exit:
	return instance;

ep_on_error:
	ep_provider_callback_data_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeProviderCallbackData *
ep_provider_callback_data_alloc_move (EventPipeProviderCallbackData *provider_callback_data_src)
{
	EventPipeProviderCallbackData *instance = ep_rt_object_alloc (EventPipeProviderCallbackData);
	ep_raise_error_if_nok (instance != NULL);

	if (provider_callback_data_src) {
		*instance = *provider_callback_data_src;
		memset (provider_callback_data_src, 0, sizeof (*provider_callback_data_src));
	}

ep_on_exit:
	return instance;

ep_on_error:
	ep_provider_callback_data_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeProviderCallbackData *
ep_provider_callback_data_init (
	EventPipeProviderCallbackData *provider_callback_data,
	const ep_char8_t *filter_data,
	EventPipeCallback callback_function,
	void *callback_data,
	int64_t keywords,
	EventPipeEventLevel provider_level,
	bool enabled)
{
	EP_ASSERT (provider_callback_data != NULL);

	provider_callback_data->filter_data = ep_rt_utf8_string_dup (filter_data);
	provider_callback_data->callback_function = callback_function;
	provider_callback_data->callback_data = callback_data;
	provider_callback_data->keywords = keywords;
	provider_callback_data->provider_level = provider_level;
	provider_callback_data->enabled = enabled;

	return provider_callback_data;
}

EventPipeProviderCallbackData *
ep_provider_callback_data_init_copy (
	EventPipeProviderCallbackData *provider_callback_data_dst,
	EventPipeProviderCallbackData *provider_callback_data_src)
{
	EP_ASSERT (provider_callback_data_dst != NULL);
	EP_ASSERT (provider_callback_data_src != NULL);

	*provider_callback_data_dst = *provider_callback_data_src;
	provider_callback_data_dst->filter_data = ep_rt_utf8_string_dup (provider_callback_data_src->filter_data);
	return provider_callback_data_dst;
}

EventPipeProviderCallbackData *
ep_provider_callback_data_init_move (
	EventPipeProviderCallbackData *provider_callback_data_dst,
	EventPipeProviderCallbackData *provider_callback_data_src)
{
	EP_ASSERT (provider_callback_data_dst != NULL);
	EP_ASSERT (provider_callback_data_src != NULL);

	*provider_callback_data_dst = *provider_callback_data_src;
	memset (provider_callback_data_src, 0, sizeof (*provider_callback_data_src));
	return provider_callback_data_dst;
}

void
ep_provider_callback_data_fini (EventPipeProviderCallbackData *provider_callback_data)
{
	ep_return_void_if_nok (provider_callback_data != NULL);
	ep_rt_utf8_string_free (provider_callback_data->filter_data);
}

void
ep_provider_callback_data_free (EventPipeProviderCallbackData *provider_callback_data)
{
	ep_return_void_if_nok (provider_callback_data != NULL);
	ep_provider_callback_data_fini (provider_callback_data);
	ep_rt_object_free (provider_callback_data);
}

/*
 * EventPipeProviderConfiguration.
 */

EventPipeProviderConfiguration *
ep_provider_config_init (
	EventPipeProviderConfiguration *provider_config,
	const ep_char8_t *provider_name,
	uint64_t keywords,
	EventPipeEventLevel logging_level,
	const ep_char8_t *filter_data)
{
	EP_ASSERT (provider_config != NULL);
	EP_ASSERT (provider_name != NULL);

	provider_config->provider_name = provider_name;
	provider_config->keywords = keywords;
	provider_config->logging_level = logging_level;
	provider_config->filter_data = filter_data;

	// Runtime specific rundown provider configuration.
	ep_rt_provider_config_init (provider_config);

	return provider_config;
}

void
ep_provider_config_fini (EventPipeProviderConfiguration *provider_config)
{
	;
}

/*
 * EventPipeExecutionCheckpoint.
 */

EventPipeExecutionCheckpoint *
ep_execution_checkpoint_alloc (
	const ep_char8_t *name,
	ep_timestamp_t timestamp)
{
	EventPipeExecutionCheckpoint *instance = ep_rt_object_alloc (EventPipeExecutionCheckpoint);
	ep_raise_error_if_nok (instance != NULL);

	instance->name = name ? ep_rt_utf8_string_dup (name) : NULL;
	instance->timestamp = timestamp;

ep_on_exit:
	return instance;

ep_on_error:
	ep_execution_checkpoint_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_execution_checkpoint_free (EventPipeExecutionCheckpoint *execution_checkpoint)
{
	ep_return_void_if_nok (execution_checkpoint != NULL);
	ep_rt_object_free (execution_checkpoint);
}

/*
 * EventPipe.
 */

static
bool
enabled (void)
{
	ep_requires_lock_held ();
	return (ep_volatile_load_eventpipe_state_without_barrier () >= EP_STATE_INITIALIZED &&
			ep_volatile_load_number_of_sessions_without_barrier () > 0);
}

static
uint32_t
generate_session_index (void)
{
	ep_requires_lock_held ();

	for (uint32_t i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; ++i)
		if (ep_volatile_load_session_without_barrier (i) == NULL)
			return i;
	return EP_MAX_NUMBER_OF_SESSIONS;
}

static
bool
is_session_id_in_collection (EventPipeSessionID session_id)
{
	EP_ASSERT (session_id != 0);

	ep_requires_lock_held ();

	const EventPipeSession *const session = (EventPipeSession *)(uintptr_t)session_id;
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
enable (
	const ep_char8_t *output_path,
	uint32_t circular_buffer_size_in_mb,
	const EventPipeProviderConfiguration *providers,
	uint32_t providers_len,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	bool rundown_requested,
	IpcStream *stream,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue,
	EventPipeSessionSynchronousCallback sync_callback,
	void *callback_additional_data)
{
	EP_ASSERT (format < EP_SERIALIZATION_FORMAT_COUNT);
	EP_ASSERT (session_type == EP_SESSION_TYPE_SYNCHRONOUS || circular_buffer_size_in_mb > 0);
	EP_ASSERT (providers_len > 0 && providers != NULL);

	ep_requires_lock_held ();

	EventPipeSession *session = NULL;
	EventPipeSessionID session_id = 0;
	uint32_t session_index = 0;

	ep_raise_error_if_nok (ep_volatile_load_eventpipe_state () == EP_STATE_INITIALIZED);

	session_index = generate_session_index ();
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
		sync_callback,
		callback_additional_data);

	ep_raise_error_if_nok (session != NULL && ep_session_is_valid (session));

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
	ep_sample_profiler_init (provider_callback_data_queue);

	// Enable the EventPipe EventSource.
	ep_raise_error_if_nok (ep_event_source_enable (ep_event_source_get (), session));

	// Save the session.
	if (ep_volatile_load_session_without_barrier (ep_session_get_index (session)) != NULL) {
		EP_ASSERT (!"Attempting to override an existing session.");
		ep_raise_error ();
	}

	ep_volatile_store_session (ep_session_get_index (session), session);

	ep_volatile_store_allow_write (ep_volatile_load_allow_write () | ep_session_get_mask (session));
	ep_volatile_store_number_of_sessions (ep_volatile_load_number_of_sessions () + 1);

	// Enable tracing.
	config_enable_disable (ep_config_get (), session, provider_callback_data_queue, true);

	if (session_requested_sampling (session))
		ep_sample_profiler_enable ();

ep_on_exit:
	ep_requires_lock_held ();
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
	const ep_char8_t *cmd_line = ep_rt_diagnostics_command_line_get ();

	// Log the process information event.
	ep_event_source_send_process_info (event_source, cmd_line);
}

static
void
disable_holding_lock (
	EventPipeSessionID id,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	EP_ASSERT (id != 0);
	EP_ASSERT (ep_volatile_load_number_of_sessions () > 0);

	ep_requires_lock_held ();

	if (is_session_id_in_collection (id)) {
		EventPipeSession *const session = (EventPipeSession *)(uintptr_t)id;

		if (session_requested_sampling (session)) {
			// Disable the profiler.
			ep_sample_profiler_disable ();
		}

		// Log the process information event.
		log_process_info_event (ep_event_source_get ());

		// Disable session tracing.
		config_enable_disable (ep_config_get (), session, provider_callback_data_queue, false);

		ep_session_disable (session); // WriteAllBuffersToFile, and remove providers.

		// Do rundown before fully stopping the session unless rundown wasn't requested
		if (ep_session_get_rundown_requested (session) && _ep_can_start_threads) {
			ep_session_enable_rundown (session); // Set Rundown provider.
			EventPipeThread *const thread = ep_thread_get_or_create ();
			if (thread != NULL) {
				ep_thread_set_as_rundown_thread (thread, session);
				{
					config_enable_disable (ep_config_get (), session, provider_callback_data_queue, true);
					{
						ep_session_execute_rundown (session, &_ep_rundown_execution_checkpoints);
					}
					config_enable_disable(ep_config_get (), session, provider_callback_data_queue, false);
				}
				ep_thread_set_as_rundown_thread (thread, NULL);
			} else {
				EP_ASSERT (!"Failed to get or create the EventPipeThread for rundown events.");
			}
		}

		ep_volatile_store_allow_write (ep_volatile_load_allow_write () & ~(ep_session_get_mask (session)));

		// Remove the session from the array before calling ep_session_suspend_write_event. This way
		// we can guarantee that either the event write got the pointer and will complete
		// the write successfully, or it gets NULL and will bail.
		EP_ASSERT (ep_volatile_load_session (ep_session_get_index (session)) == session);
		ep_volatile_store_session (ep_session_get_index (session), NULL);

		ep_session_suspend_write_event (session);

		bool ignored;
		ep_session_write_all_buffers_to_file (session, &ignored); // Flush the buffers to the stream/file

		ep_volatile_store_number_of_sessions (ep_volatile_load_number_of_sessions () - 1);

		// Write a final sequence point to the file now that all events have
		// been emitted.
		ep_session_write_sequence_point_unbuffered (session);

		ep_session_free (session);

		// Providers can't be deleted during tracing because they may be needed when serializing the file.
		config_delete_deferred_providers(ep_config_get ());
	}

	ep_requires_lock_held ();
	return;
}

static
void
disable_helper (EventPipeSessionID id)
{
	ep_requires_lock_not_held ();

	if (_ep_can_start_threads)
		ep_rt_thread_setup ();

	if (id == 0)
		return;

	// Don't block GC during clean-up.
	EP_GCX_PREEMP_ENTER

		EventPipeProviderCallbackDataQueue callback_data_queue;
		EventPipeProviderCallbackData provider_callback_data;
		EventPipeProviderCallbackDataQueue *provider_callback_data_queue = ep_provider_callback_data_queue_init (&callback_data_queue);

		EP_LOCK_ENTER (section1)
			if (ep_volatile_load_number_of_sessions () > 0)
				disable_holding_lock (id, provider_callback_data_queue);
		EP_LOCK_EXIT (section1)

		while (ep_provider_callback_data_queue_try_dequeue (provider_callback_data_queue, &provider_callback_data)) {
			ep_rt_prepare_provider_invoke_callback (&provider_callback_data);
			provider_invoke_callback (&provider_callback_data);
			ep_provider_callback_data_fini (&provider_callback_data);
		}

		ep_provider_callback_data_queue_fini (provider_callback_data_queue);

#ifdef EP_CHECKED_BUILD
		if (ep_volatile_load_number_of_sessions () == 0)
			EP_ASSERT (ep_rt_providers_validate_all_disabled ());
#endif

	EP_GCX_PREEMP_EXIT

ep_on_exit:
	ep_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

static
void
write_event (
	EventPipeEvent *ep_event,
	EventPipeEventPayload *payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (ep_event != NULL);
	EP_ASSERT (payload != NULL);

	// We can't proceed if tracing is not initialized.
	ep_return_void_if_nok (ep_volatile_load_eventpipe_state () >= EP_STATE_INITIALIZED);

	// Exit early if the event is not enabled.
	ep_return_void_if_nok (ep_event_is_enabled (ep_event));

	// Get current thread.
	ep_rt_thread_handle_t thread = ep_rt_thread_get_handle ();

	// If the activity id isn't specified AND we are in a eventpipe thread, pull it from the current thread.
	// If pThread is NULL (we aren't in writing from a managed thread) then activity_id can be NULL
	if (activity_id == NULL && thread != NULL)
		activity_id = ep_thread_get_activity_id_cref (ep_thread_get_activity_id_handle ());

	write_event_2 (
		thread,
		ep_event,
		payload,
		activity_id,
		related_activity_id,
		NULL,
		NULL);
}

static
void
write_event_2 (
	ep_rt_thread_handle_t thread,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	ep_rt_thread_handle_t event_thread,
	EventPipeStackContents *stack)
{
	EP_ASSERT (ep_event != NULL);
	EP_ASSERT (payload != NULL);

	// We can't proceed if tracing is not initialized.
	ep_return_void_if_nok (ep_volatile_load_eventpipe_state () >= EP_STATE_INITIALIZED);

	EventPipeThread *const current_thread = ep_thread_get_or_create ();
	if (!current_thread) {
		EP_ASSERT (!"Failed to get or create an EventPipeThread.");
		return;
	}

	if (ep_thread_is_rundown_thread (current_thread)) {
		EventPipeSession *const rundown_session = ep_thread_get_rundown_session (current_thread);
		EP_ASSERT (rundown_session != NULL);
		EP_ASSERT (thread != NULL);

		uint8_t *data = ep_event_payload_get_flat_data (payload);
		if (thread != NULL && rundown_session != NULL && data != NULL) {
			ep_session_write_event (
				rundown_session,
				thread,
				ep_event,
				payload,
				activity_id,
				related_activity_id,
				event_thread,
				stack);
		}
	} else {
		for (uint32_t i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; ++i) {
			if ((ep_volatile_load_allow_write () & ((uint64_t)1 << i)) == 0)
				continue;

			// Now that we know this session is probably live we pay the perf cost of the memory barriers
			// Setting this flag lets a thread trying to do a concurrent disable that it is not safe to delete
			// session ID i. The if check above also ensures that once the session is unpublished this thread
			// will eventually stop ever storing ID i into the WriteInProgress flag. This is important to
			// guarantee termination of the YIELD_WHILE loop in SuspendWriteEvents.
			ep_thread_set_session_write_in_progress (current_thread, i);
			{
				EventPipeSession *const session = ep_volatile_load_session (i);
				// Disable is allowed to set s_pSessions[i] = NULL at any time and that may have occured in between
				// the check and the load
				if (session != NULL) {
					ep_session_write_event (
						session,
						thread,
						ep_event,
						payload,
						activity_id,
						related_activity_id,
						event_thread,
						stack);
				}
			}
			// Do not reference session past this point, we are signaling Disable() that it is safe to
			// delete it
			ep_thread_set_session_write_in_progress (current_thread, UINT32_MAX);
		}
	}
}

static
const ep_char8_t *
get_next_config_value (const ep_char8_t *data, const ep_char8_t **start, const ep_char8_t **end)
{
	EP_ASSERT (data != NULL);
	EP_ASSERT (start != NULL);
	EP_ASSERT (end != NULL);

	*start = data;
	while (*data != '\0' && *data != ':')
		data++;

	*end = data;

	return *data != '\0' ? ++data : NULL;
}

static
ep_char8_t *
get_next_config_value_as_utf8_string (const ep_char8_t **data)
{
	EP_ASSERT (data != NULL);

	ep_char8_t *buffer = NULL;

	const ep_char8_t *start = NULL;
	const ep_char8_t *end = NULL;
	*data = get_next_config_value (*data, &start, &end);

	ptrdiff_t byte_len = end - start;
	if (byte_len != 0)
		buffer = ep_rt_utf8_string_dup_range(start, end);

	return buffer;
}

static
uint64_t
get_next_config_value_as_uint64_t (const ep_char8_t **data)
{
	EP_ASSERT (data != NULL);

	ep_char8_t *value_as_utf8 = get_next_config_value_as_utf8_string (data);

	uint64_t value = UINT64_MAX;
	if (value_as_utf8) {
		value = (uint64_t)strtoull (value_as_utf8, NULL, 16);
		ep_rt_utf8_string_free (value_as_utf8);
	}
	return value;
}

static
uint32_t
get_next_config_value_as_uint32_t (const ep_char8_t **data)
{
	EP_ASSERT (data != NULL);

	ep_char8_t *value_as_utf8 = get_next_config_value_as_utf8_string (data);

	uint32_t value = UINT32_MAX;
	if (value_as_utf8) {
		value = (uint32_t)strtoul (value_as_utf8, NULL, 10);
		ep_rt_utf8_string_free (value_as_utf8);
	}
	return value;
}

//
// If EventPipe environment variables are specified, parse them and start a session.
//
static
void
enable_default_session_via_env_variables (void)
{
	ep_char8_t *ep_config = NULL;
	ep_char8_t *ep_config_output_path = NULL;
	uint32_t ep_circular_mb = 0;
	const ep_char8_t *output_path = NULL;

	if (ep_rt_config_value_get_enable ()) {
		ep_config = ep_rt_config_value_get_config ();
		ep_config_output_path = ep_rt_config_value_get_output_path ();

		ep_char8_t pidStr[24];
		ep_rt_utf8_string_snprintf(pidStr, ARRAY_SIZE (pidStr), "%u", (unsigned)ep_rt_current_process_get_id());

		while (true)
		{
			if (ep_rt_utf8_string_replace(&ep_config_output_path, "{pid}", pidStr))
			{
				// In case there is a second use of {pid} in the output path
				continue;
			}

			// No more instances of {pid} in the OutputPath
			break;
		}

		ep_circular_mb = ep_rt_config_value_get_circular_mb ();
		output_path = NULL;

		output_path = ep_config_output_path ? ep_config_output_path : "trace.nettrace";
		ep_circular_mb = ep_circular_mb > 0 ? ep_circular_mb : 1;

		uint64_t session_id = ep_enable_2 (
			output_path,
			ep_circular_mb,
			ep_config,
			ep_rt_config_value_get_output_streaming () ? EP_SESSION_TYPE_FILESTREAM : EP_SESSION_TYPE_FILE,
			EP_SERIALIZATION_FORMAT_NETTRACE_V4,
			true,
			NULL,
			NULL,
			NULL);

		if (session_id)
			ep_start_streaming (session_id);
	}

	ep_rt_utf8_string_free (ep_config_output_path);
	ep_rt_utf8_string_free (ep_config);
	return;
}

static
bool
session_requested_sampling (EventPipeSession *session)
{
	EP_ASSERT (session != NULL);
	return ep_rt_session_provider_list_find_by_name (ep_session_provider_list_get_providers_cref (ep_session_get_providers (session)), ep_config_get_sample_profiler_provider_name_utf8 ());
}

static
bool
ipc_stream_factory_any_suspended_ports (void)
{
	return _ep_ipc_stream_factory_suspended_ports_callback ? _ep_ipc_stream_factory_suspended_ports_callback () : false;
}

#ifdef EP_CHECKED_BUILD
void
ep_requires_lock_held (void)
{
	ep_rt_config_requires_lock_held ();
}

void
ep_requires_lock_not_held (void)
{
	ep_rt_config_requires_lock_not_held ();
}
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
	void *callback_additional_data)
{
	ep_return_zero_if_nok (format < EP_SERIALIZATION_FORMAT_COUNT);
	ep_return_zero_if_nok (session_type == EP_SESSION_TYPE_SYNCHRONOUS || circular_buffer_size_in_mb > 0);
	ep_return_zero_if_nok (providers_len > 0 && providers != NULL);

	ep_requires_lock_not_held ();

	// If the state or arguments are invalid, bail here.
	if ((session_type == EP_SESSION_TYPE_FILE || session_type == EP_SESSION_TYPE_FILESTREAM) && output_path == NULL)
		return 0;
	if (session_type == EP_SESSION_TYPE_IPCSTREAM && stream == NULL)
		return 0;

	EventPipeSessionID session_id = 0;
	EventPipeProviderCallbackDataQueue callback_data_queue;
	EventPipeProviderCallbackData provider_callback_data;
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue = ep_provider_callback_data_queue_init (&callback_data_queue);

	EP_LOCK_ENTER (section1)
		session_id = enable (
			output_path,
			circular_buffer_size_in_mb,
			providers,
			providers_len,
			session_type,
			format,
			rundown_requested,
			stream,
			provider_callback_data_queue,
			sync_callback,
			callback_additional_data);
	EP_LOCK_EXIT (section1)

	while (ep_provider_callback_data_queue_try_dequeue (provider_callback_data_queue, &provider_callback_data)) {
		ep_rt_prepare_provider_invoke_callback (&provider_callback_data);
		provider_invoke_callback (&provider_callback_data);
		ep_provider_callback_data_fini (&provider_callback_data);
	}

ep_on_exit:
	ep_provider_callback_data_queue_fini (provider_callback_data_queue);
	ep_requires_lock_not_held ();
	return session_id;

ep_on_error:
	session_id = 0;
	ep_exit_error_handler ();
}

EventPipeSessionID
ep_enable_2 (
	const ep_char8_t *output_path,
	uint32_t circular_buffer_size_in_mb,
	const ep_char8_t *providers_config,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	bool rundown_requested,
	IpcStream *stream,
	EventPipeSessionSynchronousCallback sync_callback,
	void *callback_additional_data)
{
	const ep_char8_t *providers_config_to_parse = providers_config;
	int32_t providers_len = 0;
	EventPipeProviderConfiguration *providers = NULL;
	int32_t current_provider = 0;
	uint64_t session_id = 0;

	// If no specific providers config is used, enable EventPipe session
	// with the default provider configurations.
	if (!providers_config_to_parse || *providers_config_to_parse == '\0') {
		providers_len = 3;

		providers = ep_rt_object_array_alloc (EventPipeProviderConfiguration, providers_len);
		ep_raise_error_if_nok (providers != NULL);

		ep_provider_config_init (&providers [0], ep_rt_utf8_string_dup (ep_config_get_public_provider_name_utf8 ()), 0x4c14fccbd, EP_EVENT_LEVEL_VERBOSE, NULL);
		ep_provider_config_init (&providers [1], ep_rt_utf8_string_dup (ep_config_get_private_provider_name_utf8 ()), 0x4002000b, EP_EVENT_LEVEL_VERBOSE, NULL);
		ep_provider_config_init (&providers [2], ep_rt_utf8_string_dup (ep_config_get_sample_profiler_provider_name_utf8 ()), 0x0, EP_EVENT_LEVEL_VERBOSE, NULL);
	} else {
		// Count number of providers to parse.
		while (*providers_config_to_parse != '\0') {
			providers_len += 1;
			while (*providers_config_to_parse != '\0' && *providers_config_to_parse != ',')
				providers_config_to_parse++;

			if (*providers_config_to_parse != '\0')
				providers_config_to_parse++;
		}

		providers_config_to_parse = providers_config;

		providers = ep_rt_object_array_alloc (EventPipeProviderConfiguration, providers_len);
		ep_raise_error_if_nok (providers != NULL);

		while (*providers_config_to_parse != '\0') {
			ep_char8_t *provider_name = NULL;
			uint64_t keyword_mask = 0;
			EventPipeEventLevel level = EP_EVENT_LEVEL_VERBOSE;
			ep_char8_t *args = NULL;

			if (providers_config_to_parse && *providers_config_to_parse != ',') {
				provider_name = get_next_config_value_as_utf8_string (&providers_config_to_parse);
				ep_raise_error_if_nok (provider_name != NULL);
			}

			if (providers_config_to_parse && *providers_config_to_parse != ',')
				keyword_mask = get_next_config_value_as_uint64_t (&providers_config_to_parse);

			if (providers_config_to_parse && *providers_config_to_parse != ',')
				level = (EventPipeEventLevel)get_next_config_value_as_uint32_t (&providers_config_to_parse);

			if (providers_config_to_parse && *providers_config_to_parse != ',')
				args = get_next_config_value_as_utf8_string (&providers_config_to_parse);

			ep_provider_config_init (&providers [current_provider++], provider_name, keyword_mask, level, args);

			if (!providers_config_to_parse)
				break;

			while (*providers_config_to_parse != '\0' && *providers_config_to_parse != ',')
				providers_config_to_parse++;

			if (*providers_config_to_parse != '\0')
				providers_config_to_parse++;
		}
	}

	session_id = ep_enable (
		output_path,
		circular_buffer_size_in_mb,
		providers,
		providers_len,
		session_type,
		format,
		rundown_requested,
		stream,
		sync_callback,
		callback_additional_data);

ep_on_exit:

	if (providers) {
		for (int32_t i = 0; i < providers_len; ++i) {
			ep_provider_config_fini (&providers [i]);
			ep_rt_utf8_string_free ((ep_char8_t *)providers [i].provider_name);
			ep_rt_utf8_string_free ((ep_char8_t *)providers [i].filter_data);
		}
		ep_rt_object_array_free (providers);
	}

	return session_id;

ep_on_error:
	ep_exit_error_handler ();

}

void
ep_disable (EventPipeSessionID id)
{
	ep_requires_lock_not_held ();

	// ep_disable is called synchronously since the diagnostics server is
	// single threaded.  HOWEVER, if the runtime was suspended during startup,
	// then ep_finish_init might not have executed yet. Disabling a session
	// needs to either happen before we resume or after initialization. We briefly take the
	// lock to check _ep_can_start_threads to check whether we've finished initialization. We
	// also check whether we are still suspended in which case we can safely disable the session
	// without deferral.
	EP_LOCK_ENTER (section1)
		if (!_ep_can_start_threads && !ipc_stream_factory_any_suspended_ports ())
		{
			ep_rt_session_id_array_append (&_ep_deferred_disable_session_ids, id);
			ep_raise_error_holding_lock (section1);
		}
	EP_LOCK_EXIT (section1)

	disable_helper (id);

ep_on_exit:
	ep_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

EventPipeSession *
ep_get_session (EventPipeSessionID session_id)
{
	ep_requires_lock_not_held ();

	EP_LOCK_ENTER (section1)

		if (ep_volatile_load_eventpipe_state () == EP_STATE_NOT_INITIALIZED) {
			EP_ASSERT (!"EventPipe::GetSession invoked before EventPipe was initialized.");
			ep_raise_error_holding_lock (section1);
		}

		ep_raise_error_if_nok_holding_lock (is_session_id_in_collection (session_id), section1);

	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_requires_lock_not_held ();
	return (EventPipeSession *)(uintptr_t)session_id;

ep_on_error:
	session_id = 0;
	ep_exit_error_handler ();
}

bool
ep_is_session_enabled (EventPipeSessionID session_id)
{
	ep_return_false_if_nok (session_id != 0);
	return ep_volatile_load_session (ep_session_get_index ((EventPipeSession *)(uintptr_t)session_id)) != NULL;
}

void
ep_start_streaming (EventPipeSessionID session_id)
{
	ep_requires_lock_not_held ();

	EP_LOCK_ENTER (section1)
		ep_raise_error_if_nok_holding_lock (is_session_id_in_collection (session_id), section1);
		if (_ep_can_start_threads)
			ep_session_start_streaming ((EventPipeSession *)(uintptr_t)session_id);
		else
			ep_rt_session_id_array_append (&_ep_deferred_enable_session_ids, session_id);
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

bool
ep_enabled (void)
{
	return (ep_volatile_load_eventpipe_state () >= EP_STATE_INITIALIZED &&
			ep_volatile_load_number_of_sessions () > 0);
}

EventPipeProvider *
ep_create_provider (
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	EventPipeCallbackDataFree callback_data_free_func,
	void *callback_data)
{
	ep_return_null_if_nok (provider_name != NULL);

	ep_requires_lock_not_held ();

	EventPipeProvider *provider = NULL;
	EventPipeProviderCallbackDataQueue data_queue;
	EventPipeProviderCallbackData provider_callback_data;
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue = ep_provider_callback_data_queue_init (&data_queue);

	EP_LOCK_ENTER (section1)
		provider = config_create_provider (ep_config_get (), provider_name, callback_func, callback_data_free_func, callback_data, provider_callback_data_queue);
		ep_raise_error_if_nok_holding_lock (provider != NULL, section1);
	EP_LOCK_EXIT (section1)

	while (ep_provider_callback_data_queue_try_dequeue (provider_callback_data_queue, &provider_callback_data)) {
		ep_rt_prepare_provider_invoke_callback (&provider_callback_data);
		provider_invoke_callback (&provider_callback_data);
		ep_provider_callback_data_fini (&provider_callback_data);
	}

	ep_rt_notify_profiler_provider_created (provider);

ep_on_exit:
	ep_provider_callback_data_queue_fini (provider_callback_data_queue);
	ep_requires_lock_not_held ();
	return provider;

ep_on_error:
	ep_delete_provider (provider);

	provider = NULL;
	ep_exit_error_handler ();
}

void
ep_delete_provider (EventPipeProvider *provider)
{
	ep_return_void_if_nok (provider != NULL);

	ep_requires_lock_not_held ();

	// Take the lock to make sure that we don't have a race
	// between disabling tracing and deleting a provider
	// where we hold a provider after tracing has been disabled.
	EP_LOCK_ENTER (section1)
		if (enabled ()) {
			// Save the provider until the end of the tracing session.
			ep_provider_set_delete_deferred (provider, true);
		} else {
			config_delete_provider (ep_config_get (), provider);
		}
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

EventPipeProvider *
ep_get_provider (const ep_char8_t *provider_name)
{
	ep_return_null_if_nok (provider_name != NULL);

	ep_requires_lock_not_held ();

	EventPipeProvider *provider = NULL;

	EP_LOCK_ENTER (section1)
		provider = config_get_provider (ep_config_get (), provider_name);
		ep_raise_error_if_nok_holding_lock (provider != NULL, section1);
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_requires_lock_not_held ();
	return provider;

ep_on_error:
	provider = NULL;
	ep_exit_error_handler ();
}

bool
ep_add_provider_to_session (
	EventPipeSessionProvider *provider,
	EventPipeSession *session)
{
	ep_return_false_if_nok (provider != NULL && session != NULL);

	ep_requires_lock_not_held ();

	bool result = false;

	EP_LOCK_ENTER (section1)
		ep_raise_error_if_nok_holding_lock (ep_session_add_session_provider (session, provider), section1);
	EP_LOCK_EXIT (section1)

	result = true;

ep_on_exit:
	ep_requires_lock_not_held ();
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ep_exit_error_handler ();
}

void
ep_init (void)
{
	ep_requires_lock_not_held ();

	ep_rt_init ();

	if (ep_volatile_load_eventpipe_state () != EP_STATE_NOT_INITIALIZED) {
		EP_ASSERT (!"EventPipe already initialized.");
		return;
	}

	ep_thread_init ();

	for (uint32_t i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; ++i)
		ep_volatile_store_session (i, NULL);

	ep_config_init (ep_config_get ());

	ep_event_source_init (ep_event_source_get ());

	// This calls into auto-generated code to initialize the runtime specific providers
	// and events so that the EventPipe configuration lock isn't taken at runtime
	ep_rt_init_providers_and_events ();

	// Set the sampling rate for the sample profiler.
	const uint32_t default_profiler_sample_rate_in_nanoseconds = 1000000; // 1 msec.
	ep_sample_profiler_set_sampling_rate (default_profiler_sample_rate_in_nanoseconds);

	ep_rt_session_id_array_alloc (&_ep_deferred_enable_session_ids);
	ep_rt_session_id_array_alloc (&_ep_deferred_disable_session_ids);

	ep_rt_execution_checkpoint_array_alloc (&_ep_rundown_execution_checkpoints);

	EP_LOCK_ENTER (section1)
		ep_volatile_store_eventpipe_state (EP_STATE_INITIALIZED);
	EP_LOCK_EXIT (section1)

	enable_default_session_via_env_variables ();

ep_on_exit:
	ep_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_finish_init (void)
{
	ep_requires_lock_not_held ();

	ep_rt_init_finish ();

	// Enable streaming for any deferred sessions
	EP_LOCK_ENTER (section1)
		_ep_can_start_threads = true;
		if (ep_volatile_load_eventpipe_state () == EP_STATE_INITIALIZED) {
			ep_rt_session_id_array_iterator_t deferred_session_ids_iterator = ep_rt_session_id_array_iterator_begin (&_ep_deferred_enable_session_ids);
			while (!ep_rt_session_id_array_iterator_end (&_ep_deferred_enable_session_ids, &deferred_session_ids_iterator)) {
				EventPipeSessionID session_id = ep_rt_session_id_array_iterator_value (&deferred_session_ids_iterator);
				if (is_session_id_in_collection (session_id))
					ep_session_start_streaming ((EventPipeSession *)(uintptr_t)session_id);
				ep_rt_session_id_array_iterator_next (&deferred_session_ids_iterator);
			}
			ep_rt_session_id_array_clear (&_ep_deferred_enable_session_ids);
		}

		ep_sample_profiler_can_start_sampling ();
	EP_LOCK_EXIT (section1)

	// release lock in case someone tried to disable while we held it
	// _ep_deferred_disable_session_ids is now safe to access without the
	// lock since we've set _ep_can_start_threads to true inside the lock. Anyone
	// who was waiting on that lock will see that state and not mutate the defer list
	if (ep_volatile_load_eventpipe_state () == EP_STATE_INITIALIZED) {
		ep_rt_session_id_array_iterator_t deferred_disable_session_ids_iterator = ep_rt_session_id_array_iterator_begin (&_ep_deferred_disable_session_ids);
		while (!ep_rt_session_id_array_iterator_end (&_ep_deferred_disable_session_ids, &deferred_disable_session_ids_iterator)) {
			EventPipeSessionID session_id = ep_rt_session_id_array_iterator_value (&deferred_disable_session_ids_iterator);
			disable_helper (session_id);
			ep_rt_session_id_array_iterator_next (&deferred_disable_session_ids_iterator);
		}
		ep_rt_session_id_array_clear (&_ep_deferred_disable_session_ids);
	}

ep_on_exit:
	ep_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_shutdown (void)
{
	ep_requires_lock_not_held ();

	ep_return_void_if_nok (ep_volatile_load_eventpipe_state () != EP_STATE_SHUTTING_DOWN);
	ep_return_void_if_nok (!ep_rt_process_detach ());
	ep_return_void_if_nok (ep_volatile_load_eventpipe_state () == EP_STATE_INITIALIZED);

	EP_LOCK_ENTER (section1)
		ep_volatile_store_eventpipe_state (EP_STATE_SHUTTING_DOWN);
	EP_LOCK_EXIT (section1)

	for (uint32_t i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; ++i) {
		EventPipeSession *session = ep_volatile_load_session (i);
		if (session)
			ep_disable ((EventPipeSessionID)session);
	}

	ep_rt_execution_checkpoint_array_iterator_t checkpoint_iterator;
	EventPipeExecutionCheckpoint *checkpoint;
	checkpoint_iterator = ep_rt_execution_checkpoint_array_iterator_begin (&_ep_rundown_execution_checkpoints);
	while (!ep_rt_execution_checkpoint_array_iterator_end (&_ep_rundown_execution_checkpoints, &checkpoint_iterator)) {
		checkpoint = ep_rt_execution_checkpoint_array_iterator_value (&checkpoint_iterator);
		if (checkpoint)
			ep_rt_utf8_string_free (checkpoint->name);
		ep_rt_execution_checkpoint_array_iterator_next (&checkpoint_iterator);
	}
	ep_rt_execution_checkpoint_array_free (&_ep_rundown_execution_checkpoints);

	ep_rt_session_id_array_free (&_ep_deferred_enable_session_ids);
	ep_rt_session_id_array_free (&_ep_deferred_disable_session_ids);

	ep_thread_fini ();

	// dotnet/coreclr: issue 24850: EventPipe shutdown race conditions
	// Deallocating providers/events here might cause AV if a WriteEvent
	// was to occur. Thus, we are not doing this cleanup.

	/*EP_LOCK_ENTER (section1)
		ep_sample_profiler_shutdown ();
	EP_LOCK_EXIT (section1)*/

	// // Remove EventPipeEventSource first since it tries to use the data structures that we remove below.
	// // We need to do this after disabling sessions since those try to write to EventPipeEventSource.
	// ep_event_source_fini (ep_event_source_get ());
	// ep_config_shutdown (ep_config_get ());

ep_on_exit:
	ep_requires_lock_not_held ();
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
	ep_return_null_if_nok (event_instance != NULL);
	return ep_config_build_event_metadata_event (ep_config_get (), event_instance, metadata_id);
}

void
ep_write_event (
	EventPipeEvent *ep_event,
	uint8_t *data,
	uint32_t data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	ep_return_void_if_nok (ep_event != NULL);

	EventPipeEventPayload payload;
	EventPipeEventPayload *event_payload = ep_event_payload_init (&payload, data, data_len);

	write_event (ep_event, event_payload, activity_id, related_activity_id);

	ep_event_payload_fini (event_payload);
}

void
ep_write_event_2 (
	EventPipeEvent *ep_event,
	EventData *event_data,
	uint32_t event_data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	ep_return_void_if_nok (ep_event != NULL);

	EventPipeEventPayload payload;
	EventPipeEventPayload *event_payload = ep_event_payload_init_2 (&payload, event_data, event_data_len);

	write_event (ep_event, event_payload, activity_id, related_activity_id);

	ep_event_payload_fini (event_payload);
}

void
ep_write_sample_profile_event (
	ep_rt_thread_handle_t sampling_thread,
	EventPipeEvent *ep_event,
	ep_rt_thread_handle_t target_thread,
	EventPipeStackContents *stack,
	uint8_t *event_data,
	uint32_t event_data_len)
{
	ep_return_void_if_nok (ep_event != NULL);

	EventPipeEventPayload payload;
	EventPipeEventPayload *event_payload = ep_event_payload_init (&payload, event_data, event_data_len);

	write_event_2 (
		sampling_thread,
		ep_event,
		event_payload,
		NULL,
		NULL,
		target_thread,
		stack);

	ep_event_payload_fini (event_payload);
}

EventPipeEventInstance *
ep_get_next_event (EventPipeSessionID session_id)
{
	ep_requires_lock_not_held ();

	// Only fetch the next event if a tracing session exists.
	// The buffer manager is not disposed until the process is shutdown.
	EventPipeSession *const session = ep_get_session (session_id);
	return session ? ep_session_get_next_event (session) : NULL;
}

EventPipeWaitHandle
ep_get_wait_handle (EventPipeSessionID session_id)
{
	EventPipeSession *const session = ep_get_session (session_id);
	return session ? ep_rt_wait_event_get_wait_handle (ep_session_get_wait_event (session)) : 0;
}

bool
ep_add_rundown_execution_checkpoint (
	const ep_char8_t *name,
	ep_timestamp_t timestamp)
{
	ep_requires_lock_not_held ();

	bool result = false;

	EventPipeExecutionCheckpoint *exec_checkpoint = ep_execution_checkpoint_alloc (name, timestamp);
	ep_raise_error_if_nok (exec_checkpoint != NULL);

	EP_LOCK_ENTER (section1)
		ep_raise_error_if_nok_holding_lock (ep_rt_execution_checkpoint_array_append (&_ep_rundown_execution_checkpoints, exec_checkpoint), section1);
		exec_checkpoint = NULL;
	EP_LOCK_EXIT (section1)

	result = true;

ep_on_exit:
	ep_requires_lock_not_held ();
	return result;

ep_on_error:
	ep_execution_checkpoint_free (exec_checkpoint);
	EP_ASSERT (result == false);
	ep_exit_error_handler ();
}

/*
 * EventPipeProviderCallbackDataQueue.
 */

bool
ep_provider_callback_data_queue_enqueue (
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue,
	EventPipeProviderCallbackData *provider_callback_data)
{
	EP_ASSERT (provider_callback_data_queue != NULL);
	EventPipeProviderCallbackData *provider_callback_data_move = ep_provider_callback_data_alloc_move (provider_callback_data);
	ep_raise_error_if_nok (provider_callback_data_move != NULL);
	ep_raise_error_if_nok (ep_rt_provider_callback_data_queue_push_tail (ep_provider_callback_data_queue_get_queue_ref (provider_callback_data_queue), provider_callback_data_move));

	return true;

ep_on_error:
	ep_provider_callback_data_free (provider_callback_data_move);
	return false;
}

bool
ep_provider_callback_data_queue_try_dequeue (
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue,
	EventPipeProviderCallbackData *provider_callback_data)
{
	EP_ASSERT (provider_callback_data_queue != NULL);

	ep_return_false_if_nok (!ep_rt_provider_callback_data_queue_is_empty (ep_provider_callback_data_queue_get_queue_ref (provider_callback_data_queue)));

	EventPipeProviderCallbackData *value = NULL;
	ep_raise_error_if_nok (ep_rt_provider_callback_data_queue_pop_head (ep_provider_callback_data_queue_get_queue_ref (provider_callback_data_queue), &value));
	ep_provider_callback_data_init_move (provider_callback_data, value);
	ep_provider_callback_data_free (value);

	return true;

ep_on_error:
	return false;
}

/*
 * EventPipeSystemTime.
 */

void
ep_system_time_set (
	EventPipeSystemTime *system_time,
	uint16_t year,
	uint16_t month,
	uint16_t day_of_week,
	uint16_t day,
	uint16_t hour,
	uint16_t minute,
	uint16_t second,
	uint16_t milliseconds)
{
	EP_ASSERT (system_time != NULL);
	system_time->year = year;
	system_time->month = month;
	system_time->day_of_week = day_of_week;
	system_time->day = day;
	system_time->hour = hour;
	system_time->minute = minute;
	system_time->second = second;
	system_time->milliseconds = milliseconds;
}

void
ep_ipc_stream_factory_callback_set (EventPipeIpcStreamFactorySuspendedPortsCallback suspended_ports_callback)
{
	_ep_ipc_stream_factory_suspended_ports_callback = suspended_ports_callback;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe;
const char quiet_linker_empty_file_warning_eventpipe = 0;
#endif
