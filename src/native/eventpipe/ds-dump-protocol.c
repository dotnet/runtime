#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_DUMP_PROTOCOL_GETTER_SETTER
#include "ds-protocol.h"
#include "ds-dump-protocol.h"
#include "ds-rt.h"

/*
 * Forward declares of all static functions.
 */
static
uint8_t *
generate_core_dump_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
bool
dump_protocol_helper_generate_core_dump (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
dump_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

/*
* DiagnosticsGenerateCoreDumpCommandPayload
*/

static
uint8_t *
generate_core_dump_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	DiagnosticsGenerateCoreDumpCommandPayload *instance = ds_generate_core_dump_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!ds_ipc_message_try_parse_string_utf16_t (&buffer_cursor, &buffer_cursor_len, &instance->dump_name ) ||
		!ds_ipc_message_try_parse_uint32_t (&buffer_cursor, &buffer_cursor_len, &instance->dump_type) ||
		!ds_ipc_message_try_parse_uint32_t (&buffer_cursor, &buffer_cursor_len, &instance->diagnostics))
		ep_raise_error ();

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_generate_core_dump_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

DiagnosticsGenerateCoreDumpCommandPayload *
ds_generate_core_dump_command_payload_alloc (void)
{
	return ep_rt_object_alloc (DiagnosticsGenerateCoreDumpCommandPayload);
}

void
ds_generate_core_dump_command_payload_free (DiagnosticsGenerateCoreDumpCommandPayload *payload)
{
	ep_return_void_if_nok (payload != NULL);
	ep_rt_byte_array_free (payload->incoming_buffer);
	ep_rt_object_free (payload);
}

/*
 * DiagnosticsDumpProtocolHelper.
 */

static
bool
dump_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	DS_LOG_WARNING_1 ("Received unknown request type (%d)", ds_ipc_header_get_commandset (ds_ipc_message_get_header_ref (message)));
	ds_ipc_message_send_error (stream, DS_IPC_E_UNKNOWN_COMMAND);
	ds_ipc_stream_free (stream);
	return true;
}

static
bool
dump_protocol_helper_generate_core_dump (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	if (!stream)
		return false;

	bool result = false;
	DiagnosticsGenerateCoreDumpCommandPayload *payload;
	payload = (DiagnosticsGenerateCoreDumpCommandPayload *)ds_ipc_message_try_parse_payload (message, generate_core_dump_command_try_parse_payload);

	if (!payload) {
		ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
		ep_raise_error ();
	}

	ds_ipc_result_t ipc_result;
	ipc_result = ds_rt_generate_core_dump (payload);
	if (result != DS_IPC_S_OK) {
		ds_ipc_message_send_error (stream, result);
		ep_raise_error ();
	} else {
		ds_ipc_message_send_success (stream, result);
	}

	result = true;

ep_on_exit:
	ds_generate_core_dump_command_payload_free (payload);
	ds_ipc_stream_free (stream);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ep_exit_error_handler ();
}

bool
ds_dump_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	bool result = false;

	switch ((DiagnosticsDumpCommandId)ds_ipc_header_get_commandid (ds_ipc_message_get_header_ref (message))) {
	case DS_DUMP_COMMANDID_GENERATE_CORE_DUMP:
		result = dump_protocol_helper_generate_core_dump (message, stream);
		break;
	default:
		result = dump_protocol_helper_unknown_command (message, stream);
		break;
	}
	return result;
}

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef DS_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_diagnostics_dump_protocol;
const char quiet_linker_empty_file_warning_diagnostics_dump_protocol = 0;
#endif
