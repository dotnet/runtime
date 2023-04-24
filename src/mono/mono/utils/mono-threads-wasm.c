#include "config.h"

#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-compiler.h>

#if defined (USE_WASM_BACKEND)

#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-threads-api.h>
#include <mono/utils/mono-threads-debug.h>

#include <glib.h>

#ifdef HOST_BROWSER

#include <mono/utils/mono-threads-wasm.h>

#include <emscripten.h>
#include <emscripten/stack.h>
#ifndef DISABLE_THREADS
#include <emscripten/threading.h>
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

#else /* WASI */

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

#endif

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
	g_error ("WASM doesn't support threading");
}

static const char *thread_name;

void
mono_native_thread_set_name (MonoNativeThreadId tid, const char *name)
{
	thread_name = g_strdup (name);
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

/* jobs is not protected by a mutex, only access from a single thread! */
static GSList *jobs;

void
mono_threads_schedule_background_job (background_job_cb cb)
{
#ifndef DISABLE_THREADS
	if (!mono_threads_wasm_is_browser_thread ()) {
		THREADS_DEBUG ("worker %p queued job %p\n", (gpointer)pthread_self(), (gpointer) cb);
		mono_threads_wasm_async_run_in_main_thread_vi ((void (*)(gpointer))mono_threads_schedule_background_job, cb);
		return;
	}
#endif

	THREADS_DEBUG ("main thread queued job %p\n", (gpointer) cb);

	if (!jobs)
		schedule_background_exec ();

	if (!g_slist_find (jobs, (gconstpointer)cb))
		jobs = g_slist_prepend (jobs, (gpointer)cb);
}

G_EXTERN_C
EMSCRIPTEN_KEEPALIVE void
mono_background_exec (void);

G_EXTERN_C
EMSCRIPTEN_KEEPALIVE void
mono_background_exec (void)
{
	MONO_ENTER_GC_UNSAFE;
#ifndef DISABLE_THREADS
	g_assert (mono_threads_wasm_is_browser_thread ());
#endif
	GSList *j = jobs, *cur;
	jobs = NULL;

	for (cur = j; cur; cur = cur->next) {
		background_job_cb cb = (background_job_cb)cur->data;
		cb ();
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
mono_threads_wasm_is_browser_thread (void)
{
#ifdef DISABLE_THREADS
	return TRUE;
#else
	return emscripten_is_main_browser_thread ();
#endif
}

MonoNativeThreadId
mono_threads_wasm_browser_thread_tid (void)
{
#ifdef DISABLE_THREADS
	return (MonoNativeThreadId)1;
#else
	return (MonoNativeThreadId)emscripten_main_runtime_thread_id ();
#endif
}

#ifndef DISABLE_THREADS
extern void
mono_wasm_pthread_on_pthread_attached (gpointer pthread_id);
#endif

void
mono_threads_wasm_on_thread_attached (void)
{
#ifdef DISABLE_THREADS
	return;
#else
	if (mono_threads_wasm_is_browser_thread ()) {
		return;
	}
	// Notify JS that the pthread attachd to Mono
	pthread_t id = pthread_self ();
	MONO_ENTER_GC_SAFE;
	mono_wasm_pthread_on_pthread_attached (id);
	MONO_EXIT_GC_SAFE;
#endif
}


#ifndef DISABLE_THREADS
void
mono_threads_wasm_async_run_in_main_thread (void (*func) (void))
{
	emscripten_async_run_in_main_runtime_thread (EM_FUNC_SIG_V, func);
}

void
mono_threads_wasm_async_run_in_main_thread_vi (void (*func) (gpointer), gpointer user_data)
{
	emscripten_async_run_in_main_runtime_thread (EM_FUNC_SIG_VI, func, user_data);
}

void
mono_threads_wasm_async_run_in_main_thread_vii (void (*func) (gpointer, gpointer), gpointer user_data1, gpointer user_data2)
{
	emscripten_async_run_in_main_runtime_thread (EM_FUNC_SIG_VII, func, user_data1, user_data2);
}


#endif /* DISABLE_THREADS */

#endif /* HOST_BROWSER */

#else

MONO_EMPTY_SOURCE_FILE (mono_threads_wasm);

#endif
