/*
 * semaphores.h:  Semaphore handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_SEMAPHORES_H_
#define _WAPI_SEMAPHORES_H_

#include <glib.h>

G_BEGIN_DECLS

extern gpointer CreateSemaphore(WapiSecurityAttributes *security,
				gint32 initial, gint32 max,
				const gunichar2 *name);
extern gboolean ReleaseSemaphore(gpointer handle, gint32 count,
				 gint32 *prevcount);
extern gpointer OpenSemaphore (guint32 access, gboolean inherit,
			       const gunichar2 *name);

G_END_DECLS
#endif /* _WAPI_SEMAPHORES_H_ */
