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
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <sys/mman.h>

#include <mono/os/gc_wrapper.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/daemon-messages.h>

#undef DEBUG
#undef HEAVY_DEBUG /* This will print handle counts on every handle created */

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

struct _WapiHandleShared_list **_wapi_shared_data=NULL;
struct _WapiHandleScratch *_wapi_shared_scratch=NULL;
struct _WapiHandlePrivate_list **_wapi_private_data=NULL;
pthread_mutex_t _wapi_shared_mutex=PTHREAD_MUTEX_INITIALIZER;

/* This holds the length of the _wapi_shared_data and
 * _wapi_private_data arrays, so we know if a segment is off the end
 * of the array, requiring a realloc
 */
guint32 _wapi_shm_mapped_segments;

guint32 _wapi_fd_offset_table_size;
gpointer *_wapi_fd_offset_table=NULL;

static void shared_init (void)
{
	struct sockaddr_un shared_socket_address;
	gboolean tried_once=FALSE;
	int ret;
	int thr_ret;
	
	_wapi_shared_data=g_new0 (struct _WapiHandleShared_list *, 1);
	_wapi_private_data=g_new0 (struct _WapiHandlePrivate_list *, 1);

attach_again:

#ifndef DISABLE_SHARED_HANDLES
	if(getenv ("MONO_DISABLE_SHM"))
#endif
	{
		shared=FALSE;
#ifndef DISABLE_SHARED_HANDLES
	} else {
		/* Ensure that shared==FALSE while _wapi_shm_attach()
		 * calls fork()
		 */
		shared=FALSE;
		
		shared=_wapi_shm_attach (&_wapi_shared_data[0],
					 &_wapi_shared_scratch);
		if(shared==FALSE) {
			g_warning (
				"Failed to attach shared memory! "
				"Falling back to non-shared handles\n"
				"See: http://www.go-mono.com/issues.html#wapi for details");
		}
#endif /* DISABLE_SHARED_HANDLES */
	}
	

	if(shared==TRUE) {
		daemon_sock=socket (PF_UNIX, SOCK_STREAM, 0);
		shared_socket_address.sun_family=AF_UNIX;
		memcpy (shared_socket_address.sun_path,
			_wapi_shared_data[0]->daemon, MONO_SIZEOF_SUNPATH);
		ret=connect (daemon_sock,
			     (struct sockaddr *)&shared_socket_address,
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
		} else {
			_wapi_fd_offset_table_size = _wapi_shared_data[0]->fd_offset_table_size;
			_wapi_fd_offset_table=g_new0 (gpointer, _wapi_fd_offset_table_size);
		}
	}

	if(shared==FALSE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": Using process-private handles");
#endif
		_wapi_shared_data[0]=g_new0 (struct _WapiHandleShared_list, 1);
		_wapi_shared_data[0]->num_segments=1;

		_wapi_shared_scratch=g_new0 (struct _WapiHandleScratch, 1);

		_wapi_fd_offset_table_size=getdtablesize ();
		_wapi_fd_offset_table=g_new0 (gpointer,
					      _wapi_fd_offset_table_size);
	}
	_wapi_private_data[0]=g_new0 (struct _WapiHandlePrivate_list, 1);
	_wapi_shm_mapped_segments=1;

	thr_ret = pthread_mutexattr_init (&mutex_shared_attr);
	g_assert (thr_ret == 0);
	
	thr_ret = pthread_condattr_init (&cond_shared_attr);
	g_assert (thr_ret == 0);
	
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
	thr_ret = pthread_mutexattr_setpshared (&mutex_shared_attr,
						PTHREAD_PROCESS_SHARED);
	g_assert (thr_ret == 0);
	
	thr_ret = pthread_condattr_setpshared (&cond_shared_attr,
					       PTHREAD_PROCESS_SHARED);
	g_assert (thr_ret == 0);
#else
	thr_ret = pthread_cond_init(&_wapi_private_data[0]->signal_cond, NULL);
	g_assert (thr_ret == 0);
	
	thr_ret = mono_mutex_init(&_wapi_private_data[0]->signal_mutex, NULL);
	g_assert (thr_ret == 0);
#endif
}

#ifdef HEAVY_DEBUG
static void
print_handle_count (gint mask)
{
	gint *count, num_handles;
	gint i;
	static const gchar *names [] = {"WAPI_HANDLE_UNUSED",
				  "WAPI_HANDLE_FILE",
				  "WAPI_HANDLE_CONSOLE",
				  "WAPI_HANDLE_THREAD",
				  "WAPI_HANDLE_SEM",
				  "WAPI_HANDLE_MUTEX",
				  "WAPI_HANDLE_EVENT",
				  "WAPI_HANDLE_SOCKET",
				  "WAPI_HANDLE_FIND",
				  "WAPI_HANDLE_PROCESS",
				  "WAPI_HANDLE_PIPE"
				};


	num_handles=_wapi_handle_get_shared_segment (0)->num_segments * _WAPI_HANDLES_PER_SEGMENT;
	count=g_new0 (gint, num_handles);

	for (i = 1; i < num_handles; i++) {
		struct _WapiHandleShared *shared;
		guint32 segment, idx;
		
		_wapi_handle_segment (GUINT_TO_POINTER (i), &segment, &idx);
		_wapi_handle_ensure_mapped (segment);
		
		shared = &_wapi_handle_get_shared_segment (segment)->handles[idx];
		count [shared->type]++;
	}

	for (i = 0; i < num_handles; i++)
		if ((i & mask) == i) /* Always prints the UNUSED count */
			g_print ("%s: %d\n", names [i], count [i]);

	g_free (count);
}
#endif /* HEAVY_DEBUG */

/*
 * _wapi_handle_new_internal:
 * @type: Init handle to this type
 *
 * Search for a free handle and initialize it. Return the handle on
 * success and 0 on failure.
 */
guint32 _wapi_handle_new_internal (WapiHandleType type)
{
	guint32 segment, idx;
	guint32 i, j;
	static guint32 last=1;
	int thr_ret;
	guint32 num_segments = _wapi_handle_get_shared_segment (0)->num_segments;
	
	/* A linear scan should be fast enough.  Start from the last
	 * allocation, assuming that handles are allocated more often
	 * than they're freed. Leave 0 (NULL) as a guard
	 */
#ifdef HEAVY_DEBUG
	print_handle_count (0xFFF);
#endif
again:
	_wapi_handle_segment (GUINT_TO_POINTER (last), &segment, &idx);
	for(i=segment; i < num_segments; i++) {
		if(i!=segment) {
			idx=0;
		}
		
		for(j=idx; j<_WAPI_HANDLES_PER_SEGMENT; j++) {
			struct _WapiHandleShared *shared;
		
			/* Make sure we dont try and assign the
			 * handles that would clash with fds
			 */
			if ((i * _WAPI_HANDLES_PER_SEGMENT + j) < _wapi_fd_offset_table_size) {
				i = _wapi_fd_offset_table_size / _WAPI_HANDLES_PER_SEGMENT;
				j = _wapi_fd_offset_table_size - (i * _WAPI_HANDLES_PER_SEGMENT);
				
				if (i >= num_segments) {
					/* Need to get the caller to
					 * add more shared segments
					 */
					return(0);
				}
				
				continue;
			}
			
			shared=&_wapi_handle_get_shared_segment (i)->handles[j];
		
			if(shared->type==WAPI_HANDLE_UNUSED) {
				last=(_wapi_handle_index (i, j)+1) % (_wapi_handle_get_shared_segment (0)->num_segments * _WAPI_HANDLES_PER_SEGMENT);
				shared->type=type;
				shared->signalled=FALSE;
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
				thr_ret = mono_mutex_init (&shared->signal_mutex, &mutex_shared_attr);
				g_assert (thr_ret == 0);
				
				thr_ret = pthread_cond_init (&shared->signal_cond, &cond_shared_attr);
				g_assert (thr_ret == 0);
#else
				thr_ret = pthread_cond_init(&shared->signal_cond, NULL);
				g_assert (thr_ret == 0);
				
				thr_ret = mono_mutex_init(&shared->signal_mutex, NULL);
				g_assert (thr_ret == 0);
#endif
				
				return(_wapi_handle_index (i, j));
			}
		}
	}

	if(last>1) {
		/* Try again from the beginning */
		last=1;
		goto again;
	}

	/* Will need a new segment.  The caller will sort it out */

	return(0);
}

gpointer _wapi_handle_new (WapiHandleType type)
{
	static mono_once_t shared_init_once = MONO_ONCE_INIT;
	static pthread_mutex_t scan_mutex=PTHREAD_MUTEX_INITIALIZER;
	guint32 handle_idx = 0, idx, segment;
	gpointer handle;
	WapiHandleRequest new={0};
	WapiHandleResponse new_resp={0};
#if HAVE_BOEHM_GC
	gboolean tried_collect=FALSE;
#endif
	int thr_ret;
	
	mono_once (&shared_init_once, shared_init);
	
again:
	if(shared==TRUE) {
		new.type=WapiHandleRequestType_New;
		new.u.new.type=type;
	
		_wapi_daemon_request_response (daemon_sock, &new, &new_resp);
	
		if (new_resp.type==WapiHandleResponseType_New) {
			handle_idx=new_resp.u.new.handle;
		} else {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": bogus daemon response, type %d",
				   new_resp.type);
			g_assert_not_reached ();
		}
	} else {
		pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
				      (void *)&scan_mutex);
		thr_ret = pthread_mutex_lock (&scan_mutex);
		g_assert (thr_ret == 0);
		
		while ((handle_idx = _wapi_handle_new_internal (type)) == 0) {
			/* Try and get a new segment, and have another go */
			segment=_wapi_handle_get_shared_segment (0)->num_segments;
			_wapi_handle_ensure_mapped (segment);
			
			if(_wapi_handle_get_shared_segment (segment)!=NULL) {
				/* Got a new segment */
				_wapi_handle_get_shared_segment (0)->num_segments++;
			} else {
				/* Map failed.  Just return 0 meaning
				 * "out of handles"
				 */
			}
		}
		
		_wapi_handle_segment (GUINT_TO_POINTER (handle_idx), &segment, &idx);
		_wapi_handle_get_shared_segment (segment)->handles[idx].ref++;

		thr_ret = pthread_mutex_unlock (&scan_mutex);
		g_assert (thr_ret == 0);
		pthread_cleanup_pop (0);
	}
		
	/* Make sure we left the space for fd mappings */
	g_assert (handle_idx >= _wapi_fd_offset_table_size);
	
	if(handle_idx==0) {
		g_warning (G_GNUC_PRETTY_FUNCTION ": Ran out of handles!");

#if HAVE_BOEHM_GC
		/* See if we can reclaim some handles by forcing a GC
		 * collection
		 */
		if(tried_collect==FALSE) {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": Seeing if GC collection helps...");
			GC_gcollect (); /* FIXME: we should wait for finalizers to be called */
			tried_collect=TRUE;
			goto again;
		} else {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": didn't help, returning error");
		}
#endif
	
		return(GUINT_TO_POINTER (_WAPI_HANDLE_INVALID));
	}

	_wapi_handle_segment (GUINT_TO_POINTER (handle_idx), &segment, &idx);
	_wapi_handle_ensure_mapped (segment);

	if(_wapi_private_data!=NULL) {
		_wapi_handle_get_private_segment (segment)->handles[idx].type=type;
	}
	
#if !defined(_POSIX_THREAD_PROCESS_SHARED) || _POSIX_THREAD_PROCESS_SHARED == -1
	thr_ret = mono_mutex_init (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex, &mutex_shared_attr);
	g_assert (thr_ret == 0);
	
	thr_ret = pthread_cond_init (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_cond, &cond_shared_attr);
	g_assert (thr_ret == 0);
#endif
	handle=GUINT_TO_POINTER (handle_idx);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Allocated new handle %p", handle);
#endif
	
	return(handle);
}

gboolean _wapi_lookup_handle (gpointer handle, WapiHandleType type,
			      gpointer *shared, gpointer *private)
{
	struct _WapiHandleShared *shared_handle_data;
	struct _WapiHandlePrivate *private_handle_data = NULL;
	guint32 idx;
	guint32 segment;

	g_assert (GPOINTER_TO_UINT (handle) >= _wapi_fd_offset_table_size);
	
	_wapi_handle_segment (handle, &segment, &idx);
	_wapi_handle_ensure_mapped (segment);
	
	shared_handle_data=&_wapi_handle_get_shared_segment (segment)->handles[idx];

	if(shared!=NULL) {
		*shared=&shared_handle_data->u;
	}
	
	if(private!=NULL) {
		private_handle_data=&_wapi_handle_get_private_segment (segment)->handles[idx];

		*private=&private_handle_data->u;
	}

	if(shared_handle_data->type!=type) {
		/* If shared type is UNUSED, see if the private type
		 * matches what we are looking for - this can happen
		 * when the handle is being destroyed and the
		 * close_private function is looking up the private
		 * data
		 */
		if(shared_handle_data->type==WAPI_HANDLE_UNUSED &&
		   (private!=NULL && private_handle_data->type==type)) {
			return(TRUE);
		} else {
			return(FALSE);
		}
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
	guint32 i, segment, idx;

	for(i=1; i<_wapi_handle_get_shared_segment (0)->num_segments * _WAPI_HANDLES_PER_SEGMENT; i++) {
		struct _WapiHandleShared *shared;
		
		_wapi_handle_segment (GUINT_TO_POINTER (i), &segment, &idx);
		_wapi_handle_ensure_mapped (segment);
		
		shared=&_wapi_handle_get_shared_segment (segment)->handles[idx];
		
		if(shared->type==type) {
			if(check (GUINT_TO_POINTER (i), user_data)==TRUE) {
				break;
			}
		}
	}

	if(i==_wapi_handle_get_shared_segment (0)->num_segments * _WAPI_HANDLES_PER_SEGMENT) {
		return(GUINT_TO_POINTER (0));
	}
	
	if(shared!=NULL) {
		shared_handle_data=&_wapi_handle_get_shared_segment (segment)->handles[idx];

		*shared=&shared_handle_data->u;
	}
	
	if(private!=NULL) {
		private_handle_data=&_wapi_handle_get_private_segment (segment)->handles[idx];

		*private=&private_handle_data->u;
	}
	
	return(GUINT_TO_POINTER (i));
}

gpointer _wapi_search_handle_namespace (WapiHandleType type,
					gchar *utf8_name, gpointer *shared,
					gpointer *private)
{
	struct _WapiHandleShared *shared_handle_data;
	struct _WapiHandlePrivate *private_handle_data;
	guint32 i, segment, idx;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": Lookup for handle named [%s] type %d", utf8_name, type);
#endif

	for(i=1; i<_wapi_handle_get_shared_segment (0)->num_segments * _WAPI_HANDLES_PER_SEGMENT; i++) {
		struct _WapiHandleShared *shared;
		
		_wapi_handle_segment (GUINT_TO_POINTER (i), &segment, &idx);
		_wapi_handle_ensure_mapped (segment);
		
		shared=&_wapi_handle_get_shared_segment (segment)->handles[idx];
		
		/* Check mutex, event, semaphore, timer, job and file-mapping
		 * object names.  So far only mutex is implemented.
		 */
		if(_WAPI_SHARED_NAMESPACE (shared->type)) {
			gchar *lookup_name;
			WapiSharedNamespace *sharedns;
			
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": found a shared namespace handle at 0x%x (type %d)", i, shared->type);
#endif

			shared_handle_data=&_wapi_handle_get_shared_segment (segment)->handles[idx];
			sharedns=(WapiSharedNamespace *)&shared_handle_data->u;
			
			
			if(sharedns->name) {
				lookup_name=_wapi_handle_scratch_lookup (
					sharedns->name);
			} else {
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION
					   ": handle 0x%x is unnamed", i);
#endif
				continue;
			}

			if(lookup_name==NULL) {
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION
					   ": couldn't find handle 0x%x name",
					   i);
#endif
				continue;
			}

#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": name is [%s]",
				   lookup_name);
#endif

			if(strcmp (lookup_name, utf8_name)==0) {
				if(shared->type!=type) {
					/* Its the wrong type, so fail now */
#ifdef DEBUG
					g_message (G_GNUC_PRETTY_FUNCTION ": handle 0x%x matches name but is wrong type: %d", i, shared->type);
#endif
					return(_WAPI_HANDLE_INVALID);
				} else {
					/* fall through so we can fill
					 * in the data
					 */
#ifdef DEBUG
					g_message (G_GNUC_PRETTY_FUNCTION ": handle 0x%x matches name and type", i);
#endif
					break;
				}
			}
		}
	}

	if(i==_wapi_handle_get_shared_segment (0)->num_segments * _WAPI_HANDLES_PER_SEGMENT) {
		return(GUINT_TO_POINTER (0));
	}
	
	if(shared!=NULL) {
		shared_handle_data=&_wapi_handle_get_shared_segment (segment)->handles[idx];

		*shared=&shared_handle_data->u;
	}
	
	if(private!=NULL) {
		private_handle_data=&_wapi_handle_get_private_segment (segment)->handles[idx];

		*private=&private_handle_data->u;
	}
	
	return(GUINT_TO_POINTER (i));
}

void _wapi_handle_ref (gpointer handle)
{
	g_assert (GPOINTER_TO_UINT (handle) >= _wapi_fd_offset_table_size);
	
	if(shared==TRUE) {
		WapiHandleRequest req={0};
		WapiHandleResponse resp={0};
	
		req.type=WapiHandleRequestType_Open;
		req.u.open.handle=GPOINTER_TO_UINT (handle);
	
		_wapi_daemon_request_response (daemon_sock, &req, &resp);
		if(resp.type!=WapiHandleResponseType_Open) {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": bogus daemon response, type %d",
				   resp.type);
			g_assert_not_reached ();
		}
	} else {
		guint32 idx, segment;

		_wapi_handle_segment (handle, &segment, &idx);
		
		_wapi_handle_get_shared_segment (segment)->handles[idx].ref++;
	
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": handle %p ref now %d",
			   handle,
			   _wapi_handle_get_shared_segment (segment)->handles[idx].ref);
#endif
	}
}

/* The handle must not be locked on entry to this function */
void _wapi_handle_unref (gpointer handle)
{
	guint32 idx, segment;
	gboolean destroy = FALSE;
	int thr_ret;
	
	g_assert (GPOINTER_TO_UINT (handle) >= _wapi_fd_offset_table_size);
	
	_wapi_handle_segment (handle, &segment, &idx);
	
	if(shared==TRUE) {
		WapiHandleRequest req={0};
		WapiHandleResponse resp={0};
	
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
		_wapi_handle_get_shared_segment (segment)->handles[idx].ref--;
	
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": handle %p ref now %d", handle, _wapi_handle_get_shared_segment (segment)->handles[idx].ref);
#endif

		/* Possible race condition here if another thread refs
		 * the handle between here and setting the type to
		 * UNUSED.  I could lock a mutex, but I'm not sure
		 * that allowing a handle reference to reach 0 isn't
		 * an application bug anyway.
		 */
		destroy=(_wapi_handle_get_shared_segment (segment)->handles[idx].ref==0);
	}

	if(destroy==TRUE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Destroying handle %p",
			   handle);
#endif
		
		if(shared==FALSE) {
			_wapi_handle_ops_close_shared (handle);

			memset (&_wapi_handle_get_shared_segment (segment)->handles[idx].u, '\0', sizeof(_wapi_handle_get_shared_segment (segment)->handles[idx].u));
		}

		_wapi_handle_ops_close_private (handle);
		_wapi_handle_get_shared_segment (segment)->handles[idx].type=WAPI_HANDLE_UNUSED;
		
		/* Destroy the mutex and cond var.  We hope nobody
		 * tried to grab them between the handle unlock and
		 * now, but pthreads doesn't have a
		 * "unlock_and_destroy" atomic function.
		 */
		thr_ret = mono_mutex_destroy (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex);
		g_assert (thr_ret == 0);
			
		thr_ret = pthread_cond_destroy (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_cond);
		g_assert (thr_ret == 0);
	}
}

#define HDRSIZE sizeof(struct _WapiScratchHeader)

static pthread_mutex_t _wapi_scratch_mutex=PTHREAD_MUTEX_INITIALIZER;

/* _wapi_scratch_mutex must be held when this function is called in
 * the non-shared case
 */
static void _wapi_handle_scratch_expand (void)
{
	guint32 old_len, new_len;
		
	old_len=sizeof(struct _WapiHandleScratch) +
		_wapi_shared_scratch->data_len;
	new_len=old_len+_WAPI_SHM_SCRATCH_SIZE;
		
	if(_wapi_shared_scratch->is_shared==TRUE) {
		/* expand via mmap() */
		_wapi_shared_scratch=_wapi_shm_file_expand (_wapi_shared_scratch, WAPI_SHM_SCRATCH, 0, old_len, new_len);
	} else {
		_wapi_shared_scratch=_wapi_g_renew0 (_wapi_shared_scratch, old_len, new_len);
	}
	_wapi_shared_scratch->data_len+=_WAPI_SHM_SCRATCH_SIZE;
}

/* _wapi_scratch_mutex must be held when this function is called in
 * the non-shared case
 */
static guint32 _wapi_handle_scratch_locate_space (guint32 bytes)
{
	guint32 idx=0, last_idx=0;
	struct _WapiScratchHeader *hdr, *last_hdr = NULL;
	gboolean last_was_free=FALSE;
	guchar *storage=_wapi_shared_scratch->scratch_data;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": looking for %d bytes of scratch space (%d bytes total)",
		   bytes, _wapi_shared_scratch->data_len);
#endif
	
	while(idx< _wapi_shared_scratch->data_len) {
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

			/* But remember where the last block started */
			last_idx=idx;
		}
	}

	/* Not enough free space.  last_idx points to the last block.
	 * If it's free, just tack on more space and update the
	 * length.  If it's allocated, it must have fit right into the
	 * available space, so add more space and add a new header
	 * after this block.
	 */
	_wapi_handle_scratch_expand ();
	storage=_wapi_shared_scratch->scratch_data;
	
	hdr=(struct _WapiScratchHeader *)&storage[last_idx];
	if(hdr->flags & WAPI_SHM_SCRATCH_FREE) {
		hdr->length+=_WAPI_SHM_SCRATCH_SIZE;
	} else {
		idx=(hdr->length+HDRSIZE);
		hdr=(struct _WapiScratchHeader *)&storage[idx];
		hdr->flags |= WAPI_SHM_SCRATCH_FREE;
		hdr->length = _WAPI_SHM_SCRATCH_SIZE-HDRSIZE;
	}

	/* The caller will try again */
	return(0);
}

/*
 * _wapi_handle_scratch_store_internal:
 * @bytes: Allocate no. bytes
 *
 * Like malloc(3) except its for the shared memory segment's scratch
 * part. Memory block returned is zeroed out.
 */
guint32 _wapi_handle_scratch_store_internal (guint32 bytes, gboolean *remap)
{
	guchar *storage;
	guint32 idx;
	struct _WapiScratchHeader *hdr;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": storing %d bytes", bytes);
#endif
	
	*remap=FALSE;
	
	if(_wapi_shared_scratch->data_len==0) {
		/* Need to expand the data array for the first use */
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": setting up scratch space");
#endif

		_wapi_handle_scratch_expand ();
		*remap=TRUE;
	}

	storage=_wapi_shared_scratch->scratch_data;
	hdr=(struct _WapiScratchHeader *)&storage[0];
	if(hdr->flags==0 && hdr->length==0) {
		/* Need to initialise scratch data */
		hdr->flags |= WAPI_SHM_SCRATCH_FREE;
		hdr->length = _wapi_shared_scratch->data_len - HDRSIZE;
	}

	idx=_wapi_handle_scratch_locate_space (bytes);
	if(idx==0) {
		/* Some more space will have been allocated, so try again */
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": trying again");
#endif

		idx=_wapi_handle_scratch_locate_space (bytes);
		*remap=TRUE;
	}
	
	return(idx);
}

guint32 _wapi_handle_scratch_store (gconstpointer data, guint32 bytes)
{
	guint32 idx = 0, store_bytes;
	gboolean remap;
	int thr_ret;
	guint32 ret = 0;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": storing %d bytes", bytes);
#endif

	/* No point storing no data */
	if(bytes==0) {
		return(0);
	}

	/* Align bytes to 32 bits (needed for sparc at least) */
	store_bytes = (((bytes) + 3) & (~3));

	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&_wapi_scratch_mutex);
	thr_ret = pthread_mutex_lock (&_wapi_scratch_mutex);
	g_assert (thr_ret == 0);
	
	if(shared==TRUE) {
		WapiHandleRequest scratch={0};
		WapiHandleResponse scratch_resp={0};
		guint32 old_len=sizeof(struct _WapiHandleScratch) +
			_wapi_shared_scratch->data_len;
		
		scratch.type=WapiHandleRequestType_Scratch;
		scratch.u.scratch.length=store_bytes;
	
		_wapi_daemon_request_response (daemon_sock, &scratch,
					       &scratch_resp);
	
		if(scratch_resp.type==WapiHandleResponseType_Scratch) {
			idx=scratch_resp.u.scratch.idx;
			remap=scratch_resp.u.scratch.remap;
		} else {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": bogus daemon response, type %d",
				   scratch_resp.type);
			g_assert_not_reached ();
		}
	
		if(remap==TRUE) {
			munmap (_wapi_shared_scratch, old_len);
			_wapi_shared_scratch=_wapi_shm_file_map (WAPI_SHM_SCRATCH, 0, NULL, NULL);
		}
	} else {
		idx=_wapi_handle_scratch_store_internal (store_bytes, &remap);
		if(idx==0) {
			/* Failed to allocate space */
			goto cleanup;
		}
	}
	ret = idx;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": stored [%s] at %d (len %d, aligned len %d)",
		   (char *)data, idx, bytes, store_bytes);
#endif
	
	memcpy (&_wapi_shared_scratch->scratch_data[idx], data, bytes);

cleanup:
	thr_ret = pthread_mutex_unlock (&_wapi_scratch_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	return(ret);
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

gpointer _wapi_handle_scratch_lookup (guint32 idx)
{
	struct _WapiScratchHeader *hdr;
	gpointer ret;
	guchar *storage;
	int thr_ret;
	
	if(idx < HDRSIZE || idx > _wapi_shared_scratch->data_len) {
		return(NULL);
	}

	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&_wapi_scratch_mutex);
	thr_ret = pthread_mutex_lock (&_wapi_scratch_mutex);
	g_assert (thr_ret == 0);
	
	storage=_wapi_shared_scratch->scratch_data;
	
	hdr=(struct _WapiScratchHeader *)&storage[idx - HDRSIZE];
	ret=g_malloc0 (hdr->length+1);
	memcpy (ret, &storage[idx], hdr->length);

	thr_ret = pthread_mutex_unlock (&_wapi_scratch_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);

	return(ret);
}

gchar **_wapi_handle_scratch_lookup_string_array (guint32 idx)
{
	gchar **strings;
	guint32 *stored_strings;
	guint32 count, i;
	
	if(idx < HDRSIZE || idx > _wapi_shared_scratch->data_len) {
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
		strings[i]=_wapi_handle_scratch_lookup (stored_strings[i+1]);

#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": string %d is [%s]", i,
			   strings[i]);
#endif
	}

	g_free (stored_strings);
	
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
	guchar *storage;
	int thr_ret;
	
	if(idx < HDRSIZE || idx > _wapi_shared_scratch->data_len) {
		return;
	}

	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&_wapi_scratch_mutex);
	thr_ret = pthread_mutex_lock (&_wapi_scratch_mutex);
	g_assert (thr_ret == 0);
	
	storage=_wapi_shared_scratch->scratch_data;
	
	hdr=(struct _WapiScratchHeader *)&storage[idx - HDRSIZE];
	memset (&storage[idx], '\0', hdr->length);
	hdr->flags |= WAPI_SHM_SCRATCH_FREE;
	
	/* We could coalesce forwards here if the next block is also
	 * free, but the _store() function will do that anyway.
	 */

	thr_ret = pthread_mutex_unlock (&_wapi_scratch_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
}

void _wapi_handle_scratch_delete (guint32 idx)
{
	if(shared==TRUE) {
		WapiHandleRequest scratch_free={0};
		WapiHandleResponse scratch_free_resp={0};
	
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
	guint32 *stored_strings;
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

	g_free (stored_strings);
}

void _wapi_handle_register_capabilities (WapiHandleType type,
					 WapiHandleCapability caps)
{
	handle_caps[type]=caps;
}

gboolean _wapi_handle_test_capabilities (gpointer handle,
					 WapiHandleCapability caps)
{
	guint32 idx, segment;
	WapiHandleType type;

	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}
	
	_wapi_handle_segment (handle, &segment, &idx);
	
	type=_wapi_handle_get_shared_segment (segment)->handles[idx].type;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": testing 0x%x against 0x%x (%d)",
		   handle_caps[type], caps, handle_caps[type] & caps);
#endif
	
	return((handle_caps[type] & caps)!=0);
}

void _wapi_handle_ops_close_shared (gpointer handle)
{
	guint32 idx, segment;
	WapiHandleType type;

	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}

	_wapi_handle_segment (handle, &segment, &idx);
	
	type=_wapi_handle_get_shared_segment (segment)->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->close_shared!=NULL) {
		handle_ops[type]->close_shared (handle);
	}
}

void _wapi_handle_ops_close_private (gpointer handle)
{
	guint32 idx, segment;
	WapiHandleType type;

	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}

	_wapi_handle_segment (handle, &segment, &idx);

	type=_wapi_handle_get_shared_segment (segment)->handles[idx].type;

	/* When a handle in the process of being destroyed the shared
	 * type has already been set to UNUSED
	 */
	if(type==WAPI_HANDLE_UNUSED && _wapi_private_data!=NULL) {
		type=_wapi_handle_get_private_segment (segment)->handles[idx].type;
	}

	if(handle_ops[type]!=NULL && handle_ops[type]->close_private!=NULL) {
		handle_ops[type]->close_private (handle);
	}
}

void _wapi_handle_ops_signal (gpointer handle)
{
	guint32 idx, segment;
	WapiHandleType type;

	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}

	_wapi_handle_segment (handle, &segment, &idx);

	type=_wapi_handle_get_shared_segment (segment)->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->signal!=NULL) {
		handle_ops[type]->signal (handle);
	}
}

void _wapi_handle_ops_own (gpointer handle)
{
	guint32 idx, segment;
	WapiHandleType type;

	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}

	_wapi_handle_segment (handle, &segment, &idx);
	
	type=_wapi_handle_get_shared_segment (segment)->handles[idx].type;

	if(handle_ops[type]!=NULL && handle_ops[type]->own_handle!=NULL) {
		handle_ops[type]->own_handle (handle);
	}
}

gboolean _wapi_handle_ops_isowned (gpointer handle)
{
	guint32 idx, segment;
	WapiHandleType type;

	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}

	_wapi_handle_segment (handle, &segment, &idx);
	
	type=_wapi_handle_get_shared_segment (segment)->handles[idx].type;

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
	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}

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
	int thr_ret;
	
	/* Lock all the handles, with backoff */
again:
	for(i=0; i<numhandles; i++) {
		guint32 idx, segment;
		gpointer handle = handles[i];
		
		if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
			handle = _wapi_handle_fd_offset_to_handle (handle);
		}
		
		_wapi_handle_segment (handle, &segment, &idx);
		
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": attempting to lock %p",
			   handle);
#endif

		ret=mono_mutex_trylock (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex);
		if(ret!=0) {
			/* Bummer */
			struct timespec sleepytime;
			
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": attempt failed for %p", handle);
#endif

			while(i--) {
				_wapi_handle_segment (handle, &segment, &idx);
				thr_ret = mono_mutex_unlock (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex);
				g_assert (thr_ret == 0);
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
		guint32 idx, segment;
		gpointer handle = handles[i];
		
		if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
			handle = _wapi_handle_fd_offset_to_handle (handle);
		}
		
		_wapi_handle_ref (handle);
		
		_wapi_handle_segment (handle, &segment, &idx);
		
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Checking handle %p",
			   handle);
#endif

		if(((_wapi_handle_test_capabilities (handle, WAPI_HANDLE_CAP_OWN)==TRUE) &&
		    (_wapi_handle_ops_isowned (handle)==TRUE)) ||
		   (_wapi_handle_get_shared_segment (segment)->handles[idx].signalled==TRUE)) {
			count++;
			
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Handle %p signalled", handle);
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
	int thr_ret;
	
	for(i=0; i<numhandles; i++) {
		guint32 idx, segment;
		gpointer handle = handles[i];
		
		if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
			handle = _wapi_handle_fd_offset_to_handle (handle);
		}
		
		_wapi_handle_segment (handle, &segment, &idx);

#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": unlocking handle %p",
			   handle);
#endif

		thr_ret = mono_mutex_unlock (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex);
		g_assert (thr_ret == 0);

		_wapi_handle_unref (handle);
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
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
	return(mono_cond_wait (&_wapi_handle_get_shared_segment (0)->signal_cond,
			       &_wapi_handle_get_shared_segment (0)->signal_mutex));
#else
	struct timespec fake_timeout;
	int ret;
	
	_wapi_calc_timeout (&fake_timeout, 100);

	ret=mono_cond_timedwait (&_wapi_handle_get_private_segment (0)->signal_cond,
				 &_wapi_handle_get_private_segment (0)->signal_mutex,
				 &fake_timeout);
	if(ret==ETIMEDOUT) {
		ret=0;
	}

	return(ret);
#endif /* _POSIX_THREAD_PROCESS_SHARED */
}

int _wapi_handle_timedwait_signal (struct timespec *timeout)
{
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
	return(mono_cond_timedwait (&_wapi_handle_get_shared_segment (0)->signal_cond,
				    &_wapi_handle_get_shared_segment (0)->signal_mutex,
				    timeout));
#else
	struct timespec fake_timeout;
	int ret;
	
	_wapi_calc_timeout (&fake_timeout, 100);
	
	if((fake_timeout.tv_sec>timeout->tv_sec) ||
	   (fake_timeout.tv_sec==timeout->tv_sec &&
	    fake_timeout.tv_nsec > timeout->tv_nsec)) {
		/* Real timeout is less than 100ms time */
		ret=mono_cond_timedwait (&_wapi_handle_get_private_segment (0)->signal_cond,
					 &_wapi_handle_get_private_segment (0)->signal_mutex,
					 timeout);
	} else {
		ret=mono_cond_timedwait (&_wapi_handle_get_private_segment (0)->signal_cond,
					 &_wapi_handle_get_private_segment (0)->signal_mutex,
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
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
	guint32 idx, segment;

	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}

	_wapi_handle_segment (handle, &segment, &idx);
	
	return(mono_cond_wait (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_cond,
			       &_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex));
#else
	guint32 idx, segment;
	struct timespec fake_timeout;
	int ret;
	
	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}

	_wapi_handle_segment (handle, &segment, &idx);
	_wapi_calc_timeout (&fake_timeout, 100);
	
	ret=mono_cond_timedwait (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_cond,
				 &_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex,
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
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
	guint32 idx, segment;

	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}

	_wapi_handle_segment (handle, &segment, &idx);
	
	return(mono_cond_timedwait (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_cond,
				    &_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex,
				    timeout));
#else
	guint32 idx, segment;
	struct timespec fake_timeout;
	int ret;
	
	if (GPOINTER_TO_UINT (handle) < _wapi_fd_offset_table_size) {
		handle = _wapi_handle_fd_offset_to_handle (handle);
	}

	_wapi_handle_segment (handle, &segment, &idx);
	_wapi_calc_timeout (&fake_timeout, 100);
	
	if((fake_timeout.tv_sec>timeout->tv_sec) ||
	   (fake_timeout.tv_sec==timeout->tv_sec &&
	    fake_timeout.tv_nsec > timeout->tv_nsec)) {
		/* Real timeout is less than 100ms time */
		ret=mono_cond_timedwait (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_cond,
					 &_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex,
					 timeout);
	} else {
		ret=mono_cond_timedwait (&_wapi_handle_get_shared_segment (segment)->handles[idx].signal_cond,
					 &_wapi_handle_get_shared_segment (segment)->handles[idx].signal_mutex,
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
	WapiHandleRequest fork_proc={0};
	WapiHandleResponse fork_proc_resp={0};
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

gboolean
_wapi_handle_process_kill (pid_t process, guint32 signo, gint *errnum)
{
	WapiHandleRequest killproc = {0};
	WapiHandleResponse killprocresp = {0};
	gint result;
	
	if (shared != TRUE) {
		if (errnum) *errnum = EINVAL;
		return FALSE;
	}

	killproc.type = WapiHandleRequestType_ProcessKill;
	killproc.u.process_kill.pid = process;
	killproc.u.process_kill.signo = signo;
	
	_wapi_daemon_request_response (daemon_sock, &killproc, &killprocresp);

	if (killprocresp.type != WapiHandleResponseType_ProcessKill) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": bogus daemon response, type %d",
			   killprocresp.type);
		g_assert_not_reached ();
	}

	result = killprocresp.u.process_kill.err;
	if (result != 0 && errnum != NULL)
		*errnum = (result == FALSE) ? result : 0;
	
	return (result == 0);
}

gboolean _wapi_handle_get_or_set_share (dev_t device, ino_t inode,
					guint32 new_sharemode,
					guint32 new_access,
					guint32 *old_sharemode,
					guint32 *old_access)
{
	WapiHandleRequest req = {0};
	WapiHandleResponse resp = {0};
	
	if(shared != TRUE) {
		/* No daemon means we don't know if a file is sharable.
		 * We're running in our own little world if this is
		 * the case, so there's no point in pretending that
		 * the file isn't sharable.
		 */
		return(FALSE);
	}
	
	req.type = WapiHandleRequestType_GetOrSetShare;
	req.u.get_or_set_share.device = device;
	req.u.get_or_set_share.inode = inode;
	req.u.get_or_set_share.new_sharemode = new_sharemode;
	req.u.get_or_set_share.new_access = new_access;
	
	_wapi_daemon_request_response (daemon_sock, &req, &resp);
	if (resp.type != WapiHandleResponseType_GetOrSetShare) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": bogus daemon response, type %d", resp.type);
		g_assert_not_reached ();
	}
	
	*old_sharemode = resp.u.get_or_set_share.sharemode;
	*old_access = resp.u.get_or_set_share.access;

	return(resp.u.get_or_set_share.exists);
}

void _wapi_handle_set_share (dev_t device, ino_t inode, guint32 sharemode,
			     guint32 access)
{
	WapiHandleRequest req = {0};
	WapiHandleResponse resp = {0};
	
	if(shared != TRUE) {
		/* No daemon, so there's no one else to tell about
		 * file sharing.
		 */
		return;
	}

	req.type = WapiHandleRequestType_SetShare;
	req.u.set_share.device = device;
	req.u.set_share.inode = inode;
	req.u.set_share.sharemode = sharemode;
	req.u.set_share.access = access;
	
	_wapi_daemon_request_response (daemon_sock, &req, &resp);
	if (resp.type != WapiHandleResponseType_SetShare) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": bogus daemon response, type %d", resp.type);
		g_assert_not_reached ();
	}
}
