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

/*
 * EventPipeSessionProviderList.
 */

void
ep_session_provider_list_clear (EventPipeSessionProviderList *session_provider_list)
{
	ep_return_void_if_nok (session_provider_list != NULL);
	ep_rt_session_provider_list_clear (ep_session_provider_list_get_providers_ref (session_provider_list), session_provider_free_func);
}

bool
ep_session_provider_list_is_empty (const EventPipeSessionProviderList *session_provider_list)
{
	return (ep_rt_provider_list_is_empty (ep_session_provider_list_get_providers_cref (session_provider_list)) && ep_session_provider_list_get_catch_all_provider (session_provider_list) == NULL);
}

void
ep_session_provider_list_add_session_provider (
	EventPipeSessionProviderList *session_provider_list,
	EventPipeSessionProvider *session_provider)
{
	ep_return_void_if_nok (session_provider != NULL);
	ep_rt_session_provider_list_append (ep_session_provider_list_get_providers_ref (session_provider_list), session_provider);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_session_provider;
const char quiet_linker_empty_file_warning_eventpipe_session_provider = 0;
#endif
