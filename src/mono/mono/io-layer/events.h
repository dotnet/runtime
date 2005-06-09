/*
 * events.h:  Event handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_EVENTS_H_
#define _WAPI_EVENTS_H_

#include <glib.h>

G_BEGIN_DECLS

extern gpointer CreateEvent(WapiSecurityAttributes *security, gboolean manual,
			    gboolean initial, const gunichar2 *name);
extern gboolean PulseEvent(gpointer handle);
extern gboolean ResetEvent(gpointer handle);
extern gboolean SetEvent(gpointer handle);

G_END_DECLS

#endif /* _WAPI_EVENTS_H_ */
