#include <config.h>
#include <glib.h>
#include <pthread.h>

#include "mono/io-layer/wapi.h"


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
	return(0);
}

/**
 * SetLastError:
 * @code: The error code.
 *
 * Sets the error code in the calling thread.
 */
void SetLastError(guint32 code G_GNUC_UNUSED)
{
	/* Set the thread-local error code */
}
