/*
 * util.c: Simple runtime tools for the Win32 platform
 *
 * Author:
 *   Miguel de Icaza
 *
 * (C) 2002 Ximian, Inc. (http://www.ximian.com)
 */
#include <config.h>
#include <windows.h>
#include <mono/metadata/metadata.h>
#include <mono/os/util.h>

/*
 * mono_set_rootdir:
 *
 * Informs the runtime of the root directory for the Mono installation,
 * the vm_file
 */
void
mono_set_rootdir (void)
{
	char moddir[MAXPATHLEN], *dir, *root;

	GetModuleFileName (NULL, moddir, sizeof(moddir));
	dir = g_path_get_dirname (moddir);
	root = g_strconcat (dir, "/../lib", NULL);

	mono_assembly_setrootdir (root);
	g_free (root);
	g_free (dir);
}

 
