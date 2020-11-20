#ifndef __DIAGNOSTICS_DUMP_PROTOCOL_H__
#define __DIAGNOSTICS_DUMP_PROTOCOL_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ds-rt-config.h"
#include "ds-types.h"
#include "ds-ipc.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_DUMP_PROTOCOL_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

/*
* DiagnosticsGenerateCoreDumpCommandPayload
*/

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_DUMP_PROTOCOL_GETTER_SETTER)
struct _DiagnosticsGenerateCoreDumpCommandPayload {
#else
struct _DiagnosticsGenerateCoreDumpCommandPayload_Internal {
#endif
	uint8_t * incoming_buffer;

	// The protocol buffer is defined as:
	//   string - dumpName (UTF16)
	//   int - dumpType
	//   int - diagnostics
	// returns
	//   ulong - status

	const ep_char16_t *dump_name;
	uint32_t dump_type;
	uint32_t diagnostics;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_DUMP_PROTOCOL_GETTER_SETTER)
struct _DiagnosticsGenerateCoreDumpCommandPayload {
	uint8_t _internal [sizeof (struct _DiagnosticsGenerateCoreDumpCommandPayload_Internal)];
};
#endif

DS_DEFINE_GETTER(DiagnosticsGenerateCoreDumpCommandPayload *, generate_core_dump_command_payload, const ep_char16_t *, dump_name)
DS_DEFINE_GETTER(DiagnosticsGenerateCoreDumpCommandPayload *, generate_core_dump_command_payload, uint32_t, dump_type)
DS_DEFINE_GETTER(DiagnosticsGenerateCoreDumpCommandPayload *, generate_core_dump_command_payload, uint32_t, diagnostics)

DiagnosticsGenerateCoreDumpCommandPayload *
ds_generate_core_dump_command_payload_alloc (void);

void
ds_generate_core_dump_command_payload_free (DiagnosticsGenerateCoreDumpCommandPayload *payload);

/*
 * DiagnosticsDumpProtocolHelper.
 */

bool
ds_dump_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_DUMP_PROTOCOL_H__ */
