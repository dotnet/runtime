#ifndef __DIAGNOSTICS_RT_H__
#define __DIAGNOSTICS_RT_H__

#ifdef ENABLE_PERFTRACING
#include "ep-rt.h"
#include "ds-rt-config.h"
#include "ds-types.h"

#define DS_LOG_ALWAYS_0(msg) ds_rt_redefine
#define DS_LOG_ALWAYS_1(msg, data1) ds_rt_redefine
#define DS_LOG_ALWAYS_2(msg, data1, data2) ds_rt_redefine
#define DS_LOG_INFO_0(msg) ds_rt_redefine
#define DS_LOG_INFO_1(msg, data1) ds_rt_redefine
#define DS_LOG_INFO_2(msg, data1, data2) ds_rt_redefine
#define DS_LOG_ERROR_0(msg) ds_rt_redefine
#define DS_LOG_ERROR_1(msg, data1) ds_rt_redefine
#define DS_LOG_ERROR_2(msg, data1, data2) ds_rt_redefine
#define DS_LOG_WARNING_0(msg) ds_rt_redefine
#define DS_LOG_WARNING_1(msg, data1) ds_rt_redefine
#define DS_LOG_WARNING_2(msg, data1, data2) ds_rt_redefine
#define DS_LOG_DEBUG_0(msg) ds_rt_redefine
#define DS_LOG_DEBUG_1(msg, data1) ds_rt_redefine
#define DS_LOG_DEBUG_2(msg, data1, data2) ds_rt_redefine

#define DS_ENTER_BLOCKING_PAL_SECTION ds_rt_redefine
#define DS_EXIT_BLOCKING_PAL_SECTION ds_rt_redefine

/*
* AutoTrace.
*/

static
void
ds_rt_auto_trace_init (void);

static
void
ds_rt_auto_trace_launch (void);

static
void
ds_rt_auto_trace_signal (void);

static
void
ds_rt_auto_trace_wait (void);

/*
 * DiagnosticsConfiguration.
 */

static
bool
ds_rt_config_value_get_enable (void);

static
ep_char8_t *
ds_rt_config_value_get_ports (void);

static
uint32_t
ds_rt_config_value_get_default_port_suspend (void);

/*
* DiagnosticsDump.
*/

static
ds_ipc_result_t
ds_rt_generate_core_dump (DiagnosticsDumpCommandId commandId, DiagnosticsGenerateCoreDumpCommandPayload *payload, ep_char8_t *errorMessageBuffer, int32_t cbErrorMessageBuffer);

/*
 * DiagnosticsIpc.
 */

static
bool
ds_rt_transport_get_default_name (
	ep_char8_t *name,
	int32_t name_len,
	const ep_char8_t *prefix,
	int32_t id,
	const ep_char8_t *group_id,
	const ep_char8_t *suffix);

/*
* DiagnosticsProfiler.
*/

static
uint32_t
ds_rt_profiler_attach (DiagnosticsAttachProfilerCommandPayload *payload);

static
uint32_t
ds_rt_profiler_startup (DiagnosticsStartupProfilerCommandPayload *payload);

/*
* Environment variables
*/

static
uint32_t
ds_rt_set_environment_variable (const ep_char16_t *name, const ep_char16_t *value);

static
uint32_t
ds_rt_get_environment_variable (const ep_char16_t *name,
								uint32_t valueBufferLength,
								uint32_t *valueLengthOut,
								ep_char16_t *valueBuffer);

static
uint32_t
ds_rt_enable_perfmap (uint32_t type);

static
uint32_t
ds_rt_disable_perfmap (void);

/*
* DiagnosticServer.
*/

static
void
ds_rt_server_log_pause_message (void);

#ifndef EP_NO_RT_DEPENDENCY
#include DS_RT_H
#endif

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_RT_H__ */
