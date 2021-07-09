#ifndef __DIAGNOSTICS_PROFILER_PROTOCOL_H__
#define __DIAGNOSTICS_PROFILER_PROTOCOL_H__

#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ds-types.h"
#include "ds-ipc.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_PROFILER_PROTOCOL_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

/*
* DiagnosticsAttachProfilerCommandPayload
*/

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_PROFILER_PROTOCOL_GETTER_SETTER)
struct _DiagnosticsAttachProfilerCommandPayload {
#else
struct _DiagnosticsAttachProfilerCommandPayload_Internal {
#endif
	uint8_t * incoming_buffer;

	// The protocol buffer is defined as:
	//   uint - attach timeout
	//   CLSID - profiler GUID
	//   string - profiler path
	//   array<char> - client data
	// returns
	//   ulong - status

	uint32_t attach_timeout;
	uint8_t profiler_guid [EP_GUID_SIZE];
	const ep_char16_t *profiler_path;
	uint32_t client_data_len;
	uint8_t *client_data;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_PROFILER_PROTOCOL_GETTER_SETTER)
struct _DiagnosticsAttachProfilerCommandPayload {
	uint8_t _internal [sizeof (struct _DiagnosticsAttachProfilerCommandPayload_Internal)];
};
#endif

DS_DEFINE_GETTER(DiagnosticsAttachProfilerCommandPayload *, attach_profiler_command_payload, uint32_t, attach_timeout)
DS_DEFINE_GETTER_ARRAY_REF(DiagnosticsAttachProfilerCommandPayload *, attach_profiler_command_payload, uint8_t *, const uint8_t *, profiler_guid, profiler_guid [0])
DS_DEFINE_GETTER(DiagnosticsAttachProfilerCommandPayload *, attach_profiler_command_payload, const ep_char16_t *, profiler_path)
DS_DEFINE_GETTER(DiagnosticsAttachProfilerCommandPayload *, attach_profiler_command_payload, uint32_t, client_data_len)
DS_DEFINE_GETTER(DiagnosticsAttachProfilerCommandPayload *, attach_profiler_command_payload, uint8_t *, client_data)

DiagnosticsAttachProfilerCommandPayload *
ds_attach_profiler_command_payload_alloc (void);

void
ds_attach_profiler_command_payload_free (DiagnosticsAttachProfilerCommandPayload *payload);


#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_PROFILER_PROTOCOL_GETTER_SETTER)
struct _DiagnosticsStartupProfilerCommandPayload {
#else
struct _DiagnosticsStartupProfilerCommandPayload_Internal {
#endif
	uint8_t * incoming_buffer;

	// The protocol buffer is defined as:
	//   CLSID - profiler GUID
	//   string - profiler path
	// returns
	//   ulong - status

	uint8_t profiler_guid [EP_GUID_SIZE];
	const ep_char16_t *profiler_path;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_PROFILER_PROTOCOL_GETTER_SETTER)
struct _DiagnosticsStartupProfilerCommandPayload {
	uint8_t _internal [sizeof (struct _DiagnosticsStartupProfilerCommandPayload_Internal)];
};
#endif

DS_DEFINE_GETTER_ARRAY_REF(DiagnosticsStartupProfilerCommandPayload *, startup_profiler_command_payload, uint8_t *, const uint8_t *, profiler_guid, profiler_guid [0])
DS_DEFINE_GETTER(DiagnosticsStartupProfilerCommandPayload *, startup_profiler_command_payload, const ep_char16_t *, profiler_path)

DiagnosticsStartupProfilerCommandPayload *
ds_startup_profiler_command_payload_alloc (void);

void
ds_startup_profiler_command_payload_free (DiagnosticsAttachProfilerCommandPayload *payload);


/*
 * DiagnosticsProfilerProtocolHelper.
 */

bool
ds_profiler_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_PROFILER_PROTOCOL_H__ */
