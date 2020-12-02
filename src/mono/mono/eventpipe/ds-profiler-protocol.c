#include <config.h>

#if defined(ENABLE_PERFTRACING) && defined(FEATURE_PROFAPI_ATTACH_DETACH)
#include "ds-rt-config.h"
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_PROFILER_PROTOCOL_GETTER_SETTER
#include "ds-protocol.h"
#include "ds-profiler-protocol.h"
#include "ds-rt.h"

void
ds_profiler_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	EP_ASSERT (message != NULL);
	EP_ASSERT (stream != NULL);

	// TODO: Implement.
	DS_LOG_WARNING_0 ("Attach profiler not implemented\n");
	ds_ipc_message_send_error (stream, DS_IPC_E_NOTSUPPORTED);
	ds_ipc_stream_free (stream);
}

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* #if defined(ENABLE_PERFTRACING) && defined(FEATURE_PROFAPI_ATTACH_DETACH) */

#ifndef DS_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_diagnostics_profiler_protocol;
const char quiet_linker_empty_file_warning_diagnostics_profiler_protocol = 0;
#endif
