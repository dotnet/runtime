#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#include "ep.h"

/*
 * Forward declares of all static functions.
 */

static
void
session_disable_ipc_streaming_thread (EventPipeSession *session);

static
void
session_create_ipc_streaming_thread_lock_held (EventPipeSession *session);

/*
 * EventPipeSession.
 */

static
void
session_create_ipc_streaming_thread_lock_held (EventPipeSession *session)
{
	//TODO: Implement.
}

static
void
session_disable_ipc_streaming_thread (EventPipeSession *session)
{
	EP_ASSERT (ep_session_get_session_type (session) == EP_SESSION_TYPE_IPCSTREAM);
	EP_ASSERT (ep_session_get_ipc_streaming_enabled (session));

	EP_ASSERT (!ep_rt_process_detach ());

	// The IPC streaming thread will watch this value and exit
	// when profiling is disabled.
	ep_session_set_ipc_streaming_enabled (session, false);

	// Thread could be waiting on the event that there is new data to read.
	ep_rt_wait_event_set (ep_buffer_manager_get_rt_wait_event_ref (ep_session_get_buffer_manager (session)));

	// Wait for the sampling thread to clean itself up.
	ep_rt_wait_event_handle_t *rt_thread_shutdown_event = ep_session_get_rt_thread_shutdown_event_ref (session);
	ep_rt_wait_event_wait (rt_thread_shutdown_event, EP_INFINITE_WAIT, false /* bAlertable */);
	ep_rt_wait_event_free (rt_thread_shutdown_event);
}

EventPipeSessionProvider *
ep_session_get_session_provider_lock_held (
	const EventPipeSession *session,
	const EventPipeProvider *provider)
{
	ep_rt_config_requires_lock_held ();

	ep_return_null_if_nok (session != NULL && provider != NULL);

	EventPipeSessionProviderList *providers = ep_session_get_providers (session);
	ep_return_null_if_nok (providers != NULL);

	EventPipeSessionProvider *catch_all = ep_session_provider_list_get_catch_all_provider (providers);
	if (catch_all)
		return catch_all;

	EventPipeSessionProvider *session_provider = ep_rt_session_provider_list_find_by_name (ep_session_provider_list_get_providers_ref (providers), ep_provider_get_provider_name (provider));

	ep_rt_config_requires_lock_held ();
	return session_provider;
}

void
ep_session_enable_rundown_lock_held (EventPipeSession *session)
{
	ep_rt_config_requires_lock_held ();

	ep_return_void_if_nok (session != NULL);

	//TODO: This is CoreCLR specific keywords for native ETW events (ending up in event pipe).
	//! The keywords below seems to correspond to:
	//!  LoaderKeyword                      (0x00000008)
	//!  JitKeyword                         (0x00000010)
	//!  NgenKeyword                        (0x00000020)
	//!  unused_keyword                     (0x00000100)
	//!  JittedMethodILToNativeMapKeyword   (0x00020000)
	//!  ThreadTransferKeyword              (0x80000000)
	const uint64_t keywords = 0x80020138;
	const uint32_t verbose_logging_level = (uint32_t)EP_EVENT_LEVEL_VERBOSE;

	EventPipeProviderConfiguration rundown_providers [2];
	uint32_t rundown_providers_len = EP_ARRAY_SIZE (rundown_providers);

	ep_provider_config_init (&rundown_providers [0], ep_config_get_public_provider_name_utf8 (), keywords, verbose_logging_level, NULL); // Public provider.
	ep_provider_config_init (&rundown_providers [1], ep_config_get_rundown_provider_name_utf8 (), keywords, verbose_logging_level, NULL); // Rundown provider.

	//TODO: This is CoreCLR specific provider.
	// update the provider context here since the callback doesn't happen till we actually try to do rundown.
	//MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.Level = VerboseLoggingLevel;
	//MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.EnabledKeywordsBitmask = Keywords;
	//MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled = true;

	// Update provider list with rundown configuration.
	for (uint32_t i = 0; i < rundown_providers_len; ++i) {
		const EventPipeProviderConfiguration *config = &rundown_providers [i];

		EventPipeSessionProvider *session_provider = ep_session_provider_alloc (
			ep_provider_config_get_provider_name (config),
			ep_provider_config_get_keywords (config),
			ep_provider_config_get_logging_level (config),
			ep_provider_config_get_filter_data (config));

		ep_session_add_session_provider (session, session_provider);
	}

	ep_session_set_rundown_enabled (session, true);

	ep_rt_config_requires_lock_held ();
	return;
}

void
ep_session_execute_rundown_lock_held (EventPipeSession *session)
{
	//TODO: Implement. This is mainly runtime specific implementation
	//since it will emit native trace events into the pipe (using CoreCLR's ETW support).
}

void
ep_session_suspend_write_event_lock_held (EventPipeSession *session)
{
	//TODO: Implement.
}

void
ep_session_write_sequence_point_unbuffered_lock_held (EventPipeSession *session)
{
	//TODO: Implement.
}

void
ep_session_start_streaming_lock_held (EventPipeSession *session)
{
	ep_rt_config_requires_lock_held ();

	ep_return_void_if_nok (session != NULL);

	if (ep_session_get_file (session) != NULL)
		ep_file_initialize_file (ep_session_get_file (session));

	if (ep_session_get_session_type (session) == EP_SESSION_TYPE_IPCSTREAM)
		session_create_ipc_streaming_thread_lock_held (session);

	ep_rt_config_requires_lock_held ();
	return;
}

bool
ep_session_is_valid (const EventPipeSession *session)
{
	return !ep_session_provider_list_is_empty (ep_session_get_providers (session));
}

void
ep_session_add_session_provider (EventPipeSession *session, EventPipeSessionProvider *session_provider)
{
	ep_return_void_if_nok (session != NULL);
	ep_session_provider_list_add_session_provider (ep_session_get_providers (session), session_provider);
}

void
ep_session_disable (EventPipeSession *session)
{
	ep_return_void_if_nok (session != NULL);
	if (ep_session_get_session_type (session) == EP_SESSION_TYPE_IPCSTREAM && ep_session_get_ipc_streaming_enabled (session))
		session_disable_ipc_streaming_thread (session);

	bool ignored;
	ep_session_write_all_buffers_to_file (session, &ignored);
	ep_session_provider_list_clear (ep_session_get_providers (session));
}

bool
ep_session_write_all_buffers_to_file (EventPipeSession *session, bool *events_written)
{
	//TODO: Implement.
	*events_written = false;
	return true;
}

EventPipeEventInstance *
ep_session_get_next_event (EventPipeSession *session)
{
	//TODO: Implement.
	return NULL;
}

EventPipeWaitHandle
ep_session_get_wait_event (EventPipeSession *session)
{
	ep_raise_error_if_nok (session != NULL);

	EventPipeBufferManager *buffer_manager = ep_session_get_buffer_manager (session);
	ep_raise_error_if_nok (buffer_manager != NULL);

	return ep_rt_wait_event_get_wait_handle (ep_buffer_manager_get_rt_wait_event_ref (buffer_manager));

ep_on_error:
	return 0;
}

uint64_t
ep_session_get_mask (const EventPipeSession *session)
{
	return ((uint64_t)1 << ep_session_get_index (session));
}

bool
ep_session_get_rundown_enabled (const EventPipeSession *session)
{
	return (ep_rt_volatile_load_uint32_t(ep_session_get_rundown_enabled_cref (session)) ? true : false);
}

void
ep_session_set_rundown_enabled (
	EventPipeSession *session,
	bool enabled)
{
	ep_rt_volatile_store_uint32_t (ep_session_get_rundown_enabled_ref (session), (enabled) ? 1 : 0);
}

bool
ep_session_get_ipc_streaming_enabled (const EventPipeSession *session)
{
	return (ep_rt_volatile_load_uint32_t(ep_session_get_ipc_streaming_enabled_cref (session)) ? true : false);
}

void
ep_session_set_ipc_streaming_enabled (
	EventPipeSession *session,
	bool enabled)
{
	ep_rt_volatile_store_uint32_t (ep_session_get_ipc_streaming_enabled_ref (session), (enabled) ? 1 : 0);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_session;
const char quiet_linker_empty_file_warning_eventpipe_session = 0;
#endif
