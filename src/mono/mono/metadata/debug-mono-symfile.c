/**
 * \file
 *
 *   Support for reading debug info from .mdb files.
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright (C) 2005-2008 Novell, Inc. (http://www.novell.com)
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>
#include <string.h>
#ifdef HAVE_SYS_PARAM_H
#include <sys/param.h>
#endif
#include <sys/stat.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/bsearch.h>

#ifndef DISABLE_MDB

#include <fcntl.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#define RANGE_TABLE_CHUNK_SIZE		256
#define CLASS_TABLE_CHUNK_SIZE		256
#define TYPE_TABLE_PTR_CHUNK_SIZE	256
#define TYPE_TABLE_CHUNK_SIZE		65536

struct _MonoSymbolFile {
	const uint8_t *raw_contents;
	int raw_contents_size;
	void *raw_contents_handle;
	int major_version;
	int minor_version;
	char *filename;
	GHashTable *method_hash;
	GHashTable *source_hash;
	MonoSymbolFileOffsetTable *offset_table;
	gboolean was_loaded_from_memory;
};

static void
free_method_info (MonoDebugMethodInfo *minfo)
{
	g_free (minfo);
}

static void
free_source_info (MonoDebugSourceInfo *sinfo)
{
	g_free (sinfo->source_file);
	g_free (sinfo->guid);
	g_free (sinfo->hash);
	g_free (sinfo);
}

static int
load_symfile (MonoDebugHandle *handle, MonoSymbolFile *symfile, mono_bool in_the_debugger)
{
	const char *ptr, *start;
	gchar *guid;
	uint64_t magic;
	int minor, major;

	ptr = start = (const char*)symfile->raw_contents;
	if (!ptr)
		return FALSE;

	magic = read64(ptr);
	ptr += sizeof(uint64_t);
	if (magic != MONO_SYMBOL_FILE_MAGIC) {
		if (!in_the_debugger)
			g_warning ("Symbol file %s is not a mono symbol file", symfile->filename);
		return FALSE;
	}

	major = read32(ptr);
	ptr += sizeof(uint32_t);
	minor = read32(ptr);
	ptr += sizeof(uint32_t);

	/*
	 * 50.0 is the frozen version for Mono 2.0.
	 *
	 * Nobody except me (Martin) is allowed to check the minor version.
	 */
	if (major != MONO_SYMBOL_FILE_MAJOR_VERSION) {
		if (!in_the_debugger)
			g_warning ("Symbol file %s has incorrect version (expected %d.%d, got %d)",
				   symfile->filename, MONO_SYMBOL_FILE_MAJOR_VERSION,
				   MONO_SYMBOL_FILE_MINOR_VERSION, major);
		return FALSE;
	}

	guid = mono_guid_to_string ((const uint8_t *) ptr);
	ptr += 16;

	if (strcmp (handle->image->guid, guid)) {
		if (!in_the_debugger)
			g_warning ("Symbol file %s doesn't match image %s", symfile->filename,
				   handle->image->name);
		if (guid)
			g_free (guid);
		return FALSE;
	}

	symfile->major_version = major;
	symfile->minor_version = minor;

	symfile->offset_table = (MonoSymbolFileOffsetTable *) ptr;

	symfile->method_hash = g_hash_table_new_full (
		NULL, NULL, NULL, (GDestroyNotify) free_method_info);

	symfile->source_hash = g_hash_table_new_full (
		NULL, NULL, NULL, (GDestroyNotify) free_source_info);

	g_free (guid);
	return TRUE;
}

/**
 * mono_debug_open_mono_symbols:
 */
MonoSymbolFile *
mono_debug_open_mono_symbols (MonoDebugHandle *handle, const uint8_t *raw_contents,
			      int size, gboolean in_the_debugger)
{
	MonoSymbolFile *symfile;

	mono_debugger_lock ();
	symfile = g_new0 (MonoSymbolFile, 1);

	if (raw_contents != NULL) {
		unsigned char *p;
		symfile->raw_contents_size = size;
		symfile->raw_contents = p = (unsigned char *)g_malloc (size);
		memcpy (p, raw_contents, size);
		symfile->filename = g_strdup_printf ("LoadedFromMemory");
		symfile->was_loaded_from_memory = TRUE;
	} else {
		MonoFileMap *f;

		symfile->filename = g_strdup_printf ("%s.mdb", mono_image_get_filename (handle->image));
		symfile->was_loaded_from_memory = FALSE;
		if ((f = mono_file_map_open (symfile->filename))) {
			symfile->raw_contents_size = mono_file_map_size (f);
			if (symfile->raw_contents_size == 0) {
				if (!in_the_debugger)
					g_warning ("stat of %s failed: %s",
						   symfile->filename,  g_strerror (errno));
			} else {
				symfile->raw_contents = (const unsigned char *)mono_file_map (symfile->raw_contents_size, MONO_MMAP_READ|MONO_MMAP_PRIVATE, mono_file_map_fd (f), 0, &symfile->raw_contents_handle);
			}

			mono_file_map_close (f);
		}
	}
	
	if (load_symfile (handle, symfile, in_the_debugger)) {
		mono_debugger_unlock ();
		return symfile;
	} else if (!in_the_debugger) {
		mono_debug_close_mono_symbol_file (symfile);
		mono_debugger_unlock ();
		return NULL;
	}

	mono_debugger_unlock ();
	return symfile;
}

/**
 * mono_debug_close_mono_symbol_file:
 */
void
mono_debug_close_mono_symbol_file (MonoSymbolFile *symfile)
{
	if (!symfile)
		return;

	mono_debugger_lock ();
	if (symfile->method_hash)
		g_hash_table_destroy (symfile->method_hash);

	if (symfile->raw_contents) {
		if (symfile->was_loaded_from_memory)
			g_free ((gpointer)symfile->raw_contents);
		else
			mono_file_unmap ((gpointer) symfile->raw_contents, symfile->raw_contents_handle);
	}

	g_free (symfile->filename);
	g_free (symfile);
	mono_debugger_unlock ();
}

/**
 * mono_debug_symfile_is_loaded:
 */
mono_bool
mono_debug_symfile_is_loaded (MonoSymbolFile *symfile)
{
	return symfile && symfile->offset_table;
}

static int
read_leb128 (const uint8_t *ptr, const uint8_t **rptr)
{
	int ret = 0;
	int shift = 0;
	char b;

	do {
		b = *ptr++;
				
		ret = ret | ((b & 0x7f) << shift);
		shift += 7;
	} while ((b & 0x80) == 0x80);

	if (rptr)
		*rptr = ptr;

	return ret;
}

static gchar *
read_string (const uint8_t *ptr, const uint8_t **endp)
{
	gchar *s;
	int len = read_leb128 (ptr, &ptr);

	s = g_filename_from_utf8 ((const char *) ptr, len, NULL, NULL, NULL);
	ptr += len;
	if (endp)
		*endp = ptr;
	return s;
}

typedef struct {
	MonoSymbolFile *symfile;
	int line_base, line_range, max_address_incr;
	uint8_t opcode_base;
	uint32_t last_line, last_file, last_offset;
	uint32_t first_file;
	int line, file, offset;
	gboolean is_hidden;
} StatementMachine;

static gboolean
check_line (StatementMachine *stm, int offset, MonoDebugSourceLocation **location)
{
	gchar *source_file = NULL;

	if (stm->offset <= offset) {
		stm->last_offset = stm->offset;
		stm->last_file = stm->file;
		if (stm->line != 0xfeefee)
			stm->last_line = stm->line;
		return FALSE;
	}

	if (stm->last_file) {
		int offset = read32(&(stm->symfile->offset_table->_source_table_offset)) +
			(stm->last_file - 1) * sizeof (MonoSymbolFileSourceEntry);
		MonoSymbolFileSourceEntry *se = (MonoSymbolFileSourceEntry *)
			(stm->symfile->raw_contents + offset);

		source_file = read_string (stm->symfile->raw_contents + read32(&(se->_data_offset)), NULL);
	}

	if (stm->last_line == 0) {
		/* 
		 * The IL offset is less than the first IL offset which has a corresponding
		 * source line.
		 */
		*location = NULL;
		return TRUE;
	}

	*location = g_new0 (MonoDebugSourceLocation, 1);
	(*location)->source_file = source_file;
	(*location)->row = stm->last_line;
	(*location)->il_offset = stm->last_offset;
	return TRUE;
}

/**
 * mono_debug_symfile_lookup_location:
 * \param minfo A \c MonoDebugMethodInfo which can be retrieved by \c mono_debug_lookup_method.
 * \param offset IL offset within the corresponding method's CIL code.
 *
 * This function is similar to \c mono_debug_lookup_location, but we
 * already looked up the method and also already did the
 * native address -> IL offset mapping.
 */
MonoDebugSourceLocation *
mono_debug_symfile_lookup_location (MonoDebugMethodInfo *minfo, uint32_t offset)
{
	MonoDebugSourceLocation *location = NULL;
	MonoSymbolFile *symfile;
	const unsigned char *ptr;
	StatementMachine stm;

#define DW_LNS_copy 1
#define DW_LNS_advance_pc 2
#define DW_LNS_advance_line 3
#define DW_LNS_set_file 4
#define DW_LNS_const_add_pc 8

#define DW_LNE_end_sequence 1
#define DW_LNE_MONO_negate_is_hidden 0x40

#define DW_LNE_MONO__extensions_start 0x40
#define DW_LNE_MONO__extensions_end 0x7f

	if ((symfile = minfo->handle->symfile) == NULL)
		return NULL;

	stm.line_base = read32 (&symfile->offset_table->_line_number_table_line_base);
	stm.line_range = read32 (&symfile->offset_table->_line_number_table_line_range);
	stm.opcode_base = (uint8_t) read32 (&symfile->offset_table->_line_number_table_opcode_base);
	stm.max_address_incr = (255 - stm.opcode_base) / stm.line_range;

	mono_debugger_lock ();

	ptr = symfile->raw_contents + minfo->lnt_offset;

	stm.symfile = symfile;
	stm.offset = stm.last_offset = 0;
	stm.last_file = 0;
	stm.last_line = 0;
	stm.first_file = 0;
	stm.file = 1;
	stm.line = 1;
	stm.is_hidden = FALSE;

	while (TRUE) {
		uint8_t opcode = *ptr++;

		if (opcode == 0) {
			uint8_t size = *ptr++;
			const unsigned char *end_ptr = ptr + size;

			opcode = *ptr++;

			if (opcode == DW_LNE_end_sequence) {
				if (check_line (&stm, -1, &location))
					goto out_success;
				break;
			} else if (opcode == DW_LNE_MONO_negate_is_hidden) {
				stm.is_hidden = !stm.is_hidden;
			} else if ((opcode >= DW_LNE_MONO__extensions_start) &&
				   (opcode <= DW_LNE_MONO__extensions_end)) {
				; // reserved for future extensions
			} else {
				g_warning ("Unknown extended opcode %x in LNT", opcode);
			}

			ptr = end_ptr;
			continue;
		} else if (opcode < stm.opcode_base) {
			switch (opcode) {
			case DW_LNS_copy:
				if (check_line (&stm, offset, &location))
					goto out_success;
				break;
			case DW_LNS_advance_pc:
				stm.offset += read_leb128 (ptr, &ptr);
				break;
			case DW_LNS_advance_line:
				stm.line += read_leb128 (ptr, &ptr);
				break;
			case DW_LNS_set_file:
				stm.file = read_leb128 (ptr, &ptr);
				break;
			case DW_LNS_const_add_pc:
				stm.offset += stm.max_address_incr;
				break;
			default:
				g_warning ("Unknown standard opcode %x in LNT", opcode);
				goto error_out;
			}
		} else {
			opcode -= stm.opcode_base;

			stm.offset += opcode / stm.line_range;
			stm.line += stm.line_base + (opcode % stm.line_range);

			if (check_line (&stm, offset, &location))
				goto out_success;
		}
	}

 error_out:
	mono_debugger_unlock ();
	return NULL;

 out_success:
	mono_debugger_unlock ();
	return location;
}

static void
add_line (StatementMachine *stm, GPtrArray *il_offset_array, GPtrArray *line_number_array, GPtrArray *source_file_array, GPtrArray *hidden_array)
{
	g_ptr_array_add (il_offset_array, GUINT_TO_POINTER (stm->offset));
	g_ptr_array_add (line_number_array, GUINT_TO_POINTER (stm->line));
	g_ptr_array_add (source_file_array, GUINT_TO_POINTER (stm->file));
	g_ptr_array_add (hidden_array, GUINT_TO_POINTER (stm->is_hidden || stm->line <= 0));

	if (!stm->is_hidden && !stm->first_file)
		stm->first_file = stm->file;
}

/**
 * mono_debug_symfile_free_location:
 *
 * Free a \c MonoDebugSourceLocation returned by
 * \c mono_debug_symfile_lookup_location
 */
void
mono_debug_symfile_free_location (MonoDebugSourceLocation  *location)
{
	g_free (location->source_file);
	g_free (location);
}

/*
 * LOCKING: Assumes the debugger lock is held.
 */
static MonoDebugSourceInfo*
get_source_info (MonoSymbolFile *symfile, int index)
{
	MonoDebugSourceInfo *info;

	info = (MonoDebugSourceInfo *)g_hash_table_lookup (symfile->source_hash, GUINT_TO_POINTER (index));
	if (!info) {
		int offset = read32(&(symfile->offset_table->_source_table_offset)) +
			(index - 1) * sizeof (MonoSymbolFileSourceEntry);
		MonoSymbolFileSourceEntry *se = (MonoSymbolFileSourceEntry *)
			(symfile->raw_contents + offset);
		const uint8_t *ptr = symfile->raw_contents + read32(&(se->_data_offset));

		info = g_new0 (MonoDebugSourceInfo, 1);
		info->source_file = read_string (ptr, &ptr);
		info->guid = (guint8 *)g_malloc0 (16);
		memcpy (info->guid, ptr, 16);
		ptr += 16;
		info->hash = (guint8 *)g_malloc0 (16);
		memcpy (info->hash, ptr, 16);
		ptr += 16;
		g_hash_table_insert (symfile->source_hash, GUINT_TO_POINTER (index), info);
	}
	return info;
}

typedef enum {
	LNT_FLAG_HAS_COLUMN_INFO = 1 << 1,
	LNT_FLAG_HAS_END_INFO = 1 << 2,
} LineNumberTableFlags;

static LineNumberTableFlags
method_get_lnt_flags (MonoDebugMethodInfo *minfo)
{
	MonoSymbolFile *symfile;
	const unsigned char *ptr;
	guint32 flags;

	if ((symfile = minfo->handle->symfile) == NULL)
		return (LineNumberTableFlags)0;

	ptr = symfile->raw_contents + minfo->data_offset;

	/* Has to read 'flags' which is preceeded by a bunch of other data */
	/* compile_unit_index */
	read_leb128 (ptr, &ptr);
	/* local variable table offset */
	read_leb128 (ptr, &ptr);
	/* namespace id */
	read_leb128 (ptr, &ptr);
	/* code block table offset */
	read_leb128 (ptr, &ptr);
	/* scope variable table offset */
	read_leb128 (ptr, &ptr);
	/* real name offset */
	read_leb128 (ptr, &ptr);

	flags = read_leb128 (ptr, &ptr);
	return (LineNumberTableFlags)flags;
}

/*
 * mono_debug_symfile_get_seq_points:
 *
 * On return, SOURCE_FILE_LIST will point to a GPtrArray of MonoDebugSourceFile
 * structures, and SOURCE_FILES will contain indexes into this array.
 * The MonoDebugSourceFile structures are owned by this module.
 */
void
mono_debug_symfile_get_seq_points (MonoDebugMethodInfo *minfo, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points)
{
	// FIXME: Unify this with mono_debug_symfile_lookup_location
	MonoSymbolFile *symfile;
	const unsigned char *ptr;
	StatementMachine stm;
	uint32_t i, j, n;
	LineNumberTableFlags flags;
	GPtrArray *il_offset_array, *line_number_array, *source_file_array, *hidden_array;
	gboolean has_column_info, has_end_info;
	MonoSymSeqPoint *sps;

	if (source_file_list)
		*source_file_list = NULL;
	if (seq_points)
		*seq_points = NULL;
	if (n_seq_points)
		*n_seq_points = 0;
	if (source_files)
		*source_files = NULL;
	if (source_file)
		*source_file = NULL;

	if ((symfile = minfo->handle->symfile) == NULL)
		return;

	flags = method_get_lnt_flags (minfo);
	has_column_info = (flags & LNT_FLAG_HAS_COLUMN_INFO) > 0;
	has_end_info = (flags & LNT_FLAG_HAS_END_INFO) > 0;

	il_offset_array = g_ptr_array_new ();
	line_number_array = g_ptr_array_new ();
	source_file_array = g_ptr_array_new ();
	hidden_array = g_ptr_array_new();

	stm.line_base = read32 (&symfile->offset_table->_line_number_table_line_base);
	stm.line_range = read32 (&symfile->offset_table->_line_number_table_line_range);
	stm.opcode_base = (uint8_t) read32 (&symfile->offset_table->_line_number_table_opcode_base);
	stm.max_address_incr = (255 - stm.opcode_base) / stm.line_range;

	mono_debugger_lock ();

	ptr = symfile->raw_contents + minfo->lnt_offset;

	stm.symfile = symfile;
	stm.offset = stm.last_offset = 0;
	stm.last_file = 0;
	stm.last_line = 0;
	stm.first_file = 0;
	stm.file = 1;
	stm.line = 1;
	stm.is_hidden = FALSE;

	while (TRUE) {
		uint8_t opcode = *ptr++;

		if (opcode == 0) {
			uint8_t size = *ptr++;
			const unsigned char *end_ptr = ptr + size;

			opcode = *ptr++;

			if (opcode == DW_LNE_end_sequence) {
				if (il_offset_array->len == 0)
					/* Empty table */
					break;
				break;
			} else if (opcode == DW_LNE_MONO_negate_is_hidden) {
				stm.is_hidden = !stm.is_hidden;
			} else if ((opcode >= DW_LNE_MONO__extensions_start) &&
				   (opcode <= DW_LNE_MONO__extensions_end)) {
				; // reserved for future extensions
			} else {
				g_warning ("Unknown extended opcode %x in LNT", opcode);
			}

			ptr = end_ptr;
			continue;
		} else if (opcode < stm.opcode_base) {
			switch (opcode) {
			case DW_LNS_copy:
				add_line (&stm, il_offset_array, line_number_array, source_file_array, hidden_array);
				break;
			case DW_LNS_advance_pc:
				stm.offset += read_leb128 (ptr, &ptr);
				break;
			case DW_LNS_advance_line:
				stm.line += read_leb128 (ptr, &ptr);
				break;
			case DW_LNS_set_file:
				stm.file = read_leb128 (ptr, &ptr);
				break;
			case DW_LNS_const_add_pc:
				stm.offset += stm.max_address_incr;
				break;
			default:
				g_warning ("Unknown standard opcode %x in LNT", opcode);
				g_assert_not_reached ();
			}
		} else {
			opcode -= stm.opcode_base;

			stm.offset += opcode / stm.line_range;
			stm.line += stm.line_base + (opcode % stm.line_range);

			add_line (&stm, il_offset_array, line_number_array, source_file_array, hidden_array);
		}
	}

	if (!stm.file && stm.first_file)
		stm.file = stm.first_file;

	if (stm.file && source_file) {
		int offset = read32(&(stm.symfile->offset_table->_source_table_offset)) +
			(stm.file - 1) * sizeof (MonoSymbolFileSourceEntry);
		MonoSymbolFileSourceEntry *se = (MonoSymbolFileSourceEntry *)
			(stm.symfile->raw_contents + offset);

		if (source_file)
			*source_file = read_string (stm.symfile->raw_contents + read32(&(se->_data_offset)), NULL);
	}

	if (source_file_list) {
		int file, last_file = 0;

		*source_file_list = g_ptr_array_new ();
		if (source_files)
			*source_files = (int *)g_malloc (il_offset_array->len * sizeof (int));

		for (i = 0; i < il_offset_array->len; ++i) {
			file = GPOINTER_TO_UINT (g_ptr_array_index (source_file_array, i));
			if (file && file != last_file) {
				MonoDebugSourceInfo *info = get_source_info (symfile, file);

				g_ptr_array_add (*source_file_list, info);
			}
			last_file = file;
			if (source_files)
				(*source_files) [i] = (*source_file_list)->len - 1;
		}
	}				

	if (n_seq_points) {
		g_assert (seq_points);

		n = il_offset_array->len;
		for (i = 0; i < il_offset_array->len; i++) {
			if (GPOINTER_TO_UINT (g_ptr_array_index (hidden_array, i))) {
				n --;
			}
		}

		*n_seq_points = n;
		*seq_points = sps = g_new0 (MonoSymSeqPoint, n);
		j = 0;
		for (i = 0; i < il_offset_array->len; ++i) {
			MonoSymSeqPoint *sp = &(sps [j]);
			if (!GPOINTER_TO_UINT (g_ptr_array_index (hidden_array, i))) {
				sp->il_offset = GPOINTER_TO_UINT (g_ptr_array_index (il_offset_array, i));
				sp->line = GPOINTER_TO_UINT (g_ptr_array_index (line_number_array, i));
				sp->column = -1;
				sp->end_line = -1;
				sp->end_column = -1;
				j ++;
			}
		}

		if (has_column_info) {
			j = 0;
			for (i = 0; i < il_offset_array->len; ++i) {
				MonoSymSeqPoint *sp = &(sps [j]);
				int column = read_leb128 (ptr, &ptr);
				if (!GPOINTER_TO_UINT (g_ptr_array_index (hidden_array, i))) {
					sp->column = column;
					j++;
				}
			}
		}

		if (has_end_info) {
			j = 0;
			for (i = 0; i < il_offset_array->len; ++i) {
				MonoSymSeqPoint *sp = &(sps [j]);
				int end_row, end_column = -1;

				end_row = read_leb128 (ptr, &ptr);
				if (end_row != 0xffffff) {
					end_row += GPOINTER_TO_UINT (g_ptr_array_index (line_number_array, i));
					end_column = read_leb128 (ptr, &ptr);
					if (!GPOINTER_TO_UINT (g_ptr_array_index (hidden_array, i))) {
						sp->end_line = end_row;
						sp->end_column = end_column;
						j++;
					}
				}
			}
		}
	}

	g_ptr_array_free (il_offset_array, TRUE);
	g_ptr_array_free (line_number_array, TRUE);
	g_ptr_array_free (hidden_array, TRUE);

	mono_debugger_unlock ();
	return;
}

static int
compare_method (const void *key, const void *object)
{
	uint32_t token = GPOINTER_TO_UINT (key);
	MonoSymbolFileMethodEntry *me = (MonoSymbolFileMethodEntry*)object;

	return token - read32(&(me->_token));
}

/**
 * mono_debug_symfile_lookup_method:
 */
MonoDebugMethodInfo *
mono_debug_symfile_lookup_method (MonoDebugHandle *handle, MonoMethod *method)
{
	MonoSymbolFileMethodEntry *first_ie, *ie;
	MonoDebugMethodInfo *minfo;
	MonoSymbolFile *symfile = handle->symfile;

	if (!symfile->method_hash)
		return NULL;

	if (handle->image != mono_class_get_image (mono_method_get_class (method)))
		return NULL;

	mono_debugger_lock ();

	minfo = (MonoDebugMethodInfo *)g_hash_table_lookup (symfile->method_hash, method);
	if (minfo) {
		mono_debugger_unlock ();
		return minfo;
	}

	first_ie = (MonoSymbolFileMethodEntry *)
		(symfile->raw_contents + read32(&(symfile->offset_table->_method_table_offset)));

	ie = (MonoSymbolFileMethodEntry *)mono_binary_search (GUINT_TO_POINTER (mono_method_get_token (method)), first_ie,
				   read32(&(symfile->offset_table->_method_count)),
				   sizeof (MonoSymbolFileMethodEntry), compare_method);

	if (!ie) {
		mono_debugger_unlock ();
		return NULL;
	}

	minfo = g_new0 (MonoDebugMethodInfo, 1);
	minfo->index = (ie - first_ie) + 1;
	minfo->method = method;
	minfo->handle = handle;

	minfo->data_offset = read32 (&(ie->_data_offset));
	minfo->lnt_offset = read32 (&(ie->_line_number_table));

	g_hash_table_insert (symfile->method_hash, method, minfo);

	mono_debugger_unlock ();
	return minfo;
}

/**
 * mono_debug_symfile_lookup_locals:
 *
 * Return information about the local variables of \p minfo from the symbol file.
 * Return NULL if no information can be found.
 * The result should be freed using \c mono_debug_symfile_free_locals.
 */
MonoDebugLocalsInfo*
mono_debug_symfile_lookup_locals (MonoDebugMethodInfo *minfo)
{
	MonoSymbolFile *symfile = minfo->handle->symfile;
	const uint8_t *p;
	int i, len, locals_offset, num_locals, block_index;
	int code_block_table_offset;
	MonoDebugLocalsInfo *res;

	if (!symfile)
		return NULL;

	p = symfile->raw_contents + minfo->data_offset;

	/* compile_unit_index = */ read_leb128 (p, &p);
	locals_offset = read_leb128 (p, &p);
	/* namespace_id = */ read_leb128 (p, &p);
	code_block_table_offset = read_leb128 (p, &p);

	res = g_new0 (MonoDebugLocalsInfo, 1);

	p = symfile->raw_contents + code_block_table_offset;
	res->num_blocks = read_leb128 (p, &p);
	res->code_blocks = g_new0 (MonoDebugCodeBlock, res->num_blocks);
	for (i = 0; i < res->num_blocks; ++i) {
		res->code_blocks [i].type = read_leb128 (p, &p);
		res->code_blocks [i].parent = read_leb128 (p, &p);
		res->code_blocks [i].start_offset = read_leb128 (p, &p);
		res->code_blocks [i].end_offset = read_leb128 (p, &p);
	}

	p = symfile->raw_contents + locals_offset;
	num_locals = read_leb128 (p, &p);

	res->num_locals = num_locals;
	res->locals = g_new0 (MonoDebugLocalVar, num_locals);

	for (i = 0; i < num_locals; ++i) {
		res->locals [i].index = read_leb128 (p, &p);
		len = read_leb128 (p, &p);
		res->locals [i].name = (char *)g_malloc (len + 1);
		memcpy (res->locals [i].name, p, len);
		res->locals [i].name [len] = '\0';
		p += len;
		block_index = read_leb128 (p, &p);
		if (block_index >= 1 && block_index <= res->num_blocks)
			res->locals [i].block = &res->code_blocks [block_index - 1];
	}

	return res;
}

#else /* DISABLE_MDB */

MonoSymbolFile *
mono_debug_open_mono_symbols (MonoDebugHandle *handle, const uint8_t *raw_contents,
			      int size, gboolean in_the_debugger)
{
	return NULL;
}

void
mono_debug_close_mono_symbol_file (MonoSymbolFile *symfile)
{
}

mono_bool
mono_debug_symfile_is_loaded (MonoSymbolFile *symfile)
{
	return FALSE;
}

MonoDebugMethodInfo *
mono_debug_symfile_lookup_method (MonoDebugHandle *handle, MonoMethod *method)
{
	return NULL;
}

void
mono_debug_symfile_get_seq_points (MonoDebugMethodInfo *minfo, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points)
{
	g_assert_not_reached ();
}

MonoDebugSourceLocation *
mono_debug_symfile_lookup_location (MonoDebugMethodInfo *minfo, uint32_t offset)
{
	return NULL;
}

MonoDebugLocalsInfo*
mono_debug_symfile_lookup_locals (MonoDebugMethodInfo *minfo)
{
	return NULL;
}

void
mono_debug_symfile_free_location (MonoDebugSourceLocation  *location)
{
}

#endif
