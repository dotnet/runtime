/*
 * threads-pthread-types.c: System-specific thread type support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>

#include <mono/metadata/threads-types.h>

void mono_threads_synchronisation_init(MonoThreadsSync *sync)
{
	pthread_mutex_init(&sync->mutex, NULL);
	pthread_cond_init(&sync->cond, NULL);
	sync->sync_tid=0;
	sync->recursion_count=0;
}

void mono_threads_synchronisation_free(MonoThreadsSync *sync)
{
	if(sync->sync_tid!=0) {
		g_warning("Freeing synchronised object - expect deadlocks!");
	}

	g_free(sync);
}
