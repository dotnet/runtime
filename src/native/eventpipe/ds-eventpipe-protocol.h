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
 *  CollectTracing5 introduces additional provider configuration fields.
 *  For backwards compatibility, these fields are optional and
 *  these flags indicate which of the optional fields should be
 *  deserialized from the IPC Stream.
 */
typedef enum
{
    EP_PROVIDER_OPTFIELD_NONE = 0,
    EP_PROVIDER_OPTFIELD_EVENT_FILTER = 1,
    EP_PROVIDER_OPTFIELD_TRACEPOINT_CONFIG = 2
} EventPipeProviderOptionalFieldFlags;

/*
* EventPipeCollectTracingCommandPayload
*
* https://github.com/dotnet/diagnostics/blob/main/documentation/design-docs/ipc-protocol.md
*/

// Command = 0x0202
// Command = 0x0203
// Command = 0x0204
// Command = 0x0205
// Command = 0x0206
#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_EVENTPIPE_PROTOCOL_GETTER_SETTER)
struct _EventPipeCollectTracingCommandPayload {
#else
struct _EventPipeCollectTracingCommandPayload_Internal {
#endif
	uint8_t *incoming_buffer;
	dn_vector_t *provider_configs;
	uint32_t circular_buffer_size_in_mb;
	EventPipeSerializationFormat serialization_format;
	bool rundown_requested;
	bool stackwalk_requested;
	uint64_t rundown_keyword;
	EventPipeSessionType session_type;
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
