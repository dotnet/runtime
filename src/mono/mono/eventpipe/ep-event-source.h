#ifndef __EVENTPIPE_EVENT_SOURCE_H__
#define __EVENTPIPE_EVENT_SOURCE_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

/*
 * EventPipeEventSource.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeEventSource {
#else
struct _EventPipeEventSource_Internal {
#endif
	const ep_char8_t *provider_name;
	EventPipeProvider *provider;
	const ep_char8_t *process_info_event_name;
	EventPipeEvent *process_info_event;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeEventSource {
	uint8_t _internal [sizeof (struct _EventPipeEventSource_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeEventSource *, event_source, const ep_char8_t *, provider_name)
EP_DEFINE_GETTER(EventPipeEventSource *, event_source, const ep_char8_t *, process_info_event_name)
EP_DEFINE_GETTER(EventPipeEventSource *, event_source, EventPipeEvent *, process_info_event)

EventPipeEventSource *
ep_event_source_alloc (void);

void
ep_event_source_free (EventPipeEventSource *event_source);

void
ep_event_source_enable (EventPipeEventSource *event_source, EventPipeSession *session);

void
ep_event_source_send_process_info (EventPipeEventSource *event_source, const ep_char16_t *command_line);

EventPipeEventSource *
ep_event_source_get (void);

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_EVENT_SOURCE_H__ **/
