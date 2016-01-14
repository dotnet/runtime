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

static void
test2_arena_push_pop ()
{
	MonoHandleArena *top = NULL;

	MonoHandleArena *new_arena1 = g_malloc0 (mono_handle_arena_size ());
	mono_handle_arena_stack_push (&top, new_arena1);

	MonoHandleArena *new_arena2 = g_malloc0 (mono_handle_arena_size ());

	mono_handle_arena_stack_push (&top, new_arena2);

	g_assert (top == new_arena2);

	mono_handle_arena_stack_pop (&top, new_arena2);

	g_free (new_arena2);

	g_assert (top == new_arena1);

	mono_handle_arena_stack_pop (&top, new_arena1);

	g_assert (top == NULL);
	
	g_free (new_arena1);
}



int
main (int argc, const char* argv[])
{
	test2_arena_push_pop ();

	return 0;
}
