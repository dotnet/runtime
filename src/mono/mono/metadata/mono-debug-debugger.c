#include <config.h>
#include <stdlib.h>
#include <string.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>

#define SYMFILE_TABLE_CHUNK_SIZE	16
#define RANGE_TABLE_CHUNK_SIZE		256
#define CLASS_TABLE_CHUNK_SIZE		256
#define TYPE_TABLE_PTR_CHUNK_SIZE	256
#define TYPE_TABLE_CHUNK_SIZE		65536

static CRITICAL_SECTION debugger_lock_mutex;
static gboolean mono_debugger_initialized = FALSE;

static GHashTable *type_table = NULL;
static GHashTable *class_table = NULL;

static MonoDebuggerRangeInfo *allocate_range_entry (MonoDebuggerSymbolFile *symfile);
static MonoDebuggerClassInfo *allocate_class_entry (MonoDebuggerSymbolFile *symfile);
static guint32 allocate_type_entry (MonoDebuggerSymbolTable *table, guint32 size, guint8 **ptr);
static guint32 write_type (MonoDebuggerSymbolTable *table, MonoType *type);

MonoDebuggerSymbolTable *mono_debugger_symbol_table = NULL;
void (*mono_debugger_event_handler) (MonoDebuggerEvent event, gpointer data, guint32 arg) = NULL;

#ifndef PLATFORM_WIN32

MonoDebuggerIOLayer mono_debugger_io_layer = {
	InitializeCriticalSection, DeleteCriticalSection, TryEnterCriticalSection,
	EnterCriticalSection, LeaveCriticalSection, WaitForSingleObject, SignalObjectAndWait,
	WaitForMultipleObjects, CreateSemaphore, ReleaseSemaphore, CreateThread
};

#endif

void
mono_debugger_lock (void)
{
	if (mono_debugger_initialized)
		EnterCriticalSection (&debugger_lock_mutex);
}

void
mono_debugger_unlock (void)
{
	if (mono_debugger_initialized)
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
	table->symbol_files [table->num_symbol_files++] = symfile;
	return symfile;
}

void
mono_debugger_initialize (void)
{
	MonoDebuggerSymbolTable *symbol_table;

	g_assert (!mono_debugger_initialized);

	InitializeCriticalSection (&debugger_lock_mutex);

	mono_debugger_lock ();

	symbol_table = g_new0 (MonoDebuggerSymbolTable, 1);
	symbol_table->magic = MONO_DEBUGGER_MAGIC;
	symbol_table->version = MONO_DEBUGGER_VERSION;
	symbol_table->total_size = sizeof (MonoDebuggerSymbolTable);

	mono_debugger_symbol_table = symbol_table;
	mono_debugger_initialized = TRUE;

	mono_debugger_unlock ();
}

MonoDebuggerSymbolFile *
mono_debugger_add_symbol_file (MonoDebugHandle *handle)
{
	MonoDebuggerSymbolFile *info;

	g_assert (mono_debugger_initialized);

	info = allocate_symbol_file_entry (mono_debugger_symbol_table);
	info->symfile = handle->symfile;
	info->image = handle->image;
	info->image_file = handle->image_file;

	mono_debugger_add_type (info, mono_defaults.object_class);

	return info;
}

void
mono_debugger_add_type (MonoDebuggerSymbolFile *symfile, MonoClass *klass)
{
	MonoDebuggerClassInfo *info;

	mono_debugger_lock ();

	if (!class_table)
		class_table = g_hash_table_new (g_direct_hash, g_direct_equal);

	/* We write typeof (object) into each symbol file's type table. */
	if ((klass != mono_defaults.object_class) && g_hash_table_lookup (class_table, klass)) {
		mono_debugger_unlock ();
		return;
	}

	symfile->generation++;

	info = allocate_class_entry (symfile);
	info->klass = klass;
	if (klass->rank) {
		info->token = klass->element_class->type_token;
		info->rank = klass->rank;
	} else
		info->token = klass->type_token;
	info->type_info = write_type (mono_debugger_symbol_table, &klass->this_arg);

	mono_debugger_event (MONO_DEBUGGER_EVENT_TYPE_ADDED, NULL, 0);
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

	num_variables = minfo->entry->num_parameters + minfo->entry->num_locals;
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

	block_size = minfo->entry->num_lexical_blocks * sizeof (MonoSymbolFileLexicalBlockEntry);
	block_offset = size;
	size += block_size;

	address = g_malloc0 (size);
	ptr = (guint8 *) address;

	block = (MonoSymbolFileLexicalBlockEntry *)
		(symfile->symfile->raw_contents + minfo->entry->lexical_block_table_offset);
	block_table = (MonoDebugLexicalBlockEntry *) (ptr + block_offset);

	for (i = 0; i < minfo->entry->num_lexical_blocks; i++, block++) {
		block_table [i].start_address = _mono_debug_address_from_il_offset (jit, block->start_offset);
		block_table [i].end_address = _mono_debug_address_from_il_offset (jit, block->end_offset);
	}

	address->size = size;
	address->has_this = has_this;
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
		(symfile->symfile->raw_contents + minfo->entry->type_index_table_offset);

	if (jit->this_var)
		*var_table++ = *jit->this_var;
	*type_table++ = write_type (mono_debugger_symbol_table, &minfo->method->klass->this_arg);

	if (jit->num_params != minfo->entry->num_parameters) {
		g_warning (G_STRLOC ": Method %s.%s has %d parameters, but symbol file claims it has %d.",
			   minfo->method->klass->name, minfo->method->name, jit->num_params,
			   minfo->entry->num_parameters);
		var_table += minfo->entry->num_parameters;
	} else {
		for (i = 0; i < jit->num_params; i++) {
			*var_table++ = jit->params [i];
			*type_table++ = write_type (mono_debugger_symbol_table, minfo->method->signature->params [i]);
		}
	}

	if (jit->num_locals < minfo->entry->num_locals) {
		g_warning (G_STRLOC ": Method %s.%s has %d locals, but symbol file claims it has %d.",
			   minfo->method->klass->name, minfo->method->name, jit->num_locals,
			   minfo->entry->num_locals);
		var_table += minfo->entry->num_locals;
	} else {
		g_assert ((header != NULL) || (minfo->entry->num_locals == 0));
		for (i = 0; i < minfo->entry->num_locals; i++) {
			*var_table++ = jit->locals [i];
			*type_table++ = write_type (mono_debugger_symbol_table, header->locals [i]);
		}
	}

	mono_debugger_event (MONO_DEBUGGER_EVENT_METHOD_ADDED, NULL, 0);
}

static MonoDebuggerRangeInfo *
allocate_range_entry (MonoDebuggerSymbolFile *symfile)
{
	MonoDebuggerRangeInfo *retval;
	guint32 size, chunks;

	symfile->range_entry_size = sizeof (MonoDebuggerRangeInfo);

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
	guint32 size, chunks;

	symfile->class_entry_size = sizeof (MonoDebuggerClassInfo);

	if (!symfile->class_table) {
		size = sizeof (MonoDebuggerClassInfo) * CLASS_TABLE_CHUNK_SIZE;
		symfile->class_table = g_malloc0 (size);
		symfile->num_class_entries = 1;
		return symfile->class_table;
	}

	if (!((symfile->num_class_entries + 1) % CLASS_TABLE_CHUNK_SIZE)) {
		chunks = (symfile->num_class_entries + 1) / CLASS_TABLE_CHUNK_SIZE;
		size = sizeof (MonoDebuggerClassInfo) * CLASS_TABLE_CHUNK_SIZE * (chunks + 1);

		symfile->class_table = g_realloc (symfile->class_table, size);
	}

	retval = symfile->class_table + symfile->num_class_entries;
	symfile->num_class_entries++;
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
		table->type_table_offset = 1;
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

static guint32
write_simple_type (MonoDebuggerSymbolTable *table, MonoType *type)
{
	guint8 buffer [BUFSIZ], *ptr = buffer;
	guint32 size, offset;

	if (!type_table)
		type_table = g_hash_table_new (g_direct_hash, g_direct_equal);

	offset = GPOINTER_TO_UINT (g_hash_table_lookup (type_table, type));
	if (offset)
		return offset;

	switch (type->type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		*((gint32 *) ptr) = 1;
		ptr += sizeof(gint32);
		break;

	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		*((gint32 *) ptr) = 2;
		ptr += sizeof(gint32);
		break;

	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
		*((gint32 *) ptr) = 4;
		ptr += sizeof(gint32);
		break;

	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		*((gint32 *) ptr) = 8;
		ptr += sizeof(gint32);
		break;

	case MONO_TYPE_I:
	case MONO_TYPE_U:
		*((gint32 *) ptr) = sizeof (void *);
		ptr += sizeof(gint32);
		break;

	case MONO_TYPE_VOID:
		*((gint32 *) ptr) = 0;
		ptr += sizeof(gint32);
		break;

	case MONO_TYPE_STRING: {
		MonoString string;

		*((gint32 *) ptr) = -8;
		ptr += sizeof(gint32);
		*((guint32 *) ptr) = sizeof (MonoString);
		ptr += sizeof(guint32);
		*ptr++ = 1;
		*ptr++ = (guint8*)&string.length - (guint8*)&string;
		*ptr++ = sizeof (string.length);
		*ptr++ = (guint8*)&string.chars - (guint8*)&string;
		break;
	}

	default:
		return 0;
	}

	size = ptr - buffer;
	offset = allocate_type_entry (table, size, &ptr);
	memcpy (ptr, buffer, size);

	g_hash_table_insert (type_table, type, GUINT_TO_POINTER (offset));

	return offset;
}

/*
 * Adds type `type' to the type table and returns its offset.
 */
static guint32
write_type (MonoDebuggerSymbolTable *table, MonoType *type)
{
	guint8 buffer [BUFSIZ], *ptr = buffer, *old_ptr;
	GPtrArray *methods = NULL;
	int num_fields = 0, num_properties = 0, num_methods = 0;
	int num_params = 0, kind;
	guint32 size, data_size, offset;
	MonoClass *klass = NULL;

	if (!type_table)
		type_table = g_hash_table_new (g_direct_hash, g_direct_equal);
	if (!class_table)
		class_table = g_hash_table_new (g_direct_hash, g_direct_equal);

	offset = GPOINTER_TO_UINT (g_hash_table_lookup (type_table, type));
	if (offset)
		return offset;

	offset = write_simple_type (table, type);
	if (offset)
		return offset;

	kind = type->type;
	if (kind == MONO_TYPE_OBJECT) {
		klass = mono_defaults.object_class;
		kind = MONO_TYPE_CLASS;
	} else if ((kind == MONO_TYPE_VALUETYPE) || (kind == MONO_TYPE_CLASS)) {
		klass = type->data.klass;
		offset = GPOINTER_TO_UINT (g_hash_table_lookup (class_table, klass));
		if (offset)
			return offset;
	}

	switch (kind) {
	case MONO_TYPE_SZARRAY:
		size = 16;
		break;

	case MONO_TYPE_ARRAY:
		size = 23;
		break;

	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS: {
		GHashTable *method_slots = NULL;
		int i;

		if (klass->init_pending) {
			size = 4;
			break;
		}

		mono_class_init (klass);

		offset = GPOINTER_TO_UINT (g_hash_table_lookup (class_table, klass));
		if (offset)
			return offset;

		if (klass->enumtype) {
			size = 13;
			break;
		}

		for (i = 0; i < klass->field.count; i++)
			if (!(klass->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC))
				++num_fields;

		for (i = 0; i < klass->property.count; i++)
			if (!(klass->properties [i].attrs & FIELD_ATTRIBUTE_STATIC))
				++num_properties;

		method_slots = g_hash_table_new (NULL, NULL);
		methods = g_ptr_array_new ();

		for (i = klass->method.count - 1; i >= 0; i--) {
			MonoMethod *method = klass->methods [i];

			if (strcmp (method->name, ".ctor") == 0 || strcmp (method->name, ".cctor") == 0)
				continue;
			if (method->flags & (METHOD_ATTRIBUTE_STATIC | METHOD_ATTRIBUTE_SPECIAL_NAME))
				continue;
			if (!((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC))
				continue;
			if (g_hash_table_lookup (method_slots, GUINT_TO_POINTER (method->slot)))
				continue;
			g_hash_table_insert (method_slots, GUINT_TO_POINTER (method->slot), method);

			++num_methods;
			num_params += method->signature->param_count;

			g_ptr_array_add (methods, method);
		}

		g_hash_table_destroy (method_slots);

		size = 34 + num_fields * 8 + num_properties * (4 + 2 * sizeof (gpointer)) +
			num_methods * (8 + sizeof (gpointer)) + num_params * 4;

		if (kind == MONO_TYPE_CLASS)
			size += 4;

		break;
	}

	default:
		size = sizeof (int);
		break;
	}

	data_size = size;

	offset = allocate_type_entry (table, data_size, &ptr);
	old_ptr = ptr;

	g_hash_table_insert (type_table, type, GUINT_TO_POINTER (offset));

	switch (kind) {
	case MONO_TYPE_SZARRAY: {
		MonoArray array;

		*((gint32 *) ptr) = -size;
		ptr += sizeof(gint32);
		*((guint32 *) ptr) = sizeof (MonoArray);
		ptr += sizeof(guint32);
		*ptr++ = 2;
		*ptr++ = (guint8*)&array.max_length - (guint8*)&array;
		*ptr++ = sizeof (array.max_length);
		*ptr++ = (guint8*)&array.vector - (guint8*)&array;
		*((guint32 *) ptr) = write_type (table, type->data.type);
		ptr += sizeof(guint32);
		break;
	}

	case MONO_TYPE_ARRAY: {
		MonoArray array;
		MonoArrayBounds bounds;

		*((gint32 *) ptr) = -size;
		ptr += sizeof(gint32);
		*((guint32 *) ptr) = sizeof (MonoArray);
		ptr += sizeof(guint32);
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
		*((guint32 *) ptr) = write_type (table, type->data.array->type);
		ptr += sizeof(guint32);
		break;
	}

	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS: {
		int base_offset = kind == MONO_TYPE_CLASS ? 0 : - sizeof (MonoObject);
		int i, j;

		if (klass->init_pending) {
			*((gint32 *) ptr) = -1;
			ptr += sizeof(gint32);
			break;
		}

		g_hash_table_insert (class_table, klass, GUINT_TO_POINTER (offset));

		if (klass->enumtype) {
			*((gint32 *) ptr) = -size;
			ptr += sizeof(gint32);
			*((guint32 *) ptr) = sizeof (MonoObject);
			ptr += sizeof(guint32);
			*ptr++ = 4;
			*((guint32 *) ptr) = write_type (table, klass->enum_basetype);
			ptr += sizeof(guint32);
			break;
		}

		*((gint32 *) ptr) = -size;
		ptr += sizeof(guint32);

		*((guint32 *) ptr) = klass->instance_size + base_offset;
		ptr += sizeof(guint32);
		if (type->type == MONO_TYPE_OBJECT)
			*ptr++ = 7;
		else
			*ptr++ = kind == MONO_TYPE_CLASS ? 6 : 5;
		*ptr++ = kind == MONO_TYPE_CLASS;
		*((guint32 *) ptr) = num_fields;
		ptr += sizeof(guint32);
		*((guint32 *) ptr) = num_fields * (4 + sizeof (gpointer));
		ptr += sizeof(guint32);
		*((guint32 *) ptr) = num_properties;
		ptr += sizeof(guint32);
		*((guint32 *) ptr) = num_properties * 3 * sizeof (gpointer);
		ptr += sizeof(guint32);
		*((guint32 *) ptr) = num_methods;
		ptr += sizeof(guint32);
		*((guint32 *) ptr) = num_methods * (4 + 2 * sizeof (gpointer)) +
			num_params * sizeof (gpointer);
		ptr += sizeof(guint32);
		for (i = 0; i < klass->field.count; i++) {
			if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;

			*((guint32 *) ptr) = klass->fields [i].offset + base_offset;
			ptr += sizeof(guint32);
			*((guint32 *) ptr) = write_type (table, klass->fields [i].type);
			ptr += sizeof(guint32);
		}

		for (i = 0; i < klass->property.count; i++) {
			if (klass->properties [i].attrs & FIELD_ATTRIBUTE_STATIC)
				continue;

			if (klass->properties [i].get)
				*((guint32 *) ptr) = write_type
					(table, klass->properties [i].get->signature->ret);
			else
				*((guint32 *) ptr) = 0;
			ptr += sizeof(guint32);
			*((gpointer *) ptr) = klass->properties [i].get;
			ptr += sizeof(gpointer);
			*((gpointer *) ptr) = klass->properties [i].set;
			ptr += sizeof(gpointer);
		}

		for (i = 0; i < methods->len; i++) {
			MonoMethod *method = g_ptr_array_index (methods, i);

			*((gpointer *) ptr) = method;
			ptr += sizeof(gpointer);
			if ((method->signature->ret) && (method->signature->ret->type != MONO_TYPE_VOID))
				*((guint32 *) ptr) = write_type (table, method->signature->ret);
			else
				*((guint32 *) ptr) = 0;
			ptr += sizeof(guint32);
			*((guint32 *) ptr) = method->signature->param_count;
			ptr += sizeof(guint32);
			for (j = 0; j < method->signature->param_count; j++)
			{
				*((guint32 *) ptr) = write_type (table, method->signature->params [j]);
				ptr += sizeof(guint32);
			}
		}

		g_ptr_array_free (methods, FALSE);

		if (kind == MONO_TYPE_CLASS) {
			if (klass->parent)
				*((guint32 *) ptr) = write_type (table, &klass->parent->this_arg);
			else
				*((guint32 *) ptr) = 0;
			ptr += sizeof(guint32);		
		}

		break;
	}

	default:
		g_message (G_STRLOC ": %p - %x,%x,%x", type, type->attrs, kind, type->byref);

		*((gint32 *) ptr) = -1;
		ptr += sizeof(gint32);
		break;
	}

	if (ptr - old_ptr != data_size) {
		g_warning (G_STRLOC ": %d,%d - %d", ptr - old_ptr, data_size, kind);
		if (klass)
			g_warning (G_STRLOC ": %s.%s", klass->name_space, klass->name);
		g_assert_not_reached ();
	}

	return offset;
}


MonoReflectionMethod *
ves_icall_MonoDebugger_GetMethod (MonoReflectionAssembly *assembly, guint32 token)
{
	MonoMethod *method;

	method = mono_get_method (assembly->assembly->image, token, NULL);

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

	klass = mono_class_get (assembly->assembly->image, token);
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
	image = assembly->assembly->image;

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
