#include <config.h>

#if defined(PLATFORM_LINUX)

#include <pthread.h>

void
mono_threads_core_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	pthread_attr_t attr;
	guint8 *current = (guint8*)&attr;

	*staddr = NULL;
	*stsize = (size_t)-1;

	pthread_getattr_np (pthread_self (), &attr);
	pthread_attr_getstack (&attr, (void**)staddr, stsize);
	pthread_attr_destroy (&attr);
}

#endif
