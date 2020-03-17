/**
 * \file
 */

#ifndef _MONO_UTILS_OS_EVENT_H_
#define _MONO_UTILS_OS_EVENT_H_

#include <config.h>
#include <glib.h>

#include <mono/utils/mono-publib.h>
#include "mono-os-mutex.h"

#define MONO_INFINITE_WAIT ((guint32) 0xFFFFFFFF)

#define MONO_OS_EVENT_WAIT_MAXIMUM_OBJECTS 64

typedef enum {
	MONO_OS_EVENT_WAIT_RET_SUCCESS_0 =  0,
	MONO_OS_EVENT_WAIT_RET_ALERTED   = -1,
	MONO_OS_EVENT_WAIT_RET_TIMEOUT   = -2,
} MonoOSEventWaitRet;

typedef struct _MonoOSEvent MonoOSEvent;

typedef void (*MonoOSEventFreeCb) (MonoOSEvent*);

struct _MonoOSEvent {
#ifdef HOST_WIN32
	gpointer handle;
#else
	GPtrArray *conds;
	gboolean signalled;
#endif
};

MONO_API void
mono_os_event_init (MonoOSEvent *event, gboolean initial);

MONO_API void
mono_os_event_destroy (MonoOSEvent *event);

MONO_API void
mono_os_event_set (MonoOSEvent *event);

MONO_API void
mono_os_event_reset (MonoOSEvent *event);

MONO_API MonoOSEventWaitRet
mono_os_event_wait_one (MonoOSEvent *event, guint32 timeout, gboolean alertable);

MONO_API MonoOSEventWaitRet
mono_os_event_wait_multiple (MonoOSEvent **events, gsize nevents, gboolean waitall, guint32 timeout, gboolean alertable);

#endif /* _MONO_UTILS_OS_EVENT_H_ */
