/*
 * loader.c: Image Loader 
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
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

MonoMethod *
mono_get_method (MonoImage *image, guint32 token)
{
	MonoMethod *result;
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	metadata_tableinfo_t *tables = image->metadata.tables;
	const char *loc;
	const char *sig = NULL;
	int size;
	guint32 cols[6];

	if ((result = g_hash_table_lookup (image->method_cache, GINT_TO_POINTER (token))))
			return result;
	
	result = g_new0 (MonoMethod, 1);
	result->image = image;
	/*
	 * We need a context with cli_image_info_t for this module and the assemblies
	 * loaded later to support method refs...
	 */
	if (table != META_TABLE_METHOD) {
		g_assert (table == META_TABLE_MEMBERREF);
		mono_metadata_decode_row (&tables [table], index-1, cols, 3);
		g_print ("methodref: 0x%x 0x%x", cols [0] & 7, cols [0] >> 3);
		index = cols [0] >> 3;
		sig = mono_metadata_blob_heap (&image->metadata, cols [2]);
		result->name_idx = cols [1];
		/* Need to finish this code ... */
		switch ((cols [0] & 0x07)) {
		case 1:
			mono_metadata_decode_row (&tables [META_TABLE_TYPEREF], index, cols, 3);
			g_assert (0);
			break;
		default:
			g_assert_not_reached ();
		}
		table = META_TABLE_METHOD;
	}
	
	mono_metadata_decode_row (&tables [table], index - 1, cols, 6);
	result->name_idx = cols [3];
	/* if this is a methodref from another module/assembly, this fails */
	loc = cli_rva_map ((cli_image_info_t *)image->image_info, cols [0]);
	g_assert (loc);
	result->header = mono_metadata_parse_mh (&image->metadata, loc);
	if (!sig) /* already taken from the methodref */
		sig = mono_metadata_blob_heap (&image->metadata, cols [4]);
	size = mono_metadata_decode_blob_size (sig, &sig);
	result->signature = mono_metadata_parse_method_signature (&image->metadata, 0, sig, NULL);

	g_hash_table_insert (image->method_cache, GINT_TO_POINTER (token), result);

	return result;
}

void
mono_free_method  (MonoMethod *method)
{
	mono_metadata_free_method_signature (method->signature);
	mono_metadata_free_mh (method->header);
	g_free (method);
}
