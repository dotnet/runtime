#ifndef __EVENTPIPE_STACK_CONTENTS_H__
#define __EVENTPIPE_STACK_CONTENTS_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_STACK_CONTENTS_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeStackContents.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_STACK_CONTENTS_GETTER_SETTER)
struct _EventPipeStackContents {
#else
struct _EventPipeStackContents_Internal {
#endif
	// The next available slot in stack_frames.
	uint32_t next_available_frame;
	// Array of IP values from a stack crawl.
	// Top of stack is at index 0.
	uintptr_t stack_frames [EP_MAX_STACK_DEPTH];
#ifdef EP_CHECKED_BUILD
	// Parallel array of MethodDesc pointers.
	// Used for debug-only stack printing.
	ep_rt_method_desc_t *methods [EP_MAX_STACK_DEPTH];
#endif
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_STACK_CONTENTS_GETTER_SETTER)
struct _EventPipeStackContents {
	uint8_t _internal [sizeof (struct _EventPipeStackContents_Internal)];
};
#endif

EP_DEFINE_GETTER_ARRAY_REF(EventPipeStackContents *, stack_contents, uintptr_t *, const uintptr_t *, stack_frames, stack_frames[0])
#ifdef EP_CHECKED_BUILD
EP_DEFINE_GETTER_ARRAY_REF(EventPipeStackContents *, stack_contents, ep_rt_method_desc_t **, ep_rt_method_desc_t *const*, methods, methods[0])
#endif
EP_DEFINE_GETTER(EventPipeStackContents *, stack_contents, uint32_t, next_available_frame)
EP_DEFINE_SETTER(EventPipeStackContents *, stack_contents, uint32_t, next_available_frame)

EventPipeStackContents *
ep_stack_contents_alloc (void);

EventPipeStackContents *
ep_stack_contents_init (EventPipeStackContents *stack_contents);

void
ep_stack_contents_fini (EventPipeStackContents *stack_contents);

void
ep_stack_contents_free (EventPipeStackContents *stack_contents);

static
inline
void
ep_stack_contents_reset (EventPipeStackContents *stack_contents)
{
	ep_stack_contents_set_next_available_frame (stack_contents, 0);
}

static
inline
bool
ep_stack_contents_is_empty (EventPipeStackContents *stack_contents)
{
	return (ep_stack_contents_get_next_available_frame (stack_contents) == 0);
}

static
inline
uint32_t
ep_stack_contents_get_length (EventPipeStackContents *stack_contents)
{
	return ep_stack_contents_get_next_available_frame (stack_contents);
}

#ifdef EP_CHECKED_BUILD
static
inline
ep_rt_method_desc_t *
ep_stack_contents_get_method (
	EventPipeStackContents *stack_contents,
	uint32_t frame_index)
{
	EP_ASSERT (frame_index < EP_MAX_STACK_DEPTH);
	if (frame_index >= EP_MAX_STACK_DEPTH)
		return NULL;

	return ep_stack_contents_get_methods_cref (stack_contents)[frame_index];
}
#endif

static
inline
void
ep_stack_contents_append (
	EventPipeStackContents *stack_contents,
	uintptr_t control_pc,
	ep_rt_method_desc_t *method)
{
	EP_ASSERT (stack_contents != NULL);
	uint32_t next_frame = ep_stack_contents_get_next_available_frame (stack_contents);
	if (next_frame < EP_MAX_STACK_DEPTH) {
		ep_stack_contents_get_stack_frames_ref (stack_contents)[next_frame] = control_pc;
#ifdef EP_CHECKED_BUILD
		ep_stack_contents_get_methods_ref (stack_contents)[next_frame] = method;
#endif
		next_frame++;
		ep_stack_contents_set_next_available_frame (stack_contents, next_frame);
	}
}

static
inline
uint8_t *
ep_stack_contents_get_pointer (const EventPipeStackContents *stack_contents)
{
	EP_ASSERT (stack_contents != NULL);
	return (uint8_t *)ep_stack_contents_get_stack_frames_cref (stack_contents);
}

static
inline
uint32_t
ep_stack_contents_get_size (const EventPipeStackContents *stack_contents)
{
	EP_ASSERT (stack_contents != NULL);
	return (ep_stack_contents_get_next_available_frame (stack_contents) * sizeof (uintptr_t));
}

static
inline
uint32_t
ep_stack_contents_get_instance_size (const EventPipeStackContents *stack_contents)
{
	// The total size including the size
#ifdef EP_CHECKED_BUILD
	return stack_contents ? (ep_stack_contents_get_next_available_frame (stack_contents) * sizeof (uintptr_t) * 2) + sizeof (uint32_t) : sizeof (uint32_t);
#else /* EP_CHECKED_BUILD */
	return stack_contents ? (ep_stack_contents_get_next_available_frame (stack_contents) * sizeof (uintptr_t)) + sizeof (uint32_t) : sizeof (uint32_t);
#endif
}

/*
 * EventPipeStackContentsInstance.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_STACK_CONTENTS_GETTER_SETTER)
struct _EventPipeStackContentsInstance {
#else
struct _EventPipeStackContentsInstance_Internal {
#endif
	// The next available slot in stack_frames.
	uint32_t next_available_frame;
	// Array of IP values from a stack crawl.
	// Top of stack is at index 0.
	// In checked builds, there is room for a
	// parallel array of ep_rt_method_desc_t*
	// starting at (stack_data + next_available_frame)
	uintptr_t stack_frames [1];
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_STACK_CONTENTS_GETTER_SETTER)
struct _EventPipeStackContentsInstance {
	uint8_t _internal [sizeof (struct _EventPipeStackContentsInstance_Internal)];
};
#endif

EP_DEFINE_GETTER_ARRAY_REF(EventPipeStackContentsInstance *, stack_contents_instance, uintptr_t *, const uintptr_t *, stack_frames, stack_frames[0])
EP_DEFINE_GETTER(EventPipeStackContentsInstance *, stack_contents_instance, uint32_t, next_available_frame)
EP_DEFINE_SETTER(EventPipeStackContentsInstance *, stack_contents_instance, uint32_t, next_available_frame)

#ifdef EP_CHECKED_BUILD
static
inline
ep_rt_method_desc_t *const*
ep_stack_contents_instance_get_methods_ref (EventPipeStackContentsInstance *stack_contents_instance)
{
	return (ep_rt_method_desc_t *const*)(ep_stack_contents_instance_get_stack_frames_ref (stack_contents_instance) + ep_stack_contents_instance_get_next_available_frame (stack_contents_instance));
}

static
inline
ep_rt_method_desc_t *const*const
ep_stack_contents_instance_get_methods_cref (EventPipeStackContentsInstance *stack_contents_instance)
{
	return (ep_rt_method_desc_t *const*const)ep_stack_contents_instance_get_methods_ref (stack_contents_instance);
}

static
inline
ep_rt_method_desc_t *
ep_stack_contents_instance_get_method (
	EventPipeStackContentsInstance *stack_contents,
	uint32_t frame_index)
{
	EP_ASSERT (frame_index < EP_MAX_STACK_DEPTH);
	if (frame_index >= EP_MAX_STACK_DEPTH)
		return NULL;

	return ep_stack_contents_instance_get_methods_cref (stack_contents)[frame_index];
}
#endif

EventPipeStackContentsInstance *
ep_stack_contents_instance_alloc (void);

EventPipeStackContentsInstance *
ep_stack_contents_instance_init (EventPipeStackContentsInstance *stack_contents);

void
ep_stack_contents_instance_fini (EventPipeStackContentsInstance *stack_contents);

void
ep_stack_contents_instance_free (EventPipeStackContentsInstance *stack_contents);

static
inline
void
ep_stack_contents_instance_reset (EventPipeStackContentsInstance *stack_contents)
{
	ep_stack_contents_instance_set_next_available_frame (stack_contents, 0);
}

static
inline
uint32_t
ep_stack_contents_instance_get_size (const EventPipeStackContentsInstance *stack_contents)
{
	EP_ASSERT (stack_contents != NULL);
	return (ep_stack_contents_instance_get_next_available_frame (stack_contents) * sizeof (uintptr_t));
}

static
inline
uint32_t
ep_stack_contents_instance_get_length (EventPipeStackContentsInstance *stack_contents)
{
	return ep_stack_contents_instance_get_next_available_frame (stack_contents);
}

static
inline
uint32_t
ep_stack_contents_instance_get_total_size (const EventPipeStackContentsInstance *stack_contents_instance)
{
	// The total size including the size
#ifdef EP_CHECKED_BUILD
	return stack_contents_instance ? (ep_stack_contents_instance_get_next_available_frame (stack_contents_instance) * sizeof (uintptr_t) * 2) + sizeof (uint32_t) : sizeof (uint32_t);
#else /* EP_CHECKED_BUILD */
	return stack_contents_instance ? (ep_stack_contents_instance_get_next_available_frame (stack_contents_instance) * sizeof (uintptr_t)) + sizeof (uint32_t) : sizeof (uint32_t);
#endif
}

static
inline
bool
ep_stack_contents_instance_is_empty (EventPipeStackContentsInstance *stack_contents)
{
	return (ep_stack_contents_instance_get_next_available_frame (stack_contents) == 0);
}

static
inline
uint8_t *
ep_stack_contents_instance_get_pointer (const EventPipeStackContentsInstance *stack_contents_instance)
{
	EP_ASSERT (stack_contents_instance != NULL);
	return (uint8_t *)ep_stack_contents_instance_get_stack_frames_cref (stack_contents_instance);
}

static
inline
void
ep_stack_contents_flatten (
	EventPipeStackContents *stack_contents,
	EventPipeStackContentsInstance *dest)
{
	ep_stack_contents_instance_set_next_available_frame (dest, ep_stack_contents_get_next_available_frame (stack_contents));

	memcpy (
		ep_stack_contents_instance_get_stack_frames_ref (dest),
		ep_stack_contents_get_stack_frames_ref (stack_contents),
		ep_stack_contents_get_next_available_frame (stack_contents) * sizeof (uintptr_t));

#ifdef EP_CHECKED_BUILD
	memcpy (
		(void*)ep_stack_contents_instance_get_methods_ref (dest),
		ep_stack_contents_get_methods_ref (stack_contents),
		ep_stack_contents_get_next_available_frame (stack_contents) * sizeof (ep_rt_method_desc_t *));
#endif
}

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_STACK_CONTENTS_H__ */
