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

static char *
default_assembly_name_resolver (const char *name)
{
	if (strcmp (name, "mscorlib") == 0)
		return g_strdup (MONO_ASSEMBLIES "/corlib.dll");

	return g_strconcat (MONO_ASSEMBLIES "/", name, ".dll", NULL);
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
		    enum MonoImageOpenStatus *status)
{
	MonoAssembly *ass;
	MonoImage *image;
	MonoTableInfo *t;
	MonoMetadata *m;
	int i, j;
	const char *basename = strrchr (filename, '/');
	static MonoAssembly *corlib;
	
	if (basename == NULL)
		basename = filename;
	else
		basename++;

	/*
	 * Temporary hack until we have a complete corlib.dll
	 */
	if (!strcmp (basename, "corlib.dll") && corlib != NULL)
		return corlib;
		
	g_return_val_if_fail (filename != NULL, NULL);

	image = mono_image_open (filename, status);
	if (!image){
		if (status)
			*status = MONO_IMAGE_ERROR_ERRNO;
		return NULL;
	}

	if (resolver == NULL)
		resolver = default_assembly_name_resolver;

	m = &image->metadata;
	t = &m->tables [MONO_TABLE_ASSEMBLYREF];

	image->references = g_new (MonoAssembly *, t->rows + 1);

	ass = g_new (MonoAssembly, 1);
	ass->image = image;
	
	/*
	 * Load any assemblies this image references
	 */
	for (i = j = 0; i < t->rows; i++){
		char *assembly_ref;
		const char *name;
		guint32 cols [MONO_ASSEMBLYREF_SIZE];

		mono_metadata_decode_row (t, i, cols, CSIZE (cols));
		name = mono_metadata_string_heap (m, cols [MONO_ASSEMBLYREF_NAME]);

		/*
		 * Special case until we have a passable corlib:
		 *
		 * ie, references to mscorlib from corlib.dll are ignored 
		 * and we do not load corlib twice.
		 */
		if (strcmp (basename, "corlib.dll") == 0){
			if (corlib == NULL)
				corlib = ass;

			if (strcmp (name, "mscorlib") == 0)
				continue;
		}
		
		assembly_ref = (*resolver) (name);

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
			return NULL;
		}
		g_free (assembly_ref);
		j++;
	}
	image->references [j] = NULL;

	return ass;
}

void
mono_assembly_close (MonoAssembly *assembly)
{
	MonoImage *image;
	int i;
	
	g_return_if_fail (assembly != NULL);

	for (i = 0; image->references [i] != NULL; i++)
		mono_image_close (assembly->image);
	g_free (image->references);
	     
	mono_image_close (assembly->image);
	g_free (assembly);
}

