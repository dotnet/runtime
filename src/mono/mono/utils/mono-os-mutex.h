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

#define MONO_INFINITE_WAIT ((guint32) 0xFFFFFFFF)

#if !defined(HOST_WIN32)

#if !defined(CLOCK_MONOTONIC) || defined(HOST_DARWIN) || defined(HOST_ANDROID) || defined(HOST_WASM)
#define BROKEN_CLOCK_SOURCE
#endif

typedef pthread_mutex_t mono_mutex_t;
typedef pthread_cond_t mono_cond_t;

#ifndef DISABLE_THREADS

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

#if !defined(__HAIKU__) && !defined(MUSL) && defined (PTHREAD_PRIO_INHERIT) && HAVE_DECL_PTHREAD_MUTEXATTR_SETPROTOCOL
	/* use PTHREAD_PRIO_INHERIT if possible */
	res = pthread_mutexattr_setprotocol (&attr, PTHREAD_PRIO_INHERIT);
	if (G_UNLIKELY (res != 0 && res != ENOTSUP))
		g_error ("%s: pthread_mutexattr_setprotocol failed with \"%s\" (%d)", __func__, g_strerror (res), res);
#endif

	res = pthread_mutex_init (mutex, &attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_mutexattr_destroy (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutexattr_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);
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

	res = pthread_mutex_destroy (mutex);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

static inline void
mono_os_mutex_lock (mono_mutex_t *mutex)
{
	int res;

	res = pthread_mutex_lock (mutex);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_lock failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

static inline int
mono_os_mutex_trylock (mono_mutex_t *mutex)
{
	int res;

	res = pthread_mutex_trylock (mutex);
	if (G_UNLIKELY (res != 0 && res != EBUSY))
		g_error ("%s: pthread_mutex_trylock failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	return res != 0 ? -1 : 0;
}

static inline void
mono_os_mutex_unlock (mono_mutex_t *mutex)
{
	int res;

	res = pthread_mutex_unlock (mutex);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_unlock failed with \"%s\" (%d)", __func__, g_strerror (res), res);
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

static inline void
mono_os_mutex_unlock (mono_mutex_t *mutex)
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

	res = pthread_cond_wait (cond, mutex);
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
} mono_mutex_t;

typedef CONDITION_VARIABLE mono_cond_t;

static inline void
mono_os_mutex_init (mono_mutex_t *mutex)
{
	mutex->recursive = FALSE;
	InitializeSRWLock (&mutex->srwlock);
}

static inline void
mono_os_mutex_init_recursive (mono_mutex_t *mutex)
{
	mutex->recursive = TRUE;
	const BOOL res = InitializeCriticalSectionEx (&mutex->critical_section, 0, CRITICAL_SECTION_NO_DEBUG_INFO);

	if (G_UNLIKELY (res == 0))
		g_error ("%s: InitializeCriticalSectionEx failed with error %d", __func__, GetLastError ());
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
}

static inline int
mono_os_mutex_trylock (mono_mutex_t *mutex)
{
	return (mutex->recursive ?
		TryEnterCriticalSection (&mutex->critical_section) :
		TryAcquireSRWLockExclusive (&mutex->srwlock)) ? 0 : -1;
}

static inline void
mono_os_mutex_unlock (mono_mutex_t *mutex)
{
	mutex->recursive ?
		LeaveCriticalSection (&mutex->critical_section) :
		ReleaseSRWLockExclusive (&mutex->srwlock);
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
	const BOOL res = mutex->recursive ?
		SleepConditionVariableCS (cond, &mutex->critical_section, INFINITE) :
		SleepConditionVariableSRW (cond, &mutex->srwlock, INFINITE, 0);

	if (G_UNLIKELY (res == 0))
		g_error ("%s: SleepConditionVariable failed with error %d", __func__, GetLastError ());
}

static inline int
mono_os_cond_timedwait (mono_cond_t *cond, mono_mutex_t *mutex, guint32 timeout_ms)
{
	const BOOL res = mutex->recursive ?
		SleepConditionVariableCS (cond, &mutex->critical_section, timeout_ms) :
		SleepConditionVariableSRW (cond, &mutex->srwlock, timeout_ms, 0);

	if (G_UNLIKELY (res == 0 && GetLastError () != ERROR_TIMEOUT))
		g_error ("%s: SleepConditionVariable failed with error %d", __func__, GetLastError ());

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
