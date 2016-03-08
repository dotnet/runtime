/*
 * threads.h:  Thread handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_THREADS_H_
#define _WAPI_THREADS_H_

#include <glib.h>

#include <mono/io-layer/handles.h>
#include <mono/io-layer/io.h>
#include <mono/io-layer/status.h>
#include <mono/io-layer/processes.h>
#include <mono/io-layer/access.h>

G_BEGIN_DECLS

#define STILL_ACTIVE STATUS_PENDING

#define THREAD_ALL_ACCESS		(STANDARD_RIGHTS_REQUIRED|SYNCHRONIZE|0x3ff)

typedef guint32 (*WapiThreadStart)(gpointer);

gpointer wapi_create_thread_handle (void);
void wapi_thread_handle_set_exited (gpointer handle, guint32 exitstatus);
void wapi_ref_thread_handle (gpointer handle);
gpointer wapi_get_current_thread_handle (void);

char* wapi_current_thread_desc (void);

extern gint32 GetThreadPriority (gpointer handle);
extern gboolean SetThreadPriority (gpointer handle, gint32 priority);

G_END_DECLS
#endif /* _WAPI_THREADS_H_ */
