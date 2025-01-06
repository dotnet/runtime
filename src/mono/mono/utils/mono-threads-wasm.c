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
#ifndef DISABLE_THREADS
#include <emscripten/threading.h>
#include <mono/metadata/threads-types.h>
#endif

#endif

uintptr_t get_wasm_stack_high(void);
uintptr_t get_wasm_stack_low(void);

static int
wasm_get_stack_size (void)
{
#if defined(HOST_WASI) && !defined(DISABLE_THREADS)
	// TODO: this will need changes for WASI multithreading as the stack will be allocated per thread at different addresses
	g_assert_not_reached ();
#else
	/*
	 * | -- increasing address ---> |
	 * | data |(stack low) stack (stack high)| heap |
	 */
	size_t stack_high = get_wasm_stack_high();
	size_t stack_low = get_wasm_stack_low();
	size_t max_stack_size = stack_high - stack_low;

	g_assert (stack_low >= 0);
	g_assert (stack_high > stack_low);
	g_assert (max_stack_size >= 64 * 1024);

	// this is the max available stack size size
	return max_stack_size;
#endif
}

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

	g_assert (*staddr != NULL);
	g_assert (*stsize != (size_t)-1);
#elif defined(HOST_WASI) && !defined(DISABLE_THREADS)
	// TODO: this will need changes for WASI multithreading as the stack will be allocated per thread at different addresses
	g_assert_not_reached ();
#else
	*staddr = (guint8*)get_wasm_stack_low ();
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
// when this is called from sgen it would be wrapper of sgen_perform_collection_inner
// when this is called from gc, it would be mono_runtime_do_background_work
#ifdef DISABLE_THREADS
void
mono_main_thread_schedule_background_job (background_job_cb cb)
{
	g_assert (cb);
	THREADS_DEBUG ("mono_main_thread_schedule_background_job: thread %p queued job %p to current thread\n", (gpointer)pthread_self(), (gpointer) cb);

	if (!jobs)
		schedule_background_exec ();

	if (!g_slist_find (jobs, (gconstpointer)cb))
		jobs = g_slist_prepend (jobs, (gpointer)cb);
}

GSList *jobs;

G_EXTERN_C
EMSCRIPTEN_KEEPALIVE void
mono_background_exec (void)
{
	MONO_ENTER_GC_UNSAFE;
	GSList *j = jobs, *cur;
	jobs = NULL;

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

#else /*DISABLE_THREADS*/

extern void mono_wasm_schedule_synchronization_context ();

void mono_target_thread_schedule_synchronization_context(MonoNativeThreadId target_thread)
{
	emscripten_dispatch_to_thread_async ((pthread_t) target_thread, EM_FUNC_SIG_V, mono_wasm_schedule_synchronization_context, NULL);
}

#endif /*DISABLE_THREADS*/

gboolean
mono_threads_platform_is_main_thread (void)
{
#ifdef DISABLE_THREADS
	return TRUE;
#else
	return mono_threads_wasm_is_deputy_thread ();
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
		// but right now we do
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
static pthread_t deputy_thread_tid;
static pthread_t io_thread_tid;
extern void mono_wasm_start_deputy_thread_async (void);
extern void mono_wasm_start_io_thread_async (void);
extern void mono_wasm_trace_logger (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data);
extern void mono_wasm_dump_threads (void);

void mono_wasm_dump_threads_async (void)
{
	mono_threads_wasm_async_run_in_target_thread (mono_threads_wasm_ui_thread_tid (), mono_wasm_dump_threads);
}

gboolean
mono_threads_wasm_is_deputy_thread (void)
{
	return pthread_self () == deputy_thread_tid;
}

MonoNativeThreadId
mono_threads_wasm_deputy_thread_tid (void)
{
	return (MonoNativeThreadId) deputy_thread_tid;
}

// this is running in deputy thread
static gsize
deputy_thread_fn (void* unused_arg G_GNUC_UNUSED)
{
	deputy_thread_tid = pthread_self ();

	// this will throw JS "unwind"
	mono_wasm_start_deputy_thread_async();
	
	return 0;// never reached
}

EMSCRIPTEN_KEEPALIVE MonoNativeThreadId
mono_wasm_create_deputy_thread (void)
{
	pthread_create (&deputy_thread_tid, NULL, (void *(*)(void *)) deputy_thread_fn, NULL);
	return deputy_thread_tid;
}

gboolean
mono_threads_wasm_is_io_thread (void)
{
	return pthread_self () == io_thread_tid;
}

MonoNativeThreadId
mono_threads_wasm_io_thread_tid (void)
{
	return (MonoNativeThreadId) io_thread_tid;
}

// this is running in io thread
static gsize
io_thread_fn (void* unused_arg G_GNUC_UNUSED)
{
	io_thread_tid = pthread_self ();

	// this will throw JS "unwind"
	mono_wasm_start_io_thread_async();
	
	return 0;// never reached
}

EMSCRIPTEN_KEEPALIVE MonoNativeThreadId
mono_wasm_create_io_thread (void)
{
	pthread_create (&io_thread_tid, NULL, (void *(*)(void *)) io_thread_fn, NULL);
	return io_thread_tid;
}

// TODO ideally we should not need to have UI thread registered as managed
EMSCRIPTEN_KEEPALIVE void
mono_wasm_register_ui_thread (void)
{
	MonoThread *thread = mono_thread_internal_attach (mono_get_root_domain ());
	mono_thread_set_state (thread, ThreadState_Background);
	mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NONE);

	MonoThreadInfo *info = mono_thread_info_current_unchecked ();
	g_assert (info);
	info->runtime_thread = TRUE;
	MONO_ENTER_GC_SAFE_UNBALANCED;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_register_io_thread (void)
{
	MonoThread *thread = mono_thread_internal_attach (mono_get_root_domain ());
	mono_thread_set_state (thread, ThreadState_Background);
	mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NONE);

	MonoThreadInfo *info = mono_thread_info_current_unchecked ();
	g_assert (info);
	info->runtime_thread = TRUE;
	MONO_ENTER_GC_SAFE_UNBALANCED;
}

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

static void mono_threads_wasm_sync_run_in_target_thread_vii_cb (MonoCoopSem *done, void (*func) (gpointer, gpointer), gpointer user_data1, void* args)
{
	// in UI thread we postpone the execution via safeSetTimeout so that emscripten_proxy_execute_queue is not blocked by this call
	// see invoke_later_on_ui_thread
	if (mono_threads_wasm_is_ui_thread()) {
		MonoCoopSem **semPtrPtr = (MonoCoopSem **)(((char *) args) + 28/*JSMarshalerArgumentOffsets.SyncDoneSemaphorePtr*/);
		*semPtrPtr = done;
		func (user_data1, args);
	}
	else {
		func (user_data1, args);
		mono_coop_sem_post (done);
	}
}

EMSCRIPTEN_KEEPALIVE void 
mono_threads_wasm_sync_run_in_target_thread_done (MonoCoopSem *sem)
{
	mono_coop_sem_post (sem);
}

void
mono_threads_wasm_sync_run_in_target_thread_vii (pthread_t target_thread, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer args)
{
	MonoCoopSem sem;
	mono_coop_sem_init (&sem, 0);
	emscripten_dispatch_to_thread_async (target_thread, EM_FUNC_SIG_VIIII, mono_threads_wasm_sync_run_in_target_thread_vii_cb, NULL, &sem, func, user_data1, args);

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
