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

#include <mono/io-layer/handles-private.h>
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
	gpointer data;
} WapiHandleBase;

static void (*_wapi_handle_ops_get_close_func (WapiHandleType type))(gpointer, gpointer);

static WapiHandleCapability handle_caps [WAPI_HANDLE_COUNT];
static WapiHandleOps *handle_ops [WAPI_HANDLE_COUNT];

/*
 * We can hold _WAPI_PRIVATE_MAX_SLOTS * _WAPI_HANDLE_INITIAL_COUNT handles.
 * If 4M handles are not enough... Oh, well... we will crash.
 */
#define SLOT_INDEX(x)	(x / _WAPI_HANDLE_INITIAL_COUNT)
#define SLOT_OFFSET(x)	(x % _WAPI_HANDLE_INITIAL_COUNT)

WapiHandleBase *_wapi_private_handles [_WAPI_PRIVATE_MAX_SLOTS];
static guint32 _wapi_private_handle_count = 0;
static guint32 _wapi_private_handle_slot_count = 0;

guint32 _wapi_fd_reserve;

/* 
 * This is an internal handle which is used for handling waiting for multiple handles.
 * Threads which wait for multiple handles wait on this one handle, and when a handle
 * is signalled, this handle is signalled too.
 */
static mono_mutex_t _wapi_global_signal_mutex;
static mono_cond_t _wapi_global_signal_cond;

static gboolean shutting_down = FALSE;

static void _wapi_handle_unref_full (gpointer handle, gboolean ignore_private_busy_handles);


static mono_mutex_t scan_mutex;

static gboolean
_wapi_handle_lookup_data (gpointer handle, WapiHandleBase **handle_data)
{
	gsize index, offset;

	g_assert (handle_data);

	index = SLOT_INDEX ((gsize) handle);
	if (index >= _WAPI_PRIVATE_MAX_SLOTS)
		return FALSE;
	if (!_wapi_private_handles [index])
		return FALSE;

	offset = SLOT_OFFSET ((gsize) handle);
	if (_wapi_private_handles [index][offset].type == WAPI_HANDLE_UNUSED)
		return FALSE;

	*handle_data = &_wapi_private_handles [index][offset];
	return TRUE;
}

WapiHandleType
_wapi_handle_type (gpointer handle)
{
	WapiHandleBase *handle_data;

	if (!_wapi_handle_lookup_data (handle, &handle_data))
		return WAPI_HANDLE_UNUSED;	/* An impossible type */

	return handle_data->type;
}

void
_wapi_handle_set_signal_state (gpointer handle, gboolean state, gboolean broadcast)
{
	WapiHandleBase *handle_data;
	int thr_ret;

	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return;
	}

#ifdef DEBUG
	g_message ("%s: setting state of %p to %s (broadcast %s)", __func__,
		   handle, state?"TRUE":"FALSE", broadcast?"TRUE":"FALSE");
#endif

	if (state == TRUE) {
		/* Tell everyone blocking on a single handle */

		/* The condition the global signal cond is waiting on is the signalling of
		 * _any_ handle. So lock it before setting the signalled state.
		 */
		thr_ret = mono_os_mutex_lock (&_wapi_global_signal_mutex);
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
		thr_ret = mono_os_cond_broadcast (&_wapi_global_signal_cond);
		if (thr_ret != 0)
			g_warning ("Bad call to mono_os_cond_broadcast result %d for handle %p", thr_ret, handle);
		g_assert (thr_ret == 0);
			
		thr_ret = mono_os_mutex_unlock (&_wapi_global_signal_mutex);
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
	WapiHandleBase *handle_data;
	
	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return(FALSE);
	}

	return handle_data->signalled;
}

int
_wapi_handle_lock_signal_mutex (void)
{
#ifdef DEBUG
	g_message ("%s: lock global signal mutex", __func__);
#endif

	return(mono_os_mutex_lock (&_wapi_global_signal_mutex));
}

int
_wapi_handle_unlock_signal_mutex (void)
{
#ifdef DEBUG
	g_message ("%s: unlock global signal mutex", __func__);
#endif

	return(mono_os_mutex_unlock (&_wapi_global_signal_mutex));
}

int
_wapi_handle_lock_handle (gpointer handle)
{
	WapiHandleBase *handle_data;
	
#ifdef DEBUG
	g_message ("%s: locking handle %p", __func__, handle);
#endif

	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return(0);
	}
	
	_wapi_handle_ref (handle);

	return(mono_os_mutex_lock (&handle_data->signal_mutex));
}

int
_wapi_handle_trylock_handle (gpointer handle)
{
	WapiHandleBase *handle_data;
	int ret;
	
#ifdef DEBUG
	g_message ("%s: locking handle %p", __func__, handle);
#endif

	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return(0);
	}
	
	_wapi_handle_ref (handle);

	ret = mono_os_mutex_trylock (&handle_data->signal_mutex);
	if (ret != 0) {
		_wapi_handle_unref (handle);
	}
	
	return(ret);
}

int
_wapi_handle_unlock_handle (gpointer handle)
{
	WapiHandleBase *handle_data;
	int ret;
	
#ifdef DEBUG
	g_message ("%s: unlocking handle %p", __func__, handle);
#endif
	
	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return(0);
	}

	ret = mono_os_mutex_unlock (&handle_data->signal_mutex);

	_wapi_handle_unref (handle);
	
	return(ret);
}

/*
 * wapi_init:
 *
 *   Initialize the io-layer.
 */
void
_wapi_handle_init (void)
{
	g_assert ((sizeof (handle_ops) / sizeof (handle_ops[0]))
		  == WAPI_HANDLE_COUNT);

	_wapi_fd_reserve = eg_getdtablesize ();

	/* This is needed by the code in _wapi_handle_new_internal */
	_wapi_fd_reserve = (_wapi_fd_reserve + (_WAPI_HANDLE_INITIAL_COUNT - 1)) & ~(_WAPI_HANDLE_INITIAL_COUNT - 1);

	do {
		/* 
		 * The entries in _wapi_private_handles reserved for fds are allocated lazily to 
		 * save memory.
		 */

		_wapi_private_handle_count += _WAPI_HANDLE_INITIAL_COUNT;
		_wapi_private_handle_slot_count ++;
	} while(_wapi_fd_reserve > _wapi_private_handle_count);

	mono_os_mutex_init (&scan_mutex);

	mono_os_cond_init (&_wapi_global_signal_cond);
	mono_os_mutex_init (&_wapi_global_signal_mutex);
}

void
_wapi_handle_cleanup (void)
{
	int i, j, k;
	
	g_assert (!shutting_down);
	shutting_down = TRUE;

	/* Every shared handle we were using ought really to be closed
	 * by now, but to make sure just blow them all away.  The
	 * exiting finalizer thread in particular races us to the
	 * program exit and doesn't always win, so it can be left
	 * cluttering up the shared file.  Anything else left over is
	 * really a bug.
	 */
	for(i = SLOT_INDEX (0); _wapi_private_handles[i] != NULL; i++) {
		for(j = SLOT_OFFSET (0); j < _WAPI_HANDLE_INITIAL_COUNT; j++) {
			WapiHandleBase *handle_data = &_wapi_private_handles[i][j];
			gpointer handle = GINT_TO_POINTER (i*_WAPI_HANDLE_INITIAL_COUNT+j);

			for(k = handle_data->ref; k > 0; k--) {
				_wapi_handle_unref_full (handle, TRUE);
			}
		}
	}

	for (i = 0; i < _WAPI_PRIVATE_MAX_SLOTS; ++i)
		g_free (_wapi_private_handles [i]);
}

static void _wapi_handle_init_handle (WapiHandleBase *handle,
			       WapiHandleType type, gpointer handle_specific)
{
	int thr_ret;
	
	g_assert (!shutting_down);
	
	handle->type = type;
	handle->signalled = FALSE;
	handle->ref = 1;
	
	thr_ret = mono_os_cond_init (&handle->signal_cond);
	g_assert (thr_ret == 0);
			
	thr_ret = mono_os_mutex_init (&handle->signal_mutex);
	g_assert (thr_ret == 0);

	if (handle_specific != NULL)
		handle->data = g_memdup (handle_specific, _wapi_handle_ops_typesize (type));
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
	
	g_assert (!shutting_down);
	
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
				WapiHandleBase *handle = &_wapi_private_handles [i][k];

				if(handle->type == WAPI_HANDLE_UNUSED) {
					last = count + 1;
			
					_wapi_handle_init_handle (handle, type, handle_specific);
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

	g_assert (!shutting_down);
		
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Creating new handle of type %s", __func__,
		   _wapi_handle_ops_typename (type));

	g_assert(!_WAPI_FD_HANDLE(type));
	
	thr_ret = mono_os_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);
		
	while ((handle_idx = _wapi_handle_new_internal (type, handle_specific)) == 0) {
		/* Try and expand the array, and have another go */
		int idx = SLOT_INDEX (_wapi_private_handle_count);
		if (idx >= _WAPI_PRIVATE_MAX_SLOTS) {
			break;
		}

		_wapi_private_handles [idx] = g_new0 (WapiHandleBase,
						_WAPI_HANDLE_INITIAL_COUNT);

		_wapi_private_handle_count += _WAPI_HANDLE_INITIAL_COUNT;
		_wapi_private_handle_slot_count ++;
	}
	
	thr_ret = mono_os_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);

	if (handle_idx == 0) {
		/* We ran out of slots */
		handle = INVALID_HANDLE_VALUE;
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
		_wapi_private_handles [idx] = g_new0 (WapiHandleBase,
											  _WAPI_HANDLE_INITIAL_COUNT);
	}

	thr_ret = mono_os_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
}

gpointer _wapi_handle_new_fd (WapiHandleType type, int fd,
			      gpointer handle_specific)
{
	WapiHandleBase *handle_data;
	int fd_index, fd_offset;
	
	g_assert (!shutting_down);
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Creating new handle of type %s", __func__,
		   _wapi_handle_ops_typename (type));
	
	g_assert(_WAPI_FD_HANDLE(type));

	if (fd >= _wapi_fd_reserve) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: fd %d is too big", __func__, fd);

		return(GUINT_TO_POINTER (INVALID_HANDLE_VALUE));
	}

	fd_index = SLOT_INDEX (fd);
	fd_offset = SLOT_OFFSET (fd);

	/* Initialize the array entries on demand */
	if (_wapi_private_handles [fd_index] == NULL)
		init_handles_slot (fd_index);

	handle_data = &_wapi_private_handles [fd_index][fd_offset];
	
	if (handle_data->type != WAPI_HANDLE_UNUSED) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: fd %d is already in use!", __func__, fd);
		/* FIXME: clean up this handle?  We can't do anything
		 * with the fd, cos thats the new one
		 */
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Assigning new fd handle %p", __func__, (gpointer)(gsize)fd);

	_wapi_handle_init_handle (handle_data, type, handle_specific);

	return(GUINT_TO_POINTER(fd));
}

gboolean 
_wapi_lookup_handle (gpointer handle, WapiHandleType type,
			      gpointer *handle_specific)
{
	WapiHandleBase *handle_data;

	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return(FALSE);
	}

	if (handle_data->type != type) {
		return(FALSE);
	}

	if (handle_specific == NULL) {
		return(FALSE);
	}
	
	*handle_specific = handle_data->data;
	
	return(TRUE);
}

void
_wapi_handle_foreach (gboolean (*on_each)(gpointer handle, gpointer data, gpointer user_data), gpointer user_data)
{
	WapiHandleBase *handle_data = NULL;
	gpointer handle;
	guint32 i, k;
	int thr_ret;

	thr_ret = mono_os_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);

	for (i = SLOT_INDEX (0); i < _wapi_private_handle_slot_count; i++) {
		if (_wapi_private_handles [i]) {
			for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
				handle_data = &_wapi_private_handles [i][k];
				if (handle_data->type == WAPI_HANDLE_UNUSED)
					continue;
				handle = GUINT_TO_POINTER (i * _WAPI_HANDLE_INITIAL_COUNT + k);
				if (on_each (handle, handle_data->data, user_data) == TRUE)
					goto done;
			}
		}
	}

done:
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
				handle_data = &_wapi_private_handles [i][k];
		
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
		*handle_specific = handle_data->data;
	}

done:
	return(ret);
}

void _wapi_handle_ref (gpointer handle)
{
	WapiHandleBase *handle_data;

	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Attempting to ref invalid private handle %p", __func__, handle);
		return;
	}

	InterlockedIncrement ((gint32 *)&handle_data->ref);
	
#ifdef DEBUG_REFS
	g_message ("%s: %s handle %p ref now %d",
		__func__, _wapi_handle_ops_typename (handle_data->type), handle, handle_data->ref);
#endif
}

/* The handle must not be locked on entry to this function */
static void _wapi_handle_unref_full (gpointer handle, gboolean ignore_private_busy_handles)
{
	WapiHandleBase *handle_data;
	gboolean destroy = FALSE, early_exit = FALSE;
	int thr_ret;

	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Attempting to unref invalid private handle %p",
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
		__func__, _wapi_handle_ops_typename (handle_data->type), handle, handle_data->ref, destroy?"TRUE":"FALSE");
#endif
	
	if(destroy==TRUE) {
		/* Need to copy the handle info, reset the slot in the
		 * array, and _only then_ call the close function to
		 * avoid race conditions (eg file descriptors being
		 * closed, and another file being opened getting the
		 * same fd racing the memset())
		 */
		WapiHandleType type;
		gpointer data;
		void (*close_func)(gpointer, gpointer);

		type = handle_data->type;
		data = handle_data->data;

		thr_ret = mono_os_mutex_lock (&scan_mutex);
		g_assert (thr_ret == 0);

		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Destroying handle %p", __func__, handle);

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

		memset (handle_data, 0, sizeof (WapiHandleBase));

		thr_ret = mono_os_mutex_unlock (&scan_mutex);
		g_assert (thr_ret == 0);

		if (early_exit)
			return;
		
		close_func = _wapi_handle_ops_get_close_func (type);
		if (close_func != NULL) {
			close_func (handle, data);
		}

		g_free (data);
	}
}

void _wapi_handle_unref (gpointer handle)
{
	_wapi_handle_unref_full (handle, FALSE);
}

void
_wapi_handle_register_ops (WapiHandleType type, WapiHandleOps *ops)
{
	handle_ops [type] = ops;
}

void _wapi_handle_register_capabilities (WapiHandleType type,
					 WapiHandleCapability caps)
{
	handle_caps[type] = caps;
}

gboolean _wapi_handle_test_capabilities (gpointer handle,
					 WapiHandleCapability caps)
{
	WapiHandleBase *handle_data;
	WapiHandleType type;

	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return(FALSE);
	}

	type = handle_data->type;

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
	WapiHandleBase *handle_data;
	WapiHandleType type;

	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return;
	}

	type = handle_data->type;

	if (handle_ops[type] != NULL &&
	    handle_ops[type]->close != NULL) {
		handle_ops[type]->close (handle, data);
	}
}

void _wapi_handle_ops_details (WapiHandleType type, gpointer data)
{
	if (handle_ops[type] != NULL &&
	    handle_ops[type]->details != NULL) {
		handle_ops[type]->details (data);
	}
}

const gchar* _wapi_handle_ops_typename (WapiHandleType type)
{
	g_assert (handle_ops [type]);
	g_assert (handle_ops [type]->typename);
	return handle_ops [type]->typename ();
}

gsize _wapi_handle_ops_typesize (WapiHandleType type)
{
	g_assert (handle_ops [type]);
	g_assert (handle_ops [type]->typesize);
	return handle_ops [type]->typesize ();
}

void _wapi_handle_ops_signal (gpointer handle)
{
	WapiHandleBase *handle_data;
	WapiHandleType type;

	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return;
	}

	type = handle_data->type;

	if (handle_ops[type] != NULL && handle_ops[type]->signal != NULL) {
		handle_ops[type]->signal (handle);
	}
}

gboolean _wapi_handle_ops_own (gpointer handle)
{
	WapiHandleBase *handle_data;
	WapiHandleType type;
	
	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return(FALSE);
	}

	type = handle_data->type;

	if (handle_ops[type] != NULL && handle_ops[type]->own_handle != NULL) {
		return(handle_ops[type]->own_handle (handle));
	} else {
		return(FALSE);
	}
}

gboolean _wapi_handle_ops_isowned (gpointer handle)
{
	WapiHandleBase *handle_data;
	WapiHandleType type;

	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return(FALSE);
	}

	type = handle_data->type;

	if (handle_ops[type] != NULL && handle_ops[type]->is_owned != NULL) {
		return(handle_ops[type]->is_owned (handle));
	} else {
		return(FALSE);
	}
}

guint32 _wapi_handle_ops_special_wait (gpointer handle, guint32 timeout, gboolean alertable)
{
	WapiHandleBase *handle_data;
	WapiHandleType type;
	
	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return(WAIT_FAILED);
	}

	type = handle_data->type;
	
	if (handle_ops[type] != NULL &&
	    handle_ops[type]->special_wait != NULL) {
		return(handle_ops[type]->special_wait (handle, timeout, alertable));
	} else {
		return(WAIT_FAILED);
	}
}

void _wapi_handle_ops_prewait (gpointer handle)
{
	WapiHandleBase *handle_data;
	WapiHandleType type;
	
	if (!_wapi_handle_lookup_data (handle, &handle_data)) {
		return;
	}

	type = handle_data->type;
	
	if (handle_ops[type] != NULL &&
	    handle_ops[type]->prewait != NULL) {
		handle_ops[type]->prewait (handle);
	}
}

static void
spin (guint32 ms)
{
	struct timespec sleepytime;

	g_assert (ms < 1000);

	sleepytime.tv_sec = 0;
	sleepytime.tv_nsec = ms * 1000000;
	nanosleep (&sleepytime, NULL);
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
			spin (10 * iter);
			
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

static int
_wapi_handle_timedwait_signal_naked (mono_cond_t *cond, mono_mutex_t *mutex, guint32 timeout, gboolean poll, gboolean *alerted)
{
	int res;

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

	return res;
}

static void
signal_global (gpointer unused)
{
	/* If we reach here, then interrupt token is set to the flag value, which
	 * means that the target thread is either
	 * - before the first CAS in timedwait, which means it won't enter the wait.
	 * - it is after the first CAS, so it is already waiting, or it will enter
	 *    the wait, and it will be interrupted by the broadcast. */
	mono_os_mutex_lock (&_wapi_global_signal_mutex);
	mono_os_cond_broadcast (&_wapi_global_signal_cond);
	mono_os_mutex_unlock (&_wapi_global_signal_mutex);
}

int
_wapi_handle_timedwait_signal (guint32 timeout, gboolean poll, gboolean *alerted)
{
	int res;

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: waiting for global", __func__);

	if (alerted)
		*alerted = FALSE;

	if (alerted) {
		mono_thread_info_install_interrupt (signal_global, NULL, alerted);
		if (*alerted)
			return 0;
	}

	res = _wapi_handle_timedwait_signal_naked (&_wapi_global_signal_cond, &_wapi_global_signal_mutex, timeout, poll, alerted);

	if (alerted)
		mono_thread_info_uninstall_interrupt (alerted);

	return res;
}

static void
signal_handle_and_unref (gpointer handle)
{
	WapiHandleBase *handle_data;
	mono_cond_t *cond;
	mono_mutex_t *mutex;

	if (!_wapi_handle_lookup_data (handle, &handle_data))
		g_error ("cannot signal unknown handle %p", handle);

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
_wapi_handle_timedwait_signal_handle (gpointer handle, guint32 timeout, gboolean poll, gboolean *alerted)
{
	WapiHandleBase *handle_data;
	int res;

	if (!_wapi_handle_lookup_data (handle, &handle_data))
		g_error ("cannot wait on unknown handle %p", handle);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: waiting for %p (type %s)", __func__, handle,
		   _wapi_handle_ops_typename (_wapi_handle_type (handle)));

	if (alerted)
		*alerted = FALSE;

	if (alerted) {
		mono_thread_info_install_interrupt (signal_handle_and_unref, handle, alerted);
		if (*alerted)
			return 0;
		_wapi_handle_ref (handle);
	}

	res = _wapi_handle_timedwait_signal_naked (&handle_data->signal_cond, &handle_data->signal_mutex, timeout, poll, alerted);

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
				handle_data = &_wapi_private_handles [i][k];

				if (handle_data->type == WAPI_HANDLE_UNUSED) {
					continue;
				}
		
				g_print ("%3x [%7s] %s %d ",
						 i * _WAPI_HANDLE_INITIAL_COUNT + k,
						 _wapi_handle_ops_typename (handle_data->type),
						 handle_data->signalled?"Sg":"Un",
						 handle_data->ref);
				_wapi_handle_ops_details (handle_data->type, handle_data->data);
				g_print ("\n");
			}
		}
	}

	thr_ret = mono_os_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
}
