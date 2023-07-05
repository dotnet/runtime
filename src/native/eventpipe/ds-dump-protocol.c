#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_DUMP_PROTOCOL_GETTER_SETTER
#include "ds-protocol.h"
#include "ds-dump-protocol.h"
#include "ds-rt.h"

const ep_char16_t empty_string [1] = { 0 };

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
		!ds_ipc_message_try_parse_uint32_t (&buffer_cursor, &buffer_cursor_len, &instance->flags))
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
void
dump_protocol_generate_core_dump_response_init(
	DiagnosticsGenerateCoreDumpResponsePayload *payload,
	ds_ipc_result_t error,
	const ep_char8_t *errorText)
{
	EP_ASSERT (payload != NULL);

	payload->error = error;
	// If this conversion failures it will set error_message to NULL which will send an empty message
	payload->error_message = ep_rt_utf8_to_utf16le_string (errorText, -1);
}

static
void
dump_protocol_generate_core_dump_response_fini(
	DiagnosticsGenerateCoreDumpResponsePayload *payload)
{
	ep_rt_utf16_string_free (payload->error_message);
}

static
uint16_t
dump_protocol_generate_core_dump_response_get_size (
	DiagnosticsGenerateCoreDumpResponsePayload *payload)
{
	EP_ASSERT (payload != NULL);

	size_t size = sizeof(payload->error);

	size += sizeof(uint32_t);
	size += (payload->error_message != NULL) ? (ep_rt_utf16_string_len (payload->error_message) + 1) * sizeof(ep_char16_t) : 0;

	EP_ASSERT (size <= UINT16_MAX);
	return (uint16_t)size;
}

static
bool
dump_protocol_generate_core_dump_response_flatten (
	void *payload,
	uint8_t **buffer,
	uint16_t *size)
{
	DiagnosticsGenerateCoreDumpResponsePayload *response = (DiagnosticsGenerateCoreDumpResponsePayload*)payload;

	EP_ASSERT (payload != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (*buffer != NULL);
	EP_ASSERT (size != NULL);
	EP_ASSERT (dump_protocol_generate_core_dump_response_get_size (response) == *size);

	bool success = true;

	// ds_ipc_result_t size error
	memcpy (*buffer, &response->error, sizeof (response->error));
	*buffer += sizeof (response->error);
	*size -= sizeof (response->error);

	// LPCWSTR error message - if there is no error_message (NULL) then write an empty string
	success &= ds_ipc_message_try_write_string_utf16_t (buffer, size, response->error_message != NULL ? response->error_message : empty_string);

	// Assert we've used the whole buffer we were given
	EP_ASSERT(*size == 0);

	return success;
}

static
void
dump_protocol_generate_core_dump_response (
	DiagnosticsIpcStream *stream,
	ds_ipc_result_t error,
	const ep_char8_t * errorText)
{
	DiagnosticsGenerateCoreDumpResponsePayload payload;
	DiagnosticsIpcMessage message;
	ds_ipc_message_init (&message);

	dump_protocol_generate_core_dump_response_init(&payload, error, errorText);

	bool result = ds_ipc_message_initialize_buffer (
		&message,
		ds_ipc_header_get_generic_error (),
		&payload,
		dump_protocol_generate_core_dump_response_get_size(&payload),
		dump_protocol_generate_core_dump_response_flatten);

	if (result)
		ds_ipc_message_send (&message, stream);

	ds_ipc_message_fini (&message);
	dump_protocol_generate_core_dump_response_fini (&payload);
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

	ds_ipc_result_t ipc_result = DS_IPC_E_FAIL;
	DiagnosticsDumpCommandId commandId = (DiagnosticsDumpCommandId)ds_ipc_header_get_commandid (ds_ipc_message_get_header_ref (message));
	DiagnosticsGenerateCoreDumpCommandPayload *payload;
	payload = (DiagnosticsGenerateCoreDumpCommandPayload *)ds_ipc_message_try_parse_payload (message, generate_core_dump_command_try_parse_payload);

	if (!payload) {
		ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
		ep_raise_error ();
	}

	ep_char8_t errorMessage[1024];
	errorMessage[0] = '\0';

	ipc_result = ds_rt_generate_core_dump (commandId, payload, errorMessage, sizeof(errorMessage));
	if (ipc_result != DS_IPC_S_OK) {
		if (commandId == DS_DUMP_COMMANDID_GENERATE_CORE_DUMP3) {
			dump_protocol_generate_core_dump_response (stream, ipc_result, errorMessage);
		}
		else {
			ds_ipc_message_send_error (stream, ipc_result);
		}
		ep_raise_error ();
	} else {
		ds_ipc_message_send_success (stream, ipc_result);
	}

ep_on_exit:
	ds_generate_core_dump_command_payload_free (payload);
	ds_ipc_stream_free (stream);
	return ipc_result == DS_IPC_S_OK;

ep_on_error:
	EP_ASSERT (ipc_result != DS_IPC_S_OK);
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
	case DS_DUMP_COMMANDID_GENERATE_CORE_DUMP2:
	case DS_DUMP_COMMANDID_GENERATE_CORE_DUMP3:
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

#if !defined(ENABLE_PERFTRACING) || (defined(DS_INCLUDE_SOURCE_FILES) && !defined(DS_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_diagnostics_dump_protocol;
const char quiet_linker_empty_file_warning_diagnostics_dump_protocol = 0;
#endif
