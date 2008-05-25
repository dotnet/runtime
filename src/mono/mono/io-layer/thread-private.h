/*
 * thread-private.h:  Private definitions for thread handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_THREAD_PRIVATE_H_
#define _WAPI_THREAD_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <pthread.h>
#ifdef HAVE_SEMAPHORE_H
#include <semaphore.h>
#endif
#ifdef USE_MACH_SEMA
#include <mach/mach_init.h>
#include <mach/task.h>
#include <mach/semaphore.h>
typedef semaphore_t MonoSemType;
#define MONO_SEM_INIT(addr,value) semaphore_create (current_task (), (addr), SYNC_POLICY_FIFO, (value))
#define MONO_SEM_WAIT(sem) semaphore_wait (*(sem))
#define MONO_SEM_POST(sem) semaphore_signal (*(sem))
#define MONO_SEM_DESTROY(sem) semaphore_destroy (current_task (), *(sem))
#else
typedef sem_t MonoSemType;
#define MONO_SEM_INIT(addr,value) sem_init ((addr), 0, (value))
#define MONO_SEM_WAIT(sem) sem_wait ((sem))
#define MONO_SEM_POST(sem) sem_post ((sem))
#define MONO_SEM_DESTROY(sem) sem_destroy ((sem))
#endif

/* There doesn't seem to be a defined symbol for this */
#define _WAPI_THREAD_CURRENT (gpointer)0xFFFFFFFE
extern gpointer _wapi_thread_duplicate (void);

extern struct _WapiHandleOps _wapi_thread_ops;

typedef enum {
	THREAD_STATE_START,
	THREAD_STATE_EXITED
} WapiThreadState;

struct _WapiHandle_thread
{
	guint32 exitstatus;
	WapiThreadState state : 2;
	guint joined : 1;
	guint has_apc : 1;
	guint32 create_flags;
	/* Fields below this point are only valid for the owning process */
	pthread_t id;
	GPtrArray *owned_mutexes;
	gpointer handle;
	MonoSemType suspend_sem;
	guint32 (*start_routine)(gpointer arg);
	gpointer start_arg;
};

typedef struct
{
	guint32 (*callback)(gpointer arg);
	gpointer param;
} ApcInfo;

extern gboolean _wapi_thread_apc_pending (gpointer handle);
extern gboolean _wapi_thread_cur_apc_pending (void);
extern gboolean _wapi_thread_dispatch_apc_queue (gpointer handle);
extern void _wapi_thread_own_mutex (gpointer mutex);
extern void _wapi_thread_disown_mutex (gpointer mutex);
extern gpointer _wapi_thread_handle_from_id (pthread_t tid);
extern void _wapi_thread_set_termination_details (gpointer handle,
						  guint32 exitstatus);
extern void _wapi_thread_cleanup (void);

#endif /* _WAPI_THREAD_PRIVATE_H_ */
