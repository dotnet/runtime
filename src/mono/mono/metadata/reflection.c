
/*
 * reflection.c: Routines for creating an image at runtime.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001 Ximian, Inc.  http://www.ximian.com
 *
 */
#include <config.h>
#include "mono/metadata/reflection.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/tokentype.h"
#include <stdio.h>
#include <glib.h>
#include <errno.h>
#include <time.h>
#include <string.h>
#include "image.h"
#include "cil-coff.h"
#include "rawbuffer.h"
#include "mono-endian.h"
#include "private.h"

#define TEXT_OFFSET 512
#define CLI_H_SIZE 136
#define FILE_ALIGN 512

typedef struct {
	MonoReflectionILGen *ilgen;
	MonoReflectionType *rtype;
	MonoArray *parameters;
	guint32 attrs;
	guint32 iattrs;
	guint32 call_conv;
	guint32 *table_idx; /* note: it's a pointer */
	MonoArray *code;
	MonoObject *type;
	MonoString *name;
} ReflectionMethodBuilder;

const unsigned char table_sizes [64] = {
	MONO_MODULE_SIZE,
	MONO_TYPEREF_SIZE,
	MONO_TYPEDEF_SIZE,
	0,
	MONO_FIELD_SIZE,
	0,
	MONO_METHOD_SIZE,
	0,
	MONO_PARAM_SIZE,
	MONO_INTERFACEIMPL_SIZE,
	MONO_MEMBERREF_SIZE,	/* 0x0A */
	MONO_CONSTANT_SIZE,
	MONO_CUSTOM_ATTR_SIZE,
	MONO_FIELD_MARSHAL_SIZE,
	MONO_DECL_SECURITY_SIZE,
	MONO_CLASS_LAYOUT_SIZE,
	MONO_FIELD_LAYOUT_SIZE,	/* 0x10 */
	MONO_STAND_ALONE_SIGNATURE_SIZE,
	MONO_EVENT_MAP_SIZE,
	0,
	MONO_EVENT_SIZE,
	MONO_PROPERTY_MAP_SIZE,
	0,
	MONO_PROPERTY_SIZE,
	MONO_METHOD_SEMA_SIZE,
	MONO_MTHODIMPL_SIZE,
	MONO_MODULEREF_SIZE,	/* 0x1A */
	MONO_TYPESPEC_SIZE,
	MONO_IMPLMAP_SIZE,	
	MONO_FIELD_RVA_SIZE,
	0,
	0,
	MONO_ASSEMBLY_SIZE,	/* 0x20 */
	MONO_ASSEMBLY_PROCESSOR_SIZE,
	MONO_ASSEMBLYOS_SIZE,
	MONO_ASSEMBLYREF_SIZE,
	MONO_ASSEMBLYREFPROC_SIZE,
	MONO_ASSEMBLYREFOS_SIZE,
	MONO_FILE_SIZE,
	MONO_EXP_TYPE_SIZE,
	MONO_MANIFEST_SIZE,
	MONO_NESTED_CLASS_SIZE,
	0	/* 0x2A */
};

static void
alloc_table (MonoDynamicTable *table, guint nrows)
{
	table->rows = nrows;
	g_assert (table->columns);
	table->values = g_realloc (table->values, (1 + table->rows) * table->columns * sizeof (guint32));
}

static guint32
string_heap_insert (MonoStringHeap *sh, const char *str)
{
	guint32 idx;
	guint32 len;
	gpointer oldkey, oldval;

	if (g_hash_table_lookup_extended (sh->hash, str, &oldkey, &oldval))
		return GPOINTER_TO_UINT (oldval);

	len = strlen (str) + 1;
	idx = sh->index;
	if (idx + len > sh->alloc_size) {
		sh->alloc_size += len + 4096;
		sh->data = g_realloc (sh->data, sh->alloc_size);
	}
	/*
	 * We strdup the string even if we already copy them in sh->data
	 * so that the string pointers in the hash remain valid even if
	 * we need to realloc sh->data. We may want to avoid that later.
	 */
	g_hash_table_insert (sh->hash, g_strdup (str), GUINT_TO_POINTER (idx));
	memcpy (sh->data + idx, str, len);
	sh->index += len;
	return idx;
}

static void
string_heap_init (MonoStringHeap *sh)
{
	sh->index = 0;
	sh->alloc_size = 4096;
	sh->data = g_malloc (4096);
	sh->hash = g_hash_table_new (g_str_hash, g_str_equal);
	string_heap_insert (sh, "");
}

static void
string_heap_free (MonoStringHeap *sh)
{
	g_free (sh->data);
	g_hash_table_foreach (sh->hash, (GHFunc)g_free, NULL);
	g_hash_table_destroy (sh->hash);
}

static guint32
mono_image_add_stream_data (MonoDynamicStream *stream, char *data, guint32 len)
{
	guint32 idx;
	if (stream->alloc_size < stream->index + len) {
		stream->alloc_size += len + 4096;
		stream->data = g_realloc (stream->data, stream->alloc_size);
	}
	memcpy (stream->data + stream->index, data, len);
	idx = stream->index;
	stream->index += len;
	/* 
	 * align index? Not without adding an additional param that controls it since
	 * we may store a blob value in pieces.
	 */
	return idx;
}

static void
encode_type (MonoType *type, char *p, char **endbuf)
{
	if (!type) {
		mono_metadata_encode_value (MONO_TYPE_VOID, p, endbuf);
		return;
	}
		
	switch (type->type){
	case MONO_TYPE_VOID:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_TYPEDBYREF:
		mono_metadata_encode_value (type->type, p, endbuf);
		break;
	case MONO_TYPE_SZARRAY:
		mono_metadata_encode_value (type->type, p, endbuf);
		encode_type (type->data.type, p, endbuf);
		break;
	case MONO_TYPE_CLASS:
		mono_metadata_encode_value (type->type, p, endbuf);
		g_warning ("need to encode class %s", type->data.klass->name);
		break;
	default:
		g_error ("need to encode type %x", type->type);
	}
}

static guint32
method_encode_signature (MonoDynamicAssembly *assembly, ReflectionMethodBuilder *mb)
{
	char *buf;
	char *p;
	int i;
	guint32 nparams =  mb->parameters ? mono_array_length (mb->parameters): 0;
	guint32 size = 10 + nparams * 10;
	guint32 idx;
	char blob_size [6];
	char *b = blob_size;
	
	p = buf = g_malloc (size);
	/* LAMESPEC: all the call conv spec is foobared */
	*p = mb->call_conv & 0x60; /* has-this, explicit-this */
	if (mb->call_conv & 2)
		*p |= 0x5; /* vararg */
	if (!(mb->attrs & METHOD_ATTRIBUTE_STATIC))
		*p |= 0x20; /* hasthis */
	p++;
	mono_metadata_encode_value (nparams, p, &p);
	encode_type (mb->rtype->type, p, &p);
	for (i = 0; i < nparams; ++i) {
		MonoReflectionType *pt = mono_array_get (mb->parameters, MonoReflectionType*, i);
		encode_type (pt->type, p, &p);
	}
	/* store length */
	mono_metadata_encode_value (p-buf, b, &b);
	idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	mono_image_add_stream_data (&assembly->blob, buf, p-buf);
	g_free (buf);
	return idx;
}

static guint32
encode_locals (MonoDynamicAssembly *assembly, MonoReflectionILGen *ilgen)
{
	MonoDynamicTable *table;
	guint32 *values;
	char *p;
	guint32 idx, sig_idx;
	guint nl = mono_array_length (ilgen->locals);
	char *buf;
	char blob_size [6];
	char *b = blob_size;
	int i;

	p = buf = g_malloc (10 + nl * 10);
	table = &assembly->tables [MONO_TABLE_STANDALONESIG];
	idx = table->next_idx ++;
	table->rows ++;
	alloc_table (table, table->rows);
	values = table->values + idx * MONO_STAND_ALONE_SIGNATURE_SIZE;

	mono_metadata_encode_value (0x07, p, &p);
	mono_metadata_encode_value (nl, p, &p);
	for (i = 0; i < nl; ++i) {
		MonoReflectionLocalBuilder *lb = mono_array_get (ilgen->locals, MonoReflectionLocalBuilder*, i);
		encode_type (lb->type->type, p, &p);
	}
	mono_metadata_encode_value (p-buf, b, &b);
	sig_idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	mono_image_add_stream_data (&assembly->blob, buf, p-buf);
	g_free (buf);

	values [MONO_STAND_ALONE_SIGNATURE] = sig_idx;

	return idx;
}

static guint32
method_encode_code (MonoDynamicAssembly *assembly, ReflectionMethodBuilder *mb)
{
	/* we use only tiny formats now: need  to implement ILGenerator */
	char flags = 0;
	guint32 idx;
	guint32 code_size;
	gint32 max_stack;
	gint32 num_locals = 0;
	gint32 num_exception = 0;
	gint maybe_small;
	guint32 fat_flags;
	char fat_header [12];
	guint32 *intp;
	guint16 *shortp;
	guint32 local_sig = 0;
	MonoArray *code;

	if (mb->ilgen) {
		code = mb->ilgen->code;
		code_size = mb->ilgen->code_len;
		max_stack = mb->ilgen->max_stack;
		num_locals = mb->ilgen->locals ? mono_array_length (mb->ilgen->locals) : 0;
	} else {
		code = mb->code;
		code_size = mono_array_length (code);
		max_stack = 8; /* we probably need to run a verifier on the code... */
	}

	/* check for exceptions, maxstack, locals */
	maybe_small = (max_stack <= 8) && (!num_locals) && (!num_exception);
	if (maybe_small) {
		if (code_size < 64 && !(code_size & 1)) {
			flags = (code_size << 2) | 0x2;
		} else if (code_size < 32 && (code_size & 1)) {
			flags = (code_size << 2) | 0x6; /* LAMESPEC: see metadata.c */
		} else {
			goto fat_header;
		}
		idx = mono_image_add_stream_data (&assembly->code, &flags, 1);
		mono_image_add_stream_data (&assembly->code, mono_array_addr (code, char, 0), code_size);
		return assembly->text_rva + idx + CLI_H_SIZE;
	} 
fat_header:
	if (num_locals)
		local_sig = encode_locals (assembly, mb->ilgen);
	/* 
	 * FIXME: need to set also the header size in fat_flags.
	 * (and more sects and init locals flags)
	 */
	fat_flags =  0x03;
	shortp = (guint16*)(fat_header);
	*shortp = fat_flags;
	shortp = (guint16*)(fat_header + 2);
	*shortp = max_stack;
	intp = (guint32*)(fat_header + 4);
	*intp = code_size;
	intp = (guint32*)(fat_header + 8);
	*intp = local_sig;
	idx = mono_image_add_stream_data (&assembly->code, fat_header, 12);
	mono_image_add_stream_data (&assembly->code, mono_array_addr (code, char, 0), code_size);
	return assembly->text_rva + idx + CLI_H_SIZE;
}

static guint32
find_index_in_table (MonoDynamicAssembly *assembly, int table_idx, int col, guint32 index)
{
	int i;
	MonoDynamicTable *table;
	guint32 *values;
	
	table = &assembly->tables [table_idx];

	g_assert (col < table->columns);

	values = table->values + table->columns;
	for (i = 1; i <= table->rows; ++i) {
		if (values [col] == index)
			return i;
	}
	return 0;
}

static void
mono_image_basic_method (ReflectionMethodBuilder *mb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	char *name;

	table = &assembly->tables [MONO_TABLE_METHOD];
	*mb->table_idx = table->next_idx ++;
	values = table->values + *mb->table_idx * MONO_METHOD_SIZE;
	if (mb->name) {
		name = mono_string_to_utf8 (mb->name);
		values [MONO_METHOD_NAME] = string_heap_insert (&assembly->sheap, name);
		g_free (name);
	} else { /* a constructor */
		values [MONO_METHOD_NAME] = string_heap_insert (&assembly->sheap, mb->attrs & METHOD_ATTRIBUTE_STATIC? ".cctor": ".ctor");
	}
	values [MONO_METHOD_FLAGS] = mb->attrs;
	values [MONO_METHOD_IMPLFLAGS] = mb->iattrs;
	values [MONO_METHOD_SIGNATURE] = method_encode_signature (assembly, mb);
	values [MONO_METHOD_PARAMLIST] = 0; /* FIXME: add support later */
	values [MONO_METHOD_RVA] = method_encode_code (assembly, mb);
}

static void
mono_image_get_method_info (MonoReflectionMethodBuilder *mb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	char *name;
	ReflectionMethodBuilder rmb;

	rmb.ilgen = mb->ilgen;
	rmb.rtype = mb->rtype;
	rmb.parameters = mb->parameters;
	rmb.attrs = mb->attrs;
	rmb.iattrs = mb->iattrs;
	rmb.call_conv = mb->call_conv;
	rmb.code = mb->code;
	rmb.type = mb->type;
	rmb.name = mb->name;
	rmb.table_idx = &mb->table_idx;

	mono_image_basic_method (&rmb, assembly);

	if (mb->dll) { /* It's a P/Invoke method */
		guint32 moduleref;
		table = &assembly->tables [MONO_TABLE_IMPLMAP];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_IMPLMAP_SIZE;
		values [MONO_IMPLMAP_FLAGS] = (mb->native_cc << 8) | mb->charset;
		values [MONO_IMPLMAP_MEMBER] = (mb->table_idx << 1) | 1; /* memberforwarded: method */
		name = mono_string_to_utf8 (mb->dllentry);
		values [MONO_IMPLMAP_NAME] = string_heap_insert (&assembly->sheap, name);
		g_free (name);
		name = mono_string_to_utf8 (mb->dll);
		moduleref = string_heap_insert (&assembly->sheap, name);
		g_free (name);
		if (!(values [MONO_IMPLMAP_SCOPE] = find_index_in_table (assembly, MONO_TABLE_MODULEREF, MONO_MODULEREF_NAME, moduleref))) {
			table = &assembly->tables [MONO_TABLE_MODULEREF];
			table->rows ++;
			alloc_table (table, table->rows);
			table->values [table->rows * MONO_MODULEREF_SIZE + MONO_MODULEREF_NAME] = moduleref;
			values [MONO_IMPLMAP_SCOPE] = table->rows;
		}
	}
}

static void
mono_image_get_ctor_info (MonoReflectionCtorBuilder *mb, MonoDynamicAssembly *assembly)
{
	ReflectionMethodBuilder rmb;

	rmb.ilgen = mb->ilgen;
	rmb.rtype = NULL;
	rmb.parameters = mb->parameters;
	rmb.attrs = mb->attrs;
	rmb.iattrs = mb->iattrs;
	rmb.call_conv = mb->call_conv;
	rmb.code = NULL;
	rmb.type = mb->type;
	rmb.name = NULL;
	rmb.table_idx = &mb->table_idx;

	mono_image_basic_method (&rmb, assembly);

}

static guint32
field_encode_signature (MonoDynamicAssembly *assembly, MonoReflectionFieldBuilder *fb)
{
	char blob_size [64];
	char *b = blob_size;
	char *p;
	char* buf;
	guint32 idx;
	
	p = buf = g_malloc (64);
	
	mono_metadata_encode_value (0x06, p, &p);
	/* encode custom attributes before the type */
	encode_type (fb->type->type, p, &p);
	g_assert (p-buf < 64);
	mono_metadata_encode_value (p-buf, b, &b);
	idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	mono_image_add_stream_data (&assembly->blob, buf, p-buf);
	g_free (buf);
	return idx;
}

static void
mono_image_get_field_info (MonoReflectionFieldBuilder *fb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	char *name;

	table = &assembly->tables [MONO_TABLE_FIELD];
	fb->table_idx = table->next_idx ++;
	values = table->values + fb->table_idx * MONO_FIELD_SIZE;
	name = mono_string_to_utf8 (fb->name);
	values [MONO_FIELD_NAME] = string_heap_insert (&assembly->sheap, name);
	g_free (name);
	values [MONO_FIELD_FLAGS] = fb->attrs;
	values [MONO_FIELD_SIGNATURE] = field_encode_signature (assembly, fb);

	if (fb->offset != -1) {
		table = &assembly->tables [MONO_TABLE_FIELDLAYOUT];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_FIELD_LAYOUT_SIZE;
		values [MONO_FIELD_LAYOUT_FIELD] = fb->table_idx;
		values [MONO_FIELD_LAYOUT_OFFSET] = fb->offset;
	}
}

static guint32
property_encode_signature (MonoDynamicAssembly *assembly, MonoReflectionPropertyBuilder *fb)
{
	char *buf, *p;
	char blob_size [6];
	char *b = blob_size;
	guint32 nparams = 0;
	MonoReflectionMethodBuilder *mb = fb->get_method;
	guint32 idx, i;

	if (mb && mb->parameters)
		nparams = mono_array_length (mb->parameters);
	buf = p = g_malloc (24 + nparams * 10);
	*p = 0x08;
	p++;
	mono_metadata_encode_value (nparams, p, &p);
	if (mb) {
		encode_type (mb->rtype->type, p, &p);
		for (i = 0; i < nparams; ++i) {
			MonoReflectionType *pt = mono_array_get (mb->parameters, MonoReflectionType*, i);
			encode_type (pt->type, p, &p);
		}
	} else {
		*p++ = 1; /* void: a property should probably not be allowed without a getter */
	}
	/* store length */
	mono_metadata_encode_value (p-buf, b, &b);
	idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	mono_image_add_stream_data (&assembly->blob, buf, p-buf);
	g_free (buf);
	return idx;
}

static void
mono_image_get_property_info (MonoReflectionPropertyBuilder *pb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	char *name;
	guint num_methods = 0;
	guint32 semaidx;

	/* 
	 * we need to set things in the following tables:
	 * PROPERTYMAP (info already filled in _get_type_info ())
	 * PROPERTY    (rows already preallocated in _get_type_info ())
	 * METHOD      (method info already done with the generic method code)
	 * METHODSEMANTICS
	 */
	table = &assembly->tables [MONO_TABLE_PROPERTY];
	pb->table_idx = table->next_idx ++;
	values = table->values + pb->table_idx * MONO_FIELD_SIZE;
	name = mono_string_to_utf8 (pb->name);
	values [MONO_PROPERTY_NAME] = string_heap_insert (&assembly->sheap, name);
	g_free (name);
	values [MONO_PROPERTY_FLAGS] = pb->attrs;
	values [MONO_PROPERTY_TYPE] = property_encode_signature (assembly, pb);

	/* FIXME: we still don't handle 'other' methods */
	if (pb->get_method) num_methods ++;
	if (pb->set_method) num_methods ++;

	table = &assembly->tables [MONO_TABLE_METHODSEMANTICS];
	table->rows += num_methods;
	alloc_table (table, table->rows);

	if (pb->get_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_GETTER;
		values [MONO_METHOD_SEMA_METHOD] = pb->get_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (pb->table_idx << 1) | 1;
	}
	if (pb->set_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_SETTER;
		values [MONO_METHOD_SEMA_METHOD] = pb->set_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (pb->table_idx << 1) | 0;
	}
}

static guint32
resolution_scope_from_image (MonoDynamicAssembly *assembly, MonoImage *image)
{
	if (image != mono_defaults.corlib)
		g_error ("multiple assemblyref not yet supported");
	/* first row in assemblyref */
	return (1 << RESOLTION_SCOPE_BITS) | RESOLTION_SCOPE_ASSEMBLYREF;
}

static guint32
mono_image_typedef_or_ref (MonoDynamicAssembly *assembly, MonoType *type)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 token;
	MonoClass *klass;

	if (!assembly->typeref)
		assembly->typeref = g_hash_table_new (g_direct_hash, g_direct_equal);
	
	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->typeref, type));
	if (token)
		return token;
	klass = type->data.klass;
	/*
	 * If it's in the same module:
	 * return TYPEDEFORREF_TYPEDEF | ((klass->token & 0xffffff) << TYPEDEFORREF_BITS)
	 */

	table = &assembly->tables [MONO_TABLE_TYPEREF];
	alloc_table (table, table->rows + 1);
	values = table->values + table->next_idx * MONO_TYPEREF_SIZE;
	values [MONO_TYPEREF_SCOPE] = resolution_scope_from_image (assembly, klass->image);
	values [MONO_TYPEREF_NAME] = string_heap_insert (&assembly->sheap, klass->name);
	values [MONO_TYPEREF_NAMESPACE] = string_heap_insert (&assembly->sheap, klass->name_space);
	token = TYPEDEFORREF_TYPEREF | (table->next_idx << TYPEDEFORREF_BITS); /* typeref */
	g_hash_table_insert (assembly->typeref, type, GUINT_TO_POINTER(token));
	table->next_idx ++;
	return token;
}

static void
mono_image_get_type_info (MonoReflectionTypeBuilder *tb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;
	guint *values;
	int i;
	char *n;

	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	tb->table_idx = table->next_idx ++;
	values = table->values + tb->table_idx * MONO_TYPEDEF_SIZE;
	values [MONO_TYPEDEF_FLAGS] = tb->attrs;
	values [MONO_TYPEDEF_EXTENDS] = mono_image_typedef_or_ref (assembly, tb->parent->type);
	n = mono_string_to_utf8 (tb->name);
	values [MONO_TYPEDEF_NAME] = string_heap_insert (&assembly->sheap, n);
	g_free (n);
	n = mono_string_to_utf8 (tb->nspace);
	values [MONO_TYPEDEF_NAMESPACE] = string_heap_insert (&assembly->sheap, n);
	g_free (n);
	values [MONO_TYPEDEF_FIELD_LIST] = assembly->tables [MONO_TABLE_FIELD].next_idx;
	values [MONO_TYPEDEF_METHOD_LIST] = assembly->tables [MONO_TABLE_METHOD].next_idx;

	/* handle methods */
	if (tb->methods) {
		table = &assembly->tables [MONO_TABLE_METHOD];
		table->rows += mono_array_length (tb->methods);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length (tb->methods); ++i)
			mono_image_get_method_info (
				mono_array_get (tb->methods, MonoReflectionMethodBuilder*, i), assembly);
	}

	/* handle constructors */
	if (tb->ctors) {
		table = &assembly->tables [MONO_TABLE_METHOD];
		table->rows += mono_array_length (tb->ctors);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length (tb->ctors); ++i)
			mono_image_get_ctor_info (
				mono_array_get (tb->ctors, MonoReflectionCtorBuilder*, i), assembly);
	}

	/* handle fields */
	if (tb->fields) {
		table = &assembly->tables [MONO_TABLE_FIELD];
		table->rows += mono_array_length (tb->fields);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length (tb->fields); ++i)
			mono_image_get_field_info (
				mono_array_get (tb->fields, MonoReflectionFieldBuilder*, i), assembly);
	}

	/* Do the same with properties etc.. */
	if (tb->properties && mono_array_length (tb->properties)) {
		table = &assembly->tables [MONO_TABLE_PROPERTY];
		table->rows += mono_array_length (tb->properties);
		alloc_table (table, table->rows);
		table = &assembly->tables [MONO_TABLE_PROPERTYMAP];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_PROPERTY_MAP_SIZE;
		values [MONO_PROPERTY_MAP_PARENT] = tb->table_idx;
		values [MONO_PROPERTY_MAP_PROPERTY_LIST] = assembly->tables [MONO_TABLE_PROPERTY].next_idx;
		for (i = 0; i < mono_array_length (tb->properties); ++i)
			mono_image_get_property_info (
				mono_array_get (tb->properties, MonoReflectionPropertyBuilder*, i), assembly);
	}
}

static void
mono_image_fill_module_table (MonoReflectionModuleBuilder *mb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;
	int i;
	char *name;

	table = &assembly->tables [MONO_TABLE_MODULE];
	mb->table_idx = table->next_idx ++;
	name = mono_string_to_utf8 (mb->module.name);
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_NAME] = string_heap_insert (&assembly->sheap, name);
	g_free (name);
	/* need to set mvid? */

	/*
	 * fill-in info in other tables as well.
	 */
	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	table->rows += mono_array_length (mb->types);
	alloc_table (table, table->rows);
	for (i = 0; i < mono_array_length (mb->types); ++i)
		mono_image_get_type_info (mono_array_get (mb->types, MonoReflectionTypeBuilder*, i), assembly);
}

#define align_pointer(base,p)\
	do {\
		guint32 __diff = (unsigned char*)(p)-(unsigned char*)(base);\
		if (__diff & 3)\
			(p) += 4 - (__diff & 3);\
	} while (0)

static void
build_compressed_metadata (MonoDynamicAssembly *assembly)
{
	int i;
	guint64 valid_mask = 0;
	guint32 heapt_size = 0;
	guint32 meta_size = 256; /* allow for header and other stuff */
	guint32 table_offset;
	guint32 ntables = 0;
	guint64 *int64val;
	guint32 *int32val;
	guint16 *int16val;
	MonoImage *meta;
	unsigned char *p;
	char *version = "mono" VERSION;
	
	/* Compute table sizes */
	meta = assembly->assembly.image = g_new0 (MonoImage, 1);
	
	/* Setup the info used by compute_sizes () */
	meta->idx_blob_wide = assembly->blob.index >= 65536 ? 1 : 0;
	meta->idx_guid_wide = assembly->guid.index >= 65536 ? 1 : 0;
	meta->idx_string_wide = assembly->sheap.index >= 65536 ? 1 : 0;

	meta_size += assembly->blob.index;
	meta_size += assembly->guid.index;
	meta_size += assembly->sheap.index;
	meta_size += assembly->us.index;

	for (i=0; i < 64; ++i)
		meta->tables [i].rows = assembly->tables [i].rows;
	
	for (i = 0; i < 64; i++){
		if (meta->tables [i].rows == 0)
			continue;
		valid_mask |= (guint64)1 << i;
		ntables ++;
		meta->tables [i].row_size = mono_metadata_compute_size (
			meta, i, &meta->tables [i].size_bitfield);
		heapt_size += meta->tables [i].row_size * meta->tables [i].rows;
	}
	heapt_size += 24; /* #~ header size */
	heapt_size += ntables * 4;
	meta_size += heapt_size;
	meta->raw_metadata = g_malloc0 (meta_size);
	p = meta->raw_metadata;
	/* the metadata signature */
	*p++ = 'B'; *p++ = 'S'; *p++ = 'J'; *p++ = 'B';
	/* version numbers and 4 bytes reserved */
	int16val = (guint16*)p;
	*int16val++ = 1;
	*int16val = 1;
	p += 8;
	/* version string */
	int32val = (guint32*)p;
	*int32val = strlen (version);
	p += 4;
	memcpy (p, version, *int32val);
	p += *int32val;
	align_pointer (meta->raw_metadata, p);
	int16val = (guint16*)p;
	*int16val++ = 0; /* flags must be 0 */
	*int16val = 5; /* number of streams */
	p += 4;

	/*
	 * write the stream info.
	 */
	table_offset = (p - (unsigned char*)meta->raw_metadata) + 5 * 8 + 40; /* room needed for stream headers */
	
	int32val = (guint32*)p;
	*int32val++ = assembly->tstream.offset = table_offset;
	*int32val = heapt_size;
	table_offset += *int32val;
	p += 8;
	strcpy (p, "#~");
	p += 3;
	align_pointer (meta->raw_metadata, p);

	int32val = (guint32*)p;
	*int32val++ = assembly->sheap.offset = table_offset;
	*int32val = assembly->sheap.index;
	table_offset += *int32val;
	p += 8;
	strcpy (p, "#Strings");
	p += 9;
	align_pointer (meta->raw_metadata, p);

	int32val = (guint32*)p;
	*int32val++ = assembly->us.offset = table_offset;
	*int32val = assembly->us.index;
	table_offset += *int32val;
	p += 8;
	strcpy (p, "#US");
	p += 4;
	align_pointer (meta->raw_metadata, p);

	int32val = (guint32*)p;
	*int32val++ = assembly->blob.offset = table_offset;
	*int32val = assembly->blob.index;
	table_offset += *int32val;
	p += 8;
	strcpy (p, "#Blob");
	p += 6;
	align_pointer (meta->raw_metadata, p);

	int32val = (guint32*)p;
	*int32val++ = assembly->guid.offset = table_offset;
	*int32val = assembly->guid.index;
	table_offset += *int32val;
	p += 8;
	strcpy (p, "#GUID");
	p += 6;
	align_pointer (meta->raw_metadata, p);

	/* 
	 * now copy the data, the table stream header and contents goes first.
	 */
	g_assert ((p - (unsigned char*)meta->raw_metadata) < assembly->tstream.offset);
	p = meta->raw_metadata + assembly->tstream.offset;
	int32val = (guint32*)p;
	*int32val = 0; /* reserved */
	p += 4;
	*p++ = 1; /* version */
	*p++ = 0;
	if (meta->idx_string_wide)
		*p |= 0x01;
	if (meta->idx_guid_wide)
		*p |= 0x02;
	if (meta->idx_blob_wide)
		*p |= 0x04;
	++p;
	*p++ = 0; /* reserved */
	int64val = (guint64*)p;
	*int64val++ = valid_mask;
	*int64val++ = 0; /* bitvector of sorted tables, set to 0 for now  */
	p += 16;
	int32val = (guint32*)p;
	for (i = 0; i < 64; i++){
		if (meta->tables [i].rows == 0)
			continue;
		*int32val++ = meta->tables [i].rows;
	}
	p = (unsigned char*)int32val;
	/* compress the tables */
	for (i = 0; i < 64; i++){
		int row, col;
		guint32 *values;
		guint32 bitfield = meta->tables [i].size_bitfield;
		if (!meta->tables [i].rows)
			continue;
		if (assembly->tables [i].columns != mono_metadata_table_count (bitfield))
			g_error ("col count mismatch in %d: %d %d", i, assembly->tables [i].columns, mono_metadata_table_count (bitfield));
		meta->tables [i].base = p;
		for (row = 1; row <= meta->tables [i].rows; ++row) {
			values = assembly->tables [i].values + row * assembly->tables [i].columns;
			for (col = 0; col < assembly->tables [i].columns; ++col) {
				switch (mono_metadata_table_size (bitfield, col)) {
				case 1:
					*p++ = values [col];
					break;
				case 2:
					int16val = (guint16*)p;
					*int16val = values [col];
					p += 2;
					break;
				case 4:
					int32val = (guint32*)p;
					*int32val = values [col];
					p += 4;
					break;
				default:
					g_assert_not_reached ();
				}
			}
		}
		g_assert ((p - (unsigned char*)meta->tables [i].base) == (meta->tables [i].rows * meta->tables [i].row_size));
	}
	
	g_assert (assembly->guid.offset + assembly->guid.index < meta_size);
	memcpy (meta->raw_metadata + assembly->sheap.offset, assembly->sheap.data, assembly->sheap.index);
	memcpy (meta->raw_metadata + assembly->us.offset, assembly->us.data, assembly->us.index);
	memcpy (meta->raw_metadata + assembly->blob.offset, assembly->blob.data, assembly->blob.index);
	memcpy (meta->raw_metadata + assembly->guid.offset, assembly->guid.data, assembly->guid.index);

	assembly->meta_size = assembly->guid.offset + assembly->guid.index;
}

static void
mono_image_build_metadata (MonoReflectionAssemblyBuilder *assemblyb)
{
	MonoDynamicTable *table;
	MonoDynamicAssembly *assembly = assemblyb->dynamic_assembly;
	guint32 len;
	guint32 *values;
	char *name;
	int i;
	
	assembly->text_rva =  0x00002000;

	table = &assembly->tables [MONO_TABLE_ASSEMBLY];
	alloc_table (table, 1);
	values = table->values + MONO_ASSEMBLY_SIZE;
	values [MONO_ASSEMBLY_HASH_ALG] = 0x8004;
	name = mono_string_to_utf8 (assemblyb->name);
	values [MONO_ASSEMBLY_NAME] = string_heap_insert (&assembly->sheap, name);
	g_free (name);
	values [MONO_ASSEMBLY_CULTURE] = string_heap_insert (&assembly->sheap, "");
	values [MONO_ASSEMBLY_PUBLIC_KEY] = 0;
	values [MONO_ASSEMBLY_MAJOR_VERSION] = 0;
	values [MONO_ASSEMBLY_MINOR_VERSION] = 0;
	values [MONO_ASSEMBLY_REV_NUMBER] = 0;
	values [MONO_ASSEMBLY_BUILD_NUMBER] = 0;

	assembly->tables [MONO_TABLE_TYPEDEF].rows = 1; /* .<Module> */
	assembly->tables [MONO_TABLE_TYPEDEF].next_idx++;

	len = mono_array_length (assemblyb->modules);
	table = &assembly->tables [MONO_TABLE_MODULE];
	alloc_table (table, len);
	for (i = 0; i < len; ++i)
		mono_image_fill_module_table (mono_array_get (assemblyb->modules, MonoReflectionModuleBuilder*, i), assembly);

	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	/* 
	 * table->rows is already set above and in mono_image_fill_module_table.
	 */
	alloc_table (table, table->rows);
	/*
	 * Set the first entry.
	 */
	values = table->values + table->columns;
	values [MONO_TYPEDEF_FLAGS] = 0;
	values [MONO_TYPEDEF_NAME] = string_heap_insert (&assembly->sheap, "<Module>") ;
	values [MONO_TYPEDEF_NAMESPACE] = string_heap_insert (&assembly->sheap, "") ;
	values [MONO_TYPEDEF_EXTENDS] = 0;
	values [MONO_TYPEDEF_FIELD_LIST] = 1;
	values [MONO_TYPEDEF_METHOD_LIST] = 1;

	/* later include all the assemblies referenced */
	table = &assembly->tables [MONO_TABLE_ASSEMBLYREF];
	alloc_table (table, 1);
	values = table->values + table->columns;
	values [MONO_ASSEMBLYREF_NAME] = string_heap_insert (&assembly->sheap, "corlib");

	build_compressed_metadata (assembly);
}

guint32
mono_image_insert_string (MonoReflectionAssemblyBuilder *assembly, MonoString *str)
{
	guint32 index;
	char buf [16];
	char *b = buf;
	
	if (!assembly->dynamic_assembly)
		mono_image_basic_init (assembly);
	mono_metadata_encode_value (str->length, b, &b);
	index = mono_image_add_stream_data (&assembly->dynamic_assembly->us, buf, b-buf);
	/* FIXME: ENOENDIAN */
	mono_image_add_stream_data (&assembly->dynamic_assembly->us, (char*)mono_string_chars (str), str->length * 2);
	return index;
}

void
mono_image_basic_init (MonoReflectionAssemblyBuilder *assemblyb)
{
	MonoDynamicAssembly *assembly;
	int i;
	
	if (assemblyb->dynamic_assembly)
		return;

	assembly = assemblyb->dynamic_assembly = g_new0 (MonoDynamicAssembly, 1);

	string_heap_init (&assembly->sheap);
	mono_image_add_stream_data (&assembly->us, "", 1);
	mono_image_add_stream_data (&assembly->blob, "", 1);

	for (i=0; i < 64; ++i) {
		assembly->tables [i].next_idx = 1;
		assembly->tables [i].columns = table_sizes [i];
	}
	
}

int
mono_image_get_header (MonoReflectionAssemblyBuilder *assemblyb, char *buffer, int maxsize)
{
	MonoMSDOSHeader *msdos;
	MonoDotNetHeader *header;
	MonoSectionTable *section;
	MonoCLIHeader *cli_header;
	guint32 header_size =  TEXT_OFFSET + CLI_H_SIZE;
	MonoDynamicAssembly *assembly;

	static const unsigned char msheader[] = {
		0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,  0x04, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00,
		0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
		0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09, 0xcd,  0x21, 0xb8, 0x01, 0x4c, 0xcd, 0x21, 0x54, 0x68,
		0x69, 0x73, 0x20, 0x70, 0x72, 0x6f, 0x67, 0x72,  0x61, 0x6d, 0x20, 0x63, 0x61, 0x6e, 0x6e, 0x6f,
		0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6e,  0x20, 0x69, 0x6e, 0x20, 0x44, 0x4f, 0x53, 0x20,
		0x6d, 0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d, 0x0a,  0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
	};

	if (maxsize < header_size)
		return -1;

	mono_image_basic_init (assemblyb);
	assembly = assemblyb->dynamic_assembly;

	mono_image_build_metadata (assemblyb);

	memset (buffer, 0, header_size);
	memcpy (buffer, msheader, sizeof (MonoMSDOSHeader));

	msdos = (MonoMSDOSHeader *)buffer;
	header = (MonoDotNetHeader *)(buffer + sizeof (MonoMSDOSHeader));
	section = (MonoSectionTable*) (buffer + sizeof (MonoMSDOSHeader) + sizeof (MonoDotNetHeader));

	/* FIXME: ENDIAN problem: byteswap as needed */
	msdos->pe_offset = sizeof (MonoMSDOSHeader);

	header->pesig [0] = 'P';
	header->pesig [1] = 'E';
	header->pesig [2] = header->pesig [3] = 0;

	header->coff.coff_machine = 0x14c;
	header->coff.coff_sections = 1; /* only .text supported now */
	header->coff.coff_time = time (NULL);
	header->coff.coff_opt_header_size = sizeof (MonoDotNetHeader) - sizeof (MonoCOFFHeader) - 4;
	/* it's an exe */
	header->coff.coff_attributes = 0x010e;
	/* it's a dll */
	//header->coff.coff_attributes = 0x210e;
	header->pe.pe_magic = 0x10B;
	header->pe.pe_major = 6;
	header->pe.pe_minor = 0;
	/* need to set: pe_code_size pe_data_size pe_rva_entry_point pe_rva_code_base pe_rva_data_base */

	header->nt.pe_image_base = 0x400000;
	header->nt.pe_section_align = 8192;
	header->nt.pe_file_alignment = FILE_ALIGN;
	header->nt.pe_os_major = 4;
	header->nt.pe_os_minor = 0;
	header->nt.pe_subsys_major = 4;
	/* need to set pe_image_size, pe_header_size */
	header->nt.pe_subsys_required = 3; /* 3 -> cmdline app, 2 -> GUI app */
	header->nt.pe_stack_reserve = 0x00100000;
	header->nt.pe_stack_commit = 0x00001000;
	header->nt.pe_heap_reserve = 0x00100000;
	header->nt.pe_heap_commit = 0x00001000;
	header->nt.pe_loader_flags = 1;
	header->nt.pe_data_dir_count = 16;

#if 0
	/* set: */
	header->datadir.pe_import_table
	pe_resource_table
	pe_reloc_table
	pe_iat	
#endif
	header->datadir.pe_cli_header.size = CLI_H_SIZE;
	header->datadir.pe_cli_header.rva = assembly->text_rva; /* we put it always at the beginning */

	/* Write section tables */
	strcpy (section->st_name, ".text");
	section->st_virtual_size = 1024; /* FIXME */
	section->st_virtual_address = assembly->text_rva;
	section->st_raw_data_size = 1024; /* FIXME */
	section->st_raw_data_ptr = TEXT_OFFSET;
	section->st_flags = SECT_FLAGS_HAS_CODE | SECT_FLAGS_MEM_EXECUTE | SECT_FLAGS_MEM_READ;

	/* 
	 * align: build_compressed_metadata () assumes metadata is aligned 
	 * see below:
	 * cli_header->ch_metadata.rva = assembly->text_rva + assembly->code.index + CLI_H_SIZE;
	 */
	assembly->code.index += 3;
	assembly->code.index &= ~3;

	/*
	 * Write the MonoCLIHeader header 
	 */
	cli_header = (MonoCLIHeader*)(buffer + TEXT_OFFSET);
	cli_header->ch_size = CLI_H_SIZE;
	cli_header->ch_runtime_major = 2;
	cli_header->ch_flags = CLI_FLAGS_ILONLY;
	if (assemblyb->entry_point) 
		cli_header->ch_entry_point = assemblyb->entry_point->table_idx | MONO_TOKEN_METHOD_DEF;
	else
		cli_header->ch_entry_point = 0;
	cli_header->ch_metadata.rva = assembly->text_rva + assembly->code.index + CLI_H_SIZE;
	cli_header->ch_metadata.size = assembly->meta_size;
	
	return header_size;
}

