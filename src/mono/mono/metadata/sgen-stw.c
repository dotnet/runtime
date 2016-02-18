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

#include "sgen/sgen-gc.h"
#include "sgen/sgen-protocol.h"
#include "sgen/sgen-memory-governor.h"
#include "sgen/sgen-thread-pool.h"
#include "metadata/profiler-private.h"
#include "sgen/sgen-client.h"
#include "metadata/sgen-bridge-internals.h"
#include "metadata/gc-internals.h"

#define TV_DECLARE SGEN_TV_DECLARE
#define TV_GETTIME SGEN_TV_GETTIME
#define TV_ELAPSED SGEN_TV_ELAPSED

static void sgen_unified_suspend_restart_world (void);
static void sgen_unified_suspend_stop_world (void);

unsigned int sgen_global_stop_count = 0;

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
static mword cur_thread_regs [ARCH_NUM_REGS];
#endif

static void
update_current_thread_stack (void *start)
{
	int stack_guard = 0;
#if !defined(USE_MONO_CTX)
	void *reg_ptr = cur_thread_regs;
#endif
	SgenThreadInfo *info = mono_thread_info_current ();
	
	info->client_info.stack_start = align_pointer (&stack_guard);
	g_assert (info->client_info.stack_start >= info->client_info.stack_start_limit && info->client_info.stack_start < info->client_info.stack_end);
#ifdef USE_MONO_CTX
	MONO_CONTEXT_GET_CURRENT (cur_thread_ctx);
	memcpy (&info->client_info.ctx, &cur_thread_ctx, sizeof (MonoContext));
	if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
		mono_gc_get_gc_callbacks ()->thread_suspend_func (info->client_info.runtime_data, NULL, &info->client_info.ctx);
#else
	ARCH_STORE_REGS (reg_ptr);
	memcpy (&info->client_info.regs, reg_ptr, sizeof (info->client_info.regs));
	if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
		mono_gc_get_gc_callbacks ()->thread_suspend_func (info->client_info.runtime_data, NULL, NULL);
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
	ji = mono_jit_info_table_find_internal (domain, (char *)ip, FALSE, FALSE);
	if (!ji)
		return FALSE;

	return sgen_is_critical_method (mono_jit_info_get_method (ji));
}

static int
restart_threads_until_none_in_managed_allocator (void)
{
	int num_threads_died = 0;
	int sleep_duration = -1;

	for (;;) {
		int restart_count = 0, restarted_count = 0;
		/* restart all threads that stopped in the
		   allocator */
		FOREACH_THREAD_SAFE (info) {
			gboolean result;
			if (info->client_info.skip || info->client_info.gc_disabled || info->client_info.suspend_done)
				continue;
			if (mono_thread_info_is_live (info) &&
					(!info->client_info.stack_start || info->client_info.in_critical_region || info->client_info.info.inside_critical_region ||
					is_ip_in_managed_allocator (info->client_info.stopped_domain, info->client_info.stopped_ip))) {
				binary_protocol_thread_restart ((gpointer)mono_thread_info_get_tid (info));
				SGEN_LOG (3, "thread %p resumed.", (void*) (size_t) info->client_info.info.native_handle);
				result = sgen_resume_thread (info);
				if (result) {
					++restart_count;
				} else {
					info->client_info.skip = 1;
				}
			} else {
				/* we set the stopped_ip to
				   NULL for threads which
				   we're not restarting so
				   that we can easily identify
				   the others */
				info->client_info.stopped_ip = NULL;
				info->client_info.stopped_domain = NULL;
				info->client_info.suspend_done = TRUE;
			}
		} FOREACH_THREAD_SAFE_END
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
			if (info->client_info.skip || info->client_info.stopped_ip == NULL)
				continue;
			result = sgen_suspend_thread (info);

			if (result) {
				++restarted_count;
			} else {
				info->client_info.skip = 1;
			}
		} FOREACH_THREAD_END
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

static TV_DECLARE (stop_world_time);
static unsigned long max_pause_usec = 0;

static guint64 time_stop_world;
static guint64 time_restart_world;

/* LOCKING: assumes the GC lock is held */
void
sgen_client_stop_world (int generation)
{
	TV_DECLARE (end_handshake);

	/* notify the profiler of the leftovers */
	/* FIXME this is the wrong spot at we can STW for non collection reasons. */
	if (G_UNLIKELY (mono_profiler_events & MONO_PROFILE_GC_MOVES))
		mono_sgen_gc_event_moves ();

	acquire_gc_locks ();

	/* We start to scan after locks are taking, this ensures we won't be interrupted. */
	sgen_process_togglerefs ();

	update_current_thread_stack (&generation);

	sgen_global_stop_count++;
	SGEN_LOG (3, "stopping world n %d from %p %p", sgen_global_stop_count, mono_thread_info_current (), (gpointer) (gsize) mono_native_thread_id_get ());
	TV_GETTIME (stop_world_time);

	if (mono_thread_info_unified_management_enabled ()) {
		sgen_unified_suspend_stop_world ();
	} else {
		int count, dead;
		count = sgen_thread_handshake (TRUE);
		dead = restart_threads_until_none_in_managed_allocator ();
		if (count < dead)
			g_error ("More threads have died (%d) that been initialy suspended %d", dead, count);
	}

	SGEN_LOG (3, "world stopped");

	TV_GETTIME (end_handshake);
	time_stop_world += TV_ELAPSED (stop_world_time, end_handshake);

	sgen_memgov_collection_start (generation);
	if (sgen_need_bridge_processing ())
		sgen_bridge_reset_data ();
}

/* LOCKING: assumes the GC lock is held */
void
sgen_client_restart_world (int generation, GGTimingInfo *timing)
{
	TV_DECLARE (end_sw);
	TV_DECLARE (start_handshake);
	TV_DECLARE (end_bridge);
	unsigned long usec, bridge_usec;

	/* notify the profiler of the leftovers */
	/* FIXME this is the wrong spot at we can STW for non collection reasons. */
	if (G_UNLIKELY (mono_profiler_events & MONO_PROFILE_GC_MOVES))
		mono_sgen_gc_event_moves ();

	FOREACH_THREAD (info) {
		info->client_info.stack_start = NULL;
#ifdef USE_MONO_CTX
		memset (&info->client_info.ctx, 0, sizeof (MonoContext));
#else
		memset (&info->client_info.regs, 0, sizeof (info->client_info.regs));
#endif
	} FOREACH_THREAD_END

	TV_GETTIME (start_handshake);

	if (mono_thread_info_unified_management_enabled ())
		sgen_unified_suspend_restart_world ();
	else
		sgen_thread_handshake (FALSE);

	TV_GETTIME (end_sw);
	time_restart_world += TV_ELAPSED (start_handshake, end_sw);
	usec = TV_ELAPSED (stop_world_time, end_sw);
	max_pause_usec = MAX (usec, max_pause_usec);

	SGEN_LOG (2, "restarted (pause time: %d usec, max: %d)", (int)usec, (int)max_pause_usec);

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

	TV_GETTIME (end_bridge);
	bridge_usec = TV_ELAPSED (end_sw, end_bridge);

	if (timing) {
		timing [0].stw_time = usec;
		timing [0].bridge_time = bridge_usec;
	}
}

void
mono_sgen_init_stw (void)
{
	mono_counters_register ("World stop", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_stop_world);
	mono_counters_register ("World restart", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_restart_world);
}

/* Unified suspend code */

static gboolean
sgen_is_thread_in_current_stw (SgenThreadInfo *info)
{
	/*
	A thread explicitly asked to be skiped because it holds no managed state.
	This is used by TP and finalizer threads.
	FIXME Use an atomic variable for this to avoid everyone taking the GC LOCK.
	*/
	if (info->client_info.gc_disabled) {
		return FALSE;
	}

	/*
	We have detected that this thread is failing/dying, ignore it.
	FIXME: can't we merge this with thread_is_dying?
	*/
	if (info->client_info.skip) {
		return FALSE;
	}

	/*
	Suspending the current thread will deadlock us, bad idea.
	*/
	if (info == mono_thread_info_current ()) {
		return FALSE;
	}

	/*
	We can't suspend the workers that will do all the heavy lifting.
	FIXME Use some state bit in SgenThreadInfo for this.
	*/
	if (sgen_thread_pool_is_thread_pool_thread (mono_thread_info_get_tid (info))) {
		return FALSE;
	}

	/*
	The thread has signaled that it started to detach, ignore it.
	FIXME: can't we merge this with skip
	*/
	if (!mono_thread_info_is_live (info)) {
		return FALSE;
	}

	return TRUE;
}

static void
update_sgen_info (SgenThreadInfo *info)
{
	char *stack_start;

	/* Once we remove the old suspend code, we should move sgen to directly access the state in MonoThread */
	info->client_info.stopped_domain = (MonoDomain *)mono_thread_info_tls_get (info, TLS_KEY_DOMAIN);
	info->client_info.stopped_ip = (gpointer) MONO_CONTEXT_GET_IP (&mono_thread_info_get_suspend_state (info)->ctx);
	stack_start = (char*)MONO_CONTEXT_GET_SP (&mono_thread_info_get_suspend_state (info)->ctx) - REDZONE_SIZE;

	/* altstack signal handler, sgen can't handle them, mono-threads should have handled this. */
	if (stack_start < (char*)info->client_info.stack_start_limit || stack_start >= (char*)info->client_info.stack_end)
		g_error ("BAD STACK");

	info->client_info.stack_start = stack_start;
#ifdef USE_MONO_CTX
	info->client_info.ctx = mono_thread_info_get_suspend_state (info)->ctx;
#else
	g_assert_not_reached ();
#endif
}

static void
sgen_unified_suspend_stop_world (void)
{
	int restart_counter;
	int sleep_duration = -1;

	mono_threads_begin_global_suspend ();
	THREADS_STW_DEBUG ("[GC-STW-BEGIN] *** BEGIN SUSPEND *** \n");

	FOREACH_THREAD_SAFE (info) {
		info->client_info.skip = FALSE;
		info->client_info.suspend_done = FALSE;
		if (sgen_is_thread_in_current_stw (info)) {
			info->client_info.skip = !mono_thread_info_begin_suspend (info);
			THREADS_STW_DEBUG ("[GC-STW-BEGIN-SUSPEND] SUSPEND thread %p skip %d\n", mono_thread_info_get_tid (info), info->client_info.skip);
		} else {
			THREADS_STW_DEBUG ("[GC-STW-BEGIN-SUSPEND] IGNORE thread %p skip %d\n", mono_thread_info_get_tid (info), info->client_info.skip);
		}
	} FOREACH_THREAD_SAFE_END

	mono_thread_info_current ()->client_info.suspend_done = TRUE;
	mono_threads_wait_pending_operations ();

	for (;;) {
		restart_counter = 0;
		FOREACH_THREAD_SAFE (info) {
			if (info->client_info.suspend_done || !sgen_is_thread_in_current_stw (info)) {
				THREADS_STW_DEBUG ("[GC-STW-RESTART] IGNORE thread %p not been processed done %d current %d\n", mono_thread_info_get_tid (info), info->client_info.suspend_done, !sgen_is_thread_in_current_stw (info));
				continue;
			}

			/*
			All threads that reach here are pristine suspended. This means the following:

			- We haven't accepted the previous suspend as good.
			- We haven't gave up on it for this STW (it's either bad or asked not to)
			*/
			if (!mono_thread_info_check_suspend_result (info)) {
				THREADS_STW_DEBUG ("[GC-STW-RESTART] SKIP thread %p failed to finish to suspend\n", mono_thread_info_get_tid (info));
				info->client_info.skip = TRUE;
			} else if (mono_thread_info_in_critical_location (info)) {
				gboolean res;
				g_assert (mono_thread_info_suspend_count (info) == 1);
				res = mono_thread_info_begin_resume (info);
				THREADS_STW_DEBUG ("[GC-STW-RESTART] RESTART thread %p skip %d\n", mono_thread_info_get_tid (info), res);
				if (res)
					++restart_counter;
				else
					info->client_info.skip = TRUE;
			} else {
				THREADS_STW_DEBUG ("[GC-STW-RESTART] DONE thread %p deemed fully suspended\n", mono_thread_info_get_tid (info));
				g_assert (!info->client_info.in_critical_region);
				info->client_info.suspend_done = TRUE;
			}
		} FOREACH_THREAD_SAFE_END

		if (restart_counter == 0)
			break;
		mono_threads_wait_pending_operations ();

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

		FOREACH_THREAD_SAFE (info) {
			if (sgen_is_thread_in_current_stw (info) && mono_thread_info_is_running (info)) {
				gboolean res = mono_thread_info_begin_suspend (info);
				THREADS_STW_DEBUG ("[GC-STW-RESTART] SUSPEND thread %p skip %d\n", mono_thread_info_get_tid (info), res);
				if (!res)
					info->client_info.skip = TRUE;
			}
		} FOREACH_THREAD_SAFE_END

		mono_threads_wait_pending_operations ();
	}

	FOREACH_THREAD_SAFE (info) {
		if (sgen_is_thread_in_current_stw (info)) {
			THREADS_STW_DEBUG ("[GC-STW-SUSPEND-END] thread %p is suspended\n", mono_thread_info_get_tid (info));
			g_assert (info->client_info.suspend_done);
			update_sgen_info (info);
		} else {
			g_assert (!info->client_info.suspend_done || info == mono_thread_info_current ());
		}
	} FOREACH_THREAD_SAFE_END
}

static void
sgen_unified_suspend_restart_world (void)
{
	THREADS_STW_DEBUG ("[GC-STW-END] *** BEGIN RESUME ***\n");
	FOREACH_THREAD_SAFE (info) {
		if (sgen_is_thread_in_current_stw (info)) {
			g_assert (mono_thread_info_begin_resume (info));
			THREADS_STW_DEBUG ("[GC-STW-RESUME-WORLD] RESUME thread %p\n", mono_thread_info_get_tid (info));
		} else {
			THREADS_STW_DEBUG ("[GC-STW-RESUME-WORLD] IGNORE thread %p\n", mono_thread_info_get_tid (info));
		}
	} FOREACH_THREAD_SAFE_END

	mono_threads_wait_pending_operations ();
	mono_threads_end_global_suspend ();
}
#endif
