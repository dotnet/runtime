/*
 * threads-types.h: Generic thread typedef support (includes
 * system-specific files)
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc
 */

#ifndef _MONO_METADATA_THREADS_TYPES_H_
#define _MONO_METADATA_THREADS_TYPES_H_


#include <mono/io-layer/io-layer.h>

/*
 * This is bonkers. We are emulating condition variables here using
 * win32 calls, which on Linux are being emulated with condition
 * variables :-)
 *
 * See http://www.cs.wustl.edu/~schmidt/win32-cv-1.html for the design.
 */

typedef struct 
{
	HANDLE monitor;
	guint32 tid;

	/* Need to count how many times this monitor object has been
	 * locked by the owning thread, so that we can unlock it
	 * completely in Wait()
	 */
	guint32 count;

	/* condition variable management */
	guint32 waiters_count;
	CRITICAL_SECTION waiters_count_lock;
	HANDLE sema;
	HANDLE waiters_done;
	gboolean was_broadcast;
} MonoThreadsSync;

#endif /* _MONO_METADATA_THREADS_TYPES_H_ */
