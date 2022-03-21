// Implementation of ds-rt-types.h targeting CoreCLR runtime.
#ifndef __DIAGNOSTICS_RT_TYPES_CORECLR_H__
#define __DIAGNOSTICS_RT_TYPES_CORECLR_H__

#include <eventpipe/ds-rt-config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-types-coreclr.h"

/*
 * DiagnosticsIpcPollHandle.
 */

#undef ds_rt_ipc_poll_handle_array_t
typedef struct _rt_coreclr_array_internal_t<DiagnosticsIpcPollHandle> ds_rt_ipc_poll_handle_array_t;

#undef ds_rt_ipc_poll_handle_array_iterator_t
typedef struct _rt_coreclr_array_iterator_internal_t<DiagnosticsIpcPollHandle> ds_rt_ipc_poll_handle_array_iterator_t;

/*
 * DiagnosticsPort.
 */

#undef ds_rt_port_array_t
typedef struct _rt_coreclr_array_internal_t<DiagnosticsPort *> ds_rt_port_array_t;

#undef ds_rt_port_array_iterator_t
typedef struct _rt_coreclr_array_iterator_internal_t<DiagnosticsPort *> ds_rt_port_array_iterator_t;

#undef ds_rt_port_config_array_t
typedef struct _rt_coreclr_array_internal_t<ep_char8_t *> ds_rt_port_config_array_t;

#undef ds_rt_port_config_array_iterator_t
typedef struct _rt_coreclr_array_iterator_internal_t<ep_char8_t *> ds_rt_port_config_array_iterator_t;

#undef ds_rt_port_config_array_reverse_iterator_t
typedef struct _rt_coreclr_array_iterator_internal_t<ep_char8_t *> ds_rt_port_config_array_reverse_iterator_t;

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_RT_TYPES_CORECLR_H__ */
