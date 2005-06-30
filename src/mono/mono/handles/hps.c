#include <config.h>
#include <glib.h>

#include <mono/io-layer/io-layer.h>

/* We're digging into handle internals here... */
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/collection.h>

static const guchar *unused_details (struct _WapiHandleShared *handle);
static const guchar *unshared_details (struct _WapiHandleShared *handle);
#if 0
static const guchar *thread_details (struct _WapiHandleShared *handle);
#endif
static const guchar *namedmutex_details (struct _WapiHandleShared *handle);
static const guchar *process_details (struct _WapiHandleShared *handle);

/* This depends on the ordering of the enum WapiHandleType in
 * io-layer/wapi-private.h
 */
static const guchar * (*details[])(struct _WapiHandleShared *)=
{
	unused_details,
	unshared_details,		/* file */
	unshared_details,		/* console */
#if 0
	thread_details,
#else
	unshared_details,		/* thread */
#endif
	unshared_details,		/* sem */
	unshared_details,		/* mutex */
	unshared_details,		/* event */
	unshared_details,		/* socket */
	unshared_details,		/* find */
	process_details,
	unshared_details,		/* pipe */
	namedmutex_details,
	unused_details,
};

int main (int argc, char **argv)
{
	guint32 i;
	
	_wapi_shared_layout = _wapi_shm_attach(WAPI_SHM_DATA);
	if (_wapi_shared_layout == NULL) {
		g_error ("Failed to attach shared memory!");
		exit (-1);
	}

	_wapi_fileshare_layout = _wapi_shm_attach(WAPI_SHM_FILESHARE);
	if (_wapi_fileshare_layout == NULL) {
		g_error ("Failed to attach fileshare shared memory!");
		exit (-1);
	}
	
	if (argc > 1) {
		_wapi_shm_semaphores_init ();
		_wapi_collection_init ();
		_wapi_handle_collect ();
	}
	
	g_print ("collection: %d signals: %d sem: 0x%x\n",
		 _wapi_shared_layout->collection_count,
		 _wapi_shared_layout->signal_count,
		 _wapi_shared_layout->sem_key);
	
	for (i = 0; i < _WAPI_HANDLE_INITIAL_COUNT; i++) {
		struct _WapiHandleShared *shared;
		struct _WapiHandleSharedMetadata *meta;
		guint32 now = (guint32)(time(NULL) & 0xFFFFFFFF);
		
		meta = &_wapi_shared_layout->metadata[i];
		
		shared = &_wapi_shared_layout->handles[i];
		if (shared->stale == TRUE) {
			g_print ("    (%3x) [%7s]  *STALE*  (%s)\n", i,
				 _wapi_handle_typename[shared->type],
				 details[shared->type](shared));
		}
		
		shared = &_wapi_shared_layout->handles[meta->offset];
		if (shared->type != WAPI_HANDLE_UNUSED) {
			g_print ("%3x (%3x) [%7s] %4u %s (%s)\n",
				 i, meta->offset,
				 _wapi_handle_typename[shared->type],
				 now - meta->timestamp,
				 meta->signalled?"Sg":"Un",
				 details[shared->type](shared));
		}
	}

	g_print ("Fileshare hwm: %d\n", _wapi_fileshare_layout->hwm);
	
	for (i = 0; i <= _wapi_fileshare_layout->hwm; i++) {
		struct _WapiFileShare *file_share;
		
		file_share = &_wapi_fileshare_layout->share_info[i];
		if (file_share->handle_refs > 0) {
			g_print ("dev: 0x%llx ino: %lld open pid: %d share: 0x%x access: 0x%x refs: %d\n", file_share->device, file_share->inode, file_share->opened_by_pid, file_share->sharemode, file_share->access, file_share->handle_refs);
		}
	}
	
	exit (0);
}

static const guchar *unused_details (struct _WapiHandleShared *handle)
{
	return("unused details");
}

static const guchar *unshared_details (struct _WapiHandleShared *handle)
{
	return("unshared details");
}

#if 0
static const guchar *thread_details (struct _WapiHandleShared *handle)
{
	static guchar buf[80];
	struct _WapiHandle_thread *thr=&handle->u.thread;

	g_snprintf (buf, sizeof(buf),
		    "proc: %d, state: %d, exit: %u, join: %d",
		    thr->owner_pid, thr->state, thr->exitstatus,
		    thr->joined);
	
	return(buf);
}
#endif

static const guchar *namedmutex_details (struct _WapiHandleShared *handle)
{
	static guchar buf[80];
	gchar *name;
	struct _WapiHandle_namedmutex *mut=&handle->u.namedmutex;
	
	name = mut->sharedns.name;
	
	g_snprintf (buf, sizeof(buf), "[%15s] own: %5d:%5ld, count: %5u",
		    name==NULL?(gchar *)"":name, mut->pid, mut->tid,
		    mut->recursion);

	return(buf);
}

static const guchar *process_details (struct _WapiHandleShared *handle)
{
	static guchar buf[80];
	gchar *name;
	struct _WapiHandle_process *proc=&handle->u.process;
	
	name = proc->proc_name;
	
	g_snprintf (buf, sizeof(buf), "[%15s] pid: %5u exit: %u",
		    name==NULL?(gchar *)"":name, proc->id, proc->exitstatus);
	
	return(buf);
}
