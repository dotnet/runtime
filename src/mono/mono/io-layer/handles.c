/*
 * handles.c:  Generic and internal operations on handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <errno.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

#include <mono/os/gc_wrapper.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/daemon-messages.h>

#undef DEBUG

/* Shared threads don't seem to work yet */
#undef _POSIX_THREAD_PROCESS_SHARED

/*
 * This flag _MUST_ remain set to FALSE in the daemon process.  When
 * we exec()d a standalone daemon, that happened because shared_init()
 * didnt get called in the daemon process.  Now we just fork() without
 * exec(), we need to ensure that the fork() happens when shared is
 * still FALSE.
 *
 * This is further complicated by the second attempt to start the
 * daemon if the connect() fails.
 */
static gboolean shared=FALSE;

static WapiHandleCapability handle_caps[WAPI_HANDLE_COUNT]={0};
static struct _WapiHandleOps *handle_ops[WAPI_HANDLE_COUNT]={
	NULL,
	&_wapi_file_ops,
	&_wapi_console_ops,
	&_wapi_thread_ops,
	&_wapi_sem_ops,
	&_wapi_mutex_ops,
	&_wapi_event_ops,
	&_wapi_socket_ops,
	&_wapi_find_ops,
	&_wapi_process_ops,
	&_wapi_pipe_ops,
};

static int daemon_sock;

static pthread_mutexattr_t mutex_shared_attr;
static pthread_condattr_t cond_shared_attr;

struct _WapiHandleShared_list *_wapi_shared_data=NULL;
struct _WapiHandlePrivate_list *_wapi_private_data=NULL;

static void shared_init (void)
{
	struct sockaddr_un shared_socket_address;
	gboolean tried_once=FALSE;
	int ret;

attach_again:

#ifndef DISABLE_SHARED_HANDLES
	if(getenv ("MONO_DISABLE_SHM"))
#endif
	{
		shared=FALSE;
#ifndef DISABLE_SHARED_HANDLES
	} else {
		gboolean success;
		
		/* Ensure that shared==FALSE while _wapi_shm_attach()
		 * calls fork()
		 */
		shared=FALSE;
		
		_wapi_shared_data=_wapi_shm_attach (&success);
		shared=success;
		if(shared==FALSE) {
			g_warning ("Failed to attach shared memory! "
				   "Falling back to non-shared handles");
		}
#endif /* DISABLE_SHARED_HANDLES */
	}
	

	if(shared==TRUE) {
		daemon_sock=socket (PF_UNIX, SOCK_STREAM, 0);
		shared_socket_address.sun_family=AF_UNIX;
		memcpy (shared_socket_address.sun_path,
			_wapi_shared_data->daemon, MONO_SIZEOF_SUNPATH);
		ret=connect (daemon_sock, (struct sockaddr *)&shared_socket_address,
			     sizeof(struct sockaddr_un));
		if(ret==-1) {
			if(tried_once==TRUE) {
				g_warning (G_GNUC_PRETTY_FUNCTION
					   ": connect to daemon failed: %s",
					   g_strerror (errno));
				/* Fall back to private handles */
				shared=FALSE;
			} else {
				/* It's possible that the daemon
				 * crashed without destroying the
				 * shared memory segment (thus fooling
				 * subsequent processes into thinking
				 * the daemon is still active).
				 * 
				 * Destroy the shared memory segment
				 * and try once more.  This won't
				 * break running apps, but no new apps
				 * will be able to see the current
				 * shared memory segment.
				 */
				tried_once=TRUE;
				_wapi_shm_destroy ();
				
				goto attach_again;
			}
		}
	}

	if(shared==FALSE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": Using process-private handles");
#endif
		_wapi_shared_data=
			g_malloc0 (sizeof(struct _WapiHandleShared_list)+
				   _WAPI_SHM_SCRATCH_SIZE);
	}
	_wapi_private_data=g_new0 (struct _WapiHandlePrivate_list, 1);

	pthread_mutexattr_init (&mutex_shared_attr);
	pthread_condattr_init (&cond_shared_attr);

#ifdef _POSIX_THREAD_PROCESS_SHARED
	pthread_mutexattr_setpshared (&mutex_shared_attr,
				      PTHREAD_PROCESS_SHARED);
	pthread_condattr_setpshared (&cond_shared_attr,
				     PTHREAD_PROCESS_SHARED);
#endif
}

/*
 * _wapi_handle_new_internal:
 * @type: Init handle to this type
 *
 * Search for a free handle and initialize it. Return the handle on
 * succes and 0 on failure.
 */
guint32 _wapi_handle_new_internal (WapiHandleType type)
{
	guint32 i;
	static guint32 last=1;
	
	/* A linear scan should be fast enough.  Start from the last
	 * allocation, assuming that handles are allocated more often
	 * than they're freed. Leave 0 (NULL) as a guard
	 */
again:
	for(i=last; i<_WAPI_MAX_HANDLES; i++) {
		struct _WapiHandleShared *shared=&_wapi_shared_data->handles[i];
		
		if(shared->type==WAPI_HANDLE_UNUSED) {
			last=i+1;
			shared->type=type;
			shared->signalled=FALSE;
#ifdef _POSIX_THREAD_PROCESS_SHARED
			mono_mutex_init (&_wapi_shared_data->handles[i].signal_mutex, &mutex_shared_attr);
			pthread_cond_init (&_wapi_shared_data->handles[i].signal_cond, &cond_shared_attr);
#endif

			return(i);
		}
	}

	if(last>1) {
		/* Try again from the beginning */
		last=1;
		goto again;
	}

	return(0);
}

gpointer _wapi_handle_new (WapiHandleType type)
{
	static mono_once_t shared_init_once = MONO_ONCE_INIT;
	static pthread_mutex_t scan_mutex=PTHREAD_MUTEX_INITIALIZER;
	guint32 idx;
	gpointer handle;
	WapiHandleRequest new;
	WapiHandleResponse new_resp;
#if HAVE_BOEHM_GC
	gboolean tried_collect=FALSE;
#endif
	
	mono_once (&shared_init_once, shared_init);
	
again:
	if(shared==TRUE) {
		new.type=WapiHandleRequestType_New;
		new.u.new.type=type;
	
		_wapi_daemon_request_response (daemon_sock, &new, &new_resp);
	
		if (new_resp.type==WapiHandleResponseType_New) {
			idx=new_resp.u.new.handle;
		} else {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": bogus daemon response, type %d",
				   new_resp.type);
			g_assert_not_reached ();
		}
	} else {
		pthread_mutex_lock (&scan_mutex);
		idx=_wapi_handle_new_internal (type);
		pthread_mutex_unlock (&scan_mutex);
	}
		
	if(idx==0) {
		g_warning (G_GNUC_PRETTY_FUNCTION ": Ran out of handles!");

#if HAVE_BOEHM_GC
		/* See if we can reclaim some handles by forcing a GC
		 * collection
		 */
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

#ifndef _POSIX_THREAD_PROCESS_SHARED
	mono_mutex_init (&_wapi_shared_data->handles[idx].signal_mutex, &mutex_shared_attr);
	pthread_cond_init (&_wapi_shared_data->handles[idx].signal_cond, &cond_shared_attr);
#endif
	handle=GUINT_TO_POINTER (idx);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Allocated new handle %p", handle);
#endif

	return(handle);
}

gboolean _wapi_lookup_handle (gpointer handle, WapiHandleType type,
			      gpointer *shared, gpointer *private)
{
	struct _WapiHandleShared *shared_handle_data;
	struct _WapiHandlePrivate *private_handle_data;
	guint32 idx=GPOINTER_TO_UINT (handle);

	shared_handle_data=&_wapi_shared_data->handles[idx];

	if(shared_handle_data->type!=type) {
		return(FALSE);
	}

	if(shared!=NULL) {
		*shared=&shared_handle_data->u;
	}
	
	if(private!=NULL) {
		private_handle_data=&_wapi_private_data->handles[idx];

		*private=&private_handle_data->u;
	}
	
	return(TRUE);
}

gpointer _wapi_search_handle (WapiHandleType type,
			      gboolean (*check)(gpointer test, gpointer user),
			      gpointer user_data,
			      gpointer *shared, gpointer *private)
{
	struct _WapiHandleShared *shared_handle_data;
	struct _WapiHandlePrivate *private_handle_data;
	guint32 i;

	for(i=1; i<_WAPI_MAX_HANDLES; i++) {
		struct _WapiHandleShared *shared=&_wapi_shared_data->handles[i];
		
		if(shared->type==type) {
			if(check (GUINT_TO_POINTER (i), user_data)==TRUE) {
				break;
			}
		}
	}

	if(i==_WAPI_MAX_HANDLES) {
		return(GUINT_TO_POINTER (0));
	}
	
	if(shared!=NULL) {
		shared_handle_data=&_wapi_shared_data->handles[i];

		*shared=&shared_handle_data->u;
	}
	
	if(private!=NULL) {
		private_handle_data=&_wapi_private_data->handles[i];

		*private=&private_handle_data->u;
	}
	
	return(GUINT_TO_POINTER (i));
}

void _wapi_handle_ref (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);

	if(shared==TRUE) {
		WapiHandleRequest req;
		WapiHandleResponse resp;
	
		req.type=WapiHandleRequestType_Open;
		req.u.open.handle=idx;
	
		_wapi_daemon_request_response (daemon_sock, &req, &resp);
		if(resp.type!=WapiHandleResponseType_Open) {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": bogus daemon response, type %d",
				   resp.type);
			g_assert_not_reached ();
		}
	} else {
		_wapi_shared_data->handles[idx].ref++;
	
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": handle %p ref now %d",
			   handle, _wapi_shared_data->handles[idx].ref);
#endif
	}
}

void _wapi_handle_unref (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	gboolean destroy;
	
	if(shared==TRUE) {
		WapiHandleRequest req;
		WapiHandleResponse resp;
	
		req.type=WapiHandleRequestType_Close;
		req.u.close.handle=GPOINTER_TO_UINT (handle);
	
		_wapi_daemon_request_response (daemon_sock, &req, &resp);
		if(resp.type!=WapiHandleResponseType_Close) {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": bogus daemon response, type %d",
				   resp.type);
			g_assert_not_reached ();
		} else {
			destroy=resp.u.close.destroy;
		}
	} else {
		_wapi_shared_data->handles[idx].ref--;
	
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": handle %p ref now %d",
			   handle, _wapi_shared_data->handles[idx].ref);
#endif

		/* Possible race condition here if another thread refs
		 * the handle between here and setting the type to
		 * UNUSED.  I could lock a mutex, but I'm not sure
		 * that allowing a handle reference to reach 0 isn't
		 * an application bug anyway.
		 */
		destroy=(_wapi_shared_data->handles[idx].ref==0);
	}

	if(destroy==TRUE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Destroying handle %p",
			   handle);
#endif
		
		if(shared==FALSE) {
			_wapi_handle_ops_close_shared (handle);

			mono_mutex_destroy (&_wapi_shared_data->handles[idx].signal_mutex);
			pthread_cond_destroy (&_wapi_shared_data->handles[idx].signal_cond);
			memset (&_wapi_shared_data->handles[idx].u, '\0', sizeof(_wapi_shared_data->handles[idx].u));
		
		}
#ifndef _POSIX_THREAD_PROCESS_SHARED
		else {
			mono_mutex_destroy (&_wapi_shared_data->handles[idx].signal_mutex);
			pthread_cond_destroy (&_wapi_shared_data->handles[idx].signal_cond);
		}
#endif

		_wapi_handle_ops_close_private (handle);
		_wapi_shared_data->handles[idx].type=WAPI_HANDLE_UNUSED;
	}
}

#define HDRSIZE sizeof(struct _WapiScratchHeader)

/*
 * _wapi_handle_scratch_store_internal:
 * @bytes: Allocate no. bytes
 *
 * Like malloc(3) except its for the shared memory segment's scratch
 * part. Memory block returned is zeroed out.
 */
guint32 _wapi_handle_scratch_store_internal (guint32 bytes)
{
	guint32 idx=0, last_idx=0;
	struct _WapiScratchHeader *hdr, *last_hdr;
	gboolean last_was_free=FALSE;
	guchar *storage=&_wapi_shared_data->scratch_base[0];
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": looking for %d bytes of scratch space (%d bytes total)",
		   bytes, _WAPI_SHM_SCRATCH_SIZE);
#endif

	hdr=(struct _WapiScratchHeader *)&storage[0];
	if(hdr->flags==0 && hdr->length==0) {
		/* Need to initialise scratch data */
		hdr->flags |= WAPI_SHM_SCRATCH_FREE;
		hdr->length = _WAPI_SHM_SCRATCH_SIZE - HDRSIZE;
	}
	
	while(idx< _WAPI_SHM_SCRATCH_SIZE) {
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

			/* Put a new header in at the end of the used
			 * space
			 */
			hdr=(struct _WapiScratchHeader *)&storage[idx+bytes];
			hdr->flags |= WAPI_SHM_SCRATCH_FREE;
			hdr->length = old_length-bytes-HDRSIZE;

#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": new header at %d, length %d", idx+bytes, hdr->length);
#endif
			
			/*
			 * It was memset(0..) when free/made so no need to do it here
			 */

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

			/* Don't let the coalescing blow away this block */
			last_was_free=FALSE;
		}
	}
	
	return(0);
}

guint32 _wapi_handle_scratch_store (gconstpointer data, guint32 bytes)
{
	static pthread_mutex_t scratch_mutex=PTHREAD_MUTEX_INITIALIZER;
	guint32 idx;
	
	/* No point storing no data */
	if(bytes==0) {
		return(0);
	}

	/* Align bytes to 32 bits (needed for sparc at least) */
	bytes = (((bytes) + 3) & (~3));

	if(shared==TRUE) {
		WapiHandleRequest scratch;
		WapiHandleResponse scratch_resp;
	
		scratch.type=WapiHandleRequestType_Scratch;
		scratch.u.scratch.length=bytes;
	
		_wapi_daemon_request_response (daemon_sock, &scratch,
					       &scratch_resp);
	
		if(scratch_resp.type==WapiHandleResponseType_Scratch) {
			idx=scratch_resp.u.scratch.idx;
		} else {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": bogus daemon response, type %d",
				   scratch_resp.type);
			g_assert_not_reached ();
		}
	} else {
		pthread_mutex_lock (&scratch_mutex);
		idx=_wapi_handle_scratch_store_internal (bytes);
		pthread_mutex_unlock (&scratch_mutex);
		
		if(idx==0) {
			/* Failed to allocate space */
			return(0);
		}
	}

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": stored [%s] at %d (len %d)",
		   (char *)data, idx, bytes);
#endif
	
	memcpy (&_wapi_shared_data->scratch_base[idx], data, bytes);
	
	return(idx);
}

guint32 _wapi_handle_scratch_store_string_array (gchar **data)
{
	guint32 *stored_strings, count=0, i, idx;
	gchar **strings;
	
	/* No point storing no data */
	if(data==NULL) {
		return(0);
	}

	strings=data;
	while(*strings!=NULL) {
		count++;
		strings++;
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": %d strings to store", count);
#endif
	
	if(count==0) {
		return(0);
	}

	/* stored_strings[0] is the count */
	stored_strings=g_new0 (guint32, count+1);
	stored_strings[0]=count;
	
	strings=data;
	for(i=0; i<count; i++) {
		stored_strings[i+1]=_wapi_handle_scratch_store (strings[i], strlen (strings[i]));
	}

	idx=_wapi_handle_scratch_store (stored_strings,
					sizeof(guint32)*(count+1));
	
	return(idx);
}

guchar *_wapi_handle_scratch_lookup_as_string (guint32 idx)
{
	struct _WapiScratchHeader *hdr;
	guchar *str;
	guchar *storage=&_wapi_shared_data->scratch_base[0];
	
	if(idx < HDRSIZE || idx > _WAPI_SHM_SCRATCH_SIZE) {
		return(NULL);
	}
	
	hdr=(struct _WapiScratchHeader *)&storage[idx - HDRSIZE];
	str=g_malloc0 (hdr->length+1);
	memcpy (str, &storage[idx], hdr->length);

	return(str);
}

gconstpointer _wapi_handle_scratch_lookup (guint32 idx)
{
	guchar *storage=&_wapi_shared_data->scratch_base[0];
	
	if(idx < HDRSIZE || idx > _WAPI_SHM_SCRATCH_SIZE) {
		return(NULL);
	}
	
	return(&storage[idx]);
}

gchar **_wapi_handle_scratch_lookup_string_array (guint32 idx)
{
	gchar **strings;
	const guint32 *stored_strings;
	guint32 count, i;
	
	if(idx < HDRSIZE || idx > _WAPI_SHM_SCRATCH_SIZE) {
		return(NULL);
	}

	stored_strings=_wapi_handle_scratch_lookup (idx);
	if(stored_strings==NULL) {
		return(NULL);
	}
	
	/* stored_strings[0] is the number of strings, the index of
	 * each string follows
	 */
	count=stored_strings[0];
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": looking up an array of %d strings", count);
#endif
	
	/* NULL-terminate the array */
	strings=g_new0 (gchar *, count+1);
	
	for(i=0; i<count; i++) {
		strings[i]=_wapi_handle_scratch_lookup_as_string (stored_strings[i+1]);

#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": string %d is [%s]", i,
			   strings[i]);
#endif
	}
	
	return(strings);
}

/*
 * _wapi_handle_scratch_delete_internal:
 * @idx: Index to free block
 *
 * Like free(3) except its for the shared memory segment's scratch
 * part.
 */
void _wapi_handle_scratch_delete_internal (guint32 idx)
{
	struct _WapiScratchHeader *hdr;
	guchar *storage=&_wapi_shared_data->scratch_base[0];
	
	if(idx < HDRSIZE || idx > _WAPI_SHM_SCRATCH_SIZE) {
		return;
	}
	
	hdr=(struct _WapiScratchHeader *)&storage[idx - HDRSIZE];
	memset (&storage[idx], '\0', hdr->length);
	hdr->flags |= WAPI_SHM_SCRATCH_FREE;
	
	/* We could coalesce forwards here if the next block is also
	 * free, but the _store() function will do that anyway.
	 */
}

void _wapi_handle_scratch_delete (guint32 idx)
{
	if(shared==TRUE) {
		WapiHandleRequest scratch_free;
		WapiHandleResponse scratch_free_resp;
	
		scratch_free.type=WapiHandleRequestType_ScratchFree;
		scratch_free.u.scratch_free.idx=idx;
	
		_wapi_daemon_request_response (daemon_sock, &scratch_free,
					       &scratch_free_resp);
	
		if(scratch_free_resp.type!=WapiHandleResponseType_ScratchFree) {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": bogus daemon response, type %d",
				   scratch_free_resp.type);
			g_assert_not_reached ();
		}
	} else {
		_wapi_handle_scratch_delete_internal (idx);
	}
}

void _wapi_handle_scratch_delete_string_array (guint32 idx)
{
	const guint32 *stored_strings;
	guint32 count, i;
	
	stored_strings=_wapi_handle_scratch_lookup (idx);
	if(stored_strings==NULL) {
		return;
	}
	
	/* stored_strings[0] is the number of strings, the index of
	 * each string follows
	 */
	count=stored_strings[0];
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": deleting an array of %d strings",
		   count);
#endif
	
	for(i=1; i<count; i++) {
		_wapi_handle_scratch_delete (stored_strings[i]);
	}
	
	_wapi_handle_scratch_delete (idx);
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

void _wapi_handle_ops_close_shared (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->close_shared!=NULL) {
		handle_ops[type]->close_shared (handle);
	}
}

void _wapi_handle_ops_close_private (gpointer handle)
{
	guint32 idx=GPOINTER_TO_UINT (handle);
	WapiHandleType type;

	type=_wapi_shared_data->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->close_private!=NULL) {
		handle_ops[type]->close_private (handle);
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
		
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": attempting to lock %p",
			   handles[i]);
#endif

		ret=mono_mutex_trylock (&_wapi_shared_data->handles[idx].signal_mutex);
		if(ret!=0) {
			/* Bummer */
			struct timespec sleepytime;
			
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": attempt failed for %p", handles[i]);
#endif

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
		
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": unlocking handle %p",
			   handles[i]);
#endif

		mono_mutex_unlock (&_wapi_shared_data->handles[idx].signal_mutex);
	}
}

/* Process-shared handles (currently only process and thread handles
 * are allowed, and they only work because once signalled they can't
 * become unsignalled) are waited for by one process and signalled by
 * another.  Without process-shared conditions, the waiting process
 * will block forever.  To get around this, the four handle waiting
 * functions use a short timeout when _POSIX_THREAD_PROCESS_SHARED is
 * not available.  They also return "success" if the fake timeout
 * expired, and let the caller check signal status.
 */
int _wapi_handle_wait_signal (void)
{
#ifdef _POSIX_THREAD_PROCESS_SHARED
	return(mono_cond_wait (&_wapi_shared_data->signal_cond,
			       &_wapi_shared_data->signal_mutex));
#else
	struct timespec fake_timeout;
	int ret;
	
	_wapi_calc_timeout (&fake_timeout, 100);

	ret=mono_cond_timedwait (&_wapi_private_data->signal_cond,
				 &_wapi_private_data->signal_mutex,
				 &fake_timeout);
	if(ret==ETIMEDOUT) {
		ret=0;
	}

	return(ret);
#endif /* _POSIX_THREAD_PROCESS_SHARED */
}

int _wapi_handle_timedwait_signal (struct timespec *timeout)
{
#ifdef _POSIX_THREAD_PROCESS_SHARED
	return(mono_cond_timedwait (&_wapi_shared_data->signal_cond,
				    &_wapi_shared_data->signal_mutex,
				    timeout));
#else
	struct timespec fake_timeout;
	int ret;
	
	_wapi_calc_timeout (&fake_timeout, 100);
	
	if((fake_timeout.tv_sec>timeout->tv_sec) ||
	   (fake_timeout.tv_sec==timeout->tv_sec &&
	    fake_timeout.tv_nsec > timeout->tv_nsec)) {
		/* Real timeout is less than 100ms time */
		ret=mono_cond_timedwait (&_wapi_private_data->signal_cond,
					 &_wapi_private_data->signal_mutex,
					 timeout);
	} else {
		ret=mono_cond_timedwait (&_wapi_private_data->signal_cond,
					 &_wapi_private_data->signal_mutex,
					 &fake_timeout);
		if(ret==ETIMEDOUT) {
			ret=0;
		}
	}
	
	return(ret);
#endif /* _POSIX_THREAD_PROCESS_SHARED */
}

int _wapi_handle_wait_signal_handle (gpointer handle)
{
#ifdef _POSIX_THREAD_PROCESS_SHARED
	guint32 idx=GPOINTER_TO_UINT (handle);
	
	return(mono_cond_wait (&_wapi_shared_data->handles[idx].signal_cond,
			       &_wapi_shared_data->handles[idx].signal_mutex));
#else
	guint32 idx=GPOINTER_TO_UINT (handle);
	struct timespec fake_timeout;
	int ret;
	
	_wapi_calc_timeout (&fake_timeout, 100);
	
	ret=mono_cond_timedwait (&_wapi_shared_data->handles[idx].signal_cond,
				 &_wapi_shared_data->handles[idx].signal_mutex,
				 &fake_timeout);
	if(ret==ETIMEDOUT) {
		ret=0;
	}

	return(ret);
#endif /* _POSIX_THREAD_PROCESS_SHARED */
}

int _wapi_handle_timedwait_signal_handle (gpointer handle,
					  struct timespec *timeout)
{
#ifdef _POSIX_THREAD_PROCESS_SHARED
	guint32 idx=GPOINTER_TO_UINT (handle);
	
	return(mono_cond_timedwait (&_wapi_shared_data->handles[idx].signal_cond,
				    &_wapi_shared_data->handles[idx].signal_mutex,
				    timeout));
#else
	guint32 idx=GPOINTER_TO_UINT (handle);
	struct timespec fake_timeout;
	int ret;
	
	_wapi_calc_timeout (&fake_timeout, 100);
	
	if((fake_timeout.tv_sec>timeout->tv_sec) ||
	   (fake_timeout.tv_sec==timeout->tv_sec &&
	    fake_timeout.tv_nsec > timeout->tv_nsec)) {
		/* Real timeout is less than 100ms time */
		ret=mono_cond_timedwait (&_wapi_shared_data->handles[idx].signal_cond,
					 &_wapi_shared_data->handles[idx].signal_mutex,
					 timeout);
	} else {
		ret=mono_cond_timedwait (&_wapi_shared_data->handles[idx].signal_cond,
					 &_wapi_shared_data->handles[idx].signal_mutex,
					 &fake_timeout);
		if(ret==ETIMEDOUT) {
			ret=0;
		}
	}
	
	return(ret);
#endif /* _POSIX_THREAD_PROCESS_SHARED */
}

gboolean _wapi_handle_process_fork (guint32 cmd, guint32 env, guint32 dir,
				    gboolean inherit, guint32 flags,
				    gpointer stdin_handle,
				    gpointer stdout_handle,
				    gpointer stderr_handle,
				    gpointer *process_handle,
				    gpointer *thread_handle, guint32 *pid,
				    guint32 *tid)
{
	WapiHandleRequest fork_proc;
	WapiHandleResponse fork_proc_resp;
	int in_fd, out_fd, err_fd;
	
	if(shared!=TRUE) {
		return(FALSE);
	}

	fork_proc.type=WapiHandleRequestType_ProcessFork;
	fork_proc.u.process_fork.cmd=cmd;
	fork_proc.u.process_fork.env=env;
	fork_proc.u.process_fork.dir=dir;
	fork_proc.u.process_fork.stdin_handle=GPOINTER_TO_UINT (stdin_handle);
	fork_proc.u.process_fork.stdout_handle=GPOINTER_TO_UINT (stdout_handle);
	fork_proc.u.process_fork.stderr_handle=GPOINTER_TO_UINT (stderr_handle);
	fork_proc.u.process_fork.inherit=inherit;
	fork_proc.u.process_fork.flags=flags;
	
	in_fd=_wapi_file_handle_to_fd (stdin_handle);
	out_fd=_wapi_file_handle_to_fd (stdout_handle);
	err_fd=_wapi_file_handle_to_fd (stderr_handle);

	if(in_fd==-1 || out_fd==-1 || err_fd==-1) {
		/* We were given duff handles */
		/* FIXME: error code */
		return(FALSE);
	}
	
	_wapi_daemon_request_response_with_fds (daemon_sock, &fork_proc,
						&fork_proc_resp, in_fd,
						out_fd, err_fd);
	if(fork_proc_resp.type==WapiHandleResponseType_ProcessFork) {
		*process_handle=GUINT_TO_POINTER (fork_proc_resp.u.process_fork.process_handle);
		*thread_handle=GUINT_TO_POINTER (fork_proc_resp.u.process_fork.thread_handle);
		*pid=fork_proc_resp.u.process_fork.pid;
		*tid=fork_proc_resp.u.process_fork.tid;

		/* If there was an internal error, the handles will be
		 * 0.  If there was an error forking or execing, the
		 * handles will have values, and process_handle's
		 * exec_errno will be set, and the handle will be
		 * signalled immediately.
		 */
		if(process_handle==0 || thread_handle==0) {
			return(FALSE);
		} else {
			return(TRUE);
		}
	} else {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": bogus daemon response, type %d",
			   fork_proc_resp.type);
		g_assert_not_reached ();
	}
	
	return(FALSE);
}
