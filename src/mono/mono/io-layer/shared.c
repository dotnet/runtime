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

#undef DEBUG

static guchar *_wapi_shm_file (void)
{
	static guchar file[_POSIX_PATH_MAX];
	guchar *name = NULL, *filename, *dir, *wapi_dir;
	gchar machine_name[256];

	if (gethostname(machine_name, sizeof(machine_name)) != 0)
		machine_name[0] = '\0';
	
	/* Change the filename whenever the format of the contents
	 * changes
	 */
	name = g_strdup_printf ("shared_data-%s-%d-%d",
				machine_name, _WAPI_HANDLE_VERSION, 0);

	/* I don't know how nfs affects mmap.  If mmap() of files on
	 * nfs mounts breaks, then there should be an option to set
	 * the directory.
	 */
	wapi_dir = getenv ("MONO_SHARED_DIR");
	if (wapi_dir == NULL) {
		filename = g_build_filename (g_get_home_dir (), ".wapi", name,
					     NULL);
	} else {
		filename = g_build_filename (wapi_dir, ".wapi", name, NULL);
	}
	g_free (name);

	g_snprintf (file, _POSIX_PATH_MAX, "%s", filename);
	g_free (filename);
		
	/* No need to check if the dir already exists or check
	 * mkdir() errors, because on any error the open() call will
	 * report the problem.
	 */
	dir = g_path_get_dirname (file);
	mkdir (dir, 0755);
	g_free (dir);
	
	return(file);
}

static guchar *_wapi_fileshare_shm_file (void)
{
	static guchar file[_POSIX_PATH_MAX];
	guchar *name = NULL, *filename, *dir, *wapi_dir;
	gchar machine_name[256];

	if (gethostname(machine_name, sizeof(machine_name)) != 0)
		machine_name[0] = '\0';
	
	/* Change the filename whenever the format of the contents
	 * changes
	 */
	name = g_strdup_printf ("shared_fileshare-%s-%d-%d",
				machine_name, _WAPI_HANDLE_VERSION, 0);

	/* I don't know how nfs affects mmap.  If mmap() of files on
	 * nfs mounts breaks, then there should be an option to set
	 * the directory.
	 */
	wapi_dir = getenv ("MONO_SHARED_DIR");
	if (wapi_dir == NULL) {
		filename = g_build_filename (g_get_home_dir (), ".wapi", name,
					     NULL);
	} else {
		filename = g_build_filename (wapi_dir, ".wapi", name, NULL);
	}
	g_free (name);

	g_snprintf (file, _POSIX_PATH_MAX, "%s", filename);
	g_free (filename);
		
	/* No need to check if the dir already exists or check
	 * mkdir() errors, because on any error the open() call will
	 * report the problem.
	 */
	dir = g_path_get_dirname (file);
	mkdir (dir, 0755);
	g_free (dir);
	
	return(file);
}

static int _wapi_shm_file_open (const guchar *filename, guint32 wanted_size)
{
	int fd;
	struct stat statbuf;
	int ret;
	gboolean created = FALSE;
	
try_again:
	/* No O_CREAT yet, because we need to initialise the file if
	 * we have to create it.
	 */
	fd = open (filename, O_RDWR, 0600);
	if (fd == -1 && errno == ENOENT) {
		/* OK, its up to us to create it.  O_EXCL to avoid a
		 * race condition where two processes can
		 * simultaneously try and create the file
		 */
		fd = open (filename, O_CREAT|O_EXCL|O_RDWR, 0600);
		if (fd == -1 && errno == EEXIST) {
			/* It's possible that the file was created in
			 * between finding it didn't exist, and trying
			 * to create it.  Just try opening it again
			 */
			goto try_again;
		} else if (fd == -1) {
			g_critical ("%s: shared file [%s] open error: %s",
				    __func__, filename, g_strerror (errno));
			return(-1);
		} else {
			/* We created the file, so we need to expand
			 * the file.
			 *
			 * (wanted_size-1, because we're about to
			 * write the other byte to actually expand the
			 * file.)
			 */
			if (lseek (fd, wanted_size-1, SEEK_SET) == -1) {
				g_critical ("%s: shared file [%s] lseek error: %s", __func__, filename, g_strerror (errno));
				close (fd);
				unlink (filename);
				return(-1);
			}
			
			do {
				ret = write (fd, "", 1);
			} while (ret == -1 && errno == EINTR);
				
			if (ret == -1) {
				g_critical ("%s: shared file [%s] write error: %s", __func__, filename, g_strerror (errno));
				close (fd);
				unlink (filename);
				return(-1);
			}
			
			created = TRUE;

			/* The contents of the file is set to all
			 * zero, because it is opened up with lseek,
			 * so we don't need to do any more
			 * initialisation here
			 */
		}
	} else if (fd == -1) {
		g_critical ("%s: shared file [%s] open error: %s", __func__,
			    filename, g_strerror (errno));
		return(-1);
	}
	
	/* Use stat to find the file size (instead of hard coding it)
	 * because we can expand the file later if needed (for more
	 * handles or scratch space.)
	 */
	if (fstat (fd, &statbuf) == -1) {
		g_critical ("%s: fstat error: %s", __func__,
			    g_strerror (errno));
		if (created == TRUE) {
			unlink (filename);
		}
		close (fd);
		return(-1);
	}

	if (statbuf.st_size < wanted_size) {
		close (fd);
		if (created == TRUE) {
#ifdef HAVE_LARGE_FILE_SUPPORT
			/* Keep gcc quiet... */
			g_critical ("%s: shared file [%s] is not big enough! (found %lld, need %d bytes)", __func__, filename, statbuf.st_size, wanted_size);
#else
			g_critical ("%s: shared file [%s] is not big enough! (found %ld, need %d bytes)", __func__, filename, statbuf.st_size, wanted_size);
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

/*
 * _wapi_shm_attach:
 * @success: Was it a success
 *
 * Attach to the shared memory file or create it if it did not exist.
 * Returns the memory area the file was mmapped to.
 */
gpointer _wapi_shm_attach (void)
{
	gpointer shm_seg;
	int fd;
	struct stat statbuf;
	guchar *filename=_wapi_shm_file ();
	
	fd=_wapi_shm_file_open (filename,
				sizeof(struct _WapiHandleSharedLayout));
	if(fd==-1) {
		g_critical ("%s: shared file [%s] open error", __func__,
			    filename);
		return(NULL);
	}
	
	if(fstat (fd, &statbuf)==-1) {
		g_critical ("%s: fstat error: %s", __func__,
			    g_strerror (errno));
		close (fd);
		return(NULL);
	}
	
	shm_seg=mmap (NULL, statbuf.st_size, PROT_READ|PROT_WRITE, MAP_SHARED,
		      fd, 0);
	if(shm_seg==MAP_FAILED) {
		g_critical ("%s: mmap error: %s", __func__,
			    g_strerror (errno));
		close (fd);
		return(NULL);
	}
		
	close (fd);
	return(shm_seg);
}

gpointer _wapi_fileshare_shm_attach (void)
{
	gpointer shm_seg;
	int fd;
	struct stat statbuf;
	guchar *filename=_wapi_fileshare_shm_file ();
	
	fd=_wapi_shm_file_open (filename, sizeof(struct _WapiFileShareLayout));
	if(fd==-1) {
		g_critical ("%s: shared file [%s] open error", __func__,
			    filename);
		return(NULL);
	}
	
	if(fstat (fd, &statbuf)==-1) {
		g_critical ("%s: fstat error: %s", __func__,
			    g_strerror (errno));
		close (fd);
		return(NULL);
	}
	
	shm_seg=mmap (NULL, statbuf.st_size, PROT_READ|PROT_WRITE, MAP_SHARED,
		      fd, 0);
	if(shm_seg==MAP_FAILED) {
		g_critical ("%s: mmap error: %s", __func__,
			    g_strerror (errno));
		close (fd);
		return(NULL);
	}
		
	close (fd);
	return(shm_seg);
}
