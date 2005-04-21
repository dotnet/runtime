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
#include <dirent.h>
#include <sys/stat.h>

#include <mono/os/gc_wrapper.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/collection.h>

#undef DEBUG
#undef DEBUG_REFS

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
	&_wapi_namedmutex_ops,
};

static void _wapi_shared_details (gpointer handle_info);

static void (*handle_details[WAPI_HANDLE_COUNT])(gpointer) = {
	NULL,
	_wapi_file_details,
	_wapi_console_details,
	_wapi_shared_details,	/* thread */
	_wapi_sem_details,
	_wapi_mutex_details,
	_wapi_event_details,
	NULL,			/* Nothing useful to see in a socket handle */
	NULL,			/* Nothing useful to see in a find handle */
	_wapi_shared_details,	/* process */
	_wapi_pipe_details,
	_wapi_shared_details,	/* namedmutex */
};

const char *_wapi_handle_typename[] = {
	"Unused",
	"File",
	"Console",
	"Thread",
	"Sem",
	"Mutex",
	"Event",
	"Socket",
	"Find",
	"Process",
	"Pipe",
	"N.Mutex",
	"Error!!"
};

/*
 * We can hold _WAPI_PRIVATE_MAX_SLOTS * _WAPI_HANDLE_INITIAL_COUNT handles.
 * If 4M handles are not enough... Oh, well... we will crash.
 */
#define SLOT_INDEX(x)	(x / _WAPI_HANDLE_INITIAL_COUNT)
#define SLOT_OFFSET(x)	(x % _WAPI_HANDLE_INITIAL_COUNT)

struct _WapiHandleUnshared *_wapi_private_handles [_WAPI_PRIVATE_MAX_SLOTS];
static guint32 _wapi_private_handle_count = 0;

struct _WapiHandleSharedLayout *_wapi_shared_layout = NULL;
struct _WapiFileShareLayout *_wapi_fileshare_layout = NULL;

guint32 _wapi_fd_reserve;

mono_mutex_t _wapi_global_signal_mutex;
pthread_cond_t _wapi_global_signal_cond;

static mono_mutex_t scan_mutex = MONO_MUTEX_INITIALIZER;

static mono_once_t shared_init_once = MONO_ONCE_INIT;
static void shared_init (void)
{
	int thr_ret;
	int idx = 0;
	
	_wapi_fd_reserve = getdtablesize();

	do {
		_wapi_private_handles [idx++] = g_new0 (struct _WapiHandleUnshared,
							_WAPI_HANDLE_INITIAL_COUNT);

		_wapi_private_handle_count += _WAPI_HANDLE_INITIAL_COUNT;
	} while(_wapi_fd_reserve > _wapi_private_handle_count);
	
	_wapi_shared_layout = _wapi_shm_attach ();
	g_assert (_wapi_shared_layout != NULL);
	
	_wapi_fileshare_layout = _wapi_fileshare_shm_attach ();
	g_assert (_wapi_fileshare_layout != NULL);
	
	_wapi_collection_init ();
	
	thr_ret = pthread_cond_init(&_wapi_global_signal_cond, NULL);
	g_assert (thr_ret == 0);
	
	thr_ret = mono_mutex_init(&_wapi_global_signal_mutex, NULL);
	g_assert (thr_ret == 0);
}

static void _wapi_handle_init_shared_metadata (struct _WapiHandleSharedMetadata *meta)
{
	meta->timestamp = (guint32)(time (NULL) & 0xFFFFFFFF);
	meta->signalled = FALSE;
	meta->checking = 0;
}

static void _wapi_handle_init_shared (struct _WapiHandleShared *handle,
				      WapiHandleType type,
				      gpointer handle_specific)
{
	handle->type = type;
	handle->stale = FALSE;
	
	if (handle_specific != NULL) {
		memcpy (&handle->u, handle_specific, sizeof (handle->u));
	}
}

static void _wapi_handle_init (struct _WapiHandleUnshared *handle,
			       WapiHandleType type, gpointer handle_specific)
{
	int thr_ret;
	
	handle->type = type;
	handle->signalled = FALSE;
	handle->ref = 1;
	
	if (!_WAPI_SHARED_HANDLE(type)) {
		thr_ret = pthread_cond_init (&handle->signal_cond, NULL);
		g_assert (thr_ret == 0);
				
		thr_ret = mono_mutex_init (&handle->signal_mutex, NULL);
		g_assert (thr_ret == 0);

		if (handle_specific != NULL) {
			memcpy (&handle->u, handle_specific,
				sizeof (handle->u));
		}
	}
}

static guint32 _wapi_handle_new_shared_offset (guint32 offset)
{
	guint32 i;
	static guint32 last = 1;
	
again:
	/* FIXME: expandable array */
	/* leave a few slots at the end so that there's always space
	 * to move a handle.  (We leave the space in the offset table
	 * too, so we don't have to keep track of inter-segment
	 * offsets.)
	 */
	for(i = last; i <_WAPI_HANDLE_INITIAL_COUNT - _WAPI_HEADROOM; i++) {
		struct _WapiHandleSharedMetadata *meta = &_wapi_shared_layout->metadata[i];
		
		if(meta->offset == 0) {
			if (InterlockedCompareExchange (&meta->offset, offset,
							0) == 0) {
				last = i + 1;
			
				_wapi_handle_init_shared_metadata (meta);
				return(i);
			} else {
				/* Someone else beat us to it, just
				 * continue looking
				 */
			}
		}
	}

	if(last > 1) {
		/* Try again from the beginning */
		last = 1;
		goto again;
	}

	/* Will need to expand the array.  The caller will sort it out */

	return(0);
}

static guint32 _wapi_handle_new_shared (WapiHandleType type,
					gpointer handle_specific)
{
	guint32 offset;
	static guint32 last = 1;
	
	/* The shared memory holds an offset to the real data, so we
	 * can update the handle RCU-style without taking a lock.
	 * This function just allocates the next available data slot,
	 * use _wapi_handle_new_shared_offset to get the offset entry.
	 */

	/* Leave the first slot empty as a guard */
again:
	/* FIXME: expandable array */
	/* Leave a few slots at the end so that there's always space
	 * to move a handle
	 */
	for(offset = last; offset <_WAPI_HANDLE_INITIAL_COUNT - _WAPI_HEADROOM;
	    offset++) {
		struct _WapiHandleShared *handle = &_wapi_shared_layout->handles[offset];
		
		if(handle->type == WAPI_HANDLE_UNUSED) {
			if (InterlockedCompareExchange ((gint32 *)&handle->type, type, WAPI_HANDLE_UNUSED) == WAPI_HANDLE_UNUSED) {
				last = offset + 1;
			
				_wapi_handle_init_shared (handle, type,
							  handle_specific);
				return(offset);
			} else {
				/* Someone else beat us to it, just
				 * continue looking
				 */
			}
		}
	}

	if(last > 1) {
		/* Try again from the beginning */
		last = 1;
		goto again;
	}

	/* Will need to expand the array.  The caller will sort it out */

	return(0);
}

/*
 * _wapi_handle_new_internal:
 * @type: Init handle to this type
 *
 * Search for a free handle and initialize it. Return the handle on
 * success and 0 on failure.  This is only called from
 * _wapi_handle_new, and scan_mutex must be held.
 */
static guint32 _wapi_handle_new_internal (WapiHandleType type,
					  gpointer handle_specific)
{
	guint32 i, k, count;
	static guint32 last = 0;
	gboolean retry = FALSE;
	
	/* A linear scan should be fast enough.  Start from the last
	 * allocation, assuming that handles are allocated more often
	 * than they're freed. Leave the space reserved for file
	 * descriptors
	 */
	
	if (last < _wapi_fd_reserve) {
		last = _wapi_fd_reserve;
	} else {
		retry = TRUE;
	}

again:
	count = last;
	for(i = SLOT_INDEX (count); _wapi_private_handles [i] != NULL; i++) {
		for (k = SLOT_OFFSET (count); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
			struct _WapiHandleUnshared *handle = &_wapi_private_handles [i][k];

			if(handle->type == WAPI_HANDLE_UNUSED) {
				last = count + 1;
			
				_wapi_handle_init (handle, type, handle_specific);
				return (count);
			}
			count++;
		}
	}

	if(retry && last > _wapi_fd_reserve) {
		/* Try again from the beginning */
		last = _wapi_fd_reserve;
		goto again;
	}

	/* Will need to expand the array.  The caller will sort it out */

	return(0);
}

gpointer _wapi_handle_new (WapiHandleType type, gpointer handle_specific)
{
	guint32 handle_idx = 0;
	gpointer handle;
	int thr_ret;
	
	mono_once (&shared_init_once, shared_init);
	
#ifdef DEBUG
	g_message ("%s: Creating new handle of type %s", __func__,
		   _wapi_handle_typename[type]);
#endif

	g_assert(!_WAPI_FD_HANDLE(type));
	
	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&scan_mutex);
	thr_ret = mono_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);
		
	while ((handle_idx = _wapi_handle_new_internal (type, handle_specific)) == 0) {
		/* Try and expand the array, and have another go */
		int idx = SLOT_INDEX (_wapi_private_handle_count);
		_wapi_private_handles [idx] = g_new0 (struct _WapiHandleUnshared,
						_WAPI_HANDLE_INITIAL_COUNT);

		_wapi_private_handle_count += _WAPI_HANDLE_INITIAL_COUNT;
	}
		
	thr_ret = mono_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
		
	/* Make sure we left the space for fd mappings */
	g_assert (handle_idx >= _wapi_fd_reserve);
	
	handle = GUINT_TO_POINTER (handle_idx);

#ifdef DEBUG
	g_message ("%s: Allocated new handle %p", __func__, handle);
#endif
	
	if (_WAPI_SHARED_HANDLE(type)) {
		/* Add the shared section too */
		guint32 offset, ref;
		
		offset = _wapi_handle_new_shared (type, handle_specific);
		if (offset == 0) {
			_wapi_handle_collect ();
			offset = _wapi_handle_new_shared (type,
							  handle_specific);
			/* FIXME: grow the arrays */
			return (_WAPI_HANDLE_INVALID);
		}
		
		ref = _wapi_handle_new_shared_offset (offset);
		if (ref == 0) {
			_wapi_handle_collect ();
			ref = _wapi_handle_new_shared_offset (offset);
			/* FIXME: grow the arrays */
			return (_WAPI_HANDLE_INVALID);
		}
		
		_WAPI_PRIVATE_HANDLES(handle_idx).u.shared.offset = ref;
#ifdef DEBUG
		g_message ("%s: New shared handle at offset 0x%x", __func__,
			   ref);
#endif
	}
	
	return(handle);
}

gpointer _wapi_handle_new_for_existing_ns (WapiHandleType type,
					   gpointer handle_specific,
					   guint32 offset)
{
	guint32 handle_idx = 0;
	gpointer handle;
	int thr_ret, i, k;
	
	mono_once (&shared_init_once, shared_init);
	
#ifdef DEBUG
	g_message ("%s: Creating new handle of type %s", __func__,
		   _wapi_handle_typename[type]);
#endif

	g_assert(!_WAPI_FD_HANDLE(type));
	g_assert(_WAPI_SHARED_HANDLE(type));
	g_assert(offset != 0);
		
	for (i = SLOT_INDEX (0); _wapi_private_handles [i] != NULL; i++) {
		for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
			struct _WapiHandleUnshared *handle_data = &_wapi_private_handles [i][k];
		
			if (handle_data->type == type &&
			    handle_data->u.shared.offset == offset) {
				handle = GUINT_TO_POINTER (i * _WAPI_HANDLE_INITIAL_COUNT + k);
				_wapi_handle_ref (handle);

#ifdef DEBUG
				g_message ("%s: Returning old handle %p referencing 0x%x", __func__, handle, offset);
#endif
				return (handle);
			}
		}
	}

	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&scan_mutex);
	thr_ret = mono_mutex_lock (&scan_mutex);
	g_assert (thr_ret == 0);
	
	while ((handle_idx = _wapi_handle_new_internal (type, handle_specific)) == 0) {
		/* Try and expand the array, and have another go */
		int idx = SLOT_INDEX (_wapi_private_handle_count);
		_wapi_private_handles [idx] = g_new0 (struct _WapiHandleUnshared,
						_WAPI_HANDLE_INITIAL_COUNT);

		_wapi_private_handle_count += _WAPI_HANDLE_INITIAL_COUNT;
	}
		
	thr_ret = mono_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
		
	/* Make sure we left the space for fd mappings */
	g_assert (handle_idx >= _wapi_fd_reserve);
	
	handle = GUINT_TO_POINTER (handle_idx);
		
	_WAPI_PRIVATE_HANDLES(handle_idx).u.shared.offset = offset;

#ifdef DEBUG
	g_message ("%s: Allocated new handle %p referencing 0x%x", __func__,
		   handle, offset);
#endif

	return(handle);
}

gpointer _wapi_handle_new_fd (WapiHandleType type, int fd,
			      gpointer handle_specific)
{
	struct _WapiHandleUnshared *handle;
	
	mono_once (&shared_init_once, shared_init);
	
#ifdef DEBUG
	g_message ("%s: Creating new handle of type %s", __func__,
		   _wapi_handle_typename[type]);
#endif
	
	g_assert(_WAPI_FD_HANDLE(type));
	g_assert(!_WAPI_SHARED_HANDLE(type));
	
	if (fd >= _wapi_fd_reserve) {
#ifdef DEBUG
		g_message ("%s: fd %d is too big", __func__, fd);
#endif

		return(GUINT_TO_POINTER (_WAPI_HANDLE_INVALID));
	}

	handle = &_WAPI_PRIVATE_HANDLES(fd);
	
	if (handle->type != WAPI_HANDLE_UNUSED) {
#ifdef DEBUG
		g_message ("%s: fd %d is already in use!", __func__, fd);
#endif
		/* FIXME: clean up this handle?  We can't do anything
		 * with the fd, cos thats the new one
		 */
	}

#ifdef DEBUG
	g_message ("%s: Assigning new fd handle %d", __func__, fd);
#endif

	_wapi_handle_init (handle, type, handle_specific);

	return(GUINT_TO_POINTER(fd));
}

gpointer _wapi_handle_new_from_offset (WapiHandleType type, int offset)
{
	guint32 handle_idx = 0;
	gpointer handle;
	int thr_ret;
	struct _WapiHandle_shared_ref *ref;
	struct _WapiHandleUnshared *handle_data;
	
	mono_once (&shared_init_once, shared_init);
	
#ifdef DEBUG
	g_message ("%s: Creating new handle of type %s to offset %d", __func__,
		   _wapi_handle_typename[type], offset);
#endif

	g_assert(_WAPI_SHARED_HANDLE(type));

	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&scan_mutex);
	thr_ret = mono_mutex_lock (&scan_mutex);
	g_assert(thr_ret == 0);
	
	while ((handle_idx = _wapi_handle_new_internal (type, NULL)) == 0) {
		/* Try and expand the array, and have another go */
		int idx = SLOT_INDEX (_wapi_private_handle_count);
		_wapi_private_handles [idx] = g_new0 (struct _WapiHandleUnshared,
						_WAPI_HANDLE_INITIAL_COUNT);

		_wapi_private_handle_count += _WAPI_HANDLE_INITIAL_COUNT;
	}

	thr_ret = mono_mutex_unlock (&scan_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
				
	/* Make sure we left the space for fd mappings */
	g_assert (handle_idx >= _wapi_fd_reserve);

	handle_data = &_WAPI_PRIVATE_HANDLES(handle_idx);
	ref = &handle_data->u.shared;
	ref->offset = offset;

	handle = GUINT_TO_POINTER (handle_idx);
			
#ifdef DEBUG
	g_message ("%s: Allocated new handle %p", __func__, handle);
#endif

	return (handle);
}

gboolean _wapi_lookup_handle (gpointer handle, WapiHandleType type,
			      gpointer *handle_specific)
{
	struct _WapiHandleUnshared *handle_data;
	guint32 handle_idx = GPOINTER_TO_UINT(handle);

	handle_data = &_WAPI_PRIVATE_HANDLES(handle_idx);
	
	if (handle_data->type != type) {
		return(FALSE);
	}

	if (handle_specific == NULL) {
		return(FALSE);
	}
	
	if (_WAPI_SHARED_HANDLE(type)) {
		struct _WapiHandle_shared_ref *ref;
		struct _WapiHandleShared *shared_handle_data;
		struct _WapiHandleSharedMetadata *shared_meta;
		guint32 offset;
			
		/* Unsafe, because we don't want the handle to vanish
		 * while we're checking it
		 */
		_WAPI_HANDLE_COLLECTION_UNSAFE;
		
		do {
			ref = &handle_data->u.shared;
			shared_meta = &_wapi_shared_layout->metadata[ref->offset];
			offset = shared_meta->offset;
			shared_handle_data = &_wapi_shared_layout->handles[offset];
		
			g_assert(shared_handle_data->type == type);

			*handle_specific = &shared_handle_data->u;
		} while (offset != shared_meta->offset);

		_WAPI_HANDLE_COLLECTION_SAFE;
	} else {
		*handle_specific = &handle_data->u;
	}
	
	return(TRUE);
}

gboolean _wapi_copy_handle (gpointer handle, WapiHandleType type,
			    struct _WapiHandleShared *handle_specific)
{
	struct _WapiHandleUnshared *handle_data;
	guint32 handle_idx = GPOINTER_TO_UINT(handle);
	struct _WapiHandle_shared_ref *ref;
	struct _WapiHandleShared *shared_handle_data;
	struct _WapiHandleSharedMetadata *shared_meta;
	guint32 offset;
	
	g_assert(_WAPI_SHARED_HANDLE(type));

#ifdef DEBUG
	g_message ("%s: copying handle %p type %s", __func__, handle,
		   _wapi_handle_typename[type]);
#endif

	handle_data = &_WAPI_PRIVATE_HANDLES(handle_idx);
	
	if(handle_data->type != type) {
#ifdef DEBUG
		g_message ("%s: incorrect type, %p has type %s", __func__,
			   handle, _wapi_handle_typename[handle_data->type]);
#endif

		return(FALSE);
	}

	if(handle_specific == NULL) {
#ifdef DEBUG
		g_message ("%s: Nowhere to store data", __func__);
#endif

		return(FALSE);
	}
	
	do {
		ref = &handle_data->u.shared;
		shared_meta = &_wapi_shared_layout->metadata[ref->offset];
		offset = shared_meta->offset;
		shared_handle_data = &_wapi_shared_layout->handles[offset];
		
		g_assert(shared_handle_data->type == type);
		
		memcpy(handle_specific, shared_handle_data,
		       sizeof(struct _WapiHandleShared));
	} while (offset != shared_meta->offset);
	
#ifdef DEBUG
	g_message ("%s: OK", __func__);
#endif

	return(TRUE);
}

gboolean _wapi_replace_handle (gpointer handle, WapiHandleType type,
			       struct _WapiHandleShared *handle_specific)
{
	struct _WapiHandleShared *shared_handle_data;
	struct _WapiHandleSharedMetadata *shared_meta;
	guint32 handle_idx = GPOINTER_TO_UINT(handle);
	guint32 old_off, new_off, ref;
	
#ifdef DEBUG
	g_message ("%s: Replacing handle %p of type %s", __func__, handle,
		   _wapi_handle_typename[type]);
#endif

	g_assert(_WAPI_SHARED_HANDLE(type));
	g_assert(_WAPI_PRIVATE_HANDLES(handle_idx).type == type);
	
	ref = _WAPI_PRIVATE_HANDLES(handle_idx).u.shared.offset;
	shared_meta = &_wapi_shared_layout->metadata[ref];
	
	do {
		old_off = shared_meta->offset;
		new_off = _wapi_handle_new_shared (type, handle_specific);
		if (new_off == 0) {
			_wapi_handle_collect ();
			new_off = _wapi_handle_new_shared (type, 
							   handle_specific);
			/* FIXME: grow the arrays */
			return (FALSE);
		}
		
		shared_handle_data = &_wapi_shared_layout->handles[new_off];

		memcpy (shared_handle_data, handle_specific,
			sizeof(struct _WapiHandleShared));

		/* An entry can't become fresh again (its going to be
		 * collected eventually), so no need for atomic ops
		 * here.
		 */
		_wapi_shared_layout->handles[old_off].stale = TRUE;
	} while(InterlockedCompareExchange (&shared_meta->offset, new_off,
					    old_off) != old_off);

#ifdef DEBUG
	g_message ("%s: handle at 0x%x is now found at 0x%x", __func__, ref,
		   new_off);
#endif

	return (TRUE);
}

/* This will only find shared handles that have already been opened by
 * this process.  To look up shared handles by name, use
 * _wapi_search_handle_namespace
 */
gpointer _wapi_search_handle (WapiHandleType type,
			      gboolean (*check)(gpointer test, gpointer user),
			      gpointer user_data,
			      gpointer *handle_specific)
{
	struct _WapiHandleUnshared *handle_data = NULL;
	gpointer ret = NULL;
	guint32 i, k;
	gboolean found = FALSE;


	for(i = SLOT_INDEX (0); !found && _wapi_private_handles [i] != NULL; i++) {
		for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
			handle_data = &_wapi_private_handles [i][k];
		
			if(handle_data->type == type) {
				ret = GUINT_TO_POINTER (i * _WAPI_HANDLE_INITIAL_COUNT + k);
				if(check (ret, user_data) == TRUE) {
					found = TRUE;
					break;
				}
			}
		}
	}

	if (!found) {
		goto done;
	}
	
	if(handle_specific != NULL) {
		if (_WAPI_SHARED_HANDLE(type)) {
			struct _WapiHandle_shared_ref *ref ;
			struct _WapiHandleShared *shared_handle_data;
			struct _WapiHandleSharedMetadata *shared_meta;
			guint32 offset, now;
			
			/* Unsafe, because we don't want the handle to
			 * vanish while we're checking it
			 */
			_WAPI_HANDLE_COLLECTION_UNSAFE;

			do {
				ref = &handle_data->u.shared;
				shared_meta = &_wapi_shared_layout->metadata[ref->offset];
				offset = shared_meta->offset;
				shared_handle_data = &_wapi_shared_layout->handles[offset];
			
				g_assert(shared_handle_data->type == type);
			
				*handle_specific = &shared_handle_data->u;
			} while (offset != shared_meta->offset);

			/* Make sure this handle doesn't vanish in the
			 * next collection
			 */
			now = (guint32)(time (NULL) & 0xFFFFFFFF);
			InterlockedExchange (&shared_meta->timestamp, now);

			_WAPI_HANDLE_COLLECTION_SAFE;
		} else {
			*handle_specific = &handle_data->u;
		}
	}

done:
	return(ret);
}

/* This signature makes it easier to use in pthread cleanup handlers */
int _wapi_namespace_timestamp_release (gpointer nowptr)
{
	guint32 now = GPOINTER_TO_UINT(nowptr);
	
	return (_wapi_timestamp_release (&_wapi_shared_layout->namespace_check,
					 now));
}

int _wapi_namespace_timestamp (guint32 now)
{
	int ret;
	
	do {
		ret = _wapi_timestamp_exclusion (&_wapi_shared_layout->namespace_check, now);
		/* sleep for a bit */
		if (ret == EBUSY) {
			_wapi_handle_spin (100);
		}
	} while (ret == EBUSY);
	
	return(ret);
}

/* Returns the offset of the metadata array, or -1 on error, or 0 for
 * not found (0 is not a valid offset)
 */
gint32 _wapi_search_handle_namespace (WapiHandleType type,
				      gchar *utf8_name)
{
	struct _WapiHandleShared *shared_handle_data;
	struct _WapiHandleSharedMetadata *shared_meta;
	guint32 i;
	gint32 ret = 0;
	
	g_assert(_WAPI_SHARED_HANDLE(type));
	g_assert(_wapi_shared_layout->namespace_check != 0);
	
#ifdef DEBUG
	g_message ("%s: Lookup for handle named [%s] type %s", __func__,
		   utf8_name, _wapi_handle_typename[type]);
#endif

	_WAPI_HANDLE_COLLECTION_UNSAFE;
	
	for(i = 1; i < _WAPI_HANDLE_INITIAL_COUNT; i++) {
		WapiSharedNamespace *sharedns;
		
		shared_meta = &_wapi_shared_layout->metadata[i];
		shared_handle_data = &_wapi_shared_layout->handles[shared_meta->offset];

		/* Check mutex, event, semaphore, timer, job and file-mapping
		 * object names.  So far only mutex is implemented.
		 */
		if (!_WAPI_SHARED_NAMESPACE (shared_handle_data->type)) {
			continue;
		}

#ifdef DEBUG
		g_message ("%s: found a shared namespace handle at 0x%x (type %s)", __func__, i, _wapi_handle_typename[shared_handle_data->type]);
#endif

		sharedns=(WapiSharedNamespace *)&shared_handle_data->u;
			
#ifdef DEBUG
		g_message ("%s: name is [%s]", __func__, sharedns->name);
#endif

		if (strcmp (sharedns->name, utf8_name) == 0) {
			if (shared_handle_data->type != type) {
				/* Its the wrong type, so fail now */
#ifdef DEBUG
				g_message ("%s: handle 0x%x matches name but is wrong type: %s", __func__, i, _wapi_handle_typename[shared_handle_data->type]);
#endif
				ret = -1;
				goto done;
			} else {
#ifdef DEBUG
				g_message ("%s: handle 0x%x matches name and type", __func__, i);
#endif
				ret = i;
				goto done;
			}
		}
	}

done:
	_WAPI_HANDLE_COLLECTION_SAFE;
	
	return(ret);
}

void _wapi_handle_ref (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	guint32 now = (guint32)(time (NULL) & 0xFFFFFFFF);
	struct _WapiHandleUnshared *handle_data = &_WAPI_PRIVATE_HANDLES(idx);
	
	InterlockedIncrement (&handle_data->ref);

	/* It's possible for processes to exit before getting around
	 * to updating timestamps in the collection thread, so if a
	 * shared handle is reffed do the timestamp here as well just
	 * to make sure.
	 */
	if (_WAPI_SHARED_HANDLE(handle_data->type)) {
		struct _WapiHandleSharedMetadata *shared_meta = &_wapi_shared_layout->metadata[handle_data->u.shared.offset];
		
		InterlockedExchange (&shared_meta->timestamp, now);
	}
	
#ifdef DEBUG_REFS
	g_message ("%s: handle %p ref now %d", __func__, handle,
		   _WAPI_PRIVATE_HANDLES(idx).ref);
#endif
}

/* The handle must not be locked on entry to this function */
void _wapi_handle_unref (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	gboolean destroy = FALSE;
	int thr_ret;

	/* Possible race condition here if another thread refs the
	 * handle between here and setting the type to UNUSED.  I
	 * could lock a mutex, but I'm not sure that allowing a handle
	 * reference to reach 0 isn't an application bug anyway.
	 */
	destroy = (InterlockedDecrement (&_WAPI_PRIVATE_HANDLES(idx).ref) ==0);
	
#ifdef DEBUG_REFS
	g_message ("%s: handle %p ref now %d (destroy %s)", __func__, handle,
		   _WAPI_PRIVATE_HANDLES(idx).ref, destroy?"TRUE":"FALSE");
#endif
	
	if(destroy==TRUE) {
		WapiHandleType type = _WAPI_PRIVATE_HANDLES(idx).type;

#ifdef DEBUG
		g_message ("%s: Destroying handle %p", __func__, handle);
#endif
		
		_wapi_handle_ops_close (handle);

		memset (&_WAPI_PRIVATE_HANDLES(idx).u, '\0',
			sizeof(_WAPI_PRIVATE_HANDLES(idx).u));

		_WAPI_PRIVATE_HANDLES(idx).type = WAPI_HANDLE_UNUSED;
		
		if (!_WAPI_SHARED_HANDLE(type)) {
			/* Destroy the mutex and cond var.  We hope nobody
			 * tried to grab them between the handle unlock and
			 * now, but pthreads doesn't have a
			 * "unlock_and_destroy" atomic function.
			 */
			thr_ret = mono_mutex_destroy (&_WAPI_PRIVATE_HANDLES(idx).signal_mutex);
			g_assert (thr_ret == 0);
				
			thr_ret = pthread_cond_destroy (&_WAPI_PRIVATE_HANDLES(idx).signal_cond);
			g_assert (thr_ret == 0);
		}

		/* The garbage collector will take care of shared data
		 * if this is a shared handle
		 */
	}
}

void _wapi_handle_register_capabilities (WapiHandleType type,
					 WapiHandleCapability caps)
{
	handle_caps[type] = caps;
}

gboolean _wapi_handle_test_capabilities (gpointer handle,
					 WapiHandleCapability caps)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;

	type = _WAPI_PRIVATE_HANDLES(idx).type;

#ifdef DEBUG
	g_message ("%s: testing 0x%x against 0x%x (%d)", __func__,
		   handle_caps[type], caps, handle_caps[type] & caps);
#endif
	
	return((handle_caps[type] & caps) != 0);
}

void _wapi_handle_ops_close (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;

	type = _WAPI_PRIVATE_HANDLES(idx).type;

	if (handle_ops[type] != NULL &&
	    handle_ops[type]->close != NULL) {
		handle_ops[type]->close (handle);
	}
}

void _wapi_handle_ops_signal (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;

	type = _WAPI_PRIVATE_HANDLES(idx).type;

	if (handle_ops[type] != NULL && handle_ops[type]->signal != NULL) {
		handle_ops[type]->signal (handle);
	}
}

gboolean _wapi_handle_ops_own (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;
	
	type = _WAPI_PRIVATE_HANDLES(idx).type;

	if (handle_ops[type] != NULL && handle_ops[type]->own_handle != NULL) {
		return(handle_ops[type]->own_handle (handle));
	} else {
		return(FALSE);
	}
}

gboolean _wapi_handle_ops_isowned (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;

	type = _WAPI_PRIVATE_HANDLES(idx).type;

	if (handle_ops[type] != NULL && handle_ops[type]->is_owned != NULL) {
		return(handle_ops[type]->is_owned (handle));
	} else {
		return(FALSE);
	}
}

guint32 _wapi_handle_ops_special_wait (gpointer handle, guint32 timeout)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	WapiHandleType type;
	
	type = _WAPI_PRIVATE_HANDLES(idx).type;
	
	if (handle_ops[type] != NULL &&
	    handle_ops[type]->special_wait != NULL) {
		return(handle_ops[type]->special_wait (handle, timeout));
	} else {
		return(WAIT_FAILED);
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
					       guint32 *lowest,
					       guint32 *now)
{
	guint32 count, i, iter=0;
	gboolean ret;
	int thr_ret;
	WapiHandleType type;
	
	/* Lock all the handles, with backoff */
again:
	for(i=0; i<numhandles; i++) {
		gpointer handle = handles[i];
		guint32 idx = GPOINTER_TO_UINT(handle);

#ifdef DEBUG
		g_message ("%s: attempting to lock %p", __func__, handle);
#endif

		*now = (guint32)(time(NULL) & 0xFFFFFFFF);
		type = _WAPI_PRIVATE_HANDLES(idx).type;

		if (_WAPI_SHARED_HANDLE(type)) {
			thr_ret = _wapi_handle_shared_trylock_handle (handle,
								      *now);
		} else {
			thr_ret = _wapi_handle_trylock_handle (handle);
		}
		
		if (thr_ret != 0) {
			/* Bummer */
			
#ifdef DEBUG
			g_message ("%s: attempt failed for %p: %s", __func__,
				   handle, strerror (thr_ret));
#endif

			while (i--) {
				handle = handles[i];
				idx = GPOINTER_TO_UINT(handle);

				if (_WAPI_SHARED_HANDLE(type)) {
					thr_ret = _wapi_handle_shared_unlock_handle (handle, *now);
				} else {
					thr_ret = _wapi_handle_unlock_handle (handle);
				}
				
				g_assert (thr_ret == 0);
			}

			/* If iter ever reaches 100 the nanosleep will
			 * return EINVAL immediately, but we have a
			 * design flaw if that happens.
			 */
			iter++;
			if(iter==100) {
				g_warning ("%s: iteration overflow!",
					   __func__);
				iter=1;
			}
			
#ifdef DEBUG
			g_message ("%s: Backing off for %d ms", __func__,
				   iter*10);
#endif
			_wapi_handle_spin (10 * iter);
			
			goto again;
		}
	}
	
#ifdef DEBUG
	g_message ("%s: Locked all handles", __func__);
#endif

	count=0;
	*lowest=numhandles;
	
	for(i=0; i<numhandles; i++) {
		gpointer handle = handles[i];
		guint32 idx = GPOINTER_TO_UINT(handle);
		
		type = _WAPI_PRIVATE_HANDLES(idx).type;

		_wapi_handle_ref (handle);
		
#ifdef DEBUG
		g_message ("%s: Checking handle %p", __func__, handle);
#endif

		if(((_wapi_handle_test_capabilities (handle, WAPI_HANDLE_CAP_OWN)==TRUE) &&
		    (_wapi_handle_ops_isowned (handle) == TRUE)) ||
		   (_WAPI_SHARED_HANDLE(type) &&
		    WAPI_SHARED_HANDLE_METADATA(handle).signalled == TRUE) ||
		   (!_WAPI_SHARED_HANDLE(type) &&
		    _WAPI_PRIVATE_HANDLES(idx).signalled == TRUE)) {
			count++;
			
#ifdef DEBUG
			g_message ("%s: Handle %p signalled", __func__,
				   handle);
#endif
			if(*lowest>i) {
				*lowest=i;
			}
		}
	}
	
#ifdef DEBUG
	g_message ("%s: %d event handles signalled", __func__, count);
#endif

	if ((waitall == TRUE && count == numhandles) ||
	    (waitall == FALSE && count > 0)) {
		ret=TRUE;
	} else {
		ret=FALSE;
	}
	
#ifdef DEBUG
	g_message ("%s: Returning %d", __func__, ret);
#endif

	*retcount=count;
	
	return(ret);
}

void _wapi_handle_unlock_handles (guint32 numhandles, gpointer *handles,
				  guint32 now)
{
	guint32 i;
	int thr_ret;
	
	for(i=0; i<numhandles; i++) {
		gpointer handle = handles[i];
		guint32 idx = GPOINTER_TO_UINT(handle);
		WapiHandleType type = _WAPI_PRIVATE_HANDLES(idx).type;
		
#ifdef DEBUG
		g_message ("%s: unlocking handle %p", __func__, handle);
#endif

		if (_WAPI_SHARED_HANDLE(type)) {
			thr_ret = _wapi_handle_shared_unlock_handle (handle,
								     now);
		} else {
			thr_ret = _wapi_handle_unlock_handle (handle);
		}
		g_assert (thr_ret == 0);
	}
}

int _wapi_handle_wait_signal (void)
{
	return(mono_cond_wait (&_wapi_global_signal_cond,
			       &_wapi_global_signal_mutex));
}

int _wapi_handle_timedwait_signal (struct timespec *timeout)
{
	return(mono_cond_timedwait (&_wapi_global_signal_cond,
				    &_wapi_global_signal_mutex,
				    timeout));
}

int _wapi_handle_wait_signal_poll_share (void)
{
	struct timespec fake_timeout;
	int ret;
	
#ifdef DEBUG
	g_message ("%s: poll private and shared handles", __func__);
#endif

	_wapi_calc_timeout (&fake_timeout, 100);
	
	ret = mono_cond_timedwait (&_wapi_global_signal_cond,
				   &_wapi_global_signal_mutex, &fake_timeout);
	
	if (ret == ETIMEDOUT) {
		/* This will cause the waiting thread to check signal
		 * status for all handles
		 */
#ifdef DEBUG
		g_message ("%s: poll timed out, returning success", __func__);
#endif

		return (0);
	} else {
		/* This will be 0 indicating a private handle was
		 * signalled, or an error
		 */
#ifdef DEBUG
		g_message ("%s: returning: %d", __func__, ret);
#endif

		return (ret);
	}
}

int _wapi_handle_timedwait_signal_poll_share (struct timespec *timeout)
{
	struct timespec fake_timeout;
	guint32 signal_count = _wapi_shared_layout->signal_count;
	int ret;
	
#ifdef DEBUG
	g_message ("%s: poll private and shared handles", __func__);
#endif
	
	do {
		_wapi_calc_timeout (&fake_timeout, 100);
	
		if ((fake_timeout.tv_sec > timeout->tv_sec) ||
		    (fake_timeout.tv_sec == timeout->tv_sec &&
		     fake_timeout.tv_nsec > timeout->tv_nsec)) {
			/* Real timeout is less than 100ms time */

#ifdef DEBUG
			g_message ("%s: last few ms", __func__);
#endif

			ret = mono_cond_timedwait (&_wapi_global_signal_cond,
						   &_wapi_global_signal_mutex,
						   timeout);
			/* If this times out, it will compare the
			 * shared signal counter and then if that
			 * hasn't increased will fall out of the
			 * do-while loop.
			 */
			if (ret != ETIMEDOUT) {
				/* Either a private handle was
				 * signalled, or an error.
				 */
#ifdef DEBUG
				g_message ("%s: returning: %d", __func__, ret);
#endif
				return (ret);
			}
		} else {
			ret = mono_cond_timedwait (&_wapi_global_signal_cond,
						   &_wapi_global_signal_mutex,
						   &fake_timeout);

			/* Mask the fake timeout, this will cause
			 * another poll if the shared counter hasn't
			 * changed
			 */
			if (ret == ETIMEDOUT) {
				ret = 0;
			} else {
				/* Either a private handle was
				 * signalled, or an error
				 */
#ifdef DEBUG
				g_message ("%s: returning: %d", __func__, ret);
#endif
				return (ret);
			}
		}

		/* No private handle was signalled, so check the
		 * shared signal counter
		 */
		if (signal_count != _wapi_shared_layout->signal_count) {
#ifdef DEBUG
				g_message ("%s: A shared handle was signalled",
					   __func__);
#endif
			return (0);
		}
	} while (ret != ETIMEDOUT);

#ifdef DEBUG
	g_message ("%s: returning ETIMEDOUT", __func__);
#endif

	return (ret);
}

int _wapi_handle_wait_signal_handle (gpointer handle)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	
#ifdef DEBUG
	g_message ("%s: waiting for %p", __func__, handle);
#endif
	
	if (_WAPI_SHARED_HANDLE (_wapi_handle_type (handle))) {
		while(1) {
			if (WAPI_SHARED_HANDLE_METADATA(handle).signalled == TRUE) {
				return (0);
			}
			
			_wapi_handle_spin (100);
		}
	} else {
		return(mono_cond_wait (&_WAPI_PRIVATE_HANDLES(idx).signal_cond,
				       &_WAPI_PRIVATE_HANDLES(idx).signal_mutex));
	}
}

int _wapi_handle_timedwait_signal_handle (gpointer handle,
					  struct timespec *timeout)
{
	guint32 idx = GPOINTER_TO_UINT(handle);
	
#ifdef DEBUG
	g_message ("%s: waiting for %p (type %s)", __func__, handle,
		   _wapi_handle_typename[_wapi_handle_type (handle)]);
#endif
	
	if (_WAPI_SHARED_HANDLE (_wapi_handle_type (handle))) {
		struct timespec fake_timeout;

		while (1) {
			if (WAPI_SHARED_HANDLE_METADATA(handle).signalled == TRUE) {
				return (0);
			}
		
			_wapi_calc_timeout (&fake_timeout, 100);
		
			if ((fake_timeout.tv_sec > timeout->tv_sec) ||
			    (fake_timeout.tv_sec == timeout->tv_sec &&
			     fake_timeout.tv_nsec > timeout->tv_nsec)) {
				/* FIXME: Real timeout is less than
				 * 100ms time, but is it really worth
				 * calculating to the exact ms?
				 */
				_wapi_handle_spin (100);
				
				if (WAPI_SHARED_HANDLE_METADATA(handle).signalled == TRUE) {
					return (0);
				} else {
					return (ETIMEDOUT);
				}
			} else {
				_wapi_handle_spin (100);
			}
		}
	} else {
		return(mono_cond_timedwait (&_WAPI_PRIVATE_HANDLES(idx).signal_cond, &_WAPI_PRIVATE_HANDLES(idx).signal_mutex, timeout));
	}
}

gboolean _wapi_handle_get_or_set_share (dev_t device, ino_t inode,
					guint32 new_sharemode,
					guint32 new_access,
					guint32 *old_sharemode,
					guint32 *old_access,
					struct _WapiFileShare **share_info)
{
	struct _WapiFileShare *file_share;
	guint32 now = (guint32)(time(NULL) & 0xFFFFFFFF);
	int thr_ret, i, first_unused = -1;
	gboolean exists = FALSE;
	
	/* Marking this as COLLECTION_UNSAFE prevents entries from
	 * expiring under us as we search
	 */
	_WAPI_HANDLE_COLLECTION_UNSAFE;
	
	/* Prevent new entries racing with us */
	do {
		now = (guint32)(time(NULL) & 0xFFFFFFFF);

		thr_ret = _wapi_timestamp_exclusion (&_wapi_fileshare_layout->share_check, now);
		if (thr_ret == EBUSY) {
			_wapi_handle_spin (100);
		}
	} while (thr_ret == EBUSY);
	g_assert (thr_ret == 0);
	
	/* If a linear scan gets too slow we'll have to fit a hash
	 * table onto the shared mem backing store
	 */
	*share_info = NULL;
	for (i = 0; i <= _wapi_fileshare_layout->hwm; i++) {
		file_share = &_wapi_fileshare_layout->share_info[i];

		/* Make a note of an unused slot, in case we need to
		 * store share info
		 */
		if (first_unused == -1 && file_share->handle_refs == 0) {
			first_unused = i;
			continue;
		}
		
		if (file_share->handle_refs == 0) {
			continue;
		}
		
		if (file_share->device == device &&
		    file_share->inode == inode) {
			*old_sharemode = file_share->sharemode;
			*old_access = file_share->access;
			*share_info = file_share;
			
			/* Increment the reference count while we
			 * still have sole access to the shared area.
			 * This makes the increment atomic wrt
			 * collections
			 */
			InterlockedIncrement (&file_share->handle_refs);
			
			exists = TRUE;
			break;
		}
	}
	
	if (!exists) {
		if (i == _WAPI_FILESHARE_SIZE && first_unused == -1) {
			/* No more space */
		} else {
			if (first_unused == -1) {
				file_share = &_wapi_fileshare_layout->share_info[++i];
				_wapi_fileshare_layout->hwm = i;
			} else {
				file_share = &_wapi_fileshare_layout->share_info[first_unused];
			}
			
			file_share->device = device;
			file_share->inode = inode;
			file_share->sharemode = new_sharemode;
			file_share->access = new_access;
			file_share->handle_refs = 1;
			*share_info = file_share;
		}
	}

	if (*share_info != NULL) {
		InterlockedExchange (&(*share_info)->timestamp, now);
	}
	
	thr_ret = _wapi_timestamp_release (&_wapi_fileshare_layout->share_check, now);

	_WAPI_HANDLE_COLLECTION_SAFE;

	return(exists);
}

/* Scan /proc/<pids>/fd/ for open file descriptors to the file in
 * question.  If there are none, reset the share info.
 *
 * This implementation is Linux-specific; legacy systems will have to
 * implement their own ways of finding out if a particular file is
 * open by a process.
 */
void _wapi_handle_check_share (struct _WapiFileShare *share_info, int fd)
{
	DIR *proc_dir;
	struct dirent *proc_entry;
	gboolean found = FALSE;
	pid_t self = getpid();
	int pid;
	guint32 now = (guint32)(time(NULL) & 0xFFFFFFFF);
	int thr_ret;
	
	proc_dir = opendir ("/proc");
	if (proc_dir == NULL) {
		return;
	}
	
	/* Marking this as COLLECTION_UNSAFE prevents entries from
	 * expiring under us if we remove this one
	 */
	_WAPI_HANDLE_COLLECTION_UNSAFE;
	
	/* Prevent new entries racing with us */
	do {
		now = (guint32)(time(NULL) & 0xFFFFFFFF);

		thr_ret = _wapi_timestamp_exclusion (&_wapi_fileshare_layout->share_check, now);
		if (thr_ret == EBUSY) {
			_wapi_handle_spin (100);
		}
	} while (thr_ret == EBUSY);
	g_assert (thr_ret == 0);

	while ((proc_entry = readdir (proc_dir)) != NULL) {
		/* We only care about numerically-named directories */
		pid = atoi (proc_entry->d_name);
		if (pid != 0) {
			/* Look in /proc/<pid>/fd/ but ignore
			 * /proc/<our pid>/fd/<fd>, as we have the
			 * file open too
			 */
			DIR *fd_dir;
			struct dirent *fd_entry;
			char subdir[_POSIX_PATH_MAX];
			
			g_snprintf (subdir, _POSIX_PATH_MAX, "/proc/%d/fd",
				    pid);
			
			fd_dir = opendir (subdir);
			if (fd_dir == NULL) {
				continue;
			}
			
			while ((fd_entry = readdir (fd_dir)) != NULL) {
				char path[_POSIX_PATH_MAX];
				struct stat link_stat;
				
				if (!strcmp (fd_entry->d_name, ".") ||
				    !strcmp (fd_entry->d_name, "..") ||
				    (pid == self &&
				     fd == atoi (fd_entry->d_name))) {
					continue;
				}

				g_snprintf (path, _POSIX_PATH_MAX,
					    "/proc/%d/fd/%s", pid,
					    fd_entry->d_name);
				
				stat (path, &link_stat);
				if (link_stat.st_dev == share_info->device &&
				    link_stat.st_ino == share_info->inode) {
#ifdef DEBUG
					g_message ("%s:  Found it at %s",
						   __func__, path);
#endif

					found = TRUE;
				}
			}
			
			closedir (fd_dir);
		}
	}
	
	closedir (proc_dir);

	if (found == FALSE) {
		/* Blank out this entry, as it is stale */
#ifdef DEBUG
		g_message ("%s: Didn't find it, destroying entry", __func__);
#endif

		memset (share_info, '\0', sizeof(struct _WapiFileShare));
	}

	thr_ret = _wapi_timestamp_release (&_wapi_fileshare_layout->share_check, now);

	_WAPI_HANDLE_COLLECTION_SAFE;
}

void _wapi_handle_dump (void)
{
	struct _WapiHandleUnshared *handle_data;
	guint32 i, k;

	for(i = SLOT_INDEX (0); _wapi_private_handles [i] != NULL; i++) {
		for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
			handle_data = &_wapi_private_handles [i][k];

			if (handle_data->type == WAPI_HANDLE_UNUSED) {
				continue;
			}
		
			g_print ("%3x [%7s] %s %d ", i,
				 _wapi_handle_typename[handle_data->type],
				 handle_data->signalled?"Sg":"Un", handle_data->ref);
			handle_details[handle_data->type](&handle_data->u);
			g_print ("\n");
		}
	}
}

static void _wapi_shared_details (gpointer handle_info)
{
	struct _WapiHandle_shared_ref *shared = (struct _WapiHandle_shared_ref *)handle_info;
	
	g_print ("offset: 0x%x", shared->offset);
}

void _wapi_handle_update_refs (void)
{
	guint32 i, k, lock_now;
	int thr_ret;
	
	_WAPI_HANDLE_COLLECTION_UNSAFE;

	/* Prevent file share entries racing with us */
	do {
		lock_now = (guint32)(time(NULL) & 0xFFFFFFFF);

		thr_ret = _wapi_timestamp_exclusion (&_wapi_fileshare_layout->share_check, lock_now);

		if (thr_ret == EBUSY) {
			_wapi_handle_spin (100);
		}
	} while (thr_ret == EBUSY);
	g_assert(thr_ret == 0);

	for(i = SLOT_INDEX (0); _wapi_private_handles [i] != NULL; i++) {
		for (k = SLOT_OFFSET (0); k < _WAPI_HANDLE_INITIAL_COUNT; k++) {
			struct _WapiHandleUnshared *handle = &_wapi_private_handles [i][k];
			guint32 now = (guint32)(time (NULL) & 0xFFFFFFFF);

			if (_WAPI_SHARED_HANDLE(handle->type)) {
				struct _WapiHandleSharedMetadata *shared_meta;
				
#ifdef DEBUG
				g_message ("%s: (%d) handle 0x%x is SHARED", __func__,
					   getpid (), i);
#endif

				shared_meta = &_wapi_shared_layout->metadata[handle->u.shared.offset];

#ifdef DEBUG
				g_message ("%s: (%d) Updating timstamp of handle 0x%x",
					   __func__, getpid(),
					   handle->u.shared.offset);
#endif

				InterlockedExchange (&shared_meta->timestamp, now);
			} else if (handle->type == WAPI_HANDLE_FILE) {
				struct _WapiHandle_file *file_handle = &handle->u.file;
				
#ifdef DEBUG
				g_message ("%s: (%d) handle 0x%x is FILE", __func__,
					   getpid (), i);
#endif
				
				g_assert (file_handle->share_info != NULL);

#ifdef DEBUG
				g_message ("%s: (%d) Inc refs on fileshare 0x%x",
					   __func__, getpid(),
					   (file_handle->share_info - &_wapi_fileshare_layout->share_info[0]) / sizeof(struct _WapiFileShare));
#endif

				InterlockedExchange (&file_handle->share_info->timestamp, now);
			}
		}
	}
	
	thr_ret = _wapi_timestamp_release (&_wapi_fileshare_layout->share_check, lock_now);
	_WAPI_HANDLE_COLLECTION_SAFE;
}
