#ifndef __DIAGNOSTICS_RT_TYPES_H__
#define __DIAGNOSTICS_RT_TYPES_H__

#ifdef ENABLE_PERFTRACING

/*
 * DiagnosticsIpcPollHandle.
 */

#define ds_rt_ipc_poll_handle_array_t ds_rt_redefine
#define ds_rt_ipc_poll_handle_array_iterator_t ds_rt_redefine

/*
 * DiagnosticsPort.
 */

#define ds_rt_port_array_t ds_rt_redefine
#define ds_rt_port_array_iterator_t ds_rt_redefine

#define ds_rt_port_config_array_t ds_rt_redefine
#define ds_rt_port_config_array_iterator_t ds_rt_redefine
#define ds_rt_port_config_array_reverse_iterator_t ds_rt_redefine

#ifndef EP_NO_RT_DEPENDENCY
#include DS_RT_TYPES_H
#endif

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_RT_TYPES_H__ */
