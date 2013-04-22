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

#include "mono-mutex.h"


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
