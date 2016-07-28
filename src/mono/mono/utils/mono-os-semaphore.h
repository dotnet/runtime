/*
 * mono-os-semaphore.h:  Definitions for generic semaphore usage
 *
 * Author:
 *	Geoff Norton  <gnorton@novell.com>
 *
 * (C) 2009 Novell, Inc.
 */

#ifndef _MONO_SEMAPHORE_H_
#define _MONO_SEMAPHORE_H_

#include <config.h>
#include <glib.h>

#if defined(USE_MACH_SEMA)
#include <mach/mach_init.h>
#include <mach/task.h>
#include <mach/semaphore.h>
#elif !defined(HOST_WIN32) && defined(HAVE_SEMAPHORE_H)
#include <semaphore.h>
#else
#include <winsock2.h>
#include <windows.h>
#endif

#ifndef MONO_INFINITE_WAIT
#define MONO_INFINITE_WAIT ((guint32) 0xFFFFFFFF)
#endif

G_BEGIN_DECLS

#if defined(USE_MACH_SEMA)

typedef semaphore_t MonoSemType;

#elif !defined(HOST_WIN32) && defined(HAVE_SEMAPHORE_H)

typedef sem_t MonoSemType;

#else

typedef HANDLE MonoSemType;

#endif

typedef enum {
	MONO_SEM_FLAGS_NONE      = 0,
	MONO_SEM_FLAGS_ALERTABLE = 1 << 0,
} MonoSemFlags;

typedef enum {
	MONO_SEM_TIMEDWAIT_RET_SUCCESS  =  0,
	MONO_SEM_TIMEDWAIT_RET_ALERTED  = -1,
	MONO_SEM_TIMEDWAIT_RET_TIMEDOUT = -2,
} MonoSemTimedwaitRet;

/**
 * mono_os_sem_init:
 */
void
mono_os_sem_init (MonoSemType *sem, gint value);

/**
 * mono_os_sem_destroy:
 */
void
mono_os_sem_destroy (MonoSemType *sem);

/**
 * mono_os_sem_wait:
 *
 * @returns:
 *  -  0: success
 *  - -1: the wait was interrupted
 */
gint
mono_os_sem_wait (MonoSemType *sem, MonoSemFlags flags);

/**
 * mono_os_sem_timedwait:
 *
 * @returns: see MonoSemTimedwaitRet
 */
MonoSemTimedwaitRet
mono_os_sem_timedwait (MonoSemType *sem, guint32 timeout_ms, MonoSemFlags flags);

/**
 * mono_os_sem_post:
 */
void
mono_os_sem_post (MonoSemType *sem);

G_END_DECLS

#endif /* _MONO_SEMAPHORE_H_ */
