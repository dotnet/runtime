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
void
session_provider_event_filter_free (EventPipeSessionProviderEventFilter *event_filter);

static
EventPipeSessionProviderEventFilter *
session_provider_event_filter_alloc (const EventPipeProviderEventFilter *event_filter);

static
void
DN_CALLBACK_CALLTYPE
tracepoint_free_func (void *tracepoint);

static
void
session_provider_tracepoint_config_free (EventPipeSessionProviderTracepointConfiguration *tracepoint_config);

static
ep_char8_t *
tracepoint_format_alloc (const ep_char8_t *tracepoint_name);

static
EventPipeSessionProviderTracepointConfiguration *
session_provider_tracepoint_config_alloc (const EventPipeProviderTracepointConfiguration *tracepoint_config);

static
bool
event_filter_allows_event_id (
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

static
void
session_provider_event_filter_free (EventPipeSessionProviderEventFilter *event_filter)
{
	ep_return_void_if_nok (event_filter != NULL);

	if (event_filter->event_ids != NULL) {
		dn_umap_free (event_filter->event_ids);
		event_filter->event_ids = NULL;
	}

	ep_rt_object_free (event_filter);
}

static
EventPipeSessionProviderEventFilter *
session_provider_event_filter_alloc (const EventPipeProviderEventFilter *event_filter)
{
	EP_ASSERT (event_filter != NULL);

	EventPipeSessionProviderEventFilter *instance = ep_rt_object_alloc (EventPipeSessionProviderEventFilter);
	ep_raise_error_if_nok (instance != NULL);

	instance->enable = event_filter->enable;

	instance->event_ids = NULL;
	if (event_filter->length > 0) {
		instance->event_ids = dn_umap_alloc ();
		ep_raise_error_if_nok (instance->event_ids != NULL);

		for (uint32_t i = 0; i < event_filter->length; ++i) {
			dn_umap_result_t insert_result = dn_umap_ptr_uint32_insert (instance->event_ids, (void *)(uintptr_t)event_filter->event_ids[i], 0);
			ep_raise_error_if_nok (insert_result.result);
		}
	}

ep_on_exit:
	return instance;

ep_on_error:
	session_provider_event_filter_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

static
void
DN_CALLBACK_CALLTYPE
tracepoint_free_func (void *tracepoint)
{
	EventPipeTracepoint *tp = *(EventPipeTracepoint **)tracepoint;
	ep_rt_utf8_string_free ((ep_char8_t *)tp->tracepoint_format);
	ep_rt_object_free (tp);
}

static
void
session_provider_tracepoint_config_free (EventPipeSessionProviderTracepointConfiguration *tracepoint_config)
{
	ep_return_void_if_nok (tracepoint_config != NULL);

	if (tracepoint_config->event_id_to_tracepoint_map) {
		dn_umap_free (tracepoint_config->event_id_to_tracepoint_map);
		tracepoint_config->event_id_to_tracepoint_map = NULL;
	}

	if (tracepoint_config->tracepoints) {
		dn_vector_ptr_custom_free (tracepoint_config->tracepoints, tracepoint_free_func);
		tracepoint_config->tracepoints = NULL;
	}

	ep_rt_object_free (tracepoint_config);
}

static
ep_char8_t *
tracepoint_format_alloc (const ep_char8_t *tracepoint_name)
{
	ep_return_null_if_nok (tracepoint_name != NULL && tracepoint_name[0] != '\0');

	int32_t res = 0;
	size_t tracepoint_format_len = strlen(tracepoint_name) + strlen(EP_TRACEPOINT_FORMAT_V1) + 2; // +2 for space and null terminator
	ep_char8_t *tracepoint_format = ep_rt_utf8_string_alloc (tracepoint_format_len);
	ep_raise_error_if_nok (tracepoint_format != NULL);

	res = snprintf(tracepoint_format, tracepoint_format_len, "%s %s", tracepoint_name, EP_TRACEPOINT_FORMAT_V1);
	ep_raise_error_if_nok (res >= 0 && (size_t)res < tracepoint_format_len);

ep_on_exit:
	return tracepoint_format;

ep_on_error:
	ep_rt_utf8_string_free (tracepoint_format);
	tracepoint_format = NULL;
	ep_exit_error_handler ();
}

static
EventPipeSessionProviderTracepointConfiguration *
session_provider_tracepoint_config_alloc (const EventPipeProviderTracepointConfiguration *tracepoint_config)
{
	EP_ASSERT (tracepoint_config != NULL);

	EventPipeSessionProviderTracepointConfiguration *instance = ep_rt_object_alloc (EventPipeSessionProviderTracepointConfiguration);
	EventPipeTracepoint *tracepoint = NULL;
	ep_raise_error_if_nok (instance != NULL);

	instance->default_tracepoint.tracepoint_format = tracepoint_format_alloc (tracepoint_config->default_tracepoint_name);

	instance->tracepoints = NULL;
	instance->event_id_to_tracepoint_map = NULL;

	if (tracepoint_config->non_default_tracepoints_length > 0) {
		dn_vector_ptr_custom_alloc_params_t tracepoints_array_params = {0, };
		tracepoints_array_params.capacity = tracepoint_config->non_default_tracepoints_length;
		instance->tracepoints = dn_vector_ptr_custom_alloc (&tracepoints_array_params);
		ep_raise_error_if_nok (instance->tracepoints != NULL);

		instance->event_id_to_tracepoint_map = dn_umap_alloc ();
		ep_raise_error_if_nok (instance->event_id_to_tracepoint_map != NULL);

		for (uint32_t i = 0; i < tracepoint_config->non_default_tracepoints_length; ++i) {
			const EventPipeProviderTracepointSet *tracepoint_set = &tracepoint_config->non_default_tracepoints[i];

			tracepoint = ep_rt_object_alloc (EventPipeTracepoint);
			ep_raise_error_if_nok (tracepoint != NULL);

			tracepoint->tracepoint_format = tracepoint_format_alloc (tracepoint_set->tracepoint_name);
			ep_raise_error_if_nok (tracepoint->tracepoint_format != NULL);

			for (uint32_t j = 0; j < tracepoint_set->event_ids_length; ++j) {
				uint32_t event_id = tracepoint_set->event_ids[j];

				dn_umap_result_t insert_result = dn_umap_insert (instance->event_id_to_tracepoint_map, (void *)(uintptr_t)event_id, tracepoint);
				ep_raise_error_if_nok (insert_result.result);
			}

			ep_raise_error_if_nok (dn_vector_ptr_push_back (instance->tracepoints, tracepoint));
			tracepoint = NULL; // Ownership transferred to the session provider tracepoint configuration.
		}
	}

	ep_raise_error_if_nok (instance->default_tracepoint.tracepoint_format != NULL || instance->tracepoints != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	if (tracepoint != NULL) {
		ep_rt_utf8_string_free ((ep_char8_t *)tracepoint->tracepoint_format);
		ep_rt_object_free (tracepoint);
	}
	session_provider_tracepoint_config_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeSessionProvider *
ep_session_provider_alloc (
	const ep_char8_t *provider_name,
	uint64_t keywords,
	EventPipeEventLevel logging_level,
	const ep_char8_t *filter_data,
	const EventPipeProviderEventFilter *event_filter,
	const EventPipeProviderTracepointConfiguration *tracepoint_config)
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
	instance->event_filter = NULL;
	instance->tracepoint_config = NULL;

	if (event_filter) {
		instance->event_filter = session_provider_event_filter_alloc (event_filter);
		ep_raise_error_if_nok (instance->event_filter != NULL);
	}

	if (tracepoint_config) {
		instance->tracepoint_config = session_provider_tracepoint_config_alloc (tracepoint_config);
		ep_raise_error_if_nok (instance->tracepoint_config != NULL);
	}

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

	session_provider_tracepoint_config_free (session_provider->tracepoint_config);
	session_provider_event_filter_free ((EventPipeSessionProviderEventFilter *)session_provider->event_filter);
	ep_rt_utf8_string_free (session_provider->filter_data);
	ep_rt_utf8_string_free (session_provider->provider_name);
	ep_rt_object_free (session_provider);
}

static
bool
event_filter_allows_event_id (
	const EventPipeSessionProviderEventFilter *event_filter,
	uint32_t event_id)
{
	if (event_filter == NULL)
		return true;

	if (event_filter->event_ids == NULL)
		return !event_filter->enable;

	return event_filter->enable == dn_umap_contains (event_filter->event_ids, (void*)(uintptr_t)event_id);
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
	const EventPipeSessionProviderEventFilter *event_filter = ep_session_provider_get_event_filter (session_provider);
	return event_filter_allows_event_id (event_filter, event_id);
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
