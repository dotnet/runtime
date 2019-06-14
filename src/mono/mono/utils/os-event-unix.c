/**
 * \file
 * MonoOSEvent on Unix
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "os-event.h"

#include "atomic.h"
#include "mono-lazy-init.h"
#include "mono-threads.h"
#include "mono-time.h"

static mono_lazy_init_t status = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;

static mono_mutex_t signal_mutex;

static void
initialize (void)
{
	mono_os_mutex_init (&signal_mutex);
}

void
mono_os_event_init (MonoOSEvent *event, gboolean initial)
{
	g_assert (event);

	mono_lazy_initialize (&status, initialize);

	event->conds = g_ptr_array_new ();
	event->signalled = initial;
}

void
mono_os_event_destroy (MonoOSEvent *event)
{
	g_assert (mono_lazy_is_initialized (&status));

	g_assert (event);

	if (event->conds->len > 0)
		g_error ("%s: cannot destroy osevent, there are still %d threads waiting on it", __func__, event->conds->len);

	g_ptr_array_free (event->conds, TRUE);
}

static gboolean
mono_os_event_is_signalled (MonoOSEvent *event)
{
	return event->signalled;
}

void
mono_os_event_set (MonoOSEvent *event)
{
	gsize i;

	g_assert (mono_lazy_is_initialized (&status));

	g_assert (event);

	mono_os_mutex_lock (&signal_mutex);

	event->signalled = TRUE;

	for (i = 0; i < event->conds->len; ++i)
		mono_os_cond_signal ((mono_cond_t*) event->conds->pdata [i]);

	mono_os_mutex_unlock (&signal_mutex);
}

void
mono_os_event_reset (MonoOSEvent *event)
{
	g_assert (mono_lazy_is_initialized (&status));

	g_assert (event);

	mono_os_mutex_lock (&signal_mutex);

	event->signalled = FALSE;

	mono_os_mutex_unlock (&signal_mutex);
}

MonoOSEventWaitRet
mono_os_event_wait_one (MonoOSEvent *event, guint32 timeout, gboolean alertable)
{
	return mono_os_event_wait_multiple (&event, 1, TRUE, timeout, alertable);
}

typedef struct {
	guint32 ref;
	MonoOSEvent event;
} OSEventWaitData;

static void
signal_and_unref (gpointer user_data)
{
	OSEventWaitData *data;

	data = (OSEventWaitData*) user_data;

	mono_os_event_set (&data->event);
	if (mono_atomic_dec_i32 ((gint32*) &data->ref) == 0) {
		mono_os_event_destroy (&data->event);
		g_free (data);
	}
}

MonoOSEventWaitRet
mono_os_event_wait_multiple (MonoOSEvent **events, gsize nevents, gboolean waitall, guint32 timeout, gboolean alertable)
{
	MonoOSEventWaitRet ret;
	mono_cond_t signal_cond;
	OSEventWaitData *data = NULL;
	gboolean alerted;
	gint64 start = 0;
	gint i;

	g_assert (mono_lazy_is_initialized (&status));

	g_assert (events);
	g_assert (nevents > 0);
	g_assert (nevents <= MONO_OS_EVENT_WAIT_MAXIMUM_OBJECTS);

	for (i = 0; i < nevents; ++i)
		g_assert (events [i]);

	if (alertable) {
		data = g_new0 (OSEventWaitData, 1);
		data->ref = 2;
		mono_os_event_init (&data->event, FALSE);

		alerted = FALSE;
		mono_thread_info_install_interrupt (signal_and_unref, data, &alerted);
		if (alerted) {
			mono_os_event_destroy (&data->event);
			g_free (data);
			return MONO_OS_EVENT_WAIT_RET_ALERTED;
		}
	}

	if (timeout != MONO_INFINITE_WAIT)
		start = mono_msec_ticks ();

	mono_os_cond_init (&signal_cond);

	mono_os_mutex_lock (&signal_mutex);

	for (i = 0; i < nevents; ++i)
		g_ptr_array_add (events [i]->conds, &signal_cond);

	if (alertable)
		g_ptr_array_add (data->event.conds, &signal_cond);

	for (;;) {
		gint count, lowest;
		gboolean signalled;

		count = 0;
		lowest = -1;

		for (i = 0; i < nevents; ++i) {
			if (mono_os_event_is_signalled (events [i])) {
				count += 1;
				if (lowest == -1)
					lowest = i;
			}
		}

		if (alertable && mono_os_event_is_signalled (&data->event))
			signalled = TRUE;
		else if (waitall)
			signalled = (count == nevents);
		else /* waitany */
			signalled = (count > 0);

		if (signalled) {
			ret = (MonoOSEventWaitRet)(MONO_OS_EVENT_WAIT_RET_SUCCESS_0 + lowest);
			goto done;
		}

		if (timeout == MONO_INFINITE_WAIT) {
			mono_os_cond_wait (&signal_cond, &signal_mutex);
		} else {
			gint64 elapsed;
			gint res;

			elapsed = mono_msec_ticks () - start;
			if (elapsed >= timeout) {
				ret = MONO_OS_EVENT_WAIT_RET_TIMEOUT;
				goto done;
			}

			res = mono_os_cond_timedwait (&signal_cond, &signal_mutex, timeout - elapsed);
			if (res != 0) {
				ret = MONO_OS_EVENT_WAIT_RET_TIMEOUT;
				goto done;
			}
		}
	}

done:
	for (i = 0; i < nevents; ++i)
		g_ptr_array_remove (events [i]->conds, &signal_cond);

	if (alertable)
		g_ptr_array_remove (data->event.conds, &signal_cond);

	mono_os_mutex_unlock (&signal_mutex);

	mono_os_cond_destroy (&signal_cond);

	if (alertable) {
		mono_thread_info_uninstall_interrupt (&alerted);
		if (alerted) {
			if (mono_atomic_dec_i32 ((gint32*) &data->ref) == 0) {
				mono_os_event_destroy (&data->event);
				g_free (data);
			}
			return MONO_OS_EVENT_WAIT_RET_ALERTED;
		}

		mono_os_event_destroy (&data->event);
		g_free (data);
	}

	return ret;
}
