#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_GETTER_SETTER
#include "ep.h"

/*
 * Forward declares of all static functions.
 */

static
void
event_free_func (void *ep_event);

/*
 * EventPipeProvider.
 */

static
void
event_free_func (void *ep_event)
{
	ep_event_free ((EventPipeEvent *)ep_event);
}

EventPipeProvider *
ep_provider_alloc (
	EventPipeConfiguration *config,
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	void *callback_data)
{
	ep_return_false_if_nok (config != NULL && provider_name != NULL);

	EventPipeProvider *instance = ep_rt_object_alloc (EventPipeProvider);
	ep_raise_error_if_nok (instance != NULL);

	instance->provider_name = ep_rt_utf8_string_dup (provider_name);
	ep_raise_error_if_nok (instance->provider_name != NULL);

	instance->provider_name_utf16 = ep_rt_utf8_to_utf16_string (provider_name, ep_rt_utf8_string_len (provider_name));
	ep_raise_error_if_nok (instance->provider_name_utf16 != NULL);

	instance->keywords = 0;
	instance->provider_level = EP_EVENT_LEVEL_CRITICAL;
	instance->callback_func = callback_func;
	instance->callback_data = callback_data;
	instance->config = config;
	instance->delete_deferred = false;
	instance->sessions = 0;

ep_on_exit:
	return instance;

ep_on_error:
	ep_provider_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_provider_free (EventPipeProvider * provider)
{
	ep_return_void_if_nok (provider != NULL);

	//TODO: CoreCLR takes the lock before manipulating the list, but since complete object is
	// going away and list is only owned by provider, meaning that if we had a race related
	// to the list, it will crash anyways once lock is released and list is gone.
	ep_rt_event_list_free (&provider->event_list, event_free_func);

	ep_rt_utf16_string_free (provider->provider_name_utf16);
	ep_rt_utf8_string_free (provider->provider_name);
	ep_rt_object_free (provider);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_provider_internals;
const char quiet_linker_empty_file_warning_eventpipe_provider_internals = 0;
#endif
