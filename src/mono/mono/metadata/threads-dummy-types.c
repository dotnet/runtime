/*
 * threads-dummy-types.c: System-specific thread type support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/threads-types.h>

void mono_threads_synchronisation_init(MonoThreadsSync *sync)
{
}

void mono_threads_synchronisation_free(MonoThreadsSync *sync)
{
	g_free(sync);
}
