/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 *  Authors: Jeffrey Stedfast <fejj@ximian.com>
 *
 *  Copyright 2002 Ximain, Inc. (www.ximian.com)
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Street #330, Boston, MA 02111-1307, USA.
 *
 */


#ifndef __MONO_MUTEX_H__
#define __MONO_MUTEX_H__

#include <glib.h>
#include <pthread.h>
#include <time.h>

G_BEGIN_DECLS

typedef struct {
	pthread_mutex_t mutex;
	gboolean complete;
} mono_once_t;

#define MONO_ONCE_INIT { PTHREAD_MUTEX_INITIALIZER, FALSE }

int mono_once (mono_once_t *once, void (*once_init) (void));


#ifdef USE_MONO_MUTEX

#define MONO_THREAD_NONE ((pthread_t)~0)

/* mutex types... */
enum {
	MONO_MUTEX_NORMAL,
	MONO_MUTEX_RECURSIVE,
	MONO_MUTEX_ERRORCHECK = MONO_MUTEX_NORMAL,
	MONO_MUTEX_DEFAULT = MONO_MUTEX_NORMAL
};

/* mutex protocol attributes... */
enum {
	MONO_THREAD_PRIO_NONE,
	MONO_THREAD_PRIO_INHERIT,
	MONO_THREAD_PRIO_PROTECT,
};

/* mutex process sharing attributes... */
enum {
	MONO_THREAD_PROCESS_PRIVATE,
	MONO_THREAD_PROCESS_SHARED
};

typedef struct _mono_mutexattr_t {
	int type     : 1;
	int shared   : 1;
	int protocol : 2;
	int priority : 28;
} mono_mutexattr_t;

typedef struct _mono_mutex_t {
	int type;
	pthread_t owner;
	short waiters;
	short depth;
	pthread_mutex_t mutex;
	pthread_cond_t cond;
} mono_mutex_t;

/* static initializers */
#define MONO_MUTEX_INITIALIZER { 0, MONO_THREAD_NONE, 0, 0, PTHREAD_MUTEX_INITIALIZER, 0 }
#define MONO_RECURSIVE_MUTEX_INITIALIZER { 0, MONO_THREAD_NONE, 0, 0, PTHREAD_MUTEX_INITIALIZER, PTHREAD_COND_INITIALIZER }

int mono_mutexattr_init (mono_mutexattr_t *attr);
int mono_mutexattr_settype (mono_mutexattr_t *attr, int type);
int mono_mutexattr_gettype (mono_mutexattr_t *attr, int *type);
int mono_mutexattr_setpshared (mono_mutexattr_t *attr, int pshared);
int mono_mutexattr_getpshared (mono_mutexattr_t *attr, int *pshared);
int mono_mutexattr_setprotocol (mono_mutexattr_t *attr, int protocol);
int mono_mutexattr_getprotocol (mono_mutexattr_t *attr, int *protocol);
int mono_mutexattr_setprioceiling (mono_mutexattr_t *attr, int prioceiling);
int mono_mutexattr_getprioceiling (mono_mutexattr_t *attr, int *prioceiling);
int mono_mutexattr_destroy (mono_mutexattr_t *attr);


int mono_mutex_init (mono_mutex_t *mutex, const mono_mutexattr_t *attr);
int mono_mutex_lock (mono_mutex_t *mutex);
int mono_mutex_trylock (mono_mutex_t *mutex);
int mono_mutex_timedlock (mono_mutex_t *mutex, const struct timespec *timeout);
int mono_mutex_unlock (mono_mutex_t *mutex);
int mono_mutex_destroy (mono_mutex_t *mutex);

#define mono_cond_init(cond,attr) pthread_cond_init (cond, attr)
int mono_cond_wait (pthread_cond_t *cond, mono_mutex_t *mutex);
int mono_cond_timedwait (pthread_cond_t *cond, mono_mutex_t *mutex, const struct timespec *timeout);
#define mono_cond_signal(cond) pthread_cond_signal (cond)
#define mono_cond_broadcast(cond) pthread_cond_broadcast (cond)

#else /* system is equipped with a fully-functional pthread mutex library */

#define MONO_MUTEX_NORMAL             PTHREAD_MUTEX_NORMAL
#define MONO_MUTEX_RECURSIVE          PTHREAD_MUTEX_RECURSIVE
#define MONO_MUTEX_ERRORCHECK         PTHREAD_MUTEX_NORMAL
#define MONO_MUTEX_DEFAULT            PTHREAD_MUTEX_NORMAL

#define MONO_THREAD_PRIO_NONE         PTHREAD_PRIO_NONE
#define MONO_THREAD_PRIO_INHERIT      PTHREAD_PRIO_INHERIT
#define MONO_THREAD_PRIO_PROTECT      PTHREAD_PRIO_PROTECT

#define MONO_THREAD_PROCESS_PRIVATE   PTHREAD_PROCESS_PRIVATE
#define MONO_THREAD_PROCESS_SHARED    PTHREAD_PROCESS_SHARED

typedef pthread_mutex_t mono_mutex_t;
typedef pthread_mutexattr_t mono_mutexattr_t;

#define MONO_MUTEX_INITIALIZER PTHREAD_MUTEX_INITIALIZER
#define MONO_RECURSIVE_MUTEX_INITIALIZER PTHREAD_RECURSIVE_MUTEX_INITIALIZER

#define mono_mutexattr_init(attr) pthread_mutexattr_init (attr)
#define mono_mutexattr_settype(attr,type) pthread_mutexattr_settype (attr, type)
#define mono_mutexattr_gettype(attr,type) pthread_mutexattr_gettype (attr, type)
#define mono_mutexattr_setpshared(attr,pshared) pthread_mutexattr_setpshared (attr, pshared)
#define mono_mutexattr_getpshared(attr,pshared) pthread_mutexattr_getpshared (attr, pshared)
#define mono_mutexattr_setprotocol(attr,protocol) pthread_mutexattr_setprotocol (attr, protocol)
#define mono_mutexattr_getprotocol(attr,protocol) pthread_mutexattr_getprotocol (attr, protocol)
#define mono_mutexattr_setprioceiling(attr,prioceiling) pthread_mutexattr_setprioceiling (attr, prioceiling)
#define mono_mutexattr_getprioceiling(attr,prioceiling) pthread_mutexattr_getprioceiling (attr, prioceiling)
#define mono_mutexattr_destroy(attr) pthread_mutexattr_destroy (attr)

#define mono_mutex_init(mutex,attr) pthread_mutex_init (mutex, attr)
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

#endif /* USE_MONO_MUTEX */

/* This is a function so it can be passed to pthread_cleanup_push -
 * that is a macro and giving it a macro as a parameter breaks.
 */
G_GNUC_UNUSED
static inline int mono_mutex_unlock_in_cleanup (mono_mutex_t *mutex)
{
	return(mono_mutex_unlock (mutex));
}

G_END_DECLS

#endif /* __MONO_MUTEX_H__ */
