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

#include "mono/io-layer/wapi.h"

static pthread_key_t error_key;
static pthread_once_t error_key_once=PTHREAD_ONCE_INIT;

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
	
	pthread_once(&error_key_once, error_init);
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
	pthread_once(&error_key_once, error_init);
	pthread_setspecific(error_key, GUINT_TO_POINTER(code));
}
