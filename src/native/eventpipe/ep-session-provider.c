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
DN_CALLBACK_CALLTYPE
session_provider_free_func (void *session_provider);

static
bool
DN_CALLBACK_CALLTYPE
session_provider_compare_name_func (
	const void *a,
	const void *b);

static
bool
ep_event_filter_allows_event_id (
	const EventPipeProviderEventFilter *event_filter,
	uint32_t event_id);

/*
 * EventPipeSessionProvider.
 */

static
void
DN_CALLBACK_CALLTYPE
session_provider_free_func (void *session_provider)
{
	ep_session_provider_free ((EventPipeSessionProvider *)session_provider);
}

static
bool
DN_CALLBACK_CALLTYPE
session_provider_compare_name_func (
	const void *a,
	const void *b)
{
	return (a) ? !ep_rt_utf8_string_compare (ep_session_provider_get_provider_name ((EventPipeSessionProvider *)a), (const ep_char8_t *)b) : false;
}

EventPipeSessionProvider *
ep_session_provider_alloc (
	const ep_char8_t *provider_name,
	uint64_t keywords,
	EventPipeEventLevel logging_level,
	const ep_char8_t *filter_data,
	EventPipeProviderEventFilter *event_filter,
	EventPipeProviderTracepointConfiguration *tracepoint_config)
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
	instance->event_filter = event_filter;
	instance->tracepoint_config = tracepoint_config;

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

	ep_tracepoint_config_free (session_provider->tracepoint_config);
	ep_event_filter_free (session_provider->event_filter);
	ep_rt_utf8_string_free (session_provider->filter_data);
	ep_rt_utf8_string_free (session_provider->provider_name);
	ep_rt_object_free (session_provider);
}

static
bool
ep_event_filter_allows_event_id (
	const EventPipeProviderEventFilter *event_filter,
	uint32_t event_id)
{
	if (event_filter == NULL)
		return true;

	if (event_filter->event_ids == NULL)
		return !event_filter->enable;

	return event_filter->enable == dn_umap_contains (event_filter->event_ids, &event_id);
}

bool
ep_session_provider_allows_event (
	const EventPipeSessionProvider *session_provider,
	const EventPipeEvent *ep_event)
{
	EP_ASSERT(session_provider != NULL);

	uint64_t keywords = ep_event_get_keywords (ep_event);
	uint64_t session_keywords = ep_session_provider_get_keywords(session_provider);
	if ((keywords != 0) && ((session_keywords & keywords) == 0))
		return false;

	EventPipeEventLevel event_level = ep_event_get_level (ep_event);
	EventPipeEventLevel session_level = ep_session_provider_get_logging_level(session_provider);
	if ((event_level != EP_EVENT_LEVEL_LOGALWAYS) &&
		(session_level != EP_EVENT_LEVEL_LOGALWAYS) &&
		(session_level < event_level))
		return false;

	uint32_t event_id = ep_event_get_event_id (ep_event);
	EventPipeProviderEventFilter *event_filter = ep_session_provider_get_event_filter (session_provider);
	return ep_event_filter_allows_event_id (event_filter, event_id);
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

	instance->providers = dn_list_alloc ();
	ep_raise_error_if_nok (instance->providers != NULL);

	instance->catch_all_provider = NULL;

	for (uint32_t i = 0; i < configs_len; ++i) {
		const EventPipeProviderConfiguration *config = &configs [i];
		EP_ASSERT (config != NULL);

		// Enable all events if the provider name == '*', all keywords are on and the requested level == verbose.
		if ((ep_rt_utf8_string_compare(ep_provider_get_wildcard_name_utf8 (), ep_provider_config_get_provider_name (config)) == 0) &&
			(ep_provider_config_get_keywords (config) == 0xFFFFFFFFFFFFFFFF) &&
			((ep_provider_config_get_logging_level (config) == EP_EVENT_LEVEL_VERBOSE) && (instance->catch_all_provider == NULL))) {
			instance->catch_all_provider = ep_session_provider_alloc (NULL, 0xFFFFFFFFFFFFFFFF, EP_EVENT_LEVEL_VERBOSE, NULL, NULL, NULL);
			ep_raise_error_if_nok (instance->catch_all_provider != NULL);
		}
		else {
			EventPipeSessionProvider * session_provider = ep_session_provider_alloc (
				ep_provider_config_get_provider_name (config),
				ep_provider_config_get_keywords (config),
				ep_provider_config_get_logging_level (config),
				ep_provider_config_get_filter_data (config),
				ep_provider_config_get_event_filter (config),
				ep_provider_config_get_tracepoint_config (config));
			ep_raise_error_if_nok (dn_list_push_back (instance->providers, session_provider));
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

	dn_list_custom_free (session_provider_list->providers, session_provider_free_func);
	ep_session_provider_free (session_provider_list->catch_all_provider);
	ep_rt_object_free (session_provider_list);
}

void
ep_session_provider_list_clear (EventPipeSessionProviderList *session_provider_list)
{
	EP_ASSERT (session_provider_list != NULL);
	dn_list_custom_clear (session_provider_list->providers, session_provider_free_func);
}

bool
ep_session_provider_list_is_empty (const EventPipeSessionProviderList *session_provider_list)
{
	EP_ASSERT (session_provider_list != NULL);
	return (dn_list_empty (session_provider_list->providers) && session_provider_list->catch_all_provider == NULL);
}

bool
ep_session_provider_list_add_session_provider (
	EventPipeSessionProviderList *session_provider_list,
	EventPipeSessionProvider *session_provider)
{
	EP_ASSERT (session_provider_list != NULL);
	EP_ASSERT (session_provider != NULL);

	return dn_list_push_back (session_provider_list->providers, session_provider);
}

EventPipeSessionProvider *
ep_session_provider_list_find_by_name (
	dn_list_t *list,
	const ep_char8_t *name)
{
	dn_list_it_t found = dn_list_custom_find (list, name, session_provider_compare_name_func);
	return (!dn_list_it_end (found)) ? *dn_list_it_data_t(found, EventPipeSessionProvider *) : NULL;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_session_provider;
const char quiet_linker_empty_file_warning_eventpipe_session_provider = 0;
#endif
