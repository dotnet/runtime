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

static gboolean
probe_embedded (const char *program, int *ref_argc, char **ref_argv [])
{
	MonoBundledAssembly last = { NULL, 0, 0 };
	char sigbuffer [16+sizeof (uint64_t)];
	gboolean status = FALSE;
	uint64_t directory_location;
	off_t sigstart, baseline = 0;
	uint64_t directory_size;
	char *directory, *p;
	int items, i;
	unsigned char *mapaddress = NULL;
	void *maphandle = NULL;
	GArray *assemblies;
	char *entry_point = NULL;
	char **new_argv;
	int j;

	int fd = open (program, O_RDONLY);
#ifndef HOST_WIN32
	if (fd == -1){
		// Also search through the PATH in case the program was run from a different directory
		gchar* envPath = getenv ("PATH");
		if (envPath){
			gchar *new_program = NULL;
			if (search_directories (envPath, program, &new_program)){
				fd = open (new_program, O_RDONLY);
				g_free (new_program);
				new_program = NULL;
			}
		}
	}
#endif
	if (fd == -1)
		return FALSE;
	if ((sigstart = lseek (fd, -24, SEEK_END)) == -1)
		goto doclose;
	if (read (fd, sigbuffer, sizeof (sigbuffer)) == -1)
		goto doclose;
	// First, see if "xmonkeysloveplay" is at the end of file
	if (memcmp (sigbuffer + sizeof (uint64_t), "xmonkeysloveplay", 16) == 0)
		goto found;

#ifdef TARGET_OSX
	{
		/*
		 * If "xmonkeysloveplay" is not at the end of file,
		 * on Mac OS X, we try a little harder, by actually
		 * reading the binary's header structure, to see
		 * if it is located at the end of a LC_SYMTAB section.
		 *
		 * This is because Apple code-signing appends a
		 * LC_CODE_SIGNATURE section to the binary, so
		 * for a signed binary, "xmonkeysloveplay" is no
		 * longer at the end of file.
		 *
		 * The rest is sanity-checks for the header and section structures.
		 */
		struct mach_header_64 bin_header;
		if ((sigstart = lseek (fd, 0, SEEK_SET)) == -1)
			goto doclose;
		// Find and check binary header
		if (read (fd, &bin_header, sizeof (bin_header)) == -1)
			goto doclose;
		if (bin_header.magic != MH_MAGIC_64)
			goto doclose;

		off_t total = bin_header.sizeofcmds;
		uint32_t count = bin_header.ncmds;
		while (total > 0 && count > 0) {
			struct load_command lc;
			off_t sig_stored = lseek (fd, 0, SEEK_CUR); // get current offset
			if (read (fd, &lc, sizeof (lc)) == -1)
				goto doclose;
			if (lc.cmd == LC_SYMTAB) {
				struct symtab_command stc;
				if ((sigstart = lseek (fd, -sizeof (lc), SEEK_CUR)) == -1)
					goto doclose;
				if (read (fd, &stc, sizeof (stc)) == -1)
					goto doclose;

				// Check the end of the LC_SYMTAB section for "xmonkeysloveplay"
				if ((sigstart = lseek (fd, -(16 + sizeof (uint64_t)) + stc.stroff + stc.strsize, SEEK_SET)) == -1)
					goto doclose;
				if (read (fd, sigbuffer, sizeof (sigbuffer)) == -1)
					goto doclose;
				if (memcmp (sigbuffer + sizeof (uint64_t), "xmonkeysloveplay", 16) == 0)
					goto found;
			}
			if ((sigstart = lseek (fd, sig_stored + lc.cmdsize, SEEK_SET)) == -1)
				goto doclose;
			total -= sizeof (lc.cmdsize);
			count--;
		}
	}
#endif

	// did not find "xmonkeysloveplay" at end of file or end of LC_SYMTAB section
	goto doclose;

found:
	directory_location = GUINT64_FROM_LE ((*(uint64_t *) &sigbuffer [0]));
	if (lseek (fd, directory_location, SEEK_SET) == -1)
		goto doclose;
	directory_size = sigstart-directory_location;
	directory = g_malloc (directory_size);
	if (directory == NULL)
		goto doclose;
	if (read (fd, directory, directory_size) == -1)
		goto dofree;

	items = STREAM_INT (directory);
	p = directory+4;

	assemblies = g_array_new (0, 0, sizeof (MonoBundledAssembly*));
	for (i = 0; i < items; i++){
		char *kind;
		int strsize = STREAM_INT (p);
		uint64_t offset;
		uint32_t item_size;
		kind = p+4;
		p += 4 + strsize;
		offset = STREAM_LONG(p);
		p += 8;
		item_size = STREAM_INT (p);
		p += 4;
		
		if (mapaddress == NULL) {
			char *error_message = NULL;
			mapaddress = (guchar*)mono_file_map_error (directory_location - offset, MONO_MMAP_READ | MONO_MMAP_PRIVATE,
				fd, offset, &maphandle, program, &error_message);
			if (mapaddress == NULL) {
				if (error_message)
					fprintf (stderr, "Error mapping file: %s\n", error_message);
				else
					perror ("Error mapping file");
				exit (1);
			}
			baseline = offset;
		}
		if (strncmp (kind, "assembly:", strlen ("assembly:")) == 0){
			char *aname = kind + strlen ("assembly:");
			MonoBundledAssembly mba = { aname, mapaddress + offset - baseline, item_size }, *ptr;
			ptr = g_new (MonoBundledAssembly, 1);
			memcpy (ptr, &mba, sizeof (MonoBundledAssembly));
			g_array_append_val  (assemblies, ptr);
			if (entry_point == NULL)
				entry_point = aname;
		} else if (strncmp (kind, "config:", strlen ("config:")) == 0){
			char *config = kind + strlen ("config:");
			char *aname = g_strdup (config);
			aname [strlen(aname)-strlen(".config")] = 0;
			mono_register_config_for_assembly (aname, g_str_from_file_region (fd, offset, item_size));
		} else if (strncmp (kind, "systemconfig:", strlen ("systemconfig:")) == 0){
			mono_config_parse_memory (g_str_from_file_region (fd, offset, item_size));
		} else if (strncmp (kind, "options:", strlen ("options:")) == 0){
			mono_parse_options_from (g_str_from_file_region (fd, offset, item_size), ref_argc, ref_argv);
		} else if (strncmp (kind, "config_dir:", strlen ("config_dir:")) == 0){
			char *mono_path_value = g_getenv ("MONO_PATH");
			mono_set_dirs (mono_path_value, g_str_from_file_region (fd, offset, item_size));
			g_free (mono_path_value);
		} else if (strncmp (kind, "machineconfig:", strlen ("machineconfig:")) == 0) {
			mono_register_machine_config (g_str_from_file_region (fd, offset, item_size));
		} else if (strncmp (kind, "env:", strlen ("env:")) == 0){
			char *data = g_str_from_file_region (fd, offset, item_size);
			uint8_t count = *data++;
			char *value = data + count + 1;
			g_setenv (data, value, FALSE);
		} else if (strncmp (kind, "library:", strlen ("library:")) == 0){
			mono_loader_save_bundled_library (fd, offset, item_size, kind + strlen ("library:"));
		} else {
			fprintf (stderr, "Unknown stream on embedded package: %s\n", kind);
			exit (1);
		}
	}
	g_array_append_val (assemblies, last);

	mono_register_bundled_assemblies ((const MonoBundledAssembly **) assemblies->data);
	new_argv = g_new (char *, (*ref_argc)+1);
	new_argv [0] = (*ref_argv)[0];
	new_argv [1] = entry_point;
	for (j = 1; j < *ref_argc; j++)
		new_argv [j+1] = (*ref_argv)[j];
	*ref_argv = new_argv;
	(*ref_argc)++;
	
	return TRUE;
	
dofree:
	g_free (directory);
doclose:
	if (!status)
		close (fd);
	return status;
}

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
	gunichar2 *module_file_name;
	guint32 length;
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

	if (mono_get_module_filename (NULL, &module_file_name, &length)) {
		char *entry = g_utf16_to_utf8 (module_file_name, length, NULL, NULL, NULL);
		g_free (module_file_name);
		probe_embedded (entry, &argc, &argv);
	}

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

	probe_embedded (argv [0], &argc, &argv);
	return mono_main_with_options (argc, argv);
}

#endif
