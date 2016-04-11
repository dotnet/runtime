#include <config.h>
#include <fcntl.h>
#include <mono/metadata/assembly.h>
#include <mono/utils/mono-mmap.h>
#include "mini.h"

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

#define STREAM_INT(x) (*(uint32_t*)x)
#define STREAM_LONG(x) (*(uint64_t*)x)

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
	if ((sigstart = lseek (fd, -(16+sizeof(uint64_t)), SEEK_END)) == -1)
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
			printf ("c-Found: %s %llx\n", kind, offset);
			char *config = kind + strlen ("config:");
			char *aname = g_strdup (config);
			aname [strlen(aname)-strlen(".config")] = 0;
			mono_register_config_for_assembly (aname, config);
		} else if (strncmp (kind, "system_config:", strlen ("system_config:")) == 0){
			printf ("TODO s-Found: %s %llx\n", kind, offset);
		} else if (strncmp (kind, "options:", strlen ("options:")) == 0){
			mono_parse_options_from (kind + strlen("options:"), ref_argc, ref_argv);
		} else if (strncmp (kind, "config_dir:", strlen ("config_dir:")) == 0){
			printf ("TODO Found: %s %llx\n", kind, offset);
		} else {
			fprintf (stderr, "Unknown stream on embedded package: %s\n", kind);
			exit (1);
		}
	}
	g_array_append_val (assemblies, last);
	
	mono_register_bundled_assemblies ((const MonoBundledAssembly **) assemblies->data);
	new_argv = g_new (char *, (*ref_argc)+1);
	for (j = 0; j < *ref_argc; j++)
		new_argv [j] = (*ref_argv)[j];
	new_argv [j] = entry_point;
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
