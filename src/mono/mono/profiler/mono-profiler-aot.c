/*
 * mono-profiler-aot.c: Ahead of Time Compiler Profiler for Mono.
 *
 *
 * Copyright 2008-2009 Novell, Inc (http://www.novell.com)
 *
 * This profiler collects profiling information usable by the Mono AOT compiler
 * to generate better code. It saves the information into files under ~/.mono. 
 * The AOT compiler can load these files during compilation.
 * Currently, only the order in which methods were compiled is saved, 
 * allowing more efficient function ordering in the AOT files.
 */

#include <config.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <string.h>
#include <errno.h>
#include <stdlib.h>
#include <glib.h>
#include <sys/stat.h>

#ifdef HOST_WIN32
#include <direct.h>
#endif

struct _MonoProfiler {
	GHashTable *images;
};

typedef struct {
	GList *methods;
} PerImageData;

typedef struct ForeachData {
	MonoProfiler *prof;
	FILE *outfile;
	MonoImage *image;
	MonoMethod *method;
} ForeachData;

static void
foreach_method (gpointer data, gpointer user_data)
{
	ForeachData *udata = (ForeachData*)user_data;
	MonoMethod *method = (MonoMethod*)data;
	char *name;

	if (!mono_method_get_token (method) || mono_class_get_image (mono_method_get_class (method)) != udata->image)
		return;

	name = mono_method_full_name (method, TRUE);
	fprintf (udata->outfile, "%s\n", name);
	g_free (name);
}

static void
output_image (gpointer key, gpointer value, gpointer user_data)
{
	MonoImage *image = (MonoImage*)key;
	PerImageData *image_data = (PerImageData*)value;
	MonoProfiler *prof = (MonoProfiler*)user_data;
	char *tmp, *outfile_name;
	FILE *outfile;
	int i, err;
	ForeachData data;

	tmp = g_strdup_printf ("%s/.mono/aot-profile-data", g_get_home_dir ());

	if (!g_file_test (tmp, G_FILE_TEST_IS_DIR)) {
#ifdef HOST_WIN32
		err = mkdir (tmp);
#else
		err = mkdir (tmp, 0777);
#endif
		if (err) {
			fprintf (stderr, "mono-profiler-aot: Unable to create output directory '%s': %s\n", tmp, g_strerror (errno));
			exit (1);
		}
	}

	i = 0;
	while (TRUE) {
		outfile_name = g_strdup_printf ("%s/%s-%d", tmp, mono_image_get_name (image), i);

		if (!g_file_test (outfile_name, G_FILE_TEST_IS_REGULAR))
			break;

		i ++;
	}

	printf ("Creating output file: %s\n", outfile_name);

	outfile = fopen (outfile_name, "w+");
	g_assert (outfile);

	fprintf (outfile, "#VER:%d\n", 2);

	data.prof = prof;
	data.outfile = outfile;
	data.image = image;

	g_list_foreach (image_data->methods, foreach_method, &data);
}

/* called at the end of the program */
static void
prof_shutdown (MonoProfiler *prof)
{
	g_hash_table_foreach (prof->images, output_image, prof);
}

static void
prof_jit_enter (MonoProfiler *prof, MonoMethod *method)
{
}

static void
prof_jit_leave (MonoProfiler *prof, MonoMethod *method, int result)
{
	MonoImage *image = mono_class_get_image (mono_method_get_class (method));
	PerImageData *data;

	data = g_hash_table_lookup (prof->images, image);
	if (!data) {
		data = g_new0 (PerImageData, 1);
		g_hash_table_insert (prof->images, image, data);
	}

	data->methods = g_list_append (data->methods, method);
}

void
mono_profiler_startup (const char *desc);

/* the entry point */
void
mono_profiler_startup (const char *desc)
{
	MonoProfiler *prof;

	prof = g_new0 (MonoProfiler, 1);
	prof->images = g_hash_table_new (NULL, NULL);

	mono_profiler_install (prof, prof_shutdown);
	
	mono_profiler_install_jit_compile (prof_jit_enter, prof_jit_leave);

	mono_profiler_set_events (MONO_PROFILE_JIT_COMPILATION);
}


