#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#include "ep.h"

/*
 * EventPipeBufferManager.
 */

#ifdef EP_CHECKED_BUILD
void
ep_buffer_manager_requires_lock_held (const EventPipeBufferManager *buffer_manager)
{
	EP_ASSERT (buffer_manager != NULL);
	ep_rt_spin_lock_requires_lock_held (ep_buffer_manager_get_rt_lock_cref (buffer_manager));
}
#endif

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_buffer_manager;
const char quiet_linker_empty_file_warning_eventpipe_buffer_manager = 0;
#endif
