/*
 * util.c: Simple runtime tools for the Win32 platform
 *
 * Author:
 *   Miguel de Icaza
 *
 * (C) 2002 Ximian, Inc. (http://www.ximian.com)
 */
#include <config.h>
#include <mono/metadata/metadata.h>
#include <mono/os/util.h>

/*
 * mono_set_rootdir:
 * @vm_filename: The pathname of the code invoking us (argv [0])
 *
 * Informs the runtime of the root directory for the Mono installation,
 * the vm_file
 */
void
mono_set_rootdir (const char *vm_filename)
{
	char *dir = g_dirname (vm_filename);
	char *root = g_strconcat (dir, "/../lib", NULL);

	mono_assembly_setrootdir (root);
	g_free (root);
	g_free (dir);
}

 
