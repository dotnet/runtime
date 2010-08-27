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
#include <signal.h>
#include <string.h>
#include <sys/types.h>

#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/collection.h>
#include <mono/io-layer/shared.h>

#define _WAPI_PRIVATE_MAX_SLOTS		(1024 * 16)
#define _WAPI_PRIVATE_HANDLES(x) (_wapi_private_handles [x / _WAPI_HANDLE_INITIAL_COUNT][x % _WAPI_HANDLE_INITIAL_COUNT])
#define _WAPI_PRIVATE_HAVE_SLOT(x) ((GPOINTER_TO_UINT (x) / _WAPI_PRIVATE_MAX_SLOTS) < _WAPI_PRIVATE_MAX_SLOTS && \
					_wapi_private_handles [GPOINTER_TO_UINT (x) / _WAPI_HANDLE_INITIAL_COUNT] != NULL)
#define _WAPI_PRIVATE_VALID_SLOT(x) (((x) / _WAPI_HANDLE_INITIAL_COUNT) < _WAPI_PRIVATE_MAX_SLOTS)

#undef DEBUG

extern struct _WapiHandleUnshared *_wapi_private_handles [];
extern struct _WapiHandleSharedLayout *_wapi_shared_layout;
extern struct _WapiFileShareLayout *_wapi_fileshare_layout;

extern guint32 _wapi_fd_reserve;
extern mono_mutex_t *_wapi_global_signal_mutex;
extern pthread_cond_t *_wapi_global_signal_cond;
extern int _wapi_sem_id;
extern gboolean _wapi_has_shut_down;

extern pid_t _wapi_getpid (void);
extern gpointer _wapi_handle_new (WapiHandleType type,
				  gpointer handle_specific);
extern gpointer _wapi_handle_new_fd (WapiHandleType type, int fd,
				     gpointer handle_specific);
extern gpointer _wapi_handle_new_from_offset (WapiHandleType type,
					      guint32 offset,
					      gboolean timestamp);
extern gboolean _wapi_lookup_handle (gpointer handle, WapiHandleType type,
				     gpointer *handle_specific);
extern gpointer _wapi_search_handle (WapiHandleType type,
				     gboolean (*check)(gpointer, gpointer),
				     gpointer user_data,
				     gpointer *handle_specific,
				     gboolean search_shared);
extern gint32 _wapi_search_handle_namespace (WapiHandleType type,
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
					      guint32 timeout);
extern void _wapi_handle_ops_prewait (gpointer handle);

extern gboolean _wapi_handle_count_signalled_handles (guint32 numhandles,
						      gpointer *handles,
						      gboolean waitall,
						      guint32 *retcount,
						      guint32 *lowest);
extern void _wapi_handle_unlock_handles (guint32 numhandles,
					 gpointer *handles);
extern int _wapi_handle_wait_signal (gboolean poll);
extern int _wapi_handle_timedwait_signal (struct timespec *timeout, gboolean poll);
extern int _wapi_handle_wait_signal_handle (gpointer handle, gboolean alertable);
extern int _wapi_handle_timedwait_signal_handle (gpointer handle,
												 struct timespec *timeout, gboolean alertable, gboolean poll);
extern gboolean _wapi_handle_get_or_set_share (dev_t device, ino_t inode,
					       guint32 new_sharemode,
					       guint32 new_access,
					       guint32 *old_sharemode,
					       guint32 *old_access,
					       struct _WapiFileShare **info);
extern void _wapi_handle_check_share (struct _WapiFileShare *share_info,
				      int fd);
extern void _wapi_handle_dump (void);
extern void _wapi_handle_update_refs (void);
extern void _wapi_handle_foreach (WapiHandleType type,
					gboolean (*on_each)(gpointer test, gpointer user),
					gpointer user_data);
void _wapi_free_share_info (_WapiFileShare *share_info);

/* This is OK to use for atomic writes of individual struct members, as they
 * are independent
 */
#define WAPI_SHARED_HANDLE_DATA(handle) _wapi_shared_layout->handles[_WAPI_PRIVATE_HANDLES(GPOINTER_TO_UINT((handle))).u.shared.offset]

#define WAPI_SHARED_HANDLE_TYPED_DATA(handle, type) _wapi_shared_layout->handles[_WAPI_PRIVATE_HANDLES(GPOINTER_TO_UINT((handle))).u.shared.offset].u.type

static inline WapiHandleType _wapi_handle_type (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	
	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(WAPI_HANDLE_COUNT);	/* An impossible type */
	}
	
	return(_WAPI_PRIVATE_HANDLES(idx).type);
}

static inline void _wapi_handle_set_signal_state (gpointer handle,
						  gboolean state,
						  gboolean broadcast)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	struct _WapiHandleUnshared *handle_data;
	int thr_ret;

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return;
	}
	
	g_assert (!_WAPI_SHARED_HANDLE(_wapi_handle_type (handle)));
	
	handle_data = &_WAPI_PRIVATE_HANDLES(idx);
	
#ifdef DEBUG
	g_message ("%s: setting state of %p to %s (broadcast %s)", __func__,
		   handle, state?"TRUE":"FALSE", broadcast?"TRUE":"FALSE");
#endif

	if (state == TRUE) {
		/* Tell everyone blocking on a single handle */

		/* The condition the global signal cond is waiting on is the signalling of
		 * _any_ handle. So lock it before setting the signalled state.
		 */
		pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup, (void *)_wapi_global_signal_mutex);
		thr_ret = mono_mutex_lock (_wapi_global_signal_mutex);
		if (thr_ret != 0)
			g_warning ("Bad call to mono_mutex_lock result %d for global signal mutex", thr_ret);
		g_assert (thr_ret == 0);

		/* This function _must_ be called with
		 * handle->signal_mutex locked
		 */
		handle_data->signalled=state;
		
		if (broadcast == TRUE) {
			thr_ret = pthread_cond_broadcast (&handle_data->signal_cond);
			if (thr_ret != 0)
				g_warning ("Bad call to pthread_cond_broadcast result %d for handle %p", thr_ret, handle);
			g_assert (thr_ret == 0);
		} else {
			thr_ret = pthread_cond_signal (&handle_data->signal_cond);
			if (thr_ret != 0)
				g_warning ("Bad call to pthread_cond_signal result %d for handle %p", thr_ret, handle);
			g_assert (thr_ret == 0);
		}

		/* Tell everyone blocking on multiple handles that something
		 * was signalled
		 */			
		thr_ret = pthread_cond_broadcast (_wapi_global_signal_cond);
		if (thr_ret != 0)
			g_warning ("Bad call to pthread_cond_broadcast result %d for handle %p", thr_ret, handle);
		g_assert (thr_ret == 0);
			
		thr_ret = mono_mutex_unlock (_wapi_global_signal_mutex);
		if (thr_ret != 0)
			g_warning ("Bad call to mono_mutex_unlock result %d for global signal mutex", thr_ret);
		g_assert (thr_ret == 0);

		pthread_cleanup_pop (0);
	} else {
		handle_data->signalled=state;
	}
}

static inline void _wapi_shared_handle_set_signal_state (gpointer handle,
							 gboolean state)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	struct _WapiHandleUnshared *handle_data;
	struct _WapiHandle_shared_ref *ref;
	struct _WapiHandleShared *shared_data;
	
	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return;
	}
	
	g_assert (_WAPI_SHARED_HANDLE(_wapi_handle_type (handle)));
	
	handle_data = &_WAPI_PRIVATE_HANDLES(idx);
	
	ref = &handle_data->u.shared;
	shared_data = &_wapi_shared_layout->handles[ref->offset];
	shared_data->signalled = state;

#ifdef DEBUG
	g_message ("%s: signalled shared handle offset 0x%x", __func__,
		   ref->offset);
#endif
}

static inline gboolean _wapi_handle_issignalled (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	
	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(FALSE);
	}
	
	if (_WAPI_SHARED_HANDLE(_wapi_handle_type (handle))) {
		return(WAPI_SHARED_HANDLE_DATA(handle).signalled);
	} else {
		return(_WAPI_PRIVATE_HANDLES(idx).signalled);
	}
}

static inline int _wapi_handle_lock_signal_mutex (void)
{
#ifdef DEBUG
	g_message ("%s: lock global signal mutex", __func__);
#endif

	return(mono_mutex_lock (_wapi_global_signal_mutex));
}

/* the parameter makes it easier to call from a pthread cleanup handler */
static inline int _wapi_handle_unlock_signal_mutex (void *unused)
{
#ifdef DEBUG
	g_message ("%s: unlock global signal mutex", __func__);
#endif

	return(mono_mutex_unlock (_wapi_global_signal_mutex));
}

static inline int _wapi_handle_lock_handle (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	
#ifdef DEBUG
	g_message ("%s: locking handle %p", __func__, handle);
#endif

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(0);
	}
	
	_wapi_handle_ref (handle);
	
	if (_WAPI_SHARED_HANDLE (_wapi_handle_type (handle))) {
		return(0);
	}
	
	return(mono_mutex_lock (&_WAPI_PRIVATE_HANDLES(idx).signal_mutex));
}

static inline int _wapi_handle_trylock_handle (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	int ret;
	
#ifdef DEBUG
	g_message ("%s: locking handle %p", __func__, handle);
#endif

	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(0);
	}
	
	_wapi_handle_ref (handle);
	
	if (_WAPI_SHARED_HANDLE (_wapi_handle_type (handle))) {
		return(0);
	}

	ret = mono_mutex_trylock (&_WAPI_PRIVATE_HANDLES(idx).signal_mutex);
	if (ret != 0) {
		_wapi_handle_unref (handle);
	}
	
	return(ret);
}

static inline int _wapi_handle_unlock_handle (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	int ret;
	
#ifdef DEBUG
	g_message ("%s: unlocking handle %p", __func__, handle);
#endif
	
	if (!_WAPI_PRIVATE_VALID_SLOT (idx)) {
		return(0);
	}
	
	if (_WAPI_SHARED_HANDLE (_wapi_handle_type (handle))) {
		_wapi_handle_unref (handle);
		return(0);
	}
	
	ret = mono_mutex_unlock (&_WAPI_PRIVATE_HANDLES(idx).signal_mutex);

	_wapi_handle_unref (handle);
	
	return(ret);
}

static inline void _wapi_handle_spin (guint32 ms)
{
	struct timespec sleepytime;
	
	g_assert (ms < 1000);
	
	sleepytime.tv_sec = 0;
	sleepytime.tv_nsec = ms * 1000000;
	
	nanosleep (&sleepytime, NULL);
}

static inline int _wapi_handle_lock_shared_handles (void)
{
	return(_wapi_shm_sem_lock (_WAPI_SHARED_SEM_SHARED_HANDLES));
}

static inline int _wapi_handle_trylock_shared_handles (void)
{
	return(_wapi_shm_sem_trylock (_WAPI_SHARED_SEM_SHARED_HANDLES));
}

static inline int _wapi_handle_unlock_shared_handles (void)
{
	return(_wapi_shm_sem_unlock (_WAPI_SHARED_SEM_SHARED_HANDLES));
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
