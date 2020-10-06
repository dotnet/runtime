#ifndef __EVENTPIPE_PROVIDER_INTERNALS_H__
#define __EVENTPIPE_PROVIDER_INTERNALS_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
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

void
provider_invoke_callback (EventPipeProviderCallbackData *provider_callback_data);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_PROVIDER_INTERNALS_H__ */
