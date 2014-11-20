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
#include <mono/utils/mono-mmap.h>
#include <mono/utils/atomic.h>

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

static size_t thread_info_size;
static MonoThreadInfoCallbacks threads_callbacks;
static MonoThreadInfoRuntimeCallbacks runtime_callbacks;
static MonoNativeTlsKey thread_info_key, thread_exited_key, small_id_key;
static MonoLinkedListSet thread_list;
static gboolean disable_new_interrupt = FALSE;
static gboolean mono_threads_inited = FALSE;

static void mono_threads_unregister_current_thread (MonoThreadInfo *info);


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
			// g_warning ("thread registation failed\n");
			g_free (info);
			return NULL;
		}
	}

	mono_threads_platform_register (info);
	info->thread_state = STATE_RUNNING;
	mono_thread_info_suspend_lock ();
	/*If this fail it means a given thread has been registered twice, which doesn't make sense. */
	result = mono_thread_info_insert (info);
	g_assert (result);
	mono_thread_info_suspend_unlock ();
	return info;
}

static void
unregister_thread (void *arg)
{
	MonoThreadInfo *info = arg;
	int small_id = info->small_id;
	g_assert (info);

	THREADS_DEBUG ("unregistering info %p\n", info);

	mono_native_tls_set_value (thread_exited_key, GUINT_TO_POINTER (1));

	mono_threads_core_unregister (info);

	/*
	 * TLS destruction order is not reliable so small_id might be cleaned up
	 * before us.
	 */
	mono_native_tls_set_value (small_id_key, GUINT_TO_POINTER (info->small_id + 1));

	info->thread_state = STATE_SHUTTING_DOWN;

	/*
	First perform the callback that requires no locks.
	This callback has the potential of taking other locks, so we do it before.
	After it completes, the thread remains functional.
	*/
	if (threads_callbacks.thread_detach)
		threads_callbacks.thread_detach (info);

	mono_thread_info_suspend_lock ();

	/*
	Now perform the callback that must be done under locks.
	This will render the thread useless and non-suspendable, so it must
	be done while holding the suspend lock to give no other thread chance
	to suspend it.
	*/
	if (threads_callbacks.thread_unregister)
		threads_callbacks.thread_unregister (info);
	mono_threads_unregister_current_thread (info);

	info->thread_state = STATE_DEAD;
	mono_thread_info_suspend_unlock ();

	/*now it's safe to free the thread info.*/
	mono_thread_hazardous_free_or_queue (info, free_thread_info, TRUE, FALSE);
	mono_thread_small_id_free (small_id);
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

/**
 * Removes the current thread from the thread list.
 * This must be called from the thread unregister callback and nowhere else.
 * The current thread must be passed as TLS might have already been cleaned up.
*/
static void
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
	int dummy = 0;
	MonoThreadInfo *info;

	/* Must only be called once */
	g_assert (!mono_native_tls_get_value (thread_info_key));

	info = mono_thread_info_attach (&dummy);
	info->tools_thread = TRUE;
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
mono_thread_info_detach (void)
{
	MonoThreadInfo *info;
	if (!mono_threads_inited)
	{
		/* This can happen from DllMain(THREAD_DETACH) on Windows, if a thread
		 * is created before an embedding API user initialized Mono. */
		THREADS_DEBUG ("mono_thread_info_detach called before mono_threads_init\n");
		return;
	}
	info = mono_native_tls_get_value (thread_info_key);
	if (info) {
		THREADS_DEBUG ("detaching %p\n", info);
		unregister_thread (info);
		mono_native_tls_set_value (thread_info_key, NULL);
	}
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

void
mono_threads_init (MonoThreadInfoCallbacks *callbacks, size_t info_size)
{
	gboolean res;
	threads_callbacks = *callbacks;
	thread_info_size = info_size;
#ifdef HOST_WIN32
	res = mono_native_tls_alloc (&thread_info_key, NULL);
	res = mono_native_tls_alloc (&thread_exited_key, NULL);
#else
	res = mono_native_tls_alloc (&thread_info_key, unregister_thread);
	res = mono_native_tls_alloc (&thread_exited_key, thread_exited_dtor);
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
mono_thread_info_suspend_sync (MonoNativeThreadId tid, gboolean interrupt_kernel, const char **error_condition)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();	
	MonoThreadInfo *info = mono_thread_info_lookup (tid); /*info on HP1*/
	if (!info) {
		*error_condition = "Thread not found";
		return NULL;
	}

	MONO_SEM_WAIT_UNITERRUPTIBLE (&info->suspend_semaphore);

	/*thread is on the process of detaching*/
	if (mono_thread_info_run_state (info) > STATE_RUNNING) {
		mono_hazard_pointer_clear (hp, 1);
		*error_condition = "Thread is detaching";
		return NULL;
	}

	THREADS_DEBUG ("suspend %x IN COUNT %d\n", tid, info->suspend_count);

	if (info->suspend_count) {
		++info->suspend_count;
		mono_hazard_pointer_clear (hp, 1);
		MONO_SEM_POST (&info->suspend_semaphore);
		return info;
	}

	if (!mono_threads_core_suspend (info, interrupt_kernel)) {
		MONO_SEM_POST (&info->suspend_semaphore);
		mono_hazard_pointer_clear (hp, 1);
		*error_condition = "Could not suspend thread";
		return NULL;
	}

	if (interrupt_kernel) 
		mono_threads_core_interrupt (info);

	++info->suspend_count;
	info->thread_state |= STATE_SUSPENDED;
	MONO_SEM_POST (&info->suspend_semaphore);

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
mono_thread_info_core_resume (MonoThreadInfo *info)
{
	gboolean result;
	MonoNativeThreadId tid = mono_thread_info_get_tid (info);
	if (info->create_suspended) {
		/* Have to special case this, as the normal suspend/resume pair are racy, they don't work if he resume is received before the suspend */
		info->create_suspended = FALSE;
		mono_threads_core_resume_created (info, tid);
		return TRUE;
	}

	MONO_SEM_WAIT_UNITERRUPTIBLE (&info->suspend_semaphore);

	THREADS_DEBUG ("resume %x IN COUNT %d\n", tid, info->suspend_count);

	if (info->suspend_count <= 0) {
		MONO_SEM_POST (&info->suspend_semaphore);
		return FALSE;
	}

	/*
	 * The theory here is that if we manage to suspend the thread it means it did not
	 * start cleanup since it take the same lock. 
	*/
	g_assert (mono_thread_info_get_tid (info));

	if (--info->suspend_count == 0) {
		if (mono_thread_info_suspend_state (info) == STATE_SELF_SUSPENDED) {
			MONO_SEM_POST (&info->resume_semaphore);
			MONO_SEM_WAIT_UNITERRUPTIBLE (&info->finish_resume_semaphore);
			result = TRUE;
		} else {
			result = mono_threads_core_resume (info);
		}
		info->thread_state &= ~SUSPEND_STATE_MASK;
	} else {
		result = TRUE;
	}

	MONO_SEM_POST (&info->suspend_semaphore);
	return result;
}

gboolean
mono_thread_info_resume (MonoNativeThreadId tid)
{
	gboolean result; /* don't initialize it so the compiler can catch unitilized paths. */
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();
	MonoThreadInfo *info = mono_thread_info_lookup (tid); /*info on HP1*/

	if (!info) {
		result = FALSE;
		goto cleanup;
	}
	result = mono_thread_info_core_resume (info);

cleanup:
	mono_hazard_pointer_clear (hp, 1);
	return result;
}

void
mono_thread_info_finish_suspend (MonoThreadInfo *info)
{
	mono_atomic_store_release (&mono_thread_info_current ()->inside_critical_region, FALSE);
}

void
mono_thread_info_finish_suspend_and_resume (MonoThreadInfo *info)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();

	/*Resume can access info after the target has resumed, so we must ensure it won't touch freed memory. */
	mono_hazard_pointer_set (hp, 1, info);
	mono_thread_info_core_resume (info);
	mono_hazard_pointer_clear (hp, 1);

	mono_atomic_store_release (&mono_thread_info_current ()->inside_critical_region, FALSE);
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

	/* The target thread might be shutting down and the domain might be null, which means no managed code left to run. */
	if (!info->suspend_state.unwind_data [MONO_UNWIND_DATA_DOMAIN])
		return FALSE;

	ji = mono_jit_info_table_find (
		info->suspend_state.unwind_data [MONO_UNWIND_DATA_DOMAIN],
		MONO_CONTEXT_GET_IP (&info->suspend_state.ctx));

	if (!ji)
		return FALSE;

	method = mono_jit_info_get_method (ji);

	return threads_callbacks.mono_method_is_critical (method);
}

/*
WARNING:
If we are trying to suspend a target that is on a critical region
and running a syscall we risk looping forever if @interrupt_kernel is FALSE.
So, be VERY carefull in calling this with @interrupt_kernel == FALSE.

Info is not put on a hazard pointer as a suspended thread cannot exit and be freed.

This function MUST be matched with mono_thread_info_finish_suspend or mono_thread_info_finish_suspend_and_resume
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
		const char *suspend_error = "Unknown error";
		if (!(info = mono_thread_info_suspend_sync (id, interrupt_kernel, &suspend_error))) {
			// g_warning ("failed to suspend thread %p due to %s, hopefully it is dead", (gpointer)id, suspend_error);
			mono_thread_info_suspend_unlock ();
			return NULL;
		}
		/*WARNING: We now are in interrupt context until we resume the thread. */
		if (!is_thread_in_critical_region (info))
			break;

		if (!mono_thread_info_core_resume (info)) {
			// g_warning ("failed to resume thread %p, hopefully it is dead", (gpointer)id);
			mono_hazard_pointer_clear (mono_hazard_pointer_get (), 1);
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

	/* XXX this clears HP 1, so we restated it again */
	mono_atomic_store_release (&mono_thread_info_current ()->inside_critical_region, TRUE);
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
#if defined(HOST_WIN32)
	return !disable_new_interrupt;
#endif
#if defined (__i386__) || defined(__x86_64__)
	return !disable_new_interrupt;
#endif
#if defined(__arm__)
	return !disable_new_interrupt;
#endif
	return FALSE;
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
 * mono_threads_create_thread:
 *
 *   Create a new thread executing START with argument ARG. Store its id into OUT_TID.
 * Returns: a windows or io-layer handle for the thread.
 */
HANDLE
mono_threads_create_thread (LPTHREAD_START_ROUTINE start, gpointer arg, guint32 stack_size, guint32 creation_flags, MonoNativeThreadId *out_tid)
{
	return mono_threads_core_create_thread (start, arg, stack_size, creation_flags, out_tid);
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
	mono_threads_core_get_stack_bounds (staddr, stsize);
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
	return mono_threads_core_yield ();
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
mono_thread_info_exit (void)
{
	mono_threads_core_exit (0);
}

/*
 * mono_thread_info_open_handle:
 *
 *   Return a io-layer/win32 handle for the current thread.
 * The handle need to be closed by calling CloseHandle () when it is no
 * longer needed.
 */
HANDLE
mono_thread_info_open_handle (void)
{
	return mono_threads_core_open_handle ();
}

/*
 * mono_threads_open_thread_handle:
 *
 *   Return a io-layer/win32 handle for the thread identified by HANDLE/TID.
 * The handle need to be closed by calling CloseHandle () when it is no
 * longer needed.
 */
HANDLE
mono_threads_open_thread_handle (HANDLE handle, MonoNativeThreadId tid)
{
	return mono_threads_core_open_thread_handle (handle, tid);
}

void
mono_thread_info_set_name (MonoNativeThreadId tid, const char *name)
{
	mono_threads_core_set_name (tid, name);
}

/*
 * mono_thread_info_prepare_interrupt:
 *
 *   See wapi_prepare_interrupt ().
 */
gpointer
mono_thread_info_prepare_interrupt (HANDLE thread_handle)
{
	return mono_threads_core_prepare_interrupt (thread_handle);
}

void
mono_thread_info_finish_interrupt (gpointer wait_handle)
{
	mono_threads_core_finish_interrupt (wait_handle);
}

void
mono_thread_info_interrupt (HANDLE thread_handle)
{
	gpointer wait_handle;

	wait_handle = mono_thread_info_prepare_interrupt (thread_handle);
	mono_thread_info_finish_interrupt (wait_handle);
}
	
void
mono_thread_info_self_interrupt (void)
{
	mono_threads_core_self_interrupt ();
}

void
mono_thread_info_clear_interruption (void)
{
	mono_threads_core_clear_interruption ();
}

/* info must be self or be held in a hazard pointer. */
gboolean
mono_threads_add_async_job (MonoThreadInfo *info, MonoAsyncJob job)
{
	MonoAsyncJob old_job;
	do {
		old_job = info->service_requests;
		if (old_job & job)
			return FALSE;
	} while (InterlockedCompareExchange (&info->service_requests, old_job | job, old_job) != old_job);
	return TRUE;
}

MonoAsyncJob
mono_threads_consume_async_jobs (void)
{
	MonoThreadInfo *info = (MonoThreadInfo*)mono_native_tls_get_value (thread_info_key);

	if (!info)
		return 0;

	return InterlockedExchange (&info->service_requests, 0);
}
