/*
 * Code to support inter-process sharing of handles.
 *
 * I thought of using an mmap()ed file for this.  If linuxthreads
 * supported PTHREAD_PROCESS_SHARED I would have done; however without
 * that pthread support the only other inter-process IPC
 * synchronisation option is a sysV semaphore, and if I'm going to use
 * that I may as well take advantage of sysV shared memory too.
 * Actually, semaphores seem to be buggy, or I was using them
 * incorrectly :-).  I've replaced the sysV semaphore with a shared
 * integer controlled with Interlocked functions.

 * mmap() files have the advantage of avoiding namespace collisions,
 * but have the disadvantage of needing cleaning up, and also msync().
 * sysV shared memory has a really stupid way of getting random key
 * IDs, which can lead to collisions.
 *
 * I deliberately don't ever delete the shared memory: I'd like to
 * have been able to set the shared memory segment to destroy itself
 * on last close, but it doesn't support that. (Setting IPC_RMID on a
 * segment causes subsequent shmat() with the same key to get a new
 * segment :-( ).  The function to delete the shared memory segment is
 * only called from a debugging tool (mono/handles/shmdel).
 *
 * w32 processes do not have the POSIX parent-child relationship, so a
 * process handle is available to any other process to find out exit
 * status.  Handles are destroyed when the last reference to them is
 * closed.  New handles can be created for long lasting items such as
 * processes or threads, and also for named synchronisation objects so
 * long as these haven't been deleted by having the last referencing
 * handle closed.
 */

#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/ipc.h>
#include <sys/shm.h>
#include <errno.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/shared.h>

#undef DEBUG

static gboolean shared;

gpointer _wapi_shm_attach (guint32 *scratch_size)
{
	gpointer shm_seg;

	*scratch_size=getpagesize ()*100;
	
#ifndef DISABLE_SHARED_HANDLES
	if(getenv ("MONO_DISABLE_SHM"))
#endif
	{
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": Using process-private handles");
#endif

		shared=FALSE;
		shm_seg=g_malloc0 (sizeof(struct _WapiHandleShared_list)+
				   *scratch_size);
#ifndef DISABLE_SHARED_HANDLES
	} else {
		int shm_id;
		key_t key;
	
		shared=TRUE;
		
		/*
		 * This is an attempt to get a unique key id.  The
		 * first arg to ftok is a path, so when the config
		 * file support is done we should use that.
		 */
		key=ftok (g_get_home_dir (), _WAPI_HANDLE_VERSION);
	
		/* sysv shared mem is set to all zero when allocated,
		 * so we don't need to do any more initialisation here
		 */
		shm_id=shmget (key, sizeof(struct _WapiHandleShared_list)+
			       *scratch_size, IPC_CREAT | 0600);
		if(shm_id==-1) {
			perror ("shmget");
			exit (-1);
		}
	
		shm_seg=shmat (shm_id, NULL, 0);
		if(shm_seg==(gpointer)-1) {
			perror ("shmat");
			exit (-1);
		}
#endif /* DISABLE_SHARED_HANDLES */
	}

	return(shm_seg);
}

void _wapi_shm_destroy (void)
{
#ifndef DISABLE_SHARED_HANDLES
	int shm_id;
	key_t key;
		
	/*
	 * This is an attempt to get a unique key id.  The
	 * first arg to ftok is a path, so when the config
	 * file support is done we should use that.
	 */
	key=ftok (g_get_home_dir (), _WAPI_HANDLE_VERSION);
	
	/* sysv shared mem is set to all zero when allocated,
	 * so we don't need to do any more initialisation here
	 */
	shm_id=shmget (key, 0, 0600);
	if(shm_id==-1 && errno==ENOENT) {
		return;
	} else if (shm_id==-1) {
		perror ("shmget");
		exit (-1);
	}
	if(shmctl (shm_id, IPC_RMID, NULL)==-1) {
		perror ("shmctl");
		exit (-1);
	}
#endif /* DISABLE_SHARED_HANDLES */
}
