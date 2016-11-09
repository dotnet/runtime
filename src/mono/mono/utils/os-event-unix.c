/*
 * os-event-unix.c: MonoOSEvent on Unix
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
static mono_cond_t signal_cond;

static void
initialize (void)
{
	mono_os_mutex_init (&signal_mutex);
	mono_os_cond_init (&signal_cond);
}

void
mono_os_event_init (MonoOSEvent *event, gboolean manual, gboolean initial)
{
	g_assert (event);

	mono_lazy_initialize (&status, initialize);

	mono_os_mutex_init (&event->mutex);
	mono_os_cond_init (&event->cond);
	event->signalled = initial;
	event->manual = manual;
	event->set_count = (initial && !manual) ? 1 : 0;
}

void
mono_os_event_destroy (MonoOSEvent *event)
{
	g_assert (mono_lazy_is_initialized (&status));

	g_assert (event);

	mono_os_mutex_destroy (&event->mutex);
	mono_os_cond_destroy (&event->cond);
}

static gboolean
mono_os_event_is_signalled (MonoOSEvent *event)
{
	return event->signalled;
}

static void
mono_os_event_signal (MonoOSEvent *event, gboolean broadcast)
{
	g_assert (event);

	mono_os_mutex_lock (&signal_mutex);

	event->signalled = TRUE;

	if (broadcast)
		mono_os_cond_broadcast (&event->cond);
	else
		mono_os_cond_signal (&event->cond);

	mono_os_cond_broadcast (&signal_cond);

	mono_os_mutex_unlock (&signal_mutex);
}

void
mono_os_event_set (MonoOSEvent *event)
{
	g_assert (mono_lazy_is_initialized (&status));

	g_assert (event);

	mono_os_mutex_lock (&event->mutex);

	if (event->manual) {
		mono_os_event_signal (event, TRUE);
	} else {
		event->set_count = 1;
		mono_os_event_signal (event, FALSE);
	}

	mono_os_mutex_unlock (&event->mutex);
}

void
mono_os_event_reset (MonoOSEvent *event)
{
	g_assert (mono_lazy_is_initialized (&status));

	g_assert (event);

	mono_os_mutex_lock (&event->mutex);

	if (mono_os_event_is_signalled (event))
		event->signalled = FALSE;

	event->set_count = 0;

	mono_os_mutex_unlock (&event->mutex);
}

static gboolean
mono_os_event_own (MonoOSEvent *event)
{
	g_assert (event);

	if (!mono_os_event_is_signalled (event))
		return FALSE;

	if (!event->manual) {
		g_assert (event->set_count > 0);
		event->set_count -= 1;

		if (event->set_count == 0)
			mono_os_event_signal (event, FALSE);
	}

	return TRUE;
}

MonoOSEventWaitRet
mono_os_event_wait_one (MonoOSEvent *event, guint32 timeout)
{
	return mono_os_event_wait_multiple (&event, 1, TRUE, timeout);
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
	if (InterlockedDecrement ((gint32*) &data->ref) == 0) {
		mono_os_event_destroy (&data->event);
		g_free (data);
	}
}

static void
mono_os_event_lock_events (MonoOSEvent **events, gsize nevents)
{
	gint i, j;

retry:
	for (i = 0; i < nevents; ++i) {
		gint res;

		res = mono_os_mutex_trylock (&events [i]->mutex);
		if (res != 0) {
			for (j = i - 1; j >= 0; j--)
				mono_os_mutex_unlock (&events [j]->mutex);

			mono_thread_info_yield ();

			goto retry;
		}
	}
}

static void
mono_os_event_unlock_events (MonoOSEvent **events, gsize nevents)
{
	gint i;

	for (i = 0; i < nevents; ++i)
		mono_os_mutex_unlock (&events [i]->mutex);
}

MonoOSEventWaitRet
mono_os_event_wait_multiple (MonoOSEvent **events, gsize nevents, gboolean waitall, guint32 timeout)
{
	MonoOSEventWaitRet ret;
	MonoOSEvent *innerevents [MONO_OS_EVENT_WAIT_MAXIMUM_OBJECTS + 1];
	OSEventWaitData *data;
	gboolean alerted;
	gint64 start;
	gint i;

	g_assert (mono_lazy_is_initialized (&status));

	g_assert (events);
	g_assert (nevents > 0);
	g_assert (nevents <= MONO_OS_EVENT_WAIT_MAXIMUM_OBJECTS);

	for (i = 0; i < nevents; ++i)
		g_assert (events [i]);

	memcpy (innerevents, events, sizeof (MonoOSEvent*) * nevents);

	data = g_new0 (OSEventWaitData, 1);
	data->ref = 2;
	mono_os_event_init (&data->event, TRUE, FALSE);

	innerevents [nevents ++] = &data->event;

	alerted = FALSE;
	mono_thread_info_install_interrupt (signal_and_unref, data, &alerted);
	if (alerted) {
		mono_os_event_destroy (&data->event);
		g_free (data);
		return MONO_OS_EVENT_WAIT_RET_ALERTED;
	}

	if (timeout != MONO_INFINITE_WAIT)
		start = mono_msec_ticks ();

	for (;;) {
		gint count, lowest;
		gboolean signalled;

		mono_os_event_lock_events (innerevents, nevents);

		count = 0;
		lowest = -1;

		for (i = 0; i < nevents - 1; ++i) {
			if (mono_os_event_is_signalled (innerevents [i])) {
				count += 1;
				if (lowest == -1)
					lowest = i;
			}
		}

		if (mono_os_event_is_signalled (&data->event))
			signalled = TRUE;
		else if (waitall)
			signalled = (count == nevents - 1);
		else /* waitany */
			signalled = (count > 0);

		if (signalled) {
			for (i = 0; i < nevents - 1; ++i)
				mono_os_event_own (innerevents [i]);
		}

		mono_os_event_unlock_events (innerevents, nevents);

		if (signalled) {
			ret = MONO_OS_EVENT_WAIT_RET_SUCCESS_0 + lowest;
			goto done;
		}

		mono_os_mutex_lock (&signal_mutex);

		if (mono_os_event_is_signalled (&data->event)) {
			signalled = TRUE;
		} else if (waitall) {
			signalled = TRUE;
			for (i = 0; i < nevents - 1; ++i) {
				if (!mono_os_event_is_signalled (innerevents [i])) {
					signalled = FALSE;
					break;
				}
			}
		} else {
			signalled = FALSE;
			for (i = 0; i < nevents - 1; ++i) {
				if (mono_os_event_is_signalled (innerevents [i])) {
					signalled = TRUE;
					break;
				}
			}
		}

		if (signalled) {
			mono_os_mutex_unlock (&signal_mutex);
			continue;
		}

		if (timeout == MONO_INFINITE_WAIT) {
			mono_os_cond_wait (&signal_cond, &signal_mutex);
		} else {
			gint64 elapsed;
			gint res;

			elapsed = mono_msec_ticks () - start;
			if (elapsed >= timeout) {
				mono_os_mutex_unlock (&signal_mutex);

				ret = MONO_OS_EVENT_WAIT_RET_TIMEOUT;
				goto done;
			}

			res = mono_os_cond_timedwait (&signal_cond, &signal_mutex, timeout - elapsed);
			if (res != 0) {
				mono_os_mutex_unlock (&signal_mutex);

				ret = MONO_OS_EVENT_WAIT_RET_TIMEOUT;
				goto done;
			}
		}

		mono_os_mutex_unlock (&signal_mutex);
	}

done:
	mono_thread_info_uninstall_interrupt (&alerted);
	if (alerted) {
		if (InterlockedDecrement ((gint32*) &data->ref) == 0) {
			mono_os_event_destroy (&data->event);
			g_free (data);
		}
		return MONO_OS_EVENT_WAIT_RET_ALERTED;
	}

	mono_os_event_destroy (&data->event);
	g_free (data);

	return ret;
}
