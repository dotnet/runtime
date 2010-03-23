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
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

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
	TIMESPEC ts;
	struct timeval t;

#ifndef USE_MACH_SEMA
	if (timeout_ms == 0)
		return (!sem_trywait (sem));
#endif
	if (timeout_ms == (guint32) 0xFFFFFFFF)
		return MONO_SEM_WAIT (sem);

	t.tv_sec = 0;
	t.tv_usec = 0;
	gettimeofday (&t, NULL);
	ts.tv_sec = timeout_ms / 1000 + t.tv_sec;
	ts.tv_nsec = (timeout_ms % 1000) * 1000000 + t.tv_usec * 1000;
	while (ts.tv_nsec > 1000000000) {
		ts.tv_nsec -= 1000000000;
		ts.tv_sec++;
	}
	return (!WAIT_BLOCK (sem, &ts));
}

#else
/* Windows or io-layer functions in use */
gboolean
mono_sem_timedwait (MonoSemType *sem, guint32 timeout_ms)
{
	return WaitForSingleObjectEx (*sem, timeout_ms, TRUE);
}

#endif

