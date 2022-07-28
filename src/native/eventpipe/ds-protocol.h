#ifndef __DIAGNOSTICS_PROTOCOL_H__
#define __DIAGNOSTICS_PROTOCOL_H__

#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ds-types.h"
#include "ds-ipc.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_PROTOCOL_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

typedef bool (ds_ipc_flatten_payload_func)(void *payload, uint8_t **buffer, uint16_t *buffer_len);
typedef uint8_t * (*ds_ipc_parse_payload_func)(uint8_t *buffer, uint16_t buffer_len);

/*
* DiagnosticsIpc
*/

uint8_t *
ds_ipc_advertise_cookie_v1_get (void);

void
ds_ipc_advertise_cookie_v1_init (void);

bool
ds_icp_advertise_v1_send (DiagnosticsIpcStream *stream);

/*
* DiagnosticsIpcHeader
*/

// The header to be associated with every command and response
// to/from the diagnostics server
#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_PROTOCOL_GETTER_SETTER)
struct _DiagnosticsIpcHeader {
#else
struct _DiagnosticsIpcHeader_Internal {
#endif
	// Magic Version number; a 0 terminated char array
	uint8_t magic [14];
	// The size of the incoming packet, size = header + payload size
	uint16_t size;
	// The scope of the Command.
	uint8_t commandset;
	// The command being sent
	uint8_t commandid;
	// reserved for future use
	uint16_t reserved;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_PROTOCOL_GETTER_SETTER)
struct _DiagnosticsIpcHeader {
	uint8_t _internal [sizeof (struct _DiagnosticsIpcHeader_Internal)];
};
#endif

DS_DEFINE_GETTER_ARRAY_REF(DiagnosticsIpcHeader *, ipc_header, uint8_t *, const uint8_t *, magic, magic[0])
DS_DEFINE_GETTER(DiagnosticsIpcHeader *, ipc_header, uint8_t, commandset)
DS_DEFINE_GETTER(DiagnosticsIpcHeader *, ipc_header, uint8_t, commandid)

/*
* DiagnosticsIpcMessage
*/

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_PROTOCOL_GETTER_SETTER)
struct _DiagnosticsIpcMessage {
#else
struct _DiagnosticsIpcMessage_Internal {
#endif
	// header associated with this message
	DiagnosticsIpcHeader header;
	// Pointer to flattened buffer filled with:
	// incoming message: payload (could be empty which would be NULL)
	// outgoing message: header + payload
	uint8_t *data;
	// The total size of the message (header + payload)
	uint16_t size;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_PROTOCOL_GETTER_SETTER)
struct _DiagnosticsIpcMessage {
	uint8_t _internal [sizeof (struct _DiagnosticsIpcMessage_Internal)];
};
#endif

DS_DEFINE_GETTER_REF(DiagnosticsIpcMessage *, ipc_message, DiagnosticsIpcHeader *, header)

DiagnosticsIpcMessage *
ds_ipc_message_init (DiagnosticsIpcMessage *message);

void
ds_ipc_message_fini (DiagnosticsIpcMessage *message);

// Initialize an incoming IpcMessage from a stream by parsing
// the header and payload.
//
// If either fail, this returns false, true otherwise
bool
ds_ipc_message_initialize_stream (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

bool
ds_ipc_message_try_parse_value (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint8_t *value,
	uint32_t value_len);

bool
ds_ipc_message_try_parse_uint64_t (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint64_t *value);

bool
ds_ipc_message_try_parse_uint32_t (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint32_t *value);

bool
ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint8_t **string_byte_array,
	uint32_t *string_byte_array_len);

bool
ds_ipc_message_try_parse_string_utf16_t (
	uint8_t **buffer,
	uint32_t *buffer_len,
	const ep_char16_t **value);

bool
ds_ipc_message_initialize_header_uint32_t_payload (
	DiagnosticsIpcMessage *message,
	const DiagnosticsIpcHeader *header,
	uint32_t payload);

static
inline
bool
ds_ipc_message_initialize_header_int32_t_payload (
	DiagnosticsIpcMessage *message,
	const DiagnosticsIpcHeader *header,
	int32_t payload)
{
	return ds_ipc_message_initialize_header_uint32_t_payload (message, header, (uint32_t)payload);
}

bool
ds_ipc_message_initialize_header_uint64_t_payload (
	DiagnosticsIpcMessage *message,
	const DiagnosticsIpcHeader *header,
	uint64_t payload);

bool
ds_ipc_message_initialize_buffer (
	DiagnosticsIpcMessage *message,
	const DiagnosticsIpcHeader *header,
	void *payload,
	uint16_t payload_len,
	ds_ipc_flatten_payload_func flatten_payload);

uint8_t *
ds_ipc_message_try_parse_payload (
	DiagnosticsIpcMessage *message,
	ds_ipc_parse_payload_func parse_func);

bool
ds_ipc_message_try_write_string_utf16_t (
	uint8_t **buffer,
	uint16_t *buffer_len,
	const ep_char16_t *value);

bool
ds_ipc_message_try_write_string_utf16_t_to_stream (
	DiagnosticsIpcStream *stream,
	const ep_char16_t *value);

bool
ds_ipc_message_send (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

// Send an Error message across the pipe.
// Will return false on failure of any step (init or send).
// Regardless of success of this function, the spec
// dictates that the connection be closed on error,
// so the user is expected to delete the IpcStream
// after handling error cases.
bool
ds_ipc_message_send_error (
	DiagnosticsIpcStream *stream,
	ds_ipc_result_t error);

bool
ds_ipc_message_send_success (
	DiagnosticsIpcStream *stream,
	ds_ipc_result_t code);

const DiagnosticsIpcHeader *
ds_ipc_header_get_generic_success (void);

const DiagnosticsIpcHeader *
ds_ipc_header_get_generic_error (void);

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_PROTOCOL_H__ */
