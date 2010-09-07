/*
 * wapi-private.h:  internal definitions of handles and shared memory layout
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Novell, Inc.
 */

#ifndef _WAPI_PRIVATE_H_
#define _WAPI_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <sys/stat.h>

#include <mono/io-layer/handles.h>
#include <mono/io-layer/io.h>

/* Increment this whenever an incompatible change is made to the
 * shared handle structure.
 */
#define _WAPI_HANDLE_VERSION 12

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
	WAPI_HANDLE_PIPE,
	WAPI_HANDLE_NAMEDMUTEX,
	WAPI_HANDLE_NAMEDSEM,
	WAPI_HANDLE_NAMEDEVENT,
	WAPI_HANDLE_COUNT
} WapiHandleType;

extern const char *_wapi_handle_typename[];

#define _WAPI_SHARED_HANDLE(type) (type == WAPI_HANDLE_PROCESS || \
				   type == WAPI_HANDLE_NAMEDMUTEX || \
				   type == WAPI_HANDLE_NAMEDSEM || \
				   type == WAPI_HANDLE_NAMEDEVENT)

#define _WAPI_FD_HANDLE(type) (type == WAPI_HANDLE_FILE || \
			       type == WAPI_HANDLE_CONSOLE || \
			       type == WAPI_HANDLE_SOCKET || \
			       type == WAPI_HANDLE_PIPE)

#define _WAPI_SHARED_NAMESPACE(type) (type == WAPI_HANDLE_NAMEDMUTEX || \
				      type == WAPI_HANDLE_NAMEDSEM || \
				      type == WAPI_HANDLE_NAMEDEVENT)

typedef struct 
{
	gchar name[MAX_PATH + 1];
} WapiSharedNamespace;

typedef enum {
	WAPI_HANDLE_CAP_WAIT=0x01,
	WAPI_HANDLE_CAP_SIGNAL=0x02,
	WAPI_HANDLE_CAP_OWN=0x04,
	WAPI_HANDLE_CAP_SPECIAL_WAIT=0x08
} WapiHandleCapability;

struct _WapiHandleOps 
{
	void (*close)(gpointer handle, gpointer data);

	/* SignalObjectAndWait */
	void (*signal)(gpointer signal);

	/* Called by WaitForSingleObject and WaitForMultipleObjects,
	 * with the handle locked (shared handles aren't locked.)
	 * Returns TRUE if ownership was established, false otherwise.
	 */
	gboolean (*own_handle)(gpointer handle);

	/* Called by WaitForSingleObject and WaitForMultipleObjects, if the
	 * handle in question is "ownable" (ie mutexes), to see if the current
	 * thread already owns this handle
	 */
	gboolean (*is_owned)(gpointer handle);

	/* Called by WaitForSingleObject and WaitForMultipleObjects,
	 * if the handle in question needs a special wait function
	 * instead of using the normal handle signal mechanism.
	 * Returns the WaitForSingleObject return code.
	 */
	guint32 (*special_wait)(gpointer handle, guint32 timeout);

	/* Called by WaitForSingleObject and WaitForMultipleObjects,
	 * if the handle in question needs some preprocessing before the
	 * signal wait.
	 */
	void (*prewait)(gpointer handle);
};

#include <mono/io-layer/event-private.h>
#include <mono/io-layer/io-private.h>
#include <mono/io-layer/mutex-private.h>
#include <mono/io-layer/semaphore-private.h>
#include <mono/io-layer/socket-private.h>
#include <mono/io-layer/thread-private.h>
#include <mono/io-layer/process-private.h>

struct _WapiHandle_shared_ref
{
	/* This will be split 16:16 with the shared file segment in
	 * the top half, when I implement space increases
	 */
	guint32 offset;
};

#define _WAPI_HANDLE_INITIAL_COUNT 256

struct _WapiHandleUnshared
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
		struct _WapiHandle_shared_ref shared;
	} u;
};

struct _WapiHandleShared
{
	WapiHandleType type;
	guint32 timestamp;
	guint32 handle_refs;
	volatile gboolean signalled;
	
	union
	{
		struct _WapiHandle_process process;
		struct _WapiHandle_namedmutex namedmutex;
		struct _WapiHandle_namedsem namedsem;
		struct _WapiHandle_namedevent namedevent;
	} u;
};

#define _WAPI_SHARED_SEM_NAMESPACE 0
/*#define _WAPI_SHARED_SEM_COLLECTION 1*/
#define _WAPI_SHARED_SEM_FILESHARE 2
#define _WAPI_SHARED_SEM_SHARED_HANDLES 3
#define _WAPI_SHARED_SEM_PROCESS_COUNT_LOCK 6
#define _WAPI_SHARED_SEM_PROCESS_COUNT 7
#define _WAPI_SHARED_SEM_COUNT 8	/* Leave some future expansion space */

struct _WapiHandleSharedLayout
{
	volatile guint32 collection_count;
	volatile key_t sem_key;
	
	struct _WapiHandleShared handles[_WAPI_HANDLE_INITIAL_COUNT];
};

#define _WAPI_FILESHARE_SIZE 102400

struct _WapiFileShare
{
#ifdef WAPI_FILE_SHARE_PLATFORM_EXTRA_DATA
	WAPI_FILE_SHARE_PLATFORM_EXTRA_DATA
#endif
	dev_t device;
	ino_t inode;
	pid_t opened_by_pid;
	guint32 sharemode;
	guint32 access;
	guint32 handle_refs;
	guint32 timestamp;
};

typedef struct _WapiFileShare _WapiFileShare;

struct _WapiFileShareLayout
{
	guint32 hwm;
	
	struct _WapiFileShare share_info[_WAPI_FILESHARE_SIZE];
};



#define _WAPI_HANDLE_INVALID (gpointer)-1

#endif /* _WAPI_PRIVATE_H_ */
