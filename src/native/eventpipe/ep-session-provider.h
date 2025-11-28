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

#if HAVE_LINUX_USER_EVENTS_H
#include <linux/user_events.h> // DIAG_IOCSREG
#else // HAVE_LINUX_USER_EVENTS_H
/*
 * Describes an event registration and stores the results of the registration.
 * This structure is passed to the DIAG_IOCSREG ioctl, callers at a minimum
 * must set the size and name_args before invocation.
 */
struct user_reg {

	/* Input: Size of the user_reg structure being used */
	uint32_t size;

	/* Input: Bit in enable address to use */
	uint8_t enable_bit;

	/* Input: Enable size in bytes at address */
	uint8_t enable_size;

	/* Input: Flags to use, if any */
	uint16_t flags;

	/* Input: Address to update when enabled */
	uint64_t enable_addr;

	/* Input: Pointer to string with event name, description and flags */
	uint64_t name_args;

	/* Output: Index of the event to use when writing data */
	uint32_t write_index;
};

/*
 * Describes an event unregister, callers must set the size, address and bit.
 * This structure is passed to the DIAG_IOCSUNREG ioctl to disable bit updates.
 */
struct user_unreg {
	/* Input: Size of the user_unreg structure being used */
	uint32_t size;

	/* Input: Bit to unregister */
	uint8_t disable_bit;

	/* Input: Reserved, set to 0 */
	uint8_t _reserved;

	/* Input: Reserved, set to 0 */
	uint16_t _reserved2;

	/* Input: Address to unregister */
	uint64_t disable_addr;
};

/* Request to register a user_event */
#define DIAG_IOCSREG 0xC0082A00

/* Requests to unregister a user_event */
#define DIAG_IOCSUNREG 0x40082A02

#endif // HAVE_LINUX_USER_EVENTS_H

/*
 * EventPipeSessionProviderTracepoint.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_EP_GETTER_SETTER)
struct _EventPipeSessionProviderTracepoint {
#else
struct _EventPipeSessionProviderTracepoint_Internal {
#endif
	ep_char8_t *tracepoint_format;
	uint32_t write_index;
	uint32_t enabled;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_EP_GETTER_SETTER)
struct _EventPipeSessionProviderTracepoint {
	uint8_t _internal [sizeof (struct _EventPipeSessionProviderTracepoint_Internal)];
};
#endif

#define EP_SESSION_PROVIDER_TRACEPOINT_ENABLE_BIT 31

EP_DEFINE_GETTER(EventPipeSessionProviderTracepoint *, session_provider_tracepoint, const ep_char8_t *, tracepoint_format)
EP_DEFINE_GETTER(EventPipeSessionProviderTracepoint *, session_provider_tracepoint, uint32_t, write_index)
EP_DEFINE_GETTER(EventPipeSessionProviderTracepoint *, session_provider_tracepoint, uint32_t, enabled)

bool
ep_session_provider_register_tracepoints (
	EventPipeSessionProvider *session_provider,
	int user_events_data_fd);

void
ep_session_provider_unregister_tracepoints (
	EventPipeSessionProvider *session_provider,
	int user_events_data_fd);

const EventPipeSessionProviderTracepoint *
ep_session_provider_get_tracepoint_for_event (
	EventPipeSessionProvider *session_provider,
	EventPipeEvent *ep_event);

/*
 * EventPipeSessionProviderEventFilter.
 *
 * Introduced in CollectTracing5, the event filter provides EventPipe Sessions
 * additional control over which events are enabled/disabled for a particular provider.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_EP_GETTER_SETTER)
struct _EventPipeSessionProviderEventFilter {
#else
struct _EventPipeSessionProviderEventFilter_Internal {
#endif
	bool enable;
	dn_umap_t *event_ids;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_EP_GETTER_SETTER)
struct _EventPipeSessionProviderEventFilter {
	uint8_t _internal [sizeof (struct _EventPipeSessionProviderEventFilter_Internal)];
};
#endif

/*
 * EventPipeSessionProviderTracepointConfiguration.
 *
 * Introduced in CollectTracing5, user_events-based EventPipe Sessions are required to
 * specify a tracepoint configuration per-provider that details which events should be
 * written to which tracepoints. Atleast one of default_tracepoint_name or tracepoints
 * must be specified.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_EP_GETTER_SETTER)
struct _EventPipeSessionProviderTracepointConfiguration {
#else
struct _EventPipeSessionProviderTracepointConfiguration_Internal {
#endif
	EventPipeSessionProviderTracepoint default_tracepoint;
	dn_vector_ptr_t *tracepoints;
	dn_umap_t *event_id_to_tracepoint_map;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_EP_GETTER_SETTER)
struct _EventPipeSessionProviderTracepointConfiguration {
	uint8_t _internal [sizeof (struct _EventPipeSessionProviderTracepointConfiguration_Internal)];
};
#endif

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
	EventPipeSessionProviderEventFilter *event_filter;
	EventPipeSessionProviderTracepointConfiguration *tracepoint_config;
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

EventPipeSessionProvider *
ep_session_provider_alloc (
	const ep_char8_t *provider_name,
	uint64_t keywords,
	EventPipeEventLevel logging_level,
	const ep_char8_t *filter_data,
	const EventPipeProviderEventFilter *event_filter,
	const EventPipeProviderTracepointConfiguration *tracepoint_config);

void
ep_session_provider_free (EventPipeSessionProvider * session_provider);

bool
ep_session_provider_allows_event (
	EventPipeSessionProvider *session_provider,
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
