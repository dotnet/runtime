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

const MonoObjectHandle mono_null_value_handle = NULL;

#define THIS_IS_AN_OK_NUMBER_OF_HANDLES 100

/* Actual handles implementation */
MonoRawHandle
mono_handle_new (MonoObject *object)
{
	MonoThreadInfo *info = mono_thread_info_current ();
	HandleStack *handles = (HandleStack *)info->handle_stack;
	HandleChunk *top = handles->top;

retry:
	if (G_LIKELY (top->size < OBJECTS_PER_HANDLES_CHUNK)) {
		MonoObject **h = &top->objects [top->size++];
		*h = object;
		return h;
	}
	if (G_LIKELY (top->next)) {
		top = top->next;
		top->size = 0;
		handles->top = top;
		goto retry;
	}
	HandleChunk *new_chunk = g_new (HandleChunk, 1);
	new_chunk->size = 0;
	new_chunk->prev = top;
	new_chunk->next = NULL;
	top->next = new_chunk;
	handles->top = new_chunk;
	goto retry;
}



HandleStack*
mono_handle_stack_alloc (void)
{
	HandleStack *stack = g_new (HandleStack, 1);
	HandleChunk *chunk = g_new (HandleChunk, 1);

	stack->top = stack->bottom = chunk;
	chunk->size = 0;
	chunk->prev = chunk->next = NULL;
	return stack;
}

void
mono_handle_stack_free (HandleStack *stack)
{
	if (!stack)
		return;
	HandleChunk *c = stack->bottom;
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
	HandleChunk *cur = stack->bottom;
	HandleChunk *last = stack->top;

	if (!cur)
		return;

	while (cur) {
		int i;
		for (i = 0; i < cur->size; ++i)
			func ((gpointer*)&cur->objects [i], gc_data);
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
		printf ("%s USED %d handles\n", func_name, size);
}

/*
 * Pop the stack until @stackmark and make @value the top value.
 *
 * @return the new handle for what @value points to 
 */
MonoRawHandle
mono_stack_mark_pop_value (MonoThreadInfo *info, HandleStackMark *stackmark, MonoRawHandle value)
{
	g_error ("impl me");
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
