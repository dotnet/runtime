#include <config.h>
#include <glib.h>

#include "mini.h"

#if defined(HOST_WIN32) || !defined(HAVE_SYS_IPC_H) || !defined(HAVE_SYS_SEM_H)

int mini_wapi_hps (int argc, char **argv)
{
	return 0;
}

int mini_wapi_semdel (int argc, char **argv)
{
	return 0;
}

int mini_wapi_seminfo (int argc, char **argv)
{
	return 0;
}

#else

#include <errno.h>
#include <sys/types.h>
#include <sys/ipc.h>
#include <sys/sem.h>
#include <mono/io-layer/io-layer.h>

/* We're digging into handle internals here... */
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/collection.h>

static const gchar *unused_details (struct _WapiHandleShared *handle);
static const gchar *unshared_details (struct _WapiHandleShared *handle);
#if 0
static const gchar *thread_details (struct _WapiHandleShared *handle);
#endif
static const gchar *namedmutex_details (struct _WapiHandleShared *handle);
static const gchar *namedsem_details (struct _WapiHandleShared *handle);
static const gchar *namedevent_details (struct _WapiHandleShared *handle);
static const gchar *process_details (struct _WapiHandleShared *handle);

/* This depends on the ordering of the enum WapiHandleType in
 * io-layer/wapi-private.h
 */
static const gchar * (*details[])(struct _WapiHandleShared *)=
{
	unused_details,
	unshared_details,		/* file */
	unshared_details,		/* console */
	unshared_details,		/* thread */
	unshared_details,		/* sem */
	unshared_details,		/* mutex */
	unshared_details,		/* event */
	unshared_details,		/* socket */
	unshared_details,		/* find */
	process_details,
	unshared_details,		/* pipe */
	namedmutex_details,
	namedsem_details,
	namedevent_details,
	unused_details,
};

int mini_wapi_hps (int argc, char **argv)
{
	guint32 i;
	guint32 now;

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
	
	g_print ("collection: %d sem: 0x%x\n",
		 _wapi_shared_layout->collection_count,
		 _wapi_shared_layout->sem_key);
	
	now = (guint32)(time(NULL) & 0xFFFFFFFF);
	for (i = 0; i < _WAPI_HANDLE_INITIAL_COUNT; i++) {
		struct _WapiHandleShared *shared;
		
		shared = &_wapi_shared_layout->handles[i];
		if (shared->type != WAPI_HANDLE_UNUSED) {
			g_print ("%3x (%3d) [%7s] %4u %s (%s)\n",
				 i, shared->handle_refs,
				 _wapi_handle_typename[shared->type],
				 now - shared->timestamp,
				 shared->signalled?"Sg":"Un",
				 details[shared->type](shared));
		}
	}

	g_print ("Fileshare hwm: %d\n", _wapi_fileshare_layout->hwm);
	
	for (i = 0; i <= _wapi_fileshare_layout->hwm; i++) {
		struct _WapiFileShare *file_share;
		
		file_share = &_wapi_fileshare_layout->share_info[i];
		if (file_share->handle_refs > 0) {
			g_print ("dev: 0x%llx ino: %lld open pid: %d share: 0x%x access: 0x%x refs: %d\n", (long long int)file_share->device, (long long int)file_share->inode, file_share->opened_by_pid, file_share->sharemode, file_share->access, file_share->handle_refs);
		}
	}
	
	exit (0);
}

static const gchar *unused_details (struct _WapiHandleShared *handle)
{
	return("unused details");
}

static const gchar *unshared_details (struct _WapiHandleShared *handle)
{
	return("unshared details");
}

#if 0
static const gchar *thread_details (struct _WapiHandleShared *handle)
{
	static gchar buf[80];
	struct _WapiHandle_thread *thr=&handle->u.thread;

	g_snprintf (buf, sizeof(buf),
		    "proc: %d, tid: %ld, state: %d, exit: %u, join: %d",
		    thr->owner_pid, thr->id, thr->state, thr->exitstatus,
		    thr->joined);
	
	return(buf);
}
#endif

static const gchar *namedmutex_details (struct _WapiHandleShared *handle)
{
	static gchar buf[80];
	gchar *name;
	struct _WapiHandle_namedmutex *mut=&handle->u.namedmutex;
	
	name = mut->sharedns.name;
	
	g_snprintf (buf, sizeof(buf), "[%15s] own: %5d:%5ld, count: %5u",
		    name==NULL?(gchar *)"":name, mut->pid, mut->tid,
		    mut->recursion);

	return(buf);
}

static const gchar *namedsem_details (struct _WapiHandleShared *handle)
{
	static gchar buf[80];
	gchar *name;
	struct _WapiHandle_namedsem *sem = &handle->u.namedsem;
	
	name = sem->sharedns.name;
	
	g_snprintf (buf, sizeof(buf), "[%15s] val: %5u, max: %5d",
		    name == NULL?(gchar *)"":name, sem->val, sem->max);

	return(buf);
}

static const gchar *namedevent_details (struct _WapiHandleShared *handle)
{
	static gchar buf[80];
	gchar *name;
	struct _WapiHandle_namedevent *event = &handle->u.namedevent;
	
	name = event->sharedns.name;
	
	g_snprintf (buf, sizeof(buf), "[%15s] %s count: %5u",
		    name == NULL?(gchar *)"":name,
		    event->manual?"Manual":"Auto", event->set_count);

	return(buf);
}

static const gchar *process_details (struct _WapiHandleShared *handle)
{
	static gchar buf[80];
	gchar *name;
	struct _WapiHandle_process *proc=&handle->u.process;
	
	name = proc->proc_name;
	
	g_snprintf (buf, sizeof(buf), "[%25.25s] pid: %5u exit: %u",
		    name==NULL?(gchar *)"":name, proc->id, proc->exitstatus);
	
	return(buf);
}

/* The old handles/semdel.c */
int mini_wapi_semdel (int argc, char **argv)
{
	int sem_id, ret;
	
	_wapi_shared_layout = _wapi_shm_attach(WAPI_SHM_DATA);
	if (_wapi_shared_layout == FALSE ||
	    _wapi_shared_layout->sem_key == 0) {
		exit (0);
	}

	sem_id = semget (_wapi_shared_layout->sem_key, _WAPI_SHARED_SEM_COUNT, 0600);
	if (sem_id != -1) {
		ret = semctl (sem_id, 0, IPC_RMID);
		if (ret == -1) {
			g_message ("Error deleting semaphore: %s",
				   g_strerror (errno));
		}
	}
	
	exit (0);
}

static void sem_explain (int sem_id, ushort *vals, int which)
{
	pid_t pid;
	
	g_print ("%d ", vals[which]);
	if (vals[which] >= 1) {
		g_print ("(Unlocked)");
	} else {
		pid = semctl (sem_id, which, GETPID);
		
		g_print ("(Locked by %d)", pid);
	}
	g_print ("\n");
}

int mini_wapi_seminfo (int argc, char **argv)
{
	int sem_id, ret;
	union semun
	{
		int val;
		struct semid_ds *buf;
		ushort *array;
	} arg;
	ushort vals[_WAPI_SHARED_SEM_COUNT];
	
	_wapi_shared_layout = _wapi_shm_attach (WAPI_SHM_DATA);
	if (_wapi_shared_layout == FALSE ||
	    _wapi_shared_layout->sem_key == 0) {
		exit (0);
	}
	
	sem_id = semget (_wapi_shared_layout->sem_key, _WAPI_SHARED_SEM_COUNT, 0600);
	if (sem_id != -1) {
		g_print ("Getting values for sem: 0x%x\n",
			 _wapi_shared_layout->sem_key);
		arg.array = vals;
		ret = semctl (sem_id, 0, GETALL, arg);
		if (ret != -1) {
			g_print ("Namespace: ");
			sem_explain (sem_id, vals, _WAPI_SHARED_SEM_NAMESPACE);
			g_print ("Fileshare: ");
			sem_explain (sem_id, vals, _WAPI_SHARED_SEM_FILESHARE);
			g_print ("Handles: ");
			sem_explain (sem_id, vals,
				     _WAPI_SHARED_SEM_SHARED_HANDLES);
			g_print ("Count lock: ");
			sem_explain (sem_id, vals,
				     _WAPI_SHARED_SEM_PROCESS_COUNT_LOCK);
			g_print ("Count: %d\n",
				 vals[_WAPI_SHARED_SEM_PROCESS_COUNT]);
		}
	}
	
	exit (0);
}

#endif

