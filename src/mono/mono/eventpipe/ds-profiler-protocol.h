#ifndef __DIAGNOSTICS_PROFILER_PROTOCOL_H__
#define __DIAGNOSTICS_PROFILER_PROTOCOL_H__

#include <config.h>

#if defined(ENABLE_PERFTRACING) && defined(FEATURE_PROFAPI_ATTACH_DETACH)
#include "ds-rt-config.h"
#include "ds-types.h"
#include "ds-ipc.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_PROFILER_PROTOCOL_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

/*
 * DiagnosticsProfilerProtocolHelper.
 */

void
ds_profiler_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

#endif /* defined(ENABLE_PERFTRACING) && defined(FEATURE_PROFAPI_ATTACH_DETACH) */
#endif /* __DIAGNOSTICS_PROFILER_PROTOCOL_H__ */
