/**
 * \file
 * The main entry point for the mono executable
 *
 * The main entry point does a few things:
 *
 *   * It probes whether the executable has a bundle appended
 *     at the end, and if so, registers the various bundled
 *     resources with Mono and executes the contained bundle
 *
 *   * Parses the MONO_ENV_OPTIONS variable to treat the
 *     contents of the variable as command line arguments for
 *     the mono runtime
 *
 *   * Launches Mono, by calling mono_main.
 */
#include <config.h>
#include <fcntl.h>
#ifndef HOST_WIN32
#include <dirent.h>
#endif
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-config.h>
#include <mono/utils/mono-mmap.h>
#include "mini.h"
#include "mini-runtime.h"

#ifdef HAVE_UNISTD_H
#  include <unistd.h>
#endif
#ifdef HOST_WIN32
#  include <io.h>
#else
#  ifndef BUILDVER_INCLUDED
#    include "buildver-boehm.h"
#  endif
#endif
#ifdef TARGET_OSX
#include <mach-o/loader.h>
#endif

//#define TEST_ICALL_SYMBOL_MAP 1

/*
 * If the MONO_ENV_OPTIONS environment variable is set, it uses this as a
 * source of command line arguments that are passed to Mono before the
 * command line arguments specified in the command line.
 */
static int
mono_main_with_options (int argc, char *argv [])
{
	mono_parse_env_options (&argc, &argv);

	return mono_main (argc, argv);
}

/*
 * The Mono executable can initialize itself from a payload attached
 * at the end of the main program.   The payload contains the
 * main assembly, one or more managed assemblies, configuration
 * files and other assets that are used instead of launching a
 * program from the command line.
 *
 * The startup sequence probes for a magical signature at the end of
 * the executable, if the 16 characters "xmonkeysloveplay" are found,
 * the code expects the 64-bits just before it to contain an offset
 * within the executable with a directory of assets.
 *
 * All pointers in the file format are encoded as little-endian values
 *
 * The format of the file is thus:
 *
 * Location        Content
 * --------        -------
 * lenght-16       Optional "xmonkeysloveplay", indicating that a
 *                 bundled payload is contained in the executable.
 * length-24       pointer to the directory in the file, address DIR
 *
 * DIR             32-bit value with the number of entries in the directory
 * DIR+4           First directory entry.
 *
 * Each directory entry is made up of:
 * 4-bytes         uint32_t containing the size of a string (STR)
 * STRING          UTF8 encoded and \0 terminated string
 * 8-bytes         uint64_t offset in the file with the payload associated with STRING
 * 4-bytes         uint32_t size of the asset
 *
 * The following are the known directory entries, without the quotes:
 * "assembly:NAME"  An assembly with the name NAME, assembly is in the payload
 * "config:NAME"    A configuration file (usually file.dll.config) in the payload that is
 *                  loaded as the config file for an assembly
 * "systemconfig:"  Treats as a Mono system configuration, payload contains the config file.
 * "options:"       The payload contains command line options to initialize Mono, as if you
                    had set them on MONO_ENV_OPTIONS
 * "config_dir:DIR" Configures the MONO_PATH to point to point to DIR
 * "machineconfig:" The payload contains the machine.config file to use at runtime
 * "env:"           Sets the environment variable to the value encoded in the payload
 *                  payload contains: 1-byte lenght for the \0 terminated variable,
 *                  followed by the value.
 * "library:NAME"   Bundled dynamic library NAME, payload contains the dynamic library
 */
#define STREAM_INT(x) GUINT32_TO_LE((*(uint32_t*)x))
#define STREAM_LONG(x) GUINT64_TO_LE((*(uint64_t*)x))

#ifndef HOST_WIN32
static gboolean
search_directories(const char *envPath, const char *program, char **new_program)
{
	gchar **paths = NULL;
	gint i;

	paths = g_strsplit (envPath, G_SEARCHPATH_SEPARATOR_S, 0);
	g_assert (paths);

	for (i = 0; paths [i]; ++i) {
		gchar *path = paths [i];
		gint path_len = strlen (path);
		DIR *dir;
		struct dirent *ent;

		if (path_len == 0)
			continue;

		dir = opendir (path);
		if (!dir)
			continue;

		while ((ent = readdir (dir))){
			if (!strcmp (ent->d_name, program)){
				*new_program = g_strdup_printf ("%s%s%s", path, path [path_len - 1] == '/' ? "" : "/", program);
				closedir (dir);
				g_strfreev (paths);
				return TRUE;
			}
		}

		closedir (dir);
	}

	*new_program = NULL;
	g_strfreev (paths);
	return FALSE;
}
#endif

#if TEST_ICALL_SYMBOL_MAP

const char*
mono_lookup_icall_symbol_internal (gpointer func);

ICALL_EXPORT int ves_icall_Interop_Sys_DoubleToString (double, char*, char*, int);

#endif

#ifdef HOST_WIN32

#include <shellapi.h>

#ifdef _WINDOWS
int APIENTRY
wWinMain (HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR lpCmdLine, int nCmdShow)
#else
int
main (int _argc, char* _argv[])
#endif
{
	int argc;
	gunichar2** argvw;
	gchar** argv;
	int i;

	argvw = CommandLineToArgvW (GetCommandLineW (), &argc);
	argv = g_new0 (gchar*, argc + 1);
	for (i = 0; i < argc; i++)
		argv [i] = g_utf16_to_utf8 (argvw [i], -1, NULL, NULL, NULL);
	argv [argc] = NULL;

	LocalFree (argvw);

	return mono_main_with_options  (argc, argv);
}

#else

int
main (int argc, char* argv[])
{
	mono_build_date = build_date;

#if TEST_ICALL_SYMBOL_MAP
	const char *p  = mono_lookup_icall_symbol_internal (mono_lookup_icall_symbol_internal);
	printf ("%s\n", p ? p : "null");
	p  = mono_lookup_icall_symbol_internal (ves_icall_Interop_Sys_DoubleToString);
	printf ("%s\n", p ? p : "null");
#endif

	return mono_main_with_options (argc, argv);
}

#endif
