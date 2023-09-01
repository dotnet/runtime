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
#include <dnmd.h>
#include <dnmd_pdb.h>

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

gboolean 
mono_get_pe_debug_info_full (MonoImage *image, guint8 *out_guid, gint32 *out_age, gint32 *out_timestamp, guint8 **ppdb_data,
				   int *ppdb_uncompressed_size, int *ppdb_compressed_size, char **pdb_path, GArray *pdb_checksum_hash_type, GArray *pdb_checksum)
{
	MonoPEDirEntry *debug_dir_entry;
	ImageDebugDirectory debug_dir;
	gboolean guid_found = FALSE;
	guint8 *data;
	if (!image || !image->image_info)
		return FALSE;
	*ppdb_data = NULL;

	debug_dir_entry = (MonoPEDirEntry *) &image->image_info->cli_header.datadir.pe_debug;
	if (!debug_dir_entry || !debug_dir_entry->size)
		return FALSE;

	int offset = mono_cli_rva_image_map (image, debug_dir_entry->rva);
	for (guint32 idx = 0; idx < debug_dir_entry->size / sizeof (ImageDebugDirectory); ++idx) {
		data = (guint8 *) ((ImageDebugDirectory *) (image->raw_data + offset) + idx);
		debug_dir.characteristics = read32(data);
		debug_dir.time_date_stamp = read32(data + 4);
		debug_dir.major_version   = read16(data + 8);
		debug_dir.minor_version   = read16(data + 10);
		debug_dir.type            = read32(data + 12);
		debug_dir.size_of_data    = read32(data + 16);
		debug_dir.address         = read32(data + 20);
		debug_dir.pointer         = read32(data + 24);

		if (pdb_checksum_hash_type && pdb_checksum && debug_dir.type == DEBUG_DIR_PDB_CHECKSUM)
		{
			data  = (guint8 *) (image->raw_data + debug_dir.pointer);
			char* alg_name = (char*)data;
			guint8*	checksum = (guint8 *) (data + strlen(alg_name)+ 1);
			g_array_append_val (pdb_checksum_hash_type, alg_name);
			g_array_append_val (pdb_checksum, checksum);
		}

		if (debug_dir.type == DEBUG_DIR_ENTRY_CODEVIEW && debug_dir.major_version == 0x100 && debug_dir.minor_version == 0x504d) {
			/* This is a 'CODEVIEW' debug directory */
			CodeviewDebugDirectory dir;
			data  = (guint8 *) (image->raw_data + debug_dir.pointer);
			dir.signature = read32(data);

			if (dir.signature == 0x53445352) {
				memcpy (out_guid, data + 4, 16);
				*out_age = read32(data + 20);
				if (pdb_path)
					*pdb_path = (char*) data + 24;
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

static gboolean
get_pe_debug_info (MonoImage *image, guint8 *out_guid, gint32 *out_age, gint32 *out_timestamp, guint8 **ppdb_data,
				   int *ppdb_uncompressed_size, int *ppdb_compressed_size)
{
	return mono_get_pe_debug_info_full (image, out_guid, out_age, out_timestamp, ppdb_data, ppdb_uncompressed_size, ppdb_compressed_size, NULL, NULL, NULL);
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
	MonoDebugSourceInfo *res, *cached = NULL;

	mono_debugger_lock ();
	if (ppdb)
		cached = (MonoDebugSourceInfo *)g_hash_table_lookup (ppdb->doc_hash, GUINT_TO_POINTER (docidx));
	mono_debugger_unlock ();
	if (cached)
		return cached;

	guint32 tok = mono_metadata_make_token(MONO_TABLE_DOCUMENT, docidx);
	mdcursor_t c;
	if (!md_token_to_cursor (image->metadata_handle, tok, &c))
		return NULL;
	
	uint8_t const* name_blob;
	uint32_t name_blob_len;
	if (1 != md_get_column_value_as_blob (c, mdtDocument_Name, 1, &name_blob, &name_blob_len))
		return NULL;
	
	char* name = NULL;
	size_t name_len = 0;

	md_blob_parse_result_t document_name_parse_result = md_parse_document_name(image->metadata_handle, name_blob, name_blob_len, name, &name_len);
	// TODO: Handle malformed DocumentName blobs.
	g_assert(document_name_parse_result == mdbpr_InsufficientBuffer);
	name = g_malloc(name_len);
	document_name_parse_result = md_parse_document_name(image->metadata_handle, name_blob, name_blob_len, name, &name_len);
	g_assert(document_name_parse_result == mdbpr_Success);

	uint8_t const* hash;
	uint32_t hash_len;
	if (1 != md_get_column_value_as_blob (c, mdtDocument_Hash, 1, &hash, &hash_len))
		return NULL;
	
	g_assert(hash_len == 16);

	res = g_new0 (MonoDebugSourceInfo, 1);
	res->source_file = name;
	res->guid = NULL;
	res->hash = (guint8*)hash;

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
	char *docname = NULL;
	guint64 start_line = 0, start_col = 0;
	guint32 iloffset = 0;
	MonoDebugSourceLocation *location;

	guint32 tok = mono_metadata_make_token(MONO_TABLE_METHODBODY, idx);
	mdcursor_t c;
	if (!md_token_to_cursor (image->metadata_handle, tok, &c))
		return NULL;
	
	uint8_t const* sequence_points_blob;
	uint32_t sequence_points_blob_len;
	if (1 != md_get_column_value_as_blob (c, mdtMethodDebugInformation_SequencePoints, 1, &sequence_points_blob, &sequence_points_blob_len))
		return NULL;
	
	if (sequence_points_blob_len == 0)
		return NULL;

	md_sequence_points_t* sequence_points = NULL;
	size_t sequence_points_buffer_len = 0;
	md_blob_parse_result_t parse_result = md_parse_sequence_points (c, sequence_points_blob, sequence_points_blob_len, sequence_points, &sequence_points_buffer_len);
	g_assert(parse_result == mdbpr_InsufficientBuffer);
	sequence_points = g_malloc(sequence_points_buffer_len);
	parse_result = md_parse_sequence_points (c, sequence_points_blob, sequence_points_blob_len, sequence_points, &sequence_points_buffer_len);
	g_assert(parse_result == mdbpr_Success);

	guint32 doc_tok;
	if (1 != md_get_column_value_as_token (c, mdtMethodDebugInformation_Document, 1, &doc_tok))
		return NULL;

	if (mono_metadata_token_index(doc_tok) == 0
		&& !md_cursor_to_token (sequence_points->document, &doc_tok))
		return NULL;
	
	docname = get_docname (ppdb, image, mono_metadata_token_index(doc_tok));

	for (uint32_t i = 0; i < sequence_points->record_count; ++i) {
		if (sequence_points->records[i].kind == mdsp_DocumentRecord) {
			if (!md_cursor_to_token (sequence_points->records[i].document.document, &doc_tok))
				return NULL;
			docname = get_docname (ppdb, image, mono_metadata_token_index(doc_tok));
			continue;
		}

		guint32 il_delta;
		if (sequence_points->records[i].kind == mdsp_HiddenSequencePointRecord) {
			il_delta = sequence_points->records[i].hidden_sequence_point.il_offset;
		} else {
			il_delta = sequence_points->records[i].sequence_point.il_offset;
		}

		// The next sequence point is the first one after our target location.
		// Return before we account for it.
		// We will process at least one sequence point so we have at least one to return.
		if (i != 0 && iloffset + il_delta > offset)
			break;
		
		iloffset += il_delta;
	
		if (sequence_points->records[i].kind == mdsp_SequencePointRecord) {
			start_line += sequence_points->records[i].sequence_point.start_line;
			start_col += sequence_points->records[i].sequence_point.start_column;
		}
	}

	location = g_new0 (MonoDebugSourceLocation, 1);
	if (docname && docname [0])
		location->source_file = docname;
	location->row = (int)start_line;
	location->column = (int)start_col;
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
	MonoDebugSourceInfo *docinfo;
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

	GPtrArray* sfiles = NULL;
	GPtrArray* sindexes = NULL;
	if (source_file_list)
		*source_file_list = sfiles = g_ptr_array_new ();
	if (source_files)
		sindexes = g_ptr_array_new ();

	if (!method->token)
		return 0;

	guint32 tok = mono_metadata_make_token(MONO_TABLE_METHODBODY, mono_metadata_token_index(method->token));
	mdcursor_t c;
	if (!md_token_to_cursor (image->metadata_handle, tok, &c))
		return 0;
	
	uint8_t const* sequence_points_blob;
	uint32_t sequence_points_blob_len;
	if (1 != md_get_column_value_as_blob (c, mdtMethodDebugInformation_SequencePoints, 1, &sequence_points_blob, &sequence_points_blob_len))
		return 0;
	
	if (sequence_points_blob_len == 0)
		return 0;

	md_sequence_points_t* sequence_points = NULL;
	size_t sequence_points_buffer_len = 0;
	md_blob_parse_result_t parse_result = md_parse_sequence_points (c, sequence_points_blob, sequence_points_blob_len, sequence_points, &sequence_points_buffer_len);
	g_assert(parse_result == mdbpr_InsufficientBuffer);
	sequence_points = g_malloc(sequence_points_buffer_len);
	parse_result = md_parse_sequence_points (c, sequence_points_blob, sequence_points_blob_len, sequence_points, &sequence_points_buffer_len);
	g_assert(parse_result == mdbpr_Success);

	guint32 doc_tok;
	if (1 != md_get_column_value_as_token (c, mdtMethodDebugInformation_Document, 1, &doc_tok))
		return 0;

	if (mono_metadata_token_index(doc_tok) == 0
		&& !md_cursor_to_token (sequence_points->document, &doc_tok))
		return 0;
	
	docinfo = get_docinfo (ppdb, image, mono_metadata_token_index(doc_tok));

	guint64 start_line = 0, start_col = 0;
	guint32 iloffset = 0;

	GArray *sps = g_array_new (FALSE, TRUE, sizeof (MonoSymSeqPoint));
	for (uint32_t i = 0; i < sequence_points->record_count; ++i) {
		MonoSymSeqPoint sp = { 0 };
		if (sequence_points->records[i].kind == mdsp_DocumentRecord) {
			if (!md_cursor_to_token (sequence_points->records[i].document.document, &doc_tok))
				return 0;
			docinfo = get_docinfo (ppdb, image, mono_metadata_token_index(doc_tok));
			if (sfiles)
				g_ptr_array_add (sfiles, docinfo);
			continue;
		} else if (sequence_points->records[i].kind == mdsp_HiddenSequencePointRecord) {
			sp.il_offset = iloffset += sequence_points->records[i].hidden_sequence_point.il_offset;
		} else {
			sp.il_offset = iloffset += sequence_points->records[i].sequence_point.il_offset;
			sp.line = start_line += sequence_points->records[i].sequence_point.start_line;
			sp.column = start_col += sequence_points->records[i].sequence_point.start_column;
			sp.end_line = start_line += sequence_points->records[i].sequence_point.num_lines;
			sp.end_column = start_col += sequence_points->records[i].sequence_point.delta_columns;
		}

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
		for (gint i = 0; i < sps->len; ++i)
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

	mdcursor_t locals;
	uint32_t num_scopes;

	if (!md_create_cursor(image->metadata_handle, mdtid_LocalScope, &locals, &num_scopes))
		return NULL;

	md_range_result_t scopes_range_result = md_find_range_from_cursor(locals, mdtLocalScope_Method, method_idx, &locals, &num_scopes);
	// No tools generate unsorted tables in a Portable PDB, so we can assume this doesn't happen.
	g_assert(scopes_range_result != MD_RANGE_NOT_SUPPORTED);
	if (scopes_range_result != MD_RANGE_FOUND)
		return NULL;

	uint32_t num_locals = 0;
	mdcursor_t local_scope = locals;
	for (uint32_t i = 0; i < num_scopes; ++i, md_cursor_next (&local_scope)) {
		mdcursor_t variables;
		uint32_t num_variables;
		if (!md_get_column_value_as_range (local_scope, mdtLocalScope_VariableList, &variables, &num_variables))
			return NULL;
		
		num_locals += num_variables;
	}

	res = g_new0 (MonoDebugLocalsInfo, 1);
	res->num_blocks = num_scopes;
	res->code_blocks = g_new0 (MonoDebugCodeBlock, num_scopes);
	res->num_locals = num_locals;
	res->locals = g_new0 (MonoDebugLocalVar, num_locals);

	// Now we can iterate over the ranges, get the variables,
	// and fill in our data structure.
	uint32_t next_var = 0;
	local_scope = locals;
	for (uint32_t i = 0; i < num_scopes; ++i, md_cursor_next (&local_scope)) {
		uint32_t start_offset, length;
		if (1 != md_get_column_value_as_constant (local_scope, mdtLocalScope_StartOffset, 1, &start_offset))
			return NULL;
		
		if (1 != md_get_column_value_as_constant (local_scope, mdtLocalScope_Length, 1, &length))
			return NULL;
		
		res->code_blocks [i].start_offset = (int)start_offset;
		res->code_blocks [i].end_offset = (int)(start_offset + length);

		mdcursor_t variable;
		uint32_t num_variables;
		if (!md_get_column_value_as_range (local_scope, mdtLocalScope_VariableList, &variable, &num_variables))
			return NULL;
		
		for (uint32_t j = 0; j < num_variables; ++j, ++next_var, md_cursor_next(&variable)) {
			char const* name;
			if (1 != md_get_column_value_as_utf8 (variable, mdtLocalVariable_Name, 1, &name))
				return NULL;
			
			uint32_t index;
			if (1 != md_get_column_value_as_constant (variable, mdtLocalVariable_Index, 1, &index))
				return NULL;
			
			res->locals [next_var].name = g_strdup (name);
			res->locals [next_var].index = (int)index;
			res->locals [next_var].block = &res->code_blocks [i];
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

static gboolean
compare_guid (guint8* guid1, guint8* guid2)
{
	for (int i = 0; i < 16; i++) {
		if (guid1 [i] != guid2 [i])
			return FALSE;
	}
	return TRUE;
}

static const char*
lookup_custom_debug_information (MonoImage* image, guint32 token, guint8* guid, guint32* debug_info_size)
{
	mdcursor_t c;
	uint32_t count;
	if (!md_create_cursor (image->metadata_handle, mdtid_CustomDebugInformation, &c, &count))
		return NULL;
	
	// CustomDebugInformation will always be sorted by every Portable PDB producer, so asseume that it's sorted.
	if (md_find_range_from_cursor (c, mdtCustomDebugInformation_Parent, token, &c, &count) != MD_RANGE_FOUND)
		return NULL;
	
	for (uint32_t i = 0; i < count; i++) {
		mdguid_t debug_info_guid;
		if (1 != md_get_column_value_as_guid (c, mdtCustomDebugInformation_Kind, 1, &debug_info_guid))
			continue;
		
		if (compare_guid (guid, (guint8*)&debug_info_guid)) {
			guint8 const* debug_info;
			if (1 != md_get_column_value_as_blob (c, mdtCustomDebugInformation_Value, 1, &debug_info, debug_info_size))
				continue;
			
			return (const char*)debug_info;
		}
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
	guint32 blob_len;
	guint8 async_method_stepping_information_guid [16] = { 0xC5, 0x2A, 0xFD, 0x54, 0x25, 0xE9, 0x1A, 0x40, 0x9C, 0x2A, 0xF9, 0x4F, 0x17, 0x10, 0x72, 0xF8 };
	char const *blob = lookup_custom_debug_information (image, method->token, async_method_stepping_information_guid, &blob_len);
	if (!blob)
		return NULL;
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

	guint32 blob_len;
	guint8 sourcelink_guid [16] = { 0x56, 0x05, 0x11, 0xCC, 0x91, 0xA0, 0x38, 0x4D, 0x9F, 0xEC, 0x25, 0xAB, 0x9A, 0x35, 0x1A, 0x6A };
	/* The module table only has 1 row */
	char const *blob = lookup_custom_debug_information (image, 1, sourcelink_guid, &blob_len);
	if (!blob)
		return NULL;
	res = g_malloc (blob_len + 1);
	memcpy (res, blob, blob_len);
	res [blob_len] = '\0';
	return res;
}
