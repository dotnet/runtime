/**
 * \file
 * native threadpool worker
 *
 * Author:
 *	Ludovic Henry (ludovic.henry@xamarin.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <stdlib.h>
#define _USE_MATH_DEFINES // needed by MSVC to define math constants
#include <math.h>
#include <config.h>
#include <glib.h>

#include <mono/metadata/threadpool.h>
#include <mono/metadata/threadpool-worker.h>

static MonoThreadPoolWorkerCallback tp_cb;
static gboolean cb_scheduled;

void
mono_threadpool_worker_init (MonoThreadPoolWorkerCallback callback)
{
	tp_cb = callback;
}

void
mono_threadpool_worker_cleanup (void)
{
}

gint32
mono_threadpool_worker_get_min (void)
{
	return 1;
}

gboolean
mono_threadpool_worker_set_min (gint32 value)
{
	return value == 1;
}

gint32
mono_threadpool_worker_get_max (void)
{
	return 1;
}

gboolean
mono_threadpool_worker_set_max (gint32 value)
{
	return value == 1;
}

static void
fire_tp_callback (void)
{
	cb_scheduled = FALSE;
	tp_cb ();
}

void
mono_threadpool_worker_request (void)
{
	if (!cb_scheduled)
		mono_threads_schedule_background_job (fire_tp_callback);
	cb_scheduled = TRUE;
}

gboolean
mono_threadpool_worker_notify_completed (void)
{
	return FALSE;
}
