// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_EVENT_PIPE_H
#define _MONO_COMPONENT_EVENT_PIPE_H

#include <mono/component/component.h>
#include "mono/utils/mono-compiler.h"

#ifndef ENABLE_PERFTRACING
#define ENABLE_PERFTRACING
#endif

#include <eventpipe/ep-ipc-pal-types-forward.h>
#include <eventpipe/ep-types-forward.h>

typedef enum _EventPipeActivityControlCode {
	EP_ACTIVITY_CONTROL_GET_ID = 1,
	EP_ACTIVITY_CONTROL_SET_ID = 2,
	EP_ACTIVITY_CONTROL_CREATE_ID = 3,
	EP_ACTIVITY_CONTROL_GET_SET_ID = 4,
	EP_ACTIVITY_CONTROL_CREATE_SET_ID = 5
} EventPipeActivityControlCode;

typedef struct _EventPipeProviderConfigurationNative EventPipeProviderConfigurationNative;
typedef struct _EventPipeEventInstanceData EventPipeEventInstanceData;
typedef struct _EventPipeSessionInfo EventPipeSessionInfo;

/*
 * EventPipe.
 */

typedef void
(*event_pipe_component_init_func) (void);

typedef void
(*event_pipe_component_finish_init_func) (void);

typedef void
(*event_pipe_component_shutdown_func) (void);

typedef EventPipeSessionID
(*event_pipe_component_enable_func) (
	const ep_char8_t *output_path,
	uint32_t circular_buffer_size_in_mb,
	const EventPipeProviderConfigurationNative *providers,
	uint32_t providers_len,
	EventPipeSessionType session_type,
	EventPipeSerializationFormat format,
	bool rundown_requested,
	IpcStream *stream,
	EventPipeSessionSynchronousCallback sync_callback);

typedef void
(*event_pipe_component_disable_func) (EventPipeSessionID id);

typedef bool
(*event_pipe_component_get_next_event_func) (
	EventPipeSessionID session_id,
	EventPipeEventInstanceData *instance);

typedef EventPipeWaitHandle
(*event_pipe_component_get_wait_handle_func) (EventPipeSessionID session_id);

typedef void
(*event_pipe_component_start_streaming_func) (EventPipeSessionID session_id);

typedef void
(*event_pipe_component_write_event_2_func) (
	EventPipeEvent *ep_event,
	EventData *event_data,
	uint32_t event_data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

typedef bool
(*event_pipe_component_add_rundown_execution_checkpoint_func) (const ep_char8_t *name);

typedef bool
(*event_pipe_component_add_rundown_execution_checkpoint_2_func) (
	const ep_char8_t *name,
	ep_timestamp_t timestamp);

typedef ep_timestamp_t
(*event_pipe_component_convert_100ns_ticks_to_timestamp_t_func) (int64_t ticks_100ns);

typedef bool
(*event_pipe_component_signal_session) (EventPipeSessionID session_id);

typedef bool
(*event_pipe_component_wait_for_session_signal) (
	EventPipeSessionID session_id,
	uint32_t timeout);

/*
 * EventPipeProvider.
 */

typedef EventPipeProvider *
(*event_pipe_component_create_provider_func) (
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	EventPipeCallbackDataFree callback_data_free_func,
	void *callback_data);

typedef void
(*event_pipe_component_delete_provider_func) (EventPipeProvider *provider);

typedef EventPipeProvider *
(*event_pipe_component_get_provider_func) (const ep_char8_t *provider_name);

typedef EventPipeEvent *
(*event_pipe_component_provider_add_event_func) (
	EventPipeProvider *provider,
	uint32_t event_id,
	uint64_t keywords,
	uint32_t event_version,
	EventPipeEventLevel level,
	bool need_stack,
	const uint8_t *metadata,
	uint32_t metadata_len);

/*
 * EventPipeSession.
 */

typedef bool
(*event_pipe_component_get_session_info_func) (
	EventPipeSessionID session_id,
	EventPipeSessionInfo *instance);

/*
 * EventPipeThread.
 */

typedef bool
(*event_pipe_component_thread_ctrl_activity_id_func)(
	EventPipeActivityControlCode activity_control_code,
	uint8_t *activity_id,
	uint32_t activity_id_len);

/*
 * EventPipe Native Events.
 */

typedef bool
(*event_pipe_component_write_event_ee_startup_start_func)(void);

typedef bool
(*event_pipe_component_write_event_threadpool_worker_thread_start_func)(
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id);

typedef bool
(*event_pipe_component_write_event_threadpool_worker_thread_stop_func)(
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id);

typedef bool
(*event_pipe_component_write_event_threadpool_worker_thread_wait_func)(
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id);

typedef bool
(*event_pipe_component_write_event_threadpool_worker_thread_adjustment_sample_func)(
	double throughput,
	uint16_t clr_instance_id);

typedef bool
(*event_pipe_component_write_event_threadpool_worker_thread_adjustment_adjustment_func)(
	double average_throughput,
	uint32_t networker_thread_count,
	/*NativeRuntimeEventSource.ThreadAdjustmentReasonMap*/ int32_t reason,
	uint16_t clr_instance_id);

typedef bool
(*event_pipe_component_write_event_threadpool_worker_thread_adjustment_stats_func)(
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

typedef bool
(*event_pipe_component_write_event_threadpool_io_enqueue_func)(
	intptr_t native_overlapped,
	intptr_t overlapped,
	bool multi_dequeues,
	uint16_t clr_instance_id);

typedef bool
(*event_pipe_component_write_event_threadpool_io_dequeue_func)(
	intptr_t native_overlapped,
	intptr_t overlapped,
	uint16_t clr_instance_id);

typedef bool
(*event_pipe_component_write_event_threadpool_working_thread_count_func)(
	uint16_t count,
	uint16_t clr_instance_id);

typedef bool
(*event_pipe_component_write_event_threadpool_io_pack_func)(
	intptr_t native_overlapped,
	intptr_t overlapped,
	uint16_t clr_instance_id);

/*
 * MonoComponentEventPipe function table.
 */

typedef struct _MonoComponentEventPipe {
	MonoComponent component;
	event_pipe_component_init_func init;
	event_pipe_component_finish_init_func finish_init;
	event_pipe_component_shutdown_func shutdown;
	event_pipe_component_enable_func enable;
	event_pipe_component_disable_func disable;
	event_pipe_component_get_next_event_func get_next_event;
	event_pipe_component_get_wait_handle_func get_wait_handle;
	event_pipe_component_start_streaming_func start_streaming;
	event_pipe_component_write_event_2_func write_event_2;
	event_pipe_component_add_rundown_execution_checkpoint_func add_rundown_execution_checkpoint;
	event_pipe_component_add_rundown_execution_checkpoint_2_func add_rundown_execution_checkpoint_2;
	event_pipe_component_convert_100ns_ticks_to_timestamp_t_func convert_100ns_ticks_to_timestamp_t;
	event_pipe_component_create_provider_func create_provider;
	event_pipe_component_delete_provider_func delete_provider;
	event_pipe_component_get_provider_func get_provider;
	event_pipe_component_provider_add_event_func provider_add_event;
	event_pipe_component_get_session_info_func get_session_info;
	event_pipe_component_thread_ctrl_activity_id_func thread_ctrl_activity_id;
	event_pipe_component_write_event_ee_startup_start_func write_event_ee_startup_start;
	event_pipe_component_write_event_threadpool_worker_thread_start_func write_event_threadpool_worker_thread_start;
	event_pipe_component_write_event_threadpool_worker_thread_stop_func write_event_threadpool_worker_thread_stop;
	event_pipe_component_write_event_threadpool_worker_thread_wait_func write_event_threadpool_worker_thread_wait;
	event_pipe_component_write_event_threadpool_worker_thread_adjustment_sample_func write_event_threadpool_worker_thread_adjustment_sample;
	event_pipe_component_write_event_threadpool_worker_thread_adjustment_adjustment_func write_event_threadpool_worker_thread_adjustment_adjustment;
	event_pipe_component_write_event_threadpool_worker_thread_adjustment_stats_func write_event_threadpool_worker_thread_adjustment_stats;
	event_pipe_component_write_event_threadpool_io_enqueue_func write_event_threadpool_io_enqueue;
	event_pipe_component_write_event_threadpool_io_dequeue_func write_event_threadpool_io_dequeue;
	event_pipe_component_write_event_threadpool_working_thread_count_func write_event_threadpool_working_thread_count;
	event_pipe_component_write_event_threadpool_io_pack_func write_event_threadpool_io_pack;
	event_pipe_component_signal_session signal_session;
	event_pipe_component_wait_for_session_signal wait_for_session_signal;
} MonoComponentEventPipe;

MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentEventPipe *
mono_component_event_pipe_init (void);

#endif /*_MONO_COMPONENT_EVENT_PIPE_H*/
