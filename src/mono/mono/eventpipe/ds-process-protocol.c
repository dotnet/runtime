#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ds-rt-config.h"
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_PROCESS_PROTOCOL_GETTER_SETTER
#include "ds-protocol.h"
#include "ds-process-protocol.h"
#include "ds-server.h"
#include "ep.h"
#include "ds-rt.h"
#include "ep-event-source.h"

/*
 * Forward declares of all static functions.
 */

static
uint16_t
process_info_payload_get_size (DiagnosticsProcessInfoPayload *payload);

static
bool
process_info_payload_flatten (
	void *payload,
	uint8_t **buffer,
	uint16_t *size);

static
void
process_protocol_helper_get_process_info (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
void
process_protocol_helper_get_process_env (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
void
process_protocol_helper_resume_runtime_startup (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
void
process_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

/*
 * DiagnosticsProcessInfoPayload.
 */

static
uint16_t
process_info_payload_get_size (DiagnosticsProcessInfoPayload *payload)
{
	// see IPC spec @ https://github.com/dotnet/diagnostics/blob/master/documentation/design-docs/ipc-protocol.md
	// for definition of serialization format

	// uint64_t ProcessId;  -> 8 bytes
	// GUID RuntimeCookie;  -> 16 bytes
	// LPCWSTR CommandLine; -> 4 bytes + strlen * sizeof(WCHAR)
	// LPCWSTR OS;          -> 4 bytes + strlen * sizeof(WCHAR)
	// LPCWSTR Arch;        -> 4 bytes + strlen * sizeof(WCHAR)

	EP_ASSERT (payload != NULL);

	size_t size = 0;
	size += sizeof(payload->process_id);
	size += sizeof(payload->runtime_cookie);

	size += sizeof(uint32_t);
	size += (payload->command_line != NULL) ?
		(ep_rt_utf16_string_len (payload->command_line) + 1) * sizeof(ep_char16_t) : 0;

	size += sizeof(uint32_t);
	size += (payload->os != NULL) ?
		(ep_rt_utf16_string_len (payload->os) + 1) * sizeof(ep_char16_t) : 0;

	size += sizeof(uint32_t);
	size += (payload->arch != NULL) ?
		(ep_rt_utf16_string_len (payload->arch) + 1) * sizeof(ep_char16_t) : 0;

	EP_ASSERT (size <= UINT16_MAX);
	return (uint16_t)size;
}

static
bool
process_info_payload_flatten (
	void *payload,
	uint8_t **buffer,
	uint16_t *size)
{
	DiagnosticsProcessInfoPayload *process_info = (DiagnosticsProcessInfoPayload*)payload;

	EP_ASSERT (payload != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (*buffer != NULL);
	EP_ASSERT (size != NULL);
	EP_ASSERT (process_info_payload_get_size (process_info) == *size);

	// see IPC spec @ https://github.com/dotnet/diagnostics/blob/master/documentation/design-docs/ipc-protocol.md
	// for definition of serialization format

	bool success = true;

	// uint64_t ProcessId;
	memcpy (*buffer, &process_info->process_id, sizeof (process_info->process_id));
	*buffer += sizeof (process_info->process_id);
	*size -= sizeof (process_info->process_id);

	// GUID RuntimeCookie;
	memcpy(*buffer, &process_info->runtime_cookie, sizeof (process_info->runtime_cookie));
	*buffer += sizeof (process_info->runtime_cookie);
	*size -= sizeof (process_info->runtime_cookie);

	// LPCWSTR CommandLine;
	success &= ds_ipc_message_try_write_string_utf16_t (buffer, size, process_info->command_line);

	// LPCWSTR OS;
	if (success)
		success &= ds_ipc_message_try_write_string_utf16_t (buffer, size, process_info->os);

	// LPCWSTR Arch;
	if (success)
		success &= ds_ipc_message_try_write_string_utf16_t (buffer, size, process_info->arch);

	// Assert we've used the whole buffer we were given
	EP_ASSERT(*size == 0);

	return success;
}

DiagnosticsProcessInfoPayload *
ds_process_info_payload_init (
	DiagnosticsProcessInfoPayload *payload,
	const ep_char16_t *command_line,
	const ep_char16_t *os,
	const ep_char16_t *arch,
	uint32_t process_id,
	const uint8_t *runtime_cookie)
{
	ep_return_null_if_nok (payload != NULL);

	payload->command_line = command_line;
	payload->os = os;
	payload->arch = arch;
	payload->process_id = process_id;

	if (runtime_cookie)
		memcpy (&payload->runtime_cookie, runtime_cookie, EP_ACTIVITY_ID_SIZE);

	return payload;
}

void
ds_process_info_payload_fini (DiagnosticsProcessInfoPayload *payload)
{
	;
}

/*
 * DiagnosticsProcessProtocolHelper.
 */

static
void
process_protocol_helper_get_process_info (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	ep_char16_t *command_line = NULL;
	ep_char16_t *os_info = NULL;
	ep_char16_t *arch_info = NULL;

	if (ep_rt_managed_command_line_get ())
		command_line = ep_rt_utf8_to_utf16_string (ep_rt_managed_command_line_get (), -1);

	// Checkout https://github.com/dotnet/coreclr/pull/24433 for more information about this fall back.
	if (!command_line)
		// Use the result from ep_rt_os_command_line_get() instead
		command_line = ep_rt_utf8_to_utf16_string (ep_rt_os_command_line_get (), -1);

	// get OS + Arch info
	os_info = ep_rt_utf8_to_utf16_string (ep_event_source_get_os_info (), -1);
	arch_info = ep_rt_utf8_to_utf16_string (ep_event_source_get_arch_info (), -1);

	DiagnosticsProcessInfoPayload payload;
	ds_process_info_payload_init (
		&payload,
		command_line,
		os_info,
		arch_info,
		ep_rt_current_process_get_id (),
		ds_ipc_advertise_cookie_v1_get ());

	ep_raise_error_if_nok (ds_ipc_message_initialize_buffer (
		message,
		ds_ipc_header_get_generic_success (),
		(void *)&payload,
		process_info_payload_get_size (&payload),
		process_info_payload_flatten) == true);

	ds_ipc_message_send (message, stream);

ep_on_exit:
	ds_process_info_payload_fini (&payload);
	ep_rt_utf16_string_free (arch_info);
	ep_rt_utf16_string_free (os_info);
	ep_rt_utf16_string_free (command_line);
	ds_ipc_stream_free (stream);
	return;

ep_on_error:
	ds_ipc_message_send_error (stream, DS_IPC_E_FAIL);
	DS_LOG_WARNING_0 ("Failed to send DiagnosticsIPC response");
	ep_exit_error_handler ();
}

static
void
process_protocol_helper_get_process_env (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	// TODO: Implement.
	ds_ipc_message_send_error (stream, DS_IPC_E_NOTSUPPORTED);
	DS_LOG_WARNING_0 ("Get Process Environmnet not implemented\n");

	ds_ipc_stream_free (stream);
}

static
void
process_protocol_helper_resume_runtime_startup (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	// no payload
	ds_server_resume_runtime_startup ();
	bool success = ds_ipc_message_send_success (stream, DS_IPC_S_OK);
	if (!success) {
		ds_ipc_message_send_error (stream, DS_IPC_E_FAIL);
		DS_LOG_WARNING_0 ("Failed to send DiagnosticsIPC response");
	}

	ds_ipc_stream_free (stream);
}

static
void
process_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	DS_LOG_WARNING_1 ("Received unknown request type (%d)\n", ds_ipc_message_header_get_commandset (ds_ipc_message_get_header (&message)));
	ds_ipc_message_send_error (stream, DS_IPC_E_UNKNOWN_COMMAND);
	ds_ipc_stream_free (stream);
}

void
ds_process_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	switch ((DiagnosticsProcessCommandId)ds_ipc_header_get_commandid (ds_ipc_message_get_header_ref (message))) {
	case DS_PROCESS_COMMANDID_GET_PROCESS_INFO:
		process_protocol_helper_get_process_info (message, stream);
		break;
	case DS_PROCESS_COMMANDID_RESUME_RUNTIME:
		process_protocol_helper_resume_runtime_startup (message, stream);
		break;
	case DS_PROCESS_COMMANDID_GET_PROCESS_ENV:
		process_protocol_helper_get_process_env (message, stream);
		break;
	default:
		process_protocol_helper_unknown_command (message, stream);
		break;
	}
}

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef DS_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_diagnostics_process_protocol;
const char quiet_linker_empty_file_warning_diagnostics_process_protocol = 0;
#endif
