#include <config.h>
#include <stdlib.h>
#include <string.h>
#include <signal.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/rawbuffer.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>

#include <fcntl.h>
#include <unistd.h>

struct MonoSymbolFilePriv
{
	int fd;
	int error;
	char *file_name;
	char *source_file;
	guint32 string_table_size;
	guint32 string_offset_size;
	MonoImage *image;
	GHashTable *method_table;
	GHashTable *method_hash;
	MonoSymbolFileOffsetTable *offset_table;
	GPtrArray *range_table;
};

typedef struct
{
	MonoMethod *method;
	MonoDebugMethodInfo *minfo;
	MonoSymbolFileMethodEntry *entry;
	guint32 method_name_offset;
	guint32 index;
	gchar *name;
} MonoSymbolFileMethodEntryPriv;

static int write_string_table (MonoSymbolFile *symfile);
static int create_symfile (MonoSymbolFile *symfile, gboolean emit_warnings);
static void close_symfile (MonoSymbolFile *symfile);

static void
free_method_info (MonoDebugMethodInfo *minfo)
{
	g_free (minfo->jit);
	g_free (minfo);
}

static int
load_symfile (MonoSymbolFile *symfile)
{
	MonoSymbolFilePriv *priv = symfile->_priv;
	MonoSymbolFileMethodEntry *me;
	const char *ptr, *start;
	guint64 magic;
	long version;
	int i;

	ptr = start = symfile->raw_contents;

	magic = *((guint64 *) ptr)++;
	if (magic != MONO_SYMBOL_FILE_MAGIC) {
		g_warning ("Symbol file %s has is not a mono symbol file", priv->file_name);
		return FALSE;
	}

	version = *((guint32 *) ptr)++;
	if (version != MONO_SYMBOL_FILE_VERSION) {
		g_warning ("Symbol file %s has incorrect line number table version "
			   "(expected %d, got %ld)", priv->file_name,
			   MONO_SYMBOL_FILE_VERSION, version);
		return FALSE;
	}

	priv->offset_table = (MonoSymbolFileOffsetTable *) ptr;
	symfile->address_table_size = priv->offset_table->address_table_size;

	/*
	 * Read method table.
	 *
	 */

	priv->method_table = g_hash_table_new_full (g_direct_hash, g_direct_equal, NULL,
						    (GDestroyNotify) g_free);
	priv->method_hash = g_hash_table_new_full (g_direct_hash, g_direct_equal, NULL,
						   (GDestroyNotify) free_method_info);

	ptr = symfile->raw_contents + priv->offset_table->method_table_offset;
	me = (MonoSymbolFileMethodEntry *) ptr;

	for (i = 0; i < priv->offset_table->method_count; i++, me++) {
		MonoMethod *method = mono_get_method (priv->image, me->token, NULL);
		MonoSymbolFileMethodEntryPriv *mep;
		MonoDebugMethodInfo *minfo;

		if (!method)
			continue;

		minfo = g_new0 (MonoDebugMethodInfo, 1);
		minfo->file_offset = ((const char *) me) - start;
		minfo->method = method;
		minfo->symfile = symfile;
		minfo->num_il_offsets = me->num_line_numbers;
		minfo->il_offsets = (MonoSymbolFileLineNumberEntry *)
			(symfile->raw_contents + me->line_number_table_offset);

		mep = g_new0 (MonoSymbolFileMethodEntryPriv, 1);
		mep->method = method;
		mep->minfo = minfo;
		mep->entry = me;
		mep->index = i;

		mep->method_name_offset = priv->string_table_size;
		mep->name = g_strdup_printf ("%s%s.%s", method->klass->name_space,
					     method->klass->name, method->name);
		priv->string_table_size += strlen (mep->name) + 1;

		g_hash_table_insert (priv->method_table, method, mep);
		g_hash_table_insert (priv->method_hash, method, minfo);
	}

	if (!write_string_table (symfile))
		return FALSE;

	return TRUE;
}

MonoSymbolFile *
mono_debug_open_mono_symbol_file (MonoImage *image, const char *filename, gboolean emit_warnings)
{
	MonoSymbolFile *symfile;
	MonoSymbolFilePriv *priv;
	off_t file_size;
	void *ptr;
	int fd;

	fd = open (filename, O_RDONLY);
	if (fd == -1) {
		if (emit_warnings)
			g_warning ("Can't open symbol file: %s", filename);
		return NULL;
	}

	file_size = lseek (fd, 0, SEEK_END);
	lseek (fd, 0, SEEK_SET);

	if (file_size == (off_t) -1) {
		if (emit_warnings)
			g_warning ("Can't get size of symbol file: %s", filename);
		return NULL;
	}

	ptr = mono_raw_buffer_load (fd, FALSE, 0, file_size);
	if (!ptr) {
		if (emit_warnings)
			g_warning ("Can't read symbol file: %s", filename);
		return NULL;
	}

	symfile = g_new0 (MonoSymbolFile, 1);
	symfile->magic = MONO_SYMBOL_FILE_MAGIC;
	symfile->version = MONO_SYMBOL_FILE_VERSION;
	symfile->image_file = g_strdup (image->name);
	symfile->raw_contents = ptr;
	symfile->raw_contents_size = file_size;

	symfile->_priv = priv = g_new0 (MonoSymbolFilePriv, 1);

	priv->fd = fd;
	priv->image = image;
	priv->file_name = g_strdup (filename);

	if (!load_symfile (symfile)) {
		mono_debug_close_mono_symbol_file (symfile);
		return NULL;
	}

	return symfile;
}

static void
close_symfile (MonoSymbolFile *symfile)
{
	MonoSymbolFilePriv *priv = symfile->_priv;

	if (symfile->raw_contents) {
		mono_raw_buffer_free (symfile->raw_contents);
		symfile->raw_contents = NULL;
	}

	if (priv->fd) {
		close (priv->fd);
		priv->fd = 0;
	}

	if (priv->method_table) {
		g_hash_table_destroy (priv->method_table);
		priv->method_table = NULL;
	}

	if (priv->method_hash) {
		g_hash_table_destroy (priv->method_hash);
		priv->method_hash = NULL;
	}

	if (symfile->is_dynamic)
		unlink (priv->file_name);

	if (symfile->image_file) {
		g_free (symfile->image_file);
		symfile->image_file = NULL;
	}

	if (priv->file_name) {
		g_free (priv->file_name);
		priv->file_name = NULL;
	}

	priv->error = FALSE;
}

void
mono_debug_close_mono_symbol_file (MonoSymbolFile *symfile)
{
	if (!symfile)
		return;

	close_symfile (symfile);

	g_free (symfile->_priv->source_file);
	g_free (symfile->_priv);
	g_free (symfile);
}

static int
write_string (int fd, const char *string)
{
	guint32 length = strlen (string);

	if (write (fd, &length, sizeof (length)) < 0)
		return FALSE;

	if (write (fd, string, strlen (string)) < 0)
		return FALSE;

	return TRUE;
}

static gchar *
read_string (const char *ptr)
{
	int len = *((guint32 *) ptr)++;
	gchar *retval;

	return g_filename_from_utf8 (ptr, len, NULL, NULL, NULL);
}

gchar *
mono_debug_find_source_location (MonoSymbolFile *symfile, MonoMethod *method, guint32 offset,
				 guint32 *line_number)
{
	MonoSymbolFilePriv *priv = symfile->_priv;
	MonoSymbolFileLineNumberEntry *lne;
	MonoSymbolFileMethodEntryPriv *mep;
	gchar *source_file = NULL;
	const char *ptr;
	int i;

	if (!priv->method_table || symfile->is_dynamic)
		return NULL;

	mep = g_hash_table_lookup (priv->method_table, method);
	if (!mep)
		return NULL;

	if (mep->entry->source_file_offset)
		source_file = read_string (symfile->raw_contents + mep->entry->source_file_offset);

	ptr = symfile->raw_contents + mep->entry->line_number_table_offset;

	lne = (MonoSymbolFileLineNumberEntry *) ptr;

	for (i = 0; i < mep->entry->num_line_numbers; i++, lne++) {
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

static void
update_method_func (gpointer key, gpointer value, gpointer user_data)
{
	MonoSymbolFile *symfile = (MonoSymbolFile *) user_data;
	MonoSymbolFileMethodEntryPriv *mep = (MonoSymbolFileMethodEntryPriv *) value;
	MonoSymbolFileMethodAddress *address;
	MonoSymbolFileLineNumberEntry *lne;
	MonoDebugRangeInfo *range;
	int i;

	if (!mep->minfo) {
		mep->minfo = g_hash_table_lookup (symfile->_priv->method_hash, mep->method);
		if (!mep->minfo)
			return;
	}

	if (!mep->minfo->jit)
		return;

	address = (MonoSymbolFileMethodAddress *)
		(symfile->address_table + mep->entry->address_table_offset);

	address->is_valid = TRUE;
	address->start_address = GPOINTER_TO_UINT (mep->minfo->jit->code_start);
	address->end_address = GPOINTER_TO_UINT (mep->minfo->jit->code_start + mep->minfo->jit->code_size);

	range = g_new0 (MonoDebugRangeInfo, 1);
	range->start_address = address->start_address;
	range->end_address = address->end_address;
	range->file_offset = mep->minfo->file_offset;

	g_ptr_array_add (symfile->_priv->range_table, range);

	lne = (MonoSymbolFileLineNumberEntry *)
		(symfile->raw_contents + mep->entry->line_number_table_offset);

	for (i = 0; i < mep->entry->num_line_numbers; i++, lne++) {
		int j;

		if (i == 0) {
			address->line_addresses [i] = 0;
			continue;
		} else if (lne->offset == 0) {
			address->line_addresses [i] = mep->minfo->jit->prologue_end;
			continue;
		}

		address->line_addresses [i] = mep->minfo->jit->code_size;

		for (j = 0; j < mep->minfo->num_il_offsets; j++) {
			MonoSymbolFileLineNumberEntry *il = &mep->minfo->il_offsets [j];

			if (il->offset >= lne->offset) {
				address->line_addresses [i] = mep->minfo->jit->il_addresses [j];
				break;
			}
		}
	}
}

static gint
range_table_compare_func (gconstpointer a, gconstpointer b)
{
	const MonoDebugRangeInfo *r1 = (const MonoDebugRangeInfo *) a;
	const MonoDebugRangeInfo *r2 = (const MonoDebugRangeInfo *) b;

	if (r1->start_address < r2->start_address)
		return -1;
	else if (r1->start_address > r2->start_address)
		return 1;
	else
		return 0;
}	

void
mono_debug_update_mono_symbol_file (MonoSymbolFile *symfile)
{
	int i;

	if (!symfile->_priv->method_table)
		return;

	symfile->_priv->range_table = g_ptr_array_new ();

	if (!symfile->address_table)
		symfile->address_table = g_malloc0 (symfile->address_table_size);

	g_hash_table_foreach (symfile->_priv->method_table, update_method_func, symfile);

	symfile->range_table_size = symfile->_priv->range_table->len * sizeof (MonoDebugRangeInfo);
	symfile->range_table = g_malloc0 (symfile->range_table_size);

	g_ptr_array_sort (symfile->_priv->range_table, range_table_compare_func);

	for (i = 0; i < symfile->_priv->range_table->len; i++) {
		MonoDebugRangeInfo *range = g_ptr_array_index (symfile->_priv->range_table, i);

		symfile->range_table [i] = *range;
	}

	g_ptr_array_free (symfile->_priv->range_table, TRUE);
	symfile->_priv->range_table = NULL;
}

static void
free_method_entry (MonoSymbolFileMethodEntryPriv *mep)
{
	g_free (mep->name);
	g_free (mep->entry);
	g_free (mep);
}

static void
create_method (MonoSymbolFile *symfile, guint32 token, MonoMethod *method)
{
	MonoSymbolFileMethodEntryPriv *mep;
	MonoDebugMethodInfo *minfo;

	g_assert (method->klass->image == symfile->_priv->image);

	mep = g_new0 (MonoSymbolFileMethodEntryPriv, 1);
	mep->entry = g_new0 (MonoSymbolFileMethodEntry, 1);
	mep->entry->token = token;
	mep->entry->source_file_offset = symfile->_priv->offset_table->source_table_offset;

	mep->method_name_offset = symfile->_priv->string_table_size;
	mep->name = g_strdup_printf ("%s%s.%s", method->klass->name_space,
				     method->klass->name, method->name);
	symfile->_priv->string_table_size += strlen (mep->name) + 1;

	minfo = g_new0 (MonoDebugMethodInfo, 1);
	minfo->method = method;
	minfo->symfile = symfile;

	mep->minfo = minfo;
	mep->method = method;

	symfile->_priv->offset_table->method_count++;

	g_hash_table_insert (symfile->_priv->method_table, method, mep);
	g_hash_table_insert (symfile->_priv->method_hash, method, minfo);
}

static void
write_method (gpointer key, gpointer value, gpointer user_data)
{
	MonoSymbolFile *symfile = (MonoSymbolFile *) user_data;
	MonoSymbolFileMethodEntryPriv *mep = (MonoSymbolFileMethodEntryPriv *) value;

	if (symfile->_priv->error)
		return;

	mep->minfo->file_offset = lseek (symfile->_priv->fd, 0, SEEK_CUR);

	if (write (symfile->_priv->fd, mep->entry, sizeof (MonoSymbolFileMethodEntry)) < 0) {
		symfile->_priv->error = TRUE;
		return;
	}
}

static void
write_line_numbers (gpointer key, gpointer value, gpointer user_data)
{
	MonoSymbolFile *symfile = (MonoSymbolFile *) user_data;
	MonoSymbolFilePriv *priv = symfile->_priv;
	MonoSymbolFileMethodEntryPriv *mep = (MonoSymbolFileMethodEntryPriv *) value;
	MonoSymbolFileLineNumberEntry lne;
	const unsigned char *ip, *start, *end;
	MonoMethodHeader *header;

	if (priv->error)
		return;

	if ((mep->method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (mep->method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (mep->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		g_assert_not_reached ();

	header = ((MonoMethodNormal *) mep->method)->header;

	mep->entry->line_number_table_offset = lseek (priv->fd, 0, SEEK_CUR);
	++mep->entry->num_line_numbers;

	lne.offset = 0;
	lne.row = -1;

	if (write (priv->fd, &lne, sizeof (MonoSymbolFileLineNumberEntry)) < 0) {
		priv->error = TRUE;
		return;
	}

	ip = start = header->code;
	end = ip + header->code_size;

	while (ip < end) {
		gchar *line;

		++mep->entry->num_line_numbers;
		lne.offset = ip - start;
		lne.row = -1;

		if (write (priv->fd, &lne, sizeof (MonoSymbolFileLineNumberEntry)) < 0) {
			priv->error = TRUE;
			return;
		}

		line = mono_disasm_code_one (NULL, mep->method, ip, &ip);
		g_free (line);
	}

	mep->entry->address_table_offset = symfile->address_table_size;
	mep->entry->address_table_size = sizeof (MonoSymbolFileMethodAddress) +
		mep->entry->num_line_numbers * sizeof (guint32);

	symfile->address_table_size += mep->entry->address_table_size;
}

static void
create_methods (MonoSymbolFile *symfile)
{
	MonoImage *image = symfile->_priv->image;
	MonoTableInfo *table = &image->tables [MONO_TABLE_METHOD];
	int idx;

	for (idx = 1; idx <= table->rows; idx++) {
		guint32 token = mono_metadata_make_token (MONO_TABLE_METHOD, idx);
		MonoMethod *method = mono_get_method (image, token, NULL);

		if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
			continue;

		if (method->wrapper_type != MONO_WRAPPER_NONE)
			continue;

		create_method (symfile, token, method);
	}
}

static void
load_line_numbers (gpointer key, gpointer value, gpointer user_data)
{
	MonoSymbolFile *symfile = (MonoSymbolFile *) user_data;
	MonoSymbolFileMethodEntryPriv *mep = (MonoSymbolFileMethodEntryPriv *) value;

	mep->minfo->num_il_offsets = mep->entry->num_line_numbers;
	mep->minfo->il_offsets = (MonoSymbolFileLineNumberEntry *)
		(symfile->raw_contents + mep->entry->line_number_table_offset);
}

static int
create_symfile (MonoSymbolFile *symfile, gboolean emit_warnings)
{
	MonoSymbolFilePriv *priv = symfile->_priv;
	char *ptr;
	guint64 magic;
	long version;
	off_t offset;

	priv->fd = g_file_open_tmp (NULL, &priv->file_name, NULL);
	if (priv->fd == -1) {
		if (emit_warnings)
			g_warning ("Can't create symbol file");
		return FALSE;
	}

	magic = MONO_SYMBOL_FILE_MAGIC;
	if (write (priv->fd, &magic, sizeof (magic)) < 0)
		return FALSE;

	version = MONO_SYMBOL_FILE_VERSION;
	if (write (priv->fd, &version, sizeof (version)) < 0)
		return FALSE;

	offset = lseek (priv->fd, 0, SEEK_CUR);

	priv->offset_table = g_new0 (MonoSymbolFileOffsetTable, 1);
	if (write (priv->fd, priv->offset_table, sizeof (MonoSymbolFileOffsetTable)) < 0)
		return FALSE;

	//
	// Write source file table.
	//
	if (priv->source_file) {
		priv->offset_table->source_table_offset = lseek (priv->fd, 0, SEEK_CUR);
		if (!write_string (priv->fd, priv->source_file))
			return FALSE;
		priv->offset_table->source_table_size = lseek (priv->fd, 0, SEEK_CUR) -
			priv->offset_table->source_table_offset;
	}

	//
	// Create method table.
	//

	priv->method_table = g_hash_table_new_full (g_direct_hash, g_direct_equal, NULL,
						    (GDestroyNotify) free_method_entry);
	priv->method_hash = g_hash_table_new_full (g_direct_hash, g_direct_equal, NULL,
						   (GDestroyNotify) free_method_info);

	create_methods (symfile);

	//
	// Write line numbers.
	//

	priv->offset_table->line_number_table_offset = lseek (priv->fd, 0, SEEK_CUR);

	g_hash_table_foreach (priv->method_table, write_line_numbers, symfile);
	if (priv->error)
		return FALSE;

	priv->offset_table->line_number_table_size = lseek (priv->fd, 0, SEEK_CUR) -
		priv->offset_table->line_number_table_offset;

	//
	// Write method table.
	//

	priv->offset_table->method_table_offset = lseek (priv->fd, 0, SEEK_CUR);

	g_hash_table_foreach (priv->method_table, write_method, symfile);
	if (priv->error)
		return FALSE;

	priv->offset_table->method_table_size = lseek (priv->fd, 0, SEEK_CUR) -
		priv->offset_table->method_table_offset;

	//
	// Write offset table.
	//

	symfile->raw_contents_size = lseek (priv->fd, 0, SEEK_CUR);
	priv->offset_table->address_table_size = symfile->address_table_size;

	lseek (priv->fd, offset, SEEK_SET);
	if (write (priv->fd, priv->offset_table, sizeof (MonoSymbolFileOffsetTable)) < 0)
		return FALSE;

	lseek (priv->fd, symfile->raw_contents_size, SEEK_SET);

	ptr = mono_raw_buffer_load (priv->fd, TRUE, 0, symfile->raw_contents_size);
	if (!ptr)
		return FALSE;

	symfile->raw_contents = ptr;

	//
	// Load line number table.
	//
	g_hash_table_foreach (priv->method_table, load_line_numbers, symfile);
	if (priv->error)
		return FALSE;

	if (!write_string_table (symfile))
		return FALSE;

	return TRUE;
}

MonoSymbolFile *
mono_debug_create_mono_symbol_file (MonoImage *image)
{
	MonoSymbolFile *symfile;

	symfile = g_new0 (MonoSymbolFile, 1);
	symfile->magic = MONO_SYMBOL_FILE_MAGIC;
	symfile->version = MONO_SYMBOL_FILE_VERSION;
	symfile->is_dynamic = TRUE;
	symfile->image_file = g_strdup (image->name);

	symfile->_priv = g_new0 (MonoSymbolFilePriv, 1);
	symfile->_priv->image = image;

	if (!create_symfile (symfile, TRUE)) {
		mono_debug_close_mono_symbol_file (symfile);
		return NULL;
	}

	return symfile;
}

MonoDebugMethodInfo *
mono_debug_find_method (MonoSymbolFile *symfile, MonoMethod *method)
{
	return g_hash_table_lookup (symfile->_priv->method_hash, method);
}

static void
write_method_name (gpointer key, gpointer value, gpointer user_data)
{
	MonoSymbolFile *symfile = (MonoSymbolFile *) user_data;
	MonoSymbolFileMethodEntryPriv *mep = (MonoSymbolFileMethodEntryPriv *) value;
	MonoSymbolFilePriv *priv = symfile->_priv;
	guint8 *offset_ptr, *string_ptr;
	guint32 offset;

	offset = mep->method_name_offset + priv->string_offset_size;

	offset_ptr = symfile->string_table + mep->index * 4;
	string_ptr = symfile->string_table + offset;

	*((guint32 *) offset_ptr) = offset;
	strcpy (string_ptr, mep->name);
}

static int
write_string_table (MonoSymbolFile *symfile)
{
	MonoSymbolFilePriv *priv = symfile->_priv;

	priv->string_offset_size = priv->offset_table->method_count * 4;

	symfile->string_table_size = priv->string_table_size + priv->string_offset_size;
	symfile->string_table = g_malloc0 (symfile->string_table_size);

	g_hash_table_foreach (symfile->_priv->method_table, write_method_name, symfile);
	return TRUE;
}
