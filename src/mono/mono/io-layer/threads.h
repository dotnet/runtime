/*
 * threads.h:  Thread handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_THREADS_H_
#define _WAPI_THREADS_H_

#include <glib.h>

#include <pthread.h>

#include <mono/io-layer/io.h>
#include <mono/io-layer/status.h>
#include <mono/io-layer/processes.h>
#include <mono/io-layer/access.h>

G_BEGIN_DECLS

#define STILL_ACTIVE STATUS_PENDING

#define THREAD_ALL_ACCESS		(STANDARD_RIGHTS_REQUIRED|SYNCHRONIZE|0x3ff)

typedef guint32 (*WapiThreadStart)(gpointer);

typedef struct {
	pthread_t id;
	GPtrArray *owned_mutexes;
	gint32 priority;
} MonoW32HandleThread;

G_END_DECLS
#endif /* _WAPI_THREADS_H_ */
