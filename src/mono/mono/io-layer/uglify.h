#ifndef _WAPI_UGLIFY_H_
#define _WAPI_UGLIFY_H_

/* Include this file if you insist on using the nasty Win32 typedefs */

#include <stdlib.h>

#include "mono/io-layer/wapi.h"

typedef const guchar *LPCTSTR;		/* replace this with gunichar */
typedef guint16 WORD;
typedef guint32 DWORD;
typedef gpointer LPVOID;
typedef gboolean BOOL;
typedef guint32 *LPDWORD;
typedef gint32 LONG;
typedef gint32 *PLONG;

typedef WapiHandle *HANDLE;
typedef WapiHandle **LPHANDLE;
typedef WapiSecurityAttributes *LPSECURITY_ATTRIBUTES;
typedef WapiOverlapped *LPOVERLAPPED;
typedef WapiThreadStart LPTHREAD_START_ROUTINE;
typedef WapiCriticalSection CRITICAL_SECTION;
typedef WapiCriticalSection *LPCRITICAL_SECTION;
typedef WapiFileTime FILETIME;
typedef WapiFileTime *LPFILETIME;
typedef WapiSystemTime SYSTEMTIME;
typedef WapiSystemTime *LPSYSTEMTIME;

#define CONST const
#define VOID void

#define WINAPI

#endif /* _WAPI_UGLIFY_H_ */
