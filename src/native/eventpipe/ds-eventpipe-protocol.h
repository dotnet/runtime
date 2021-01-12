#ifndef __DIAGNOSTICS_EVENTPIPE_PROTOCOL_H__
#define __DIAGNOSTICS_EVENTPIPE_PROTOCOL_H__

#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ds-types.h"
#include "ds-ipc.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_EVENTPIPE_PROTOCOL_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

/*
* EventPipeCollectTracingCommandPayload
*/

// Command = 0x0202
#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_EVENTPIPE_PROTOCOL_GETTER_SETTER)
struct _EventPipeCollectTracingCommandPayload {
#else
struct _EventPipeCollectTracingCommandPayload_Internal {
#endif
	// The protocol buffer is defined as:
	// X, Y, Z means encode bytes for X followed by bytes for Y followed by bytes for Z
	// message = uint circularBufferMB, uint format, array<provider_config> providers
	// uint = 4 little endian bytes
	// wchar = 2 little endian bytes, UTF16 encoding
	// array<T> = uint length, length # of Ts
	// string = (array<char> where the last char must = 0) or (length = 0)
	// provider_config = ulong keywords, uint logLevel, string provider_name, string filter_data

	uint8_t *incoming_buffer;
	ep_rt_provider_config_array_t provider_configs;
	uint32_t circular_buffer_size_in_mb;
	EventPipeSerializationFormat serialization_format;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_EVENTPIPE_PROTOCOL_GETTER_SETTER)
struct _EventPipeCollectTracingCommandPayload {
	uint8_t _internal [sizeof (struct _EventPipeCollectTracingCommandPayload_Internal)];
};
#endif

EventPipeCollectTracingCommandPayload *
ds_eventpipe_collect_tracing_command_payload_alloc (void);

void
ds_eventpipe_collect_tracing_command_payload_free (EventPipeCollectTracingCommandPayload *payload);

/*
* EventPipeCollectTracing2CommandPayload
*/

// Command = 0x0202
#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_EVENTPIPE_PROTOCOL_GETTER_SETTER)
struct _EventPipeCollectTracing2CommandPayload {
#else
struct _EventPipeCollectTracing2CommandPayload_Internal {
#endif
	// The protocol buffer is defined as:
	// X, Y, Z means encode bytes for X followed by bytes for Y followed by bytes for Z
	// message = uint circularBufferMB, uint format, array<provider_config> providers
	// uint = 4 little endian bytes
	// wchar = 2 little endian bytes, UTF16 encoding
	// array<T> = uint length, length # of Ts
	// string = (array<char> where the last char must = 0) or (length = 0)
	// provider_config = ulong keywords, uint logLevel, string provider_name, string filter_data

	uint8_t *incoming_buffer;
	ep_rt_provider_config_array_t provider_configs;
	uint32_t circular_buffer_size_in_mb;
	EventPipeSerializationFormat serialization_format;
	bool rundown_requested;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_EVENTPIPE_PROTOCOL_GETTER_SETTER)
struct _EventPipeCollectTracing2CommandPayload {
	uint8_t _internal [sizeof (struct _EventPipeCollectTracing2CommandPayload_Internal)];
};
#endif

EventPipeCollectTracing2CommandPayload *
ds_eventpipe_collect_tracing2_command_payload_alloc (void);

void
ds_eventpipe_collect_tracing2_command_payload_free (EventPipeCollectTracing2CommandPayload *payload);

/*
* EventPipeStopTracingCommandPayload
*/

// Command = 0x0201
#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_EVENTPIPE_PROTOCOL_GETTER_SETTER)
struct _EventPipeStopTracingCommandPayload {
#else
struct _EventPipeStopTracingCommandPayload_Internal {
#endif
	EventPipeSessionID session_id;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_EVENTPIPE_PROTOCOL_GETTER_SETTER)
struct _EventPipeStopTracingCommandPayload {
	uint8_t _internal [sizeof (struct _EventPipeStopTracingCommandPayload_Internal)];
};
#endif

void
ds_eventpipe_stop_tracing_command_payload_free (EventPipeStopTracingCommandPayload *payload);

/*
* EventPipeProtocolHelper
*/

bool
ds_eventpipe_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_EVENTPIPE_PROTOCOL_H__ */
