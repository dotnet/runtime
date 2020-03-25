#ifndef __EVENTPIPE_PROVIDER_H__
#define __EVENTPIPE_PROVIDER_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

/*
 * EventPipeProvider.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeProvider {
#else
struct _EventPipeProvider_Internal {
#endif
	ep_char8_t *provider_name;
	ep_char16_t *provider_name_utf16;
	int64_t keywords;
	EventPipeEventLevel provider_level;
	ep_rt_event_list_t event_list;
	EventPipeCallback callback_func;
	void *callback_data;
	EventPipeConfiguration *config;
	bool delete_deferred;
	uint64_t sessions;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeProvider {
	uint8_t _internal [sizeof (struct _EventPipeProvider_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeProvider *, provider, const ep_char8_t *, provider_name)
EP_DEFINE_GETTER(EventPipeProvider *, provider, const ep_char16_t *, provider_name_utf16)
EP_DEFINE_GETTER(EventPipeProvider *, provider, uint64_t, keywords)
EP_DEFINE_SETTER(EventPipeProvider *, provider, uint64_t, keywords)
EP_DEFINE_GETTER(EventPipeProvider *, provider, EventPipeEventLevel, provider_level)
EP_DEFINE_SETTER(EventPipeProvider *, provider, EventPipeEventLevel, provider_level)
EP_DEFINE_GETTER_REF(EventPipeProvider *, provider, ep_rt_event_list_t *, event_list)
EP_DEFINE_GETTER(EventPipeProvider *, provider, EventPipeCallback, callback_func)
EP_DEFINE_GETTER(EventPipeProvider *, provider, void *, callback_data)
EP_DEFINE_GETTER(EventPipeProvider *, provider, EventPipeConfiguration *, config)
EP_DEFINE_GETTER(EventPipeProvider *, provider, bool, delete_deferred)
EP_DEFINE_SETTER(EventPipeProvider *, provider, bool, delete_deferred)
EP_DEFINE_GETTER(EventPipeProvider *, provider, uint64_t, sessions)
EP_DEFINE_SETTER(EventPipeProvider *, provider, uint64_t, sessions)

static
inline
bool
ep_provider_get_enabled (const EventPipeProvider *provider)
{
	return ep_provider_get_sessions (provider) != 0;
}

static
inline
const ep_char8_t *
ep_provider_get_wildcard_name_utf8 (void)
{
	return "*";
}

static
inline
const ep_char8_t *
ep_provider_get_default_name_utf8 (void)
{
	return "Microsoft-DotNETCore-EventPipe";
}

EventPipeProvider *
ep_provider_alloc (
	EventPipeConfiguration *config,
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	void *callback_data);

void
ep_provider_free (EventPipeProvider * provider);

EventPipeEvent *
ep_provider_add_event (
	EventPipeProvider *provider,
	uint32_t event_id,
	uint64_t keywords,
	uint32_t event_version,
	EventPipeEventLevel level,
	bool need_stack,
	const uint8_t *metadata,
	uint32_t metadata_len);

const EventPipeProviderCallbackData *
ep_provider_set_config_lock_held (
	EventPipeProvider *provider,
	int64_t keywords_for_all_sessions,
	EventPipeEventLevel level_for_all_sessions,
	uint64_t session_mask,
	int64_t keywords,
	EventPipeEventLevel level,
	const ep_char8_t *filter_data,
	EventPipeProviderCallbackData *callback_data);

const EventPipeProviderCallbackData *
ep_provider_unset_config_lock_held (
	EventPipeProvider *provider,
	int64_t keywords_for_all_sessions,
	EventPipeEventLevel level_for_all_sessions,
	uint64_t session_mask,
	int64_t keywords,
	EventPipeEventLevel level,
	const ep_char8_t *filter_data,
	EventPipeProviderCallbackData *callback_data);

void
ep_provider_invoke_callback (EventPipeProviderCallbackData *provider_callback_data);

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_PROVIDER_H__ **/
