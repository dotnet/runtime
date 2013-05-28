/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 * mono-mutex.h: Portability wrappers around POSIX Mutexes
 *
 * Authors: Jeffrey Stedfast <fejj@ximian.com>
 *
 * Copyright 2002 Ximian, Inc. (www.ximian.com)
 */


#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <assert.h>
#include <sys/time.h>
#include <mono/utils/mono-memory-model.h>

#include "mono-mutex.h"

#ifndef HOST_WIN32

#if defined(__APPLE__)
#define _DARWIN_C_SOURCE
#include <pthread_spis.h>
#include <dlfcn.h>
#endif

#ifndef HAVE_PTHREAD_MUTEX_TIMEDLOCK
/* Android does not implement pthread_mutex_timedlock(), but does provide an
 * unusual declaration: http://code.google.com/p/android/issues/detail?id=7807
 */
#ifdef PLATFORM_ANDROID
#define CONST_NEEDED
#else
#define CONST_NEEDED const
#endif

int pthread_mutex_timedlock (pthread_mutex_t *mutex,
			    CONST_NEEDED struct timespec *timeout);
int
pthread_mutex_timedlock (pthread_mutex_t *mutex, CONST_NEEDED struct timespec *timeout)
{
	struct timeval timenow;
	struct timespec sleepytime;
	int retcode;
	
	/* This is just to avoid a completely busy wait */
	sleepytime.tv_sec = 0;
	sleepytime.tv_nsec = 10000000;	/* 10ms */
	
	while ((retcode = pthread_mutex_trylock (mutex)) == EBUSY) {
		gettimeofday (&timenow, NULL);
		
		if (timenow.tv_sec >= timeout->tv_sec &&
		    (timenow.tv_usec * 1000) >= timeout->tv_nsec) {
			return ETIMEDOUT;
		}
		
		nanosleep (&sleepytime, NULL);
	}
	
	return retcode;
}
#endif /* HAVE_PTHREAD_MUTEX_TIMEDLOCK */


int
mono_once (mono_once_t *once, void (*once_init) (void))
{
	int thr_ret;
	
	if (!once->complete) {
		pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
				      (void *)&once->mutex);
		thr_ret = pthread_mutex_lock (&once->mutex);
		g_assert (thr_ret == 0);
		
		if (!once->complete) {
			once_init ();
			once->complete = TRUE;
		}
		thr_ret = pthread_mutex_unlock (&once->mutex);
		g_assert (thr_ret == 0);
		
		pthread_cleanup_pop (0);
	}
	
	return 0;
}

#endif

/*
Returns a recursive mutex that is safe under suspension.

A suspension safe mutex means one that can handle this scenario:

mutex M

thread 1:
1)lock M
2)suspend thread 2
3)unlock M
4)lock M

thread 2:
5)lock M

Say (1) happens before (5) and (5) happens before (2).
This means that thread 2 was suspended by the kernel because
it's waiting on mutext M.

Thread 1 then proceed to suspend thread 2 and unlock/lock the
mutex.

If the kernel implements mutexes with FIFO wait lists, this means
that thread 1 will be blocked waiting for thread 2 acquire the lock.
Since thread 2 is suspended, we have a deadlock.

A suspend safe mutex is an unfair lock but will schedule any runable
thread that is waiting for a the lock.

This problem was witnessed on OSX in mono/tests/thread-exit.cs.

*/
int
mono_mutex_init_suspend_safe (mono_mutex_t *mutex)
{
#if defined(__APPLE__)
	int res;
	pthread_mutexattr_t attr;
	static gboolean inited;
	static int (*setpolicy_np) (pthread_mutexattr_t *, int);

	if (!inited) {
		setpolicy_np = dlsym (RTLD_NEXT, "pthread_mutexattr_setpolicy_np");
		mono_atomic_store_release (&inited, TRUE);
	}

	pthread_mutexattr_init (&attr);
	pthread_mutexattr_settype (&attr, PTHREAD_MUTEX_RECURSIVE);
	if (setpolicy_np)
		setpolicy_np (&attr, _PTHREAD_MUTEX_POLICY_FIRSTFIT);
	res = pthread_mutex_init (mutex, &attr);
	pthread_mutexattr_destroy (&attr);

	return res;
#else
	return mono_mutex_init (mutex);
#endif
}
