#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <errno.h>

#if HAVE_BOEHM_GC
#include <gc/gc.h>
#endif

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/misc-private.h>

#undef DEBUG

static pthread_once_t shared_data_once=PTHREAD_ONCE_INIT;
static WapiHandleCapability handle_caps[WAPI_HANDLE_COUNT]={0};
static struct _WapiHandleOps *handle_ops[WAPI_HANDLE_COUNT]={NULL};
static guint32 scratch_size=0;

struct _WapiHandleShared_list *_wapi_shared_data=NULL;
struct _WapiHandlePrivate_list *_wapi_private_data=NULL;

/* All non-zero entries here need to be CloseHandle()d that many times
 * on process exit
 */
guint32 _wapi_open_handles[_WAPI_MAX_HANDLES]={0};

static void shared_init (void)
{
	_wapi_shared_data=_wapi_shm_attach (&scratch_size);
	_wapi_private_data=g_new0 (struct _WapiHandlePrivate_list, 1);

	/* This wont be called if the process exits due to a signal,
	 * any suggestions welcome (I tried {on_,at,g_at}exit and gcc
	 * destructor attributes.)
	 */
	atexit (_wapi_handle_cleanup);
}

void _wapi_handle_init (void)
{
	/* Create our own process and main thread handles (assume this
	 * function is being called from the main thread)
	 */
	/* No need to make these variables visible outside this
	 * function, the handles will be cleaned up at process exit.
	 */
	gpointer process_handle;
	gpointer thread_handle;

	process_handle=_wapi_handle_new (WAPI_HANDLE_PROCESS);
	_wapi_handle_lock_handle (process_handle);
	_wapi_handle_unlock_handle (process_handle);
	
	thread_handle=_wapi_handle_new (WAPI_HANDLE_THREAD);
	_wapi_handle_lock_handle (thread_handle);
	_wapi_handle_unlock_handle (thread_handle);
}

void _wapi_handle_cleanup (void)
{
	guint32 i, j;
	
	/* Close all the handles listed in _wapi_open_handles */
	for(i=0; i<_WAPI_MAX_HANDLES; i++) {
		for(j=0; j< _wapi_open_handles[i]; j++) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": Closing %d", i);
#endif

			CloseHandle ((gpointer)i);
		}
	}
}

gpointer _wapi_handle_new (WapiHandleType type)
{
	guint32 idx;
	gpointer handle;
#if HAVE_BOEHM_GC
	gboolean tried_collect=FALSE;
#endif

	pthread_once (&shared_data_once, shared_init);
	
again:
	_wapi_handle_shared_lock ();
	
	/* A linear scan should be fast enough.  Start from 1, leaving
	 * 0 (NULL) as a guard
	 */
	for(idx=1; idx<_WAPI_MAX_HANDLES; idx++) {
		struct _WapiHandleShared *shared=&_wapi_shared_data->handles[idx];
		
		if(shared->type==WAPI_HANDLE_UNUSED) {
			shared->type=type;
			shared->signalled=FALSE;
			mono_mutex_init (&_wapi_shared_data->handles[idx].signal_mutex, NULL);
			pthread_cond_init (&_wapi_shared_data->handles[idx].signal_cond, NULL);
			
			handle=GUINT_TO_POINTER (idx);
			_wapi_handle_ref (handle);
			
			_wapi_handle_shared_unlock ();
			
			return(handle);
		}
	}
	
	_wapi_handle_shared_unlock ();
	
	g_warning (G_GNUC_PRETTY_FUNCTION ": Ran out of handles!");

#if HAVE_BOEHM_GC
	/* See if we can reclaim some handles by forcing a GC collection */
	if(tried_collect==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": Seeing if GC collection helps...");
		GC_gcollect ();
		tried_collect=TRUE;
		goto again;
	} else {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": didn't help, returning error");
	}
#endif
	
	return(GUINT_TO_POINTER (_WAPI_HANDLE_INVALID));
}

gboolean _wapi_lookup_handle (gpointer handle, WapiHandleType type,
			      gpointer *shared, gpointer *private)
{
	struct _WapiHandleShared *shared_handle_data;
	struct _WapiHandlePrivate *private_handle_data;
	guint32 idx=GPOINTER_TO_UINT (handle);

	if(shared!=NULL) {
		shared_handle_data=&_wapi_shared_data->handles[idx];
		if(shared_handle_data->type!=type) {
			return(FALSE);
		}

		*shared=&shared_handle_data->u;
	}
	
	if(private!=NULL) {
		private_handle_data=&_wapi_private_data->handles[idx];

		*private=&private_handle_data->u;
	}
	
	return(TRUE);
}

#define HDRSIZE sizeof(struct _WapiScratchHeader)

guint32 _wapi_handle_scratch_store (gconstpointer data, guint32 bytes)
{
	guint32 idx=0, last_idx=0;
	struct _WapiScratchHeader *hdr, *last_hdr;
	gboolean last_was_free=FALSE;
	guchar *storage=&_wapi_shared_data->scratch_base[0];
	
	_wapi_handle_shared_lock ();
	
	hdr=(struct _WapiScratchHeader *)&storage[0];
	if(hdr->flags==0 && hdr->length==0) {
		/* Need to initialise scratch data */
		hdr->flags |= WAPI_SHM_SCRATCH_FREE;
		hdr->length = scratch_size - HDRSIZE;
	}
	
	while(idx<scratch_size) {
		hdr=(struct _WapiScratchHeader *)&storage[idx];
		
		/* Do a simple first-fit allocation, coalescing
		 * adjacent free blocks as we progress through the
		 * scratch space
		 */
		if(hdr->flags & WAPI_SHM_SCRATCH_FREE &&
		   hdr->length >= bytes + HDRSIZE) {
			/* found space */
			guint32 old_length=hdr->length;
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": found suitable free size at %d, length %d", idx, hdr->length);
#endif

			hdr->flags &= ~WAPI_SHM_SCRATCH_FREE;
			hdr->length=bytes;
			idx += HDRSIZE;
			memcpy (&storage[idx], data, bytes);

#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": copied %d bytes",
				   bytes);
#endif

			/* Put a new header in at the end of the used
			 * space
			 */
			hdr=(struct _WapiScratchHeader *)&storage[idx+bytes];
			hdr->flags |= WAPI_SHM_SCRATCH_FREE;
			hdr->length = old_length-bytes-HDRSIZE;

#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": new header at %d, length %d", idx+bytes, hdr->length);
#endif
			
			_wapi_handle_shared_unlock ();
			
			return(idx);
		} else if(hdr->flags & WAPI_SHM_SCRATCH_FREE &&
			  last_was_free == FALSE) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": found too-small free block at %d, length %d (previous used)", idx, hdr->length);
#endif

			/* save this point in case we can coalesce it with
			 * the next block, if that is free.
			 */
			last_was_free=TRUE;
			last_idx=idx;
			last_hdr=hdr;
			idx+=(hdr->length+HDRSIZE);
		} else if (hdr->flags & WAPI_SHM_SCRATCH_FREE &&
			   last_was_free == TRUE) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": found too-small free block at %d, length %d (previous free)", idx, hdr->length);
#endif

			/* This block and the previous are both free,
			 * so coalesce them
			 */
			last_hdr->length += (hdr->length + HDRSIZE);

			/* If the new block is now big enough, use it
			 * (next time round the loop)
			 */
			if(last_hdr->length >= bytes + HDRSIZE) {
				idx=last_idx;
			} else {
				/* leave the last free info as it is,
				 * in case the next block is also free
				 * and can be coalesced too
				 */
				idx=last_idx+last_hdr->length+HDRSIZE;
			}
		} else {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": found used block at %d, length %d", idx,
				   hdr->length);
#endif

			/* must be used, try next chunk */
			idx+=(hdr->length+HDRSIZE);
		}
	}
	
	_wapi_handle_shared_unlock ();
	
	return(0);
}

guchar *_wapi_handle_scratch_lookup_as_string (guint32 idx)
{
	struct _WapiScratchHeader *hdr;
	guchar *str;
	guchar *storage=&_wapi_shared_data->scratch_base[0];
	
	if(idx < HDRSIZE || idx > scratch_size) {
		return(NULL);
	}
	
	hdr=(struct _WapiScratchHeader *)&storage[idx - HDRSIZE];
	str=g_malloc0 (hdr->length+1);
	memcpy (str, &storage[idx], hdr->length);

	return(str);
}

void _wapi_handle_scratch_delete (guint32 idx)
{
	struct _WapiScratchHeader *hdr;
	guchar *storage=&_wapi_shared_data->scratch_base[0];
	
	if(idx < HDRSIZE || idx > scratch_size) {
		return;
	}
	
	hdr=(struct _WapiScratchHeader *)&storage[idx - HDRSIZE];
	memset (&storage[idx], '\0', hdr->length);
	hdr->flags |= WAPI_SHM_SCRATCH_FREE;
	
	/* We could coalesce forwards here if the next block is also
	 * free, but the _store() function will do that anyway.
	 */
}

void _wapi_handle_register_ops (WapiHandleType type,
				struct _WapiHandleOps *ops)
{
	handle_ops[type]=ops;
}

void _wapi_handle_register_capabilities (WapiHandleType type,
					 WapiHandleCapability caps)
{
	handle_caps[type]=caps;
}

gboolean _wapi_handle_test_capabilities (gpointer handle,
					 WapiHandleCapability caps)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": testing 0x%x against 0x%x (%d)",
		   handle_caps[type], caps, handle_caps[type] & caps);
#endif
	
	return((handle_caps[type] & caps)!=0);
}

void _wapi_handle_ops_close (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->close!=NULL) {
		handle_ops[type]->close(handle);
	}
}

WapiFileType _wapi_handle_ops_getfiletype (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->getfiletype!=NULL) {
		return(handle_ops[type]->getfiletype());
	} else {
		return(FILE_TYPE_UNKNOWN);
	}
}

gboolean _wapi_handle_ops_readfile (gpointer handle, gpointer buffer, guint32 numbytes, guint32 *bytesread, WapiOverlapped *overlapped)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->readfile!=NULL) {
		return(handle_ops[type]->readfile(handle, buffer, numbytes, bytesread, overlapped));
	} else {
		return(FALSE);
	}
}

gboolean _wapi_handle_ops_writefile (gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten, WapiOverlapped *overlapped)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->writefile!=NULL) {
		return(handle_ops[type]->writefile(handle, buffer, numbytes, byteswritten, overlapped));
	} else {
		return(FALSE);
	}
}

gboolean _wapi_handle_ops_flushfile (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->flushfile!=NULL) {
		return(handle_ops[type]->flushfile(handle));
	} else {
		return(FALSE);
	}
}

guint32 _wapi_handle_ops_seek (gpointer handle, gint32 movedistance, gint32 *highmovedistance, WapiSeekMethod method)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->seek!=NULL) {
		return(handle_ops[type]->seek(handle, movedistance, highmovedistance, method));
	} else {
		return(INVALID_SET_FILE_POINTER);
	}
}

gboolean _wapi_handle_ops_setendoffile (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->setendoffile!=NULL) {
		return(handle_ops[type]->setendoffile(handle));
	} else {
		return(FALSE);
	}
}

guint32 _wapi_handle_ops_getfilesize (gpointer handle, guint32 *highsize)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->getfilesize!=NULL) {
		return(handle_ops[type]->getfilesize(handle, highsize));
	} else {
		return(INVALID_FILE_SIZE);
	}
}

gboolean _wapi_handle_ops_getfiletime (gpointer handle, WapiFileTime *create_time, WapiFileTime *last_access, WapiFileTime *last_write)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->getfiletime!=NULL) {
		return(handle_ops[type]->getfiletime(handle, create_time, last_access, last_write));
	} else {
		return(FALSE);
	}
}

gboolean _wapi_handle_ops_setfiletime (gpointer handle, const WapiFileTime *create_time, const WapiFileTime *last_access, const WapiFileTime *last_write)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->setfiletime!=NULL) {
		return(handle_ops[type]->setfiletime(handle, create_time, last_access, last_write));
	} else {
		return(FALSE);
	}
}

void _wapi_handle_ops_signal (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->signal!=NULL) {
		handle_ops[type]->signal (handle);
	}
}

void _wapi_handle_ops_own (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->own_handle!=NULL) {
		handle_ops[type]->own_handle (handle);
	}
}

gboolean _wapi_handle_ops_isowned (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->is_owned!=NULL) {
		return(handle_ops[type]->is_owned (handle));
	} else {
		return(FALSE);
	}
}

/**
 * CloseHandle:
 * @handle: The handle to release
 *
 * Closes and invalidates @handle, releasing any resources it
 * consumes.  When the last handle to a temporary or non-persistent
 * object is closed, that object can be deleted.  Closing the same
 * handle twice is an error.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean CloseHandle(gpointer handle)
{
	_wapi_handle_unref (handle);
	
	return(TRUE);
}

gboolean _wapi_handle_count_signalled_handles (guint32 numhandles,
					       gpointer *handles,
					       gboolean waitall,
					       guint32 *retcount,
					       guint32 *lowest)
{
	guint32 count, i, iter=0;
	gboolean ret;
	
	/* Lock all the handles, with backoff */
again:
	for(i=0; i<numhandles; i++) {
		guint32 idx=GPOINTER_TO_UINT (handles[i]);
		
		ret=mono_mutex_trylock (&_wapi_shared_data->handles[idx].signal_mutex);
		if(ret!=0) {
			/* Bummer */
			struct timespec sleepytime;
			
			while(i--) {
				idx=GPOINTER_TO_UINT (handles[i]);
				mono_mutex_unlock (&_wapi_shared_data->handles[idx].signal_mutex);
			}

			/* If iter ever reaches 100 the nanosleep will
			 * return EINVAL immediately, but we have a
			 * design flaw if that happens.
			 */
			iter++;
			if(iter==100) {
				g_warning (G_GNUC_PRETTY_FUNCTION
					   ": iteration overflow!");
				iter=1;
			}
			
			sleepytime.tv_sec=0;
			sleepytime.tv_nsec=10000000 * iter;	/* 10ms*iter */
			
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Backing off for %d ms", iter*10);
#endif
			nanosleep (&sleepytime, NULL);
			
			goto again;
		}
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Locked all handles");
#endif

	count=0;
	*lowest=numhandles;
	
	for(i=0; i<numhandles; i++) {
		guint32 idx=GPOINTER_TO_UINT (handles[i]);
		
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Checking handle %p",
			   handles[i]);
#endif

		if(((_wapi_handle_test_capabilities (handles[i], WAPI_HANDLE_CAP_OWN)==TRUE) &&
		    (_wapi_handle_ops_isowned (handles[i])==TRUE)) ||
		   (_wapi_shared_data->handles[idx].signalled==TRUE)) {
			count++;
			
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Handle %p signalled", handles[i]);
#endif
			if(*lowest>i) {
				*lowest=i;
			}
		}
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": %d event handles signalled",
		   count);
#endif

	if((waitall==TRUE && count==numhandles) ||
	   (waitall==FALSE && count>0)) {
		ret=TRUE;
	} else {
		ret=FALSE;
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Returning %d", ret);
#endif

	*retcount=count;
	
	return(ret);
}

void _wapi_handle_unlock_handles (guint32 numhandles, gpointer *handles)
{
	guint32 i;
	
	for(i=0; i<numhandles; i++) {
		guint32 idx=GPOINTER_TO_UINT (handles[i]);
		
		mono_mutex_unlock (&_wapi_shared_data->handles[idx].signal_mutex);
	}
}

int _wapi_handle_wait_signal (void)
{
#ifdef _POSIX_THREAD_PROCESS_SHARED
	return(mono_cond_wait (&_wapi_shared_data->signal_cond,
			       &_wapi_shared_data->signal_mutex));
#else
	return(mono_cond_wait (&_wapi_private_data->signal_cond,
			       &_wapi_private_data->signal_mutex));
#endif /* _POSIX_THREAD_PROCESS_SHARED */
}

int _wapi_handle_timedwait_signal (struct timespec *timeout)
{
#ifdef _POSIX_THREAD_PROCESS_SHARED
	return(mono_cond_timedwait (&_wapi_shared_data->signal_cond,
				    &_wapi_shared_data->signal_mutex,
				    timeout));
#else
	return(mono_cond_timedwait (&_wapi_private_data->signal_cond,
				    &_wapi_private_data->signal_mutex,
				    timeout));
#endif /* _POSIX_THREAD_PROCESS_SHARED */
}

int _wapi_handle_wait_signal_handle (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	
	return(mono_cond_wait (&_wapi_shared_data->handles[idx].signal_cond,
			       &_wapi_shared_data->handles[idx].signal_mutex));
}

int _wapi_handle_timedwait_signal_handle (gpointer handle,
					  struct timespec *timeout)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	
	return(mono_cond_timedwait (&_wapi_shared_data->handles[idx].signal_cond,
				    &_wapi_shared_data->handles[idx].signal_mutex,
				    timeout));
}
