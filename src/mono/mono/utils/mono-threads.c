/*
 * mono-threads.c: Low-level threading
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */

#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-semaphore.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/domain-internals.h>

#include <errno.h>

#if defined(__MACH__)
#include <mono/utils/mach-support.h>
#endif

#define THREADS_DEBUG(...)
//#define THREADS_DEBUG(...) g_message(__VA_ARGS__)

/*
Mutex that makes sure only a single thread can be suspending others.
Suspend is a very racy operation since it requires restarting until
the target thread is not on an unsafe region.

We could implement this using critical regions, but would be much much
harder for an operation that is hardly performance critical.

The GC has to acquire this lock before starting a STW to make sure
a runtime suspend won't make it wronly see a thread in a safepoint
when it is in fact not.
*/
static MonoSemType global_suspend_semaphore;

static int thread_info_size;
static MonoThreadInfoCallbacks threads_callbacks;
static MonoThreadInfoRuntimeCallbacks runtime_callbacks;
static MonoNativeTlsKey thread_info_key, small_id_key;
static MonoLinkedListSet thread_list;
static gboolean disable_new_interrupt = FALSE;
static gboolean mono_threads_inited = FALSE;

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
	return mono_hazard_pointer_get_val (hp, 1);
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
	MonoThreadInfo *info = mem;

	MONO_SEM_DESTROY (&info->suspend_semaphore);
	MONO_SEM_DESTROY (&info->resume_semaphore);
	MONO_SEM_DESTROY (&info->finish_resume_semaphore);
	mono_threads_platform_free (info);

	g_free (info);
}

int
mono_thread_info_register_small_id (void)
{
	int small_id = mono_thread_small_id_alloc ();
	mono_native_tls_set_value (small_id_key, GUINT_TO_POINTER (small_id + 1));
	return small_id;
}

static void*
register_thread (MonoThreadInfo *info, gpointer baseptr)
{
	int small_id = mono_thread_info_register_small_id ();
	gboolean result;
	mono_thread_info_set_tid (info, mono_native_thread_id_get ());
	info->small_id = small_id;

	MONO_SEM_INIT (&info->suspend_semaphore, 1);
	MONO_SEM_INIT (&info->resume_semaphore, 0);
	MONO_SEM_INIT (&info->finish_resume_semaphore, 0);

	/*set TLS early so SMR works */
	mono_native_tls_set_value (thread_info_key, info);

	THREADS_DEBUG ("registering info %p tid %p small id %x\n", info, mono_thread_info_get_tid (info), info->small_id);

	if (threads_callbacks.thread_register) {
		if (threads_callbacks.thread_register (info, baseptr) == NULL) {
			g_warning ("thread registation failed\n");
			g_free (info);
			return NULL;
		}
	}

	mono_threads_platform_register (info);

	/*If this fail it means a given thread has been registered twice, which doesn't make sense. */
	result = mono_thread_info_insert (info);
	g_assert (result);
	return info;
}

static void
unregister_thread (void *arg)
{
	MonoThreadInfo *info = arg;
	int small_id = info->small_id;
	g_assert (info);

	THREADS_DEBUG ("unregistering info %p\n", info);

	/*
	 * TLS destruction order is not reliable so small_id might be cleaned up
	 * before us.
	 */
	mono_native_tls_set_value (small_id_key, GUINT_TO_POINTER (info->small_id + 1));

	/*
	The unregister callback is reposible for calling mono_threads_unregister_current_thread
	since it usually needs to be done in sync with the GC does a stop-the-world.
	*/
	if (threads_callbacks.thread_unregister)
		threads_callbacks.thread_unregister (info);
	else
		mono_threads_unregister_current_thread (info);

	/*now it's safe to free the thread info.*/
	mono_thread_hazardous_free_or_queue (info, free_thread_info, TRUE, FALSE);
	mono_thread_small_id_free (small_id);
}

/**
 * Removes the current thread from the thread list.
 * This must be called from the thread unregister callback and nowhere else.
 * The current thread must be passed as TLS might have already been cleaned up.
*/
void
mono_threads_unregister_current_thread (MonoThreadInfo *info)
{
	gboolean result;
	g_assert (mono_thread_info_get_tid (info) == mono_native_thread_id_get ());
	result = mono_thread_info_remove (info);
	g_assert (result);

}

MonoThreadInfo*
mono_thread_info_current (void)
{
	return mono_native_tls_get_value (thread_info_key);
}

static MonoThreadInfo*
mono_thread_info_current_slow (void)
{
	MonoThreadInfo *info = mono_thread_info_current ();
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

int
mono_thread_info_get_small_id (void)
{
	gpointer val = mono_native_tls_get_value (small_id_key);
	if (!val)
		return -1;
	return GPOINTER_TO_INT (val) - 1;
}

MonoLinkedListSet*
mono_thread_info_list_head (void)
{
	return &thread_list;
}

MonoThreadInfo*
mono_thread_info_attach (void *baseptr)
{
	MonoThreadInfo *info;
	if (!mono_threads_inited)
	{
		/* This can happen from DllMain(DLL_THREAD_ATTACH) on Windows, if a
		 * thread is created before an embedding API user initialized Mono. */
		THREADS_DEBUG ("mono_thread_info_attach called before mono_threads_init\n");
		return NULL;
	}
	info = mono_native_tls_get_value (thread_info_key);
	if (!info) {
		info = g_malloc0 (thread_info_size);
		THREADS_DEBUG ("attaching %p\n", info);
		if (!register_thread (info, baseptr))
			return NULL;
	} else if (threads_callbacks.thread_attach) {
		threads_callbacks.thread_attach (info);
	}
	return info;
}

void
mono_thread_info_dettach (void)
{
	MonoThreadInfo *info;
	if (!mono_threads_inited)
	{
		/* This can happen from DllMain(THREAD_DETACH) on Windows, if a thread
		 * is created before an embedding API user initialized Mono. */
		THREADS_DEBUG ("mono_thread_info_dettach called before mono_threads_init\n");
		return;
	}
	info = mono_native_tls_get_value (thread_info_key);
	if (info) {
		THREADS_DEBUG ("detaching %p\n", info);
		unregister_thread (info);
		mono_native_tls_set_value (thread_info_key, NULL);
	}
}

void
mono_threads_init (MonoThreadInfoCallbacks *callbacks, size_t info_size)
{
	gboolean res;
	threads_callbacks = *callbacks;
	thread_info_size = info_size;
#ifdef HOST_WIN32
	res = mono_native_tls_alloc (&thread_info_key, NULL);
#else
	res = mono_native_tls_alloc (&thread_info_key, unregister_thread);
#endif
	g_assert (res);

	res = mono_native_tls_alloc (&small_id_key, NULL);
	g_assert (res);

	MONO_SEM_INIT (&global_suspend_semaphore, 1);

	mono_lls_init (&thread_list, NULL);
	mono_thread_smr_init ();
	mono_threads_init_platform ();

#if defined(__MACH__)
	mono_mach_init (thread_info_key);
#endif

	mono_threads_inited = TRUE;

	g_assert (sizeof (MonoNativeThreadId) <= sizeof (uintptr_t));
}

void
mono_threads_runtime_init (MonoThreadInfoRuntimeCallbacks *callbacks)
{
	runtime_callbacks = *callbacks;
}

MonoThreadInfoCallbacks *
mono_threads_get_callbacks (void)
{
	return &threads_callbacks;
}

MonoThreadInfoRuntimeCallbacks *
mono_threads_get_runtime_callbacks (void)
{
	return &runtime_callbacks;
}

/*
The return value is only valid until a matching mono_thread_info_resume is called
*/
static MonoThreadInfo*
mono_thread_info_suspend_sync (MonoNativeThreadId tid, gboolean interrupt_kernel)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();	
	MonoThreadInfo *info = mono_thread_info_lookup (tid); /*info on HP1*/
	if (!info)
		return NULL;

	MONO_SEM_WAIT_UNITERRUPTIBLE (&info->suspend_semaphore);

	/*thread is on the process of detaching*/
	if (mono_thread_info_run_state (info) > STATE_RUNNING) {
		mono_hazard_pointer_clear (hp, 1);
		return NULL;
	}

	THREADS_DEBUG ("suspend %x IN COUNT %d\n", tid, info->suspend_count);

	if (info->suspend_count) {
		++info->suspend_count;
		mono_hazard_pointer_clear (hp, 1);
		MONO_SEM_POST (&info->suspend_semaphore);
		return info;
	}

	if (!mono_threads_core_suspend (info)) {
		MONO_SEM_POST (&info->suspend_semaphore);
		mono_hazard_pointer_clear (hp, 1);
		return NULL;
	}

	if (interrupt_kernel) 
		mono_threads_core_interrupt (info);

	++info->suspend_count;
	info->thread_state |= STATE_SUSPENDED;
	MONO_SEM_POST (&info->suspend_semaphore);
	mono_hazard_pointer_clear (hp, 1);

	return info;
}

void
mono_thread_info_self_suspend (void)
{
	gboolean ret;
	MonoThreadInfo *info = mono_thread_info_current ();
	if (!info)
		return;

	MONO_SEM_WAIT_UNITERRUPTIBLE (&info->suspend_semaphore);

	THREADS_DEBUG ("self suspend IN COUNT %d\n", info->suspend_count);

	g_assert (info->suspend_count == 0);
	++info->suspend_count;

	info->thread_state |= STATE_SELF_SUSPENDED;

	ret = mono_threads_get_runtime_callbacks ()->thread_state_init_from_sigctx (&info->suspend_state, NULL);
	g_assert (ret);

	MONO_SEM_POST (&info->suspend_semaphore);

	MONO_SEM_WAIT_UNITERRUPTIBLE (&info->resume_semaphore);

	g_assert (!info->async_target); /*FIXME this should happen normally for suspend. */
	MONO_SEM_POST (&info->finish_resume_semaphore);
}

static gboolean
mono_thread_info_resume_internal (MonoThreadInfo *info)
{
	gboolean result;
	if (mono_thread_info_suspend_state (info) == STATE_SELF_SUSPENDED) {
		MONO_SEM_POST (&info->resume_semaphore);
		MONO_SEM_WAIT_UNITERRUPTIBLE (&info->finish_resume_semaphore);
		result = TRUE;
	} else {
		result = mono_threads_core_resume (info);
	}
	info->thread_state &= ~SUSPEND_STATE_MASK;
	return result;
}

gboolean
mono_thread_info_resume (MonoNativeThreadId tid)
{
	gboolean result = TRUE;
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();	
	MonoThreadInfo *info = mono_thread_info_lookup (tid); /*info on HP1*/
	if (!info)
		return FALSE;

	MONO_SEM_WAIT_UNITERRUPTIBLE (&info->suspend_semaphore);

	THREADS_DEBUG ("resume %x IN COUNT %d\n",tid, info->suspend_count);

	if (info->suspend_count <= 0) {
		MONO_SEM_POST (&info->suspend_semaphore);
		mono_hazard_pointer_clear (hp, 1);
		return FALSE;
	}

	/*
	 * The theory here is that if we manage to suspend the thread it means it did not
	 * start cleanup since it take the same lock. 
	*/
	g_assert (mono_thread_info_get_tid (info));

	if (--info->suspend_count == 0)
		result = mono_thread_info_resume_internal (info);

	MONO_SEM_POST (&info->suspend_semaphore);
	mono_hazard_pointer_clear (hp, 1);
	mono_atomic_store_release (&mono_thread_info_current_slow ()->inside_critical_region, FALSE);

	return result;
}

void
mono_thread_info_finish_suspend (void)
{
	mono_atomic_store_release (&mono_thread_info_current_slow ()->inside_critical_region, FALSE);
}

/*
FIXME fix cardtable WB to be out of line and check with the runtime if the target is not the
WB trampoline. Another option is to encode wb ranges in MonoJitInfo, but that is somewhat hard.
*/
static gboolean
is_thread_in_critical_region (MonoThreadInfo *info)
{
	MonoMethod *method;
	MonoJitInfo *ji;

	if (info->inside_critical_region)
		return TRUE;

	ji = mono_jit_info_table_find (
		info->suspend_state.unwind_data [MONO_UNWIND_DATA_DOMAIN],
		MONO_CONTEXT_GET_IP (&info->suspend_state.ctx));

	if (!ji)
		return FALSE;

	method = ji->method;

	return threads_callbacks.mono_method_is_critical (method);
}

/*
WARNING:
If we are trying to suspend a target that is on a critical region
and running a syscall we risk looping forever if @interrupt_kernel is FALSE.
So, be VERY carefull in calling this with @interrupt_kernel == FALSE.
*/
MonoThreadInfo*
mono_thread_info_safe_suspend_sync (MonoNativeThreadId id, gboolean interrupt_kernel)
{
	MonoThreadInfo *info = NULL;
	int sleep_duration = 0;

	/*FIXME: unify this with self-suspend*/
	g_assert (id != mono_native_thread_id_get ());

	mono_thread_info_suspend_lock ();

	for (;;) {
		if (!(info = mono_thread_info_suspend_sync (id, interrupt_kernel))) {
			g_warning ("failed to suspend thread %p, hopefully it is dead", (gpointer)id);
			mono_thread_info_suspend_unlock ();
			return NULL;
		}
		/*WARNING: We now are in interrupt context until we resume the thread. */
		if (!is_thread_in_critical_region (info))
			break;

		if (!mono_thread_info_resume (id)) {
			g_warning ("failed to result thread %p, hopefully it is dead", (gpointer)id);
			mono_thread_info_suspend_unlock ();
			return NULL;
		}
		THREADS_DEBUG ("restarted thread %p\n", (gpointer)id);

		if (!sleep_duration) {
#ifdef HOST_WIN32
			SwitchToThread ();
#else
			sched_yield ();
#endif
		}
		else {
			g_usleep (sleep_duration);
		}
		sleep_duration += 10;
	}

	mono_atomic_store_release (&mono_thread_info_current_slow ()->inside_critical_region, TRUE);

	mono_thread_info_suspend_unlock ();
	return info;
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
	g_assert (info->suspend_count);
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
void
mono_thread_info_suspend_lock (void)
{
	MONO_SEM_WAIT_UNITERRUPTIBLE (&global_suspend_semaphore);
}

void
mono_thread_info_suspend_unlock (void)
{
	MONO_SEM_POST (&global_suspend_semaphore);
}

void
mono_thread_info_disable_new_interrupt (gboolean disable)
{
	disable_new_interrupt = disable;
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
	
	if (tid == mono_native_thread_id_get () || !mono_threads_core_needs_abort_syscall ())
		return;

	hp = mono_hazard_pointer_get ();	
	info = mono_thread_info_lookup (tid); /*info on HP1*/
	if (!info)
		return;

	if (mono_thread_info_run_state (info) > STATE_RUNNING) {
		mono_hazard_pointer_clear (hp, 1);
		return;
	}

	mono_thread_info_suspend_lock ();

	mono_threads_core_abort_syscall (info);

	mono_hazard_pointer_clear (hp, 1);
	mono_thread_info_suspend_unlock ();
}

/*
Disabled by default for now.
To enable this we need mini to implement the callbacks by MonoThreadInfoRuntimeCallbacks
which means mono-context and setup_async_callback, and we need a mono-threads backend.
*/
gboolean
mono_thread_info_new_interrupt_enabled (void)
{
	/*We need STW gc events to work correctly*/
#if defined (HAVE_BOEHM_GC) && !defined (USE_INCLUDED_LIBGC)
	return FALSE;
#endif
	/*port not done*/
#if defined(HOST_WIN32)
	return FALSE;
#endif
#if defined (__i386__)
	return !disable_new_interrupt;
#endif
	return FALSE;
}
