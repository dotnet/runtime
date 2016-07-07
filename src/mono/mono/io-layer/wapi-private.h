/*
 * wapi-private.h:  internal definitions of handles and shared memory layout
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Novell, Inc.
 */

#ifndef _WAPI_PRIVATE_H_
#define _WAPI_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <sys/stat.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/io.h>
#include <mono/io-layer/shared.h>

#include <mono/utils/mono-os-mutex.h>

/* There doesn't seem to be a defined symbol for this */
#define _WAPI_THREAD_CURRENT (gpointer)0xFFFFFFFE

extern gboolean _wapi_has_shut_down;

typedef struct 
{
	gchar name[MAX_PATH + 1];
} WapiSharedNamespace;

#include <mono/io-layer/event-private.h>
#include <mono/io-layer/io-private.h>
#include <mono/io-layer/mutex-private.h>
#include <mono/io-layer/semaphore-private.h>
#include <mono/io-layer/socket-private.h>
#include <mono/io-layer/process-private.h>
#include <mono/utils/w32handle.h>

struct _WapiHandle_shared_ref
{
	/* This will be split 16:16 with the shared file segment in
	 * the top half, when I implement space increases
	 */
	guint32 offset;
};

#define _WAPI_SHARED_SEM_NAMESPACE 0
/*#define _WAPI_SHARED_SEM_COLLECTION 1*/
#define _WAPI_SHARED_SEM_FILESHARE 2
#define _WAPI_SHARED_SEM_PROCESS_COUNT_LOCK 6
#define _WAPI_SHARED_SEM_PROCESS_COUNT 7
#define _WAPI_SHARED_SEM_COUNT 8	/* Leave some future expansion space */

struct _WapiFileShare
{
#ifdef WAPI_FILE_SHARE_PLATFORM_EXTRA_DATA
	WAPI_FILE_SHARE_PLATFORM_EXTRA_DATA
#endif
	guint64 device;
	guint64 inode;
	pid_t opened_by_pid;
	guint32 sharemode;
	guint32 access;
	guint32 handle_refs;
	guint32 timestamp;
};

typedef struct _WapiFileShare _WapiFileShare;

gpointer
_wapi_search_handle_namespace (MonoW32HandleType type, gchar *utf8_name);

static inline int _wapi_namespace_lock (void)
{
	return(_wapi_shm_sem_lock (_WAPI_SHARED_SEM_NAMESPACE));
}

/* This signature makes it easier to use in pthread cleanup handlers */
static inline int _wapi_namespace_unlock (gpointer data G_GNUC_UNUSED)
{
	return(_wapi_shm_sem_unlock (_WAPI_SHARED_SEM_NAMESPACE));
}

#endif /* _WAPI_PRIVATE_H_ */
