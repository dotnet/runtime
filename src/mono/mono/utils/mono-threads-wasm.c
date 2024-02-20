#include "config.h"

#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-compiler.h>

#if defined (USE_WASM_BACKEND)

#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-threads-api.h>
#include <mono/utils/mono-threads-debug.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/checked-build.h>

#include <glib.h>

#ifdef HOST_BROWSER

#include <mono/utils/mono-threads-wasm.h>

#include <emscripten.h>
#include <emscripten/stack.h>
#ifndef DISABLE_THREADS
#include <emscripten/threading.h>
#include <mono/metadata/threads-types.h>
#endif


#define round_down(addr, val) ((void*)((addr) & ~((val) - 1)))

EMSCRIPTEN_KEEPALIVE
static int
wasm_get_stack_base (void)
{
	// wasm-mt: add MONO_ENTER_GC_UNSAFE / MONO_EXIT_GC_UNSAFE if this function becomes more complex
	return emscripten_stack_get_end ();
}

EMSCRIPTEN_KEEPALIVE
static int
wasm_get_stack_size (void)
{
	// wasm-mt: add MONO_ENTER_GC_UNSAFE / MONO_EXIT_GC_UNSAFE if this function becomes more complex
	return (guint8*)emscripten_stack_get_base () - (guint8*)emscripten_stack_get_end ();
}

#else /* HOST_BROWSER -> WASI */

// TODO after https://github.com/llvm/llvm-project/commit/1532be98f99384990544bd5289ba339bca61e15b
// use __stack_low && __stack_high
// see mono-threads-wasi.S
uintptr_t get_wasm_heap_base(void);
uintptr_t get_wasm_data_end(void);

static int
wasm_get_stack_size (void)
{
	/*
	 * | -- increasing address ---> |
	 * | data (data_end)| stack |(heap_base) heap |
	 */
	size_t heap_base = get_wasm_heap_base();
	size_t data_end = get_wasm_data_end();
	size_t max_stack_size = heap_base - data_end;

	g_assert (data_end > 0);
	g_assert (heap_base > data_end);

	// this is the max available stack size size,
	// return a 16-byte aligned smaller size
	return max_stack_size & ~0xF;
}

static int
wasm_get_stack_base (void)
{
	return get_wasm_data_end();
	// this will need further change for multithreading as the stack will allocated be per thread at different addresses
}

#endif /* HOST_BROWSER */

int
mono_thread_info_get_system_max_stack_size (void)
{
	return wasm_get_stack_size ();
}


void
mono_threads_suspend_init_signals (void)
{
}

void
mono_threads_suspend_init (void)
{
#ifndef DISABLE_THREADS
	// wasm threading require full cooperative suspend
	g_assert (mono_threads_is_cooperative_suspension_enabled ());
#endif
}

void
mono_threads_suspend_register (MonoThreadInfo *info)
{
}

gboolean
mono_threads_suspend_begin_async_resume (MonoThreadInfo *info)
{
	return TRUE;
}

void
mono_threads_suspend_free (MonoThreadInfo *info)
{
}

gboolean
mono_threads_suspend_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	return TRUE;
}

gboolean
mono_threads_suspend_check_suspend_result (MonoThreadInfo *info)
{
	return TRUE;
}

void
mono_threads_suspend_abort_syscall (MonoThreadInfo *info)
{
}

gboolean
mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2)
{
	return id1 == id2;
}

MonoNativeThreadId
mono_native_thread_id_get (void)
{
#ifdef __EMSCRIPTEN_PTHREADS__
	return pthread_self ();
#else
	return (MonoNativeThreadId)1;
#endif
}

guint64
mono_native_thread_os_id_get (void)
{
#ifdef __EMSCRIPTEN_PTHREADS__
	return (guint64)pthread_self ();
#else
	return 1;
#endif
}

MONO_API gboolean
mono_native_thread_create (MonoNativeThreadId *tid, gpointer func, gpointer arg)
{
#ifdef __EMSCRIPTEN_PTHREADS__
	return pthread_create (tid, NULL, (void *(*)(void *)) func, arg) == 0;
#else
	g_error ("WASM doesn't support threading");
#endif
}

void
mono_native_thread_set_name (MonoNativeThreadId tid, const char *name)
{
#ifndef DISABLE_THREADS
	// note there is also emscripten_set_thread_name, but it only changes the name for emscripten profiler
	// this only sets the name for the current thread
	mono_wasm_pthread_set_name (name);
#endif
}

gboolean
mono_native_thread_join (MonoNativeThreadId tid)
{
#ifdef __EMSCRIPTEN_PTHREADS__
	void *res;

	return !pthread_join (tid, &res);
#else
	g_assert_not_reached ();
#endif
}

gboolean
mono_threads_platform_yield (void)
{
	return TRUE;
}

void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	int tmp;
#ifdef __EMSCRIPTEN_PTHREADS__
	pthread_attr_t attr;
	gint res;

	*staddr = NULL;
	*stsize = (size_t)-1;

	res = pthread_attr_init (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_attr_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_getattr_np (pthread_self (), &attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_getattr_np failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_attr_getstack (&attr, (void**)staddr, stsize);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_attr_getstack failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_attr_destroy (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_attr_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	if (*staddr == NULL) {
		*staddr = (guint8*)wasm_get_stack_base ();
		*stsize = wasm_get_stack_size ();
	}
#else
	*staddr = (guint8*)wasm_get_stack_base ();
	*stsize = wasm_get_stack_size ();
#endif

	g_assert ((guint8*)&tmp > *staddr);
	g_assert ((guint8*)&tmp < (guint8*)*staddr + *stsize);
}

gboolean
mono_thread_platform_create_thread (MonoThreadStart thread_fn, gpointer thread_data, gsize* const stack_size, MonoNativeThreadId *tid)
{
#ifdef __EMSCRIPTEN_PTHREADS__
	pthread_attr_t attr;
	pthread_t thread;
	gint res;

	res = pthread_attr_init (&attr);
	if (res != 0)
		g_error ("%s: pthread_attr_init failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);

#if 0
	gsize set_stack_size;

	if (stack_size)
		set_stack_size = *stack_size;
	else
		set_stack_size = 0;

#ifdef HAVE_PTHREAD_ATTR_SETSTACKSIZE
	if (set_stack_size == 0) {
#if HAVE_VALGRIND_MEMCHECK_H
		if (RUNNING_ON_VALGRIND)
			set_stack_size = 1 << 20;
		else
			set_stack_size = (SIZEOF_VOID_P / 4) * 1024 * 1024;
#else
		set_stack_size = (SIZEOF_VOID_P / 4) * 1024 * 1024;
#endif
	}

#ifdef PTHREAD_STACK_MIN
	if (set_stack_size < PTHREAD_STACK_MIN)
		set_stack_size = PTHREAD_STACK_MIN;
#endif

	res = pthread_attr_setstacksize (&attr, set_stack_size);
	if (res != 0)
		g_error ("%s: pthread_attr_setstacksize failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);
#endif /* HAVE_PTHREAD_ATTR_SETSTACKSIZE */
#endif

	/* Actually start the thread */
	res = pthread_create (&thread, &attr, (gpointer (*)(gpointer)) thread_fn, thread_data);
	if (res) {
		res = pthread_attr_destroy (&attr);
		if (res != 0)
			g_error ("%s: pthread_attr_destroy failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);

		return FALSE;
	}

	if (tid)
		*tid = thread;

#if 0
	if (stack_size) {
		res = pthread_attr_getstacksize (&attr, stack_size);
		if (res != 0)
			g_error ("%s: pthread_attr_getstacksize failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);
	}
#endif

	res = pthread_attr_destroy (&attr);
	if (res != 0)
		g_error ("%s: pthread_attr_destroy failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);

	return TRUE;
#elif defined(HOST_WASI)
	return TRUE;
#else
	g_assert_not_reached ();
#endif
}

gboolean
mono_thread_platform_external_eventloop_keepalive_check (void)
{
#if defined(HOST_BROWSER) && !defined(DISABLE_THREADS)
	MONO_REQ_GC_SAFE_MODE;
	/* if someone called emscripten_runtime_keepalive_push (), the
	 * thread will stay alive in the JS event loop after returning
	 * from the thread's main function.
	 */
	return emscripten_runtime_keepalive_check ();
#else
	return FALSE;
#endif
}

void mono_threads_platform_init (void)
{
}

void
mono_threads_platform_exit (gsize exit_code)
{
#ifdef __EMSCRIPTEN_PTHREADS__
	pthread_exit ((gpointer) exit_code);
#else
	g_assert_not_reached ();
#endif
}

gboolean
mono_threads_platform_in_critical_region (THREAD_INFO_TYPE *info)
{
	return FALSE;
}

void
mono_memory_barrier_process_wide (void)
{
}

#ifdef HOST_BROWSER

G_EXTERN_C
extern void schedule_background_exec (void);

// when this is called from ThreadPool, the cb would be System.Threading.ThreadPool.BackgroundJobHandler
// when this is called from JSSynchronizationContext, the cb would be System.Runtime.InteropServices.JavaScript.JSSynchronizationContext.BackgroundJobHandler
// when this is called from sgen it would be wrapper of sgen_perform_collection_inner
// when this is called from gc, it would be mono_runtime_do_background_work
#ifdef DISABLE_THREADS
void
mono_main_thread_schedule_background_job (background_job_cb cb)
{
	g_assert (cb);
	THREADS_DEBUG ("mono_main_thread_schedule_background_job2: thread %p queued job %p to current thread\n", (gpointer)pthread_self(), (gpointer) cb);
	mono_current_thread_schedule_background_job (cb);
}
#endif /*DISABLE_THREADS*/

#ifndef DISABLE_THREADS
MonoNativeTlsKey jobs_key;
#else /* DISABLE_THREADS */
GSList *jobs;
#endif /* DISABLE_THREADS */

void
mono_current_thread_schedule_background_job (background_job_cb cb)
{
	g_assert (cb);
#ifdef DISABLE_THREADS

	if (!jobs)
		schedule_background_exec ();

	if (!g_slist_find (jobs, (gconstpointer)cb))
		jobs = g_slist_prepend (jobs, (gpointer)cb);

#else /*DISABLE_THREADS*/

	GSList *jobs = mono_native_tls_get_value (jobs_key);
	THREADS_DEBUG ("mono_current_thread_schedule_background_job1: thread %p queuing job %p into %p\n", (gpointer)pthread_self(), (gpointer) cb, (gpointer) jobs);
	if (!jobs)
	{
		THREADS_DEBUG ("mono_current_thread_schedule_background_job2: thread %p calling schedule_background_exec before job %p\n", (gpointer)pthread_self(), (gpointer) cb);
		schedule_background_exec ();
	}

	if (!g_slist_find (jobs, (gconstpointer)cb))
	{
		jobs = g_slist_prepend (jobs, (gpointer)cb);
		mono_native_tls_set_value (jobs_key, jobs);
		THREADS_DEBUG ("mono_current_thread_schedule_background_job3: thread %p queued job %p\n", (gpointer)pthread_self(), (gpointer) cb);
	}

#endif /*DISABLE_THREADS*/
}

#ifndef DISABLE_THREADS
void
mono_target_thread_schedule_background_job (MonoNativeThreadId target_thread, background_job_cb cb)
{
	THREADS_DEBUG ("worker %p queued job %p to worker %p \n", (gpointer)pthread_self(), (gpointer) cb, (gpointer) target_thread);
	// NOTE: here the cb is [UnmanagedCallersOnly] which wraps it with MONO_ENTER_GC_UNSAFE/MONO_EXIT_GC_UNSAFE
	mono_threads_wasm_async_run_in_target_thread_vi ((pthread_t) target_thread, (void*)mono_current_thread_schedule_background_job, (gpointer)cb);
}
#endif /*DISABLE_THREADS*/

G_EXTERN_C
EMSCRIPTEN_KEEPALIVE void
mono_background_exec (void);

G_EXTERN_C
EMSCRIPTEN_KEEPALIVE void
mono_background_exec (void)
{
	MONO_ENTER_GC_UNSAFE;
#ifdef DISABLE_THREADS
	GSList *j = jobs, *cur;
	jobs = NULL;
#else /* DISABLE_THREADS */
	THREADS_DEBUG ("mono_background_exec on thread %p started\n", (gpointer)pthread_self());
	GSList *jobs = mono_native_tls_get_value (jobs_key);
	GSList *j = jobs, *cur;
	mono_native_tls_set_value (jobs_key, NULL);
#endif /* DISABLE_THREADS */

	for (cur = j; cur; cur = cur->next) {
		background_job_cb cb = (background_job_cb)cur->data;
		g_assert (cb);
		THREADS_DEBUG ("mono_background_exec on thread %p running job %p \n", (gpointer)pthread_self(), (gpointer)cb);
		cb ();
		THREADS_DEBUG ("mono_background_exec on thread %p done job %p \n", (gpointer)pthread_self(), (gpointer)cb);
	}
	g_slist_free (j);
	MONO_EXIT_GC_UNSAFE;
}

gboolean
mono_threads_platform_is_main_thread (void)
{
#ifdef DISABLE_THREADS
	return TRUE;
#else
	return emscripten_is_main_runtime_thread ();
#endif
}

gboolean
mono_threads_wasm_is_ui_thread (void)
{
#ifdef DISABLE_THREADS
	return TRUE;
#else
	return emscripten_is_main_browser_thread ();
#endif
}

MonoNativeThreadId
mono_threads_wasm_ui_thread_tid (void)
{
#ifdef DISABLE_THREADS
	return (MonoNativeThreadId)1;
#else
	return (MonoNativeThreadId)emscripten_main_runtime_thread_id ();
#endif
}

#ifndef DISABLE_THREADS
extern void mono_wasm_pthread_on_pthread_attached (MonoNativeThreadId pthread_id, const char* thread_name, gboolean background_thread, gboolean threadpool_thread, gboolean external_eventloop, gboolean debugger_thread);
extern void mono_wasm_pthread_on_pthread_unregistered (MonoNativeThreadId pthread_id);
extern void mono_wasm_pthread_on_pthread_registered (MonoNativeThreadId pthread_id);
#endif

void
mono_threads_wasm_on_thread_attached (pthread_t tid, const char* thread_name, gboolean background_thread, gboolean threadpool_thread, gboolean external_eventloop, gboolean debugger_thread)
{
#ifdef DISABLE_THREADS
	return;
#else
	if (mono_threads_wasm_is_ui_thread ()) {
		// FIXME: we should not be attaching UI thread with deputy design
		// but right now we do, because mono_wasm_load_runtime is running in UI thread
		// g_assert(!mono_threads_wasm_is_ui_thread ());
		return;
	}

	// Notify JS that the pthread attached to Mono
	MONO_ENTER_GC_SAFE;
	mono_wasm_pthread_on_pthread_attached (tid, thread_name, background_thread, threadpool_thread, external_eventloop, debugger_thread);
	MONO_EXIT_GC_SAFE;
#endif
}

void
mono_threads_wasm_on_thread_unregistered (void)
{
#ifdef DISABLE_THREADS
	return;
#else
	if (mono_threads_wasm_is_ui_thread ()) {
		return;
	}
	// Notify JS that the pthread detached from Mono
	pthread_t id = pthread_self ();

	mono_wasm_pthread_on_pthread_unregistered (id);
#endif
}

void
mono_threads_wasm_on_thread_registered (void)
{
#ifdef DISABLE_THREADS
	return;
#else
	if (mono_threads_wasm_is_ui_thread ()) {
		return;
	}
	// Notify JS that the pthread registered to Mono
	pthread_t id = pthread_self ();

	mono_wasm_pthread_on_pthread_registered (id);
#endif
}

#ifndef DISABLE_THREADS

void
mono_threads_wasm_async_run_in_target_thread (pthread_t target_thread, void (*func) (void))
{
	emscripten_dispatch_to_thread_async (target_thread, EM_FUNC_SIG_V, func, NULL);
}

void
mono_threads_wasm_async_run_in_target_thread_vi (pthread_t target_thread, void (*func) (gpointer), gpointer user_data)
{
	emscripten_dispatch_to_thread_async (target_thread, EM_FUNC_SIG_VI, func, NULL, user_data);
}

void
mono_threads_wasm_async_run_in_target_thread_vii (pthread_t target_thread, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer user_data2)
{
	emscripten_dispatch_to_thread_async (target_thread, EM_FUNC_SIG_VII, func, NULL, user_data1, user_data2);
}

static void mono_threads_wasm_sync_run_in_target_thread_vii_cb (MonoCoopSem *done, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer user_data2)
{
	func (user_data1, user_data2);
	mono_coop_sem_post (done);
}

void
mono_threads_wasm_sync_run_in_target_thread_vii (pthread_t target_thread, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer user_data2)
{
	MonoCoopSem sem;
	mono_coop_sem_init (&sem, 0);
	emscripten_dispatch_to_thread_async (target_thread, EM_FUNC_SIG_VIIII, mono_threads_wasm_sync_run_in_target_thread_vii_cb, NULL, &sem, func, user_data1, user_data2);

	MONO_ENTER_GC_UNSAFE;
	mono_coop_sem_wait (&sem, MONO_SEM_FLAGS_NONE);
	MONO_EXIT_GC_UNSAFE;

	mono_coop_sem_destroy (&sem);
}

#endif /* DISABLE_THREADS */

#endif /* HOST_BROWSER */

#else

MONO_EMPTY_SOURCE_FILE (mono_threads_wasm);

#endif
