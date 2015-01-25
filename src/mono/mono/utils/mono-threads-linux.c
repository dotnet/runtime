#include <config.h>

#if defined(__linux__) && !defined(PLATFORM_ANDROID)

#include <mono/utils/mono-threads.h>
#include <pthread.h>
#include <sys/syscall.h>
#include <sys/resource.h>
#include <sys/types.h>
#include <unistd.h>

void
mono_threads_core_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	pthread_attr_t attr;

	*staddr = NULL;
	*stsize = (size_t)-1;

	pthread_getattr_np (pthread_self (), &attr);
	pthread_attr_getstack (&attr, (void**)staddr, stsize);
	pthread_attr_destroy (&attr);

	/*
	 * The pthread implementation of musl (and possibly others) does not
	 * return a useful stack size value for the main thread. It gives us
	 * a value like 139264 instead of 8388608 because it is reporting
	 * the currently-committed stack's size rather than the size it would
	 * have if the kernel were to commit the entire 8 MB stack.
	 *
	 * This is technically 'correct' in that, in extreme OOM situations,
	 * the kernel is not necessarily able to provide the full 8 MB stack.
	 * Realistically, in such a situation, nothing in Mono would work
	 * anyway...
	 *
	 * We work around this by getting the main thread's stack limit from
	 * getrlimit(2).
	 */
	if (syscall (SYS_gettid) == getpid ()) {
		struct rlimit rlim;

		if (getrlimit (RLIMIT_STACK, &rlim) != -1 && rlim.rlim_cur != RLIM_INFINITY)
			*stsize = (size_t) rlim.rlim_cur;
	}
}

#endif
