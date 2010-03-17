/*
 * mono-semaphore.c: mono-semaphore functions
 *
 * Author:
 *	Gonzalo Paniagua Javier  <gonzalo@novell.com>
 *
 * (C) 2010 Novell, Inc.
 */

#include <config.h>
#include "utils/mono-semaphore.h"

#if defined(HAVE_SEMAPHORE_H) || defined(USE_MACH_SEMA)
/* sem_* or semaphore_* functions in use */
#  ifdef USE_MACH_SEMA
#    define TIMESPEC mach_timespec_t
#    define WAIT_BLOCK(a,b) semaphore_timedwait (*(a), *(b))
#  else
#    define TIMESPEC struct timespec
#    define WAIT_BLOCK(a,b) sem_timedwait (a, b)
#  endif

gboolean
mono_sem_timedwait (MonoSemType *sem, guint32 timeout_ms)
{
	TIMESPEC tv;

#ifndef USE_MACH_SEMA
	if (timeout_ms == 0)
		return (!sem_trywait (sem));
#endif

	tv.tv_sec = time (NULL) + timeout_ms / 1000;
	tv.tv_nsec = (timeout_ms % 1000) * 1000000;
	return (!WAIT_BLOCK (sem, &tv));
}

#else
/* Windows or io-layer functions in use */
gboolean
mono_sem_timedwait (MonoSemType *sem, guint32 timeout_ms)
{
	return WaitForSingleObjectEx (*sem, timeout_ms, TRUE);
}

#endif

