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

#include "mono/io-layer/handles.h"
#include "mono/io-layer/io.h"
#include "mono/io-layer/status.h"
#include "mono/io-layer/processes.h"

#define TLS_MINIMUM_AVAILABLE 64
#define TLS_OUT_OF_INDEXES 0xFFFFFFFF

#define STILL_ACTIVE STATUS_PENDING


typedef guint32 (*WapiThreadStart)(gpointer);

extern gpointer CreateThread(WapiSecurityAttributes *security,
			     guint32 stacksize, WapiThreadStart start,
			     gpointer param, guint32 create, guint32 *tid);
extern void ExitThread(guint32 exitcode) G_GNUC_NORETURN;
extern gboolean GetExitCodeThread(gpointer handle, guint32 *exitcode);
extern guint32 GetCurrentThreadId(void);
extern gpointer GetCurrentThread(void);
extern guint32 ResumeThread(gpointer handle);
extern guint32 SuspendThread(gpointer handle);
extern guint32 TlsAlloc(void);
extern gboolean TlsFree(guint32 idx);
extern gpointer TlsGetValue(guint32 idx);
extern gboolean TlsSetValue(guint32 idx, gpointer value);
extern void Sleep(guint32 ms);
extern void SleepEx(guint32 ms, gboolean);

#endif /* _WAPI_THREADS_H_ */
