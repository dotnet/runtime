/*
 * wait.h:  wait for handles to become signalled
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_WAIT_H_
#define _WAPI_WAIT_H_

#include "mono/io-layer/status.h"

G_BEGIN_DECLS

#define MAXIMUM_WAIT_OBJECTS 64

#define INFINITE		0xFFFFFFFF

#define WAIT_FAILED		0xFFFFFFFF
#define WAIT_OBJECT_0		((STATUS_WAIT_0) +0)
#define WAIT_ABANDONED		((STATUS_ABANDONED_WAIT_0) +0)
#define WAIT_ABANDONED_0	((STATUS_ABANDONED_WAIT_0) +0)

/* WAIT_TIMEOUT is also defined in error.h. Luckily it's the same value */
#define WAIT_TIMEOUT		STATUS_TIMEOUT
#define WAIT_IO_COMPLETION	STATUS_USER_APC

extern guint32 WaitForSingleObject(gpointer handle, guint32 timeout);
extern guint32 WaitForSingleObjectEx(gpointer handle, guint32 timeout, 
					gboolean alertable);
extern guint32 SignalObjectAndWait(gpointer signal_handle, gpointer wait,
				   guint32 timeout, gboolean alertable);
extern guint32 WaitForMultipleObjects(guint32 numobjects, gpointer *handles,
				      gboolean waitall, guint32 timeout);
extern guint32 WaitForMultipleObjectsEx(guint32 numobjects, gpointer *handles,
				      gboolean waitall, guint32 timeout, gboolean alertable);
extern guint32 WaitForInputIdle(gpointer handle, guint32 timeout);

G_END_DECLS
#endif /* _WAPI_WAIT_H_ */
