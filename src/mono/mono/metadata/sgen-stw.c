/**
 * \file
 * Stop the world functionality
 *
 * Author:
 * 	Paolo Molaro (lupus@ximian.com)
 *  Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2005-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright 2011 Xamarin, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "sgen/sgen-gc.h"
#include "sgen/sgen-protocol.h"
#include "sgen/sgen-memory-governor.h"
#include "sgen/sgen-workers.h"
#include "metadata/profiler-private.h"
#include "sgen/sgen-client.h"
#include "metadata/sgen-bridge-internals.h"
#include "metadata/gc-internals.h"
#include "utils/mono-threads.h"
#include "utils/mono-threads-debug.h"

#define TV_DECLARE SGEN_TV_DECLARE
#define TV_GETTIME SGEN_TV_GETTIME
#define TV_ELAPSED SGEN_TV_ELAPSED

static void sgen_unified_suspend_restart_world (void);
static void sgen_unified_suspend_stop_world (void);

static TV_DECLARE (end_of_last_stw);

guint64 mono_time_since_last_stw ()
{
	if (end_of_last_stw == 0)
		return 0;

	TV_DECLARE (current_time);
	TV_GETTIME (current_time);
	return TV_ELAPSED (end_of_last_stw, current_time);
}

unsigned int sgen_global_stop_count = 0;

inline static void*
align_pointer (void *ptr)
{
	mword p = (mword)ptr;
	p += sizeof (gpointer) - 1;
	p &= ~ (sizeof (gpointer) - 1);
	return (void*)p;
}

static void
update_current_thread_stack (void *start)
{
	int stack_guard = 0;
	SgenThreadInfo *info = mono_thread_info_current ();

	info->client_info.stack_start = align_pointer (&stack_guard);
	g_assert (info->client_info.stack_start);
	g_assert (info->client_info.stack_start >= info->client_info.info.stack_start_limit && info->client_info.stack_start < info->client_info.info.stack_end);

#if !defined(MONO_CROSS_COMPILE) && MONO_ARCH_HAS_MONO_CONTEXT
	MONO_CONTEXT_GET_CURRENT (info->client_info.ctx);
#elif defined (HOST_WASM)
	//nothing
#else
	g_error ("Sgen STW requires a working mono-context");
#endif

	if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
		mono_gc_get_gc_callbacks ()->thread_suspend_func (info->client_info.runtime_data, NULL, &info->client_info.ctx);
}

static void
acquire_gc_locks (void)
{
	LOCK_INTERRUPTION;
	mono_thread_info_suspend_lock ();
}

static void
release_gc_locks (void)
{
	mono_thread_info_suspend_unlock ();
	UNLOCK_INTERRUPTION;
}

static TV_DECLARE (stop_world_time);
static unsigned long max_pause_usec = 0;

static guint64 time_stop_world;
static guint64 time_restart_world;

/* LOCKING: assumes the GC lock is held */
void
sgen_client_stop_world (int generation)
{
	TV_DECLARE (end_handshake);

	MONO_PROFILER_RAISE (gc_event, (MONO_GC_EVENT_PRE_STOP_WORLD, generation));

	acquire_gc_locks ();

	MONO_PROFILER_RAISE (gc_event, (MONO_GC_EVENT_PRE_STOP_WORLD_LOCKED, generation));

	/* We start to scan after locks are taking, this ensures we won't be interrupted. */
	sgen_process_togglerefs ();

	update_current_thread_stack (&generation);

	sgen_global_stop_count++;
	SGEN_LOG (3, "stopping world n %d from %p %p", sgen_global_stop_count, mono_thread_info_current (), (gpointer) (gsize) mono_native_thread_id_get ());
	TV_GETTIME (stop_world_time);

	sgen_unified_suspend_stop_world ();

	SGEN_LOG (3, "world stopped");

	MONO_PROFILER_RAISE (gc_event, (MONO_GC_EVENT_POST_STOP_WORLD, generation));

	TV_GETTIME (end_handshake);
	time_stop_world += TV_ELAPSED (stop_world_time, end_handshake);

	sgen_memgov_collection_start (generation);
	if (sgen_need_bridge_processing ())
		sgen_bridge_reset_data ();
}

/* LOCKING: assumes the GC lock is held */
void
sgen_client_restart_world (int generation, gint64 *stw_time)
{
	TV_DECLARE (end_sw);
	TV_DECLARE (start_handshake);
	unsigned long usec;

	/* notify the profiler of the leftovers */
	/* FIXME this is the wrong spot at we can STW for non collection reasons. */
	if (MONO_PROFILER_ENABLED (gc_moves))
		mono_sgen_gc_event_moves ();

	MONO_PROFILER_RAISE (gc_event, (MONO_GC_EVENT_PRE_START_WORLD, generation));

	FOREACH_THREAD (info) {
		info->client_info.stack_start = NULL;
		memset (&info->client_info.ctx, 0, sizeof (MonoContext));
	} FOREACH_THREAD_END

	TV_GETTIME (start_handshake);

	sgen_unified_suspend_restart_world ();

	TV_GETTIME (end_sw);
	time_restart_world += TV_ELAPSED (start_handshake, end_sw);
	usec = TV_ELAPSED (stop_world_time, end_sw);
	max_pause_usec = MAX (usec, max_pause_usec);
	end_of_last_stw = end_sw;

	SGEN_LOG (2, "restarted (pause time: %d usec, max: %d)", (int)usec, (int)max_pause_usec);

	MONO_PROFILER_RAISE (gc_event, (MONO_GC_EVENT_POST_START_WORLD, generation));

	/*
	 * We must release the thread info suspend lock after doing
	 * the thread handshake.  Otherwise, if the GC stops the world
	 * and a thread is in the process of starting up, but has not
	 * yet registered (it's not in the thread_list), it is
	 * possible that the thread does register while the world is
	 * stopped.  When restarting the GC will then try to restart
	 * said thread, but since it never got the suspend signal, it
	 * cannot answer the restart signal, so a deadlock results.
	 */
	release_gc_locks ();

	MONO_PROFILER_RAISE (gc_event, (MONO_GC_EVENT_POST_START_WORLD_UNLOCKED, generation));

	*stw_time = usec;
}

void
mono_sgen_init_stw (void)
{
	mono_counters_register ("World stop", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_stop_world);
	mono_counters_register ("World restart", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_restart_world);
}

/* Unified suspend code */

static gboolean
sgen_is_thread_in_current_stw (SgenThreadInfo *info, int *reason)
{
	/*
	A thread explicitly asked to be skiped because it holds no managed state.
	This is used by TP and finalizer threads.
	FIXME Use an atomic variable for this to avoid everyone taking the GC LOCK.
	*/
	if (info->client_info.gc_disabled) {
		if (reason)
			*reason = 1;
		return FALSE;
	}

	/*
	We have detected that this thread is failing/dying, ignore it.
	FIXME: can't we merge this with thread_is_dying?
	*/
	if (info->client_info.skip) {
		if (reason)
			*reason = 2;
		return FALSE;
	}

	/*
	Suspending the current thread will deadlock us, bad idea.
	*/
	if (info == mono_thread_info_current ()) {
		if (reason)
			*reason = 3;
		return FALSE;
	}

	/*
	We can't suspend the workers that will do all the heavy lifting.
	FIXME Use some state bit in SgenThreadInfo for this.
	*/
	if (sgen_thread_pool_is_thread_pool_thread (mono_thread_info_get_tid (info))) {
		if (reason)
			*reason = 4;
		return FALSE;
	}

	/*
	The thread has signaled that it started to detach, ignore it.
	FIXME: can't we merge this with skip
	*/
	if (!mono_thread_info_is_live (info)) {
		if (reason)
			*reason = 5;
		return FALSE;
	}

	return TRUE;
}

static void
sgen_unified_suspend_stop_world (void)
{
	int sleep_duration = -1;

	mono_threads_begin_global_suspend ();
	THREADS_STW_DEBUG ("[GC-STW-BEGIN][%p] *** BEGIN SUSPEND *** \n", mono_thread_info_get_tid (mono_thread_info_current ()));

	FOREACH_THREAD (info) {
		info->client_info.skip = FALSE;
		info->client_info.suspend_done = FALSE;

		int reason;
		if (!sgen_is_thread_in_current_stw (info, &reason)) {
			THREADS_STW_DEBUG ("[GC-STW-BEGIN-SUSPEND] IGNORE thread %p skip %s reason %d\n", mono_thread_info_get_tid (info), info->client_info.skip ? "true" : "false", reason);
			continue;
		}

		info->client_info.skip = !mono_thread_info_begin_suspend (info);

		THREADS_STW_DEBUG ("[GC-STW-BEGIN-SUSPEND] SUSPEND thread %p skip %s\n", mono_thread_info_get_tid (info), info->client_info.skip ? "true" : "false");
	} FOREACH_THREAD_END

	mono_thread_info_current ()->client_info.suspend_done = TRUE;
	mono_threads_wait_pending_operations ();

	for (;;) {
		gint restart_counter = 0;

		FOREACH_THREAD (info) {
			gint suspend_count;

			int reason = 0;
			if (info->client_info.suspend_done || !sgen_is_thread_in_current_stw (info, &reason)) {
				THREADS_STW_DEBUG ("[GC-STW-RESTART] IGNORE RESUME thread %p not been processed done %d current %d reason %d\n", mono_thread_info_get_tid (info), info->client_info.suspend_done, !sgen_is_thread_in_current_stw (info, NULL), reason);
				continue;
			}

			/*
			All threads that reach here are pristine suspended. This means the following:

			- We haven't accepted the previous suspend as good.
			- We haven't gave up on it for this STW (it's either bad or asked not to)
			*/
			if (!mono_thread_info_in_critical_location (info)) {
				info->client_info.suspend_done = TRUE;

				THREADS_STW_DEBUG ("[GC-STW-RESTART] DONE thread %p deemed fully suspended\n", mono_thread_info_get_tid (info));
				continue;
			}

			suspend_count = mono_thread_info_suspend_count (info);
			if (!(suspend_count == 1))
				g_error ("[%p] suspend_count = %d, but should be 1", mono_thread_info_get_tid (info), suspend_count);

			info->client_info.skip = !mono_thread_info_begin_resume (info);
			if (!info->client_info.skip)
				restart_counter += 1;

			THREADS_STW_DEBUG ("[GC-STW-RESTART] RESTART thread %p skip %s\n", mono_thread_info_get_tid (info), info->client_info.skip ? "true" : "false");
		} FOREACH_THREAD_END

		mono_threads_wait_pending_operations ();

		if (restart_counter == 0)
			break;

		if (sleep_duration < 0) {
			mono_thread_info_yield ();
			sleep_duration = 0;
		} else {
			g_usleep (sleep_duration);
			sleep_duration += 10;
		}

		FOREACH_THREAD (info) {
			int reason = 0;
			if (info->client_info.suspend_done || !sgen_is_thread_in_current_stw (info, &reason)) {
				THREADS_STW_DEBUG ("[GC-STW-RESTART] IGNORE SUSPEND thread %p not been processed done %d current %d reason %d\n", mono_thread_info_get_tid (info), info->client_info.suspend_done, !sgen_is_thread_in_current_stw (info, NULL), reason);
				continue;
			}

			if (!mono_thread_info_is_running (info)) {
				THREADS_STW_DEBUG ("[GC-STW-RESTART] IGNORE SUSPEND thread %p not running\n", mono_thread_info_get_tid (info));
				continue;
			}

			info->client_info.skip = !mono_thread_info_begin_suspend (info);

			THREADS_STW_DEBUG ("[GC-STW-RESTART] SUSPEND thread %p skip %s\n", mono_thread_info_get_tid (info), info->client_info.skip ? "true" : "false");
		} FOREACH_THREAD_END

		mono_threads_wait_pending_operations ();
	}

	FOREACH_THREAD (info) {
		gpointer stopped_ip;

		int reason = 0;
		if (!sgen_is_thread_in_current_stw (info, &reason)) {
			g_assert (!info->client_info.suspend_done || info == mono_thread_info_current ());

			THREADS_STW_DEBUG ("[GC-STW-SUSPEND-END] thread %p is NOT suspended, reason %d\n", mono_thread_info_get_tid (info), reason);
			continue;
		}

		g_assert (info->client_info.suspend_done);

		info->client_info.ctx = mono_thread_info_get_suspend_state (info)->ctx;

		/* Once we remove the old suspend code, we should move sgen to directly access the state in MonoThread */
		info->client_info.stack_start = (gpointer) ((char*)MONO_CONTEXT_GET_SP (&info->client_info.ctx) - REDZONE_SIZE);

		if (info->client_info.stack_start < info->client_info.info.stack_start_limit
			 || info->client_info.stack_start >= info->client_info.info.stack_end) {
			/*
			 * Thread context is in unhandled state, most likely because it is
			 * dying. We don't scan it.
			 * FIXME We should probably rework and check the valid flag instead.
			 */
			info->client_info.stack_start = NULL;
		}

		stopped_ip = (gpointer) (MONO_CONTEXT_GET_IP (&info->client_info.ctx));

		binary_protocol_thread_suspend ((gpointer) mono_thread_info_get_tid (info), stopped_ip);

		THREADS_STW_DEBUG ("[GC-STW-SUSPEND-END] thread %p is suspended, stopped_ip = %p, stack = %p -> %p\n",
			mono_thread_info_get_tid (info), stopped_ip, info->client_info.stack_start, info->client_info.stack_start ? info->client_info.info.stack_end : NULL);
	} FOREACH_THREAD_END
}

static void
sgen_unified_suspend_restart_world (void)
{
	THREADS_STW_DEBUG ("[GC-STW-END] *** BEGIN RESUME ***\n");
	FOREACH_THREAD (info) {
		int reason = 0;
		if (sgen_is_thread_in_current_stw (info, &reason)) {
			g_assert (mono_thread_info_begin_resume (info));
			THREADS_STW_DEBUG ("[GC-STW-RESUME-WORLD] RESUME thread %p\n", mono_thread_info_get_tid (info));

			binary_protocol_thread_restart ((gpointer) mono_thread_info_get_tid (info));
		} else {
			THREADS_STW_DEBUG ("[GC-STW-RESUME-WORLD] IGNORE thread %p, reason %d\n", mono_thread_info_get_tid (info), reason);
		}
	} FOREACH_THREAD_END

	mono_threads_wait_pending_operations ();
	mono_threads_end_global_suspend ();
}
#endif
