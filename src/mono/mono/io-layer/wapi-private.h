/*
 * wapi-private.h:  internal definitions of handles and shared memory layout
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_PRIVATE_H_
#define _WAPI_PRIVATE_H_

#include <config.h>
#include <glib.h>

#include <mono/io-layer/handles.h>
#include <mono/io-layer/io.h>
#include <mono/io-layer/daemon-private.h>

/* for non-GCC compilers where Glib gives an empty pretty function 
 * macro create one that gives file & line number instead
 */
#ifndef __GNUC__
#undef G_GNUC_PRETTY_FUNCTION
#define STRINGIZE_HELPER(exp) #exp
#define STRINGIZE(exp) STRINGIZE_HELPER(exp)
#define G_GNUC_PRETTY_FUNCTION __FILE__ "(" STRINGIZE(__LINE__) ")"
#endif

/* Catch this here rather than corrupt the shared data at runtime */
#if MONO_SIZEOF_SUNPATH==0
#error configure failed to discover size of unix socket path
#endif

/* Increment this whenever an incompatible change is made to the
 * shared handle structure.
 */
#define _WAPI_HANDLE_VERSION 3

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
	WAPI_HANDLE_COUNT
} WapiHandleType;

#define _WAPI_SHARED_NAMESPACE(type) (type==WAPI_HANDLE_MUTEX)

typedef struct 
{
	guint32 name;
} WapiSharedNamespace;

typedef enum {
	WAPI_HANDLE_CAP_WAIT=0x01,
	WAPI_HANDLE_CAP_SIGNAL=0x02,
	WAPI_HANDLE_CAP_OWN=0x04
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

/* Shared threads don't seem to work yet */
#undef _POSIX_THREAD_PROCESS_SHARED

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

#define _WAPI_HANDLES_PER_SEGMENT 4096
#define _WAPI_HANDLE_INVALID (gpointer)-1

#define _WAPI_SHM_SCRATCH_SIZE 512000

/*
 * This is the layout of the shared scratch data.  When the data array
 * is filled, it will be expanded by _WAPI_SHM_SCRATCH_SIZE
 * bytes. (scratch data is always copied out of the shared memory, so
 * it doesn't matter that the mapping will move around.)
 */
struct _WapiHandleScratch
{
	guint32 data_len;

	/* This is set to TRUE by the daemon.  It determines whether a
	 * resize will go via mremap() or just realloc().
	 */
	gboolean is_shared;
	guchar scratch_data[MONO_ZERO_ARRAY_LENGTH];
};

/*
 * This is the layout of the shared memory segments.  When the handles
 * array is filled, another shared memory segment will be allocated
 * with the same structure.  This is to avoid having the shared memory
 * potentially move if it is resized and remapped.
 *
 * Note that the additional segments have the same structure, but only
 * the handle array is used.
 */
struct _WapiHandleShared_list
{
	guchar daemon[MONO_SIZEOF_SUNPATH];
	_wapi_daemon_status daemon_running;
	guint32 fd_offset_table_size;
	
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
	mono_mutex_t signal_mutex;
	pthread_cond_t signal_cond;
#endif

	/* This holds the number of segments */
	guint32 num_segments;
	struct _WapiHandleShared handles[_WAPI_HANDLES_PER_SEGMENT];
};

struct _WapiHandlePrivate
{
	WapiHandleType type;

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

/* Per-process handle info. For lookup convenience, each segment and
 * index matches the corresponding shared data.
 *
 * Note that the additional segments have the same structure, but only
 * the handle array is used.
 */
struct _WapiHandlePrivate_list
{
#if !defined(_POSIX_THREAD_PROCESS_SHARED) || _POSIX_THREAD_PROCESS_SHARED == -1
	mono_mutex_t signal_mutex;
	pthread_cond_t signal_cond;
#endif
	
	struct _WapiHandlePrivate handles[_WAPI_HANDLES_PER_SEGMENT];
};


#endif /* _WAPI_PRIVATE_H_ */
