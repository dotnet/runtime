#ifndef _WAPI_EVENTS_H_
#define _WAPI_EVENTS_H_

#include <glib.h>

extern gpointer CreateEvent(WapiSecurityAttributes *security, gboolean manual,
			    gboolean initial, const guchar *name);
extern gboolean PulseEvent(gpointer handle);
extern gboolean ResetEvent(gpointer handle);
extern gboolean SetEvent(gpointer handle);

#endif /* _WAPI_EVENTS_H_ */
