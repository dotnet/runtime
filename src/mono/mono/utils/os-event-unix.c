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

	if (event->signalled)
		event->signalled = FALSE;

	event->set_count = 0;

	mono_os_mutex_unlock (&event->mutex);
}

static gboolean
mono_os_event_own (MonoOSEvent *event)
{
	g_assert (event);

	if (!event->signalled)
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
	MonoOSEventWaitRet ret;
	gint64 start;

	g_assert (mono_lazy_is_initialized (&status));

	g_assert (event);

	mono_os_mutex_lock (&event->mutex);

	if (timeout != MONO_INFINITE_WAIT)
		start = mono_msec_ticks ();

	for (;;) {
		if (mono_os_event_own (event)) {
			ret = MONO_OS_EVENT_WAIT_RET_SUCCESS_0;
			goto done;
		}

		if (timeout == MONO_INFINITE_WAIT) {
			mono_os_cond_wait (&event->cond, &event->mutex);
		} else {
			gint64 elapsed;
			gint res;

			elapsed = mono_msec_ticks () - start;
			if (elapsed >= timeout) {
				ret = MONO_OS_EVENT_WAIT_RET_TIMEOUT;
				goto done;
			}

			res = mono_os_cond_timedwait (&event->cond, &event->mutex, timeout - elapsed);
			if (res != 0) {
				ret = MONO_OS_EVENT_WAIT_RET_TIMEOUT;
				goto done;
			}
		}
	}

done:
	mono_os_mutex_unlock (&event->mutex);

	return ret;
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
	gint64 start;
	gint i;

	g_assert (mono_lazy_is_initialized (&status));

	g_assert (events);
	g_assert (nevents > 0);
	g_assert (nevents <= MONO_OS_EVENT_WAIT_MAXIMUM_OBJECTS);

	if (nevents == 1)
		return mono_os_event_wait_one (events [0], timeout);

	for (i = 0; i < nevents; ++i) {
		g_assert (events [i]);
	}

	if (timeout != MONO_INFINITE_WAIT)
		start = mono_msec_ticks ();

	for (;;) {
		gint count, lowest;
		gboolean signalled;

		mono_os_event_lock_events (events, nevents);

		count = 0;
		lowest = -1;

		for (i = 0; i < nevents; ++i) {
			if (events [i]->signalled) {
				count += 1;
				if (lowest == -1)
					lowest = i;
			}
		}

		signalled = (waitall && count == nevents) || (!waitall && count > 0);

		if (signalled) {
			for (i = 0; i < nevents; ++i)
				mono_os_event_own (events [i]);
		}

		mono_os_event_unlock_events (events, nevents);

		if (signalled) {
			ret = MONO_OS_EVENT_WAIT_RET_SUCCESS_0 + lowest;
			goto done;
		}

		mono_os_mutex_lock (&signal_mutex);

		if (waitall) {
			signalled = TRUE;
			for (i = 0; i < nevents; ++i) {
				if (!events [i]->signalled) {
					signalled = FALSE;
					break;
				}
			}
		} else {
			signalled = FALSE;
			for (i = 0; i < nevents; ++i) {
				if (events [i]->signalled) {
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
	return ret;
}
