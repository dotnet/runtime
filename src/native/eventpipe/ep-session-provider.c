#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_SESSION_PROVIDER_GETTER_SETTER
#include "ep-session-provider.h"
#include "ep-rt.h"

/*
 * Forward declares of all static functions.
 */

static
void
session_provider_free_func (void *session_provider);

/*
 * EventPipeSessionProvider.
 */

static
void
session_provider_free_func (void *session_provider)
{
	ep_session_provider_free ((EventPipeSessionProvider *)session_provider);
}

EventPipeSessionProvider *
ep_session_provider_alloc (
	const ep_char8_t *provider_name,
	uint64_t keywords,
	EventPipeEventLevel logging_level,
	const ep_char8_t *filter_data)
{
	EventPipeSessionProvider *instance = ep_rt_object_alloc (EventPipeSessionProvider);
	ep_raise_error_if_nok (instance != NULL);

	if (provider_name) {
		instance->provider_name = ep_rt_utf8_string_dup (provider_name);
		ep_raise_error_if_nok (instance->provider_name != NULL);
	}

	if (filter_data) {
		instance->filter_data = ep_rt_utf8_string_dup (filter_data);
		ep_raise_error_if_nok (instance->filter_data != NULL);
	}

	instance->keywords = keywords;
	instance->logging_level = logging_level;

ep_on_exit:
	return instance;

ep_on_error:
	ep_session_provider_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_session_provider_free (EventPipeSessionProvider * session_provider)
{
	ep_return_void_if_nok (session_provider != NULL);

	ep_rt_utf8_string_free (session_provider->filter_data);
	ep_rt_utf8_string_free (session_provider->provider_name);
	ep_rt_object_free (session_provider);
}

/*
 * EventPipeSessionProviderList.
 */

EventPipeSessionProviderList *
ep_session_provider_list_alloc (
	const EventPipeProviderConfiguration *configs,
	uint32_t configs_len)
{
	ep_return_null_if_nok ((configs_len == 0) || (configs_len > 0 && configs != NULL));

	EventPipeSessionProviderList *instance = ep_rt_object_alloc (EventPipeSessionProviderList);
	ep_raise_error_if_nok (instance != NULL);

	ep_rt_session_provider_list_alloc (&instance->providers);
	ep_raise_error_if_nok (ep_rt_session_provider_list_is_valid (&instance->providers));

	instance->catch_all_provider = NULL;

	for (uint32_t i = 0; i < configs_len; ++i) {
		const EventPipeProviderConfiguration *config = &configs [i];
		EP_ASSERT (config != NULL);

		// Enable all events if the provider name == '*', all keywords are on and the requested level == verbose.
		if ((ep_rt_utf8_string_compare(ep_provider_get_wildcard_name_utf8 (), ep_provider_config_get_provider_name (config)) == 0) &&
			(ep_provider_config_get_keywords (config) == 0xFFFFFFFFFFFFFFFF) &&
			((ep_provider_config_get_logging_level (config) == EP_EVENT_LEVEL_VERBOSE) && (instance->catch_all_provider == NULL))) {
			instance->catch_all_provider = ep_session_provider_alloc (NULL, 0xFFFFFFFFFFFFFFFF, EP_EVENT_LEVEL_VERBOSE, NULL );
			ep_raise_error_if_nok (instance->catch_all_provider != NULL);
		}
		else {
			EventPipeSessionProvider * session_provider = ep_session_provider_alloc (
				ep_provider_config_get_provider_name (config),
				ep_provider_config_get_keywords (config),
				ep_provider_config_get_logging_level (config),
				ep_provider_config_get_filter_data (config));
			ep_raise_error_if_nok (ep_rt_session_provider_list_append (&instance->providers, session_provider));
		}
	}

ep_on_exit:
	return instance;

ep_on_error:
	ep_session_provider_list_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_session_provider_list_free (EventPipeSessionProviderList *session_provider_list)
{
	ep_return_void_if_nok (session_provider_list != NULL);

	ep_rt_session_provider_list_free (&session_provider_list->providers, session_provider_free_func);
	ep_session_provider_free (session_provider_list->catch_all_provider);
	ep_rt_object_free (session_provider_list);
}

void
ep_session_provider_list_clear (EventPipeSessionProviderList *session_provider_list)
{
	EP_ASSERT (session_provider_list != NULL);
	ep_rt_session_provider_list_clear (&session_provider_list->providers, session_provider_free_func);
}

bool
ep_session_provider_list_is_empty (const EventPipeSessionProviderList *session_provider_list)
{
	EP_ASSERT (session_provider_list != NULL);
	return (ep_rt_session_provider_list_is_empty (&session_provider_list->providers) && session_provider_list->catch_all_provider == NULL);
}

bool
ep_session_provider_list_add_session_provider (
	EventPipeSessionProviderList *session_provider_list,
	EventPipeSessionProvider *session_provider)
{
	EP_ASSERT (session_provider_list != NULL);
	EP_ASSERT (session_provider != NULL);

	return ep_rt_session_provider_list_append (&session_provider_list->providers, session_provider);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_session_provider;
const char quiet_linker_empty_file_warning_eventpipe_session_provider = 0;
#endif
