/**
 * \file
 */

#include <config.h>

#if defined(__FreeBSD__)

#include <mono/utils/mono-threads.h>
#include <pthread.h>
#include <pthread_np.h>
#include <sys/thr.h>

void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	pthread_attr_t attr;
	guint8 *current = (guint8*)&attr;

	*staddr = NULL;
	*stsize = (size_t)-1;

	pthread_attr_init (&attr);
	pthread_attr_get_np (pthread_self (), &attr);

	pthread_attr_getstack (&attr, (void**)staddr, stsize);
	pthread_attr_destroy (&attr);
}

guint64
mono_native_thread_os_id_get (void)
{
	long tid;
	thr_self (&tid);
	return (guint64)tid;
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_threads_freebsd);

#endif
