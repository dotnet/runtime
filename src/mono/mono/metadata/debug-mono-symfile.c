#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>
#include <string.h>
#include <signal.h>
#ifdef HAVE_SYS_PARAM_H
#include <sys/param.h>
#endif
#include <sys/stat.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/rawbuffer.h>
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

#include <fcntl.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#define RANGE_TABLE_CHUNK_SIZE		256
#define CLASS_TABLE_CHUNK_SIZE		256
#define TYPE_TABLE_PTR_CHUNK_SIZE	256
#define TYPE_TABLE_CHUNK_SIZE		65536

static void
free_method_info (MonoDebugMethodInfo *minfo)
{
	g_free (minfo);
}

static gchar *
get_class_name (MonoClass *klass)
{
	MonoClass *nested_in = mono_class_get_nesting_type (klass);
	const char *name_space;
	if (nested_in) {
		gchar *parent_name = get_class_name (nested_in);
		gchar *name = g_strdup_printf ("%s.%s", parent_name, mono_class_get_name (klass));
		g_free (parent_name);
		return name;
	}

	name_space = mono_class_get_namespace (klass);
	return g_strdup_printf ("%s%s%s", name_space,
				name_space [0] ? "." : "", mono_class_get_name (klass));
}

static int
load_symfile (MonoDebugHandle *handle, MonoSymbolFile *symfile, gboolean in_the_debugger)
{
	const char *ptr, *start;
	gchar *guid;
	guint64 magic;
	int minor, major;

	ptr = start = (const char*)symfile->raw_contents;
	if (!ptr)
		return FALSE;

	magic = read64(ptr);
	ptr += sizeof(guint64);
	if (magic != MONO_SYMBOL_FILE_MAGIC) {
		if (!in_the_debugger)
			g_warning ("Symbol file %s is not a mono symbol file", symfile->filename);
		return FALSE;
	}

	major = read32(ptr);
	ptr += sizeof(guint32);
	minor = read32(ptr);
	ptr += sizeof(guint32);

	if ((major != MONO_SYMBOL_FILE_MAJOR_VERSION) || (minor != MONO_SYMBOL_FILE_MINOR_VERSION)) {
		if (!in_the_debugger)
			g_warning ("Symbol file %s has incorrect version "
				   "(expected %d.%d, got %d.%d)", symfile->filename,
				   MONO_SYMBOL_FILE_MAJOR_VERSION, MONO_SYMBOL_FILE_MINOR_VERSION,
				   major, minor);
		return FALSE;
	}

	guid = mono_guid_to_string ((const guint8 *) ptr);
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
		g_direct_hash, g_direct_equal, NULL, (GDestroyNotify) free_method_info);

	g_free (guid);
	return TRUE;
}

MonoSymbolFile *
mono_debug_open_mono_symbols (MonoDebugHandle *handle, const guint8 *raw_contents,
			      int size, gboolean in_the_debugger)
{
	MonoSymbolFile *symfile;
	FILE* f;

	mono_debugger_lock ();
	symfile = g_new0 (MonoSymbolFile, 1);

	if (raw_contents != NULL) {
		unsigned char *p;
		symfile->raw_contents_size = size;
		symfile->raw_contents = p = g_malloc (size);
		memcpy (p, raw_contents, size);
		symfile->filename = g_strdup_printf ("LoadedFromMemory");
	} else {
		symfile->filename = g_strdup_printf ("%s.mdb", mono_image_get_filename (handle->image));

		if ((f = fopen (symfile->filename, "rb"))) {
			struct stat stat_buf;
			
			if (fstat (fileno (f), &stat_buf) < 0) {
				if (!in_the_debugger)
					g_warning ("stat of %s failed: %s",
						   symfile->filename,  g_strerror (errno));
			} else {
				symfile->raw_contents_size = stat_buf.st_size;
				symfile->raw_contents = mono_raw_buffer_load (fileno (f), FALSE, 0, stat_buf.st_size);
			}

			fclose (f);
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

	if (symfile->raw_contents)
		mono_raw_buffer_free ((gpointer) symfile->raw_contents);

	if (symfile->filename)
		g_free (symfile->filename);
	g_free (symfile);
	mono_debugger_unlock ();
}

static int
read_leb128 (const guint8 *ptr, const guint8 **rptr)
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
read_string (const guint8 *ptr)
{
	int len = read_leb128 (ptr, &ptr);
	return g_filename_from_utf8 ((const char *) ptr, len, NULL, NULL, NULL);
}

typedef struct {
	MonoSymbolFile *symfile;
	int line_base, line_range, max_address_incr;
	guint8 opcode_base;
	guint32 last_line, last_file, last_offset;
	guint32 line, file, offset;
} StatementMachine;

static gboolean
check_line (StatementMachine *stm, int offset, MonoDebugSourceLocation **location)
{
	gchar *source_file = NULL;

	if ((offset > 0) && (stm->offset <= offset)) {
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
mono_debug_symfile_lookup_location (MonoDebugMethodInfo *minfo, guint32 offset)
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

	if ((symfile = minfo->handle->symfile) == NULL)
		return NULL;

	stm.line_base = symfile->offset_table->_line_number_table_line_base;
	stm.line_range = symfile->offset_table->_line_number_table_line_range;
	stm.opcode_base = (guint8) symfile->offset_table->_line_number_table_opcode_base;
	stm.max_address_incr = (255 - stm.opcode_base) / stm.line_range;

	mono_debugger_lock ();

	ptr = symfile->raw_contents + minfo->lnt_offset;

	stm.symfile = symfile;
	stm.offset = stm.last_offset = 0;
	stm.file = stm.last_file = 1;
	stm.line = stm.last_line = 1;

	while (TRUE) {
		guint8 opcode = *ptr++;

		if (opcode == 0) {
			guint8 size = *ptr++;
			const unsigned char *end_ptr = ptr + size;

			opcode = *ptr++;

			if (opcode == DW_LNE_end_sequence) {
				if (check_line (&stm, -1, &location))
					goto out_success;
				break;
			} else if (opcode == DW_LNE_MONO_negate_is_hidden) {
				;
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

gint32
_mono_debug_address_from_il_offset (MonoDebugMethodJitInfo *jit, guint32 il_offset)
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
	guint32 token = GPOINTER_TO_UINT (key);
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
