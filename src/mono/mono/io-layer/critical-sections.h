/*
 * critical-sections.h:  Critical sections
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_CRITICAL_SECTIONS_H_
#define _WAPI_CRITICAL_SECTIONS_H_

#include <config.h>

#include <glib.h>
#include <pthread.h>

#include "mono-mutex.h"

typedef struct _WapiCriticalSection WapiCriticalSection;

struct _WapiCriticalSection
{
	guint32 depth;
	mono_mutex_t mutex;
};

extern void InitializeCriticalSection(WapiCriticalSection *section);
extern gboolean InitializeCriticalSectionAndSpinCount(WapiCriticalSection *section, guint32 spincount);
extern void DeleteCriticalSection(WapiCriticalSection *section);
extern guint32 SetCriticalSectionSpinCount(WapiCriticalSection *section, guint32 spincount);
extern gboolean TryEnterCriticalSection(WapiCriticalSection *section);
extern void EnterCriticalSection(WapiCriticalSection *section);
extern void LeaveCriticalSection(WapiCriticalSection *section);

#endif /* _WAPI_CRITICAL_SECTIONS_H_ */
