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
 * integer controlled with Interlocked functions.  And I've since
 * replaced that with a separate process to serialise access to the
 * shared memory, to avoid the possibility of DOS by leaving the
 * shared memory locked, and also to allow the shared memory to be
 * cleaned up.
 *
 * mmap() files have the advantage of avoiding namespace collisions,
 * but have the disadvantage of needing cleaning up, and also msync().
 * sysV shared memory has a really stupid way of getting random key
 * IDs, which can lead to collisions.
 *
 * Having tried sysv shm, I tested mmap() and found that MAP_SHARED
 * makes msync() irrelevent, and both types need cleaning up.  Seeing
 * as mmap() doesn't suffer from the bonkers method of allocating
 * segments, it seems to be the best method.
 *
 * This shared memory is needed because w32 processes do not have the
 * POSIX parent-child relationship, so a process handle is available
 * to any other process to find out exit status.  Handles are
 * destroyed when the last reference to them is closed.  New handles
 * can be created for long lasting items such as processes or threads,
 * and also for named synchronisation objects so long as these haven't
 * been deleted by having the last referencing handle closed.
 */


#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <fcntl.h>
#include <unistd.h>
#include <sys/mman.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <errno.h>
#include <string.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/daemon-private.h>

#undef DEBUG

static guchar *shared_file (void)
{
	static guchar *file=NULL;
	guchar *name, *dir;
	
	if(file!=NULL) {
		return(file);
	}
	
	/* Change the filename whenever the format of the contents
	 * changes
	 */
	name=g_strdup_printf ("shared_data-%d", _WAPI_HANDLE_VERSION);

	/* I don't know how nfs affects mmap.  If mmap() of files on
	 * nfs mounts breaks, then there should be an option to set
	 * the directory.
	 */
	file=g_build_filename (g_get_home_dir (), ".wapi", name, NULL);
	g_free (name);

	/* No need to check if the dir already exists or check
	 * mkdir() errors, because on any error the open() call will
	 * report the problem.
	 */
	dir=g_path_get_dirname (file);
	mkdir (dir, 0755);
	g_free (dir);
		
	return(file);
}

/*
 * _wapi_shm_attach:
 * @success: Was it a success
 *
 * Attach to the shared memory file or create it if it did not
 * exist. If it was created and daemon was FALSE a new daemon is
 * forked into existence. Returns the memory area the file was mmapped
 * to.
 */
gpointer _wapi_shm_attach (gboolean *success)
{
	gpointer shm_seg;
	int fd;
	gboolean fork_daemon=FALSE;
	struct stat statbuf;
	struct _WapiHandleShared_list *data;
	int tries;
	int wanted_size=sizeof(struct _WapiHandleShared_list) +
		_WAPI_SHM_SCRATCH_SIZE;
	
	*success=FALSE;

try_again:
	/* No O_CREAT yet, because we need to initialise the file if
	 * we have to create it.
	 */
	fd=open (shared_file (), O_RDWR, 0600);
	if(fd==-1 && errno==ENOENT) {
		/* OK, its up to us to create it.  O_EXCL to avoid a
		 * race condition where two processes can
		 * simultaneously try and create the file
		 */
		fd=open (shared_file (), O_CREAT|O_EXCL|O_RDWR, 0600);
		if(fd==-1 && errno==EEXIST) {
			/* It's possible that the file was created in
			 * between finding it didn't exist, and trying
			 * to create it.  Just try opening it again
			 */
			goto try_again;
		} else if (fd==-1) {
			g_critical (G_GNUC_PRETTY_FUNCTION
				    ": shared file [%s] open error: %s",
				    shared_file (), g_strerror (errno));
			return(NULL);
		} else {
			/* We created the file, so we need to expand
			 * the file and fork the handle daemon too
			 */
			if(lseek (fd, wanted_size, SEEK_SET)==-1) {
				g_critical (G_GNUC_PRETTY_FUNCTION ": shared file [%s] lseek error: %s", shared_file (), g_strerror (errno));
				_wapi_shm_destroy ();
				return(NULL);
			}
			
			if(write (fd, "", 1)==-1) {
				g_critical (G_GNUC_PRETTY_FUNCTION ": shared file [%s] write error: %s", shared_file (), g_strerror (errno));
				_wapi_shm_destroy ();
				return(NULL);
			}
			
			fork_daemon=TRUE;

			/* The contents of the file is set to all
			 * zero, because it is opened up with lseek,
			 * so we don't need to do any more
			 * initialisation here
			 */
		}
	} else if(fd==-1) {
		g_critical (G_GNUC_PRETTY_FUNCTION
			    ": shared file [%s] open error: %s",
			    shared_file (), g_strerror (errno));
		return(NULL);
	} else {
		/* We dont need to fork the handle daemon */
	}
	
	/* From now on, we need to delete the file before exiting on
	 * error if we created it (ie, if fork_daemon==TRUE)
	 */

	/* Use stat to find the file size (instead of hard coding it)
	 * so that we can expand the file later if needed (for more
	 * handles or scratch space, though that will require a tweak
	 * to the file format to store the count).
	 */
	if(fstat (fd, &statbuf)==-1) {
		g_critical (G_GNUC_PRETTY_FUNCTION ": fstat error: %s",
			    g_strerror (errno));
		if(fork_daemon==TRUE) {
			_wapi_shm_destroy ();
		}
		return(NULL);
	}

	if(statbuf.st_size < wanted_size) {
#ifdef HAVE_LARGE_FILE_SUPPORT
		/* Keep gcc quiet... */
		g_critical (G_GNUC_PRETTY_FUNCTION ": shared file [%s] is not big enough! (found %lld, need %d bytes)", shared_file (), statbuf.st_size, wanted_size);
#else
		g_critical (G_GNUC_PRETTY_FUNCTION ": shared file [%s] is not big enough! (found %ld, need %d bytes)", shared_file (), statbuf.st_size, wanted_size);
#endif
		if(fork_daemon==TRUE) {
			_wapi_shm_destroy ();
		}
		return(NULL);
	}
	
	shm_seg=mmap (NULL, statbuf.st_size, PROT_READ|PROT_WRITE, MAP_SHARED,
		      fd, 0);
	if(shm_seg==MAP_FAILED) {
		g_critical (G_GNUC_PRETTY_FUNCTION ": mmap error: %s",
			    g_strerror (errno));
		if(fork_daemon==TRUE) {
			_wapi_shm_destroy ();
		}
		return(NULL);
	}
	close (fd);
		
	data=shm_seg;

	if(fork_daemon==TRUE) {
		pid_t pid;
			
		pid=fork ();
		if(pid==-1) {
			g_critical (G_GNUC_PRETTY_FUNCTION ": fork error: %s",
				    strerror (errno));
			_wapi_shm_destroy ();
			return(NULL);
		} else if (pid==0) {
			int i;
			
			/* child */
			setsid ();
			
			/* FIXME: Clean up memory.  We can delete all
			 * the managed data
			 */
			/* FIXME2: Set process title to something
			 * informative
			 */

			/* Start the daemon with a clean sheet of file
			 * descriptors
			 */
			for(i=3; i<getdtablesize (); i++) {
				close (i);
			}
			
			/* _wapi_daemon_main() does not return */
			_wapi_daemon_main (data);
			
			/* But just in case... */
			data->daemon_running=DAEMON_DIED_AT_STARTUP;
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
		if(!(data->daemon_running==DAEMON_STARTING || 
		     data->daemon_running==DAEMON_RUNNING ||
		     data->daemon_running==DAEMON_DIED_AT_STARTUP) ||
#ifdef NEED_LINK_UNLINK
		   (strncmp (data->daemon, "/tmp/mono-handle-daemon-",
			     24)!=0)) {
#else
		   (strncmp (data->daemon+1, "mono-handle-daemon-", 19)!=0)) {
#endif
			g_warning ("Shared memory sanity check failed.");
			return(NULL);
		}
	}
		
	for(tries=0; data->daemon_running==DAEMON_STARTING && tries < 100;
	    tries++) {
		/* wait for the daemon to sort itself out.  To be
		 * completely safe, we should have a timeout before
		 * giving up.
		 */
		struct timespec sleepytime;
			
		sleepytime.tv_sec=0;
		sleepytime.tv_nsec=10000000;	/* 10ms */
			
		nanosleep (&sleepytime, NULL);
	}
	if(tries==100 && data->daemon_running==DAEMON_STARTING) {
		/* Daemon didnt get going */
		if(fork_daemon==TRUE) {
			_wapi_shm_destroy ();
		}
		g_warning ("The handle daemon didnt start up properly");
		return(NULL);
	}
	
	if(data->daemon_running==DAEMON_DIED_AT_STARTUP) {
		/* Oh dear, the daemon had an error starting up */
		if(fork_daemon==TRUE) {
			_wapi_shm_destroy ();
		}
		g_warning ("Handle daemon failed to start");
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
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": unlinking %s", shared_file ());
#endif
	unlink (shared_file ());
#endif /* DISABLE_SHARED_HANDLES */
}

