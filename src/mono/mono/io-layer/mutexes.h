/*
 * mutexes.h: Mutex handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_MUTEXES_H_
#define _WAPI_MUTEXES_H_

#include <glib.h>

G_BEGIN_DECLS

extern gpointer CreateMutex (WapiSecurityAttributes *security, gboolean owned,
			     const gunichar2 *name);
extern gboolean ReleaseMutex (gpointer handle);
extern gpointer OpenMutex (guint32 access, gboolean inherit,
			   const gunichar2 *name);

G_END_DECLS

#endif /* _WAPI_MUTEXES_H_ */
