/**
 * \file
 */

#include <config.h>

#if defined(_AIX)

#include <mono/utils/mono-threads.h>
#include <pthread.h>

void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	/* see GC_push_all_stacks in libgc/aix_irix_threads.c 
           for why we do this; pthread_getattr_np exists only
           on some versions of AIX and not on PASE, so use a
           legacy way to get the stack information */
	struct __pthrdsinfo pi;
	pthread_t pt;
	int res, rbv, ps;
	char rb[255];

	pt = pthread_self();
	ps = sizeof(pi);
	rbv = sizeof(rb);

	*staddr = NULL;
	*stsize = (size_t)-1;

	res = pthread_getthrds_np(&pt, PTHRDSINFO_QUERY_ALL, &pi, ps, rb, &rbv);
	/* FIXME: are these the right values? */
	*staddr = (guint8*)pi.__pi_stackaddr;
	/*
	 * ruby doesn't use stacksize; see:
	 * github.com/ruby/ruby/commit/a2594be783c727c6034308f5294333752c3845bb
	 */
	*stsize = pi.__pi_stackend - pi.__pi_stackaddr;
}

gboolean
mono_threads_platform_is_main_thread (void)
{
	/* returns 1 on main thread, even if the kernel tid is diff */
	return pthread_self () == 1;
}

guint64
mono_native_thread_os_id_get (void)
{
	pthread_t t = pthread_self ();
	struct __pthrdsinfo ti;
	int err, size = 0;
	err = pthread_getthrds_np (&t, PTHRDSINFO_QUERY_TID, &ti, sizeof (struct __pthrdsinfo), NULL, &size);
	return (guint64)ti.__pi_tid;
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_threads_aix);

#endif
