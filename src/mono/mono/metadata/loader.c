/*
 * loader.c: Image Loader 
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 *
 * This file is used by the interpreter and the JIT engine to locate
 * assemblies.  Used to load AssemblyRef and later to resolve various
 * kinds of `Refs'.
 *
 * TODO:
 *   This should keep track of the assembly versions that we are loading.
 *
 */
#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/cil-coff.h>
#include "cli.h"

typedef struct {
	MonoImage    *img;
	const char   *name;
	guint16       ver [4];
} ImageInfo;

static GHashTable *image_hash;

#define CSIZE(x) (sizeof (x) / 4)

static void
register_image (MonoImage *img)
{
	cli_image_info_t *iinfo = img->image_info;
	metadata_t *t = &iinfo->cli_metadata;
	ImageInfo *ii = g_new (ImageInfo, 1);
	metadata_tableinfo_t *assembly_table = &t->tables [META_TABLE_ASSEMBLY];
	guint32 cols [9];

	if (assembly_table->base == NULL) {
		ii->name = img->name;
		ii->ver [0] = 0;
		ii->ver [1] = 0;
		ii->ver [2] = 0;
		ii->ver [3] = 0;
	} else {
		mono_metadata_decode_row (assembly_table, 0, cols, CSIZE (cols));
		ii->name = mono_metadata_string_heap (t, cols [7]);
		ii->ver [0] = cols [1];
		ii->ver [1] = cols [2];
		ii->ver [2] = cols [3];
		ii->ver [3] = cols [4];
	}

	if (image_hash == NULL)
		image_hash = g_hash_table_new (g_str_hash, g_str_equal);

	g_hash_table_insert (image_hash, (void *) ii->name, ii);
}

static void
unregister_image (MonoImage *img)
{
}

/**
 * mono_load_image:
 * @fname: file that contains the image
 * @status: pointer to the status that can be returned.
 *
 * Returns: on success a pointer to a MonoImage.
 *
 * Side Effects: This uses mono_image_open to load the image
 * and later registers this image.  The image can later be located
 * with mono_locate_image.
 */
MonoImage *
mono_load_image (const char *fname, enum MonoImageOpenStatus *status)
{
	MonoImage *img;
	
	img = mono_image_open (fname, status);
	if (img == NULL)
		return NULL;

	register_image (img);

	return img;
}

/**
 * mono_locate_image:
 * @name: assembly name to locate.
 */
MonoImage *
mono_locate_image (const char *name)
{
	ImageInfo *ii = g_hash_table_lookup (image_hash, name);

	if (ii == NULL)
		return NULL;
	
	return ii->img;
}

MonoMethod *
mono_get_method (cli_image_info_t *iinfo, guint32 token)
{
	MonoMethod *result = g_new0 (MonoMethod, 1);
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	metadata_tableinfo_t *tables = iinfo->cli_metadata.tables;
	const char *loc;
	const char *sig = NULL;
	int size;
	guint32 cols[6];

	/*
	 * We need a context with cli_image_info_t for this module and the assemblies
	 * loaded later to support method refs...
	 */
	if (table != META_TABLE_METHOD) {
		g_assert (table == META_TABLE_MEMBERREF);
		mono_metadata_decode_row (&tables [table], index, cols, 3);
		g_assert ((cols [0] & 0x07) != 3);
		table = META_TABLE_METHOD;
		index = cols [0] >> 3;
		sig = mono_metadata_blob_heap (&iinfo->cli_metadata, cols [2]);
		result->name_idx = cols [1];
	}
	
	mono_metadata_decode_row (&tables [table], index - 1, cols, 6);
	result->name_idx = cols [3];
	/* if this is a methodref from another module/assembly, this fails */
	loc = cli_rva_map (iinfo, cols [0]);
	g_assert (loc);
	result->header = mono_metadata_parse_mh (&iinfo->cli_metadata, loc);
	if (!sig) /* already taken from the methodref */
		sig = mono_metadata_blob_heap (&iinfo->cli_metadata, cols [4]);
	sig = mono_metadata_decode_blob_size (sig, &size);
	result->signature = mono_metadata_parse_method_signature (&iinfo->cli_metadata, 0, sig, NULL);

	return result;
}

void
mono_free_method  (MonoMethod *method)
{
	mono_metadata_free_method_signature (method->signature);
	mono_metadata_free_mh (method->header);
	g_free (method);
}
