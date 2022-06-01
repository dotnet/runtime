/**
 * \file
 * Support for the portable PDB symbol
 * file format
 *
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright 2015 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
#include <mono/metadata/debug-internals.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/cil-coff.h>
#include <mono/utils/bsearch.h>
#include <mono/utils/mono-logger-internals.h>

#ifndef DISABLE_EMBEDDED_PDB
#ifdef INTERNAL_ZLIB
#include <external/zlib/zlib.h>
#else
#include <zlib.h>
#endif
#endif

#include "debug-mono-ppdb.h"

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

#define EMBEDDED_PPDB_MAGIC 0x4244504d

enum {
	MONO_HAS_CUSTOM_DEBUG_METHODDEF = 0,
	MONO_HAS_CUSTOM_DEBUG_MODULE = 7,
	MONO_HAS_CUSTOM_DEBUG_BITS = 5,
	MONO_HAS_CUSTOM_DEBUG_MASK = 0x1f
};

static gboolean
get_pe_debug_info (MonoImage *image, guint8 *out_guid, gint32 *out_age, gint32 *out_timestamp, guint8 **ppdb_data,
				   int *ppdb_uncompressed_size, int *ppdb_compressed_size)
{
	MonoPEDirEntry *debug_dir_entry;
	ImageDebugDirectory debug_dir;
	int idx;
	gboolean guid_found = FALSE;
	guint8 *data;

	*ppdb_data = NULL;

	debug_dir_entry = (MonoPEDirEntry *) &image->image_info->cli_header.datadir.pe_debug;
	if (!debug_dir_entry->size)
		return FALSE;

	int offset = mono_cli_rva_image_map (image, debug_dir_entry->rva);
	for (idx = 0; idx < debug_dir_entry->size / sizeof (ImageDebugDirectory); ++idx) {
		data = (guint8 *) ((ImageDebugDirectory *) (image->raw_data + offset) + idx);
		debug_dir.characteristics = read32(data);
		debug_dir.time_date_stamp = read32(data + 4);
		debug_dir.major_version   = read16(data + 8);
		debug_dir.minor_version   = read16(data + 10);
		debug_dir.type            = read32(data + 12);
		debug_dir.size_of_data    = read32(data + 16);
		debug_dir.address         = read32(data + 20);
		debug_dir.pointer         = read32(data + 24);

		if (debug_dir.type == DEBUG_DIR_ENTRY_CODEVIEW && debug_dir.major_version == 0x100 && debug_dir.minor_version == 0x504d) {
			/* This is a 'CODEVIEW' debug directory */
			CodeviewDebugDirectory dir;
			data  = (guint8 *) (image->raw_data + debug_dir.pointer);
			dir.signature = read32(data);

			if (dir.signature == 0x53445352) {
				memcpy (out_guid, data + 4, 16);
				*out_age = read32(data + 20);
				*out_timestamp = debug_dir.time_date_stamp;
				guid_found = TRUE;
			}
		}
		if (debug_dir.type == DEBUG_DIR_ENTRY_PPDB && debug_dir.major_version >= 0x100 && debug_dir.minor_version == 0x100) {
			/* Embedded PPDB blob */
			/* See src/System.Reflection.Metadata/src/System/Reflection/PortableExecutable/PEReader.EmbeddedPortablePdb.cs in corefx */
			data = (guint8*)(image->raw_data + debug_dir.pointer);
			guint32 magic = read32 (data);
			g_assert (magic == EMBEDDED_PPDB_MAGIC);
			guint32 size = read32 (data + 4);
			*ppdb_data = data + 8;
			*ppdb_uncompressed_size = size;
			*ppdb_compressed_size = debug_dir.size_of_data - 8;
		}
	}
	return guid_found;
}

static void
doc_free (gpointer key)
{
	MonoDebugSourceInfo *info = (MonoDebugSourceInfo *)key;

	g_free (info->source_file);
	g_free (info);
}

MonoPPDBFile*
mono_create_ppdb_file (MonoImage *ppdb_image, gboolean is_embedded_ppdb)
{
	MonoPPDBFile *ppdb;

	ppdb = g_new0 (MonoPPDBFile, 1);
	ppdb->image = ppdb_image;
	ppdb->doc_hash = g_hash_table_new_full (NULL, NULL, NULL, (GDestroyNotify) doc_free);
	ppdb->method_hash = g_hash_table_new_full (NULL, NULL, NULL, (GDestroyNotify) g_free);
	ppdb->is_embedded = is_embedded_ppdb;
	return ppdb;
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
	gint32 pe_timestamp, pdb_timestamp;
	guint8 *ppdb_data = NULL;
	guint8 *to_free = NULL;
	int ppdb_size = 0, ppdb_compressed_size = 0;
	gboolean is_embedded_ppdb = FALSE;

	if (table_info_get_rows (&image->tables [MONO_TABLE_DOCUMENT])) {
		/* Embedded ppdb */
		mono_image_addref (image);
		return mono_create_ppdb_file (image, TRUE);
	}

	if (!get_pe_debug_info (image, pe_guid, &pe_age, &pe_timestamp, &ppdb_data, &ppdb_size, &ppdb_compressed_size)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Image '%s' has no debug directory.", image->name);
		return NULL;
	}

#ifndef DISABLE_EMBEDDED_PDB
	if (ppdb_data) {
		/* Embedded PPDB data */
		/* ppdb_size is the uncompressed size */
		guint8 *data = g_malloc0 (ppdb_size);
		z_stream stream;

		memset (&stream, 0, sizeof (stream));
		stream.avail_in = ppdb_compressed_size;
		stream.next_in = ppdb_data;
		stream.avail_out = ppdb_size;
		stream.next_out = data;
		int res = inflateInit2 (&stream, -15);
		g_assert (res == Z_OK);
		res = inflate (&stream, Z_NO_FLUSH);
		g_assert (res == Z_STREAM_END);

		g_assert (ppdb_size > 4);
		g_assert (strncmp ((char*)data, "BSJB", 4) == 0);
		raw_contents = data;
		size = ppdb_size;
		to_free = data;
		is_embedded_ppdb = TRUE;
	}
#endif

	MonoAssemblyLoadContext *alc = mono_image_get_alc (image);
	if (raw_contents) {
		if (size > 4 && strncmp ((char*)raw_contents, "BSJB", 4) == 0)
			ppdb_image = mono_image_open_from_data_internal (alc, (char*)raw_contents, size, TRUE, &status, TRUE, NULL, NULL);
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

		ppdb_image = mono_image_open_metadata_only (alc, ppdb_filename, &status);
		g_free (ppdb_filename);
	}
	g_free (to_free);
	if (!ppdb_image)
		return NULL;

	/*
	 * Check that the images match.
	 * The same id is stored in the Debug Directory of the PE file, and in the
	 * #Pdb stream in the ppdb file.
	 */
	PdbStreamHeader *pdb_stream = (PdbStreamHeader*)ppdb_image->heap_pdb.data;

	g_assert (pdb_stream);

	/* The pdb id is a concentation of the pe guid and the timestamp */
	pdb_timestamp = read32(pdb_stream->guid + 16);
	if (memcmp (pe_guid, pdb_stream->guid, 16) != 0 || pe_timestamp != pdb_timestamp) {
		g_warning ("Symbol file %s doesn't match image %s", ppdb_image->name,
				   image->name);
		mono_image_close (ppdb_image);
		return NULL;
	}

	return mono_create_ppdb_file (ppdb_image, is_embedded_ppdb);
}

void
mono_ppdb_close (MonoPPDBFile *ppdb)
{
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
	MonoDebugSourceInfo *res, *cached = NULL;

	mono_debugger_lock ();
	if (ppdb)
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
 * \param minfo A \c MonoDebugMethodInfo which can be retrieved by mono_debug_lookup_method().
 * \param offset IL offset within the corresponding method's CIL code.
 *
 * This function is similar to mono_debug_lookup_location(), but we
 * already looked up the method and also already did the
 * native address -> IL offset mapping.
 */
static MonoDebugSourceLocation *
mono_ppdb_lookup_location_internal (MonoImage *image, int idx, uint32_t offset, MonoPPDBFile *ppdb)
{
	MonoTableInfo *tables = image->tables;
	guint32 cols [MONO_METHODBODY_SIZE];
	const char *ptr;
	const char *end;
	char *docname = NULL;
	int size, docidx, iloffset, delta_il, delta_lines, delta_cols, start_line, start_col, adv_line, adv_col;
	gboolean first = TRUE, first_non_hidden = TRUE;
	MonoDebugSourceLocation *location;

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
	if (docname && docname [0])
		location->source_file = docname;
	location->row = start_line;
	location->column = start_col;
	location->il_offset = iloffset;

	return location;
}


MonoDebugSourceLocation *
mono_ppdb_lookup_location (MonoDebugMethodInfo *minfo, uint32_t offset)
{
	MonoPPDBFile *ppdb = minfo->handle->ppdb;
	MonoImage *image = ppdb->image;
	MonoMethod *method = minfo->method;
	if (!method->token)
		return NULL;
	return mono_ppdb_lookup_location_internal (image, mono_metadata_token_index (method->token), offset, ppdb);
}

MonoDebugSourceLocation *
mono_ppdb_lookup_location_enc (MonoPPDBFile *ppdb_file, int idx, uint32_t offset)
{
	return mono_ppdb_lookup_location_internal (ppdb_file->image, idx, offset, ppdb_file);
}

MonoImage *
mono_ppdb_get_image (MonoPPDBFile *ppdb)
{
	return ppdb->image;
}


gboolean
mono_ppdb_is_embedded (MonoPPDBFile *ppdb)
{
	return ppdb->is_embedded;
}

static int
mono_ppdb_get_seq_points_internal (MonoImage *image, MonoPPDBFile *ppdb, MonoMethod* method, int method_idx, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points)
{
	MonoTableInfo *tables = image->tables;
	guint32 cols [MONO_METHODBODY_SIZE];
	const char *ptr;
	const char *end;
	MonoDebugSourceInfo *docinfo;
	int i, size, docidx, iloffset, delta_il, delta_lines, delta_cols, start_line, start_col, adv_line, adv_col;
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

	if (!method->token || table_info_get_rows (&tables [MONO_TABLE_METHODBODY]) == 0)
		return -1;

	MonoTableInfo *methodbody_table = &tables [MONO_TABLE_METHODBODY];
	if (G_UNLIKELY (method_idx - 1 >= table_info_get_rows (methodbody_table))) {
		char *method_name = mono_method_full_name (method, FALSE);
		g_error ("Method idx %d is greater than number of rows (%d) in PPDB MethodDebugInformation table, for method %s in '%s'. Likely a malformed PDB file.",
		 method_idx - 1, table_info_get_rows (methodbody_table), method_name, image->name);
		g_free (method_name);
	}

	mono_metadata_decode_row (methodbody_table, method_idx - 1, cols, MONO_METHODBODY_SIZE);

	docidx = cols [MONO_METHODBODY_DOCUMENT];

	if (!cols [MONO_METHODBODY_SEQ_POINTS])
		return 0;

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

	if (source_file)
		*source_file = g_strdup (docinfo->source_file);

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

	if (source_files) {
		*source_files = g_new (int, sps->len);
		for (i = 0; i < sps->len; ++i)
			(*source_files)[i] = GPOINTER_TO_INT (g_ptr_array_index (sindexes, i));
		g_ptr_array_free (sindexes, TRUE);
	}
	int n_seqs = sps->len;
	g_array_free (sps, TRUE);

	return n_seqs;
}

gboolean
mono_ppdb_get_seq_points_enc (MonoDebugMethodInfo *minfo, MonoPPDBFile *ppdb_file, int idx, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points)
{
	MonoMethod *method = minfo->method;
	if (mono_ppdb_get_seq_points_internal (ppdb_file->image, ppdb_file, method, idx, source_file, source_file_list, source_files, seq_points, n_seq_points) >= 0)
		return TRUE;
	return FALSE;
}

void
mono_ppdb_get_seq_points (MonoDebugMethodInfo *minfo, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points)
{
	MonoPPDBFile *ppdb = minfo->handle->ppdb;
	MonoImage *image = ppdb->image;
	MonoMethod *method = minfo->method;

	int method_idx = mono_metadata_token_index (method->token);

	mono_ppdb_get_seq_points_internal (image, ppdb, method, method_idx, source_file, source_file_list, source_files, seq_points, n_seq_points);
}

static MonoDebugLocalsInfo*
mono_ppdb_lookup_locals_internal (MonoImage *image, int method_idx)
{
	MonoDebugLocalsInfo *res;
	MonoTableInfo *tables = image->tables;

	guint32 cols [MONO_LOCALSCOPE_SIZE];
	guint32 locals_cols [MONO_LOCALVARIABLE_SIZE];

	int i, lindex, sindex, locals_idx, locals_end_idx, nscopes, start_scope_idx, scope_idx;

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
	int rows = table_info_get_rows (&tables [MONO_TABLE_LOCALSCOPE]);
	while (scope_idx <= rows) {
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
	if (scope_idx > table_info_get_rows (&tables [MONO_TABLE_LOCALSCOPE])) {
		locals_end_idx = table_info_get_rows (&tables [MONO_TABLE_LOCALVARIABLE]) + 1;
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
		if (scope_idx == table_info_get_rows (&tables [MONO_TABLE_LOCALSCOPE])) {
			locals_end_idx = table_info_get_rows (&tables [MONO_TABLE_LOCALVARIABLE]) + 1;
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

MonoDebugLocalsInfo*
mono_ppdb_lookup_locals_enc (MonoImage *image, int method_idx)
{
	return mono_ppdb_lookup_locals_internal (image, method_idx);
}

MonoDebugLocalsInfo*
mono_ppdb_lookup_locals (MonoDebugMethodInfo *minfo)
{
	MonoPPDBFile *ppdb = minfo->handle->ppdb;
	MonoImage *image = ppdb->image;
	MonoMethod *method = minfo->method;
	int method_idx;
	MonoMethodSignature *sig;

	if (!method->token)
		return NULL;

	sig = mono_method_signature_internal (method);
	if (!sig)
		return NULL;

	method_idx = mono_metadata_token_index (method->token);


	return mono_ppdb_lookup_locals_internal (image, method_idx);
}

/*
* We use this to pass context information to the row locator
*/
typedef struct {
	int idx;			/* The index that we are trying to locate */
	int col_idx;		/* The index in the row where idx may be stored */
	MonoTableInfo *t;	/* pointer to the table */
	guint32 result;
} locator_t;

static int
table_locator (const void *a, const void *b)
{
	locator_t *loc = (locator_t *)a;
	const char *bb = (const char *)b;
	guint32 table_index = GPTRDIFF_TO_UINT32 ((bb - loc->t->base) / loc->t->row_size);
	guint32 col;

	col = mono_metadata_decode_row_col(loc->t, table_index, loc->col_idx);

	if (loc->idx == col) {
		loc->result = table_index;
		return 0;
	}
	if (loc->idx < col)
		return -1;
	else
		return 1;
}

static gboolean
compare_guid (guint8* guid1, guint8* guid2)
{
	for (int i = 0; i < 16; i++) {
		if (guid1 [i] != guid2 [i])
			return FALSE;
	}
	return TRUE;
}

// for parent_type see HasCustomDebugInformation table at
// https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md
static const char*
lookup_custom_debug_information (MonoImage* image, guint32 token, uint8_t parent_type, guint8* guid)
{
	MonoTableInfo *tables = image->tables;
	MonoTableInfo *table = &tables[MONO_TABLE_CUSTOMDEBUGINFORMATION];
	locator_t loc;

	if (!table->base)
		return 0;

	loc.idx = (mono_metadata_token_index (token) << MONO_HAS_CUSTOM_DEBUG_BITS) | parent_type;
	loc.col_idx = MONO_CUSTOMDEBUGINFORMATION_PARENT;
	loc.t = table;

	if (!mono_binary_search (&loc, table->base, table_info_get_rows (table), table->row_size, table_locator))
		return NULL;
	// Great we found one of possibly many CustomDebugInformations of this entity they are distinguished by KIND guid
	// First try on this index found by binary search...(it's most likeley to be only one and binary search found the one we want)
	if (compare_guid (guid, (guint8*)mono_metadata_guid_heap (image, mono_metadata_decode_row_col (table, loc.result, MONO_CUSTOMDEBUGINFORMATION_KIND))))
		return mono_metadata_blob_heap (image, mono_metadata_decode_row_col (table, loc.result, MONO_CUSTOMDEBUGINFORMATION_VALUE));

	// Move forward from binary found index, until parent token differs
	int rows = table_info_get_rows (table);
	for (int i = loc.result + 1; i < rows; i++)
	{
		if (mono_metadata_decode_row_col (table, i, MONO_CUSTOMDEBUGINFORMATION_PARENT) != loc.idx)
			break;
		if (compare_guid (guid, (guint8*)mono_metadata_guid_heap (image, mono_metadata_decode_row_col (table, i, MONO_CUSTOMDEBUGINFORMATION_KIND))))
			return mono_metadata_blob_heap (image, mono_metadata_decode_row_col (table, i, MONO_CUSTOMDEBUGINFORMATION_VALUE));
	}

	// Move backward from binary found index, until parent token differs
	for (int i = loc.result - 1; i >= 0; i--) {
		if (mono_metadata_decode_row_col (table, i, MONO_CUSTOMDEBUGINFORMATION_PARENT) != loc.idx)
			break;
		if (compare_guid (guid, (guint8*)mono_metadata_guid_heap (image, mono_metadata_decode_row_col (table, i, MONO_CUSTOMDEBUGINFORMATION_KIND))))
			return mono_metadata_blob_heap (image, mono_metadata_decode_row_col (table, i, MONO_CUSTOMDEBUGINFORMATION_VALUE));
	}
	return NULL;
}

MonoDebugMethodAsyncInfo*
mono_ppdb_lookup_method_async_debug_info (MonoDebugMethodInfo *minfo)
{
	MonoMethod *method = minfo->method;
	MonoPPDBFile *ppdb = minfo->handle->ppdb;
	MonoImage *image = ppdb->image;

	// Guid is taken from Roslyn source code:
	// https://github.com/dotnet/roslyn/blob/1ad4b58/src/Dependencies/CodeAnalysis.Metadata/PortableCustomDebugInfoKinds.cs#L9
	guint8 async_method_stepping_information_guid [16] = { 0xC5, 0x2A, 0xFD, 0x54, 0x25, 0xE9, 0x1A, 0x40, 0x9C, 0x2A, 0xF9, 0x4F, 0x17, 0x10, 0x72, 0xF8 };
	char const *blob = lookup_custom_debug_information (image, method->token, MONO_HAS_CUSTOM_DEBUG_METHODDEF, async_method_stepping_information_guid);
	if (!blob)
		return NULL;
	int blob_len = mono_metadata_decode_blob_size (blob, &blob);
	MonoDebugMethodAsyncInfo* res = g_new0 (MonoDebugMethodAsyncInfo, 1);
	char const *pointer = blob;

	// Format of this blob is taken from Roslyn source code:
	// https://github.com/dotnet/roslyn/blob/1ad4b58/src/Compilers/Core/Portable/PEWriter/MetadataWriter.PortablePdb.cs#L566

	pointer += 4;//catch_handler_offset
	while (pointer - blob < blob_len) {
		res->num_awaits++;
		pointer += 8;//yield_offsets+resume_offsets
		mono_metadata_decode_value (pointer, &pointer);//move_next_method_token
	}
	g_assert(pointer - blob == blob_len); //Check that we used all blob data
	pointer = blob; //reset pointer after we figured num_awaits

	res->yield_offsets = g_new (uint32_t, res->num_awaits);
	res->resume_offsets = g_new (uint32_t, res->num_awaits);
	res->move_next_method_token = g_new (uint32_t, res->num_awaits);

	res->catch_handler_offset = read32 (pointer); pointer += 4;
	for (int i = 0; i < res->num_awaits; i++) {
		res->yield_offsets [i] = read32 (pointer); pointer += 4;
		res->resume_offsets [i] = read32 (pointer); pointer += 4;
		res->move_next_method_token [i] = mono_metadata_decode_value (pointer, &pointer);
	}
	return res;
}

char*
mono_ppdb_get_sourcelink (MonoDebugHandle *handle)
{
	MonoPPDBFile *ppdb = handle->ppdb;
	MonoImage *image = ppdb->image;
	char *res;

	guint8 sourcelink_guid [16] = { 0x56, 0x05, 0x11, 0xCC, 0x91, 0xA0, 0x38, 0x4D, 0x9F, 0xEC, 0x25, 0xAB, 0x9A, 0x35, 0x1A, 0x6A };
	/* The module table only has 1 row */
	char const *blob = lookup_custom_debug_information (image, 1, MONO_HAS_CUSTOM_DEBUG_MODULE, sourcelink_guid);
	if (!blob)
		return NULL;
	int blob_len = mono_metadata_decode_blob_size (blob, &blob);
	res = g_malloc (blob_len + 1);
	memcpy (res, blob, blob_len);
	res [blob_len] = '\0';
	return res;
}
