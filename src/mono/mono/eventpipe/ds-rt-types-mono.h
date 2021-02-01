// Implementation of ds-rt-types.h targeting Mono runtime.
#ifndef __DIAGNOSTICS_RT_TYPES_MONO_H__
#define __DIAGNOSTICS_RT_TYPES_MONO_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ds-rt-config.h>
#include "ep-rt-types-mono.h"

/*
 * DiagnosticsIpcPollHandle.
 */

#undef ds_rt_ipc_poll_handle_array_t
typedef struct _rt_mono_array_internal_t ds_rt_ipc_poll_handle_array_t;

#undef ds_rt_ipc_poll_handle_array_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ds_rt_ipc_poll_handle_array_iterator_t;

/*
 * DiagnosticsPort.
 */

#undef ds_rt_port_array_t
typedef struct _rt_mono_array_internal_t ds_rt_port_array_t;

#undef ds_rt_port_array_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ds_rt_port_array_iterator_t;

#undef ds_rt_port_config_array_t
typedef struct _rt_mono_array_internal_t ds_rt_port_config_array_t;

#undef ds_rt_port_config_array_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ds_rt_port_config_array_iterator_t;

#undef ds_rt_port_config_array_reverse_iterator_t
typedef struct _rt_mono_array_iterator_internal_t ds_rt_port_config_array_reverse_iterator_t;

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_RT_TYPES_MONO_H__ */
