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

#define RANGE_TABLE_CHUNK_SIZE	256

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

static GHashTable *type_table;

static int write_string_table (MonoSymbolFile *symfile);
static int create_symfile (MonoSymbolFile *symfile, gboolean emit_warnings);
static void close_symfile (MonoSymbolFile *symfile);
static MonoDebugRangeInfo *allocate_range_entry (MonoSymbolFile *symfile);
static gpointer write_type (MonoSymbolFile *symfile, MonoType *type);

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

		if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
			g_assert_not_reached ();

		if (!((MonoMethodNormal *) method)->header) {
			g_warning (G_STRLOC ": Internal error: method %s.%s doesn't have a header",
				   method->klass->name, method->name);
			continue;
		}

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
	symfile->dynamic_magic = MONO_SYMBOL_FILE_DYNAMIC_MAGIC;
	symfile->dynamic_version = MONO_SYMBOL_FILE_DYNAMIC_VERSION;
	symfile->image_file = g_strdup (image->name);
	symfile->symbol_file = g_strdup (filename);
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
	g_free (symfile->image_file);
	g_free (symfile->symbol_file);
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

void
mono_debug_symfile_add_method (MonoSymbolFile *symfile, MonoMethod *method)
{
	MonoSymbolFileMethodEntryPriv *mep;
	MonoSymbolFileMethodAddress *address;
	MonoDebugVarInfo *var_table;
	MonoSymbolFileLineNumberEntry *lne;
	MonoDebugRangeInfo *range;
	guint32 size, line_size, line_offset, num_variables, variable_size, variable_offset;
	guint32 type_size, type_offset, *line_addresses, *type_index_table;
	gpointer *type_table;
	guint8 *ptr;
	int i;

	mep = g_hash_table_lookup (symfile->_priv->method_table, method);
	if (!mep)
		return;

	if (!mep->minfo) {
		mep->minfo = g_hash_table_lookup (symfile->_priv->method_hash, mep->method);
		if (!mep->minfo)
			return;
	}

	if (!mep->minfo->jit)
		return;

	symfile->generation++;

	size = sizeof (MonoSymbolFileMethodAddress);

	line_size = mep->entry->num_line_numbers * sizeof (MonoSymbolFileLineNumberEntry);
	line_offset = size;
	size += line_size;

	num_variables = mep->entry->num_parameters + mep->entry->num_locals;
	if (mep->entry->this_type_index)
		num_variables++;

	variable_size = num_variables * sizeof (MonoDebugVarInfo);
	variable_offset = size;
	size += variable_size;

	type_size = num_variables * sizeof (gpointer);
	type_offset = size;
	size += type_size;

	address = g_malloc0 (size);
	ptr = (guint8 *) address;

	address->size = size;
	address->start_address = GPOINTER_TO_UINT (mep->minfo->jit->code_start);
	address->end_address = GPOINTER_TO_UINT (mep->minfo->jit->code_start + mep->minfo->jit->code_size);
	address->line_table_offset = line_offset;
	address->variable_table_offset = variable_offset;
	address->type_table_offset = type_offset;

	range = allocate_range_entry (symfile);
	range->file_offset = mep->minfo->file_offset;
	range->start_address = address->start_address;
	range->end_address = address->end_address;
	range->dynamic_data = address;
	range->dynamic_size = size;

	var_table = (MonoDebugVarInfo *) (ptr + variable_offset);
	type_table = (gpointer *) (ptr + type_offset);

	type_index_table = (guint32 *)
		(symfile->raw_contents + mep->entry->type_index_table_offset);

	if (mep->entry->this_type_index) {
		if (!mep->minfo->jit->this_var) {
			g_warning (G_STRLOC ": Method %s.%s doesn't have `this'.",
				   mep->method->klass->name, mep->method->name);
			var_table++;
		} else {
			*var_table++ = *mep->minfo->jit->this_var;
			*type_table++ = write_type (symfile, &method->klass->this_arg);
		}
	}

	if (mep->minfo->jit->num_params != mep->entry->num_parameters) {
		g_warning (G_STRLOC ": Method %s.%s has %d parameters, but symbol file claims it has %d.",
			   mep->method->klass->name, mep->method->name, mep->minfo->jit->num_params,
			   mep->entry->num_parameters);
		var_table += mep->entry->num_parameters;
	} else {
		for (i = 0; i < mep->minfo->jit->num_params; i++) {
			*var_table++ = mep->minfo->jit->params [i];
			*type_table++ = write_type (symfile, method->signature->params [i]);
		}
	}

	if (mep->minfo->jit->num_locals != mep->entry->num_locals) {
#if 0
		g_warning (G_STRLOC ": Method %s.%s has %d locals, but symbol file claims it has %d.",
			   mep->method->klass->name, mep->method->name, mep->minfo->jit->num_locals,
			   mep->entry->num_locals);
#endif
		var_table += mep->entry->num_locals;
	} else
		for (i = 0; i < mep->minfo->jit->num_locals; i++)
			*var_table++ = mep->minfo->jit->locals [i];

	lne = (MonoSymbolFileLineNumberEntry *)
		(symfile->raw_contents + mep->entry->line_number_table_offset);

	line_addresses = (guint32 *) (ptr + line_offset);

	for (i = 0; i < mep->entry->num_line_numbers; i++, lne++) {
		int j;

		if (i == 0) {
			line_addresses [i] = 0;
			continue;
		} else if (lne->offset == 0) {
			line_addresses [i] = mep->minfo->jit->prologue_end;
			continue;
		}

		line_addresses [i] = mep->minfo->jit->code_size;

		for (j = 0; j < mep->minfo->num_il_offsets; j++) {
			MonoSymbolFileLineNumberEntry *il = &mep->minfo->il_offsets [j];

			if (il->offset >= lne->offset) {
				line_addresses [i] = mep->minfo->jit->il_addresses [j];
				break;
			}
		}
	}
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
	g_assert (header);

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

		if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
			g_assert_not_reached ();

		if (!((MonoMethodNormal *) method)->header) {
			g_warning (G_STRLOC ": Internal error: method %s.%s doesn't have a header",
				   method->klass->name, method->name);
			continue;
		}

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

	symfile->symbol_file = g_strdup (priv->file_name);

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
	symfile->dynamic_magic = MONO_SYMBOL_FILE_DYNAMIC_MAGIC;
	symfile->dynamic_version = MONO_SYMBOL_FILE_DYNAMIC_VERSION;
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

MonoReflectionMethod *
ves_icall_MonoDebugger_GetMethod (MonoReflectionAssembly *assembly, guint32 token)
{
	MonoMethod *method;

	method = mono_get_method (assembly->assembly->image, token, NULL);

	return mono_method_get_object (mono_domain_get (), method, NULL);
}

MonoReflectionType *
ves_icall_MonoDebugger_GetLocalTypeFromSignature (MonoReflectionAssembly *assembly, MonoArray *signature)
{
	MonoDomain *domain; 
	MonoImage *image;
	MonoType *type;
	const char *ptr;
	int len = 0;

	MONO_CHECK_ARG_NULL (assembly);
	MONO_CHECK_ARG_NULL (signature);

	domain = mono_domain_get();
	image = assembly->assembly->image;

	ptr = mono_array_addr (signature, char, 0);
	g_assert (*ptr++ == 0x07);
	len = mono_metadata_decode_value (ptr, &ptr);
	g_assert (len == 1);

	type = mono_metadata_parse_type (image, MONO_PARSE_LOCAL, 0, ptr, &ptr);

	return mono_type_get_object (domain, type);
}

static MonoDebugRangeInfo *
allocate_range_entry (MonoSymbolFile *symfile)
{
	MonoDebugRangeInfo *retval;
	guint32 size, chunks;

	symfile->range_entry_size = sizeof (MonoDebugRangeInfo);

	if (!symfile->range_table) {
		size = sizeof (MonoDebugRangeInfo) * RANGE_TABLE_CHUNK_SIZE;
		symfile->range_table = g_malloc0 (size);
		symfile->num_range_entries = 1;
		return symfile->range_table;
	}

	if (!((symfile->num_range_entries + 1) % RANGE_TABLE_CHUNK_SIZE)) {
		chunks = (symfile->num_range_entries + 1) / RANGE_TABLE_CHUNK_SIZE;
		size = sizeof (MonoDebugRangeInfo) * RANGE_TABLE_CHUNK_SIZE * (chunks + 1);

		symfile->range_table = g_realloc (symfile->range_table, size);
	}

	retval = symfile->range_table + symfile->num_range_entries;
	symfile->num_range_entries++;
	return retval;
}

static gpointer
write_simple_type (MonoSymbolFile *symfile, MonoType *type)
{
	guint8 buffer [BUFSIZ], *ptr = buffer, *retval;
	guint32 size;

	if (!type_table)
		type_table = g_hash_table_new (g_direct_hash, g_direct_equal);

	retval = g_hash_table_lookup (type_table, type);
	if (retval)
		return retval;

	switch (type->type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		*((int *) ptr)++ = 1;
		break;

	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		*((int *) ptr)++ = 2;
		break;

	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
		*((int *) ptr)++ = 4;
		break;

	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		*((int *) ptr)++ = 8;
		break;

	case MONO_TYPE_STRING: {
		MonoString string;

		*((int *) ptr)++ = -8;
		*((guint32 *) ptr)++ = sizeof (MonoString);
		*ptr++ = 1;
		*ptr++ = (guint8*)&string.length - (guint8*)&string;
		*ptr++ = sizeof (string.length);
		*ptr++ = (guint8*)&string.chars - (guint8*)&string;
		break;
	}

	default:
		return NULL;
	}

	size = ptr - buffer;

	retval = g_malloc0 (size + 4);
	memcpy (retval + 4, buffer, size);
	*((int *) retval) = size;

	g_hash_table_insert (type_table, type, retval);

	return retval;
}

static gpointer
write_type (MonoSymbolFile *symfile, MonoType *type)
{
	guint8 buffer [BUFSIZ], *ptr = buffer, *retval;
	int num_fields = 0;
	guint32 size;

	if (!type_table)
		type_table = g_hash_table_new (g_direct_hash, g_direct_equal);

	retval = g_hash_table_lookup (type_table, type);
	if (retval)
		return retval;

	retval = write_simple_type (symfile, type);
	if (retval)
		return retval;

	switch (type->type) {
	case MONO_TYPE_SZARRAY:
		size = 8 + sizeof (int) + sizeof (gpointer);
		break;

	case MONO_TYPE_ARRAY:
		size = 15 + sizeof (int) + sizeof (gpointer);
		break;

	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS: {
		MonoClass *klass = type->data.klass;
		int i;

		mono_class_init (klass);
		if (klass->enumtype) {
			size = 5 + sizeof (int) + sizeof (gpointer);
			break;
		}

		for (i = 0; i < klass->field.count; i++)
			if (!(klass->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC))
				++num_fields;

		size = 10 + sizeof (int) + num_fields * (4 + sizeof (gpointer));

		if (type->type == MONO_TYPE_CLASS)
			size += sizeof (gpointer);
		break;
	}

	case MONO_TYPE_OBJECT:
		size = 5 + sizeof (int);
		break;

	default:
		size = sizeof (int);
		break;
	}

	retval = g_malloc0 (size + 4);
	memcpy (retval + 4, buffer, size);
	*((int *) retval) = size;

	g_hash_table_insert (type_table, type, retval);

	ptr = retval + 4;

	switch (type->type) {
	case MONO_TYPE_SZARRAY: {
		MonoArray array;

		*((int *) ptr)++ = -8 - sizeof (gpointer);
		*((guint32 *) ptr)++ = sizeof (MonoArray);
		*ptr++ = 2;
		*ptr++ = (guint8*)&array.max_length - (guint8*)&array;
		*ptr++ = sizeof (array.max_length);
		*ptr++ = (guint8*)&array.vector - (guint8*)&array;
		*((gpointer *) ptr)++ = write_type (symfile, type->data.type);
		break;
	}

	case MONO_TYPE_ARRAY: {
		MonoArray array;
		MonoArrayBounds bounds;

		*((int *) ptr)++ = -15 - sizeof (gpointer);
		*((guint32 *) ptr)++ = sizeof (MonoArray);
		*ptr++ = 3;
		*ptr++ = (guint8*)&array.max_length - (guint8*)&array;
		*ptr++ = sizeof (array.max_length);
		*ptr++ = (guint8*)&array.vector - (guint8*)&array;
		*ptr++ = type->data.array->rank;
		*ptr++ = (guint8*)&array.bounds - (guint8*)&array;
		*ptr++ = sizeof (MonoArrayBounds);
		*ptr++ = (guint8*)&bounds.lower_bound - (guint8*)&bounds;
		*ptr++ = sizeof (bounds.lower_bound);
		*ptr++ = (guint8*)&bounds.length - (guint8*)&bounds;
		*ptr++ = sizeof (bounds.length);
		*((gpointer *) ptr)++ = write_type (symfile, type->data.array->type);
		break;
	}

	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS: {
		MonoClass *klass = type->data.klass;
		int base_offset = type->type == MONO_TYPE_CLASS ? 0 : - sizeof (MonoObject);
		int i;

		mono_class_init (klass);
		if (klass->enumtype) {
			*((int *) ptr)++ = -5 - sizeof (gpointer);
			*((guint32 *) ptr)++ = sizeof (MonoObject);
			*ptr++ = 4;
			*((gpointer *) ptr)++ = write_type (symfile, klass->enum_basetype);
			break;
		}

		*((int *) ptr)++ = -10 - num_fields * (4 + sizeof (gpointer));
		*((guint32 *) ptr)++ = klass->instance_size + base_offset;
		*ptr++ = type->type == MONO_TYPE_CLASS ? 6 : 5;
		*ptr++ = type->type == MONO_TYPE_CLASS;
		*((guint32 *) ptr)++ = num_fields;
		for (i = 0; i < klass->field.count; i++) {
			if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;

			*((guint32 *) ptr)++ = klass->fields [i].offset + base_offset;
			*((gpointer *) ptr)++ = write_type (symfile, klass->fields [i].type);
		}

		if (type->type == MONO_TYPE_CLASS) {
			if (klass->parent)
				*((gpointer *) ptr)++ = write_type (symfile, &klass->parent->this_arg);
			else
				*((gpointer *) ptr)++ = NULL;
		}

		break;
	}

	case MONO_TYPE_OBJECT:
		*((int *) ptr)++ = -5;
		*((guint32 *) ptr)++ = sizeof (MonoObject);
		*ptr++ = 7;
		break;

	default:
		g_message (G_STRLOC ": %p - %x,%x,%x", type, type->attrs, type->type, type->byref);

		*((int *) ptr)++ = -1;
		break;
	}

	return retval;
}
