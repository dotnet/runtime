// Implementation of ds-rt.h targeting Mono runtime.
#ifndef __DIAGNOSTICS_RT_MONO_H__
#define __DIAGNOSTICS_RT_MONO_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-mono.h"

#undef DS_LOG_ALWAYS_0
#define DS_LOG_ALWAYS_0(msg)

#undef DS_LOG_ALWAYS_1
#define DS_LOG_ALWAYS_1(msg, data1)

#undef DS_LOG_ALWAYS_2
#define DS_LOG_ALWAYS_2(msg, data1, data2)

#undef DS_LOG_INFO_0
#define DS_LOG_INFO_0(msg)

#undef DS_LOG_INFO_1
#define DS_LOG_INFO_1(msg, data1)

#undef DS_LOG_INFO_2
#define DS_LOG_INFO_2(msg, data1, data2)

#undef DS_LOG_ERROR_0
#define DS_LOG_ERROR_0(msg)

#undef DS_LOG_ERROR_1
#define DS_LOG_ERROR_1(msg, data1)

#undef DS_LOG_ERROR_2
#define DS_LOG_ERROR_2(msg, data1, data2)

#undef DS_LOG_WARNING_0
#define DS_LOG_WARNING_0(msg)

#undef DS_LOG_WARNING_1
#define DS_LOG_WARNING_1(msg, data1)

#undef DS_LOG_WARNING_2
#define DS_LOG_WARNING_2(msg, data1, data2)

#undef DS_LOG_DEBUG_0
#define DS_LOG_DEBUG_0(msg)

#undef DS_ENTER_BLOCKING_PAL_SECTION
#define DS_ENTER_BLOCKING_PAL_SECTION \
	MONO_REQ_GC_UNSAFE_MODE \
	MONO_ENTER_GC_SAFE

#undef DS_EXIT_BLOCKING_PAL_SECTION
#define DS_EXIT_BLOCKING_PAL_SECTION \
	MONO_REQ_GC_SAFE_MODE \
	MONO_EXIT_GC_SAFE \
	MONO_REQ_GC_UNSAFE_MODE

#define DS_RT_DEFINE_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_PREFIX(ds, array_name, array_type, iterator_type, item_type)

#define DS_RT_DEFINE_ARRAY_ITERATOR(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_ITERATOR_PREFIX(ds, array_name, array_type, iterator_type, item_type)

#define DS_RT_DEFINE_ARRAY_REVERSE_ITERATOR(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_REVERSE_ITERATOR_PREFIX(ds, array_name, array_type, iterator_type, item_type)

/*
* AutoTrace.
*/

static
inline
void
ds_rt_auto_trace_init (void)
{
	// TODO: Implement.
}

static
inline
void
ds_rt_auto_trace_launch (void)
{
	// TODO: Implement.
}

static
inline
void
ds_rt_auto_trace_signal (void)
{
	// TODO: Implement.
}

static
inline
void
ds_rt_auto_trace_wait (void)
{
	// TODO: Implement.
}

/*
 * DiagnosticsConfiguration.
 */

static
inline
bool
ds_rt_config_value_get_enable (void)
{
	bool enable = true;
	gchar *value = g_getenv ("COMPlus_EnableDiagnostics");
	if (value && atoi (value) == 0)
		enable = false;
	g_free (value);
	return enable;
}

static
inline
ep_char8_t *
ds_rt_config_value_get_ports (void)
{
	return g_getenv ("DOTNET_DiagnosticPorts");
}

static
inline
uint32_t
ds_rt_config_value_get_default_port_suspend (void)
{
	uint32_t value_uint32_t = 0;
	gchar *value = g_getenv ("DOTNET_DefaultDiagnosticPortSuspend");
	if (value)
		value_uint32_t = (uint32_t)atoi (value);
	g_free (value);
	return value_uint32_t;
}

/*
* DiagnosticsDump.
*/

static
inline
ds_ipc_result_t
ds_rt_generate_core_dump (DiagnosticsGenerateCoreDumpCommandPayload *payload)
{
	// TODO: Implement.
	return DS_IPC_E_NOTSUPPORTED;
}

/*
 * DiagnosticsIpc.
 */

static
inline
bool
ds_rt_transport_get_default_name (
	ep_char8_t *name,
	int32_t name_len,
	const ep_char8_t *prefix,
	int32_t id,
	const ep_char8_t *group_id,
	const ep_char8_t *suffix)
{
	extern bool ipc_transport_get_default_name (ep_char8_t *name, uint32_t name_len, const ep_char8_t *prefix, int32_t id, const ep_char8_t *group_id, const ep_char8_t *suffix);
	return ipc_transport_get_default_name (name, name_len, prefix, id, group_id, suffix);
}

/*
 * DiagnosticsIpcPollHandle.
 */

DS_RT_DEFINE_ARRAY (ipc_poll_handle_array, ds_rt_ipc_poll_handle_array_t, ds_rt_ipc_poll_handle_array_iterator_t, DiagnosticsIpcPollHandle)
DS_RT_DEFINE_ARRAY_ITERATOR (ipc_poll_handle_array, ds_rt_ipc_poll_handle_array_t, ds_rt_ipc_poll_handle_array_iterator_t, DiagnosticsIpcPollHandle)

/*
 * DiagnosticsPort.
 */

DS_RT_DEFINE_ARRAY (port_array, ds_rt_port_array_t, ds_rt_port_array_iterator_t, DiagnosticsPort *)
DS_RT_DEFINE_ARRAY_ITERATOR (port_array, ds_rt_port_array_t, ds_rt_port_array_iterator_t, DiagnosticsPort *)

DS_RT_DEFINE_ARRAY (port_config_array, ds_rt_port_config_array_t, ds_rt_port_config_array_iterator_t, ep_char8_t *)
DS_RT_DEFINE_ARRAY_ITERATOR (port_config_array, ds_rt_port_config_array_t, ds_rt_port_config_array_iterator_t, ep_char8_t *)
DS_RT_DEFINE_ARRAY_REVERSE_ITERATOR (port_config_array, ds_rt_port_config_array_t, ds_rt_port_config_array_reverse_iterator_t, ep_char8_t *)

/*
* DiagnosticsProfiler.
*/

static
inline
uint32_t
ds_rt_profiler_attach (DiagnosticsAttachProfilerCommandPayload *payload)
{
	// TODO: Implement.
	return DS_IPC_E_NOTSUPPORTED;
}

/*
* DiagnosticServer.
*/

static
inline
void
ds_rt_server_log_pause_message (void)
{
	ep_char8_t * ports = ds_rt_config_value_get_ports ();
#if WCHAR_MAX == 0xFFFF
	wchar_t* ports_wcs = ports ? (wchar_t *)g_utf8_to_utf16 ((const gchar *)ports, -1, NULL, NULL, NULL) : NULL;
#else
	wchar_t* ports_wcs = ports ? (wchar_t *)g_utf8_to_ucs4 ((const gchar *)ports, -1, NULL, NULL, NULL) : NULL;
#endif
	uint32_t port_suspended = ds_rt_config_value_get_default_port_suspend ();

	printf ("The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command from a Diagnostic Port.\n");
	printf ("DOTNET_DiagnosticPorts=\"%ls\"\n", ports_wcs == NULL ? L"" : ports_wcs);
	printf("DOTNET_DefaultDiagnosticPortSuspend=%d\n", port_suspended);
	fflush (stdout);

	g_free (ports_wcs);
	ep_rt_utf8_string_free (ports);
}

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_RT_MONO_H__ */
