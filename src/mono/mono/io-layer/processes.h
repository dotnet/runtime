/*
 * processes.h:  Process handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_PROCESSES_H_
#define _WAPI_PROCESSES_H_

#include <glib.h>

#include "mono/io-layer/handles.h"

typedef struct _WapiStartupInfo WapiStartupInfo;

struct _WapiStartupInfo 
{
	guint32 cb;
	guchar *lpReserved;
	guchar *lpDesktop;
	guchar *lpTitle;
	guint32 dwX;
	guint32 dwY;
	guint32 dwXSize;
	guint32 dwYSize;
	guint32 dwXCountChars;
	guint32 dwYCountChars;
	guint32 dwFillAttribute;
	guint32 dwFlags;
	guint16 wShowWindow;
	guint16 cbReserved2;
	guint8 *lpReserved2;
	gpointer hStdInput;
	gpointer hStdOutput;
	gpointer hStdError;
};

typedef struct _WapiProcessInformation WapiProcessInformation;

struct _WapiProcessInformation 
{
	gpointer hProcess;
	gpointer hThread;
	guint32 dwProcessId;
	guint32 dwThreadId;
};

	
#define DEBUG_PROCESS 0x00000001
#define DEBUG_ONLY_THIS_PROCESS 0x00000002
#define CREATE_SUSPENDED 0x00000004
#define DETACHED_PROCESS 0x00000008
#define CREATE_NEW_CONSOLE 0x00000010
#define NORMAL_PRIORITY_CLASS 0x00000020
#define IDLE_PRIORITY_CLASS 0x00000040
#define HIGH_PRIORITY_CLASS 0x00000080
#define REALTIME_PRIORITY_CLASS 0x00000100
#define CREATE_NEW_PROCESS_GROUP 0x00000200
#define CREATE_UNICODE_ENVIRONMENT 0x00000400
#define CREATE_SEPARATE_WOW_VDM 0x00000800
#define CREATE_SHARED_WOW_VDM 0x00001000
#define CREATE_FORCEDOS 0x00002000
#define BELOW_NORMAL_PRIORITY_CLASS 0x00004000
#define ABOVE_NORMAL_PRIORITY_CLASS 0x00008000
#define CREATE_BREAKAWAY_FROM_JOB 0x01000000
#define CREATE_WITH_USERPROFILE 0x02000000
#define CREATE_DEFAULT_ERROR_MODE 0x04000000
#define CREATE_NO_WINDOW 0x08000000

#ifdef NEW_STUFF
#define CREATE_PRESERVE_CODE_AUTHZ_LEVEL find out the value for this one...
#endif

extern gboolean CreateProcess (const gunichar2 *appname, gunichar2 *cmdline,
			       WapiSecurityAttributes *process_attrs,
			       WapiSecurityAttributes *thread_attrs,
			       gboolean inherit_handles, guint32 create_flags,
			       gpointer env, const gunichar2 *cwd,
			       WapiStartupInfo *startup,
			       WapiProcessInformation *process_info);

#endif /* _WAPI_PROCESSES_H_ */
