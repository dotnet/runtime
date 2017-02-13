/**
 * \file
 */

#include <config.h>

#if defined(__OpenBSD__)

#include <pthread.h>
#include <pthread_np.h>

void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	/* TODO :   Determine if this code is actually still needed. It may already be covered by the case above. */
	pthread_attr_t attr;
	guint8 *current = (guint8*)&attr;

	*staddr = NULL;
	*stsize = (size_t)-1;

	pthread_attr_init (&attr);

	stack_t ss;
	int rslt;

	rslt = pthread_stackseg_np (pthread_self (), &ss);
	g_assert (rslt == 0);

	*staddr = (guint8*)((size_t)ss.ss_sp - ss.ss_size);
	*stsize = ss.ss_size;

	pthread_attr_destroy (&attr);
}

#endif
