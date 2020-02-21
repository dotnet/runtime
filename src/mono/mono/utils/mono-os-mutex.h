/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/**
 * \file
 * Portability wrappers around POSIX Mutexes
 *
 * Authors: Jeffrey Stedfast <fejj@ximian.com>
 *
 * Copyright 2002 Ximian, Inc. (www.ximian.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_OS_MUTEX_H__
#define __MONO_OS_MUTEX_H__

#include <config.h>
#include <glib.h>

#include <stdlib.h>
#include <string.h>
#include <time.h>

#if !defined(HOST_WIN32)
#include <pthread.h>
#include <errno.h>
#else
#include <winsock2.h>
#include <windows.h>
#endif

#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

#include "checked-build.h"
#include "mono-threads-api.h"

/* assert that there is no safepoint while we're holding an OS lock in the
 * runtime.  The issue is if two threads in GC Unsafe mode try to take an OS
 * lock.  The first thread will take the lock and then reach the safepoint and
 * suspend.  The second thread will block, but the suspend initiator (the GC)
 * will wait for it to reach a safepoint and the runtime will deadlock.
 * If you take an OS lock in GC Unsafe mode, you must release it without reaching a safepoint.
 */
static inline void
mono_check_no_safepoint_in_os_mutex_begin (const char *func, int32_t *cookie)
{
#ifdef ENABLE_CHECKED_BUILD_THREAD
	if (G_UNLIKELY (mono_check_mode_enabled (MONO_CHECK_MODE_THREAD))) {
		g_assert (cookie);
		/* cookie logic:
		 * if the cookie is 0 when we lock the mutex:
		 * if we did a transition - set the cookie to 1 - this lock will be responsible
		 *   for doing the exit transition.
		 * if we did not do a transition - leave the cookie at 0 - this lock will not be
		 *   responsible for doing the exit transition.
		 * if the cookie is positive when we lock the mutex:
		 *   increment the cookie.
		 *
		 * on exit:
		 *  if the cookie is positive:
		 *     decrement the cookie, if it is now 0 do the exit transition.
		 *   if the cookie is zero: do nothing
		 * 
		 * Suppose we have OS recursive mutexes m1 and m2 and we do:
		 * 
		 * // m1.cookie == m2.cookie == 0
		 * lock (m1); // enter no safepoints mode; m1.cookie == 1
		 * lock (m2); // already in safepoints mode; m2.cookie == 0
		 * lock (m1); // cookie is positive; increment; m1.cookie == 2
		 * 
		 * unlock (m1); // decrement cookie; m1.cookie == 1
		 * unlock (m2); // do nothing; m2.cookie == 0
		 * unlock (m1); // decrement cookie; do exit transition ; m1.cookie == 0
		 */
		if (*cookie == 0) {
			if (mono_threads_enter_no_safepoints_region_if_unsafe (func))
				*cookie = 1;
		} else {
			g_assert (*cookie > 0);
			*cookie++;
		}
	}
#endif
}

static inline void
mono_check_no_safepoint_in_os_mutex_end (const char *func, int32_t *cookie)
{
#ifdef ENABLE_CHECKED_BUILD_THREAD
	if (G_UNLIKELY (mono_check_mode_enabled (MONO_CHECK_MODE_THREAD))) {
		g_assert (cookie);
		/* see cookie logic in mono_check_no_safepoint_in_os_mutex_begin */
		int32_t cookie_val = *cookie;
		if (cookie_val == 0)
			return;
		g_assert (cookie_val > 0);
		*cookie = (cookie_val -= 1);
		if (cookie_val == 0)
			mono_threads_exit_no_safepoints_region_if_unsafe (func);
	}
#endif
}

#define MONO_INFINITE_WAIT ((guint32) 0xFFFFFFFF)

#if !defined(HOST_WIN32)

#if !defined(CLOCK_MONOTONIC) || defined(HOST_DARWIN) || defined(HOST_ANDROID) || defined(HOST_WASM)
#define BROKEN_CLOCK_SOURCE
#endif

#ifndef ENABLE_CHECKED_BUILD_THREAD
typedef pthread_mutex_t mono_mutex_t;
#else
typedef struct {
	pthread_mutex_t mutex;
	int32_t cookie;
} mono_mutex_t;
#endif
typedef pthread_cond_t mono_cond_t;

#ifndef DISABLE_THREADS

static inline pthread_mutex_t*
os_mutex_mutex (mono_mutex_t *mutex)
{
#ifndef ENABLE_CHECKED_BUILD_THREAD
	return mutex;
#else
	return &mutex->mutex;
#endif
}

static inline int32_t*
os_mutex_cookie (mono_mutex_t * mutex G_GNUC_UNUSED)
{
#ifndef ENABLE_CHECKED_BUILD_THREAD
	return NULL;
#else
	return &mutex->cookie;
#endif
}

static inline void
os_mutex_cookie_init (mono_mutex_t *mutex G_GNUC_UNUSED)
{
#ifndef ENABLE_CHECKED_BUILD_THREAD
#else
	mutex->cookie = 0;
#endif
}

static inline int32_t
os_mutex_save_and_reinit_cookie (mono_mutex_t * mutex G_GNUC_UNUSED)
{
#ifndef ENABLE_CHECKED_BUILD_THREAD
	return 0;
#else
	int32_t old_cookie = mutex->cookie;
	mutex->cookie = 0;
	return old_cookie;
#endif
}

static inline void
os_mutex_restore_cookie (mono_mutex_t * mutex G_GNUC_UNUSED, int32_t saved_cookie)
{
#ifndef ENABLE_CHECKED_BUILD_THREAD
#else
	mutex->cookie = saved_cookie;
#endif
}

static inline void
mono_os_mutex_init_type (mono_mutex_t *mutex, int type)
{
	int res;
	pthread_mutexattr_t attr;

	res = pthread_mutexattr_init (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutexattr_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_mutexattr_settype (&attr, type);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutexattr_settype failed with \"%s\" (%d)", __func__, g_strerror (res), res);

#if !defined(__HAIKU__) && defined (PTHREAD_PRIO_INHERIT) && HAVE_DECL_PTHREAD_MUTEXATTR_SETPROTOCOL
	/* use PTHREAD_PRIO_INHERIT if possible */
	res = pthread_mutexattr_setprotocol (&attr, PTHREAD_PRIO_INHERIT);
	if (G_UNLIKELY (res != 0 && res != ENOTSUP))
		g_error ("%s: pthread_mutexattr_setprotocol failed with \"%s\" (%d)", __func__, g_strerror (res), res);
#endif

	res = pthread_mutex_init (os_mutex_mutex (mutex), &attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_mutexattr_destroy (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutexattr_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	os_mutex_cookie_init (mutex);
}

static inline void
mono_os_mutex_init (mono_mutex_t *mutex)
{
	mono_os_mutex_init_type(mutex, PTHREAD_MUTEX_DEFAULT);
}

static inline void
mono_os_mutex_init_recursive (mono_mutex_t *mutex)
{
	mono_os_mutex_init_type(mutex, PTHREAD_MUTEX_RECURSIVE);
}

static inline void
mono_os_mutex_destroy (mono_mutex_t *mutex)
{
	int res;

	res = pthread_mutex_destroy (os_mutex_mutex (mutex));
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

static inline void
mono_os_mutex_lock (mono_mutex_t *mutex)
{
	int res;

	res = pthread_mutex_lock (os_mutex_mutex (mutex));
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_lock failed with \"%s\" (%d)", __func__, g_strerror (res), res);
	mono_check_no_safepoint_in_os_mutex_begin (__func__, os_mutex_cookie (mutex));
}

static inline int
mono_os_mutex_trylock_internal (mono_mutex_t *mutex, gboolean enter_no_safepoints)
{
	int res;

	res = pthread_mutex_trylock (os_mutex_mutex (mutex));
	if (G_UNLIKELY (res != 0 && res != EBUSY))
		g_error ("%s: pthread_mutex_trylock failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	if (res == 0 && enter_no_safepoints)
		mono_check_no_safepoint_in_os_mutex_begin (__func__, os_mutex_cookie (mutex));
	return res != 0 ? -1 : 0;
}

static inline int
mono_os_mutex_trylock_from_coop (mono_mutex_t *mutex)
{
	return mono_os_mutex_trylock_internal (mutex, FALSE);
}

static inline int
mono_os_mutex_trylock (mono_mutex_t *mutex)
{
	return mono_os_mutex_trylock_internal (mutex, TRUE);
}

static inline void
mono_os_mutex_unlock_internal (mono_mutex_t *mutex, gboolean exit_no_safepoints)
{
	int res;

	if (exit_no_safepoints)
		mono_check_no_safepoint_in_os_mutex_end (__func__, os_mutex_cookie (mutex));

	res = pthread_mutex_unlock (os_mutex_mutex (mutex));
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_unlock failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

static inline void
mono_os_mutex_unlock_from_coop (mono_mutex_t *mutex)
{
	mono_os_mutex_unlock_internal (mutex, FALSE);
}

static inline void
mono_os_mutex_unlock (mono_mutex_t *mutex)
{
	mono_os_mutex_unlock_internal (mutex, TRUE);
}

#else /* DISABLE_THREADS */

static inline void
mono_os_mutex_init_type (mono_mutex_t *mutex, int type)
{
}

static inline void
mono_os_mutex_init (mono_mutex_t *mutex)
{
}

static inline void
mono_os_mutex_init_recursive (mono_mutex_t *mutex)
{
}

static inline void
mono_os_mutex_destroy (mono_mutex_t *mutex)
{
}

static inline void
mono_os_mutex_lock (mono_mutex_t *mutex)
{
}

static inline int
mono_os_mutex_trylock (mono_mutex_t *mutex)
{
	return 0;
}

static inline int
mono_os_mutex_trylock_from_coop (mono_mutex_t *mutex)
{
	return 0;
}

static inline void
mono_os_mutex_unlock (mono_mutex_t *mutex)
{
}

static inline void
mono_os_mutex_unlock_from_coop (mono_mutex_t *mutex)
{
}

#endif /* DISABLE_THREADS */

static inline void
mono_os_cond_init (mono_cond_t *cond)
{
	int res;

#ifdef BROKEN_CLOCK_SOURCE
	res = pthread_cond_init (cond, NULL);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_cond_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);
#else
	/* POSIX standard does not compel to have CLOCK_MONOTONIC */
	pthread_condattr_t attr;

	res = pthread_condattr_init (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_condattr_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_condattr_setclock (&attr, CLOCK_MONOTONIC);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_condattr_setclock failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	/* Attach an attribute having CLOCK_MONOTONIC to condition */
	res = pthread_cond_init (cond, &attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_cond_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_condattr_destroy (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_condattr_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);
#endif
}

static inline void
mono_os_cond_destroy (mono_cond_t *cond)
{
	int res;

	res = pthread_cond_destroy (cond);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_cond_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

static inline void
mono_os_cond_wait (mono_cond_t *cond, mono_mutex_t *mutex)
{
	int res;

	int32_t saved_cookie = os_mutex_save_and_reinit_cookie (mutex);
	res = pthread_cond_wait (cond, os_mutex_mutex (mutex));
	os_mutex_restore_cookie (mutex, saved_cookie);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_cond_wait failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

int
mono_os_cond_timedwait (mono_cond_t *cond, mono_mutex_t *mutex, guint32 timeout_ms);

static inline void
mono_os_cond_signal (mono_cond_t *cond)
{
	int res;

	res = pthread_cond_signal (cond);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_cond_signal failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

static inline void
mono_os_cond_broadcast (mono_cond_t *cond)
{
	int res;

	res = pthread_cond_broadcast (cond);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_cond_broadcast failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

#else

// FIXME mono_mutex_t and mono_mutex_recursive_t.
typedef struct mono_mutex_t {
	union {
		CRITICAL_SECTION critical_section;
		SRWLOCK srwlock;
	};
	gboolean recursive;
#ifdef ENABLE_CHECKED_BUILD_THREAD
	int32_t cookie;
#endif
} mono_mutex_t;

typedef CONDITION_VARIABLE mono_cond_t;

static inline int32_t*
os_mutex_cookie (mono_mutex_t * mutex G_GNUC_UNUSED)
{
#ifndef ENABLE_CHECKED_BUILD_THREAD
	return NULL;
#else
	return &mutex->cookie;
#endif
}


static inline void
os_mutex_cookie_init (mono_mutex_t *mutex G_GNUC_UNUSED)
{
#ifndef ENABLE_CHECKED_BUILD_THREAD
#else
	mutex->cookie = 0;
#endif
}

static inline int32_t
os_mutex_save_and_reinit_cookie (mono_mutex_t * mutex G_GNUC_UNUSED)
{
#ifndef ENABLE_CHECKED_BUILD_THREAD
	return 0;
#else
	int32_t old_cookie = mutex->cookie;
	mutex->cookie = 0;
	return old_cookie;
#endif
}

static inline void
os_mutex_restore_cookie (mono_mutex_t * mutex G_GNUC_UNUSED, int32_t saved_cookie)
{
#ifndef ENABLE_CHECKED_BUILD_THREAD
#else
	mutex->cookie = saved_cookie;
#endif
}

static inline void
mono_os_mutex_init (mono_mutex_t *mutex)
{
	mutex->recursive = FALSE;
	InitializeSRWLock (&mutex->srwlock);
	os_mutex_cookie_init (mutex);
}

static inline void
mono_os_mutex_init_recursive (mono_mutex_t *mutex)
{
	mutex->recursive = TRUE;
	const BOOL res = InitializeCriticalSectionEx (&mutex->critical_section, 0, CRITICAL_SECTION_NO_DEBUG_INFO);

	if (G_UNLIKELY (res == 0))
		g_error ("%s: InitializeCriticalSectionEx failed with error %d", __func__, GetLastError ());
	os_mutex_cookie_init (mutex);
}

static inline void
mono_os_mutex_destroy (mono_mutex_t *mutex)
{
	// There is no way to destroy a Win32 SRWLOCK.
	if (mutex->recursive)
		DeleteCriticalSection (&mutex->critical_section);
}

static inline void
mono_os_mutex_lock (mono_mutex_t *mutex)
{
	mutex->recursive ?
		EnterCriticalSection (&mutex->critical_section) :
		AcquireSRWLockExclusive (&mutex->srwlock);
	mono_check_no_safepoint_in_os_mutex_begin (__func__, os_mutex_cookie (mutex));
}

static inline int
mono_os_mutex_trylock_internal (mono_mutex_t *mutex, gboolean enter_no_safepoints)
{
	int res = (mutex->recursive ?
		TryEnterCriticalSection (&mutex->critical_section) :
		TryAcquireSRWLockExclusive (&mutex->srwlock)) ? 0 : -1;
	if (res == 0 && enter_no_safepoints)
		mono_check_no_safepoint_in_os_mutex_begin (__func__, os_mutex_cookie (mutex));
	return res;
}

static inline int
mono_os_mutex_trylock_from_coop (mono_mutex_t *mutex)
{
	return mono_os_mutex_trylock_internal (mutex, FALSE);
}

static inline int
mono_os_mutex_trylock (mono_mutex_t *mutex)
{
	return mono_os_mutex_trylock_internal (mutex, TRUE);
}

static inline void
mono_os_mutex_unlock_internal (mono_mutex_t *mutex, gboolean exit_no_safepoints)
{
	if (exit_no_safepoints)
		mono_check_no_safepoint_in_os_mutex_end (__func__, os_mutex_cookie (mutex));
	mutex->recursive ?
		LeaveCriticalSection (&mutex->critical_section) :
		ReleaseSRWLockExclusive (&mutex->srwlock);
}

static inline void
mono_os_mutex_unlock_from_coop (mono_mutex_t *mutex)
{
	mono_os_mutex_unlock_internal (mutex, FALSE);
}

static inline void
mono_os_mutex_unlock (mono_mutex_t *mutex)
{
	mono_os_mutex_unlock_internal (mutex, TRUE);
}

static inline void
mono_os_cond_init (mono_cond_t *cond)
{
	InitializeConditionVariable (cond);
}

static inline void
mono_os_cond_destroy (mono_cond_t *cond)
{
	// There is no way to destroy a Win32 condition variable.
}

static inline void
mono_os_cond_wait (mono_cond_t *cond, mono_mutex_t *mutex)
{
	int32_t saved_cookie = os_mutex_save_and_reinit_cookie (mutex);
	const BOOL res = mutex->recursive ?
		SleepConditionVariableCS (cond, &mutex->critical_section, INFINITE) :
		SleepConditionVariableSRW (cond, &mutex->srwlock, INFINITE, 0);

	if (G_UNLIKELY (res == 0))
		g_error ("%s: SleepConditionVariable failed with error %d", __func__, GetLastError ());
	if (res != 0)
		os_mutex_restore_cookie (mutex, saved_cookie);
}

static inline int
mono_os_cond_timedwait (mono_cond_t *cond, mono_mutex_t *mutex, guint32 timeout_ms)
{
	int32_t saved_cookie = os_mutex_save_and_reinit_cookie (mutex);
	const BOOL res = mutex->recursive ?
		SleepConditionVariableCS (cond, &mutex->critical_section, timeout_ms) :
		SleepConditionVariableSRW (cond, &mutex->srwlock, timeout_ms, 0);

	if (G_UNLIKELY (res == 0 && GetLastError () != ERROR_TIMEOUT))
		g_error ("%s: SleepConditionVariable failed with error %d", __func__, GetLastError ());
	if (res != 0)
		os_mutex_restore_cookie (mutex, saved_cookie);

	return res ? 0 : -1;
}

static inline void
mono_os_cond_signal (mono_cond_t *cond)
{
	WakeConditionVariable (cond);
}

static inline void
mono_os_cond_broadcast (mono_cond_t *cond)
{
	WakeAllConditionVariable (cond);
}

#endif

#endif /* __MONO_OS_MUTEX_H__ */
