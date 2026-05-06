/**
 * \file
 */

#include <config.h>

#if (defined(__linux__) && !defined(HOST_ANDROID)) || defined(__FreeBSD_kernel__)

#include <mono/utils/mono-threads.h>
#include <pthread.h>
#include <sys/syscall.h>

void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	pthread_attr_t attr;
	gint res;

	*staddr = NULL;
	*stsize = (size_t)-1;

	res = pthread_attr_init (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_attr_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_getattr_np (pthread_self (), &attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_getattr_np failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_attr_getstack (&attr, (void**)staddr, stsize);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_attr_getstack failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_attr_destroy (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_attr_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);

}

guint64
mono_native_thread_os_id_get (void)
{
	return (guint64)syscall (SYS_gettid);
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_threads_linux);

#endif
