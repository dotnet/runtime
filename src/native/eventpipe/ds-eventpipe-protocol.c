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
eventpipe_collect_tracing_command_try_parse_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	ep_rt_provider_config_array_t *result);

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
bool
eventpipe_protocol_helper_stop_tracing (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
eventpipe_protocol_helper_collect_tracing (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
eventpipe_protocol_helper_collect_tracing_2 (
	DiagnosticsIpcMessage *message,
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

	return ds_ipc_message_try_parse_value (buffer, buffer_len, (uint8_t *)rundown_requested, (uint32_t)sizeof (bool));
}

static
bool
eventpipe_collect_tracing_command_try_parse_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	ep_rt_provider_config_array_t *result)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (result != NULL);

	// Picking an arbitrary upper bound,
	// This should be larger than any reasonable client request.
	// TODO: This might be too large.
	const uint32_t max_count_configs = 1000;
	uint32_t count_configs = 0;

	ep_char8_t *provider_name_utf8 = NULL;
	ep_char8_t *filter_data_utf8 = NULL;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &count_configs));
	ep_raise_error_if_nok (count_configs <= max_count_configs);

	ep_rt_provider_config_array_alloc_capacity (result, count_configs);

	for (uint32_t i = 0; i < count_configs; ++i) {
		uint64_t keywords = 0;
		ep_raise_error_if_nok (ds_ipc_message_try_parse_uint64_t (buffer, buffer_len, &keywords));

		uint32_t log_level = 0;
		ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &log_level));
		ep_raise_error_if_nok (log_level <= EP_EVENT_LEVEL_VERBOSE);

		const ep_char16_t *provider_name = NULL;
		ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t (buffer, buffer_len, &provider_name));

		provider_name_utf8 = ep_rt_utf16_to_utf8_string (provider_name, -1);
		ep_raise_error_if_nok (provider_name_utf8 != NULL);

		ep_raise_error_if_nok (!ep_rt_utf8_string_is_null_or_empty (provider_name_utf8));

		const ep_char16_t *filter_data = NULL; // This parameter is optional.
		ds_ipc_message_try_parse_string_utf16_t (buffer, buffer_len, &filter_data);

		if (filter_data) {
			filter_data_utf8 = ep_rt_utf16_to_utf8_string (filter_data, -1);
			ep_raise_error_if_nok (filter_data_utf8 != NULL);
		}

		EventPipeProviderConfiguration provider_config;
		if (ep_provider_config_init (&provider_config, provider_name_utf8, keywords, (EventPipeEventLevel)log_level, filter_data_utf8)) {
			if (ep_rt_provider_config_array_append (result, provider_config)) {
				// Ownership transfered.
				provider_name_utf8 = NULL;
				filter_data_utf8 = NULL;
			}
			ep_provider_config_fini (&provider_config);
		}
		ep_raise_error_if_nok (provider_name_utf8 == NULL && filter_data_utf8 == NULL);
	}

ep_on_exit:
	return (count_configs > 0);

ep_on_error:
	count_configs = 0;
	ep_rt_utf8_string_free (provider_name_utf8);
	ep_rt_utf8_string_free (filter_data_utf8);
	ep_exit_error_handler ();
}

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
		!eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, &instance->provider_configs))
		ep_raise_error ();

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing_command_payload_free (instance);
	instance = NULL;
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

	EventPipeProviderConfiguration *config = ep_rt_provider_config_array_data (&payload->provider_configs);
	size_t config_len = ep_rt_provider_config_array_size (&payload->provider_configs);
	for (size_t i = 0; i < config_len; ++i) {
		ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_provider_name (&config [i]));
		ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_filter_data (&config [i]));
	}

	ep_rt_object_free (payload);
}

/*
* EventPipeCollectTracing2CommandPayload
*/

static
uint8_t *
eventpipe_collect_tracing2_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	EventPipeCollectTracing2CommandPayload *instance = ds_eventpipe_collect_tracing2_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb ) ||
		!eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format) ||
		!eventpipe_collect_tracing_command_try_parse_rundown_requested (&buffer_cursor, &buffer_cursor_len, &instance->rundown_requested) ||
		!eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, &instance->provider_configs))
		ep_raise_error ();

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing2_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeCollectTracing2CommandPayload *
ds_eventpipe_collect_tracing2_command_payload_alloc (void)
{
	return ep_rt_object_alloc (EventPipeCollectTracing2CommandPayload);
}

void
ds_eventpipe_collect_tracing2_command_payload_free (EventPipeCollectTracing2CommandPayload *payload)
{
	ep_return_void_if_nok (payload != NULL);
	ep_rt_byte_array_free (payload->incoming_buffer);

	EventPipeProviderConfiguration *config = ep_rt_provider_config_array_data (&payload->provider_configs);
	size_t config_len = ep_rt_provider_config_array_size (&payload->provider_configs);
	for (size_t i = 0; i < config_len; ++i) {
		ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_provider_name (&config [i]));
		ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_filter_data (&config [i]));
	}

	ep_rt_object_free (payload);
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
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	ep_return_false_if_nok (message != NULL && stream != NULL);

	bool result = false;
	EventPipeCollectTracingCommandPayload *payload;
	payload = (EventPipeCollectTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing_command_try_parse_payload);

	if (!payload) {
		ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
		ep_raise_error ();
	}

	EventPipeSessionID session_id;
	session_id = ep_enable (
		NULL,
		payload->circular_buffer_size_in_mb,
		ep_rt_provider_config_array_data (&payload->provider_configs),
		(uint32_t)ep_rt_provider_config_array_size (&payload->provider_configs),
		EP_SESSION_TYPE_IPCSTREAM,
		payload->serialization_format,
		true,
		ds_ipc_stream_get_stream_ref (stream),
		NULL);

	if (session_id == 0) {
		ds_ipc_message_send_error (stream, DS_IPC_E_FAIL);
		ep_raise_error ();
	} else {
		eventpipe_protocol_helper_send_start_tracing_success (stream, session_id);
		ep_start_streaming (session_id);
	}

	result = true;

ep_on_exit:
	ds_eventpipe_collect_tracing_command_payload_free (payload);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ds_ipc_stream_free (stream);
	ep_exit_error_handler ();
}

static
bool
eventpipe_protocol_helper_collect_tracing_2 (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	ep_return_false_if_nok (message != NULL && stream != NULL);

	bool result = false;
	EventPipeCollectTracing2CommandPayload *payload;
	payload = (EventPipeCollectTracing2CommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing2_command_try_parse_payload);

	if (!payload) {
		ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
		ep_raise_error ();
	}

	EventPipeSessionID session_id;
	session_id = ep_enable (
		NULL,
		payload->circular_buffer_size_in_mb,
		ep_rt_provider_config_array_data (&payload->provider_configs),
		(uint32_t)ep_rt_provider_config_array_size (&payload->provider_configs),
		EP_SESSION_TYPE_IPCSTREAM,
		payload->serialization_format,
		payload->rundown_requested,
		ds_ipc_stream_get_stream_ref (stream),
		NULL);

	if (session_id == 0) {
		ds_ipc_message_send_error (stream, DS_IPC_E_FAIL);
		ep_raise_error ();
	} else {
		eventpipe_protocol_helper_send_start_tracing_success (stream, session_id);
		ep_start_streaming (session_id);
	}

	result = true;

ep_on_exit:
	ds_eventpipe_collect_tracing2_command_payload_free (payload);
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

	switch ((EventPipeCommandId)ds_ipc_header_get_commandid (ds_ipc_message_get_header_cref (message))) {
	case EP_COMMANDID_COLLECT_TRACING:
		result = eventpipe_protocol_helper_collect_tracing (message, stream);
		break;
	case EP_COMMANDID_COLLECT_TRACING_2:
		result = eventpipe_protocol_helper_collect_tracing_2 (message, stream);
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

#ifndef DS_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_diagnostics_eventpipe_protocol;
const char quiet_linker_empty_file_warning_diagnostics_eventpipe_protocol = 0;
#endif
