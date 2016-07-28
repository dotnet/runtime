/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 * mono-os-mutex.h: Portability wrappers around POSIX Mutexes
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

#if !defined(HOST_WIN32)
#include <pthread.h>
#else
#include <winsock2.h>
#include <windows.h>
#endif

#ifndef MONO_INFINITE_WAIT
#define MONO_INFINITE_WAIT ((guint32) 0xFFFFFFFF)
#endif

G_BEGIN_DECLS

#if !defined(HOST_WIN32)

typedef pthread_mutex_t mono_mutex_t;
typedef pthread_cond_t mono_cond_t;

#else

typedef CRITICAL_SECTION mono_mutex_t;
typedef CONDITION_VARIABLE mono_cond_t;

#endif

/**
 * mono_os_mutex_init:
 */
void
mono_os_mutex_init (mono_mutex_t *mutex);

/**
 * mono_os_mutex_init_recursive:
 */
void
mono_os_mutex_init_recursive (mono_mutex_t *mutex);

/**
 * mono_os_mutex_destroy:
 *
 * @returns:
 *  -  0: success
 *  - -1: the mutex is busy (used by the io-layer)
 */
gint
mono_os_mutex_destroy (mono_mutex_t *mutex);

/**
 * mono_os_mutex_lock:
 */
void
mono_os_mutex_lock (mono_mutex_t *mutex);

/**
 * mono_os_mutex_trylock:
 *
 * @returns:
 *  -  0: success
 *  - -1: the mutex is busy
 */
gint
mono_os_mutex_trylock (mono_mutex_t *mutex);

/**
 * mono_os_mutex_unlock:
 */
void
mono_os_mutex_unlock (mono_mutex_t *mutex);

/**
 * mono_os_cond_init:
 */
void
mono_os_cond_init (mono_cond_t *cond);

/**
 * mono_os_cond_destroy:
 *
 * @returns:
 *  -  0: success
 *  - -1: the cond is busy (used by the io-layer)
 */
gint
mono_os_cond_destroy (mono_cond_t *cond);

/**
 * mono_os_cond_wait:
 */
void
mono_os_cond_wait (mono_cond_t *cond, mono_mutex_t *mutex);

/**
 * mono_os_cond_timedwait:
 *
 * @returns:
 *  -  0: success
 *  - -1: wait timed out
 */
gint
mono_os_cond_timedwait (mono_cond_t *cond, mono_mutex_t *mutex, guint32 timeout_ms);

/**
 * mono_os_cond_signal:
 */
void
mono_os_cond_signal (mono_cond_t *cond);

/**
 * mono_os_cond_broadcast:
 */
void
mono_os_cond_broadcast (mono_cond_t *cond);

G_END_DECLS

#endif /* __MONO_OS_MUTEX_H__ */
