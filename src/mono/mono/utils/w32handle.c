/*
 * w32handle.c:  Generic and internal operations on handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * (C) 2002-2011 Novell, Inc.
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#if !defined(HOST_WIN32)

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

#include "w32handle.h"

#include "atomic.h"
#include "mono-logger-internals.h"
#include "mono-os-mutex.h"
#include "mono-proclib.h"
#include "mono-threads.h"
#include "mono-time.h"

#undef DEBUG_REFS

#define SLOT_MAX		(1024 * 16)

/* must be a power of 2 */
#define HANDLE_PER_SLOT	(256)

#define INFINITE 0xFFFFFFFF

typedef struct {
	MonoW32HandleType type;
	guint ref;
	gboolean signalled;
	mono_mutex_t signal_mutex;
	mono_cond_t signal_cond;
	gpointer specific;
} MonoW32HandleBase;

static MonoW32HandleCapability handle_caps [MONO_W32HANDLE_COUNT];
static MonoW32HandleOps *handle_ops [MONO_W32HANDLE_COUNT];

/*
 * We can hold SLOT_MAX * HANDLE_PER_SLOT handles.
 * If 4M handles are not enough... Oh, well... we will crash.
 */
#define SLOT_INDEX(x)	(x / HANDLE_PER_SLOT)
#define SLOT_OFFSET(x)	(x % HANDLE_PER_SLOT)

static MonoW32HandleBase *private_handles [SLOT_MAX];
static guint32 private_handles_count = 0;
static guint32 private_handles_slots_count = 0;

guint32 mono_w32handle_fd_reserve;

/*
 * This is an internal handle which is used for handling waiting for multiple handles.
 * Threads which wait for multiple handles wait on this one handle, and when a handle
 * is signalled, this handle is signalled too.
 */
static mono_mutex_t global_signal_mutex;
static mono_cond_t global_signal_cond;

static mono_mutex_t scan_mutex;

static gboolean shutting_down = FALSE;

static gboolean
type_is_fd (MonoW32HandleType type)
{
	switch (type) {
	case MONO_W32HANDLE_FILE:
	case MONO_W32HANDLE_CONSOLE:
	case MONO_W32HANDLE_SOCKET:
	case MONO_W32HANDLE_PIPE:
		return TRUE;
	default:
		return FALSE;
	}
}

static gboolean
mono_w32handle_lookup_data (gpointer handle, MonoW32HandleBase **handle_data)
{
	gsize index, offset;

	g_assert (handle_data);

	index = SLOT_INDEX ((gsize) handle);
	if (index >= SLOT_MAX)
		return FALSE;
	if (!private_handles [index])
		return FALSE;

	offset = SLOT_OFFSET ((gsize) handle);
	if (private_handles [index][offset].type == MONO_W32HANDLE_UNUSED)
		return FALSE;

	*handle_data = &private_handles [index][offset];
	return TRUE;
}

MonoW32HandleType
mono_w32handle_get_type (gpointer handle)
{
	MonoW32HandleBase *handle_data;

	if (!mono_w32handle_lookup_data (handle, &handle_data))
		return MONO_W32HANDLE_UNUSED;	/* An impossible type */

	return handle_data->type;
}

void
mono_w32handle_set_signal_state (gpointer handle, gboolean state, gboolean broadcast)
{
	MonoW32HandleBase *handle_data;

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
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
		mono_os_mutex_lock (&global_signal_mutex);

		/* This function _must_ be called with
		 * handle->signal_mutex locked
		 */
		handle_data->signalled=state;

		if (broadcast == TRUE) {
			mono_os_cond_broadcast (&handle_data->signal_cond);
		} else {
			mono_os_cond_signal (&handle_data->signal_cond);
		}

		/* Tell everyone blocking on multiple handles that something
		 * was signalled
		 */
		mono_os_cond_broadcast (&global_signal_cond);

		mono_os_mutex_unlock (&global_signal_mutex);
	} else {
		handle_data->signalled=state;
	}
}

gboolean
mono_w32handle_issignalled (gpointer handle)
{
	MonoW32HandleBase *handle_data;

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		return(FALSE);
	}

	return handle_data->signalled;
}

static int
mono_w32handle_lock_signal_mutex (void)
{
#ifdef DEBUG
	g_message ("%s: lock global signal mutex", __func__);
#endif

	mono_os_mutex_lock (&global_signal_mutex);

	return 0;
}

static int
mono_w32handle_unlock_signal_mutex (void)
{
#ifdef DEBUG
	g_message ("%s: unlock global signal mutex", __func__);
#endif

	mono_os_mutex_unlock (&global_signal_mutex);

	return 0;
}

int
mono_w32handle_lock_handle (gpointer handle)
{
	MonoW32HandleBase *handle_data;

#ifdef DEBUG
	g_message ("%s: locking handle %p", __func__, handle);
#endif

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		return(0);
	}

	mono_w32handle_ref (handle);

	mono_os_mutex_lock (&handle_data->signal_mutex);

	return 0;
}

int
mono_w32handle_trylock_handle (gpointer handle)
{
	MonoW32HandleBase *handle_data;
	int ret;

#ifdef DEBUG
	g_message ("%s: locking handle %p", __func__, handle);
#endif

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		return(0);
	}

	mono_w32handle_ref (handle);

	ret = mono_os_mutex_trylock (&handle_data->signal_mutex);
	if (ret != 0) {
		mono_w32handle_unref (handle);
	}

	return(ret);
}

int
mono_w32handle_unlock_handle (gpointer handle)
{
	MonoW32HandleBase *handle_data;

#ifdef DEBUG
	g_message ("%s: unlocking handle %p", __func__, handle);
#endif

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		return(0);
	}

	mono_os_mutex_unlock (&handle_data->signal_mutex);

	mono_w32handle_unref (handle);

	return 0;
}

/*
 * wapi_init:
 *
 *   Initialize the io-layer.
 */
void
mono_w32handle_init (void)
{
	static gboolean initialized = FALSE;

	if (initialized)
		return;

	g_assert ((sizeof (handle_ops) / sizeof (handle_ops[0]))
		  == MONO_W32HANDLE_COUNT);

	/* This is needed by the code in mono_w32handle_new_internal */
	mono_w32handle_fd_reserve = (eg_getdtablesize () + (HANDLE_PER_SLOT - 1)) & ~(HANDLE_PER_SLOT - 1);

	do {
		/*
		 * The entries in private_handles reserved for fds are allocated lazily to
		 * save memory.
		 */

		private_handles_count += HANDLE_PER_SLOT;
		private_handles_slots_count ++;
	} while(mono_w32handle_fd_reserve > private_handles_count);

	mono_os_mutex_init (&scan_mutex);

	mono_os_cond_init (&global_signal_cond);
	mono_os_mutex_init (&global_signal_mutex);

	initialized = TRUE;
}

void
mono_w32handle_cleanup (void)
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
	for(i = SLOT_INDEX (0); private_handles[i] != NULL; i++) {
		for(j = SLOT_OFFSET (0); j < HANDLE_PER_SLOT; j++) {
			MonoW32HandleBase *handle_data = &private_handles[i][j];
			gpointer handle = GINT_TO_POINTER (i*HANDLE_PER_SLOT+j);

			for(k = handle_data->ref; k > 0; k--) {
				mono_w32handle_unref (handle);
			}
		}
	}

	for (i = 0; i < SLOT_MAX; ++i)
		g_free (private_handles [i]);
}

static void mono_w32handle_init_handle (MonoW32HandleBase *handle,
			       MonoW32HandleType type, gpointer handle_specific)
{
	g_assert (handle->ref == 0);

	handle->type = type;
	handle->signalled = FALSE;
	handle->ref = 1;

	mono_os_cond_init (&handle->signal_cond);
	mono_os_mutex_init (&handle->signal_mutex);

	if (handle_specific)
		handle->specific = g_memdup (handle_specific, mono_w32handle_ops_typesize (type));
}

/*
 * mono_w32handle_new_internal:
 * @type: Init handle to this type
 *
 * Search for a free handle and initialize it. Return the handle on
 * success and 0 on failure.  This is only called from
 * mono_w32handle_new, and scan_mutex must be held.
 */
static guint32 mono_w32handle_new_internal (MonoW32HandleType type,
					  gpointer handle_specific)
{
	guint32 i, k, count;
	static guint32 last = 0;
	gboolean retry = FALSE;
	
	/* A linear scan should be fast enough.  Start from the last
	 * allocation, assuming that handles are allocated more often
	 * than they're freed. Leave the space reserved for file
	 * descriptors
	 */

	if (last < mono_w32handle_fd_reserve) {
		last = mono_w32handle_fd_reserve;
	} else {
		retry = TRUE;
	}

again:
	count = last;
	for(i = SLOT_INDEX (count); i < private_handles_slots_count; i++) {
		if (private_handles [i]) {
			for (k = SLOT_OFFSET (count); k < HANDLE_PER_SLOT; k++) {
				MonoW32HandleBase *handle = &private_handles [i][k];

				if(handle->type == MONO_W32HANDLE_UNUSED) {
					last = count + 1;

					mono_w32handle_init_handle (handle, type, handle_specific);
					return (count);
				}
				count++;
			}
		}
	}

	if(retry && last > mono_w32handle_fd_reserve) {
		/* Try again from the beginning */
		last = mono_w32handle_fd_reserve;
		goto again;
	}

	/* Will need to expand the array.  The caller will sort it out */

	return(0);
}

gpointer
mono_w32handle_new (MonoW32HandleType type, gpointer handle_specific)
{
	guint32 handle_idx = 0;
	gpointer handle;

	g_assert (!shutting_down);

	g_assert(!type_is_fd(type));

	mono_os_mutex_lock (&scan_mutex);

	while ((handle_idx = mono_w32handle_new_internal (type, handle_specific)) == 0) {
		/* Try and expand the array, and have another go */
		int idx = SLOT_INDEX (private_handles_count);
		if (idx >= SLOT_MAX) {
			break;
		}

		private_handles [idx] = g_new0 (MonoW32HandleBase, HANDLE_PER_SLOT);

		private_handles_count += HANDLE_PER_SLOT;
		private_handles_slots_count ++;
	}

	mono_os_mutex_unlock (&scan_mutex);

	if (handle_idx == 0) {
		/* We ran out of slots */
		handle = INVALID_HANDLE_VALUE;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: failed to create %s handle", __func__, mono_w32handle_ops_typename (type));
		goto done;
	}

	/* Make sure we left the space for fd mappings */
	g_assert (handle_idx >= mono_w32handle_fd_reserve);

	handle = GUINT_TO_POINTER (handle_idx);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: create %s handle %p", __func__, mono_w32handle_ops_typename (type), handle);

done:
	return(handle);
}

gpointer mono_w32handle_new_fd (MonoW32HandleType type, int fd,
			      gpointer handle_specific)
{
	MonoW32HandleBase *handle_data;
	int fd_index, fd_offset;

	g_assert (!shutting_down);

	g_assert(type_is_fd(type));

	if (fd >= mono_w32handle_fd_reserve) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: failed to create %s handle, fd is too big", __func__, mono_w32handle_ops_typename (type));

		return(GUINT_TO_POINTER (INVALID_HANDLE_VALUE));
	}

	fd_index = SLOT_INDEX (fd);
	fd_offset = SLOT_OFFSET (fd);

	/* Initialize the array entries on demand */
	if (!private_handles [fd_index]) {
		mono_os_mutex_lock (&scan_mutex);

		if (!private_handles [fd_index])
			private_handles [fd_index] = g_new0 (MonoW32HandleBase, HANDLE_PER_SLOT);

		mono_os_mutex_unlock (&scan_mutex);
	}

	handle_data = &private_handles [fd_index][fd_offset];

	if (handle_data->type != MONO_W32HANDLE_UNUSED) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: failed to create %s handle, fd is already in use", __func__, mono_w32handle_ops_typename (type));
		/* FIXME: clean up this handle?  We can't do anything
		 * with the fd, cos thats the new one
		 */
		return(GUINT_TO_POINTER (INVALID_HANDLE_VALUE));
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: create %s handle %p", __func__, mono_w32handle_ops_typename (type), GUINT_TO_POINTER(fd));

	mono_w32handle_init_handle (handle_data, type, handle_specific);

	return(GUINT_TO_POINTER(fd));
}

gboolean
mono_w32handle_lookup (gpointer handle, MonoW32HandleType type,
			      gpointer *handle_specific)
{
	MonoW32HandleBase *handle_data;

	g_assert (handle_specific);

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		return(FALSE);
	}

	if (handle_data->type != type) {
		return(FALSE);
	}

	*handle_specific = handle_data->specific;

	return(TRUE);
}

static gboolean
mono_w32handle_ref_core (gpointer handle, MonoW32HandleBase *handle_data);

static gboolean
mono_w32handle_unref_core (gpointer handle, MonoW32HandleBase *handle_data, guint minimum);

void
mono_w32handle_foreach (gboolean (*on_each)(gpointer handle, gpointer data, gpointer user_data), gpointer user_data)
{
	guint32 i, k;

	mono_os_mutex_lock (&scan_mutex);

	for (i = SLOT_INDEX (0); i < private_handles_slots_count; i++) {
		if (!private_handles [i])
			continue;
		for (k = SLOT_OFFSET (0); k < HANDLE_PER_SLOT; k++) {
			MonoW32HandleBase *handle_data = NULL;
			gpointer handle;
			gboolean destroy, finished;

			handle_data = &private_handles [i][k];
			if (handle_data->type == MONO_W32HANDLE_UNUSED)
				continue;

			handle = GUINT_TO_POINTER (i * HANDLE_PER_SLOT + k);

			if (!mono_w32handle_ref_core (handle, handle_data)) {
				/* we are racing with mono_w32handle_unref:
				 *  the handle ref has been decremented, but it
				 *  hasn't yet been destroyed. */
				continue;
			}

			finished = on_each (handle, handle_data->specific, user_data);

			/* we do not want to have to destroy the handle here,
			 * as it would means the ref/unref are unbalanced */
			destroy = mono_w32handle_unref_core (handle, handle_data, 2);
			g_assert (!destroy);

			if (finished)
				goto done;
		}
	}

done:
	mono_os_mutex_unlock (&scan_mutex);
}

typedef struct {
	MonoW32HandleType type;
	gboolean (*search_user_callback)(gpointer handle, gpointer data);
	gpointer search_user_data;
	gpointer handle;
	gpointer handle_specific;
} SearchData;

static gboolean
search_callback (gpointer handle, gpointer handle_specific, gpointer user_data)
{
	SearchData *search_data = (SearchData*) user_data;

	if (search_data->type != mono_w32handle_get_type (handle))
		return FALSE;

	if (!search_data->search_user_callback (handle, search_data->search_user_data))
		return FALSE;

	mono_w32handle_ref (handle);
	search_data->handle = handle;
	search_data->handle_specific = handle_specific;
	return TRUE;
}

/* This might list some shared handles twice if they are already
 * opened by this process, and the check function returns FALSE the
 * first time.  Shared handles that are created during the search are
 * unreffed if the check function returns FALSE, so callers must not
 * rely on the handle persisting (unless the check function returns
 * TRUE)
 * The caller owns the returned handle.
 */
gpointer mono_w32handle_search (MonoW32HandleType type,
			      gboolean (*check)(gpointer test, gpointer user),
			      gpointer user_data,
			      gpointer *handle_specific,
			      gboolean search_shared)
{
	SearchData search_data;

	memset (&search_data, 0, sizeof (search_data));
	search_data.type = type;
	search_data.search_user_callback = check;
	search_data.search_user_data = user_data;
	mono_w32handle_foreach (search_callback, &search_data);
	if (handle_specific)
		*handle_specific = search_data.handle_specific;
	return search_data.handle;
}

static gboolean
mono_w32handle_ref_core (gpointer handle, MonoW32HandleBase *handle_data)
{
	guint old, new;

	do {
		old = handle_data->ref;
		if (old == 0)
			return FALSE;

		new = old + 1;
	} while (InterlockedCompareExchange ((gint32*) &handle_data->ref, new, old) != old);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: ref %s handle %p, ref: %d -> %d",
		__func__, mono_w32handle_ops_typename (handle_data->type), handle, old, new);

	return TRUE;
}

static gboolean
mono_w32handle_unref_core (gpointer handle, MonoW32HandleBase *handle_data, guint minimum)
{
	MonoW32HandleType type;
	guint old, new;

	type = handle_data->type;

	do {
		old = handle_data->ref;
		if (!(old >= minimum))
			g_error ("%s: handle %p has ref %d, it should be >= %d", __func__, handle, old, minimum);

		new = old - 1;
	} while (InterlockedCompareExchange ((gint32*) &handle_data->ref, new, old) != old);

	/* handle_data might contain invalid data from now on, if
	 * another thread is unref'ing this handle at the same time */

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: unref %s handle %p, ref: %d -> %d destroy: %s",
		__func__, mono_w32handle_ops_typename (type), handle, old, new, new == 0 ? "true" : "false");

	return new == 0;
}

void mono_w32handle_ref (gpointer handle)
{
	MonoW32HandleBase *handle_data;

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: failed to ref handle %p, unknown handle", __func__, handle);
		return;
	}

	if (!mono_w32handle_ref_core (handle, handle_data))
		g_error ("%s: failed to ref handle %p", __func__, handle);
}

static void (*_wapi_handle_ops_get_close_func (MonoW32HandleType type))(gpointer, gpointer);

/* The handle must not be locked on entry to this function */
void
mono_w32handle_unref (gpointer handle)
{
	MonoW32HandleBase *handle_data;
	gboolean destroy;

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: failed to unref handle %p, unknown handle",
			__func__, handle);
		return;
	}

	destroy = mono_w32handle_unref_core (handle, handle_data, 1);

	if (destroy) {
		/* Need to copy the handle info, reset the slot in the
		 * array, and _only then_ call the close function to
		 * avoid race conditions (eg file descriptors being
		 * closed, and another file being opened getting the
		 * same fd racing the memset())
		 */
		MonoW32HandleType type;
		gpointer handle_specific;
		void (*close_func)(gpointer, gpointer);

		type = handle_data->type;
		handle_specific = handle_data->specific;

		mono_os_mutex_lock (&scan_mutex);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: destroy %s handle %p", __func__, mono_w32handle_ops_typename (type), handle);

		mono_os_mutex_destroy (&handle_data->signal_mutex);
		mono_os_cond_destroy (&handle_data->signal_cond);

		memset (handle_data, 0, sizeof (MonoW32HandleBase));

		mono_os_mutex_unlock (&scan_mutex);

		close_func = _wapi_handle_ops_get_close_func (type);
		if (close_func != NULL) {
			close_func (handle, handle_specific);
		}

		g_free (handle_specific);
	}
}

void
mono_w32handle_register_ops (MonoW32HandleType type, MonoW32HandleOps *ops)
{
	handle_ops [type] = ops;
}

void mono_w32handle_register_capabilities (MonoW32HandleType type,
					 MonoW32HandleCapability caps)
{
	handle_caps[type] = caps;
}

gboolean mono_w32handle_test_capabilities (gpointer handle,
					 MonoW32HandleCapability caps)
{
	MonoW32HandleBase *handle_data;
	MonoW32HandleType type;

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		return(FALSE);
	}

	type = handle_data->type;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: testing 0x%x against 0x%x (%d)", __func__,
		   handle_caps[type], caps, handle_caps[type] & caps);

	return((handle_caps[type] & caps) != 0);
}

static void (*_wapi_handle_ops_get_close_func (MonoW32HandleType type))(gpointer, gpointer)
{
	if (handle_ops[type] != NULL &&
	    handle_ops[type]->close != NULL) {
		return (handle_ops[type]->close);
	}

	return (NULL);
}

void mono_w32handle_ops_close (gpointer handle, gpointer data)
{
	MonoW32HandleBase *handle_data;
	MonoW32HandleType type;

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		return;
	}

	type = handle_data->type;

	if (handle_ops[type] != NULL &&
	    handle_ops[type]->close != NULL) {
		handle_ops[type]->close (handle, data);
	}
}

void mono_w32handle_ops_details (MonoW32HandleType type, gpointer data)
{
	if (handle_ops[type] != NULL &&
	    handle_ops[type]->details != NULL) {
		handle_ops[type]->details (data);
	}
}

const gchar* mono_w32handle_ops_typename (MonoW32HandleType type)
{
	g_assert (handle_ops [type]);
	g_assert (handle_ops [type]->typename);
	return handle_ops [type]->typename ();
}

gsize mono_w32handle_ops_typesize (MonoW32HandleType type)
{
	g_assert (handle_ops [type]);
	g_assert (handle_ops [type]->typesize);
	return handle_ops [type]->typesize ();
}

void mono_w32handle_ops_signal (gpointer handle)
{
	MonoW32HandleBase *handle_data;
	MonoW32HandleType type;

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		return;
	}

	type = handle_data->type;

	if (handle_ops[type] != NULL && handle_ops[type]->signal != NULL) {
		handle_ops[type]->signal (handle);
	}
}

gboolean mono_w32handle_ops_own (gpointer handle, guint32 *statuscode)
{
	MonoW32HandleBase *handle_data;
	MonoW32HandleType type;

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		return(FALSE);
	}

	type = handle_data->type;

	if (handle_ops[type] != NULL && handle_ops[type]->own_handle != NULL) {
		return(handle_ops[type]->own_handle (handle, statuscode));
	} else {
		return(FALSE);
	}
}

gboolean mono_w32handle_ops_isowned (gpointer handle)
{
	MonoW32HandleBase *handle_data;
	MonoW32HandleType type;

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		return(FALSE);
	}

	type = handle_data->type;

	if (handle_ops[type] != NULL && handle_ops[type]->is_owned != NULL) {
		return(handle_ops[type]->is_owned (handle));
	} else {
		return(FALSE);
	}
}

guint32 mono_w32handle_ops_specialwait (gpointer handle, guint32 timeout, gboolean *alerted)
{
	MonoW32HandleBase *handle_data;
	MonoW32HandleType type;

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
		return(WAIT_FAILED);
	}

	type = handle_data->type;

	if (handle_ops[type] != NULL &&
	    handle_ops[type]->special_wait != NULL) {
		return(handle_ops[type]->special_wait (handle, timeout, alerted));
	} else {
		return(WAIT_FAILED);
	}
}

void mono_w32handle_ops_prewait (gpointer handle)
{
	MonoW32HandleBase *handle_data;
	MonoW32HandleType type;

	if (!mono_w32handle_lookup_data (handle, &handle_data)) {
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

static void
mono_w32handle_lock_handles (gpointer *handles, gsize numhandles)
{
	guint32 i, iter=0;
	int thr_ret;

	/* Lock all the handles, with backoff */
again:
	for(i=0; i<numhandles; i++) {
		gpointer handle = handles[i];

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: attempting to lock %p", __func__, handle);

		thr_ret = mono_w32handle_trylock_handle (handle);

		if (thr_ret != 0) {
			/* Bummer */

			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: attempt failed for %p: %s", __func__,
				   handle, strerror (thr_ret));

			while (i--) {
				handle = handles[i];

				thr_ret = mono_w32handle_unlock_handle (handle);
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

			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: Backing off for %d ms", __func__,
				   iter*10);
			spin (10 * iter);

			goto again;
		}
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: Locked all handles", __func__);
}

static void
mono_w32handle_unlock_handles (gpointer *handles, gsize numhandles)
{
	guint32 i;
	int thr_ret;

	for(i=0; i<numhandles; i++) {
		gpointer handle = handles[i];

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: unlocking handle %p", __func__, handle);

		thr_ret = mono_w32handle_unlock_handle (handle);
		g_assert (thr_ret == 0);
	}
}

static int
mono_w32handle_timedwait_signal_naked (mono_cond_t *cond, mono_mutex_t *mutex, guint32 timeout, gboolean poll, gboolean *alerted)
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
				if (res == -1)
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
	mono_os_mutex_lock (&global_signal_mutex);
	mono_os_cond_broadcast (&global_signal_cond);
	mono_os_mutex_unlock (&global_signal_mutex);
}

static int
mono_w32handle_timedwait_signal (guint32 timeout, gboolean poll, gboolean *alerted)
{
	int res;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: waiting for global", __func__);

	if (alerted)
		*alerted = FALSE;

	if (alerted) {
		mono_thread_info_install_interrupt (signal_global, NULL, alerted);
		if (*alerted)
			return 0;
	}

	res = mono_w32handle_timedwait_signal_naked (&global_signal_cond, &global_signal_mutex, timeout, poll, alerted);

	if (alerted)
		mono_thread_info_uninstall_interrupt (alerted);

	return res;
}

static void
signal_handle_and_unref (gpointer handle)
{
	MonoW32HandleBase *handle_data;
	mono_cond_t *cond;
	mono_mutex_t *mutex;

	if (!mono_w32handle_lookup_data (handle, &handle_data))
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

	mono_w32handle_unref (handle);
}

static int
mono_w32handle_timedwait_signal_handle (gpointer handle, guint32 timeout, gboolean poll, gboolean *alerted)
{
	MonoW32HandleBase *handle_data;
	int res;

	if (!mono_w32handle_lookup_data (handle, &handle_data))
		g_error ("cannot wait on unknown handle %p", handle);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: waiting for %p (type %s)", __func__, handle,
		   mono_w32handle_ops_typename (mono_w32handle_get_type (handle)));

	if (alerted)
		*alerted = FALSE;

	if (alerted) {
		mono_thread_info_install_interrupt (signal_handle_and_unref, handle, alerted);
		if (*alerted)
			return 0;
		mono_w32handle_ref (handle);
	}

	res = mono_w32handle_timedwait_signal_naked (&handle_data->signal_cond, &handle_data->signal_mutex, timeout, poll, alerted);

	if (alerted) {
		mono_thread_info_uninstall_interrupt (alerted);
		if (!*alerted) {
			/* if it is alerted, then the handle is unref in the interrupt callback */
			mono_w32handle_unref (handle);
		}
	}

	return res;
}

static gboolean
dump_callback (gpointer handle, gpointer handle_specific, gpointer user_data)
{
	MonoW32HandleBase *handle_data;

	if (!mono_w32handle_lookup_data (handle, &handle_data))
		g_error ("cannot dump unknown handle %p", handle);

	g_print ("%p [%7s] signalled: %5s ref: %3d ",
		handle, mono_w32handle_ops_typename (handle_data->type), handle_data->signalled ? "true" : "false", handle_data->ref);
	mono_w32handle_ops_details (handle_data->type, handle_data->specific);
	g_print ("\n");

	return FALSE;
}

void mono_w32handle_dump (void)
{
	mono_w32handle_foreach (dump_callback, NULL);
}

static gboolean
own_if_signalled (gpointer handle, guint32 *statuscode)
{
	if (!mono_w32handle_issignalled (handle))
		return FALSE;

	*statuscode = WAIT_OBJECT_0;
	mono_w32handle_ops_own (handle, statuscode);
	return TRUE;
}

static gboolean
own_if_owned( gpointer handle, guint32 *statuscode)
{
	if (!mono_w32handle_ops_isowned (handle))
		return FALSE;

	*statuscode = WAIT_OBJECT_0;
	mono_w32handle_ops_own (handle, statuscode);
	return TRUE;
}

MonoW32HandleWaitRet
mono_w32handle_wait_one (gpointer handle, guint32 timeout, gboolean alertable)
{
	MonoW32HandleWaitRet ret;
	gboolean alerted;
	gint64 start;
	gint thr_ret;
	guint32 statuscode = 0;

	alerted = FALSE;

	if (mono_w32handle_test_capabilities (handle, MONO_W32HANDLE_CAP_SPECIAL_WAIT)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: handle %p has special wait",
			__func__, handle);

		switch (mono_w32handle_ops_specialwait (handle, timeout, alertable ? &alerted : NULL)) {
		case WAIT_OBJECT_0:
			ret = MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
			break;
		case WAIT_ABANDONED_0:
			ret = MONO_W32HANDLE_WAIT_RET_ABANDONED_0;
			break;
		case WAIT_IO_COMPLETION:
			ret = MONO_W32HANDLE_WAIT_RET_ALERTED;
			break;
		case WAIT_TIMEOUT:
			ret = MONO_W32HANDLE_WAIT_RET_TIMEOUT;
			break;
		case WAIT_FAILED:
			ret = MONO_W32HANDLE_WAIT_RET_FAILED;
			break;
		default:
			g_assert_not_reached ();
		}

		if (alerted)
			ret = MONO_W32HANDLE_WAIT_RET_ALERTED;

		return ret;
	}

	if (!mono_w32handle_test_capabilities (handle, MONO_W32HANDLE_CAP_WAIT)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: handle %p can't be waited for",
			__func__, handle);

		return MONO_W32HANDLE_WAIT_RET_FAILED;
	}

	thr_ret = mono_w32handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	if (mono_w32handle_test_capabilities (handle, MONO_W32HANDLE_CAP_OWN)) {
		if (own_if_owned (handle, &statuscode)) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: handle %p already owned",
				__func__, handle);

			ret = statuscode == WAIT_ABANDONED_0 ? MONO_W32HANDLE_WAIT_RET_ABANDONED_0 : MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
			goto done;
		}
	}

	if (timeout != INFINITE)
		start = mono_msec_ticks ();

	for (;;) {
		gint waited;

		if (own_if_signalled (handle, &statuscode)) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: handle %p signalled",
				__func__, handle);

			ret = statuscode == WAIT_ABANDONED_0 ? MONO_W32HANDLE_WAIT_RET_ABANDONED_0 : MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
			goto done;
		}

		mono_w32handle_ops_prewait (handle);

		if (timeout == INFINITE) {
			waited = mono_w32handle_timedwait_signal_handle (handle, INFINITE, FALSE, alertable ? &alerted : NULL);
		} else {
			gint64 elapsed;

			elapsed = mono_msec_ticks () - start;
			if (elapsed > timeout) {
				ret = MONO_W32HANDLE_WAIT_RET_TIMEOUT;
				goto done;
			}

			waited = mono_w32handle_timedwait_signal_handle (handle, timeout - elapsed, FALSE, alertable ? &alerted : NULL);
		}

		if (alerted) {
			ret = MONO_W32HANDLE_WAIT_RET_ALERTED;
			goto done;
		}

		if (waited != 0) {
			ret = MONO_W32HANDLE_WAIT_RET_TIMEOUT;
			goto done;
		}
	}

done:
	thr_ret = mono_w32handle_unlock_handle (handle);
	g_assert (thr_ret == 0);

	return ret;
}

MonoW32HandleWaitRet
mono_w32handle_wait_multiple (gpointer *handles, gsize nhandles, gboolean waitall, guint32 timeout, gboolean alertable)
{
	MonoW32HandleWaitRet ret;
	gboolean alerted, poll;
	gint i, thr_ret;
	gint64 start;
	gpointer handles_sorted [MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS];
	guint32 statuscodes [MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS] = {0};

	if (nhandles == 0)
		return MONO_W32HANDLE_WAIT_RET_FAILED;

	if (nhandles == 1)
		return mono_w32handle_wait_one (handles [0], timeout, alertable);

	alerted = FALSE;

	if (nhandles > MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: too many handles: %zd",
			__func__, nhandles);

		return MONO_W32HANDLE_WAIT_RET_FAILED;
	}

	for (i = 0; i < nhandles; ++i) {
		if (!mono_w32handle_test_capabilities (handles[i], MONO_W32HANDLE_CAP_WAIT)
			 && !mono_w32handle_test_capabilities (handles[i], MONO_W32HANDLE_CAP_SPECIAL_WAIT))
		{
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: handle %p can't be waited for",
				   __func__, handles [i]);

			return MONO_W32HANDLE_WAIT_RET_FAILED;
		}

		handles_sorted [i] = handles [i];
	}

	qsort (handles_sorted, nhandles, sizeof (gpointer), g_direct_equal);
	for (i = 1; i < nhandles; ++i) {
		if (handles_sorted [i - 1] == handles_sorted [i]) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: handle %p is duplicated",
				__func__, handles_sorted [i]);

			return MONO_W32HANDLE_WAIT_RET_FAILED;
		}
	}

	poll = FALSE;
	for (i = 0; i < nhandles; ++i) {
		if (mono_w32handle_get_type (handles [i]) == MONO_W32HANDLE_PROCESS) {
			/* Can't wait for a process handle + another handle without polling */
			poll = TRUE;
		}
	}

	if (timeout != INFINITE)
		start = mono_msec_ticks ();

	for (i = 0; i < nhandles; ++i) {
		/* Add a reference, as we need to ensure the handle wont
		 * disappear from under us while we're waiting in the loop
		 * (not lock, as we don't want exclusive access here) */
		mono_w32handle_ref (handles [i]);
	}

	for (;;) {
		gsize count, lowest;
		gboolean signalled;
		gint waited;

		count = 0;
		lowest = nhandles;

		mono_w32handle_lock_handles (handles, nhandles);

		for (i = 0; i < nhandles; i++) {
			if ((mono_w32handle_test_capabilities (handles [i], MONO_W32HANDLE_CAP_OWN) && mono_w32handle_ops_isowned (handles [i]))
				 || mono_w32handle_issignalled (handles [i]))
			{
				count ++;

				if (i < lowest)
					lowest = i;
			}
		}

		signalled = (waitall && count == nhandles) || (!waitall && count > 0);

		if (signalled) {
			for (i = 0; i < nhandles; i++)
				own_if_signalled (handles [i], &statuscodes [i]);
		}

		mono_w32handle_unlock_handles (handles, nhandles);

		if (signalled) {
			ret = MONO_W32HANDLE_WAIT_RET_SUCCESS_0 + lowest;
			for (i = lowest; i < nhandles; i++) {
				if (statuscodes [i] == WAIT_ABANDONED_0) {
					ret = MONO_W32HANDLE_WAIT_RET_ABANDONED_0 + lowest;
					break;
				}
			}
			goto done;
		}

		for (i = 0; i < nhandles; i++) {
			mono_w32handle_ops_prewait (handles[i]);

			if (mono_w32handle_test_capabilities (handles [i], MONO_W32HANDLE_CAP_SPECIAL_WAIT)
				 && !mono_w32handle_issignalled (handles [i]))
			{
				mono_w32handle_ops_specialwait (handles [i], 0, alertable ? &alerted : NULL);
			}
		}

		thr_ret = mono_w32handle_lock_signal_mutex ();
		g_assert (thr_ret == 0);

		if (waitall) {
			signalled = TRUE;
			for (i = 0; i < nhandles; ++i) {
				if (!mono_w32handle_issignalled (handles [i])) {
					signalled = FALSE;
					break;
				}
			}
		} else {
			signalled = FALSE;
			for (i = 0; i < nhandles; ++i) {
				if (mono_w32handle_issignalled (handles [i])) {
					signalled = TRUE;
					break;
				}
			}
		}

		waited = 0;

		if (!signalled) {
			if (timeout == INFINITE) {
				waited = mono_w32handle_timedwait_signal (INFINITE, poll, alertable ? &alerted : NULL);
			} else {
				gint64 elapsed;

				elapsed = mono_msec_ticks () - start;
				if (elapsed > timeout) {
					ret = MONO_W32HANDLE_WAIT_RET_TIMEOUT;

					thr_ret = mono_w32handle_unlock_signal_mutex ();
					g_assert (thr_ret == 0);

					goto done;
				}

				waited = mono_w32handle_timedwait_signal (timeout - elapsed, poll, alertable ? &alerted : NULL);
			}
		}

		thr_ret = mono_w32handle_unlock_signal_mutex ();
		g_assert (thr_ret == 0);

		if (alerted) {
			ret = MONO_W32HANDLE_WAIT_RET_ALERTED;
			goto done;
		}

		if (waited != 0) {
			ret = MONO_W32HANDLE_WAIT_RET_TIMEOUT;
			goto done;
		}
	}

done:
	for (i = 0; i < nhandles; i++) {
		/* Unref everything we reffed above */
		mono_w32handle_unref (handles [i]);
	}

	return ret;
}

MonoW32HandleWaitRet
mono_w32handle_signal_and_wait (gpointer signal_handle, gpointer wait_handle, guint32 timeout, gboolean alertable)
{
	MonoW32HandleWaitRet ret;
	gint64 start;
	gboolean alerted;
	gint thr_ret;
	guint32 statuscode = 0;

	alerted = FALSE;

	if (!mono_w32handle_test_capabilities (signal_handle, MONO_W32HANDLE_CAP_SIGNAL))
		return MONO_W32HANDLE_WAIT_RET_FAILED;
	if (!mono_w32handle_test_capabilities (wait_handle, MONO_W32HANDLE_CAP_WAIT))
		return MONO_W32HANDLE_WAIT_RET_FAILED;

	if (mono_w32handle_test_capabilities (wait_handle, MONO_W32HANDLE_CAP_SPECIAL_WAIT)) {
		g_warning ("%s: handle %p has special wait, implement me!!", __func__, wait_handle);
		return MONO_W32HANDLE_WAIT_RET_FAILED;
	}

	thr_ret = mono_w32handle_lock_handle (wait_handle);
	g_assert (thr_ret == 0);

	mono_w32handle_ops_signal (signal_handle);

	if (mono_w32handle_test_capabilities (wait_handle, MONO_W32HANDLE_CAP_OWN)) {
		if (own_if_owned (wait_handle, &statuscode)) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: handle %p already owned",
				__func__, wait_handle);

			ret = statuscode == WAIT_ABANDONED_0 ? MONO_W32HANDLE_WAIT_RET_ABANDONED_0 : MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
			goto done;
		}
	}

	if (timeout != INFINITE)
		start = mono_msec_ticks ();

	for (;;) {
		gint waited;

		if (own_if_signalled (wait_handle, &statuscode)) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_W32HANDLE, "%s: handle %p signalled",
				__func__, wait_handle);

			ret = statuscode == WAIT_ABANDONED_0 ? MONO_W32HANDLE_WAIT_RET_ABANDONED_0 : MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
			goto done;
		}

		mono_w32handle_ops_prewait (wait_handle);

		if (timeout == INFINITE) {
			waited = mono_w32handle_timedwait_signal_handle (wait_handle, INFINITE, FALSE, alertable ? &alerted : NULL);
		} else {
			gint64 elapsed;

			elapsed = mono_msec_ticks () - start;
			if (elapsed > timeout) {
				ret = MONO_W32HANDLE_WAIT_RET_TIMEOUT;
				goto done;
			}

			waited = mono_w32handle_timedwait_signal_handle (wait_handle, timeout - elapsed, FALSE, alertable ? &alerted : NULL);
		}

		if (alerted) {
			ret = MONO_W32HANDLE_WAIT_RET_ALERTED;
			goto done;
		}

		if (waited != 0) {
			ret = MONO_W32HANDLE_WAIT_RET_TIMEOUT;
			goto done;
		}
	}

done:
	thr_ret = mono_w32handle_unlock_handle (wait_handle);
	g_assert (thr_ret == 0);

	return ret;
}

#endif /* !defined(HOST_WIN32) */
