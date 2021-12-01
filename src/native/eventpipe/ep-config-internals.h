#ifndef __EVENTPIPE_CONFIGURATION_INTERNALS_H__
#define __EVENTPIPE_CONFIGURATION_INTERNALS_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

/*
 * EventPipeConfiguration internal library functions.
 */

// _Requires_lock_held (config)
EventPipeSessionProvider *
config_get_session_provider (
	const EventPipeConfiguration *config,
	const EventPipeSession *session,
	const EventPipeProvider *provider);

// _Requires_lock_held (config)
EventPipeProvider *
config_get_provider (
	EventPipeConfiguration *config,
	const ep_char8_t *name);

// _Requires_lock_held (config)
EventPipeProvider *
config_create_provider (
	EventPipeConfiguration *config,
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	EventPipeCallbackDataFree callback_data_free_func,
	void *callback_data,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

// _Requires_lock_held (config)
void
config_delete_provider (
	EventPipeConfiguration *config,
	EventPipeProvider *provider);

// _Requires_lock_held (config)
void
config_delete_deferred_providers (EventPipeConfiguration *config);

// _Requires_lock_held (config)
void
config_enable_disable (
	EventPipeConfiguration *config,
	const EventPipeSession *session,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue,
	bool enable);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_CONFIGURATION_INTERNALS_H__ */
