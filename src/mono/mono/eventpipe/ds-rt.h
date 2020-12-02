#ifndef __DIAGNOSTICS_RT_H__
#define __DIAGNOSTICS_RT_H__

#include <config.h>

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

#define DS_ENTER_BLOCKING_PAL_SECTION ds_rt_redefine
#define DS_EXIT_BLOCKING_PAL_SECTION ds_rt_redefine

#define DS_RT_DECLARE_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DECLARE_ARRAY_PREFIX(ds, array_name, array_type, iterator_type, item_type)

#define DS_RT_DECLARE_ARRAY_ITERATOR(array_name, array_type, iterator_type, item_type) \
	EP_RT_DECLARE_ARRAY_ITERATOR_PREFIX(ds, array_name, array_type, iterator_type, item_type)

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
int32_t
ds_rt_config_value_get_default_port_suspend (void);

/*
 * DiagnosticsIpc.
 */

static
void
ds_rt_transport_get_default_name (
	ep_char8_t *name,
	int32_t name_len,
	const ep_char8_t *prefix,
	int32_t id,
	const ep_char8_t *group_id,
	const ep_char8_t *suffix);

/*
 * DiagnosticsIpcPollHandle.
 */

DS_RT_DECLARE_ARRAY (ipc_poll_handle_array, ds_rt_ipc_poll_handle_array_t, ds_rt_ipc_poll_handle_array_iterator_t, DiagnosticsIpcPollHandle)
DS_RT_DECLARE_ARRAY_ITERATOR (ipc_poll_handle_array, ds_rt_ipc_poll_handle_array_t, ds_rt_ipc_poll_handle_array_iterator_t, DiagnosticsIpcPollHandle)

/*
 * DiagnosticsPort.
 */

DS_RT_DECLARE_ARRAY (port_array, ds_rt_port_array_t, ds_rt_port_array_iterator_t, DiagnosticsPort *)
DS_RT_DECLARE_ARRAY_ITERATOR (port_array, ds_rt_port_array_t, ds_rt_port_array_iterator_t, DiagnosticsPort *)

DS_RT_DECLARE_ARRAY (port_config_array, ds_rt_port_config_array_t, ds_rt_port_array_iterator_t, ep_char8_t *)
DS_RT_DECLARE_ARRAY_ITERATOR (port_config_array, ds_rt_port_config_array_t, ds_rt_port_array_iterator_t, ep_char8_t *)

#include "ds-rt-mono.h"

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_RT_H__ */
