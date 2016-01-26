/*
 * handle.c: Handle to object in native code
 *
 * Authors:
 *  - Ludovic Henry <ludovic@xamarin.com>
 *
 * Copyright 2015 Xamarin, Inc. (www.xamarin.com)
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/handle.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-threads.h>

#define HANDLES_PER_CHUNK (16 - 2)

typedef struct _MonoHandleArenaChunk MonoHandleArenaChunk;
struct _MonoHandleArenaChunk {
	/* if next is NULL, this is the first chunk.
	 *
	 * The first chunk is special - it was allocated together with
	 * its owning arena and must not be deallocated unless the
	 * arena is being deallocated.  N.B: Arenas are
	 * stack-allocated.
	 */
	MonoHandleArenaChunk *next;
	gsize handles_size;
	MonoHandleStorage handles [HANDLES_PER_CHUNK];
};

struct _MonoHandleArena {
	MonoHandleArenaChunk *chunk;
	MonoHandleArena *prev;
};

static mono_lazy_init_t arena_status = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;

static MonoGCDescriptor arena_desc = MONO_GC_DESCRIPTOR_NULL;

static MonoHandleArenaChunk *chunk_free_list = NULL;

static inline MonoHandleArenaChunk*
chunk_alloc (void)
{
	MonoHandleArenaChunk *old, *new;

	do {
		old = chunk_free_list;
		if (!old) {
			MonoHandleArenaChunk *chunk;

			chunk = g_malloc0 (sizeof (MonoHandleArenaChunk));
			g_assert (chunk);

			return chunk;
		}

		new = old->next;
	} while (InterlockedCompareExchangePointer ((gpointer*) &chunk_free_list, new, old) != old);

	memset (old, 0, sizeof (MonoHandleArenaChunk));
	return old;
}

static inline void
chunk_free (MonoHandleArenaChunk *chunk)
{
	if (chunk == NULL)
		return;
	while (chunk->next != NULL) {
		MonoHandleArenaChunk *next = chunk->next;
		chunk->next = NULL;
		do {
			chunk->next = chunk_free_list;
		} while (InterlockedCompareExchangePointer ((gpointer*) &chunk_free_list, chunk, chunk->next) != chunk->next);
		chunk = next;
	}
}

static MonoHandle
handle_new (MonoHandleArena *arena, MonoObject *obj)
{
	MonoHandleArenaChunk *chunk;

	g_assert (arena->chunk);

	chunk = arena->chunk;

	if (chunk->handles_size < HANDLES_PER_CHUNK) {
		chunk->handles [chunk->handles_size].__private_obj = obj;
		chunk->handles_size += 1;

		return &chunk->handles [chunk->handles_size - 1];
	} else {

		MonoHandleArenaChunk *new_chunk = chunk_alloc ();
		new_chunk->next = chunk;
		arena->chunk = chunk = new_chunk;

		chunk->handles [0].__private_obj = obj;
		chunk->handles_size = 1;

		return &chunk->handles [0];
	}
}

MonoHandle
mono_handle_arena_new (MonoHandleArena *arena, MonoObject *obj)
{
	g_assert (arena);
	return handle_new (arena, obj);
}

/*
 * Elevate the handle to the parent arena
 */
MonoHandle
mono_handle_arena_elevate (MonoHandleArena *arena, MonoHandle handle)
{
	g_assert (handle);
	g_assert (arena);
	g_assert (arena->prev);

	return handle_new (arena->prev, handle->__private_obj);
}

gsize
mono_handle_arena_size (void)
{
	return sizeof (MonoHandleArena) + sizeof (MonoHandleArenaChunk);
}

void
mono_handle_arena_stack_push(MonoHandleArena **arena_stack, MonoHandleArena *arena)
{
	g_assert (arena_stack);
	g_assert (arena);

	arena->prev = *arena_stack;
	arena->chunk = (MonoHandleArenaChunk*) (((char*) arena) + sizeof (MonoHandleArena));

	arena->chunk->next = NULL;
	arena->chunk->handles_size = 0;
	memset (&arena->chunk->handles [0], 0, sizeof (MonoHandleStorage) * HANDLES_PER_CHUNK);

	*arena_stack = arena;
}

void
mono_handle_arena_stack_pop(MonoHandleArena **arena_stack, MonoHandleArena *arena)
{
	MonoHandleArenaChunk *chunk, *next;

	g_assert (arena);
	g_assert (arena->chunk);
	g_assert (arena_stack);

	*arena_stack = arena->prev;

	for (chunk = arena->chunk; chunk; chunk = next) {
		next = chunk->next;
		memset (&chunk->handles [0], 0, sizeof (MonoHandleStorage) * HANDLES_PER_CHUNK);
		if (next != NULL)
			chunk_free (chunk);
	}
}

static void
arena_scan (gpointer addr, MonoGCMarkFunc mark_func, gpointer gc_data)
{
	MonoHandleArena *arena;
	MonoHandleArenaChunk *chunk;
	int i;

	for (arena = *(MonoHandleArena**) addr; arena; arena = arena->prev) {
		for (chunk = arena->chunk; chunk; chunk = chunk->next) {
			for (i = 0; i < chunk->handles_size; ++i) {
				if (chunk->handles [i].__private_obj != NULL)
					mark_func (&chunk->handles [i].__private_obj, gc_data);
			}
		}
	}
}

static void
initialize (void)
{
	arena_desc = mono_gc_make_root_descr_user (arena_scan);
}

void
mono_handle_arena_init (MonoHandleArena **arena_stack)
{
	mono_lazy_initialize (&arena_status, initialize);
	mono_gc_register_root ((char*) arena_stack, sizeof (MonoHandleArena*), arena_desc, MONO_ROOT_SOURCE_HANDLE, "runtime threads handle arena");
}

void
mono_handle_arena_cleanup (MonoHandleArena **arena_stack)
{
	mono_gc_deregister_root ((char*) arena_stack);
}

MonoHandleArena*
mono_handle_arena_current (void)
{
	return (MonoHandleArena*) mono_thread_info_current ()->handle_arena;
}

MonoHandleArena**
mono_handle_arena_current_addr (void)
{
	return (MonoHandleArena**) &mono_thread_info_current ()->handle_arena;
}
