#ifndef __EVENTPIPE_BUFFERMANAGER_H__
#define __EVENTPIPE_BUFFERMANAGER_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

/*
 * EventPipeBufferList.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
//TODO: Implement.
struct _EventPipeBufferList {
#else
struct _EventPipeBufferList_Internal {
#endif
	uint8_t x;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeBufferList {
	uint8_t _internal [sizeof (struct _EventPipeBufferList_Internal)];
};
#endif

/*
 * EventPipeBufferManager.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
//TODO: Implement.
struct _EventPipeBufferManager {
#else
struct _EventPipeBufferManager_Internal {
#endif
	ep_rt_wait_event_handle_t rt_wait_event;
	ep_rt_spin_lock_handle_t rt_lock;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeBufferManager {
	uint8_t _internal [sizeof (struct _EventPipeBufferManager_Internal)];
};
#endif

EP_DEFINE_GETTER_REF(EventPipeBufferManager *, buffer_manager, ep_rt_wait_event_handle_t *, rt_wait_event)
EP_DEFINE_GETTER_REF(EventPipeBufferManager *, buffer_manager, ep_rt_spin_lock_handle_t *, rt_lock);

EventPipeBufferManager *
ep_buffer_manager_alloc (
	EventPipeSession *session,
	size_t max_size_of_all_buffers,
	size_t sequence_point_allocation_budget);

void
ep_buffer_manager_free (EventPipeBufferManager *buffer_manager);

#ifdef EP_CHECKED_BUILD
void
ep_buffer_manager_requires_lock_held (const EventPipeBufferManager *buffer_manager);
#else
#define ep_buffer_manager_requires_lock_held(x)
#endif

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_BUFFERMANAGER_H__ **/
