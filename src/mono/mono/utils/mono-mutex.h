/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 * mono-mutex.h: Portability wrappers around POSIX Mutexes
 *
 * Authors: Jeffrey Stedfast <fejj@ximian.com>
 *
 * Copyright 2002 Ximian, Inc. (www.ximian.com)
 */

#ifndef __MONO_MUTEX_H__
#define __MONO_MUTEX_H__

#include <config.h>

#include <glib.h>
#ifdef HAVE_PTHREAD_H
#include <pthread.h>
#endif
#include <time.h>

#ifdef HOST_WIN32
#include <winsock2.h>
#include <windows.h>

/* Vanilla MinGW is missing some defs, loan them from MinGW-w64. */
#if defined __MINGW32__ && !defined __MINGW64_VERSION_MAJOR

#if (_WIN32_WINNT >= 0x0600)
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
#endif
#define RTL_SRWLOCK_INIT 0
#define RTL_CONDITION_VARIABLE_INIT 0
#define RTL_CONDITION_VARIABLE_LOCKMODE_SHARED 1

#define CONDITION_VARIABLE_INIT RTL_CONDITION_VARIABLE_INIT
#define CONDITION_VARIABLE_LOCKMODE_SHARED RTL_CONDITION_VARIABLE_LOCKMODE_SHARED
#define SRWLOCK_INIT RTL_SRWLOCK_INIT
#endif

#if (_WIN32_WINNT >= 0x0600)
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
#endif

#endif /* defined __MINGW32__ && !defined __MINGW64_VERSION_MAJOR */
#endif /* HOST_WIN32 */

G_BEGIN_DECLS

#ifndef HOST_WIN32

typedef struct {
	pthread_mutex_t mutex;
	gboolean complete;
} mono_once_t;

#define MONO_ONCE_INIT { PTHREAD_MUTEX_INITIALIZER, FALSE }

int mono_once (mono_once_t *once, void (*once_init) (void));

typedef pthread_mutex_t mono_mutex_t;
typedef pthread_cond_t mono_cond_t;

#define mono_mutex_init(mutex) pthread_mutex_init (mutex, NULL)
#define mono_mutex_lock(mutex) pthread_mutex_lock (mutex)
#define mono_mutex_trylock(mutex) pthread_mutex_trylock (mutex)
#define mono_mutex_timedlock(mutex,timeout) pthread_mutex_timedlock (mutex, timeout)
#define mono_mutex_unlock(mutex) pthread_mutex_unlock (mutex)
#define mono_mutex_destroy(mutex) pthread_mutex_destroy (mutex)

#define mono_cond_init(cond,attr) pthread_cond_init (cond,attr)
#define mono_cond_wait(cond,mutex) pthread_cond_wait (cond, mutex)
#define mono_cond_timedwait(cond,mutex,timeout) pthread_cond_timedwait (cond, mutex, timeout)
#define mono_cond_signal(cond) pthread_cond_signal (cond)
#define mono_cond_broadcast(cond) pthread_cond_broadcast (cond)
#define mono_cond_destroy(cond)

/*
 * This should be used instead of mono_cond_timedwait, since that function is not implemented on windows.
 */
int mono_cond_timedwait_ms (mono_cond_t *cond, mono_mutex_t *mutex, int timeout_ms);

/* This is a function so it can be passed to pthread_cleanup_push -
 * that is a macro and giving it a macro as a parameter breaks.
 */
G_GNUC_UNUSED
static inline int mono_mutex_unlock_in_cleanup (mono_mutex_t *mutex)
{
	return(mono_mutex_unlock (mutex));
}

/* Returns zero on success. */
static inline int
mono_mutex_init_recursive (mono_mutex_t *mutex)
{
	int res;
	pthread_mutexattr_t attr;

	pthread_mutexattr_init (&attr);
	pthread_mutexattr_settype (&attr, PTHREAD_MUTEX_RECURSIVE);
	res = pthread_mutex_init (mutex, &attr);
	pthread_mutexattr_destroy (&attr);

	return res;
}

#else

typedef CRITICAL_SECTION mono_mutex_t;
typedef CONDITION_VARIABLE mono_cond_t;

#define mono_mutex_init(mutex) (InitializeCriticalSection((mutex)), 0)
#define mono_mutex_init_recursive(mutex) (InitializeCriticalSection((mutex)), 0)
#define mono_mutex_lock(mutex) EnterCriticalSection((mutex))
#define mono_mutex_trylock(mutex) (!TryEnterCriticalSection((mutex)))
#define mono_mutex_unlock(mutex)  LeaveCriticalSection((mutex))
#define mono_mutex_destroy(mutex) DeleteCriticalSection((mutex))

static inline int
mono_cond_init (mono_cond_t *cond, int attr)
{
	InitializeConditionVariable (cond);
	return 0;
}

static inline int
mono_cond_wait (mono_cond_t *cond, mono_mutex_t *mutex)
{
	return SleepConditionVariableCS (cond, mutex, INFINITE) ? 0 : 1;
}

static inline int
mono_cond_timedwait (mono_cond_t *cond, mono_mutex_t *mutex, struct timespec *timeout)
{
	// FIXME:
	g_assert_not_reached ();
	return 0;
}

static inline int
mono_cond_signal (mono_cond_t *cond)
{
	WakeConditionVariable (cond);
	return 0;
}

static inline int
mono_cond_broadcast (mono_cond_t *cond)
{
	WakeAllConditionVariable (cond);
	return 0;
}

static inline int
mono_cond_destroy (mono_cond_t *cond)
{
	return 0;
}

static inline int
mono_cond_timedwait_ms (mono_cond_t *cond, mono_mutex_t *mutex, int timeout_ms)
{
	return SleepConditionVariableCS (cond, mutex, timeout_ms) ? 0 : 1;
}

#endif

int mono_mutex_init_suspend_safe (mono_mutex_t *mutex);

G_END_DECLS

#endif /* __MONO_MUTEX_H__ */
