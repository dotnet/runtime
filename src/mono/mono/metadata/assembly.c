/*
 * assembly.c: Routines for loading assemblies.
 * 
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.  http://www.ximian.com
 *
 * TODO:
 *   Implement big-endian versions of the reading routines.
 */
#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <errno.h>
#include <string.h>
#include "assembly.h"
#include "image.h"
#include "cil-coff.h"
#include "rawbuffer.h"

#define CSIZE(x) (sizeof (x) / 4)

/*
 * keeps track of loaded assemblies
 */
static GHashTable *assemblies;

/**
 * g_concat_dir_and_file:
 * @dir:  directory name
 * @file: filename.
 *
 * returns a new allocated string that is the concatenation of dir and file,
 * takes care of the exact details for concatenating them.
 */
static char *
g_concat_dir_and_file (const char *dir, const char *file)
{
	g_return_val_if_fail (dir != NULL, NULL);
	g_return_val_if_fail (file != NULL, NULL);

        /*
	 * If the directory name doesn't have a / on the end, we need
	 * to add one so we get a proper path to the file
	 */
	if (dir [strlen(dir) - 1] != G_DIR_SEPARATOR)
		return g_strconcat (dir, G_DIR_SEPARATOR_S, file, NULL);
	else
		return g_strconcat (dir, file, NULL);
}

static char *
default_assembly_name_resolver (const char *base_dir, const char *name)
{
	char *file, *path;
	
	if ((strcmp (name, "mscorlib") == 0) ||
			(strcmp (name, "mscorlib.dll") == 0) ||
			(strcmp (name, "corlib.dll") == 0) ||
			(strcmp (name, "corlib") == 0))
		return g_concat_dir_and_file (MONO_ASSEMBLIES, CORLIB_NAME);

	/* Full name already supplied */
	path = g_strdup (name);
	if (g_file_test (name, G_FILE_TEST_EXISTS))
		return path;

	g_free (path);
	path = g_concat_dir_and_file (base_dir, name);
	if (g_file_test (path, G_FILE_TEST_EXISTS))
		return path;

	file = path;
	path = g_strconcat (file, ".dll", NULL);
	g_free (file);
	if (g_file_test (path, G_FILE_TEST_EXISTS))
		return path;
	g_free (path);
	
	path = g_concat_dir_and_file (MONO_ASSEMBLIES, name);
	if (g_file_test (path, G_FILE_TEST_EXISTS))
		return path;
	g_free (path);

	file = g_strconcat (name, ".dll", NULL);
	path = g_concat_dir_and_file (MONO_ASSEMBLIES, file);
	g_free (file);

	return path;
}

/**
 * mono_assembly_open:
 * @filename: Opens the assembly pointed out by this name
 * @resolver: A user provided function to resolve assembly references
 * @status: where a status code can be returned
 *
 * mono_assembly_open opens the PE-image pointed by @filename, and
 * loads any external assemblies referenced by it.
 *
 * NOTE: we could do lazy loading of the assemblies.  Or maybe not worth
 * it. 
 */
MonoAssembly *
mono_assembly_open (const char *filename, MonoAssemblyResolverFn resolver,
		    MonoImageOpenStatus *status)
{
	MonoAssembly *ass;
	MonoImage *image;
	MonoTableInfo *t;
	int i;
	char *fullname, *base_dir;
	const char *base_name = strrchr (filename, G_DIR_SEPARATOR);
	static MonoAssembly *corlib;
	
	g_return_val_if_fail (filename != NULL, NULL);

	if (assemblies == NULL)
		assemblies = g_hash_table_new (g_str_hash, g_str_equal);

	if ((ass = g_hash_table_lookup (assemblies, filename)) != NULL){
		ass->ref_count++;
		return ass;
	}
	
	if (base_name == NULL)
		base_name = filename;
	else
		base_name++;

	if (resolver == NULL)
		resolver = default_assembly_name_resolver;

	base_dir = g_path_get_dirname (filename);
	
	fullname = resolver (base_dir, filename);
	image = mono_image_open (fullname, status);

	if (!image){
		if (status)
			*status = MONO_IMAGE_ERROR_ERRNO;
		g_free (fullname);
		g_free (base_dir);
		return NULL;
	}

	t = &image->tables [MONO_TABLE_ASSEMBLYREF];

	image->references = g_new0 (MonoAssembly *, t->rows + 1);

	/*
	 * Create assembly struct, and enter it into the assembly cache
	 */
	ass = g_new (MonoAssembly, 1);
	ass->name = fullname;
	ass->image = image;

	image->assembly = ass;

	g_hash_table_insert (assemblies, image->name, ass);
	g_hash_table_insert (assemblies, ass->name, ass);
	
	/*
	 * Load any assemblies this image references
	 */
	for (i = 0; i < t->rows; i++){
		char *assembly_ref;
		const char *name;
		guint32 cols [MONO_ASSEMBLYREF_SIZE];

		mono_metadata_decode_row (t, i, cols, CSIZE (cols));
		name = mono_metadata_string_heap (image, cols [MONO_ASSEMBLYREF_NAME]);

		/*
		 * Special case until we have a passable corlib:
		 *
		 * ie, references to mscorlib from corlib.dll are ignored 
		 * and we do not load corlib twice.
		 */
		if (strcmp (base_name, CORLIB_NAME) == 0){
			if (corlib == NULL)
				corlib = ass;
			
			if (strcmp (name, "mscorlib") == 0)
				continue;
		}
		
		assembly_ref = (*resolver) (base_dir, name);

		image->references [i] = mono_assembly_open (assembly_ref, resolver, status);

		if (image->references [i] == NULL){
			int j;
			
			for (j = 0; j < i; j++)
				mono_assembly_close (image->references [j]);
			g_free (image->references);
			mono_image_close (image);

			g_warning ("Could not find assembly %s %s", name, assembly_ref);
			g_free (assembly_ref);
			if (status)
				*status = MONO_IMAGE_MISSING_ASSEMBLYREF;
			g_free (ass);
			g_free (base_dir);
			return NULL;
		}
		g_free (assembly_ref);
	}
	image->references [i] = NULL;

	g_free (base_dir);
	return ass;
}

void
mono_assembly_close (MonoAssembly *assembly)
{
	MonoImage *image;
	int i;
	
	g_return_if_fail (assembly != NULL);

	if (--assembly->ref_count != 0)
		return;
	
	image = assembly->image;
	for (i = 0; image->references [i] != NULL; i++)
		mono_image_close (image->references [i]->image);
	g_free (image->references);
	     
	mono_image_close (assembly->image);

	g_hash_table_remove (assemblies, assembly->name);
			     
	g_free (assembly->name);
	g_free (assembly);
}

/*
 * Temporary hack until we get AppDomains
 */
GHashTable *
mono_get_assemblies ()
{
	return assemblies;
}
