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

G_BEGIN_DECLS

#ifndef MONO_INFINITE_WAIT
#define MONO_INFINITE_WAIT ((guint32) 0xFFFFFFFF)
#endif


#if !defined(HOST_WIN32)

#if !defined(CLOCK_MONOTONIC) || defined(HOST_DARWIN) || defined(HOST_ANDROID) || defined(HOST_WASM)
#define BROKEN_CLOCK_SOURCE
#endif

typedef pthread_mutex_t mono_mutex_t;
typedef pthread_cond_t mono_cond_t;

static inline void
mono_os_mutex_init (mono_mutex_t *mutex)
{
	int res;

	res = pthread_mutex_init (mutex, NULL);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

static inline void
mono_os_mutex_init_recursive (mono_mutex_t *mutex)
{
	int res;
	pthread_mutexattr_t attr;

	res = pthread_mutexattr_init (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutexattr_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_mutexattr_settype (&attr, PTHREAD_MUTEX_RECURSIVE);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutexattr_settype failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_mutex_init (mutex, &attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_mutexattr_destroy (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutexattr_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);
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

/* Vanilla MinGW is missing some defs, load them from MinGW-w64. */
#if defined __MINGW32__ && !defined __MINGW64_VERSION_MAJOR && (_WIN32_WINNT >= 0x0600)

/* Fixme: Opaque structs */
typedef PVOID RTL_CONDITION_VARIABLE;
typedef PVOID RTL_SRWLOCK;

#ifndef _RTL_RUN_ONCE_DEF
#define _RTL_RUN_ONCE_DEF 1
typedef PVOID RTL_RUN_ONCE, *PRTL_RUN_ONCE;
typedef DWORD (WINAPI *PRTL_RUN_ONCE_INIT_FN)(PRTL_RUN_ONCE, PVOID, PVOID *);
#define RTL_RUN_ONCE_INIT 0
#define RTL_RUN_ONCE_CHECK_ONLY 1UL
#define RTL_RUN_ONCE_ASYNC 2UL
#define RTL_RUN_ONCE_INIT_FAILED 4UL
#define RTL_RUN_ONCE_CTX_RESERVED_BITS 2
#endif /* _RTL_RUN_ONCE_DEF */
#define RTL_SRWLOCK_INIT 0
#define RTL_CONDITION_VARIABLE_INIT 0
#define RTL_CONDITION_VARIABLE_LOCKMODE_SHARED 1

#define CONDITION_VARIABLE_INIT RTL_CONDITION_VARIABLE_INIT
#define CONDITION_VARIABLE_LOCKMODE_SHARED RTL_CONDITION_VARIABLE_LOCKMODE_SHARED
#define SRWLOCK_INIT RTL_SRWLOCK_INIT

/*Condition Variables http://msdn.microsoft.com/en-us/library/ms682052%28VS.85%29.aspx*/
typedef RTL_CONDITION_VARIABLE CONDITION_VARIABLE, *PCONDITION_VARIABLE;
typedef RTL_SRWLOCK SRWLOCK, *PSRWLOCK;

WINBASEAPI VOID WINAPI InitializeConditionVariable(PCONDITION_VARIABLE ConditionVariable);
WINBASEAPI WINBOOL WINAPI SleepConditionVariableCS(PCONDITION_VARIABLE ConditionVariable, PCRITICAL_SECTION CriticalSection, DWORD dwMilliseconds);
WINBASEAPI WINBOOL WINAPI SleepConditionVariableSRW(PCONDITION_VARIABLE ConditionVariable, PSRWLOCK SRWLock, DWORD dwMilliseconds, ULONG Flags);
WINBASEAPI VOID WINAPI WakeAllConditionVariable(PCONDITION_VARIABLE ConditionVariable);
WINBASEAPI VOID WINAPI WakeConditionVariable(PCONDITION_VARIABLE ConditionVariable);

/*Slim Reader/Writer (SRW) Locks http://msdn.microsoft.com/en-us/library/aa904937%28VS.85%29.aspx*/
WINBASEAPI VOID WINAPI AcquireSRWLockExclusive(PSRWLOCK SRWLock);
WINBASEAPI VOID WINAPI AcquireSRWLockShared(PSRWLOCK SRWLock);
WINBASEAPI VOID WINAPI InitializeSRWLock(PSRWLOCK SRWLock);
WINBASEAPI VOID WINAPI ReleaseSRWLockExclusive(PSRWLOCK SRWLock);
WINBASEAPI VOID WINAPI ReleaseSRWLockShared(PSRWLOCK SRWLock);

WINBASEAPI BOOLEAN TryAcquireSRWLockExclusive(PSRWLOCK SRWLock);
WINBASEAPI BOOLEAN TryAcquireSRWLockShared(PSRWLOCK SRWLock);

/*One-Time Initialization http://msdn.microsoft.com/en-us/library/aa363808(VS.85).aspx*/
#define INIT_ONCE_ASYNC 0x00000002UL
#define INIT_ONCE_INIT_FAILED 0x00000004UL

typedef PRTL_RUN_ONCE PINIT_ONCE;
typedef PRTL_RUN_ONCE LPINIT_ONCE;
typedef WINBOOL CALLBACK (*PINIT_ONCE_FN) (PINIT_ONCE InitOnce, PVOID Parameter, PVOID *Context);

WINBASEAPI WINBOOL WINAPI InitOnceBeginInitialize(LPINIT_ONCE lpInitOnce, DWORD dwFlags, PBOOL fPending, LPVOID *lpContext);
WINBASEAPI WINBOOL WINAPI InitOnceComplete(LPINIT_ONCE lpInitOnce, DWORD dwFlags, LPVOID lpContext);
WINBASEAPI WINBOOL WINAPI InitOnceExecuteOnce(PINIT_ONCE InitOnce, PINIT_ONCE_FN InitFn, PVOID Parameter, LPVOID *Context);

/* https://msdn.microsoft.com/en-us/library/windows/desktop/ms683477(v=vs.85).aspx */
WINBASEAPI BOOL WINAPI InitializeCriticalSectionEx(LPCRITICAL_SECTION lpCriticalSection, DWORD dwSpinCount, DWORD Flags);

#define CRITICAL_SECTION_NO_DEBUG_INFO 0x01000000

#endif /* defined __MINGW32__ && !defined __MINGW64_VERSION_MAJOR && (_WIN32_WINNT >= 0x0600) */

typedef CRITICAL_SECTION mono_mutex_t;
typedef CONDITION_VARIABLE mono_cond_t;

static inline void
mono_os_mutex_init (mono_mutex_t *mutex)
{
	BOOL res;

	res = InitializeCriticalSectionEx (mutex, 0, CRITICAL_SECTION_NO_DEBUG_INFO);
	if (G_UNLIKELY (res == 0))
		g_error ("%s: InitializeCriticalSectionEx failed with error %d", __func__, GetLastError ());
}

static inline void
mono_os_mutex_init_recursive (mono_mutex_t *mutex)
{
	BOOL res;

	res = InitializeCriticalSectionEx (mutex, 0, CRITICAL_SECTION_NO_DEBUG_INFO);
	if (G_UNLIKELY (res == 0))
		g_error ("%s: InitializeCriticalSectionEx failed with error %d", __func__, GetLastError ());
}

static inline void
mono_os_mutex_destroy (mono_mutex_t *mutex)
{
	DeleteCriticalSection (mutex);
}

static inline void
mono_os_mutex_lock (mono_mutex_t *mutex)
{
	EnterCriticalSection (mutex);
}

static inline int
mono_os_mutex_trylock (mono_mutex_t *mutex)
{
	return TryEnterCriticalSection (mutex) == 0 ? -1 : 0;
}

static inline void
mono_os_mutex_unlock (mono_mutex_t *mutex)
{
	LeaveCriticalSection (mutex);
}

static inline void
mono_os_cond_init (mono_cond_t *cond)
{
	InitializeConditionVariable (cond);
}

static inline void
mono_os_cond_destroy (mono_cond_t *cond)
{
	/* Beauty of win32 API: do not destroy it */
}

static inline void
mono_os_cond_wait (mono_cond_t *cond, mono_mutex_t *mutex)
{
	BOOL res;

	res = SleepConditionVariableCS (cond, mutex, INFINITE);
	if (G_UNLIKELY (res == 0))
		g_error ("%s: SleepConditionVariableCS failed with error %d", __func__, GetLastError ());
}

static inline int
mono_os_cond_timedwait (mono_cond_t *cond, mono_mutex_t *mutex, guint32 timeout_ms)
{
	BOOL res;

	res = SleepConditionVariableCS (cond, mutex, timeout_ms);
	if (G_UNLIKELY (res == 0 && GetLastError () != ERROR_TIMEOUT))
		g_error ("%s: SleepConditionVariableCS failed with error %d", __func__, GetLastError ());

	return res == 0 ? -1 : 0;
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

G_END_DECLS

#endif /* __MONO_OS_MUTEX_H__ */
