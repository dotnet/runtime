#ifndef __EVENTPIPE_SESSION_PROVIDER_H__
#define __EVENTPIPE_SESSION_PROVIDER_H__

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_SESSION_PROVIDER_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeSessionProvider.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_SESSION_PROVIDER_GETTER_SETTER)
struct _EventPipeSessionProvider {
#else
struct _EventPipeSessionProvider_Internal {
#endif
	ep_char8_t *provider_name;
	uint64_t keywords;
	EventPipeEventLevel logging_level;
	ep_char8_t *filter_data;
	EventPipeProviderEventFilter *event_filter;
	EventPipeProviderTracepointConfiguration *tracepoint_config;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_SESSION_PROVIDER_GETTER_SETTER)
struct _EventPipeSessionProvider {
	uint8_t _internal [sizeof (struct _EventPipeSessionProvider_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeSessionProvider *, session_provider, const ep_char8_t *, provider_name)
EP_DEFINE_GETTER(EventPipeSessionProvider *, session_provider, uint64_t, keywords)
EP_DEFINE_GETTER(EventPipeSessionProvider *, session_provider, EventPipeEventLevel, logging_level)
EP_DEFINE_GETTER(EventPipeSessionProvider *, session_provider, const ep_char8_t *, filter_data)
EP_DEFINE_GETTER(EventPipeSessionProvider *, session_provider, EventPipeProviderEventFilter *, event_filter)
EP_DEFINE_GETTER(EventPipeSessionProvider *, session_provider, EventPipeProviderTracepointConfiguration *, tracepoint_config)

EventPipeSessionProvider *
ep_session_provider_alloc (
	const ep_char8_t *provider_name,
	uint64_t keywords,
	EventPipeEventLevel logging_level,
	const ep_char8_t *filter_data);

EventPipeSessionProvider *
ep_session_provider_alloc (
	const ep_char8_t *provider_name,
	uint64_t keywords,
	EventPipeEventLevel logging_level,
	const ep_char8_t *filter_data,
	EventPipeProviderEventFilter *event_filter,
	EventPipeProviderTracepointConfiguration *tracepoint_config);

void
ep_session_provider_free (EventPipeSessionProvider * session_provider);

bool
ep_session_provider_allows_event (
	const EventPipeSessionProvider *session_provider,
	const EventPipeEvent *ep_event);

/*
* EventPipeSessionProviderList.
 */
#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_SESSION_PROVIDER_GETTER_SETTER)
struct _EventPipeSessionProviderList {
#else
struct _EventPipeSessionProviderList_Internal {
#endif
	dn_list_t *providers;
	EventPipeSessionProvider *catch_all_provider;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_SESSION_PROVIDER_GETTER_SETTER)
struct _EventPipeSessionProviderList {
	uint8_t _internal [sizeof (struct _EventPipeSessionProviderList_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeSessionProviderList *, session_provider_list, dn_list_t *, providers)
EP_DEFINE_GETTER(EventPipeSessionProviderList *, session_provider_list, EventPipeSessionProvider *, catch_all_provider)

EventPipeSessionProviderList *
ep_session_provider_list_alloc (
	const EventPipeProviderConfiguration *configs,
	uint32_t configs_len);

void
ep_session_provider_list_free (EventPipeSessionProviderList *session_provider_list);

void
ep_session_provider_list_clear (EventPipeSessionProviderList *session_provider_list);

bool
ep_session_provider_list_is_empty (const EventPipeSessionProviderList *session_provider_list);

bool
ep_session_provider_list_add_session_provider (
	EventPipeSessionProviderList *session_provider_list,
	EventPipeSessionProvider *session_provider);

EventPipeSessionProvider *
ep_session_provider_list_find_by_name (
	dn_list_t *list,
	const ep_char8_t *name);

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_SESSION_PROVIDER_H__ **/
