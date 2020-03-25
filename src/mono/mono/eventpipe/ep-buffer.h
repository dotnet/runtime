#ifndef __EVENTPIPE_BUFFER_H__
#define __EVENTPIPE_BUFFER_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

/*
 * EventPipeBuffer.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
//TODO: Implement.
struct _EventPipeBuffer {
#else
struct _EventPipeBuffer_Internal {
#endif
	volatile uint32_t state;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeBuffer {
	uint8_t _internal [sizeof (struct _EventPipeBuffer_Internal)];
};
#endif

EP_DEFINE_GETTER_REF(EventPipeBuffer *, buffer, volatile uint32_t *, state)

static
inline
void
ep_buffer_convert_to_read_only (EventPipeBuffer *buffer)
{
	//TODO: Implement.
}

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_BUFFER_H__ **/
