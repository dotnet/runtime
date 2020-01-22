/**
 * \file
 */

#include "w32error.h"

#include "utils/mono-lazy-init.h"

static mono_lazy_init_t error_key_once = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;

static pthread_key_t error_key;

static void
error_key_init (void)
{
	gint ret;
	ret = pthread_key_create (&error_key, NULL);
	g_assert (ret == 0);
}

guint32
mono_w32error_get_last (void)
{
	mono_lazy_initialize (&error_key_once, error_key_init);
	return GPOINTER_TO_UINT (pthread_getspecific (error_key));
}

void
mono_w32error_set_last (guint32 error)
{
	gint ret;
	mono_lazy_initialize (&error_key_once, error_key_init);
	ret = pthread_setspecific (error_key, GUINT_TO_POINTER (error));
	g_assert (ret == 0);
}

guint32
mono_w32error_unix_to_win32 (guint32 error)
{
	/* mapping ideas borrowed from wine. they may need some work */

	switch (error) {
	case EACCES:
	case EPERM:
	case EROFS: return ERROR_ACCESS_DENIED;
	case EAGAIN: return ERROR_SHARING_VIOLATION;
	case EBUSY: return ERROR_LOCK_VIOLATION;
	case EEXIST: return ERROR_FILE_EXISTS;
	case EINVAL:
	case ESPIPE: return ERROR_SEEK;
	case EISDIR: return ERROR_CANNOT_MAKE;
	case ENFILE:
	case EMFILE: return ERROR_TOO_MANY_OPEN_FILES;
	case ENOENT:
	case ENOTDIR: return ERROR_FILE_NOT_FOUND;
	case ENOSPC: return ERROR_HANDLE_DISK_FULL;
#if !defined(_AIX) || (defined(_AIX) && defined(_LINUX_SOURCE_COMPAT))
	case ENOTEMPTY: return ERROR_DIR_NOT_EMPTY;
#endif
	case ENOEXEC: return ERROR_BAD_FORMAT;
	case ENAMETOOLONG: return ERROR_FILENAME_EXCED_RANGE;
#ifdef EINPROGRESS
	case EINPROGRESS: return ERROR_IO_PENDING;
#endif
	case ENOSYS: return ERROR_NOT_SUPPORTED;
	case EBADF: return ERROR_INVALID_HANDLE;
	case EIO: return ERROR_INVALID_HANDLE;
#ifdef ERESTART
	case ERESTART:
#endif
	case EINTR: return ERROR_IO_PENDING; /* best match I could find */
	case EPIPE: return ERROR_WRITE_FAULT;
	case ELOOP: return ERROR_CANT_RESOLVE_FILENAME;
#ifdef ENODEV
	case ENODEV: return ERROR_DEV_NOT_EXIST;
#endif
#ifdef ENXIO
	case ENXIO: return ERROR_DEV_NOT_EXIST;
#endif
#ifdef ENOTCONN
	case ENOTCONN: return ERROR_DEV_NOT_EXIST;
#endif
#ifdef EHOSTDOWN
	case EHOSTDOWN: return ERROR_DEV_NOT_EXIST;
#endif
#ifdef ENEEDAUTH
	case ENEEDAUTH: return ERROR_ACCESS_DENIED;
#endif

	default:
		g_warning ("%s: unknown error (%d) \"%s\"", __FILE__, error, g_strerror (error));
		return ERROR_NOT_SUPPORTED;
	}
}
