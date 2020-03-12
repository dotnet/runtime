/**
 * \file
 */

#include <config.h>

#if defined(HOST_ANDROID)

#include <pthread.h>
#include <stdio.h>
#include <inttypes.h>
#include "glib.h"
#include <mono/utils/mono-threads.h>
#include <sys/syscall.h>

static void
slow_get_thread_bounds (guint8 *current, guint8 **staddr, size_t *stsize)
{
	char buff [1024];
	FILE *f = fopen ("/proc/self/maps", "r");
	if (!f)
		g_error ("Could not determine thread bounds, failed to open /proc/self/maps");

	while (fgets (buff, sizeof (buff), f)) {
		intmax_t low, high;
		char *ptr = buff;
		char *end = NULL;
		//each line starts with the range we want: f7648000-f7709000
		low = strtoimax (ptr, &end, 16);
		if (end) {
			ptr = end + 1; //skip the dash to make sure we don't get a negative number
			end = NULL;
			high = strtoimax (ptr, &end, 16);
		}
		if (end && low <= (intmax_t)(size_t)current && high > (intmax_t)(size_t)current) {
			*staddr = (guint8 *)(size_t)low;
			*stsize = (size_t)(high - low);
			fclose (f);
			return;
		}
	}
	g_error ("Could not determine thread bounds, failed to find current stack pointer in /proc/self/maps");
}

void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	pthread_attr_t attr;
	guint8 *current = (guint8*)&attr;

	*staddr = NULL;
	*stsize = (size_t)-1;

	pthread_getattr_np (pthread_self (), &attr);
	pthread_attr_getstack (&attr, (void**)staddr, stsize);
	pthread_attr_destroy (&attr);

	if (*staddr && ((current <= *staddr) || (current > *staddr + *stsize)))
		slow_get_thread_bounds (current, staddr, stsize);
}

guint64
mono_native_thread_os_id_get (void)
{
	return (guint64)syscall (SYS_gettid);
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_threads_android);

#endif
