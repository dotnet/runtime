#include "config.h"

#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-mmap.h>

#if defined (USE_WASM_BACKEND)

#define round_down(addr, val) ((void*)((addr) & ~((val) - 1)))

void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	*staddr = round_down ((size_t)&staddr, 65536); //WASM pagesize is 64k
	*stsize = 65536 * 4; //we say it's 4 pages, there isn't much that uses this beyond the GC
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

#endif
