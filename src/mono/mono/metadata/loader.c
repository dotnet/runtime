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
	MonoMethod *result = g_new0 (MonoMethod, 1);
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	cli_image_info_t *iinfo = image->image_info;
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
