#include <config.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/mono-endian.h>
#include <string.h>

#define DATA_TABLE_CHUNK_SIZE		16384

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

#if NO_UNALIGNED_ACCESS
#define RETURN_UNALIGNED(type, addr) \
	{ \
		type val; \
		memcpy(&val, p + offset, sizeof(val)); \
		return val; \
	}
#define WRITE_UNALIGNED(type, addr, val) \
	memcpy(addr, &val, sizeof(type))
#define READ_UNALIGNED(type, addr, val) \
	memcpy(&val, addr, sizeof(type))
#else
#define RETURN_UNALIGNED(type, addr) \
	return *(type*)(p + offset);
#define WRITE_UNALIGNED(type, addr, val) \
	(*(type *)(addr) = (val))
#define READ_UNALIGNED(type, addr, val) \
	val = (*(type *)(addr))
#endif

typedef enum {
	MONO_DEBUG_DATA_ITEM_UNKNOWN		= 0,
	MONO_DEBUG_DATA_ITEM_CLASS,
	MONO_DEBUG_DATA_ITEM_METHOD,
	MONO_DEBUG_DATA_ITEM_DELEGATE_TRAMPOLINE
} MonoDebugDataItemType;

typedef struct _MonoDebugDataChunk MonoDebugDataChunk;

struct _MonoDebugDataChunk {
	guint32 total_size;
	guint32 allocated_size;
	guint32 current_offset;
	guint32 dummy;
	MonoDebugDataChunk *next;
	guint8 data [MONO_ZERO_LEN_ARRAY];
};

struct _MonoDebugDataTable {
	gint32 domain;
	gint32 _dummy; /* alignment for next field. */
	MonoDebugDataChunk *first_chunk;
	MonoDebugDataChunk *current_chunk;
	GHashTable *method_hash;
	GHashTable *method_address_hash;
};

typedef struct {
	const gchar *method_name;
	const gchar *cil_code;
	guint32 wrapper_type;
} MonoDebugWrapperData;

typedef struct {
	guint32 size;
	guint32 symfile_id;
	guint32 domain_id;
	guint32 method_id;
	MonoDebugWrapperData *wrapper_data;
	MonoMethod *method;
	GSList *address_list;
} MonoDebugMethodHeader;

struct _MonoDebugMethodAddress {
	MonoDebugMethodHeader header;
	const guint8 *code_start;
	const guint8 *wrapper_addr;
	guint32 code_size;
	guint8 data [MONO_ZERO_LEN_ARRAY];
};

struct _MonoDebugClassEntry {
	guint32 size;
	guint8 data [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	gpointer code;
	guint32 size;
} MonoDebugDelegateTrampolineEntry;

MonoSymbolTable *mono_symbol_table = NULL;
MonoDebugFormat mono_debug_format = MONO_DEBUG_FORMAT_NONE;
gint32 mono_debug_debugger_version = 3;
gint32 _mono_debug_using_mono_debugger = 0;

static gboolean mono_debug_initialized = FALSE;
GHashTable *mono_debug_handles = NULL;

static GHashTable *data_table_hash = NULL;
static int next_symbol_file_id = 0;

static MonoDebugHandle     *mono_debug_open_image      (MonoImage *image, const guint8 *raw_contents, int size);

static MonoDebugHandle     *_mono_debug_get_image      (MonoImage *image);
static void                 mono_debug_add_assembly    (MonoAssembly *assembly,
							gpointer user_data);
static void                 mono_debug_add_type        (MonoClass *klass);

void _mono_debug_init_corlib (MonoDomain *domain);

extern void (*mono_debugger_class_init_func) (MonoClass *klass);
extern void (*mono_debugger_class_loaded_methods_func) (MonoClass *klass);

static MonoDebugDataTable *
create_data_table (MonoDomain *domain)
{
	MonoDebugDataTable *table;
	MonoDebugDataChunk *chunk;

	table = g_new0 (MonoDebugDataTable, 1);
	table->domain = domain ? mono_domain_get_id (domain) : -1;

	table->method_address_hash = g_hash_table_new (NULL, NULL);
	table->method_hash = g_hash_table_new (NULL, NULL);

	chunk = g_malloc0 (sizeof (MonoDebugDataChunk) + DATA_TABLE_CHUNK_SIZE);
	chunk->total_size = DATA_TABLE_CHUNK_SIZE;

	table->first_chunk = table->current_chunk = chunk;

	if (domain) {
		mono_debug_list_add (&mono_symbol_table->data_tables, table);
		g_hash_table_insert (data_table_hash, domain, table);
	}

	return table;
}

static void
free_header_data (gpointer key, gpointer value, gpointer user_data)
{
	MonoDebugMethodHeader *header = (MonoDebugMethodHeader*)value;

	if (header->wrapper_data) {
		g_free ((gpointer)header->wrapper_data->method_name);
		g_free ((gpointer)header->wrapper_data->cil_code);
		g_slist_free (header->address_list);
		g_free (header->wrapper_data);
	}
}

static void
free_data_table (MonoDebugDataTable *table)
{
	MonoDebugDataChunk *chunk, *next_chunk;

	g_hash_table_foreach (table->method_hash, free_header_data, NULL);
	g_hash_table_destroy (table->method_hash);
	g_hash_table_destroy (table->method_address_hash);

	table->method_hash = NULL;
	table->method_address_hash = NULL;

	chunk = table->first_chunk;
	while (chunk) {
		next_chunk = chunk->next;
		g_free (chunk);
		chunk = next_chunk;
	}

	table->first_chunk = table->current_chunk = NULL;
	mono_debug_list_remove (&mono_symbol_table->data_tables, table);
	g_free (table);
}

static MonoDebugDataTable *
lookup_data_table (MonoDomain *domain)
{
	MonoDebugDataTable *table;

	table = g_hash_table_lookup (data_table_hash, domain);
	g_assert (table);
	return table;
}

static void
free_debug_handle (MonoDebugHandle *handle)
{
	if (handle->symfile)
		mono_debug_close_mono_symbol_file (handle->symfile);
	/* decrease the refcount added with mono_image_addref () */
	free_data_table (handle->type_table);
	mono_image_close (handle->image);
	g_free (handle->image_file);
	g_free (handle);
}

/*
 * Initialize debugging support.
 *
 * This method must be called after loading corlib,
 * but before opening the application's main assembly because we need to set some
 * callbacks here.
 */
void
mono_debug_init (MonoDebugFormat format)
{
	g_assert (!mono_debug_initialized);

	mono_debug_initialized = TRUE;
	mono_debug_format = format;

	mono_debugger_initialize (_mono_debug_using_mono_debugger);

	mono_debugger_lock ();

	mono_symbol_table = g_new0 (MonoSymbolTable, 1);
	mono_symbol_table->magic = MONO_DEBUGGER_MAGIC;
	mono_symbol_table->version = MONO_DEBUGGER_VERSION;
	mono_symbol_table->total_size = sizeof (MonoSymbolTable);

	mono_debug_handles = g_hash_table_new_full
		(NULL, NULL, NULL, (GDestroyNotify) free_debug_handle);

	data_table_hash = g_hash_table_new_full (
		NULL, NULL, NULL, (GDestroyNotify) free_data_table);

	mono_debugger_class_init_func = mono_debug_add_type;
	mono_debugger_class_loaded_methods_func = mono_debugger_class_initialized;
	mono_install_assembly_load_hook (mono_debug_add_assembly, NULL);

	mono_symbol_table->global_data_table = create_data_table (NULL);

	mono_debugger_unlock ();
}

/*
 * INTERNAL USE ONLY !
 */
void
_mono_debug_init_corlib (MonoDomain *domain)
{
	if (!mono_debug_initialized)
		return;

	mono_symbol_table->corlib = mono_debug_open_image (mono_defaults.corlib, NULL, 0);
	mono_debugger_event (MONO_DEBUGGER_EVENT_INITIALIZE_CORLIB,
			     (guint64) (gsize) mono_symbol_table->corlib, 0);
}

void
mono_debug_open_image_from_memory (MonoImage *image, const guint8 *raw_contents, int size)
{
	mono_debug_open_image (image, raw_contents, size);
}


gboolean
mono_debug_using_mono_debugger (void)
{
	return _mono_debug_using_mono_debugger;
}

void
mono_debug_cleanup (void)
{
	if (mono_debug_handles)
		g_hash_table_destroy (mono_debug_handles);
	mono_debug_handles = NULL;

	if (data_table_hash) {
		g_hash_table_destroy (data_table_hash);
		data_table_hash = NULL;
	}

	g_free (mono_symbol_table);
	mono_symbol_table = NULL;
}

void
mono_debug_domain_create (MonoDomain *domain)
{
	MonoDebugDataTable *table;

	if (!mono_debug_initialized)
		return;

	mono_debugger_lock ();

	table = create_data_table (domain);

	mono_debugger_event (MONO_DEBUGGER_EVENT_DOMAIN_CREATE, (guint64) (gsize) table,
			     mono_domain_get_id (domain));

	mono_debugger_unlock ();
}

void
mono_debug_domain_unload (MonoDomain *domain)
{
	MonoDebugDataTable *table;

	if (!mono_debug_initialized)
		return;

	mono_debugger_lock ();

	table = g_hash_table_lookup (data_table_hash, domain);
	if (!table) {
		g_warning (G_STRLOC ": unloading unknown domain %p / %d",
			   domain, mono_domain_get_id (domain));
		mono_debugger_unlock ();
		return;
	}

	mono_debugger_event (MONO_DEBUGGER_EVENT_DOMAIN_UNLOAD, (guint64) (gsize) table,
			     mono_domain_get_id (domain));

	g_hash_table_remove (data_table_hash, domain);

	mono_debugger_unlock ();
}

static MonoDebugHandle *
_mono_debug_get_image (MonoImage *image)
{
	return g_hash_table_lookup (mono_debug_handles, image);
}

void
mono_debug_close_image (MonoImage *image)
{
	MonoDebugHandle *handle;

	if (!mono_debug_initialized)
		return;

	handle = _mono_debug_get_image (image);
	if (!handle)
		return;

	mono_debugger_lock ();

	mono_debugger_event (MONO_DEBUGGER_EVENT_UNLOAD_MODULE, (guint64) (gsize) handle,
			     handle->index);

	mono_debug_list_remove (&mono_symbol_table->symbol_files, handle);
	g_hash_table_remove (mono_debug_handles, image);

	mono_debugger_unlock ();
}

static MonoDebugHandle *
mono_debug_open_image (MonoImage *image, const guint8 *raw_contents, int size)
{
	MonoDebugHandle *handle;

	if (mono_image_is_dynamic (image))
		return NULL;

	handle = _mono_debug_get_image (image);
	if (handle != NULL)
		return handle;

	mono_debugger_lock ();

	handle = g_new0 (MonoDebugHandle, 1);
	handle->index = ++next_symbol_file_id;

	handle->image = image;
	mono_image_addref (image);
	handle->image_file = g_strdup (mono_image_get_filename (image));

	handle->type_table = create_data_table (NULL);

	handle->symfile = mono_debug_open_mono_symbols (
		handle, raw_contents, size, _mono_debug_using_mono_debugger);

	mono_debug_list_add (&mono_symbol_table->symbol_files, handle);

	g_hash_table_insert (mono_debug_handles, image, handle);

	if (mono_symbol_table->corlib)
		mono_debugger_event (MONO_DEBUGGER_EVENT_LOAD_MODULE,
				     (guint64) (gsize) handle, 0);

	mono_debugger_unlock ();

	return handle;
}

static void
mono_debug_add_assembly (MonoAssembly *assembly, gpointer user_data)
{
	mono_debugger_lock ();
	mono_debug_open_image (mono_assembly_get_image (assembly), NULL, 0);
	mono_debugger_unlock ();
}

static guint8 *
allocate_data_item (MonoDebugDataTable *table, MonoDebugDataItemType type, guint32 size)
{
	guint32 chunk_size;
	guint8 *data;

	size = ALIGN_TO (size, sizeof (gpointer));

	if (size + 16 < DATA_TABLE_CHUNK_SIZE)
		chunk_size = DATA_TABLE_CHUNK_SIZE;
	else
		chunk_size = size + 16;

	g_assert (table->current_chunk->current_offset == table->current_chunk->allocated_size);

	if (table->current_chunk->allocated_size + size + 8 >= table->current_chunk->total_size) {
		MonoDebugDataChunk *new_chunk;

		new_chunk = g_malloc0 (sizeof (MonoDebugDataChunk) + chunk_size);
		new_chunk->total_size = chunk_size;

		table->current_chunk->next = new_chunk;
		table->current_chunk = new_chunk;
	}

	data = &table->current_chunk->data [table->current_chunk->allocated_size];
	table->current_chunk->allocated_size += size + 8;

	* ((guint32 *) data) = size;
	data += 4;
	* ((guint32 *) data) = type;
	data += 4;
	return data;
}

static void
write_data_item (MonoDebugDataTable *table, const guint8 *data)
{
	MonoDebugDataChunk *current_chunk = table->current_chunk;
	guint32 size = * ((guint32 *) (data - 8));

	g_assert (current_chunk->current_offset + size + 8 == current_chunk->allocated_size);
	current_chunk->current_offset = current_chunk->allocated_size;
}

struct LookupMethodData
{
	MonoDebugMethodInfo *minfo;
	MonoMethod *method;
};

static void
lookup_method_func (gpointer key, gpointer value, gpointer user_data)
{
	MonoDebugHandle *handle = (MonoDebugHandle *) value;
	struct LookupMethodData *data = (struct LookupMethodData *) user_data;

	if (data->minfo)
		return;

	if (handle->symfile)
		data->minfo = mono_debug_symfile_lookup_method (handle, data->method);
}

static MonoDebugMethodInfo *
_mono_debug_lookup_method (MonoMethod *method)
{
	struct LookupMethodData data;

	data.minfo = NULL;
	data.method = method;

	if (!mono_debug_handles)
		return NULL;

	g_hash_table_foreach (mono_debug_handles, lookup_method_func, &data);
	return data.minfo;
}

/**
 * mono_debug_lookup_method:
 *
 * Lookup symbol file information for the method @method.  The returned
 * `MonoDebugMethodInfo' is a private structure, but it can be passed to
 * mono_debug_symfile_lookup_location().
 */
MonoDebugMethodInfo *
mono_debug_lookup_method (MonoMethod *method)
{
	MonoDebugMethodInfo *minfo;

	mono_debugger_lock ();
	minfo = _mono_debug_lookup_method (method);
	mono_debugger_unlock ();
	return minfo;
}

static inline void
write_leb128 (guint32 value, guint8 *ptr, guint8 **rptr)
{
	do {
		guint8 byte = value & 0x7f;
		value >>= 7;
		if (value)
			byte |= 0x80;
		*ptr++ = byte;
	} while (value);

	*rptr = ptr;
}

static inline void
write_sleb128 (gint32 value, guint8 *ptr, guint8 **rptr)
{
	gboolean more = 1;

	while (more) {
		guint8 byte = value & 0x7f;
		value >>= 7;

		if (((value == 0) && ((byte & 0x40) == 0)) || ((value == -1) && (byte & 0x40)))
			more = 0;
		else
			byte |= 0x80;
		*ptr++ = byte;
	}

	*rptr = ptr;
}

static void
write_variable (MonoDebugVarInfo *var, guint8 *ptr, guint8 **rptr)
{
	write_leb128 (var->index, ptr, &ptr);
	write_sleb128 (var->offset, ptr, &ptr);
	write_leb128 (var->size, ptr, &ptr);
	write_leb128 (var->begin_scope, ptr, &ptr);
	write_leb128 (var->end_scope, ptr, &ptr);
	WRITE_UNALIGNED (gpointer, ptr, var->type);
	ptr += sizeof (gpointer);
	*rptr = ptr;
}

MonoDebugMethodAddress *
mono_debug_add_method (MonoMethod *method, MonoDebugMethodJitInfo *jit, MonoDomain *domain)
{
	MonoMethod *declaring;
	MonoDebugDataTable *table;
	MonoDebugMethodHeader *header;
	MonoDebugMethodAddress *address;
	MonoDebugMethodInfo *minfo;
	MonoDebugHandle *handle;
	guint8 buffer [BUFSIZ];
	guint8 *ptr, *oldptr;
	guint32 i, size, total_size, max_size;
	gboolean is_wrapper = FALSE;

	mono_debugger_lock ();

	table = lookup_data_table (domain);

	handle = _mono_debug_get_image (method->klass->image);
	minfo = _mono_debug_lookup_method (method);

	if (!minfo || (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->flags & METHOD_ATTRIBUTE_ABSTRACT) ||
	    (method->wrapper_type != MONO_WRAPPER_NONE)) {
		is_wrapper = TRUE;
	}

	max_size = 24 + 8 * jit->num_line_numbers +
		(20 + sizeof (gpointer)) * (1 + jit->num_params + jit->num_locals);

	if (max_size > BUFSIZ)
		ptr = oldptr = g_malloc (max_size);
	else
		ptr = oldptr = buffer;

	write_leb128 (jit->prologue_end, ptr, &ptr);
	write_leb128 (jit->epilogue_begin, ptr, &ptr);

	write_leb128 (jit->num_line_numbers, ptr, &ptr);
	for (i = 0; i < jit->num_line_numbers; i++) {
		MonoDebugLineNumberEntry *lne = &jit->line_numbers [i];

		write_sleb128 (lne->il_offset, ptr, &ptr);
		write_sleb128 (lne->native_offset, ptr, &ptr);
	}

	*ptr++ = jit->this_var ? 1 : 0;
	if (jit->this_var)
		write_variable (jit->this_var, ptr, &ptr);

	write_leb128 (jit->num_params, ptr, &ptr);
	for (i = 0; i < jit->num_params; i++)
		write_variable (&jit->params [i], ptr, &ptr);

	write_leb128 (jit->num_locals, ptr, &ptr);
	for (i = 0; i < jit->num_locals; i++)
		write_variable (&jit->locals [i], ptr, &ptr);

	size = ptr - oldptr;
	g_assert (size < max_size);
	total_size = size + sizeof (MonoDebugMethodAddress);

	address = (MonoDebugMethodAddress *) allocate_data_item (
		table, MONO_DEBUG_DATA_ITEM_METHOD, total_size);

	address->header.size = total_size;
	address->header.symfile_id = handle ? handle->index : 0;
	address->header.domain_id = mono_domain_get_id (domain);
	address->header.method_id = is_wrapper ? 0 : minfo->index;
	address->header.method = method;

	address->code_start = jit->code_start;
	address->code_size = jit->code_size;

	memcpy (&address->data, oldptr, size);
	if (max_size > BUFSIZ)
		g_free (oldptr);

	declaring = method->is_inflated ? ((MonoMethodInflated *) method)->declaring : method;
	header = g_hash_table_lookup (table->method_hash, declaring);

	if (!header) {
		header = &address->header;
		g_hash_table_insert (table->method_hash, declaring, header);

		if (is_wrapper) {
			const unsigned char* il_code;
			MonoMethodHeader *mheader;
			MonoDebugWrapperData *wrapper;
			guint32 il_codesize;

			mheader = mono_method_get_header (declaring);
			il_code = mono_method_header_get_code (mheader, &il_codesize, NULL);

			header->wrapper_data = wrapper = g_new0 (MonoDebugWrapperData, 1);

			wrapper->wrapper_type = method->wrapper_type;
			wrapper->method_name = mono_method_full_name (declaring, TRUE);
			wrapper->cil_code = mono_disasm_code (
				NULL, declaring, il_code, il_code + il_codesize);
		}
	} else {
		address->header.wrapper_data = header->wrapper_data;
		header->address_list = g_slist_prepend (header->address_list, address);
	}

	g_hash_table_insert (table->method_address_hash, method, address);

	write_data_item (table, (guint8 *) address);

	mono_debugger_unlock ();
	return address;
}

void
mono_debug_add_delegate_trampoline (gpointer code, int size)
{
	MonoDebugDelegateTrampolineEntry *entry;

	if (!mono_debug_initialized)
		return;

	mono_debugger_lock ();

	entry = (MonoDebugDelegateTrampolineEntry *) allocate_data_item (
		mono_symbol_table->global_data_table, MONO_DEBUG_DATA_ITEM_DELEGATE_TRAMPOLINE,
		sizeof (MonoDebugDelegateTrampolineEntry));
	entry->code = code;
	entry->size = size;

	write_data_item (mono_symbol_table->global_data_table, (guint8 *) entry);

	mono_debugger_unlock ();
}

static inline guint32
read_leb128 (guint8 *ptr, guint8 **rptr)
{
	guint32 result = 0, shift = 0;

	while (TRUE) {
		guint8 byte = *ptr++;

		result |= (byte & 0x7f) << shift;
		if ((byte & 0x80) == 0)
			break;
		shift += 7;
	}

	*rptr = ptr;
	return result;
}

static inline gint32
read_sleb128 (guint8 *ptr, guint8 **rptr)
{
	gint32 result = 0;
	guint32 shift = 0;

	while (TRUE) {
		guint8 byte = *ptr++;

		result |= (byte & 0x7f) << shift;
		shift += 7;

		if (byte & 0x80)
			continue;

		if ((shift < 32) && (byte & 0x40))
			result |= - (1 << shift);
		break;
	}

	*rptr = ptr;
	return result;
}

static void
read_variable (MonoDebugVarInfo *var, guint8 *ptr, guint8 **rptr)
{
	var->index = read_leb128 (ptr, &ptr);
	var->offset = read_sleb128 (ptr, &ptr);
	var->size = read_leb128 (ptr, &ptr);
	var->begin_scope = read_leb128 (ptr, &ptr);
	var->end_scope = read_leb128 (ptr, &ptr);
	READ_UNALIGNED (gpointer, ptr, var->type);
	ptr += sizeof (gpointer);
	*rptr = ptr;
}

static MonoDebugMethodJitInfo *
mono_debug_read_method (MonoDebugMethodAddress *address)
{
	MonoDebugMethodJitInfo *jit;
	guint32 i;
	guint8 *ptr;

	jit = g_new0 (MonoDebugMethodJitInfo, 1);
	jit->code_start = address->code_start;
	jit->code_size = address->code_size;
	jit->wrapper_addr = address->wrapper_addr;

	ptr = (guint8 *) &address->data;

	jit->prologue_end = read_leb128 (ptr, &ptr);
	jit->epilogue_begin = read_leb128 (ptr, &ptr);

	jit->num_line_numbers = read_leb128 (ptr, &ptr);
	jit->line_numbers = g_new0 (MonoDebugLineNumberEntry, jit->num_line_numbers);
	for (i = 0; i < jit->num_line_numbers; i++) {
		MonoDebugLineNumberEntry *lne = &jit->line_numbers [i];

		lne->il_offset = read_sleb128 (ptr, &ptr);
		lne->native_offset = read_sleb128 (ptr, &ptr);
	}

	if (*ptr++) {
		jit->this_var = g_new0 (MonoDebugVarInfo, 1);
		read_variable (jit->this_var, ptr, &ptr);
	}

	jit->num_params = read_leb128 (ptr, &ptr);
	jit->params = g_new0 (MonoDebugVarInfo, jit->num_params);
	for (i = 0; i < jit->num_params; i++)
		read_variable (&jit->params [i], ptr, &ptr);

	jit->num_locals = read_leb128 (ptr, &ptr);
	jit->locals = g_new0 (MonoDebugVarInfo, jit->num_locals);
	for (i = 0; i < jit->num_locals; i++)
		read_variable (&jit->locals [i], ptr, &ptr);

	return jit;
}

static void
mono_debug_add_type (MonoClass *klass)
{
	MonoDebugHandle *handle;
	MonoDebugClassEntry *entry;
	guint8 buffer [BUFSIZ];
	guint8 *ptr, *oldptr;
	guint32 size, total_size, max_size;
	int base_offset = 0;

	handle = _mono_debug_get_image (klass->image);
	if (!handle)
		return;

	if (klass->generic_class || klass->rank ||
	    (klass->byval_arg.type == MONO_TYPE_VAR) || (klass->byval_arg.type == MONO_TYPE_MVAR))
		return;

	mono_debugger_lock ();

	max_size = 12 + sizeof (gpointer);
	if (max_size > BUFSIZ)
		ptr = oldptr = g_malloc (max_size);
	else
		ptr = oldptr = buffer;

	if (klass->valuetype)
		base_offset = - (int)(sizeof (MonoObject));

	write_leb128 (klass->type_token, ptr, &ptr);
	write_leb128 (klass->instance_size + base_offset, ptr, &ptr);
	WRITE_UNALIGNED (gpointer, ptr, klass);
	ptr += sizeof (gpointer);

	size = ptr - oldptr;
	g_assert (size < max_size);
	total_size = size + sizeof (MonoDebugClassEntry);

	g_assert (total_size + 9 < DATA_TABLE_CHUNK_SIZE);

	entry = (MonoDebugClassEntry *) allocate_data_item (
		handle->type_table, MONO_DEBUG_DATA_ITEM_CLASS, total_size);

	entry->size = total_size;

	memcpy (&entry->data, oldptr, size);

	write_data_item (handle->type_table, (guint8 *) entry);

	if (max_size > BUFSIZ)
		g_free (oldptr);

	mono_debugger_unlock ();
}

static MonoDebugMethodJitInfo *
find_method (MonoMethod *method, MonoDomain *domain)
{
	MonoDebugDataTable *table;
	MonoDebugMethodAddress *address;

	table = lookup_data_table (domain);
	address = g_hash_table_lookup (table->method_address_hash, method);

	if (!address)
		return NULL;

	return mono_debug_read_method (address);
}

MonoDebugMethodJitInfo *
mono_debug_find_method (MonoMethod *method, MonoDomain *domain)
{
	MonoDebugMethodJitInfo *res;
	mono_debugger_lock ();
	res = find_method (method, domain);
	mono_debugger_unlock ();
	return res;
}

struct LookupMethodAddressData
{
	MonoMethod *method;
	MonoDebugMethodHeader *result;
};

static void
lookup_method_address_func (gpointer key, gpointer value, gpointer user_data)
{
	MonoDebugDataTable *table = (MonoDebugDataTable *) value;
	struct LookupMethodAddressData *data = (struct LookupMethodAddressData *) user_data;
	MonoDebugMethodHeader *header;

	header = g_hash_table_lookup (table->method_hash, data->method);
	if (header)
		data->result = header;
}

MonoDebugMethodAddressList *
mono_debug_lookup_method_addresses (MonoMethod *method)
{
	MonoDebugMethodAddressList *info;
	MonoDebugMethodHeader *header = NULL;
	struct LookupMethodAddressData data;
	MonoMethod *declaring;
	int count, size;
	GSList *list;
	guint8 *ptr;

	g_assert (mono_debug_debugger_version == 3);

	mono_debugger_lock ();

	declaring = method->is_inflated ? ((MonoMethodInflated *) method)->declaring : method;

	data.method = declaring;
	data.result = NULL;

	g_hash_table_foreach (data_table_hash, lookup_method_address_func, &data);
	header = data.result;

	if (!header) {
		mono_debugger_unlock ();
		return NULL;
	}

	count = g_slist_length (header->address_list) + 1;
	size = sizeof (MonoDebugMethodAddressList) + count * sizeof (gpointer);

	info = g_malloc0 (size);
	info->size = size;
	info->count = count;

	ptr = info->data;

	WRITE_UNALIGNED (gpointer, ptr, header);
	ptr += sizeof (gpointer);

	for (list = header->address_list; list; list = list->next) {
		WRITE_UNALIGNED (gpointer, ptr, list->data);
		ptr += sizeof (gpointer);
	}

	mono_debugger_unlock ();
	return info;
}

static gint32
il_offset_from_address (MonoMethod *method, MonoDomain *domain, guint32 native_offset)
{
	MonoDebugMethodJitInfo *jit;
	int i;

	jit = find_method (method, domain);
	if (!jit || !jit->line_numbers)
		return -1;

	for (i = jit->num_line_numbers - 1; i >= 0; i--) {
		MonoDebugLineNumberEntry lne = jit->line_numbers [i];

		if (lne.native_offset <= native_offset)
			return lne.il_offset;
	}

	return -1;
}

/**
 * mono_debug_lookup_source_location:
 * @address: Native offset within the @method's machine code.
 *
 * Lookup the source code corresponding to the machine instruction located at
 * native offset @address within @method.
 *
 * The returned `MonoDebugSourceLocation' contains both file / line number
 * information and the corresponding IL offset.  It must be freed by
 * mono_debug_free_source_location().
 */
MonoDebugSourceLocation *
mono_debug_lookup_source_location (MonoMethod *method, guint32 address, MonoDomain *domain)
{
	MonoDebugMethodInfo *minfo;
	MonoDebugSourceLocation *location;
	gint32 offset;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return NULL;

	mono_debugger_lock ();
	minfo = _mono_debug_lookup_method (method);
	if (!minfo || !minfo->handle || !minfo->handle->symfile || !minfo->handle->symfile->offset_table) {
		mono_debugger_unlock ();
		return NULL;
	}

	offset = il_offset_from_address (method, domain, address);
	if (offset < 0) {
		mono_debugger_unlock ();
		return NULL;
	}

	location = mono_debug_symfile_lookup_location (minfo, offset);
	mono_debugger_unlock ();
	return location;
}

/**
 * mono_debug_free_source_location:
 * @location: A `MonoDebugSourceLocation'.
 *
 * Frees the @location.
 */
void
mono_debug_free_source_location (MonoDebugSourceLocation *location)
{
	if (location) {
		g_free (location->source_file);
		g_free (location);
	}
}

/**
 * mono_debug_print_stack_frame:
 * @native_offset: Native offset within the @method's machine code.
 *
 * Conventient wrapper around mono_debug_lookup_source_location() which can be
 * used if you only want to use the location to print a stack frame.
 */
gchar *
mono_debug_print_stack_frame (MonoMethod *method, guint32 native_offset, MonoDomain *domain)
{
	MonoDebugSourceLocation *location;
	gchar *fname, *ptr, *res;

	fname = mono_method_full_name (method, TRUE);
	for (ptr = fname; *ptr; ptr++) {
		if (*ptr == ':') *ptr = '.';
	}

	location = mono_debug_lookup_source_location (method, native_offset, domain);

	if (!location) {
		res = g_strdup_printf ("at %s <0x%05x>", fname, native_offset);
		g_free (fname);
		return res;
	}

	res = g_strdup_printf ("at %s [0x%05x] in %s:%d", fname, location->il_offset,
			       location->source_file, location->row);

	g_free (fname);
	mono_debug_free_source_location (location);
	return res;
}

void
mono_debug_list_add (MonoDebugList **list, gconstpointer data)
{
	MonoDebugList *element, **ptr;

	element = g_new0 (MonoDebugList, 1);
	element->data = data;

	for (ptr = list; *ptr; ptr = &(*ptr)->next)
		;

	*ptr = element;
}

void
mono_debug_list_remove (MonoDebugList **list, gconstpointer data)
{
	MonoDebugList **ptr;
	MonoDebugList *next;

	for (ptr = list; *ptr; ptr = &(*ptr)->next) {
		if ((*ptr)->data != data)
			continue;

		next = (*ptr)->next;
		g_free ((*ptr));
		*ptr = next;
		break;
	}
}
