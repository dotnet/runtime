// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Implementation of ds-rt.h targeting CoreCLR runtime.
#ifndef __DIAGNOSTICS_RT_MONO_H__
#define __DIAGNOSTICS_RT_MONO_H__

#include <eventpipe/ds-rt-config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-coreclr.h"
#include <clrconfignocache.h>
#include <generatedumpflags.h>
#include <eventpipe/ds-process-protocol.h>
#include <eventpipe/ds-profiler-protocol.h>
#include <eventpipe/ds-dump-protocol.h>
#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

#undef DS_LOG_ALWAYS_0
#define DS_LOG_ALWAYS_0(msg) STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_ALWAYS, msg "\n")

#undef DS_LOG_ALWAYS_1
#define DS_LOG_ALWAYS_1(msg, data1) STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_ALWAYS, msg "\n", data1)

#undef DS_LOG_ALWAYS_2
#define DS_LOG_ALWAYS_2(msg, data1, data2) STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_ALWAYS, msg "\n", data1, data2)

#undef DS_LOG_INFO_0
#define DS_LOG_INFO_0(msg) STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_INFO10, msg "\n")

#undef DS_LOG_INFO_1
#define DS_LOG_INFO_1(msg, data1) STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_INFO10, msg "\n", data1)

#undef DS_LOG_INFO_2
#define DS_LOG_INFO_2(msg, data1, data2) STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, msg "\n", data1, data2)

#undef DS_LOG_ERROR_0
#define DS_LOG_ERROR_0(msg) STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_ERROR, msg "\n")

#undef DS_LOG_ERROR_1
#define DS_LOG_ERROR_1(msg, data1) STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_ERROR, msg "\n", data1)

#undef DS_LOG_ERROR_2
#define DS_LOG_ERROR_2(msg, data1, data2) STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_ERROR, msg "\n", data1, data2)

#undef DS_LOG_WARNING_0
#define DS_LOG_WARNING_0(msg) STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_WARNING, msg "\n")

#undef DS_LOG_WARNING_1
#define DS_LOG_WARNING_1(msg, data1) STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_WARNING, msg "\n", data1)

#undef DS_LOG_WARNING_2
#define DS_LOG_WARNING_2(msg, data1, data2) STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_WARNING, msg "\n", data1, data2)

#undef DS_LOG_DEBUG_0
#define DS_LOG_DEBUG_0(msg) STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_INFO1000, msg "\n")

#undef DS_LOG_DEBUG_1
#define DS_LOG_DEBUG_1(msg, data1) STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_INFO1000, msg "\n", data1)

#undef DS_LOG_DEBUG_2
#define DS_LOG_DEBUG_2(msg, data1, data2) STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO1000, msg "\n", data1, data2)

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
	EX_TRY
	{
		auto_trace_init ();
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
#endif
}

static
void
ds_rt_auto_trace_launch (void)
{
	STATIC_CONTRACT_NOTHROW;

#ifdef FEATURE_AUTO_TRACE
	EX_TRY
	{
		auto_trace_launch ();
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
#endif
}

static
void
ds_rt_auto_trace_signal (void)
{
	STATIC_CONTRACT_NOTHROW;

#ifdef FEATURE_AUTO_TRACE
	EX_TRY
	{
		auto_trace_signal ();
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
#endif
}

static
void
ds_rt_auto_trace_wait (void)
{
	STATIC_CONTRACT_NOTHROW;

#ifdef FEATURE_AUTO_TRACE
	EX_TRY
	{
		auto_trace_wait ();
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
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
	if (CLRConfig::GetConfigValue (CLRConfig::EXTERNAL_EnableDiagnostics) == 0)
	{
		return false;
	}
    return CLRConfig::GetConfigValue (CLRConfig::EXTERNAL_EnableDiagnostics_IPC) != 0;
}

static
inline
ep_char8_t *
ds_rt_config_value_get_ports (void)
{
	STATIC_CONTRACT_NOTHROW;

	CLRConfigStringHolder value(CLRConfig::GetConfigValue (CLRConfig::EXTERNAL_DOTNET_DiagnosticPorts));
	return ep_rt_utf16_to_utf8_string (reinterpret_cast<ep_char16_t *>(value.GetValue ()), -1);
}

static
inline
uint32_t
ds_rt_config_value_get_default_port_suspend (void)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<uint32_t>(CLRConfig::GetConfigValue (CLRConfig::EXTERNAL_DOTNET_DefaultDiagnosticPortSuspend));
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
	EX_TRY
	{
		uint32_t flags = ds_generate_core_dump_command_payload_get_flags(payload);
		if (commandId == DS_DUMP_COMMANDID_GENERATE_CORE_DUMP)
		{
			// For the old commmand, this payload field is a bool of whether to enable logging
			flags = flags != 0 ? GenerateDumpFlagsLoggingEnabled : 0;
		}
		LPCWSTR dumpName = reinterpret_cast<LPCWSTR>(ds_generate_core_dump_command_payload_get_dump_name (payload));
		int32_t dumpType = static_cast<int32_t>(ds_generate_core_dump_command_payload_get_dump_type (payload));
		if (GenerateDump(dumpName, dumpType, flags, errorMessageBuffer, cbErrorMessageBuffer))
		{
			result = DS_IPC_S_OK;
		}
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
	return result;
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
	STATIC_CONTRACT_NOTHROW;

#ifdef TARGET_UNIX
	PAL_GetTransportName (name_len, name, prefix, id, group_id, suffix);
#endif
	return true;
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
	EX_TRY {
		hr = ProfilingAPIUtility::LoadProfilerForAttach (reinterpret_cast<const CLSID *>(ds_attach_profiler_command_payload_get_profiler_guid_cref (payload)),
			reinterpret_cast<LPCWSTR>(ds_attach_profiler_command_payload_get_profiler_path (payload)),
			reinterpret_cast<LPVOID>(ds_attach_profiler_command_payload_get_client_data (payload)),
			static_cast<UINT>(ds_attach_profiler_command_payload_get_client_data_len (payload)),
			static_cast<DWORD>(ds_attach_profiler_command_payload_get_attach_timeout (payload)));
	}
	EX_CATCH_HRESULT (hr);

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
	EX_TRY {
		StoredProfilerNode *profilerData = new StoredProfilerNode();
		profilerData->guid = *(reinterpret_cast<const CLSID *>(ds_startup_profiler_command_payload_get_profiler_guid_cref (payload)));
		profilerData->path.Set(reinterpret_cast<LPCWSTR>(ds_startup_profiler_command_payload_get_profiler_path (payload)));

		g_profControlBlock.storedProfilers.InsertHead(profilerData);
	}
	EX_CATCH_HRESULT (hr);

	return hr;
}
#endif // PROFILING_SUPPORTED

static
uint32_t
ds_rt_set_environment_variable (const ep_char16_t *name, const ep_char16_t *value)
{
	return SetEnvironmentVariableW(reinterpret_cast<LPCWSTR>(name), reinterpret_cast<LPCWSTR>(value)) ? S_OK : HRESULT_FROM_WIN32(GetLastError());
}

static
uint32_t
ds_rt_enable_perfmap (uint32_t type)
{
	LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_PERFMAP
	PerfMap::PerfMapType perfMapType = (PerfMap::PerfMapType)type;
	if (perfMapType == PerfMap::PerfMapType::DISABLED || perfMapType > PerfMap::PerfMapType::PERFMAP)
	{
		return DS_IPC_E_INVALIDARG;
	}

	PerfMap::Enable(perfMapType, true);

    return DS_IPC_S_OK;
#else // FEATURE_PERFMAP
    return DS_IPC_E_NOTSUPPORTED;
#endif // FEATURE_PERFMAP
}

static
uint32_t
ds_rt_disable_perfmap (void)
{
	LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_PERFMAP
	PerfMap::Disable();
	return DS_IPC_S_OK;
#else // FEATURE_PERFMAP
	return DS_IPC_E_NOTSUPPORTED;
#endif // FEATURE_PERFMAP
}

static ep_char16_t * _ds_rt_coreclr_diagnostic_startup_hook_paths = NULL;

static
uint32_t
ds_rt_apply_startup_hook (const ep_char16_t *startup_hook_path)
{
	if (NULL == startup_hook_path)
		return DS_IPC_E_INVALIDARG;

	HRESULT hr = S_OK;
	// This is set to true when the EE has initialized, which occurs after
	// the diagnostic suspension point has completed.
	if (g_fEEStarted)
	{
		// This is not actually starting the EE (the above already checked that),
		// but waits for the EE to be started so that the startup hook can be loaded
		// and executed.
		IfFailRet(EnsureEEStarted());

		// Ensure runtime thread for diagnostic server thread
		SetupThreadNoThrow(&hr);
		IfFailRet(hr);

		EX_TRY {
			GCX_COOP();

			// Load and call startup hook since managed execution is already running.
			MethodDescCallSite callStartupHook(METHOD__STARTUP_HOOK_PROVIDER__CALL_STARTUP_HOOK);

			ARG_SLOT args[1];
			args[0] = PtrToArgSlot(startup_hook_path);

			callStartupHook.Call(args);
		}
		EX_CATCH_HRESULT (hr);

		IfFailRet(hr);
	}
	else
	{
		Assembly::AddDiagnosticStartupHookPath(reinterpret_cast<LPCWSTR>(startup_hook_path));
	}

	return DS_IPC_S_OK;
}

/*
* DiagnosticServer.
*/

static
void
ds_rt_server_log_pause_message (void)
{
	STATIC_CONTRACT_NOTHROW;

	const char diagPortsName[] = "DiagnosticPorts";
	CLRConfigNoCache diagPorts = CLRConfigNoCache::Get(diagPortsName);
	LPCSTR ports = nullptr;
	if (diagPorts.IsSet())
	{
		ports = diagPorts.AsString();
	}

	uint32_t port_suspended = ds_rt_config_value_get_default_port_suspend();

	printf("The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command from a Diagnostic Port.\n");
	printf("DOTNET_%s=\"%s\"\n", diagPortsName, ports == nullptr ? "" : ports);
	printf("DOTNET_DefaultDiagnosticPortSuspend=%u\n", port_suspended);
	fflush(stdout);
}

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_RT_MONO_H__ */
