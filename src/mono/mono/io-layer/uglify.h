/*
 * uglify.h:  Optional header to provide the nasty w32 typedefs
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_UGLIFY_H_
#define _WAPI_UGLIFY_H_

/* Include this file if you insist on using the nasty Win32 typedefs */

#include <stdlib.h>

#include "mono/io-layer/wapi.h"

typedef const gunichar2 *LPCTSTR;
typedef gunichar2 *LPTSTR;
typedef guint8 BYTE;
typedef guint8 *LPBYTE;
typedef guint16 WORD;
typedef guint32 DWORD;
typedef gpointer PVOID;
typedef gpointer LPVOID;
typedef gboolean BOOL;
typedef guint32 *LPDWORD;
typedef gint32 LONG;
typedef guint32 ULONG;
typedef gint32 *PLONG;
typedef guint64 LONGLONG;
typedef gunichar2 TCHAR;

typedef gpointer HANDLE;
typedef gpointer *LPHANDLE;
typedef guint32 SOCKET;
typedef gpointer HMODULE;

typedef WapiSecurityAttributes *LPSECURITY_ATTRIBUTES;
typedef WapiOverlapped *LPOVERLAPPED;
typedef WapiThreadStart LPTHREAD_START_ROUTINE;
typedef WapiCriticalSection CRITICAL_SECTION;
typedef WapiCriticalSection *LPCRITICAL_SECTION;
typedef WapiFileTime FILETIME;
typedef WapiFileTime *LPFILETIME;
typedef WapiSystemTime SYSTEMTIME;
typedef WapiSystemTime *LPSYSTEMTIME;
typedef WapiWSAData WSADATA;
typedef WapiWSAData *LDWSADATA;
typedef WapiLargeInteger LARGE_INTEGER;
typedef WapiLargeInteger *PLARGE_INTEGER;
typedef WapiSystemInfo SYSTEM_INFO;
typedef WapiSystemInfo *LPSYSTEM_INFO;
typedef WapiFloatingSaveArea FLOATING_SAVE_AREA;
typedef WapiFloatingSaveArea *PFLOATING_SAVE_AREA;
typedef WapiContext CONTEXT;
typedef WapiContext *PCONTEXT;
typedef WapiFindData WIN32_FIND_DATA;
typedef WapiFindData *LPWIN32_FIND_DATA;
typedef WapiFileAttributesData WIN32_FILE_ATTRIBUTE_DATA;
typedef WapiGetFileExInfoLevels GET_FILEEX_INFO_LEVELS;
typedef WapiStartupInfo STARTUPINFO;
typedef WapiStartupInfo *LPSTARTUPINFO;
typedef WapiProcessInformation PROCESS_INFORMATION;
typedef WapiFixedFileInfo VS_FIXEDFILEINFO;

#define CONST const
#define VOID void

#define IN
#define OUT
#define WINAPI

#endif /* _WAPI_UGLIFY_H_ */
