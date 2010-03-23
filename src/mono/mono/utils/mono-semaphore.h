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
#ifdef HAVE_SEMAPHORE_H
#include <semaphore.h>
#endif
#include <mono/io-layer/io-layer.h>

#if defined (HAVE_SEMAPHORE_H) || defined (USE_MACH_SEMA)
#  define MONO_HAS_SEMAPHORES

#  if defined (USE_MACH_SEMA)
#    include <mach/mach_init.h>
#    include <mach/task.h>
#    include <mach/semaphore.h>
typedef semaphore_t MonoSemType;
#    define MONO_SEM_INIT(addr,value) semaphore_create (current_task (), (addr), SYNC_POLICY_FIFO, (value))
#    define MONO_SEM_WAIT(sem) semaphore_wait (*(sem))
#    define MONO_SEM_POST(sem) semaphore_signal (*(sem))
#    define MONO_SEM_DESTROY(sem) semaphore_destroy (current_task (), *(sem))
#  else
typedef sem_t MonoSemType;
#    define MONO_SEM_INIT(addr,value) sem_init ((addr), 0, (value))
#    define MONO_SEM_WAIT(sem) sem_wait ((sem))
#    define MONO_SEM_POST(sem) sem_post ((sem))
#    define MONO_SEM_DESTROY(sem) sem_destroy ((sem))
#  endif
#elif defined(HOST_WIN32) || defined(_WAPI_SEMAPHORES_H)
#  define MONO_HAS_SEMAPHORES
typedef HANDLE MonoSemType;
#    define MONO_SEM_INIT(addr,initial) do {*(addr) = CreateSemaphore ( NULL,(initial),0x7FFFFFFF,NULL);} while(0)
#    define MONO_SEM_WAIT(sem) (!WaitForSingleObjectEx (*(sem),INFINITE, TRUE))
#    define MONO_SEM_POST(sem) (!(ReleaseSemaphore (*(sem),1,NULL)))
#    define MONO_SEM_DESTROY(sem) CloseHandle (*(sem))
#endif

#define MONO_SEM_TIMEDWAIT(sem, timeout_ms) mono_sem_timedwait ((sem), (timeout_ms)) 

G_BEGIN_DECLS

gboolean mono_sem_timedwait (MonoSemType *sem, guint32 timeout_ms);

G_END_DECLS
#endif /* _MONO_SEMAPHORE_H_ */
