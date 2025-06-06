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
eventpipe_collect_tracing_command_try_parse_event_filter (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderEventFilter *event_filter);

static
bool
eventpipe_collect_tracing_command_try_parse_tracepoint_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderTracepointConfiguration *tracepoint_config);

static
bool
eventpipe_collect_tracing_command_try_parse_provider_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderOptionalFieldFlags optional_field_flags,
	EventPipeProviderConfiguration *provider_config);

static
bool
eventpipe_collect_tracing_command_try_parse_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderOptionalFieldFlags optional_field_flags,
	dn_vector_ptr_t **result);

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

/*
 *  eventpipe_collect_tracing_command_try_parse_event_filter
 *
 *  Introduced in CollectTracing5, the event filter provides EventPipe Sessions
 *  additional control over which events are enabled/disabled for a particular provider.
 *
 *  event_filter format:
 *  - bool enable: 0 to disable events, 1 to enable events
 *  - array<uint> event_ids: If specified, a list of Event IDs to disable or enable
 *
 *  Dynamically allocates memory for the event_ids hashset and passes ownership to the caller.
 */
static
bool
eventpipe_collect_tracing_command_try_parse_event_filter (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderEventFilter *event_filter)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (event_filter != NULL);

	bool result = false;
	uint32_t event_id_array_len = 0;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_bool (buffer, buffer_len, &event_filter->enable));

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &event_id_array_len));
	if (event_id_array_len > 0) {
		event_filter->event_ids = dn_umap_alloc ();
		ep_raise_error_if_nok (event_filter->event_ids != NULL);

		for (uint32_t i = 0; i < event_id_array_len; ++i) {
			uint32_t event_id;
			ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &event_id));
			dn_umap_result_t insert_result = dn_umap_ptr_uint32_insert (event_filter->event_ids, (void *)(uintptr_t)event_id, 0);
			ep_raise_error_if_nok (insert_result.result);
		}
	} else {
		event_filter->event_ids = NULL;
	}

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	ep_event_filter_fini (event_filter);

	ep_exit_error_handler ();
}

/*
 *  eventpipe_collect_tracing_command_try_parse_tracepoint_config
 *
 *  Introduced in CollectTracing5, user_events-based EventPipe Sessions are required to
 *  specify a tracepoint configuration per-provider that details which events should be
 *  written to which tracepoints. Atleast one of default_tracepoint_name or tracepoints
 *  must be specified.
 *
 *  tracepoint_config format:
 *  - string default_tracepoint_name: If specified, the default tracepoint to write unmapped enabled events to.
 *  - array<tracepoint_set> tracepoints: If specified, maps enabled events to tracepoints.
 *
 *  tracepoint_set format:
 *  - string tracepoint_name: the tracepoint to write the following enabled events_ids to.
 *  - array<uint> event_ids: The Event IDs to be written to tracepoint_name.
 */
static
bool
eventpipe_collect_tracing_command_try_parse_tracepoint_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderTracepointConfiguration *tracepoint_config)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (tracepoint_config != NULL);

	bool result = false;

	EventPipeTracepoint *tracepoint = NULL;
	ep_char8_t *tracepoint_name = NULL;
	size_t default_tracepoint_format_len = 0;
	uint8_t *tracepoint_name_byte_array = NULL;
	uint32_t tracepoint_name_byte_array_len = 0;

	uint32_t tracepoint_set_array_len = 0;

	tracepoint_config->default_tracepoint.tracepoint_format[0] = '\0'; // Initialize to empty string.
	tracepoint_config->tracepoints = NULL;
	tracepoint_config->event_id_to_tracepoint_map = NULL;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (buffer, buffer_len, &tracepoint_name_byte_array, &tracepoint_name_byte_array_len));

	if (tracepoint_name_byte_array) {
		// Make Helper
		tracepoint_name = ep_rt_utf16le_to_utf8_string ((const ep_char16_t *)tracepoint_name_byte_array);
		ep_raise_error_if_nok (!ep_rt_utf8_string_is_null_or_empty (tracepoint_name));
		ep_rt_byte_array_free (tracepoint_name_byte_array);
		tracepoint_name_byte_array = NULL;

		default_tracepoint_format_len = strlen(tracepoint_name) + strlen(EP_TRACEPOINT_FORMAT_V1) + 2; // +2 for space and null terminator
		int32_t res = snprintf(tracepoint_config->default_tracepoint.tracepoint_format, sizeof(tracepoint_config->default_tracepoint.tracepoint_format), "%s %s", tracepoint_name, EP_TRACEPOINT_FORMAT_V1);
		ep_raise_error_if_nok (res >= 0 && (size_t)res < default_tracepoint_format_len);
		ep_rt_utf8_string_free ((ep_char8_t *)tracepoint_name);
		tracepoint_name = NULL;
	}

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &tracepoint_set_array_len));

	if (tracepoint_set_array_len > 0) {
		dn_vector_ptr_custom_alloc_params_t tracepoint_set_array_params = {0, };
		tracepoint_set_array_params.capacity = tracepoint_set_array_len;
		tracepoint_config->tracepoints = dn_vector_ptr_custom_alloc (&tracepoint_set_array_params);
		ep_raise_error_if_nok (tracepoint_config->tracepoints != NULL);

		tracepoint_config->event_id_to_tracepoint_map = dn_umap_alloc ();
		ep_raise_error_if_nok (tracepoint_config->event_id_to_tracepoint_map != NULL);

		for (uint32_t i = 0; i < tracepoint_set_array_len; ++i) {
			tracepoint = ep_rt_object_alloc (EventPipeTracepoint); // Ownership will be transferred to the tracepoint_config's tracepoint vector.
			ep_raise_error_if_nok (tracepoint != NULL);

			ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (buffer, buffer_len, &tracepoint_name_byte_array, &tracepoint_name_byte_array_len));

			// Make Helper
			tracepoint_name = ep_rt_utf16le_to_utf8_string ((const ep_char16_t *)tracepoint_name_byte_array);
			ep_raise_error_if_nok (!ep_rt_utf8_string_is_null_or_empty (tracepoint_name));
			ep_rt_byte_array_free (tracepoint_name_byte_array);
			tracepoint_name_byte_array = NULL;

			default_tracepoint_format_len = strlen(tracepoint_name) + strlen(EP_TRACEPOINT_FORMAT_V1) + 2; // +2 for space and null terminator
			int32_t res = snprintf(tracepoint->tracepoint_format, sizeof(tracepoint->tracepoint_format), "%s %s", tracepoint_name, EP_TRACEPOINT_FORMAT_V1);
			ep_raise_error_if_nok (res >= 0 && (size_t)res < default_tracepoint_format_len);
			ep_rt_utf8_string_free ((ep_char8_t *)tracepoint_name);
			tracepoint_name = NULL;

			uint32_t event_id_array_len = 0;
			ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &event_id_array_len));
			ep_raise_error_if_nok (event_id_array_len > 0);

			for (uint32_t j = 0; j < event_id_array_len; ++j) {
				uint32_t event_id;
				ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &event_id));
				dn_umap_result_t insert_result = dn_umap_insert (tracepoint_config->event_id_to_tracepoint_map, (void *)(uintptr_t)event_id, tracepoint);
				ep_raise_error_if_nok (insert_result.result);
			}

			ep_raise_error_if_nok (dn_vector_ptr_push_back (tracepoint_config->tracepoints, tracepoint));
			tracepoint = NULL;
			// Ownership of tracepoint is transferred to the tracepoint_config's tracepoint vector.
		}
	}

	ep_raise_error_if_nok (tracepoint_config->default_tracepoint.tracepoint_format[0] != '\0' || tracepoint_config->tracepoints != NULL);

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	ep_rt_object_free (tracepoint);
	tracepoint = NULL;

	ep_rt_byte_array_free (tracepoint_name_byte_array);
	tracepoint_name_byte_array = NULL;

	ep_rt_utf8_string_free ((ep_char8_t *)tracepoint_name);
	tracepoint_name = NULL;

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

	uint32_t log_level = 0;

	uint8_t *provider_name_byte_array = NULL;
	uint32_t provider_name_byte_array_len = 0;

	uint8_t *filter_data_byte_array = NULL;
	uint32_t filter_data_byte_array_len = 0;

	EventPipeProviderEventFilter *event_filter = NULL;
	EventPipeProviderTracepointConfiguration *tracepoint_config = NULL;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint64_t (buffer, buffer_len, &provider_config->keywords));

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &log_level));
	provider_config->logging_level = (EventPipeEventLevel)log_level;
	ep_raise_error_if_nok (provider_config->logging_level <= EP_EVENT_LEVEL_VERBOSE);

	ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (buffer, buffer_len, &provider_name_byte_array, &provider_name_byte_array_len));
	provider_config->provider_name = ep_rt_utf16le_to_utf8_string ((const ep_char16_t *)provider_name_byte_array);
	ep_raise_error_if_nok (!ep_rt_utf8_string_is_null_or_empty (provider_config->provider_name));
	ep_rt_byte_array_free (provider_name_byte_array);
	provider_name_byte_array = NULL;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (buffer, buffer_len, &filter_data_byte_array, &filter_data_byte_array_len));
	if (filter_data_byte_array) {
		provider_config->filter_data = ep_rt_utf16le_to_utf8_string ((const ep_char16_t *)filter_data_byte_array);
		ep_raise_error_if_nok (provider_config->filter_data != NULL);
		ep_rt_byte_array_free (filter_data_byte_array);
		filter_data_byte_array = NULL;
	}

	if ((optional_field_flags & EP_PROVIDER_OPTFIELD_EVENT_FILTER) != 0) {
		event_filter = ep_rt_object_alloc(EventPipeProviderEventFilter);
		ep_raise_error_if_nok (event_filter != NULL);
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_event_filter (buffer, buffer_len, event_filter));
		provider_config->event_filter = event_filter;
		event_filter = NULL; // Ownership transferred to provider_config.
	}

	if ((optional_field_flags & EP_PROVIDER_OPTFIELD_TRACEPOINT_CONFIG) != 0) {
		tracepoint_config = ep_rt_object_alloc (EventPipeProviderTracepointConfiguration);
		ep_raise_error_if_nok (tracepoint_config != NULL);
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_tracepoint_config (buffer, buffer_len, tracepoint_config));
		provider_config->tracepoint_config = tracepoint_config;
		tracepoint_config = NULL; // Ownership transferred to provider_config.
	}

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	if (tracepoint_config != NULL) {
		ep_tracepoint_config_free (tracepoint_config);
		tracepoint_config = NULL;
	}

	if (event_filter != NULL) {
		ep_event_filter_free (event_filter);
		event_filter = NULL;
	}

	ep_rt_byte_array_free (filter_data_byte_array);
	filter_data_byte_array = NULL;

	ep_rt_byte_array_free (provider_name_byte_array);
	provider_name_byte_array = NULL;

	ep_exit_error_handler ();
}

/*
 * eventpipe_collect_tracing_command_try_parse_config
 *
 * With the introduction of CollectTracing5, there is more flexiblity in provider configuration encoding.
 * This function deserializes all provider configurations from the IPC Stream, providing callers the flexibility
 * to specify which optional fields are present for the particular CollectTracingN command.
 *
 * Ownership of all EventPipeProviderConfigurations data is transferred to the caller.
 */
static
bool
eventpipe_collect_tracing_command_try_parse_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeProviderOptionalFieldFlags optional_field_flags,
	dn_vector_ptr_t **result)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (result != NULL);

	// Picking an arbitrary upper bound,
	// This should be larger than any reasonable client request.
	const uint32_t max_count_configs = 1000;
	uint32_t count_configs = 0;
	dn_vector_ptr_custom_alloc_params_t params = {0, };

	EventPipeProviderConfiguration *provider_config = NULL;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &count_configs));
	ep_raise_error_if_nok (count_configs <= max_count_configs);
	params.capacity = count_configs;
	*result = dn_vector_ptr_custom_alloc (&params);
	ep_raise_error_if_nok (*result);

	for (uint32_t i = 0; i < count_configs; ++i) {
		provider_config = ep_rt_object_alloc (EventPipeProviderConfiguration);
		ep_raise_error_if_nok (provider_config != NULL);
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_provider_config (
			buffer,
			buffer_len,
			optional_field_flags,
			provider_config));

		ep_raise_error_if_nok (dn_vector_ptr_push_back (*result, provider_config));
		provider_config = NULL; // Ownership transferred.
	}

ep_on_exit:
	return (count_configs > 0);

ep_on_error:
	count_configs = 0;

	if (provider_config != NULL) {
		ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_provider_name (provider_config));
		ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_filter_data (provider_config));
		ep_event_filter_free ((EventPipeProviderEventFilter *)ep_provider_config_get_event_filter (provider_config));
		ep_tracepoint_config_free ((EventPipeProviderTracepointConfiguration *)ep_provider_config_get_tracepoint_config (provider_config));
		ep_rt_object_free (provider_config);
		provider_config = NULL;
	}

	ep_exit_error_handler ();
}

EventPipeCollectTracingCommandPayload *
ds_eventpipe_collect_tracing_command_payload_alloc (void)
{
	return ep_rt_object_alloc (EventPipeCollectTracingCommandPayload);
}

void
ds_eventpipe_collect_tracing_command_payload_free (EventPipeCollectTracingCommandPayload *payload)
{
	ep_return_void_if_nok (payload != NULL);
	ep_rt_byte_array_free (payload->incoming_buffer);

	DN_VECTOR_PTR_FOREACH_BEGIN (EventPipeProviderConfiguration *, config, payload->provider_configs) {
		ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_provider_name (config));
		ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_filter_data (config));
		ep_event_filter_free ((EventPipeProviderEventFilter *)ep_provider_config_get_event_filter (config));
		ep_tracepoint_config_free ((EventPipeProviderTracepointConfiguration *)ep_provider_config_get_tracepoint_config (config));
	} DN_VECTOR_FOREACH_END;

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
		!eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, EP_PROVIDER_OPTFIELD_NONE, &instance->provider_configs))
		ep_raise_error ();
	instance->rundown_requested = true;
	instance->stackwalk_requested = true;
	instance->rundown_keyword = ep_default_rundown_keyword;
	instance->session_type = EP_IPC_SESSION_TYPE_STREAMING;

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
		!eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, EP_PROVIDER_OPTFIELD_NONE, &instance->provider_configs))
		ep_raise_error ();

	instance->rundown_keyword = instance->rundown_requested ? ep_default_rundown_keyword : 0;

	instance->stackwalk_requested = true;
	instance->session_type = EP_IPC_SESSION_TYPE_STREAMING;

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
		!eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, EP_PROVIDER_OPTFIELD_NONE, &instance->provider_configs))
		ep_raise_error ();

	instance->rundown_keyword = instance->rundown_requested ? ep_default_rundown_keyword : 0;
	instance->session_type = EP_IPC_SESSION_TYPE_STREAMING;

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
		!eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, EP_PROVIDER_OPTFIELD_NONE, &instance->provider_configs))
		ep_raise_error ();

	instance->rundown_requested = instance->rundown_keyword != 0;
	instance->session_type = EP_IPC_SESSION_TYPE_STREAMING;

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

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (&buffer_cursor, &buffer_cursor_len, &instance->session_type));
	ep_raise_error_if_nok (instance->session_type < EP_IPC_SESSION_TYPE_COUNT);

	if (instance->session_type == EP_IPC_SESSION_TYPE_STREAMING) {
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb));
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format));
	} else if (instance->session_type == EP_IPC_SESSION_TYPE_USEREVENTS) {
		instance->circular_buffer_size_in_mb = 0;
		instance->serialization_format = EP_SERIALIZATION_FORMAT_NETTRACE_V4; // Serialization format isn't used for user_events sessions, default for check_options_valid.
	}

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint64_t (&buffer_cursor, &buffer_cursor_len, &instance->rundown_keyword));

	if (instance->session_type == EP_IPC_SESSION_TYPE_STREAMING) {
		ep_raise_error_if_nok (ds_ipc_message_try_parse_bool (&buffer_cursor, &buffer_cursor_len, &instance->stackwalk_requested));
	} else if (instance->session_type == EP_IPC_SESSION_TYPE_USEREVENTS) {
		instance->stackwalk_requested = false;
	}

	if (instance->session_type == EP_IPC_SESSION_TYPE_USEREVENTS)
		optional_field_flags = (EventPipeProviderOptionalFieldFlags)(optional_field_flags | EP_PROVIDER_OPTFIELD_TRACEPOINT_CONFIG);

	ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, optional_field_flags, &instance->provider_configs));

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

	int user_events_data_fd = 0;
	if (payload->session_type == EP_IPC_SESSION_TYPE_USEREVENTS) {
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
		*(EventPipeProviderConfiguration **)dn_vector_ptr_data (payload->provider_configs),
		dn_vector_ptr_size (payload->provider_configs),
		payload->session_type == EP_IPC_SESSION_TYPE_USEREVENTS ? EP_SESSION_TYPE_USEREVENTS : EP_SESSION_TYPE_IPCSTREAM,
		payload->serialization_format,
		payload->rundown_keyword,
		payload->stackwalk_requested,
		payload->session_type == EP_IPC_SESSION_TYPE_STREAMING ? ds_ipc_stream_get_stream_ref (stream) : NULL,
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
