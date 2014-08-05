/*
 * sgen-stw.c: Stop the world functionality
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
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "metadata/sgen-gc.h"
#include "metadata/sgen-protocol.h"
#include "metadata/sgen-memory-governor.h"
#include "metadata/profiler-private.h"
#include "utils/mono-time.h"
#include "utils/dtrace.h"
#include "utils/mono-counters.h"

#define TV_DECLARE SGEN_TV_DECLARE
#define TV_GETTIME SGEN_TV_GETTIME
#define TV_ELAPSED SGEN_TV_ELAPSED
#define TV_ELAPSED_MS SGEN_TV_ELAPSED_MS

inline static void*
align_pointer (void *ptr)
{
	mword p = (mword)ptr;
	p += sizeof (gpointer) - 1;
	p &= ~ (sizeof (gpointer) - 1);
	return (void*)p;
}

#ifdef USE_MONO_CTX
static MonoContext cur_thread_ctx;
#else
static mword cur_thread_regs [ARCH_NUM_REGS] = {0};
#endif

static void
update_current_thread_stack (void *start)
{
	int stack_guard = 0;
#if !defined(USE_MONO_CTX)
	void *reg_ptr = cur_thread_regs;
#endif
	SgenThreadInfo *info = mono_thread_info_current ();
	
	info->stack_start = align_pointer (&stack_guard);
	g_assert (info->stack_start >= info->stack_start_limit && info->stack_start < info->stack_end);
#ifdef USE_MONO_CTX
	MONO_CONTEXT_GET_CURRENT (cur_thread_ctx);
	memcpy (&info->ctx, &cur_thread_ctx, sizeof (MonoContext));
	if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
		mono_gc_get_gc_callbacks ()->thread_suspend_func (info->runtime_data, NULL, &info->ctx);
#else
	ARCH_STORE_REGS (reg_ptr);
	memcpy (&info->regs, reg_ptr, sizeof (info->regs));
	if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
		mono_gc_get_gc_callbacks ()->thread_suspend_func (info->runtime_data, NULL, NULL);
#endif
}

static gboolean
is_ip_in_managed_allocator (MonoDomain *domain, gpointer ip)
{
	MonoJitInfo *ji;

	if (!mono_thread_internal_current ())
		/* Happens during thread attach */
		return FALSE;

	if (!ip || !domain)
		return FALSE;
	if (!sgen_has_critical_method ())
		return FALSE;

	/*
	 * mono_jit_info_table_find is not async safe since it calls into the AOT runtime to load information for
	 * missing methods (#13951). To work around this, we disable the AOT fallback. For this to work, the JIT needs
	 * to register the jit info for all GC critical methods after they are JITted/loaded.
	 */
	ji = mono_jit_info_table_find_internal (domain, ip, FALSE);
	if (!ji)
		return FALSE;

	return sgen_is_critical_method (mono_jit_info_get_method (ji));
}

static int
restart_threads_until_none_in_managed_allocator (void)
{
	SgenThreadInfo *info;
	int num_threads_died = 0;
	int sleep_duration = -1;

	for (;;) {
		int restart_count = 0, restarted_count = 0;
		/* restart all threads that stopped in the
		   allocator */
		FOREACH_THREAD_SAFE (info) {
			gboolean result;
			if (info->skip || info->gc_disabled)
				continue;
			if (mono_thread_info_run_state (info) == STATE_RUNNING && (!info->stack_start || info->in_critical_region || info->info.inside_critical_region ||
					is_ip_in_managed_allocator (info->stopped_domain, info->stopped_ip))) {
				binary_protocol_thread_restart ((gpointer)mono_thread_info_get_tid (info));
				SGEN_LOG (3, "thread %p resumed.", (void*) (size_t) info->info.native_handle);
				result = sgen_resume_thread (info);
				if (result) {
					++restart_count;
				} else {
					info->skip = 1;
				}
			} else {
				/* we set the stopped_ip to
				   NULL for threads which
				   we're not restarting so
				   that we can easily identify
				   the others */
				info->stopped_ip = NULL;
				info->stopped_domain = NULL;
			}
		} END_FOREACH_THREAD_SAFE
		/* if no threads were restarted, we're done */
		if (restart_count == 0)
			break;

		/* wait for the threads to signal their restart */
		sgen_wait_for_suspend_ack (restart_count);

		if (sleep_duration < 0) {
			mono_thread_info_yield ();
			sleep_duration = 0;
		} else {
			g_usleep (sleep_duration);
			sleep_duration += 10;
		}

		/* stop them again */
		FOREACH_THREAD (info) {
			gboolean result;
			if (info->skip || info->stopped_ip == NULL)
				continue;
			result = sgen_suspend_thread (info);

			if (result) {
				++restarted_count;
			} else {
				info->skip = 1;
			}
		} END_FOREACH_THREAD
		/* some threads might have died */
		num_threads_died += restart_count - restarted_count;
		/* wait for the threads to signal their suspension
		   again */
		sgen_wait_for_suspend_ack (restarted_count);
	}

	return num_threads_died;
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

static void
count_cards (long long *major_total, long long *major_marked, long long *los_total, long long *los_marked)
{
	sgen_get_major_collector ()->count_cards (major_total, major_marked);
	sgen_los_count_cards (los_total, los_marked);
}

static TV_DECLARE (stop_world_time);
static unsigned long max_pause_usec = 0;

static long long time_stop_world;
static long long time_restart_world;

/* LOCKING: assumes the GC lock is held */
int
sgen_stop_world (int generation)
{
	TV_DECLARE (end_handshake);
	int count, dead;

	mono_profiler_gc_event (MONO_GC_EVENT_PRE_STOP_WORLD, generation);
	MONO_GC_WORLD_STOP_BEGIN ();
	binary_protocol_world_stopping (sgen_timestamp ());
	acquire_gc_locks ();

	/* We start to scan after locks are taking, this ensures we won't be interrupted. */
	sgen_process_togglerefs ();

	update_current_thread_stack (&count);

	sgen_global_stop_count++;
	SGEN_LOG (3, "stopping world n %d from %p %p", sgen_global_stop_count, mono_thread_info_current (), (gpointer)mono_native_thread_id_get ());
	TV_GETTIME (stop_world_time);
	count = sgen_thread_handshake (TRUE);
	dead = restart_threads_until_none_in_managed_allocator ();
	if (count < dead)
		g_error ("More threads have died (%d) that been initialy suspended %d", dead, count);
	count -= dead;

	SGEN_LOG (3, "world stopped %d thread(s)", count);
	mono_profiler_gc_event (MONO_GC_EVENT_POST_STOP_WORLD, generation);
	MONO_GC_WORLD_STOP_END ();
	if (binary_protocol_is_enabled ()) {
		long long major_total, major_marked, los_total, los_marked;
		count_cards (&major_total, &major_marked, &los_total, &los_marked);
		binary_protocol_world_stopped (sgen_timestamp (), major_total, major_marked, los_total, los_marked);
	}

	TV_GETTIME (end_handshake);
	time_stop_world += TV_ELAPSED (stop_world_time, end_handshake);

	sgen_memgov_collection_start (generation);
	if (sgen_need_bridge_processing ())
		sgen_bridge_reset_data ();

	return count;
}

/* LOCKING: assumes the GC lock is held */
int
sgen_restart_world (int generation, GGTimingInfo *timing)
{
	int count;
	SgenThreadInfo *info;
	TV_DECLARE (end_sw);
	TV_DECLARE (start_handshake);
	TV_DECLARE (end_bridge);
	unsigned long usec, bridge_usec;

	if (binary_protocol_is_enabled ()) {
		long long major_total, major_marked, los_total, los_marked;
		count_cards (&major_total, &major_marked, &los_total, &los_marked);
		binary_protocol_world_restarting (generation, sgen_timestamp (), major_total, major_marked, los_total, los_marked);
	}

	/* notify the profiler of the leftovers */
	/* FIXME this is the wrong spot at we can STW for non collection reasons. */
	if (G_UNLIKELY (mono_profiler_events & MONO_PROFILE_GC_MOVES))
		sgen_gc_event_moves ();
	mono_profiler_gc_event (MONO_GC_EVENT_PRE_START_WORLD, generation);
	MONO_GC_WORLD_RESTART_BEGIN (generation);
	FOREACH_THREAD (info) {
		info->stack_start = NULL;
#ifdef USE_MONO_CTX
		memset (&info->ctx, 0, sizeof (MonoContext));
#else
		memset (&info->regs, 0, sizeof (info->regs));
#endif
	} END_FOREACH_THREAD

	TV_GETTIME (start_handshake);
	count = sgen_thread_handshake (FALSE);
	TV_GETTIME (end_sw);
	time_restart_world += TV_ELAPSED (start_handshake, end_sw);
	usec = TV_ELAPSED (stop_world_time, end_sw);
	max_pause_usec = MAX (usec, max_pause_usec);
	SGEN_LOG (2, "restarted %d thread(s) (pause time: %d usec, max: %d)", count, (int)usec, (int)max_pause_usec);
	mono_profiler_gc_event (MONO_GC_EVENT_POST_START_WORLD, generation);
	MONO_GC_WORLD_RESTART_END (generation);
	binary_protocol_world_restarted (generation, sgen_timestamp ());

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

	sgen_try_free_some_memory = TRUE;

	if (sgen_need_bridge_processing ())
		sgen_bridge_processing_finish (generation);

	TV_GETTIME (end_bridge);
	bridge_usec = TV_ELAPSED (end_sw, end_bridge);

	if (timing) {
		timing [0].stw_time = usec;
		timing [0].bridge_time = bridge_usec;
	}
	
	sgen_memgov_collection_end (generation, timing, timing ? 2 : 0);

	return count;
}

void
sgen_init_stw (void)
{
	mono_counters_register ("World stop", MONO_COUNTER_GC | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &time_stop_world);
	mono_counters_register ("World restart", MONO_COUNTER_GC | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &time_restart_world);
}

#endif
