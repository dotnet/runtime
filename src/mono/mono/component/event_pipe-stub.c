// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include "mono/component/event_pipe.h"
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
	&event_pipe_stub_create_provider,
	&event_pipe_stub_delete_provider,
	&event_pipe_stub_get_provider,
	&event_pipe_stub_provider_add_event,
	&event_pipe_stub_get_session_info,
	&event_pipe_stub_thread_ctrl_activity_id,
	&event_pipe_stub_write_event_ee_startup_start
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
