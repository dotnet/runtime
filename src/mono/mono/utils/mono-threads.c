/**
 * \file
 * Low-level threading
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

/* enable pthread extensions */
#ifdef TARGET_MACH
#define _DARWIN_C_SOURCE
#endif

#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-os-semaphore.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/mono-coop-semaphore.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/mono-threads-debug.h>
#include <mono/utils/os-event.h>
#include <mono/utils/w32api.h>

#include <errno.h>

#if defined(__MACH__)
#include <mono/utils/mach-support.h>
#endif

/*
Mutex that makes sure only a single thread can be suspending others.
Suspend is a very racy operation since it requires restarting until
the target thread is not on an unsafe region.

We could implement this using critical regions, but would be much much
harder for an operation that is hardly performance critical.

The GC has to acquire this lock before starting a STW to make sure
a runtime suspend won't make it wronly see a thread in a safepoint
when it is in fact not.

This has to be a naked locking primitive, and not a coop aware one, as
it needs to be usable when destroying thread_info_key, the TLS key for
the current MonoThreadInfo. In this case, mono_thread_info_current_unchecked,
(which is used inside MONO_ENTER_GC_SAFE), would return NULL, leading
to an assertion error. We then simply switch state manually in
mono_thread_info_suspend_lock_with_info.
*/
static MonoSemType global_suspend_semaphore;

static size_t thread_info_size;
static MonoThreadInfoCallbacks threads_callbacks;
static MonoThreadInfoRuntimeCallbacks runtime_callbacks;
static MonoNativeTlsKey thread_info_key, thread_exited_key;
#ifdef HAVE_KW_THREAD
static __thread gint32 tls_small_id = -1;
#else
static MonoNativeTlsKey small_id_key;
#endif
static MonoLinkedListSet thread_list;
static gboolean mono_threads_inited = FALSE;

static MonoSemType suspend_semaphore;
static size_t pending_suspends;

static mono_mutex_t join_mutex;

#define mono_thread_info_run_state(info) (((MonoThreadInfo*)info)->thread_state & THREAD_STATE_MASK)

/*warn at 50 ms*/
#define SLEEP_DURATION_BEFORE_WARNING (50)
/*never aborts */
#define SLEEP_DURATION_BEFORE_ABORT MONO_INFINITE_WAIT

static guint32 sleepWarnDuration = SLEEP_DURATION_BEFORE_WARNING,
	    sleepAbortDuration = SLEEP_DURATION_BEFORE_ABORT;

static int suspend_posts, resume_posts, abort_posts, waits_done, pending_ops;

void
mono_threads_notify_initiator_of_abort (MonoThreadInfo* info)
{
	THREADS_SUSPEND_DEBUG ("[INITIATOR-NOTIFY-ABORT] %p\n", mono_thread_info_get_tid (info));
	mono_atomic_inc_i32 (&abort_posts);
	mono_os_sem_post (&suspend_semaphore);
}

void
mono_threads_notify_initiator_of_suspend (MonoThreadInfo* info)
{
	THREADS_SUSPEND_DEBUG ("[INITIATOR-NOTIFY-SUSPEND] %p\n", mono_thread_info_get_tid (info));
	mono_atomic_inc_i32 (&suspend_posts);
	mono_os_sem_post (&suspend_semaphore);
}

void
mono_threads_notify_initiator_of_resume (MonoThreadInfo* info)
{
	THREADS_SUSPEND_DEBUG ("[INITIATOR-NOTIFY-RESUME] %p\n", mono_thread_info_get_tid (info));
	mono_atomic_inc_i32 (&resume_posts);
	mono_os_sem_post (&suspend_semaphore);
}

static gboolean
begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	if (mono_threads_is_coop_enabled ()) {
		/* There's nothing else to do after we async request the thread to suspend */
		mono_threads_add_to_pending_operation_set (info);
		return TRUE;
	}

	return mono_threads_suspend_begin_async_suspend (info, interrupt_kernel);
}

static gboolean
check_async_suspend (MonoThreadInfo *info)
{
	if (mono_threads_is_coop_enabled ()) {
		/* Async suspend can't async fail on coop */
		return TRUE;
	}

	return mono_threads_suspend_check_suspend_result (info);
}

static void
resume_async_suspended (MonoThreadInfo *info)
{
	if (mono_threads_is_coop_enabled ())
		g_assert_not_reached ();

	g_assert (mono_threads_suspend_begin_async_resume (info));
}

static void
resume_self_suspended (MonoThreadInfo* info)
{
	THREADS_SUSPEND_DEBUG ("**BEGIN self-resume %p\n", mono_thread_info_get_tid (info));
	mono_os_sem_post (&info->resume_semaphore);
}

void
mono_thread_info_wait_for_resume (MonoThreadInfo* info)
{
	int res;
	THREADS_SUSPEND_DEBUG ("**WAIT self-resume %p\n", mono_thread_info_get_tid (info));
	res = mono_os_sem_wait (&info->resume_semaphore, MONO_SEM_FLAGS_NONE);
	g_assert (res != -1);
}

static void
resume_blocking_suspended (MonoThreadInfo* info)
{
	THREADS_SUSPEND_DEBUG ("**BEGIN blocking-resume %p\n", mono_thread_info_get_tid (info));
	mono_os_sem_post (&info->resume_semaphore);
}

void
mono_threads_add_to_pending_operation_set (MonoThreadInfo* info)
{
	THREADS_SUSPEND_DEBUG ("added %p to pending suspend\n", mono_thread_info_get_tid (info));
	++pending_suspends;
	mono_atomic_inc_i32 (&pending_ops);
}

void
mono_threads_begin_global_suspend (void)
{
	size_t ps = pending_suspends;
	if (G_UNLIKELY (ps != 0))
		g_error ("pending_suspends = %d, but must be 0", ps);
	THREADS_SUSPEND_DEBUG ("------ BEGIN GLOBAL OP sp %d rp %d ap %d wd %d po %d (sp + rp + ap == wd) (wd == po)\n", suspend_posts, resume_posts,
		abort_posts, waits_done, pending_ops);
	g_assert ((suspend_posts + resume_posts + abort_posts) == waits_done);
	mono_threads_coop_begin_global_suspend ();
}

void
mono_threads_end_global_suspend (void) 
{
	size_t ps = pending_suspends;
	if (G_UNLIKELY (ps != 0))
		g_error ("pending_suspends = %d, but must be 0", ps);
	THREADS_SUSPEND_DEBUG ("------ END GLOBAL OP sp %d rp %d ap %d wd %d po %d\n", suspend_posts, resume_posts,
		abort_posts, waits_done, pending_ops);
	g_assert ((suspend_posts + resume_posts + abort_posts) == waits_done);
	mono_threads_coop_end_global_suspend ();
}

static void
dump_threads (void)
{
	MonoThreadInfo *cur = mono_thread_info_current ();

	MOSTLY_ASYNC_SAFE_PRINTF ("STATE CUE CARD: (? means a positive number, usually 1 or 2, * means any number)\n");
	MOSTLY_ASYNC_SAFE_PRINTF ("\t0x0\t- starting (GOOD, unless the thread is running managed code)\n");
	MOSTLY_ASYNC_SAFE_PRINTF ("\t0x1\t- running (BAD, unless it's the gc thread)\n");
	MOSTLY_ASYNC_SAFE_PRINTF ("\t0x2\t- detached (GOOD, unless the thread is running managed code)\n");
	MOSTLY_ASYNC_SAFE_PRINTF ("\t0x?03\t- async suspended (GOOD)\n");
	MOSTLY_ASYNC_SAFE_PRINTF ("\t0x?04\t- self suspended (GOOD)\n");
	MOSTLY_ASYNC_SAFE_PRINTF ("\t0x?05\t- async suspend requested (BAD)\n");
	MOSTLY_ASYNC_SAFE_PRINTF ("\t0x?06\t- self suspend requested (BAD)\n");
	MOSTLY_ASYNC_SAFE_PRINTF ("\t0x*07\t- blocking (GOOD)\n");
	MOSTLY_ASYNC_SAFE_PRINTF ("\t0x?08\t- blocking with pending suspend (GOOD)\n");

	FOREACH_THREAD_SAFE (info) {
#ifdef TARGET_MACH
		char thread_name [256] = { 0 };
		pthread_getname_np (mono_thread_info_get_tid (info), thread_name, 255);

		MOSTLY_ASYNC_SAFE_PRINTF ("--thread %p id %p [%p] (%s) state %x  %s\n", info, (void *) mono_thread_info_get_tid (info), (void*)(size_t)info->native_handle, thread_name, info->thread_state, info == cur ? "GC INITIATOR" : "" );
#else
		MOSTLY_ASYNC_SAFE_PRINTF ("--thread %p id %p [%p] state %x  %s\n", info, (void *) mono_thread_info_get_tid (info), (void*)(size_t)info->native_handle, info->thread_state, info == cur ? "GC INITIATOR" : "" );
#endif
	} FOREACH_THREAD_SAFE_END
}

gboolean
mono_threads_wait_pending_operations (void)
{
	int i;
	int c = pending_suspends;

	/* Wait threads to park */
	THREADS_SUSPEND_DEBUG ("[INITIATOR-WAIT-COUNT] %d\n", c);
	if (pending_suspends) {
		MonoStopwatch suspension_time;
		mono_stopwatch_start (&suspension_time);
		for (i = 0; i < pending_suspends; ++i) {
			THREADS_SUSPEND_DEBUG ("[INITIATOR-WAIT-WAITING]\n");
			mono_atomic_inc_i32 (&waits_done);
			if (mono_os_sem_timedwait (&suspend_semaphore, sleepAbortDuration, MONO_SEM_FLAGS_NONE) == MONO_SEM_TIMEDWAIT_RET_SUCCESS)
				continue;
			mono_stopwatch_stop (&suspension_time);

			dump_threads ();

			MOSTLY_ASYNC_SAFE_PRINTF ("WAITING for %d threads, got %d suspended\n", (int)pending_suspends, i);
			g_error ("suspend_thread suspend took %d ms, which is more than the allowed %d ms", (int)mono_stopwatch_elapsed_ms (&suspension_time), sleepAbortDuration);
		}
		mono_stopwatch_stop (&suspension_time);
		THREADS_SUSPEND_DEBUG ("Suspending %d threads took %d ms.\n", (int)pending_suspends, (int)mono_stopwatch_elapsed_ms (&suspension_time));

	}

	pending_suspends = 0;

	return c > 0;
}


//Thread initialization code

static inline void
mono_hazard_pointer_clear_all (MonoThreadHazardPointers *hp, int retain)
{
	if (retain != 0)
		mono_hazard_pointer_clear (hp, 0);
	if (retain != 1)
		mono_hazard_pointer_clear (hp, 1);
	if (retain != 2)
		mono_hazard_pointer_clear (hp, 2);
}

/*
If return non null Hazard Pointer 1 holds the return value.
*/
MonoThreadInfo*
mono_thread_info_lookup (MonoNativeThreadId id)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();

	if (!mono_lls_find (&thread_list, hp, (uintptr_t)id)) {
		mono_hazard_pointer_clear_all (hp, -1);
		return NULL;
	} 

	mono_hazard_pointer_clear_all (hp, 1);
	return (MonoThreadInfo *) mono_hazard_pointer_get_val (hp, 1);
}

static gboolean
mono_thread_info_insert (MonoThreadInfo *info)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();

	if (!mono_lls_insert (&thread_list, hp, (MonoLinkedListSetNode*)info)) {
		mono_hazard_pointer_clear_all (hp, -1);
		return FALSE;
	} 

	mono_hazard_pointer_clear_all (hp, -1);
	return TRUE;
}

static gboolean
mono_thread_info_remove (MonoThreadInfo *info)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();
	gboolean res;

	THREADS_DEBUG ("removing info %p\n", info);
	res = mono_lls_remove (&thread_list, hp, (MonoLinkedListSetNode*)info);
	mono_hazard_pointer_clear_all (hp, -1);
	return res;
}

static void
free_thread_info (gpointer mem)
{
	MonoThreadInfo *info = (MonoThreadInfo *) mem;

	mono_os_sem_destroy (&info->resume_semaphore);
	mono_threads_suspend_free (info);

	g_free (info);
}

/*
 * mono_thread_info_register_small_id
 *
 * Registers a small ID for the current thread. This is a 16-bit value uniquely
 * identifying the current thread. If the current thread already has a small ID
 * assigned, that small ID will be returned; otherwise, the newly assigned small
 * ID is returned.
 */
int
mono_thread_info_register_small_id (void)
{
	int small_id = mono_thread_info_get_small_id ();

	if (small_id != -1)
		return small_id;

	small_id = mono_thread_small_id_alloc ();
#ifdef HAVE_KW_THREAD
	tls_small_id = small_id;
#else
	mono_native_tls_set_value (small_id_key, GUINT_TO_POINTER (small_id + 1));
#endif
	return small_id;
}

static void
thread_handle_destroy (gpointer data)
{
	MonoThreadHandle *thread_handle;

	thread_handle = (MonoThreadHandle*) data;

	mono_os_event_destroy (&thread_handle->event);
	g_free (thread_handle);
}

static gboolean
register_thread (MonoThreadInfo *info)
{
	size_t stsize = 0;
	guint8 *staddr = NULL;
	gboolean result;

	info->small_id = mono_thread_info_register_small_id ();
	mono_thread_info_set_tid (info, mono_native_thread_id_get ());

	info->handle = g_new0 (MonoThreadHandle, 1);
	mono_refcount_init (info->handle, thread_handle_destroy);
	mono_os_event_init (&info->handle->event, FALSE);

	mono_os_sem_init (&info->resume_semaphore, 0);

	/*set TLS early so SMR works */
	mono_native_tls_set_value (thread_info_key, info);

	mono_thread_info_get_stack_bounds (&staddr, &stsize);
	g_assert (staddr);
	g_assert (stsize);
	info->stack_start_limit = staddr;
	info->stack_end = staddr + stsize;

	info->stackdata = g_byte_array_new ();

	info->internal_thread_gchandle = G_MAXUINT32;

	info->profiler_signal_ack = 1;

	mono_threads_suspend_register (info);

	THREADS_DEBUG ("registering info %p tid %p small id %x\n", info, mono_thread_info_get_tid (info), info->small_id);

	if (threads_callbacks.thread_attach) {
		if (!threads_callbacks.thread_attach (info)) {
			// g_warning ("thread registation failed\n");
			mono_native_tls_set_value (thread_info_key, NULL);
			return FALSE;
		}
	}

	/*
	Transition it before taking any locks or publishing itself to reduce the chance
	of others witnessing a detached thread.
	We can reasonably expect that until this thread gets published, no other thread will
	try to manipulate it.
	*/
	mono_threads_transition_attach (info);
	mono_thread_info_suspend_lock ();
	/*If this fail it means a given thread has been registered twice, which doesn't make sense. */
	result = mono_thread_info_insert (info);
	g_assert (result);
	mono_thread_info_suspend_unlock ();

	return TRUE;
}

static void
mono_thread_info_suspend_lock_with_info (MonoThreadInfo *info);

static void
mono_threads_signal_thread_handle (MonoThreadHandle* thread_handle);

static void
unregister_thread (void *arg)
{
	gpointer gc_unsafe_stackdata;
	MonoThreadInfo *info;
	int small_id;
	gboolean result;
	gpointer handle;

	info = (MonoThreadInfo *) arg;
	g_assert (info);
	g_assert (mono_thread_info_is_current (info));
	g_assert (mono_thread_info_is_live (info));

	/* Pump the HP queue while the thread is alive.*/
	mono_thread_hazardous_try_free_some ();

	small_id = info->small_id;

	/* We only enter the GC unsafe region, as when exiting this function, the thread
	 * will be detached, and the current MonoThreadInfo* will be destroyed. */
	mono_threads_enter_gc_unsafe_region_unbalanced_with_info (info, &gc_unsafe_stackdata);

	THREADS_DEBUG ("unregistering info %p\n", info);

	mono_native_tls_set_value (thread_exited_key, GUINT_TO_POINTER (1));

	/*
	 * TLS destruction order is not reliable so small_id might be cleaned up
	 * before us.
	 */
#ifndef HAVE_KW_THREAD
	mono_native_tls_set_value (small_id_key, GUINT_TO_POINTER (info->small_id + 1));
#endif

	/* we need to duplicate it, as the info->handle is going
	 * to be closed when unregistering from the platform */
	handle = mono_threads_open_thread_handle (info->handle);

	/*
	First perform the callback that requires no locks.
	This callback has the potential of taking other locks, so we do it before.
	After it completes, the thread remains functional.
	*/
	if (threads_callbacks.thread_detach)
		threads_callbacks.thread_detach (info);

	mono_thread_info_suspend_lock_with_info (info);

	/*
	Now perform the callback that must be done under locks.
	This will render the thread useless and non-suspendable, so it must
	be done while holding the suspend lock to give no other thread chance
	to suspend it.
	*/
	if (threads_callbacks.thread_detach_with_lock)
		threads_callbacks.thread_detach_with_lock (info);

	/* The thread is no longer active, so unref its handle */
	mono_threads_close_thread_handle (info->handle);
	info->handle = NULL;

	result = mono_thread_info_remove (info);
	g_assert (result);
	mono_threads_transition_detach (info);

	mono_thread_info_suspend_unlock ();

	g_byte_array_free (info->stackdata, /*free_segment=*/TRUE);

	/*now it's safe to free the thread info.*/
	mono_thread_hazardous_try_free (info, free_thread_info);

	mono_thread_small_id_free (small_id);

	mono_threads_signal_thread_handle (handle);

	mono_threads_close_thread_handle (handle);

	mono_native_tls_set_value (thread_info_key, NULL);
}

static void
thread_exited_dtor (void *arg)
{
#if defined(__MACH__)
	/*
	 * Since we use pthread dtors to clean up thread data, if a thread
	 * is attached to the runtime by another pthread dtor after our dtor
	 * has ran, it will never be detached, leading to various problems
	 * since the thread ids etc. will be reused while they are still in
	 * the threads hashtables etc.
	 * Dtors are called in a loop until all user tls entries are 0,
	 * but the loop has a maximum count (4), so if we set the tls
	 * variable every time, it will remain set when system tls dtors
	 * are ran. This allows mono_thread_info_is_exiting () to detect
	 * whenever the thread is exiting, even if it is executed from a
	 * system tls dtor (i.e. obj-c dealloc methods).
	 */
	mono_native_tls_set_value (thread_exited_key, GUINT_TO_POINTER (1));
#endif
}

MonoThreadInfo*
mono_thread_info_current_unchecked (void)
{
	return mono_threads_inited ? (MonoThreadInfo*)mono_native_tls_get_value (thread_info_key) : NULL;
}


MonoThreadInfo*
mono_thread_info_current (void)
{
	MonoThreadInfo *info = (MonoThreadInfo*)mono_native_tls_get_value (thread_info_key);
	if (info)
		return info;

	info = mono_thread_info_lookup (mono_native_thread_id_get ()); /*info on HP1*/

	/*
	We might be called during thread cleanup, but we cannot be called after cleanup as happened.
	The way to distinguish between before, during and after cleanup is the following:

	-If the TLS key is set, cleanup has not begun;
	-If the TLS key is clean, but the thread remains registered, cleanup is in progress;
	-If the thread is nowhere to be found, cleanup has finished.

	We cannot function after cleanup since there's no way to ensure what will happen.
	*/
	g_assert (info);

	/*We're looking up the current thread which will not be freed until we finish running, so no need to keep it on a HP */
	mono_hazard_pointer_clear (mono_hazard_pointer_get (), 1);

	return info;
}

/*
 * mono_thread_info_get_small_id
 *
 * Retrieve the small ID for the current thread. This is a 16-bit value uniquely
 * identifying the current thread. Returns -1 if the current thread doesn't have
 * a small ID assigned.
 *
 * To ensure that the calling thread has a small ID assigned, call either
 * mono_thread_info_attach or mono_thread_info_register_small_id.
 */
int
mono_thread_info_get_small_id (void)
{
#ifdef HAVE_KW_THREAD
	return tls_small_id;
#else
	gpointer val = mono_native_tls_get_value (small_id_key);
	if (!val)
		return -1;
	return GPOINTER_TO_INT (val) - 1;
#endif
}

MonoLinkedListSet*
mono_thread_info_list_head (void)
{
	return &thread_list;
}

/**
 * mono_threads_attach_tools_thread
 *
 * Attach the current thread as a tool thread. DON'T USE THIS FUNCTION WITHOUT READING ALL DISCLAIMERS.
 *
 * A tools thread is a very special kind of thread that needs access to core runtime facilities but should
 * not be counted as a regular thread for high order facilities such as executing managed code or accessing
 * the managed heap.
 *
 * This is intended only to tools such as a profiler than needs to be able to use our lock-free support when
 * doing things like resolving backtraces in their background processing thread.
 */
void
mono_threads_attach_tools_thread (void)
{
	MonoThreadInfo *info;

	/* Must only be called once */
	g_assert (!mono_native_tls_get_value (thread_info_key));
	
	while (!mono_threads_inited) { 
		mono_thread_info_usleep (10);
	}

	info = mono_thread_info_attach ();
	g_assert (info);

	info->tools_thread = TRUE;
}

MonoThreadInfo*
mono_thread_info_attach (void)
{
	MonoThreadInfo *info;

#ifdef HOST_WIN32
	if (!mono_threads_inited)
	{
		/* This can happen from DllMain(DLL_THREAD_ATTACH) on Windows, if a
		 * thread is created before an embedding API user initialized Mono. */
		THREADS_DEBUG ("mono_thread_info_attach called before mono_thread_info_init\n");
		return NULL;
	}
#endif

	g_assert (mono_threads_inited);

	info = (MonoThreadInfo *) mono_native_tls_get_value (thread_info_key);
	if (!info) {
		info = (MonoThreadInfo *) g_malloc0 (thread_info_size);
		THREADS_DEBUG ("attaching %p\n", info);
		if (!register_thread (info)) {
			g_free (info);
			return NULL;
		}
	}

	return info;
}

void
mono_thread_info_detach (void)
{
	MonoThreadInfo *info;

#ifdef HOST_WIN32
	if (!mono_threads_inited)
	{
		/* This can happen from DllMain(THREAD_DETACH) on Windows, if a thread
		 * is created before an embedding API user initialized Mono. */
		THREADS_DEBUG ("mono_thread_info_detach called before mono_thread_info_init\n");
		return;
	}
#endif

	g_assert (mono_threads_inited);

	info = (MonoThreadInfo *) mono_native_tls_get_value (thread_info_key);
	if (info) {
		THREADS_DEBUG ("detaching %p\n", info);
		unregister_thread (info);
	}
}

gboolean
mono_thread_info_try_get_internal_thread_gchandle (MonoThreadInfo *info, guint32 *gchandle)
{
	g_assert (info);

	if (info->internal_thread_gchandle == G_MAXUINT32)
		return FALSE;

	*gchandle = info->internal_thread_gchandle;
	return TRUE;
}

void
mono_thread_info_set_internal_thread_gchandle (MonoThreadInfo *info, guint32 gchandle)
{
	g_assert (info);
	g_assert (gchandle != G_MAXUINT32);
	info->internal_thread_gchandle = gchandle;
}

void
mono_thread_info_unset_internal_thread_gchandle (THREAD_INFO_TYPE *info)
{
	g_assert (info);
	info->internal_thread_gchandle = G_MAXUINT32;
}

/*
 * mono_thread_info_is_exiting:
 *
 *   Return whenever the current thread is exiting, i.e. it is running pthread
 * dtors.
 */
gboolean
mono_thread_info_is_exiting (void)
{
#if defined(__MACH__)
	if (mono_native_tls_get_value (thread_exited_key) == GUINT_TO_POINTER (1))
		return TRUE;
#endif
	return FALSE;
}

#ifndef HOST_WIN32
static void
thread_info_key_dtor (void *arg)
{
	/* Put the MonoThreadInfo back for the duration of the
	 * unregister code.  In some circumstances the thread needs to
	 * take the GC lock which may block which requires a coop
	 * state transition. */
	mono_native_tls_set_value (thread_info_key, arg);
	unregister_thread (arg);
	mono_native_tls_set_value (thread_info_key, NULL);
}
#endif

void
mono_thread_info_init (size_t info_size)
{
	gboolean res;
	thread_info_size = info_size;
	char *sleepLimit;
#ifdef HOST_WIN32
	res = mono_native_tls_alloc (&thread_info_key, NULL);
	res = mono_native_tls_alloc (&thread_exited_key, NULL);
#else
	res = mono_native_tls_alloc (&thread_info_key, (void *) thread_info_key_dtor);
	res = mono_native_tls_alloc (&thread_exited_key, (void *) thread_exited_dtor);
#endif

	g_assert (res);

#ifndef HAVE_KW_THREAD
	res = mono_native_tls_alloc (&small_id_key, NULL);
#endif
	g_assert (res);

	if ((sleepLimit = g_getenv ("MONO_SLEEP_ABORT_LIMIT")) != NULL) {
		errno = 0;
		long threshold = strtol(sleepLimit, NULL, 10);
		if ((errno == 0) && (threshold >= 40))  {
			sleepAbortDuration = threshold;
			sleepWarnDuration = threshold / 20;
		} else
			g_warning("MONO_SLEEP_ABORT_LIMIT must be a number >= 40");
		g_free (sleepLimit);
	}

	mono_os_sem_init (&global_suspend_semaphore, 1);
	mono_os_sem_init (&suspend_semaphore, 0);
	mono_os_mutex_init (&join_mutex);

	mono_lls_init (&thread_list, NULL);
	mono_thread_smr_init ();
	mono_threads_suspend_init ();
	mono_threads_coop_init ();
	mono_threads_platform_init ();

#if defined(__MACH__)
	mono_mach_init (thread_info_key);
#endif

	mono_threads_inited = TRUE;

	g_assert (sizeof (MonoNativeThreadId) <= sizeof (uintptr_t));
}

void
mono_thread_info_callbacks_init (MonoThreadInfoCallbacks *callbacks)
{
	threads_callbacks = *callbacks;
}

void
mono_thread_info_signals_init (void)
{
	mono_threads_suspend_init_signals ();
}

void
mono_thread_info_runtime_init (MonoThreadInfoRuntimeCallbacks *callbacks)
{
	runtime_callbacks = *callbacks;
}

MonoThreadInfoRuntimeCallbacks *
mono_threads_get_runtime_callbacks (void)
{
	return &runtime_callbacks;
}

static gboolean
mono_thread_info_core_resume (MonoThreadInfo *info)
{
	gboolean res = FALSE;

	switch (mono_threads_transition_request_resume (info)) {
	case ResumeError:
		res = FALSE;
		break;
	case ResumeOk:
		res = TRUE;
		break;
	case ResumeInitSelfResume:
		resume_self_suspended (info);
		res = TRUE;
		break;
	case ResumeInitAsyncResume:
		resume_async_suspended (info);
		res = TRUE;
		break;
	case ResumeInitBlockingResume:
		resume_blocking_suspended (info);
		res = TRUE;
		break;
	}

	return res;
}

gboolean
mono_thread_info_resume (MonoNativeThreadId tid)
{
	gboolean result; /* don't initialize it so the compiler can catch unitilized paths. */
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();
	MonoThreadInfo *info;

	THREADS_SUSPEND_DEBUG ("RESUMING tid %p\n", (void*)tid);

	mono_thread_info_suspend_lock ();

	info = mono_thread_info_lookup (tid); /*info on HP1*/
	if (!info) {
		result = FALSE;
		goto cleanup;
	}

	result = mono_thread_info_core_resume (info);

	//Wait for the pending resume to finish
	mono_threads_wait_pending_operations ();

cleanup:
	mono_thread_info_suspend_unlock ();
	mono_hazard_pointer_clear (hp, 1);
	return result;
}

gboolean
mono_thread_info_begin_suspend (MonoThreadInfo *info)
{
	switch (mono_threads_transition_request_async_suspension (info)) {
	case AsyncSuspendAlreadySuspended:
	case AsyncSuspendBlocking:
		return TRUE;
	case AsyncSuspendWait:
		mono_threads_add_to_pending_operation_set (info);
		return TRUE;
	case AsyncSuspendInitSuspend:
		return begin_async_suspend (info, FALSE);
	default:
		g_assert_not_reached ();
	}
}

gboolean
mono_thread_info_begin_resume (MonoThreadInfo *info)
{
	return mono_thread_info_core_resume (info);
}

/*
FIXME fix cardtable WB to be out of line and check with the runtime if the target is not the
WB trampoline. Another option is to encode wb ranges in MonoJitInfo, but that is somewhat hard.
*/
static gboolean
is_thread_in_critical_region (MonoThreadInfo *info)
{
	gpointer stack_start;
	MonoThreadUnwindState *state;

	if (mono_threads_platform_in_critical_region (mono_thread_info_get_tid (info)))
		return TRUE;

	/* Are we inside a system critical region? */
	if (info->inside_critical_region)
		return TRUE;

	/* Are we inside a GC critical region? */
	if (threads_callbacks.thread_in_critical_region && threads_callbacks.thread_in_critical_region (info)) {
		return TRUE;
	}

	/* The target thread might be shutting down and the domain might be null, which means no managed code left to run. */
	state = mono_thread_info_get_suspend_state (info);
	if (!state->unwind_data [MONO_UNWIND_DATA_DOMAIN])
		return FALSE;

	stack_start = MONO_CONTEXT_GET_SP (&state->ctx);
	/* altstack signal handler, sgen can't handle them, so we treat them as critical */
	if (stack_start < info->stack_start_limit || stack_start >= info->stack_end)
		return TRUE;

	if (threads_callbacks.ip_in_critical_region)
		return threads_callbacks.ip_in_critical_region ((MonoDomain *) state->unwind_data [MONO_UNWIND_DATA_DOMAIN], (char *) MONO_CONTEXT_GET_IP (&state->ctx));

	return FALSE;
}

gboolean
mono_thread_info_in_critical_location (MonoThreadInfo *info)
{
	return is_thread_in_critical_region (info);
}

/*
The return value is only valid until a matching mono_thread_info_resume is called
*/
static MonoThreadInfo*
suspend_sync (MonoNativeThreadId tid, gboolean interrupt_kernel)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();
	MonoThreadInfo *info = mono_thread_info_lookup (tid); /*info on HP1*/
	if (!info)
		return NULL;

	switch (mono_threads_transition_request_async_suspension (info)) {
	case AsyncSuspendAlreadySuspended:
		mono_hazard_pointer_clear (hp, 1); //XXX this is questionable we got to clean the suspend/resume nonsense of critical sections
		return info;
	case AsyncSuspendWait:
		mono_threads_add_to_pending_operation_set (info);
		break;
	case AsyncSuspendInitSuspend:
		if (!begin_async_suspend (info, interrupt_kernel)) {
			mono_hazard_pointer_clear (hp, 1);
			return NULL;
		}
		break;
	case AsyncSuspendBlocking:
		if (interrupt_kernel)
			mono_threads_suspend_abort_syscall (info);

		return info;
	default:
		g_assert_not_reached ();
	}

	//Wait for the pending suspend to finish
	mono_threads_wait_pending_operations ();

	if (!check_async_suspend (info)) {
		mono_thread_info_core_resume (info);
		mono_threads_wait_pending_operations ();
		mono_hazard_pointer_clear (hp, 1);
		return NULL;
	}
	return info;
}

static MonoThreadInfo*
suspend_sync_nolock (MonoNativeThreadId id, gboolean interrupt_kernel)
{
	MonoThreadInfo *info = NULL;
	int sleep_duration = 0;
	for (;;) {
		if (!(info = suspend_sync (id, interrupt_kernel))) {
			mono_hazard_pointer_clear (mono_hazard_pointer_get (), 1);
			return NULL;
		}

		/*WARNING: We now are in interrupt context until we resume the thread. */
		if (!is_thread_in_critical_region (info))
			break;

		if (!mono_thread_info_core_resume (info)) {
			mono_hazard_pointer_clear (mono_hazard_pointer_get (), 1);
			return NULL;
		}
		THREADS_SUSPEND_DEBUG ("RESTARTED thread tid %p\n", (void*)id);

		/* Wait for the pending resume to finish */
		mono_threads_wait_pending_operations ();

		if (sleep_duration == 0)
			mono_thread_info_yield ();
		else
			g_usleep (sleep_duration);

		sleep_duration += 10;
	}
	return info;
}

void
mono_thread_info_safe_suspend_and_run (MonoNativeThreadId id, gboolean interrupt_kernel, MonoSuspendThreadCallback callback, gpointer user_data)
{
	int result;
	MonoThreadInfo *info = NULL;
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();

	THREADS_SUSPEND_DEBUG ("SUSPENDING tid %p\n", (void*)id);
	/*FIXME: unify this with self-suspend*/
	g_assert (id != mono_native_thread_id_get ());

	/* This can block during stw */
	mono_thread_info_suspend_lock ();
	mono_threads_begin_global_suspend ();

	info = suspend_sync_nolock (id, interrupt_kernel);
	if (!info)
		goto done;

	switch (result = callback (info, user_data)) {
	case MonoResumeThread:
		mono_hazard_pointer_set (hp, 1, info);
		mono_thread_info_core_resume (info);
		mono_threads_wait_pending_operations ();
		break;
	case KeepSuspended:
		g_assert (!mono_threads_is_coop_enabled ());
		break;
	default:
		g_error ("Invalid suspend_and_run callback return value %d", result);
	}

done:
	mono_hazard_pointer_clear (hp, 1);
	mono_threads_end_global_suspend ();
	mono_thread_info_suspend_unlock ();
}

/**
Inject an assynchronous call into the target thread. The target thread must be suspended and
only a single async call can be setup for a given suspend cycle.
This async call must cause stack unwinding as the current implementation doesn't save enough state
to resume execution of the top-of-stack function. It's an acceptable limitation since this is
currently used only to deliver exceptions.
*/
void
mono_thread_info_setup_async_call (MonoThreadInfo *info, void (*target_func)(void*), void *user_data)
{
	if (!mono_threads_is_coop_enabled ()) {
		/* In non-coop mode, an async call can only be setup on an async suspended thread, but in coop mode, a thread
		 * may be in blocking state, and will execute the async call when leaving the safepoint, leaving a gc safe
		 * region or entering a gc unsafe region */
		g_assert (mono_thread_info_run_state (info) == STATE_ASYNC_SUSPENDED);
	}
	/*FIXME this is a bad assert, we probably should do proper locking and fail if one is already set*/
	g_assert (!info->async_target);
	info->async_target = target_func;
	/* This is not GC tracked */
	info->user_data = user_data;
}

/*
The suspend lock is held during any suspend in progress.
A GC that has safepoints must take this lock as part of its
STW to make sure no unsafe pending suspend is in progress.   
*/

static void
mono_thread_info_suspend_lock_with_info (MonoThreadInfo *info)
{
	g_assert (info);
	g_assert (mono_thread_info_is_current (info));
	g_assert (mono_thread_info_is_live (info));

	MONO_ENTER_GC_SAFE_WITH_INFO(info);

	int res = mono_os_sem_wait (&global_suspend_semaphore, MONO_SEM_FLAGS_NONE);
	g_assert (res != -1);

	MONO_EXIT_GC_SAFE_WITH_INFO;
}

void
mono_thread_info_suspend_lock (void)
{
	MonoThreadInfo *info;
	gint res;

	info = mono_thread_info_current_unchecked ();
	if (info && mono_thread_info_is_live (info)) {
		mono_thread_info_suspend_lock_with_info (info);
		return;
	}

	/* mono_thread_info_suspend_lock () can be called from boehm-gc.c on_gc_notification before the new thread's
	 * start_wrapper calls mono_thread_info_attach but after pthread_create calls the start wrapper. */

	res = mono_os_sem_wait (&global_suspend_semaphore, MONO_SEM_FLAGS_NONE);
	g_assert (res != -1);
}

void
mono_thread_info_suspend_unlock (void)
{
	mono_os_sem_post (&global_suspend_semaphore);
}

/*
 * This is a very specific function whose only purpose is to
 * break a given thread from socket syscalls.
 *
 * This only exists because linux won't fail a call to connect
 * if the underlying is closed.
 *
 * TODO We should cleanup and unify this with the other syscall abort
 * facility.
 */
void
mono_thread_info_abort_socket_syscall_for_close (MonoNativeThreadId tid)
{
	MonoThreadHazardPointers *hp;
	MonoThreadInfo *info;

	if (tid == mono_native_thread_id_get ())
		return;

	hp = mono_hazard_pointer_get ();
	info = mono_thread_info_lookup (tid);
	if (!info)
		return;

	if (mono_thread_info_run_state (info) == STATE_DETACHED) {
		mono_hazard_pointer_clear (hp, 1);
		return;
	}

	mono_thread_info_suspend_lock ();
	mono_threads_begin_global_suspend ();

	mono_threads_suspend_abort_syscall (info);
	mono_threads_wait_pending_operations ();

	mono_hazard_pointer_clear (hp, 1);

	mono_threads_end_global_suspend ();
	mono_thread_info_suspend_unlock ();
}

/*
 * mono_thread_info_set_is_async_context:
 *
 *   Set whenever the current thread is in an async context. Some runtime functions might behave
 * differently while in an async context in order to be async safe.
 */
void
mono_thread_info_set_is_async_context (gboolean async_context)
{
	MonoThreadInfo *info = mono_thread_info_current ();

	if (info)
		info->is_async_context = async_context;
}

gboolean
mono_thread_info_is_async_context (void)
{
	MonoThreadInfo *info = mono_thread_info_current ();

	if (info)
		return info->is_async_context;
	else
		return FALSE;
}

/*
 * mono_thread_info_get_stack_bounds:
 *
 *   Return the address and size of the current threads stack. Return NULL as the 
 * stack address if the stack address cannot be determined.
 */
void
mono_thread_info_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	guint8 *current = (guint8 *)&stsize;
	mono_threads_platform_get_stack_bounds (staddr, stsize);
	if (!*staddr)
		return;

	/* Sanity check the result */
	g_assert ((current > *staddr) && (current < *staddr + *stsize));

	/* When running under emacs, sometimes staddr is not aligned to a page size */
	*staddr = (guint8*)((gssize)*staddr & ~(mono_pagesize () - 1));
}

gboolean
mono_thread_info_yield (void)
{
	return mono_threads_platform_yield ();
}

static mono_lazy_init_t sleep_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
static MonoCoopMutex sleep_mutex;
static MonoCoopCond sleep_cond;

static void
sleep_initialize (void)
{
	mono_coop_mutex_init (&sleep_mutex);
	mono_coop_cond_init (&sleep_cond);
}

static void
sleep_interrupt (gpointer data)
{
	mono_coop_mutex_lock (&sleep_mutex);
	mono_coop_cond_broadcast (&sleep_cond);
	mono_coop_mutex_unlock (&sleep_mutex);
}

static inline guint32
sleep_interruptable (guint32 ms, gboolean *alerted)
{
	gint64 now, end;

	g_assert (MONO_INFINITE_WAIT == G_MAXUINT32);

	g_assert (alerted);
	*alerted = FALSE;

	if (ms != MONO_INFINITE_WAIT)
		end = mono_msec_ticks() + ms;

	mono_lazy_initialize (&sleep_init, sleep_initialize);

	mono_coop_mutex_lock (&sleep_mutex);

	for (;;) {
		if (ms != MONO_INFINITE_WAIT) {
			now = mono_msec_ticks();
			if (now >= end)
				break;
		}

		mono_thread_info_install_interrupt (sleep_interrupt, NULL, alerted);
		if (*alerted) {
			mono_coop_mutex_unlock (&sleep_mutex);
			return WAIT_IO_COMPLETION;
		}

		if (ms != MONO_INFINITE_WAIT)
			mono_coop_cond_timedwait (&sleep_cond, &sleep_mutex, end - now);
		else
			mono_coop_cond_wait (&sleep_cond, &sleep_mutex);

		mono_thread_info_uninstall_interrupt (alerted);
		if (*alerted) {
			mono_coop_mutex_unlock (&sleep_mutex);
			return WAIT_IO_COMPLETION;
		}
	}

	mono_coop_mutex_unlock (&sleep_mutex);

	return 0;
}

gint
mono_thread_info_sleep (guint32 ms, gboolean *alerted)
{
	if (ms == 0) {
		MonoThreadInfo *info;

		mono_thread_info_yield ();

		info = mono_thread_info_current ();
		if (info && mono_thread_info_is_interrupt_state (info))
			return WAIT_IO_COMPLETION;

		return 0;
	}

	if (alerted)
		return sleep_interruptable (ms, alerted);

	MONO_ENTER_GC_SAFE;

	if (ms == MONO_INFINITE_WAIT) {
		do {
#ifdef HOST_WIN32
			Sleep (G_MAXUINT32);
#else
			sleep (G_MAXUINT32);
#endif
		} while (1);
	} else {
		int ret;
#if defined (__linux__) && !defined(HOST_ANDROID)
		struct timespec start, target;

		/* Use clock_nanosleep () to prevent time drifting problems when nanosleep () is interrupted by signals */
		ret = clock_gettime (CLOCK_MONOTONIC, &start);
		g_assert (ret == 0);

		target = start;
		target.tv_sec += ms / 1000;
		target.tv_nsec += (ms % 1000) * 1000000;
		if (target.tv_nsec > 999999999) {
			target.tv_nsec -= 999999999;
			target.tv_sec ++;
		}

		do {
			ret = clock_nanosleep (CLOCK_MONOTONIC, TIMER_ABSTIME, &target, NULL);
		} while (ret != 0);
#elif HOST_WIN32
		Sleep (ms);
#else
		struct timespec req, rem;

		req.tv_sec = ms / 1000;
		req.tv_nsec = (ms % 1000) * 1000000;

		do {
			memset (&rem, 0, sizeof (rem));
			ret = nanosleep (&req, &rem);
		} while (ret != 0);
#endif /* __linux__ */
	}

	MONO_EXIT_GC_SAFE;

	return 0;
}

gint
mono_thread_info_usleep (guint64 us)
{
	MONO_ENTER_GC_SAFE;
	g_usleep (us);
	MONO_EXIT_GC_SAFE;
	return 0;
}

gpointer
mono_thread_info_tls_get (THREAD_INFO_TYPE *info, MonoTlsKey key)
{
	return ((MonoThreadInfo*)info)->tls [key];
}

/*
 * mono_threads_info_tls_set:
 *
 *   Set the TLS key to VALUE in the info structure. This can be used to obtain
 * values of TLS variables for threads other than the current thread.
 * This should only be used for infrequently changing TLS variables, and it should
 * be paired with setting the real TLS variable since this provides no GC tracking.
 */
void
mono_thread_info_tls_set (THREAD_INFO_TYPE *info, MonoTlsKey key, gpointer value)
{
	((MonoThreadInfo*)info)->tls [key] = value;
}

/*
 * mono_thread_info_exit:
 *
 *   Exit the current thread.
 * This function doesn't return.
 */
void
mono_thread_info_exit (gsize exit_code)
{
	mono_thread_info_detach ();

	mono_threads_platform_exit (0);
}

/*
 * mono_threads_open_thread_handle:
 *
 *  Duplicate the handle. The handle needs to be closed by calling
 *  mono_threads_close_thread_handle () when it is no longer needed.
 */
MonoThreadHandle*
mono_threads_open_thread_handle (MonoThreadHandle *thread_handle)
{
	return mono_refcount_inc (thread_handle);
}

void
mono_threads_close_thread_handle (MonoThreadHandle *thread_handle)
{
	mono_refcount_dec (thread_handle);
}

static void
mono_threads_signal_thread_handle (MonoThreadHandle* thread_handle)
{
	mono_os_event_set (&thread_handle->event);
}

#define INTERRUPT_STATE ((MonoThreadInfoInterruptToken*) (size_t) -1)

struct _MonoThreadInfoInterruptToken {
	void (*callback) (gpointer data);
	gpointer data;
};

/*
 * mono_thread_info_install_interrupt: install an interruption token for the current thread.
 *
 *  - @callback: must be able to be called from another thread and always cancel the wait
 *  - @data: passed to the callback
 *  - @interrupted: will be set to TRUE if a token is already installed, FALSE otherwise
 *     if set to TRUE, it must mean that the thread is in interrupted state
 */
void
mono_thread_info_install_interrupt (void (*callback) (gpointer data), gpointer data, gboolean *interrupted)
{
	MonoThreadInfo *info;
	MonoThreadInfoInterruptToken *previous_token, *token;

	g_assert (callback);

	g_assert (interrupted);
	*interrupted = FALSE;

	info = mono_thread_info_current ();
	g_assert (info);

	/* The memory of this token can be freed at 2 places:
	 *  - if the token is not interrupted: it will be freed in uninstall, as info->interrupt_token has not been replaced
	 *     by the INTERRUPT_STATE flag value, and it still contains the pointer to the memory location
	 *  - if the token is interrupted: it will be freed in finish, as the token is now owned by the prepare/finish
	 *     functions, and info->interrupt_token does not contains a pointer to the memory anymore */
	token = g_new0 (MonoThreadInfoInterruptToken, 1);
	token->callback = callback;
	token->data = data;

	previous_token = (MonoThreadInfoInterruptToken *)mono_atomic_cas_ptr ((gpointer*) &info->interrupt_token, token, NULL);

	if (previous_token) {
		if (previous_token != INTERRUPT_STATE)
			g_error ("mono_thread_info_install_interrupt: previous_token should be INTERRUPT_STATE (%p), but it was %p", INTERRUPT_STATE, previous_token);

		g_free (token);

		*interrupted = TRUE;
	}

	THREADS_INTERRUPT_DEBUG ("interrupt install    tid %p token %p previous_token %p interrupted %s\n",
		mono_thread_info_get_tid (info), token, previous_token, *interrupted ? "TRUE" : "FALSE");
}

void
mono_thread_info_uninstall_interrupt (gboolean *interrupted)
{
	MonoThreadInfo *info;
	MonoThreadInfoInterruptToken *previous_token;

	g_assert (interrupted);
	*interrupted = FALSE;

	info = mono_thread_info_current ();
	g_assert (info);

	previous_token = (MonoThreadInfoInterruptToken *)mono_atomic_xchg_ptr ((gpointer*) &info->interrupt_token, NULL);

	/* only the installer can uninstall the token */
	g_assert (previous_token);

	if (previous_token == INTERRUPT_STATE) {
		/* if it is interrupted, then it is going to be freed in finish interrupt */
		*interrupted = TRUE;
	} else {
		g_free (previous_token);
	}

	THREADS_INTERRUPT_DEBUG ("interrupt uninstall  tid %p previous_token %p interrupted %s\n",
		mono_thread_info_get_tid (info), previous_token, *interrupted ? "TRUE" : "FALSE");
}

static MonoThreadInfoInterruptToken*
set_interrupt_state (MonoThreadInfo *info)
{
	MonoThreadInfoInterruptToken *token, *previous_token;

	g_assert (info);

	/* Atomically obtain the token the thread is
	* waiting on, and change it to a flag value. */

	do {
		previous_token = info->interrupt_token;

		/* Already interrupted */
		if (previous_token == INTERRUPT_STATE) {
			token = NULL;
			break;
		}

		token = previous_token;
	} while (mono_atomic_cas_ptr ((gpointer*) &info->interrupt_token, INTERRUPT_STATE, previous_token) != previous_token);

	return token;
}

/*
 * mono_thread_info_prepare_interrupt:
 *
 * The state of the thread info interrupt token is set to 'interrupted' which means that :
 *  - if the thread calls one of the WaitFor functions, the function will return with
 *     WAIT_IO_COMPLETION instead of waiting
 *  - if the thread was waiting when this function was called, the wait will be broken
 *
 * It is possible that the wait functions return WAIT_IO_COMPLETION, but the target thread
 * didn't receive the interrupt signal yet, in this case it should call the wait function
 * again. This essentially means that the target thread will busy wait until it is ready to
 * process the interruption.
 */
MonoThreadInfoInterruptToken*
mono_thread_info_prepare_interrupt (MonoThreadInfo *info)
{
	MonoThreadInfoInterruptToken *token;

	token = set_interrupt_state (info);

	THREADS_INTERRUPT_DEBUG ("interrupt prepare    tid %p token %p\n",
		mono_thread_info_get_tid (info), token);

	return token;
}

void
mono_thread_info_finish_interrupt (MonoThreadInfoInterruptToken *token)
{
	THREADS_INTERRUPT_DEBUG ("interrupt finish     token %p\n", token);

	if (token == NULL)
		return;

	g_assert (token->callback);

	token->callback (token->data);

	g_free (token);
}

void
mono_thread_info_self_interrupt (void)
{
	MonoThreadInfo *info;
	MonoThreadInfoInterruptToken *token;

	info = mono_thread_info_current ();
	g_assert (info);

	token = set_interrupt_state (info);
	g_assert (!token);

	THREADS_INTERRUPT_DEBUG ("interrupt self       tid %p\n",
		mono_thread_info_get_tid (info));
}

/* Clear the interrupted flag of the current thread, set with
 * mono_thread_info_self_interrupt, so it can wait again */
void
mono_thread_info_clear_self_interrupt ()
{
	MonoThreadInfo *info;
	MonoThreadInfoInterruptToken *previous_token;

	info = mono_thread_info_current ();
	g_assert (info);

	previous_token = (MonoThreadInfoInterruptToken *)mono_atomic_cas_ptr ((gpointer*) &info->interrupt_token, NULL, INTERRUPT_STATE);
	g_assert (previous_token == NULL || previous_token == INTERRUPT_STATE);

	THREADS_INTERRUPT_DEBUG ("interrupt clear self tid %p previous_token %p\n", mono_thread_info_get_tid (info), previous_token);
}

gboolean
mono_thread_info_is_interrupt_state (MonoThreadInfo *info)
{
	g_assert (info);
	return mono_atomic_load_ptr ((gpointer*) &info->interrupt_token) == INTERRUPT_STATE;
}

void
mono_thread_info_describe_interrupt_token (MonoThreadInfo *info, GString *text)
{
	g_assert (info);

	if (!mono_atomic_load_ptr ((gpointer*) &info->interrupt_token))
		g_string_append_printf (text, "not waiting");
	else if (mono_atomic_load_ptr ((gpointer*) &info->interrupt_token) == INTERRUPT_STATE)
		g_string_append_printf (text, "interrupted state");
	else
		g_string_append_printf (text, "waiting");
}

gboolean
mono_thread_info_is_current (MonoThreadInfo *info)
{
	return mono_thread_info_get_tid (info) == mono_native_thread_id_get ();
}

MonoThreadInfoWaitRet
mono_thread_info_wait_one_handle (MonoThreadHandle *thread_handle, guint32 timeout, gboolean alertable)
{
	MonoOSEventWaitRet res;

	res = mono_os_event_wait_one (&thread_handle->event, timeout, alertable);
	if (res == MONO_OS_EVENT_WAIT_RET_SUCCESS_0)
		return MONO_THREAD_INFO_WAIT_RET_SUCCESS_0;
	else if (res == MONO_OS_EVENT_WAIT_RET_ALERTED)
		return MONO_THREAD_INFO_WAIT_RET_ALERTED;
	else if (res == MONO_OS_EVENT_WAIT_RET_TIMEOUT)
		return MONO_THREAD_INFO_WAIT_RET_TIMEOUT;
	else
		g_error ("%s: unknown res value %d", __func__, res);
}

MonoThreadInfoWaitRet
mono_thread_info_wait_multiple_handle (MonoThreadHandle **thread_handles, gsize nhandles, MonoOSEvent *background_change_event, gboolean waitall, guint32 timeout, gboolean alertable)
{
	MonoOSEventWaitRet res;
	MonoOSEvent *thread_events [MONO_OS_EVENT_WAIT_MAXIMUM_OBJECTS];
	gint i;

	g_assert (nhandles <= MONO_OS_EVENT_WAIT_MAXIMUM_OBJECTS);
	if (background_change_event)
		g_assert (nhandles <= MONO_OS_EVENT_WAIT_MAXIMUM_OBJECTS - 1);

	for (i = 0; i < nhandles; ++i)
		thread_events [i] = &thread_handles [i]->event;

	if (background_change_event)
		thread_events [nhandles ++] = background_change_event;

	res = mono_os_event_wait_multiple (thread_events, nhandles, waitall, timeout, alertable);
	if (res >= MONO_OS_EVENT_WAIT_RET_SUCCESS_0 && res <= MONO_OS_EVENT_WAIT_RET_SUCCESS_0 + nhandles - 1)
		return MONO_THREAD_INFO_WAIT_RET_SUCCESS_0 + (res - MONO_OS_EVENT_WAIT_RET_SUCCESS_0);
	else if (res == MONO_OS_EVENT_WAIT_RET_ALERTED)
		return MONO_THREAD_INFO_WAIT_RET_ALERTED;
	else if (res == MONO_OS_EVENT_WAIT_RET_TIMEOUT)
		return MONO_THREAD_INFO_WAIT_RET_TIMEOUT;
	else
		g_error ("%s: unknown res value %d", __func__, res);
}

/*
 * mono_threads_join_mutex:
 *
 *   This mutex is used to avoid races between pthread_create () and pthread_join () on osx, see
 * https://bugzilla.xamarin.com/show_bug.cgi?id=50529
 * The code inside the lock should not block.
 */
void
mono_threads_join_lock (void)
{
#ifdef TARGET_OSX
	mono_os_mutex_lock (&join_mutex);
#endif
}

void
mono_threads_join_unlock (void)
{
#ifdef TARGET_OSX
	mono_os_mutex_unlock (&join_mutex);
#endif
}
