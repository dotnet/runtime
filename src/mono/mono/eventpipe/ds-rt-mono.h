// Implementation of ds-rt.h targeting Mono runtime.
#ifndef __DIAGNOSTICS_RT_MONO_H__
#define __DIAGNOSTICS_RT_MONO_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-mono.h"
#include <mono/utils/mono-logger-internals.h>

#undef DS_LOG_ALWAYS_0
#define DS_LOG_ALWAYS_0(msg) mono_trace(G_LOG_LEVEL_MESSAGE, MONO_TRACE_DIAGNOSTICS, msg)

#undef DS_LOG_ALWAYS_1
#define DS_LOG_ALWAYS_1(msg, data1) mono_trace(G_LOG_LEVEL_MESSAGE, MONO_TRACE_DIAGNOSTICS, msg, data1)

#undef DS_LOG_ALWAYS_2
#define DS_LOG_ALWAYS_2(msg, data1, data2) mono_trace(G_LOG_LEVEL_MESSAGE, MONO_TRACE_DIAGNOSTICS, msg, data1, data2)

#undef DS_LOG_INFO_0
#define DS_LOG_INFO_0(msg) mono_trace(G_LOG_LEVEL_INFO, MONO_TRACE_DIAGNOSTICS, msg)

#undef DS_LOG_INFO_1
#define DS_LOG_INFO_1(msg, data1) mono_trace(G_LOG_LEVEL_INFO, MONO_TRACE_DIAGNOSTICS, msg, data1)

#undef DS_LOG_INFO_2
#define DS_LOG_INFO_2(msg, data1, data2) mono_trace(G_LOG_LEVEL_INFO, MONO_TRACE_DIAGNOSTICS, msg, data1, data2)

#undef DS_LOG_ERROR_0
#define DS_LOG_ERROR_0(msg) mono_trace(G_LOG_LEVEL_CRITICAL, MONO_TRACE_DIAGNOSTICS, msg)

#undef DS_LOG_ERROR_1
#define DS_LOG_ERROR_1(msg, data1) mono_trace(G_LOG_LEVEL_CRITICAL, MONO_TRACE_DIAGNOSTICS, msg, data1)

#undef DS_LOG_ERROR_2
#define DS_LOG_ERROR_2(msg, data1, data2) mono_trace(G_LOG_LEVEL_CRITICAL, MONO_TRACE_DIAGNOSTICS, msg, data1, data2)

#undef DS_LOG_WARNING_0
#define DS_LOG_WARNING_0(msg) mono_trace(G_LOG_LEVEL_WARNING, MONO_TRACE_DIAGNOSTICS, msg)

#undef DS_LOG_WARNING_1
#define DS_LOG_WARNING_1(msg, data1) mono_trace(G_LOG_LEVEL_WARNING, MONO_TRACE_DIAGNOSTICS, msg, data1)

#undef DS_LOG_WARNING_2
#define DS_LOG_WARNING_2(msg, data1, data2) mono_trace(G_LOG_LEVEL_WARNING, MONO_TRACE_DIAGNOSTICS, msg, data1, data2)

#undef DS_LOG_DEBUG_0
#define DS_LOG_DEBUG_0(msg) mono_trace(G_LOG_LEVEL_DEBUG, MONO_TRACE_DIAGNOSTICS, msg)

#undef DS_LOG_DEBUG_1
#define DS_LOG_DEBUG_1(msg, data1) mono_trace(G_LOG_LEVEL_DEBUG, MONO_TRACE_DIAGNOSTICS, msg, data1)

#undef DS_LOG_DEBUG_2
#define DS_LOG_DEBUG_2(msg, data1, data2) mono_trace(G_LOG_LEVEL_DEBUG, MONO_TRACE_DIAGNOSTICS, msg, data1, data2)

#undef DS_ENTER_BLOCKING_PAL_SECTION
#define DS_ENTER_BLOCKING_PAL_SECTION \
	MONO_REQ_GC_UNSAFE_MODE \
	MONO_ENTER_GC_SAFE

#undef DS_EXIT_BLOCKING_PAL_SECTION
#define DS_EXIT_BLOCKING_PAL_SECTION \
	MONO_REQ_GC_SAFE_MODE \
	MONO_EXIT_GC_SAFE \
	MONO_REQ_GC_UNSAFE_MODE

#undef DS_RT_DEFINE_ARRAY
#define DS_RT_DEFINE_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_PREFIX(ds, array_name, array_type, iterator_type, item_type)

#undef DS_RT_DEFINE_LOCAL_ARRAY
#define DS_RT_DEFINE_LOCAL_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_LOCAL_ARRAY_PREFIX(ds, array_name, array_type, iterator_type, item_type)

#undef DS_RT_DEFINE_ARRAY_ITERATOR
#define DS_RT_DEFINE_ARRAY_ITERATOR(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_ITERATOR_PREFIX(ds, array_name, array_type, iterator_type, item_type)

#undef DS_RT_DEFINE_ARRAY_REVERSE_ITERATOR
#define DS_RT_DEFINE_ARRAY_REVERSE_ITERATOR(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_REVERSE_ITERATOR_PREFIX(ds, array_name, array_type, iterator_type, item_type)

bool
ds_rt_mono_transport_get_default_name (
	ep_char8_t *name,
	uint32_t name_len,
	const ep_char8_t *prefix,
	int32_t id,
	const ep_char8_t *group_id,
	const ep_char8_t *suffix);

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
	gchar *value = g_getenv ("DOTNET_EnableDiagnostics");
	if (!value)
		value = g_getenv ("COMPlus_EnableDiagnostics");
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
	return ds_rt_mono_transport_get_default_name (name, name_len, prefix, id, group_id, suffix);
}

/*
 * DiagnosticsIpcPollHandle.
 */

DS_RT_DEFINE_ARRAY (ipc_poll_handle_array, ds_rt_ipc_poll_handle_array_t, ds_rt_ipc_poll_handle_array_iterator_t, DiagnosticsIpcPollHandle)
DS_RT_DEFINE_LOCAL_ARRAY (ipc_poll_handle_array, ds_rt_ipc_poll_handle_array_t, ds_rt_ipc_poll_handle_array_iterator_t, DiagnosticsIpcPollHandle)
DS_RT_DEFINE_ARRAY_ITERATOR (ipc_poll_handle_array, ds_rt_ipc_poll_handle_array_t, ds_rt_ipc_poll_handle_array_iterator_t, DiagnosticsIpcPollHandle)

#undef DS_RT_DECLARE_LOCAL_IPC_POLL_HANDLE_ARRAY
#define DS_RT_DECLARE_LOCAL_IPC_POLL_HANDLE_ARRAY(var_name) \
	ds_rt_ipc_poll_handle_array_t var_name

/*
 * DiagnosticsPort.
 */

DS_RT_DEFINE_ARRAY (port_array, ds_rt_port_array_t, ds_rt_port_array_iterator_t, DiagnosticsPort *)
DS_RT_DEFINE_ARRAY_ITERATOR (port_array, ds_rt_port_array_t, ds_rt_port_array_iterator_t, DiagnosticsPort *)

DS_RT_DEFINE_ARRAY (port_config_array, ds_rt_port_config_array_t, ds_rt_port_config_array_iterator_t, ep_char8_t *)
DS_RT_DEFINE_LOCAL_ARRAY (port_config_array, ds_rt_port_config_array_t, ds_rt_port_config_array_iterator_t, ep_char8_t *)
DS_RT_DEFINE_ARRAY_ITERATOR (port_config_array, ds_rt_port_config_array_t, ds_rt_port_config_array_iterator_t, ep_char8_t *)
DS_RT_DEFINE_ARRAY_REVERSE_ITERATOR (port_config_array, ds_rt_port_config_array_t, ds_rt_port_config_array_reverse_iterator_t, ep_char8_t *)

#undef DS_RT_DECLARE_LOCAL_PORT_CONFIG_ARRAY
#define DS_RT_DECLARE_LOCAL_PORT_CONFIG_ARRAY(var_name) \
	ds_rt_port_config_array_t var_name

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

static
inline
uint32_t
ds_rt_profiler_startup (DiagnosticsStartupProfilerCommandPayload *payload)
{
	// TODO: Implement.
	return DS_IPC_E_NOTSUPPORTED;
}

/*
* Environment variables
*/

static
uint32_t
ds_rt_set_environment_variable (const ep_char16_t *name, const ep_char16_t *value)
{
	gchar *nameNarrow = ep_rt_utf16_to_utf8_string (name, ep_rt_utf16_string_len (name));
	gchar *valueNarrow = ep_rt_utf16_to_utf8_string (value, ep_rt_utf16_string_len (value));

	gboolean success = g_setenv(nameNarrow, valueNarrow, true);

	g_free (nameNarrow);
	g_free (valueNarrow);

	return success ? DS_IPC_S_OK : DS_IPC_E_FAIL;
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
