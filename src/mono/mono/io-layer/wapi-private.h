#ifndef _WAPI_PRIVATE_H_
#define _WAPI_PRIVATE_H_

#include <config.h>
#include <glib.h>

#include "mono/io-layer/handles.h"

typedef enum {
	WAPI_HANDLE_FILE,
	WAPI_HANDLE_CONSOLE,
	WAPI_HANDLE_THREAD,
	WAPI_HANDLE_SEM,
	WAPI_HANDLE_MUTEX,
	WAPI_HANDLE_EVENT,
	WAPI_HANDLE_COUNT,
} WapiHandleType;

struct _WapiHandleOps 
{
	/* All handle types */
	void (*close)(WapiHandle *handle);

	/* File, console and pipe handles */
	WapiFileType (*getfiletype)(void);
	
	/* File and console handles */
	gboolean (*readfile)(WapiHandle *handle, gpointer buffer,
			     guint32 numbytes, guint32 *bytesread,
			     WapiOverlapped *overlapped);
	gboolean (*writefile)(WapiHandle *handle, gconstpointer buffer,
			      guint32 numbytes, guint32 *byteswritten,
			      WapiOverlapped *overlapped);
	
	/* File handles */
	guint32 (*seek)(WapiHandle *handle, gint32 movedistance,
			gint32 *highmovedistance, WapiSeekMethod method);
	gboolean (*setendoffile)(WapiHandle *handle);
	guint32 (*getfilesize)(WapiHandle *handle, guint32 *highsize);
	gboolean (*getfiletime)(WapiHandle *handle, WapiFileTime *create_time,
				WapiFileTime *last_access,
				WapiFileTime *last_write);
	gboolean (*setfiletime)(WapiHandle *handle,
				const WapiFileTime *create_time,
				const WapiFileTime *last_access,
				const WapiFileTime *last_write);
	
	/* WaitForSingleObject */
	gboolean (*wait)(WapiHandle *handle, WapiHandle *signal, guint32 ms);

	/* WaitForMultipleObjects */
	guint32 (*wait_multiple)(gpointer data);

	/* SignalObjectAndWait */
	void (*signal)(WapiHandle *signal);
};

struct _WapiHandle
{
	WapiHandleType type;
	guint ref;
	gboolean signalled;
	struct _WapiHandleOps *ops;
};

#define _WAPI_HANDLE_INIT(_handle, _type, _ops)	G_STMT_START {\
		_handle->type=_type;\
		_handle->ref=1;\
		_handle->signalled=FALSE;\
		_handle->ops=&_ops;\
	} G_STMT_END;

#endif /* _WAPI_PRIVATE_H_ */
