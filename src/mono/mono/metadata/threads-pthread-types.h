/*
 * threads-pthread-types.h: System-specific thread type support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_METADATA_THREADS_PTHREAD_TYPES_H_
#define _MONO_METADATA_THREADS_PTHREAD_TYPES_H_

#include <glib.h>
#include <pthread.h>

typedef struct 
{
	pthread_mutex_t mutex;
	pthread_cond_t cond;
	pthread_t sync_tid;
	guint recursion_count;	/* Need to simulate recursive mutexes, cos
				 * you can't mix timed and recursive variants
				 */
} MonoThreadsSync;


#endif /* _MONO_METADATA_THREADS_PTHREAD_TYPES_H_ */
