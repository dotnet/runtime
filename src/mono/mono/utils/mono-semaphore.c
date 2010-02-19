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
#    define WAIT_NOBLOCK(a) semaphore_wait_noblock (a)
#    define WAIT_BLOCK(a,b) semaphore_timedwait (a, b)
#  else
#    define TIMESPEC struct timespec
#    define WAIT_NOBLOCK(a) sem_trywait (a)
#    define WAIT_BLOCK(a,b) sem_timedwait (a, b)
#  endif

gboolean
mono_sem_timedwait (MonoSemType *sem, guint32 timeout_ms)
{
	TIMESPEC tv;

	if (timeout_ms == 0)
		return (!WAIT_NOBLOCK (sem));

	tv.tv_sec = timeout_ms / 1000;
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

