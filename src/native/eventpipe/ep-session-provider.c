#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_SESSION_PROVIDER_GETTER_SETTER
#include "ep-session-provider.h"
#include "ep-rt.h"

#if HAVE_LINUX_USER_EVENTS_H
#include <linux/user_events.h> // DIAG_IOCSREG
#endif // HAVE_LINUX_USER_EVENTS_H

#if HAVE_SYS_IOCTL_H
#include <sys/ioctl.h> // session_register_tracepoint
#endif // HAVE_SYS_IOCTL_H

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
bool
session_provider_tracepoint_register (
	EventPipeSessionProviderTracepoint *tracepoint,
	int user_events_data_fd);

static
bool
session_provider_tracepoint_unregister (
	EventPipeSessionProviderTracepoint *tracepoint,
	int user_events_data_fd);

static
void
session_provider_tracepoint_free (EventPipeSessionProviderTracepoint *tracepoint);

static
void
DN_CALLBACK_CALLTYPE
tracepoint_free_func (void *tracepoint);

static
void
session_provider_tracepoint_config_free (EventPipeSessionProviderTracepointConfiguration *tracepoint_config);

static
ep_char8_t *
tracepoint_format_alloc (ep_char8_t *tracepoint_name);

static
EventPipeSessionProviderTracepointConfiguration *
session_provider_tracepoint_config_alloc (const EventPipeProviderTracepointConfiguration *tracepoint_config);

static
bool
event_filter_enables_event_id (
	EventPipeSessionProviderEventFilter *event_filter,
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
			dn_umap_result_t insert_result = dn_umap_uint32_ptr_insert (instance->event_ids, event_filter->event_ids[i], NULL);
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

/*
 * EventPipeSessionProviderTracepoint.
 */

#if HAVE_LINUX_USER_EVENTS_H && HAVE_SYS_IOCTL_H
static
bool
session_provider_tracepoint_register (
	EventPipeSessionProviderTracepoint *tracepoint,
	int user_events_data_fd)
{
	EP_ASSERT (tracepoint != NULL);
	EP_ASSERT (user_events_data_fd != -1);
	struct user_reg reg = {0};

	reg.size = sizeof(reg);
	reg.enable_bit = EP_SESSION_PROVIDER_TRACEPOINT_ENABLE_BIT;
	reg.enable_size = sizeof(tracepoint->enabled);
	reg.enable_addr = (uint64_t)&tracepoint->enabled;

	reg.name_args = (uint64_t)tracepoint->tracepoint_format;

	if (ioctl(user_events_data_fd, DIAG_IOCSREG, &reg) == -1)
		return false;

	tracepoint->write_index = reg.write_index;

	return true;
}

static
bool
session_provider_tracepoint_unregister (
	EventPipeSessionProviderTracepoint *tracepoint,
	int user_events_data_fd)
{
	EP_ASSERT (tracepoint != NULL);
	EP_ASSERT (user_events_data_fd != -1);
	struct user_unreg unreg = {0};

	unreg.size = sizeof(unreg);
	unreg.disable_bit = EP_SESSION_PROVIDER_TRACEPOINT_ENABLE_BIT;
	unreg.disable_addr = (uint64_t)&tracepoint->enabled;

	if (ioctl(user_events_data_fd, DIAG_IOCSUNREG, &unreg) == -1)
		return false;

	return true;
}
#else // HAVE_LINUX_USER_EVENTS_H && HAVE_SYS_IOCTL_H
static
bool
session_provider_tracepoint_register (
	EventPipeSessionProviderTracepoint *tracepoint,
	int user_events_data_fd)
{
	// Not Supported
	return false;
}

static
bool
session_provider_tracepoint_unregister (
	EventPipeSessionProviderTracepoint *tracepoint,
	int user_events_data_fd)
{
	// Not Supported
	return false;
}
#endif // HAVE_LINUX_USER_EVENTS_H && HAVE_SYS_IOCTL_H

/*
 *  ep_session_provider_register_tracepoints
 *
 *  Registers the tracepoints configured for the session provider with the user events data file descriptor.
 *
 *  Returns true if the session_provider has tracepoints and all were successfully registered.
 */
bool
ep_session_provider_register_tracepoints (
	EventPipeSessionProvider *session_provider,
	int user_events_data_fd)
{
	EP_ASSERT (session_provider != NULL);
	EP_ASSERT (user_events_data_fd != -1);

	if (user_events_data_fd < 0)
		return false;

	EventPipeSessionProviderTracepointConfiguration *tracepoint_config = session_provider->tracepoint_config;
	if (tracepoint_config == NULL)
		return false;

	if (tracepoint_config->default_tracepoint.tracepoint_format == NULL && tracepoint_config->tracepoints == NULL)
		return false;

	if (tracepoint_config->default_tracepoint.tracepoint_format != NULL &&
		!session_provider_tracepoint_register (&tracepoint_config->default_tracepoint, user_events_data_fd))
		return false;

	if (tracepoint_config->tracepoints != NULL) {
		DN_VECTOR_PTR_FOREACH_BEGIN (EventPipeSessionProviderTracepoint *, tracepoint, tracepoint_config->tracepoints) {
			EP_ASSERT (tracepoint != NULL);
			if (!session_provider_tracepoint_register (tracepoint, user_events_data_fd))
				return false;
		} DN_VECTOR_PTR_FOREACH_END;
	}

	return true;
}

/*
 *  ep_session_provider_unregister_tracepoints
 *
 *  Attempts to unregister all tracepoints configured for the session provider with the user events data file descriptor.
 */
void
ep_session_provider_unregister_tracepoints (
	EventPipeSessionProvider *session_provider,
	int user_events_data_fd)
{
	EP_ASSERT (session_provider != NULL);
	EP_ASSERT (user_events_data_fd != -1);

	if (user_events_data_fd < 0)
		return;

	if (session_provider->tracepoint_config == NULL)
		return;

	EventPipeSessionProviderTracepointConfiguration *tracepoint_config = session_provider->tracepoint_config;
	if (tracepoint_config->default_tracepoint.tracepoint_format != NULL)
		session_provider_tracepoint_unregister (&tracepoint_config->default_tracepoint, user_events_data_fd);

	if (tracepoint_config->tracepoints != NULL) {
		DN_VECTOR_PTR_FOREACH_BEGIN (EventPipeSessionProviderTracepoint *, tracepoint, tracepoint_config->tracepoints) {
			EP_ASSERT (tracepoint != NULL);
			session_provider_tracepoint_unregister (tracepoint, user_events_data_fd);
		} DN_VECTOR_PTR_FOREACH_END;
	}
}

/*
 *  ep_session_provider_get_tracepoint_for_event
 *
 *  Returns the session provider's tracepoint associated with the EventPipeEvent.
 */
const EventPipeSessionProviderTracepoint *
ep_session_provider_get_tracepoint_for_event (
	EventPipeSessionProvider *session_provider,
	EventPipeEvent *ep_event)
{
	EP_ASSERT (session_provider != NULL);
	EP_ASSERT (ep_event != NULL);

	EventPipeSessionProviderTracepoint *tracepoint = NULL;
	EventPipeSessionProviderTracepointConfiguration *tracepoint_config = session_provider->tracepoint_config;
	if (tracepoint_config == NULL)
		return tracepoint;

	if (tracepoint_config->default_tracepoint.tracepoint_format != NULL)
		tracepoint = &tracepoint_config->default_tracepoint;

	if (tracepoint_config->event_id_to_tracepoint_map == NULL)
		return tracepoint;

	dn_umap_it_t tracepoint_found = dn_umap_uint32_ptr_find (tracepoint_config->event_id_to_tracepoint_map, ep_event_get_event_id (ep_event));
	if (!dn_umap_it_end (tracepoint_found))
		tracepoint = dn_umap_it_value_t (tracepoint_found, EventPipeSessionProviderTracepoint *);

	return tracepoint;
}

static
void
session_provider_tracepoint_free (EventPipeSessionProviderTracepoint *tracepoint)
{
	ep_return_void_if_nok (tracepoint != NULL);

	ep_rt_utf8_string_free (tracepoint->tracepoint_format);
	ep_rt_object_free (tracepoint);
}

static
void
DN_CALLBACK_CALLTYPE
tracepoint_free_func (void *tracepoint)
{
	session_provider_tracepoint_free (*(EventPipeSessionProviderTracepoint **)tracepoint);
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

	ep_rt_utf8_string_free (tracepoint_config->default_tracepoint.tracepoint_format);

	ep_rt_object_free (tracepoint_config);
}

static
ep_char8_t *
tracepoint_format_alloc (ep_char8_t *tracepoint_name)
{
	EP_ASSERT (tracepoint_name != NULL);

	const int format_max_size = 512;
	const ep_char8_t *args = "u8 version; u16 event_id; __rel_loc u8[] extension; __rel_loc u8[] payload";

	if ((strlen(tracepoint_name) + strlen(args) + 2) > (size_t)format_max_size) // +2 for the space and null terminator
		return NULL;

	return ep_rt_utf8_string_printf_alloc ("%s %s", tracepoint_name, args);
}

static
EventPipeSessionProviderTracepointConfiguration *
session_provider_tracepoint_config_alloc (const EventPipeProviderTracepointConfiguration *tracepoint_config)
{
	EP_ASSERT (tracepoint_config != NULL);

	EventPipeSessionProviderTracepointConfiguration *instance = ep_rt_object_alloc (EventPipeSessionProviderTracepointConfiguration);
	EventPipeSessionProviderTracepoint *tracepoint = NULL;
	ep_raise_error_if_nok (instance != NULL);

	instance->default_tracepoint.tracepoint_format = NULL;
	if (tracepoint_config->default_tracepoint_name != NULL) {
		instance->default_tracepoint.tracepoint_format = tracepoint_format_alloc (tracepoint_config->default_tracepoint_name);
		ep_raise_error_if_nok (instance->default_tracepoint.tracepoint_format != NULL);
	}

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

			tracepoint = ep_rt_object_alloc (EventPipeSessionProviderTracepoint);
			ep_raise_error_if_nok (tracepoint != NULL);

			tracepoint->tracepoint_format = tracepoint_format_alloc (tracepoint_set->tracepoint_name);
			ep_raise_error_if_nok (tracepoint->tracepoint_format != NULL);

			for (uint32_t j = 0; j < tracepoint_set->event_ids_length; ++j) {
				uint32_t event_id = tracepoint_set->event_ids[j];

				dn_umap_result_t insert_result = dn_umap_uint32_ptr_insert (instance->event_id_to_tracepoint_map, event_id, tracepoint);
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
	session_provider_tracepoint_free (tracepoint);
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
	session_provider_event_filter_free (session_provider->event_filter);
	ep_rt_utf8_string_free (session_provider->filter_data);
	ep_rt_utf8_string_free (session_provider->provider_name);
	ep_rt_object_free (session_provider);
}

static
bool
event_filter_enables_event_id (
	EventPipeSessionProviderEventFilter *event_filter,
	uint32_t event_id)
{
	if (event_filter == NULL)
		return true;

	if (event_filter->event_ids == NULL)
		return !event_filter->enable;

	dn_umap_it_t it = dn_umap_uint32_ptr_find (event_filter->event_ids, event_id);
	bool contains_event_id = !dn_umap_it_end (it);

	return event_filter->enable == contains_event_id;
}

bool
ep_session_provider_allows_event (
	EventPipeSessionProvider *session_provider,
	const EventPipeEvent *ep_event)
{
	EP_ASSERT(session_provider != NULL);

	uint64_t keywords = ep_event_get_keywords (ep_event);
	if ((keywords != 0) && ((session_provider->keywords & keywords) == 0))
		return false;

	EventPipeEventLevel event_level = ep_event_get_level (ep_event);
	EventPipeEventLevel session_level = session_provider->logging_level;
	if ((event_level != EP_EVENT_LEVEL_LOGALWAYS) &&
		(session_level != EP_EVENT_LEVEL_LOGALWAYS) &&
		(session_level < event_level))
		return false;

	uint32_t event_id = ep_event_get_event_id (ep_event);
	return event_filter_enables_event_id (session_provider->event_filter, event_id);
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
