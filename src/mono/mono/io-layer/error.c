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
	pthread_key_create(&error_key, NULL);
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
	/* Set the thread-local error code */
	mono_once(&error_key_once, error_init);
	pthread_setspecific(error_key, GUINT_TO_POINTER(code));
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
	case EPROTONOSUPPORT: result = WSAENETDOWN; break;
#if ERESTARTSYS
	case ERESTARTSYS: result = WSAENETDOWN; break;
#endif
	/*FIXME: case EROFS: result = WSAE????; break; */
	case ESOCKTNOSUPPORT: result = WSAENETDOWN; break;
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

