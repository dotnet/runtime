/*
 * shared.c:  Shared memory handling, and daemon launching
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

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
#include <string.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/daemon-private.h>

#undef DEBUG

gpointer _wapi_shm_attach (gboolean daemon, gboolean *success, int *shm_id)
{
	gpointer shm_seg;
	key_t key;
	gboolean fork_daemon=FALSE;
	struct _WapiHandleShared_list *data;
	int tries;
	
	/*
	 * This is an attempt to get a unique key id.  The first arg
	 * to ftok is a path, so when the config file support is done
	 * we should use that.
	 */
	key=ftok (g_get_home_dir (), _WAPI_HANDLE_VERSION);
	
try_again:
	*shm_id=shmget (key, sizeof(struct _WapiHandleShared_list)+
			_WAPI_SHM_SCRATCH_SIZE, IPC_CREAT | IPC_EXCL | 0600);
	if(*shm_id==-1 && errno==EEXIST) {
		/* Cool, we dont have to fork the handle daemon, but
		 * we still need to try and get the shm_id.
		 */
		*shm_id=shmget (key, 0, 0600);
			
		/* it's possible that the shared memory segment was
		 * deleted in between seeing if it exists, and
		 * attaching it.  If we got an error here, just try
		 * attaching it again.
		 */
		if(*shm_id==-1) {
			goto try_again;
		}
	} else if (*shm_id!=-1) {
		/* We created the shared memory segment, so we need to
		 * fork the handle daemon too
		 */
		fork_daemon=TRUE;

		/* sysv shared mem is set to all zero when allocated,
		 * so we don't need to do any more initialisation here
		 */
	} else {
		/* Some error other than EEXIST */
		g_message (G_GNUC_PRETTY_FUNCTION ": shmget error: %s",
			   strerror (errno));
		exit (-1);
	}
	
	/* From now on, we need to delete the shm segment before
	 * exiting on error if we created it (ie, if
	 * fork_daemon==TRUE)
	 */
	shm_seg=shmat (*shm_id, NULL, 0);
	if(shm_seg==(gpointer)-1) {
		g_message (G_GNUC_PRETTY_FUNCTION ": shmat error: %s",
			   strerror (errno));
		if(fork_daemon==TRUE) {
			_wapi_shm_destroy ();
		}
		exit (-1);
	}

	if(daemon==TRUE) {
		/* No more to do in the daemon */
		*success=TRUE;
		return(shm_seg);
	}
		
	data=shm_seg;

	if(fork_daemon==TRUE) {
		pid_t pid;
			
		pid=fork ();
		if(pid==-1) {
			g_message (G_GNUC_PRETTY_FUNCTION ": fork error: %s",
				   strerror (errno));
			_wapi_shm_destroy ();
			exit (-1);
		} else if (pid==0) {
			/* child */
			setsid ();
			
			/* FIXME: Clean up memory.  We can delete all
			 * the managed data
			 */
			/* FIXME2: Set process title to something
			 * informative
			 */

			/* _wapi_daemon_main() does not return */
			_wapi_daemon_main ();
			
			/* But just in case... */
			data->daemon_running=2;
			exit (-1);
		}
		/* parent carries on */
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Daemon pid %d", pid);
#endif
	} else {
		/* Do some sanity checking on the shared memory we
		 * attached
		 */
		if(!(data->daemon_running==0 || data->daemon_running==1 ||
		     data->daemon_running==2) ||
		   (strncmp (data->daemon+1, "mono-handle-daemon-", 19)!=0)) {
			g_warning ("Shared memory sanity check failed.");
			*success=FALSE;
			return(NULL);
		}
	}
		
	for(tries=0; data->daemon_running==0 && tries < 100; tries++) {
		/* wait for the daemon to sort itself out.  To be
		 * completely safe, we should have a timeout before
		 * giving up.
		 */
		struct timespec sleepytime;
			
		sleepytime.tv_sec=0;
		sleepytime.tv_nsec=10000000;	/* 10ms */
			
		nanosleep (&sleepytime, NULL);
	}
	if(tries==100 && data->daemon_running==0) {
		/* Daemon didnt get going */
		if(fork_daemon==TRUE) {
			_wapi_shm_destroy ();
		}
		g_warning ("The handle daemon didnt start up properly");
		*success=FALSE;
		return(NULL);
	}
	
	if(data->daemon_running==2) {
		/* Oh dear, the daemon had an error starting up */
		if(fork_daemon==TRUE) {
			_wapi_shm_destroy ();
		}
		g_warning ("Handle daemon failed to start");
		*success=FALSE;
		return(NULL);
	}
		
	/* From now on, it's up to the daemon to delete the shared
	 * memory segment
	 */
	
	*success=TRUE;
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
	
	shm_id=shmget (key, 0, 0600);
	if(shm_id==-1 && errno==ENOENT) {
		return;
	} else if (shm_id==-1) {
		g_message (G_GNUC_PRETTY_FUNCTION ": shmget error: %s",
			   strerror (errno));
		exit (-1);
	}
	if(shmctl (shm_id, IPC_RMID, NULL)==-1) {
		g_message (G_GNUC_PRETTY_FUNCTION ": shmctl error: %s",
			   strerror (errno));
		exit (-1);
	}
#endif /* DISABLE_SHARED_HANDLES */
}
