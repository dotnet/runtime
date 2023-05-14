// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Implementation of ds-rt.h targeting NativeAOT runtime.
#ifndef __DIAGNOSTICS_RT_AOT_H__
#define __DIAGNOSTICS_RT_AOT_H__

#include <eventpipe/ds-rt-config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-aot.h"
#include <eventpipe/ds-process-protocol.h>
#include <eventpipe/ds-profiler-protocol.h>
#include <eventpipe/ds-dump-protocol.h>

#undef DS_LOG_ALWAYS_0
#define DS_LOG_ALWAYS_0(msg) do {} while (0)

#undef DS_LOG_ALWAYS_1
#define DS_LOG_ALWAYS_1(msg, data1) do {} while (0)

#undef DS_LOG_ALWAYS_2
#define DS_LOG_ALWAYS_2(msg, data1, data2) do {} while (0)

#undef DS_LOG_INFO_0
#define DS_LOG_INFO_0(msg) do {} while (0)

#undef DS_LOG_INFO_1
#define DS_LOG_INFO_1(msg, data1) do {} while (0)

#undef DS_LOG_INFO_2
#define DS_LOG_INFO_2(msg, data1, data2) do {} while (0)

#undef DS_LOG_ERROR_0
#define DS_LOG_ERROR_0(msg) do {} while (0)

#undef DS_LOG_ERROR_1
#define DS_LOG_ERROR_1(msg, data1) do {} while (0)

#undef DS_LOG_ERROR_2
#define DS_LOG_ERROR_2(msg, data1, data2) do {} while (0)

#undef DS_LOG_WARNING_0
#define DS_LOG_WARNING_0(msg) do {} while (0)

#undef DS_LOG_WARNING_1
#define DS_LOG_WARNING_1(msg, data1) do {} while (0)

#undef DS_LOG_WARNING_2
#define DS_LOG_WARNING_2(msg, data1, data2) do {} while (0)

#undef DS_LOG_DEBUG_0
#define DS_LOG_DEBUG_0(msg) do {} while (0)

#undef DS_LOG_DEBUG_1
#define DS_LOG_DEBUG_1(msg, data1) do {} while (0)

#undef DS_LOG_DEBUG_2
#define DS_LOG_DEBUG_2(msg, data1, data2) do {} while (0)

#undef DS_ENTER_BLOCKING_PAL_SECTION
#define DS_ENTER_BLOCKING_PAL_SECTION

#undef DS_EXIT_BLOCKING_PAL_SECTION
#define DS_EXIT_BLOCKING_PAL_SECTION

/*
* AutoTrace.
*/

#ifdef FEATURE_AUTO_TRACE
#include "autotrace.h"
#endif

static
void
ds_rt_auto_trace_init (void)
{
    STATIC_CONTRACT_NOTHROW;

#ifdef FEATURE_AUTO_TRACE
    auto_trace_init ();
#endif
}

static
void
ds_rt_auto_trace_launch (void)
{
    STATIC_CONTRACT_NOTHROW;

#ifdef FEATURE_AUTO_TRACE
    auto_trace_launch ();
#endif
}

static
void
ds_rt_auto_trace_signal (void)
{
    STATIC_CONTRACT_NOTHROW;

#ifdef FEATURE_AUTO_TRACE
    auto_trace_signal ();
#endif
}

static
void
ds_rt_auto_trace_wait (void)
{
    STATIC_CONTRACT_NOTHROW;

#ifdef FEATURE_AUTO_TRACE
    auto_trace_wait ();
#endif
}

/*
 * DiagnosticsConfiguration.
 */

static
inline
bool
ds_rt_config_value_get_enable (void)
{
    STATIC_CONTRACT_NOTHROW;

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: EventPipe Configuration values - RhConfig?
    return true;
}

static
inline
ep_char8_t *
ds_rt_config_value_get_ports (void)
{
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: EventPipe Configuration values - RhConfig?
    return nullptr;
}

static
inline
uint32_t
ds_rt_config_value_get_default_port_suspend (void)
{
    STATIC_CONTRACT_NOTHROW;
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: EventPipe Configuration values - RhConfig?
    return 0;
}

/*
* DiagnosticsDump.
*/

static
ds_ipc_result_t
ds_rt_generate_core_dump (
    DiagnosticsDumpCommandId commandId,
    DiagnosticsGenerateCoreDumpCommandPayload *payload,
    ep_char8_t *errorMessageBuffer,
    int32_t cbErrorMessageBuffer)
{
    STATIC_CONTRACT_NOTHROW;

    ds_ipc_result_t result = DS_IPC_E_FAIL;
    uint32_t flags = ds_generate_core_dump_command_payload_get_flags(payload);
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Generate an exception dump
    // PalDebugBreak();

    return 0;
}

/*
 * DiagnosticsIpc.
 */

static
bool
ipc_get_process_id_disambiguation_key (
	uint32_t process_id,
	uint64_t *key)
{
	if (!key) {
		EP_ASSERT (!"key argument cannot be null!");
		return false;
	}

	*key = 0;

// Mono implementation, restricted just to Unix
#ifdef TARGET_UNIX

	// Here we read /proc/<pid>/stat file to get the start time for the process.
	// We return this value (which is expressed in jiffies since boot time).

	// Making something like: /proc/123/stat
	char stat_file_name [64];
	snprintf (stat_file_name, sizeof (stat_file_name), "/proc/%d/stat", process_id);

	FILE *stat_file = fopen (stat_file_name, "r");
	if (!stat_file) {
		EP_ASSERT (!"Failed to get start time of a process, fopen failed.");
		return false;
	}

	char *line = NULL;
	size_t line_len = 0;
	if (getline (&line, &line_len, stat_file) == -1)
	{
		EP_ASSERT (!"Failed to get start time of a process, getline failed.");
		return false;
	}

	unsigned long long start_time;

	// According to `man proc`, the second field in the stat file is the filename of the executable,
	// in parentheses. Tokenizing the stat file using spaces as separators breaks when that name
	// has spaces in it, so we start using sscanf_s after skipping everything up to and including the
	// last closing paren and the space after it.
	char *scan_start_position = strrchr (line, ')');
	if (!scan_start_position || scan_start_position [1] == '\0') {
		EP_ASSERT (!"Failed to parse stat file contents with strrchr.");
		return false;
	}

	scan_start_position += 2;

	// All the format specifiers for the fields in the stat file are provided by 'man proc'.
	int result_sscanf = sscanf (scan_start_position,
		"%*c %*d %*d %*d %*d %*d %*u %*u %*u %*u %*u %*u %*u %*d %*d %*d %*d %*d %*d %llu \n",
		&start_time);

	if (result_sscanf != 1) {
		EP_ASSERT (!"Failed to parse stat file contents with sscanf.");
		return false;
	}

	free (line);
	fclose (stat_file);

	*key = (uint64_t)start_time;
	return true;
#else
	// If we don't have /proc, we just return false.
	DS_LOG_WARNING_0 ("ipc_get_process_id_disambiguation_key was called but is not implemented on this platform!");
	return false;
#endif
}

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
    STATIC_CONTRACT_NOTHROW;
    
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: PAL_GetTransportName is defined in coreclr\pal\inc\pal.h
    // TODO: CoreCLR has dependency on PAL_GetTransportName (defined in coreclr\pal\inc\pal.h) and mono one looks simpler
    // Copying ds_rt_mono_transport_get_default_name here to unblock testing
#ifdef TARGET_UNIX

	EP_ASSERT (name != NULL);

	bool result = false;
	int32_t format_result = 0;
	uint64_t disambiguation_key = 0;
	ep_char8_t *format_buffer = NULL;

	*name = '\0';

	format_buffer = (ep_char8_t *)malloc (name_len + 1);
	ep_raise_error_if_nok (format_buffer != NULL);

	*format_buffer = '\0';

	// If ipc_get_process_id_disambiguation_key failed for some reason, it should set the value
	// to 0. We expect that anyone else making the pipe name will also fail and thus will
	// also try to use 0 as the value.
	if (!ipc_get_process_id_disambiguation_key (id, &disambiguation_key))
		EP_ASSERT (disambiguation_key == 0);
		// Get a temp file location
    format_result = ep_rt_temp_path_get (format_buffer, name_len);
    if (format_result == 0) {
        DS_LOG_ERROR_0 ("ep_rt_temp_path_get failed");
        ep_raise_error ();
    }

    EP_ASSERT (format_result <= name_len);

	format_result = snprintf(name, name_len, "%s%s-%d-%llu-%s", format_buffer, prefix, id, (unsigned long long)disambiguation_key, suffix);
	if (format_result <= 0 || (uint32_t)format_result > name_len) {
		DS_LOG_ERROR_0 ("name buffer to small");
		ep_raise_error ();
	}

	result = true;

ep_on_exit:
	free (format_buffer);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	name [0] = '\0';
	ep_exit_error_handler ();

#else
    return true;
#endif
}

/*
* DiagnosticsProfiler.
*/
#ifdef PROFILING_SUPPORTED
#include "profilinghelper.h"
#include "profilinghelper.inl"

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
static
uint32_t
ds_rt_profiler_attach (DiagnosticsAttachProfilerCommandPayload *payload)
{
    STATIC_CONTRACT_NOTHROW;

    if (!g_profControlBlock.fProfControlBlockInitialized)
        return DS_IPC_E_RUNTIME_UNINITIALIZED;

    // Certain actions are only allowable during attach, and this flag is how we track it.
    ClrFlsSetThreadType (ThreadType_ProfAPI_Attach);

    HRESULT hr = S_OK;
    hr = ProfilingAPIUtility::LoadProfilerForAttach (reinterpret_cast<const CLSID *>(ds_attach_profiler_command_payload_get_profiler_guid_cref (payload)),
        reinterpret_cast<LPCWSTR>(ds_attach_profiler_command_payload_get_profiler_path (payload)),
        reinterpret_cast<LPVOID>(ds_attach_profiler_command_payload_get_client_data (payload)),
        static_cast<UINT>(ds_attach_profiler_command_payload_get_client_data_len (payload)),
        static_cast<DWORD>(ds_attach_profiler_command_payload_get_attach_timeout (payload)));

    // Clear the flag so this thread isn't permanently marked as the attach thread.
    ClrFlsClearThreadType (ThreadType_ProfAPI_Attach);

    return hr;
}
#endif // FEATURE_PROFAPI_ATTACH_DETACH

static
uint32_t
ds_rt_profiler_startup (DiagnosticsStartupProfilerCommandPayload *payload)
{
    STATIC_CONTRACT_NOTHROW;

    HRESULT hr = S_OK;
    StoredProfilerNode *profilerData = new StoredProfilerNode();
    profilerData->guid = *(reinterpret_cast<const CLSID *>(ds_startup_profiler_command_payload_get_profiler_guid_cref (payload)));
    profilerData->path.Set(reinterpret_cast<LPCWSTR>(ds_startup_profiler_command_payload_get_profiler_path (payload)));

    g_profControlBlock.storedProfilers.InsertHead(profilerData);

    return hr;
}
#endif // PROFILING_SUPPORTED

static
uint32_t
ds_rt_set_environment_variable (const ep_char16_t *name, const ep_char16_t *value)
{
     // return SetEnvironmentVariableW(reinterpret_cast<LPCWSTR>(name), reinterpret_cast<LPCWSTR>(value)) ? S_OK : HRESULT_FROM_WIN32(GetLastError());
     // PalDebugBreak();
    return 0xffff;
}

/*
* DiagnosticServer.
*/

static
void
ds_rt_server_log_pause_message (void)
{
    STATIC_CONTRACT_NOTHROW;

    const char diagPortsName[] = "DOTNET_DiagnosticPorts";
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Cannot find nocache versions of RhConfig
    // PalDebugBreak();
}

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_RT_AOT_H__ */
