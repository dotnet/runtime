/*
 * debug-mono-ppdb.c: Support for the portable PDB symbol
 * file format
 *
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright 2015 Xamarin Inc (http://www.xamarin.com)
 */

#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>
#include <string.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>

#include "debug-mono-ppdb.h"

struct _MonoPPDBFile {
	MonoImage *image;
	GHashTable *doc_cache;
};

MonoPPDBFile*
mono_ppdb_load_file (MonoImage *image)
{
	MonoImage *ppdb_image;
	const char *filename;
	char *s, *ppdb_filename;
	MonoImageOpenStatus status;
#if 0
	MonoTableInfo *tables;
	guint32 cols [MONO_MODULE_SIZE];
	const char *guid, *ppdb_guid;
#endif
	MonoPPDBFile *ppdb;

	/* ppdb files drop the .exe/.dll extension */
	filename = mono_image_get_filename (image);
	if (strlen (filename) > 4 && (!strcmp (filename + strlen (filename) - 4, ".exe"))) {
		s = g_strdup (filename);
		s [strlen (filename) - 4] = '\0';
		ppdb_filename = g_strdup_printf ("%s.pdb", s);
		g_free (s);
	} else {
		ppdb_filename = g_strdup_printf ("%s.pdb", filename);
	}

	ppdb_image = mono_image_open_metadata_only (ppdb_filename, &status);
	if (!ppdb_image)
		return NULL;

#if 0
	/* Check that the images match */
	// FIXME: ppdb files no longer have a MODULE table */
	tables = image->tables;
	g_assert (tables [MONO_TABLE_MODULE].rows);
	mono_metadata_decode_row (&tables [MONO_TABLE_MODULE], 0, cols, MONO_MODULE_SIZE);
	guid = mono_metadata_guid_heap (image, cols [MONO_MODULE_MVID]);

	tables = ppdb_image->tables;
	g_assert (tables [MONO_TABLE_MODULE].rows);
	mono_metadata_decode_row (&tables [MONO_TABLE_MODULE], 0, cols, MONO_MODULE_SIZE);
	ppdb_guid = mono_metadata_guid_heap (ppdb_image, cols [MONO_MODULE_MVID]);

	if (memcmp (guid, ppdb_guid, 16) != 0) {
		g_warning ("Symbol file %s doesn't match image %s", ppdb_image->name,
				   image->name);
		mono_image_close (ppdb_image);
		return NULL;
	}
#endif

	ppdb = g_new0 (MonoPPDBFile, 1);
	ppdb->image = ppdb_image;

	return ppdb;
}

void
mono_ppdb_close (MonoDebugHandle *handle)
{
	MonoPPDBFile *ppdb = handle->ppdb;

	mono_image_close (ppdb->image);
	if (ppdb->doc_cache)
		g_hash_table_destroy (ppdb->doc_cache);
	g_free (ppdb);
}

MonoDebugMethodInfo *
mono_ppdb_lookup_method (MonoDebugHandle *handle, MonoMethod *method)
{
	MonoDebugMethodInfo *minfo;

	if (handle->image != mono_class_get_image (mono_method_get_class (method)))
		return NULL;

	// FIXME: Cache

	// FIXME: Methods without tokens

	minfo = g_new0 (MonoDebugMethodInfo, 1);
	minfo->index = 0;
	minfo->method = method;
	minfo->handle = handle;

	return minfo;
}

static MonoDebugSourceInfo*
get_docinfo (MonoPPDBFile *ppdb, MonoImage *image, int docidx)
{
	MonoTableInfo *tables = image->tables;
	guint32 cols [MONO_DOCUMENT_SIZE];
	const char *ptr;
	const char *start;
	const char *part_ptr;
	int size, part_size, partidx, nparts;
	char sep;
	GString *s;
	MonoDebugSourceInfo *res;

	// FIXME: Cache

	mono_metadata_decode_row (&tables [MONO_TABLE_DOCUMENT], docidx-1, cols, MONO_DOCUMENT_SIZE);

	ptr = mono_metadata_blob_heap (image, cols [MONO_DOCUMENT_NAME]);
	size = mono_metadata_decode_blob_size (ptr, &ptr);
	start = ptr;

	// FIXME: UTF8
	sep = ptr [0];
	ptr ++;

	s = g_string_new ("");

	nparts = 0;
	while (ptr < start + size) {
		partidx = mono_metadata_decode_value (ptr, &ptr);
		if (nparts)
			g_string_append_c (s, sep);
		if (partidx) {
			part_ptr = mono_metadata_blob_heap (image, partidx);
			part_size = mono_metadata_decode_blob_size (part_ptr, &part_ptr);

			// FIXME: UTF8
			g_string_append_len (s, part_ptr, part_size);
		}
		nparts ++;
	}

	res = g_new0 (MonoDebugSourceInfo, 1);
	res->source_file = g_string_free (s, FALSE);
	res->guid = NULL;
	res->hash = (guint8*)mono_metadata_blob_heap (image, cols [MONO_DOCUMENT_HASH]);

	return res;
}

static char*
get_docname (MonoPPDBFile *ppdb, MonoImage *image, int docidx)
{
	MonoDebugSourceInfo *info;

	info = get_docinfo (ppdb, image, docidx);
	return g_strdup (info->source_file);
}

/**
 * mono_ppdb_lookup_location:
 * @minfo: A `MonoDebugMethodInfo' which can be retrieved by
 *         mono_debug_lookup_method().
 * @offset: IL offset within the corresponding method's CIL code.
 *
 * This function is similar to mono_debug_lookup_location(), but we
 * already looked up the method and also already did the
 * `native address -> IL offset' mapping.
 */
MonoDebugSourceLocation *
mono_ppdb_lookup_location (MonoDebugMethodInfo *minfo, uint32_t offset)
{
	MonoPPDBFile *ppdb = minfo->handle->ppdb;
	MonoImage *image = ppdb->image;
	MonoMethod *method = minfo->method;
	MonoTableInfo *tables = image->tables;
	guint32 cols [MONO_METHODBODY_SIZE];
	const char *ptr;
	const char *end;
	char *docname;
	int idx, size, docidx, iloffset, delta_il, delta_lines, delta_cols, start_line, start_col, adv_line, adv_col;
	gboolean first = TRUE, first_non_hidden = TRUE;
	MonoDebugSourceLocation *location;

	g_assert (method->token);

	idx = mono_metadata_token_index (method->token);

	mono_metadata_decode_row (&tables [MONO_TABLE_METHODBODY], idx-1, cols, MONO_METHODBODY_SIZE);

	// FIXME:
	g_assert (cols [MONO_METHODBODY_SEQ_POINTS]);

	ptr = mono_metadata_blob_heap (image, cols [MONO_METHODBODY_SEQ_POINTS]);
	size = mono_metadata_decode_blob_size (ptr, &ptr);
	end = ptr + size;

	/* Header */
	/* LocalSignature */
	mono_metadata_decode_value (ptr, &ptr);
	docidx = mono_metadata_decode_value (ptr, &ptr);
	docname = get_docname (ppdb, image, docidx);

	iloffset = 0;
	start_line = 0;
	start_col = 0;
	while (ptr < end) {
		delta_il = mono_metadata_decode_value (ptr, &ptr);
		if (!first && delta_il == 0) {
			/* Document record */
			// FIXME:
			g_assert_not_reached ();
		}
		if (!first && iloffset + delta_il > offset)
			break;
		iloffset += delta_il;
		first = FALSE;

		delta_lines = mono_metadata_decode_value (ptr, &ptr);
		if (delta_lines == 0)
			delta_cols = mono_metadata_decode_value (ptr, &ptr);
		else
			delta_cols = mono_metadata_decode_signed_value (ptr, &ptr);
		if (delta_lines == 0 && delta_cols == 0)
			// FIXME:
			g_assert_not_reached ();
		if (first_non_hidden) {
			start_line = mono_metadata_decode_value (ptr, &ptr);
			start_col = mono_metadata_decode_value (ptr, &ptr);
		} else {
			adv_line = mono_metadata_decode_signed_value (ptr, &ptr);
			adv_col = mono_metadata_decode_signed_value (ptr, &ptr);
			start_line += adv_line;
			start_col += adv_col;
		}
		first_non_hidden = TRUE;
	}

	location = g_new0 (MonoDebugSourceLocation, 1);
	location->source_file = docname;
	location->row = start_line;
	location->il_offset = iloffset;

	return location;
}

void
mono_ppdb_get_seq_points (MonoDebugMethodInfo *minfo, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points)
{
	MonoPPDBFile *ppdb = minfo->handle->ppdb;
	MonoImage *image = ppdb->image;
	MonoMethod *method = minfo->method;
	MonoTableInfo *tables = image->tables;
	guint32 cols [MONO_METHODBODY_SIZE];
	const char *ptr;
	const char *end;
	MonoDebugSourceInfo *docinfo;
	int i, method_idx, size, docidx, iloffset, delta_il, delta_lines, delta_cols, start_line, start_col, adv_line, adv_col;
	gboolean first = TRUE, first_non_hidden = TRUE;
	GArray *sps;
	MonoSymSeqPoint sp;
	GPtrArray *sfiles = NULL;
	GPtrArray *sindexes = NULL;

	if (source_file)
		*source_file = NULL;
	if (source_file_list)
		*source_file_list = NULL;
	if (source_files)
		*source_files = NULL;
	if (seq_points)
		*seq_points = NULL;
	if (n_seq_points)
		*n_seq_points = 0;

	if (source_file_list)
		*source_file_list = sfiles = g_ptr_array_new ();
	if (source_files)
		sindexes = g_ptr_array_new ();

	if (!method->token)
		return;

	method_idx = mono_metadata_token_index (method->token);

	mono_metadata_decode_row (&tables [MONO_TABLE_METHODBODY], method_idx-1, cols, MONO_METHODBODY_SIZE);

	if (!cols [MONO_METHODBODY_SEQ_POINTS])
		return;

	ptr = mono_metadata_blob_heap (image, cols [MONO_METHODBODY_SEQ_POINTS]);
	size = mono_metadata_decode_blob_size (ptr, &ptr);
	end = ptr + size;

	sps = g_array_new (FALSE, TRUE, sizeof (MonoSymSeqPoint));

	/* Header */
	/* LocalSignature */
	mono_metadata_decode_value (ptr, &ptr);
	docidx = mono_metadata_decode_value (ptr, &ptr);
	docinfo = get_docinfo (ppdb, image, docidx);
	if (sfiles)
		g_ptr_array_add (sfiles, docinfo);

	iloffset = 0;
	start_line = 0;
	start_col = 0;
	while (ptr < end) {
		delta_il = mono_metadata_decode_value (ptr, &ptr);
		if (!first && delta_il == 0) {
			/* subsequent-document-record */
			docidx = mono_metadata_decode_value (ptr, &ptr);
			docinfo = get_docinfo (ppdb, image, docidx);
			if (sfiles)
				g_ptr_array_add (sfiles, docinfo);
			continue;
		}
		iloffset += delta_il;
		first = FALSE;

		delta_lines = mono_metadata_decode_value (ptr, &ptr);
		if (delta_lines == 0)
			delta_cols = mono_metadata_decode_value (ptr, &ptr);
		else
			delta_cols = mono_metadata_decode_signed_value (ptr, &ptr);

		if (delta_lines == 0 && delta_cols == 0) {
			/* Hidden sequence point */
			continue;
		}

		if (first_non_hidden) {
			start_line = mono_metadata_decode_value (ptr, &ptr);
			start_col = mono_metadata_decode_value (ptr, &ptr);
		} else {
			adv_line = mono_metadata_decode_signed_value (ptr, &ptr);
			adv_col = mono_metadata_decode_signed_value (ptr, &ptr);
			start_line += adv_line;
			start_col += adv_col;
		}
		first_non_hidden = TRUE;

		memset (&sp, 0, sizeof (sp));
		sp.il_offset = iloffset;
		sp.line = start_line;
		sp.column = start_col;
		sp.end_line = start_line + delta_lines;
		sp.end_column = start_col + delta_cols;

		g_array_append_val (sps, sp);
		if (source_files)
			g_ptr_array_add (sindexes, GUINT_TO_POINTER (sfiles->len - 1));
	}

	if (n_seq_points) {
		*n_seq_points = sps->len;
		g_assert (seq_points);
		*seq_points = g_new (MonoSymSeqPoint, sps->len);
		memcpy (*seq_points, sps->data, sps->len * sizeof (MonoSymSeqPoint));
	}

	if (source_file)
		*source_file = g_strdup (((MonoDebugSourceInfo*)g_ptr_array_index (sfiles, 0))->source_file);
	if (source_files) {
		*source_files = g_new (int, sps->len);
		for (i = 0; i < sps->len; ++i)
			(*source_files)[i] = GPOINTER_TO_INT (g_ptr_array_index (sindexes, i));
		g_ptr_array_free (sindexes, TRUE);
	}

	g_array_free (sps, TRUE);
}

MonoDebugLocalsInfo*
mono_ppdb_lookup_locals (MonoDebugMethodInfo *minfo)
{
	MonoPPDBFile *ppdb = minfo->handle->ppdb;
	MonoImage *image = ppdb->image;
	MonoTableInfo *tables = image->tables;
	MonoMethod *method = minfo->method;
	guint32 cols [MONO_LOCALSCOPE_SIZE];
	guint32 locals_cols [MONO_LOCALVARIABLE_SIZE];
	int i, lindex, sindex, method_idx, start_scope_idx, scope_idx, locals_idx, locals_end_idx, nscopes;
	MonoDebugLocalsInfo *res;
	MonoMethodSignature *sig;

	if (!method->token)
		return NULL;

	sig = mono_method_signature (method);
	if (!sig)
		return NULL;

	method_idx = mono_metadata_token_index (method->token);

	start_scope_idx = mono_metadata_localscope_from_methoddef (image, method_idx);

	if (!start_scope_idx)
		return NULL;

	/* Compute number of locals and scopes */
	scope_idx = start_scope_idx;
	mono_metadata_decode_row (&tables [MONO_TABLE_LOCALSCOPE], scope_idx-1, cols, MONO_LOCALSCOPE_SIZE);
	locals_idx = cols [MONO_LOCALSCOPE_VARIABLELIST];
	while (TRUE) {
		mono_metadata_decode_row (&tables [MONO_TABLE_LOCALSCOPE], scope_idx-1, cols, MONO_LOCALSCOPE_SIZE);
		if (cols [MONO_LOCALSCOPE_METHOD] != method_idx)
			break;
		scope_idx ++;
	}
	nscopes = scope_idx - start_scope_idx;
	if (scope_idx == tables [MONO_TABLE_LOCALSCOPE].rows) {
		// FIXME:
		g_assert_not_reached ();
		locals_end_idx = -1;
	} else {
		locals_end_idx = cols [MONO_LOCALSCOPE_VARIABLELIST];
	}

	res = g_new0 (MonoDebugLocalsInfo, 1);
	res->num_blocks = nscopes;
	res->code_blocks = g_new0 (MonoDebugCodeBlock, res->num_blocks);
	res->num_locals = locals_end_idx - locals_idx;
	res->locals = g_new0 (MonoDebugLocalVar, res->num_locals);

	lindex = 0;
	for (sindex = 0; sindex < nscopes; ++sindex) {
		scope_idx = start_scope_idx + sindex;
		mono_metadata_decode_row (&tables [MONO_TABLE_LOCALSCOPE], scope_idx-1, cols, MONO_LOCALSCOPE_SIZE);

		locals_idx = cols [MONO_LOCALSCOPE_VARIABLELIST];
		if (scope_idx == tables [MONO_TABLE_LOCALSCOPE].rows) {
			// FIXME:
			g_assert_not_reached ();
		} else {
			locals_end_idx = mono_metadata_decode_row_col (&tables [MONO_TABLE_LOCALSCOPE], scope_idx-1 + 1, MONO_LOCALSCOPE_VARIABLELIST);
		}

		res->code_blocks [sindex].start_offset = cols [MONO_LOCALSCOPE_STARTOFFSET];
		res->code_blocks [sindex].end_offset = cols [MONO_LOCALSCOPE_STARTOFFSET] + cols [MONO_LOCALSCOPE_LENGTH];

		//printf ("Scope: %s %d %d %d-%d\n", mono_method_full_name (method, 1), cols [MONO_LOCALSCOPE_STARTOFFSET], cols [MONO_LOCALSCOPE_LENGTH], locals_idx, locals_end_idx);

		for (i = locals_idx; i < locals_end_idx; ++i) {
			mono_metadata_decode_row (&tables [MONO_TABLE_LOCALVARIABLE], i - 1, locals_cols, MONO_LOCALVARIABLE_SIZE);

			res->locals [lindex].name = g_strdup (mono_metadata_string_heap (image, locals_cols [MONO_LOCALVARIABLE_NAME]));
			res->locals [lindex].index = locals_cols [MONO_LOCALVARIABLE_INDEX];
			res->locals [lindex].block = &res->code_blocks [sindex];
			lindex ++;

			//printf ("\t %s %d\n", mono_metadata_string_heap (image, locals_cols [MONO_LOCALVARIABLE_NAME]), locals_cols [MONO_LOCALVARIABLE_INDEX]);
		}
	}

	return res;
}
