
/*
 * reflection.c: Routines for creating an image at runtime.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001, 2002 Ximian, Inc.  http://www.ximian.com
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
	MonoArray *pinfo;
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

static guint32 mono_image_typedef_or_ref (MonoDynamicAssembly *assembly, MonoType *type);
static guint32 mono_image_get_methodref_token (MonoDynamicAssembly *assembly, MonoMethod *method);

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
stream_data_align (MonoDynamicStream *stream)
{
	char buf [4] = {0};
	guint32 count = stream->index % 4;

	/* we assume the stream data will be aligned */
	if (count)
		mono_image_add_stream_data (stream, buf, 4 - count);
}

static void
encode_type (MonoDynamicAssembly *assembly, MonoType *type, char *p, char **endbuf)
{
	if (!type) {
		g_assert_not_reached ();
		return;
	}
		
	if (type->byref)
		mono_metadata_encode_value (MONO_TYPE_BYREF, p, &p);

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
		mono_metadata_encode_value (type->type, p, &p);
		break;
	case MONO_TYPE_SZARRAY:
		mono_metadata_encode_value (type->type, p, &p);
		encode_type (assembly, type->data.type, p, &p);
		break;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
		mono_metadata_encode_value (type->type, p, &p);
		mono_metadata_encode_value (mono_image_typedef_or_ref (assembly, type), p, &p);
		break;
	default:
		g_error ("need to encode type %x", type->type);
	}
	*endbuf = p;
}

static void
encode_reflection_type (MonoDynamicAssembly *assembly, MonoReflectionType *type, char *p, char **endbuf)
{
	MonoReflectionTypeBuilder *tb;
	guint32 token;

	if (!type) {
		mono_metadata_encode_value (MONO_TYPE_VOID, p, endbuf);
		return;
	}
	if (type->type) {
		encode_type (assembly, type->type, p, endbuf);
		return;
	}

	tb = (MonoReflectionTypeBuilder*) type;
	token = TYPEDEFORREF_TYPEDEF | (tb->table_idx << TYPEDEFORREF_BITS); /* typedef */

	/* FIXME: handle other base types (need to have also some hacks to compile corlib) ... */
	/* FIXME: handle byref ... */
	mono_metadata_encode_value (MONO_TYPE_CLASS, p, &p);
	mono_metadata_encode_value (token, p, endbuf);
	/*g_print ("encoding type %s to 0x%08x\n", mono_string_to_utf8 (tb->name), token);*/
}

static guint32
method_encode_signature (MonoDynamicAssembly *assembly, MonoMethodSignature *sig)
{
	char *buf;
	char *p;
	int i;
	guint32 nparams =  sig->param_count;
	guint32 size = 10 + nparams * 10;
	guint32 idx;
	char blob_size [6];
	char *b = blob_size;
	
	p = buf = g_malloc (size);
	/*
	 * FIXME: vararg, explicit_this, differenc call_conv values...
	 */
	*p = sig->call_convention;
	if (sig->hasthis)
		*p |= 0x20; /* hasthis */
	p++;
	mono_metadata_encode_value (nparams, p, &p);
	encode_type (assembly, sig->ret, p, &p);
	for (i = 0; i < nparams; ++i)
		encode_type (assembly, sig->params [i], p, &p);
	/* store length */
	mono_metadata_encode_value (p-buf, b, &b);
	idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	mono_image_add_stream_data (&assembly->blob, buf, p-buf);
	g_free (buf);
	return idx;
}

static guint32
method_builder_encode_signature (MonoDynamicAssembly *assembly, ReflectionMethodBuilder *mb)
{
	/*
	 * FIXME: reuse code from method_encode_signature().
	 */
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
	encode_reflection_type (assembly, mb->rtype, p, &p);
	for (i = 0; i < nparams; ++i) {
		MonoReflectionType *pt = mono_array_get (mb->parameters, MonoReflectionType*, i);
		encode_reflection_type (assembly, pt, p, &p);
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
		encode_reflection_type (assembly, lb->type, p, &p);
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
	gint32 max_stack, i;
	gint32 num_locals = 0;
	gint32 num_exception = 0;
	gint maybe_small;
	guint32 fat_flags;
	char fat_header [12];
	guint32 *intp;
	guint16 *shortp;
	guint32 local_sig = 0;
	guint32 header_size = 12;
	MonoArray *code;

	if ((mb->attrs & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
			(mb->iattrs & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
			(mb->iattrs & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
			(mb->attrs & METHOD_ATTRIBUTE_ABSTRACT))
		return 0;

	/*if (mb->name)
		g_print ("Encode method %s\n", mono_string_to_utf8 (mb->name));*/
	if (mb->ilgen) {
		code = mb->ilgen->code;
		code_size = mb->ilgen->code_len;
		max_stack = mb->ilgen->max_stack;
		num_locals = mb->ilgen->locals ? mono_array_length (mb->ilgen->locals) : 0;
		if (mb->ilgen->ex_handlers) {
			MonoILExceptionInfo *ex_info;
			for (i = 0; i < mono_array_length (mb->ilgen->ex_handlers); ++i) {
				ex_info = (MonoILExceptionInfo*)mono_array_addr (mb->ilgen->ex_handlers, MonoILExceptionInfo, i);
				if (ex_info->handlers)
					num_exception += mono_array_length (ex_info->handlers);
				else
					num_exception++;
			}
		}
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
		local_sig = MONO_TOKEN_SIGNATURE | encode_locals (assembly, mb->ilgen);
	/* 
	 * FIXME: need to set also the header size in fat_flags.
	 * (and more sects and init locals flags)
	 */
	fat_flags =  0x03;
	if (num_exception)
		fat_flags |= METHOD_HEADER_MORE_SECTS;
	fat_header [0] = fat_flags;
	fat_header [1] = (header_size / 4 ) << 4;
	shortp = (guint16*)(fat_header + 2);
	*shortp = max_stack;
	intp = (guint32*)(fat_header + 4);
	*intp = code_size;
	intp = (guint32*)(fat_header + 8);
	*intp = local_sig;
	idx = mono_image_add_stream_data (&assembly->code, fat_header, 12);
	mono_image_add_stream_data (&assembly->code, mono_array_addr (code, char, 0), code_size);
	if (num_exception) {
		unsigned char sheader [4];
		MonoExceptionClause clause;
		MonoILExceptionInfo * ex_info;
		MonoILExceptionBlock * ex_block;
		int j;

		stream_data_align (&assembly->code);
		/* always use fat format for now */
		sheader [0] = METHOD_HEADER_SECTION_FAT_FORMAT | METHOD_HEADER_SECTION_EHTABLE;
		num_exception *= sizeof (MonoExceptionClause);
		sheader [1] = num_exception & 0xff;
		sheader [2] = (num_exception >> 8) & 0xff;
		sheader [3] = (num_exception >> 16) & 0xff;
		mono_image_add_stream_data (&assembly->code, sheader, 4);
		/* fat header, so we are already aligned */
		/* reverse order */
		for (i = mono_array_length (mb->ilgen->ex_handlers) - 1; i >= 0; --i) {
			ex_info = (MonoILExceptionInfo *)mono_array_addr (mb->ilgen->ex_handlers, MonoILExceptionInfo, i);
			if (ex_info->handlers) {
				for (j = 0; j < mono_array_length (ex_info->handlers); ++j) {
					ex_block = (MonoILExceptionBlock*)mono_array_addr (ex_info->handlers, MonoILExceptionBlock, j);
					clause.flags = ex_block->type;
					clause.try_offset = ex_info->start;
					clause.try_len = ex_info->len;
					clause.handler_offset = ex_block->start;
					clause.handler_len = ex_block->len;
					clause.token_or_filter = ex_block->extype ? mono_metadata_token_from_dor (
							mono_image_typedef_or_ref (assembly, ex_block->extype->type)): 0;
					/* FIXME: ENOENDIAN */
					mono_image_add_stream_data (&assembly->code, (char*)&clause, sizeof (clause));
				}
			} else {
				g_error ("No clauses");
			}
		}
	}
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
	guint i, count;

	/* room in this table is already allocated */
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
	values [MONO_METHOD_SIGNATURE] = method_builder_encode_signature (assembly, mb);
	values [MONO_METHOD_RVA] = method_encode_code (assembly, mb);
	
	table = &assembly->tables [MONO_TABLE_PARAM];
	values [MONO_METHOD_PARAMLIST] = table->next_idx;

	if (mb->pinfo) {
		count = 0;
		for (i = 0; i < mono_array_length (mb->pinfo); ++i) {
			if (mono_array_get (mb->pinfo, gpointer, i))
				count++;
		}
		table->rows += count;
		alloc_table (table, table->rows);
		values = table->values + table->next_idx * MONO_PARAM_SIZE;
		for (i = 0; i < mono_array_length (mb->pinfo); ++i) {
			MonoReflectionParamBuilder *pb;
			if ((pb = mono_array_get (mb->pinfo, MonoReflectionParamBuilder*, i))) {
				values [MONO_PARAM_FLAGS] = pb->attrs;
				values [MONO_PARAM_SEQUENCE] = i;
				name = mono_string_to_utf8 (pb->name);
				values [MONO_PARAM_NAME] = string_heap_insert (&assembly->sheap, name);
				g_free (name);
				values += MONO_PARAM_SIZE;
				table->next_idx++;
			}
		}
	}
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
	rmb.pinfo = mb->pinfo;
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
	if (mb->override_method) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mb->type;
		table = &assembly->tables [MONO_TABLE_METHODIMPL];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_MTHODIMPL_SIZE;
		values [MONO_MTHODIMPL_CLASS] = tb->table_idx;
		values [MONO_MTHODIMPL_BODY] = METHODDEFORREF_METHODDEF | (mb->table_idx << METHODDEFORREF_BITS);
		if (mb->override_method->method)
			values [MONO_MTHODIMPL_DECLARATION] = mono_image_get_methodref_token (assembly, mb->override_method->method);
		else {
			MonoReflectionMethodBuilder *omb = (MonoReflectionMethodBuilder*)mb->override_method;
			values [MONO_MTHODIMPL_DECLARATION] = METHODDEFORREF_METHODDEF | (omb->table_idx << METHODDEFORREF_BITS);
		}
	}
}

static void
mono_image_get_ctor_info (MonoDomain *domain, MonoReflectionCtorBuilder *mb, MonoDynamicAssembly *assembly)
{
	ReflectionMethodBuilder rmb;

	rmb.ilgen = mb->ilgen;
	rmb.rtype = mono_type_get_object (domain, &mono_defaults.void_class->byval_arg);
	rmb.parameters = mb->parameters;
	rmb.pinfo = mb->pinfo;
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
fieldref_encode_signature (MonoDynamicAssembly *assembly, MonoClassField *field)
{
	char blob_size [64];
	char *b = blob_size;
	char *p;
	char* buf;
	guint32 idx;
	
	p = buf = g_malloc (64);
	
	/* No start code with field refs...
	 * mono_metadata_encode_value (0x06, p, &p);
	 */
	/* encode custom attributes before the type */
	encode_type (assembly, field->type, p, &p);
	g_assert (p-buf < 64);
	mono_metadata_encode_value (p-buf, b, &b);
	idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	mono_image_add_stream_data (&assembly->blob, buf, p-buf);
	g_free (buf);
	return idx;
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
	encode_reflection_type (assembly, fb->type, p, &p);
	g_assert (p-buf < 64);
	mono_metadata_encode_value (p-buf, b, &b);
	idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	mono_image_add_stream_data (&assembly->blob, buf, p-buf);
	g_free (buf);
	return idx;
}

static guint32
encode_constant (MonoDynamicAssembly *assembly, MonoObject *val, guint32 *ret_type) {
	char blob_size [64];
	char *b = blob_size;
	char *p, *box_val;
	char* buf;
	guint32 idx, len;
	
	p = buf = g_malloc (64);

	box_val = ((char*)val) + sizeof (MonoObject);
	*ret_type = val->vtable->klass->byval_arg.type;
	switch (*ret_type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
		len = 1;
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
		len = 2;
		break;
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_R4:
		len = 4;
		break;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R8:
		len = 8;
		break;
	case MONO_TYPE_STRING:
	default:
		g_error ("we don't encode constant type 0x%02x yet", *ret_type);
	}

	/* there is no signature */
	mono_metadata_encode_value (len, b, &b);
	idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	/* FIXME: ENOENDIAN */
	mono_image_add_stream_data (&assembly->blob, box_val, len);

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
	if (fb->attrs & FIELD_ATTRIBUTE_LITERAL) {
		guint32 field_type = 0;
		table = &assembly->tables [MONO_TABLE_CONSTANT];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_CONSTANT_SIZE;
		values [MONO_CONSTANT_PARENT] = HASCONSTANT_FIEDDEF | (fb->table_idx << HASCONSTANT_BITS);
		values [MONO_CONSTANT_VALUE] = encode_constant (assembly, fb->def_value, &field_type);
		values [MONO_CONSTANT_TYPE] = field_type;
		values [MONO_CONSTANT_PADDING] = 0;
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
		encode_reflection_type (assembly, mb->rtype, p, &p);
		for (i = 0; i < nparams; ++i) {
			MonoReflectionType *pt = mono_array_get (mb->parameters, MonoReflectionType*, i);
			encode_reflection_type (assembly, pt, p, &p);
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
		values [MONO_METHOD_SEMA_ASSOCIATION] = (pb->table_idx << HAS_SEMANTICS_BITS) | HAS_SEMANTICS_PROPERTY;
	}
	if (pb->set_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_SETTER;
		values [MONO_METHOD_SEMA_METHOD] = pb->set_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (pb->table_idx << HAS_SEMANTICS_BITS) | HAS_SEMANTICS_PROPERTY;
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

static guint32
mono_image_get_memberref_token (MonoDynamicAssembly *assembly, MonoClass *klass, const char *name, guint32 sig)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 token, pclass;
	guint32 parent;

	/*
	 * FIXME: we need to cache the token.
	 */
	parent = mono_image_typedef_or_ref (assembly, &klass->byval_arg);
	switch (parent & TYPEDEFORREF_MASK) {
	case TYPEDEFORREF_TYPEREF:
		pclass = MEMBERREF_PARENT_TYPEREF;
		break;
	case TYPEDEFORREF_TYPESPEC:
		pclass = MEMBERREF_PARENT_TYPESPEC;
		break;
	case TYPEDEFORREF_TYPEDEF:
		/* should never get here */
	default:
		g_error ("unknow typeref or def token");
	}
	/* extract the index */
	parent >>= TYPEDEFORREF_BITS;

	table = &assembly->tables [MONO_TABLE_MEMBERREF];
	alloc_table (table, table->rows + 1);
	values = table->values + table->next_idx * MONO_MEMBERREF_SIZE;
	values [MONO_MEMBERREF_CLASS] = pclass | (parent << MEMBERREF_PARENT_BITS);
	values [MONO_MEMBERREF_NAME] = string_heap_insert (&assembly->sheap, name);
	values [MONO_MEMBERREF_SIGNATURE] = sig;
	token = MONO_TOKEN_MEMBER_REF | table->next_idx;
	table->next_idx ++;

	return token;
}

static guint32
mono_image_get_methodref_token (MonoDynamicAssembly *assembly, MonoMethod *method)
{
	return mono_image_get_memberref_token (assembly, method->klass, 
		method->name,  method_encode_signature (assembly, method->signature));
}

static guint32
mono_image_get_fieldref_token (MonoDynamicAssembly *assembly, MonoClassField *field, MonoClass *klass)
{
	return mono_image_get_memberref_token (assembly, klass, 
		field->name,  fieldref_encode_signature (assembly, field));
}

static void
mono_image_get_type_info (MonoDomain *domain, MonoReflectionTypeBuilder *tb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;
	guint *values;
	int i;
	char *n;

	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	tb->table_idx = table->next_idx ++;
	values = table->values + tb->table_idx * MONO_TYPEDEF_SIZE;
	values [MONO_TYPEDEF_FLAGS] = tb->attrs;
	if (tb->parent) { /* interfaces don't have a parent */
		if (tb->parent->type)
			values [MONO_TYPEDEF_EXTENDS] = mono_image_typedef_or_ref (assembly, tb->parent->type);
		else {
			MonoReflectionTypeBuilder *ptb = (MonoReflectionTypeBuilder *)tb->parent;
			values [MONO_TYPEDEF_EXTENDS] = TYPEDEFORREF_TYPEDEF | (ptb->table_idx << TYPEDEFORREF_BITS);
		}
	} else
		values [MONO_TYPEDEF_EXTENDS] = 0;
	n = mono_string_to_utf8 (tb->name);
	values [MONO_TYPEDEF_NAME] = string_heap_insert (&assembly->sheap, n);
	g_free (n);
	n = mono_string_to_utf8 (tb->nspace);
	values [MONO_TYPEDEF_NAMESPACE] = string_heap_insert (&assembly->sheap, n);
	g_free (n);
	values [MONO_TYPEDEF_FIELD_LIST] = assembly->tables [MONO_TABLE_FIELD].next_idx;
	values [MONO_TYPEDEF_METHOD_LIST] = assembly->tables [MONO_TABLE_METHOD].next_idx;

	/*
	 * FIXME: constructors and methods need to be output in the same order
	 * as they are defined (according to table_idx).
	 */

	/* handle constructors */
	if (tb->ctors) {
		table = &assembly->tables [MONO_TABLE_METHOD];
		table->rows += mono_array_length (tb->ctors);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length (tb->ctors); ++i)
			mono_image_get_ctor_info (domain,
				mono_array_get (tb->ctors, MonoReflectionCtorBuilder*, i), assembly);
	}

	/* handle methods */
	if (tb->methods) {
		table = &assembly->tables [MONO_TABLE_METHOD];
		table->rows += mono_array_length (tb->methods);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length (tb->methods); ++i)
			mono_image_get_method_info (
				mono_array_get (tb->methods, MonoReflectionMethodBuilder*, i), assembly);
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
	if (tb->subtypes) {
		MonoDynamicTable *ntable;
		
		table = &assembly->tables [MONO_TABLE_TYPEDEF];
		table->rows += mono_array_length (tb->subtypes);
		alloc_table (table, table->rows);

		ntable = &assembly->tables [MONO_TABLE_NESTEDCLASS];
		ntable->rows += mono_array_length (tb->subtypes);
		alloc_table (ntable, ntable->rows);
		values = ntable->values + ntable->next_idx * MONO_NESTED_CLASS_SIZE;

		for (i = 0; i < mono_array_length (tb->subtypes); ++i) {
			MonoReflectionTypeBuilder *subtype = mono_array_get (tb->subtypes, MonoReflectionTypeBuilder*, i);

			mono_image_get_type_info (domain, subtype, assembly);
			values [MONO_NESTED_CLASS_NESTED] = subtype->table_idx;
			values [MONO_NESTED_CLASS_ENCLOSING] = tb->table_idx;
			/*g_print ("nesting %s (%d) in %s (%d) (rows %d/%d)\n",
				mono_string_to_utf8 (subtype->name), subtype->table_idx,
				mono_string_to_utf8 (tb->name), tb->table_idx,
				ntable->next_idx, ntable->rows);*/
			values += MONO_NESTED_CLASS_SIZE;
			ntable->next_idx++;
		}
	}
}

static void
mono_image_fill_module_table (MonoDomain *domain, MonoReflectionModuleBuilder *mb, MonoDynamicAssembly *assembly)
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
		mono_image_get_type_info (domain, mono_array_get (mb->types, MonoReflectionTypeBuilder*, i), assembly);
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
	table_offset += 3; table_offset &= ~3;
	
	int32val = (guint32*)p;
	*int32val++ = assembly->tstream.offset = table_offset;
	*int32val = heapt_size;
	table_offset += *int32val;
	table_offset += 3; table_offset &= ~3;
	p += 8;
	strcpy (p, "#~");
	p += 3;
	align_pointer (meta->raw_metadata, p);

	int32val = (guint32*)p;
	*int32val++ = assembly->sheap.offset = table_offset;
	*int32val = assembly->sheap.index;
	table_offset += *int32val;
	table_offset += 3; table_offset &= ~3;
	p += 8;
	strcpy (p, "#Strings");
	p += 9;
	align_pointer (meta->raw_metadata, p);

	int32val = (guint32*)p;
	*int32val++ = assembly->us.offset = table_offset;
	*int32val = assembly->us.index;
	table_offset += *int32val;
	table_offset += 3; table_offset &= ~3;
	p += 8;
	strcpy (p, "#US");
	p += 4;
	align_pointer (meta->raw_metadata, p);

	int32val = (guint32*)p;
	*int32val++ = assembly->blob.offset = table_offset;
	*int32val = assembly->blob.index;
	table_offset += *int32val;
	table_offset += 3; table_offset &= ~3;
	p += 8;
	strcpy (p, "#Blob");
	p += 6;
	align_pointer (meta->raw_metadata, p);

	int32val = (guint32*)p;
	*int32val++ = assembly->guid.offset = table_offset;
	*int32val = assembly->guid.index;
	table_offset += *int32val;
	table_offset += 3; table_offset &= ~3;
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
	MonoDomain *domain = ((MonoObject *)assemblyb)->vtable->domain;
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
		mono_image_fill_module_table (domain, mono_array_get (assemblyb->modules, MonoReflectionModuleBuilder*, i), assembly);

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
	values [MONO_ASSEMBLYREF_MAJOR_VERSION] = 0;
	values [MONO_ASSEMBLYREF_MINOR_VERSION] = 0;
	values [MONO_ASSEMBLYREF_BUILD_NUMBER] = 0;
	values [MONO_ASSEMBLYREF_REV_NUMBER] = 0;
	values [MONO_ASSEMBLYREF_FLAGS] = 0;
	values [MONO_ASSEMBLYREF_PUBLIC_KEY] = 0;
	values [MONO_ASSEMBLYREF_CULTURE] = 0;
	values [MONO_ASSEMBLYREF_HASH_VALUE] = 0;

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
	mono_metadata_encode_value (1 | (str->length * 2), b, &b);
	index = mono_image_add_stream_data (&assembly->dynamic_assembly->us, buf, b-buf);
	/* FIXME: ENOENDIAN */
	mono_image_add_stream_data (&assembly->dynamic_assembly->us, (char*)mono_string_chars (str), str->length * 2);
	mono_image_add_stream_data (&assembly->dynamic_assembly->us, "", 1);
	return MONO_TOKEN_STRING | index;
}

/*
 * Get a token to insert in the IL code stream for the given MemberInfo.
 * obj can be:
 * 	ConstructorBuilder
 * 	MethodBuilder
 * 	FieldBuilder
 * 	MonoCMethod
 * 	MonoMethod
 * 	MonoField
 * 	MonoType
 * 	TypeBuilder
 */
guint32
mono_image_create_token (MonoReflectionAssemblyBuilder *assembly, MonoObject *obj)
{
	MonoClass *klass = obj->vtable->klass;
	guint32 token;

	mono_image_basic_init (assembly);

	if (strcmp (klass->name, "MethodBuilder") == 0) {
		MonoReflectionMethodBuilder *mb = (MonoReflectionMethodBuilder *)obj;
		token = mb->table_idx | MONO_TOKEN_METHOD_DEF;
		/*g_print ("got token 0x%08x for %s\n", token, mono_string_to_utf8 (mb->name));*/
		return token;
	}
	if (strcmp (klass->name, "ConstructorBuilder") == 0) {
		MonoReflectionCtorBuilder *mb = (MonoReflectionCtorBuilder *)obj;
		token = mb->table_idx | MONO_TOKEN_METHOD_DEF;
		/*g_print ("got token 0x%08x for %s\n", token, mono_string_to_utf8 (mb->name));*/
		return token;
	}
	if (strcmp (klass->name, "FieldBuilder") == 0) {
		MonoReflectionFieldBuilder *mb = (MonoReflectionFieldBuilder *)obj;
		return mb->table_idx | MONO_TOKEN_FIELD_DEF;
	}
	if (strcmp (klass->name, "TypeBuilder") == 0) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)obj;
		return tb->table_idx | MONO_TOKEN_TYPE_DEF;
	}
	if (strcmp (klass->name, "MonoType") == 0) {
		MonoReflectionType *tb = (MonoReflectionType *)obj;
		return mono_metadata_token_from_dor (
			mono_image_typedef_or_ref (assembly->dynamic_assembly, tb->type));
	}
	if (strcmp (klass->name, "MonoCMethod") == 0 ||
			strcmp (klass->name, "MonoMethod") == 0) {
		MonoReflectionMethod *m = (MonoReflectionMethod *)obj;
		token = mono_image_get_methodref_token (assembly->dynamic_assembly, m->method);
		/*g_print ("got token 0x%08x for %s\n", token, m->method->name);*/
		return token;
	}
	if (strcmp (klass->name, "MonoField") == 0) {
		MonoReflectionField *f = (MonoReflectionField *)obj;
		token = mono_image_get_fieldref_token (assembly->dynamic_assembly, f->field, f->klass);
		/*g_print ("got token 0x%08x for %s\n", token, f->field->name);*/
		return token;
	}
	g_print ("requested token for %s\n", klass->name);
	return 0;
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

/*
 * We need to return always the same object for Type, MethodInfo, FieldInfo etc..
 */
static GHashTable *object_cache = NULL;

#define CHECK_OBJECT(t,p)	\
	do {	\
		t _obj;	\
		if (!object_cache)	\
			object_cache = g_hash_table_new (g_direct_hash, g_direct_equal);	\
		if ((_obj = g_hash_table_lookup (object_cache, (p))))	\
			return _obj;	\
	} while (0)

#define CACHE_OBJECT(p,o)	\
	do {	\
		g_hash_table_insert (object_cache, p,o);	\
	} while (0)
	
MonoReflectionAssembly*
mono_assembly_get_object (MonoDomain *domain, MonoAssembly *assembly)
{
	static MonoClass *System_Reflection_Assembly;
	MonoReflectionAssembly *res;
	
	CHECK_OBJECT (MonoReflectionAssembly *, assembly);
	if (!System_Reflection_Assembly)
		System_Reflection_Assembly = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "Assembly");
	res = (MonoReflectionAssembly *)mono_object_new (domain, System_Reflection_Assembly);
	res->assembly = assembly;
	CACHE_OBJECT (assembly, res);
	return res;
}

MonoReflectionType*
mono_type_get_object (MonoDomain *domain, MonoType *type)
{
	MonoReflectionType *res;
	MonoClass *klass;

	/* 
	 * FIXME: type may come from the cache in metadata.c, we hand out only
	 * the types from a MonoClass structure: the long term fix is to just
	 * load corlib always and remove the cache in metadata.c altogether.
	 * Or we may still handle it this way so we can store in MonoType additional info
	 * as we do now.
	 */
	klass = mono_class_from_mono_type (type);
	if ((type != &klass->byval_arg) && (type != &klass->this_arg)) {
		if (type->byref)
			type = &klass->this_arg;
		else
			type = &klass->byval_arg;
	}
	CHECK_OBJECT (MonoReflectionType *, type);
	res = (MonoReflectionType *)mono_object_new (domain, mono_defaults.monotype_class);
	res->type = type;
	CACHE_OBJECT (type, res);
	return res;
}

MonoReflectionMethod*
mono_method_get_object (MonoDomain *domain, MonoMethod *method)
{
	/*
	 * We use the same C representation for methods and constructors, but the type 
	 * name in C# is different.
	 */
	char *cname;
	MonoClass *klass;
	MonoReflectionMethod *ret;

	CHECK_OBJECT (MonoReflectionMethod *, method);
	if (*method->name == '.' && (strcmp (method->name, ".ctor") == 0 || strcmp (method->name, ".cctor") == 0))
		cname = "MonoCMethod";
	else
		cname = "MonoMethod";
	klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", cname);

	ret = (MonoReflectionMethod*)mono_object_new (domain, klass);
	ret->method = method;
	CACHE_OBJECT (method, ret);
	return ret;
}

MonoReflectionField*
mono_field_get_object (MonoDomain *domain, MonoClass *klass, MonoClassField *field)
{
	MonoReflectionField *res;
	MonoClass *oklass;

	CHECK_OBJECT (MonoReflectionField *, field);
	oklass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoField");
	res = (MonoReflectionField *)mono_object_new (domain, oklass);
	res->klass = klass;
	res->field = field;
	CACHE_OBJECT (field, res);
	return res;
}

MonoReflectionProperty*
mono_property_get_object (MonoDomain *domain, MonoClass *klass, MonoProperty *property)
{
	MonoReflectionProperty *res;
	MonoClass *oklass;

	CHECK_OBJECT (MonoReflectionProperty *, property);
	oklass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoProperty");
	res = (MonoReflectionProperty *)mono_object_new (domain, oklass);
	res->klass = klass;
	res->property = property;
	CACHE_OBJECT (property, res);
	return res;
}

MonoReflectionParameter**
mono_param_get_objects (MonoDomain *domain, MonoMethod *method)
{
	MonoReflectionParameter **res;
	MonoReflectionMethod *member;
	MonoClass *oklass;
	char **names;
	int i;

	if (!method->signature->param_count)
		return NULL;

	member = mono_method_get_object (domain, method);
	names = g_new (char*, method->signature->param_count);
	mono_method_get_param_names (method, names);
	
	/* Note: the cache is based on the address of the signature into the method
	 * since we already cache MethodInfos with the method as keys.
	 */
	CHECK_OBJECT (MonoReflectionParameter**, &(method->signature));
	oklass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "ParameterInfo");
	res = g_new0 (MonoReflectionParameter*, method->signature->param_count);
	for (i = 0; i < method->signature->param_count; ++i) {
		res [i] = (MonoReflectionParameter *)mono_object_new (domain, oklass);
		res [i]->ClassImpl = mono_type_get_object (domain, method->signature->params [i]);
		res [i]->DefaultValueImpl = NULL; /* FIXME */
		res [i]->MemberImpl = (MonoObject*)member;
		res [i]->NameImpl = mono_string_new (domain, names [i]);
		res [i]->PositionImpl = i + 1;
		res [i]->AttrsImpl = method->signature->params [i]->attrs;
	}
	g_free (names);
	CACHE_OBJECT (&(method->signature), res);
	return res;
}

/*
 * Parse a type name as accepted by the GetType () method and output the info
 * extracted in the info structure.
 * the name param will be mangled, so, make a copy before passing it to this function.
 * The fields in info will be valid until the memory pointed to by name is valid.
 * Returns 0 on parse error.
 */
int
mono_reflection_parse_type (char *name, MonoTypeNameParse *info) {

	char *start, *p, *w, *last_point;
	int in_modifiers = 0;

	start = p = w = name;

	info->name = info->name_space = info->assembly = NULL;
	info->nest_name = info->nest_name_space = NULL;
	info->rank = info->isbyref = info->ispointer = 0;

	last_point = NULL;

	while (*p) {
		switch (*p) {
		case '+':
			/* we have parsed the nesting namespace + name */
			if (last_point) {
				info->nest_name_space = start;
				*last_point = 0;
				info->nest_name = last_point + 1;
			} else {
				info->nest_name_space = "";
				info->nest_name = start;
			}
			*p = 0; /* NULL terminate */
			last_point = NULL;
			start = p + 1;
			break;
		case '.':
			last_point = w;
			break;
		case '\\':
			++p;
			break;
		case '&':
		case '*':
		case '[':
		case ',':
			in_modifiers = 1;
			break;
		default:
			break;
		}
		if (in_modifiers)
			break;
		*w++ = *p++;
	}
	
	if (last_point) {
		info->name_space = start;
		*last_point = 0;
		info->name = last_point + 1;
	} else {
		info->name_space = "";
		info->name = start;
	}
	/* FIXME: we don't mainatin an order for byref, pointer and array... */
	while (*p) {
		switch (*p) {
		case '&':
			info->isbyref = 1;
			*p++ = 0;
			break;
		case '*':
			info->ispointer = 1;
			*p++ = 0;
			break;
		case '[':
			info->rank = 1;
			*p++ = 0;
			while (*p) {
				if (*p == ',')
					info->rank++;
				if (*p == ']')
					break;
				++p;
			}
			if (*p != ']')
				return 0;
			break;
		case ',':
			*p++ = 0;
			while (*p) {
				if (*p == ' ') {
					++p;
					continue;
				}
				break;
			}
			if (!*p)
				return 0; /* missing assembly name */
			info->assembly = p;
			break;
		default:
			break;
		}
	}
	*w = 0; /* terminate class name */
	if (!info->name || !*info->name)
		return 0;
	if (info->nest_name && !*info->nest_name)
		return 0;
	/* add other consistency checks */
	return 1;
}

