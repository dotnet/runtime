#ifndef _WAPI_HANDLES_PRIVATE_H_
#define _WAPI_HANDLES_PRIVATE_H_

#include <config.h>
#include <glib.h>

#include <mono/io-layer/wait-private.h>
#include <mono/io-layer/shared.h>

#undef DEBUG

extern struct _WapiHandleShared_list *_wapi_shared_data;
extern struct _WapiHandlePrivate_list *_wapi_private_data;
extern guint32 _wapi_open_handles[];

extern void _wapi_handle_init (void);
extern void _wapi_handle_cleanup (void);

extern gpointer _wapi_handle_new (WapiHandleType type);
extern gboolean _wapi_lookup_handle (gpointer handle, WapiHandleType type,
				     gpointer *shared, gpointer *private);
extern guint32 _wapi_handle_scratch_store (gconstpointer data, guint32 bytes);
extern gconstpointer _wapi_handle_scratch_lookup (guint32 idx);
extern guchar *_wapi_handle_scratch_lookup_as_string (guint32 idx);
extern void _wapi_handle_scratch_delete (guint32 idx);
extern void _wapi_handle_register_capabilities (WapiHandleType type,
						WapiHandleCapability caps);
extern gboolean _wapi_handle_test_capabilities (gpointer handle,
						WapiHandleCapability caps);
extern void _wapi_handle_register_ops (WapiHandleType type,
				       struct _WapiHandleOps *ops);
extern void _wapi_handle_ops_close (gpointer handle);
extern WapiFileType _wapi_handle_ops_getfiletype (gpointer handle);
extern gboolean _wapi_handle_ops_readfile (gpointer handle, gpointer buffer,
					   guint32 numbytes,
					   guint32 *bytesread,
					   WapiOverlapped *overlapped);
extern gboolean _wapi_handle_ops_writefile (gpointer handle,
					    gconstpointer buffer,
					    guint32 numbytes,
					    guint32 *byteswritten,
					    WapiOverlapped *overlapped);
extern gboolean _wapi_handle_ops_flushfile (gpointer handle);
extern guint32 _wapi_handle_ops_seek (gpointer handle, gint32 movedistance,
				      gint32 *highmovedistance,
				      WapiSeekMethod method);
extern gboolean _wapi_handle_ops_setendoffile (gpointer handle);
extern guint32 _wapi_handle_ops_getfilesize (gpointer handle,
					     guint32 *highsize);
extern gboolean _wapi_handle_ops_getfiletime (gpointer handle,
					      WapiFileTime *create_time,
					      WapiFileTime *last_access,
					      WapiFileTime *last_write);
extern gboolean _wapi_handle_ops_setfiletime (gpointer handle,
					      const WapiFileTime *create_time,
					      const WapiFileTime *last_access,
					      const WapiFileTime *last_write);
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

static inline void _wapi_handle_shared_lock (void)
{
	/* Spin while waiting for the lock */
	while(InterlockedCompareExchange (&_wapi_shared_data->lock, 1, 0) != 0) {
		sched_yield ();
	}
}

static inline void _wapi_handle_shared_unlock (void)
{
	InterlockedExchange (&_wapi_shared_data->lock, 0);
}

static inline void _wapi_handle_ref (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	InterlockedIncrement (&_wapi_shared_data->handles[idx].ref);
	InterlockedIncrement (&_wapi_open_handles[idx]);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": handle %p ref now %d (%d this process)", handle,
		   _wapi_shared_data->handles[idx].ref,
		   _wapi_open_handles[idx]);
#endif
}

static inline void _wapi_handle_unref (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);

	InterlockedDecrement (&_wapi_shared_data->handles[idx].ref);
	InterlockedDecrement (&_wapi_open_handles[idx]);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": handle %p ref now %d (%d this process)", handle,
		   _wapi_shared_data->handles[idx].ref,
		   _wapi_open_handles[idx]);
#endif

	if(_wapi_shared_data->handles[idx].ref==0) {
		_wapi_handle_shared_lock ();
	
		if (_wapi_open_handles[idx]!=0) {
			g_warning (G_GNUC_PRETTY_FUNCTION ": _wapi_open_handles mismatch, set to %d, should be 0", _wapi_open_handles[idx]);
		}
		
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Destroying handle %p",
			   handle);
#endif
		
		_wapi_handle_ops_close (handle);
		
		_wapi_shared_data->handles[idx].type=WAPI_HANDLE_UNUSED;
		mono_mutex_destroy (&_wapi_shared_data->handles[idx].signal_mutex);
		pthread_cond_destroy (&_wapi_shared_data->handles[idx].signal_cond);
		memset (&_wapi_shared_data->handles[idx].u, '\0', sizeof(_wapi_shared_data->handles[idx].u));

		_wapi_handle_shared_unlock ();
	}
}

static inline void _wapi_handle_set_signal_state (gpointer handle,
						  gboolean state,
						  gboolean broadcast)
{
	guint32 idx=GPOINTER_TO_UINT (handle);

	if(state==TRUE) {
		/* Tell everyone blocking on a single handle */

		/* This function _must_ be called with
		 * handle->signal_mutex locked
		 */
		_wapi_shared_data->handles[idx].signalled=state;
		
		if(broadcast==TRUE) {
			pthread_cond_broadcast (&_wapi_shared_data->handles[idx].signal_cond);
		} else {
			pthread_cond_signal (&_wapi_shared_data->handles[idx].signal_cond);
		}

		/* Tell everyone blocking on multiple handles that something
		 * was signalled
		 */
#ifdef _POSIX_THREAD_PROCESS_SHARED
		mono_mutex_lock (&_wapi_shared_data->signal_mutex);
		pthread_cond_broadcast (&_wapi_shared_data->signal_cond);
		mono_mutex_unlock (&_wapi_shared_data->signal_mutex);
#else
		mono_mutex_lock (&_wapi_private_data->signal_mutex);
		pthread_cond_broadcast (&_wapi_private_data->signal_cond);
		mono_mutex_unlock (&_wapi_private_data->signal_mutex);
#endif /* _POSIX_THREAD_PROCESS_SHARED */
	} else {
		_wapi_shared_data->handles[idx].signalled=state;
	}
}

static inline gboolean _wapi_handle_issignalled (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);

	return(_wapi_shared_data->handles[idx].signalled);
}

static inline int _wapi_handle_lock_signal_mutex (void)
{
#ifdef _POSIX_THREAD_PROCESS_SHARED
	return(mono_mutex_lock (&_wapi_shared_data->signal_mutex));
#else
	return(mono_mutex_lock (&_wapi_private_data->signal_mutex));
#endif /* _POSIX_THREAD_PROCESS_SHARED */
}

static inline int _wapi_handle_unlock_signal_mutex (void)
{
#ifdef _POSIX_THREAD_PROCESS_SHARED
	return(mono_mutex_unlock (&_wapi_shared_data->signal_mutex));
#else
	return(mono_mutex_unlock (&_wapi_private_data->signal_mutex));
#endif /* _POSIX_THREAD_PROCESS_SHARED */
}

static inline int _wapi_handle_lock_handle (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	
	return(mono_mutex_lock (&_wapi_shared_data->handles[idx].signal_mutex));
}

static inline int _wapi_handle_unlock_handle (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	
	return(mono_mutex_unlock (&_wapi_shared_data->handles[idx].signal_mutex));
}

#endif /* _WAPI_HANDLES_PRIVATE_H_ */
