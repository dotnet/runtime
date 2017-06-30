/**
 * \file
 * Portability wrappers around POSIX Mutexes
 *
 * Authors: Jeffrey Stedfast <fejj@ximian.com>
 *
 * Copyright 2002 Ximian, Inc. (www.ximian.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 *
 */

#include <config.h>

#if !defined(HOST_WIN32)

#if defined(TARGET_OSX)
/* So we can use the declaration of pthread_cond_timedwait_relative_np () */
#undef _XOPEN_SOURCE
#endif
#include <pthread.h>

#include "mono-os-mutex.h"

int
mono_os_cond_timedwait (mono_cond_t *cond, mono_mutex_t *mutex, guint32 timeout_ms)
{
	struct timespec ts;
	int res;

	if (timeout_ms == MONO_INFINITE_WAIT) {
		mono_os_cond_wait (cond, mutex);
		return 0;
	}

	/* ms = 10^-3, us = 10^-6, ns = 10^-9 */

	/* This function only seems to be available on 64bit osx */
#if defined(HAVE_PTHREAD_COND_TIMEDWAIT_RELATIVE_NP) && defined(TARGET_OSX)
	memset (&ts, 0, sizeof (struct timespec));
	ts.tv_sec = timeout_ms / 1000;
	ts.tv_nsec = (timeout_ms % 1000) * 1000 * 1000;

	res = pthread_cond_timedwait_relative_np (cond, mutex, &ts);
	if (G_UNLIKELY (res != 0 && res != ETIMEDOUT)) {
		g_print ("cond: %p mutex: %p\n", *(gpointer*)cond, *(gpointer*)mutex);
		g_error ("%s: pthread_cond_timedwait_relative_np failed with \"%s\" (%d) %ld %ld %d", __func__, g_strerror (res), res, ts.tv_sec, ts.tv_nsec, timeout_ms);
	}
	return res != 0 ? -1 : 0;
#else
#ifdef BROKEN_CLOCK_SOURCE
	struct timeval tv;

	/* clock_gettime is not supported in MAC OS x */
	res = gettimeofday (&tv, NULL);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: gettimeofday failed with \"%s\" (%d)", __func__, g_strerror (errno), errno);

	ts.tv_sec = tv.tv_sec;
	ts.tv_nsec = tv.tv_usec * 1000;
#else
	/* cond is using CLOCK_MONOTONIC as time source */
	res = clock_gettime (CLOCK_MONOTONIC, &ts);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: clock_gettime failed with \"%s\" (%d)", __func__, g_strerror (errno), errno);
#endif

	ts.tv_sec += timeout_ms / 1000;
	ts.tv_nsec += (timeout_ms % 1000) * 1000 * 1000;
	if (ts.tv_nsec >= 1000 * 1000 * 1000) {
		ts.tv_nsec -= 1000 * 1000 * 1000;
		ts.tv_sec ++;
	}

	res = pthread_cond_timedwait (cond, mutex, &ts);
	if (G_UNLIKELY (res != 0 && res != ETIMEDOUT)) {
		g_print ("cond: %p mutex: %p\n", *(gpointer*)cond, *(gpointer*)mutex);
		g_error ("%s: pthread_cond_timedwait failed with \"%s\" (%d) %ld %ld %d", __func__, g_strerror (res), res, ts.tv_sec, ts.tv_nsec, timeout_ms);
	}
	return res != 0 ? -1 : 0;
#endif /* !HAVE_PTHREAD_COND_TIMEDWAIT_RELATIVE_NP */
}

#endif /* HOST_WIN32 */
