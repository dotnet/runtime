#include <config.h>
#include <glib.h>

#include <mono/io-layer/io-layer.h>

/* We're digging into handle internals here... */
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/wapi-private.h>

static const guchar *unused_details (struct _WapiHandleShared *handle);
static const guchar *file_details (struct _WapiHandleShared *handle);
static const guchar *console_details (struct _WapiHandleShared *handle);
static const guchar *thread_details (struct _WapiHandleShared *handle);
static const guchar *sem_details (struct _WapiHandleShared *handle);
static const guchar *mutex_details (struct _WapiHandleShared *handle);
static const guchar *event_details (struct _WapiHandleShared *handle);
static const guchar *socket_details (struct _WapiHandleShared *handle);
static const guchar *find_details (struct _WapiHandleShared *handle);
static const guchar *process_details (struct _WapiHandleShared *handle);
static const guchar *pipe_details (struct _WapiHandleShared *handle);

/* This depends on the ordering of the enum WapiHandleType in
 * io-layer/wapi-private.h
 */
static const char *typename[]={
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
	"Error!!"
};

/* So does this... */
static const guchar * (*details[])(struct _WapiHandleShared *)=
{
	unused_details,
	file_details,
	console_details,
	thread_details,
	sem_details,
	mutex_details,
	event_details,
	socket_details,
	find_details,
	process_details,
	pipe_details,
	unused_details,
};

int main (int argc, char **argv)
{
	guint32 handle_idx;
	gboolean success;
	
	_wapi_shared_data=g_new0 (struct _WapiHandleShared_list *, 1);
	_wapi_shared_scratch=g_new0 (struct _WapiHandleScratch, 1);
	
	success=_wapi_shm_attach (&_wapi_shared_data[0],
				  &_wapi_shared_scratch);
	if(success==FALSE) {
		g_error ("Failed to attach shared memory!");
		exit (-1);
	}
	
	/* Make sure index 0 is actually unused */
	for(handle_idx=0; handle_idx<_wapi_shared_data[0]->num_segments * _WAPI_HANDLES_PER_SEGMENT; handle_idx++) {
		guint32 segment, idx;
		struct _WapiHandleShared *shared;

		_wapi_handle_segment (GUINT_TO_POINTER (handle_idx), &segment,
				      &idx);
		_wapi_handle_ensure_mapped (segment);
		
		shared=&_wapi_shared_data[segment]->handles[idx];
		
		if(shared->type!=WAPI_HANDLE_UNUSED) {
			g_print ("%6x [%7s] %4u %s (%s)\n", handle_idx,
				 typename[shared->type], shared->ref,
				 shared->signalled?"Sg":"Un",
				 details[shared->type](shared));
		}
	}
	
	exit (0);
}

static const guchar *unused_details (struct _WapiHandleShared *handle)
{
	return("unused details");
}

static const guchar *file_details (struct _WapiHandleShared *handle)
{
	static guchar buf[80];
	guchar *name;
	struct _WapiHandle_file *file=&handle->u.file;
	
	name=_wapi_handle_scratch_lookup (file->filename);
	
	g_snprintf (buf, sizeof(buf),
		    "[%20s] acc: %c%c%c, shr: %c%c%c, attrs: %5u",
		    name==NULL?(guchar *)"":name,
		    file->fileaccess&GENERIC_READ?'R':'.',
		    file->fileaccess&GENERIC_WRITE?'W':'.',
		    file->fileaccess&GENERIC_EXECUTE?'X':'.',
		    file->sharemode&FILE_SHARE_READ?'R':'.',
		    file->sharemode&FILE_SHARE_WRITE?'W':'.',
		    file->sharemode&FILE_SHARE_DELETE?'D':'.',
		    file->attrs);

	if(name!=NULL) {
		g_free (name);
	}
	
	return(buf);
}

static const guchar *console_details (struct _WapiHandleShared *handle)
{
	return(file_details (handle));
}

static const guchar *thread_details (struct _WapiHandleShared *handle)
{
	static guchar buf[80];
	struct _WapiHandle_thread *thr=&handle->u.thread;

	g_snprintf (buf, sizeof(buf),
		    "proc: %p, state: %d, exit: %u",
		    thr->process_handle, thr->state, thr->exitstatus);
	
	return(buf);
}

static const guchar *sem_details (struct _WapiHandleShared *handle)
{
	static guchar buf[80];
	struct _WapiHandle_sem *sem=&handle->u.sem;
	
	g_snprintf (buf, sizeof(buf), "val: %5u, max: %5d",
		    sem->val, sem->max);
	
	return(buf);
}

static const guchar *mutex_details (struct _WapiHandleShared *handle)
{
	static guchar buf[80];
	guchar *name;
	struct _WapiHandle_mutex *mut=&handle->u.mutex;
	
	name=_wapi_handle_scratch_lookup (mut->name);
	
	g_snprintf (buf, sizeof(buf), "[%20s] own: %5d:%5ld, count: %5u",
		    name==NULL?(guchar *)"":name, mut->pid, mut->tid,
		    mut->recursion);

	if(name!=NULL) {
		g_free (name);
	}
	
	return(buf);
}

static const guchar *event_details (struct _WapiHandleShared *handle)
{
	static guchar buf[80];
	struct _WapiHandle_event *event=&handle->u.event;

	g_snprintf (buf, sizeof(buf), "manual: %s",
		    event->manual?"TRUE":"FALSE");
	
	return(buf);
}

static const guchar *socket_details (struct _WapiHandleShared *handle)
{
	/* Nothing to see here */
	return("");
}

static const guchar *find_details (struct _WapiHandleShared *handle)
{
	static guchar buf[80];
	struct _WapiHandle_find *find=&handle->u.find;
	
	g_snprintf (buf, sizeof(buf), "count: %5d",
		    find->count);
	
	return(buf);
}

static const guchar *process_details (struct _WapiHandleShared *handle)
{
	static guchar buf[80];
	guchar *name;
	struct _WapiHandle_process *proc=&handle->u.process;
	
	name=_wapi_handle_scratch_lookup (proc->proc_name);
	
	g_snprintf (buf, sizeof(buf), "[%20s] pid: %5u",
		    name==NULL?(guchar *)"":name, proc->id);

	if(name!=NULL) {
		g_free (name);
	}
	
	return(buf);
}

static const guchar *pipe_details (struct _WapiHandleShared *handle)
{
	return(file_details (handle));
}
