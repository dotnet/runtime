#ifndef _WAPI_MUTEXES_H_
#define _WAPI_MUTEXES_H_

#include <glib.h>

extern gpointer CreateMutex(WapiSecurityAttributes *security, gboolean owned,
			    const guchar *name);
extern gboolean ReleaseMutex(gpointer handle);

#endif /* _WAPI_MUTEXES_H_ */
