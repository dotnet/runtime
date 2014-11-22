/*
 * handles.c:  Generic and internal operations on handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2011 Novell, Inc.
 * Copyright 2011 Xamarin Inc
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <errno.h>
#include <unistd.h>
#include <signal.h>
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
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/collection.h>
#include <mono/io-layer/process-private.h>

#include <mono/utils/mono-mutex.h>
#include <mono/utils/mono-proclib.h>
#undef DEBUG_REFS

#if 0
#define DEBUG(...) g_message(__VA_ARGS__)
#else
#define DEBUG(...)
#endif

static void (*_wapi_handle_ops_get_close_func (WapiHandleType type))(gpointer, gpointer);

static WapiHandleCapability handle_caps[WAPI_HANDLE_COUNT]={0};
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

struct _WapiHandleSharedLayout *_wapi_shared_layout = NULL;

/*
 * If SHM is enabled, this will point to shared memory, otherwise it will be NULL.
 */
struct _WapiFileShareLayout *_wapi_fileshare_layout = NULL;

/*
 * If SHM is disabled, this will point to a hash of _WapiFileShare structures, otherwise
 * it will be NULL. We use this instead of _wapi_fileshare_layout to avoid allocating a
 * 4MB array.
 */
static GHashTable *file_share_hash;
static mono_mutex_t file_share_hash_mutex;

#define file_share_hash_lock() mono_mutex_lock (&file_share_hash_mutex)
#define file_share_hash_unlock() mono_mutex_unlock (&file_share_hash_mutex)

guint32 _wapi_fd_reserve;

/* 
 * This is an internal handle which is used for handling waiting for multiple handles.
 * Threads which wait for multiple handles wait on this one handle, and when a handle
 * is signalled, this handle is signalled too.
 */
static gpointer _wapi_global_signal_handle;

/* Point to the mutex/cond inside _wapi_global_signal_handle */
mono_mutex_t *_wapi_global_signal_mutex;
pthread_cond_t *_wapi_global_signal_cond;

int _wapi_sem_id;
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
			struct _WapiHandleUnshared *handle_data = &_wapi_private_handles[i][j];
			gpointer handle = GINT_TO_POINTER (i*_WAPI_HANDLE_INITIAL_COUNT+j);

			for(k = handle_data->ref; k > 0; k--) {
				DEBUG ("%s: unreffing %s handle %p", __func__, _wapi_handle_typename[type], handle);
					
				_wapi_handle_unref_full (handle, TRUE);
			}
		}
	}
	
	_wapi_shm_semaphores_remove ();

	_wapi_shm_detach (WAPI_SHM_DATA);
	_wapi_shm_detach (WAPI_SHM_FILESHARE);

	if (file_share_hash) {
		g_hash_table_destroy (file_share_hash);
		mono_mutex_destroy (&file_share_hash_mutex);
	}

	for (i = 0; i < _WAPI_PRIVATE_MAX_SLOTS; ++i)
		g_free (_wapi_private_handles [i]);
}

int
wapi_getdtablesize (void)
{
#ifdef HAVE_GETRLIMIT
	struct rlimit limit;
	int res;

	res = getrlimit (RLIMIT_NOFILE, &limit);
	g_assert (res == 0);
	return limit.rlim_cur;
#else
	return getdtablesize ();
#endif
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
	
	_wapi_shared_layout = _wapi_shm_attach (WAPI_SHM_DATA);
	g_assert (_wapi_shared_layout != NULL);
	
	if (_wapi_shm_enabled ()) {
		/* This allocates a 4mb array, so do it only if SHM is enabled */
		_wapi_fileshare_layout = _wapi_shm_attach (WAPI_SHM_FILESHARE);
		g_assert (_wapi_fileshare_layout != NULL);
	}
	
#if !defined (DISABLE_SHARED_HANDLES)
	if (_wapi_shm_enabled ())
		_wapi_collection_init ();
#endif
	_wapi_io_init ();
	mono_mutex_init (&scan_mutex);

	_wapi_global_signal_handle = _wapi_handle_new (WAPI_HANDLE_EVENT, NULL);

	_wapi_global_signal_cond = &_WAPI_PRIVATE_HANDLES (GPOINTER_TO_UINT (_wapi_global_signal_handle)).signal_cond;
	_wapi_global_signal_mutex = &_WAPI_PRIVATE_HANDLES (GPOINTER_TO_UINT (_wapi_global_signal_handle)).signal_mutex;

	wapi_processes_init ();

	/* Using atexit here instead of an explicit function call in
	 * a cleanup routine lets us cope when a third-party library
	 * calls exit (eg if an X client loses the connection to its
	 * server.)
	 */
	mono_atexit (handle_cleanup);
}

void
wapi_cleanup (void)
{
	g_assert (_wapi_has_shut_down == FALSE);
	
	_wapi_has_shut_down = TRUE;

	_wapi_error_cleanup ();
	_wapi_thread_cleanup ();
	wapi_processes_cleanup ();
}

static void _wapi_handle_init_shared (struct _WapiHandleShared *handle,
				      WapiHandleType type,
				      gpointer handle_specific)
{
	g_assert (_wapi_has_shut_down == FALSE);
	
	handle->type = type;
	handle->timestamp = (guint32)(time (NULL) & 0xFFFFFFFF);
	handle->signalled = FALSE;
	handle->handle_refs = 1;
	
	if (handle_specific != NULL) {
		memcpy (&handle->u, handle_specific, sizeof (handle->u));
	}
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

static void _wapi_handle_init (struct _WapiHandleUnshared *handle,
			       WapiHandleType type, gpointer handle_specific)
{
	int thr_ret;
	int type_size;
	
	g_assert (_wapi_has_shut_down == FALSE);
	
	handle->type = type;
	handle->signalled = FALSE;
	handle->ref = 1;
	
	if (!_WAPI_SHARED_HANDLE(type)) {
		thr_ret = pthread_cond_init (&handle->signal_cond, NULL);
		g_assert (thr_ret == 0);
				
		thr_ret = mono_mutex_init (&handle->signal_mutex);
		g_assert (thr_ret == 0);

		if (handle_specific != NULL) {
			type_size = _wapi_handle_struct_size (type);
			memcpy (&handle->u, handle_specific,
				type_size);
		}
	}
}

static guint32 _wapi_handle_new_shared (WapiHandleType type,
					gpointer handle_specific)
{
	guint32 offset;
	static guint32 last = 1;
	int thr_ret;
	
	g_assert (_wapi_has_shut_down == FALSE);
	
	/* Leave the first slot empty as a guard */
again:
	/* FIXME: expandable array */
	for(offset = last; offset <_WAPI_HANDLE_INITIAL_COUNT; offset++) {
		struct _WapiHandleShared *handle = &_wapi_shared_layout->handles[offset];
		
		if(handle->type == WAPI_HANDLE_UNUSED) {
			thr_ret = _wapi_handle_lock_shared_handles ();
			g_assert (thr_ret == 0);
			
			if (InterlockedCompareExchange ((gint32 *)&handle->type, type, WAPI_HANDLE_UNUSED) == WAPI_HANDLE_UNUSED) {
				last = offset + 1;
			
				_wapi_handle_init_shared (handle, type,
							  handle_specific);

				_wapi_handle_unlock_shared_handles ();
				
				return(offset);
			} else {
				/* Someone else beat us to it, just
				 * continue looking
				 */
			}

			_wapi_handle_unlock_shared_handles ();
		}
	}

	if(last > 1) {
		/* Try again from the beginning */
		last = 1;
		goto again;
	}

	/* Will need to expand the array.  The caller will sort it out */

	return(0);
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
				struct _WapiHandleUnshared *handle = &_wapi_private_handles [i][k];

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
		
	DEBUG ("%s: Creating new handle of type %s", __func__,
		   _wapi_handle_typename[type]);

	g_assert(!_WAPI_FD_HANDLE(type));
	
	thr_ret = mono_mutex_lock (&scan_mutex);
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
	
	thr_ret = mono_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);

	if (handle_idx == 0) {
		/* We ran out of slots */
		handle = _WAPI_HANDLE_INVALID;
		goto done;
	}
		
	/* Make sure we left the space for fd mappings */
	g_assert (handle_idx >= _wapi_fd_reserve);
	
	handle = GUINT_TO_POINTER (handle_idx);

	DEBUG ("%s: Allocated new handle %p", __func__, handle);
	
	if (_WAPI_SHARED_HANDLE(type)) {
		/* Add the shared section too */
		guint32 ref;
		
		ref = _wapi_handle_new_shared (type, handle_specific);
		if (ref == 0) {
			_wapi_handle_collect ();
			ref = _wapi_handle_new_shared (type, handle_specific);
			if (ref == 0) {
				/* FIXME: grow the arrays */
				handle = _WAPI_HANDLE_INVALID;
				goto done;
			}
		}
		
		_WAPI_PRIVATE_HANDLES(handle_idx).u.shared.offset = ref;
		DEBUG ("%s: New shared handle at offset 0x%x", __func__,
			   ref);
	}
		
done:
	return(handle);
}

gpointer _wapi_handle_new_from_offset (WapiHandleType type, guint32 offset,
				       gboolean timestamp)
{
	guint32 handle_idx = 0;
	gpointer handle = INVALID_HANDLE_VALUE;
	int thr_ret, i, k;
	struct _WapiHandleShared *shared;
	
	g_assert (_wapi_has_shut_down == FALSE);
	
	DEBUG ("%s: Creating new handle of type %s to offset %d", __func__,
		   _wapi_handle_typename[type], offset);

	g_assert(!_WAPI_FD_HANDLE(type));
	g_assert(_WAPI_SHARED_HANDLE(type));
	g_assert(offset != 0);

	shared = &_wapi_shared_layout->handles[offset];
	if (timestamp) {
		guint32 now = (guint32)(time (NULL) & 0xFFFFFFFF);
		/* Bump up the timestamp for this offset */
		InterlockedExchange ((gint32 *)&shared->timestamp, now);
	}
		
	thr_ret = mono_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);

	for (i = SLOT_INDEX (0); i < _wapi_private_handle_slot_count; i++) {
		if (_wapi_private_handles [i]) {
			for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
				struct _WapiHandleUnshared *handle_data = &_wapi_private_handles [i][k];
		
				if (handle_data->type == type &&
					handle_data->u.shared.offset == offset) {
					handle = GUINT_TO_POINTER (i * _WAPI_HANDLE_INITIAL_COUNT + k);
					goto first_pass_done;
				}
			}
		}
	}

first_pass_done:
	thr_ret = mono_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);

	if (handle != INVALID_HANDLE_VALUE) {
		_wapi_handle_ref (handle);

		DEBUG ("%s: Returning old handle %p referencing 0x%x",
			   __func__, handle, offset);
		return (handle);
	}

	/* Prevent entries expiring under us as we search */
	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);
	
	if (shared->type == WAPI_HANDLE_UNUSED) {
		/* Someone deleted this handle while we were working */
		DEBUG ("%s: Handle at 0x%x unused", __func__, offset);
		goto done;
	}

	if (shared->type != type) {
		DEBUG ("%s: Wrong type at %d 0x%x! Found %s wanted %s",
			   __func__, offset, offset,
			   _wapi_handle_typename[shared->type],
			   _wapi_handle_typename[type]);
		goto done;
	}
	
	thr_ret = mono_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);
	
	while ((handle_idx = _wapi_handle_new_internal (type, NULL)) == 0) {
		/* Try and expand the array, and have another go */
		int idx = SLOT_INDEX (_wapi_private_handle_count);
		_wapi_private_handles [idx] = g_new0 (struct _WapiHandleUnshared,
						_WAPI_HANDLE_INITIAL_COUNT);

		_wapi_private_handle_count += _WAPI_HANDLE_INITIAL_COUNT;
		_wapi_private_handle_slot_count ++;
	}
		
	thr_ret = mono_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
		
	/* Make sure we left the space for fd mappings */
	g_assert (handle_idx >= _wapi_fd_reserve);
	
	handle = GUINT_TO_POINTER (handle_idx);
		
	_WAPI_PRIVATE_HANDLES(handle_idx).u.shared.offset = offset;
	InterlockedIncrement ((gint32 *)&shared->handle_refs);
	
	DEBUG ("%s: Allocated new handle %p referencing 0x%x (shared refs %d)", __func__, handle, offset, shared->handle_refs);
	
done:
	_wapi_handle_unlock_shared_handles ();

	return(handle);
}

static void
init_handles_slot (int idx)
{
	int thr_ret;

	thr_ret = mono_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);

	if (_wapi_private_handles [idx] == NULL) {
		_wapi_private_handles [idx] = g_new0 (struct _WapiHandleUnshared,
											  _WAPI_HANDLE_INITIAL_COUNT);
		g_assert (_wapi_private_handles [idx]);
	}

	thr_ret = mono_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
}

gpointer _wapi_handle_new_fd (WapiHandleType type, int fd,
			      gpointer handle_specific)
{
	struct _WapiHandleUnshared *handle;
	int thr_ret;
	
	g_assert (_wapi_has_shut_down == FALSE);
	
	DEBUG ("%s: Creating new handle of type %s", __func__,
		   _wapi_handle_typename[type]);
	
	g_assert(_WAPI_FD_HANDLE(type));
	g_assert(!_WAPI_SHARED_HANDLE(type));
	
	if (fd >= _wapi_fd_reserve) {
		DEBUG ("%s: fd %d is too big", __func__, fd);

		return(GUINT_TO_POINTER (_WAPI_HANDLE_INVALID));
	}

	/* Initialize the array entries on demand */
	if (_wapi_private_handles [SLOT_INDEX (fd)] == NULL)
		init_handles_slot (SLOT_INDEX (fd));

	handle = &_WAPI_PRIVATE_HANDLES(fd);
	
	if (handle->type != WAPI_HANDLE_UNUSED) {
		DEBUG ("%s: fd %d is already in use!", __func__, fd);
		/* FIXME: clean up this handle?  We can't do anything
		 * with the fd, cos thats the new one
		 */
	}

	DEBUG ("%s: Assigning new fd handle %d", __func__, fd);

	/* Prevent file share entries racing with us, when the file
	 * handle is only half initialised
	 */
	thr_ret = _wapi_shm_sem_lock (_WAPI_SHARED_SEM_FILESHARE);
	g_assert(thr_ret == 0);

	_wapi_handle_init (handle, type, handle_specific);

	thr_ret = _wapi_shm_sem_unlock (_WAPI_SHARED_SEM_FILESHARE);

	return(GUINT_TO_POINTER(fd));
}

gboolean _wapi_lookup_handle (gpointer handle, WapiHandleType type,
			      gpointer *handle_specific)
{
	struct _WapiHandleUnshared *handle_data;
	guint32 handle_idx = GPOINTER_TO_UINT(handle);

	if (!_WAPI_PRIVATE_VALID_SLOT (handle_idx)) {
		return(FALSE);
	}

	/* Initialize the array entries on demand */
	if (_wapi_private_handles [SLOT_INDEX (handle_idx)] == NULL)
		init_handles_slot (SLOT_INDEX (handle_idx));
	
	handle_data = &_WAPI_PRIVATE_HANDLES(handle_idx);
	
	if (handle_data->type != type) {
		return(FALSE);
	}

	if (handle_specific == NULL) {
		return(FALSE);
	}
	
	if (_WAPI_SHARED_HANDLE(type)) {
		struct _WapiHandle_shared_ref *ref;
		struct _WapiHandleShared *shared_handle_data;
			
		ref = &handle_data->u.shared;
		shared_handle_data = &_wapi_shared_layout->handles[ref->offset];
		
		if (shared_handle_data->type != type) {
			/* The handle must have been deleted on us
			 */
			return (FALSE);
		}
		
		*handle_specific = &shared_handle_data->u;
	} else {
		*handle_specific = &handle_data->u;
	}
	
	return(TRUE);
}

void
_wapi_handle_foreach (WapiHandleType type,
			gboolean (*on_each)(gpointer test, gpointer user),
			gpointer user_data)
{
	struct _WapiHandleUnshared *handle_data = NULL;
	gpointer ret = NULL;
	guint32 i, k;
	int thr_ret;

	thr_ret = mono_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);

	for (i = SLOT_INDEX (0); i < _wapi_private_handle_slot_count; i++) {
		if (_wapi_private_handles [i]) {
			for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
				handle_data = &_wapi_private_handles [i][k];
			
				if (handle_data->type == type) {
					ret = GUINT_TO_POINTER (i * _WAPI_HANDLE_INITIAL_COUNT + k);
					if (on_each (ret, user_data) == TRUE)
						break;
				}
			}
		}
	}

	thr_ret = mono_mutex_unlock (&scan_mutex);
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
	struct _WapiHandleUnshared *handle_data = NULL;
	struct _WapiHandleShared *shared = NULL;
	gpointer ret = NULL;
	guint32 i, k;
	gboolean found = FALSE;
	int thr_ret;

	thr_ret = mono_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);
	
	for (i = SLOT_INDEX (0); !found && i < _wapi_private_handle_slot_count; i++) {
		if (_wapi_private_handles [i]) {
			for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
				handle_data = &_wapi_private_handles [i][k];
		
				if (handle_data->type == type) {
					ret = GUINT_TO_POINTER (i * _WAPI_HANDLE_INITIAL_COUNT + k);
					if (check (ret, user_data) == TRUE) {
						_wapi_handle_ref (ret);
						found = TRUE;

						if (_WAPI_SHARED_HANDLE (type)) {
							shared = &_wapi_shared_layout->handles[i];
						}
					
						break;
					}
				}
			}
		}
	}

	thr_ret = mono_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);

	if (!found && search_shared && _WAPI_SHARED_HANDLE (type)) {
		/* Not found yet, so search the shared memory too */
		DEBUG ("%s: Looking at other shared handles...", __func__);

		for (i = 0; i < _WAPI_HANDLE_INITIAL_COUNT; i++) {
			shared = &_wapi_shared_layout->handles[i];
			
			if (shared->type == type) {
				/* Tell new_from_offset to not
				 * timestamp this handle, because
				 * otherwise it will ping every handle
				 * in the list and they will never
				 * expire
				 */
				ret = _wapi_handle_new_from_offset (type, i,
								    FALSE);
				if (ret == INVALID_HANDLE_VALUE) {
					/* This handle was deleted
					 * while we were looking at it
					 */
					continue;
				}
				
				DEBUG ("%s: Opened tmp handle %p (type %s) from offset %d", __func__, ret, _wapi_handle_typename[type], i);

				/* It's possible that the shared part
				 * of this handle has now been blown
				 * away (after new_from_offset
				 * successfully opened it,) if its
				 * timestamp is too old.  The check
				 * function needs to be aware of this,
				 * and cope if the handle has
				 * vanished.
				 */
				if (check (ret, user_data) == TRUE) {
					/* Timestamp this handle, but make
					 * sure it still exists first
					 */
					thr_ret = _wapi_handle_lock_shared_handles ();
					g_assert (thr_ret == 0);
					
					if (shared->type == type) {
						guint32 now = (guint32)(time (NULL) & 0xFFFFFFFF);
						InterlockedExchange ((gint32 *)&shared->timestamp, now);

						found = TRUE;
						handle_data = &_WAPI_PRIVATE_HANDLES(GPOINTER_TO_UINT(ret));
					
						_wapi_handle_unlock_shared_handles ();
						break;
					} else {
						/* It's been deleted,
						 * so just keep
						 * looking
						 */
						_wapi_handle_unlock_shared_handles ();
					}
				}
				
				/* This isn't the handle we're looking
				 * for, so drop the reference we took
				 * in _wapi_handle_new_from_offset ()
				 */
				_wapi_handle_unref (ret);
			}
		}
	}
	
	if (!found) {
		ret = NULL;
		goto done;
	}
	
	if(handle_specific != NULL) {
		if (_WAPI_SHARED_HANDLE(type)) {
			g_assert(shared->type == type);
			
			*handle_specific = &shared->u;
		} else {
			*handle_specific = &handle_data->u;
		}
	}

done:
	return(ret);
}

/* Returns the offset of the metadata array, or -1 on error, or 0 for
 * not found (0 is not a valid offset)
 */
gint32 _wapi_search_handle_namespace (WapiHandleType type,
				      gchar *utf8_name)
{
	struct _WapiHandleShared *shared_handle_data;
	guint32 i;
	gint32 ret = 0;
	int thr_ret;
	
	g_assert(_WAPI_SHARED_HANDLE(type));
	
	DEBUG ("%s: Lookup for handle named [%s] type %s", __func__,
		   utf8_name, _wapi_handle_typename[type]);

	/* Do a handle collection before starting to look, so that any
	 * stale cruft gets removed
	 */
	_wapi_handle_collect ();
	
	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);
	
	for(i = 1; i < _WAPI_HANDLE_INITIAL_COUNT; i++) {
		WapiSharedNamespace *sharedns;
		
		shared_handle_data = &_wapi_shared_layout->handles[i];

		/* Check mutex, event, semaphore, timer, job and
		 * file-mapping object names.  So far only mutex,
		 * semaphore and event are implemented.
		 */
		if (!_WAPI_SHARED_NAMESPACE (shared_handle_data->type)) {
			continue;
		}

		DEBUG ("%s: found a shared namespace handle at 0x%x (type %s)", __func__, i, _wapi_handle_typename[shared_handle_data->type]);

		sharedns=(WapiSharedNamespace *)&shared_handle_data->u;
			
		DEBUG ("%s: name is [%s]", __func__, sharedns->name);

		if (strcmp (sharedns->name, utf8_name) == 0) {
			if (shared_handle_data->type != type) {
				/* Its the wrong type, so fail now */
				DEBUG ("%s: handle 0x%x matches name but is wrong type: %s", __func__, i, _wapi_handle_typename[shared_handle_data->type]);
				ret = -1;
				goto done;
			} else {
				DEBUG ("%s: handle 0x%x matches name and type", __func__, i);
				ret = i;
				goto done;
			}
		}
	}

done:
	_wapi_handle_unlock_shared_handles ();
	
	return(ret);
}

void _wapi_handle_ref (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	struct _WapiHandleUnshared *handle_data;

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return;
	}
	
	if (_wapi_handle_type (handle) == WAPI_HANDLE_UNUSED) {
		g_warning ("%s: Attempting to ref unused handle %p", __func__,
			   handle);
		return;
	}

	handle_data = &_WAPI_PRIVATE_HANDLES(idx);
	
	InterlockedIncrement ((gint32 *)&handle_data->ref);

	/* It's possible for processes to exit before getting around
	 * to updating timestamps in the collection thread, so if a
	 * shared handle is reffed do the timestamp here as well just
	 * to make sure.
	 */
	if (_WAPI_SHARED_HANDLE(handle_data->type)) {
		struct _WapiHandleShared *shared_data = &_wapi_shared_layout->handles[handle_data->u.shared.offset];
		guint32 now = (guint32)(time (NULL) & 0xFFFFFFFF);
		InterlockedExchange ((gint32 *)&shared_data->timestamp, now);
	}
	
#ifdef DEBUG_REFS
	g_message ("%s: %s handle %p ref now %d", __func__, 
		   _wapi_handle_typename[_WAPI_PRIVATE_HANDLES (idx).type],
		   handle,
		   _WAPI_PRIVATE_HANDLES(idx).ref);
#endif
}

/* The handle must not be locked on entry to this function */
static void _wapi_handle_unref_full (gpointer handle, gboolean ignore_private_busy_handles)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	gboolean destroy = FALSE, early_exit = FALSE;
	int thr_ret;

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return;
	}
	
	if (_wapi_handle_type (handle) == WAPI_HANDLE_UNUSED) {
		g_warning ("%s: Attempting to unref unused handle %p",
			   __func__, handle);
		return;
	}

	/* Possible race condition here if another thread refs the
	 * handle between here and setting the type to UNUSED.  I
	 * could lock a mutex, but I'm not sure that allowing a handle
	 * reference to reach 0 isn't an application bug anyway.
	 */
	destroy = (InterlockedDecrement ((gint32 *)&_WAPI_PRIVATE_HANDLES(idx).ref) ==0);
	
#ifdef DEBUG_REFS
	g_message ("%s: %s handle %p ref now %d (destroy %s)", __func__,
		   _wapi_handle_typename[_WAPI_PRIVATE_HANDLES (idx).type],
		   handle,
		   _WAPI_PRIVATE_HANDLES(idx).ref, destroy?"TRUE":"FALSE");
#endif
	
	if(destroy==TRUE) {
		/* Need to copy the handle info, reset the slot in the
		 * array, and _only then_ call the close function to
		 * avoid race conditions (eg file descriptors being
		 * closed, and another file being opened getting the
		 * same fd racing the memset())
		 */
		struct _WapiHandleUnshared handle_data;
		struct _WapiHandleShared shared_handle_data;
		WapiHandleType type = _WAPI_PRIVATE_HANDLES(idx).type;
		void (*close_func)(gpointer, gpointer) = _wapi_handle_ops_get_close_func (type);
		gboolean is_shared = _WAPI_SHARED_HANDLE(type);

		if (is_shared) {
			/* If this is a shared handle we need to take
			 * the shared lock outside of the scan_mutex
			 * lock to avoid deadlocks
			 */
			thr_ret = _wapi_handle_lock_shared_handles ();
			g_assert (thr_ret == 0);
		}
		
		thr_ret = mono_mutex_lock (&scan_mutex);

		DEBUG ("%s: Destroying handle %p", __func__, handle);
		
		memcpy (&handle_data, &_WAPI_PRIVATE_HANDLES(idx),
			sizeof (struct _WapiHandleUnshared));

		memset (&_WAPI_PRIVATE_HANDLES(idx).u, '\0',
			sizeof(_WAPI_PRIVATE_HANDLES(idx).u));

		_WAPI_PRIVATE_HANDLES(idx).type = WAPI_HANDLE_UNUSED;
		
		if (!is_shared) {
			/* Destroy the mutex and cond var.  We hope nobody
			 * tried to grab them between the handle unlock and
			 * now, but pthreads doesn't have a
			 * "unlock_and_destroy" atomic function.
			 */
			thr_ret = mono_mutex_destroy (&_WAPI_PRIVATE_HANDLES(idx).signal_mutex);
			/*WARNING gross hack to make cleanup not crash when exiting without the whole runtime teardown.*/
			if (thr_ret == EBUSY && ignore_private_busy_handles) {
				early_exit = TRUE;
			} else {
				if (thr_ret != 0)
					g_error ("Error destroying handle %p mutex due to %d\n", handle, thr_ret);

				thr_ret = pthread_cond_destroy (&_WAPI_PRIVATE_HANDLES(idx).signal_cond);
				if (thr_ret == EBUSY && ignore_private_busy_handles)
					early_exit = TRUE;
				else if (thr_ret != 0)
					g_error ("Error destroying handle %p cond var due to %d\n", handle, thr_ret);
			}
		} else {
			struct _WapiHandleShared *shared = &_wapi_shared_layout->handles[handle_data.u.shared.offset];

			memcpy (&shared_handle_data, shared,
				sizeof (struct _WapiHandleShared));
			
			/* It's possible that this handle is already
			 * pointing at a deleted shared section
			 */
#ifdef DEBUG_REFS
			g_message ("%s: %s handle %p shared refs before dec %d", __func__, _wapi_handle_typename[type], handle, shared->handle_refs);
#endif

			if (shared->handle_refs > 0) {
				shared->handle_refs--;
				if (shared->handle_refs == 0) {
					memset (shared, '\0', sizeof (struct _WapiHandleShared));
				}
			}
		}

		thr_ret = mono_mutex_unlock (&scan_mutex);
		g_assert (thr_ret == 0);

		if (early_exit)
			return;
		if (is_shared) {
			_wapi_handle_unlock_shared_handles ();
		}
		
		if (close_func != NULL) {
			if (is_shared) {
				close_func (handle, &shared_handle_data.u);
			} else {
				close_func (handle, &handle_data.u);
			}
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
	
	type = _WAPI_PRIVATE_HANDLES(idx).type;

	DEBUG ("%s: testing 0x%x against 0x%x (%d)", __func__,
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
	
	type = _WAPI_PRIVATE_HANDLES(idx).type;

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
	
	type = _WAPI_PRIVATE_HANDLES(idx).type;

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
	
	type = _WAPI_PRIVATE_HANDLES(idx).type;

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
	
	type = _WAPI_PRIVATE_HANDLES(idx).type;

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
	
	type = _WAPI_PRIVATE_HANDLES(idx).type;
	
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
	
	type = _WAPI_PRIVATE_HANDLES (idx).type;
	
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
		if (_WAPI_PRIVATE_HANDLES (0).type != WAPI_HANDLE_CONSOLE) {
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
	WapiHandleType type;
	
	/* Lock all the handles, with backoff */
again:
	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);
	
	for(i=0; i<numhandles; i++) {
		gpointer handle = handles[i];
		guint32 idx = GPOINTER_TO_UINT(handle);

		DEBUG ("%s: attempting to lock %p", __func__, handle);

		type = _WAPI_PRIVATE_HANDLES(idx).type;

		thr_ret = _wapi_handle_trylock_handle (handle);
		
		if (thr_ret != 0) {
			/* Bummer */
			
			DEBUG ("%s: attempt failed for %p: %s", __func__,
				   handle, strerror (thr_ret));

			thr_ret = _wapi_handle_unlock_shared_handles ();
			g_assert (thr_ret == 0);
			
			while (i--) {
				handle = handles[i];
				idx = GPOINTER_TO_UINT(handle);

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
			
			DEBUG ("%s: Backing off for %d ms", __func__,
				   iter*10);
			_wapi_handle_spin (10 * iter);
			
			goto again;
		}
	}
	
	DEBUG ("%s: Locked all handles", __func__);

	count=0;
	*lowest=numhandles;
	
	for(i=0; i<numhandles; i++) {
		gpointer handle = handles[i];
		guint32 idx = GPOINTER_TO_UINT(handle);
		
		type = _WAPI_PRIVATE_HANDLES(idx).type;

		DEBUG ("%s: Checking handle %p", __func__, handle);

		if(((_wapi_handle_test_capabilities (handle, WAPI_HANDLE_CAP_OWN)==TRUE) &&
		    (_wapi_handle_ops_isowned (handle) == TRUE)) ||
		   (_WAPI_SHARED_HANDLE(type) &&
		    WAPI_SHARED_HANDLE_DATA(handle).signalled == TRUE) ||
		   (!_WAPI_SHARED_HANDLE(type) &&
		    _WAPI_PRIVATE_HANDLES(idx).signalled == TRUE)) {
			count++;
			
			DEBUG ("%s: Handle %p signalled", __func__,
				   handle);
			if(*lowest>i) {
				*lowest=i;
			}
		}
	}
	
	DEBUG ("%s: %d event handles signalled", __func__, count);

	if ((waitall == TRUE && count == numhandles) ||
	    (waitall == FALSE && count > 0)) {
		ret=TRUE;
	} else {
		ret=FALSE;
	}
	
	DEBUG ("%s: Returning %d", __func__, ret);

	*retcount=count;
	
	return(ret);
}

void _wapi_handle_unlock_handles (guint32 numhandles, gpointer *handles)
{
	guint32 i;
	int thr_ret;
	
	thr_ret = _wapi_handle_unlock_shared_handles ();
	g_assert (thr_ret == 0);
	
	for(i=0; i<numhandles; i++) {
		gpointer handle = handles[i];
		
		DEBUG ("%s: unlocking handle %p", __func__, handle);

		thr_ret = _wapi_handle_unlock_handle (handle);
		g_assert (thr_ret == 0);
	}
}

static int timedwait_signal_poll_cond (pthread_cond_t *cond, mono_mutex_t *mutex, struct timespec *timeout, gboolean alertable)
{
	struct timespec fake_timeout;
	int ret;

	if (!alertable) {
		if (timeout)
			ret=mono_cond_timedwait (cond, mutex, timeout);
		else
			ret=mono_cond_wait (cond, mutex);
	} else {
		_wapi_calc_timeout (&fake_timeout, 100);
	
		if (timeout != NULL && ((fake_timeout.tv_sec > timeout->tv_sec) ||
								(fake_timeout.tv_sec == timeout->tv_sec &&
								 fake_timeout.tv_nsec > timeout->tv_nsec))) {
			/* Real timeout is less than 100ms time */
			ret=mono_cond_timedwait (cond, mutex, timeout);
		} else {
			ret=mono_cond_timedwait (cond, mutex, &fake_timeout);

			/* Mask the fake timeout, this will cause
			 * another poll if the cond was not really signaled
			 */
			if (ret==ETIMEDOUT) {
				ret=0;
			}
		}
	}
	
	return(ret);
}

int _wapi_handle_wait_signal (gboolean poll)
{
	return _wapi_handle_timedwait_signal_handle (_wapi_global_signal_handle, NULL, TRUE, poll);
}

int _wapi_handle_timedwait_signal (struct timespec *timeout, gboolean poll)
{
	return _wapi_handle_timedwait_signal_handle (_wapi_global_signal_handle, timeout, TRUE, poll);
}

int _wapi_handle_wait_signal_handle (gpointer handle, gboolean alertable)
{
	DEBUG ("%s: waiting for %p", __func__, handle);
	
	return _wapi_handle_timedwait_signal_handle (handle, NULL, alertable, FALSE);
}

int _wapi_handle_timedwait_signal_handle (gpointer handle,
										  struct timespec *timeout, gboolean alertable, gboolean poll)
{
	DEBUG ("%s: waiting for %p (type %s)", __func__, handle,
		   _wapi_handle_typename[_wapi_handle_type (handle)]);
	
	if (_WAPI_SHARED_HANDLE (_wapi_handle_type (handle))) {
		if (WAPI_SHARED_HANDLE_DATA(handle).signalled == TRUE) {
			return (0);
		}
		if (timeout != NULL) {
			struct timespec fake_timeout;
			_wapi_calc_timeout (&fake_timeout, 100);
		
			if ((fake_timeout.tv_sec > timeout->tv_sec) ||
				(fake_timeout.tv_sec == timeout->tv_sec &&
				 fake_timeout.tv_nsec > timeout->tv_nsec)) {
				/* FIXME: Real timeout is less than
				 * 100ms time, but is it really worth
				 * calculating to the exact ms?
				 */
				_wapi_handle_spin (100);

				if (WAPI_SHARED_HANDLE_DATA(handle).signalled == TRUE) {
					return (0);
				} else {
					return (ETIMEDOUT);
				}
			}
		}
		_wapi_handle_spin (100);
		return (0);
		
	} else {
		guint32 idx = GPOINTER_TO_UINT(handle);
		int res;
		pthread_cond_t *cond;
		mono_mutex_t *mutex;

		if (alertable && !wapi_thread_set_wait_handle (handle))
			return 0;

		cond = &_WAPI_PRIVATE_HANDLES (idx).signal_cond;
		mutex = &_WAPI_PRIVATE_HANDLES (idx).signal_mutex;

		if (poll) {
			/* This is needed when waiting for process handles */
			res = timedwait_signal_poll_cond (cond, mutex, timeout, alertable);
		} else {
			if (timeout)
				res = mono_cond_timedwait (cond, mutex, timeout);
			else
				res = mono_cond_wait (cond, mutex);
		}

		if (alertable)
			wapi_thread_clear_wait_handle (handle);

		return res;
	}
}

void
_wapi_free_share_info (_WapiFileShare *share_info)
{
	if (!_wapi_shm_enabled ()) {
		file_share_hash_lock ();
		g_hash_table_remove (file_share_hash, share_info);
		file_share_hash_unlock ();
		/* The hashtable dtor frees share_info */
	} else {
		memset (share_info, '\0', sizeof(struct _WapiFileShare));
	}
}

static gint
wapi_share_info_equal (gconstpointer ka, gconstpointer kb)
{
	const _WapiFileShare *s1 = ka;
	const _WapiFileShare *s2 = kb;

	return (s1->device == s2->device && s1->inode == s2->inode) ? 1 : 0;
}

static guint
wapi_share_info_hash (gconstpointer data)
{
	const _WapiFileShare *s = data;

	return s->inode;
}

gboolean _wapi_handle_get_or_set_share (dev_t device, ino_t inode,
					guint32 new_sharemode,
					guint32 new_access,
					guint32 *old_sharemode,
					guint32 *old_access,
					struct _WapiFileShare **share_info)
{
	struct _WapiFileShare *file_share;
	guint32 now = (guint32)(time(NULL) & 0xFFFFFFFF);
	int thr_ret, i, first_unused = -1;
	gboolean exists = FALSE;

	/* Prevents entries from expiring under us as we search
	 */
	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);
	
	/* Prevent new entries racing with us */
	thr_ret = _wapi_shm_sem_lock (_WAPI_SHARED_SEM_FILESHARE);
	g_assert (thr_ret == 0);

	if (!_wapi_shm_enabled ()) {
		_WapiFileShare tmp;

		/*
		 * Instead of allocating a 4MB array, we use a hash table to keep track of this
		 * info. This is needed even if SHM is disabled, to track sharing inside
		 * the current process.
		 */
		if (!file_share_hash) {
			file_share_hash = g_hash_table_new_full (wapi_share_info_hash, wapi_share_info_equal, NULL, g_free);
			mono_mutex_init_recursive (&file_share_hash_mutex);
		}
			
		tmp.device = device;
		tmp.inode = inode;

		file_share_hash_lock ();

		file_share = g_hash_table_lookup (file_share_hash, &tmp);
		if (file_share) {
			*old_sharemode = file_share->sharemode;
			*old_access = file_share->access;
			*share_info = file_share;
			
			InterlockedIncrement ((gint32 *)&file_share->handle_refs);
			exists = TRUE;
		} else {
			file_share = g_new0 (_WapiFileShare, 1);

			file_share->device = device;
			file_share->inode = inode;
			file_share->opened_by_pid = _wapi_getpid ();
			file_share->sharemode = new_sharemode;
			file_share->access = new_access;
			file_share->handle_refs = 1;
			*share_info = file_share;

			g_hash_table_insert (file_share_hash, file_share, file_share);
		}

		file_share_hash_unlock ();
	} else {
		/* If a linear scan gets too slow we'll have to fit a hash
		 * table onto the shared mem backing store
		 */
		*share_info = NULL;
		for (i = 0; i <= _wapi_fileshare_layout->hwm; i++) {
			file_share = &_wapi_fileshare_layout->share_info[i];

			/* Make a note of an unused slot, in case we need to
			 * store share info
			 */
			if (first_unused == -1 && file_share->handle_refs == 0) {
				first_unused = i;
				continue;
			}
		
			if (file_share->handle_refs == 0) {
				continue;
			}
		
			if (file_share->device == device &&
				file_share->inode == inode) {
				*old_sharemode = file_share->sharemode;
				*old_access = file_share->access;
				*share_info = file_share;
			
				/* Increment the reference count while we
				 * still have sole access to the shared area.
				 * This makes the increment atomic wrt
				 * collections
				 */
				InterlockedIncrement ((gint32 *)&file_share->handle_refs);
			
				exists = TRUE;
				break;
			}
		}
	
		if (!exists) {
			if (i == _WAPI_FILESHARE_SIZE && first_unused == -1) {
				/* No more space */
			} else {
				if (first_unused == -1) {
					file_share = &_wapi_fileshare_layout->share_info[++i];
					_wapi_fileshare_layout->hwm = i;
				} else {
					file_share = &_wapi_fileshare_layout->share_info[first_unused];
				}
			
				file_share->device = device;
				file_share->inode = inode;
				file_share->opened_by_pid = _wapi_getpid ();
				file_share->sharemode = new_sharemode;
				file_share->access = new_access;
				file_share->handle_refs = 1;
				*share_info = file_share;
			}
		}

		if (*share_info != NULL) {
			InterlockedExchange ((gint32 *)&(*share_info)->timestamp, now);
		}
	}
	
	thr_ret = _wapi_shm_sem_unlock (_WAPI_SHARED_SEM_FILESHARE);

	_wapi_handle_unlock_shared_handles ();

	return(exists);
}

/* If we don't have the info in /proc, check if the process that
 * opened this share info is still there (it's not a perfect method,
 * due to pid reuse)
 */
static void _wapi_handle_check_share_by_pid (struct _WapiFileShare *share_info)
{
#if defined(__native_client__)
	g_assert_not_reached ();
#else
	if (kill (share_info->opened_by_pid, 0) == -1 &&
	    (errno == ESRCH ||
	     errno == EPERM)) {
		/* It's gone completely (or there's a new process
		 * owned by someone else) so mark this share info as
		 * dead
		 */
		DEBUG ("%s: Didn't find it, destroying entry", __func__);

		_wapi_free_share_info (share_info);
	}
#endif
}

#ifdef __linux__
/* Scan /proc/<pids>/fd/ for open file descriptors to the file in
 * question.  If there are none, reset the share info.
 *
 * This implementation is Linux-specific; legacy systems will have to
 * implement their own ways of finding out if a particular file is
 * open by a process.
 */
void _wapi_handle_check_share (struct _WapiFileShare *share_info, int fd)
{
	gboolean found = FALSE, proc_fds = FALSE;
	pid_t self = _wapi_getpid ();
	int pid;
	int thr_ret, i;
	
	/* Prevents entries from expiring under us if we remove this
	 * one
	 */
	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);
	
	/* Prevent new entries racing with us */
	thr_ret = _wapi_shm_sem_lock (_WAPI_SHARED_SEM_FILESHARE);
	g_assert (thr_ret == 0);
	
	/* If there is no /proc, there's nothing more we can do here */
	if (access ("/proc", F_OK) == -1) {
		_wapi_handle_check_share_by_pid (share_info);
		goto done;
	}

	/* If there's another handle that thinks it owns this fd, then even
	 * if the fd has been closed behind our back consider it still owned.
	 * See bugs 75764 and 75891
	 */
	for (i = 0; i < _wapi_fd_reserve; i++) {
		if (_wapi_private_handles [SLOT_INDEX (i)]) {
			struct _WapiHandleUnshared *handle = &_WAPI_PRIVATE_HANDLES(i);

			if (i != fd &&
				handle->type == WAPI_HANDLE_FILE) {
				struct _WapiHandle_file *file_handle = &handle->u.file;

				if (file_handle->share_info == share_info) {
					DEBUG ("%s: handle 0x%x has this file open!",
							   __func__, i);

					goto done;
				}
			}
		}
	}

	if (proc_fds == FALSE) {
		_wapi_handle_check_share_by_pid (share_info);
	} else if (found == FALSE) {
		/* Blank out this entry, as it is stale */
		DEBUG ("%s: Didn't find it, destroying entry", __func__);

		_wapi_free_share_info (share_info);
	}

done:
	thr_ret = _wapi_shm_sem_unlock (_WAPI_SHARED_SEM_FILESHARE);

	_wapi_handle_unlock_shared_handles ();
}
#else
//
// Other implementations (non-Linux)
//
void _wapi_handle_check_share (struct _WapiFileShare *share_info, int fd)
{
	int thr_ret;
	
	/* Prevents entries from expiring under us if we remove this
	 * one */
	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);
	
	/* Prevent new entries racing with us */
	thr_ret = _wapi_shm_sem_lock (_WAPI_SHARED_SEM_FILESHARE);
	g_assert (thr_ret == 0);
	
	_wapi_handle_check_share_by_pid (share_info);

	thr_ret = _wapi_shm_sem_unlock (_WAPI_SHARED_SEM_FILESHARE);
	_wapi_handle_unlock_shared_handles ();
}
#endif

void _wapi_handle_dump (void)
{
	struct _WapiHandleUnshared *handle_data;
	guint32 i, k;
	int thr_ret;
	
	thr_ret = mono_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);
	
	for(i = SLOT_INDEX (0); i < _wapi_private_handle_slot_count; i++) {
		if (_wapi_private_handles [i]) {
			for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
				handle_data = &_wapi_private_handles [i][k];

				if (handle_data->type == WAPI_HANDLE_UNUSED) {
					continue;
				}
		
				g_print ("%3x [%7s] %s %d ",
						 i * _WAPI_HANDLE_INITIAL_COUNT + k,
						 _wapi_handle_typename[handle_data->type],
						 handle_data->signalled?"Sg":"Un",
						 handle_data->ref);
				handle_details[handle_data->type](&handle_data->u);
				g_print ("\n");
			}
		}
	}

	thr_ret = mono_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
}

static void _wapi_shared_details (gpointer handle_info)
{
	struct _WapiHandle_shared_ref *shared = (struct _WapiHandle_shared_ref *)handle_info;
	
	g_print ("offset: 0x%x", shared->offset);
}

void _wapi_handle_update_refs (void)
{
	guint32 i, k;
	int thr_ret;
	guint32 now = (guint32)(time (NULL) & 0xFFFFFFFF);
	
	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);

	/* Prevent file share entries racing with us */
	thr_ret = _wapi_shm_sem_lock (_WAPI_SHARED_SEM_FILESHARE);
	g_assert(thr_ret == 0);

	thr_ret = mono_mutex_lock (&scan_mutex);
	
	for(i = SLOT_INDEX (0); i < _wapi_private_handle_slot_count; i++) {
		if (_wapi_private_handles [i]) {
			for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
				struct _WapiHandleUnshared *handle = &_wapi_private_handles [i][k];

				if (_WAPI_SHARED_HANDLE(handle->type)) {
					struct _WapiHandleShared *shared_data;
				
					DEBUG ("%s: (%d) handle 0x%x is SHARED (%s)", __func__, _wapi_getpid (), i * _WAPI_HANDLE_INITIAL_COUNT + k, _wapi_handle_typename[handle->type]);

					shared_data = &_wapi_shared_layout->handles[handle->u.shared.offset];

					DEBUG ("%s: (%d) Updating timestamp of handle 0x%x", __func__, _wapi_getpid (), handle->u.shared.offset);

					InterlockedExchange ((gint32 *)&shared_data->timestamp, now);
				} else if (handle->type == WAPI_HANDLE_FILE) {
					struct _WapiHandle_file *file_handle = &handle->u.file;
				
					DEBUG ("%s: (%d) handle 0x%x is FILE", __func__, _wapi_getpid (), i * _WAPI_HANDLE_INITIAL_COUNT + k);
				
					g_assert (file_handle->share_info != NULL);

					DEBUG ("%s: (%d) Inc refs on fileshare 0x%x", __func__, _wapi_getpid (), (file_handle->share_info - &_wapi_fileshare_layout->share_info[0]) / sizeof(struct _WapiFileShare));

					InterlockedExchange ((gint32 *)&file_handle->share_info->timestamp, now);
				}
			}
		}
	}

	thr_ret = mono_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
	
	thr_ret = _wapi_shm_sem_unlock (_WAPI_SHARED_SEM_FILESHARE);

	_wapi_handle_unlock_shared_handles ();
}

