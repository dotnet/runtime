#ifndef _WAPI_THREAD_PRIVATE_H_
#define _WAPI_THREAD_PRIVATE_H_

#include <config.h>
#include <glib.h>

#include <mono/io-layer/timed-thread.h>

extern struct _WapiHandleOps _wapi_thread_ops;

typedef enum {
	THREAD_STATE_START,
	THREAD_STATE_EXITED,
} WapiThreadState;

struct _WapiHandle_thread
{
	WapiThreadState state;
	guint32 exitstatus;
	gboolean joined;
	gpointer process_handle;
};

struct _WapiHandlePrivate_thread
{
	TimedThread *thread;
};

#endif /* _WAPI_THREAD_PRIVATE_H_ */
