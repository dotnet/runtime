#include <config.h>

#if defined(__HAIKU__)

#include <mono/utils/mono-threads.h>
#include <pthread.h>
#include <os/kernel/OS.h>

void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	thread_info ti;
	get_thread_info(get_pthread_thread_id(pthread_self()), &ti);

	*staddr = ti.stack_base;
	*stsize = ti.stack_end - ti.stack_base;
}

guint64
mono_native_thread_os_id_get (void)
{
	return (guint64)get_pthread_thread_id (pthread_self ());
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_threads_haiku);

#endif
