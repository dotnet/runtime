#include <config.h>
#include <stdlib.h>
#include <string.h>
#include <signal.h>
#include <sys/param.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/rawbuffer.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/debug-mono-symfile.h>

#include <fcntl.h>
#include <unistd.h>

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
	if (klass->nested_in) {
		gchar *parent_name = get_class_name (klass->nested_in);
		gchar *name = g_strdup_printf ("%s.%s", parent_name, klass->name);
		g_free (parent_name);
		return name;
	}

	return g_strdup_printf ("%s%s%s", klass->name_space,
				klass->name_space [0] ? "." : "", klass->name);
}

static int
load_symfile (MonoDebugHandle *handle, MonoSymbolFile *symfile)
{
	MonoSymbolFileMethodEntry *me;
	MonoSymbolFileMethodIndexEntry *ie;
	const char *ptr, *start;
	guint64 magic;
	long version;
	int i;

	ptr = start = symfile->raw_contents;
	if (!ptr)
		return FALSE;

	magic = *((guint64 *) ptr);
	ptr += sizeof(guint64);
	if (magic != MONO_SYMBOL_FILE_MAGIC) {
		g_warning ("Symbol file %s has is not a mono symbol file", handle->image_file);
		return FALSE;
	}

	version = *((guint32 *) ptr);
	ptr += sizeof(guint32);
	if (version != MONO_SYMBOL_FILE_VERSION) {
		g_warning ("Symbol file %s has incorrect version "
			   "(expected %d, got %ld)", handle->image_file,
			   MONO_SYMBOL_FILE_VERSION, version);
		return FALSE;
	}

	symfile->offset_table = (MonoSymbolFileOffsetTable *) ptr;

	/*
	 * Read method table.
	 *
	 */

	symfile->method_hash = g_hash_table_new_full (g_direct_hash, g_direct_equal, NULL,
							     (GDestroyNotify) free_method_info);

	ie = (MonoSymbolFileMethodIndexEntry *)
		(symfile->raw_contents + symfile->offset_table->method_table_offset);

	for (i = 0; i < symfile->offset_table->method_count; i++, me++, ie++) {
		MonoMethod *method;
		MonoDebugMethodInfo *minfo;

		me = (MonoSymbolFileMethodEntry *) (symfile->raw_contents + ie->file_offset);

		method = mono_get_method (handle->image, me->token, NULL);

		if (!method)
			continue;

		minfo = g_new0 (MonoDebugMethodInfo, 1);
		minfo->index = i + 1;
		minfo->method = method;
		minfo->handle = handle;
		minfo->num_il_offsets = me->num_line_numbers;
		minfo->il_offsets = (MonoSymbolFileLineNumberEntry *)
			(symfile->raw_contents + me->line_number_table_offset);
		minfo->entry = me;

		g_hash_table_insert (symfile->method_hash, method, minfo);
	}

	return TRUE;
}

static gconstpointer
open_symfile (MonoImage *image, guint32 *size)
{
	MonoTableInfo *table = &image->tables [MONO_TABLE_MANIFESTRESOURCE];
	guint32 i;
	guint32 cols [MONO_MANIFEST_SIZE];
	const char *val;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, cols, MONO_MANIFEST_SIZE);
		val = mono_metadata_string_heap (image, cols [MONO_MANIFEST_NAME]);
		if (!strcmp (val, "MonoSymbolFile"))
			break;
	}
	if (i == table->rows)
		return NULL;
	g_assert (!cols [MONO_MANIFEST_IMPLEMENTATION]);

	return mono_image_get_resource (image, cols [MONO_MANIFEST_OFFSET], size);
}

MonoSymbolFile *
mono_debug_open_mono_symbol_file (MonoDebugHandle *handle, gboolean create_symfile)
{
	MonoSymbolFile *symfile;

	symfile = g_new0 (MonoSymbolFile, 1);

	symfile->raw_contents = open_symfile (handle->image, &symfile->raw_contents_size);

	if (load_symfile (handle, symfile))
		return symfile;
	else if (!create_symfile) {
		mono_debug_close_mono_symbol_file (symfile);
		return NULL;
	}

	return symfile;
}

void
mono_debug_close_mono_symbol_file (MonoSymbolFile *symfile)
{
	if (!symfile)
		return;

	if (symfile->method_hash)
		g_hash_table_destroy (symfile->method_hash);

	g_free (symfile);
}

static gchar *
read_string (const char *ptr)
{
	int len = *((guint32 *) ptr);
	ptr += sizeof(guint32);
	return g_filename_from_utf8 (ptr, len, NULL, NULL, NULL);
}

gchar *
mono_debug_find_source_location (MonoSymbolFile *symfile, MonoMethod *method, guint32 offset,
				 guint32 *line_number)
{
	MonoSymbolFileLineNumberEntry *lne;
	MonoDebugMethodInfo *minfo;
	gchar *source_file = NULL;
	const char *ptr;
	int i;

	if (!symfile->method_hash)
		return NULL;

	minfo = g_hash_table_lookup (symfile->method_hash, method);
	if (!minfo)
		return NULL;

	if (minfo->entry->source_index) {
		int offset = symfile->offset_table->source_table_offset +
			(minfo->entry->source_index - 1) * sizeof (MonoSymbolFileSourceEntry);
		MonoSymbolFileSourceEntry *se = (MonoSymbolFileSourceEntry *) (symfile->raw_contents + offset);

		source_file = read_string (symfile->raw_contents + se->name_offset);
	}

	ptr = symfile->raw_contents + minfo->entry->line_number_table_offset;

	lne = (MonoSymbolFileLineNumberEntry *) ptr;

	for (i = 0; i < minfo->entry->num_line_numbers; i++, lne++) {
		if (lne->offset < offset)
			continue;

		if (line_number) {
			*line_number = lne->row;
			if (source_file)
				return source_file;
			else
				return NULL;
		} else if (source_file) {
			gchar *retval = g_strdup_printf ("%s:%d", source_file, lne->row);
			g_free (source_file);
			return retval;
		} else
			return g_strdup_printf ("%d", lne->row);
	}

	return NULL;
}

gint32
_mono_debug_address_from_il_offset (MonoDebugMethodJitInfo *jit, guint32 il_offset)
{
	int i;

	if (!jit || !jit->line_numbers)
		return -1;

	for (i = jit->line_numbers->len - 1; i >= 0; i--) {
		MonoDebugLineNumberEntry lne = g_array_index (
			jit->line_numbers, MonoDebugLineNumberEntry, i);

		if (lne.offset <= il_offset)
			return lne.address;
	}

	return -1;
}

MonoDebugMethodInfo *
mono_debug_find_method (MonoSymbolFile *symfile, MonoMethod *method)
{
	if (!symfile->method_hash)
		return NULL;
	else
		return g_hash_table_lookup (symfile->method_hash, method);
}
