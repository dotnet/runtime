#include <config.h>

#if defined(__linux__) && !defined(PLATFORM_ANDROID)

#include <mono/utils/mono-threads.h>
#include <pthread.h>

void
mono_threads_core_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	pthread_attr_t attr;

	*staddr = NULL;
	*stsize = (size_t)-1;

	pthread_getattr_np (pthread_self (), &attr);
	pthread_attr_getstack (&attr, (void**)staddr, stsize);
	pthread_attr_destroy (&attr);
}

#endif
