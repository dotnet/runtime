/*
 * error.c:  Error reporting
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <string.h>
#include <errno.h>

#include "mono/io-layer/wapi.h"

static pthread_key_t error_key;
static mono_once_t error_key_once=MONO_ONCE_INIT;

static void error_init(void)
{
	int ret;
	
	ret = pthread_key_create(&error_key, NULL);
	g_assert (ret == 0);
}

/**
 * GetLastError:
 *
 * Retrieves the last error that occurred in the calling thread.
 *
 * Return value: The error code for the last error that happened on
 * the calling thread.
 */
guint32 GetLastError(void)
{
	guint32 err;
	void *errptr;
	
	mono_once(&error_key_once, error_init);
	errptr=pthread_getspecific(error_key);
	err=GPOINTER_TO_UINT(errptr);
	
	return(err);
}

/**
 * SetLastError:
 * @code: The error code.
 *
 * Sets the error code in the calling thread.
 */
void SetLastError(guint32 code)
{
	int ret;
	
	/* Set the thread-local error code */
	mono_once(&error_key_once, error_init);
	ret = pthread_setspecific(error_key, GUINT_TO_POINTER(code));
	g_assert (ret == 0);
}

guint32
errno_to_WSA (guint32 code, const gchar *function_name)
{
	gint result = -1;
	char *sys_error;
	gchar *msg;

	switch (code) {
	case EACCES: result = WSAEACCES; break;
	case EADDRINUSE: result = WSAEADDRINUSE; break;
	case EAFNOSUPPORT: result = WSAEAFNOSUPPORT; break;
#if EAGAIN != EWOULDBLOCK
	case EAGAIN: result = WSAEWOULDBLOCK; break;
#endif
	case EALREADY: result = WSAEALREADY; break;
	case EBADF: result = WSAENOTSOCK; break;
	case ECONNABORTED: result = WSAENETDOWN; break;
	case ECONNREFUSED: result = WSAECONNREFUSED; break;
	case ECONNRESET: result = WSAECONNRESET; break;
	case EFAULT: result = WSAEFAULT; break;
	case EHOSTUNREACH: result = WSAEHOSTUNREACH; break;
	case EINPROGRESS: result = WSAEINPROGRESS; break;
	case EINTR: result = WSAEINTR; break;
	case EINVAL: result = WSAEINVAL; break;
	/*FIXME: case EIO: result = WSAE????; break; */
	case EISCONN: result = WSAEISCONN; break;
	/* FIXME: case ELOOP: result = WSA????; break; */
	case EMFILE: result = WSAEMFILE; break;
	case EMSGSIZE: result = WSAEMSGSIZE; break;
	/* FIXME: case ENAMETOOLONG: result = WSAEACCES; break; */
	case ENETUNREACH: result = WSAENETUNREACH; break;
	case ENOBUFS: result = WSAENOBUFS; break; /* not documented */
	/* case ENOENT: result = WSAE????; break; */
	case ENOMEM: result = WSAENOBUFS; break;
	case ENOPROTOOPT: result = WSAENOPROTOOPT; break;
#ifdef ENOSR
	case ENOSR: result = WSAENETDOWN; break;
#endif
	case ENOTCONN: result = WSAENOTCONN; break;
	/*FIXME: case ENOTDIR: result = WSAE????; break; */
	case ENOTSOCK: result = WSAENOTSOCK; break;
	case ENOTTY: result = WSAENOTSOCK; break;
	case EOPNOTSUPP: result = WSAEOPNOTSUPP; break;
	case EPERM: result = WSAEACCES; break;
	case EPIPE: result = WSAESHUTDOWN; break;
	case EPROTONOSUPPORT: result = WSAEPROTONOSUPPORT; break;
#if ERESTARTSYS
	case ERESTARTSYS: result = WSAENETDOWN; break;
#endif
	/*FIXME: case EROFS: result = WSAE????; break; */
	case ESOCKTNOSUPPORT: result = WSAESOCKTNOSUPPORT; break;
	case ETIMEDOUT: result = WSAENETDOWN; break;
	case EWOULDBLOCK: result = WSAEWOULDBLOCK; break;
	default:
		sys_error = strerror (code);
		msg = g_locale_to_utf8 (sys_error, strlen (sys_error), NULL, NULL, NULL);
		if (function_name == NULL)
			function_name = G_GNUC_PRETTY_FUNCTION;

		g_warning ("%s: Need to translate %d [%s] into winsock error",
			   function_name, code, msg);

		g_free (msg);
		result = WSASYSCALLFAILURE;
	}

	return result;
}

gint
_wapi_get_win32_file_error (gint err)
{
	gint ret;
	/* mapping ideas borrowed from wine. they may need some work */

	switch (err) {
	case EACCES: case EPERM: case EROFS:
		ret = ERROR_ACCESS_DENIED;
		break;
	
	case EAGAIN:
		ret = ERROR_SHARING_VIOLATION;
		break;
	
	case EBUSY:
		ret = ERROR_LOCK_VIOLATION;
		break;
	
	case EEXIST:
		ret = ERROR_FILE_EXISTS;
		break;
	
	case EINVAL: case ESPIPE:
		ret = ERROR_SEEK;
		break;
	
	case EISDIR:
		ret = ERROR_CANNOT_MAKE;
		break;
	
	case ENFILE: case EMFILE:
		ret = ERROR_NO_MORE_FILES;
		break;

	case ENOENT: case ENOTDIR:
		ret = ERROR_FILE_NOT_FOUND;
		break;
	
	case ENOSPC:
		ret = ERROR_HANDLE_DISK_FULL;
		break;
	
	case ENOTEMPTY:
		ret = ERROR_DIR_NOT_EMPTY;
		break;

	case ENOEXEC:
		ret = ERROR_BAD_FORMAT;
		break;

	case ENAMETOOLONG:
		ret = ERROR_FILENAME_EXCED_RANGE;
		break;
	
	case EINPROGRESS:
		ret = ERROR_IO_PENDING;
		break;
	
	case ENOSYS:
		ret = ERROR_NOT_SUPPORTED;
		break;
	
	case EBADF:
		ret = ERROR_INVALID_HANDLE;
		break;
		
	case EIO:
		ret = ERROR_INVALID_HANDLE;
		break;
		
	default:
		g_message ("Unknown errno: %s\n", strerror (err));
		ret = ERROR_GEN_FAILURE;
		break;
	}

	return ret;
}

