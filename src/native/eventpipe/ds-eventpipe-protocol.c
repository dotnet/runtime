#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_EVENTPIPE_PROTOCOL_GETTER_SETTER
#include "ds-protocol.h"
#include "ds-eventpipe-protocol.h"
#include "ep.h"
#include "ds-rt.h"

/*
 * Forward declares of all static functions.
 */

static
bool
eventpipe_protocol_helper_send_stop_tracing_success (
	DiagnosticsIpcStream *stream,
	EventPipeSessionID session_id);

static
bool
eventpipe_protocol_helper_send_start_tracing_success (
	DiagnosticsIpcStream *stream,
	EventPipeSessionID session_id);

static
inline
bool
eventpipe_collect_tracing_command_try_parse_session_type (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeSessionType *session_type);

static
bool
eventpipe_collect_tracing_command_try_parse_serialization_format (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeSerializationFormat *format);

static
bool
eventpipe_collect_tracing_command_try_parse_circular_buffer_size (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint32_t *circular_buffer);

static
bool
eventpipe_collect_tracing_command_try_parse_rundown_requested (
	uint8_t **buffer,
	uint32_t *buffer_len,
	bool *rundown_requested);

static
bool
eventpipe_collect_tracing_command_try_parse_rundown_keyword (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint64_t *rundown_keyword);

static
bool
eventpipe_collect_tracing_command_try_parse_stackwalk_requested (
	uint8_t **buffer,
	uint32_t *buffer_len,
	bool *stackwalk_requested);

static
bool
eventpipe_collect_tracing_command_try_parse_logging_level (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeEventLevel *logging_level);

static
bool
eventpipe_collect_tracing_command_try_parse_event_ids (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint32_t event_ids_len,
	uint32_t **event_ids);

static
bool
eventpipe_collect_tracing_command_try_parse_event_filter (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderEventFilter **event_filter);

static
bool
eventpipe_collect_tracing_command_try_parse_tracepoint_sets (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint32_t tracepoint_sets_len,
	EventPipeProviderTracepointSet **tracepoint_sets);

static
bool
eventpipe_collect_tracing_command_try_parse_tracepoint_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderTracepointConfiguration **tracepoint_config);

static
bool
eventpipe_collect_tracing_command_try_parse_provider_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderOptionalFieldFlags optional_field_flags,
	EventPipeProviderConfiguration *provider_config);

static
bool
eventpipe_collect_tracing_command_try_parse_provider_configs (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderOptionalFieldFlags optional_field_flags,
	dn_vector_t **result);

static
void
DN_CALLBACK_CALLTYPE
eventpipe_provider_configs_free_func (void *data);

static
uint8_t *
eventpipe_collect_tracing_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
uint8_t *
eventpipe_collect_tracing2_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
uint8_t *
eventpipe_collect_tracing3_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
uint8_t *
eventpipe_collect_tracing4_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
uint8_t *
eventpipe_collect_tracing5_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
bool
eventpipe_protocol_helper_stop_tracing (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
eventpipe_protocol_helper_collect_tracing (
	EventPipeCollectTracingCommandPayload *payload,
	DiagnosticsIpcStream *stream);

static
bool
eventpipe_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

/*
* EventPipeCollectTracingCommandPayload
*/

static
inline
bool
eventpipe_collect_tracing_command_try_parse_session_type (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeSessionType *type)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (type != NULL);

	uint32_t session_type;
	bool can_parse = ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &session_type);

	bool ipc_valid_session_type = false;
	switch (session_type) {
	case 0:
		*type = EP_SESSION_TYPE_IPCSTREAM;
		ipc_valid_session_type = true;
		break;
	case 1:
		*type = EP_SESSION_TYPE_USEREVENTS;
		ipc_valid_session_type = true;
		break;
	default:
		break;
	}

	return can_parse && ipc_valid_session_type;
}

static
inline
bool
eventpipe_collect_tracing_command_try_parse_serialization_format (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeSerializationFormat *format)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (format != NULL);

	uint32_t serialization_format;
	bool can_parse = ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &serialization_format);

	*format = (EventPipeSerializationFormat)serialization_format;
	return can_parse && (0 <= (int32_t)serialization_format) && ((int32_t)serialization_format < (int32_t)EP_SERIALIZATION_FORMAT_COUNT);
}

static
inline
bool
eventpipe_collect_tracing_command_try_parse_circular_buffer_size (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint32_t *circular_buffer)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (circular_buffer != NULL);

	bool can_parse = ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, circular_buffer);
	return can_parse && (*circular_buffer > 0);
}

static
inline
bool
eventpipe_collect_tracing_command_try_parse_rundown_requested (
	uint8_t **buffer,
	uint32_t *buffer_len,
	bool *rundown_requested)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (rundown_requested != NULL);

	return ds_ipc_message_try_parse_bool (buffer, buffer_len, rundown_requested);
}

static
inline
bool
eventpipe_collect_tracing_command_try_parse_rundown_keyword (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint64_t *rundown_keyword)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (rundown_keyword != NULL);

	return ds_ipc_message_try_parse_uint64_t (buffer, buffer_len, rundown_keyword);
}

static
inline
bool
eventpipe_collect_tracing_command_try_parse_stackwalk_requested (
	uint8_t **buffer,
	uint32_t *buffer_len,
	bool *stackwalk_requested)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (stackwalk_requested != NULL);

	return ds_ipc_message_try_parse_bool (buffer, buffer_len, stackwalk_requested);
}

static
bool
eventpipe_collect_tracing_command_try_parse_logging_level (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeEventLevel *logging_level)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (logging_level != NULL);

	uint32_t log_level = 0;
	bool can_parse = ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &log_level);

	*logging_level = (EventPipeEventLevel)log_level;
	return can_parse && (0 <= (int32_t)log_level) && ((int32_t)log_level <= (int32_t)EP_EVENT_LEVEL_VERBOSE);
}

/*
 *  eventpipe_collect_tracing_command_try_parse_event_ids
 *
 *  Parses an array of event IDs from the IPC buffer. Allocates memory for the array
 *  and transfers ownership to the caller.
 */
static
bool
eventpipe_collect_tracing_command_try_parse_event_ids (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint32_t event_ids_len,
	uint32_t **event_ids)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (event_ids != NULL);

	bool result = false;
	*event_ids = NULL;

	if (event_ids_len == 0)
		return true;

	*event_ids = ep_rt_object_array_alloc (uint32_t, event_ids_len);
	ep_raise_error_if_nok (*event_ids != NULL);
	for (uint32_t i = 0; i < event_ids_len; ++i)
		ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &(*event_ids)[i]));

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	ep_rt_object_array_free (*event_ids);
	*event_ids = NULL;
	ep_exit_error_handler ();
}

/*
 *  eventpipe_collect_tracing_command_try_parse_event_filter
 *
 *  Parses an EventPipeProviderEventFilter from the IPC Stream. Allocates memory for the EventPipeProviderEventFilter
 *  and transfers ownership to the caller.
 */
static
bool
eventpipe_collect_tracing_command_try_parse_event_filter (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderEventFilter **event_filter)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (event_filter != NULL);

	bool result = false;

	*event_filter = ep_rt_object_alloc (EventPipeProviderEventFilter);
	ep_raise_error_if_nok (*event_filter != NULL);

	ep_raise_error_if_nok (ds_ipc_message_try_parse_bool (buffer, buffer_len, &(*event_filter)->enable));

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &(*event_filter)->length));

	ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_event_ids (buffer, buffer_len, (*event_filter)->length, &(*event_filter)->event_ids));

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	eventpipe_collect_tracing_command_free_event_filter (*event_filter);
	*event_filter = NULL;
	ep_exit_error_handler ();
}

/*
 *  eventpipe_collect_tracing_command_try_parse_tracepoint_sets
 *
 *  Parses an array of EventPipeProviderTracepointSets from the IPC buffer.
 *  Allocates memory for the array and its contents, passing ownership to the caller.
 */
static
bool
eventpipe_collect_tracing_command_try_parse_tracepoint_sets (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint32_t tracepoint_sets_len,
	EventPipeProviderTracepointSet **tracepoint_sets)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (tracepoint_sets != NULL);

	bool result = false;
	*tracepoint_sets = NULL;

	if (tracepoint_sets_len == 0)
		return false;

	*tracepoint_sets = ep_rt_object_array_alloc (EventPipeProviderTracepointSet, tracepoint_sets_len);
	ep_raise_error_if_nok (*tracepoint_sets != NULL);

	for (uint32_t i = 0; i < tracepoint_sets_len; ++i) {
		ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_string_utf8_t_alloc (buffer, buffer_len, &(*tracepoint_sets)[i].tracepoint_name));
		ep_raise_error_if_nok (!ep_rt_utf8_string_is_null_or_empty ((*tracepoint_sets)[i].tracepoint_name));

		ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &(*tracepoint_sets)[i].event_ids_length));

		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_event_ids (buffer, buffer_len, (*tracepoint_sets)[i].event_ids_length, &(*tracepoint_sets)[i].event_ids));
	}

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	eventpipe_collect_tracing_command_free_tracepoint_sets (*tracepoint_sets, tracepoint_sets_len);
	*tracepoint_sets = NULL;
	ep_exit_error_handler ();
}

/*
 *  eventpipe_collect_tracing_command_try_parse_tracepoint_config
 *
 *  Parses an EventPipeProviderTracepointConfiguration from the IPC Stream.
 *  Allocates memory for the EventPipeProviderTracepointConfiguration and its fields,
 *  passing ownership to the caller.
 */
static
bool
eventpipe_collect_tracing_command_try_parse_tracepoint_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderTracepointConfiguration **tracepoint_config)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (tracepoint_config != NULL);

	bool result = false;

	*tracepoint_config = ep_rt_object_alloc (EventPipeProviderTracepointConfiguration);
	ep_raise_error_if_nok (*tracepoint_config != NULL);

	ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_string_utf8_t_alloc (buffer, buffer_len, &(*tracepoint_config)->default_tracepoint_name));

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &(*tracepoint_config)->non_default_tracepoints_length));

	ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_tracepoint_sets (buffer, buffer_len, (*tracepoint_config)->non_default_tracepoints_length, &(*tracepoint_config)->non_default_tracepoints));

	ep_raise_error_if_nok ((*tracepoint_config)->default_tracepoint_name != NULL || (*tracepoint_config)->non_default_tracepoints_length > 0);

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	eventpipe_collect_tracing_command_free_tracepoint_config (*tracepoint_config);
	*tracepoint_config = NULL;
	ep_exit_error_handler ();
}

/*
 *  eventpipe_collect_tracing_command_try_parse_provider_config
 *
 *  Deserializes a single EventPipeProviderConfiguration from the IPC Stream
 *
 *  Dynamically allocates memory for the EventPipeProviderConfiguration fields and passes ownership to the caller.
 */
static
bool
eventpipe_collect_tracing_command_try_parse_provider_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderOptionalFieldFlags optional_field_flags,
	EventPipeProviderConfiguration *provider_config)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (provider_config != NULL);

	bool result = false;

	provider_config->keywords = 0;
	provider_config->logging_level = (EventPipeEventLevel)0;
	provider_config->provider_name = NULL;
	provider_config->filter_data = NULL;
	provider_config->event_filter = NULL;
	provider_config->tracepoint_config = NULL;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint64_t (buffer, buffer_len, &provider_config->keywords));

	ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_logging_level (buffer, buffer_len, &provider_config->logging_level));

	ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_string_utf8_t_alloc (buffer, buffer_len, &provider_config->provider_name));
	ep_raise_error_if_nok (!ep_rt_utf8_string_is_null_or_empty (provider_config->provider_name));

	ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_string_utf8_t_alloc (buffer, buffer_len, &provider_config->filter_data));

	if ((optional_field_flags & EP_PROVIDER_OPTFIELD_EVENT_FILTER) != 0)
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_event_filter (buffer, buffer_len, &provider_config->event_filter));

	if ((optional_field_flags & EP_PROVIDER_OPTFIELD_TRACEPOINT_CONFIG) != 0)
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_tracepoint_config (buffer, buffer_len, &provider_config->tracepoint_config));

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	ep_provider_config_fini (provider_config);
	ep_exit_error_handler ();
}

/*
 * eventpipe_collect_tracing_command_try_parse_provider_configs
 *
 * With the introduction of CollectTracing5, there is more flexiblity in provider configuration encoding.
 * This function deserializes all provider configurations from the IPC Stream, providing callers the flexibility
 * to specify which optional fields are present for the particular CollectTracingN command.
 *
 * Ownership of all EventPipeProviderConfigurations data is transferred to the caller.
 */
static
bool
eventpipe_collect_tracing_command_try_parse_provider_configs (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderOptionalFieldFlags optional_field_flags,
	dn_vector_t **result)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (result != NULL);

	// Picking an arbitrary upper bound,
	// This should be larger than any reasonable client request.
	const uint32_t max_count_configs = 1000;
	uint32_t count_configs = 0;
	dn_vector_custom_alloc_params_t  params = {0, };

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &count_configs));
	ep_raise_error_if_nok (count_configs <= max_count_configs);
	params.capacity = count_configs;
	*result = dn_vector_custom_alloc_t (&params, EventPipeProviderConfiguration);
	ep_raise_error_if_nok (*result);

	for (uint32_t i = 0; i < count_configs; ++i) {
		EventPipeProviderConfiguration provider_config;
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_provider_config (
			buffer,
			buffer_len,
			optional_field_flags,
			&provider_config));

		if (!dn_vector_push_back (*result, provider_config))
			ep_provider_config_fini (&provider_config);
	}

ep_on_exit:
	return (count_configs > 0);

ep_on_error:
	count_configs = 0;
	dn_vector_custom_free (*result, eventpipe_provider_configs_free_func);
	*result = NULL;
	ep_exit_error_handler ();
}

EventPipeCollectTracingCommandPayload *
ds_eventpipe_collect_tracing_command_payload_alloc (void)
{
	return ep_rt_object_alloc (EventPipeCollectTracingCommandPayload);
}

static
void
DN_CALLBACK_CALLTYPE
eventpipe_provider_configs_free_func (void *data)
{
	ep_provider_config_fini ((EventPipeProviderConfiguration *)data);
}

void
ds_eventpipe_collect_tracing_command_payload_free (EventPipeCollectTracingCommandPayload *payload)
{
	ep_return_void_if_nok (payload != NULL);
	ep_rt_byte_array_free (payload->incoming_buffer);

	dn_vector_custom_free (payload->provider_configs, eventpipe_provider_configs_free_func);

	ep_rt_object_free (payload);
}

/*
* EventPipeCollectTracingCommandPayload
*/

static
uint8_t *
eventpipe_collect_tracing_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	EventPipeCollectTracingCommandPayload * instance = ds_eventpipe_collect_tracing_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb ) ||
		!eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format) ||
		!eventpipe_collect_tracing_command_try_parse_provider_configs (&buffer_cursor, &buffer_cursor_len, EP_PROVIDER_OPTFIELD_NONE, &instance->provider_configs))
		ep_raise_error ();
	instance->rundown_requested = true;
	instance->stackwalk_requested = true;
	instance->rundown_keyword = ep_default_rundown_keyword;
	instance->session_type = EP_SESSION_TYPE_IPCSTREAM;

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

static
uint8_t *
eventpipe_collect_tracing2_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	EventPipeCollectTracingCommandPayload *instance = ds_eventpipe_collect_tracing_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb ) ||
		!eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format) ||
		!eventpipe_collect_tracing_command_try_parse_rundown_requested (&buffer_cursor, &buffer_cursor_len, &instance->rundown_requested) ||
		!eventpipe_collect_tracing_command_try_parse_provider_configs (&buffer_cursor, &buffer_cursor_len, EP_PROVIDER_OPTFIELD_NONE, &instance->provider_configs))
		ep_raise_error ();

	instance->rundown_keyword = instance->rundown_requested ? ep_default_rundown_keyword : 0;

	instance->stackwalk_requested = true;
	instance->session_type = EP_SESSION_TYPE_IPCSTREAM;

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

static
uint8_t *
eventpipe_collect_tracing3_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	EventPipeCollectTracingCommandPayload *instance = ds_eventpipe_collect_tracing_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb ) ||
		!eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format) ||
		!eventpipe_collect_tracing_command_try_parse_rundown_requested (&buffer_cursor, &buffer_cursor_len, &instance->rundown_requested) ||
		!eventpipe_collect_tracing_command_try_parse_stackwalk_requested (&buffer_cursor, &buffer_cursor_len, &instance->stackwalk_requested) ||
		!eventpipe_collect_tracing_command_try_parse_provider_configs (&buffer_cursor, &buffer_cursor_len, EP_PROVIDER_OPTFIELD_NONE, &instance->provider_configs))
		ep_raise_error ();

	instance->rundown_keyword = instance->rundown_requested ? ep_default_rundown_keyword : 0;
	instance->session_type = EP_SESSION_TYPE_IPCSTREAM;

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

static
uint8_t *
eventpipe_collect_tracing4_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	EventPipeCollectTracingCommandPayload *instance = ds_eventpipe_collect_tracing_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb ) ||
		!eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format) ||
		!eventpipe_collect_tracing_command_try_parse_rundown_keyword (&buffer_cursor, &buffer_cursor_len, &instance->rundown_keyword) ||
		!eventpipe_collect_tracing_command_try_parse_stackwalk_requested (&buffer_cursor, &buffer_cursor_len, &instance->stackwalk_requested) ||
		!eventpipe_collect_tracing_command_try_parse_provider_configs (&buffer_cursor, &buffer_cursor_len, EP_PROVIDER_OPTFIELD_NONE, &instance->provider_configs))
		ep_raise_error ();

	instance->rundown_requested = instance->rundown_keyword != 0;
	instance->session_type = EP_SESSION_TYPE_IPCSTREAM;

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

/*
 *  eventpipe_collect_tracing5_command_try_parse_payload
 *
 *  Implements the CollectTracing5 IPC Protocol deserialization.
 *
 *  Ownership of the EventPipeCollectTracingCommandPayload is transferred to the caller.
 */
static
uint8_t *
eventpipe_collect_tracing5_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	EventPipeProviderOptionalFieldFlags optional_field_flags = EP_PROVIDER_OPTFIELD_EVENT_FILTER;

	EventPipeCollectTracingCommandPayload *instance = ds_eventpipe_collect_tracing_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_session_type (&buffer_cursor, &buffer_cursor_len, &instance->session_type));

	if (instance->session_type == EP_SESSION_TYPE_IPCSTREAM) {
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb));
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format));
	} else if (instance->session_type == EP_SESSION_TYPE_USEREVENTS) {
		instance->circular_buffer_size_in_mb = 0;
		instance->serialization_format = EP_SERIALIZATION_FORMAT_NETTRACE_V4; // Serialization format isn't used for user_events sessions, default for check_options_valid.
	}

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint64_t (&buffer_cursor, &buffer_cursor_len, &instance->rundown_keyword));

	if (instance->session_type == EP_SESSION_TYPE_IPCSTREAM) {
		ep_raise_error_if_nok (ds_ipc_message_try_parse_bool (&buffer_cursor, &buffer_cursor_len, &instance->stackwalk_requested));
	} else if (instance->session_type == EP_SESSION_TYPE_USEREVENTS) {
		instance->stackwalk_requested = false;
	}

	if (instance->session_type == EP_SESSION_TYPE_USEREVENTS)
		optional_field_flags = (EventPipeProviderOptionalFieldFlags)(optional_field_flags | EP_PROVIDER_OPTFIELD_TRACEPOINT_CONFIG);

	ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_provider_configs (&buffer_cursor, &buffer_cursor_len, optional_field_flags, &instance->provider_configs));

	instance->rundown_requested = instance->rundown_keyword != 0;

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

/*
* EventPipeProtocolHelper
*/

static
bool
eventpipe_protocol_helper_send_stop_tracing_success (
	DiagnosticsIpcStream *stream,
	EventPipeSessionID session_id)
{
	EP_ASSERT (stream != NULL);

	bool result = false;
	DiagnosticsIpcMessage success_message;
	if (ds_ipc_message_init (&success_message)) {
		result = ds_ipc_message_initialize_header_uint64_t_payload (&success_message, ds_ipc_header_get_generic_success (), (uint64_t)session_id);
		if (result)
			result = ds_ipc_message_send (&success_message, stream);
		ds_ipc_message_fini (&success_message);
	}
	return result;
}

static
bool
eventpipe_protocol_helper_send_start_tracing_success (
	DiagnosticsIpcStream *stream,
	EventPipeSessionID session_id)
{
	EP_ASSERT (stream != NULL);

	bool result = false;
	DiagnosticsIpcMessage success_message;
	if (ds_ipc_message_init (&success_message)) {
		result = ds_ipc_message_initialize_header_uint64_t_payload (&success_message, ds_ipc_header_get_generic_success (), (uint64_t)session_id);
		if (result)
			result = ds_ipc_message_send (&success_message, stream);
		ds_ipc_message_fini (&success_message);
	}
	return result;
}

static
bool
eventpipe_protocol_helper_stop_tracing (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	ep_return_false_if_nok (message != NULL && stream != NULL);

	bool result = false;
	EventPipeStopTracingCommandPayload *payload;
	payload = (EventPipeStopTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, NULL);

	if (!payload) {
		ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
		ep_raise_error ();
	}

	ep_disable (payload->session_id);

	eventpipe_protocol_helper_send_stop_tracing_success (stream, payload->session_id);
	ds_ipc_stream_flush (stream);

	result = true;

ep_on_exit:
	ds_eventpipe_stop_tracing_command_payload_free (payload);
	ds_ipc_stream_free (stream);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ep_exit_error_handler ();
}

static
bool
eventpipe_protocol_helper_collect_tracing (
	EventPipeCollectTracingCommandPayload *payload,
	DiagnosticsIpcStream *stream)
{
	ep_return_false_if_nok (stream != NULL);

	if (!payload) {
		ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
		return false;
	}

	int user_events_data_fd = -1;
	if (payload->session_type == EP_SESSION_TYPE_USEREVENTS) {
		if (!ds_ipc_stream_read_fd (stream, &user_events_data_fd)) {
			ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
			return false;
		}
	}

	EventPipeSessionOptions options;
	ep_session_options_init(
		&options,
		NULL,
		payload->circular_buffer_size_in_mb,
		dn_vector_data_t (payload->provider_configs, EventPipeProviderConfiguration),
		dn_vector_size (payload->provider_configs),
		payload->session_type,
		payload->serialization_format,
		payload->rundown_keyword,
		payload->stackwalk_requested,
		payload->session_type == EP_SESSION_TYPE_IPCSTREAM ? ds_ipc_stream_get_stream_ref (stream) : NULL,
		NULL,
		NULL,
		user_events_data_fd);

	EventPipeSessionID session_id = 0;
	bool result = false;
	session_id = ep_enable_3 (&options);

	if (session_id == 0) {
		ds_ipc_message_send_error (stream, DS_IPC_E_FAIL);
		ep_raise_error ();
	} else {
		eventpipe_protocol_helper_send_start_tracing_success (stream, session_id);
		ep_start_streaming (session_id);
	}

	result = true;

ep_on_exit:
	ep_session_options_fini(&options);
	ds_eventpipe_collect_tracing_command_payload_free (payload);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ds_ipc_stream_free (stream);
	ep_exit_error_handler ();
}

static
bool
eventpipe_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	DS_LOG_WARNING_1 ("Received unknown request type (%d)", ds_ipc_header_get_commandset (ds_ipc_message_get_header_cref (message)));
	ds_ipc_message_send_error (stream, DS_IPC_E_UNKNOWN_COMMAND);
	ds_ipc_stream_free (stream);
	return true;
}

void
ds_eventpipe_stop_tracing_command_payload_free (EventPipeStopTracingCommandPayload *payload)
{
	ep_return_void_if_nok (payload != NULL);
	ep_rt_byte_array_free ((uint8_t *)payload);
}

bool
ds_eventpipe_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	ep_return_false_if_nok (message != NULL && stream != NULL);

	bool result = false;
	EventPipeCollectTracingCommandPayload* payload = NULL;

	switch ((EventPipeCommandId)ds_ipc_header_get_commandid (ds_ipc_message_get_header_cref (message))) {
	case EP_COMMANDID_COLLECT_TRACING:
		payload = (EventPipeCollectTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing_command_try_parse_payload);
		result = eventpipe_protocol_helper_collect_tracing (payload, stream);
		break;
	case EP_COMMANDID_COLLECT_TRACING_2:
		payload = (EventPipeCollectTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing2_command_try_parse_payload);
		result = eventpipe_protocol_helper_collect_tracing (payload, stream);
		break;
	case EP_COMMANDID_COLLECT_TRACING_3:
		payload = (EventPipeCollectTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing3_command_try_parse_payload);
		result = eventpipe_protocol_helper_collect_tracing (payload, stream);
		break;
	case EP_COMMANDID_COLLECT_TRACING_4:
		payload = (EventPipeCollectTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing4_command_try_parse_payload);
		result = eventpipe_protocol_helper_collect_tracing (payload, stream);
		break;
	case EP_COMMANDID_COLLECT_TRACING_5:
		payload = (EventPipeCollectTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing5_command_try_parse_payload);
		result = eventpipe_protocol_helper_collect_tracing (payload, stream);
		break;
	case EP_COMMANDID_STOP_TRACING:
		result = eventpipe_protocol_helper_stop_tracing (message, stream);
		break;
	default:
		result = eventpipe_protocol_helper_unknown_command (message, stream);
		break;
	}

	return result;
}

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(DS_INCLUDE_SOURCE_FILES) && !defined(DS_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_diagnostics_eventpipe_protocol;
const char quiet_linker_empty_file_warning_diagnostics_eventpipe_protocol = 0;
#endif
