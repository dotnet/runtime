// Implementation of ds-rt.h targeting AOT runtime.
#ifndef __DIAGNOSTICS_RT_AOT_H__
#define __DIAGNOSTICS_RT_AOT_H__

#include <eventpipe/ds-rt-config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-aot.h"
#include <eventpipe/ds-process-protocol.h>
#include <eventpipe/ds-profiler-protocol.h>
#include <eventpipe/ds-dump-protocol.h>

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
    PalDebugBreak();

    return 0;
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
    
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: PAL_GetTransportName is defined in coreclr\pal\inc\pal.h
    return true;
}

/*
 * DiagnosticsIpcPollHandle.
 */

DS_RT_DEFINE_ARRAY (ipc_poll_handle_array, ds_rt_ipc_poll_handle_array_t, ds_rt_ipc_poll_handle_array_iterator_t, DiagnosticsIpcPollHandle)
DS_RT_DEFINE_LOCAL_ARRAY (ipc_poll_handle_array, ds_rt_ipc_poll_handle_array_t, ds_rt_ipc_poll_handle_array_iterator_t, DiagnosticsIpcPollHandle)
DS_RT_DEFINE_ARRAY_ITERATOR (ipc_poll_handle_array, ds_rt_ipc_poll_handle_array_t, ds_rt_ipc_poll_handle_array_iterator_t, DiagnosticsIpcPollHandle)

#undef DS_RT_DECLARE_LOCAL_IPC_POLL_HANDLE_ARRAY
#define DS_RT_DECLARE_LOCAL_IPC_POLL_HANDLE_ARRAY(var_name) \
    EP_RT_DECLARE_LOCAL_ARRAY_VARIABLE(var_name, ds_rt_ipc_poll_handle_array_t)

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
    EP_RT_DECLARE_LOCAL_ARRAY_VARIABLE(var_name, ds_rt_port_config_array_t)

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
     PalDebugBreak();
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
    PalDebugBreak();
}

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_RT_AOT_H__ */
