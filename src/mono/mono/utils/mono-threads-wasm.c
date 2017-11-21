#include "config.h"

#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-mmap.h>


#if defined (USE_WASM_BACKEND)

#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-mmap.h>

#include <emscripten.h>
#include <glib.h>

#define round_down(addr, val) ((void*)((addr) & ~((val) - 1)))

EMSCRIPTEN_KEEPALIVE
static int
wasm_get_stack_base (void)
{
	return EM_ASM_INT ({
		return STACK_BASE;
	});
}

EMSCRIPTEN_KEEPALIVE
static int
wasm_get_stack_size (void)
{
	return EM_ASM_INT ({
		return TOTAL_STACK;
	});
}


int
mono_threads_get_max_stack_size (void)
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

//----

gboolean
mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2)
{
	return id1 == id2;
}


MonoNativeThreadId
mono_native_thread_id_get (void)
{
	return (MonoNativeThreadId)1;
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
	g_error ("WASM doesn't support threading");
	return FALSE;
}

//----

gboolean
mono_threads_platform_yield (void)
{
	return TRUE;
}

void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	*staddr = (void*)wasm_get_stack_base ();
	*stsize = wasm_get_stack_size ();
}


gboolean
mono_thread_platform_create_thread (MonoThreadStart thread_fn, gpointer thread_data, gsize* const stack_size, MonoNativeThreadId *tid)
{
	g_warning ("WASM doesn't support threading");
	return FALSE;
}

void mono_threads_platform_init (void)
{
}

void
mono_threads_platform_exit (gsize exit_code)
{
	g_error ("WASM doesn't support threading");
}

gboolean
mono_threads_platform_in_critical_region (MonoNativeThreadId tid)
{
	return FALSE;
}


extern void schedule_background_exec (void);

static GSList *jobs;

void
mono_threads_schedule_background_job (background_job_cb cb)
{
	if (!jobs)
		schedule_background_exec ();

	if (!g_slist_find (jobs, cb))
		jobs = g_slist_prepend (jobs, cb);
}

EMSCRIPTEN_KEEPALIVE void
mono_background_exec (void)
{
	GSList *j = jobs, *cur;
	jobs = NULL;

	for (cur = j; cur; cur = cur->next) {
		background_job_cb cb = (background_job_cb)cur->data;
		cb ();
	}
	g_slist_free (j);
}

#endif
