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

#define TLS_MINIMUM_AVAILABLE 64
#define TLS_OUT_OF_INDEXES 0xFFFFFFFF

#define STILL_ACTIVE STATUS_PENDING


#define THREAD_TERMINATE		0x0001
#define THREAD_SUSPEND_RESUME		0x0002
#define THREAD_GET_CONTEXT		0x0008
#define THREAD_SET_CONTEXT		0x0010
#define THREAD_SET_INFORMATION		0x0020
#define THREAD_QUERY_INFORMATION	0x0040
#define THREAD_SET_THREAD_TOKEN		0x0080
#define THREAD_IMPERSONATE		0x0100
#define THREAD_DIRECT_IMPERSONATION	0x0200
#define THREAD_ALL_ACCESS		(STANDARD_RIGHTS_REQUIRED|SYNCHRONIZE|0x3ff)

typedef guint32 (*WapiThreadStart)(gpointer);
typedef guint32 (*WapiApcProc)(gpointer);

extern gpointer CreateThread(WapiSecurityAttributes *security,
			     guint32 stacksize, WapiThreadStart start,
			     gpointer param, guint32 create, guint32 *tid);
extern gpointer OpenThread (guint32 access, gboolean inherit, guint32 tid);
extern void ExitThread(guint32 exitcode) G_GNUC_NORETURN;
extern gboolean GetExitCodeThread(gpointer handle, guint32 *exitcode);
extern guint32 GetCurrentThreadId(void);
extern gpointer GetCurrentThread(void);
extern guint32 ResumeThread(gpointer handle);
extern guint32 SuspendThread(gpointer handle);
extern guint32 mono_pthread_key_for_tls (guint32 idx);
extern guint32 TlsAlloc(void);
extern gboolean TlsFree(guint32 idx);
extern gpointer TlsGetValue(guint32 idx);
extern gboolean TlsSetValue(guint32 idx, gpointer value);
extern void Sleep(guint32 ms);
extern guint32 SleepEx(guint32 ms, gboolean alertable);
extern gboolean BindIoCompletionCallback (gpointer handle,
					  WapiOverlappedCB callback,
					  guint64 flags);

extern guint32 QueueUserAPC (WapiApcProc apc_callback, gpointer thread_handle, 
					gpointer param);

#endif /* _WAPI_THREADS_H_ */
