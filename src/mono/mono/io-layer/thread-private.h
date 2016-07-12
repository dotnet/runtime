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

#include "wapi-private.h"

/* There doesn't seem to be a defined symbol for this */
#define _WAPI_THREAD_CURRENT (gpointer)0xFFFFFFFE

struct _WapiHandle_thread
{
	pthread_t id;
	GPtrArray *owned_mutexes;
	gint32 priority;
};

typedef struct _WapiHandle_thread WapiHandle_thread;

void
_wapi_thread_init (void);

extern gboolean _wapi_thread_cur_apc_pending (void);
extern void _wapi_thread_own_mutex (gpointer mutex);
extern void _wapi_thread_disown_mutex (gpointer mutex);
extern void _wapi_thread_cleanup (void);

#endif /* _WAPI_THREAD_PRIVATE_H_ */
