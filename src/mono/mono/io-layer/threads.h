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

extern gsize GetCurrentThreadId(void); /* NB return is 32bit in MS API */
extern void Sleep(guint32 ms);
extern guint32 SleepEx(guint32 ms, gboolean alertable);

void wapi_interrupt_thread (gpointer handle);
void wapi_clear_interruption (void);
gboolean wapi_thread_set_wait_handle (gpointer handle);
void wapi_thread_clear_wait_handle (gpointer handle);
void wapi_self_interrupt (void);

gpointer wapi_prepare_interrupt_thread (gpointer thread_handle);
void wapi_finish_interrupt_thread (gpointer wait_handle);

gpointer wapi_create_thread_handle (void);
void wapi_thread_handle_set_exited (gpointer handle, guint32 exitstatus);
void wapi_ref_thread_handle (gpointer handle);
gpointer wapi_get_current_thread_handle (void);

char* wapi_current_thread_desc (void);

G_END_DECLS
#endif /* _WAPI_THREADS_H_ */
