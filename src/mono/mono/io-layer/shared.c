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

/* Define this to make it easier to run valgrind on the daemon.  Then
 * the first process to start will turn into a daemon without forking
 * (the debug utility mono/handles/hps is ideal for this.)
 */
#undef VALGRINDING

guchar *_wapi_shm_file (_wapi_shm_t type, guint32 segment)
{
	static guchar file[_POSIX_PATH_MAX];
	guchar *name = NULL, *filename, *dir, *wapi_dir;
	gchar machine_name[256];

	if (gethostname(machine_name, sizeof(machine_name)) != 0)
		machine_name[0] = '\0';
	
	/* Change the filename whenever the format of the contents
	 * changes
	 */
	if(type==WAPI_SHM_DATA) {
		name=g_strdup_printf ("shared_data-%s-%d-%d",
				      machine_name, _WAPI_HANDLE_VERSION, segment);
	} else if (type==WAPI_SHM_SCRATCH) {
		name=g_strdup_printf ("shared_scratch-%s-%d-%d",
				      machine_name, _WAPI_HANDLE_VERSION, segment);
	} else {
		g_assert_not_reached ();
	}

	/* I don't know how nfs affects mmap.  If mmap() of files on
	 * nfs mounts breaks, then there should be an option to set
	 * the directory.
	 */
	wapi_dir=getenv ("MONO_SHARED_DIR");
	if(wapi_dir==NULL) {
		filename=g_build_filename (g_get_home_dir (), ".wapi", name,
					   NULL);
	} else {
		filename=g_build_filename (wapi_dir, ".wapi", name, NULL);
	}
	g_free (name);

	g_snprintf (file, _POSIX_PATH_MAX, "%s", filename);
	g_free (filename);
		
	/* No need to check if the dir already exists or check
	 * mkdir() errors, because on any error the open() call will
	 * report the problem.
	 */
	dir=g_path_get_dirname (file);
	mkdir (dir, 0755);
	g_free (dir);
	
	return(file);
}

gpointer _wapi_shm_file_expand (gpointer mem, _wapi_shm_t type,
				guint32 segment, guint32 old_len,
				guint32 new_len)
{
	int fd;
	gpointer new_mem;
	guchar *filename=_wapi_shm_file (type, segment);
	int ret;

	if(old_len>=new_len) {
		return(mem);
	}
	
	munmap (mem, old_len);
	
	fd=open (filename, O_RDWR, 0600);
	if(fd==-1) {
		g_critical (G_GNUC_PRETTY_FUNCTION
			    ": shared file [%s] open error: %s", filename,
			    g_strerror (errno));
		return(NULL);
	}

	if(lseek (fd, new_len-1, SEEK_SET)==-1) {
		g_critical (G_GNUC_PRETTY_FUNCTION
			    ": shared file [%s] lseek error: %s", filename,
			    g_strerror (errno));
		return(NULL);
	}
	
	do {
		ret=write (fd, "", 1);
	}
	while (ret==-1 && errno==EINTR);

	if(ret==-1) {
		g_critical (G_GNUC_PRETTY_FUNCTION
			    ": shared file [%s] write error: %s", filename,
			    g_strerror (errno));
		return(NULL);
	}

	close (fd);
	
	new_mem=_wapi_shm_file_map (type, segment, NULL, NULL);
	
	return(new_mem);
}

static int _wapi_shm_file_open (const guchar *filename, _wapi_shm_t type,
				gboolean *created)
{
	int fd;
	struct stat statbuf;
	guint32 wanted_size = 0;
	int ret;
	
	if(created) {
		*created=FALSE;
	}
	
	if(type==WAPI_SHM_DATA) {
		wanted_size=sizeof(struct _WapiHandleShared_list);
	} else if (type==WAPI_SHM_SCRATCH) {
		wanted_size=sizeof(struct _WapiHandleScratch) + 
				(_WAPI_SHM_SCRATCH_SIZE - MONO_ZERO_ARRAY_LENGTH);
	} else {
		g_assert_not_reached ();
	}
	
try_again:
	/* No O_CREAT yet, because we need to initialise the file if
	 * we have to create it.
	 */
	fd=open (filename, O_RDWR, 0600);
	if(fd==-1 && errno==ENOENT) {
		/* OK, its up to us to create it.  O_EXCL to avoid a
		 * race condition where two processes can
		 * simultaneously try and create the file
		 */
		fd=open (filename, O_CREAT|O_EXCL|O_RDWR, 0600);
		if(fd==-1 && errno==EEXIST) {
			/* It's possible that the file was created in
			 * between finding it didn't exist, and trying
			 * to create it.  Just try opening it again
			 */
			goto try_again;
		} else if (fd==-1) {
			g_critical (G_GNUC_PRETTY_FUNCTION
				    ": shared file [%s] open error: %s",
				    filename, g_strerror (errno));
			return(-1);
		} else {
			/* We created the file, so we need to expand
			 * the file and inform the caller so it can
			 * fork the handle daemon too.
			 *
			 * (wanted_size-1, because we're about to
			 * write the other byte to actually expand the
			 * file.)
			 */
			if(lseek (fd, wanted_size-1, SEEK_SET)==-1) {
				g_critical (G_GNUC_PRETTY_FUNCTION ": shared file [%s] lseek error: %s", filename, g_strerror (errno));
				close (fd);
				unlink (filename);
				return(-1);
			}
			
			do {
				ret=write (fd, "", 1);
			}
			while (ret==-1 && errno==EINTR);
				
			if(ret==-1) {
				g_critical (G_GNUC_PRETTY_FUNCTION ": shared file [%s] write error: %s", filename, g_strerror (errno));
				close (fd);
				unlink (filename);
				return(-1);
			}
			
			if(created) {
				*created=TRUE;
			}

			/* The contents of the file is set to all
			 * zero, because it is opened up with lseek,
			 * so we don't need to do any more
			 * initialisation here
			 */
		}
	} else if(fd==-1) {
		g_critical (G_GNUC_PRETTY_FUNCTION
			    ": shared file [%s] open error: %s", filename,
			    g_strerror (errno));
		return(-1);
	}
	
	/* From now on, we need to delete the file before exiting on
	 * error if we created it (ie, if *created==TRUE)
	 */

	/* Use stat to find the file size (instead of hard coding it)
	 * because we can expand the file later if needed (for more
	 * handles or scratch space.)
	 */
	if(fstat (fd, &statbuf)==-1) {
		g_critical (G_GNUC_PRETTY_FUNCTION ": fstat error: %s",
			    g_strerror (errno));
		if(created && *created==TRUE) {
			unlink (filename);
		}
		close (fd);
		return(-1);
	}

	if(statbuf.st_size < wanted_size) {
		close (fd);
		if(created && *created==TRUE) {
#ifdef HAVE_LARGE_FILE_SUPPORT
			/* Keep gcc quiet... */
			g_critical (G_GNUC_PRETTY_FUNCTION ": shared file [%s] is not big enough! (found %lld, need %d bytes)", filename, statbuf.st_size, wanted_size);
#else
			g_critical (G_GNUC_PRETTY_FUNCTION ": shared file [%s] is not big enough! (found %ld, need %d bytes)", filename, statbuf.st_size, wanted_size);
#endif
			unlink (filename);
			return(-1);
		} else {
			/* We didn't create it, so just try opening it again */
			goto try_again;
		}
	}
	
	return(fd);
}

gpointer _wapi_shm_file_map (_wapi_shm_t type, guint32 segment,
			     gboolean *created, off_t *size)
{
	gpointer shm_seg;
	int fd;
	struct stat statbuf;
	guchar *filename=_wapi_shm_file (type, segment);
	
	fd=_wapi_shm_file_open (filename, type, created);
	if(fd==-1) {
		g_critical (G_GNUC_PRETTY_FUNCTION
			    ": shared file [%s] open error", filename);
		return(NULL);
	}
	
	if(fstat (fd, &statbuf)==-1) {
		g_critical (G_GNUC_PRETTY_FUNCTION ": fstat error: %s",
			    g_strerror (errno));
		close (fd);
		return(NULL);
	}
	if(size) {
		*size=statbuf.st_size;
	}
	
	shm_seg=mmap (NULL, statbuf.st_size, PROT_READ|PROT_WRITE, MAP_SHARED,
		      fd, 0);
	if(shm_seg==MAP_FAILED) {
		g_critical (G_GNUC_PRETTY_FUNCTION ": mmap error: %s",
			    g_strerror (errno));
		close (fd);
		return(NULL);
	}
		
	close (fd);
	return(shm_seg);
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
gboolean _wapi_shm_attach (struct _WapiHandleShared_list **data,
			   struct _WapiHandleScratch **scratch)
{
	gboolean data_created=FALSE, scratch_created=FALSE;
	off_t data_size, scratch_size;
	int tries, closing_tries=0;

map_again:
	*data=_wapi_shm_file_map (WAPI_SHM_DATA, 0, &data_created, &data_size);
	if(*data==NULL) {
		return(FALSE);
	}
	
	*scratch=_wapi_shm_file_map (WAPI_SHM_SCRATCH, 0, &scratch_created,
				     &scratch_size);
	if(*scratch==NULL) {
		if(data_created) {
			_wapi_shm_destroy ();
		}
		return(FALSE);
	}

	if(scratch_created)
		(*scratch)->data_len = scratch_size - 
				(sizeof(struct _WapiHandleScratch) - MONO_ZERO_ARRAY_LENGTH);

	if(data_created==FALSE && (*data)->daemon_running==DAEMON_CLOSING) {
		/* Daemon is closing down, give it a few ms and try
		 * again.
		 */
		
		struct timespec sleepytime;
			
		/* Something must have gone wrong, so delete the
		 * shared segments and try again.
		 */
		_wapi_shm_destroy ();
		
		munmap (*data, data_size);
		munmap (*scratch, scratch_size);
		
		if(closing_tries++ == 5) {
			/* Still can't get going, so bail out */
			g_warning ("The handle daemon is stuck closing");
			return(FALSE);
		}
		
		sleepytime.tv_sec=0;
		sleepytime.tv_nsec=10000000;	/* 10ms */
			
		nanosleep (&sleepytime, NULL);
		goto map_again;
	}
	
	if(data_created==TRUE) {
#ifdef VALGRINDING
		/* _wapi_daemon_main() does not return */
		_wapi_daemon_main (*data, *scratch);
			
		/* But just in case... */
		(*data)->daemon_running=DAEMON_DIED_AT_STARTUP;
		exit (-1);
#else
		pid_t pid;
			
		pid=fork ();
		if(pid==-1) {
			g_critical (G_GNUC_PRETTY_FUNCTION ": fork error: %s",
				    strerror (errno));
			_wapi_shm_destroy ();
			return(FALSE);
		} else if (pid==0) {
			int i;
			
			/* child */
			setsid ();
			
			/* FIXME: Set process title to something
			 * informative
			 */

			/* Start the daemon with a clean sheet of file
			 * descriptors
			 */
			for(i=3; i<getdtablesize (); i++) {
				close (i);
			}
			
			/* _wapi_daemon_main() does not return */
			_wapi_daemon_main (*data, *scratch);
			
			/* But just in case... */
			(*data)->daemon_running=DAEMON_DIED_AT_STARTUP;
			exit (-1);
		}
		/* parent carries on */
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Daemon pid %d", pid);
#endif
#endif /* !VALGRINDING */
	}
		
	for(tries=0; (*data)->daemon_running==DAEMON_STARTING && tries < 100;
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
	if(tries==100 && (*data)->daemon_running==DAEMON_STARTING) {
		/* Daemon didnt get going */
		struct timespec sleepytime;
			
		/* Something must have gone wrong, so delete the
		 * shared segments and try again.
		 */
		_wapi_shm_destroy ();

		/* Daemon didn't get going, give it a few ms and try
		 * again.
		 */
		
		munmap (*data, data_size);
		munmap (*scratch, scratch_size);
		
		if(closing_tries++ == 5) {
			/* Still can't get going, so bail out */
			g_warning ("The handle daemon didnt start up properly");
			return(FALSE);
		}
		
		sleepytime.tv_sec=0;
		sleepytime.tv_nsec=10000000;	/* 10ms */
			
		nanosleep (&sleepytime, NULL);
		goto map_again;
	}
	
	if((*data)->daemon_running==DAEMON_DIED_AT_STARTUP) {
		/* Oh dear, the daemon had an error starting up */
		if(data_created==TRUE) {
			_wapi_shm_destroy ();
		}
		g_warning ("Handle daemon failed to start");
		return(FALSE);
	}

	/* Do some sanity checking on the shared memory we
	 * attached
	 */
	if(((*data)->daemon_running!=DAEMON_RUNNING) ||
#ifdef NEED_LINK_UNLINK
	   (strncmp ((*data)->daemon, "/tmp/mono-handle-daemon-",
		     24)!=0)) {
#else
	   (strncmp ((*data)->daemon+1, "mono-handle-daemon-", 19)!=0)) {
#endif
		g_warning ("Shared memory sanity check failed.");
		g_warning("status: %d", (*data)->daemon_running);
#ifdef NEED_LINK_UNLINK
		g_warning("daemon: [%s]", (*data)->daemon);
#else
		g_warning("daemon: [%s]", (*data)->daemon+1);
#endif
		return(FALSE);
	}
		
	/* From now on, it's up to the daemon to delete the shared
	 * memory segment
	 */
	
	return(TRUE);
}

void _wapi_shm_destroy (void)
{
#ifndef DISABLE_SHARED_HANDLES
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": unlinking shared data");
#endif
	/* Only delete the first segments.  The daemon will destroy
	 * any others when it exits
	 */
	unlink (_wapi_shm_file (WAPI_SHM_DATA, 0));
	unlink (_wapi_shm_file (WAPI_SHM_SCRATCH, 0));
#endif /* DISABLE_SHARED_HANDLES */
}

