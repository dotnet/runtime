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

#include <glib.h>
#ifdef HAVE_PTHREAD_H
#include <pthread.h>
#endif
#include <time.h>

#ifdef HOST_WIN32
#include <windows.h>
#endif

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
typedef HANDLE mono_cond_t;

#define mono_mutex_init(mutex) (InitializeCriticalSection((mutex)), 0)
#define mono_mutex_init_recursive(mutex) (InitializeCriticalSection((mutex)), 0)
#define mono_mutex_lock(mutex) EnterCriticalSection((mutex))
#define mono_mutex_trylock(mutex) TryEnterCriticalSection((mutex))
#define mono_mutex_unlock(mutex)  LeaveCriticalSection((mutex))
#define mono_mutex_destroy(mutex) DeleteCriticalSection((mutex))


#define mono_cond_init(cond,attr) do{*(cond) = CreateEvent(NULL,FALSE,FALSE,NULL); } while (0)
#define mono_cond_wait(cond,mutex) WaitForSingleObject(*(cond),INFINITE)
#define mono_cond_timedwait(cond,mutex,timeout) WaitForSingleObject(*(cond),timeout)
#define mono_cond_signal(cond) SetEvent(*(cond))
#define mono_cond_broadcast(cond) (!SetEvent(*(cond)))
#define mono_cond_destroy(cond) CloseHandle(*(cond))

#endif

int mono_mutex_init_suspend_safe (mono_mutex_t *mutex);

G_END_DECLS

#endif /* __MONO_MUTEX_H__ */
