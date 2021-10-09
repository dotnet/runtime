#ifndef __EVENTPIPE_PROVIDER_H__
#define __EVENTPIPE_PROVIDER_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_PROVIDER_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeProvider.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_PROVIDER_GETTER_SETTER)
struct _EventPipeProvider {
#else
struct _EventPipeProvider_Internal {
#endif
	// Bit vector containing the currently enabled keywords.
	int64_t keywords;
	// Bit mask of sessions for which this provider is enabled.
	uint64_t sessions;
	// The name of the provider.
	ep_char8_t *provider_name;
	ep_char16_t *provider_name_utf16;
	// List of every event currently associated with the provider.
	// New events can be added on-the-fly.
	ep_rt_event_list_t event_list;
	// The optional provider callback function.
	EventPipeCallback callback_func;
	// The optional provider callback_data free callback function.
	EventPipeCallbackDataFree callback_data_free_func;
	// The optional provider callback data pointer.
	void *callback_data;
	// The configuration object.
	EventPipeConfiguration *config;
	// The current verbosity of the provider.
	EventPipeEventLevel provider_level;
	// True if the provider has been deleted, but that deletion
	// has been deferred until tracing is stopped.
	bool delete_deferred;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_PROVIDER_GETTER_SETTER)
struct _EventPipeProvider {
	uint8_t _internal [sizeof (struct _EventPipeProvider_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeProvider *, provider, const ep_char8_t *, provider_name)
EP_DEFINE_GETTER(EventPipeProvider *, provider, const ep_char16_t *, provider_name_utf16)
EP_DEFINE_GETTER(EventPipeProvider *, provider, bool, delete_deferred)
EP_DEFINE_GETTER(EventPipeProvider *, provider, uint64_t, sessions)

static
inline
bool
ep_provider_get_enabled (const EventPipeProvider *provider)
{
	return ep_provider_get_sessions (provider) != 0;
}

static
inline
bool
ep_provider_is_enabled_by_mask (
	const EventPipeProvider *provider,
	uint64_t session_mask)
{
	return ((ep_provider_get_sessions (provider) & session_mask) != 0);
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
	EventPipeCallbackDataFree callback_data_free_func,
	void *callback_data);

void
ep_provider_free (EventPipeProvider * provider);

// Add an event to the provider.
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

void
ep_provider_set_delete_deferred (
	EventPipeProvider *provider,
	bool deferred);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_PROVIDER_H__ */
