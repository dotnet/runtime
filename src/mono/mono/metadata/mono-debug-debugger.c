#include <config.h>
#include <stdlib.h>
#include <string.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/gc-internal.h>
#include <mono/os/gc_wrapper.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/mono-endian.h>

#define SYMFILE_TABLE_CHUNK_SIZE	16
#define RANGE_TABLE_CHUNK_SIZE		256
#define CLASS_TABLE_CHUNK_SIZE		256
#define TYPE_TABLE_PTR_CHUNK_SIZE	256
#define TYPE_TABLE_CHUNK_SIZE		65536
#define MISC_TABLE_PTR_CHUNK_SIZE	256
#define MISC_TABLE_CHUNK_SIZE		65536

static guint32 debugger_lock_level = 0;
static CRITICAL_SECTION debugger_lock_mutex;
static gboolean mono_debugger_initialized = FALSE;
static MonoObject *last_exception = NULL;

static gboolean must_reload_symtabs = FALSE;
static gboolean builtin_types_initialized = FALSE;

static GHashTable *images = NULL;
static GHashTable *type_table = NULL;
static GHashTable *misc_table = NULL;
static GHashTable *class_table = NULL;
static GHashTable *class_info_table = NULL;

static MonoDebuggerRangeInfo *allocate_range_entry (MonoDebuggerSymbolFile *symfile);
static MonoDebuggerClassInfo *allocate_class_entry (MonoDebuggerSymbolFile *symfile);
static guint32 allocate_type_entry (MonoDebuggerSymbolTable *table, guint32 size, guint8 **ptr);
static guint32 write_type (MonoDebuggerSymbolTable *table, MonoType *type);
static guint32 do_write_class (MonoDebuggerSymbolTable *table, MonoClass *klass,
			       MonoDebuggerClassInfo *cinfo);
static guint32 write_class (MonoDebuggerSymbolTable *table, MonoClass *klass);

MonoDebuggerSymbolTable *mono_debugger_symbol_table = NULL;
void (*mono_debugger_event_handler) (MonoDebuggerEvent event, gpointer data, guint32 arg) = NULL;

#define WRITE_UINT32(ptr,value) G_STMT_START {	\
	* ((guint32 *) ptr) = value;		\
	ptr += 4;				\
} G_STMT_END

#define WRITE_POINTER(ptr,value) G_STMT_START {	\
	* ((gpointer *) ptr) = value;		\
	ptr += sizeof (gpointer);		\
} G_STMT_END

#define WRITE_STRING(ptr,value) G_STMT_START {	\
	memcpy (ptr, value, strlen (value)+1);	\
	ptr += strlen (value)+1;		\
} G_STMT_END

#ifndef PLATFORM_WIN32

MonoDebuggerIOLayer mono_debugger_io_layer = {
	InitializeCriticalSection, DeleteCriticalSection, TryEnterCriticalSection,
	EnterCriticalSection, LeaveCriticalSection, WaitForSingleObjectEx, SignalObjectAndWait,
	WaitForMultipleObjectsEx, CreateSemaphore, ReleaseSemaphore, CreateThread,
	GetCurrentThreadId
};

#endif

void
mono_debugger_lock (void)
{
	if (!mono_debugger_initialized) {
		debugger_lock_level++;
		return;
	}

	EnterCriticalSection (&debugger_lock_mutex);
	debugger_lock_level++;
}

void
mono_debugger_unlock (void)
{
	g_assert (debugger_lock_level > 0);

	if (!mono_debugger_initialized) {
		debugger_lock_level--;
		return;
	}

	if (debugger_lock_level == 1) {
		if (must_reload_symtabs) {
			mono_debugger_event (MONO_DEBUGGER_EVENT_RELOAD_SYMTABS, NULL, 0);
			must_reload_symtabs = FALSE;
		}
	}

	debugger_lock_level--;
	LeaveCriticalSection (&debugger_lock_mutex);
}

static MonoDebuggerSymbolFile *
allocate_symbol_file_entry (MonoDebuggerSymbolTable *table)
{
	MonoDebuggerSymbolFile *symfile;

	if (!table->symbol_files)
		table->symbol_files = g_new0 (MonoDebuggerSymbolFile *, SYMFILE_TABLE_CHUNK_SIZE);
	else if (!((table->num_symbol_files + 1) % SYMFILE_TABLE_CHUNK_SIZE)) {
		guint32 chunks = (table->num_symbol_files + 1) / SYMFILE_TABLE_CHUNK_SIZE;
		guint32 size = sizeof (MonoDebuggerSymbolFile *) * SYMFILE_TABLE_CHUNK_SIZE * (chunks + 1);

		table->symbol_files = g_realloc (table->symbol_files, size);
	}

	symfile = g_new0 (MonoDebuggerSymbolFile, 1);
	symfile->index = table->num_symbol_files;
	symfile->range_entry_size = sizeof (MonoDebuggerRangeInfo);
	symfile->class_entry_size = sizeof (MonoDebuggerClassInfo);
	symfile->class_table_size = sizeof (MonoDebuggerClassTable);
	table->symbol_files [table->num_symbol_files++] = symfile;
	return symfile;
}

void
mono_debugger_initialize (void)
{
	MonoDebuggerSymbolTable *symbol_table;
	
	MONO_GC_REGISTER_ROOT (last_exception);
	
	g_assert (!mono_debugger_initialized);

	InitializeCriticalSection (&debugger_lock_mutex);
	mono_debugger_initialized = TRUE;

	mono_debugger_lock ();

	symbol_table = g_new0 (MonoDebuggerSymbolTable, 1);
	symbol_table->magic = MONO_DEBUGGER_MAGIC;
	symbol_table->version = MONO_DEBUGGER_VERSION;
	symbol_table->total_size = sizeof (MonoDebuggerSymbolTable);

	images = g_hash_table_new (g_direct_hash, g_direct_equal);
	type_table = g_hash_table_new (g_direct_hash, g_direct_equal);
	misc_table = g_hash_table_new (g_direct_hash, g_direct_equal);
	class_table = g_hash_table_new (g_direct_hash, g_direct_equal);
	class_info_table = g_hash_table_new (g_direct_hash, g_direct_equal);

	mono_debugger_symbol_table = symbol_table;

	mono_debugger_unlock ();
}

MonoDebuggerSymbolFile *
mono_debugger_add_symbol_file (MonoDebugHandle *handle)
{
	MonoDebuggerSymbolFile *info;

	g_assert (mono_debugger_initialized);
	mono_debugger_lock ();

	info = g_hash_table_lookup (images, handle->image);
	if (info) {
		mono_debugger_unlock ();
		return info;
	}

	info = allocate_symbol_file_entry (mono_debugger_symbol_table);
	info->symfile = handle->symfile;
	info->image = handle->image;
	info->image_file = handle->image_file;

	g_hash_table_insert (images, handle->image, info);
	mono_debugger_unlock ();

	return info;
}

static void
write_builtin_type (MonoDebuggerSymbolTable *table, MonoDebuggerSymbolFile *symfile,
		    MonoClass *klass, MonoDebuggerBuiltinTypeInfo *info)
{
	guint8 buffer [BUFSIZ], *ptr = buffer;
	guint32 size;

	g_assert (!klass->init_pending);
	mono_class_init (klass);

	switch (klass->byval_arg.type) {
	case MONO_TYPE_VOID:
		*ptr++ = MONO_DEBUGGER_TYPE_KIND_UNKNOWN;
		WRITE_UINT32 (ptr, 0);
		ptr += 4;
		break;

	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		*ptr++ = MONO_DEBUGGER_TYPE_KIND_FUNDAMENTAL;
		WRITE_UINT32 (ptr, klass->instance_size - sizeof (MonoObject));
		ptr += 4;
		break;

	case MONO_TYPE_STRING: {
		MonoString string;

		*ptr++ = MONO_DEBUGGER_TYPE_KIND_STRING;
		WRITE_UINT32 (ptr, klass->instance_size);
		ptr += 4;
		*ptr++ = (guint8*)&string.length - (guint8*)&string;
		*ptr++ = sizeof (string.length);
		*ptr++ = (guint8*)&string.chars - (guint8*)&string;

		break;
	}

	case MONO_TYPE_I:
	case MONO_TYPE_U:
		*ptr++ = MONO_DEBUGGER_TYPE_KIND_FUNDAMENTAL;
		WRITE_UINT32 (ptr, sizeof (void *));
		ptr += 4;
		break;

	case MONO_TYPE_VALUETYPE:
		*ptr++ = MONO_DEBUGGER_TYPE_KIND_STRUCT;
		WRITE_UINT32 (ptr, klass->instance_size);
		ptr += 4;
		break;

	case MONO_TYPE_CLASS:
		*ptr++ = MONO_DEBUGGER_TYPE_KIND_CLASS;
		WRITE_UINT32 (ptr, klass->instance_size);
		ptr += 4;
		break;

	case MONO_TYPE_OBJECT:
		g_assert (klass == mono_defaults.object_class);
		*ptr++ = MONO_DEBUGGER_TYPE_KIND_OBJECT;
		WRITE_UINT32 (ptr, klass->instance_size);
		ptr += 4;
		break;

	default:
		g_error (G_STRLOC ": Unknown builtin type %s.%s - %d", klass->name_space, klass->name, klass->byval_arg.type);
	}

	size = ptr - buffer;
	info->type_info = allocate_type_entry (table, size, &info->type_data);
	memcpy (info->type_data, buffer, size);

	info->centry->info = g_new0 (MonoDebuggerClassInfo, 1); //allocate_class_entry (symfile);
	info->centry->info->klass = klass;
	if (klass->rank) {
		info->centry->info->token = klass->element_class->type_token;
		info->centry->info->rank = klass->rank;
	} else
		info->centry->info->token = klass->type_token;
	info->centry->info->type_info = info->type_info;
}

static MonoDebuggerBuiltinTypeInfo *
add_builtin_type (MonoDebuggerSymbolFile *symfile, MonoClass *klass)
{
	MonoDebuggerClassEntry *centry;
	MonoDebuggerBuiltinTypeInfo *info;

	centry = g_new0 (MonoDebuggerClassEntry, 1);

	info = g_new0 (MonoDebuggerBuiltinTypeInfo, 1);
	info->klass = klass;
	info->centry = centry;

	g_hash_table_insert (class_info_table, klass, centry);

	write_builtin_type (mono_debugger_symbol_table, symfile, klass, info);
	return info;
}

static void
add_builtin_type_2 (MonoDebuggerBuiltinTypeInfo *info)
{
	info->class_info = do_write_class (mono_debugger_symbol_table, info->klass, NULL);
	* (guint32 *) (info->type_data + 5) = info->class_info;
}

static void
add_exception_class (MonoDebuggerSymbolFile *symfile, MonoException *exc)
{
	mono_debugger_start_add_type (symfile, ((MonoObject *) exc)->vtable->klass);
	mono_debugger_add_type (symfile, ((MonoObject *) exc)->vtable->klass);
}

MonoDebuggerBuiltinTypes *
mono_debugger_add_builtin_types (MonoDebuggerSymbolFile *symfile)
{
	MonoDebuggerBuiltinTypes *types = g_new0 (MonoDebuggerBuiltinTypes, 1);
	MonoClass *klass;

	mono_debugger_symbol_table->corlib = symfile;
	mono_debugger_symbol_table->builtin_types = types;

	types->total_size = sizeof (MonoDebuggerBuiltinTypes);
	types->type_info_size = sizeof (MonoDebuggerBuiltinTypeInfo);

	types->object_type = add_builtin_type (symfile, mono_defaults.object_class);
	klass = mono_class_from_name (mono_defaults.corlib, "System", "ValueType");
	g_assert (klass);
	types->valuetype_type = add_builtin_type (symfile, klass);

	types->byte_type = add_builtin_type (symfile, mono_defaults.byte_class);
	types->void_type = add_builtin_type (symfile, mono_defaults.void_class);
	types->boolean_type = add_builtin_type (symfile, mono_defaults.boolean_class);
	types->sbyte_type = add_builtin_type (symfile, mono_defaults.sbyte_class);
	types->int16_type = add_builtin_type (symfile, mono_defaults.int16_class);
	types->uint16_type = add_builtin_type (symfile, mono_defaults.uint16_class);
	types->int32_type = add_builtin_type (symfile, mono_defaults.int32_class);
	types->uint32_type = add_builtin_type (symfile, mono_defaults.uint32_class);
	types->int_type = add_builtin_type (symfile, mono_defaults.int_class);
	types->uint_type = add_builtin_type (symfile, mono_defaults.uint_class);
	types->int64_type = add_builtin_type (symfile, mono_defaults.int64_class);
	types->uint64_type = add_builtin_type (symfile, mono_defaults.uint64_class);
	types->single_type = add_builtin_type (symfile, mono_defaults.single_class);
	types->double_type = add_builtin_type (symfile, mono_defaults.double_class);
	types->char_type = add_builtin_type (symfile, mono_defaults.char_class);
	types->string_type = add_builtin_type (symfile, mono_defaults.string_class);

	klass = mono_class_from_name (mono_defaults.corlib, "System", "Type");
	g_assert (klass);
	types->type_type = add_builtin_type (symfile, klass);

	types->enum_type = add_builtin_type (symfile, mono_defaults.enum_class);
	types->array_type = add_builtin_type (symfile, mono_defaults.array_class);
	types->exception_type = add_builtin_type (symfile, mono_defaults.exception_class);

	add_builtin_type_2 (types->object_type);
	add_builtin_type_2 (types->valuetype_type);

	add_builtin_type_2 (types->byte_type);
	add_builtin_type_2 (types->void_type);
	add_builtin_type_2 (types->boolean_type);
	add_builtin_type_2 (types->sbyte_type);
	add_builtin_type_2 (types->int16_type);
	add_builtin_type_2 (types->uint16_type);
	add_builtin_type_2 (types->int32_type);
	add_builtin_type_2 (types->uint32_type);
	add_builtin_type_2 (types->int_type);
	add_builtin_type_2 (types->uint_type);
	add_builtin_type_2 (types->int64_type);
	add_builtin_type_2 (types->uint64_type);
	add_builtin_type_2 (types->single_type);
	add_builtin_type_2 (types->double_type);
	add_builtin_type_2 (types->char_type);
	add_builtin_type_2 (types->string_type);
	add_builtin_type_2 (types->type_type);

	add_builtin_type_2 (types->enum_type);
	add_builtin_type_2 (types->array_type);
	add_builtin_type_2 (types->exception_type);

	add_exception_class (symfile, mono_get_exception_divide_by_zero ());
	add_exception_class (symfile, mono_get_exception_security ());
	add_exception_class (symfile, mono_get_exception_arithmetic ());
	add_exception_class (symfile, mono_get_exception_overflow ());
	add_exception_class (symfile, mono_get_exception_null_reference ());
	add_exception_class (symfile, mono_get_exception_thread_abort ());
	add_exception_class (symfile, mono_get_exception_invalid_cast ());
	add_exception_class (symfile, mono_get_exception_index_out_of_range ());
	add_exception_class (symfile, mono_get_exception_thread_abort ());
	add_exception_class (symfile, mono_get_exception_index_out_of_range ());
	add_exception_class (symfile, mono_get_exception_array_type_mismatch ());
	add_exception_class (symfile, mono_get_exception_missing_method ());
	add_exception_class (symfile, mono_get_exception_appdomain_unloaded ());
	add_exception_class (symfile, mono_get_exception_stack_overflow ());

	builtin_types_initialized = TRUE;

	return types;
}

static guint32
write_class (MonoDebuggerSymbolTable *table, MonoClass *klass)
{
	MonoDebuggerClassEntry *centry;

	if (builtin_types_initialized && !klass->init_pending)
		mono_class_init (klass);

	centry = g_hash_table_lookup (class_info_table, klass);
	if (!centry) {
		MonoDebuggerSymbolFile *symfile = _mono_debugger_get_symfile (klass->image);

		g_assert (symfile);
		mono_debugger_start_add_type (symfile, klass);
		centry = g_hash_table_lookup (class_info_table, klass);
	}
	g_assert (centry);

	if (centry->info) {
		g_assert (centry->info->type_info);
		return centry->info->type_info;
	}

	if (!centry->type_reference) {
		guint8 *ptr;

		centry->type_reference = allocate_type_entry (table, 5, &ptr);

		*ptr++ = MONO_DEBUGGER_TYPE_KIND_REFERENCE;
		WRITE_POINTER (ptr, klass);
	}

	return centry->type_reference;
}

void
mono_debugger_start_add_type (MonoDebuggerSymbolFile *symfile, MonoClass *klass)
{
	MonoDebuggerClassEntry *centry;

	mono_debugger_lock ();
	centry = g_hash_table_lookup (class_info_table, klass);
	if (centry) {
		mono_debugger_unlock ();
		return;
	}

	centry = g_new0 (MonoDebuggerClassEntry, 1);
	g_hash_table_insert (class_info_table, klass, centry);
	mono_debugger_unlock ();
}

void
mono_debugger_add_type (MonoDebuggerSymbolFile *symfile, MonoClass *klass)
{
	MonoDebuggerClassEntry *centry;

	mono_debugger_lock ();
	centry = g_hash_table_lookup (class_info_table, klass);
	g_assert (centry);

	if (centry->info) {
		mono_debugger_unlock ();
		return;
	}

	centry->info = allocate_class_entry (symfile);
	centry->info->klass = klass;
	if (klass->rank) {
		centry->info->token = klass->element_class->type_token;
		centry->info->rank = klass->rank;
	} else
		centry->info->token = klass->type_token;

	do_write_class (mono_debugger_symbol_table, klass, centry->info);

	g_assert (centry->info && centry->info->type_info);

	symfile->generation++;
	must_reload_symtabs = TRUE;

	mono_debugger_unlock ();
}

void
mono_debugger_add_method (MonoDebuggerSymbolFile *symfile, MonoDebugMethodInfo *minfo,
			  MonoDebugMethodJitInfo *jit)
{
	MonoSymbolFileMethodAddress *address;
	MonoSymbolFileLexicalBlockEntry *block;
	MonoDebugVarInfo *var_table;
	MonoDebuggerRangeInfo *range;
	MonoMethodHeader *header;
	guint32 size, num_variables, variable_size, variable_offset;
	guint32 type_size, type_offset, *type_index_table, has_this;
	guint32 line_size, line_offset, block_offset, block_size;
	MonoDebugLexicalBlockEntry *block_table;
	MonoDebugLineNumberEntry *line_table;
	guint32 *type_table;
	guint8 *ptr;
	int i;

	if (!symfile->symfile->method_hash)
		return;

	header = ((MonoMethodNormal *) minfo->method)->header;

	symfile->generation++;

	size = sizeof (MonoSymbolFileMethodAddress);

	num_variables = jit->num_params + read32(&(minfo->entry->_num_locals));
	has_this = jit->this_var != NULL;

	variable_size = (num_variables + has_this) * sizeof (MonoDebugVarInfo);
	variable_offset = size;
	size += variable_size;

	type_size = (num_variables + 1) * sizeof (gpointer);
	type_offset = size;
	size += type_size;

	if (jit->line_numbers) {
		line_offset = size;
		line_size = jit->line_numbers->len * sizeof (MonoDebugLineNumberEntry);
		size += line_size;
	}

	block_size = read32(&(minfo->entry->_num_lexical_blocks)) * sizeof (MonoSymbolFileLexicalBlockEntry);
	block_offset = size;
	size += block_size;

	address = g_malloc0 (size);
	ptr = (guint8 *) address;

	block = (MonoSymbolFileLexicalBlockEntry *)
		(symfile->symfile->raw_contents + read32(&(minfo->entry->_lexical_block_table_offset)));
	block_table = (MonoDebugLexicalBlockEntry *) (ptr + block_offset);

	for (i = 0; i < read32(&(minfo->entry->_num_lexical_blocks)); i++, block++) {
		block_table [i].start_address = _mono_debug_address_from_il_offset (jit, read32(&(block->_start_offset)));
		block_table [i].end_address = _mono_debug_address_from_il_offset (jit, read32(&(block->_end_offset)));
	}

	address->size = size;
	address->has_this = has_this;
	address->num_params = jit->num_params;
	address->start_address = jit->code_start;
	address->end_address = jit->code_start + jit->code_size;
	address->method_start_address = address->start_address + jit->prologue_end;
	address->method_end_address = address->start_address + jit->epilogue_begin;
	address->wrapper_address = jit->wrapper_addr;
	address->variable_table_offset = variable_offset;
	address->type_table_offset = type_offset;
	address->lexical_block_table_offset = block_offset;

	if (jit->line_numbers) {
		address->num_line_numbers = jit->line_numbers->len;
		address->line_number_offset = line_offset;

		line_table = (MonoDebugLineNumberEntry *) (ptr + line_offset);
		memcpy (line_table, jit->line_numbers->data, line_size);
	}

	range = allocate_range_entry (symfile);
	range->index = minfo->index;
	range->start_address = address->start_address;
	range->end_address = address->end_address;
	range->dynamic_data = address;
	range->dynamic_size = size;

	if ((minfo->method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (minfo->method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (minfo->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return;

	var_table = (MonoDebugVarInfo *) (ptr + variable_offset);
	type_table = (guint32 *) (ptr + type_offset);

	type_index_table = (guint32 *)
		(symfile->symfile->raw_contents + read32(&(minfo->entry->_type_index_table_offset)));

	if (jit->this_var)
		*var_table++ = *jit->this_var;
	*type_table++ = write_type (mono_debugger_symbol_table, &minfo->method->klass->this_arg);

	for (i = 0; i < jit->num_params; i++) {
		*var_table++ = jit->params [i];
		*type_table++ = write_type (mono_debugger_symbol_table, minfo->method->signature->params [i]);
	}

	if (jit->num_locals < read32(&(minfo->entry->_num_locals))) {
		g_warning (G_STRLOC ": Method %s.%s has %d locals, but symbol file claims it has %d.",
			   minfo->method->klass->name, minfo->method->name, jit->num_locals,
			   read32(&(minfo->entry->_num_locals)));
		var_table += read32(&(minfo->entry->_num_locals));
	} else {
		g_assert ((header != NULL) || (minfo->entry->_num_locals == 0));
		for (i = 0; i < read32(&(minfo->entry->_num_locals)); i++) {
			*var_table++ = jit->locals [i];
			*type_table++ = write_type (mono_debugger_symbol_table, header->locals [i]);
		}
	}

	must_reload_symtabs = TRUE;
}

static MonoDebuggerRangeInfo *
allocate_range_entry (MonoDebuggerSymbolFile *symfile)
{
	MonoDebuggerRangeInfo *retval;
	guint32 size, chunks;

	if (!symfile->range_table) {
		size = sizeof (MonoDebuggerRangeInfo) * RANGE_TABLE_CHUNK_SIZE;
		symfile->range_table = g_malloc0 (size);
		symfile->num_range_entries = 1;
		return symfile->range_table;
	}

	if (!((symfile->num_range_entries + 1) % RANGE_TABLE_CHUNK_SIZE)) {
		chunks = (symfile->num_range_entries + 1) / RANGE_TABLE_CHUNK_SIZE;
		size = sizeof (MonoDebuggerRangeInfo) * RANGE_TABLE_CHUNK_SIZE * (chunks + 1);

		symfile->range_table = g_realloc (symfile->range_table, size);
	}

	retval = symfile->range_table + symfile->num_range_entries;
	symfile->num_range_entries++;
	return retval;
}

static MonoDebuggerClassInfo *
allocate_class_entry (MonoDebuggerSymbolFile *symfile)
{
	MonoDebuggerClassInfo *retval;
	MonoDebuggerClassTable *table;
	guint32 size;

	if (!symfile->class_table_start) {
		table = g_new0 (MonoDebuggerClassTable, 1);
		symfile->class_table_start = symfile->current_class_table = table;

		size = sizeof (MonoDebuggerClassInfo) * CLASS_TABLE_CHUNK_SIZE;
		table->data = g_malloc0 (size);
		table->size = CLASS_TABLE_CHUNK_SIZE;
		table->index = 1;

		return table->data;
	}

	table = symfile->current_class_table;
	if (table->index >= table->size) {
		table = g_new0 (MonoDebuggerClassTable, 1);

		symfile->current_class_table->next = table;
		symfile->current_class_table = table;

		size = sizeof (MonoDebuggerClassInfo) * CLASS_TABLE_CHUNK_SIZE;
		table->data = g_malloc0 (size);
		table->size = CLASS_TABLE_CHUNK_SIZE;
		table->index = 1;

		return table->data;
	}

	retval = table->data + table->index;
	table->index++;
	return retval;
}

/*
 * Allocate a new entry of size `size' in the type table.
 * Returns the global offset which is to be used to reference this type and
 * a pointer (in the `ptr' argument) which is to be used to write the type.
 */
static guint32
allocate_type_entry (MonoDebuggerSymbolTable *table, guint32 size, guint8 **ptr)
{
	guint32 retval;
	guint8 *data;

	g_assert (size + 4 < TYPE_TABLE_CHUNK_SIZE);
	g_assert (ptr != NULL);

	/* Initialize things if necessary. */
	if (!table->current_type_table) {
		table->current_type_table = g_malloc0 (TYPE_TABLE_CHUNK_SIZE);
		table->type_table_size = TYPE_TABLE_CHUNK_SIZE;
		table->type_table_chunk_size = TYPE_TABLE_CHUNK_SIZE;
		table->type_table_offset = MONO_DEBUGGER_TYPE_MAX + 1;
	}

 again:
	/* First let's check whether there's still enough room in the current_type_table. */
	if (table->type_table_offset + size + 4 < table->type_table_size) {
		retval = table->type_table_offset;
		table->type_table_offset += size + 4;
		data = ((guint8 *) table->current_type_table) + retval - table->type_table_start;
		*(gint32 *) data = size;
		data += sizeof(gint32);
		*ptr = data;
		return retval;
	}

	/* Add the current_type_table to the type_tables vector and ... */
	if (!table->type_tables) {
		guint32 tsize = sizeof (gpointer) * TYPE_TABLE_PTR_CHUNK_SIZE;
		table->type_tables = g_malloc0 (tsize);
	}

	if (!((table->num_type_tables + 1) % TYPE_TABLE_PTR_CHUNK_SIZE)) {
		guint32 chunks = (table->num_type_tables + 1) / TYPE_TABLE_PTR_CHUNK_SIZE;
		guint32 tsize = sizeof (gpointer) * TYPE_TABLE_PTR_CHUNK_SIZE * (chunks + 1);

		table->type_tables = g_realloc (table->type_tables, tsize);
	}

	table->type_tables [table->num_type_tables++] = table->current_type_table;

	/* .... allocate a new current_type_table. */
	table->current_type_table = g_malloc0 (TYPE_TABLE_CHUNK_SIZE);
	table->type_table_start = table->type_table_offset = table->type_table_size;
	table->type_table_size += TYPE_TABLE_CHUNK_SIZE;

	goto again;
}

/*
 * Allocate a new entry of size `size' in the misc table.
 * Returns the global offset which is to be used to reference this entry and
 * a pointer (in the `ptr' argument) which is to be used to write the entry.
 */
static guint32
allocate_misc_entry (MonoDebuggerSymbolTable *table, guint32 size, guint8 **ptr)
{
	guint32 retval;
	guint8 *data;

	g_assert (size + 4 < MISC_TABLE_CHUNK_SIZE);
	g_assert (ptr != NULL);

	/* Initialize things if necessary. */
	if (!table->current_misc_table) {
		table->current_misc_table = g_malloc0 (MISC_TABLE_CHUNK_SIZE);
		table->misc_table_size = MISC_TABLE_CHUNK_SIZE;
		table->misc_table_chunk_size = MISC_TABLE_CHUNK_SIZE;
		table->misc_table_offset = 1;
	}

 again:
	/* First let's check whether there's still enough room in the current_misc_table. */
	if (table->misc_table_offset + size + 4 < table->misc_table_size) {
		retval = table->misc_table_offset;
		table->misc_table_offset += size + 4;
		data = ((guint8 *) table->current_misc_table) + retval - table->misc_table_start;
		*(gint32 *) data = size;
		data += sizeof(gint32);
		*ptr = data;
		return retval;
	}

	/* Add the current_misc_table to the misc_tables vector and ... */
	if (!table->misc_tables) {
		guint32 tsize = sizeof (gpointer) * MISC_TABLE_PTR_CHUNK_SIZE;
		table->misc_tables = g_malloc0 (tsize);
	}

	if (!((table->num_misc_tables + 1) % MISC_TABLE_PTR_CHUNK_SIZE)) {
		guint32 chunks = (table->num_misc_tables + 1) / MISC_TABLE_PTR_CHUNK_SIZE;
		guint32 tsize = sizeof (gpointer) * MISC_TABLE_PTR_CHUNK_SIZE * (chunks + 1);

		table->misc_tables = g_realloc (table->misc_tables, tsize);
	}

	table->misc_tables [table->num_misc_tables++] = table->current_misc_table;

	/* .... allocate a new current_misc_table. */
	table->current_misc_table = g_malloc0 (MISC_TABLE_CHUNK_SIZE);
	table->misc_table_start = table->misc_table_offset = table->misc_table_size;
	table->misc_table_size += MISC_TABLE_CHUNK_SIZE;

	goto again;
}

static gboolean
property_is_static (MonoProperty *prop)
{
	MonoMethod *method;

	method = prop->get;
	if (!method)
		method = prop->set;

	return method->flags & METHOD_ATTRIBUTE_STATIC;
}

static gboolean
event_is_static (MonoEvent *ev)
{
	MonoMethod *method;

	method = ev->add;

	return method->flags & METHOD_ATTRIBUTE_STATIC;
}

static guint32
do_write_class (MonoDebuggerSymbolTable *table, MonoClass *klass, MonoDebuggerClassInfo *cinfo)
{
	guint8 buffer [BUFSIZ], *ptr = buffer, *old_ptr;
	GPtrArray *methods = NULL, *static_methods = NULL, *ctors = NULL;
	int num_fields = 0, num_static_fields = 0, num_properties = 0, num_static_properties = 0;
	int num_events = 0, num_static_events = 0;
	int num_methods = 0, num_static_methods = 0, num_params = 0, num_static_params = 0, base_offset = 0;
	int num_ctors = 0, num_ctor_params = 0;
	int field_info_size = 0, static_field_info_size = 0, property_info_size = 0, event_info_size = 0, static_event_info_size = 0;
	int static_property_info_size = 0, method_info_size = 0, static_method_info_size = 0;
	int ctor_info_size = 0, iface_info_size = 0;
	guint32 size, data_size, offset, data_offset;
	GHashTable *method_slots = NULL;
	int i;

	if (klass->init_pending)
		g_warning (G_STRLOC ": %p - %s.%s", klass, klass->name_space, klass->name);
	g_assert (!klass->init_pending);
	mono_class_init (klass);

	offset = GPOINTER_TO_UINT (g_hash_table_lookup (class_table, klass));
	if (offset)
		return offset;

	if (klass->enumtype) {
		offset = allocate_type_entry (table, 13, &ptr);
		if (cinfo)
			cinfo->type_info = offset;
		g_hash_table_insert (class_table, klass, GUINT_TO_POINTER (offset));

		*ptr++ = MONO_DEBUGGER_TYPE_KIND_ENUM;
		WRITE_UINT32 (ptr, klass->instance_size);
		WRITE_UINT32 (ptr, MONO_DEBUGGER_TYPE_ENUM);
		WRITE_UINT32 (ptr, write_type (table, klass->enum_basetype));
		return offset;
	}

	for (i = 0; i < klass->field.count; i++)
		if (!(klass->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC))
			++num_fields;
		else
			++num_static_fields;

	for (i = 0; i < klass->property.count; i++)
		if (!property_is_static (&klass->properties [i]))
			++num_properties;
		else
			++num_static_properties;

	for (i = 0; i < klass->event.count; i++)
		if (!event_is_static (&klass->events [i]))
			++num_events;
		else
			++num_static_events;

	method_slots = g_hash_table_new (NULL, NULL);
	methods = g_ptr_array_new ();
	static_methods = g_ptr_array_new ();
	ctors = g_ptr_array_new ();

	for (i = 0; i < klass->method.count; i++) {
		MonoMethod *method = klass->methods [i];

		if (!strcmp (method->name, ".cctor"))
			continue;
		if (!((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC))
			continue;

		if (!strcmp (method->name, ".ctor")) {
			++num_ctors;
			num_ctor_params += method->signature->param_count;
			g_ptr_array_add (ctors, method);
			continue;
		}

		if (method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME)
			continue;
		
		if (method->slot != -1) {
			if (g_hash_table_lookup (method_slots, GUINT_TO_POINTER (method->slot)))
				continue;
			g_hash_table_insert (method_slots, GUINT_TO_POINTER (method->slot), method);
		}

		if (method->flags & METHOD_ATTRIBUTE_STATIC) {
			++num_static_methods;
			num_static_params += method->signature->param_count;
			g_ptr_array_add (static_methods, method);
		} else {
			++num_methods;
			num_params += method->signature->param_count;
			g_ptr_array_add (methods, method);
		}
	}

	g_hash_table_destroy (method_slots);

	field_info_size = num_fields * 8;
	static_field_info_size = num_static_fields * 8;
	property_info_size = num_properties * (4 + 2 * sizeof (gpointer));
	static_property_info_size = num_static_properties * (4 + 2 * sizeof (gpointer));
	event_info_size = num_events * (4 + 2 * sizeof (gpointer));
	static_event_info_size = num_static_events * (4 + 2 * sizeof (gpointer));
	method_info_size = num_methods * (4 + 2 * sizeof (gpointer)) + num_params * 4;
	static_method_info_size = num_static_methods * (4 + 2 * sizeof (gpointer)) +
		num_static_params * 4;
	ctor_info_size = num_ctors * (4 + 2 * sizeof (gpointer)) + num_ctor_params * 4;
	iface_info_size = klass->interface_count * 4;

	size = 90 + sizeof (gpointer) + field_info_size + static_field_info_size +
		property_info_size + static_property_info_size + event_info_size +
		static_event_info_size + method_info_size + static_method_info_size +
		ctor_info_size + iface_info_size;

	data_size = size;

	offset = allocate_type_entry (table, data_size, &ptr);
	old_ptr = ptr;

	if (cinfo)
		cinfo->type_info = offset;

	g_hash_table_insert (class_table, klass, GUINT_TO_POINTER (offset));

	*ptr++ = MONO_DEBUGGER_TYPE_KIND_CLASS_INFO;

	if (klass->valuetype)
		base_offset = - sizeof (MonoObject);

	WRITE_UINT32 (ptr, klass->instance_size + base_offset);
	*ptr++ = klass->valuetype;
	WRITE_POINTER (ptr, klass);
	data_offset = 0;
	WRITE_UINT32 (ptr, num_fields);
	WRITE_UINT32 (ptr, data_offset);
	data_offset += field_info_size;
	WRITE_UINT32 (ptr, num_properties);
	WRITE_UINT32 (ptr, data_offset);
	data_offset += property_info_size;
	WRITE_UINT32 (ptr, num_events);
	WRITE_UINT32 (ptr, data_offset);
	data_offset += event_info_size;
	WRITE_UINT32 (ptr, num_methods);
	WRITE_UINT32 (ptr, data_offset);
	data_offset += method_info_size;
	WRITE_UINT32 (ptr, num_static_fields);
	WRITE_UINT32 (ptr, data_offset);
	data_offset += static_field_info_size;
	WRITE_UINT32 (ptr, num_static_properties);
	WRITE_UINT32 (ptr, data_offset);
	data_offset += static_property_info_size;
	WRITE_UINT32 (ptr, num_static_events);
	WRITE_UINT32 (ptr, data_offset);
	data_offset += static_event_info_size;
	WRITE_UINT32 (ptr, num_static_methods);
	WRITE_UINT32 (ptr, data_offset);
	data_offset += static_method_info_size;
	WRITE_UINT32 (ptr, num_ctors);
	WRITE_UINT32 (ptr, data_offset);
	data_offset += ctor_info_size;
	WRITE_UINT32 (ptr, klass->interface_count);
	WRITE_UINT32 (ptr, data_offset);
	data_offset += iface_info_size;

	if (klass->parent && (klass->parent != mono_defaults.object_class))
		WRITE_UINT32 (ptr, write_class (table, klass->parent));
	else
		WRITE_UINT32 (ptr, 0);

	for (i = 0; i < klass->field.count; i++) {
		if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		WRITE_UINT32 (ptr, klass->fields [i].offset + base_offset);
		WRITE_UINT32 (ptr, write_type (table, klass->fields [i].type));
	}

	for (i = 0; i < klass->property.count; i++) {
		if (property_is_static (&klass->properties [i]))
			continue;

		if (klass->properties [i].get)
			WRITE_UINT32 (ptr, write_type (table, klass->properties [i].get->signature->ret));
		else
			WRITE_UINT32 (ptr, 0);
		WRITE_POINTER (ptr, klass->properties [i].get);
		WRITE_POINTER (ptr, klass->properties [i].set);
	}

	for (i = 0; i < klass->event.count; i++) {
		if (event_is_static (&klass->events[i]))
			continue;

		if (klass->events [i].add) {
			WRITE_UINT32 (ptr, write_type (table, klass->events [i].add->signature->params[0]));
		}
		else {
			g_warning ("event add method not defined");
			WRITE_UINT32 (ptr, 0);
		}
		WRITE_POINTER (ptr, klass->events [i].add);
		WRITE_POINTER (ptr, klass->events [i].remove);
		/* raise?  other? */
	}

	for (i = 0; i < methods->len; i++) {
		MonoMethod *method = g_ptr_array_index (methods, i);
		int j;

		WRITE_POINTER (ptr, method);
		if ((method->signature->ret) && (method->signature->ret->type != MONO_TYPE_VOID))
			WRITE_UINT32 (ptr, write_type (table, method->signature->ret));
		else
			WRITE_UINT32 (ptr, 0);
		WRITE_UINT32 (ptr, method->signature->param_count);
		for (j = 0; j < method->signature->param_count; j++)
			WRITE_UINT32 (ptr, write_type (table, method->signature->params [j]));
	}

	g_ptr_array_free (methods, FALSE);

	for (i = 0; i < klass->field.count; i++) {
		if (!(klass->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC))
			continue;

		WRITE_UINT32 (ptr, klass->fields [i].offset);
		WRITE_UINT32 (ptr, write_type (table, klass->fields [i].type));
	}

	for (i = 0; i < klass->property.count; i++) {
		if (!property_is_static (&klass->properties [i]))
			continue;

		if (klass->properties [i].get)
			WRITE_UINT32 (ptr, write_type (table, klass->properties [i].get->signature->ret));
		else
			WRITE_UINT32 (ptr, 0);
		WRITE_POINTER (ptr, klass->properties [i].get);
		WRITE_POINTER (ptr, klass->properties [i].set);
	}

	for (i = 0; i < klass->event.count; i++) {
		if (!event_is_static (&klass->events[i]))
			continue;

		if (klass->events [i].add) {
			WRITE_UINT32 (ptr, write_type (table, klass->events [i].add->signature->params[0]));
		}
		else {
			g_warning ("event add method not defined");
			WRITE_UINT32 (ptr, 0);
		}
		WRITE_POINTER (ptr, klass->events [i].add);
		WRITE_POINTER (ptr, klass->events [i].remove);
		/* raise?  other? */
	}

	for (i = 0; i < static_methods->len; i++) {
		MonoMethod *method = g_ptr_array_index (static_methods, i);
		int j;

		WRITE_POINTER (ptr, method);
		if ((method->signature->ret) && (method->signature->ret->type != MONO_TYPE_VOID))
			WRITE_UINT32 (ptr, write_type (table, method->signature->ret));
		else
			WRITE_UINT32 (ptr, 0);
		WRITE_UINT32 (ptr, method->signature->param_count);
		for (j = 0; j < method->signature->param_count; j++)
			WRITE_UINT32 (ptr, write_type (table, method->signature->params [j]));
	}

	g_ptr_array_free (static_methods, FALSE);

	for (i = 0; i < ctors->len; i++) {
		MonoMethod *ctor = g_ptr_array_index (ctors, i);
		int j;

		WRITE_POINTER (ptr, ctor);
		WRITE_UINT32 (ptr, 0);
		WRITE_UINT32 (ptr, ctor->signature->param_count);
		for (j = 0; j < ctor->signature->param_count; j++)
			WRITE_UINT32 (ptr, write_type (table, ctor->signature->params [j]));
	}

	g_ptr_array_free (ctors, FALSE);

	for (i = 0; i < klass->interface_count; i++)
		WRITE_UINT32 (ptr, write_class (table, klass->interfaces [i]));

	if (ptr - old_ptr != data_size) {
		g_warning (G_STRLOC ": %d,%d,%d", ptr - old_ptr, data_size, sizeof (gpointer));
		if (klass)
			g_warning (G_STRLOC ": %s.%s", klass->name_space, klass->name);
		g_assert_not_reached ();
	}

	return offset;
}

/*
 * Adds type `type' to the type table and returns its offset.
 */
static guint32
write_type (MonoDebuggerSymbolTable *table, MonoType *type)
{
	guint8 buffer [BUFSIZ], *ptr = buffer;
	guint32 size, offset;
	MonoClass *klass;

	offset = GPOINTER_TO_UINT (g_hash_table_lookup (type_table, type));
	if (offset)
		return offset;

	klass = mono_class_from_mono_type (type);
	if (type->type == MONO_TYPE_CLASS)
		return write_class (table, klass);

	// mono_class_init (klass);

	switch (type->type) {
	case MONO_TYPE_VOID:
		return MONO_DEBUGGER_TYPE_VOID;

	case MONO_TYPE_BOOLEAN:
		return MONO_DEBUGGER_TYPE_BOOLEAN;

	case MONO_TYPE_I1:
		return MONO_DEBUGGER_TYPE_I1;

	case MONO_TYPE_U1:
		return MONO_DEBUGGER_TYPE_U1;

	case MONO_TYPE_CHAR:
		return MONO_DEBUGGER_TYPE_CHAR;

	case MONO_TYPE_I2:
		return MONO_DEBUGGER_TYPE_I2;

	case MONO_TYPE_U2:
		return MONO_DEBUGGER_TYPE_U2;

	case MONO_TYPE_I4:
		return MONO_DEBUGGER_TYPE_I4;

	case MONO_TYPE_U4:
		return MONO_DEBUGGER_TYPE_U4;

	case MONO_TYPE_I8:
		return MONO_DEBUGGER_TYPE_I8;

	case MONO_TYPE_U8:
		return MONO_DEBUGGER_TYPE_U8;

	case MONO_TYPE_R4:
		return MONO_DEBUGGER_TYPE_R4;

	case MONO_TYPE_R8:
		return MONO_DEBUGGER_TYPE_R8;

	case MONO_TYPE_STRING:
		return MONO_DEBUGGER_TYPE_STRING;

	case MONO_TYPE_I:
		return MONO_DEBUGGER_TYPE_I;

	case MONO_TYPE_U:
		return MONO_DEBUGGER_TYPE_U;

	case MONO_TYPE_SZARRAY: {
		MonoArray array;

		*ptr++ = MONO_DEBUGGER_TYPE_KIND_SZARRAY;
		WRITE_UINT32 (ptr, sizeof (MonoArray));
		WRITE_UINT32 (ptr, MONO_DEBUGGER_TYPE_ARRAY);
		*ptr++ = (guint8*)&array.max_length - (guint8*)&array;
		*ptr++ = sizeof (array.max_length);
		*ptr++ = (guint8*)&array.vector - (guint8*)&array;
		WRITE_UINT32 (ptr, write_type (table, &type->data.klass->byval_arg));
		break;
	}

	case MONO_TYPE_ARRAY: {
		MonoArray array;
		MonoArrayBounds bounds;

		*ptr++ = MONO_DEBUGGER_TYPE_KIND_ARRAY;
		WRITE_UINT32 (ptr, sizeof (MonoArray));
		WRITE_UINT32 (ptr, MONO_DEBUGGER_TYPE_ARRAY);
		*ptr++ = (guint8*)&array.max_length - (guint8*)&array;
		*ptr++ = sizeof (array.max_length);
		*ptr++ = (guint8*)&array.vector - (guint8*)&array;
		*ptr++ = klass->rank;
		*ptr++ = (guint8*)&array.bounds - (guint8*)&array;
		*ptr++ = sizeof (MonoArrayBounds);
		*ptr++ = (guint8*)&bounds.lower_bound - (guint8*)&bounds;
		*ptr++ = sizeof (bounds.lower_bound);
		*ptr++ = (guint8*)&bounds.length - (guint8*)&bounds;
		*ptr++ = sizeof (bounds.length);
		WRITE_UINT32 (ptr, write_type (table, &type->data.array->eklass->byval_arg));
		break;
	}

	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_GENERICINST:
	case MONO_TYPE_OBJECT:
		return write_class (table, klass);

	case MONO_TYPE_PTR:
		*ptr++ = MONO_DEBUGGER_TYPE_KIND_POINTER;
		WRITE_UINT32 (ptr, sizeof (gpointer));
		WRITE_UINT32 (ptr, write_type (table, type->data.type));
		break;

	default:
		/* g_message (G_STRLOC ": %s.%s - %p - %d", klass->name_space, klass->name, klass, type->type); */
		*ptr++ = MONO_DEBUGGER_TYPE_KIND_UNKNOWN;
		WRITE_UINT32 (ptr, klass->instance_size);
		WRITE_UINT32 (ptr, write_class (table, klass));
		break;
	}

	size = ptr - buffer;
	offset = allocate_type_entry (mono_debugger_symbol_table, size, &ptr);
	memcpy (ptr, buffer, size);

	return offset;
}

MonoReflectionMethod *
ves_icall_MonoDebugger_GetMethod (MonoReflectionAssembly *assembly, guint32 token)
{
	MonoMethod *method;

	method = mono_get_method (mono_assembly_get_image (assembly->assembly), token, NULL);

	return mono_method_get_object (mono_domain_get (), method, NULL);
}

int
ves_icall_MonoDebugger_GetMethodToken (MonoReflectionAssembly *assembly, MonoReflectionMethod *method)
{
	return method->method->token;
}

MonoReflectionType *
ves_icall_MonoDebugger_GetType (MonoReflectionAssembly *assembly, guint32 token)
{
	MonoClass *klass;

	klass = mono_class_get (mono_assembly_get_image (assembly->assembly), token);
	if (!klass) {
		g_warning (G_STRLOC ": %x", token);
		return NULL;
	}

	return mono_type_get_object (mono_domain_get (), &klass->byval_arg);
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
	image = mono_assembly_get_image (assembly->assembly);

	ptr = mono_array_addr (signature, char, 0);
	g_assert (*ptr++ == 0x07);
	len = mono_metadata_decode_value (ptr, &ptr);
	g_assert (len == 1);

	type = mono_metadata_parse_type (image, MONO_PARSE_LOCAL, 0, ptr, &ptr);

	return mono_type_get_object (domain, type);
}

void
mono_debugger_event (MonoDebuggerEvent event, gpointer data, guint32 arg)
{
	if (mono_debugger_event_handler)
		(* mono_debugger_event_handler) (event, data, arg);
}

void
mono_debugger_cleanup (void)
{
	/* Do nothing yet. */
}

/*
 * Debugger breakpoint interface.
 *
 * This interface is used to insert breakpoints on methods which are not yet JITed.
 * The debugging code keeps a list of all such breakpoints and automatically inserts the
 * breakpoint when the method is JITed.
 */

static GPtrArray *breakpoints = NULL;

int
mono_debugger_insert_breakpoint_full (MonoMethodDesc *desc)
{
	static int last_breakpoint_id = 0;
	MonoDebuggerBreakpointInfo *info;

	info = g_new0 (MonoDebuggerBreakpointInfo, 1);
	info->desc = desc;
	info->index = ++last_breakpoint_id;

	if (!breakpoints)
		breakpoints = g_ptr_array_new ();

	g_ptr_array_add (breakpoints, info);

	return info->index;
}

int
mono_debugger_remove_breakpoint (int breakpoint_id)
{
	int i;

	if (!breakpoints)
		return 0;

	for (i = 0; i < breakpoints->len; i++) {
		MonoDebuggerBreakpointInfo *info = g_ptr_array_index (breakpoints, i);

		if (info->index != breakpoint_id)
			continue;

		mono_method_desc_free (info->desc);
		g_ptr_array_remove (breakpoints, info);
		g_free (info);
		return 1;
	}

	return 0;
}

int
mono_debugger_insert_breakpoint (const gchar *method_name, gboolean include_namespace)
{
	MonoMethodDesc *desc;

	desc = mono_method_desc_new (method_name, include_namespace);
	if (!desc)
		return 0;

	return mono_debugger_insert_breakpoint_full (desc);
}

int
mono_debugger_method_has_breakpoint (MonoMethod *method)
{
	int i;

	if (!breakpoints || (method->wrapper_type != MONO_WRAPPER_NONE))
		return 0;

	for (i = 0; i < breakpoints->len; i++) {
		MonoDebuggerBreakpointInfo *info = g_ptr_array_index (breakpoints, i);

		if (!mono_method_desc_full_match (info->desc, method))
			continue;

		return info->index;
	}

	return 0;
}

void
mono_debugger_breakpoint_callback (MonoMethod *method, guint32 index)
{
	mono_debugger_event (MONO_DEBUGGER_EVENT_BREAKPOINT, method, index);
}

gboolean
mono_debugger_unhandled_exception (gpointer addr, gpointer stack, MonoObject *exc)
{
	if (!mono_debugger_initialized)
		return FALSE;

	// Prevent the object from being finalized.
	last_exception = exc;
	mono_debugger_event (MONO_DEBUGGER_EVENT_UNHANDLED_EXCEPTION, exc, addr);
	return TRUE;
}

void
mono_debugger_handle_exception (gpointer addr, gpointer stack, MonoObject *exc)
{
	if (!mono_debugger_initialized)
		return;

	// Prevent the object from being finalized.
	last_exception = exc;
	mono_debugger_event (MONO_DEBUGGER_EVENT_EXCEPTION, stack, addr);
}

static gchar *
get_exception_message (MonoObject *exc)
{
	char *message = NULL;
	MonoString *str; 
	MonoMethod *method;
	MonoClass *klass;
	gint i;

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		klass = exc->vtable->klass;
		method = NULL;
		while (klass && method == NULL) {
			for (i = 0; i < klass->method.count; ++i) {
				method = klass->methods [i];
				if (!strcmp ("ToString", method->name) &&
				    method->signature->param_count == 0 &&
				    method->flags & METHOD_ATTRIBUTE_VIRTUAL &&
				    method->flags & METHOD_ATTRIBUTE_PUBLIC) {
					break;
				}
				method = NULL;
			}
			
			if (method == NULL)
				klass = klass->parent;
		}

		g_assert (method);

		str = (MonoString *) mono_runtime_invoke (method, exc, NULL, NULL);
		if (str)
			message = mono_string_to_utf8 (str);
	}

	return message;
}

MonoObject *
mono_debugger_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc)
{
	MonoObject *retval;
	gchar *message;

	if (method->klass->valuetype && (obj != NULL))
		obj = mono_value_box (mono_domain_get (), method->klass, obj);

	if (!strcmp (method->name, ".ctor")) {
		retval = obj = mono_object_new (mono_domain_get (), method->klass);

		mono_runtime_invoke (method, obj, params, exc);
	} else
		retval = mono_runtime_invoke (method, obj, params, exc);

	if (!exc || (*exc == NULL))
		return retval;

	message = get_exception_message (*exc);
	if (message) {
		*exc = (MonoObject *) mono_string_new_wrapper (message);
		g_free (message);
	}

	return retval;
}

guint32
mono_debugger_lookup_type (const gchar *type_name)
{
	int i;

	mono_debugger_lock ();

	for (i = 0; i < mono_debugger_symbol_table->num_symbol_files; i++) {
		MonoDebuggerSymbolFile *symfile = mono_debugger_symbol_table->symbol_files [i];
		MonoType *type;
		guint32 offset;
		gchar *name;

		name = g_strdup (type_name);
		type = mono_reflection_type_from_name (name, symfile->image);
		g_free (name);
		if (!type)
			continue;

		offset = write_type (mono_debugger_symbol_table, type);

		mono_debugger_unlock ();
		return offset;
	}

	mono_debugger_unlock ();
	return 0;
}

gint32
mono_debugger_lookup_assembly (const gchar *name)
{
	MonoAssembly *assembly;
	MonoImageOpenStatus status;
	int i;

	mono_debugger_lock ();

 again:
	for (i = 0; i < mono_debugger_symbol_table->num_symbol_files; i++) {
		MonoDebuggerSymbolFile *symfile = mono_debugger_symbol_table->symbol_files [i];

		if (!strcmp (symfile->image_file, name)) {
			mono_debugger_unlock ();
			return i;
		}
	}

	assembly = mono_assembly_open (name, &status);

	if (status != MONO_IMAGE_OK) {
		g_warning (G_STRLOC ": Cannot open image `%s'", name);
		mono_debugger_unlock ();
		return -1;
	}

	must_reload_symtabs = TRUE;
	goto again;
}

void
mono_debugger_add_wrapper (MonoMethod *wrapper, MonoDebugMethodJitInfo *jit, gpointer addr)
{
	guint32 size, offset;
	guint8 *ptr;

	if (!mono_debugger_symbol_table)
		return;

	size = strlen (wrapper->name) + 5 + 5 * sizeof (gpointer);

	offset = allocate_misc_entry (mono_debugger_symbol_table, size, &ptr);

	WRITE_UINT32 (ptr, MONO_DEBUGGER_MISC_ENTRY_TYPE_WRAPPER);
	WRITE_STRING (ptr, wrapper->name);
	WRITE_POINTER (ptr, jit->code_start);
	WRITE_POINTER (ptr, jit->code_start + jit->code_size);
	WRITE_POINTER (ptr, addr);
	WRITE_POINTER (ptr, jit->code_start + jit->prologue_end);
	WRITE_POINTER (ptr, jit->code_start + jit->epilogue_begin);
}
