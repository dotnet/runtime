/*
 * handle.c: Handle to object in native code
 *
 * Authors:
 *  - Ludovic Henry <ludovic@xamarin.com>
 *  - Aleksey Klieger <aleksey.klieger@xamarin.com>
 *  - Rodrigo Kumpera <kumpera@xamarin.com>
 *
 * Copyright 2016 Dot net foundation.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/handle.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-threads.h>
/* TODO (missing pieces)

Add counters for:
	number of stack marks
	stack marks per icall
	mix/max/avg size of stack marks
	handle stack wastage

Actually do something in mono_handle_verify

Shrink the handles stack in mono_handle_stack_scan
Properly report it to the profiler.
Add a boehm implementation

TODO (things to explore):

There's no convenient way to wrap the object allocation function.
Right now we do this:
	MonoCultureInfoHandle culture = MONO_HANDLE_NEW (MonoCultureInfo, mono_object_new_checked (domain, klass, &error));

Maybe what we need is a round of cleanup around all exposed types in the runtime to unify all helpers under the same hoof.
Combine: MonoDefaults, GENERATE_GET_CLASS_WITH_CACHE, TYPED_HANDLE_DECL and friends.
	This would solve the age old issue of making it clear which types are optional and tell that to the linker.
	We could then generate neat type safe wrappers.
*/

/*
 * NOTE: Async suspend
 * 
 * If we are running with cooperative GC, all the handle stack
 * manipulation will complete before a GC thread scans the handle
 * stack. If we are using async suspend, however, a thread may be
 * trying to allocate a new handle, or unwind the handle stack when
 * the GC stops the world.
 *
 * In particular, we need to ensure that if the mutator thread is
 * suspended while manipulating the handle stack, the stack is in a
 * good enough state to be scanned.  In particular, the size of each
 * chunk should be updated before an object is written into the
 * handle, and chunks to be scanned (between bottom and top) should
 * always be valid.
 *
 * Note that the handle stack is scanned PRECISELY (see
 * sgen_client_scan_thread_data ()).  That means there should not be
 * stale objects scanned.  So when we manipulate the size of a chunk,
 * wemust ensure that the newly scannable slot is either null or
 * points to a valid value.
 */

const MonoObjectHandle mono_null_value_handle = NULL;

#define THIS_IS_AN_OK_NUMBER_OF_HANDLES 100

static MonoObject**
chunk_element_objslot (HandleChunk *chunk, int idx)
{
	return &chunk->objects[idx].o;
}

#ifdef MONO_HANDLE_TRACK_OWNER
#define SET_OWNER(chunk,idx) do { (chunk)->objects[(idx)].owner = owner; } while (0)
#else
#define SET_OWNER(chunk,idx) do { } while (0)
#endif

/* Actual handles implementation */
MonoRawHandle
#ifndef MONO_HANDLE_TRACK_OWNER
mono_handle_new (MonoObject *object)
#else
mono_handle_new (MonoObject *object, const char *owner)
#endif
{
	MonoThreadInfo *info = mono_thread_info_current ();
	HandleStack *handles = (HandleStack *)info->handle_stack;
	HandleChunk *top = handles->top;

retry:
	if (G_LIKELY (top->size < OBJECTS_PER_HANDLES_CHUNK)) {
		int idx = top->size;
		MonoObject** objslot = chunk_element_objslot (top, idx);
		/* can be interrupted anywhere here, so:
		 * 1. make sure the new slot is null
		 * 2. make the new slot scannable (increment size)
		 * 3. put a valid object in there
		 *
		 * (have to do 1 then 3 so that if we're interrupted
		 * between 1 and 2, the object is still live)
		 */
		*objslot = NULL;
		mono_memory_write_barrier ();
		top->size++;
		mono_memory_write_barrier ();
		*objslot = object;
		SET_OWNER (top,idx);
		return objslot;
	}
	if (G_LIKELY (top->next)) {
		top->next->size = 0;
		/* make sure size == 0 is visible to a GC thread before it sees the new top */
		mono_memory_write_barrier ();
		top = top->next;
		handles->top = top;
		goto retry;
	}
	HandleChunk *new_chunk = g_new (HandleChunk, 1);
	new_chunk->size = 0;
	new_chunk->prev = top;
	new_chunk->next = NULL;
	/* make sure size == 0 before new chunk is visible */
	mono_memory_write_barrier ();
	top->next = new_chunk;
	handles->top = new_chunk;
	goto retry;
}



HandleStack*
mono_handle_stack_alloc (void)
{
	HandleStack *stack = g_new (HandleStack, 1);
	HandleChunk *chunk = g_new (HandleChunk, 1);

	chunk->size = 0;
	chunk->prev = chunk->next = NULL;
	mono_memory_write_barrier ();
	stack->top = stack->bottom = chunk;
	return stack;
}

void
mono_handle_stack_free (HandleStack *stack)
{
	if (!stack)
		return;
	HandleChunk *c = stack->bottom;
	stack->top = stack->bottom = NULL;
	mono_memory_write_barrier ();
	while (c) {
		HandleChunk *next = c->next;
		g_free (c);
		c = next;
	}
	g_free (c);
	g_free (stack);
}

void
mono_handle_stack_scan (HandleStack *stack, GcScanFunc func, gpointer gc_data)
{
	/* if we're running, we know the world is stopped.
	 */
	HandleChunk *cur = stack->bottom;
	HandleChunk *last = stack->top;

	if (!cur)
		return;

	while (cur) {
		int i;
		for (i = 0; i < cur->size; ++i) {
			MonoObject **obj_slot = chunk_element_objslot (cur, i);
			if (*obj_slot != NULL)
				func ((gpointer*)obj_slot, gc_data);
		}
		if (cur == last)
			break;
		cur = cur->next;
	}
}

void
mono_stack_mark_record_size (MonoThreadInfo *info, HandleStackMark *stackmark, const char *func_name)
{
	HandleStack *handles = (HandleStack *)info->handle_stack;
	HandleChunk *cur = stackmark->chunk;
	int size = -stackmark->size; //discard the starting point of the stack
	while (cur) {
		size += cur->size;
		if (cur == handles->top)
			break;
		cur = cur->next;
	}

	if (size > THIS_IS_AN_OK_NUMBER_OF_HANDLES)
		g_warning ("%s USED %d handles\n", func_name, size);
}

/*
 * Pop the stack until @stackmark and make @value the top value.
 *
 * @return the new handle for what @value points to 
 */
MonoRawHandle
mono_stack_mark_pop_value (MonoThreadInfo *info, HandleStackMark *stackmark, MonoRawHandle value)
{
	MonoObject *obj = value ? *((MonoObject**)value) : NULL;
	mono_stack_mark_pop (info, stackmark);
#ifndef MONO_HANDLE_TRACK_OWNER
	return mono_handle_new (obj);
#else
	return mono_handle_new (obj, "<mono_stack_mark_pop_value>");
#endif
}

/* Temporary place for some of the handle enabled wrapper functions*/

MonoStringHandle
mono_string_new_handle (MonoDomain *domain, const char *data, MonoError *error)
{
	return MONO_HANDLE_NEW (MonoString, mono_string_new_checked (domain, data, error));
}

MonoArrayHandle
mono_array_new_handle (MonoDomain *domain, MonoClass *eclass, uintptr_t n, MonoError *error)
{
	return MONO_HANDLE_NEW (MonoArray, mono_array_new_checked (domain, eclass, n, error));
}

#ifdef ENABLE_CHECKED_BUILD
/* Checked build helpers */
void
mono_handle_verify (MonoRawHandle raw_handle)
{
	
}
#endif

uintptr_t
mono_array_handle_length (MonoArrayHandle arr)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return MONO_HANDLE_RAW (arr)->max_length;
}

uint32_t
mono_gchandle_from_handle (MonoObjectHandle handle, mono_bool pinned)
{
	return mono_gchandle_new (MONO_HANDLE_RAW(handle), pinned);
}

MonoObjectHandle
mono_gchandle_get_target_handle (uint32_t gchandle)
{
	return MONO_HANDLE_NEW (MonoObject, mono_gchandle_get_target (gchandle));
}

gpointer
mono_array_handle_pin_with_size (MonoArrayHandle handle, int size, uintptr_t idx, uint32_t *gchandle)
{
	g_assert (gchandle != NULL);
	*gchandle = mono_gchandle_from_handle (MONO_HANDLE_CAST(MonoObject,handle), TRUE);
	MonoArray *raw = MONO_HANDLE_RAW (handle);
	return mono_array_addr_with_size (raw, size, idx);
}

void
mono_array_handle_memcpy_refs (MonoArrayHandle dest, uintptr_t dest_idx, MonoArrayHandle src, uintptr_t src_idx, uintptr_t len)
{
	mono_array_memcpy_refs (MONO_HANDLE_RAW (dest), dest_idx, MONO_HANDLE_RAW (src), src_idx, len);
}

gboolean
mono_handle_stack_is_empty (HandleStack *stack)
{
	return (stack->top == stack->bottom && stack->top->size == 0);
}
