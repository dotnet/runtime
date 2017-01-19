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
typedef const char *LPCSTR;
typedef char *LPSTR;
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
typedef size_t SIZE_T;
typedef guint64 ULONG64;
typedef guint UINT;
typedef gconstpointer LPCVOID;

typedef gpointer HANDLE;
typedef gpointer *LPHANDLE;
typedef gpointer HMODULE;
typedef gpointer HINSTANCE;
typedef gpointer HWND;
typedef gpointer HKEY;

typedef WapiSecurityAttributes SECURITY_ATTRIBUTES;
typedef WapiSecurityAttributes *LPSECURITY_ATTRIBUTES;
typedef WapiOverlapped *LPOVERLAPPED;
typedef WapiOverlappedCB LPOVERLAPPED_COMPLETION_ROUTINE;
typedef WapiFileTime FILETIME;
typedef WapiFileTime *LPFILETIME;
typedef WapiSystemTime SYSTEMTIME;
typedef WapiSystemTime *LPSYSTEMTIME;
typedef WapiFindData WIN32_FIND_DATA;
typedef WapiFindData *LPWIN32_FIND_DATA;
typedef WapiFileAttributesData WIN32_FILE_ATTRIBUTE_DATA;
typedef WapiGetFileExInfoLevels GET_FILEEX_INFO_LEVELS;

#define CONST const
#define VOID void

#define IN
#define OUT
#define WINAPI

#endif /* _WAPI_UGLIFY_H_ */
