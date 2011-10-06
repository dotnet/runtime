/*
 * mono-semaphore.h:  Definitions for generic semaphore usage
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
#include <time.h>
#if defined(HAVE_SEMAPHORE_H) && !defined(HOST_WIN32)
#include <semaphore.h>
#endif
#include <mono/io-layer/io-layer.h>

#if (defined (HAVE_SEMAPHORE_H) || defined (USE_MACH_SEMA)) && !defined(HOST_WIN32)
#  define MONO_HAS_SEMAPHORES

#  if defined (USE_MACH_SEMA)
#    include <mach/mach_init.h>
#    include <mach/task.h>
#    include <mach/semaphore.h>
typedef semaphore_t MonoSemType;
#    define MONO_SEM_INIT(addr,value) semaphore_create (current_task (), (addr), SYNC_POLICY_FIFO, (value))
#    define MONO_SEM_DESTROY(sem) semaphore_destroy (current_task (), *(sem))
#  else
typedef sem_t MonoSemType;
#    define MONO_SEM_INIT(addr,value) sem_init ((addr), 0, (value))
#    define MONO_SEM_DESTROY(sem) sem_destroy ((sem))
#  endif
#else
#  define MONO_HAS_SEMAPHORES
typedef HANDLE MonoSemType;
#    define MONO_SEM_INIT(addr,initial) do {*(addr) = CreateSemaphore ( NULL,(initial),0x7FFFFFFF,NULL);} while(0)
#    define MONO_SEM_DESTROY(sem) CloseHandle (*(sem))
#endif

#define MONO_SEM_WAIT(sem) MONO_SEM_WAIT_ALERTABLE(sem, FALSE)
#define MONO_SEM_WAIT_ALERTABLE(sem,alertable) mono_sem_wait ((sem), alertable)
#define MONO_SEM_POST(sem) mono_sem_post ((sem))
#define MONO_SEM_TIMEDWAIT(sem, timeout_ms) MONO_SEM_TIMEDWAIT_ALERTABLE(sem, timeout_ms, FALSE)
#define MONO_SEM_TIMEDWAIT_ALERTABLE(sem, timeout_ms, alertable) mono_sem_timedwait ((sem), (timeout_ms), alertable) 

G_BEGIN_DECLS

int mono_sem_wait (MonoSemType *sem, gboolean alertable);
int mono_sem_timedwait (MonoSemType *sem, guint32 timeout_ms, gboolean alertable);
int mono_sem_post (MonoSemType *sem);

G_END_DECLS
#endif /* _MONO_SEMAPHORE_H_ */
