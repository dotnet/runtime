
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
#include "mono/metadata/appdomain.h"
#include "mono/metadata/opcodes.h"
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
#if HAVE_BOEHM_GC
#include <gc/gc.h>
#endif

#define TEXT_OFFSET 512
#define CLI_H_SIZE 136
#define FILE_ALIGN 512
#define START_TEXT_RVA  0x00002000

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
	MonoBoolean init_locals;
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
string_heap_insert (MonoDynamicStream *sh, const char *str)
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
string_heap_init (MonoDynamicStream *sh)
{
	sh->index = 0;
	sh->alloc_size = 4096;
	sh->data = g_malloc (4096);
	sh->hash = g_hash_table_new (g_str_hash, g_str_equal);
	string_heap_insert (sh, "");
}

#if 0 /* never used */
static void
string_heap_free (MonoDynamicStream *sh)
{
	g_free (sh->data);
	g_hash_table_foreach (sh->hash, (GHFunc)g_free, NULL);
	g_hash_table_destroy (sh->hash);
}
#endif

static guint32
mono_image_add_stream_data (MonoDynamicStream *stream, const char *data, guint32 len)
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
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
		mono_metadata_encode_value (type->type, p, &p);
		encode_type (assembly, type->data.type, p, &p);
		break;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
		mono_metadata_encode_value (type->type, p, &p);
		mono_metadata_encode_value (mono_image_typedef_or_ref (assembly, type), p, &p);
		break;
	case MONO_TYPE_ARRAY:
		mono_metadata_encode_value (type->type, p, &p);
		encode_type (assembly, type->data.array->type, p, &p);
		mono_metadata_encode_value (type->data.array->rank, p, &p);
		mono_metadata_encode_value (0, p, &p); /* FIXME: set to 0 for now */
		mono_metadata_encode_value (0, p, &p);
		break;
	default:
		g_error ("need to encode type %x", type->type);
	}
	*endbuf = p;
}

static void
encode_reflection_type (MonoDynamicAssembly *assembly, MonoReflectionType *type, char *p, char **endbuf)
{
	if (!type) {
		mono_metadata_encode_value (MONO_TYPE_VOID, p, endbuf);
		return;
	}
	if (type->type) {
		encode_type (assembly, type->type, p, endbuf);
		return;
	}

	g_assert_not_reached ();

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
	g_assert (p - buf < size);
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
	g_assert (p - buf < size);
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
	guint32 idx, sig_idx, size;
	guint nl = mono_array_length (ilgen->locals);
	char *buf;
	char blob_size [6];
	char *b = blob_size;
	int i;

	size = 10 + nl * 10;
	p = buf = g_malloc (size);
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
	g_assert (p - buf < size);
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
		/* add to the fixup todo list */
		if (mb->ilgen && mb->ilgen->num_token_fixups)
			mono_g_hash_table_insert (assembly->token_fixups, mb->ilgen, GUINT_TO_POINTER (idx + 1));
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
	if (mb->init_locals)
		fat_flags |= METHOD_HEADER_INIT_LOCALS;
	fat_header [0] = fat_flags;
	fat_header [1] = (header_size / 4 ) << 4;
	shortp = (guint16*)(fat_header + 2);
	*shortp = GUINT16_TO_LE (max_stack);
	intp = (guint32*)(fat_header + 4);
	*intp = GUINT32_TO_LE (code_size);
	intp = (guint32*)(fat_header + 8);
	*intp = GUINT32_TO_LE (local_sig);
	idx = mono_image_add_stream_data (&assembly->code, fat_header, 12);
	/* add to the fixup todo list */
	if (mb->ilgen && mb->ilgen->num_token_fixups)
		mono_g_hash_table_insert (assembly->token_fixups, mb->ilgen, GUINT_TO_POINTER (idx + 12));
	
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
				int finally_start = ex_info->start + ex_info->len;
				for (j = 0; j < mono_array_length (ex_info->handlers); ++j) {
					ex_block = (MonoILExceptionBlock*)mono_array_addr (ex_info->handlers, MonoILExceptionBlock, j);
					clause.flags = GUINT32_TO_LE (ex_block->type);
					clause.try_offset = GUINT32_TO_LE (ex_info->start);
					/* need fault, too, probably */
					if (ex_block->type == MONO_EXCEPTION_CLAUSE_FINALLY)
						clause.try_len = GUINT32_TO_LE (finally_start - ex_info->start);
					else
						clause.try_len = GUINT32_TO_LE (ex_info->len);
					clause.handler_offset = GUINT32_TO_LE (ex_block->start);
					clause.handler_len = GUINT32_TO_LE (ex_block->len);
					finally_start = clause.handler_offset + clause.handler_len;
					clause.token_or_filter = ex_block->extype ? mono_metadata_token_from_dor (
							mono_image_typedef_or_ref (assembly, ex_block->extype->type)): 0;
					clause.token_or_filter = GUINT32_TO_LE (clause.token_or_filter);
					/*g_print ("out clause %d: from %d len=%d, handler at %d, %d, finally_start=%d, ex_info->start=%d, ex_info->len=%d, ex_block->type=%d, j=%d, i=%d\n", 
							clause.flags, clause.try_offset, clause.try_len, clause.handler_offset, clause.handler_len, finally_start, ex_info->start, ex_info->len, ex_block->type, j, i);*/
					mono_image_add_stream_data (&assembly->code, (char*)&clause, sizeof (clause));
				}
			} else {
				g_error ("No clauses for ex info block %d", i);
			}
		}
	}
	return assembly->text_rva + idx + CLI_H_SIZE;
}

static guint32
find_index_in_table (MonoDynamicAssembly *assembly, int table_idx, int col, guint32 token)
{
	int i;
	MonoDynamicTable *table;
	guint32 *values;
	
	table = &assembly->tables [table_idx];

	g_assert (col < table->columns);

	values = table->values + table->columns;
	for (i = 1; i <= table->rows; ++i) {
		if (values [col] == token)
			return i;
	}
	return 0;
}

/*
 * idx is the table index of the object
 * type is one of CUSTOM_ATTR_*
 */
static void
mono_image_add_cattrs (MonoDynamicAssembly *assembly, guint32 idx, guint32 type, MonoArray *cattrs)
{
	MonoDynamicTable *table;
	MonoReflectionCustomAttr *cattr;
	guint32 *values;
	guint32 count, i, token;
	char blob_size [6];
	char *p = blob_size;
	
	/* it is legal to pass a NULL cattrs: we avoid to use the if in a lot of places */
	if (!cattrs)
		return;
	count = mono_array_length (cattrs);
	table = &assembly->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	table->rows += count;
	alloc_table (table, table->rows);
	values = table->values + table->next_idx * MONO_CUSTOM_ATTR_SIZE;
	idx <<= CUSTOM_ATTR_BITS;
	idx |= type;
	for (i = 0; i < count; ++i) {
		cattr = (MonoReflectionCustomAttr*)mono_array_get (cattrs, gpointer, i);
		values [MONO_CUSTOM_ATTR_PARENT] = idx;
		token = mono_image_create_token (assembly, (MonoObject*)cattr->ctor);
		type = mono_metadata_token_index (token);
		type <<= CUSTOM_ATTR_TYPE_BITS;
		switch (mono_metadata_token_table (token)) {
		case MONO_TABLE_METHOD:
			type |= CUSTOM_ATTR_TYPE_METHODDEF;
			break;
		case MONO_TABLE_MEMBERREF:
			type |= CUSTOM_ATTR_TYPE_MEMBERREF;
			break;
		default:
			g_error ("got wrong token in custom attr");
		}
		values [MONO_CUSTOM_ATTR_TYPE] = type;
		p = blob_size;
		mono_metadata_encode_value (mono_array_length (cattr->data), p, &p);
		values [MONO_CUSTOM_ATTR_VALUE] = mono_image_add_stream_data (&assembly->blob, blob_size, p - blob_size);
		mono_image_add_stream_data (&assembly->blob,
			mono_array_addr (cattr->data, char, 0), mono_array_length (cattr->data));
		values += MONO_CUSTOM_ATTR_SIZE;
		++table->next_idx;
	}
}

/*
 * Fill in the MethodDef and ParamDef tables for a method.
 * This is used for both normal methods and constructors.
 */
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
				mono_image_add_cattrs (assembly, pb->table_idx, CUSTOM_ATTR_PARAMDEF, pb->cattrs);
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
	rmb.init_locals = mb->init_locals;

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
	mono_image_add_cattrs (assembly, mb->table_idx, CUSTOM_ATTR_METHODDEF, mb->cattrs);
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
	rmb.init_locals = mb->init_locals;

	mono_image_basic_method (&rmb, assembly);
	mono_image_add_cattrs (assembly, mb->table_idx, CUSTOM_ATTR_METHODDEF, mb->cattrs);

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

/*
 * Copy len * nelem bytes from val to dest, swapping bytes to LE if necessary.
 * dest may be misaligned.
 */
static void
swap_with_size (char *dest, const char* val, int len, int nelem) {
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	int elem;

	for (elem = 0; elem < nelem; ++elem) {
		switch (len) {
		case 1:
			*dest = *val;
			break;
		case 2:
			dest [0] = val [1];
			dest [1] = val [0];
			break;
		case 4:
			dest [0] = val [3];
			dest [1] = val [2];
			dest [2] = val [1];
			dest [3] = val [0];
			break;
		case 8:
			dest [0] = val [7];
			dest [1] = val [6];
			dest [2] = val [5];
			dest [3] = val [4];
			dest [4] = val [3];
			dest [5] = val [2];
			dest [6] = val [1];
			dest [7] = val [0];
			break;
		default:
			g_assert_not_reached ();
		}
		dest += len;
		val += len;
	}
#else
	memcpy (dest, val, len * nelem);
#endif
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
handle_enum:
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
	case MONO_TYPE_VALUETYPE:
		if (val->vtable->klass->enumtype) {
			*ret_type = val->vtable->klass->enum_basetype->type;
			goto handle_enum;
		} else
			g_error ("we can't encode valuetypes");
	case MONO_TYPE_STRING: {
		MonoString *str = (MonoString*)val;
		/* there is no signature */
		len = str->length * 2;
		mono_metadata_encode_value (len, b, &b);
		idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
		{
			char *swapped = g_malloc (2 * mono_string_length (str));
			const char *p = (const char*)mono_string_chars (str);

			swap_with_size (swapped, p, 2, mono_string_length (str));
			mono_image_add_stream_data (&assembly->blob, swapped, len);
			g_free (swapped);
		}
#else
		mono_image_add_stream_data (&assembly->blob, (const char*)mono_string_chars (str), len);
#endif

		g_free (buf);
		return idx;
	}
	default:
		g_error ("we don't encode constant type 0x%02x yet", *ret_type);
	}

	/* there is no signature */
	mono_metadata_encode_value (len, b, &b);
	idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	swap_with_size (blob_size, val, len, 1);
	mono_image_add_stream_data (&assembly->blob, blob_size, len);
#else
	mono_image_add_stream_data (&assembly->blob, box_val, len);
#endif

	g_free (buf);
	return idx;
}

static void
mono_image_get_field_info (MonoReflectionFieldBuilder *fb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	char *name;

	/* maybe this fixup should be done in the C# code */
	if (fb->attrs & FIELD_ATTRIBUTE_LITERAL)
		fb->attrs |= FIELD_ATTRIBUTE_HAS_DEFAULT;
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
	if (fb->rva_data) {
		guint32 rva_idx;
		table = &assembly->tables [MONO_TABLE_FIELDRVA];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_FIELD_RVA_SIZE;
		values [MONO_FIELD_RVA_FIELD] = fb->table_idx;
		/*
		 * We store it in the code section because it's simpler for now.
		 */
		rva_idx = mono_image_add_stream_data (&assembly->code, mono_array_addr (fb->rva_data, char, 0), mono_array_length (fb->rva_data));
		values [MONO_FIELD_RVA_RVA] = rva_idx + assembly->text_rva + CLI_H_SIZE;
	}
	mono_image_add_cattrs (assembly, fb->table_idx, CUSTOM_ATTR_FIELDDEF, fb->cattrs);
}

static guint32
property_encode_signature (MonoDynamicAssembly *assembly, MonoReflectionPropertyBuilder *fb)
{
	char *buf, *p;
	char blob_size [6];
	char *b = blob_size;
	guint32 nparams = 0;
	MonoReflectionMethodBuilder *mb = fb->get_method;
	guint32 idx, i, size;

	if (mb && mb->parameters)
		nparams = mono_array_length (mb->parameters);
	size = 24 + nparams * 10;
	buf = p = g_malloc (size);
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
	g_assert (p - buf < size);
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
	values = table->values + pb->table_idx * MONO_PROPERTY_SIZE;
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
	mono_image_add_cattrs (assembly, pb->table_idx, CUSTOM_ATTR_PROPERTY, pb->cattrs);
}

static void
mono_image_get_event_info (MonoReflectionEventBuilder *eb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	char *name;
	guint num_methods = 0;
	guint32 semaidx;

	/* 
	 * we need to set things in the following tables:
	 * EVENTMAP (info already filled in _get_type_info ())
	 * EVENT    (rows already preallocated in _get_type_info ())
	 * METHOD      (method info already done with the generic method code)
	 * METHODSEMANTICS
	 */
	table = &assembly->tables [MONO_TABLE_EVENT];
	eb->table_idx = table->next_idx ++;
	values = table->values + eb->table_idx * MONO_EVENT_SIZE;
	name = mono_string_to_utf8 (eb->name);
	values [MONO_EVENT_NAME] = string_heap_insert (&assembly->sheap, name);
	g_free (name);
	values [MONO_EVENT_FLAGS] = eb->attrs;
	values [MONO_EVENT_TYPE] = mono_image_typedef_or_ref (assembly, eb->type->type);

	/*
	 * FIXME: we still don't handle 'other' methods 
	 */
	if (eb->add_method) num_methods ++;
	if (eb->remove_method) num_methods ++;
	if (eb->raise_method) num_methods ++;

	table = &assembly->tables [MONO_TABLE_METHODSEMANTICS];
	table->rows += num_methods;
	alloc_table (table, table->rows);

	if (eb->add_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_ADD_ON;
		values [MONO_METHOD_SEMA_METHOD] = eb->add_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (eb->table_idx << HAS_SEMANTICS_BITS) | HAS_SEMANTICS_EVENT;
	}
	if (eb->remove_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_REMOVE_ON;
		values [MONO_METHOD_SEMA_METHOD] = eb->remove_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (eb->table_idx << HAS_SEMANTICS_BITS) | HAS_SEMANTICS_EVENT;
	}
	if (eb->raise_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_FIRE;
		values [MONO_METHOD_SEMA_METHOD] = eb->raise_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (eb->table_idx << HAS_SEMANTICS_BITS) | HAS_SEMANTICS_EVENT;
	}
	mono_image_add_cattrs (assembly, eb->table_idx, CUSTOM_ATTR_EVENT, eb->cattrs);
}

static guint32
resolution_scope_from_image (MonoDynamicAssembly *assembly, MonoImage *image)
{
	MonoDynamicTable *table;
	guint32 token;
	guint32 *values;
	guint32 cols [MONO_ASSEMBLY_SIZE];

	if ((token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, image))))
		return token;

	mono_metadata_decode_row (&image->tables [MONO_TABLE_ASSEMBLY], 0, cols, MONO_ASSEMBLY_SIZE);

	table = &assembly->tables [MONO_TABLE_ASSEMBLYREF];
	token = table->next_idx ++;
	table->rows ++;
	alloc_table (table, table->rows);
	values = table->values + token * MONO_ASSEMBLYREF_SIZE;
	if (strcmp ("corlib", image->assembly_name) == 0)
		values [MONO_ASSEMBLYREF_NAME] = string_heap_insert (&assembly->sheap, "mscorlib");
	else
		values [MONO_ASSEMBLYREF_NAME] = string_heap_insert (&assembly->sheap, image->assembly_name);
	values [MONO_ASSEMBLYREF_MAJOR_VERSION] = cols [MONO_ASSEMBLY_MAJOR_VERSION];
	values [MONO_ASSEMBLYREF_MINOR_VERSION] = cols [MONO_ASSEMBLY_MINOR_VERSION];
	values [MONO_ASSEMBLYREF_BUILD_NUMBER] = cols [MONO_ASSEMBLY_BUILD_NUMBER];
	values [MONO_ASSEMBLYREF_REV_NUMBER] = cols [MONO_ASSEMBLY_REV_NUMBER];
	values [MONO_ASSEMBLYREF_FLAGS] = 0;
	values [MONO_ASSEMBLYREF_PUBLIC_KEY] = 0;
	values [MONO_ASSEMBLYREF_CULTURE] = 0;
	values [MONO_ASSEMBLYREF_HASH_VALUE] = 0;

	token <<= RESOLTION_SCOPE_BITS;
	token |= RESOLTION_SCOPE_ASSEMBLYREF;
	g_hash_table_insert (assembly->handleref, image, GUINT_TO_POINTER (token));
	return token;
}

static guint32
create_typespec (MonoDynamicAssembly *assembly, MonoType *type)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 token;
	char sig [128];
	char *p = sig;
	char blob_size [6];
	char *b = blob_size;

	switch (type->type) {
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		encode_type (assembly, type, p, &p);
		break;
	default:
		return 0;
	}
	
	g_assert (p-sig < 128);
	mono_metadata_encode_value (p-sig, b, &b);
	token = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	mono_image_add_stream_data (&assembly->blob, sig, p-sig);

	table = &assembly->tables [MONO_TABLE_TYPESPEC];
	alloc_table (table, table->rows + 1);
	values = table->values + table->next_idx * MONO_TYPESPEC_SIZE;
	values [MONO_TYPESPEC_SIGNATURE] = token;

	token = TYPEDEFORREF_TYPESPEC | (table->next_idx << TYPEDEFORREF_BITS);
	g_hash_table_insert (assembly->typeref, type, GUINT_TO_POINTER(token));
	table->next_idx ++;
	return token;
}

/*
 * Despite the name, we handle also TypeSpec (with the above helper).
 */
static guint32
mono_image_typedef_or_ref (MonoDynamicAssembly *assembly, MonoType *type)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 token;
	MonoClass *klass;

	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->typeref, type));
	if (token)
		return token;
	token = create_typespec (assembly, type);
	if (token)
		return token;
	klass = mono_class_from_mono_type (type);
	/*
	 * If it's in the same module:
	 */
	if (klass->image == assembly->assembly.image) {
		MonoReflectionTypeBuilder *tb = klass->reflection_info;
		return TYPEDEFORREF_TYPEDEF | (tb->table_idx << TYPEDEFORREF_BITS);
	}

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

/*
 * Insert a memberef row into the metadata: the token that point to the memberref
 * is returned. Caching is done in the caller (mono_image_get_methodref_token() or
 * mono_image_get_fieldref_token()).
 * The sig param is an index to an already built signature.
 */
static guint32
mono_image_get_memberref_token (MonoDynamicAssembly *assembly, MonoClass *klass, const char *name, guint32 sig)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 token, pclass;
	guint32 parent;

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
	guint32 token;
	
	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, method));
	if (token)
		return token;
	token = mono_image_get_memberref_token (assembly, method->klass, 
		method->name,  method_encode_signature (assembly, method->signature));
	g_hash_table_insert (assembly->handleref, method, GUINT_TO_POINTER(token));
	return token;
}

static guint32
mono_image_get_fieldref_token (MonoDynamicAssembly *assembly, MonoClassField *field, MonoClass *klass)
{
	guint32 token;
	
	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, field));
	if (token)
		return token;
	token = mono_image_get_memberref_token (assembly, klass, 
		field->name,  fieldref_encode_signature (assembly, field));
	g_hash_table_insert (assembly->handleref, field, GUINT_TO_POINTER(token));
	return token;
}

/*
 * Insert into the metadata tables all the info about the TypeBuilder tb.
 * Data in the tables is inserted in a predefined order, since some tables need to be sorted.
 */
static void
mono_image_get_type_info (MonoDomain *domain, MonoReflectionTypeBuilder *tb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;
	guint *values;
	int i;
	char *n;

	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	values = table->values + tb->table_idx * MONO_TYPEDEF_SIZE;
	values [MONO_TYPEDEF_FLAGS] = tb->attrs;
	if (tb->parent) { /* interfaces don't have a parent */
		values [MONO_TYPEDEF_EXTENDS] = mono_image_typedef_or_ref (assembly, tb->parent->type);
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
	 * if we have explicitlayout or sequentiallayouts, output data in the
	 * ClassLayout table.
	 */
	if ((tb->attrs & TYPE_ATTRIBUTE_LAYOUT_MASK) != TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		table = &assembly->tables [MONO_TABLE_CLASSLAYOUT];
		table->rows++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_CLASS_LAYOUT_SIZE;
		values [MONO_CLASS_LAYOUT_PARENT] = tb->table_idx;
		values [MONO_CLASS_LAYOUT_CLASS_SIZE] = tb->class_size;
		values [MONO_CLASS_LAYOUT_PACKING_SIZE] = tb->packing_size;
	}

	/* handle interfaces */
	if (tb->interfaces) {
		table = &assembly->tables [MONO_TABLE_INTERFACEIMPL];
		i = table->rows;
		table->rows += mono_array_length (tb->interfaces);
		alloc_table (table, table->rows);
		values = table->values + (i + 1) * MONO_INTERFACEIMPL_SIZE;
		for (i = 0; i < mono_array_length (tb->interfaces); ++i) {
			MonoReflectionType* iface = (MonoReflectionType*) mono_array_get (tb->interfaces, gpointer, i);
			values [MONO_INTERFACEIMPL_CLASS] = tb->table_idx;
			values [MONO_INTERFACEIMPL_INTERFACE] = mono_image_typedef_or_ref (assembly, iface->type);
			values += MONO_INTERFACEIMPL_SIZE;
		}
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

	/* Do the same with properties etc.. */
	if (tb->events && mono_array_length (tb->events)) {
		table = &assembly->tables [MONO_TABLE_EVENT];
		table->rows += mono_array_length (tb->events);
		alloc_table (table, table->rows);
		table = &assembly->tables [MONO_TABLE_EVENTMAP];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_EVENT_MAP_SIZE;
		values [MONO_EVENT_MAP_PARENT] = tb->table_idx;
		values [MONO_EVENT_MAP_EVENTLIST] = assembly->tables [MONO_TABLE_EVENT].next_idx;
		for (i = 0; i < mono_array_length (tb->events); ++i)
			mono_image_get_event_info (
				mono_array_get (tb->events, MonoReflectionEventBuilder*, i), assembly);
	}
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
	mono_image_add_cattrs (assembly, tb->table_idx, CUSTOM_ATTR_TYPEDEF, tb->cattrs);
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
	i = mono_image_add_stream_data (&assembly->guid, mono_array_addr (mb->guid, char, 0), 16);
	i /= 16;
	++i;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_MVID] = i;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_ENC] = 0;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_ENCBASE] = 0;

	mono_image_add_cattrs (assembly, mb->table_idx, CUSTOM_ATTR_MODULE, mb->cattrs);
	/*
	 * fill-in info in other tables as well.
	 */
	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	table->rows += mono_array_length (mb->types);
	alloc_table (table, table->rows);
	/*
	 * We assign here the typedef indexes to avoid mismatches if a type that
	 * has not yet been stored in the tables is referenced by another type.
	 */
	for (i = 0; i < mono_array_length (mb->types); ++i) {
		int j;
		MonoReflectionTypeBuilder *type = mono_array_get (mb->types, MonoReflectionTypeBuilder*, i);
		type->table_idx = table->next_idx ++;
		if (!type->subtypes)
			continue;
		for (j = 0; j < mono_array_length (type->subtypes); ++j) {
			MonoReflectionTypeBuilder *subtype = mono_array_get (type->subtypes, MonoReflectionTypeBuilder*, j);
			subtype->table_idx = table->next_idx ++;
		}
	}
	for (i = 0; i < mono_array_length (mb->types); ++i)
		mono_image_get_type_info (domain, mono_array_get (mb->types, MonoReflectionTypeBuilder*, i), assembly);
}

#define align_pointer(base,p)\
	do {\
		guint32 __diff = (unsigned char*)(p)-(unsigned char*)(base);\
		if (__diff & 3)\
			(p) += 4 - (__diff & 3);\
	} while (0)

static int
compare_semantics (const void *a, const void *b)
{
	const guint32 *a_values = a;
	const guint32 *b_values = b;
	int assoc = a_values [MONO_METHOD_SEMA_ASSOCIATION] - b_values [MONO_METHOD_SEMA_ASSOCIATION];
	if (assoc)
		return assoc;
	return a_values [MONO_METHOD_SEMA_SEMANTICS] - b_values [MONO_METHOD_SEMA_SEMANTICS];
}

static int
compare_custom_attrs (const void *a, const void *b)
{
	const guint32 *a_values = a;
	const guint32 *b_values = b;

	return a_values [MONO_CUSTOM_ATTR_PARENT] - b_values [MONO_CUSTOM_ATTR_PARENT];
}

/*
 * build_compressed_metadata() fills in the blob of data that represents the 
 * raw metadata as it will be saved in the PE file. The five streams are output 
 * and the metadata tables are comnpressed from the guint32 array representation, 
 * to the compressed on-disk format.
 */
static void
build_compressed_metadata (MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;
	int i;
	guint64 valid_mask = 0;
	guint64 sorted_mask;
	guint32 heapt_size = 0;
	guint32 meta_size = 256; /* allow for header and other stuff */
	guint32 table_offset;
	guint32 ntables = 0;
	guint64 *int64val;
	guint32 *int32val;
	guint16 *int16val;
	MonoImage *meta;
	unsigned char *p;
	const char *version = "mono" VERSION;
	struct StreamDesc {
		const char *name;
		MonoDynamicStream *stream;
	} stream_desc [] = {
		{"#~", &assembly->tstream},
		{"#Strings", &assembly->sheap},
		{"#US", &assembly->us},
		{"#Blob", &assembly->blob},
		{"#GUID", &assembly->guid}
	};
	
	/* tables that are sorted */
	sorted_mask = ((guint64)1 << MONO_TABLE_CONSTANT) | ((guint64)1 << MONO_TABLE_FIELDMARSHAL)
		| ((guint64)1 << MONO_TABLE_METHODSEMANTICS) | ((guint64)1 << MONO_TABLE_CLASSLAYOUT)
		| ((guint64)1 << MONO_TABLE_FIELDLAYOUT) | ((guint64)1 << MONO_TABLE_FIELDRVA)
		| ((guint64)1 << MONO_TABLE_IMPLMAP) | ((guint64)1 << MONO_TABLE_NESTEDCLASS)
		| ((guint64)1 << MONO_TABLE_METHODIMPL) | ((guint64)1 << MONO_TABLE_CUSTOMATTRIBUTE)
		| ((guint64)1 << MONO_TABLE_DECLSECURITY);
	
	/* Compute table sizes */
	/* the MonoImage has already been created in mono_image_basic_init() */
	meta = assembly->assembly.image;
	
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
	*int16val++ = GUINT16_TO_LE (1);
	*int16val = GUINT16_TO_LE (1);
	p += 8;
	/* version string */
	int32val = (guint32*)p;
	*int32val = GUINT32_TO_LE (strlen (version));
	p += 4;
	memcpy (p, version, GUINT32_FROM_LE (*int32val));
	p += GUINT32_FROM_LE (*int32val);
	align_pointer (meta->raw_metadata, p);
	int16val = (guint16*)p;
	*int16val++ = GUINT16_TO_LE (0); /* flags must be 0 */
	*int16val = GUINT16_TO_LE (5); /* number of streams */
	p += 4;

	/*
	 * write the stream info.
	 */
	table_offset = (p - (unsigned char*)meta->raw_metadata) + 5 * 8 + 40; /* room needed for stream headers */
	table_offset += 3; table_offset &= ~3;

	assembly->tstream.index = heapt_size;
	for (i = 0; i < 5; ++i) {
		int32val = (guint32*)p;
		stream_desc [i].stream->offset = table_offset;
		*int32val++ = GUINT32_TO_LE (table_offset);
		*int32val = GUINT32_TO_LE (stream_desc [i].stream->index);
		table_offset += GUINT32_FROM_LE (*int32val);
		table_offset += 3; table_offset &= ~3;
		p += 8;
		strcpy (p, stream_desc [i].name);
		p += strlen (stream_desc [i].name) + 1;
		align_pointer (meta->raw_metadata, p);
	}
	/* 
	 * now copy the data, the table stream header and contents goes first.
	 */
	g_assert ((p - (unsigned char*)meta->raw_metadata) < assembly->tstream.offset);
	p = meta->raw_metadata + assembly->tstream.offset;
	int32val = (guint32*)p;
	*int32val = GUINT32_TO_LE (0); /* reserved */
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
	*int64val++ = GUINT64_TO_LE (valid_mask);
	*int64val++ = GUINT64_TO_LE (valid_mask & sorted_mask); /* bitvector of sorted tables  */
	p += 16;
	int32val = (guint32*)p;
	for (i = 0; i < 64; i++){
		if (meta->tables [i].rows == 0)
			continue;
		*int32val++ = GUINT32_TO_LE (meta->tables [i].rows);
	}
	p = (unsigned char*)int32val;

	/* sort the tables that still need sorting */
	table = &assembly->tables [MONO_TABLE_METHODSEMANTICS];
	if (table->rows)
		qsort (table->values + MONO_METHOD_SEMA_SIZE, table->rows, sizeof (guint32) * MONO_METHOD_SEMA_SIZE, compare_semantics);
	table = &assembly->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	if (table->rows)
		qsort (table->values + MONO_CUSTOM_ATTR_SIZE, table->rows, sizeof (guint32) * MONO_CUSTOM_ATTR_SIZE, compare_custom_attrs);

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
					*int16val = GUINT16_TO_LE (values [col]);
					p += 2;
					break;
				case 4:
					int32val = (guint32*)p;
					*int32val = GUINT32_TO_LE (values [col]);
					p += 4;
					break;
				default:
					g_assert_not_reached ();
				}
			}
		}
		g_assert ((p - (const unsigned char*)meta->tables [i].base) == (meta->tables [i].rows * meta->tables [i].row_size));
	}
	
	g_assert (assembly->guid.offset + assembly->guid.index < meta_size);
	memcpy (meta->raw_metadata + assembly->sheap.offset, assembly->sheap.data, assembly->sheap.index);
	memcpy (meta->raw_metadata + assembly->us.offset, assembly->us.data, assembly->us.index);
	memcpy (meta->raw_metadata + assembly->blob.offset, assembly->blob.data, assembly->blob.index);
	memcpy (meta->raw_metadata + assembly->guid.offset, assembly->guid.data, assembly->guid.index);

	assembly->meta_size = assembly->guid.offset + assembly->guid.index;
}

/*
 * Some tables in metadata need to be sorted according to some criteria, but
 * when methods and fields are first created with reflection, they may be assigned a token
 * that doesn't correspond to the final token they will get assigned after the sorting.
 * ILGenerator.cs keeps a fixup table that maps the position of tokens in the IL code stream
 * with the reflection objects that represent them. Once all the tables are set up, the 
 * reflection objects will contains the correct table index. fixup_method() will fixup the
 * tokens for the method with ILGenerator @ilgen.
 */
static void
fixup_method (MonoReflectionILGen *ilgen, gpointer value, MonoDynamicAssembly *assembly) {
	guint32 code_idx = GPOINTER_TO_UINT (value);
	MonoReflectionILTokenInfo *iltoken;
	MonoReflectionFieldBuilder *field;
	MonoReflectionCtorBuilder *ctor;
	MonoReflectionMethodBuilder *method;
	guint32 i, idx;
	unsigned char *target;

	for (i = 0; i < ilgen->num_token_fixups; ++i) {
		iltoken = (MonoReflectionILTokenInfo *)mono_array_addr_with_size (ilgen->token_fixups, sizeof (MonoReflectionILTokenInfo), i);
		target = assembly->code.data + code_idx + iltoken->code_pos;
		switch (target [3]) {
		case MONO_TABLE_FIELD:
			if (strcmp (iltoken->member->vtable->klass->name, "FieldBuilder"))
				g_assert_not_reached ();
			field = (MonoReflectionFieldBuilder *)iltoken->member;
			idx = field->table_idx;
			break;
		case MONO_TABLE_METHOD:
			if (!strcmp (iltoken->member->vtable->klass->name, "MethodBuilder")) {
				method = (MonoReflectionMethodBuilder *)iltoken->member;
				idx = method->table_idx;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "ConstructorBuilder")) {
				ctor = (MonoReflectionCtorBuilder *)iltoken->member;
				idx = ctor->table_idx;
			} else {
				g_assert_not_reached ();
			}
			break;
		default:
			g_error ("got unexpected table 0x%02x in fixup", target [3]);
		}
		target [0] = idx & 0xff;
		target [1] = (idx >> 8) & 0xff;
		target [2] = (idx >> 16) & 0xff;
	}
}

/*
 * mono_image_build_metadata() will fill the info in all the needed metadata tables
 * for the AssemblyBuilder @assemblyb: it iterates over the assembly modules
 * and recursively outputs the info for a module. Each module will output all the info
 * about it's types etc.
 * At the end of the process, method and field tokens are fixed up and the on-disk
 * compressed metadata representation is created.
 */
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
	
	assembly->text_rva = START_TEXT_RVA;

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
	values [MONO_ASSEMBLY_FLAGS] = 0;

	mono_image_add_cattrs (assembly, 1, CUSTOM_ATTR_ASSEMBLY, assemblyb->cattrs);

	assembly->tables [MONO_TABLE_TYPEDEF].rows = 1; /* .<Module> */
	assembly->tables [MONO_TABLE_TYPEDEF].next_idx++;

	if (assemblyb->modules) {
		len = mono_array_length (assemblyb->modules);
		table = &assembly->tables [MONO_TABLE_MODULE];
		alloc_table (table, len);
		for (i = 0; i < len; ++i)
			mono_image_fill_module_table (domain, mono_array_get (assemblyb->modules, MonoReflectionModuleBuilder*, i), assembly);
	} else {
		table = &assembly->tables [MONO_TABLE_MODULE];
		table->rows++;
		alloc_table (table, table->rows);
		table->values [table->next_idx * MONO_MODULE_SIZE + MONO_MODULE_NAME] = string_heap_insert (&assembly->sheap, "RefEmit_YouForgotToDefineAModule");
		table->next_idx ++;
	}

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

	/* fixup tokens */
	mono_g_hash_table_foreach (assembly->token_fixups, (GHFunc)fixup_method, assembly);
	
	build_compressed_metadata (assembly);
}

/*
 * mono_image_insert_string:
 * @assembly: assembly builder object
 * @str: a string
 *
 * Insert @str into the user string stream of @assembly.
 */
guint32
mono_image_insert_string (MonoReflectionAssemblyBuilder *assembly, MonoString *str)
{
	guint32 idx;
	char buf [16];
	char *b = buf;
	
	if (!assembly->dynamic_assembly)
		mono_image_basic_init (assembly);
	mono_metadata_encode_value (1 | (str->length * 2), b, &b);
	idx = mono_image_add_stream_data (&assembly->dynamic_assembly->us, buf, b-buf);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	{
		char *swapped = g_malloc (2 * mono_string_length (str));
		const char *p = (const char*)mono_string_chars (str);

		swap_with_size (swapped, p, 2, mono_string_length (str));
		mono_image_add_stream_data (&assembly->dynamic_assembly->us, swapped, str->length * 2);
		g_free (swapped);
	}
#else
	mono_image_add_stream_data (&assembly->dynamic_assembly->us, (const char*)mono_string_chars (str), str->length * 2);
#endif
	mono_image_add_stream_data (&assembly->dynamic_assembly->us, "", 1);
	return MONO_TOKEN_STRING | idx;
}

/*
 * mono_image_create_token:
 * @assembly: a dynamic assembly
 * @obj:
 *
 * Get a token to insert in the IL code stream for the given MemberInfo.
 * @obj can be one of:
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
mono_image_create_token (MonoDynamicAssembly *assembly, MonoObject *obj)
{
	MonoClass *klass;
	guint32 token;

	if (!obj)
		g_error ("System.Array methods not yet supported");
	
	klass = obj->vtable->klass;
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
			mono_image_typedef_or_ref (assembly, tb->type));
	}
	if (strcmp (klass->name, "MonoCMethod") == 0 ||
			strcmp (klass->name, "MonoMethod") == 0) {
		MonoReflectionMethod *m = (MonoReflectionMethod *)obj;
		token = mono_image_get_methodref_token (assembly, m->method);
		/*g_print ("got token 0x%08x for %s\n", token, m->method->name);*/
		return token;
	}
	if (strcmp (klass->name, "MonoField") == 0) {
		MonoReflectionField *f = (MonoReflectionField *)obj;
		token = mono_image_get_fieldref_token (assembly, f->field, f->klass);
		/*g_print ("got token 0x%08x for %s\n", token, f->field->name);*/
		return token;
	}
	g_print ("requested token for %s\n", klass->name);
	return 0;
}

/*
 * mono_image_basic_ini:
 * @assembly: an assembly builder object
 *
 * Create the MonoImage that represents the assembly builder and setup some
 * of the helper hash table and the basic metadata streams.
 */
void
mono_image_basic_init (MonoReflectionAssemblyBuilder *assemblyb)
{
	MonoDynamicAssembly *assembly;
	MonoImage *image;
	int i;
	
	if (assemblyb->dynamic_assembly)
		return;

#if HAVE_BOEHM_GC
	assembly = assemblyb->dynamic_assembly = GC_malloc (sizeof (MonoDynamicAssembly));
#else
	assembly = assemblyb->dynamic_assembly = g_new0 (MonoDynamicAssembly, 1);
#endif

	assembly->token_fixups = mono_g_hash_table_new (g_direct_hash, g_direct_equal);
	assembly->handleref = g_hash_table_new (g_direct_hash, g_direct_equal);
	assembly->typeref = g_hash_table_new (mono_metadata_type_hash, mono_metadata_type_equal);

	string_heap_init (&assembly->sheap);
	mono_image_add_stream_data (&assembly->us, "", 1);
	mono_image_add_stream_data (&assembly->blob, "", 1);

	for (i=0; i < 64; ++i) {
		assembly->tables [i].next_idx = 1;
		assembly->tables [i].columns = table_sizes [i];
	}

	image = g_new0 (MonoImage, 1);
	
	/* keep in sync with image.c */
	image->name = mono_string_to_utf8 (assemblyb->name);
	image->assembly_name = image->name; /* they may be different */

	image->method_cache = g_hash_table_new (g_direct_hash, g_direct_equal);
	image->class_cache = g_hash_table_new (g_direct_hash, g_direct_equal);
	image->name_cache = g_hash_table_new (g_str_hash, g_str_equal);
	image->array_cache = g_hash_table_new (g_direct_hash, g_direct_equal);

	assembly->assembly.image = image;
	
}

typedef struct {
	guint32 import_lookup_table;
	guint32 timestamp;
	guint32 forwarder;
	guint32 name_rva;
	guint32 import_address_table_rva;
} MonoIDT;

typedef struct {
	guint32 name_rva;
	guint32 flags;
} MonoILT;

/*
 * mono_image_get_heade:
 * @assemblyb: an assembly builder object
 * @buffer:
 * @maxsize
 * 
 * When we need to save an assembly, we first call this function that ensures the metadata 
 * tables are built for all the modules in the assembly. This function creates the PE-COFF
 * header, the image sections, the CLI header etc. The header is written in @buffer
 * and the length of the data written is returned.
 * If @buffer is not big enough (@maxsize), -1 is returned.
 */
int
mono_image_get_header (MonoReflectionAssemblyBuilder *assemblyb, char *buffer, int maxsize)
{
	MonoMSDOSHeader *msdos;
	MonoDotNetHeader *header;
	MonoSectionTable *section, *reloc;
	MonoCLIHeader *cli_header;
	guint32 header_size =  TEXT_OFFSET + CLI_H_SIZE;
	MonoDynamicAssembly *assembly;
	MonoIDT import_directory;
	MonoILT import_lookup;

	static const guchar entrycode [16] = {0xff, 0x25, 0};
	guint32 entry_offset, import_table_offset, import_hint_offset;
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

	memset (&import_directory, 0, sizeof (MonoIDT));
	assembly->code.index += 3;
	assembly->code.index &= ~3;
	import_table_offset = TEXT_OFFSET + CLI_H_SIZE + assembly->code.index;
	/* calc rva... */
	import_hint_offset = import_table_offset + 40 + 8;
	import_hint_offset += 15;
	import_hint_offset &= ~15;

	import_directory.import_lookup_table = import_table_offset + 40;
	import_directory.name_rva = import_hint_offset + 14;
	import_directory.import_address_table_rva = 0x200; /* FIXME */

	mono_image_add_stream_data (&assembly->code, (char*)&import_directory, sizeof (MonoIDT));
	/* add an empty item to mark the end */
	memset (&import_directory, 0, sizeof (MonoIDT));
	mono_image_add_stream_data (&assembly->code, (char*)&import_directory, sizeof (MonoIDT));
	
	memset (&import_lookup, 0, sizeof (MonoILT));
	import_lookup.name_rva = import_hint_offset;
	mono_image_add_stream_data (&assembly->code, (char*)&import_lookup, sizeof (MonoILT));
	assembly->code.index += 15;
	assembly->code.index &= ~15;
	mono_image_add_stream_data (&assembly->code, "", 1);
	mono_image_add_stream_data (&assembly->code, "", 1);
	mono_image_add_stream_data (&assembly->code, "_CorExeMain", 12);
	mono_image_add_stream_data (&assembly->code, "mscoree.dll", 12);
	mono_image_add_stream_data (&assembly->code, "", 1); /* lame */
	mono_image_add_stream_data (&assembly->code, "", 1);
	mono_image_add_stream_data (&assembly->code, "", 1);
	mono_image_add_stream_data (&assembly->code, "", 1);

	entry_offset = mono_image_add_stream_data (&assembly->code, entrycode, sizeof (entrycode));
	/*g_print ("ep offset: 0x%08x\n", entry_offset);*/
	
	memset (buffer, 0, header_size);
	memcpy (buffer, msheader, sizeof (MonoMSDOSHeader));

	msdos = (MonoMSDOSHeader *)buffer;
	header = (MonoDotNetHeader *)(buffer + sizeof (MonoMSDOSHeader));
	section = (MonoSectionTable*) (buffer + sizeof (MonoMSDOSHeader) + sizeof (MonoDotNetHeader));

	msdos->pe_offset = GUINT32_FROM_LE (sizeof (MonoMSDOSHeader));

	header->pesig [0] = 'P';
	header->pesig [1] = 'E';
	header->pesig [2] = header->pesig [3] = 0;

	header->coff.coff_machine = GUINT16_FROM_LE (0x14c);
	header->coff.coff_sections = GUINT16_FROM_LE (2); /* only .text and .reloc supported now */
	header->coff.coff_time = GUINT32_FROM_LE (time (NULL));
	header->coff.coff_opt_header_size = GUINT16_FROM_LE (sizeof (MonoDotNetHeader) - sizeof (MonoCOFFHeader) - 4);
	/* it's an exe */
	header->coff.coff_attributes = GUINT16_FROM_LE (0x010e);
	/* it's a dll */
	/*header->coff.coff_attributes = GUINT16_FROM_LE (0x210e); */
	header->pe.pe_magic = GUINT16_FROM_LE (0x10B);
	header->pe.pe_major = 6;
	header->pe.pe_minor = 0;
	/* set later: pe_code_size pe_data_size pe_rva_entry_point pe_rva_code_base pe_rva_data_base */

	header->nt.pe_image_base = GUINT32_FROM_LE (0x400000);
	header->nt.pe_section_align = GUINT32_FROM_LE (8192);
	header->nt.pe_file_alignment = GUINT32_FROM_LE (FILE_ALIGN);
	header->nt.pe_os_major = GUINT16_FROM_LE (4);
	header->nt.pe_os_minor = GUINT16_FROM_LE (0);
	header->nt.pe_subsys_major = GUINT16_FROM_LE (4);
	/* need to set pe_image_size, pe_header_size */
	header->nt.pe_header_size = GUINT32_FROM_LE (0x200);
	header->nt.pe_image_size = GUINT32_FROM_LE (0x00008000);
	header->nt.pe_subsys_required = GUINT16_FROM_LE (3); /* 3 -> cmdline app, 2 -> GUI app */
	header->nt.pe_stack_reserve = GUINT32_FROM_LE (0x00100000);
	header->nt.pe_stack_commit = GUINT32_FROM_LE (0x00001000);
	header->nt.pe_heap_reserve = GUINT32_FROM_LE (0x00100000);
	header->nt.pe_heap_commit = GUINT32_FROM_LE (0x00001000);
	header->nt.pe_loader_flags = GUINT32_FROM_LE (0);
	header->nt.pe_data_dir_count = GUINT32_FROM_LE (16);

#if 0
	/* set: */
	header->datadir.pe_import_table
	pe_resource_table
	pe_reloc_table
	pe_iat	
#endif
	header->datadir.pe_cli_header.size = GUINT32_FROM_LE (72);
	header->datadir.pe_cli_header.rva = GUINT32_FROM_LE (assembly->text_rva); /* we put it always at the beginning */

	/* Write section tables */
	strcpy (section->st_name, ".text");
	section->st_virtual_address = GUINT32_FROM_LE (assembly->text_rva);
	section->st_virtual_size = GUINT32_FROM_LE (assembly->meta_size + assembly->code.index);
	section->st_raw_data_size = GUINT32_FROM_LE (GUINT32_TO_LE (section->st_virtual_size) + (FILE_ALIGN - 1));
	section->st_raw_data_size &= GUINT32_FROM_LE (~(FILE_ALIGN - 1));
	section->st_raw_data_ptr = GUINT32_FROM_LE (TEXT_OFFSET);
	section->st_flags = GUINT32_FROM_LE (SECT_FLAGS_HAS_CODE | SECT_FLAGS_MEM_EXECUTE | SECT_FLAGS_MEM_READ);

	reloc = section + 1;
	strcpy (reloc->st_name, ".reloc");
	reloc->st_virtual_address = GUINT32_FROM_LE (GUINT32_TO_LE (section->st_virtual_address)
						     + GUINT32_TO_LE (section->st_raw_data_size));
	reloc->st_virtual_size = GUINT32_FROM_LE (12);
	reloc->st_raw_data_size = GUINT32_FROM_LE (GUINT32_TO_LE (reloc->st_virtual_size) + (FILE_ALIGN - 1));
	reloc->st_raw_data_size &= GUINT32_FROM_LE (~(FILE_ALIGN - 1));
	reloc->st_raw_data_ptr = GUINT32_FROM_LE (GUINT32_TO_LE (section->st_raw_data_ptr)
						  + GUINT32_TO_LE (section->st_raw_data_size));
	reloc->st_flags = GUINT32_FROM_LE (SECT_FLAGS_MEM_DISCARDABLE | SECT_FLAGS_HAS_INITIALIZED_DATA | SECT_FLAGS_MEM_READ);
	

	header->pe.pe_code_size = section->st_raw_data_size; /* already in native */
	header->pe.pe_rva_entry_point = GUINT32_FROM_LE (TEXT_OFFSET + CLI_H_SIZE + entry_offset);

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
	cli_header->ch_size = GUINT32_FROM_LE (72);
	cli_header->ch_runtime_major = GUINT16_FROM_LE (2);
	cli_header->ch_flags = GUINT32_FROM_LE (CLI_FLAGS_ILONLY);
	if (assemblyb->entry_point) 
		cli_header->ch_entry_point = GUINT32_FROM_LE (assemblyb->entry_point->table_idx | MONO_TOKEN_METHOD_DEF);
	else
		cli_header->ch_entry_point = GUINT32_FROM_LE (0);
	cli_header->ch_metadata.rva = GUINT32_FROM_LE (assembly->text_rva + assembly->code.index + CLI_H_SIZE);
	cli_header->ch_metadata.size = GUINT32_FROM_LE (assembly->meta_size);
	
	return header_size;
}

/*
 * We need to return always the same object for MethodInfo, FieldInfo etc..
 * type uses a different hash, since it uses custom hash/equal functions.
 */
static MonoGHashTable *object_cache = NULL;
static MonoGHashTable *type_cache = NULL;

#define CHECK_OBJECT(t,p)	\
	do {	\
		t _obj;	\
		if (!object_cache)	\
			object_cache = mono_g_hash_table_new (g_direct_hash, g_direct_equal);	\
		if ((_obj = mono_g_hash_table_lookup (object_cache, (p))))	\
			return _obj;	\
	} while (0)

#define CACHE_OBJECT(p,o)	\
	do {	\
		mono_g_hash_table_insert (object_cache, p,o);	\
	} while (0)

/*
 * mono_assembly_get_object:
 * @domain: an app domain
 * @assembly: an assembly
 *
 * Return an System.Reflection.Assembly object representing the MonoAssembly @assembly.
 */
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

static gboolean
mymono_metadata_type_equal (MonoType *t1, MonoType *t2)
{
	if ((t1->type != t2->type) ||
	    (t1->byref != t2->byref))
		return FALSE;

	switch (t1->type) {
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
	case MONO_TYPE_STRING:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_OBJECT:
		return TRUE;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
		return t1->data.klass == t2->data.klass;
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
		return mymono_metadata_type_equal (t1->data.type, t2->data.type);
	case MONO_TYPE_ARRAY:
		if (t1->data.array->rank != t2->data.array->rank)
			return FALSE;
		return mymono_metadata_type_equal (t1->data.array->type, t2->data.array->type);
	default:
		g_error ("implement type compare for %0x!", t1->type);
		return FALSE;
	}

	return FALSE;
}

static guint
mymono_metadata_type_hash (MonoType *t1)
{
	guint hash;

	hash = t1->type;

	hash |= t1->byref << 6; /* do not collide with t1->type values */
	switch (t1->type) {
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
		/* check if the distribution is good enough */
		return hash << 7 | g_str_hash (t1->data.klass->name);
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
		return hash << 7 | mymono_metadata_type_hash (t1->data.type);
	}
	return hash;
}

/*
 * mono_type_get_object:
 * @domain: an app domain
 * @type: a type
 *
 * Return an System.MonoType object representing the type @type.
 */
MonoReflectionType*
mono_type_get_object (MonoDomain *domain, MonoType *type)
{
	MonoReflectionType *res;
	MonoClass *klass = mono_class_from_mono_type (type);

	if (!type_cache)
		type_cache = mono_g_hash_table_new ((GHashFunc)mymono_metadata_type_hash, 
				(GCompareFunc)mymono_metadata_type_equal);
	if ((res = mono_g_hash_table_lookup (type_cache, type)))
		return res;
	if (klass->reflection_info) {
		/* should this be considered an error condition? */
		if (!type->byref)
			return klass->reflection_info;
	}
	mono_class_init (klass);
	res = (MonoReflectionType *)mono_object_new (domain, mono_defaults.monotype_class);
	res->type = type;
	mono_g_hash_table_insert (type_cache, type, res);
	return res;
}

/*
 * mono_method_get_object:
 * @domain: an app domain
 * @method: a method
 *
 * Return an System.Reflection.MonoMethod object representing the method @method.
 */
MonoReflectionMethod*
mono_method_get_object (MonoDomain *domain, MonoMethod *method)
{
	/*
	 * We use the same C representation for methods and constructors, but the type 
	 * name in C# is different.
	 */
	const char *cname;
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
	ret->name = mono_string_new (domain, method->name);
	CACHE_OBJECT (method, ret);
	return ret;
}

/*
 * mono_field_get_object:
 * @domain: an app domain
 * @klass: a type
 * @field: a field
 *
 * Return an System.Reflection.MonoField object representing the field @field
 * in class @klass.
 */
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

/*
 * mono_property_get_object:
 * @domain: an app domain
 * @klass: a type
 * @property: a property
 *
 * Return an System.Reflection.MonoProperty object representing the property @property
 * in class @klass.
 */
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

/*
 * mono_event_get_object:
 * @domain: an app domain
 * @klass: a type
 * @event: a event
 *
 * Return an System.Reflection.MonoEvent object representing the event @event
 * in class @klass.
 */
MonoReflectionEvent*
mono_event_get_object (MonoDomain *domain, MonoClass *klass, MonoEvent *event)
{
	MonoReflectionEvent *res;
	MonoClass *oklass;

	CHECK_OBJECT (MonoReflectionEvent *, event);
	oklass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoEvent");
	res = (MonoReflectionEvent *)mono_object_new (domain, oklass);
	res->klass = klass;
	res->event = event;
	CACHE_OBJECT (event, res);
	return res;
}

/*
 * mono_param_get_objects:
 * @domain: an app domain
 * @method: a method
 *
 * Return an System.Reflection.ParameterInfo array object representing the parameters
 * in the method @method.
 */
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
	names = g_new (char *, method->signature->param_count);
	mono_method_get_param_names (method, (const char **) names);
	
	/* Note: the cache is based on the address of the signature into the method
	 * since we already cache MethodInfos with the method as keys.
	 */
	CHECK_OBJECT (MonoReflectionParameter**, &(method->signature));
	oklass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "ParameterInfo");
#if HAVE_BOEHM_GC
	res = GC_malloc (sizeof (MonoReflectionParameter*) * method->signature->param_count);
#else
	res = g_new0 (MonoReflectionParameter*, method->signature->param_count);
#endif
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
 * mono_reflection_parse_type:
 * @name: type name
 *
 * Parse a type name as accepted by the GetType () method and output the info
 * extracted in the info structure.
 * the name param will be mangled, so, make a copy before passing it to this function.
 * The fields in info will be valid until the memory pointed to by name is valid.
 * Returns 0 on parse error.
 * See also mono_type_get_name () below.
 */
int
mono_reflection_parse_type (char *name, MonoTypeNameParse *info) {

	char *start, *p, *w, *last_point;
	int in_modifiers = 0;
	int isbyref = 0, rank;

	start = p = w = name;

	info->name = info->name_space = info->assembly = NULL;
	info->nest_name = info->nest_name_space = NULL;
	info->modifiers = NULL;

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
				info->nest_name_space = (char *)"";
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
		info->name_space = (char *)"";
		info->name = start;
	}
	while (*p) {
		switch (*p) {
		case '&':
			if (isbyref) /* only one level allowed by the spec */
				return 0;
			isbyref = 1;
			info->modifiers = g_list_append (info->modifiers, GUINT_TO_POINTER (0));
			*p++ = 0;
			break;
		case '*':
			info->modifiers = g_list_append (info->modifiers, GUINT_TO_POINTER (-1));
			*p++ = 0;
			break;
		case '[':
			rank = 1;
			*p++ = 0;
			while (*p) {
				if (*p == ']')
					break;
				if (*p == ',')
					rank++;
				else if (*p != '*') /* '*' means unknown lower bound */
					return 0;
				++p;
			}
			if (*p++ != ']')
				return 0;
			info->modifiers = g_list_append (info->modifiers, GUINT_TO_POINTER (rank));
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

static void
mono_type_get_name_recurse (MonoType *type, GString *str)
{
	MonoClass *klass;
	
	switch (type->type) {
	case MONO_TYPE_ARRAY: {
		int i, rank = type->data.array->rank;

		mono_type_get_name_recurse (type->data.array->type, str);
		g_string_append_c (str, '[');
		for (i = 1; i < rank; i++)
			g_string_append_c (str, ',');
		g_string_append_c (str, ']');
		break;
	}
	case MONO_TYPE_SZARRAY:
		mono_type_get_name_recurse (type->data.type, str);
		g_string_append (str, "[]");
		break;
	case MONO_TYPE_PTR:
		mono_type_get_name_recurse (type->data.type, str);
		g_string_append_c (str, '*');
		break;
	default:
		klass = mono_class_from_mono_type (type);
		if (klass->nested_in) {
			mono_type_get_name_recurse (&klass->nested_in->byval_arg, str);
			g_string_append_c (str, '+');
		}
		if (*klass->name_space) {
			g_string_append (str, klass->name_space);
			g_string_append_c (str, '.');
		}
		g_string_append (str, klass->name);
		break;
	}
}

/*
 * mono_type_get_name:
 * @type: a type
 *
 * Returns the string representation for type as required by System.Reflection.
 * The inverse of mono_reflection_parse_type ().
 */
char*
mono_type_get_name (MonoType *type)
{
	GString* result = g_string_new ("");
	mono_type_get_name_recurse (type, result);

	if (type->byref)
		g_string_append_c (result, '&');

	return g_string_free (result, FALSE);
}

/*
 * mono_reflection_get_type:
 * @image: a metadata context
 * @info: type description structure
 * @ignorecase: flag for case-insensitive string compares
 *
 * Build a MonoType from the type description in @info.
 * 
 */
MonoType*
mono_reflection_get_type (MonoImage* image, MonoTypeNameParse *info, gboolean ignorecase)
{
	MonoClass *klass;
	GList *mod;
	int modval;
	
	if (ignorecase) {
		g_warning ("Ignore case not yet supported in GetType()");
		return NULL;
	}

	if (!image)
		image = mono_defaults.corlib;

	if (info->nest_name) {
		klass = mono_class_from_name (image, info->nest_name_space, info->nest_name);
		if (klass) {
			GList *nested;
			mono_class_init (klass);
			nested = klass->nested_classes;
			klass = NULL;
			while (nested) {
				klass = nested->data;
				if (strcmp (klass->name, info->nest_name) == 0 &&
						strcmp (klass->name_space, info->nest_name_space) == 0)
					break;
				klass = NULL;
				nested = nested->next;
			}
		}
	} else {
		klass = mono_class_from_name (image, info->name_space, info->name);
	}
	if (!klass)
		return NULL;
	mono_class_init (klass);
	for (mod = info->modifiers; mod; mod = mod->next) {
		modval = GPOINTER_TO_UINT (mod->data);
		if (!modval) { /* byref: must be last modifier */
			return &klass->this_arg;
		} else if (modval == -1) {
			klass = mono_ptr_class_get (&klass->byval_arg);
		} else { /* array rank */
			klass = mono_array_class_get (&klass->byval_arg, modval);
		}
		mono_class_init (klass);
	}
	return &klass->byval_arg;
}

/*
 * Optimization we could avoid mallocing() an little-endian archs that
 * don't crash with unaligned accesses.
 */
static void
fill_param_data (MonoImage *image, MonoMethodSignature *sig, guint32 blobidx, void **params) {
	int len, i, slen, type;
	const char *p = mono_metadata_blob_heap (image, blobidx);

	len = mono_metadata_decode_value (p, &p);
	if (len < 2 || read16 (p) != 0x0001) /* Prolog */
		return;

	/* skip prolog */
	p += 2;
	for (i = 0; i < sig->param_count; ++i) {
		type = sig->params [i]->type;
handle_enum:
		switch (type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN: {
			MonoBoolean *bval = params [i] = g_malloc (sizeof (MonoBoolean));
			*bval = *p;
			++p;
			break;
		}
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U2:
		case MONO_TYPE_I2: {
			guint16 *val = params [i] = g_malloc (sizeof (guint16));
			*val = read16 (p);
			p += 2;
			break;
		}
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_U:
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_R4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I4: {
			guint32 *val = params [i] = g_malloc (sizeof (guint32));
			*val = read32 (p);
			p += 4;
			break;
		}
#if SIZEOF_VOID_P == 8
		case MONO_TYPE_U: /* error out instead? this should probably not happen */
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_R8:
		case MONO_TYPE_U8:
		case MONO_TYPE_I8: {
			guint64 *val = params [i] = g_malloc (sizeof (guint64));
			*val = read64 (p);
			p += 8;
			break;
		}
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				type = sig->params [i]->data.klass->enum_basetype->type;
				goto handle_enum;
			} else {
				g_warning ("generic valutype %s not handled in custom attr value decoding", sig->params [i]->data.klass->name);
			}
			break;
		case MONO_TYPE_STRING: {
			slen = mono_metadata_decode_value (p, &p);
			params [i] = mono_string_new_len (mono_domain_get (), p, slen);
			p += slen;
			break;
		}
		default:
			g_warning ("Type %02x not handled in custom attr value decoding", sig->params [i]->type);
			break;
		}
	}
}

static void
free_param_data (MonoMethodSignature *sig, void **params) {
	int i;
	for (i = 0; i < sig->param_count; ++i) {
		switch (sig->params [i]->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U:
		case MONO_TYPE_I:
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
		case MONO_TYPE_R8:
		case MONO_TYPE_R4:
		case MONO_TYPE_VALUETYPE:
			g_free (params [i]);
			break;
		default:
			break;
		}
	}
}

/*
 * Find the method index in the metadata methodDef table.
 * Later put these three helper methods in metadata and export them.
 */
static guint32
find_method_index (MonoMethod *method) {
	MonoClass *klass = method->klass;
	int i;

	for (i = 0; i < klass->method.count; ++i) {
		if (method == klass->methods [i])
			return klass->method.first + 1 + i;
	}
	return 0;
}

/*
 * Find the field index in the metadata FieldDef table.
 */
static guint32
find_field_index (MonoClass *klass, MonoClassField *field) {
	int i;

	for (i = 0; i < klass->field.count; ++i) {
		if (field == &klass->fields [i])
			return klass->field.first + 1 + i;
	}
	return 0;
}

/*
 * Find the property index in the metadata Property table.
 */
static guint32
find_property_index (MonoClass *klass, MonoProperty *property) {
	int i;

	for (i = 0; i < klass->property.count; ++i) {
		if (property == &klass->properties [i])
			return klass->property.first + 1 + i;
	}
	return 0;
}

/*
 * Find the event index in the metadata Event table.
 */
static guint32
find_event_index (MonoClass *klass, MonoEvent *event) {
	int i;

	for (i = 0; i < klass->event.count; ++i) {
		if (event == &klass->events [i])
			return klass->event.first + 1 + i;
	}
	return 0;
}

/*
 * mono_reflection_get_custom_attrs:
 * @obj: a reflection object handle
 *
 * Return an array with all the custom attributes defined of the
 * reflection handle @obj. The objects are fully build.
 */
MonoArray*
mono_reflection_get_custom_attrs (MonoObject *obj)
{
	guint32 idx, mtoken, i, len;
	guint32 cols [MONO_CUSTOM_ATTR_SIZE];
	MonoClass *klass;
	MonoImage *image;
	MonoTableInfo *ca;
	MonoMethod *method;
	MonoObject *attr;
	MonoArray *result;
	GList *list = NULL;
	void **params;
	
	klass = obj->vtable->klass;
	/* FIXME: need to handle: Module */
	if (klass == mono_defaults.monotype_class) {
		MonoReflectionType *rtype = (MonoReflectionType*)obj;
		klass = mono_class_from_mono_type (rtype->type);
		idx = mono_metadata_token_index (klass->type_token);
		idx <<= CUSTOM_ATTR_BITS;
		idx |= CUSTOM_ATTR_TYPEDEF;
		image = klass->image;
	} else if (strcmp ("Assembly", klass->name) == 0) {
		MonoReflectionAssembly *rassembly = (MonoReflectionAssembly*)obj;
		idx = 1; /* there is only one assembly */
		idx <<= CUSTOM_ATTR_BITS;
		idx |= CUSTOM_ATTR_ASSEMBLY;
		image = rassembly->assembly->image;
	} else if (strcmp ("MonoProperty", klass->name) == 0) {
		MonoReflectionProperty *rprop = (MonoReflectionProperty*)obj;
		idx = find_property_index (rprop->klass, rprop->property);
		idx <<= CUSTOM_ATTR_BITS;
		idx |= CUSTOM_ATTR_PROPERTY;
		image = rprop->klass->image;
	} else if (strcmp ("MonoEvent", klass->name) == 0) {
		MonoReflectionEvent *revent = (MonoReflectionEvent*)obj;
		idx = find_event_index (revent->klass, revent->event);
		idx <<= CUSTOM_ATTR_BITS;
		idx |= CUSTOM_ATTR_EVENT;
		image = revent->klass->image;
	} else if (strcmp ("MonoField", klass->name) == 0) {
		MonoReflectionField *rfield = (MonoReflectionField*)obj;
		idx = find_field_index (rfield->klass, rfield->field);
		idx <<= CUSTOM_ATTR_BITS;
		idx |= CUSTOM_ATTR_FIELDDEF;
		image = rfield->klass->image;
	} else if ((strcmp ("MonoMethod", klass->name) == 0) || (strcmp ("MonoCMethod", klass->name) == 0)) {
		MonoReflectionMethod *rmethod = (MonoReflectionMethod*)obj;
		idx = find_method_index (rmethod->method);
		idx <<= CUSTOM_ATTR_BITS;
		idx |= CUSTOM_ATTR_METHODDEF;
		image = rmethod->method->klass->image;
	} else if (strcmp ("ParameterInfo", klass->name) == 0) {
		MonoReflectionParameter *param = (MonoReflectionParameter*)obj;
		MonoReflectionMethod *rmethod = (MonoReflectionMethod*)param->MemberImpl;
		guint32 method_index = find_method_index (rmethod->method);
		guint32 param_list, param_last, param_pos, found;

		image = rmethod->method->klass->image;
		ca = &image->tables [MONO_TABLE_METHOD];

		param_list = mono_metadata_decode_row_col (ca, method_index - 1, MONO_METHOD_PARAMLIST);
		if (method_index == ca->rows) {
			ca = &image->tables [MONO_TABLE_PARAM];
			param_last = ca->rows + 1;
		} else {
			param_last = mono_metadata_decode_row_col (ca, method_index, MONO_METHOD_PARAMLIST);
			ca = &image->tables [MONO_TABLE_PARAM];
		}
		found = 0;
		for (i = param_list; i < param_last; ++i) {
			param_pos = mono_metadata_decode_row_col (ca, i - 1, MONO_PARAM_SEQUENCE);
			if (param_pos == param->PositionImpl) {
				found = 1;
				break;
			}
		}
		if (!found)
			return mono_array_new (mono_domain_get (), mono_defaults.object_class, 0);
		idx = i;
		idx <<= CUSTOM_ATTR_BITS;
		idx |= CUSTOM_ATTR_PARAMDEF;
	} else { /* handle other types here... */
		g_error ("get custom attrs not yet supported for %s", klass->name);
	}

	/* at this point image and index are set correctly for searching the custom attr */
	ca = &image->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	/* the table is not sorted */
	for (i = 0; i < ca->rows; ++i) {
		mono_metadata_decode_row (ca, i, cols, MONO_CUSTOM_ATTR_SIZE);
		if (cols [MONO_CUSTOM_ATTR_PARENT] != idx)
			continue;
		mtoken = cols [MONO_CUSTOM_ATTR_TYPE] >> CUSTOM_ATTR_TYPE_BITS;
		switch (cols [MONO_CUSTOM_ATTR_TYPE] & CUSTOM_ATTR_TYPE_MASK) {
		case CUSTOM_ATTR_TYPE_METHODDEF:
			mtoken |= MONO_TOKEN_METHOD_DEF;
			break;
		case CUSTOM_ATTR_TYPE_MEMBERREF:
			mtoken |= MONO_TOKEN_MEMBER_REF;
			break;
		default:
			g_error ("Unknown table for custom attr type %08x", cols [MONO_CUSTOM_ATTR_TYPE]);
			break;
		}
		method = mono_get_method (image, mtoken, NULL);
		if (!method)
			g_error ("Can't find custom attr constructor");
		mono_class_init (method->klass);
		/*g_print ("got attr %s\n", method->klass->name);*/
		params = g_new (void*, method->signature->param_count);
		fill_param_data (image, method->signature, cols [MONO_CUSTOM_ATTR_VALUE], params);
		attr = mono_object_new (mono_domain_get (), method->klass);
		mono_runtime_invoke (method, attr, params, NULL);
		list = g_list_prepend (list, attr);
		free_param_data (method->signature, params);
		g_free (params);
	}

	len = g_list_length (list);
	/*
	 * The return type is really object[], but System/Attribute.cs does a cast
	 * to (Attribute []) and that is not allowed: I'm lazy for now, but we should
	 * probably fix that.
	 */
	klass = mono_class_from_name (mono_defaults.corlib, "System", "Attribute");
	result = mono_array_new (mono_domain_get (), klass, len);
	for (i = 0; i < len; ++i) {
		mono_array_set (result, gpointer, i, list->data);
		list = list->next;
	}
	g_list_free (g_list_first (list));

	return result;
}

static MonoMethodSignature*
ctor_builder_to_signature (MonoReflectionCtorBuilder *ctor) {
	MonoMethodSignature *sig;
	int count, i;

	count = ctor->parameters? mono_array_length (ctor->parameters): 0;

	sig = g_malloc0 (sizeof (MonoMethodSignature) + sizeof (MonoType*) * count);
	sig->hasthis = 1;
	sig->param_count = count;
	sig->sentinelpos = -1; /* FIXME */
	for (i = 0; i < count; ++i) {
		MonoReflectionType *pt = mono_array_get (ctor->parameters, MonoReflectionType*, i);
		sig->params [i] = pt->type;
	}
	return sig;
}

static void
get_prop_name_and_type (MonoObject *prop, char **name, MonoType **type)
{
	MonoClass *klass = mono_object_class (prop);
	if (strcmp (klass->name, "PropertyBuilder") == 0) {
		MonoReflectionPropertyBuilder *pb = (MonoReflectionPropertyBuilder *)prop;
		*name = mono_string_to_utf8 (pb->name);
		*type = pb->type->type;
	} else {
		MonoReflectionProperty *p = (MonoReflectionProperty *)prop;
		*name = g_strdup (p->property->name);
		if (p->property->get)
			*type = p->property->get->signature->ret;
		else
			*type = p->property->set->signature->params [p->property->set->signature->param_count - 1];
	}
}

static void
get_field_name_and_type (MonoObject *field, char **name, MonoType **type)
{
	MonoClass *klass = mono_object_class (field);
	if (strcmp (klass->name, "FieldBuilder") == 0) {
		MonoReflectionFieldBuilder *fb = (MonoReflectionFieldBuilder *)field;
		*name = mono_string_to_utf8 (fb->name);
		*type = fb->type->type;
	} else {
		MonoReflectionField *f = (MonoReflectionField *)field;
		*name = g_strdup (f->field->name);
		*type = f->field->type;
	}
}

static char*
type_get_qualified_name (MonoType *type, MonoAssembly *ass) {
	char *name, *result;
	MonoClass *klass;
	MonoAssembly *ta;

	name = mono_type_get_name (type);
	klass = mono_class_from_mono_type (type);
	ta = klass->image->assembly;
	if (ta == ass || klass->image == mono_defaults.corlib)
		return name;

	/* missing public key */
	result = g_strdup_printf ("%s, %s, Version=%d.%d.%d.%d, Culture=%s", 
		name, ta->aname.name,
		ta->aname.major, ta->aname.minor, ta->aname.build, ta->aname.revision,
		ta->aname.culture && *ta->aname.culture? ta->aname.culture: "neutral");
	g_free (name);
	return result;
}

static void
encode_cattr_value (char *buffer, char *p, char **retbuffer, char **retp, guint32 *buflen, MonoType *type, MonoObject *arg)
{
	char *argval;
	MonoTypeEnum simple_type;
	
	if ((p-buffer) + 10 >= *buflen) {
		char *newbuf;
		*buflen *= 2;
		newbuf = g_realloc (buffer, *buflen);
		p = newbuf + (p-buffer);
		buffer = newbuf;
	}
	argval = ((char*)arg + sizeof (MonoObject));
	simple_type = type->type;
handle_enum:
	switch (simple_type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
		*p++ = *argval;
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
		swap_with_size (p, argval, 2, 1);
		p += 2;
		break;
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_R4:
		swap_with_size (p, argval, 4, 1);
		p += 4;
		break;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R8:
		swap_with_size (p, argval, 8, 1);
		p += 8;
		break;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			simple_type = type->data.klass->enum_basetype->type;
			goto handle_enum;
		} else {
			g_warning ("generic valutype %s not handled in custom attr value decoding", type->data.klass->name);
		}
		break;
	case MONO_TYPE_STRING: {
		char *str = mono_string_to_utf8 ((MonoString*)arg);
		guint32 slen = strlen (str);
		if ((p-buffer) + 10 + slen >= *buflen) {
			char *newbuf;
			*buflen *= 2;
			*buflen += slen;
			newbuf = g_realloc (buffer, *buflen);
			p = newbuf + (p-buffer);
			buffer = newbuf;
		}
		mono_metadata_encode_value (slen, p, &p);
		memcpy (p, str, slen);
		p += slen;
		g_free (str);
		break;
	}
	case MONO_TYPE_CLASS: {
		char *str;
		guint32 slen;
		if (!mono_object_isinst (arg, mono_defaults.monotype_class))
			g_error ("only types allowed");
handle_type:
		str = type_get_qualified_name (((MonoReflectionType*)arg)->type, NULL);
		slen = strlen (str);
		if ((p-buffer) + 10 + slen >= *buflen) {
			char *newbuf;
			*buflen *= 2;
			*buflen += slen;
			newbuf = g_realloc (buffer, *buflen);
			p = newbuf + (p-buffer);
			buffer = newbuf;
		}
		mono_metadata_encode_value (slen, p, &p);
		memcpy (p, str, slen);
		p += slen;
		g_free (str);
		break;
	}
	/* it may be a boxed value or a Type */
	case MONO_TYPE_OBJECT: {
		MonoClass *klass = mono_object_class (arg);
		char *str;
		guint32 slen;
		
		if (mono_object_isinst (arg, mono_defaults.monotype_class)) {
			*p++ = 0x50;
			goto handle_type;
		} else if (klass->enumtype) {
			*p++ = 0x55;
		} else if (klass->byval_arg.type >= MONO_TYPE_BOOLEAN && klass->byval_arg.type <= MONO_TYPE_R8) {
			*p++ = simple_type = klass->byval_arg.type;
			goto handle_enum;
		} else {
			g_error ("unhandled type in custom attr");
		}
		str = type_get_qualified_name (klass->enum_basetype, NULL);
		slen = strlen (str);
		if ((p-buffer) + 10 + slen >= *buflen) {
			char *newbuf;
			*buflen *= 2;
			*buflen += slen;
			newbuf = g_realloc (buffer, *buflen);
			p = newbuf + (p-buffer);
			buffer = newbuf;
		}
		mono_metadata_encode_value (slen, p, &p);
		memcpy (p, str, slen);
		p += slen;
		g_free (str);
		simple_type = klass->enum_basetype->type;
		goto handle_enum;
	}
	default:
		g_error ("type 0x%02x not yet supported in custom attr encoder", simple_type);
	}
	*retp = p;
	*retbuffer = buffer;
}

/*
 * mono_reflection_get_custom_attrs_blob:
 * @ctor: custom attribute constructor
 * @ctorArgs: arguments o the constructor
 * @properties:
 * @propValues:
 * @fields:
 * @fieldValues:
 * 
 * Creates the blob of data that needs to be saved in the metadata and that represents
 * the custom attributed described by @ctor, @ctorArgs etc.
 * Returns: a Byte array representing the blob of data.
 */
MonoArray*
mono_reflection_get_custom_attrs_blob (MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *propValues, MonoArray *fields, MonoArray* fieldValues) {
	MonoArray *result;
	MonoMethodSignature *sig;
	MonoObject *arg;
	char *buffer, *p;
	guint32 buflen, i;

	if (strcmp (ctor->vtable->klass->name, "MonoCMethod")) {
		sig = ctor_builder_to_signature ((MonoReflectionCtorBuilder*)ctor);
	} else {
		sig = ((MonoReflectionMethod*)ctor)->method->signature;
	}
	buflen = 256;
	p = buffer = g_malloc (buflen);
	/* write the prolog */
	*p++ = 1;
	*p++ = 0;
	for (i = 0; i < sig->param_count; ++i) {
		arg = (MonoObject*)mono_array_get (ctorArgs, gpointer, i);
		encode_cattr_value (buffer, p, &buffer, &p, &buflen, sig->params [i], arg);
	}
	i = 0;
	if (properties)
		i += mono_array_length (properties);
	if (fields)
		i += mono_array_length (fields);
	*p++ = i & 0xff;
	*p++ = (i >> 8) & 0xff;
	if (properties) {
		MonoObject *prop;
		for (i = 0; i < mono_array_length (properties); ++i) {
			MonoType *ptype;
			char *pname;
			int len;
			
			prop = mono_array_get (properties, gpointer, i);
			get_prop_name_and_type (prop, &pname, &ptype);
			*p++ = 0x54; /* PROPERTY signature */
			len = strlen (pname);
			mono_metadata_encode_value (len, p, &p);
			memcpy (p, pname, len);
			p += len;
			encode_cattr_value (buffer, p, &buffer, &p, &buflen, ptype, (MonoObject*)mono_array_get (propValues, gpointer, i));
			g_free (pname);
		}
	}

	if (fields) {
		MonoObject *field;
		for (i = 0; i < mono_array_length (fields); ++i) {
			MonoType *ftype;
			char *fname;
			int len;
			
			field = mono_array_get (fields, gpointer, i);
			get_field_name_and_type (field, &fname, &ftype);
			*p++ = 0x53; /* FIELD signature */
			len = strlen (fname);
			mono_metadata_encode_value (len, p, &p);
			memcpy (p, fname, len);
			p += len;
			encode_cattr_value (buffer, p, &buffer, &p, &buflen, ftype, (MonoObject*)mono_array_get (fieldValues, gpointer, i));
			g_free (fname);
		}
	}

	g_assert (p - buffer <= buflen);
	buflen = p - buffer;
	result = mono_array_new (mono_domain_get (), mono_defaults.byte_class, buflen);
	p = mono_array_addr (result, char, 0);
	memcpy (p, buffer, buflen);
	g_free (buffer);
	if (strcmp (ctor->vtable->klass->name, "MonoCMethod"))
		g_free (sig);
	return result;
}

/*
 * mono_reflection_setup_internal_class:
 * @tb: a TypeBuilder object
 *
 * Creates a MonoClass that represents the TypeBuilder.
 * This is a trick that lets us simplify a lot of reflection code
 * (and will allow us to support Build and Run assemblies easier).
 */
void
mono_reflection_setup_internal_class (MonoReflectionTypeBuilder *tb)
{
	MonoClass *klass, *parent;

	klass = g_new0 (MonoClass, 1);

	klass->image = tb->module->assemblyb->dynamic_assembly->assembly.image;

	if (tb->parent)
		parent = mono_class_from_mono_type (tb->parent->type);
	else
		parent = NULL;
	
	klass->inited = 1; /* we lie to the runtime */
	klass->name = mono_string_to_utf8 (tb->name);
	klass->name_space = mono_string_to_utf8 (tb->nspace);
	klass->type_token = MONO_TOKEN_TYPE_DEF | tb->table_idx;
	klass->flags = tb->attrs;

	klass->element_class = klass;
	klass->reflection_info = tb; /* need to pin. */

	if (parent != NULL)
		mono_class_setup_parent (klass, parent);
	mono_class_setup_mono_type (klass);

	/*
	 * FIXME: handle interfaces.
	 */

	tb->type.type = &klass->byval_arg;

	/*g_print ("setup %s as %s (%p)\n", klass->name, ((MonoObject*)tb)->vtable->klass->name, tb);*/
}

/*
 * mono_reflection_create_internal_class:
 * @tb: a TypeBuilder object
 *
 * Actually create the MonoClass that is associated with the TypeBuilder.
 */
void
mono_reflection_create_internal_class (MonoReflectionTypeBuilder *tb)
{
	MonoClass *klass;

	klass = mono_class_from_mono_type (tb->type.type);

	if (klass->enumtype && klass->enum_basetype == NULL) {
		MonoReflectionFieldBuilder *fb;

		g_assert (tb->fields != NULL);
		g_assert (mono_array_length (tb->fields) >= 1);

		fb = mono_array_get (tb->fields, MonoReflectionFieldBuilder*, 0);

		klass->enum_basetype = fb->type->type;
		klass->element_class = mono_class_from_mono_type (klass->enum_basetype);
	}
}

MonoArray *
mono_reflection_sighelper_get_signature_local (MonoReflectionSigHelper *sig)
{
	MonoDynamicAssembly *assembly = sig->module->assemblyb->dynamic_assembly;
	guint32 na = mono_array_length (sig->arguments);
	guint32 buflen, i;
	MonoArray *result;
	char *buf, *p;

	p = buf = g_malloc (10 + na * 10);

	mono_metadata_encode_value (0x07, p, &p);
	mono_metadata_encode_value (na, p, &p);
	for (i = 0; i < na; ++i) {
		MonoReflectionType *type = mono_array_get (sig->arguments, MonoReflectionType *, i);
		encode_reflection_type (assembly, type, p, &p);
	}

	buflen = p - buf;
	result = mono_array_new (mono_domain_get (), mono_defaults.byte_class, buflen);
	p = mono_array_addr (result, char, 0);
	memcpy (p, buf, buflen);
	g_free (buf);

	return result;
}

MonoArray *
mono_reflection_sighelper_get_signature_field (MonoReflectionSigHelper *sig)
{
	MonoDynamicAssembly *assembly = sig->module->assemblyb->dynamic_assembly;
	guint32 na = mono_array_length (sig->arguments);
	guint32 buflen, i;
	MonoArray *result;
	char *buf, *p;

	p = buf = g_malloc (10 + na * 10);

	mono_metadata_encode_value (0x06, p, &p);
	for (i = 0; i < na; ++i) {
		MonoReflectionType *type = mono_array_get (sig->arguments, MonoReflectionType *, i);
		encode_reflection_type (assembly, type, p, &p);
	}

	buflen = p - buf;
	result = mono_array_new (mono_domain_get (), mono_defaults.byte_class, buflen);
	p = mono_array_addr (result, char, 0);
	memcpy (p, buf, buflen);
	g_free (buf);

	return result;
}

