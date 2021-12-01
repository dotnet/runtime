/**
 * \file
 * Generic and internal operations on handles
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
#include <glib.h>
#include "w32handle.h"
#include "utils/atomic.h"
#include "utils/mono-logger-internals.h"
#include "utils/mono-proclib.h"
#include "utils/mono-threads.h"
#include "utils/mono-time.h"
#include "utils/mono-error-internals.h"

#undef DEBUG_REFS

#define HANDLES_PER_SLOT 240

typedef struct _MonoW32HandleSlot MonoW32HandleSlot;
struct _MonoW32HandleSlot {
	MonoW32HandleSlot *next;
	MonoW32Handle handles[HANDLES_PER_SLOT];
};

static MonoW32HandleCapability handle_caps [MONO_W32TYPE_COUNT];
static MonoW32HandleOps const *handle_ops [MONO_W32TYPE_COUNT];

static MonoW32HandleSlot *handles_slots_first;
static MonoW32HandleSlot *handles_slots_last;

/*
 * This is an internal handle which is used for handling waiting for multiple handles.
 * Threads which wait for multiple handles wait on this one handle, and when a handle
 * is signalled, this handle is signalled too.
 */
static MonoCoopMutex global_signal_mutex;
static MonoCoopCond global_signal_cond;

static MonoCoopMutex scan_mutex;

static gboolean shutting_down;

static const gchar*
mono_w32handle_ops_typename (MonoW32Type type);

const gchar*
mono_w32handle_get_typename (MonoW32Type type)
{
	return mono_w32handle_ops_typename (type);
}

void
mono_w32handle_set_signal_state (MonoW32Handle *handle_data, gboolean state, gboolean broadcast)
{
#ifdef DEBUG
	g_message ("%s: setting state of %p to %s (broadcast %s)", __func__,
		   handle, state?"TRUE":"FALSE", broadcast?"TRUE":"FALSE");
#endif

	if (state) {
		/* Tell everyone blocking on a single handle */

		/* The condition the global signal cond is waiting on is the signalling of
		 * _any_ handle. So lock it before setting the signalled state.
		 */
		mono_coop_mutex_lock (&global_signal_mutex);

		/* This function _must_ be called with
		 * handle->signal_mutex locked
		 */
		handle_data->signalled = TRUE;

		if (broadcast)
			mono_coop_cond_broadcast (&handle_data->signal_cond);
		else
			mono_coop_cond_signal (&handle_data->signal_cond);

		/* Tell everyone blocking on multiple handles that something
		 * was signalled
		 */
		mono_coop_cond_broadcast (&global_signal_cond);

		mono_coop_mutex_unlock (&global_signal_mutex);
	} else {
		handle_data->signalled = FALSE;
	}
}

gboolean
mono_w32handle_issignalled (MonoW32Handle *handle_data)
{
	return handle_data->signalled;
}

static void
mono_w32handle_set_in_use (MonoW32Handle *handle_data, gboolean in_use)
{
	handle_data->in_use = in_use;
}

static void
mono_w32handle_lock_signal_mutex (void)
{
#ifdef DEBUG
	g_message ("%s: lock global signal mutex", __func__);
#endif

	mono_coop_mutex_lock (&global_signal_mutex);
}

static void
mono_w32handle_unlock_signal_mutex (void)
{
#ifdef DEBUG
	g_message ("%s: unlock global signal mutex", __func__);
#endif

	mono_coop_mutex_unlock (&global_signal_mutex);
}

void
mono_w32handle_lock (MonoW32Handle *handle_data)
{
	mono_coop_mutex_lock (&handle_data->signal_mutex);
}

void
mono_w32handle_unlock (MonoW32Handle *handle_data)
{
	mono_coop_mutex_unlock (&handle_data->signal_mutex);
}

void
mono_w32handle_init (void)
{
	static gboolean initialized = FALSE;

	if (initialized)
		return;

	mono_coop_mutex_init (&scan_mutex);

	mono_coop_cond_init (&global_signal_cond);
	mono_coop_mutex_init (&global_signal_mutex);

	handles_slots_first = handles_slots_last = g_new0 (MonoW32HandleSlot, 1);

	initialized = TRUE;
}

static gsize
mono_w32handle_ops_typesize (MonoW32Type type);

/*
 * mono_w32handle_new_internal:
 * @type: Init handle to this type
 *
 * Search for a free handle and initialize it. Return the handle on
 * success and 0 on failure.  This is only called from
 * mono_w32handle_new, and scan_mutex must be held.
 */
static MonoW32Handle*
mono_w32handle_new_internal (MonoW32Type type, gpointer handle_specific)
{
	static MonoW32HandleSlot *slot_last = NULL;
	static guint32 index_last = 0;
	MonoW32HandleSlot *slot;
	guint32 index;
	gboolean retried;

	if (!slot_last) {
		slot_last = handles_slots_first;
		g_assert (slot_last);
	}

	/* A linear scan should be fast enough. Start from the last allocation, assuming that handles are allocated more
	 * often than they're freed. */

retry_from_beginning:
	retried = FALSE;

	slot = slot_last;
	g_assert (slot);

	index = index_last;
	g_assert (index >= 0);
	g_assert (index <= HANDLES_PER_SLOT);

retry:
	for(; slot; slot = slot->next) {
		for (; index < HANDLES_PER_SLOT; index++) {
			MonoW32Handle *handle_data = &slot->handles [index];

			if (handle_data->type == MONO_W32TYPE_UNUSED) {
				slot_last = slot;
				index_last = index + 1;

				g_assert (handle_data->ref == 0);

				handle_data->type = type;
				handle_data->signalled = FALSE;
				handle_data->ref = 1;

				mono_coop_cond_init (&handle_data->signal_cond);
				mono_coop_mutex_init (&handle_data->signal_mutex);

				if (handle_specific)
					handle_data->specific = g_memdup (handle_specific, mono_w32handle_ops_typesize (type));

				return handle_data;
			}
		}
		index = 0;
	}

	if (!retried) {
		/* Try again from the beginning */
		slot = handles_slots_first;
		index = 0;
		retried = TRUE;
		goto retry;
	}

	handles_slots_last = (handles_slots_last->next = g_new0 (MonoW32HandleSlot, 1));
	goto retry_from_beginning;

	/* We already went around and didn't find a slot, so let's put ourselves on the empty slot we just allocated */
	slot_last = handles_slots_last;
	index_last = 0;
}

gpointer
mono_w32handle_new (MonoW32Type type, gpointer handle_specific)
{
	MonoW32Handle *handle_data;

	g_assert (!shutting_down);

	mono_coop_mutex_lock (&scan_mutex);

	handle_data = mono_w32handle_new_internal (type, handle_specific);
	g_assert (handle_data);

	mono_coop_mutex_unlock (&scan_mutex);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: create %s handle %p", __func__, mono_w32handle_ops_typename (type), handle_data);

	return (gpointer) handle_data;
}

static gboolean
mono_w32handle_ref_core (MonoW32Handle *handle_data);

static gboolean
mono_w32handle_unref_core (MonoW32Handle *handle_data);

static void
w32handle_destroy (MonoW32Handle *handle_data);

gpointer
mono_w32handle_duplicate (MonoW32Handle *handle_data)
{
	if (!mono_w32handle_ref_core (handle_data))
		g_error ("%s: unknown handle %p", __func__, handle_data);

	return (gpointer) handle_data;
}

gboolean
mono_w32handle_close (gpointer handle)
{
	MonoW32Handle *handle_data;
	gboolean destroy;

	if (handle == INVALID_HANDLE_VALUE)
		return FALSE;

	handle_data = (MonoW32Handle*) handle;

	if (handle_data->type == MONO_W32TYPE_UNUSED)
		return FALSE;

	destroy = mono_w32handle_unref_core (handle_data);
	if (destroy)
		w32handle_destroy (handle_data);

	return TRUE;
}

gboolean
mono_w32handle_lookup_and_ref (gpointer handle, MonoW32Handle **handle_data)
{
	g_assert (handle_data);

	if (handle == INVALID_HANDLE_VALUE)
		return FALSE;

	*handle_data = (MonoW32Handle*) handle;

	if (!mono_w32handle_ref_core (*handle_data))
		return FALSE;

	if ((*handle_data)->type == MONO_W32TYPE_UNUSED) {
		mono_w32handle_unref_core (*handle_data);
		return FALSE;
	}

	return TRUE;
}

static gboolean
mono_w32handle_ref_core (MonoW32Handle *handle_data)
{
	guint old, new_;

	do {
		old = handle_data->ref;
		if (old == 0)
			return FALSE;

		new_ = old + 1;
	} while (mono_atomic_cas_i32 ((gint32*) &handle_data->ref, (gint32)new_, (gint32)old) != (gint32)old);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: ref %s handle %p, ref: %d -> %d",
		__func__, mono_w32handle_ops_typename (handle_data->type), handle_data, old, new_);

	return TRUE;
}

static gboolean
mono_w32handle_unref_core (MonoW32Handle *handle_data)
{
	MonoW32Type type;
	guint old, new_;

	type = handle_data->type;

	do {
		old = handle_data->ref;
		if (!(old >= 1))
			g_error ("%s: handle %p has ref %d, it should be >= 1", __func__, handle_data, old);

		new_ = old - 1;
	} while (mono_atomic_cas_i32 ((gint32*) &handle_data->ref, (gint32)new_, (gint32)old) != (gint32)old);

	/* handle_data might contain invalid data from now on, if
	 * another thread is unref'ing this handle at the same time */

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: unref %s handle %p, ref: %d -> %d destroy: %s",
		__func__, mono_w32handle_ops_typename (type), handle_data, old, new_, new_ == 0 ? "true" : "false");

	return new_ == 0;
}

static void
mono_w32handle_ops_close (MonoW32Type type, gpointer handle_specific);

static void
w32handle_destroy (MonoW32Handle *handle_data)
{
	/* Need to copy the handle info, reset the slot in the
	 * array, and _only then_ call the close function to
	 * avoid race conditions (eg file descriptors being
	 * closed, and another file being opened getting the
	 * same fd racing the memset())
	 */
	MonoW32Type type;
	gpointer handle_specific;

	g_assert (!handle_data->in_use);

	type = handle_data->type;
	handle_specific = handle_data->specific;

	mono_coop_mutex_lock (&scan_mutex);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: destroy %s handle %p", __func__, mono_w32handle_ops_typename (type), handle_data);

	mono_coop_mutex_destroy (&handle_data->signal_mutex);
	mono_coop_cond_destroy (&handle_data->signal_cond);

	memset (handle_data, 0, sizeof (MonoW32Handle));

	mono_coop_mutex_unlock (&scan_mutex);

	mono_w32handle_ops_close (type, handle_specific);

	memset (handle_specific, 0, mono_w32handle_ops_typesize (type));

	g_free (handle_specific);
}

/* The handle must not be locked on entry to this function */
void
mono_w32handle_unref (MonoW32Handle *handle_data)
{
	gboolean destroy;

	destroy = mono_w32handle_unref_core (handle_data);
	if (destroy)
		w32handle_destroy (handle_data);
}

void
mono_w32handle_register_ops (MonoW32Type type, const MonoW32HandleOps *ops)
{
	handle_ops [type] = ops;
}

void
mono_w32handle_register_capabilities (MonoW32Type type, MonoW32HandleCapability caps)
{
	handle_caps[type] = caps;
}

static gboolean
mono_w32handle_test_capabilities (MonoW32Handle *handle_data, MonoW32HandleCapability caps)
{
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: testing 0x%x against 0x%x (%d)", __func__,
		   handle_caps[handle_data->type], caps, handle_caps[handle_data->type] & caps);

	return (handle_caps [handle_data->type] & caps) != 0;
}

static void
mono_w32handle_ops_close (MonoW32Type type, gpointer data)
{
	const MonoW32HandleOps *ops = handle_ops [type];
	if (ops && ops->close)
		ops->close (data);
}

static void
mono_w32handle_ops_details (MonoW32Handle *handle_data)
{
	if (handle_ops [handle_data->type] && handle_ops [handle_data->type]->details != NULL)
		handle_ops [handle_data->type]->details (handle_data);
}

static const gchar*
mono_w32handle_ops_typename (MonoW32Type type)
{
	g_assert (handle_ops [type]);
	g_assert (handle_ops [type]->type_name);
	return handle_ops [type]->type_name ();
}

static gsize
mono_w32handle_ops_typesize (MonoW32Type type)
{
	g_assert (handle_ops [type]);
	g_assert (handle_ops [type]->typesize);
	return handle_ops [type]->typesize ();
}

static gint32
mono_w32handle_ops_signal (MonoW32Handle *handle_data)
{
	if (handle_ops [handle_data->type] && handle_ops [handle_data->type]->signal)
		return handle_ops [handle_data->type]->signal (handle_data);

	return MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
}

static gboolean
mono_w32handle_ops_own (MonoW32Handle *handle_data, gboolean *abandoned)
{
	if (handle_ops [handle_data->type] && handle_ops [handle_data->type]->own_handle)
		return handle_ops [handle_data->type]->own_handle (handle_data, abandoned);

	return FALSE;
}

static gboolean
mono_w32handle_ops_isowned (MonoW32Handle *handle_data)
{
	if (handle_ops [handle_data->type] && handle_ops [handle_data->type]->is_owned)
		return handle_ops [handle_data->type]->is_owned (handle_data);

	return FALSE;
}

static MonoW32HandleWaitRet
mono_w32handle_ops_specialwait (MonoW32Handle *handle_data, guint32 timeout, gboolean *alerted)
{
	if (handle_ops [handle_data->type] && handle_ops [handle_data->type]->special_wait)
		return handle_ops [handle_data->type]->special_wait (handle_data, timeout, alerted);

	return MONO_W32HANDLE_WAIT_RET_FAILED;
}

static void
mono_w32handle_ops_prewait (MonoW32Handle *handle_data)
{
	if (handle_ops [handle_data->type] && handle_ops [handle_data->type]->prewait)
		handle_ops [handle_data->type]->prewait (handle_data);
}

static void
mono_w32handle_unlock_handles (MonoW32Handle **handles_data, gsize nhandles)
{
	gint i;

	for (i = nhandles - 1; i >= 0; i--) {
		if (!handles_data [i])
			continue;
		mono_w32handle_unlock (handles_data [i]);
	}
}

static int
mono_w32handle_timedwait_signal_naked (MonoCoopCond *cond, MonoCoopMutex *mutex, guint32 timeout, gboolean poll, gboolean *alerted)
{
	int res;

	if (!poll) {
		res = mono_coop_cond_timedwait (cond, mutex, timeout);
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
			res = mono_coop_cond_timedwait (cond, mutex, timeout);
		} else {
			if (timeout < 100) {
				/* Real timeout is less than 100ms time */
				res = mono_coop_cond_timedwait (cond, mutex, timeout);
			} else {
				res = mono_coop_cond_timedwait (cond, mutex, 100);

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
	mono_coop_mutex_lock (&global_signal_mutex);
	mono_coop_cond_broadcast (&global_signal_cond);
	mono_coop_mutex_unlock (&global_signal_mutex);
}

static int
mono_w32handle_timedwait_signal (guint32 timeout, gboolean poll, gboolean *alerted)
{
	int res;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: waiting for global", __func__);

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
signal_handle_and_unref (gpointer handle_duplicate)
{
	MonoW32Handle *handle_data;
	MonoCoopCond *cond;
	MonoCoopMutex *mutex;

	if (!mono_w32handle_lookup_and_ref (handle_duplicate, &handle_data))
		g_error ("%s: unknown handle %p", __func__, handle_duplicate);

	/* If we reach here, then interrupt token is set to the flag value, which
	 * means that the target thread is either
	 * - before the first CAS in timedwait, which means it won't enter the wait.
	 * - it is after the first CAS, so it is already waiting, or it will enter
	 *    the wait, and it will be interrupted by the broadcast. */
	cond = &handle_data->signal_cond;
	mutex = &handle_data->signal_mutex;

	mono_coop_mutex_lock (mutex);
	mono_coop_cond_broadcast (cond);
	mono_coop_mutex_unlock (mutex);

	mono_w32handle_unref (handle_data);

	mono_w32handle_close (handle_duplicate);
}

static int
mono_w32handle_timedwait_signal_handle (MonoW32Handle *handle_data, guint32 timeout, gboolean poll, gboolean *alerted)
{
	gpointer handle_duplicate;
	int res;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: waiting for %p (type %s)", __func__, handle_data,
		   mono_w32handle_ops_typename (handle_data->type));

	if (alerted)
		*alerted = FALSE;

	if (alerted) {
		mono_thread_info_install_interrupt (signal_handle_and_unref, handle_duplicate = mono_w32handle_duplicate (handle_data), alerted);
		if (*alerted) {
			mono_w32handle_close (handle_duplicate);
			return 0;
		}
	}

	res = mono_w32handle_timedwait_signal_naked (&handle_data->signal_cond, &handle_data->signal_mutex, timeout, poll, alerted);

	if (alerted) {
		mono_thread_info_uninstall_interrupt (alerted);
		if (!*alerted) {
			/* if it is alerted, then the handle_duplicate is closed in the interrupt callback */
			mono_w32handle_close (handle_duplicate);
		}
	}

	return res;
}

static gboolean
own_if_signalled (MonoW32Handle *handle_data, gboolean *abandoned)
{
	if (!mono_w32handle_issignalled (handle_data))
		return FALSE;

	*abandoned = FALSE;
	mono_w32handle_ops_own (handle_data, abandoned);
	return TRUE;
}

static gboolean
own_if_owned (MonoW32Handle *handle_data, gboolean *abandoned)
{
	if (!mono_w32handle_ops_isowned (handle_data))
		return FALSE;

	*abandoned = FALSE;
	mono_w32handle_ops_own (handle_data, abandoned);
	return TRUE;
}

#ifdef HOST_WIN32
MonoW32HandleWaitRet
mono_w32handle_wait_one (gpointer handle, guint32 timeout, gboolean alertable)
{
	return mono_w32handle_convert_wait_ret (mono_coop_win32_wait_for_single_object_ex (handle, timeout, alertable), 1);
}
#else
MonoW32HandleWaitRet
mono_w32handle_wait_one (gpointer handle, guint32 timeout, gboolean alertable)
{
	MonoW32Handle *handle_data;
	MonoW32HandleWaitRet ret;
	gboolean alerted;
	gint64 start = 0;
	gboolean abandoned = FALSE;

	alerted = FALSE;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data))
		return MONO_W32HANDLE_WAIT_RET_FAILED;

	if (mono_w32handle_test_capabilities (handle_data, MONO_W32HANDLE_CAP_SPECIAL_WAIT)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: handle %p has special wait", __func__, handle_data);

		mono_w32handle_unref (handle_data);
		return mono_w32handle_ops_specialwait (handle_data, timeout, alertable ? &alerted : NULL);
	}

	if (!mono_w32handle_test_capabilities (handle_data, MONO_W32HANDLE_CAP_WAIT)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: handle %p can't be waited for", __func__, handle_data);

		mono_w32handle_unref (handle_data);
		return MONO_W32HANDLE_WAIT_RET_FAILED;
	}

	mono_w32handle_lock (handle_data);

	if (mono_w32handle_test_capabilities (handle_data, MONO_W32HANDLE_CAP_OWN)) {
		if (own_if_owned (handle_data, &abandoned)) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: handle %p already owned", __func__, handle_data);

			ret = abandoned ? MONO_W32HANDLE_WAIT_RET_ABANDONED_0 : MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
			goto done;
		}
	}

	if (timeout != MONO_INFINITE_WAIT)
		start = mono_msec_ticks ();

	mono_w32handle_set_in_use (handle_data, TRUE);

	for (;;) {
		gint waited;

		if (own_if_signalled (handle_data, &abandoned)) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: handle %p signalled", __func__, handle_data);

			ret = abandoned ? MONO_W32HANDLE_WAIT_RET_ABANDONED_0 : MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
			goto done;
		}

		mono_w32handle_ops_prewait (handle_data);

		if (timeout == MONO_INFINITE_WAIT) {
			waited = mono_w32handle_timedwait_signal_handle (handle_data, MONO_INFINITE_WAIT, FALSE, alertable ? &alerted : NULL);
		} else {
			gint64 elapsed;

			elapsed = mono_msec_ticks () - start;
			if (elapsed > timeout) {
				ret = MONO_W32HANDLE_WAIT_RET_TIMEOUT;
				goto done;
			}

			waited = mono_w32handle_timedwait_signal_handle (handle_data, timeout - elapsed, FALSE, alertable ? &alerted : NULL);
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
	mono_w32handle_set_in_use (handle_data, FALSE);

	mono_w32handle_unlock (handle_data);

	mono_w32handle_unref (handle_data);

	return ret;
}
#endif /* HOST_WIN32 */
