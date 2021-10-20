#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_PROFILER_PROTOCOL_GETTER_SETTER
#include "ds-protocol.h"
#include "ds-profiler-protocol.h"
#include "ds-server.h"
#include "ds-rt.h"

#ifdef PROFILING_SUPPORTED

/*
 * Forward declares of all static functions.
 */
static
uint8_t *
attach_profiler_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
uint8_t *
startup_profiler_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
bool
profiler_protocol_helper_attach_profiler (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
profiler_protocol_helper_startup_profiler (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
profiler_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

#ifdef FEATURE_PROFAPI_ATTACH_DETACH

/*
* DiagnosticsAttachProfilerCommandPayload
*/

static
uint8_t *
attach_profiler_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	DiagnosticsAttachProfilerCommandPayload *instance = ds_attach_profiler_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!ds_ipc_message_try_parse_uint32_t (&buffer_cursor, &buffer_cursor_len, &instance->attach_timeout ) ||
		!ds_ipc_message_try_parse_value (&buffer_cursor, &buffer_cursor_len, (uint8_t *)&instance->profiler_guid, (uint32_t)EP_ARRAY_SIZE (instance->profiler_guid)) ||
		!ds_ipc_message_try_parse_string_utf16_t (&buffer_cursor, &buffer_cursor_len, &instance->profiler_path) ||
		!ds_ipc_message_try_parse_uint32_t (&buffer_cursor, &buffer_cursor_len, &instance->client_data_len) ||
		!(buffer_cursor_len <= instance->client_data_len))
		ep_raise_error ();

	instance->client_data = buffer_cursor;

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_attach_profiler_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

DiagnosticsAttachProfilerCommandPayload *
ds_attach_profiler_command_payload_alloc (void)
{
	return ep_rt_object_alloc (DiagnosticsAttachProfilerCommandPayload);
}

void
ds_attach_profiler_command_payload_free (DiagnosticsAttachProfilerCommandPayload *payload)
{
	ep_return_void_if_nok (payload != NULL);
	ep_rt_byte_array_free (payload->incoming_buffer);
	ep_rt_object_free (payload);
}

static
bool
profiler_protocol_helper_attach_profiler (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	if (!stream)
		return false;

    bool result = false;
    DiagnosticsAttachProfilerCommandPayload *payload = NULL;

    if (!ep_rt_is_running ()) {
        ds_ipc_message_send_error (stream, DS_IPC_E_NOT_YET_AVAILABLE);
        ep_raise_error ();
    }

	payload = (DiagnosticsAttachProfilerCommandPayload *)ds_ipc_message_try_parse_payload (message, attach_profiler_command_try_parse_payload);

	if (!payload) {
		ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
		ep_raise_error ();
	}

	ds_ipc_result_t ipc_result;
	ipc_result = ds_rt_profiler_attach (payload);
	if (ipc_result != DS_IPC_S_OK) {
		ds_ipc_message_send_error (stream, ipc_result);
		ep_raise_error ();
	} else {
		ds_ipc_message_send_success (stream, ipc_result);
	}

	result = true;

ep_on_exit:
	ds_attach_profiler_command_payload_free (payload);
    ds_ipc_stream_free (stream);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ep_exit_error_handler ();
}

#endif // FEATURE_PROFAPI_ATTACH_DETACH

DiagnosticsStartupProfilerCommandPayload *
ds_startup_profiler_command_payload_alloc (void)
{
	return ep_rt_object_alloc (DiagnosticsStartupProfilerCommandPayload);
}

void
ds_startup_profiler_command_payload_free (DiagnosticsStartupProfilerCommandPayload *payload)
{
	ep_return_void_if_nok (payload != NULL);
	ep_rt_byte_array_free (payload->incoming_buffer);
	ep_rt_object_free (payload);
}

/*
 * DiagnosticsProfilerProtocolHelper.
 */

static
bool
profiler_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	DS_LOG_WARNING_1 ("Received unknown request type (%d)", ds_ipc_header_get_commandset (ds_ipc_message_get_header_ref (message)));
	ds_ipc_message_send_error (stream, DS_IPC_E_UNKNOWN_COMMAND);
	return true;
}

static
uint8_t *
startup_profiler_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	DiagnosticsStartupProfilerCommandPayload *instance = ds_startup_profiler_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!ds_ipc_message_try_parse_value (&buffer_cursor, &buffer_cursor_len, (uint8_t *)&instance->profiler_guid, (uint32_t)EP_ARRAY_SIZE (instance->profiler_guid)) ||
		!ds_ipc_message_try_parse_string_utf16_t (&buffer_cursor, &buffer_cursor_len, &instance->profiler_path))
		ep_raise_error ();

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_startup_profiler_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

bool
profiler_protocol_helper_startup_profiler (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	if (!stream)
		return false;

    bool result = false;
    DiagnosticsStartupProfilerCommandPayload *payload = NULL;

	if (!ds_server_is_paused_in_startup()) {
		ds_ipc_message_send_error (stream, DS_IPC_E_INVALIDARG);
		ep_raise_error ();
	}		

	payload = (DiagnosticsStartupProfilerCommandPayload *)ds_ipc_message_try_parse_payload (message, startup_profiler_command_try_parse_payload);

	if (!payload) {
		ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
		ep_raise_error ();
	}

	ds_ipc_result_t ipc_result;
	ipc_result = ds_rt_profiler_startup (payload);
	if (ipc_result != DS_IPC_S_OK) {
		ds_ipc_message_send_error (stream, ipc_result);
		ep_raise_error ();
	} else {
		ds_ipc_message_send_success (stream, ipc_result);
	}

	result = true;

ep_on_exit:
	ds_startup_profiler_command_payload_free (payload);
    ds_ipc_stream_free (stream);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ep_exit_error_handler ();
}

bool
ds_profiler_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	bool result = false;

	switch ((DiagnosticsProfilerCommandId)ds_ipc_header_get_commandid (ds_ipc_message_get_header_ref (message))) {
#ifdef FEATURE_PROFAPI_ATTACH_DETACH
	case DS_PROFILER_COMMANDID_ATTACH_PROFILER:
		result = profiler_protocol_helper_attach_profiler (message, stream);
		break;
#endif // FEATURE_PROFAPI_ATTACH_DETACH
	case DS_PROFILER_COMMANDID_STARTUP_PROFILER:
		result = profiler_protocol_helper_startup_profiler (message, stream);
		break;
 	default:
		result = profiler_protocol_helper_unknown_command (message, stream);
		break;
	}

	return result;
}
#else // PROFILING_SUPPORTED
bool
ds_profiler_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	DS_LOG_WARNING_0 ("Profiler support not enabled in this runtime");
	ds_ipc_message_send_error (stream, DS_IPC_E_NOTSUPPORTED);
    ds_ipc_stream_free (stream);

	return true;
}
#endif // PROFILING_SUPPORTED 

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef DS_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_diagnostics_profiler_protocol;
const char quiet_linker_empty_file_warning_diagnostics_profiler_protocol = 0;
#endif
