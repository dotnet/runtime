
#ifndef _MONO_UTILS_OS_EVENT_H_
#define _MONO_UTILS_OS_EVENT_H_

#include <config.h>
#include <glib.h>

#include "mono-os-mutex.h"

#ifndef MONO_INFINITE_WAIT
#define MONO_INFINITE_WAIT ((guint32) 0xFFFFFFFF)
#endif

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
	mono_mutex_t mutex;
	mono_cond_t cond;
	gboolean signalled;
#endif
};

void
mono_os_event_init (MonoOSEvent *event, gboolean initial);

void
mono_os_event_destroy (MonoOSEvent *event);

void
mono_os_event_set (MonoOSEvent *event);

void
mono_os_event_reset (MonoOSEvent *event);

MonoOSEventWaitRet
mono_os_event_wait_one (MonoOSEvent *event, guint32 timeout);

MonoOSEventWaitRet
mono_os_event_wait_multiple (MonoOSEvent **events, gsize nevents, gboolean waitall, guint32 timeout);

#endif /* _MONO_UTILS_OS_EVENT_H_ */
