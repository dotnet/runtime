/*
 * filewatcher.c: File System Watcher internal calls
 *
 * Authors:
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * (C) 2004,2005,2006 Novell, Inc. (http://www.novell.com)
 */

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/filewatcher.h>

#if (defined (PLATFORM_WIN32) && WINVER >= 0x0400)

/*
 * TODO:
 * We use the managed watcher on windows, so the code inside this #if is never used
 */
gint
ves_icall_System_IO_FSW_SupportsFSW (void)
{
	return 1;
}

gpointer
ves_icall_System_IO_FSW_OpenDirectory (MonoString *path, gpointer reserved)
{
	return NULL;
	/*
	gpointer dir;
	gchar *utf8path;

	MONO_ARCH_SAVE_REGS;

	dir = CreateFile (path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE,
			  NULL, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, NULL);

	return dir;
	*/
}

gboolean
ves_icall_System_IO_FSW_CloseDirectory (gpointer handle)
{
	return FALSE;
	/*
	MONO_ARCH_SAVE_REGS;

	return CloseHandle (handle);
	*/
}

gboolean
ves_icall_System_IO_FSW_ReadDirectoryChanges (gpointer handle,
					      MonoArray *buffer,
					      gboolean includeSubdirs,
					      gint filters,
					      gpointer overlap,
					      gpointer callback)
{
	return FALSE;
	/*
	gpointer dest;
	gint size;
	MonoObject *delegate = (MonoObject *) callback;
	MonoMethod *im;
	LPOVERLAPPED_COMPLETION_ROUTINE func;

	MONO_ARCH_SAVE_REGS;

	size = mono_array_length (buffer);
	dest = mono_array_addr_with_size (buffer, 1, 0);

	im = mono_get_delegate_invoke (mono_object_get_class (delegate));
	func = mono_compile_method (im);
	return FALSE;
	* return ReadDirectoryChanges (handle, dest, size, includeSubdirs, filters,
				     NULL, (LPOVERLAPPED) overlap,
				     func); */
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
	GModule *fam_module;
	gchar *filename;
	int lib_used = 4; /* gamin */
	int inotify_instance;

	MONO_ARCH_SAVE_REGS;

	inotify_instance = ves_icall_System_IO_InotifyWatcher_GetInotifyInstance ();
	if (inotify_instance != -1) {
		close (inotify_instance);
		return 5; /* inotify */
	}

	filename = g_module_build_path (NULL, "libgamin-1.so.0");
	fam_module = g_module_open (filename, G_MODULE_BIND_LAZY);
	g_free (filename);
	if (fam_module == NULL) {
		lib_used = 2; /* FAM */
		filename = g_module_build_path (NULL, "libfam.so.0");
		fam_module = g_module_open (filename, G_MODULE_BIND_LAZY);
		g_free (filename);
	}

	if (fam_module == NULL)
		return 0;

	g_module_symbol (fam_module, "FAMNextEvent", (gpointer *) &FAMNextEvent);
	if (FAMNextEvent == NULL)
		return 0;

	return lib_used;
#endif
}

gpointer
ves_icall_System_IO_FSW_OpenDirectory (MonoString *path, gpointer reserved)
{
	return NULL;
}

gboolean
ves_icall_System_IO_FSW_CloseDirectory (gpointer handle)
{
	return FALSE;
}

gboolean
ves_icall_System_IO_FSW_ReadDirectoryChanges (gpointer handle,
					      MonoArray *buffer,
					      gboolean includeSubdirs,
					      gint filters,
					      gpointer overlap,
					      gpointer callback)
{
	return FALSE;
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

#if !defined(__linux__) || !defined(__NR_inotify_init)
int ves_icall_System_IO_InotifyWatcher_GetInotifyInstance ()
{
	return -1;
}

int ves_icall_System_IO_InotifyWatcher_AddDirectoryWatch (int fd, MonoString *directory, gint32 mask)
{
	return -1;
}

int ves_icall_System_IO_InotifyWatcher_RemoveDirectoryWatch (int fd, int watch_descriptor)
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
	char *str;
	int retval;
	int error;

	if (name == NULL)
		return -1;

	str = mono_string_to_utf8 (name);
	retval = syscall (__NR_inotify_add_watch, fd, str, mask);
	if (retval < 0)
		error = errno;
	g_free (str);
	if (retval < 0)
		errno = error;
	return retval;
}

int
ves_icall_System_IO_InotifyWatcher_RemoveWatch (int fd, gint32 watch_descriptor)
{
	return syscall (__NR_inotify_rm_watch, fd, watch_descriptor);
}
#endif

