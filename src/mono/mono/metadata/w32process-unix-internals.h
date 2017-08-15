/**
 * \file
 */

#ifndef _MONO_METADATA_W32PROCESS_UNIX_INTERNALS_H_
#define _MONO_METADATA_W32PROCESS_UNIX_INTERNALS_H_

#include <config.h>
#include <glib.h>

/*
 * FOR EXCLUSIVE USE BY w32process-unix.c
 */

#if defined(HOST_DARWIN)
#define USE_OSX_BACKEND
#elif (defined(__OpenBSD__) || defined(__FreeBSD__)) && defined(HAVE_LINK_H)
#define USE_BSD_BACKEND
#elif defined(__HAIKU__)
#define USE_HAIKU_BACKEND
/* Define header for team_info */
#include <os/kernel/OS.h>
#else
#define USE_DEFAULT_BACKEND
#endif

typedef struct {
	gpointer address_start;
	gpointer address_end;
	gchar *perms;
	gpointer address_offset;
	guint64 device;
	guint64 inode;
	gchar *filename;
} MonoW32ProcessModule;

gchar*
mono_w32process_get_name (pid_t pid);

GSList*
mono_w32process_get_modules (pid_t pid);

static void
mono_w32process_module_free (MonoW32ProcessModule *module)
{
	g_free (module->perms);
	g_free (module->filename);
	g_free (module);
}

/*
 * Used to look through the GSList* returned by mono_w32process_get_modules
 */
static gint
mono_w32process_module_equals (gconstpointer a, gconstpointer b)
{
	MonoW32ProcessModule *want = (MonoW32ProcessModule *)a;
	MonoW32ProcessModule *compare = (MonoW32ProcessModule *)b;
	return (want->device == compare->device && want->inode == compare->inode) ? 0 : 1;
}

#endif /* _MONO_METADATA_W32PROCESS_UNIX_INTERNALS_H_ */
