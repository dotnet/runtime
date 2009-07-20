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
#include <mono/utils/mono-semaphore.h>

/* There doesn't seem to be a defined symbol for this */
#define _WAPI_THREAD_CURRENT (gpointer)0xFFFFFFFE
extern gpointer _wapi_thread_duplicate (void);

extern struct _WapiHandleOps _wapi_thread_ops;

typedef enum {
	THREAD_STATE_START,
	THREAD_STATE_EXITED
} WapiThreadState;

#define INTERRUPTION_REQUESTED_HANDLE (gpointer)0xFFFFFFFE

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
	/* 
     * Handle this thread waits on. If this is INTERRUPTION_REQUESTED_HANDLE,
	 * it means the thread is interrupted by another thread, and shouldn't enter
	 * a wait.
	 * This also acts as a reference for the handle.
	 */
	gpointer wait_handle;
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
