#ifndef _WAPI_SEMAPHORES_H_
#define _WAPI_SEMAPHORES_H_

#include <glib.h>

extern gpointer CreateSemaphore(WapiSecurityAttributes *security,
				gint32 initial, gint32 max,
				const guchar *name);
extern gboolean ReleaseSemaphore(gpointer handle, gint32 count,
				 gint32 *prevcount);

#endif /* _WAPI_SEMAPHORES_H_ */
