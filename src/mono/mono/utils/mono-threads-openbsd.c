/**
 * \file
 */

#include <config.h>

#if defined(__OpenBSD__)

#include <mono/utils/mono-threads.h>
#include <pthread.h>
#include <pthread_np.h>

void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	stack_t ss;
	int rslt;

	rslt = pthread_stackseg_np (pthread_self (), &ss);
	g_assert (rslt == 0);

	*staddr = (void*)((size_t)ss.ss_sp - ss.ss_size);
	*stsize = ss.ss_size;
}

guint64
mono_native_thread_os_id_get (void)
{
	return (guint64)getthrid ();
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_threads_openbsd);

#endif
