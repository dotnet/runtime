/*
 * handles.c:  Generic and internal operations on handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2011 Novell, Inc.
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <errno.h>
#include <unistd.h>
#ifdef HAVE_SIGNAL_H
#include <signal.h>
#endif
#include <string.h>
#include <sys/types.h>
#ifdef HAVE_SYS_SOCKET_H
#  include <sys/socket.h>
#endif
#ifdef HAVE_SYS_UN_H
#  include <sys/un.h>
#endif
#ifdef HAVE_SYS_MMAN_H
#  include <sys/mman.h>
#endif
#ifdef HAVE_DIRENT_H
#  include <dirent.h>
#endif
#include <sys/stat.h>
#ifdef HAVE_SYS_RESOURCE_H
#  include <sys/resource.h>
#endif

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/process-private.h>
#include <mono/io-layer/io-trace.h>

#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-once.h>
#include <mono/utils/mono-logger-internals.h>
#undef DEBUG_REFS

#define _WAPI_PRIVATE_MAX_SLOTS		(1024 * 16)

/* must be a power of 2 */
#define _WAPI_HANDLE_INITIAL_COUNT	(256)

typedef struct {
	WapiHandleType type;
	guint ref;
	gboolean signalled;
	mono_mutex_t signal_mutex;
	mono_cond_t signal_cond;
} WapiHandleBase;

struct _WapiHandleUnshared
{
	WapiHandleBase base;
	
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
		struct _WapiHandle_shared_ref shared;
		struct _WapiHandle_namedmutex namedmutex;
		struct _WapiHandle_namedsem namedsem;
		struct _WapiHandle_namedevent namedevent;
	} u;
};

static void (*_wapi_handle_ops_get_close_func (WapiHandleType type))(gpointer, gpointer);

static WapiHandleCapability handle_caps[WAPI_HANDLE_COUNT] = { (WapiHandleCapability)0 };
static struct _WapiHandleOps *handle_ops[WAPI_HANDLE_COUNT]={
	NULL,
	&_wapi_file_ops,
	&_wapi_console_ops,
	&_wapi_thread_ops,
	&_wapi_sem_ops,
	&_wapi_mutex_ops,
	&_wapi_event_ops,
#ifndef DISABLE_SOCKETS
	&_wapi_socket_ops,
#endif
	&_wapi_find_ops,
	&_wapi_process_ops,
	&_wapi_pipe_ops,
	&_wapi_namedmutex_ops,
	&_wapi_namedsem_ops,
	&_wapi_namedevent_ops,
};

static void _wapi_shared_details (gpointer handle_info);

static void (*handle_details[WAPI_HANDLE_COUNT])(gpointer) = {
	NULL,
	_wapi_file_details,
	_wapi_console_details,
	_wapi_shared_details,	/* thread */
	_wapi_sem_details,
	_wapi_mutex_details,
	_wapi_event_details,
	NULL,			/* Nothing useful to see in a socket handle */
	NULL,			/* Nothing useful to see in a find handle */
	_wapi_shared_details,	/* process */
	_wapi_pipe_details,
	_wapi_shared_details,	/* namedmutex */
	_wapi_shared_details,	/* namedsem */
	_wapi_shared_details,	/* namedevent */
};

const char *_wapi_handle_typename[] = {
	"Unused",
	"File",
	"Console",
	"Thread",
	"Sem",
	"Mutex",
	"Event",
	"Socket",
	"Find",
	"Process",
	"Pipe",
	"N.Mutex",
	"N.Sem",
	"N.Event",
	"Error!!"
};

/*
 * We can hold _WAPI_PRIVATE_MAX_SLOTS * _WAPI_HANDLE_INITIAL_COUNT handles.
 * If 4M handles are not enough... Oh, well... we will crash.
 */
#define SLOT_INDEX(x)	(x / _WAPI_HANDLE_INITIAL_COUNT)
#define SLOT_OFFSET(x)	(x % _WAPI_HANDLE_INITIAL_COUNT)

struct _WapiHandleUnshared *_wapi_private_handles [_WAPI_PRIVATE_MAX_SLOTS];
static guint32 _wapi_private_handle_count = 0;
static guint32 _wapi_private_handle_slot_count = 0;

guint32 _wapi_fd_reserve;

/* 
 * This is an internal handle which is used for handling waiting for multiple handles.
 * Threads which wait for multiple handles wait on this one handle, and when a handle
 * is signalled, this handle is signalled too.
 */
static gpointer _wapi_global_signal_handle;

/* Point to the mutex/cond inside _wapi_global_signal_handle */
static mono_mutex_t *_wapi_global_signal_mutex;
static mono_cond_t *_wapi_global_signal_cond;

gboolean _wapi_has_shut_down = FALSE;

/* Use this instead of getpid(), to cope with linuxthreads.  It's a
 * function rather than a variable lookup because we need to get at
 * this before share_init() might have been called.
 */
static pid_t _wapi_pid;
static mono_once_t pid_init_once = MONO_ONCE_INIT;

static void _wapi_handle_unref_full (gpointer handle, gboolean ignore_private_busy_handles);

static void pid_init (void)
{
	_wapi_pid = getpid ();
}

pid_t _wapi_getpid (void)
{
	mono_once (&pid_init_once, pid_init);
	
	return(_wapi_pid);
}


static mono_mutex_t scan_mutex;

#define _WAPI_PRIVATE_HANDLES(x) ((WapiHandleBase*)(&_wapi_private_handles [SLOT_INDEX ((guint32) x)][SLOT_OFFSET ((guint32) x)]))

static gboolean
_WAPI_PRIVATE_HAVE_SLOT (guint32 x)
{
	return (x / _WAPI_PRIVATE_MAX_SLOTS) < _WAPI_PRIVATE_MAX_SLOTS && _wapi_private_handles [SLOT_INDEX (x)];
}

static gboolean
_WAPI_PRIVATE_VALID_SLOT (guint32 x)
{
	return SLOT_INDEX (x) < _WAPI_PRIVATE_MAX_SLOTS;
}

WapiHandleType
_wapi_handle_type (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);

	if (!_WAPI_PRIVATE_VALID_SLOT (idx) || !_WAPI_PRIVATE_HAVE_SLOT (idx))
		return WAPI_HANDLE_UNUSED;	/* An impossible type */

	return _WAPI_PRIVATE_HANDLES(idx)->type;
}

void
_wapi_handle_set_signal_state (gpointer handle, gboolean state, gboolean broadcast)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleBase *handle_data;
	int thr_ret;

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return;
	}

	handle_data = _WAPI_PRIVATE_HANDLES(idx);
	
#ifdef DEBUG
	g_message ("%s: setting state of %p to %s (broadcast %s)", __func__,
		   handle, state?"TRUE":"FALSE", broadcast?"TRUE":"FALSE");
#endif

	if (state == TRUE) {
		/* Tell everyone blocking on a single handle */

		/* The condition the global signal cond is waiting on is the signalling of
		 * _any_ handle. So lock it before setting the signalled state.
		 */
		thr_ret = mono_os_mutex_lock (_wapi_global_signal_mutex);
		if (thr_ret != 0)
			g_warning ("Bad call to mono_os_mutex_lock result %d for global signal mutex", thr_ret);
		g_assert (thr_ret == 0);

		/* This function _must_ be called with
		 * handle->signal_mutex locked
		 */
		handle_data->signalled=state;
		
		if (broadcast == TRUE) {
			thr_ret = mono_os_cond_broadcast (&handle_data->signal_cond);
			if (thr_ret != 0)
				g_warning ("Bad call to mono_os_cond_broadcast result %d for handle %p", thr_ret, handle);
			g_assert (thr_ret == 0);
		} else {
			thr_ret = mono_os_cond_signal (&handle_data->signal_cond);
			if (thr_ret != 0)
				g_warning ("Bad call to mono_os_cond_signal result %d for handle %p", thr_ret, handle);
			g_assert (thr_ret == 0);
		}

		/* Tell everyone blocking on multiple handles that something
		 * was signalled
		 */			
		thr_ret = mono_os_cond_broadcast (_wapi_global_signal_cond);
		if (thr_ret != 0)
			g_warning ("Bad call to mono_os_cond_broadcast result %d for handle %p", thr_ret, handle);
		g_assert (thr_ret == 0);
			
		thr_ret = mono_os_mutex_unlock (_wapi_global_signal_mutex);
		if (thr_ret != 0)
			g_warning ("Bad call to mono_os_mutex_unlock result %d for global signal mutex", thr_ret);
		g_assert (thr_ret == 0);
	} else {
		handle_data->signalled=state;
	}
}

gboolean
_wapi_handle_issignalled (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	
	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(FALSE);
	}

	return _WAPI_PRIVATE_HANDLES (idx)->signalled;
}

int
_wapi_handle_lock_signal_mutex (void)
{
#ifdef DEBUG
	g_message ("%s: lock global signal mutex", __func__);
#endif

	return(mono_os_mutex_lock (_wapi_global_signal_mutex));
}

int
_wapi_handle_unlock_signal_mutex (void)
{
#ifdef DEBUG
	g_message ("%s: unlock global signal mutex", __func__);
#endif

	return(mono_os_mutex_unlock (_wapi_global_signal_mutex));
}

int
_wapi_handle_lock_handle (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	
#ifdef DEBUG
	g_message ("%s: locking handle %p", __func__, handle);
#endif

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(0);
	}
	
	_wapi_handle_ref (handle);

	return(mono_os_mutex_lock (&_WAPI_PRIVATE_HANDLES(idx)->signal_mutex));
}

int
_wapi_handle_trylock_handle (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	int ret;
	
#ifdef DEBUG
	g_message ("%s: locking handle %p", __func__, handle);
#endif

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(0);
	}
	
	_wapi_handle_ref (handle);

	ret = mono_os_mutex_trylock (&_WAPI_PRIVATE_HANDLES(idx)->signal_mutex);
	if (ret != 0) {
		_wapi_handle_unref (handle);
	}
	
	return(ret);
}

int
_wapi_handle_unlock_handle (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	int ret;
	
#ifdef DEBUG
	g_message ("%s: unlocking handle %p", __func__, handle);
#endif
	
	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(0);
	}

	ret = mono_os_mutex_unlock (&_WAPI_PRIVATE_HANDLES(idx)->signal_mutex);

	_wapi_handle_unref (handle);
	
	return(ret);
}

static void handle_cleanup (void)
{
	int i, j, k;
	
	/* Every shared handle we were using ought really to be closed
	 * by now, but to make sure just blow them all away.  The
	 * exiting finalizer thread in particular races us to the
	 * program exit and doesn't always win, so it can be left
	 * cluttering up the shared file.  Anything else left over is
	 * really a bug.
	 */
	for(i = SLOT_INDEX (0); _wapi_private_handles[i] != NULL; i++) {
		for(j = SLOT_OFFSET (0); j < _WAPI_HANDLE_INITIAL_COUNT; j++) {
			WapiHandleBase *handle_data = (WapiHandleBase*) &_wapi_private_handles[i][j];
			gpointer handle = GINT_TO_POINTER (i*_WAPI_HANDLE_INITIAL_COUNT+j);

			for(k = handle_data->ref; k > 0; k--) {
				_wapi_handle_unref_full (handle, TRUE);
			}
		}
	}

	_wapi_io_cleanup ();

	for (i = 0; i < _WAPI_PRIVATE_MAX_SLOTS; ++i)
		g_free (_wapi_private_handles [i]);
}

int
wapi_getdtablesize (void)
{
	return eg_getdtablesize ();
}

/*
 * wapi_init:
 *
 *   Initialize the io-layer.
 */
void
wapi_init (void)
{
	g_assert ((sizeof (handle_ops) / sizeof (handle_ops[0]))
		  == WAPI_HANDLE_COUNT);

	_wapi_fd_reserve = wapi_getdtablesize ();

	/* This is needed by the code in _wapi_handle_new_internal */
	_wapi_fd_reserve = (_wapi_fd_reserve + (_WAPI_HANDLE_INITIAL_COUNT - 1)) & ~(_WAPI_HANDLE_INITIAL_COUNT - 1);

	do {
		/* 
		 * The entries in _wapi_private_handles reserved for fds are allocated lazily to 
		 * save memory.
		 */
		/*
		_wapi_private_handles [idx++] = g_new0 (struct _WapiHandleUnshared,
							_WAPI_HANDLE_INITIAL_COUNT);
		*/

		_wapi_private_handle_count += _WAPI_HANDLE_INITIAL_COUNT;
		_wapi_private_handle_slot_count ++;
	} while(_wapi_fd_reserve > _wapi_private_handle_count);

	_wapi_shm_semaphores_init ();

	_wapi_io_init ();
	mono_os_mutex_init (&scan_mutex);

	_wapi_global_signal_handle = _wapi_handle_new (WAPI_HANDLE_EVENT, NULL);

	_wapi_global_signal_cond = &_WAPI_PRIVATE_HANDLES (GPOINTER_TO_UINT (_wapi_global_signal_handle))->signal_cond;
	_wapi_global_signal_mutex = &_WAPI_PRIVATE_HANDLES (GPOINTER_TO_UINT (_wapi_global_signal_handle))->signal_mutex;

	wapi_processes_init ();
}

void
wapi_cleanup (void)
{
	g_assert (_wapi_has_shut_down == FALSE);
	
	_wapi_has_shut_down = TRUE;

	_wapi_error_cleanup ();
	_wapi_thread_cleanup ();
	wapi_processes_cleanup ();
	handle_cleanup ();
}

static size_t _wapi_handle_struct_size (WapiHandleType type)
{
	size_t type_size;

	switch (type) {
		case WAPI_HANDLE_FILE: case WAPI_HANDLE_CONSOLE: case WAPI_HANDLE_PIPE:
			type_size = sizeof (struct _WapiHandle_file);
			break;
		case WAPI_HANDLE_THREAD:
			type_size = sizeof (struct _WapiHandle_thread);
			break;
		case WAPI_HANDLE_SEM:
			type_size = sizeof (struct _WapiHandle_sem);
			break;
		case WAPI_HANDLE_MUTEX:
			type_size = sizeof (struct _WapiHandle_mutex);
			break;
		case WAPI_HANDLE_EVENT:
			type_size = sizeof (struct _WapiHandle_event);
			break;
		case WAPI_HANDLE_SOCKET:
			type_size = sizeof (struct _WapiHandle_socket);
			break;
		case WAPI_HANDLE_FIND:
			type_size = sizeof (struct _WapiHandle_find);
			break;
		case WAPI_HANDLE_PROCESS:
			type_size = sizeof (struct _WapiHandle_process);
			break;
		case WAPI_HANDLE_NAMEDMUTEX:
			type_size = sizeof (struct _WapiHandle_namedmutex);
			break;
		case WAPI_HANDLE_NAMEDSEM:
			type_size = sizeof (struct _WapiHandle_namedsem);
			break;
		case WAPI_HANDLE_NAMEDEVENT:
			type_size = sizeof (struct _WapiHandle_namedevent);
			break;

		default:
			g_error ("Unknown WapiHandleType: %d\n", type);
	}

	return type_size;
}

static void _wapi_handle_init (WapiHandleBase *handle,
			       WapiHandleType type, gpointer handle_specific)
{
	int thr_ret;
	int type_size;
	
	g_assert (_wapi_has_shut_down == FALSE);
	
	handle->type = type;
	handle->signalled = FALSE;
	handle->ref = 1;
	
	thr_ret = mono_os_cond_init (&handle->signal_cond);
	g_assert (thr_ret == 0);
			
	thr_ret = mono_os_mutex_init (&handle->signal_mutex);
	g_assert (thr_ret == 0);

	if (handle_specific != NULL) {
		type_size = _wapi_handle_struct_size (type);
		memcpy (&((struct _WapiHandleUnshared*) handle)->u, handle_specific,
			type_size);
	}
}

/*
 * _wapi_handle_new_internal:
 * @type: Init handle to this type
 *
 * Search for a free handle and initialize it. Return the handle on
 * success and 0 on failure.  This is only called from
 * _wapi_handle_new, and scan_mutex must be held.
 */
static guint32 _wapi_handle_new_internal (WapiHandleType type,
					  gpointer handle_specific)
{
	guint32 i, k, count;
	static guint32 last = 0;
	gboolean retry = FALSE;
	
	g_assert (_wapi_has_shut_down == FALSE);
	
	/* A linear scan should be fast enough.  Start from the last
	 * allocation, assuming that handles are allocated more often
	 * than they're freed. Leave the space reserved for file
	 * descriptors
	 */
	
	if (last < _wapi_fd_reserve) {
		last = _wapi_fd_reserve;
	} else {
		retry = TRUE;
	}

again:
	count = last;
	for(i = SLOT_INDEX (count); i < _wapi_private_handle_slot_count; i++) {
		if (_wapi_private_handles [i]) {
			for (k = SLOT_OFFSET (count); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
				WapiHandleBase *handle = (WapiHandleBase*) &_wapi_private_handles [i][k];

				if(handle->type == WAPI_HANDLE_UNUSED) {
					last = count + 1;
			
					_wapi_handle_init (handle, type, handle_specific);
					return (count);
				}
				count++;
			}
		}
	}

	if(retry && last > _wapi_fd_reserve) {
		/* Try again from the beginning */
		last = _wapi_fd_reserve;
		goto again;
	}

	/* Will need to expand the array.  The caller will sort it out */

	return(0);
}

gpointer 
_wapi_handle_new (WapiHandleType type, gpointer handle_specific)
{
	guint32 handle_idx = 0;
	gpointer handle;
	int thr_ret;

	g_assert (_wapi_has_shut_down == FALSE);
		
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Creating new handle of type %s", __func__,
		   _wapi_handle_typename[type]);

	g_assert(!_WAPI_FD_HANDLE(type));
	
	thr_ret = mono_os_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);
		
	while ((handle_idx = _wapi_handle_new_internal (type, handle_specific)) == 0) {
		/* Try and expand the array, and have another go */
		int idx = SLOT_INDEX (_wapi_private_handle_count);
		if (idx >= _WAPI_PRIVATE_MAX_SLOTS) {
			break;
		}

		_wapi_private_handles [idx] = g_new0 (struct _WapiHandleUnshared,
						_WAPI_HANDLE_INITIAL_COUNT);

		_wapi_private_handle_count += _WAPI_HANDLE_INITIAL_COUNT;
		_wapi_private_handle_slot_count ++;
	}
	
	thr_ret = mono_os_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);

	if (handle_idx == 0) {
		/* We ran out of slots */
		handle = _WAPI_HANDLE_INVALID;
		goto done;
	}
		
	/* Make sure we left the space for fd mappings */
	g_assert (handle_idx >= _wapi_fd_reserve);
	
	handle = GUINT_TO_POINTER (handle_idx);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Allocated new handle %p", __func__, handle);

done:
	return(handle);
}

static void
init_handles_slot (int idx)
{
	int thr_ret;

	thr_ret = mono_os_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);

	if (_wapi_private_handles [idx] == NULL) {
		_wapi_private_handles [idx] = g_new0 (struct _WapiHandleUnshared,
											  _WAPI_HANDLE_INITIAL_COUNT);
		g_assert (_wapi_private_handles [idx]);
	}

	thr_ret = mono_os_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
}

gpointer _wapi_handle_new_fd (WapiHandleType type, int fd,
			      gpointer handle_specific)
{
	WapiHandleBase *handle;
	int thr_ret;
	
	g_assert (_wapi_has_shut_down == FALSE);
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Creating new handle of type %s", __func__,
		   _wapi_handle_typename[type]);
	
	g_assert(_WAPI_FD_HANDLE(type));

	if (fd >= _wapi_fd_reserve) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: fd %d is too big", __func__, fd);

		return(GUINT_TO_POINTER (_WAPI_HANDLE_INVALID));
	}

	/* Initialize the array entries on demand */
	if (_wapi_private_handles [SLOT_INDEX (fd)] == NULL)
		init_handles_slot (SLOT_INDEX (fd));

	handle = _WAPI_PRIVATE_HANDLES(fd);
	
	if (handle->type != WAPI_HANDLE_UNUSED) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: fd %d is already in use!", __func__, fd);
		/* FIXME: clean up this handle?  We can't do anything
		 * with the fd, cos thats the new one
		 */
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Assigning new fd handle %d", __func__, fd);

	/* Prevent file share entries racing with us, when the file
	 * handle is only half initialised
	 */
	thr_ret = _wapi_shm_sem_lock (_WAPI_SHARED_SEM_FILESHARE);
	g_assert(thr_ret == 0);

	_wapi_handle_init (handle, type, handle_specific);

	thr_ret = _wapi_shm_sem_unlock (_WAPI_SHARED_SEM_FILESHARE);

	return(GUINT_TO_POINTER(fd));
}

gboolean 
_wapi_lookup_handle (gpointer handle, WapiHandleType type,
			      gpointer *handle_specific)
{
	WapiHandleBase *handle_data;
	guint32 handle_idx = GPOINTER_TO_UINT(handle);

	if (!_WAPI_PRIVATE_VALID_SLOT (handle_idx)) {
		return(FALSE);
	}

	/* Initialize the array entries on demand */
	if (_wapi_private_handles [SLOT_INDEX (handle_idx)] == NULL)
		init_handles_slot (SLOT_INDEX (handle_idx));
	
	handle_data = _WAPI_PRIVATE_HANDLES(handle_idx);
	
	if (handle_data->type != type) {
		return(FALSE);
	}

	if (handle_specific == NULL) {
		return(FALSE);
	}
	
	*handle_specific = &((struct _WapiHandleUnshared*) handle_data)->u;
	
	return(TRUE);
}

void
_wapi_handle_foreach (WapiHandleType type,
			gboolean (*on_each)(gpointer test, gpointer user),
			gpointer user_data)
{
	WapiHandleBase *handle_data = NULL;
	gpointer ret = NULL;
	guint32 i, k;
	int thr_ret;

	thr_ret = mono_os_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);

	for (i = SLOT_INDEX (0); i < _wapi_private_handle_slot_count; i++) {
		if (_wapi_private_handles [i]) {
			for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
				handle_data = (WapiHandleBase*) &_wapi_private_handles [i][k];
			
				if (handle_data->type == type) {
					ret = GUINT_TO_POINTER (i * _WAPI_HANDLE_INITIAL_COUNT + k);
					if (on_each (ret, user_data) == TRUE)
						break;
				}
			}
		}
	}

	thr_ret = mono_os_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
}

/* This might list some shared handles twice if they are already
 * opened by this process, and the check function returns FALSE the
 * first time.  Shared handles that are created during the search are
 * unreffed if the check function returns FALSE, so callers must not
 * rely on the handle persisting (unless the check function returns
 * TRUE)
 * The caller owns the returned handle.
 */
gpointer _wapi_search_handle (WapiHandleType type,
			      gboolean (*check)(gpointer test, gpointer user),
			      gpointer user_data,
			      gpointer *handle_specific,
			      gboolean search_shared)
{
	WapiHandleBase *handle_data = NULL;
	gpointer ret = NULL;
	guint32 i, k;
	gboolean found = FALSE;
	int thr_ret;

	thr_ret = mono_os_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);
	
	for (i = SLOT_INDEX (0); !found && i < _wapi_private_handle_slot_count; i++) {
		if (_wapi_private_handles [i]) {
			for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
				handle_data = (WapiHandleBase*) &_wapi_private_handles [i][k];
		
				if (handle_data->type == type) {
					ret = GUINT_TO_POINTER (i * _WAPI_HANDLE_INITIAL_COUNT + k);
					if (check (ret, user_data) == TRUE) {
						_wapi_handle_ref (ret);
						found = TRUE;
						break;
					}
				}
			}
		}
	}

	thr_ret = mono_os_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);

	if (!found) {
		ret = NULL;
		goto done;
	}
	
	if(handle_specific != NULL) {
		*handle_specific = &((struct _WapiHandleUnshared*) handle_data)->u;
	}

done:
	return(ret);
}

/* Returns the offset of the metadata array, or _WAPI_HANDLE_INVALID on error, or NULL for
 * not found
 */
gpointer _wapi_search_handle_namespace (WapiHandleType type,
				      gchar *utf8_name)
{
	WapiHandleBase *handle_data;
	guint32 i, k;
	gpointer ret = NULL;
	int thr_ret;
	
	g_assert(_WAPI_SHARED_NAMESPACE(type));
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Lookup for handle named [%s] type %s", __func__,
		   utf8_name, _wapi_handle_typename[type]);

	thr_ret = mono_os_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);
	
	for(i = SLOT_INDEX (0); i < _wapi_private_handle_slot_count; i++) {
		if (!_wapi_private_handles [i])
			continue;
		for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
			WapiSharedNamespace *sharedns;
			
			handle_data = (WapiHandleBase*) &_wapi_private_handles [i][k];

			/* Check mutex, event, semaphore, timer, job and
			 * file-mapping object names.  So far only mutex,
			 * semaphore and event are implemented.
			 */
			if (!_WAPI_SHARED_NAMESPACE (handle_data->type)) {
				continue;
			}

			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: found a shared namespace handle at 0x%x (type %s)", __func__, i, _wapi_handle_typename[handle_data->type]);

			switch (handle_data->type) {
			case WAPI_HANDLE_NAMEDMUTEX: sharedns = &((struct _WapiHandleUnshared*) handle_data)->u.namedmutex.sharedns; break;
			case WAPI_HANDLE_NAMEDSEM:   sharedns = &((struct _WapiHandleUnshared*) handle_data)->u.namedsem.sharedns;   break;
			case WAPI_HANDLE_NAMEDEVENT: sharedns = &((struct _WapiHandleUnshared*) handle_data)->u.namedevent.sharedns; break;
			default:
				g_assert_not_reached ();
			}
				
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: name is [%s]", __func__, sharedns->name);

			if (strcmp (sharedns->name, utf8_name) == 0) {
				if (handle_data->type != type) {
					/* Its the wrong type, so fail now */
					MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: handle 0x%x matches name but is wrong type: %s", __func__, i, _wapi_handle_typename[handle_data->type]);
					ret = _WAPI_HANDLE_INVALID;
					goto done;
				} else {
					MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: handle 0x%x matches name and type", __func__, i);
					ret = handle_data;
					goto done;
				}
			}
		}
	}

done:
	thr_ret = mono_os_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
	
	return(ret);
}

void _wapi_handle_ref (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleBase *handle_data;

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Attempting to ref invalid private handle %p", __func__, handle);
		return;
	}
	
	if (_wapi_handle_type (handle) == WAPI_HANDLE_UNUSED) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Attempting to ref unused handle %p", __func__, handle);
		return;
	}

	handle_data = _WAPI_PRIVATE_HANDLES(idx);
	
	InterlockedIncrement ((gint32 *)&handle_data->ref);
	
#ifdef DEBUG_REFS
	g_message ("%s: %s handle %p ref now %d",
		__func__, _wapi_handle_typename[handle_data->type], handle, handle_data->ref);
#endif
}

/* The handle must not be locked on entry to this function */
static void _wapi_handle_unref_full (gpointer handle, gboolean ignore_private_busy_handles)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleBase *handle_data;
	gboolean destroy = FALSE, early_exit = FALSE;
	int thr_ret;

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return;
	}
	
	handle_data = _WAPI_PRIVATE_HANDLES(idx);

	if (handle_data->type == WAPI_HANDLE_UNUSED) {
		g_warning ("%s: Attempting to unref unused handle %p",
			   __func__, handle);
		return;
	}

	/* Possible race condition here if another thread refs the
	 * handle between here and setting the type to UNUSED.  I
	 * could lock a mutex, but I'm not sure that allowing a handle
	 * reference to reach 0 isn't an application bug anyway.
	 */
	destroy = (InterlockedDecrement ((gint32 *)&handle_data->ref) ==0);
	
#ifdef DEBUG_REFS
	g_message ("%s: %s handle %p ref now %d (destroy %s)",
		__func__, _wapi_handle_typename[handle_data->type], handle, handle_data->ref, destroy?"TRUE":"FALSE");
#endif
	
	if(destroy==TRUE) {
		/* Need to copy the handle info, reset the slot in the
		 * array, and _only then_ call the close function to
		 * avoid race conditions (eg file descriptors being
		 * closed, and another file being opened getting the
		 * same fd racing the memset())
		 */
		struct _WapiHandleUnshared handle_data_cpy;
		WapiHandleType type = handle_data->type;
		void (*close_func)(gpointer, gpointer) = _wapi_handle_ops_get_close_func (type);

		thr_ret = mono_os_mutex_lock (&scan_mutex);

		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Destroying handle %p", __func__, handle);
		
		memcpy (&handle_data_cpy, handle_data,
			sizeof (struct _WapiHandleUnshared));

		memset (&((struct _WapiHandleUnshared*) handle_data)->u, '\0',
			sizeof(((struct _WapiHandleUnshared*) handle_data)->u));

		handle_data->type = WAPI_HANDLE_UNUSED;

		/* Destroy the mutex and cond var.  We hope nobody
		 * tried to grab them between the handle unlock and
		 * now, but pthreads doesn't have a
		 * "unlock_and_destroy" atomic function.
		 */
		thr_ret = mono_os_mutex_destroy (&handle_data->signal_mutex);
		/*WARNING gross hack to make cleanup not crash when exiting without the whole runtime teardown.*/
		if (thr_ret == EBUSY && ignore_private_busy_handles) {
			early_exit = TRUE;
		} else {
			if (thr_ret != 0)
				g_error ("Error destroying handle %p mutex due to %d\n", handle, thr_ret);

			thr_ret = mono_os_cond_destroy (&handle_data->signal_cond);
			if (thr_ret == EBUSY && ignore_private_busy_handles)
				early_exit = TRUE;
			else if (thr_ret != 0)
				g_error ("Error destroying handle %p cond var due to %d\n", handle, thr_ret);
		}

		thr_ret = mono_os_mutex_unlock (&scan_mutex);
		g_assert (thr_ret == 0);

		if (early_exit)
			return;
		
		if (close_func != NULL) {
			close_func (handle, &handle_data_cpy.u);
		}
	}
}

void _wapi_handle_unref (gpointer handle)
{
	_wapi_handle_unref_full (handle, FALSE);
}

void _wapi_handle_register_capabilities (WapiHandleType type,
					 WapiHandleCapability caps)
{
	handle_caps[type] = caps;
}

gboolean _wapi_handle_test_capabilities (gpointer handle,
					 WapiHandleCapability caps)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(FALSE);
	}
	
	type = _WAPI_PRIVATE_HANDLES(idx)->type;

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: testing 0x%x against 0x%x (%d)", __func__,
		   handle_caps[type], caps, handle_caps[type] & caps);
	
	return((handle_caps[type] & caps) != 0);
}

static void (*_wapi_handle_ops_get_close_func (WapiHandleType type))(gpointer, gpointer)
{
	if (handle_ops[type] != NULL &&
	    handle_ops[type]->close != NULL) {
		return (handle_ops[type]->close);
	}

	return (NULL);
}

void _wapi_handle_ops_close (gpointer handle, gpointer data)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return;
	}
	
	type = _WAPI_PRIVATE_HANDLES(idx)->type;

	if (handle_ops[type] != NULL &&
	    handle_ops[type]->close != NULL) {
		handle_ops[type]->close (handle, data);
	}
}

void _wapi_handle_ops_signal (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return;
	}
	
	type = _WAPI_PRIVATE_HANDLES(idx)->type;

	if (handle_ops[type] != NULL && handle_ops[type]->signal != NULL) {
		handle_ops[type]->signal (handle);
	}
}

gboolean _wapi_handle_ops_own (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;
	
	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(FALSE);
	}
	
	type = _WAPI_PRIVATE_HANDLES(idx)->type;

	if (handle_ops[type] != NULL && handle_ops[type]->own_handle != NULL) {
		return(handle_ops[type]->own_handle (handle));
	} else {
		return(FALSE);
	}
}

gboolean _wapi_handle_ops_isowned (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(FALSE);
	}
	
	type = _WAPI_PRIVATE_HANDLES(idx)->type;

	if (handle_ops[type] != NULL && handle_ops[type]->is_owned != NULL) {
		return(handle_ops[type]->is_owned (handle));
	} else {
		return(FALSE);
	}
}

guint32 _wapi_handle_ops_special_wait (gpointer handle, guint32 timeout, gboolean alertable)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;
	
	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(WAIT_FAILED);
	}
	
	type = _WAPI_PRIVATE_HANDLES(idx)->type;
	
	if (handle_ops[type] != NULL &&
	    handle_ops[type]->special_wait != NULL) {
		return(handle_ops[type]->special_wait (handle, timeout, alertable));
	} else {
		return(WAIT_FAILED);
	}
}

void _wapi_handle_ops_prewait (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT (handle);
	WapiHandleType type;
	
	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return;
	}
	
	type = _WAPI_PRIVATE_HANDLES (idx)->type;
	
	if (handle_ops[type] != NULL &&
	    handle_ops[type]->prewait != NULL) {
		handle_ops[type]->prewait (handle);
	}
}


/**
 * CloseHandle:
 * @handle: The handle to release
 *
 * Closes and invalidates @handle, releasing any resources it
 * consumes.  When the last handle to a temporary or non-persistent
 * object is closed, that object can be deleted.  Closing the same
 * handle twice is an error.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean CloseHandle(gpointer handle)
{
	if (handle == NULL) {
		/* Problem: because we map file descriptors to the
		 * same-numbered handle we can't tell the difference
		 * between a bogus handle and the handle to stdin.
		 * Assume that it's the console handle if that handle
		 * exists...
		 */
		if (_WAPI_PRIVATE_HANDLES (0)->type != WAPI_HANDLE_CONSOLE) {
			SetLastError (ERROR_INVALID_PARAMETER);
			return(FALSE);
		}
	}
	if (handle == _WAPI_HANDLE_INVALID){
		SetLastError (ERROR_INVALID_PARAMETER);
		return(FALSE);
	}
	
	_wapi_handle_unref (handle);
	
	return(TRUE);
}

/* Lots more to implement here, but this is all we need at the moment */
gboolean DuplicateHandle (gpointer srcprocess, gpointer src,
			  gpointer targetprocess, gpointer *target,
			  guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, guint32 options G_GNUC_UNUSED)
{
	if (srcprocess != _WAPI_PROCESS_CURRENT ||
	    targetprocess != _WAPI_PROCESS_CURRENT) {
		/* Duplicating other process's handles is not supported */
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	if (src == _WAPI_PROCESS_CURRENT) {
		*target = _wapi_process_duplicate ();
	} else if (src == _WAPI_THREAD_CURRENT) {
		g_assert_not_reached ();
	} else {
		_wapi_handle_ref (src);
		*target = src;
	}
	
	return(TRUE);
}

gboolean _wapi_handle_count_signalled_handles (guint32 numhandles,
					       gpointer *handles,
					       gboolean waitall,
					       guint32 *retcount,
					       guint32 *lowest)
{
	guint32 count, i, iter=0;
	gboolean ret;
	int thr_ret;
	
	/* Lock all the handles, with backoff */
again:
	for(i=0; i<numhandles; i++) {
		gpointer handle = handles[i];

		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: attempting to lock %p", __func__, handle);

		thr_ret = _wapi_handle_trylock_handle (handle);
		
		if (thr_ret != 0) {
			/* Bummer */
			
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: attempt failed for %p: %s", __func__,
				   handle, strerror (thr_ret));

			while (i--) {
				handle = handles[i];

				thr_ret = _wapi_handle_unlock_handle (handle);
				g_assert (thr_ret == 0);
			}

			/* If iter ever reaches 100 the nanosleep will
			 * return EINVAL immediately, but we have a
			 * design flaw if that happens.
			 */
			iter++;
			if(iter==100) {
				g_warning ("%s: iteration overflow!",
					   __func__);
				iter=1;
			}
			
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Backing off for %d ms", __func__,
				   iter*10);
			_wapi_handle_spin (10 * iter);
			
			goto again;
		}
	}
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Locked all handles", __func__);

	count=0;
	*lowest=numhandles;
	
	for(i=0; i<numhandles; i++) {
		gpointer handle = handles[i];

		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Checking handle %p", __func__, handle);

		if(((_wapi_handle_test_capabilities (handle, WAPI_HANDLE_CAP_OWN)==TRUE) &&
		    (_wapi_handle_ops_isowned (handle) == TRUE)) ||
		   (_wapi_handle_issignalled (handle))) {
			count++;
			
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Handle %p signalled", __func__,
				   handle);
			if(*lowest>i) {
				*lowest=i;
			}
		}
	}
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: %d event handles signalled", __func__, count);

	if ((waitall == TRUE && count == numhandles) ||
	    (waitall == FALSE && count > 0)) {
		ret=TRUE;
	} else {
		ret=FALSE;
	}
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Returning %d", __func__, ret);

	*retcount=count;
	
	return(ret);
}

void _wapi_handle_unlock_handles (guint32 numhandles, gpointer *handles)
{
	guint32 i;
	int thr_ret;
	
	for(i=0; i<numhandles; i++) {
		gpointer handle = handles[i];
		
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: unlocking handle %p", __func__, handle);

		thr_ret = _wapi_handle_unlock_handle (handle);
		g_assert (thr_ret == 0);
	}
}

static void
signal_handle_and_unref (gpointer handle)
{
	WapiHandleBase *handle_data;
	mono_cond_t *cond;
	mono_mutex_t *mutex;
	guint32 idx;

	idx = GPOINTER_TO_UINT (handle);

	handle_data = _WAPI_PRIVATE_HANDLES (idx);
	g_assert (handle_data->type != WAPI_HANDLE_UNUSED);

	/* If we reach here, then interrupt token is set to the flag value, which
	 * means that the target thread is either
	 * - before the first CAS in timedwait, which means it won't enter the wait.
	 * - it is after the first CAS, so it is already waiting, or it will enter
	 *    the wait, and it will be interrupted by the broadcast. */
	cond = &handle_data->signal_cond;
	mutex = &handle_data->signal_mutex;

	mono_os_mutex_lock (mutex);
	mono_os_cond_broadcast (cond);
	mono_os_mutex_unlock (mutex);

	_wapi_handle_unref (handle);
}

int
_wapi_handle_timedwait_signal (guint32 timeout, gboolean poll, gboolean *alerted)
{
	return _wapi_handle_timedwait_signal_handle (_wapi_global_signal_handle, timeout, poll, alerted);
}

int
_wapi_handle_timedwait_signal_handle (gpointer handle, guint32 timeout, gboolean poll, gboolean *alerted)
{
	guint32 idx;
	WapiHandleBase *handle_data;
	int res;
	mono_cond_t *cond;
	mono_mutex_t *mutex;

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: waiting for %p (type %s)", __func__, handle,
		   _wapi_handle_typename[_wapi_handle_type (handle)]);

	if (alerted)
		*alerted = FALSE;

	idx = GPOINTER_TO_UINT(handle);
	handle_data = _WAPI_PRIVATE_HANDLES (idx);

	if (alerted) {
		mono_thread_info_install_interrupt (signal_handle_and_unref, handle, alerted);
		if (*alerted)
			return 0;
		_wapi_handle_ref (handle);
	}

	cond = &handle_data->signal_cond;
	mutex = &handle_data->signal_mutex;

	if (!poll) {
		res = mono_os_cond_timedwait (cond, mutex, timeout);
	} else {
		/* This is needed when waiting for process handles */
		if (!alerted) {
			/*
			 * pthread_cond_(timed)wait() can return 0 even if the condition was not
			 * signalled.  This happens at least on Darwin.  We surface this, i.e., we
			 * get spurious wake-ups.
			 *
			 * http://pubs.opengroup.org/onlinepubs/007908775/xsh/pthread_cond_wait.html
			 */
			res = mono_os_cond_timedwait (cond, mutex, timeout);
		} else {
			if (timeout < 100) {
				/* Real timeout is less than 100ms time */
				res = mono_os_cond_timedwait (cond, mutex, timeout);
			} else {
				res = mono_os_cond_timedwait (cond, mutex, 100);

				/* Mask the fake timeout, this will cause
				 * another poll if the cond was not really signaled
				 */
				if (res == ETIMEDOUT)
					res = 0;
			}
		}
	}

	if (alerted) {
		mono_thread_info_uninstall_interrupt (alerted);
		if (!*alerted) {
			/* if it is alerted, then the handle is unref in the interrupt callback */
			_wapi_handle_unref (handle);
		}
	}

	return res;
}

void _wapi_handle_dump (void)
{
	WapiHandleBase *handle_data;
	guint32 i, k;
	int thr_ret;
	
	thr_ret = mono_os_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);
	
	for(i = SLOT_INDEX (0); i < _wapi_private_handle_slot_count; i++) {
		if (_wapi_private_handles [i]) {
			for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
				handle_data = (WapiHandleBase*) &_wapi_private_handles [i][k];

				if (handle_data->type == WAPI_HANDLE_UNUSED) {
					continue;
				}
		
				g_print ("%3x [%7s] %s %d ",
						 i * _WAPI_HANDLE_INITIAL_COUNT + k,
						 _wapi_handle_typename[handle_data->type],
						 handle_data->signalled?"Sg":"Un",
						 handle_data->ref);
				if (handle_details[handle_data->type])
					handle_details[handle_data->type](&((struct _WapiHandleUnshared*)handle_data)->u);
				g_print ("\n");
			}
		}
	}

	thr_ret = mono_os_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
}

static void _wapi_shared_details (gpointer handle_info)
{
	struct _WapiHandle_shared_ref *shared = (struct _WapiHandle_shared_ref *)handle_info;
	
	g_print ("offset: 0x%x", shared->offset);
}
