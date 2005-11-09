/*
 * util.c: Simple runtime tools for the Unix platform
 *
 * Author:
 *   Miguel de Icaza
 *
 * (C) 2002 Ximian, Inc. (http://www.ximian.com)
 */
#include <config.h>
#include <stdio.h>
#include <string.h>
#include <glib.h>
#include <sys/types.h>
#include <unistd.h>
#include <mono/os/util.h>
#include <mono/metadata/assembly.h>

static char *
compute_base (char *path)
{
	char *p = rindex (path, '/');
	if (p == NULL)
		return NULL;
	*p = 0;
	p = rindex (path, '/');
	if (p == NULL)
		return NULL;
	
	if (strcmp (p, "/bin") != 0)
		return NULL;
	*p = 0;
	return path;
}

static void
fallback ()
{
	mono_assembly_setrootdir (MONO_ASSEMBLIES);
	mono_internal_set_config_dir (MONO_CFG_DIR);
}

static void
set_dirs (char *exe)
{
	char *base;
	
	/*
	 * Only /usr prefix is treated specially
	 */
	if (strncmp (exe, MONO_BINDIR, strlen (MONO_BINDIR)) == 0 || (base = compute_base (exe)) == NULL){
		fallback ();
		return;
	} else {
		char *config, *lib;
		config = g_build_filename (base, "etc", NULL);
		lib = g_build_filename (base, "lib", NULL);
		mono_assembly_setrootdir (lib);
		mono_internal_set_config_dir (config);
		g_free (config);
		g_free (lib);
	}
}
	

/**
 * mono_set_rootdir:
 *
 * Registers the root directory for the Mono runtime, for Linux and Solaris 10,
 * this auto-detects the prefix where Mono was installed. 
 */
void
mono_set_rootdir (void)
{
	char buf [4096];
	int  s;
	char *str;

	/* Linux style */
	s = readlink ("/proc/self/exe", buf, sizeof (buf)-1);

	if (s != -1){
		buf [s] = 0;
		set_dirs (buf);
		return;
	}

	/* Solaris 10 style */
	str = g_strdup_printf ("/proc/%d/path/a.out", getpid ());
	s = readlink (str, buf, sizeof (buf)-1);
	g_free (str);
	if (s != -1){
		buf [s] = 0;
		set_dirs (buf);
		return;
	} 
	fallback ();
}

