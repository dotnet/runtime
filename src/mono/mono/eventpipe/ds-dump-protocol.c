#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ds-rt-config.h"
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_DUMP_PROTOCOL_GETTER_SETTER
#include "ds-protocol.h"
#include "ds-dump-protocol.h"
#include "ds-rt.h"

void
ds_dump_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	// TODO: Implement.
	DS_LOG_WARNING_0 ("Generate Core Dump not implemented\n");
	ds_ipc_message_send_error (stream, DS_IPC_E_NOTSUPPORTED);
	ds_ipc_stream_free (stream);
}

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef DS_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_diagnostics_dump_protocol;
const char quiet_linker_empty_file_warning_diagnostics_dump_protocol = 0;
#endif
