/*
 * handles-private.h:  Internal operations on handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_HANDLES_PRIVATE_H_
#define _WAPI_HANDLES_PRIVATE_H_

#include <config.h>
#include <glib.h>

#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/misc-private.h>

#undef DEBUG

/* Shared threads dont seem to work yet */
#undef _POSIX_THREAD_PROCESS_SHARED

extern struct _WapiHandleShared_list **_wapi_shared_data;
extern struct _WapiHandleScratch *_wapi_shared_scratch;
extern struct _WapiHandlePrivate_list **_wapi_private_data;
extern pthread_mutex_t _wapi_shared_mutex;
extern guint32 _wapi_shm_mapped_segments;

extern guint32 _wapi_fd_offset_table_size;
extern gpointer *_wapi_fd_offset_table;

extern guint32 _wapi_handle_new_internal (WapiHandleType type);
extern gpointer _wapi_handle_new (WapiHandleType type);
extern gboolean _wapi_lookup_handle (gpointer handle, WapiHandleType type,
				     gpointer *shared, gpointer *private);
extern gpointer _wapi_search_handle (WapiHandleType type,
				     gboolean (*check)(gpointer, gpointer),
				     gpointer user_data,
				     gpointer *shared, gpointer *private);
extern gpointer _wapi_search_handle_namespace (WapiHandleType type,
					       gchar *utf8_name,
					       gpointer *shared,
					       gpointer *private);
extern void _wapi_handle_ref (gpointer handle);
extern void _wapi_handle_unref (gpointer handle);
extern guint32 _wapi_handle_scratch_store_internal (guint32 bytes,
						    gboolean *remap);
extern guint32 _wapi_handle_scratch_store (gconstpointer data, guint32 bytes);
extern guint32 _wapi_handle_scratch_store_string_array (gchar **data);
extern gpointer _wapi_handle_scratch_lookup (guint32 idx);
extern gchar **_wapi_handle_scratch_lookup_string_array (guint32 idx);
extern void _wapi_handle_scratch_delete_internal (guint32 idx);
extern void _wapi_handle_scratch_delete (guint32 idx);
extern void _wapi_handle_scratch_delete_string_array (guint32 idx);
extern void _wapi_handle_register_capabilities (WapiHandleType type,
						WapiHandleCapability caps);
extern gboolean _wapi_handle_test_capabilities (gpointer handle,
						WapiHandleCapability caps);
extern void _wapi_handle_ops_close_shared (gpointer handle);
extern void _wapi_handle_ops_close_private (gpointer handle);
extern void _wapi_handle_ops_signal (gpointer handle);
extern void _wapi_handle_ops_own (gpointer handle);
extern gboolean _wapi_handle_ops_isowned (gpointer handle);

extern gboolean _wapi_handle_count_signalled_handles (guint32 numhandles,
						      gpointer *handles,
						      gboolean waitall,
						      guint32 *retcount,
						      guint32 *lowest);
extern void _wapi_handle_unlock_handles (guint32 numhandles,
					 gpointer *handles);
extern int _wapi_handle_wait_signal (void);
extern int _wapi_handle_timedwait_signal (struct timespec *timeout);
extern int _wapi_handle_wait_signal_handle (gpointer handle);
extern int _wapi_handle_timedwait_signal_handle (gpointer handle,
						 struct timespec *timeout);
extern gboolean _wapi_handle_process_fork (guint32 cmd, guint32 env,
					   guint32 dir, gboolean inherit,
					   guint32 flags,
					   gpointer stdin_handle,
					   gpointer stdout_handle,
					   gpointer stderr_handle,
					   gpointer *process_handle,
					   gpointer *thread_handle,
					   guint32 *pid, guint32 *tid);

extern gboolean _wapi_handle_process_kill (pid_t pid, guint32 signo,
					   gint *err);
extern gboolean _wapi_handle_get_or_set_share (dev_t device, ino_t inode,
					       guint32 new_sharemode,
					       guint32 new_access,
					       guint32 *old_sharemode,
					       guint32 *old_access);
extern void _wapi_handle_set_share (dev_t device, ino_t inode,
				    guint32 sharemode, guint32 access);

static inline void _wapi_handle_fd_offset_store (int fd, gpointer handle)
{
	g_assert (fd < _wapi_fd_offset_table_size);
	g_assert (_wapi_fd_offset_table[fd]==NULL || handle==NULL);
	g_assert (GPOINTER_TO_UINT (handle) >= _wapi_fd_offset_table_size || handle==NULL);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Assigning fd offset %d to %p", fd,
		   handle);
#endif

	_wapi_fd_offset_table[fd]=handle;
}

static inline gpointer _wapi_handle_fd_offset_to_handle (gpointer fd_handle)
{
	int fd = GPOINTER_TO_INT (fd_handle);
	
	g_assert (fd < _wapi_fd_offset_table_size);
	g_assert (_wapi_fd_offset_table[fd]!=NULL);
	g_assert (GPOINTER_TO_UINT (_wapi_fd_offset_table[fd]) >= _wapi_fd_offset_table_size);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Returning fd offset %d of %p", fd,
		   _wapi_fd_offset_table[fd]);
#endif

	return(_wapi_fd_offset_table[fd]);
}

static inline struct _WapiHandleShared_list *_wapi_handle_get_shared_segment (guint32 segment)
{
	struct _WapiHandleShared_list *shared;
	int thr_ret;
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&_wapi_shared_mutex);
	thr_ret = pthread_mutex_lock (&_wapi_shared_mutex);
	g_assert (thr_ret == 0);
	
	shared=_wapi_shared_data[segment];

	thr_ret = pthread_mutex_unlock (&_wapi_shared_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);

	return(shared);
}

static inline struct _WapiHandlePrivate_list *_wapi_handle_get_private_segment (guint32 segment)
{
	struct _WapiHandlePrivate_list *priv;
	int thr_ret;
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&_wapi_shared_mutex);
	thr_ret = pthread_mutex_lock (&_wapi_shared_mutex);
	g_assert (thr_ret == 0);
	
	priv=_wapi_private_data[segment];
	
	thr_ret = pthread_mutex_unlock (&_wapi_shared_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	return(priv);
}

static inline void _wapi_handle_ensure_mapped (guint32 segment)
{
	int thr_ret;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": checking segment %d is mapped",
		   segment);
	g_message (G_GNUC_PRETTY_FUNCTION ": _wapi_shm_mapped_segments: %d",
		   _wapi_shm_mapped_segments);
	if(segment<_wapi_shm_mapped_segments) {
		g_message (G_GNUC_PRETTY_FUNCTION ": _wapi_handle_get_shared_segment(segment): %p", _wapi_handle_get_shared_segment (segment));
	}
#endif

	if(segment<_wapi_shm_mapped_segments &&
	   _wapi_handle_get_shared_segment (segment)!=NULL) {
		/* Got it already */
		return;
	}
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&_wapi_shared_mutex);
	thr_ret = pthread_mutex_lock (&_wapi_shared_mutex);
	g_assert (thr_ret == 0);
	
	if(segment>=_wapi_shm_mapped_segments) {
		/* Need to extend the arrays.  We can't use g_renew
		 * here, because the unmapped segments must be NULL,
		 * and g_renew doesn't initialise the memory it
		 * returns
		 */
		gulong old_len, new_len;
		
		old_len=_wapi_shm_mapped_segments;
		new_len=segment+1;
		
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": extending shared array: mapped_segments is %d",
			   _wapi_shm_mapped_segments);
#endif
		
		_wapi_shared_data=_wapi_g_renew0 (_wapi_shared_data, sizeof(struct _WapiHandleShared_list *) * old_len, sizeof(struct _WapiHandleShared_list *) * new_len);

		if(_wapi_private_data!=NULL) {
			/* the daemon doesn't deal with private data */
			_wapi_private_data=_wapi_g_renew0 (_wapi_private_data, sizeof(struct _WapiHandlePrivate_list *) * old_len, sizeof(struct _WapiHandlePrivate_list *) * new_len);
		}
		
		_wapi_shm_mapped_segments=segment+1;
	}
	
	if(_wapi_shared_data[segment]==NULL) {
		/* Need to map it too */
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": mapping segment %d",
			   segment);
#endif

		_wapi_shared_data[segment]=_wapi_shm_file_map (WAPI_SHM_DATA,
							       segment, NULL,
							       NULL);
		if(_wapi_private_data!=NULL) {
			/* the daemon doesn't deal with private data */
			_wapi_private_data[segment]=g_new0 (struct _WapiHandlePrivate_list, 1);
		}
	}

	thr_ret = pthread_mutex_unlock (&_wapi_shared_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
}

static inline void _wapi_handle_segment (gpointer handle, guint32 *segment,
					 guint32 *idx)
{
	guint32 h=GPOINTER_TO_UINT (handle);
	div_t divvy;

	divvy=div (h, _WAPI_HANDLES_PER_SEGMENT);
	*segment=divvy.quot;
	*idx=divvy.rem;
}

static inline guint32 _wapi_handle_index (guint32 segment, guint32 idx)
{
	return((segment*_WAPI_HANDLES_PER_SEGMENT)+idx);
}

static inline WapiHandleType _wapi_handle_type (gpointer handle)
{
	guint32 idx;
	guint32 segment;
	
	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}
	
	_wapi_handle_segment (handle, &segment, &idx);

	if(segment>=_wapi_shm_mapped_segments)
		return WAPI_HANDLE_UNUSED;
	
	return(_wapi_handle_get_shared_segment (segment)->handles[idx].type);
}

static inline void _wapi_handle_set_signal_state (gpointer handle,
						  gboolean state,
						  gboolean broadcast)
{
	guint32 idx;
	guint32 segment;
	struct _WapiHandleShared *shared_handle;
	int thr_ret;
	
	g_assert (GPOINTER_TO_UINT (handle) >= _wapi_fd_offset_table_size);
	
	_wapi_handle_segment (handle, &segment, &idx);
	shared_handle=&_wapi_handle_get_shared_segment (segment)->handles[idx];
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": setting state of %p to %s (broadcast %s)", handle, state?"TRUE":"FALSE", broadcast?"TRUE":"FALSE");
#endif

	if(state==TRUE) {
		/* Tell everyone blocking on a single handle */

		/* This function _must_ be called with
		 * handle->signal_mutex locked
		 */
		shared_handle->signalled=state;
		
		if(broadcast==TRUE) {
			thr_ret = pthread_cond_broadcast (&shared_handle->signal_cond);
			g_assert (thr_ret == 0);
		} else {
			thr_ret = pthread_cond_signal (&shared_handle->signal_cond);
			g_assert (thr_ret == 0);
		}

		/* Tell everyone blocking on multiple handles that something
		 * was signalled
		 */
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
		{
			struct _WapiHandleShared_list *segment0=_wapi_handle_get_shared_segment (0);
			
			pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup, (void *)&segment0->signal_mutex);
			thr_ret = mono_mutex_lock (&segment0->signal_mutex);
			g_assert (thr_ret == 0);
			
			thr_ret = pthread_cond_broadcast (&segment0->signal_cond);
			g_assert (thr_ret == 0);
			
			thr_ret = mono_mutex_unlock (&segment0->signal_mutex);
			g_assert (thr_ret == 0);
			pthread_cleanup_pop (0);
		}
#else
		{
			struct _WapiHandlePrivate_list *segment0=_wapi_handle_get_private_segment (0);
			
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": lock global signal mutex");
#endif

			pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup, (void *)&segment0->signal_mutex);
			thr_ret = mono_mutex_lock (&segment0->signal_mutex);
			g_assert (thr_ret == 0);
			
			thr_ret = pthread_cond_broadcast (&segment0->signal_cond);
			g_assert (thr_ret == 0);

#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": unlock global signal mutex");
#endif

			thr_ret = mono_mutex_unlock (&segment0->signal_mutex);
			g_assert (thr_ret == 0);
			pthread_cleanup_pop (0);
		}
#endif /* _POSIX_THREAD_PROCESS_SHARED */
	} else {
		shared_handle->signalled=state;
	}
}

static inline gboolean _wapi_handle_issignalled (gpointer handle)
{
	guint32 idx;
	guint32 segment;
	
	g_assert (GPOINTER_TO_UINT (handle) >= _wapi_fd_offset_table_size);
	
	_wapi_handle_segment (handle, &segment, &idx);
	
	return(_wapi_handle_get_shared_segment (segment)->handles[idx].signalled);
}

static inline int _wapi_handle_lock_signal_mutex (void)
{
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": lock global signal mutex");
#endif
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
	return(mono_mutex_lock (&_wapi_handle_get_shared_segment (0)->signal_mutex));
#else
	return(mono_mutex_lock (&_wapi_handle_get_private_segment (0)->signal_mutex));
#endif /* _POSIX_THREAD_PROCESS_SHARED */
}

/* the parameter makes it easier to call from a pthread cleanup handler */
static inline int _wapi_handle_unlock_signal_mutex (void *unused)
{
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": unlock global signal mutex");
#endif
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
	return(mono_mutex_unlock (&_wapi_handle_get_shared_segment (0)->signal_mutex));
#else
	return(mono_mutex_unlock (&_wapi_handle_get_private_segment (0)->signal_mutex));
#endif /* _POSIX_THREAD_PROCESS_SHARED */
}

static inline int _wapi_handle_lock_handle (gpointer handle)
{
	guint32 idx;
	guint32 segment;
	
	g_assert (GPOINTER_TO_UINT (handle) >= _wapi_fd_offset_table_size);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": locking handle %p", handle);
#endif

	_wapi_handle_ref (handle);
	
	_wapi_handle_segment (handle, &segment, &idx);
	
	return(mono_mutex_lock (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex));
}

static inline int _wapi_handle_unlock_handle (gpointer handle)
{
	guint32 idx;
	guint32 segment;
	int ret;
	
	g_assert (GPOINTER_TO_UINT (handle) >= _wapi_fd_offset_table_size);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": unlocking handle %p", handle);
#endif

	_wapi_handle_segment (handle, &segment, &idx);
	
	ret = mono_mutex_unlock (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex);

	_wapi_handle_unref (handle);
	
	return(ret);
}

#endif /* _WAPI_HANDLES_PRIVATE_H_ */
