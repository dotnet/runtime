/*
 * shared.c:  Shared memory handling, and daemon launching
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Novell, Inc.
 */


#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <errno.h>
#include <string.h>
#include <unistd.h>

#if defined(HAVE_SYS_SEM_H) && !(defined(__native_client__) && defined(__GLIBC__))
#  include <sys/sem.h>
#else
#  define DISABLE_SHARED_HANDLES
#endif

#ifndef DISABLE_SHARED_HANDLES
#  include <sys/mman.h>
#  include <sys/ipc.h>
#  ifdef HAVE_SYS_UTSNAME_H
#    include <sys/utsname.h>
#  endif
#endif

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/shared.h>
#include <mono/io-layer/handles-private.h>

#define DEBUGLOG(...)
//#define DEBUGLOG(...) g_message(__VA_ARGS__);

// Semaphores used when no-shared-memory use is in use

static mono_mutex_t noshm_sems[_WAPI_SHARED_SEM_COUNT];

gboolean _wapi_shm_disabled = TRUE;

static gpointer wapi_storage [16];

static void
noshm_semaphores_init (void)
{
	int i;

	for (i = 0; i < _WAPI_SHARED_SEM_COUNT; i++) 
		mono_mutex_init (&noshm_sems [i]);
}

static int
noshm_sem_lock (int sem)
{
	int ret;
	
	DEBUGLOG ("%s: locking nosem %d", __func__, sem);
	
	ret = mono_mutex_lock (&noshm_sems[sem]);
	
	return ret;
}

static int
noshm_sem_trylock (int sem)
{
	int ret;
	
	DEBUGLOG ("%s: trying to lock nosem %d", __func__, sem);
	
	ret = mono_mutex_trylock (&noshm_sems[sem]);
	
	return ret;
}

static int
noshm_sem_unlock (int sem)
{
	int ret;
	
	DEBUGLOG ("%s: unlocking nosem %d", __func__, sem);
	
	ret = mono_mutex_unlock (&noshm_sems[sem]);
	
	return ret;
}

#ifdef DISABLE_SHARED_HANDLES
void
_wapi_shm_semaphores_init (void)
{
	noshm_semaphores_init ();
}

void
_wapi_shm_semaphores_remove (void)
{
	/* Nothing */
}

int
_wapi_shm_sem_lock (int sem)
{
	return noshm_sem_lock (sem);
}

int
_wapi_shm_sem_trylock (int sem)
{
	return noshm_sem_trylock (sem);
}

int
_wapi_shm_sem_unlock (int sem)
{
	return noshm_sem_unlock (sem);
}

gpointer
_wapi_shm_attach (_wapi_shm_t type)
{
	gpointer res;

	switch(type) {
	case WAPI_SHM_DATA:
		res = g_malloc0 (sizeof(struct _WapiHandleSharedLayout));
		break;
	case WAPI_SHM_FILESHARE:
		res = g_malloc0 (sizeof(struct _WapiFileShareLayout));
		break;
	default:
		g_error ("Invalid type in _wapi_shm_attach ()");
		return NULL;
	}

	wapi_storage [type] = res;
	return res;
}

void
_wapi_shm_detach (_wapi_shm_t type)
{
	g_free (wapi_storage [type]);
}

gboolean
_wapi_shm_enabled (void)
{
	return FALSE;
}

#else
/*
 * Use POSIX shared memory if possible, it is simpler, and it has the advantage that 
 * writes to the shared area does not need to be written to disk, avoiding spinning up 
 * the disk every x secs on laptops.
 */
#ifdef HAVE_SHM_OPEN
#define USE_SHM 1
#endif

static gchar *
_wapi_shm_base_name (_wapi_shm_t type)
{
	gchar *name = NULL;
	gchar machine_name[256];
	const gchar *fake_name;
	struct utsname ubuf;
	int ret;
	int len;
	
	ret = uname (&ubuf);
	if (ret == -1) {
		ubuf.machine[0] = '\0';
		ubuf.sysname[0] = '\0';
	} else {
		g_strdelimit (ubuf.sysname, "/", '_');
		g_strdelimit (ubuf.machine, "/", '_');
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

	return name;
}

#ifdef USE_SHM

static gchar *_wapi_shm_shm_name (_wapi_shm_t type)
{
	char *base_name = _wapi_shm_base_name (type);

	/* Also add the uid to avoid permission problems */
	char *res = g_strdup_printf ("/mono-shared-%d-%s", getuid (), base_name);

	g_free (base_name);

	return res;
}

static int
_wapi_shm_open (const char *filename, int size)
{
	int fd;

	fd = shm_open (filename, O_CREAT|O_RDWR, S_IRUSR|S_IWUSR|S_IRGRP);
	if (fd == -1)
		/* Maybe /dev/shm is not mounted */
		return -1;
	if (ftruncate (fd, size) != 0) {
		perror ("_wapi_shm_open (): ftruncate ()");
		g_assert_not_reached ();
	}

	return fd;
}

#endif

static gchar *
_wapi_shm_file (_wapi_shm_t type)
{
	static gchar file[_POSIX_PATH_MAX];
	gchar *name = NULL, *filename, *wapi_dir;

	name = _wapi_shm_base_name (type);

	/* I don't know how nfs affects mmap.  If mmap() of files on
	 * nfs mounts breaks, then there should be an option to set
	 * the directory.
	 */
	wapi_dir = g_getenv ("MONO_SHARED_DIR");
	if (wapi_dir == NULL) {
		filename = g_build_filename (g_get_home_dir (), ".wapi", name,
					     NULL);
	} else {
		filename = g_build_filename (wapi_dir, ".wapi", name, NULL);
	}
	g_free (name);

	g_snprintf (file, _POSIX_PATH_MAX, "%s", filename);
	g_free (filename);
	
	return file;
}

static int
_wapi_shm_file_open (const gchar *filename, guint32 wanted_size)
{
	int fd;
	struct stat statbuf;
	int ret, tries = 0;
	gboolean created = FALSE;
	mode_t oldmask;
	gchar *dir;
		
	/* No need to check if the dir already exists or check
	 * mkdir() errors, because on any error the open() call will
	 * report the problem.
	 */
	dir = g_path_get_dirname (filename);
	mkdir (dir, 0755);
	g_free (dir);

try_again:
	if (tries++ > 10) {
		/* Just give up */
		return (-1);
	} else if (tries > 5) {
		/* Break out of a loop */
		unlink (filename);
	}
	
	/* Make sure future processes can open the shared data files */
	oldmask = umask (066);

	/* No O_CREAT yet, because we need to initialise the file if
	 * we have to create it.
	 */
	fd = open (filename, O_RDWR, 0600);
	umask (oldmask);
	
	if (fd == -1 && errno == ENOENT) {
		/* OK, its up to us to create it.  O_EXCL to avoid a
		 * race condition where two processes can
		 * simultaneously try and create the file
		 */
		oldmask = umask (066);
		fd = open (filename, O_CREAT|O_EXCL|O_RDWR, 0600);
		umask (oldmask);
		
		if (fd == -1 && errno == EEXIST) {
			/* It's possible that the file was created in
			 * between finding it didn't exist, and trying
			 * to create it.  Just try opening it again
			 */
			goto try_again;
		} else if (fd == -1) {
			g_critical ("%s: shared file [%s] open error: %s",
				    __func__, filename, g_strerror (errno));
			return -1;
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
				return -1;
			}
			
			do {
				ret = write (fd, "", 1);
			} while (ret == -1 && errno == EINTR);
				
			if (ret == -1) {
				g_critical ("%s: shared file [%s] write error: %s", __func__, filename, g_strerror (errno));
				close (fd);
				unlink (filename);
				return -1;
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
		return -1;
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
		return -1;
	}

	if (statbuf.st_size < wanted_size) {
		close (fd);
		if (created == TRUE) {
			g_critical ("%s: shared file [%s] is not big enough! (found %ld, need %d bytes)", __func__, filename, (long)statbuf.st_size, wanted_size);
			unlink (filename);
			return -1;
		} else {
			/* We didn't create it, so just try opening it again */
			_wapi_handle_spin (100);
			goto try_again;
		}
	}
	
	return fd;
}

gboolean
_wapi_shm_enabled (void)
{
	static gboolean env_checked;

	if (!env_checked) {
		if (g_getenv ("MONO_ENABLE_SHM"))
			_wapi_shm_disabled = FALSE;
		env_checked = TRUE;
	}

	return !_wapi_shm_disabled;
}

/*
 * _wapi_shm_attach:
 * @success: Was it a success
 *
 * Attach to the shared memory file or create it if it did not exist.
 * Returns the memory area the file was mmapped to.
 */
gpointer
_wapi_shm_attach (_wapi_shm_t type)
{
	gpointer shm_seg;
	int fd;
	struct stat statbuf;
	gchar *filename = _wapi_shm_file (type), *shm_name;
	guint32 size;
	
	switch(type) {
	case WAPI_SHM_DATA:
		size = sizeof(struct _WapiHandleSharedLayout);
		break;
		
	case WAPI_SHM_FILESHARE:
		size = sizeof(struct _WapiFileShareLayout);
		break;
	default:
		g_error ("Invalid type in _wapi_shm_attach ()");
		return NULL;
	}

	if (!_wapi_shm_enabled ()) {
		wapi_storage [type] = g_malloc0 (size);
		return wapi_storage [type];
	}

#ifdef USE_SHM
	shm_name = _wapi_shm_shm_name (type);
	fd = _wapi_shm_open (shm_name, size);
	g_free (shm_name);
#else
	fd = -1;
#endif

	/* Fall back to files if POSIX shm fails (for example, because /dev/shm is not mounted */
	if (fd == -1)
		fd = _wapi_shm_file_open (filename, size);
	if (fd == -1) {
		g_critical ("%s: shared file [%s] open error", __func__,
			    filename);
		return NULL;
	}

	if (fstat (fd, &statbuf)==-1) {
		g_critical ("%s: fstat error: %s", __func__,
			    g_strerror (errno));
		close (fd);
		return NULL;
	}
	
	shm_seg = mmap (NULL, statbuf.st_size, PROT_READ|PROT_WRITE,
			MAP_SHARED, fd, 0);
	if (shm_seg == MAP_FAILED) {
		shm_seg = mmap (NULL, statbuf.st_size, PROT_READ|PROT_WRITE,
			MAP_PRIVATE, fd, 0);
		if (shm_seg == MAP_FAILED) {
			g_critical ("%s: mmap error: %s", __func__, g_strerror (errno));
			close (fd);
			return NULL;
		}
	}
		
	close (fd);
	return shm_seg;
}

void
_wapi_shm_detach (_wapi_shm_t type)
{
	if (!_wapi_shm_enabled ())
		g_free (wapi_storage [type]);
}

static void
shm_semaphores_init (void)
{
	key_t key;
	key_t oldkey;
	int thr_ret;
	struct _WapiHandleSharedLayout *tmp_shared;
	gchar *ftmp;
	gchar *filename;
	
	/*
	 * Yet more barmy API - this union is a well-defined parameter
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

	/*
	 * Process count must start at '0' - the 1 for all the others
	 * sets the semaphore to "unlocked"
	 */
	def_vals[_WAPI_SHARED_SEM_PROCESS_COUNT] = 0;
	
	defs.array = def_vals;
	
	/*
	 *Temporarily attach the shared data so we can read the
	 * semaphore key.  We release this mapping and attach again
	 * after getting the semaphores to avoid a race condition
	 * where a terminating process can delete the shared files
	 * between a new process attaching the file and getting access
	 * to the semaphores (which increments the process count,
	 * preventing destruction of the shared data...)
	 */
	tmp_shared = _wapi_shm_attach (WAPI_SHM_DATA);
	g_assert (tmp_shared != NULL);
	
#ifdef USE_SHM
	ftmp=_wapi_shm_shm_name (WAPI_SHM_DATA);
	filename = g_build_filename ("/dev/shm", ftmp, NULL);
	g_assert (filename!=NULL);
	key = ftok (filename, 'M');
	g_free (ftmp);
	g_free (filename);
#else
	key = ftok ( _wapi_shm_file (WAPI_SHM_DATA), 'M');
#endif

again:
	retries++;
	oldkey = tmp_shared->sem_key;

	if (oldkey == 0) {
		DEBUGLOG ("%s: Creating with new key (0x%x)", __func__, key);

		/*
		 * The while loop attempts to make some sense of the
		 * bonkers 'think of a random number' method of
		 * picking a key without collision with other
		 * applications
		 */
		while ((_wapi_sem_id = semget (key, _WAPI_SHARED_SEM_COUNT,
					       IPC_CREAT | IPC_EXCL | 0600)) == -1) {
			if (errno == ENOMEM) {
				g_error ("%s: semget error: %s", __func__,
					    g_strerror (errno));
			} else if (errno == ENOSPC) {
				g_error ("%s: semget error: %s.  Try deleting some semaphores with ipcs and ipcrm\nor increase the maximum number of semaphore in the system.", __func__, g_strerror (errno));
			} else if (errno != EEXIST) {
				if (retries > 3)
					g_warning ("%s: semget error: %s key 0x%x - trying again", __func__,
							g_strerror (errno), key);
			}
			
			key++;
			DEBUGLOG ("%s: Got (%s), trying with new key (0x%x)", __func__, g_strerror (errno), key);
		}
		/*
		 * Got a semaphore array, so initialise it and install
		 * the key into the shared memory
		 */
		
		if (semctl (_wapi_sem_id, 0, SETALL, defs) == -1) {
			if (retries > 3)
				g_warning ("%s: semctl init error: %s - trying again", __func__, g_strerror (errno));

			/*
			 * Something went horribly wrong, so try
			 * getting a new set from scratch
			 */
			semctl (_wapi_sem_id, 0, IPC_RMID);
			goto again;
		}

		if (InterlockedCompareExchange (&tmp_shared->sem_key,
						key, 0) != 0) {
			/*
			 * Someone else created one and installed the
			 * key while we were working, so delete the
			 * array we created and fall through to the
			 * 'key already known' case.
			 */
			semctl (_wapi_sem_id, 0, IPC_RMID);
			oldkey = tmp_shared->sem_key;
		} else {
			/*
			 * We've installed this semaphore set's key into
			 * the shared memory
			 */
			goto done;
		}
	}
	
	DEBUGLOG ("%s: Trying with old key 0x%x", __func__, oldkey);

	_wapi_sem_id = semget (oldkey, _WAPI_SHARED_SEM_COUNT, 0600);
	if (_wapi_sem_id == -1) {
		if (retries > 3)
			g_warning ("%s: semget error opening old key 0x%x (%s) - trying again",
					__func__, oldkey,g_strerror (errno));

		/*
		 * Someone must have deleted the semaphore set, so
		 * blow away the bad key and try again
		 */
		InterlockedCompareExchange (&tmp_shared->sem_key, 0, oldkey);
		
		goto again;
	}

  done:
	/* Increment the usage count of this semaphore set */
	thr_ret = _wapi_shm_sem_lock (_WAPI_SHARED_SEM_PROCESS_COUNT_LOCK);
	g_assert (thr_ret == 0);
	
	DEBUGLOG ("%s: Incrementing the process count (%d)", __func__, _wapi_getpid ());

	/*
	 * We only ever _unlock_ this semaphore, letting the kernel
	 * restore (ie decrement) this unlock when this process exits.
	 * We lock another semaphore around it so we can serialise
	 * access when we're testing the value of this semaphore when
	 * we exit cleanly, so we can delete the whole semaphore set.
	 */
	_wapi_shm_sem_unlock (_WAPI_SHARED_SEM_PROCESS_COUNT);

	DEBUGLOG ("%s: Process count is now %d (%d)", __func__, semctl (_wapi_sem_id, _WAPI_SHARED_SEM_PROCESS_COUNT, GETVAL), _wapi_getpid ());
	
	_wapi_shm_sem_unlock (_WAPI_SHARED_SEM_PROCESS_COUNT_LOCK);

	if (_wapi_shm_disabled)
		g_free (tmp_shared);
	else
		munmap (tmp_shared, sizeof(struct _WapiHandleSharedLayout));
}

static void
shm_semaphores_remove (void)
{
	int thr_ret;
	int proc_count;
	gchar *shm_name;
	
	DEBUGLOG ("%s: Checking process count (%d)", __func__, _wapi_getpid ());
	
	thr_ret = _wapi_shm_sem_lock (_WAPI_SHARED_SEM_PROCESS_COUNT_LOCK);
	g_assert (thr_ret == 0);
	
	proc_count = semctl (_wapi_sem_id, _WAPI_SHARED_SEM_PROCESS_COUNT,
			     GETVAL);

	g_assert (proc_count > 0);
	if (proc_count == 1) {
		/*
		 * Just us, so blow away the semaphores and the shared
		 * files
		 */
		DEBUGLOG ("%s: Removing semaphores! (%d)", __func__, _wapi_getpid ());

		semctl (_wapi_sem_id, 0, IPC_RMID);
#ifdef USE_SHM
		shm_name = _wapi_shm_shm_name (WAPI_SHM_DATA);
		shm_unlink (shm_name);
		g_free (shm_name);

		shm_name = _wapi_shm_shm_name (WAPI_SHM_FILESHARE);
		shm_unlink (shm_name);
		g_free (shm_name);
#endif
		unlink (_wapi_shm_file (WAPI_SHM_DATA));
		unlink (_wapi_shm_file (WAPI_SHM_FILESHARE));
	} else {
		/*
		 * "else" clause, because there's no point unlocking
		 * the semaphore if we've just blown it away...
		 */
		_wapi_shm_sem_unlock (_WAPI_SHARED_SEM_PROCESS_COUNT_LOCK);
	}
}

static int
shm_sem_lock (int sem)
{
	struct sembuf ops;
	int ret;
	
	DEBUGLOG ("%s: locking sem %d", __func__, sem);

	ops.sem_num = sem;
	ops.sem_op = -1;
	ops.sem_flg = SEM_UNDO;
	
  retry:
	do {
		ret = semop (_wapi_sem_id, &ops, 1);
	} while (ret == -1 && errno == EINTR);

	if (ret == -1) {
		/*
		 * EINVAL covers the case when the semaphore was
		 * deleted before we started the semop
		 */
		if (errno == EIDRM || errno == EINVAL) {
			/*
			 * Someone blew away this semaphore set, so
			 * get a new one and try again
			 */
			DEBUGLOG ("%s: Reinitialising the semaphores!", __func__);

			_wapi_shm_semaphores_init ();
			goto retry;
		}
		
		/* Turn this into a pthreads-style return value */
		ret = errno;
	}
	
	DEBUGLOG ("%s: returning %d (%s)", __func__, ret, g_strerror (ret));
	
	return ret;
}

static int
shm_sem_trylock (int sem)
{
	struct sembuf ops;
	int ret;
	
	DEBUGLOG ("%s: trying to lock sem %d", __func__, sem);
	
	ops.sem_num = sem;
	ops.sem_op = -1;
	ops.sem_flg = IPC_NOWAIT | SEM_UNDO;
	
  retry:
	do {
		ret = semop (_wapi_sem_id, &ops, 1);
	} while (ret == -1 && errno == EINTR);

	if (ret == -1) {
		/*
		 * EINVAL covers the case when the semaphore was
		 * deleted before we started the semop
		 */
		if (errno == EIDRM || errno == EINVAL) {
			/*
			 * Someone blew away this semaphore set, so
			 * get a new one and try again
			 */
			DEBUGLOG ("%s: Reinitialising the semaphores!", __func__);

			_wapi_shm_semaphores_init ();
			goto retry;
		}
		
		/* Turn this into a pthreads-style return value */
		ret = errno;
	}
	
	if (ret == EAGAIN) {
		/* But pthreads uses this code instead */
		ret = EBUSY;
	}
	
	DEBUGLOG ("%s: returning %d (%s)", __func__, ret, g_strerror (ret));
	
	return ret;
}

static int
shm_sem_unlock (int sem)
{
	struct sembuf ops;
	int ret;
	
	DEBUGLOG ("%s: unlocking sem %d", __func__, sem);
	
	ops.sem_num = sem;
	ops.sem_op = 1;
	ops.sem_flg = SEM_UNDO;
	
  retry:
	do {
		ret = semop (_wapi_sem_id, &ops, 1);
	} while (ret == -1 && errno == EINTR);

	if (ret == -1) {
		/* EINVAL covers the case when the semaphore was
		 * deleted before we started the semop
		 */
		if (errno == EIDRM || errno == EINVAL) {
			/* Someone blew away this semaphore set, so
			 * get a new one and try again (we can't just
			 * assume that the semaphore is now unlocked)
			 */
			DEBUGLOG ("%s: Reinitialising the semaphores!", __func__);

			_wapi_shm_semaphores_init ();
			goto retry;
		}
		
		/* Turn this into a pthreads-style return value */
		ret = errno;
	}
	
	DEBUGLOG ("%s: returning %d (%s)", __func__, ret, g_strerror (ret));

	return ret;
}

void
_wapi_shm_semaphores_init (void)
{
	if (!_wapi_shm_enabled ())
		noshm_semaphores_init ();
	else
		shm_semaphores_init ();
}

void
_wapi_shm_semaphores_remove (void)
{
	if (!_wapi_shm_disabled) 
		shm_semaphores_remove ();
}

int
_wapi_shm_sem_lock (int sem)
{
	if (_wapi_shm_disabled) 
		return noshm_sem_lock (sem);
	else
		return shm_sem_lock (sem);
}

int
_wapi_shm_sem_trylock (int sem)
{
	if (_wapi_shm_disabled) 
		return noshm_sem_trylock (sem);
	else 
		return shm_sem_trylock (sem);
}

int
_wapi_shm_sem_unlock (int sem)
{
	if (_wapi_shm_disabled) 
		return noshm_sem_unlock (sem);
	else 
		return shm_sem_unlock (sem);
}
#endif /* !DISABLE_SHARED_HANDLES */
