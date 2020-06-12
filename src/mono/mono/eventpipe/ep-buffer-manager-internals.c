#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_GETTER_SETTER
#include "ep.h"

/*
 * EventPipeBufferManager.
 */

EventPipeBufferManager *
ep_buffer_manager_alloc (
	EventPipeSession *session,
	size_t max_size_of_all_buffers,
	size_t sequence_point_allocation_budget)
{
	//TODO: Implement.
	return ep_rt_object_alloc (EventPipeBufferManager);
}

void
ep_buffer_manager_free (EventPipeBufferManager * buffer_manager)
{
	//TODO: Implement.
	ep_return_void_if_nok (buffer_manager != NULL);
	ep_rt_object_free (buffer_manager);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_buffer_manager_internals;
const char quiet_linker_empty_file_warning_eventpipe_buffer_manager_internals = 0;
#endif
