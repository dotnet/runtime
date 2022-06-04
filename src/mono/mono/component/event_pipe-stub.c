// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include "mono/component/event_pipe.h"
#include "mono/component/event_pipe-wasm.h"
#include "mono/metadata/components.h"

static EventPipeSessionID _dummy_session_id;

static uint8_t _max_event_pipe_type_size [256];

/*
 * Forward declares of all static functions.
 */

static bool
event_pipe_stub_available (void);

static void
event_pipe_stub_init (void);

static void
event_pipe_stub_finish_init (void);

static void
event_pipe_stub_shutdown (void);

static EventPipeSessionID
event_pipe_stub_enable (
	const ep_char8_t *output_path,
	uint32_t circular_buffer_size_in_mb,
	const EventPipeProviderConfigurationNative *providers,
	uint32_t providers_len,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	bool rundown_requested,
	IpcStream *stream,
	EventPipeSessionSynchronousCallback sync_callback);

static void
event_pipe_stub_disable (EventPipeSessionID id);

static bool
event_pipe_stub_get_next_event (
	EventPipeSessionID session_id,
	EventPipeEventInstanceData *instance);

static EventPipeWaitHandle
event_pipe_stub_get_wait_handle (EventPipeSessionID session_id);

static void
event_pipe_stub_start_streaming (EventPipeSessionID session_id);

static void
event_pipe_stub_write_event_2 (
	EventPipeEvent *ep_event,
	EventData *event_data,
	uint32_t event_data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static bool
event_pipe_stub_add_rundown_execution_checkpoint (const ep_char8_t *name);

static bool
event_pipe_stub_add_rundown_execution_checkpoint_2 (
	const ep_char8_t *name,
	ep_timestamp_t timestamp);

static ep_timestamp_t
event_pipe_stub_convert_100ns_ticks_to_timestamp_t (int64_t ticks_100ns);

static EventPipeProvider *
event_pipe_stub_create_provider (
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	EventPipeCallbackDataFree callback_data_free_func,
	void *callback_data);

static void
event_pipe_stub_delete_provider (EventPipeProvider *provider);

static EventPipeProvider *
event_pipe_stub_get_provider (const ep_char8_t *provider_name);

static EventPipeEvent *
event_pipe_stub_provider_add_event (
	EventPipeProvider *provider,
	uint32_t event_id,
	uint64_t keywords,
	uint32_t event_version,
	EventPipeEventLevel level,
	bool need_stack,
	const uint8_t *metadata,
	uint32_t metadata_len);

static bool
event_pipe_stub_get_session_info (
	EventPipeSessionID session_id,
	EventPipeSessionInfo *instance);

static bool
event_pipe_stub_thread_ctrl_activity_id (
	EventPipeActivityControlCode activity_control_code,
	uint8_t *activity_id,
	uint32_t activity_id_len);

static bool
event_pipe_stub_write_event_ee_startup_start (void);

static bool
event_pipe_stub_write_event_threadpool_worker_thread_start (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id);

static bool
event_pipe_stub_write_event_threadpool_worker_thread_stop (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id);

static bool
event_pipe_stub_write_event_threadpool_worker_thread_wait (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id);

static bool
event_pipe_stub_write_event_threadpool_worker_thread_adjustment_sample (
	double throughput,
	uint16_t clr_instance_id);

static bool
event_pipe_stub_write_event_threadpool_worker_thread_adjustment_adjustment (
	double average_throughput,
	uint32_t networker_thread_count,
	/*NativeRuntimeEventSource.ThreadAdjustmentReasonMap*/ int32_t reason,
	uint16_t clr_instance_id);

static bool
event_pipe_stub_write_event_threadpool_worker_thread_adjustment_stats (
	double duration,
	double throughput,
	double threadpool_worker_thread_wait,
	double throughput_wave,
	double throughput_error_estimate,
	double average_throughput_error_estimate,
	double throughput_ratio,
	double confidence,
	double new_control_setting,
	uint16_t new_thread_wave_magnitude,
	uint16_t clr_instance_id);

static bool
event_pipe_stub_write_event_threadpool_io_enqueue (
	intptr_t native_overlapped,
	intptr_t overlapped,
	bool multi_dequeues,
	uint16_t clr_instance_id);

static bool
event_pipe_stub_write_event_threadpool_io_dequeue (
	intptr_t native_overlapped,
	intptr_t overlapped,
	uint16_t clr_instance_id);

static bool
event_pipe_stub_write_event_threadpool_working_thread_count (
	uint16_t count,
	uint16_t clr_instance_id);

static bool
event_pipe_stub_write_event_threadpool_io_pack (
	intptr_t native_overlapped,
	intptr_t overlapped,
	uint16_t clr_instance_id);

static bool
event_pipe_stub_signal_session (EventPipeSessionID session_id);

static bool
event_pipe_stub_wait_for_session_signal (
	EventPipeSessionID session_id,
	uint32_t timeout);

MonoComponentEventPipe *
component_event_pipe_stub_init (void);

static MonoComponentEventPipe fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &event_pipe_stub_available },
	&event_pipe_stub_init,
	&event_pipe_stub_finish_init,
	&event_pipe_stub_shutdown,
	&event_pipe_stub_enable,
	&event_pipe_stub_disable,
	&event_pipe_stub_get_next_event,
	&event_pipe_stub_get_wait_handle,
	&event_pipe_stub_start_streaming,
	&event_pipe_stub_write_event_2,
	&event_pipe_stub_add_rundown_execution_checkpoint,
	&event_pipe_stub_add_rundown_execution_checkpoint_2,
	&event_pipe_stub_convert_100ns_ticks_to_timestamp_t,
	&event_pipe_stub_create_provider,
	&event_pipe_stub_delete_provider,
	&event_pipe_stub_get_provider,
	&event_pipe_stub_provider_add_event,
	&event_pipe_stub_get_session_info,
	&event_pipe_stub_thread_ctrl_activity_id,
	&event_pipe_stub_write_event_ee_startup_start,
	&event_pipe_stub_write_event_threadpool_worker_thread_start,
	&event_pipe_stub_write_event_threadpool_worker_thread_stop,
	&event_pipe_stub_write_event_threadpool_worker_thread_wait,
	&event_pipe_stub_write_event_threadpool_worker_thread_adjustment_sample,
	&event_pipe_stub_write_event_threadpool_worker_thread_adjustment_adjustment,
	&event_pipe_stub_write_event_threadpool_worker_thread_adjustment_stats,
	&event_pipe_stub_write_event_threadpool_io_enqueue,
	&event_pipe_stub_write_event_threadpool_io_dequeue,
	&event_pipe_stub_write_event_threadpool_working_thread_count,
	&event_pipe_stub_write_event_threadpool_io_pack,
	&event_pipe_stub_signal_session,
	&event_pipe_stub_wait_for_session_signal
};

static bool
event_pipe_stub_available (void)
{
	return false;
}

static void
event_pipe_stub_init (void)
{
}

static void
event_pipe_stub_finish_init (void)
{
}

static void
event_pipe_stub_shutdown (void)
{
}

static EventPipeSessionID
event_pipe_stub_enable (
	const ep_char8_t *output_path,
	uint32_t circular_buffer_size_in_mb,
	const EventPipeProviderConfigurationNative *providers,
	uint32_t providers_len,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	bool rundown_requested,
	IpcStream *stream,
	EventPipeSessionSynchronousCallback sync_callback)
{
	return (EventPipeSessionID)&_dummy_session_id;
}

static void
event_pipe_stub_disable (EventPipeSessionID id)
{
}

static bool
event_pipe_stub_get_next_event (
	EventPipeSessionID session_id,
	EventPipeEventInstanceData *instance)
{
	return false;
}

static EventPipeWaitHandle
event_pipe_stub_get_wait_handle (EventPipeSessionID session_id)
{
	return (EventPipeWaitHandle)NULL;
}

static void
event_pipe_stub_start_streaming (EventPipeSessionID session_id)
{
}

static void
event_pipe_stub_write_event_2 (
	EventPipeEvent *ep_event,
	EventData *event_data,
	uint32_t event_data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
}

static bool
event_pipe_stub_add_rundown_execution_checkpoint (const ep_char8_t *name)
{
	return true;
}

static bool
event_pipe_stub_add_rundown_execution_checkpoint_2 (
	const ep_char8_t *name,
	ep_timestamp_t timestamp)
{
	return true;
}

static ep_timestamp_t
event_pipe_stub_convert_100ns_ticks_to_timestamp_t (int64_t ticks_100ns)
{
	return 0;
}

static EventPipeProvider *
event_pipe_stub_create_provider (
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	EventPipeCallbackDataFree callback_data_free_func,
	void *callback_data)
{
	return (EventPipeProvider *)_max_event_pipe_type_size;
}

static void
event_pipe_stub_delete_provider (EventPipeProvider *provider)
{
}

static EventPipeProvider *
event_pipe_stub_get_provider (const ep_char8_t *provider_name)
{
	return NULL;
}

static EventPipeEvent *
event_pipe_stub_provider_add_event (
	EventPipeProvider *provider,
	uint32_t event_id,
	uint64_t keywords,
	uint32_t event_version,
	EventPipeEventLevel level,
	bool need_stack,
	const uint8_t *metadata,
	uint32_t metadata_len)
{
	return (EventPipeEvent *)_max_event_pipe_type_size;
}

static bool
event_pipe_stub_get_session_info (
	EventPipeSessionID session_id,
	EventPipeSessionInfo *instance)
{
	return false;
}

static bool
event_pipe_stub_thread_ctrl_activity_id (
	EventPipeActivityControlCode activity_control_code,
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	return false;
}

static bool
event_pipe_stub_write_event_ee_startup_start (void)
{
	return true;
}

static bool
event_pipe_stub_write_event_threadpool_worker_thread_start (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id)
{
	return true;
}

static bool
event_pipe_stub_write_event_threadpool_worker_thread_stop (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id)
{
	return true;
}

static bool
event_pipe_stub_write_event_threadpool_worker_thread_wait (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id)
{
	return true;
}

static bool
event_pipe_stub_write_event_threadpool_worker_thread_adjustment_sample (
	double throughput,
	uint16_t clr_instance_id)
{
	return true;
}

static bool
event_pipe_stub_write_event_threadpool_worker_thread_adjustment_adjustment (
	double average_throughput,
	uint32_t networker_thread_count,
	/*NativeRuntimeEventSource.ThreadAdjustmentReasonMap*/ int32_t reason,
	uint16_t clr_instance_id)
{
	return true;
}

static bool
event_pipe_stub_write_event_threadpool_worker_thread_adjustment_stats (
	double duration,
	double throughput,
	double threadpool_worker_thread_wait,
	double throughput_wave,
	double throughput_error_estimate,
	double average_throughput_error_estimate,
	double throughput_ratio,
	double confidence,
	double new_control_setting,
	uint16_t new_thread_wave_magnitude,
	uint16_t clr_instance_id)
{
	return true;
}

static bool
event_pipe_stub_write_event_threadpool_io_enqueue (
	intptr_t native_overlapped,
	intptr_t overlapped,
	bool multi_dequeues,
	uint16_t clr_instance_id)
{
	return true;
}

static bool
event_pipe_stub_write_event_threadpool_io_dequeue (
	intptr_t native_overlapped,
	intptr_t overlapped,
	uint16_t clr_instance_id)
{
	return true;
}

static bool
event_pipe_stub_write_event_threadpool_working_thread_count (
	uint16_t count,
	uint16_t clr_instance_id)
{
	return true;
}

static bool
event_pipe_stub_write_event_threadpool_io_pack (
	intptr_t native_overlapped,
	intptr_t overlapped,
	uint16_t clr_instance_id)
{
	return true;
}

static bool
event_pipe_stub_signal_session (EventPipeSessionID session_id)
{
	return true;
}

static bool
event_pipe_stub_wait_for_session_signal (
	EventPipeSessionID session_id,
	uint32_t timeout)
{
	return true;
}

MonoComponentEventPipe *
component_event_pipe_stub_init (void)
{
	return &fn_table;
}

MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentEventPipe *
mono_component_event_pipe_init (void)
{
	return component_event_pipe_stub_init ();
}

#ifdef HOST_WASM

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_event_pipe_enable (const ep_char8_t *output_path,
			     uint32_t circular_buffer_size_in_mb,
			     const ep_char8_t *providers,
			     /* EventPipeSessionType session_type = EP_SESSION_TYPE_FILE, */
			     /* EventPipieSerializationFormat format = EP_SERIALIZATION_FORMAT_NETTRACE_V4, */
			     /* bool */ gboolean rundown_requested,
			     /* IpcStream stream = NULL, */
			     /* EventPipeSessionSycnhronousCallback sync_callback = NULL, */
			     /* void *callback_additional_data, */
			     MonoWasmEventPipeSessionID *out_session_id)
{
	if (out_session_id)
		*out_session_id = 0;
	return 0;
}


EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_event_pipe_session_start_streaming (MonoWasmEventPipeSessionID session_id)
{
	g_assert_not_reached ();
}

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_event_pipe_session_disable (MonoWasmEventPipeSessionID session_id)
{
	g_assert_not_reached ();
}

#endif /* HOST_WASM */
