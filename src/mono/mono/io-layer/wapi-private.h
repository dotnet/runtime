#ifndef _WAPI_PRIVATE_H_
#define _WAPI_PRIVATE_H_

#include <config.h>
#include <glib.h>

#include "mono/io-layer/handles.h"
#include "mono/io-layer/io.h"

/* Increment this whenever an incompatible change is made to the
 * shared handle structure.
 *
 * If this ever reaches 255, we have problems :-(
 */
#define _WAPI_HANDLE_VERSION 1

typedef enum {
	WAPI_HANDLE_UNUSED=0,
	WAPI_HANDLE_FILE,
	WAPI_HANDLE_CONSOLE,
	WAPI_HANDLE_THREAD,
	WAPI_HANDLE_SEM,
	WAPI_HANDLE_MUTEX,
	WAPI_HANDLE_EVENT,
	WAPI_HANDLE_SOCKET,
	WAPI_HANDLE_FIND,
	WAPI_HANDLE_PROCESS,
	WAPI_HANDLE_COUNT,
} WapiHandleType;

typedef enum {
	WAPI_HANDLE_CAP_WAIT=0x01,
	WAPI_HANDLE_CAP_SIGNAL=0x02,
	WAPI_HANDLE_CAP_OWN=0x04,
} WapiHandleCapability;

struct _WapiHandleOps 
{
	void (*close_shared)(gpointer handle);
	void (*close_private)(gpointer handle);

	/* SignalObjectAndWait */
	void (*signal)(gpointer signal);

	/* Called by WaitForSingleObject and WaitForMultipleObjects,
	 * with the handle locked
	 */
	void (*own_handle)(gpointer handle);

	/* Called by WaitForSingleObject and WaitForMultipleObjects, if the
	 * handle in question is "ownable" (ie mutexes), to see if the current
	 * thread already owns this handle
	 */
	gboolean (*is_owned)(gpointer handle);
};

#include <mono/io-layer/event-private.h>
#include <mono/io-layer/io-private.h>
#include <mono/io-layer/mutex-private.h>
#include <mono/io-layer/semaphore-private.h>
#include <mono/io-layer/socket-private.h>
#include <mono/io-layer/thread-private.h>
#include <mono/io-layer/process-private.h>

struct _WapiHandleShared
{
	WapiHandleType type;
	guint ref;
	gboolean signalled;
	mono_mutex_t signal_mutex;
	pthread_cond_t signal_cond;
	
	union 
	{
		struct _WapiHandle_event event;
		struct _WapiHandle_file file;
		struct _WapiHandle_find find;
		struct _WapiHandle_mutex mutex;
		struct _WapiHandle_sem sem;
		struct _WapiHandle_socket sock;
		struct _WapiHandle_thread thread;
		struct _WapiHandle_process process;
	} u;
};

#define _WAPI_MAX_HANDLES 4096
#define _WAPI_HANDLE_INVALID (gpointer)-1

/*
 * This is the layout of the shared memory segment
 */
struct _WapiHandleShared_list
{
	/* UNIX_PATH_MAX doesnt seem to be defined in any accessible
	 * header file
	 */
	guchar daemon[108];
	guint32 daemon_running;
	
#ifdef _POSIX_THREAD_PROCESS_SHARED
	mono_mutex_t signal_mutex;
	pthread_cond_t signal_cond;
#endif
	struct _WapiHandleShared handles[_WAPI_MAX_HANDLES];
	guchar scratch_base[0];
};

struct _WapiHandlePrivate
{
	union 
	{
		struct _WapiHandlePrivate_event event;
		struct _WapiHandlePrivate_file file;
		struct _WapiHandlePrivate_find find;
		struct _WapiHandlePrivate_mutex mutex;
		struct _WapiHandlePrivate_sem sem;
		struct _WapiHandlePrivate_socket sock;
		struct _WapiHandlePrivate_thread thread;
		struct _WapiHandlePrivate_process process;
	} u;
};

/* Per-process handle info. For lookup convenience, each index matches
 * the corresponding shared data.
 */
struct _WapiHandlePrivate_list
{
#ifndef _POSIX_THREAD_PROCESS_SHARED
	mono_mutex_t signal_mutex;
	pthread_cond_t signal_cond;
#endif
	struct _WapiHandlePrivate handles[_WAPI_MAX_HANDLES];
};


#endif /* _WAPI_PRIVATE_H_ */
