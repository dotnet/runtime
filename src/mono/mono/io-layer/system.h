/*
 * system.h:  System information
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_SYSTEM_H_
#define _WAPI_SYSTEM_H_

#include <glib.h>

typedef struct _WapiSystemInfo WapiSystemInfo;

struct _WapiSystemInfo 
{
	union _anon_union
	{
		guint32 dwOemId;
		struct _anon_struct
		{
			guint16 wProcessorArchitecture;
			guint16 wReserved;
		} _anon_struct;
	} _anon_union;
	
	guint32 dwPageSize;
	gpointer lpMinimumApplicationAddress;
	gpointer lpMaximumApplicationAddress;
	guint32 /*_PTR?*/ dwActiveProcessorMask;
	guint32 dwNumberOfProcessors;
	guint32 dwProcessorType;
	guint32 dwAllocationGranularity;
	guint16 wProcessorLevel;
	guint16 wProcessorRevision;
};

extern void GetSystemInfo(WapiSystemInfo *info);

#endif /* _WAPI_SYSTEM_H_ */
