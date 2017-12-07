/**
 * \file
 * Handle to object in native code
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
#ifdef HAVE_BACKTRACE_SYMBOLS
#include <execinfo.h>
#endif

/* TODO (missing pieces)

Add counters for:
	number of stack marks
	stack marks per icall
	mix/max/avg size of stack marks
	handle stack wastage

Actually do something in mono_handle_verify

Shrink the handles stack in mono_handle_stack_scan
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

static HandleStack*
new_handle_stack (void)
{
	return g_new (HandleStack, 1);
}

static void
free_handle_stack (HandleStack *stack)
{
	g_free (stack);
}

static HandleChunk*
new_handle_chunk (void)
{
	return g_new (HandleChunk, 1);
}

static void
free_handle_chunk (HandleChunk *chunk)
{
	g_free (chunk);
}

const MonoObjectHandle mono_null_value_handle = NULL;

#define THIS_IS_AN_OK_NUMBER_OF_HANDLES 100

static HandleChunkElem*
chunk_element (HandleChunk *chunk, int idx)
{
	return &chunk->elems[idx];
}

static HandleChunkElem*
handle_to_chunk_element (MonoObjectHandle o)
{
	return (HandleChunkElem*)o;
}

/* Given a HandleChunkElem* search through the current handle stack to find its chunk and offset. */
static HandleChunk*
chunk_element_to_chunk_idx (HandleStack *stack, HandleChunkElem *elem, int *out_idx)
{
	HandleChunk *top = stack->top;
	HandleChunk *cur = stack->bottom;

	*out_idx = 0;

	while (cur != NULL) {
		HandleChunkElem *front = &cur->elems [0];
		HandleChunkElem *back = &cur->elems [cur->size];

		if (front <= elem && elem < back) {
			*out_idx = (int)(elem - front);
			return cur;
		}

		if (cur == top)
			break; /* didn't find it. */
		cur = cur->next;
	}
	return NULL;
}

#ifdef MONO_HANDLE_TRACK_OWNER
#ifdef HAVE_BACKTRACE_SYMBOLS
#define SET_BACKTRACE(btaddrs) do {					\
	backtrace(btaddrs, 7);						\
	} while (0)
#else
#define SET_BACKTRACE(btaddrs) 0
#endif
#define SET_OWNER(chunk,idx) do { (chunk)->elems[(idx)].owner = owner; SET_BACKTRACE (&((chunk)->elems[(idx)].backtrace_ips[0])); } while (0)
#else
#define SET_OWNER(chunk,idx) do { } while (0)
#endif

#ifdef MONO_HANDLE_TRACK_SP
#define SET_SP(handles,chunk,idx) do { (chunk)->elems[(idx)].alloc_sp = handles->stackmark_sp; } while (0)
#else
#define SET_SP(handles,chunk,idx) do { } while (0)
#endif

#ifdef MONO_HANDLE_TRACK_SP
void
mono_handle_chunk_leak_check (HandleStack *handles) {
	if (handles->stackmark_sp) {
		/* walk back from the top to the topmost non-empty chunk */
		HandleChunk *c = handles->top;
		while (c && c->size <= 0 && c != handles->bottom) {
			c = c->prev;
		}
		if (c == NULL || c->size == 0)
			return;
		g_assert (c && c->size > 0);
		HandleChunkElem *e = chunk_element (c, c->size - 1);
		if (e->alloc_sp < handles->stackmark_sp) {
			/* If we get here, the topmost object on the handle stack was
			 * allocated from a function that is deeper in the call stack than
			 * the most recent HANDLE_FUNCTION_ENTER.  That means it was
			 * probably not wrapped in a HANDLE_FUNCTION_ENTER/_RETURN pair
			 * and will never be reclaimed. */
			g_warning ("Handle %p (object = %p) (allocated from \"%s\") is leaking.\n", e, e->o,
#ifdef MONO_HANDLE_TRACK_OWNER
				   e->owner
#else
				   "<unknown owner>"
#endif
				);
		}
	}
}
#endif

/* Actual handles implementation */
MonoRawHandle
#ifndef MONO_HANDLE_TRACK_OWNER
mono_handle_new (MonoObject *obj)
#else
mono_handle_new (MonoObject *obj, const char *owner)
#endif
{
	MonoThreadInfo *info = mono_thread_info_current ();
	HandleStack *handles = (HandleStack *)info->handle_stack;
	HandleChunk *top = handles->top;
#ifdef MONO_HANDLE_TRACK_SP
	mono_handle_chunk_leak_check (handles);
#endif

retry:
	if (G_LIKELY (top->size < OBJECTS_PER_HANDLES_CHUNK)) {
		int idx = top->size;
		gpointer* objslot = &top->elems [idx].o;
		/* can be interrupted anywhere here, so:
		 * 1. make sure the new slot is null
		 * 2. make the new slot scannable (increment size)
		 * 3. put a valid object in there
		 *
		 * (have to do 1 then 3 so that if we're interrupted
		 * between 1 and 2, the object is still live)
		 */
		*objslot = NULL;
		SET_OWNER (top,idx);
		SET_SP (handles, top, idx);
		mono_memory_write_barrier ();
		top->size++;
		mono_memory_write_barrier ();
		*objslot = obj;
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
	HandleChunk *new_chunk = new_handle_chunk ();
	new_chunk->size = 0;
	new_chunk->prev = top;
	new_chunk->next = NULL;
	/* make sure size == 0 before new chunk is visible */
	mono_memory_write_barrier ();
	top->next = new_chunk;
	handles->top = new_chunk;
	goto retry;
}

MonoRawHandle
#ifndef MONO_HANDLE_TRACK_OWNER
mono_handle_new_interior (gpointer rawptr)
#else
mono_handle_new_interior (gpointer rawptr, const char *owner)
#endif
{
	MonoThreadInfo *info = mono_thread_info_current ();
	HandleStack *handles = (HandleStack *)info->handle_stack;
	HandleChunk *top = handles->interior;
#ifdef MONO_HANDLE_TRACK_SP
	mono_handle_chunk_leak_check (handles);
#endif

	g_assert (top);

	/*
	 * Don't extend the chunk now, interior handles are
	 * only used for icall arguments, they shouldn't
	 * overflow.
	 */
	g_assert (top->size < OBJECTS_PER_HANDLES_CHUNK);
	int idx = top->size;
	gpointer *objslot = &top->elems [idx].o;
	*objslot = NULL;
	mono_memory_write_barrier ();
	top->size++;
	mono_memory_write_barrier ();
	*objslot = rawptr;
	SET_OWNER (top,idx);
	SET_SP (handles, top, idx);
	return objslot;
}

HandleStack*
mono_handle_stack_alloc (void)
{
	HandleStack *stack = new_handle_stack ();
	HandleChunk *chunk = new_handle_chunk ();
	HandleChunk *interior = new_handle_chunk ();

	chunk->prev = chunk->next = NULL;
	chunk->size = 0;
	interior->prev = interior->next = NULL;
	interior->size = 0;
	mono_memory_write_barrier ();
	stack->top = stack->bottom = chunk;
	stack->interior = interior;
#ifdef MONO_HANDLE_TRACK_SP
	stack->stackmark_sp = NULL;
#endif
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
		free_handle_chunk (c);
		c = next;
	}
	free_handle_chunk (c);
	free_handle_chunk (stack->interior);
	free_handle_stack (stack);
}

void
mono_handle_stack_free_domain (HandleStack *stack, MonoDomain *domain)
{
	/* Called by the GC while clearing out objects of the given domain from the heap. */
	/* If there are no handles-related bugs, there is nothing to do: if a
	 * thread accessed objects from the domain it was aborted, so any
	 * threads left alive cannot have any handles that point into the
	 * unloading domain.  However if there is a handle leak, the handle stack is not */
	if (!stack)
		return;
	/* Root domain only unloaded when mono is shutting down, don't need to check anything */
	if (domain == mono_get_root_domain () || mono_runtime_is_shutting_down ())
		return;
	HandleChunk *cur = stack->bottom;
	HandleChunk *last = stack->top;
	if (!cur)
		return;
	while (cur) {
		for (int idx = 0; idx < cur->size; ++idx) {
			HandleChunkElem *elem = &cur->elems[idx];
			if (!elem->o)
				continue;
			g_assert (mono_object_domain (elem->o) != domain);
		}
		if (cur == last)
			break;
		cur = cur->next;
	}
	/* We don't examine the interior pointers here because the GC treats
	 * them conservatively and anyway we don't have enough information here to
	 * find the object's vtable.
	 */
}

static void
check_handle_stack_monotonic (HandleStack *stack)
{
	/* check that every allocated handle in the current handle stack is at no higher in the native stack than its predecessors */
#ifdef MONO_HANDLE_TRACK_SP
	HandleChunk *cur = stack->bottom;
	HandleChunk *last = stack->top;
	if (!cur)
		return;
	HandleChunkElem *prev = NULL;
	gboolean monotonic = TRUE;
	while (cur) {
		for (int i = 0;i < cur->size; ++i) {
			HandleChunkElem *elem = chunk_element (cur, i);
			if (prev && elem->alloc_sp > prev->alloc_sp) {
				monotonic = FALSE;
#ifdef MONO_HANDLE_TRACK_OWNER
				g_warning ("Handle %p (object %p) (allocated from \"%s\") was allocated deeper in the call stack than its successor Handle %p (object %p) (allocated from \"%s\").", prev, prev->o, prev->owner, elem, elem->o, elem->owner);
#else
				g_warning ("Handle %p (object %p) was allocated deeper in the call stack than its successor Handle %p (object %p).", prev, prev->o, elem, elem->o);
#endif
			}
			prev = elem;
		}
		if (cur == last)
			break;
		cur = cur->next;
	}
	g_assert (monotonic);
#endif
}

void
mono_handle_stack_scan (HandleStack *stack, GcScanFunc func, gpointer gc_data, gboolean precise, gboolean check)
{
	if (check) /* run just once (per handle stack) per GC */
		check_handle_stack_monotonic (stack);

	/*
	  We're called twice - on the imprecise pass we call func to pin the
	  objects where the handle points to its interior.  On the precise
	  pass, we scan all the objects where the handles point to the start of
	  the object.

	  Note that if we're running, we know the world is stopped.
	*/
	if (precise) {
		HandleChunk *cur = stack->bottom;
		HandleChunk *last = stack->top;

		while (cur) {
			for (int i = 0; i < cur->size; ++i) {
				HandleChunkElem* elem = chunk_element (cur, i);
				gpointer* obj_slot = &elem->o;
				if (*obj_slot != NULL)
					func (obj_slot, gc_data);
			}
			if (cur == last)
				break;
			cur = cur->next;
		}
	} else {
		HandleChunk *cur = stack->interior;

		if (!cur)
			return;
		for (int i = 0; i < cur->size; ++i) {
			HandleChunkElem* elem = chunk_element (cur, i);
			gpointer* ptr_slot = &elem->o;
			if (*ptr_slot != NULL)
				func (ptr_slot, gc_data);
		}
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

MonoArrayHandle
mono_array_new_full_handle (MonoDomain *domain, MonoClass *array_class, uintptr_t *lengths, intptr_t *lower_bounds, MonoError *error)
{
	return MONO_HANDLE_NEW (MonoArray, mono_array_new_full_checked (domain, array_class, lengths, lower_bounds, error));
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
	/* FIXME: chunk_element_to_chunk_idx does a linear search through the
	 * chunks and we only need it for the assert */
	MonoThreadInfo *info = mono_thread_info_current ();
	HandleStack *stack = (HandleStack*) info->handle_stack;
	HandleChunkElem* elem = handle_to_chunk_element (handle);
	int elem_idx = 0;
	HandleChunk *chunk = chunk_element_to_chunk_idx (stack, elem, &elem_idx);
	/* gchandles cannot deal with interior pointers */
	g_assert (chunk != NULL);
	return mono_gchandle_new (MONO_HANDLE_RAW (handle), pinned);
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

gunichar2*
mono_string_handle_pin_chars (MonoStringHandle handle, uint32_t *gchandle)
{
	g_assert (gchandle != NULL);
	*gchandle = mono_gchandle_from_handle (MONO_HANDLE_CAST (MonoObject, handle), TRUE);
	MonoString *raw = MONO_HANDLE_RAW (handle);
	return mono_string_chars (raw);
}

gpointer
mono_object_handle_pin_unbox (MonoObjectHandle obj, uint32_t *gchandle)
{
	g_assert (!MONO_HANDLE_IS_NULL (obj));
	MonoClass *klass = mono_handle_class (obj);
	g_assert (klass->valuetype);
	*gchandle = mono_gchandle_from_handle (obj, TRUE);
	return mono_object_unbox (MONO_HANDLE_RAW (obj));
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
