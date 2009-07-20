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
#ifdef HAVE_SEMAPHORE_H
#include <semaphore.h>
#endif

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
#endif

#endif /* _MONO_SEMAPHORE_H_ */
