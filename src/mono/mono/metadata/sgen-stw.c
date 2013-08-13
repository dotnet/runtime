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
static MonoContext cur_thread_ctx = {0};
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

	return sgen_is_critical_method (ji->method);
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
			if (info->skip || info->gc_disabled || !info->joined_stw)
				continue;
			if (!info->thread_is_dying && (!info->stack_start || info->in_critical_region || info->info.inside_critical_region ||
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
#ifdef HOST_WIN32
			SwitchToThread ();
#else
			sched_yield ();
#endif
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
stw_bridge_process (void)
{
	sgen_bridge_processing_stw_step ();
}

static void
bridge_process (int generation)
{
	sgen_bridge_processing_finish (generation);
}

static TV_DECLARE (stop_world_time);
static unsigned long max_pause_usec = 0;

/* LOCKING: assumes the GC lock is held */
int
sgen_stop_world (int generation)
{
	int count, dead;

	/*XXX this is the right stop, thought might not be the nicest place to put it*/
	sgen_process_togglerefs ();

	mono_profiler_gc_event (MONO_GC_EVENT_PRE_STOP_WORLD, generation);
	MONO_GC_WORLD_STOP_BEGIN ();
	acquire_gc_locks ();

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

	sgen_memgov_collection_start (generation);
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
	TV_DECLARE (end_bridge);
	unsigned long usec, bridge_usec;

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

	stw_bridge_process ();
	release_gc_locks ();

	count = sgen_thread_handshake (FALSE);
	TV_GETTIME (end_sw);
	usec = TV_ELAPSED (stop_world_time, end_sw);
	max_pause_usec = MAX (usec, max_pause_usec);
	SGEN_LOG (2, "restarted %d thread(s) (pause time: %d usec, max: %d)", count, (int)usec, (int)max_pause_usec);
	mono_profiler_gc_event (MONO_GC_EVENT_POST_START_WORLD, generation);
	MONO_GC_WORLD_RESTART_END (generation);

	mono_thread_hazardous_try_free_some ();

	bridge_process (generation);

	TV_GETTIME (end_bridge);
	bridge_usec = TV_ELAPSED (end_sw, end_bridge);

	if (timing) {
		timing [0].stw_time = usec;
		timing [0].bridge_time = bridge_usec;
	}
	
	sgen_memgov_collection_end (generation, timing, timing ? 2 : 0);

	return count;
}

#endif
