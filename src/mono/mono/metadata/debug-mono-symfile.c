/*
 * debug-mono-symfile.c: 
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright (C) 2005-2008 Novell, Inc. (http://www.novell.com)
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
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/utils/mono-mmap.h>

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
	MonoSymbolFileOffsetTable *offset_table;
	gboolean was_loaded_from_memory;
};

static void
free_method_info (MonoDebugMethodInfo *minfo)
{
	g_free (minfo);
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
				   handle->image_file);
		if (guid)
			g_free (guid);
		return FALSE;
	}

	symfile->major_version = major;
	symfile->minor_version = minor;

	symfile->offset_table = (MonoSymbolFileOffsetTable *) ptr;

	symfile->method_hash = g_hash_table_new_full (
		NULL, NULL, NULL, (GDestroyNotify) free_method_info);

	g_free (guid);
	return TRUE;
}

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
		symfile->raw_contents = p = g_malloc (size);
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
				symfile->raw_contents = mono_file_map (symfile->raw_contents_size, MONO_MMAP_READ|MONO_MMAP_PRIVATE, mono_file_map_fd (f), 0, &symfile->raw_contents_handle);
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

	if (symfile->filename)
		g_free (symfile->filename);
	g_free (symfile);
	mono_debugger_unlock ();
}

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
read_string (const uint8_t *ptr)
{
	int len = read_leb128 (ptr, &ptr);
	return g_filename_from_utf8 ((const char *) ptr, len, NULL, NULL, NULL);
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

		source_file = read_string (stm->symfile->raw_contents + read32(&(se->_data_offset)));
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
 * @minfo: A `MonoDebugMethodInfo' which can be retrieved by
 *         mono_debug_lookup_method().
 * @offset: IL offset within the corresponding method's CIL code.
 *
 * This function is similar to mono_debug_lookup_location(), but we
 * already looked up the method and also already did the
 * `native address -> IL offset' mapping.
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
add_line (StatementMachine *stm, GPtrArray *il_offset_array, GPtrArray *line_number_array)
{
	if (stm->line > 0) {
		g_ptr_array_add (il_offset_array, GUINT_TO_POINTER (stm->offset));
		g_ptr_array_add (line_number_array, GUINT_TO_POINTER (stm->line));
	}

	if (!stm->is_hidden && !stm->first_file)
		stm->first_file = stm->file;
}

/*
 * mono_debug_symfile_free_location:
 *
 *   Free a MonoDebugSourceLocation returned by
 *   mono_debug_symfile_lookup_location
 */
void
mono_debug_symfile_free_location   (MonoDebugSourceLocation  *location)
{
	g_free (location->source_file);
	g_free (location);
}

/*
 * mono_debug_symfile_get_line_numbers:
 *
 *   All the output parameters can be NULL.
 */ 
void
mono_debug_symfile_get_line_numbers (MonoDebugMethodInfo *minfo, char **source_file, int *n_il_offsets, int **il_offsets, int **line_numbers)
{
	// FIXME: Unify this with mono_debug_symfile_lookup_location
	MonoSymbolFile *symfile;
	const unsigned char *ptr;
	StatementMachine stm;
	uint32_t i;
	GPtrArray *il_offset_array, *line_number_array;

	if (source_file)
		*source_file = NULL;
	if (n_il_offsets)
		*n_il_offsets = 0;

	if ((symfile = minfo->handle->symfile) == NULL)
		return;

	il_offset_array = g_ptr_array_new ();
	line_number_array = g_ptr_array_new ();

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
				add_line (&stm, il_offset_array, line_number_array);
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
				add_line (&stm, il_offset_array, line_number_array);
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

			add_line (&stm, il_offset_array, line_number_array);
		}
	}

	if (!stm.file && stm.first_file)
		stm.file = stm.first_file;

	if (stm.file) {
		int offset = read32(&(stm.symfile->offset_table->_source_table_offset)) +
			(stm.file - 1) * sizeof (MonoSymbolFileSourceEntry);
		MonoSymbolFileSourceEntry *se = (MonoSymbolFileSourceEntry *)
			(stm.symfile->raw_contents + offset);

		if (source_file)
			*source_file = read_string (stm.symfile->raw_contents + read32(&(se->_data_offset)));
	}

	if (n_il_offsets)
		*n_il_offsets = il_offset_array->len;
	if (il_offsets && line_numbers) {
		*il_offsets = g_malloc (il_offset_array->len * sizeof (int));
		*line_numbers = g_malloc (il_offset_array->len * sizeof (int));
		for (i = 0; i < il_offset_array->len; ++i) {
			(*il_offsets) [i] = GPOINTER_TO_UINT (g_ptr_array_index (il_offset_array, i));
			(*line_numbers) [i] = GPOINTER_TO_UINT (g_ptr_array_index (line_number_array, i));
		}
	}
	g_ptr_array_free (il_offset_array, TRUE);
	g_ptr_array_free (line_number_array, TRUE);

	mono_debugger_unlock ();
	return;
}

int32_t
_mono_debug_address_from_il_offset (MonoDebugMethodJitInfo *jit, uint32_t il_offset)
{
	int i;

	if (!jit || !jit->line_numbers)
		return -1;

	for (i = jit->num_line_numbers - 1; i >= 0; i--) {
		MonoDebugLineNumberEntry lne = jit->line_numbers [i];

		if (lne.il_offset < 0)
			continue;
		if (lne.il_offset <= il_offset)
			return lne.native_offset;
	}

	return 0;
}

static int
compare_method (const void *key, const void *object)
{
	uint32_t token = GPOINTER_TO_UINT (key);
	MonoSymbolFileMethodEntry *me = (MonoSymbolFileMethodEntry*)object;

	return token - read32(&(me->_token));
}

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

	minfo = g_hash_table_lookup (symfile->method_hash, method);
	if (minfo) {
		mono_debugger_unlock ();
		return minfo;
	}

	first_ie = (MonoSymbolFileMethodEntry *)
		(symfile->raw_contents + read32(&(symfile->offset_table->_method_table_offset)));

	ie = bsearch (GUINT_TO_POINTER (mono_method_get_token (method)), first_ie,
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

/*
 * mono_debug_symfile_lookup_locals:
 *
 *   Return information about the local variables of MINFO from the symbol file.
 * Return NULL if no information can be found.
 * The result should be freed using mono_debug_symfile_free_locals ().
 */
MonoDebugLocalsInfo*
mono_debug_symfile_lookup_locals (MonoDebugMethodInfo *minfo)
{
	MonoSymbolFile *symfile = minfo->handle->symfile;
	const uint8_t *p;
	int i, len, compile_unit_index, locals_offset, num_locals, block_index;
	int namespace_id, code_block_table_offset;
	MonoDebugLocalsInfo *res;

	if (!symfile)
		return NULL;

	p = symfile->raw_contents + minfo->data_offset;

	compile_unit_index = read_leb128 (p, &p);
	locals_offset = read_leb128 (p, &p);
	namespace_id = read_leb128 (p, &p);
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
		res->locals [i].name = g_malloc (len + 1);
		memcpy (res->locals [i].name, p, len);
		res->locals [i].name [len] = '\0';
		p += len;
		block_index = read_leb128 (p, &p);
		if (block_index >= 1 && block_index <= res->num_blocks)
			res->locals [i].block = &res->code_blocks [block_index - 1];
	}

	return res;
}

/*
 * mono_debug_symfile_free_locals:
 *
 *   Free all the data allocated by mono_debug_symfile_lookup_locals ().
 */
void
mono_debug_symfile_free_locals (MonoDebugLocalsInfo *info)
{
	int i;

	for (i = 0; i < info->num_locals; ++i)
		g_free (info->locals [i].name);
	g_free (info->locals);
	g_free (info->code_blocks);
	g_free (info);
}
