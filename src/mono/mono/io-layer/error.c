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
#include "mono/io-layer/wapi-private.h"
#include "mono/utils/mono-lazy-init.h"

static pthread_key_t error_key;
static mono_lazy_init_t error_key_once = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;

static void error_init(void)
{
	int ret;
	
	ret = pthread_key_create(&error_key, NULL);
	g_assert (ret == 0);
}

static void error_cleanup (void)
{
	int ret;

	ret = pthread_key_delete (error_key);
	g_assert (ret == 0);
}

void _wapi_error_cleanup (void)
{
	mono_lazy_cleanup (&error_key_once, error_cleanup);
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

	if (_wapi_has_shut_down)
		return 0;
	mono_lazy_initialize(&error_key_once, error_init);
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
	
	if (_wapi_has_shut_down)
		return;
	/* Set the thread-local error code */
	mono_lazy_initialize(&error_key_once, error_init);
	ret = pthread_setspecific(error_key, GUINT_TO_POINTER(code));
	g_assert (ret == 0);
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
		ret = ERROR_TOO_MANY_OPEN_FILES;
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
	
#ifdef EINPROGRESS
	case EINPROGRESS:
		ret = ERROR_IO_PENDING;
		break;
#endif
	
	case ENOSYS:
		ret = ERROR_NOT_SUPPORTED;
		break;
	
	case EBADF:
		ret = ERROR_INVALID_HANDLE;
		break;
		
	case EIO:
		ret = ERROR_INVALID_HANDLE;
		break;
		
	case EINTR:
		ret = ERROR_IO_PENDING;		/* best match I could find */
		break;
		
	case EPIPE:
		ret = ERROR_WRITE_FAULT;
		break;
		
	default:
		g_message ("Unknown errno: %s\n", g_strerror (err));
		ret = ERROR_GEN_FAILURE;
		break;
	}

	return ret;
}

