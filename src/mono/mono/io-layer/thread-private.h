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

#include <mono/io-layer/timed-thread.h>

extern struct _WapiHandleOps _wapi_thread_ops;

typedef enum {
	THREAD_STATE_START,
	THREAD_STATE_EXITED
} WapiThreadState;

struct _WapiHandle_thread
{
	WapiThreadState state;
	guint32 exitstatus;
	gpointer process_handle;
};

struct _WapiHandlePrivate_thread
{
	TimedThread *thread;
	gboolean joined;
};

gboolean _wapi_thread_apc_pending (gpointer handle);
gboolean _wapi_thread_cur_apc_pending (void);
gboolean _wapi_thread_dispatch_apc_queue (gpointer handle);


#endif /* _WAPI_THREAD_PRIVATE_H_ */
