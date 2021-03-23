#ifndef __EVENTPIPE_PROVIDER_INTERNALS_H__
#define __EVENTPIPE_PROVIDER_INTERNALS_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

/*
 * EventPipeProvider internal library functions.
 */

// Set the provider configuration (enable sets of events).
// _Requires_lock_held (ep)
const EventPipeProviderCallbackData *
provider_set_config (
	EventPipeProvider *provider,
	int64_t keywords_for_all_sessions,
	EventPipeEventLevel level_for_all_sessions,
	uint64_t session_mask,
	int64_t keywords,
	EventPipeEventLevel level,
	const ep_char8_t *filter_data,
	EventPipeProviderCallbackData *callback_data);

// Unset the provider configuration for the specified session (disable sets of events).
// _Requires_lock_held (ep)
const EventPipeProviderCallbackData *
provider_unset_config (
	EventPipeProvider *provider,
	int64_t keywords_for_all_sessions,
	EventPipeEventLevel level_for_all_sessions,
	uint64_t session_mask,
	int64_t keywords,
	EventPipeEventLevel level,
	const ep_char8_t *filter_data,
	EventPipeProviderCallbackData *callback_data);

// _Requires_lock_not_held (ep)
void
provider_invoke_callback (EventPipeProviderCallbackData *provider_callback_data);

// Create and register provider.
// _Requires_lock_held (ep)
EventPipeProvider *
provider_create_register (
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	EventPipeCallbackDataFree callback_data_free_func,
	void *callback_data,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

// Unregister and delete provider.
// _Requires_lock_held (ep)
void
provider_unregister_delete (EventPipeProvider *provider);

// Free provider.
// _Requires_lock_held (ep)
void
provider_free (EventPipeProvider *provider);

// Add event.
// _Requires_lock_held (ep)
EventPipeEvent *
provider_add_event (
	EventPipeProvider *provider,
	uint32_t event_id,
	uint64_t keywords,
	uint32_t event_version,
	EventPipeEventLevel level,
	bool need_stack,
	const uint8_t *metadata,
	uint32_t metadata_len);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_PROVIDER_INTERNALS_H__ */
