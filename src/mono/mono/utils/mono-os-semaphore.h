/*
 * mono-os-semaphore.h:  Definitions for generic semaphore usage
 *
 * Author:
 *	Geoff Norton  <gnorton@novell.com>
 *
 * (C) 2009 Novell, Inc.
 */

#ifndef _MONO_SEMAPHORE_H_
#define _MONO_SEMAPHORE_H_

#include <config.h>
#include <glib.h>

#include <errno.h>

#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#if defined(USE_MACH_SEMA)
#include <mach/mach_init.h>
#include <mach/task.h>
#include <mach/semaphore.h>
#elif !defined(HOST_WIN32) && defined(HAVE_SEMAPHORE_H)
#include <semaphore.h>
#else
#include <winsock2.h>
#include <windows.h>
#endif

#define MONO_HAS_SEMAPHORES 1

#ifndef NSEC_PER_SEC
#define NSEC_PER_SEC (1000 * 1000 * 1000)
#endif

G_BEGIN_DECLS

typedef enum {
	MONO_SEM_FLAGS_NONE      = 0,
	MONO_SEM_FLAGS_ALERTABLE = 1 << 0,
} MonoSemFlags;

#if defined(USE_MACH_SEMA)

typedef semaphore_t MonoSemType;

static inline int
mono_os_sem_init (MonoSemType *sem, int value)
{
	return semaphore_create (current_task (), sem, SYNC_POLICY_FIFO, value) != KERN_SUCCESS ? -1 : 0;
}

static inline int
mono_os_sem_destroy (MonoSemType *sem)
{
	return semaphore_destroy (current_task (), *sem) != KERN_SUCCESS ? -1 : 0;
}

static inline int
mono_os_sem_wait (MonoSemType *sem, MonoSemFlags flags)
{
	int res;

retry:
	res = semaphore_wait (*sem);
	g_assert (res != KERN_INVALID_ARGUMENT);

	if (res == KERN_ABORTED && !(flags & MONO_SEM_FLAGS_ALERTABLE))
		goto retry;

	return res != KERN_SUCCESS ? -1 : 0;
}

static inline int
mono_os_sem_timedwait (MonoSemType *sem, guint32 timeout_ms, MonoSemFlags flags)
{
	mach_timespec_t ts, copy;
	struct timeval start, current;
	int res = 0;

	if (timeout_ms == (guint32) 0xFFFFFFFF)
		return mono_os_sem_wait (sem, flags);

	ts.tv_sec = timeout_ms / 1000;
	ts.tv_nsec = (timeout_ms % 1000) * 1000000;
	while (ts.tv_nsec >= NSEC_PER_SEC) {
		ts.tv_nsec -= NSEC_PER_SEC;
		ts.tv_sec++;
	}

	copy = ts;
	gettimeofday (&start, NULL);

retry:
	res = semaphore_timedwait (*sem, ts);
	g_assert (res != KERN_INVALID_ARGUMENT);

	if (res == KERN_ABORTED && !(flags & MONO_SEM_FLAGS_ALERTABLE)) {
		ts = copy;

		gettimeofday (&current, NULL);
		ts.tv_sec -= (current.tv_sec - start.tv_sec);
		ts.tv_nsec -= (current.tv_usec - start.tv_usec) * 1000;
		if (ts.tv_nsec < 0) {
			if (ts.tv_sec <= 0) {
				ts.tv_nsec = 0;
			} else {
				ts.tv_sec--;
				ts.tv_nsec += NSEC_PER_SEC;
			}
		}
		if (ts.tv_sec < 0) {
			ts.tv_sec = 0;
			ts.tv_nsec = 0;
		}

		goto retry;
	}

	return res != KERN_SUCCESS ? -1 : 0;
}

static inline int
mono_os_sem_post (MonoSemType *sem)
{
	int res;

	res = semaphore_signal (*sem);
	g_assert (res != KERN_INVALID_ARGUMENT);

	return res != KERN_SUCCESS ? -1 : 0;
}

#elif !defined(HOST_WIN32) && defined(HAVE_SEMAPHORE_H)

typedef sem_t MonoSemType;

static inline int
mono_os_sem_init (MonoSemType *sem, int value)
{
	return sem_init (sem, 0, value);
}

static inline int
mono_os_sem_destroy (MonoSemType *sem)
{
	return sem_destroy (sem);
}

static inline int
mono_os_sem_wait (MonoSemType *sem, MonoSemFlags flags)
{
	int res;

retry:
	res = sem_wait (sem);
	if (res == -1)
		g_assert (errno != EINVAL);

	if (res == -1 && errno == EINTR && !(flags & MONO_SEM_FLAGS_ALERTABLE))
		goto retry;

	return res != 0 ? -1 : 0;
}

static inline int
mono_os_sem_timedwait (MonoSemType *sem, guint32 timeout_ms, MonoSemFlags flags)
{
	struct timespec ts, copy;
	struct timeval t;
	int res = 0;

	if (timeout_ms == 0) {
		res = sem_trywait (sem) != 0 ? -1 : 0;
		if (res == -1)
			g_assert (errno != EINVAL);

		return res != 0 ? -1 : 0;
	}

	if (timeout_ms == (guint32) 0xFFFFFFFF)
		return mono_os_sem_wait (sem, flags);

	gettimeofday (&t, NULL);
	ts.tv_sec = timeout_ms / 1000 + t.tv_sec;
	ts.tv_nsec = (timeout_ms % 1000) * 1000000 + t.tv_usec * 1000;
	while (ts.tv_nsec >= NSEC_PER_SEC) {
		ts.tv_nsec -= NSEC_PER_SEC;
		ts.tv_sec++;
	}

	copy = ts;

retry:
#if defined(__native_client__) && defined(USE_NEWLIB)
	res = sem_trywait (sem);
#else
	res = sem_timedwait (sem, &ts);
#endif
	if (res == -1)
		g_assert (errno != EINVAL);

	if (res == -1 && errno == EINTR && !(flags & MONO_SEM_FLAGS_ALERTABLE)) {
		ts = copy;
		goto retry;
	}

	return res != 0 ? -1 : 0;
}

static inline int
mono_os_sem_post (MonoSemType *sem)
{
	int res;

	res = sem_post (sem);
	if (res == -1)
		g_assert (errno != EINVAL);

	return res;
}

#else

typedef HANDLE MonoSemType;

static inline int
mono_os_sem_init (MonoSemType *sem, int value)
{
	*sem = CreateSemaphore (NULL, value, 0x7FFFFFFF, NULL);
	return *sem == NULL ? -1 : 0;
}

static inline int
mono_os_sem_destroy (MonoSemType *sem)
{
	return !CloseHandle (*sem) ? -1 : 0;
}

static inline int
mono_os_sem_timedwait (MonoSemType *sem, guint32 timeout_ms, MonoSemFlags flags)
{
	gboolean res;

retry:
	res = WaitForSingleObjectEx (*sem, timeout_ms, flags & MONO_SEM_FLAGS_ALERTABLE);

	if (res == WAIT_IO_COMPLETION && !(flags & MONO_SEM_FLAGS_ALERTABLE))
		goto retry;

	return res != WAIT_OBJECT_0 ? -1 : 0;
}

static inline int
mono_os_sem_wait (MonoSemType *sem, MonoSemFlags flags)
{
	return mono_os_sem_timedwait (sem, INFINITE, flags);
}

static inline int
mono_os_sem_post (MonoSemType *sem)
{
	return !ReleaseSemaphore (*sem, 1, NULL) ? -1 : 0;
}

#endif

G_END_DECLS

#endif /* _MONO_SEMAPHORE_H_ */
