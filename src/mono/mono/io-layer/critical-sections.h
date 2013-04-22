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

#include <glib.h>
#include <pthread.h>

#include <mono/utils/mono-mutex.h>

G_BEGIN_DECLS

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

/* These two are perf critical so avoid the wrapper function */

#define EnterCriticalSection(section) do { \
	int ret = mono_mutex_lock(&(section)->mutex);    \
	if (ret != 0) \
		g_warning ("Bad call to mono_mutex_lock result %d", ret); \
	g_assert (ret == 0);				 \
} while (0)

#define LeaveCriticalSection(section) do { \
	int ret = mono_mutex_unlock(&(section)->mutex);      \
	if (ret != 0) \
		g_warning ("Bad call to mono_mutex_unlock result %d", ret); \
	g_assert (ret == 0);    \
} while (0)

G_END_DECLS

#endif /* _WAPI_CRITICAL_SECTIONS_H_ */
