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

/* There doesn't seem to be a defined symbol for this */
#define _WAPI_THREAD_CURRENT (gpointer)0xFFFFFFFE

extern struct _WapiHandleOps _wapi_thread_ops;

struct _WapiHandle_thread
{
	pthread_t id;
	GPtrArray *owned_mutexes;
};

typedef struct _WapiHandle_thread WapiHandle_thread;

extern gboolean _wapi_thread_cur_apc_pending (void);
extern void _wapi_thread_own_mutex (gpointer mutex);
extern void _wapi_thread_disown_mutex (gpointer mutex);
extern void _wapi_thread_cleanup (void);

#endif /* _WAPI_THREAD_PRIVATE_H_ */
