#ifndef __EVENTPIPE_EVENT_SOURCE_H__
#define __EVENTPIPE_EVENT_SOURCE_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_EVENT_SOURCE_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

extern const ep_char8_t *_ep_os_info;
extern const ep_char8_t *_ep_arch_info;
extern EventPipeEventSource _ep_event_source_instance;

/*
 * EventPipeEventSource.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_EVENT_SOURCE_GETTER_SETTER)
struct _EventPipeEventSource {
#else
struct _EventPipeEventSource_Internal {
#endif
	const ep_char8_t *provider_name;
	EventPipeProvider *provider;
	const ep_char8_t *process_info_event_name;
	EventPipeEvent *process_info_event;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_EVENT_SOURCE_GETTER_SETTER)
struct _EventPipeEventSource {
	uint8_t _internal [sizeof (struct _EventPipeEventSource_Internal)];
};
#endif

static
inline
const ep_char8_t *
ep_event_source_get_os_info (void)
{
	return _ep_os_info;
}

static
inline
const ep_char8_t *
ep_event_source_get_arch_info (void)
{
	return _ep_arch_info;
}

EventPipeEventSource *
ep_event_source_alloc (void);

EventPipeEventSource *
ep_event_source_init (EventPipeEventSource *event_source);

void
ep_event_source_fini (EventPipeEventSource *event_source);

void
ep_event_source_free (EventPipeEventSource *event_source);

bool
ep_event_source_enable (EventPipeEventSource *event_source, EventPipeSession *session);

void
ep_event_source_send_process_info (EventPipeEventSource *event_source, const ep_char8_t *command_line);

static
inline
EventPipeEventSource *
ep_event_source_get (void)
{
	// Singleton.
	return &_ep_event_source_instance;
}

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_EVENT_SOURCE_H__ */
