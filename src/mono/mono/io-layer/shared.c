/*
 * shared.c:  Shared memory handling, and daemon launching
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
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
#include <sys/ipc.h>
#include <sys/sem.h>
#include <sys/utsname.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/handles-private.h>

#undef DEBUG

static guchar *_wapi_shm_file (_wapi_shm_t type)
{
	static guchar file[_POSIX_PATH_MAX];
	guchar *name = NULL, *filename, *dir, *wapi_dir;
	gchar machine_name[256];
	gchar *fake_name;
	struct utsname ubuf;
	int ret;
	int len;
	
	ret = uname (&ubuf);
	if (ret == -1) {
		ubuf.machine[0] = '\0';
		ubuf.sysname[0] = '\0';
	}

	fake_name = g_getenv ("MONO_SHARED_HOSTNAME");
	if (fake_name == NULL) {
		if (gethostname(machine_name, sizeof(machine_name)) != 0)
			machine_name[0] = '\0';
	} else {
		len = MIN (strlen (fake_name), sizeof (machine_name) - 1);
		strncpy (machine_name, fake_name, len);
		machine_name [len] = '\0';
	}
	
	switch (type) {
	case WAPI_SHM_DATA:
		name = g_strdup_printf ("shared_data-%s-%s-%s-%d-%d-%d",
					machine_name, ubuf.sysname,
					ubuf.machine,
					(int) sizeof(struct _WapiHandleShared),
					_WAPI_HANDLE_VERSION, 0);
		break;
		
	case WAPI_SHM_FILESHARE:
		name = g_strdup_printf ("shared_fileshare-%s-%s-%s-%d-%d-%d",
					machine_name, ubuf.sysname,
					ubuf.machine,
					(int) sizeof(struct _WapiFileShare),
					_WAPI_HANDLE_VERSION, 0);
		break;
	}

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
gpointer _wapi_shm_attach (_wapi_shm_t type)
{
	gpointer shm_seg;
	int fd;
	struct stat statbuf;
	guchar *filename=_wapi_shm_file (type);
	guint32 size;
	
	switch(type) {
	case WAPI_SHM_DATA:
		size = sizeof(struct _WapiHandleSharedLayout);
		break;
		
	case WAPI_SHM_FILESHARE:
		size = sizeof(struct _WapiFileShareLayout);
		break;
	}
	
	fd = _wapi_shm_file_open (filename, size);
	if (fd == -1) {
		g_critical ("%s: shared file [%s] open error", __func__,
			    filename);
		return(NULL);
	}
	
	if (fstat (fd, &statbuf)==-1) {
		g_critical ("%s: fstat error: %s", __func__,
			    g_strerror (errno));
		close (fd);
		return(NULL);
	}
	
	shm_seg = mmap (NULL, statbuf.st_size, PROT_READ|PROT_WRITE,
			MAP_SHARED, fd, 0);
	if (shm_seg == MAP_FAILED) {
		shm_seg = mmap (NULL, statbuf.st_size, PROT_READ|PROT_WRITE,
			MAP_PRIVATE, fd, 0);
		if (shm_seg == MAP_FAILED) {
			g_critical ("%s: mmap error: %s", __func__, g_strerror (errno));
			close (fd);
			return(NULL);
		}
	}
		
	close (fd);
	return(shm_seg);
}

void _wapi_shm_semaphores_init ()
{
	key_t key = ftok (_wapi_shm_file (WAPI_SHM_DATA), 'M');
	key_t oldkey;

	/* Yet more barmy API - this union is a well-defined parameter
	 * in a syscall, yet I still have to define it here as it
	 * doesn't appear in a header
	 */
	union semun {
		int val;
		struct semid_ds *buf;
		ushort *array;
	} defs;
	ushort def_vals[_WAPI_SHARED_SEM_COUNT];
	int i;
	int retries = 0;
	
	for (i = 0; i < _WAPI_SHARED_SEM_COUNT; i++) {
		def_vals[i] = 1;
	}
	defs.array = def_vals;
	
again:
	retries++;
	oldkey = _wapi_shared_layout->sem_key;

	if (oldkey == 0) {
#ifdef DEBUG
		g_message ("%s: Creating with new key (0x%x)", __func__, key);
#endif

		/* The while loop attempts to make some sense of the
		 * bonkers 'think of a random number' method of
		 * picking a key without collision with other
		 * applications
		 */
		while ((_wapi_sem_id = semget (key, _WAPI_SHARED_SEM_COUNT,
					       IPC_CREAT | IPC_EXCL | 0600)) == -1) {
			if (errno == ENOMEM) {
				g_critical ("%s: semget error: %s", __func__,
					    g_strerror (errno));
			} else if (errno == ENOSPC) {
				g_critical ("%s: semget error: %s.  Try deleting some semaphores with ipcs and ipcrm", __func__, g_strerror (errno));
			} else if (errno != EEXIST) {
				if (retries > 3)
					g_warning ("%s: semget error: %s key 0x%x - trying again", __func__,
							g_strerror (errno), key);
			}
			
			key++;
#ifdef DEBUG
			g_message ("%s: Got (%s), trying with new key (0x%x)",
				   __func__, g_strerror (errno), key);
#endif
		}
		/* Got a semaphore array, so initialise it and install
		 * the key into the shared memory
		 */
		
		if (semctl (_wapi_sem_id, 0, SETALL, defs) == -1) {
			if (retries > 3)
				g_warning ("%s: semctl init error: %s - trying again", __func__, g_strerror (errno));

			/* Something went horribly wrong, so try
			 * getting a new set from scratch
			 */
			semctl (_wapi_sem_id, 0, IPC_RMID);
			goto again;
		}

		if (InterlockedCompareExchange (&_wapi_shared_layout->sem_key,
						key, 0) != 0) {
			/* Someone else created one and installed the
			 * key while we were working, so delete the
			 * array we created and fall through to the
			 * 'key already known' case.
			 */
			semctl (_wapi_sem_id, 0, IPC_RMID);
			oldkey = _wapi_shared_layout->sem_key;
		} else {
			/* We've installed this semaphore set's key into
			 * the shared memory
			 */
			return;
		}
	}
	
#ifdef DEBUG
	g_message ("%s: Trying with old key 0x%x", __func__, oldkey);
#endif

	_wapi_sem_id = semget (oldkey, _WAPI_SHARED_SEM_COUNT, 0600);
	if (_wapi_sem_id == -1) {
		if (retries > 3)
			g_warning ("%s: semget error opening old key 0x%x (%s) - trying again",
					__func__, oldkey,g_strerror (errno));

		/* Someone must have deleted the semaphore set, so
		 * blow away the bad key and try again
		 */
		InterlockedCompareExchange (&_wapi_shared_layout->sem_key, 0,
					    oldkey);
		
		goto again;
	}
}

int _wapi_shm_sem_lock (int sem)
{
	struct sembuf ops;
	int ret;
	
#ifdef DEBUG
	g_message ("%s: locking sem %d", __func__, sem);
#endif

	ops.sem_num = sem;
	ops.sem_op = -1;
	ops.sem_flg = SEM_UNDO;
	
	do {
		ret = semop (_wapi_sem_id, &ops, 1);
	} while (ret == -1 && errno == EINTR);

	if (ret == -1) {
		/* Turn this into a pthreads-style return value */
		ret = errno;
	}
	
#ifdef DEBUG
	g_message ("%s: returning %d (%s)", __func__, ret, g_strerror (ret));
#endif
	
	return(ret);
}

int _wapi_shm_sem_trylock (int sem)
{
	struct sembuf ops;
	int ret;
	
#ifdef DEBUG
	g_message ("%s: trying to lock sem %d", __func__, sem);
#endif
	
	ops.sem_num = sem;
	ops.sem_op = -1;
	ops.sem_flg = IPC_NOWAIT | SEM_UNDO;
	
	do {
		ret = semop (_wapi_sem_id, &ops, 1);
	} while (ret == -1 && errno == EINTR);

	if (ret == -1) {
		/* Turn this into a pthreads-style return value */
		ret = errno;
	}
	
	if (ret == EAGAIN) {
		/* But pthreads uses this code instead */
		ret = EBUSY;
	}
	
#ifdef DEBUG
	g_message ("%s: returning %d (%s)", __func__, ret, g_strerror (ret));
#endif
	
	return(ret);
}

int _wapi_shm_sem_unlock (int sem)
{
	struct sembuf ops;
	int ret;
	
#ifdef DEBUG
	g_message ("%s: unlocking sem %d", __func__, sem);
#endif
	
	ops.sem_num = sem;
	ops.sem_op = 1;
	ops.sem_flg = SEM_UNDO;
	
	do {
		ret = semop (_wapi_sem_id, &ops, 1);
	} while (ret == -1 && errno == EINTR);

	if (ret == -1) {
		/* Turn this into a pthreads-style return value */
		ret = errno;
	}
	
#ifdef DEBUG
	g_message ("%s: returning %d (%s)", __func__, ret, g_strerror (ret));
#endif

	return(ret);
}

