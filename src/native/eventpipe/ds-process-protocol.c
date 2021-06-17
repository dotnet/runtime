#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
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
uint16_t
env_info_payload_get_size (DiagnosticsEnvironmentInfoPayload *payload);

static
uint32_t
env_info_env_block_get_size (DiagnosticsEnvironmentInfoPayload *payload);

static
bool
env_info_payload_flatten (
	void *payload,
	uint8_t **buffer,
	uint16_t *size);

static
bool
env_info_stream_env_block (
	DiagnosticsEnvironmentInfoPayload *payload,
	DiagnosticsIpcStream *stream);

static
bool
process_protocol_helper_get_process_info (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
process_protocol_helper_get_process_info_2 (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
process_protocol_helper_get_process_env (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
process_protocol_helper_resume_runtime_startup (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
process_protocol_helper_set_environment_variable (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
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
		memcpy (&payload->runtime_cookie, runtime_cookie, EP_GUID_SIZE);

	return payload;
}

void
ds_process_info_payload_fini (DiagnosticsProcessInfoPayload *payload)
{
	;
}

/*
 * DiagnosticsProcessInfo2Payload.
 */

static
uint16_t
process_info_2_payload_get_size (DiagnosticsProcessInfo2Payload *payload)
{
	// see IPC spec @ https://github.com/dotnet/diagnostics/blob/master/documentation/design-docs/ipc-protocol.md
	// for definition of serialization format

	// uint64_t ProcessId;  -> 8 bytes
	// GUID RuntimeCookie;  -> 16 bytes
	// LPCWSTR CommandLine; -> 4 bytes + strlen * sizeof(WCHAR)
	// LPCWSTR OS;          -> 4 bytes + strlen * sizeof(WCHAR)
	// LPCWSTR Arch;        -> 4 bytes + strlen * sizeof(WCHAR)
	// LPCWSTR managed_entrypoint_assembly_name;	-> 4 bytes + strlen * sizeof(WCHAR)
	// LPCWSTR clr_product_version; 				-> 4 bytes + strlen * sizeof(WCHAR)

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

	size += sizeof(uint32_t);
	size += (payload->managed_entrypoint_assembly_name != NULL) ?
		(ep_rt_utf16_string_len (payload->managed_entrypoint_assembly_name) + 1) * sizeof(ep_char16_t) : 0;

	size += sizeof(uint32_t);
	size += (payload->clr_product_version != NULL) ?
		(ep_rt_utf16_string_len (payload->clr_product_version) + 1) * sizeof(ep_char16_t) : 0;

	EP_ASSERT (size <= UINT16_MAX);
	return (uint16_t)size;
}

static
bool
process_info_2_payload_flatten (
	void *payload,
	uint8_t **buffer,
	uint16_t *size)
{
	DiagnosticsProcessInfo2Payload *process_info = (DiagnosticsProcessInfo2Payload*)payload;

	EP_ASSERT (payload != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (*buffer != NULL);
	EP_ASSERT (size != NULL);
	EP_ASSERT (process_info_2_payload_get_size (process_info) == *size);

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

	// LPCWSTR managed_entrypoint_assembly_name;
	if (success)
		success &= ds_ipc_message_try_write_string_utf16_t (buffer, size, process_info->managed_entrypoint_assembly_name);

	// LPCWSTR clr_product_version;
	if (success)
		success &= ds_ipc_message_try_write_string_utf16_t (buffer, size, process_info->clr_product_version);

	// Assert we've used the whole buffer we were given
	EP_ASSERT(*size == 0);

	return success;
}

DiagnosticsProcessInfo2Payload *
ds_process_info_2_payload_init (
	DiagnosticsProcessInfo2Payload *payload,
	const ep_char16_t *command_line,
	const ep_char16_t *os,
	const ep_char16_t *arch,
	uint32_t process_id,
	const uint8_t *runtime_cookie,
	const ep_char16_t *managed_entrypoint_assembly_name,
	const ep_char16_t *clr_product_version)
{
	ep_return_null_if_nok (payload != NULL);

	payload->command_line = command_line;
	payload->os = os;
	payload->arch = arch;
	payload->process_id = process_id;
	payload->managed_entrypoint_assembly_name = managed_entrypoint_assembly_name;
	payload->clr_product_version = clr_product_version;

	if (runtime_cookie)
		memcpy (&payload->runtime_cookie, runtime_cookie, EP_GUID_SIZE);

	return payload;
}

void
ds_process_info_2_payload_fini (DiagnosticsProcessInfo2Payload *payload)
{
	;
}


/*
 * DiagnosticsEnvironmentInfoPayload.
 */

static
uint16_t
env_info_payload_get_size (DiagnosticsEnvironmentInfoPayload *payload)
{
	EP_ASSERT (payload != NULL);

	size_t size = 0;
	size += sizeof (payload->incoming_bytes);
	size += sizeof (payload->future);

	EP_ASSERT (size <= UINT16_MAX);
	return (uint16_t)size;
}

static
uint32_t
env_info_env_block_get_size (DiagnosticsEnvironmentInfoPayload *payload)
{
	EP_ASSERT (payload != NULL);

	size_t size = 0;

	size += sizeof (uint32_t);
	size += (sizeof (uint32_t) * ep_rt_env_array_utf16_size (&payload->env_array));

	ep_rt_env_array_utf16_iterator_t iterator = ep_rt_env_array_utf16_iterator_begin (&payload->env_array);
	while (!ep_rt_env_array_utf16_iterator_end (&payload->env_array, &iterator)) {
		size += ((ep_rt_utf16_string_len (ep_rt_env_array_utf16_iterator_value (&iterator)) + 1) * sizeof (ep_char16_t));
		ep_rt_env_array_utf16_iterator_next (&iterator);
	}

	EP_ASSERT (size <= UINT32_MAX);
	return (uint32_t)size;
}

static
bool
env_info_payload_flatten (
	void *payload,
	uint8_t **buffer,
	uint16_t *size)
{
	DiagnosticsEnvironmentInfoPayload *env_info = (DiagnosticsEnvironmentInfoPayload*)payload;

	EP_ASSERT (payload != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (*buffer != NULL);
	EP_ASSERT (size != NULL);
	EP_ASSERT (env_info_payload_get_size (env_info) == *size);

	// see IPC spec @ https://github.com/dotnet/diagnostics/blob/master/documentation/design-docs/ipc-protocol.md
	// for definition of serialization format

	bool success = true;

	// uint32_t incoming_bytes;
	memcpy (*buffer, &env_info->incoming_bytes, sizeof (env_info->incoming_bytes));
	*buffer += sizeof (env_info->incoming_bytes);
	*size -= sizeof (env_info->incoming_bytes);

	// uint16_t future;
	memcpy(*buffer, &env_info->future, sizeof (env_info->future));
	*buffer += sizeof (env_info->future);
	*size -= sizeof (env_info->future);

	// Assert we've used the whole buffer we were given
	EP_ASSERT(*size == 0);

	return success;
}

static
bool
env_info_stream_env_block (
	DiagnosticsEnvironmentInfoPayload *payload,
	DiagnosticsIpcStream *stream)
{
	DiagnosticsEnvironmentInfoPayload *env_info = (DiagnosticsEnvironmentInfoPayload*)payload;

	EP_ASSERT (payload != NULL);
	EP_ASSERT (stream != NULL);

	// see IPC spec @ https://github.com/dotnet/diagnostics/blob/master/documentation/design-docs/ipc-protocol.md
	// for definition of serialization format

	bool success = true;
	uint32_t bytes_written = 0;

	// Array<Array<WCHAR>>
	uint32_t env_len = (uint32_t)ep_rt_env_array_utf16_size (&env_info->env_array);
	success &= ds_ipc_stream_write (stream, (const uint8_t *)&env_len, sizeof (env_len), &bytes_written, EP_INFINITE_WAIT);

	ep_rt_env_array_utf16_iterator_t iterator = ep_rt_env_array_utf16_iterator_begin (&env_info->env_array);
	while (!ep_rt_env_array_utf16_iterator_end (&env_info->env_array, &iterator)) {
		success &= ds_ipc_message_try_write_string_utf16_t_to_stream (stream, ep_rt_env_array_utf16_iterator_value (&iterator));
		ep_rt_env_array_utf16_iterator_next (&iterator);
	}

	return success;
}

DiagnosticsEnvironmentInfoPayload *
ds_env_info_payload_init (DiagnosticsEnvironmentInfoPayload *payload)
{
	ep_return_null_if_nok (payload != NULL);

	ep_rt_env_array_utf16_alloc (&payload->env_array);
	ep_rt_os_environment_get_utf16 (&payload->env_array);

	payload->incoming_bytes = env_info_env_block_get_size (payload);
	payload->future = 0;

	return payload;
}

void
ds_env_info_payload_fini (DiagnosticsEnvironmentInfoPayload *payload)
{
	ep_rt_env_array_utf16_iterator_t iterator = ep_rt_env_array_utf16_iterator_begin (&payload->env_array);
	while (!ep_rt_env_array_utf16_iterator_end (&payload->env_array, &iterator)) {
		ep_rt_utf16_string_free (ep_rt_env_array_utf16_iterator_value (&iterator));
		ep_rt_env_array_utf16_iterator_next (&iterator);
	}

	ep_rt_env_array_utf16_free (&payload->env_array);
}

/*
 * DiagnosticsProcessProtocolHelper.
 */

static
bool
process_protocol_helper_get_process_info (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	bool result = false;
	ep_char16_t *command_line = NULL;
	ep_char16_t *os_info = NULL;
	ep_char16_t *arch_info = NULL;
	DiagnosticsProcessInfoPayload payload;
	DiagnosticsProcessInfoPayload *process_info_payload = NULL;

	command_line = ep_rt_utf8_to_utf16_string (ep_rt_diagnostics_command_line_get (), -1);
	ep_raise_error_if_nok (command_line != NULL);

	os_info = ep_rt_utf8_to_utf16_string (ep_event_source_get_os_info (), -1);
	ep_raise_error_if_nok (os_info != NULL);

	arch_info = ep_rt_utf8_to_utf16_string (ep_event_source_get_arch_info (), -1);
	ep_raise_error_if_nok (arch_info != NULL);

	process_info_payload = ds_process_info_payload_init (
		&payload,
		command_line,
		os_info,
		arch_info,
		ep_rt_current_process_get_id (),
		ds_ipc_advertise_cookie_v1_get ());
	ep_raise_error_if_nok (process_info_payload != NULL);

	ep_raise_error_if_nok (ds_ipc_message_initialize_buffer (
		message,
		ds_ipc_header_get_generic_success (),
		(void *)process_info_payload,
		process_info_payload_get_size (process_info_payload),
		process_info_payload_flatten));

	ep_raise_error_if_nok (ds_ipc_message_send (message, stream));

	result = true;

ep_on_exit:
	ds_process_info_payload_fini (process_info_payload);
	ep_rt_utf16_string_free (arch_info);
	ep_rt_utf16_string_free (os_info);
	ep_rt_utf16_string_free (command_line);
	ds_ipc_stream_free (stream);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ds_ipc_message_send_error (stream, DS_IPC_E_FAIL);
	DS_LOG_WARNING_0 ("Failed to send DiagnosticsIPC response");
	ep_exit_error_handler ();
}

static
bool
process_protocol_helper_get_process_info_2 (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	bool result = false;
	ep_char16_t *command_line = NULL;
	ep_char16_t *os_info = NULL;
	ep_char16_t *arch_info = NULL;
	ep_char16_t *managed_entrypoint_assembly_name = NULL;
	ep_char16_t *clr_product_version = NULL;
	DiagnosticsProcessInfo2Payload payload;
	DiagnosticsProcessInfo2Payload *process_info_2_payload = NULL;

	command_line = ep_rt_utf8_to_utf16_string (ep_rt_diagnostics_command_line_get (), -1);
	ep_raise_error_if_nok (command_line != NULL);

	os_info = ep_rt_utf8_to_utf16_string (ep_event_source_get_os_info (), -1);
	ep_raise_error_if_nok (os_info != NULL);

	arch_info = ep_rt_utf8_to_utf16_string (ep_event_source_get_arch_info (), -1);
	ep_raise_error_if_nok (arch_info != NULL);

	managed_entrypoint_assembly_name = ep_rt_utf8_to_utf16_string (ep_rt_entrypoint_assembly_name_get_utf8 (), -1);
	ep_raise_error_if_nok (managed_entrypoint_assembly_name != NULL);

	clr_product_version = ep_rt_utf8_to_utf16_string (ep_rt_runtime_version_get_utf8 (), -1);
	ep_raise_error_if_nok (clr_product_version != NULL);

	process_info_2_payload = ds_process_info_2_payload_init (
		&payload,
		command_line,
		os_info,
		arch_info,
		ep_rt_current_process_get_id (),
		ds_ipc_advertise_cookie_v1_get (),
		managed_entrypoint_assembly_name,
		clr_product_version);
	ep_raise_error_if_nok (process_info_2_payload != NULL);

	ep_raise_error_if_nok (ds_ipc_message_initialize_buffer (
		message,
		ds_ipc_header_get_generic_success (),
		(void *)process_info_2_payload,
		process_info_2_payload_get_size (process_info_2_payload),
		process_info_2_payload_flatten));

	ep_raise_error_if_nok (ds_ipc_message_send (message, stream));

	result = true;

ep_on_exit:
	ds_process_info_2_payload_fini (process_info_2_payload);
	ep_rt_utf16_string_free (arch_info);
	ep_rt_utf16_string_free (os_info);
	ep_rt_utf16_string_free (command_line);
	ep_rt_utf16_string_free (managed_entrypoint_assembly_name);
	ep_rt_utf16_string_free (clr_product_version);
	ds_ipc_stream_free (stream);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ds_ipc_message_send_error (stream, DS_IPC_E_FAIL);
	DS_LOG_WARNING_0 ("Failed to send DiagnosticsIPC response");
	ep_exit_error_handler ();
}

static
bool
process_protocol_helper_get_process_env (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	bool result = false;
	DiagnosticsEnvironmentInfoPayload payload;
	DiagnosticsEnvironmentInfoPayload *env_info_payload;

	env_info_payload = ds_env_info_payload_init (&payload);
	ep_raise_error_if_nok (env_info_payload);

	ep_raise_error_if_nok (ds_ipc_message_initialize_buffer (
		message,
		ds_ipc_header_get_generic_success (),
		(void *)env_info_payload,
		env_info_payload_get_size (env_info_payload),
		env_info_payload_flatten));

	ep_raise_error_if_nok (ds_ipc_message_send (message, stream));
	ep_raise_error_if_nok (env_info_stream_env_block (env_info_payload, stream));

	result = true;

ep_on_exit:
	ds_env_info_payload_fini (env_info_payload);
	ds_ipc_stream_free (stream);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ds_ipc_message_send_error (stream, DS_IPC_E_FAIL);
	DS_LOG_WARNING_0 ("Failed to send DiagnosticsIPC response");
	ep_exit_error_handler ();
}

static
bool
process_protocol_helper_resume_runtime_startup (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	bool result = false;

	// no payload
	ds_server_resume_runtime_startup ();
	result = ds_ipc_message_send_success (stream, DS_IPC_S_OK);
	if (!result) {
		ds_ipc_message_send_error (stream, DS_IPC_E_FAIL);
		DS_LOG_WARNING_0 ("Failed to send DiagnosticsIPC response");
	}

	ds_ipc_stream_free (stream);
	return result;
}

DiagnosticsSetEnvironmentVariablePayload *
ds_set_environment_variable_payload_alloc (void)
{
	return ep_rt_object_alloc (DiagnosticsSetEnvironmentVariablePayload);
}

void
ds_set_environment_variable_payload_free (DiagnosticsSetEnvironmentVariablePayload *payload)
{
	ep_return_void_if_nok (payload != NULL);
	ep_rt_byte_array_free (payload->incoming_buffer);
	ep_rt_object_free (payload);
}

static
uint8_t *
set_environment_variable_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	DiagnosticsSetEnvironmentVariablePayload *instance = ds_set_environment_variable_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!ds_ipc_message_try_parse_string_utf16_t (&buffer_cursor, &buffer_cursor_len, &instance->name) ||
		!ds_ipc_message_try_parse_string_utf16_t (&buffer_cursor, &buffer_cursor_len, &instance->value))
		ep_raise_error ();

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_set_environment_variable_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

static
bool
process_protocol_helper_set_environment_variable (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	if (!stream)
		return false;

    bool result = false;
    DiagnosticsSetEnvironmentVariablePayload *payload = (DiagnosticsSetEnvironmentVariablePayload *)ds_ipc_message_try_parse_payload (message, set_environment_variable_command_try_parse_payload);
	if (!payload) {
		ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
		ep_raise_error ();
	}

	ds_ipc_result_t ipc_result;
	ipc_result = ds_rt_set_environment_variable (payload->name, payload->value);
	if (ipc_result != DS_IPC_S_OK) {
		ds_ipc_message_send_error (stream, ipc_result);
		ep_raise_error ();
	} else {
		ds_ipc_message_send_success (stream, ipc_result);
	}

	result = true;

ep_on_exit:
	ds_set_environment_variable_payload_free (payload);
    ds_ipc_stream_free (stream);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ep_exit_error_handler ();
}

static
bool
process_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	DS_LOG_WARNING_1 ("Received unknown request type (%d)", ds_ipc_header_get_commandset (ds_ipc_message_get_header_ref (message)));
	ds_ipc_message_send_error (stream, DS_IPC_E_UNKNOWN_COMMAND);
	ds_ipc_stream_free (stream);
	return true;
}

bool
ds_process_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	bool result = false;

	switch ((DiagnosticsProcessCommandId)ds_ipc_header_get_commandid (ds_ipc_message_get_header_ref (message))) {
	case DS_PROCESS_COMMANDID_GET_PROCESS_INFO:
		result = process_protocol_helper_get_process_info (message, stream);
		break;
	case DS_PROCESS_COMMANDID_RESUME_RUNTIME:
		result = process_protocol_helper_resume_runtime_startup (message, stream);
		break;
	case DS_PROCESS_COMMANDID_GET_PROCESS_ENV:
		result = process_protocol_helper_get_process_env (message, stream);
		break;
	case DS_PROCESS_COMMANDID_SET_ENV_VAR:
		result = process_protocol_helper_set_environment_variable (message, stream);
        break;
	case DS_PROCESS_COMMANDID_GET_PROCESS_INFO_2:
		result = process_protocol_helper_get_process_info_2 (message, stream);
		break;
	default:
		result = process_protocol_helper_unknown_command (message, stream);
		break;
	}

	return result;
}

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef DS_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_diagnostics_process_protocol;
const char quiet_linker_empty_file_warning_diagnostics_process_protocol = 0;
#endif
