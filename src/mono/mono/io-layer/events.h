#ifndef _WAPI_EVENTS_H_
#define _WAPI_EVENTS_H_

#include <glib.h>

extern WapiHandle *CreateEvent(WapiSecurityAttributes *security, gboolean manual, gboolean initial, const guchar *name);
extern gboolean PulseEvent(WapiHandle *handle);
extern gboolean ResetEvent(WapiHandle *handle);
extern gboolean SetEvent(WapiHandle *handle);

#endif /* _WAPI_EVENTS_H_ */
