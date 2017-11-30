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
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-config.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-dl.h>
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

/**
 * Loads a chunk of data from the file pointed to by the
 * @fd starting at the file offset @offset for @size bytes
 * and returns an allocated version of that string, or NULL
 * on error.
 */
static char *
load_from_region (int fd, uint64_t offset, uint64_t size)
{
	char *buffer;
	off_t loc;
	int status;
	
	do {
		loc = lseek (fd, offset, SEEK_SET);
	} while (loc == -1 && errno == EINTR);
	if (loc == -1)
		return NULL;
	buffer = g_malloc (size + 1);
	if (buffer == NULL)
		return NULL;
	buffer [size] = 0;
	do {
		status = read (fd, buffer, size);
	} while (status == -1 && errno == EINTR);
	if (status == -1){
		g_free (buffer);
		return NULL;
	}
	return buffer;
}

/* Did we initialize the temporary directory for dynamic libraries */
static int bundle_save_library_initialized;

/* List of bundled libraries we unpacked */
static GSList *bundle_library_paths;

/* Directory where we unpacked dynamic libraries */
static char *bundled_dylibrary_directory;

static void
delete_bundled_libraries (void)
{
	GSList *list;

	for (list = bundle_library_paths; list != NULL; list = list->next){
		unlink (list->data);
	}
	rmdir (bundled_dylibrary_directory);
}

static void
bundle_save_library_initialize (void)
{
	bundle_save_library_initialized = 1;
	char *path = g_build_filename (g_get_tmp_dir (), "mono-bundle-XXXXXX", NULL);
	bundled_dylibrary_directory = g_mkdtemp (path);
	g_free (path);
	if (bundled_dylibrary_directory == NULL)
		return;
	atexit (delete_bundled_libraries);
}

static void
save_library (int fd, uint64_t offset, uint64_t size, const char *destfname)
{
	MonoDl *lib;
	char *file, *buffer, *err, *internal_path;
	if (!bundle_save_library_initialized)
		bundle_save_library_initialize ();
	
	file = g_build_filename (bundled_dylibrary_directory, destfname, NULL);
	buffer = load_from_region (fd, offset, size);
	g_file_set_contents (file, buffer, size, NULL);

	lib = mono_dl_open (file, MONO_DL_LAZY, &err);
	if (lib == NULL){
		fprintf (stderr, "Error loading shared library: %s %s\n", file, err);
		exit (1);
	}
	// Register the name with "." as this is how it will be found when embedded
	internal_path = g_build_filename (".", destfname, NULL);
 	mono_loader_register_module (internal_path, lib);
	g_free (internal_path);
	bundle_library_paths = g_slist_append (bundle_library_paths, file);
	
	g_free (buffer);
}

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
	if (fd == -1)
		return FALSE;
	if ((sigstart = lseek (fd, -24, SEEK_END)) == -1)
		goto doclose;
	if (read (fd, sigbuffer, sizeof (sigbuffer)) == -1)
		goto doclose;
	if (memcmp (sigbuffer+sizeof(uint64_t), "xmonkeysloveplay", 16) != 0)
		goto doclose;
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
		uint64_t offset, item_size;
		kind = p+4;
		p += 4 + strsize;
		offset = STREAM_LONG(p);
		p += 8;
		item_size = STREAM_INT (p);
		p += 4;
		
		if (mapaddress == NULL){
			mapaddress = mono_file_map (directory_location-offset, MONO_MMAP_READ | MONO_MMAP_PRIVATE, fd, offset, &maphandle);
			if (mapaddress == NULL){
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
			mono_register_config_for_assembly (aname, load_from_region (fd, offset, item_size));
		} else if (strncmp (kind, "systemconfig:", strlen ("systemconfig:")) == 0){
			mono_config_parse_memory (load_from_region (fd, offset, item_size));
		} else if (strncmp (kind, "options:", strlen ("options:")) == 0){
			mono_parse_options_from (load_from_region (fd, offset, item_size), ref_argc, ref_argv);
		} else if (strncmp (kind, "config_dir:", strlen ("config_dir:")) == 0){
			mono_set_dirs (getenv ("MONO_PATH"), load_from_region (fd, offset, item_size));
		} else if (strncmp (kind, "machineconfig:", strlen ("machineconfig:")) == 0) {
			mono_register_machine_config (load_from_region (fd, offset, item_size));
		} else if (strncmp (kind, "env:", strlen ("env:")) == 0){
			char *data = load_from_region (fd, offset, item_size);
			uint8_t count = *data++;
			char *value = data + count + 1;
			g_setenv (data, value, FALSE);
		} else if (strncmp (kind, "library:", strlen ("library:")) == 0){
			save_library (fd, offset, item_size, kind + strlen ("library:"));
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

#ifdef HOST_WIN32

#include <shellapi.h>

int
main (void)
{
	TCHAR szFileName[MAX_PATH];
	int argc;
	gunichar2** argvw;
	gchar** argv;
	int i;
	DWORD count;
	
	argvw = CommandLineToArgvW (GetCommandLine (), &argc);
	argv = g_new0 (gchar*, argc + 1);
	for (i = 0; i < argc; i++)
		argv [i] = g_utf16_to_utf8 (argvw [i], -1, NULL, NULL, NULL);
	argv [argc] = NULL;

	LocalFree (argvw);

	if ((count = GetModuleFileName (NULL, szFileName, MAX_PATH)) != 0){
		char *entry = g_utf16_to_utf8 (szFileName, count, NULL, NULL, NULL);
		probe_embedded (entry, &argc, &argv);
	}

	return mono_main_with_options  (argc, argv);
}

#else

int
main (int argc, char* argv[])
{
	mono_build_date = build_date;

	probe_embedded (argv [0], &argc, &argv);
	return mono_main_with_options (argc, argv);
}

#endif
