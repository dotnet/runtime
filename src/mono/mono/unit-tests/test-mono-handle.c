/*
 * test-mono-handle: tests for MonoHandle and MonoHandleArena
 *
 * Authors:
 *   Aleksey Kliger <aleksey@xamarin.com>
 *
 * Copyright 2015 Xamarin, Inc. (www.xamarin.com)
 */

#include <config.h>
#include <glib.h>
#include <mono/metadata/handle.h>
#include <mono/metadata/handle-private.h>

static void
test1_arena_size ()
{
	for (gsize i = 1; i < 10; ++i) {
		gsize sz = mono_handle_arena_size(i);
		g_assert(sz >= i*sizeof(MonoHandle));
	}
}

static void
test2_arena_push_pop ()
{
	MonoHandleArena *top = NULL;

	const int n_handles = 3;
	MonoHandleArena *new_arena1 = g_malloc0 (mono_handle_arena_size (n_handles));
	mono_handle_arena_stack_push (&top, new_arena1, n_handles);

	MonoHandleArena *new_arena2 = g_malloc0 (mono_handle_arena_size (n_handles));

	mono_handle_arena_stack_push (&top, new_arena2, n_handles);

	g_assert (top == new_arena2);

	mono_handle_arena_stack_pop (&top, new_arena2, n_handles);

	g_free (new_arena2);

	g_assert (top == new_arena1);

	mono_handle_arena_stack_pop (&top, new_arena1, n_handles);

	g_assert (top == NULL);
	
	g_free (new_arena1);
}



int
main (int argc, const char* argv[])
{
	test1_arena_size ();
	
	test2_arena_push_pop ();

	return 0;
}
