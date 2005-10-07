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

#define SYMFILE_TABLE_CHUNK_SIZE	16
#define DATA_TABLE_PTR_CHUNK_SIZE	256
#define DATA_TABLE_CHUNK_SIZE		32768

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
#else
#define RETURN_UNALIGNED(type, addr) \
	return *(type*)(p + offset);
#define WRITE_UNALIGNED(type, addr, val) \
	(*(type *)(addr) = (val))
#endif

MonoSymbolTable *mono_symbol_table = NULL;
MonoDebugFormat mono_debug_format = MONO_DEBUG_FORMAT_NONE;

static gboolean in_the_mono_debugger = FALSE;
static gboolean mono_debug_initialized = FALSE;
GHashTable *mono_debug_handles = NULL;

static GHashTable *method_hash = NULL;

static MonoDebugHandle     *mono_debug_open_image      (MonoImage *image);
static void                 mono_debug_close_image     (MonoDebugHandle *debug);

static MonoDebugHandle     *_mono_debug_get_image      (MonoImage *image);
static void                 mono_debug_add_assembly    (MonoAssembly *assembly,
							gpointer user_data);
static void                 mono_debug_start_add_type  (MonoClass *klass);
static void                 mono_debug_add_type        (MonoClass *klass);

extern void (*mono_debugger_class_init_func) (MonoClass *klass);
extern void (*mono_debugger_start_class_init_func) (MonoClass *klass);

typedef struct {
	guint32 symfile_id;
	guint32 domain_id;
	guint32 method_id;
} MethodHashEntry;

static guint
method_hash_hash (gconstpointer data)
{
	const MethodHashEntry *entry = (const MethodHashEntry *) data;
	return entry->symfile_id | (entry->domain_id << 16);
}

static gint
method_hash_equal (gconstpointer ka, gconstpointer kb)
{
	const MethodHashEntry *a = (const MethodHashEntry *) ka;
	const MethodHashEntry *b = (const MethodHashEntry *) kb;

	if ((a->symfile_id != b->symfile_id) || (a->method_id != b->method_id) || (a->domain_id != b->domain_id))
		return 0;
	return 1;
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
	in_the_mono_debugger = format == MONO_DEBUG_FORMAT_DEBUGGER;

	mono_debugger_initialize (in_the_mono_debugger);

	mono_debugger_lock ();

	mono_symbol_table = g_new0 (MonoSymbolTable, 1);
	mono_symbol_table->magic = MONO_DEBUGGER_MAGIC;
	mono_symbol_table->version = MONO_DEBUGGER_VERSION;
	mono_symbol_table->total_size = sizeof (MonoSymbolTable);

	mono_debug_handles = g_hash_table_new_full
		(NULL, NULL, NULL, (GDestroyNotify) mono_debug_close_image);
	method_hash = g_hash_table_new (method_hash_hash, method_hash_equal);

	mono_debugger_start_class_init_func = mono_debug_start_add_type;
	mono_debugger_class_init_func = mono_debug_add_type;
	mono_install_assembly_load_hook (mono_debug_add_assembly, NULL);

	if (!in_the_mono_debugger)
		mono_debugger_unlock ();
}

void
mono_debug_init_1 (MonoDomain *domain)
{
	MonoDebugHandle *handle = mono_debug_open_image (mono_get_corlib ());

	if (in_the_mono_debugger)
		mono_debugger_add_builtin_types (handle);
}

/*
 * Initialize debugging support - part 2.
 *
 * This method must be called after loading the application's main assembly.
 */
void
mono_debug_init_2 (MonoAssembly *assembly)
{
	mono_debug_open_image (mono_assembly_get_image (assembly));
}

void
mono_debug_cleanup (void)
{
	mono_debugger_cleanup ();

	if (mono_debug_handles)
		g_hash_table_destroy (mono_debug_handles);
	mono_debug_handles = NULL;
}

static MonoDebugHandle *
_mono_debug_get_image (MonoImage *image)
{
	return g_hash_table_lookup (mono_debug_handles, image);
}

static MonoDebugHandle *
allocate_debug_handle (MonoSymbolTable *table)
{
	MonoDebugHandle *handle;

	if (!table->symbol_files)
		table->symbol_files = g_new0 (MonoDebugHandle *, SYMFILE_TABLE_CHUNK_SIZE);
	else if (!((table->num_symbol_files + 1) % SYMFILE_TABLE_CHUNK_SIZE)) {
		guint32 chunks = (table->num_symbol_files + 1) / SYMFILE_TABLE_CHUNK_SIZE;
		guint32 size = sizeof (MonoDebugHandle *) * SYMFILE_TABLE_CHUNK_SIZE * (chunks + 1);

		table->symbol_files = g_realloc (table->symbol_files, size);
	}

	handle = g_new0 (MonoDebugHandle, 1);
	handle->index = table->num_symbol_files;
	table->symbol_files [table->num_symbol_files++] = handle;
	return handle;
}

static MonoDebugHandle *
mono_debug_open_image (MonoImage *image)
{
	MonoDebugHandle *handle;

	if (mono_image_is_dynamic (image))
		return NULL;

	handle = _mono_debug_get_image (image);
	if (handle != NULL)
		return handle;

	handle = allocate_debug_handle (mono_symbol_table);

	handle->image = image;
	mono_image_addref (image);
	handle->image_file = g_strdup (mono_image_get_filename (image));

	g_hash_table_insert (mono_debug_handles, image, handle);

	handle->symfile = mono_debug_open_mono_symbol_file (handle, in_the_mono_debugger);
	if (in_the_mono_debugger)
		mono_debugger_add_symbol_file (handle);

	return handle;
}

static void
mono_debug_close_image (MonoDebugHandle *handle)
{
	if (handle->symfile)
		mono_debug_close_mono_symbol_file (handle->symfile);
	/* decrease the refcount added with mono_image_addref () */
	mono_image_close (handle->image);
	g_free (handle->image_file);
	g_free (handle->_priv);
	g_free (handle);
}

static void
mono_debug_add_assembly (MonoAssembly *assembly, gpointer user_data)
{
	mono_debugger_lock ();
	mono_debug_open_image (mono_assembly_get_image (assembly));
	mono_debugger_unlock ();
}

/*
 * Allocate a new data item of size `size'.
 * Returns the global offset which is to be used to reference this data item and
 * a pointer (in the `ptr' argument) which is to be used to write it.
 */
static guint8 *
allocate_data_item (MonoDebugDataItemType type, guint32 size)
{
	guint32 chunk_size;
	guint8 *data;

	g_assert (mono_symbol_table);

	size = ALIGN_TO (size, sizeof (gpointer));

	if (size + 16 < DATA_TABLE_CHUNK_SIZE)
		chunk_size = DATA_TABLE_CHUNK_SIZE;
	else
		chunk_size = size + 16;

	/* Initialize things if necessary. */
	if (!mono_symbol_table->current_data_table) {
		mono_symbol_table->current_data_table = g_malloc0 (chunk_size);
		mono_symbol_table->current_data_table_size = chunk_size;
		mono_symbol_table->current_data_table_offset = sizeof (gpointer);

		* ((guint32 *) mono_symbol_table->current_data_table) = chunk_size;
	}

 again:
	/* First let's check whether there's still enough room in the current_data_table. */
	if (mono_symbol_table->current_data_table_offset + size + 8 < mono_symbol_table->current_data_table_size) {
		data = ((guint8 *) mono_symbol_table->current_data_table) + mono_symbol_table->current_data_table_offset;
		mono_symbol_table->current_data_table_offset += size + 8;

		* ((guint32 *) data) = size;
		data += 4;
		* ((guint32 *) data) = type;
		data += 4;
		return data;
	}

	/* Add the current_data_table to the data_tables vector and ... */
	if (!mono_symbol_table->data_tables) {
		guint32 tsize = sizeof (gpointer) * DATA_TABLE_PTR_CHUNK_SIZE;
		mono_symbol_table->data_tables = g_malloc0 (tsize);
	}

	if (!((mono_symbol_table->num_data_tables + 1) % DATA_TABLE_PTR_CHUNK_SIZE)) {
		guint32 chunks = (mono_symbol_table->num_data_tables + 1) / DATA_TABLE_PTR_CHUNK_SIZE;
		guint32 tsize = sizeof (gpointer) * DATA_TABLE_PTR_CHUNK_SIZE * (chunks + 1);

		mono_symbol_table->data_tables = g_realloc (mono_symbol_table->data_tables, tsize);
	}

	mono_symbol_table->data_tables [mono_symbol_table->num_data_tables++] = mono_symbol_table->current_data_table;

	/* .... allocate a new current_data_table. */
	mono_symbol_table->current_data_table = g_malloc0 (chunk_size);
	mono_symbol_table->current_data_table_size = chunk_size;
	mono_symbol_table->current_data_table_offset = sizeof (gpointer);
	* ((guint32 *) mono_symbol_table->current_data_table) = chunk_size;

	goto again;
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
		data->minfo = mono_debug_find_method (handle, data->method);
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
	*rptr = ptr;
}

static MonoDebugWrapperData *
mono_debug_add_wrapper (MonoMethod *method, MonoDebugMethodJitInfo *jit)
{
	MonoMethodHeader *header;
	MonoDebugWrapperData *wrapper;
	char buffer [BUFSIZ];
	guint8 *ptr, *oldptr;
	guint32 i, size, total_size, max_size;
	gint32 last_il_offset = 0, last_native_offset = 0;

	if (!in_the_mono_debugger)
		return NULL;

	mono_debugger_lock ();

	header = ((MonoMethodNormal *) method)->header;

	max_size = 28 * jit->num_line_numbers;
	if (max_size > BUFSIZ)
		ptr = oldptr = g_malloc (max_size);
	else
		ptr = oldptr = buffer;

	write_leb128 (jit->prologue_end, ptr, &ptr);
	write_leb128 (jit->epilogue_begin, ptr, &ptr);
	write_leb128 (jit->num_line_numbers, ptr, &ptr);
	for (i = 0; i < jit->num_line_numbers; i++) {
		MonoDebugLineNumberEntry *lne = &jit->line_numbers [i];

		write_sleb128 (lne->il_offset - last_il_offset, ptr, &ptr);
		write_sleb128 (lne->native_offset - last_native_offset, ptr, &ptr);

		last_il_offset = lne->il_offset;
		last_native_offset = lne->native_offset;
	}

	write_leb128 (method->wrapper_type, ptr, &ptr);

	size = ptr - oldptr;
	g_assert (size < max_size);
	total_size = size + sizeof (MonoDebugWrapperData);

	if (total_size + 9 >= DATA_TABLE_CHUNK_SIZE) {
		// FIXME: Maybe we should print a warning here.
		//        This should only happen for very big methods, for instance
		//        with more than 40.000 line numbers and more than 5.000
		//        local variables.
		mono_debugger_unlock ();
		return NULL;
	}

	wrapper = (MonoDebugWrapperData *) allocate_data_item (MONO_DEBUG_DATA_ITEM_WRAPPER, total_size);

	wrapper->method = method;
	wrapper->size = total_size;
	wrapper->code_start = jit->code_start;
	wrapper->code_size = jit->code_size;
	wrapper->name = mono_method_full_name (method, TRUE);

	wrapper->cil_code = mono_disasm_code (
		NULL, method, header->code, header->code + header->code_size);

	memcpy (&wrapper->data, oldptr, size);

	if (max_size > BUFSIZ)
		g_free (oldptr);

	mono_debugger_unlock ();

	return wrapper;
}

/*
 * This is called by the JIT to tell the debugging code about a newly
 * compiled method.
 */
MonoDebugMethodAddress *
mono_debug_add_method (MonoMethod *method, MonoDebugMethodJitInfo *jit, MonoDomain *domain)
{
	MonoDebugMethodAddress *address;
	char buffer [BUFSIZ];
	guint8 *ptr, *oldptr;
	guint32 i, size, total_size, max_size;
	gint32 last_il_offset = 0, last_native_offset = 0;
	MonoDebugHandle *handle;
	MonoDebugMethodInfo *minfo;
	MethodHashEntry *hash;

	if (method->is_inflated)
		return NULL;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->flags & METHOD_ATTRIBUTE_ABSTRACT) ||
	    (method->wrapper_type != MONO_WRAPPER_NONE)) {
		mono_debug_add_wrapper (method, jit);
		return NULL;
	}

	mono_debugger_lock ();

	handle = _mono_debug_get_image (method->klass->image);
	if (!handle || !handle->symfile || !handle->symfile->offset_table) {
		mono_debug_add_wrapper (method, jit);
		mono_debugger_unlock ();
		return NULL;
	}

	minfo = _mono_debug_lookup_method (method);
	if (!minfo) {
		mono_debugger_unlock ();
		return NULL;
	}

	max_size = 24 + 8 * jit->num_line_numbers + 16 * minfo->num_lexical_blocks + 20 * (1 + jit->num_params + jit->num_locals);
	if (max_size > BUFSIZ)
		ptr = oldptr = g_malloc (max_size);
	else
		ptr = oldptr = buffer;

	write_leb128 (jit->prologue_end, ptr, &ptr);
	write_leb128 (jit->epilogue_begin, ptr, &ptr);

	write_leb128 (jit->num_line_numbers, ptr, &ptr);
	for (i = 0; i < jit->num_line_numbers; i++) {
		MonoDebugLineNumberEntry *lne = &jit->line_numbers [i];

		write_sleb128 (lne->il_offset - last_il_offset, ptr, &ptr);
		write_sleb128 (lne->native_offset - last_native_offset, ptr, &ptr);

		last_il_offset = lne->il_offset;
		last_native_offset = lne->native_offset;
	}

	jit->num_lexical_blocks = minfo->num_lexical_blocks;
	jit->lexical_blocks = g_new0 (MonoDebugLexicalBlockEntry, jit->num_lexical_blocks);
	for (i = 0; i < jit->num_lexical_blocks; i ++) {
		MonoDebugLexicalBlockEntry *jit_lbe = &jit->lexical_blocks [i];
		MonoSymbolFileLexicalBlockEntry *minfo_lbe = &minfo->lexical_blocks [i];
		jit_lbe->il_start_offset = minfo_lbe->_start_offset;
		jit_lbe->native_start_offset = _mono_debug_address_from_il_offset (jit, jit_lbe->il_start_offset);

		jit_lbe->il_end_offset = minfo_lbe->_end_offset;
		jit_lbe->native_end_offset = _mono_debug_address_from_il_offset (jit, jit_lbe->il_end_offset);
	}

	last_il_offset = 0;
	last_native_offset = 0;
	write_leb128 (jit->num_lexical_blocks, ptr, &ptr);
	for (i = 0; i < jit->num_lexical_blocks; i++) {
		MonoDebugLexicalBlockEntry *lbe = &jit->lexical_blocks [i];

		write_sleb128 (lbe->il_start_offset - last_il_offset, ptr, &ptr);
		write_sleb128 (lbe->native_start_offset - last_native_offset, ptr, &ptr);

		last_il_offset = lbe->il_start_offset;
		last_native_offset = lbe->native_start_offset;

		write_sleb128 (lbe->il_end_offset - last_il_offset, ptr, &ptr);
		write_sleb128 (lbe->native_end_offset - last_native_offset, ptr, &ptr);

		last_il_offset = lbe->il_end_offset;
		last_native_offset = lbe->native_end_offset;
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

	if (total_size + 9 >= DATA_TABLE_CHUNK_SIZE) {
		// FIXME: Maybe we should print a warning here.
		//        This should only happen for very big methods, for instance
		//        with more than 40.000 line numbers and more than 5.000
		//        local variables.
		mono_debugger_unlock ();
		return NULL;
	}

	address = (MonoDebugMethodAddress *) allocate_data_item (MONO_DEBUG_DATA_ITEM_METHOD, total_size);

	address->size = total_size;
	address->symfile_id = handle->index;
	address->domain_id = mono_domain_get_id (domain);
	address->method_id = minfo->index;
	address->code_start = jit->code_start;
	address->code_size = jit->code_size;
	address->wrapper_addr = jit->wrapper_addr;

	memcpy (&address->data, oldptr, size);

	if (max_size > BUFSIZ)
		g_free (oldptr);

	hash = g_new0 (MethodHashEntry, 1);
	hash->symfile_id = address->symfile_id;
	hash->domain_id = address->domain_id;
	hash->method_id = address->method_id;

	g_hash_table_insert (method_hash, hash, address);

	mono_debugger_unlock ();

	return address;
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
	*rptr = ptr;
}

MonoDebugMethodJitInfo *
mono_debug_read_method (MonoDebugMethodAddress *address)
{
	MonoDebugMethodJitInfo *jit;
	guint32 i, il_offset = 0, native_offset = 0;
	guint8 *ptr;

	if (address->jit)
		return address->jit;

	jit = address->jit = g_new0 (MonoDebugMethodJitInfo, 1);
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

		il_offset += read_sleb128 (ptr, &ptr);
		native_offset += read_sleb128 (ptr, &ptr);

		lne->il_offset = il_offset;
		lne->native_offset = native_offset;
	}

	il_offset = 0;
	native_offset = 0;
	jit->num_lexical_blocks = read_leb128 (ptr, &ptr);
	jit->lexical_blocks = g_new0 (MonoDebugLexicalBlockEntry, jit->num_lexical_blocks);
	for (i = 0; i < jit->num_lexical_blocks; i ++) {
		MonoDebugLexicalBlockEntry *lbe = &jit->lexical_blocks [i];

		il_offset += read_sleb128 (ptr, &ptr);
		native_offset += read_sleb128 (ptr, &ptr);

		lbe->il_start_offset = il_offset;
		lbe->native_start_offset = native_offset;

		il_offset += read_sleb128 (ptr, &ptr);
		native_offset += read_sleb128 (ptr, &ptr);

		lbe->il_end_offset = il_offset;
		lbe->native_end_offset = native_offset;
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

void
mono_debug_free_method_jit_info (MonoDebugMethodJitInfo *jit)
{
	if (jit->address)
		jit->address->jit = NULL;

	g_free (jit->line_numbers);
	g_free (jit->this_var);
	g_free (jit->params);
	g_free (jit->locals);
	g_free (jit);
}

/*
 * This is called via the `mono_debugger_class_init_func' from mono_class_init() each time
 * a new class is initialized.
 */
static void
mono_debug_start_add_type (MonoClass *klass)
{
	MonoDebugHandle *handle;

	handle = _mono_debug_get_image (klass->image);
	if (!handle)
		return;
}

static void
mono_debug_add_type (MonoClass *klass)
{
	MonoDebugHandle *handle;
	MonoDebugClassEntry *entry;
	char buffer [BUFSIZ];
	guint8 *ptr, *oldptr;
	guint32 size, total_size, max_size;
	int base_offset = 0;

	handle = _mono_debug_get_image (klass->image);
	if (!handle)
		return;

	if (klass->generic_class || klass->rank ||
	    (klass->byval_arg.type == MONO_TYPE_VAR) || (klass->byval_arg.type == MONO_TYPE_MVAR))
		return;

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

	entry = (MonoDebugClassEntry *) allocate_data_item (MONO_DEBUG_DATA_ITEM_CLASS, total_size);

	entry->size = total_size;
	entry->symfile_id = handle->index;

	memcpy (&entry->data, oldptr, size);

	if (max_size > BUFSIZ)
		g_free (oldptr);

	mono_debugger_start_add_type (handle, klass);
}

static MonoDebugMethodJitInfo *
find_method (MonoDebugMethodInfo *minfo, MonoDomain *domain)
{
	MethodHashEntry lookup;
	MonoDebugMethodAddress *address;

	lookup.symfile_id = minfo->handle->index;
	lookup.domain_id = mono_domain_get_id (domain);
	lookup.method_id = minfo->index;

	address = g_hash_table_lookup (method_hash, &lookup);
	if (!address)
		return NULL;

	return mono_debug_read_method (address);
}

static gint32
il_offset_from_address (MonoDebugMethodInfo *minfo, MonoDomain *domain, guint32 native_offset)
{
	MonoDebugMethodJitInfo *jit;
	int i;

	jit = find_method (minfo, domain);
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
 * mono_debug_source_location_from_address:
 * @method:
 * @address:
 * @line_number:
 * @domain:
 *
 * Used by the exception code to get a source location from a machine address.
 *
 * Returns: a textual representation of the specified address which is suitable to be displayed to
 * the user (for instance "/home/martin/monocvs/debugger/test/Y.cs:8").
 *
 * If the optional @line_number argument is not NULL, the line number is stored there and just the
 * source file is returned (ie. it'd return "/home/martin/monocvs/debugger/test/Y.cs" and store the
 * line number 8 in the variable pointed to by @line_number).
 */
gchar *
mono_debug_source_location_from_address (MonoMethod *method, guint32 address, guint32 *line_number,
					 MonoDomain *domain)
{
	MonoDebugMethodInfo *minfo;
	char *res = NULL;
	gint32 offset;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return NULL;

	mono_debugger_lock ();
	minfo = _mono_debug_lookup_method (method);
	if (!minfo || !minfo->handle || !minfo->handle->symfile || !minfo->handle->symfile->offset_table) {
		mono_debugger_unlock ();
		return NULL;
	}

	offset = il_offset_from_address (minfo, domain, address);
		
	if (offset >= 0)
		res = mono_debug_find_source_location (minfo->handle->symfile, method, offset, line_number);

	mono_debugger_unlock ();
	return res;
}

/**
 * mono_debug_source_location_from_il_offset:
 * @method:
 * @offset:
 * @line_number:
 *
 * Used by the exception code to get a source location from an IL offset.
 *
 * Returns a textual representation of the specified address which is suitable to be displayed to
 * the user (for instance "/home/martin/monocvs/debugger/test/Y.cs:8").
 *
 * If the optional @line_number argument is not NULL, the line number is stored there and just the
 * source file is returned (ie. it'd return "/home/martin/monocvs/debugger/test/Y.cs" and store the
 * line number 8 in the variable pointed to by @line_number).
 */
gchar *
mono_debug_source_location_from_il_offset (MonoMethod *method, guint32 offset, guint32 *line_number)
{
	char *res;
	MonoDebugMethodInfo *minfo;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return NULL;

	mono_debugger_lock ();
	minfo = _mono_debug_lookup_method (method);
	if (!minfo || !minfo->handle || !minfo->handle->symfile) {
		mono_debugger_unlock ();
		return NULL;
	}

	res = mono_debug_find_source_location (minfo->handle->symfile, method, offset, line_number);
	mono_debugger_unlock ();
	return res;
}

/**
 * mono_debug_il_offset_from_address:
 * @method:
 * @address:
 * @domain:
 *
 * Returns: the IL offset corresponding to machine address @address which is an offset
 * relative to the beginning of the method @method.
 */
gint32
mono_debug_il_offset_from_address (MonoMethod *method, gint32 address, MonoDomain *domain)
{
	MonoDebugMethodInfo *minfo;
	gint32 res;

	if ((address < 0) || (mono_debug_format == MONO_DEBUG_FORMAT_NONE))
		return -1;

	mono_debugger_lock ();
	minfo = _mono_debug_lookup_method (method);
	if (!minfo || !minfo->il_offsets || !minfo->handle || !minfo->handle->symfile ||
	    !minfo->handle->symfile->offset_table) {
		mono_debugger_unlock ();
		return -1;
	}

	res = il_offset_from_address (minfo, domain, address);
	mono_debugger_unlock ();
	return res;
}

/**
 * mono_debug_address_from_il_offset:
 * @method:
 * @il_offset:
 * @domain:
 *
 * Returns: the machine address corresponding to IL offset @il_offset.
 * The returned value is an offset relative to the beginning of the method @method.
 */
gint32
mono_debug_address_from_il_offset (MonoMethod *method, gint32 il_offset, MonoDomain *domain)
{
	MonoDebugMethodInfo *minfo;
	MonoDebugMethodJitInfo *jit;
	gint32 res;

	if ((il_offset < 0) || (mono_debug_format == MONO_DEBUG_FORMAT_NONE))
		return -1;

	mono_debugger_lock ();
	minfo = _mono_debug_lookup_method (method);
	if (!minfo || !minfo->il_offsets || !minfo->handle || !minfo->handle->symfile ||
	    !minfo->handle->symfile->offset_table) {
		mono_debugger_unlock ();
		return -1;
	}

	jit = find_method (minfo, domain);
	if (!jit) {
		mono_debugger_unlock ();
		return -1;
	}

	res = _mono_debug_address_from_il_offset (jit, il_offset);
	mono_debugger_unlock ();
	return res;
}
