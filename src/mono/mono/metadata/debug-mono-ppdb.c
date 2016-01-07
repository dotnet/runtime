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
#include <mono/metadata/cil-coff.h>

#include "debug-mono-ppdb.h"

struct _MonoPPDBFile {
	MonoImage *image;
	GHashTable *doc_hash;
	GHashTable *method_hash;
};

/* IMAGE_DEBUG_DIRECTORY structure */
typedef struct
{
	gint32 characteristics;
	gint32 time_date_stamp;
	gint16 major_version;
	gint16 minor_version;
	gint32 type;
	gint32 size_of_data;
	gint32 address;
	gint32 pointer;
}  ImageDebugDirectory;

typedef struct {
	gint32 signature;
	guint8 guid [16];
	gint32 age;
} CodeviewDebugDirectory;

typedef struct {
	guint8 guid [20];
	guint32 entry_point;
	guint64 referenced_tables;
} PdbStreamHeader;

static gboolean
get_pe_debug_guid (MonoImage *image, guint8 *out_guid, gint32 *out_age, gint32 *out_timestamp)
{
	MonoPEDirEntry *debug_dir_entry;
	ImageDebugDirectory *debug_dir;

	debug_dir_entry = &((MonoCLIImageInfo*)image->image_info)->cli_header.datadir.pe_debug;
	if (!debug_dir_entry->size)
		return FALSE;

	int offset = mono_cli_rva_image_map (image, debug_dir_entry->rva);
	debug_dir = (ImageDebugDirectory*)(image->raw_data + offset);
	if (debug_dir->type == 2 && debug_dir->major_version == 0x100 && debug_dir->minor_version == 0x504d) {
		/* This is a 'CODEVIEW' debug directory */
		CodeviewDebugDirectory *dir = (CodeviewDebugDirectory*)(image->raw_data + debug_dir->pointer);

		if (dir->signature == 0x53445352) {
			memcpy (out_guid, dir->guid, 16);
			*out_age = dir->age;
			*out_timestamp = debug_dir->time_date_stamp;
			return TRUE;
		}
	}
	return FALSE;
}

static void
doc_free (gpointer key)
{
	MonoDebugSourceInfo *info = (MonoDebugSourceInfo *)key;

	g_free (info->source_file);
	g_free (info);
}

MonoPPDBFile*
mono_ppdb_load_file (MonoImage *image, const guint8 *raw_contents, int size)
{
	MonoImage *ppdb_image = NULL;
	const char *filename;
	char *s, *ppdb_filename;
	MonoImageOpenStatus status;
	guint8 pe_guid [16];
	gint32 pe_age;
	gint32 pe_timestamp;
	MonoPPDBFile *ppdb;

	if (raw_contents) {
		if (size > 4 && strncmp ((char*)raw_contents, "BSJB", 4) == 0)
			ppdb_image = mono_image_open_from_data_internal ((char*)raw_contents, size, TRUE, &status, FALSE, TRUE, NULL);
	} else {
		/* ppdb files drop the .exe/.dll extension */
		filename = mono_image_get_filename (image);
		if (strlen (filename) > 4 && (!strcmp (filename + strlen (filename) - 4, ".exe") || !strcmp (filename + strlen (filename) - 4, ".dll"))) {
			s = g_strdup (filename);
			s [strlen (filename) - 4] = '\0';
			ppdb_filename = g_strdup_printf ("%s.pdb", s);
			g_free (s);
		} else {
			ppdb_filename = g_strdup_printf ("%s.pdb", filename);
		}

		ppdb_image = mono_image_open_metadata_only (ppdb_filename, &status);
		if (!ppdb_image)
			g_free (ppdb_filename);
	}
	if (!ppdb_image)
		return NULL;

	/*
	 * Check that the images match.
	 * The same id is stored in the Debug Directory of the PE file, and in the
	 * #Pdb stream in the ppdb file.
	 */
	if (get_pe_debug_guid (image, pe_guid, &pe_age, &pe_timestamp)) {
		PdbStreamHeader *pdb_stream = (PdbStreamHeader*)ppdb_image->heap_pdb.data;

		g_assert (pdb_stream);

		/* The pdb id is a concentation of the pe guid and the timestamp */
		if (memcmp (pe_guid, pdb_stream->guid, 16) != 0 || memcmp (&pe_timestamp, pdb_stream->guid + 16, 4) != 0) {
			g_warning ("Symbol file %s doesn't match image %s", ppdb_image->name,
					   image->name);
			mono_image_close (ppdb_image);
			return NULL;
		}
	}

	ppdb = g_new0 (MonoPPDBFile, 1);
	ppdb->image = ppdb_image;
	ppdb->doc_hash = g_hash_table_new_full (NULL, NULL, NULL, (GDestroyNotify) doc_free);
	ppdb->method_hash = g_hash_table_new_full (NULL, NULL, NULL, (GDestroyNotify) g_free);

	return ppdb;
}

void
mono_ppdb_close (MonoDebugHandle *handle)
{
	MonoPPDBFile *ppdb = handle->ppdb;

	mono_image_close (ppdb->image);
	g_hash_table_destroy (ppdb->doc_hash);
	g_hash_table_destroy (ppdb->method_hash);
	g_free (ppdb);
}

MonoDebugMethodInfo *
mono_ppdb_lookup_method (MonoDebugHandle *handle, MonoMethod *method)
{
	MonoDebugMethodInfo *minfo;
	MonoPPDBFile *ppdb = handle->ppdb;

	if (handle->image != mono_class_get_image (mono_method_get_class (method)))
		return NULL;

	mono_debugger_lock ();

	minfo = (MonoDebugMethodInfo *)g_hash_table_lookup (ppdb->method_hash, method);
	if (minfo) {
		mono_debugger_unlock ();
		return minfo;
	}

	minfo = g_new0 (MonoDebugMethodInfo, 1);
	minfo->index = 0;
	minfo->method = method;
	minfo->handle = handle;

	g_hash_table_insert (ppdb->method_hash, method, minfo);

	mono_debugger_unlock ();

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
	MonoDebugSourceInfo *res, *cached;

	mono_debugger_lock ();
	cached = (MonoDebugSourceInfo *)g_hash_table_lookup (ppdb->doc_hash, GUINT_TO_POINTER (docidx));
	mono_debugger_unlock ();
	if (cached)
		return cached;

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

	mono_debugger_lock ();
	cached = (MonoDebugSourceInfo *)g_hash_table_lookup (ppdb->doc_hash, GUINT_TO_POINTER (docidx));
	if (!cached) {
		g_hash_table_insert (ppdb->doc_hash, GUINT_TO_POINTER (docidx), res);
	} else {
		doc_free (res);
		res = cached;
	}
	mono_debugger_unlock ();
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

	if (!method->token)
		return NULL;

	idx = mono_metadata_token_index (method->token);

	mono_metadata_decode_row (&tables [MONO_TABLE_METHODBODY], idx-1, cols, MONO_METHODBODY_SIZE);

	docidx = cols [MONO_METHODBODY_DOCUMENT];

	if (!cols [MONO_METHODBODY_SEQ_POINTS])
		return NULL;
	ptr = mono_metadata_blob_heap (image, cols [MONO_METHODBODY_SEQ_POINTS]);
	size = mono_metadata_decode_blob_size (ptr, &ptr);
	end = ptr + size;

	/* Header */
	/* LocalSignature */
	mono_metadata_decode_value (ptr, &ptr);
	if (docidx == 0)
		docidx = mono_metadata_decode_value (ptr, &ptr);
	docname = get_docname (ppdb, image, docidx);

	iloffset = 0;
	start_line = 0;
	start_col = 0;
	while (ptr < end) {
		delta_il = mono_metadata_decode_value (ptr, &ptr);
		if (!first && delta_il == 0) {
			/* document-record */
			docidx = mono_metadata_decode_value (ptr, &ptr);
			docname = get_docname (ppdb, image, docidx);
			continue;
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
			/* hidden-sequence-point-record */
			continue;
		if (first_non_hidden) {
			start_line = mono_metadata_decode_value (ptr, &ptr);
			start_col = mono_metadata_decode_value (ptr, &ptr);
		} else {
			adv_line = mono_metadata_decode_signed_value (ptr, &ptr);
			adv_col = mono_metadata_decode_signed_value (ptr, &ptr);
			start_line += adv_line;
			start_col += adv_col;
		}
		first_non_hidden = FALSE;
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

	docidx = cols [MONO_METHODBODY_DOCUMENT];

	if (!cols [MONO_METHODBODY_SEQ_POINTS])
		return;

	ptr = mono_metadata_blob_heap (image, cols [MONO_METHODBODY_SEQ_POINTS]);
	size = mono_metadata_decode_blob_size (ptr, &ptr);
	end = ptr + size;

	sps = g_array_new (FALSE, TRUE, sizeof (MonoSymSeqPoint));

	/* Header */
	/* LocalSignature */
	mono_metadata_decode_value (ptr, &ptr);
	if (docidx == 0)
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
		first_non_hidden = FALSE;

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

	// https://github.com/dotnet/roslyn/blob/2ae8d5fed96ab3f1164031f9b4ac827f53289159/docs/specs/PortablePdb-Metadata.md#LocalScopeTable
	//
	// The variableList attribute in the pdb metadata table is a contiguous array that starts at a
	// given offset (locals_idx) above and
	//
	// """
	// continues to the smaller of:
	//
	// the last row of the LocalVariable table
	// the next run of LocalVariables, found by inspecting the VariableList of the next row in this LocalScope table.
	// """
	// this endpoint becomes locals_end_idx below

	// March to the last scope that is in this method
	while (scope_idx <= tables [MONO_TABLE_LOCALSCOPE].rows) {
		mono_metadata_decode_row (&tables [MONO_TABLE_LOCALSCOPE], scope_idx-1, cols, MONO_LOCALSCOPE_SIZE);
		if (cols [MONO_LOCALSCOPE_METHOD] != method_idx)
			break;
		scope_idx ++;
	}
	// The number of scopes is the difference in the indices
	// for the first and last scopes
	nscopes = scope_idx - start_scope_idx;

	// Ends with "the last row of the LocalVariable table"
	// this happens if the above loop marched one past the end
	// of the rows
	if (scope_idx > tables [MONO_TABLE_LOCALSCOPE].rows) {
		locals_end_idx = tables [MONO_TABLE_LOCALVARIABLE].rows + 1;
	} else {
		// Ends with "the next run of LocalVariables,
		// found by inspecting the VariableList of the next row in this LocalScope table."
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
			locals_end_idx = tables [MONO_TABLE_LOCALVARIABLE].rows + 1;
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
