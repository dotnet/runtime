/*
 * handles-private.h:  Internal operations on handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Novell, Inc.
 */

#ifndef _WAPI_HANDLES_PRIVATE_H_
#define _WAPI_HANDLES_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <errno.h>
#include <string.h>
#include <sys/types.h>

#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/shared.h>
#include <mono/utils/atomic.h>

#define _WAPI_PRIVATE_MAX_SLOTS		(1024 * 16)

#undef DEBUG

extern guint32 _wapi_fd_reserve;
extern gpointer _wapi_global_signal_handle;
extern mono_mutex_t *_wapi_global_signal_mutex;
extern mono_cond_t *_wapi_global_signal_cond;
extern int _wapi_sem_id;
extern gboolean _wapi_has_shut_down;

extern pid_t _wapi_getpid (void);
extern gpointer _wapi_handle_new (WapiHandleType type,
				  gpointer handle_specific);
extern gpointer _wapi_handle_new_fd (WapiHandleType type, int fd,
				     gpointer handle_specific);
extern gboolean _wapi_lookup_handle (gpointer handle, WapiHandleType type,
				     gpointer *handle_specific);
extern gpointer _wapi_search_handle (WapiHandleType type,
				     gboolean (*check)(gpointer, gpointer),
				     gpointer user_data,
				     gpointer *handle_specific,
				     gboolean search_shared);
extern gpointer _wapi_search_handle_namespace (WapiHandleType type,
					     gchar *utf8_name);
extern void _wapi_handle_ref (gpointer handle);
extern void _wapi_handle_unref (gpointer handle);
extern void _wapi_handle_register_capabilities (WapiHandleType type,
						WapiHandleCapability caps);
extern gboolean _wapi_handle_test_capabilities (gpointer handle,
						WapiHandleCapability caps);
extern void _wapi_handle_ops_close (gpointer handle, gpointer data);
extern void _wapi_handle_ops_signal (gpointer handle);
extern gboolean _wapi_handle_ops_own (gpointer handle);
extern gboolean _wapi_handle_ops_isowned (gpointer handle);
extern guint32 _wapi_handle_ops_special_wait (gpointer handle,
					      guint32 timeout,
					      gboolean alertable);
extern void _wapi_handle_ops_prewait (gpointer handle);

extern gboolean _wapi_handle_count_signalled_handles (guint32 numhandles,
						      gpointer *handles,
						      gboolean waitall,
						      guint32 *retcount,
						      guint32 *lowest);
extern void _wapi_handle_unlock_handles (guint32 numhandles,
					 gpointer *handles);
extern int _wapi_handle_timedwait_signal_handle (gpointer handle, guint32 timeout, gboolean poll, gboolean *alerted);
extern gboolean _wapi_handle_get_or_set_share (guint64 device, guint64 inode,
					       guint32 new_sharemode,
					       guint32 new_access,
					       guint32 *old_sharemode,
					       guint32 *old_access,
					       struct _WapiFileShare **info);
extern void _wapi_handle_dump (void);
extern void _wapi_handle_foreach (WapiHandleType type,
					gboolean (*on_each)(gpointer test, gpointer user),
					gpointer user_data);
void _wapi_free_share_info (_WapiFileShare *share_info);

WapiHandleType
_wapi_handle_type (gpointer handle);

void
_wapi_handle_set_signal_state (gpointer handle, gboolean state, gboolean broadcast);

gboolean
_wapi_handle_issignalled (gpointer handle);

static inline int _wapi_handle_lock_signal_mutex (void)
{
#ifdef DEBUG
	g_message ("%s: lock global signal mutex", __func__);
#endif

	return(mono_os_mutex_lock (_wapi_global_signal_mutex));
}

/* the parameter makes it easier to call from a pthread cleanup handler */
static inline int _wapi_handle_unlock_signal_mutex (void *unused)
{
#ifdef DEBUG
	g_message ("%s: unlock global signal mutex", __func__);
#endif

	return(mono_os_mutex_unlock (_wapi_global_signal_mutex));
}

int
_wapi_handle_lock_handle (gpointer handle);

int
_wapi_handle_trylock_handle (gpointer handle);

int
_wapi_handle_unlock_handle (gpointer handle);

static inline void _wapi_handle_spin (guint32 ms)
{
	struct timespec sleepytime;
	
	g_assert (ms < 1000);
	
	sleepytime.tv_sec = 0;
	sleepytime.tv_nsec = ms * 1000000;
	
	nanosleep (&sleepytime, NULL);
}

static inline int _wapi_namespace_lock (void)
{
	return(_wapi_shm_sem_lock (_WAPI_SHARED_SEM_NAMESPACE));
}

/* This signature makes it easier to use in pthread cleanup handlers */
static inline int _wapi_namespace_unlock (gpointer data G_GNUC_UNUSED)
{
	return(_wapi_shm_sem_unlock (_WAPI_SHARED_SEM_NAMESPACE));
}

static inline void _wapi_handle_share_release (struct _WapiFileShare *info)
{
	int thr_ret;

	g_assert (info->handle_refs > 0);
	
	/* Prevent new entries racing with us */
	thr_ret = _wapi_shm_sem_lock (_WAPI_SHARED_SEM_FILESHARE);
	g_assert(thr_ret == 0);

	if (InterlockedDecrement ((gint32 *)&info->handle_refs) == 0) {
		_wapi_free_share_info (info);
	}

	thr_ret = _wapi_shm_sem_unlock (_WAPI_SHARED_SEM_FILESHARE);
}

#endif /* _WAPI_HANDLES_PRIVATE_H_ */
