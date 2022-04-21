#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_PROTOCOL_GETTER_SETTER
#include "ds-protocol.h"
#include "ds-server.h"
#include "ep.h"
#include "ep-stream.h"
#include "ep-event-source.h"

const DiagnosticsIpcHeader _ds_ipc_generic_success_header = {
	{ DOTNET_IPC_V1_MAGIC },
	(uint16_t)sizeof (DiagnosticsIpcHeader),
	(uint8_t)DS_SERVER_COMMANDSET_SERVER,
	(uint8_t)DS_SERVER_RESPONSEID_OK,
	(uint16_t)0x0000
};

const DiagnosticsIpcHeader _ds_ipc_generic_error_header = {
	{ DOTNET_IPC_V1_MAGIC },
	(uint16_t)sizeof (DiagnosticsIpcHeader),
	(uint8_t)DS_SERVER_COMMANDSET_SERVER,
	(uint8_t)DS_SERVER_RESPONSEID_ERROR,
	(uint16_t)0x0000
};

static uint8_t _ds_ipc_advertise_cooike_v1 [EP_GUID_SIZE] = { 0 };

/*
 * Forward declares of all static functions.
 */

static
bool
ipc_message_flatten_blitable_type (
	DiagnosticsIpcMessage *message,
	uint8_t *payload,
	size_t payload_len);

static
bool
ipc_message_try_parse (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
ipc_message_try_send_string_utf16_t (
	DiagnosticsIpcStream *stream,
	const ep_char16_t *value);

static
bool
ipc_message_flatten (
	DiagnosticsIpcMessage *message,
	void *payload,
	uint16_t payload_len,
	ds_ipc_flatten_payload_func flatten_payload);

static
bool
ipc_message_try_parse_string_utf16_t_byte_array (
	uint8_t **buffer,
	uint32_t *buffer_len,
	const uint8_t **string_byte_array,
	uint32_t *string_byte_array_len);

/*
* DiagnosticsIpc
*/

uint8_t *
ds_ipc_advertise_cookie_v1_get (void)
{
	return _ds_ipc_advertise_cooike_v1;
}

void
ds_ipc_advertise_cookie_v1_init (void)
{
	ep_rt_create_activity_id ((uint8_t *)&_ds_ipc_advertise_cooike_v1, EP_GUID_SIZE);
}

/**
* ==ADVERTISE PROTOCOL==
* Before standard IPC Protocol communication can occur on a client-mode connection
* the runtime must advertise itself over the connection.  ALL SUBSEQUENT COMMUNICATION 
* IS STANDARD DIAGNOSTICS IPC PROTOCOL COMMUNICATION.
* 
* See spec in: dotnet/diagnostics@documentation/design-docs/ipc-spec.md
* 
* The flow for Advertise is a one-way burst of 34 bytes consisting of
* 8 bytes  - "ADVR_V1\0" (ASCII chars + null byte)
* 16 bytes - random 128 bit number cookie (little-endian)
* 8 bytes  - PID (little-endian)
* 2 bytes  - unused 2 byte field for futureproofing
*/
bool
ds_icp_advertise_v1_send (DiagnosticsIpcStream *stream)
{
	uint8_t advertise_buffer [DOTNET_IPC_V1_ADVERTISE_SIZE];
	uint8_t *cookie = ds_ipc_advertise_cookie_v1_get ();
	uint64_t pid = DS_VAL64 (ep_rt_current_process_get_id ());
	uint64_t *buffer = (uint64_t *)advertise_buffer;
	bool result = false;

	ep_return_false_if_nok (stream != NULL);

	memcpy (buffer, DOTNET_IPC_V1_ADVERTISE_MAGIC, sizeof (uint64_t));
	buffer++;

	// fills buffer[1] and buffer[2]
	memcpy (buffer, cookie, EP_GUID_SIZE);
	buffer +=2;

	memcpy (buffer, &pid, sizeof (uint64_t));
	buffer++;

	// zero out unused filed
	memset (buffer, 0, sizeof (uint16_t));

	uint32_t bytes_written = 0;
	ep_raise_error_if_nok (ds_ipc_stream_write (stream, advertise_buffer, sizeof (advertise_buffer), &bytes_written, 100 /*ms*/));

	EP_ASSERT (bytes_written == sizeof (advertise_buffer));
	result = (bytes_written == sizeof (advertise_buffer));

ep_on_exit:
	return result;

ep_on_error:
	result = false;
	ep_exit_error_handler ();
}

/*
* DiagnosticsIpcMessage
*/

static
bool
ipc_message_try_send_string_utf16_t (
	DiagnosticsIpcStream *stream,
	const ep_char16_t *value)
{
	uint32_t string_len = (uint32_t)(ep_rt_utf16_string_len (value) + 1);
	uint32_t string_bytes = (uint32_t)(string_len * sizeof (ep_char16_t));
	uint32_t total_bytes = (uint32_t)(string_bytes + sizeof (uint32_t));

	uint32_t total_written = 0;
	uint32_t written = 0;

	bool result = ds_ipc_stream_write (stream, (const uint8_t *)&string_len, (uint32_t)sizeof (string_len), &written, EP_INFINITE_WAIT);
	total_written += written;

	if (result) {
		result &= ds_ipc_stream_write (stream, (const uint8_t *)value, string_bytes, &written, EP_INFINITE_WAIT);
		total_written += written;
	}

	EP_ASSERT (total_bytes == total_written);
	return result && (total_bytes == total_written);
}

static
bool
ipc_message_flatten_blitable_type (
	DiagnosticsIpcMessage *message,
	uint8_t *payload,
	size_t payload_len)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (payload != NULL);

	if (message->data != NULL)
		return true;

	bool result = false;
	uint8_t *buffer = NULL;
	uint8_t *buffer_cursor = NULL;

	EP_ASSERT (sizeof (message->header) + payload_len <= UINT16_MAX);
	message->size = (uint16_t)(sizeof (message->header) + payload_len);

	buffer = ep_rt_byte_array_alloc (message->size);
	ep_raise_error_if_nok (buffer != NULL);

	buffer_cursor = buffer;
	message->header.size = message->size;

	memcpy (buffer_cursor, &message->header, sizeof (message->header));
	buffer_cursor += sizeof (message->header);

	memcpy (buffer_cursor, payload, payload_len);

	EP_ASSERT (message->data == NULL);
	message->data = buffer;

	buffer = NULL;
	result = true;

ep_on_exit:
	return result;

ep_on_error:
	ep_rt_byte_array_free (buffer);
	result = false;
	ep_exit_error_handler ();
}

// Attempt to populate header and payload from a buffer.
// Payload is left opaque as a flattened buffer in m_pData
static
bool
ipc_message_try_parse (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	uint8_t *buffer = NULL;
	bool result = false;

	// Read out header first
	uint32_t bytes_read;
	result = ds_ipc_stream_read (stream, (uint8_t *)&message->header, sizeof (message->header), &bytes_read, EP_INFINITE_WAIT);
	if (!result || (bytes_read < sizeof (message->header)))
		ep_raise_error ();

	if (message->header.size < sizeof (message->header))
		ep_raise_error ();

	message->size = message->header.size;

	// Then read out payload to buffer.
	uint16_t payload_len;
	payload_len = message->header.size - sizeof (message->header);
	if (payload_len != 0) {
		buffer = ep_rt_byte_array_alloc (payload_len);
		ep_raise_error_if_nok (buffer != NULL);

		result = ds_ipc_stream_read (stream, buffer, payload_len, &bytes_read, EP_INFINITE_WAIT);
		if (!result || (bytes_read < payload_len))
			ep_raise_error ();

		message->data = buffer;
		buffer = NULL;
	}

ep_on_exit:
	return result;

ep_on_error:
	ep_rt_byte_array_free (buffer);
	result = false;
	ep_exit_error_handler ();
}

static
bool
ipc_message_flatten (
	DiagnosticsIpcMessage *message,
	void *payload,
	uint16_t payload_len,
	ds_ipc_flatten_payload_func flatten_payload)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (payload != NULL);

	if (message->data)
		return true;

	bool result = true;
	uint8_t *buffer = NULL;

	uint16_t total_len = 0;
	EP_ASSERT (UINT16_MAX >= sizeof (DiagnosticsIpcHeader) + payload_len);
	total_len += sizeof (DiagnosticsIpcHeader) + payload_len;

	uint16_t remaining_len = total_len;
	message->size = total_len;

	buffer = ep_rt_byte_array_alloc (message->size);
	ep_raise_error_if_nok (buffer != NULL);

	uint8_t * buffer_cursor;
	buffer_cursor = buffer;
	message->header.size = message->size;

	memcpy (buffer_cursor, &message->header, sizeof (DiagnosticsIpcHeader));
	buffer_cursor += sizeof (DiagnosticsIpcHeader);
	remaining_len -= sizeof (DiagnosticsIpcHeader);

	if (flatten_payload)
		result = flatten_payload (payload, &buffer_cursor, &remaining_len);
	else
		memcpy (buffer_cursor, payload, payload_len);

	EP_ASSERT (message->data == NULL);

	//Transfer ownership.
	message->data = buffer;
	buffer = NULL;

ep_on_exit:
	ep_rt_byte_array_free (buffer);
	return result;

ep_on_error:
	result = false;
	ep_exit_error_handler ();
}

static
bool
ipc_message_try_parse_string_utf16_t_byte_array (
	uint8_t **buffer,
	uint32_t *buffer_len,
	const uint8_t **string_byte_array,
	uint32_t *string_byte_array_len)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (string_byte_array != NULL);
	EP_ASSERT (string_byte_array_len != NULL);

	bool result = false;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, string_byte_array_len));
	*string_byte_array_len *= sizeof (ep_char16_t);

	if (*string_byte_array_len != 0) {
		if (*string_byte_array_len > *buffer_len)
			ep_raise_error ();

		if (((const ep_char16_t *)*buffer) [(*string_byte_array_len / sizeof (ep_char16_t)) - 1] != 0)
			ep_raise_error ();

		*string_byte_array = *buffer;

	} else {
		*string_byte_array = NULL;
	}

	*buffer = *buffer + *string_byte_array_len;
	*buffer_len = *buffer_len - *string_byte_array_len;

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ep_exit_error_handler ();
}

DiagnosticsIpcMessage *
ds_ipc_message_init (DiagnosticsIpcMessage *message)
{
	ep_return_null_if_nok (message != NULL);

	message->data = NULL;
	message->size = 0;
	memset (&message->header, 0 , sizeof (message->header));

	return message;
}

void
ds_ipc_message_fini (DiagnosticsIpcMessage *message)
{
	ep_return_void_if_nok (message != NULL);
	ep_rt_byte_array_free (message->data);
}

bool
ds_ipc_message_initialize_stream (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	return ipc_message_try_parse (message, stream);
}

bool
ds_ipc_message_try_parse_value (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint8_t *value,
	uint32_t value_len)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (value != NULL);
	EP_ASSERT ((buffer_len - value_len) <= buffer_len);

	memcpy (value, *buffer, value_len);
	*buffer = *buffer + value_len;
	*buffer_len = *buffer_len - value_len;
	return true;
}

bool
ds_ipc_message_try_parse_uint64_t (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint64_t *value)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (value != NULL);

	bool result = ds_ipc_message_try_parse_value (buffer, buffer_len, (uint8_t *)value, (uint32_t)sizeof (uint64_t));
	if (result)
		*value = DS_VAL64 (*value);
	return result;
}

bool
ds_ipc_message_try_parse_uint32_t (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint32_t *value)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (value != NULL);

	bool result = ds_ipc_message_try_parse_value (buffer, buffer_len, (uint8_t*)value, (uint32_t)sizeof (uint32_t));
	if (result)
		*value = DS_VAL32 (*value);
	return result;
}

bool
ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint8_t **string_byte_array,
	uint32_t *string_byte_array_len)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (string_byte_array != NULL);
	EP_ASSERT (string_byte_array_len != NULL);

	bool result = false;

	const uint8_t *temp_buffer = NULL;
	uint32_t temp_buffer_len = 0;

	ep_raise_error_if_nok (ipc_message_try_parse_string_utf16_t_byte_array (buffer, buffer_len, (const uint8_t **)&temp_buffer, &temp_buffer_len));

	if (temp_buffer_len != 0) {
		*string_byte_array = ep_rt_byte_array_alloc (temp_buffer_len);
		ep_raise_error_if_nok (*string_byte_array != NULL);

		memcpy (*string_byte_array, temp_buffer, temp_buffer_len);
	} else {
		*string_byte_array = NULL;
	}

	*string_byte_array_len = temp_buffer_len;
	result = true;

ep_on_exit:
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ep_exit_error_handler ();
}

bool
ds_ipc_message_try_parse_string_utf16_t (
	uint8_t **buffer,
	uint32_t *buffer_len,
	const ep_char16_t **value)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (value != NULL);
	EP_ASSERT (!(((size_t)*buffer) & 0x1));

	uint32_t string_byte_array_len = 0;
	return ipc_message_try_parse_string_utf16_t_byte_array (buffer, buffer_len, (const uint8_t **)value, &string_byte_array_len);
}

bool
ds_ipc_message_initialize_header_uint32_t_payload (
	DiagnosticsIpcMessage *message,
	const DiagnosticsIpcHeader *header,
	uint32_t payload)
{
	EP_ASSERT (message);
	EP_ASSERT (header);

	message->header = *header;
	return ipc_message_flatten_blitable_type (message, (uint8_t *)&payload, sizeof (payload));
}

bool
ds_ipc_message_initialize_header_uint64_t_payload (
	DiagnosticsIpcMessage *message,
	const DiagnosticsIpcHeader *header,
	uint64_t payload)
{
	EP_ASSERT (message);
	EP_ASSERT (header);

	message->header = *header;
	return ipc_message_flatten_blitable_type (message, (uint8_t *)&payload, sizeof (payload));
}

bool
ds_ipc_message_initialize_buffer (
	DiagnosticsIpcMessage *message,
	const DiagnosticsIpcHeader *header,
	void *payload,
	uint16_t payload_len,
	ds_ipc_flatten_payload_func flatten_payload)
{
	message->header = *header;
	return ipc_message_flatten (message, payload, payload_len, flatten_payload);
}

uint8_t *
ds_ipc_message_try_parse_payload (
	DiagnosticsIpcMessage *message,
	ds_ipc_parse_payload_func parse_func)
{
	ep_return_null_if_nok (message != NULL);

	EP_ASSERT (message->data);

	uint8_t *payload = NULL;

	if (parse_func)
		payload = parse_func (message->data, message->size - sizeof (message->header));
	else
		payload = message->data;

	message->data = NULL; // user is expected to clean up buffer when finished with it
	return payload;
}

bool
ds_ipc_message_try_write_string_utf16_t (
	uint8_t **buffer,
	uint16_t *buffer_len,
	const ep_char16_t *value)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (*buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (value != NULL);

	bool result = true;
	uint32_t string_len = (uint32_t)(ep_rt_utf16_string_len (value) + 1);
	size_t total_bytes = (string_len * sizeof (ep_char16_t)) + sizeof(uint32_t);

	EP_ASSERT (total_bytes <= UINT16_MAX);
	EP_ASSERT (*buffer_len >= (uint16_t)total_bytes);
	if (*buffer_len < (uint16_t)total_bytes || total_bytes > UINT16_MAX)
		ep_raise_error ();

	memcpy (*buffer, &string_len, sizeof (string_len));
	*buffer += sizeof (string_len);

	memcpy (*buffer, value, string_len * sizeof (ep_char16_t));
	*buffer += (string_len * sizeof (ep_char16_t));

	*buffer_len -= (uint16_t)total_bytes;

ep_on_exit:
	return result;

ep_on_error:
	result = false;
	ep_exit_error_handler ();
}

bool
ds_ipc_message_try_write_string_utf16_t_to_stream (
	DiagnosticsIpcStream *stream,
	const ep_char16_t *value)
{
	EP_ASSERT (stream != NULL);
	EP_ASSERT (value != NULL);

	bool result = true;
	uint32_t bytes_written = 0;
	uint32_t string_len = (uint32_t)(ep_rt_utf16_string_len (value) + 1);
	size_t total_bytes = (string_len * sizeof (ep_char16_t)) + sizeof(uint32_t);

	EP_ASSERT (total_bytes <= UINT16_MAX);

	result &= ds_ipc_stream_write (stream, (const uint8_t *)&string_len, sizeof (string_len), &bytes_written, EP_INFINITE_WAIT);
	total_bytes -= bytes_written;
	if (result) {
		result &= ds_ipc_stream_write (stream, (const uint8_t *)value, string_len * sizeof (ep_char16_t), &bytes_written, EP_INFINITE_WAIT);
		total_bytes -= bytes_written;
	}

	EP_ASSERT (total_bytes == 0);
	return result && (total_bytes == 0);
}

bool
ds_ipc_message_send (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (message->data != NULL);
	EP_ASSERT (stream != NULL);

	uint32_t bytes_written;
	bool result = ds_ipc_stream_write (stream, message->data, message->size, &bytes_written, EP_INFINITE_WAIT);
	return (bytes_written == message->size) && result;
}

bool
ds_ipc_message_send_error (
	DiagnosticsIpcStream *stream,
	ds_ipc_result_t error)
{
	ep_return_false_if_nok (stream != NULL);

	DiagnosticsIpcMessage error_message;
	ds_ipc_message_init (&error_message);
	bool result = ds_ipc_message_initialize_header_int32_t_payload (&error_message, ds_ipc_header_get_generic_error (), (int32_t)error);
	if (result)
		ds_ipc_message_send (&error_message, stream);
	ds_ipc_message_fini (&error_message);
	return result;
}

bool
ds_ipc_message_send_success (
	DiagnosticsIpcStream *stream,
	ds_ipc_result_t code)
{
	ep_return_false_if_nok (stream != NULL);

	DiagnosticsIpcMessage success_message;
	ds_ipc_message_init (&success_message);
	bool result = ds_ipc_message_initialize_header_int32_t_payload (&success_message, ds_ipc_header_get_generic_success (), (int32_t)code);
	if (result)
		ds_ipc_message_send (&success_message, stream);
	ds_ipc_message_fini (&success_message);
	return result;
}

const DiagnosticsIpcHeader *
ds_ipc_header_get_generic_success (void)
{
	return &_ds_ipc_generic_success_header;
}

const DiagnosticsIpcHeader *
ds_ipc_header_get_generic_error (void)
{
	return &_ds_ipc_generic_error_header;
}


#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(DS_INCLUDE_SOURCE_FILES) && !defined(DS_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_diagnostics_protocol;
const char quiet_linker_empty_file_warning_diagnostics_protocol = 0;
#endif
