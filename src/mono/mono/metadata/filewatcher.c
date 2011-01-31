/*
 * filewatcher.c: File System Watcher internal calls
 *
 * Authors:
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/filewatcher.h>
#include <mono/metadata/marshal.h>
#include <mono/utils/mono-dl.h>
#include <mono/utils/mono-io-portability.h>
#ifdef HOST_WIN32

/*
 * TODO:
 * We use the managed watcher on windows, so the code inside this #if is never used
 */
gint
ves_icall_System_IO_FSW_SupportsFSW (void)
{
	return 1;
}

gboolean
ves_icall_System_IO_FAMW_InternalFAMNextEvent (gpointer conn,
					       MonoString **filename,
					       gint *code,
					       gint *reqnum)
{
	return FALSE;
}

#else

static int (*FAMNextEvent) (gpointer, gpointer);

gint
ves_icall_System_IO_FSW_SupportsFSW (void)
{
#if HAVE_KQUEUE
	return 3;
#else
	MonoDl *fam_module;
	int lib_used = 4; /* gamin */
	int inotify_instance;
	void *iter;
	char *err;

	MONO_ARCH_SAVE_REGS;

	inotify_instance = ves_icall_System_IO_InotifyWatcher_GetInotifyInstance ();
	if (inotify_instance != -1) {
		close (inotify_instance);
		return 5; /* inotify */
	}

	iter = NULL;
	fam_module = mono_dl_open ("libgamin-1.so", MONO_DL_LAZY, NULL);
	if (fam_module == NULL) {
		lib_used = 2; /* FAM */
		iter = NULL;
		fam_module = mono_dl_open ("libfam.so", MONO_DL_LAZY, NULL);
	}

	if (fam_module == NULL)
		return 0;

	err = mono_dl_symbol (fam_module, "FAMNextEvent", (gpointer *) &FAMNextEvent);
	g_free (err);
	if (FAMNextEvent == NULL)
		return 0;

	return lib_used;
#endif
}

/* Almost copied from fam.h. Weird, I know */
typedef struct {
	gint reqnum;
} FAMRequest;

typedef struct FAMEvent {
	gpointer fc;
	FAMRequest fr;
	gchar *hostname;
	gchar filename [PATH_MAX];
	gpointer userdata;
	gint code;
} FAMEvent;

gboolean
ves_icall_System_IO_FAMW_InternalFAMNextEvent (gpointer conn,
					       MonoString **filename,
					       gint *code,
					       gint *reqnum)
{
	FAMEvent ev;

	MONO_ARCH_SAVE_REGS;

	if (FAMNextEvent (conn, &ev) == 1) {
		*filename = mono_string_new (mono_domain_get (), ev.filename);
		*code = ev.code;
		*reqnum = ev.fr.reqnum;
		return TRUE;
	}

	return FALSE;
}
#endif

#if defined(__linux__) && defined(HAVE_SYS_SYSCALL_H) && !defined(__NR_inotify_init)
#  if defined(__i386__)
#     define __NR_inotify_init		291
#  elif defined(__x86_64__)
#     define __NR_inotify_init		253
#  elif defined(__ppc__) || defined(__powerpc__) || defined(__powerpc64__)
#     define __NR_inotify_init		275
#  elif defined (__s390__) || defined (__s390x__)
#     define __NR_inotify_init		284
#  elif defined(__sparc__) || defined (__sparc64__)
#     define __NR_inotify_init		151
#  elif defined (__ia64__)
#     define __NR_inotify_init		1277
#  elif defined (__arm__)
#     define __NR_inotify_init		316
#  elif defined(__alpha__)
#     define __NR_inotify_init		444
#  endif
#ifdef __NR_inotify_init
#  ifndef __NR_inotify_add_watch
#    define __NR_inotify_add_watch (__NR_inotify_init + 1)
#  endif
#  ifndef __NR_inotify_rm_watch
#    define __NR_inotify_rm_watch (__NR_inotify_init + 2)
#  endif
#endif
#endif

#if !defined(__linux__) || !defined(__NR_inotify_init)
int ves_icall_System_IO_InotifyWatcher_GetInotifyInstance ()
{
	return -1;
}

int ves_icall_System_IO_InotifyWatcher_AddWatch (int fd, MonoString *directory, gint32 mask)
{
	return -1;
}

int ves_icall_System_IO_InotifyWatcher_RemoveWatch (int fd, gint32 watch_descriptor)
{
	return -1;
}
#else
#include <errno.h>

int
ves_icall_System_IO_InotifyWatcher_GetInotifyInstance ()
{
	return syscall (__NR_inotify_init);
}

int
ves_icall_System_IO_InotifyWatcher_AddWatch (int fd, MonoString *name, gint32 mask)
{
	char *str, *path;
	int retval;

	MONO_ARCH_SAVE_REGS;

	if (name == NULL)
		return -1;

	str = mono_string_to_utf8 (name);
	path = mono_portability_find_file (str, TRUE);
	if (!path)
		path = str;

	retval = syscall (__NR_inotify_add_watch, fd, path, mask);
	if (retval < 0) {
		switch (errno) {
		case EACCES:
			errno = ERROR_ACCESS_DENIED;
			break;
		case EBADF:
			errno = ERROR_INVALID_HANDLE;
			break;
		case EFAULT:
			errno = ERROR_INVALID_ACCESS;
			break;
		case EINVAL:
			errno = ERROR_INVALID_DATA;
			break;
		case ENOMEM:
			errno = ERROR_NOT_ENOUGH_MEMORY;
			break;
		case ENOSPC:
			errno = ERROR_TOO_MANY_OPEN_FILES;
			break;
		default:
			errno = ERROR_GEN_FAILURE;
			break;
		}
		mono_marshal_set_last_error ();
	}
	if (path != str)
		g_free (path);
	g_free (str);
	return retval;
}

int
ves_icall_System_IO_InotifyWatcher_RemoveWatch (int fd, gint32 watch_descriptor)
{
	return syscall (__NR_inotify_rm_watch, fd, watch_descriptor);
}
#endif

