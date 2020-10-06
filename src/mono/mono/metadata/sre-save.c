/**
 * \file
 * Routine for saving an image to a file.
 *   
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Rodrigo Kumpera
 * Copyright 2016 Microsoft
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#include "mono/metadata/dynamic-image-internals.h"
#include "mono/metadata/dynamic-stream-internals.h"
#include "mono/metadata/mono-ptr-array.h"
#include "mono/metadata/mono-hash-internals.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/sre-internals.h"
#include "mono/metadata/security-manager.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/tokentype.h"
#include "mono/metadata/w32file.h"
#include "mono/metadata/w32error.h"

#include "mono/utils/checked-build.h"
#include "mono/utils/mono-digest.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/w32api.h"

#define TEXT_OFFSET 512
#define CLI_H_SIZE 136
#define FILE_ALIGN 512
#define VIRT_ALIGN 8192
#define START_TEXT_RVA  0x00002000

static void    mono_image_get_generic_param_info (MonoReflectionGenericParam *gparam, guint32 owner, MonoDynamicImage *assembly);

static void
alloc_table (MonoDynamicTable *table, guint nrows)
{
	mono_dynimage_alloc_table (table, nrows);
}

static guint32
string_heap_insert (MonoDynamicStream *sh, const char *str)
{
	return mono_dynstream_insert_string (sh, str);
}

static guint32
string_heap_insert_mstring (MonoDynamicStream *sh, MonoString *str, MonoError *error)
{
	return mono_dynstream_insert_mstring (sh, str, error);
}

static guint32
mono_image_add_stream_data (MonoDynamicStream *stream, const char *data, guint32 len)
{
	return mono_dynstream_add_data (stream, data, len);
}

static guint32
mono_image_add_stream_zero (MonoDynamicStream *stream, guint32 len)
{
	return mono_dynstream_add_zero (stream, len);
}

static void
stream_data_align (MonoDynamicStream *stream)
{
	mono_dynstream_data_align (stream);
}

static guint32
mono_image_typedef_or_ref (MonoDynamicImage *assembly, MonoType *type)
{
	return mono_dynimage_encode_typedef_or_ref_full (assembly, type, TRUE);
}

static guint32
find_index_in_table (MonoDynamicImage *assembly, int table_idx, int col, guint32 token)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	int i;
	MonoDynamicTable *table;
	guint32 *values;
	
	table = &assembly->tables [table_idx];

	g_assert (col < table->columns);

	values = table->values + table->columns;
	for (i = 1; i <= table->rows; ++i) {
		if (values [col] == token)
			return i;
		values += table->columns;
	}
	return 0;
}

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
/*
 * Copy len * nelem bytes from val to dest, swapping bytes to LE if necessary.
 * dest may be misaligned.
 */
static void
swap_with_size (char *dest, const char* val, int len, int nelem) {
	MONO_REQ_GC_NEUTRAL_MODE;
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
#endif

static guint32
add_mono_string_to_blob_cached (MonoDynamicImage *assembly, MonoString *str)
{
	MONO_REQ_GC_UNSAFE_MODE;
	
	char blob_size [64];
	char *b = blob_size;
	guint32 idx = 0, len;

	len = str->length * 2;
	mono_metadata_encode_value (len, b, &b);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	{
		char *swapped = g_malloc (2 * mono_string_length_internal (str));
		const char *p = (const char*)mono_string_chars_internal (str);

		swap_with_size (swapped, p, 2, mono_string_length_internal (str));
		idx = mono_dynamic_image_add_to_blob_cached (assembly, blob_size, b-blob_size, swapped, len);
		g_free (swapped);
	}
#else
	idx = mono_dynamic_image_add_to_blob_cached (assembly, blob_size, b-blob_size, mono_string_chars_internal (str), len);
#endif
	return idx;
}

static guint32
image_create_token_raw  (MonoDynamicImage *assembly, MonoObject* obj_raw, gboolean create_methodspec, gboolean register_token, MonoError *error)
{
	HANDLE_FUNCTION_ENTER (); /* FIXME callers of image_create_token_raw should use handles */
	error_init (error);
	MONO_HANDLE_DCL (MonoObject, obj);
	guint32 const result = mono_image_create_token (assembly, obj, create_methodspec, register_token, error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}


/*
 * idx is the table index of the object
 * type is one of MONO_CUSTOM_ATTR_*
 */
static gboolean
mono_image_add_cattrs (MonoDynamicImage *assembly, guint32 idx, guint32 type, MonoArray *cattrs, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDynamicTable *table;
	MonoReflectionCustomAttr *cattr;
	guint32 *values;
	guint32 count, i, token;
	char blob_size [6];
	char *p = blob_size;
	
	error_init (error);

	/* it is legal to pass a NULL cattrs: we avoid to use the if in a lot of places */
	if (!cattrs)
		return TRUE;
	count = mono_array_length_internal (cattrs);
	table = &assembly->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	table->rows += count;
	alloc_table (table, table->rows);
	values = table->values + table->next_idx * MONO_CUSTOM_ATTR_SIZE;
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= type;
	for (i = 0; i < count; ++i) {
		cattr = (MonoReflectionCustomAttr*)mono_array_get_internal (cattrs, gpointer, i);
		values [MONO_CUSTOM_ATTR_PARENT] = idx;
		g_assert (cattr->ctor != NULL);
		if (mono_is_sre_ctor_builder (mono_object_class (cattr->ctor))) {
			MonoReflectionCtorBuilder *ctor = (MonoReflectionCtorBuilder*)cattr->ctor;
			MonoMethod *method = ctor->mhandle;
			if (m_class_get_image (method->klass) == &assembly->image)
				token = MONO_TOKEN_METHOD_DEF | ((MonoReflectionCtorBuilder*)cattr->ctor)->table_idx;
			else
				token = mono_image_get_methodref_token (assembly, method, FALSE);
		} else {
			token = image_create_token_raw (assembly, (MonoObject*)cattr->ctor, FALSE, FALSE, error); /* FIXME use handles */
			if (!is_ok (error)) goto fail;
		}
		type = mono_metadata_token_index (token);
		type <<= MONO_CUSTOM_ATTR_TYPE_BITS;
		switch (mono_metadata_token_table (token)) {
		case MONO_TABLE_METHOD:
			type |= MONO_CUSTOM_ATTR_TYPE_METHODDEF;
			/*
			 * fixup_cattrs () needs to fix this up. We can't use image->tokens, since it contains the old token for the
			 * method, not the one returned by mono_image_create_token ().
			 */
			mono_g_hash_table_insert_internal (assembly->remapped_tokens, GUINT_TO_POINTER (token), cattr->ctor);
			break;
		case MONO_TABLE_MEMBERREF:
			type |= MONO_CUSTOM_ATTR_TYPE_MEMBERREF;
			break;
		default:
			g_warning ("got wrong token in custom attr");
			continue;
		}
		values [MONO_CUSTOM_ATTR_TYPE] = type;
		p = blob_size;
		mono_metadata_encode_value (mono_array_length_internal (cattr->data), p, &p);
		values [MONO_CUSTOM_ATTR_VALUE] = mono_dynamic_image_add_to_blob_cached (assembly, blob_size, p - blob_size,
			mono_array_addr_internal (cattr->data, char, 0), mono_array_length_internal (cattr->data));
		values += MONO_CUSTOM_ATTR_SIZE;
		++table->next_idx;
	}

	return TRUE;

fail:
	return FALSE;
}

static void
mono_image_add_decl_security (MonoDynamicImage *assembly, guint32 parent_token, MonoArray *permissions)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDynamicTable *table;
	guint32 *values;
	guint32 count, i, idx;
	MonoReflectionPermissionSet *perm;

	if (!permissions)
		return;

	count = mono_array_length_internal (permissions);
	table = &assembly->tables [MONO_TABLE_DECLSECURITY];
	table->rows += count;
	alloc_table (table, table->rows);

	for (i = 0; i < mono_array_length_internal (permissions); ++i) {
		perm = (MonoReflectionPermissionSet*)mono_array_addr_internal (permissions, MonoReflectionPermissionSet, i);

		values = table->values + table->next_idx * MONO_DECL_SECURITY_SIZE;

		idx = mono_metadata_token_index (parent_token);
		idx <<= MONO_HAS_DECL_SECURITY_BITS;
		switch (mono_metadata_token_table (parent_token)) {
		case MONO_TABLE_TYPEDEF:
			idx |= MONO_HAS_DECL_SECURITY_TYPEDEF;
			break;
		case MONO_TABLE_METHOD:
			idx |= MONO_HAS_DECL_SECURITY_METHODDEF;
			break;
		case MONO_TABLE_ASSEMBLY:
			idx |= MONO_HAS_DECL_SECURITY_ASSEMBLY;
			break;
		default:
			g_assert_not_reached ();
		}

		values [MONO_DECL_SECURITY_ACTION] = perm->action;
		values [MONO_DECL_SECURITY_PARENT] = idx;
		values [MONO_DECL_SECURITY_PERMISSIONSET] = add_mono_string_to_blob_cached (assembly, perm->pset);

		++table->next_idx;
	}
}

/**
 * method_encode_code:
 *
 * @assembly the assembly
 * @mb the managed MethodBuilder
 * @error set on error
 *
 * Note that the return value is not sensible if @error is set.
 */
static guint32
method_encode_code (MonoDynamicImage *assembly, ReflectionMethodBuilder *mb, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	char flags = 0;
	guint32 idx;
	guint32 code_size;
	gint32 max_stack, i;
	gint32 num_locals = 0;
	gint32 num_exception = 0;
	gint maybe_small;
	guint32 fat_flags;
	char fat_header [12];
	guint32 int_value;
	guint16 short_value;
	guint32 local_sig = 0;
	guint32 header_size = 12;
	MonoArray *code;

	error_init (error);

	if ((mb->attrs & (METHOD_ATTRIBUTE_PINVOKE_IMPL | METHOD_ATTRIBUTE_ABSTRACT)) ||
			(mb->iattrs & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)))
		return 0;

	/*if (mb->name)
		g_print ("Encode method %s\n", mono_string_to_utf8 (mb->name));*/
	if (mb->ilgen) {
		code = mb->ilgen->code;
		code_size = mb->ilgen->code_len;
		max_stack = mb->ilgen->max_stack;
		num_locals = mb->ilgen->locals ? mono_array_length_internal (mb->ilgen->locals) : 0;
		if (mb->ilgen->ex_handlers)
			num_exception = mono_reflection_method_count_clauses (mb->ilgen);
	} else {
		code = mb->code;
		if (code == NULL) {
			ERROR_DECL (inner_error);
			char *name = mono_string_to_utf8_checked_internal (mb->name, inner_error);
			if (!is_ok (inner_error))
				mono_error_set_argument (error, NULL, "a method does not have any IL associated");
			else
				mono_error_set_argument_format (error, NULL, "Method %s does not have any IL associated", name);
			mono_error_cleanup (inner_error);
			g_free (name);
			return 0;
		}

		code_size = mono_array_length_internal (code);
		max_stack = 8; /* we probably need to run a verifier on the code... */
	}

	stream_data_align (&assembly->code);

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
			mono_g_hash_table_insert_internal (assembly->token_fixups, mb->ilgen, GUINT_TO_POINTER (idx + 1));
		mono_image_add_stream_data (&assembly->code, mono_array_addr_internal (code, char, 0), code_size);
		return assembly->text_rva + idx;
	} 
fat_header:
	if (num_locals) {
		local_sig = MONO_TOKEN_SIGNATURE | mono_dynimage_encode_locals (assembly, mb->ilgen, error);
		return_val_if_nok (error, 0);
	}
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
	short_value = GUINT16_TO_LE (max_stack);
	memcpy (fat_header + 2, &short_value, 2);
	int_value = GUINT32_TO_LE (code_size);
	memcpy (fat_header + 4, &int_value, 4);
	int_value = GUINT32_TO_LE (local_sig);
	memcpy (fat_header + 8, &int_value, 4);
	idx = mono_image_add_stream_data (&assembly->code, fat_header, 12);
	/* add to the fixup todo list */
	if (mb->ilgen && mb->ilgen->num_token_fixups)
		mono_g_hash_table_insert_internal (assembly->token_fixups, mb->ilgen, GUINT_TO_POINTER (idx + 12));
	
	mono_image_add_stream_data (&assembly->code, mono_array_addr_internal (code, char, 0), code_size);
	if (num_exception) {
		unsigned char sheader [4];
		MonoILExceptionInfo * ex_info;
		MonoILExceptionBlock * ex_block;
		int j;

		stream_data_align (&assembly->code);
		/* always use fat format for now */
		sheader [0] = METHOD_HEADER_SECTION_FAT_FORMAT | METHOD_HEADER_SECTION_EHTABLE;
		num_exception *= 6 * sizeof (guint32);
		num_exception += 4; /* include the size of the header */
		sheader [1] = num_exception & 0xff;
		sheader [2] = (num_exception >> 8) & 0xff;
		sheader [3] = (num_exception >> 16) & 0xff;
		mono_image_add_stream_data (&assembly->code, (char*)sheader, 4);
		/* fat header, so we are already aligned */
		/* reverse order */
		for (i = mono_array_length_internal (mb->ilgen->ex_handlers) - 1; i >= 0; --i) {
			ex_info = (MonoILExceptionInfo *)mono_array_addr_internal (mb->ilgen->ex_handlers, MonoILExceptionInfo, i);
			if (ex_info->handlers) {
				int finally_start = ex_info->start + ex_info->len;
				for (j = 0; j < mono_array_length_internal (ex_info->handlers); ++j) {
					guint32 val;
					ex_block = (MonoILExceptionBlock*)mono_array_addr_internal (ex_info->handlers, MonoILExceptionBlock, j);
					/* the flags */
					val = GUINT32_TO_LE (ex_block->type);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					/* try offset */
					val = GUINT32_TO_LE (ex_info->start);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					/* need fault, too, probably */
					if (ex_block->type == MONO_EXCEPTION_CLAUSE_FINALLY)
						val = GUINT32_TO_LE (finally_start - ex_info->start);
					else
						val = GUINT32_TO_LE (ex_info->len);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					/* handler offset */
					val = GUINT32_TO_LE (ex_block->start);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					/* handler len */
					val = GUINT32_TO_LE (ex_block->len);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					finally_start = ex_block->start + ex_block->len;
					if (ex_block->extype) {
						MonoType *extype = mono_reflection_type_get_handle ((MonoReflectionType*)ex_block->extype, error);
						return_val_if_nok (error, 0);

						val = mono_metadata_token_from_dor (mono_image_typedef_or_ref (assembly, extype));
					} else {
						if (ex_block->type == MONO_EXCEPTION_CLAUSE_FILTER)
							val = ex_block->filter_offset;
						else
							val = 0;
					}
					val = GUINT32_TO_LE (val);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					/*g_print ("out clause %d: from %d len=%d, handler at %d, %d, finally_start=%d, ex_info->start=%d, ex_info->len=%d, ex_block->type=%d, j=%d, i=%d\n", 
							clause.flags, clause.try_offset, clause.try_len, clause.handler_offset, clause.handler_len, finally_start, ex_info->start, ex_info->len, ex_block->type, j, i);*/
				}
			} else {
				g_error ("No clauses for ex info block %d", i);
			}
		}
	}
	return assembly->text_rva + idx;
}

/*
 * Fill in the MethodDef and ParamDef tables for a method.
 * This is used for both normal methods and constructors.
 */
static gboolean
mono_image_basic_method (ReflectionMethodBuilder *mb, MonoDynamicImage *assembly, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDynamicTable *table;
	guint32 *values;
	guint i, count;

	error_init (error);

	/* room in this table is already allocated */
	table = &assembly->tables [MONO_TABLE_METHOD];
	*mb->table_idx = table->next_idx ++;
	g_hash_table_insert (assembly->method_to_table_idx, mb->mhandle, GUINT_TO_POINTER ((*mb->table_idx)));
	values = table->values + *mb->table_idx * MONO_METHOD_SIZE;
	values [MONO_METHOD_NAME] = string_heap_insert_mstring (&assembly->sheap, mb->name, error);
	return_val_if_nok (error, FALSE);
	values [MONO_METHOD_FLAGS] = mb->attrs;
	values [MONO_METHOD_IMPLFLAGS] = mb->iattrs;
	values [MONO_METHOD_SIGNATURE] = mono_dynimage_encode_method_builder_signature (assembly, mb, error);
	return_val_if_nok (error, FALSE);
	values [MONO_METHOD_RVA] = method_encode_code (assembly, mb, error);
	return_val_if_nok (error, FALSE);

	table = &assembly->tables [MONO_TABLE_PARAM];
	values [MONO_METHOD_PARAMLIST] = table->next_idx;

	mono_image_add_decl_security (assembly, 
		mono_metadata_make_token (MONO_TABLE_METHOD, *mb->table_idx), mb->permissions);

	if (mb->pinfo) {
		MonoDynamicTable *mtable;
		guint32 *mvalues;
		
		mtable = &assembly->tables [MONO_TABLE_FIELDMARSHAL];
		mvalues = mtable->values + mtable->next_idx * MONO_FIELD_MARSHAL_SIZE;
		
		count = 0;
		for (i = 0; i < mono_array_length_internal (mb->pinfo); ++i) {
			if (mono_array_get_internal (mb->pinfo, gpointer, i))
				count++;
		}
		table->rows += count;
		alloc_table (table, table->rows);
		values = table->values + table->next_idx * MONO_PARAM_SIZE;
		for (i = 0; i < mono_array_length_internal (mb->pinfo); ++i) {
			MonoReflectionParamBuilder *pb;
			if ((pb = mono_array_get_internal (mb->pinfo, MonoReflectionParamBuilder*, i))) {
				values [MONO_PARAM_FLAGS] = pb->attrs;
				values [MONO_PARAM_SEQUENCE] = i;
				if (pb->name != NULL) {
					values [MONO_PARAM_NAME] = string_heap_insert_mstring (&assembly->sheap, pb->name, error);
					return_val_if_nok (error, FALSE);
				} else {
					values [MONO_PARAM_NAME] = 0;
				}
				values += MONO_PARAM_SIZE;
				if (pb->marshal_info) {
					mtable->rows++;
					alloc_table (mtable, mtable->rows);
					mvalues = mtable->values + mtable->rows * MONO_FIELD_MARSHAL_SIZE;
					mvalues [MONO_FIELD_MARSHAL_PARENT] = (table->next_idx << MONO_HAS_FIELD_MARSHAL_BITS) | MONO_HAS_FIELD_MARSHAL_PARAMDEF;
					mvalues [MONO_FIELD_MARSHAL_NATIVE_TYPE] = mono_dynimage_save_encode_marshal_blob (assembly, pb->marshal_info, error);
					return_val_if_nok (error, FALSE);
				}
				pb->table_idx = table->next_idx++;
				if (pb->attrs & PARAM_ATTRIBUTE_HAS_DEFAULT) {
					MonoTypeEnum field_type = (MonoTypeEnum)0;
					mtable = &assembly->tables [MONO_TABLE_CONSTANT];
					mtable->rows ++;
					alloc_table (mtable, mtable->rows);
					mvalues = mtable->values + mtable->rows * MONO_CONSTANT_SIZE;
					mvalues [MONO_CONSTANT_PARENT] = MONO_HASCONSTANT_PARAM | (pb->table_idx << MONO_HASCONSTANT_BITS);
					mvalues [MONO_CONSTANT_VALUE] = mono_dynimage_encode_constant (assembly, pb->def_value, &field_type);
					mvalues [MONO_CONSTANT_TYPE] = field_type;
					mvalues [MONO_CONSTANT_PADDING] = 0;
				}
			}
		}
	}

	return TRUE;
}

static gboolean
mono_image_add_methodimpl (MonoDynamicImage *assembly, MonoReflectionMethodBuilder *mb, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mb->type;
	MonoDynamicTable *table;
	guint32 *values;
	guint32 tok;
	MonoReflectionMethod *m;
	int i;

	error_init (error);

	if (!mb->override_methods)
		return TRUE;

	for (i = 0; i < mono_array_length_internal (mb->override_methods); ++i) {
		m = mono_array_get_internal (mb->override_methods, MonoReflectionMethod*, i);

		table = &assembly->tables [MONO_TABLE_METHODIMPL];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_METHODIMPL_SIZE;
		values [MONO_METHODIMPL_CLASS] = tb->table_idx;
		values [MONO_METHODIMPL_BODY] = MONO_METHODDEFORREF_METHODDEF | (mb->table_idx << MONO_METHODDEFORREF_BITS);

		tok = image_create_token_raw (assembly, (MonoObject*)m, FALSE, FALSE, error); /* FIXME use handles */
		return_val_if_nok (error, FALSE);

		switch (mono_metadata_token_table (tok)) {
		case MONO_TABLE_MEMBERREF:
			tok = (mono_metadata_token_index (tok) << MONO_METHODDEFORREF_BITS ) | MONO_METHODDEFORREF_METHODREF;
			break;
		case MONO_TABLE_METHOD:
			tok = (mono_metadata_token_index (tok) << MONO_METHODDEFORREF_BITS ) | MONO_METHODDEFORREF_METHODDEF;
			break;
		default:
			g_assert_not_reached ();
		}
		values [MONO_METHODIMPL_DECLARATION] = tok;
	}

	return TRUE;
}

#ifndef DISABLE_REFLECTION_EMIT
static gboolean
mono_image_get_method_info (MonoReflectionMethodBuilder *mb, MonoDynamicImage *assembly, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;
	/* We need to clear handles for rmb fields created in mono_reflection_methodbuilder_from_method_builder */
	HANDLE_FUNCTION_ENTER ();

	MonoDynamicTable *table;
	guint32 *values;
	ReflectionMethodBuilder rmb;
	int i;
	gboolean ret = TRUE;

	error_init (error);

	if (!mono_reflection_methodbuilder_from_method_builder (&rmb, mb, error) ||
	    !mono_image_basic_method (&rmb, assembly, error)) {
		ret = FALSE;
		goto exit;
	}

	mb->table_idx = *rmb.table_idx;

	if (mb->dll) { /* It's a P/Invoke method */
		guint32 moduleref;
		/* map CharSet values to on-disk values */
		int ncharset = (mb->charset ? (mb->charset - 1) * 2 : 0);
		int extra_flags = mb->extra_flags;
		table = &assembly->tables [MONO_TABLE_IMPLMAP];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_IMPLMAP_SIZE;
		
		values [MONO_IMPLMAP_FLAGS] = (mb->native_cc << 8) | ncharset | extra_flags;
		values [MONO_IMPLMAP_MEMBER] = (mb->table_idx << 1) | 1; /* memberforwarded: method */
		if (mb->dllentry) {
			values [MONO_IMPLMAP_NAME] = string_heap_insert_mstring (&assembly->sheap, mb->dllentry, error);
			return_val_if_nok (error, FALSE);
		} else {
			values [MONO_IMPLMAP_NAME] = string_heap_insert_mstring (&assembly->sheap, mb->name, error);
			return_val_if_nok (error, FALSE);
		}
		moduleref = string_heap_insert_mstring (&assembly->sheap, mb->dll, error);
		return_val_if_nok (error, FALSE);
		if (!(values [MONO_IMPLMAP_SCOPE] = find_index_in_table (assembly, MONO_TABLE_MODULEREF, MONO_MODULEREF_NAME, moduleref))) {
			table = &assembly->tables [MONO_TABLE_MODULEREF];
			table->rows ++;
			alloc_table (table, table->rows);
			table->values [table->rows * MONO_MODULEREF_SIZE + MONO_MODULEREF_NAME] = moduleref;
			values [MONO_IMPLMAP_SCOPE] = table->rows;
		}
	}

	if (mb->generic_params) {
		table = &assembly->tables [MONO_TABLE_GENERICPARAM];
		table->rows += mono_array_length_internal (mb->generic_params);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length_internal (mb->generic_params); ++i) {
			guint32 owner = MONO_TYPEORMETHOD_METHOD | (mb->table_idx << MONO_TYPEORMETHOD_BITS);

			mono_image_get_generic_param_info (
				(MonoReflectionGenericParam *)mono_array_get_internal (mb->generic_params, gpointer, i), owner, assembly);
		}
	}
exit:
	HANDLE_FUNCTION_RETURN_VAL (ret);
}

static gboolean
mono_image_get_ctor_info (MonoDomain *domain, MonoReflectionCtorBuilder *mb, MonoDynamicImage *assembly, MonoError *error)
{
	/* We need to clear handles for rmb fields created in mono_reflection_methodbuilder_from_ctor_builder */
	HANDLE_FUNCTION_ENTER ();
	MONO_REQ_GC_UNSAFE_MODE;

	gboolean ret = TRUE;
	ReflectionMethodBuilder rmb;

	if (!mono_reflection_methodbuilder_from_ctor_builder (&rmb, mb, error)) {
		ret = FALSE;
		goto exit;
	}

	if (!mono_image_basic_method (&rmb, assembly, error)) {
		ret = FALSE;
		goto exit;
	}

	mb->table_idx = *rmb.table_idx;
exit:
	HANDLE_FUNCTION_RETURN_VAL (ret);
}
#endif

static void
mono_image_get_field_info (MonoReflectionFieldBuilder *fb, MonoDynamicImage *assembly, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);

	MonoDynamicTable *table;
	guint32 *values;

	/* maybe this fixup should be done in the C# code */
	if (fb->attrs & FIELD_ATTRIBUTE_LITERAL)
		fb->attrs |= FIELD_ATTRIBUTE_HAS_DEFAULT;
	table = &assembly->tables [MONO_TABLE_FIELD];
	guint32 fb_table_idx = table->next_idx ++;
	g_hash_table_insert (assembly->field_to_table_idx, fb->handle, GUINT_TO_POINTER (fb_table_idx));
	values = table->values + fb_table_idx * MONO_FIELD_SIZE;
	values [MONO_FIELD_NAME] = string_heap_insert_mstring (&assembly->sheap, fb->name, error);
	return_if_nok (error);
	values [MONO_FIELD_FLAGS] = fb->attrs;
	values [MONO_FIELD_SIGNATURE] = mono_dynimage_encode_field_signature (assembly, fb, error);
	return_if_nok (error);

	if (fb->offset != -1) {
		table = &assembly->tables [MONO_TABLE_FIELDLAYOUT];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_FIELD_LAYOUT_SIZE;
		values [MONO_FIELD_LAYOUT_FIELD] = fb_table_idx;
		values [MONO_FIELD_LAYOUT_OFFSET] = fb->offset;
	}
	if (fb->attrs & FIELD_ATTRIBUTE_LITERAL) {
		MonoTypeEnum field_type = (MonoTypeEnum)0;
		table = &assembly->tables [MONO_TABLE_CONSTANT];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_CONSTANT_SIZE;
		values [MONO_CONSTANT_PARENT] = MONO_HASCONSTANT_FIEDDEF | (fb_table_idx << MONO_HASCONSTANT_BITS);
		values [MONO_CONSTANT_VALUE] = mono_dynimage_encode_constant (assembly, fb->def_value, &field_type);
		values [MONO_CONSTANT_TYPE] = field_type;
		values [MONO_CONSTANT_PADDING] = 0;
	}
	if (fb->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA) {
		guint32 rva_idx;
		table = &assembly->tables [MONO_TABLE_FIELDRVA];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_FIELD_RVA_SIZE;
		values [MONO_FIELD_RVA_FIELD] = fb_table_idx;
		/*
		 * We store it in the code section because it's simpler for now.
		 */
		if (fb->rva_data) {
			if (mono_array_length_internal (fb->rva_data) >= 10)
				stream_data_align (&assembly->code);
			rva_idx = mono_image_add_stream_data (&assembly->code, mono_array_addr_internal (fb->rva_data, char, 0), mono_array_length_internal (fb->rva_data));
		} else
			rva_idx = mono_image_add_stream_zero (&assembly->code, mono_class_value_size (fb->handle->parent, NULL));
		values [MONO_FIELD_RVA_RVA] = rva_idx + assembly->text_rva;
	}
	if (fb->marshal_info) {
		table = &assembly->tables [MONO_TABLE_FIELDMARSHAL];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_FIELD_MARSHAL_SIZE;
		values [MONO_FIELD_MARSHAL_PARENT] = (fb_table_idx << MONO_HAS_FIELD_MARSHAL_BITS) | MONO_HAS_FIELD_MARSHAL_FIELDSREF;
		values [MONO_FIELD_MARSHAL_NATIVE_TYPE] = mono_dynimage_save_encode_marshal_blob (assembly, fb->marshal_info, error);
		return_if_nok (error);
	}
}

static void
mono_image_get_property_info (MonoReflectionPropertyBuilder *pb, MonoDynamicImage *assembly, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);

	MonoDynamicTable *table;
	guint32 *values;
	guint num_methods = 0;
	guint32 semaidx;

	/* 
	 * we need to set things in the following tables:
	 * PROPERTYMAP (info already filled in _get_type_info ())
	 * PROPERTY    (rows already preallocated in _get_type_info ())
	 * METHOD      (method info already done with the generic method code)
	 * METHODSEMANTICS
	 * CONSTANT
	 */
	table = &assembly->tables [MONO_TABLE_PROPERTY];
	pb->table_idx = table->next_idx ++;
	values = table->values + pb->table_idx * MONO_PROPERTY_SIZE;
	values [MONO_PROPERTY_NAME] = string_heap_insert_mstring (&assembly->sheap, pb->name, error);
	return_if_nok (error);
	values [MONO_PROPERTY_FLAGS] = pb->attrs;
	values [MONO_PROPERTY_TYPE] = mono_dynimage_save_encode_property_signature (assembly, pb, error);
	return_if_nok (error);


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
		values [MONO_METHOD_SEMA_ASSOCIATION] = (pb->table_idx << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_PROPERTY;
	}
	if (pb->set_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_SETTER;
		values [MONO_METHOD_SEMA_METHOD] = pb->set_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (pb->table_idx << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_PROPERTY;
	}
	if (pb->attrs & PROPERTY_ATTRIBUTE_HAS_DEFAULT) {
		MonoTypeEnum field_type = (MonoTypeEnum)0;
		table = &assembly->tables [MONO_TABLE_CONSTANT];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_CONSTANT_SIZE;
		values [MONO_CONSTANT_PARENT] = MONO_HASCONSTANT_PROPERTY | (pb->table_idx << MONO_HASCONSTANT_BITS);
		values [MONO_CONSTANT_VALUE] = mono_dynimage_encode_constant (assembly, pb->def_value, &field_type);
		values [MONO_CONSTANT_TYPE] = field_type;
		values [MONO_CONSTANT_PADDING] = 0;
	}
}

static void
mono_image_get_event_info (MonoReflectionEventBuilder *eb, MonoDynamicImage *assembly, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDynamicTable *table;
	guint32 *values;
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
	values [MONO_EVENT_NAME] = string_heap_insert_mstring (&assembly->sheap, eb->name, error);
	return_if_nok (error);
	values [MONO_EVENT_FLAGS] = eb->attrs;
	MonoType *ebtype = mono_reflection_type_get_handle (eb->type, error);
	return_if_nok (error);
	values [MONO_EVENT_TYPE] = mono_image_typedef_or_ref (assembly, ebtype);

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
		values [MONO_METHOD_SEMA_ASSOCIATION] = (eb->table_idx << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_EVENT;
	}
	if (eb->remove_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_REMOVE_ON;
		values [MONO_METHOD_SEMA_METHOD] = eb->remove_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (eb->table_idx << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_EVENT;
	}
	if (eb->raise_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_FIRE;
		values [MONO_METHOD_SEMA_METHOD] = eb->raise_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (eb->table_idx << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_EVENT;
	}
}

static void
encode_constraints (MonoReflectionGenericParam *gparam, guint32 owner, MonoDynamicImage *assembly, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);

	MonoDynamicTable *table;
	guint32 num_constraints, i;
	guint32 *values;
	guint32 table_idx;

	table = &assembly->tables [MONO_TABLE_GENERICPARAMCONSTRAINT];
	num_constraints = gparam->iface_constraints ?
		mono_array_length_internal (gparam->iface_constraints) : 0;
	table->rows += num_constraints;
	if (gparam->base_type)
		table->rows++;
	alloc_table (table, table->rows);

	if (gparam->base_type) {
		table_idx = table->next_idx ++;
		values = table->values + table_idx * MONO_GENPARCONSTRAINT_SIZE;

		MonoType *gpbasetype = mono_reflection_type_get_handle (gparam->base_type, error);
		return_if_nok (error);
		values [MONO_GENPARCONSTRAINT_GENERICPAR] = owner;
		values [MONO_GENPARCONSTRAINT_CONSTRAINT] = mono_image_typedef_or_ref (assembly, gpbasetype);
	}

	for (i = 0; i < num_constraints; i++) {
		MonoReflectionType *constraint = (MonoReflectionType *)mono_array_get_internal (
			gparam->iface_constraints, gpointer, i);

		table_idx = table->next_idx ++;
		values = table->values + table_idx * MONO_GENPARCONSTRAINT_SIZE;

		MonoType *constraint_type = mono_reflection_type_get_handle (constraint, error);
		return_if_nok (error);

		values [MONO_GENPARCONSTRAINT_GENERICPAR] = owner;
		values [MONO_GENPARCONSTRAINT_CONSTRAINT] = mono_image_typedef_or_ref (assembly, constraint_type);
	}
}

static void
mono_image_get_generic_param_info (MonoReflectionGenericParam *gparam, guint32 owner, MonoDynamicImage *assembly)
{
	MONO_REQ_GC_UNSAFE_MODE;

	GenericParamTableEntry *entry;

	/*
	 * The GenericParam table must be sorted according to the `owner' field.
	 * We need to do this sorting prior to writing the GenericParamConstraint
	 * table, since we have to use the final GenericParam table indices there
	 * and they must also be sorted.
	 */

	entry = g_new0 (GenericParamTableEntry, 1);
	entry->owner = owner;
	/* FIXME: track where gen_params should be freed and remove the GC root as well */
	MONO_GC_REGISTER_ROOT_IF_MOVING (entry->gparam, MONO_ROOT_SOURCE_REFLECTION, NULL, "Reflection Generic Parameter");
	entry->gparam = gparam;
	
	g_ptr_array_add (assembly->gen_params, entry);
}

static gboolean
write_generic_param_entry (MonoDynamicImage *assembly, GenericParamTableEntry *entry, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDynamicTable *table;
	MonoGenericParam *param;
	guint32 *values;
	guint32 table_idx;

	error_init (error);

	table = &assembly->tables [MONO_TABLE_GENERICPARAM];
	table_idx = table->next_idx ++;
	values = table->values + table_idx * MONO_GENERICPARAM_SIZE;

	MonoType *gparam_type = mono_reflection_type_get_handle ((MonoReflectionType*)entry->gparam, error);
	return_val_if_nok (error, FALSE);

	param = gparam_type->data.generic_param;

	values [MONO_GENERICPARAM_OWNER] = entry->owner;
	values [MONO_GENERICPARAM_FLAGS] = entry->gparam->attrs;
	values [MONO_GENERICPARAM_NUMBER] = mono_generic_param_num (param);
	values [MONO_GENERICPARAM_NAME] = string_heap_insert (&assembly->sheap, mono_generic_param_name (param));

	if (!mono_image_add_cattrs (assembly, table_idx, MONO_CUSTOM_ATTR_GENERICPAR, entry->gparam->cattrs, error))
		return FALSE;

	encode_constraints (entry->gparam, table_idx, assembly, error);
	return_val_if_nok (error, FALSE);

	return TRUE;
}

static void
collect_types (MonoPtrArray *types, MonoReflectionTypeBuilder *type)
{
	int i;

	mono_ptr_array_append (*types, type);

	if (!type->subtypes)
		return;

	for (i = 0; i < mono_array_length_internal (type->subtypes); ++i) {
		MonoReflectionTypeBuilder *subtype = mono_array_get_internal (type->subtypes, MonoReflectionTypeBuilder*, i);
		collect_types (types, subtype);
	}
}

static gint
compare_types_by_table_idx (MonoReflectionTypeBuilder **type1, MonoReflectionTypeBuilder **type2)
{
	if ((*type1)->table_idx < (*type2)->table_idx)
		return -1;
	else
		if ((*type1)->table_idx > (*type2)->table_idx)
			return 1;
	else
		return 0;
}

static gboolean
params_add_cattrs (MonoDynamicImage *assembly, MonoArray *pinfo, MonoError *error) {
	int i;

	error_init (error);
	if (!pinfo)
		return TRUE;
	for (i = 0; i < mono_array_length_internal (pinfo); ++i) {
		MonoReflectionParamBuilder *pb;
		pb = mono_array_get_internal (pinfo, MonoReflectionParamBuilder *, i);
		if (!pb)
			continue;
		if (!mono_image_add_cattrs (assembly, pb->table_idx, MONO_CUSTOM_ATTR_PARAMDEF, pb->cattrs, error))
			return FALSE;
	}

	return TRUE;
}

static guint32
field_builder_table_index (MonoDynamicImage* assembly, MonoReflectionFieldBuilder *fb)
{
	return GPOINTER_TO_UINT (g_hash_table_lookup (assembly->field_to_table_idx, fb->handle));
}

static gboolean
type_add_cattrs (MonoDynamicImage *assembly, MonoReflectionTypeBuilder *tb, MonoError *error) {
	int i;

	error_init (error);
	
	if (!mono_image_add_cattrs (assembly, tb->table_idx, MONO_CUSTOM_ATTR_TYPEDEF, tb->cattrs, error))
		return FALSE;
	if (tb->fields) {
		for (i = 0; i < tb->num_fields; ++i) {
			MonoReflectionFieldBuilder* fb;
			fb = mono_array_get_internal (tb->fields, MonoReflectionFieldBuilder*, i);
			if (!mono_image_add_cattrs (assembly, field_builder_table_index (assembly, fb), MONO_CUSTOM_ATTR_FIELDDEF, fb->cattrs, error))
				return FALSE;
		}
	}
	if (tb->events) {
		for (i = 0; i < mono_array_length_internal (tb->events); ++i) {
			MonoReflectionEventBuilder* eb;
			eb = mono_array_get_internal (tb->events, MonoReflectionEventBuilder*, i);
			if (!mono_image_add_cattrs (assembly, eb->table_idx, MONO_CUSTOM_ATTR_EVENT, eb->cattrs, error))
				return FALSE;
		}
	}
	if (tb->properties) {
		for (i = 0; i < mono_array_length_internal (tb->properties); ++i) {
			MonoReflectionPropertyBuilder* pb;
			pb = mono_array_get_internal (tb->properties, MonoReflectionPropertyBuilder*, i);
			if (!mono_image_add_cattrs (assembly, pb->table_idx, MONO_CUSTOM_ATTR_PROPERTY, pb->cattrs, error))
				return FALSE;
		}
	}
	if (tb->ctors) {
		for (i = 0; i < mono_array_length_internal (tb->ctors); ++i) {
			MonoReflectionCtorBuilder* cb;
			cb = mono_array_get_internal (tb->ctors, MonoReflectionCtorBuilder*, i);
			if (!mono_image_add_cattrs (assembly, cb->table_idx, MONO_CUSTOM_ATTR_METHODDEF, cb->cattrs, error) ||
			    !params_add_cattrs (assembly, cb->pinfo, error))
				return FALSE;
		}
	}

	if (tb->methods) {
		for (i = 0; i < tb->num_methods; ++i) {
			MonoReflectionMethodBuilder* mb;
			mb = mono_array_get_internal (tb->methods, MonoReflectionMethodBuilder*, i);
			if (!mono_image_add_cattrs (assembly, mb->table_idx, MONO_CUSTOM_ATTR_METHODDEF, mb->cattrs, error) ||
			    !params_add_cattrs (assembly, mb->pinfo, error))
				return FALSE;
		}
	}

	if (tb->subtypes) {
		for (i = 0; i < mono_array_length_internal (tb->subtypes); ++i) {
			if (!type_add_cattrs (assembly, mono_array_get_internal (tb->subtypes, MonoReflectionTypeBuilder*, i), error))
				return FALSE;
		}
	}

	return TRUE;
}

static gboolean
module_add_cattrs (MonoDynamicImage *assembly, MonoReflectionModuleBuilder *moduleb, MonoError *error)
{
	int i;
	
	error_init (error);

	if (!mono_image_add_cattrs (assembly, moduleb->table_idx, MONO_CUSTOM_ATTR_MODULE, moduleb->cattrs, error))
		return FALSE;

	if (moduleb->global_methods) {
		for (i = 0; i < mono_array_length_internal (moduleb->global_methods); ++i) {
			MonoReflectionMethodBuilder* mb = mono_array_get_internal (moduleb->global_methods, MonoReflectionMethodBuilder*, i);
			if (!mono_image_add_cattrs (assembly, mb->table_idx, MONO_CUSTOM_ATTR_METHODDEF, mb->cattrs, error) ||
			    !params_add_cattrs (assembly, mb->pinfo, error))
				return FALSE;
		}
	}

	if (moduleb->global_fields) {
		for (i = 0; i < mono_array_length_internal (moduleb->global_fields); ++i) {
			MonoReflectionFieldBuilder *fb = mono_array_get_internal (moduleb->global_fields, MonoReflectionFieldBuilder*, i);
			if (!mono_image_add_cattrs (assembly, field_builder_table_index (assembly, fb), MONO_CUSTOM_ATTR_FIELDDEF, fb->cattrs, error))
				return FALSE;
		}
	}
	
	if (moduleb->types) {
		for (i = 0; i < moduleb->num_types; ++i) {
			if (!type_add_cattrs (assembly, mono_array_get_internal (moduleb->types, MonoReflectionTypeBuilder*, i), error))
				return FALSE;
		}
	}

	return TRUE;
}

static gboolean
mono_image_fill_file_table (MonoDomain *domain, MonoReflectionModule *module, MonoDynamicImage *assembly, MonoError *error)
{
	MonoDynamicTable *table;
	guint32 *values;
	char blob_size [6];
	guchar hash [20];
	char *b = blob_size;
	char *dir, *path;

	error_init (error);

	table = &assembly->tables [MONO_TABLE_FILE];
	table->rows++;
	alloc_table (table, table->rows);
	values = table->values + table->next_idx * MONO_FILE_SIZE;
	values [MONO_FILE_FLAGS] = FILE_CONTAINS_METADATA;
	values [MONO_FILE_NAME] = string_heap_insert (&assembly->sheap, module->image->module_name);
	if (image_is_dynamic (module->image)) {
		/* This depends on the fact that the main module is emitted last */
		dir = mono_string_to_utf8_checked_internal (((MonoReflectionModuleBuilder*)module)->assemblyb->dir, error);
		return_val_if_nok (error, FALSE);
		path = g_strdup_printf ("%s%c%s", dir, G_DIR_SEPARATOR, module->image->module_name);
	} else {
		dir = NULL;
		path = g_strdup (module->image->name);
	}
	mono_sha1_get_digest_from_file (path, hash);
	g_free (dir);
	g_free (path);
	mono_metadata_encode_value (20, b, &b);
	values [MONO_FILE_HASH_VALUE] = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	mono_image_add_stream_data (&assembly->blob, (char*)hash, 20);
	table->next_idx ++;
	return TRUE;
}

static void
mono_image_fill_module_table (MonoDomain *domain, MonoReflectionModuleBuilder *mb, MonoDynamicImage *assembly, MonoError *error)
{
	MonoDynamicTable *table;
	int i;

	error_init (error);

	table = &assembly->tables [MONO_TABLE_MODULE];
	mb->table_idx = table->next_idx ++;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_NAME] = string_heap_insert_mstring (&assembly->sheap, mb->module.name, error);
	return_if_nok (error);
	i = mono_image_add_stream_data (&assembly->guid, mono_array_addr_internal (mb->guid, char, 0), 16);
	i /= 16;
	++i;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_GENERATION] = 0;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_MVID] = i;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_ENC] = 0;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_ENCBASE] = 0;
}

static guint32
mono_image_fill_export_table_from_class (MonoDomain *domain, MonoClass *klass,
	guint32 module_index, guint32 parent_index, MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 visib, res;

	visib = mono_class_get_flags (klass) & TYPE_ATTRIBUTE_VISIBILITY_MASK;
	if (! ((visib & TYPE_ATTRIBUTE_PUBLIC) || (visib & TYPE_ATTRIBUTE_NESTED_PUBLIC)))
		return 0;

	table = &assembly->tables [MONO_TABLE_EXPORTEDTYPE];
	table->rows++;
	alloc_table (table, table->rows);
	values = table->values + table->next_idx * MONO_EXP_TYPE_SIZE;

	values [MONO_EXP_TYPE_FLAGS] = mono_class_get_flags (klass);
	values [MONO_EXP_TYPE_TYPEDEF] = m_class_get_type_token (klass);
	if (m_class_get_nested_in (klass))
		values [MONO_EXP_TYPE_IMPLEMENTATION] = (parent_index << MONO_IMPLEMENTATION_BITS) + MONO_IMPLEMENTATION_EXP_TYPE;
	else
		values [MONO_EXP_TYPE_IMPLEMENTATION] = (module_index << MONO_IMPLEMENTATION_BITS) + MONO_IMPLEMENTATION_FILE;
	values [MONO_EXP_TYPE_NAME] = string_heap_insert (&assembly->sheap, m_class_get_name (klass));
	values [MONO_EXP_TYPE_NAMESPACE] = string_heap_insert (&assembly->sheap, m_class_get_name_space (klass));

	res = table->next_idx;

	table->next_idx ++;

	/* Emit nested types */
	GList *nested_classes = mono_class_get_nested_classes_property (klass);
	GList *tmp;
	for (tmp = nested_classes; tmp; tmp = tmp->next)
		mono_image_fill_export_table_from_class (domain, (MonoClass *)tmp->data, module_index, table->next_idx - 1, assembly);

	return res;
}

static void
mono_image_fill_export_table (MonoDomain *domain, MonoReflectionTypeBuilder *tb,
			      guint32 module_index, guint32 parent_index, MonoDynamicImage *assembly,
			      MonoError *error)
{
	MonoClass *klass;
	guint32 idx, i;

	error_init (error);

	MonoType *t = mono_reflection_type_get_handle ((MonoReflectionType*)tb, error);
	return_if_nok (error);

	klass = mono_class_from_mono_type_internal (t);

	guint32 tb_token = mono_metadata_make_token (MONO_TABLE_TYPEDEF, tb->table_idx);
	if (m_class_get_type_token (klass) != tb_token) {
		g_error ("TypeBuilder token %08x does not match klass token %08x", tb_token, m_class_get_type_token (klass));
	}

	idx = mono_image_fill_export_table_from_class (domain, klass, module_index, 
												   parent_index, assembly);

	/* 
	 * Emit nested types
	 * We need to do this ourselves since klass->nested_classes is not set up.
	 */
	if (tb->subtypes) {
		for (i = 0; i < mono_array_length_internal (tb->subtypes); ++i) {
			mono_image_fill_export_table (domain, mono_array_get_internal (tb->subtypes, MonoReflectionTypeBuilder*, i), module_index, idx, assembly, error);
			return_if_nok (error);
		}
	}
}

static void
mono_image_fill_export_table_from_module (MonoDomain *domain, MonoReflectionModule *module,
	guint32 module_index, MonoDynamicImage *assembly)
{
	MonoImage *image = module->image;
	MonoTableInfo  *t;
	guint32 i;

	t = &image->tables [MONO_TABLE_TYPEDEF];

	for (i = 0; i < t->rows; ++i) {
		ERROR_DECL (error);
		MonoClass *klass = mono_class_get_checked (image, mono_metadata_make_token (MONO_TABLE_TYPEDEF, i + 1), error);
		g_assert (is_ok (error)); /* FIXME don't swallow the error */

		if (mono_class_is_public (klass))
			mono_image_fill_export_table_from_class (domain, klass, module_index, 0, assembly);
	}
}

static void
add_exported_type (MonoReflectionAssemblyBuilder *assemblyb, MonoDynamicImage *assembly, MonoClass *klass, guint32 parent_index)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 scope, scope_idx, impl, current_idx;
	gboolean forwarder = TRUE;
	gpointer iter = NULL;
	MonoClass *nested;

	if (m_class_get_nested_in (klass)) {
		impl = (parent_index << MONO_IMPLEMENTATION_BITS) + MONO_IMPLEMENTATION_EXP_TYPE;
		forwarder = FALSE;
	} else {
		scope = mono_reflection_resolution_scope_from_image (assembly, m_class_get_image (klass));
		g_assert ((scope & MONO_RESOLUTION_SCOPE_MASK) == MONO_RESOLUTION_SCOPE_ASSEMBLYREF);
		scope_idx = scope >> MONO_RESOLUTION_SCOPE_BITS;
		impl = (scope_idx << MONO_IMPLEMENTATION_BITS) + MONO_IMPLEMENTATION_ASSEMBLYREF;
	}

	table = &assembly->tables [MONO_TABLE_EXPORTEDTYPE];

	table->rows++;
	alloc_table (table, table->rows);
	current_idx = table->next_idx;
	values = table->values + current_idx * MONO_EXP_TYPE_SIZE;

	values [MONO_EXP_TYPE_FLAGS] = forwarder ? TYPE_ATTRIBUTE_FORWARDER : 0;
	values [MONO_EXP_TYPE_TYPEDEF] = 0;
	values [MONO_EXP_TYPE_IMPLEMENTATION] = impl;
	values [MONO_EXP_TYPE_NAME] = string_heap_insert (&assembly->sheap, m_class_get_name (klass));
	values [MONO_EXP_TYPE_NAMESPACE] = string_heap_insert (&assembly->sheap, m_class_get_name_space (klass));

	table->next_idx++;

	while ((nested = mono_class_get_nested_types (klass, &iter)))
		add_exported_type (assemblyb, assembly, nested, current_idx);
}

static void
mono_image_fill_export_table_from_type_forwarders (MonoReflectionAssemblyBuilder *assemblyb, MonoDynamicImage *assembly)
{
	ERROR_DECL (error);
	MonoClass *klass;
	int i;

	if (!assemblyb->type_forwarders)
		return;

	for (i = 0; i < mono_array_length_internal (assemblyb->type_forwarders); ++i) {
		MonoReflectionType *t = mono_array_get_internal (assemblyb->type_forwarders, MonoReflectionType *, i);
		MonoType *type;
		if (!t)
			continue;

		type = mono_reflection_type_get_handle (t, error);
		mono_error_assert_ok (error);
		g_assert (type);

		klass = mono_class_from_mono_type_internal (type);

		add_exported_type (assemblyb, assembly, klass, 0);
	}
}

#define align_pointer(base,p)\
	do {\
		guint32 __diff = (unsigned char*)(p)-(unsigned char*)(base);\
		if (__diff & 3)\
			(p) += 4 - (__diff & 3);\
	} while (0)

static int
compare_constants (const void *a, const void *b)
{
	const guint32 *a_values = (const guint32 *)a;
	const guint32 *b_values = (const guint32 *)b;
	return a_values [MONO_CONSTANT_PARENT] - b_values [MONO_CONSTANT_PARENT];
}

static int
compare_semantics (const void *a, const void *b)
{
	const guint32 *a_values = (const guint32 *)a;
	const guint32 *b_values = (const guint32 *)b;
	int assoc = a_values [MONO_METHOD_SEMA_ASSOCIATION] - b_values [MONO_METHOD_SEMA_ASSOCIATION];
	if (assoc)
		return assoc;
	return a_values [MONO_METHOD_SEMA_SEMANTICS] - b_values [MONO_METHOD_SEMA_SEMANTICS];
}

static int
compare_custom_attrs (const void *a, const void *b)
{
	const guint32 *a_values = (const guint32 *)a;
	const guint32 *b_values = (const guint32 *)b;

	return a_values [MONO_CUSTOM_ATTR_PARENT] - b_values [MONO_CUSTOM_ATTR_PARENT];
}

static int
compare_field_marshal (const void *a, const void *b)
{
	const guint32 *a_values = (const guint32 *)a;
	const guint32 *b_values = (const guint32 *)b;

	return a_values [MONO_FIELD_MARSHAL_PARENT] - b_values [MONO_FIELD_MARSHAL_PARENT];
}

static int
compare_nested (const void *a, const void *b)
{
	const guint32 *a_values = (const guint32 *)a;
	const guint32 *b_values = (const guint32 *)b;

	return a_values [MONO_NESTED_CLASS_NESTED] - b_values [MONO_NESTED_CLASS_NESTED];
}

static int
compare_genericparam (const void *a, const void *b)
{
	ERROR_DECL (error);
	const GenericParamTableEntry **a_entry = (const GenericParamTableEntry **) a;
	const GenericParamTableEntry **b_entry = (const GenericParamTableEntry **) b;

	if ((*b_entry)->owner == (*a_entry)->owner) {
		MonoType *a_type = mono_reflection_type_get_handle ((MonoReflectionType*)(*a_entry)->gparam, error);
		mono_error_assert_ok (error);
		MonoType *b_type = mono_reflection_type_get_handle ((MonoReflectionType*)(*b_entry)->gparam, error);
		mono_error_assert_ok (error);
		return 
			mono_type_get_generic_param_num (a_type) -
			mono_type_get_generic_param_num (b_type);
	} else
		return (*a_entry)->owner - (*b_entry)->owner;
}

static int
compare_declsecurity_attrs (const void *a, const void *b)
{
	const guint32 *a_values = (const guint32 *)a;
	const guint32 *b_values = (const guint32 *)b;

	return a_values [MONO_DECL_SECURITY_PARENT] - b_values [MONO_DECL_SECURITY_PARENT];
}

static int
compare_interface_impl (const void *a, const void *b)
{
	const guint32 *a_values = (const guint32 *)a;
	const guint32 *b_values = (const guint32 *)b;

	int klass = a_values [MONO_INTERFACEIMPL_CLASS] - b_values [MONO_INTERFACEIMPL_CLASS];
	if (klass)
		return klass;

	return a_values [MONO_INTERFACEIMPL_INTERFACE] - b_values [MONO_INTERFACEIMPL_INTERFACE];
}

struct StreamDesc {
	const char *name;
	MonoDynamicStream *stream;
};

/*
 * build_compressed_metadata() fills in the blob of data that represents the 
 * raw metadata as it will be saved in the PE file. The five streams are output 
 * and the metadata tables are comnpressed from the guint32 array representation, 
 * to the compressed on-disk format.
 */
static gboolean
build_compressed_metadata (MonoDynamicImage *assembly, MonoError *error)
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
	struct StreamDesc stream_desc [5];

	error_init (error);

	mono_qsort (assembly->gen_params->pdata, assembly->gen_params->len, sizeof (gpointer), compare_genericparam);
	for (i = 0; i < assembly->gen_params->len; i++) {
		GenericParamTableEntry *entry = (GenericParamTableEntry *)g_ptr_array_index (assembly->gen_params, i);
		if (!write_generic_param_entry (assembly, entry, error))
			return FALSE;
	}

	stream_desc [0].name  = "#~";
	stream_desc [0].stream = &assembly->tstream;
	stream_desc [1].name  = "#Strings";
	stream_desc [1].stream = &assembly->sheap;
	stream_desc [2].name  = "#US";
	stream_desc [2].stream = &assembly->us;
	stream_desc [3].name  = "#Blob";
	stream_desc [3].stream = &assembly->blob;
	stream_desc [4].name  = "#GUID";
	stream_desc [4].stream = &assembly->guid;
	
	/* tables that are sorted */
	sorted_mask = ((guint64)1 << MONO_TABLE_CONSTANT) | ((guint64)1 << MONO_TABLE_FIELDMARSHAL)
		| ((guint64)1 << MONO_TABLE_METHODSEMANTICS) | ((guint64)1 << MONO_TABLE_CLASSLAYOUT)
		| ((guint64)1 << MONO_TABLE_FIELDLAYOUT) | ((guint64)1 << MONO_TABLE_FIELDRVA)
		| ((guint64)1 << MONO_TABLE_IMPLMAP) | ((guint64)1 << MONO_TABLE_NESTEDCLASS)
		| ((guint64)1 << MONO_TABLE_METHODIMPL) | ((guint64)1 << MONO_TABLE_CUSTOMATTRIBUTE)
		| ((guint64)1 << MONO_TABLE_DECLSECURITY) | ((guint64)1 << MONO_TABLE_GENERICPARAM)
		| ((guint64)1 << MONO_TABLE_INTERFACEIMPL);
	
	/* Compute table sizes */
	/* the MonoImage has already been created in mono_reflection_dynimage_basic_init() */
	meta = &assembly->image;

	/* sizes should be multiple of 4 */
	mono_dynstream_data_align (&assembly->blob);
	mono_dynstream_data_align (&assembly->guid);
	mono_dynstream_data_align (&assembly->sheap);
	mono_dynstream_data_align (&assembly->us);

	/* Setup the info used by compute_sizes () */
	meta->idx_blob_wide = assembly->blob.index >= 65536 ? 1 : 0;
	meta->idx_guid_wide = assembly->guid.index >= 65536 ? 1 : 0;
	meta->idx_string_wide = assembly->sheap.index >= 65536 ? 1 : 0;

	meta_size += assembly->blob.index;
	meta_size += assembly->guid.index;
	meta_size += assembly->sheap.index;
	meta_size += assembly->us.index;

	for (i=0; i < MONO_TABLE_NUM; ++i)
		meta->tables [i].rows = assembly->tables [i].rows;
	
	for (i = 0; i < MONO_TABLE_NUM; i++){
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
	/* make multiple of 4 */
	heapt_size += 3;
	heapt_size &= ~3;
	meta_size += heapt_size;
	meta->raw_metadata = (char *)g_malloc0 (meta_size);
	p = (unsigned char*)meta->raw_metadata;
	/* the metadata signature */
	*p++ = 'B'; *p++ = 'S'; *p++ = 'J'; *p++ = 'B';
	/* version numbers and 4 bytes reserved */
	int16val = (guint16*)p;
	*int16val++ = GUINT16_TO_LE (meta->md_version_major);
	*int16val = GUINT16_TO_LE (meta->md_version_minor);
	p += 8;
	/* version string */
	int32val = (guint32*)p;
	*int32val = GUINT32_TO_LE ((strlen (meta->version) + 3) & (~3)); /* needs to be multiple of 4 */
	p += 4;
	memcpy (p, meta->version, strlen (meta->version));
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
		strcpy ((char*)p, stream_desc [i].name);
		p += strlen (stream_desc [i].name) + 1;
		align_pointer (meta->raw_metadata, p);
	}
	/* 
	 * now copy the data, the table stream header and contents goes first.
	 */
	g_assert ((p - (unsigned char*)meta->raw_metadata) < assembly->tstream.offset);
	p = (guchar*)meta->raw_metadata + assembly->tstream.offset;
	int32val = (guint32*)p;
	*int32val = GUINT32_TO_LE (0); /* reserved */
	p += 4;

	*p++ = 2; /* version */
	*p++ = 0;

	if (meta->idx_string_wide)
		*p |= 0x01;
	if (meta->idx_guid_wide)
		*p |= 0x02;
	if (meta->idx_blob_wide)
		*p |= 0x04;
	++p;
	*p++ = 1; /* reserved */
	int64val = (guint64*)p;
	*int64val++ = GUINT64_TO_LE (valid_mask);
	*int64val++ = GUINT64_TO_LE (valid_mask & sorted_mask); /* bitvector of sorted tables  */
	p += 16;
	int32val = (guint32*)p;
	for (i = 0; i < MONO_TABLE_NUM; i++){
		if (meta->tables [i].rows == 0)
			continue;
		*int32val++ = GUINT32_TO_LE (meta->tables [i].rows);
	}
	p = (unsigned char*)int32val;

	/* sort the tables that still need sorting */
	table = &assembly->tables [MONO_TABLE_CONSTANT];
	if (table->rows)
		mono_qsort (table->values + MONO_CONSTANT_SIZE, table->rows, sizeof (guint32) * MONO_CONSTANT_SIZE, compare_constants);
	table = &assembly->tables [MONO_TABLE_METHODSEMANTICS];
	if (table->rows)
		mono_qsort (table->values + MONO_METHOD_SEMA_SIZE, table->rows, sizeof (guint32) * MONO_METHOD_SEMA_SIZE, compare_semantics);
	table = &assembly->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	if (table->rows)
		mono_qsort (table->values + MONO_CUSTOM_ATTR_SIZE, table->rows, sizeof (guint32) * MONO_CUSTOM_ATTR_SIZE, compare_custom_attrs);
	table = &assembly->tables [MONO_TABLE_FIELDMARSHAL];
	if (table->rows)
		mono_qsort (table->values + MONO_FIELD_MARSHAL_SIZE, table->rows, sizeof (guint32) * MONO_FIELD_MARSHAL_SIZE, compare_field_marshal);
	table = &assembly->tables [MONO_TABLE_NESTEDCLASS];
	if (table->rows)
		mono_qsort (table->values + MONO_NESTED_CLASS_SIZE, table->rows, sizeof (guint32) * MONO_NESTED_CLASS_SIZE, compare_nested);
	/* Section 21.11 DeclSecurity in Partition II doesn't specify this to be sorted by MS implementation requires it */
	table = &assembly->tables [MONO_TABLE_DECLSECURITY];
	if (table->rows)
		mono_qsort (table->values + MONO_DECL_SECURITY_SIZE, table->rows, sizeof (guint32) * MONO_DECL_SECURITY_SIZE, compare_declsecurity_attrs);
	table = &assembly->tables [MONO_TABLE_INTERFACEIMPL];
	if (table->rows)
		mono_qsort (table->values + MONO_INTERFACEIMPL_SIZE, table->rows, sizeof (guint32) * MONO_INTERFACEIMPL_SIZE, compare_interface_impl);

	/* compress the tables */
	for (i = 0; i < MONO_TABLE_NUM; i++){
		int row, col;
		guint32 *values;
		guint32 bitfield = meta->tables [i].size_bitfield;
		if (!meta->tables [i].rows)
			continue;
		if (assembly->tables [i].columns != mono_metadata_table_count (bitfield))
			g_error ("col count mismatch in %d: %d %d", i, assembly->tables [i].columns, mono_metadata_table_count (bitfield));
		meta->tables [i].base = (char*)p;
		for (row = 1; row <= meta->tables [i].rows; ++row) {
			values = assembly->tables [i].values + row * assembly->tables [i].columns;
			for (col = 0; col < assembly->tables [i].columns; ++col) {
				switch (mono_metadata_table_size (bitfield, col)) {
				case 1:
					*p++ = values [col];
					break;
				case 2:
					*p++ = values [col] & 0xff;
					*p++ = (values [col] >> 8) & 0xff;
					break;
				case 4:
					*p++ = values [col] & 0xff;
					*p++ = (values [col] >> 8) & 0xff;
					*p++ = (values [col] >> 16) & 0xff;
					*p++ = (values [col] >> 24) & 0xff;
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

	return TRUE;
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
fixup_method (MonoReflectionILGen *ilgen, gpointer value, MonoDynamicImage *assembly)
{
	guint32 code_idx = GPOINTER_TO_UINT (value);
	MonoReflectionILTokenInfo *iltoken;
	MonoReflectionTypeBuilder *tb;
	MonoReflectionArrayMethod *am;
	guint32 i, idx = 0;
	unsigned char *target;

	for (i = 0; i < ilgen->num_token_fixups; ++i) {
		iltoken = (MonoReflectionILTokenInfo *)mono_array_addr_with_size_internal (ilgen->token_fixups, sizeof (MonoReflectionILTokenInfo), i);
		target = (guchar*)assembly->code.data + code_idx + iltoken->code_pos;
		MonoClass *iltoken_member_class = mono_object_class (iltoken->member);
		const char *iltoken_member_class_name = m_class_get_name (iltoken_member_class);
		switch (target [3]) {
		case MONO_TABLE_FIELD:
			if (!strcmp (iltoken_member_class_name, "FieldBuilder")) {
				g_assert_not_reached ();
			} else if (!strcmp (iltoken_member_class_name, "RuntimeFieldInfo")) {
				MonoClassField *f = ((MonoReflectionField*)iltoken->member)->field;
				idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->field_to_table_idx, f));
			} else {
				g_assert_not_reached ();
			}
			break;
		case MONO_TABLE_METHOD:
			if (!strcmp (iltoken_member_class_name, "MethodBuilder")) {
				g_assert_not_reached ();
			} else if (!strcmp (iltoken_member_class_name, "ConstructorBuilder")) {
				g_assert_not_reached ();
			} else if (!strcmp (iltoken_member_class_name, "RuntimeMethodInfo") ||
					   !strcmp (iltoken_member_class_name, "RuntimeConstructorInfo")) {
				MonoMethod *m = ((MonoReflectionMethod*)iltoken->member)->method;
				idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->method_to_table_idx, m));
			} else {
				g_assert_not_reached ();
			}
			break;
		case MONO_TABLE_TYPEDEF:
			if (!strcmp (iltoken_member_class_name, "TypeBuilder")) {
				g_assert_not_reached ();
			} else if (!strcmp (iltoken_member_class_name, "RuntimeType")) {
				MonoClass *k = mono_class_from_mono_type_internal (((MonoReflectionType*)iltoken->member)->type);
				MonoObject *obj = &mono_class_get_ref_info_raw (k)->type.object; /* FIXME use handles */
				g_assert (obj);
				g_assert (!strcmp (m_class_get_name (mono_object_class (obj)), "TypeBuilder"));
				tb = (MonoReflectionTypeBuilder*)obj;
				idx = tb->table_idx;
			} else {
				g_assert_not_reached ();
			}
			break;
		case MONO_TABLE_TYPEREF:
			g_assert (!strcmp (iltoken_member_class_name, "RuntimeType"));
			MonoClass *k;
			k = mono_class_from_mono_type_internal (((MonoReflectionType*)iltoken->member)->type);
			MonoObject *obj;
			obj = &mono_class_get_ref_info_raw (k)->type.object; /* FIXME use handles */
			g_assert (obj);
			g_assert (!strcmp (m_class_get_name (mono_object_class (obj)), "TypeBuilder"));
			g_assert (((MonoReflectionTypeBuilder*)obj)->module->dynamic_image != assembly);
			continue;
		case MONO_TABLE_MEMBERREF:
			if (!strcmp (iltoken_member_class_name, "MonoArrayMethod")) {
				am = (MonoReflectionArrayMethod*)iltoken->member;
				idx = am->table_idx;
			} else if (!strcmp (iltoken_member_class_name, "RuntimeMethodInfo") ||
					   !strcmp (iltoken_member_class_name, "RuntimeConstructorInfo")) {
				MonoMethod *m = ((MonoReflectionMethod*)iltoken->member)->method;
				g_assert (mono_class_is_ginst (m->klass) || mono_class_is_gtd (m->klass));
				continue;
			} else if (!strcmp (iltoken_member_class_name, "FieldBuilder")) {
				g_assert_not_reached ();
				continue;
			} else if (!strcmp (iltoken_member_class_name, "RuntimeFieldInfo")) {
				continue;
			} else if (!strcmp (iltoken_member_class_name, "MethodBuilder") ||
					!strcmp (iltoken_member_class_name, "ConstructorBuilder")) {
				g_assert_not_reached ();
				continue;
			} else if (!strcmp (iltoken_member_class_name, "FieldOnTypeBuilderInst")) {
				g_assert_not_reached ();
				continue;
			} else if (!strcmp (iltoken_member_class_name, "MethodOnTypeBuilderInst")) {
				g_assert_not_reached ();
				continue;
			} else if (!strcmp (iltoken_member_class_name, "ConstructorOnTypeBuilderInst")) {
				g_assert_not_reached ();
				continue;
			} else {
				g_assert_not_reached ();
			}
			break;
		case MONO_TABLE_METHODSPEC:
			if (!strcmp (iltoken_member_class_name, "RuntimeMethodInfo")) {
				MonoMethod *m = ((MonoReflectionMethod*)iltoken->member)->method;
				g_assert (mono_method_signature_internal (m)->generic_param_count);
				continue;
			} else if (!strcmp (iltoken_member_class_name, "MethodBuilder")) {
				g_assert_not_reached ();
				continue;
			} else if (!strcmp (iltoken_member_class_name, "MethodOnTypeBuilderInst")) {
				g_assert_not_reached ();
				continue;
			} else {
				g_assert_not_reached ();
			}
			break;
		case MONO_TABLE_TYPESPEC:
			if (!strcmp (iltoken_member_class_name, "RuntimeType")) {
				continue;
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
 * fixup_cattrs:
 *
 *   The CUSTOM_ATTRIBUTE table might contain METHODDEF tokens whose final
 * value is not known when the table is emitted.
 */
static void
fixup_cattrs (MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 type, i, idx, token;
	MonoObject *ctor;

	table = &assembly->tables [MONO_TABLE_CUSTOMATTRIBUTE];

	for (i = 0; i < table->rows; ++i) {
		values = table->values + ((i + 1) * MONO_CUSTOM_ATTR_SIZE);

		type = values [MONO_CUSTOM_ATTR_TYPE];
		if ((type & MONO_CUSTOM_ATTR_TYPE_MASK) == MONO_CUSTOM_ATTR_TYPE_METHODDEF) {
			idx = type >> MONO_CUSTOM_ATTR_TYPE_BITS;
			token = mono_metadata_make_token (MONO_TABLE_METHOD, idx);
			ctor = (MonoObject *)mono_g_hash_table_lookup (assembly->remapped_tokens, GUINT_TO_POINTER (token));
			g_assert (ctor);

			if (!strcmp (m_class_get_name (mono_object_class (ctor)), "RuntimeConstructorInfo")) {
				MonoMethod *m = ((MonoReflectionMethod*)ctor)->method;
				idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->method_to_table_idx, m));
				values [MONO_CUSTOM_ATTR_TYPE] = (idx << MONO_CUSTOM_ATTR_TYPE_BITS) | MONO_CUSTOM_ATTR_TYPE_METHODDEF;
			} else if (!strcmp (m_class_get_name (mono_object_class (ctor)), "ConstructorBuilder")) {
				MonoMethod *m = ((MonoReflectionCtorBuilder*)ctor)->mhandle;
				idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->method_to_table_idx, m));
				values [MONO_CUSTOM_ATTR_TYPE] = (idx << MONO_CUSTOM_ATTR_TYPE_BITS) | MONO_CUSTOM_ATTR_TYPE_METHODDEF;
			}
		}
	}
}

static gboolean
assembly_add_resource_manifest (MonoReflectionModuleBuilder *mb, MonoDynamicImage *assembly, MonoReflectionResource *rsrc, guint32 implementation, MonoError *error)
{
	MonoDynamicTable *table;
	guint32 *values;

	error_init (error);

	table = &assembly->tables [MONO_TABLE_MANIFESTRESOURCE];
	table->rows++;
	alloc_table (table, table->rows);
	values = table->values + table->next_idx * MONO_MANIFEST_SIZE;
	values [MONO_MANIFEST_OFFSET] = rsrc->offset;
	values [MONO_MANIFEST_FLAGS] = rsrc->attrs;
	values [MONO_MANIFEST_NAME] = string_heap_insert_mstring (&assembly->sheap, rsrc->name, error);
	return_val_if_nok (error, FALSE);
	values [MONO_MANIFEST_IMPLEMENTATION] = implementation;
	table->next_idx++;
	return TRUE;
}

static gboolean
assembly_add_resource (MonoReflectionModuleBuilder *mb, MonoDynamicImage *assembly, MonoReflectionResource *rsrc, MonoError *error)
{
	MonoDynamicTable *table;
	guint32 *values;
	char blob_size [6];
	guchar hash [20];
	char *b = blob_size;
	char *name, *sname;
	guint32 idx, offset;

	error_init (error);

	if (rsrc->filename) {
		name = mono_string_to_utf8_checked_internal (rsrc->filename, error);
		return_val_if_nok (error, FALSE);
		sname = g_path_get_basename (name);
	
		table = &assembly->tables [MONO_TABLE_FILE];
		table->rows++;
		alloc_table (table, table->rows);
		values = table->values + table->next_idx * MONO_FILE_SIZE;
		values [MONO_FILE_FLAGS] = FILE_CONTAINS_NO_METADATA;
		values [MONO_FILE_NAME] = string_heap_insert (&assembly->sheap, sname);
		g_free (sname);

		mono_sha1_get_digest_from_file (name, hash);
		mono_metadata_encode_value (20, b, &b);
		values [MONO_FILE_HASH_VALUE] = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
		mono_image_add_stream_data (&assembly->blob, (char*)hash, 20);
		g_free (name);
		idx = table->next_idx++;
		rsrc->offset = 0;
		idx = MONO_IMPLEMENTATION_FILE | (idx << MONO_IMPLEMENTATION_BITS);
	} else {
		char sizebuf [4];
		char *data;
		guint len;
		if (rsrc->data) {
			data = mono_array_addr_internal (rsrc->data, char, 0);
			len = mono_array_length_internal (rsrc->data);
		} else {
			data = NULL;
			len = 0;
		}
		offset = len;
		sizebuf [0] = offset; sizebuf [1] = offset >> 8;
		sizebuf [2] = offset >> 16; sizebuf [3] = offset >> 24;
		rsrc->offset = mono_image_add_stream_data (&assembly->resources, sizebuf, 4);
		mono_image_add_stream_data (&assembly->resources, data, len);

		if (!mb->is_main)
			/* 
			 * The entry should be emitted into the MANIFESTRESOURCE table of 
			 * the main module, but that needs to reference the FILE table
			 * which isn't emitted yet.
			 */
			return TRUE;
		else
			idx = 0;
	}

	return assembly_add_resource_manifest (mb, assembly, rsrc, idx, error);
}

static gboolean
set_version_from_string (MonoString *version, guint32 *values, MonoError *error)
{
	gchar *ver, *p, *str;
	guint32 i;
	
	error_init (error);

	values [MONO_ASSEMBLY_MAJOR_VERSION] = 0;
	values [MONO_ASSEMBLY_MINOR_VERSION] = 0;
	values [MONO_ASSEMBLY_REV_NUMBER] = 0;
	values [MONO_ASSEMBLY_BUILD_NUMBER] = 0;
	if (!version)
		return TRUE;
	ver = str = mono_string_to_utf8_checked_internal (version, error);
	return_val_if_nok (error, FALSE);
	for (i = 0; i < 4; ++i) {
		values [MONO_ASSEMBLY_MAJOR_VERSION + i] = strtol (ver, &p, 10);
		switch (*p) {
		case '.':
			p++;
			break;
		case '*':
			/* handle Revision and Build */
			p++;
			break;
		}
		ver = p;
	}
	g_free (str);
	return TRUE;
}

static guint32
load_public_key (MonoArray *pkey, MonoDynamicImage *assembly) {
	gsize len;
	guint32 token = 0;
	char blob_size [6];
	char *b = blob_size;

	if (!pkey)
		return token;

	len = mono_array_length_internal (pkey);
	mono_metadata_encode_value (len, b, &b);
	token = mono_image_add_stream_data (&assembly->blob, blob_size, b - blob_size);
	mono_image_add_stream_data (&assembly->blob, mono_array_addr_internal (pkey, char, 0), len);

	assembly->public_key = (guint8 *)g_malloc (len);
	memcpy (assembly->public_key, mono_array_addr_internal (pkey, char, 0), len);
	assembly->public_key_len = len;

	/* Special case: check for ECMA key (16 bytes) */
	if ((len == MONO_ECMA_KEY_LENGTH) && mono_is_ecma_key (mono_array_addr_internal (pkey, char, 0), len)) {
		/* In this case we must reserve 128 bytes (1024 bits) for the signature */
		assembly->strong_name_size = MONO_DEFAULT_PUBLIC_KEY_LENGTH;
	} else if (len >= MONO_PUBLIC_KEY_HEADER_LENGTH + MONO_MINIMUM_PUBLIC_KEY_LENGTH) {
		/* minimum key size (in 2.0) is 384 bits */
		assembly->strong_name_size = len - MONO_PUBLIC_KEY_HEADER_LENGTH;
	} else {
		/* FIXME - verifier */
		g_warning ("Invalid public key length: %d bits (total: %d)", (int)MONO_PUBLIC_KEY_BIT_SIZE (len), (int)len);
		assembly->strong_name_size = MONO_DEFAULT_PUBLIC_KEY_LENGTH; /* to be safe */
	}
	assembly->strong_name = (char *)g_malloc0 (assembly->strong_name_size);

	return token;
}

static gboolean
mono_image_emit_manifest (MonoReflectionModuleBuilder *moduleb, MonoError *error)
{
	MonoDynamicTable *table;
	MonoDynamicImage *assembly;
	MonoReflectionAssemblyBuilder *assemblyb;
	MonoDomain *domain;
	guint32 *values;
	int i;
	guint32 module_index;

	error_init (error);

	assemblyb = moduleb->assemblyb;
	assembly = moduleb->dynamic_image;
	domain = mono_object_domain (assemblyb);

	/* Emit ASSEMBLY table */
	table = &assembly->tables [MONO_TABLE_ASSEMBLY];
	alloc_table (table, 1);
	values = table->values + MONO_ASSEMBLY_SIZE;
	values [MONO_ASSEMBLY_HASH_ALG] = assemblyb->algid? assemblyb->algid: ASSEMBLY_HASH_SHA1;
	values [MONO_ASSEMBLY_NAME] = string_heap_insert_mstring (&assembly->sheap, assemblyb->name, error);
	return_val_if_nok (error, FALSE);
	if (assemblyb->culture) {
		values [MONO_ASSEMBLY_CULTURE] = string_heap_insert_mstring (&assembly->sheap, assemblyb->culture, error);
		return_val_if_nok (error, FALSE);
	} else {
		values [MONO_ASSEMBLY_CULTURE] = string_heap_insert (&assembly->sheap, "");
	}
	values [MONO_ASSEMBLY_PUBLIC_KEY] = load_public_key (assemblyb->public_key, assembly);
	values [MONO_ASSEMBLY_FLAGS] = assemblyb->flags;
	if (!set_version_from_string (assemblyb->version, values, error))
		return FALSE;

	/* Emit FILE + EXPORTED_TYPE table */
	module_index = 0;
	for (i = 0; i < mono_array_length_internal (assemblyb->modules); ++i) {
		int j;
		MonoReflectionModuleBuilder *file_module = 
			mono_array_get_internal (assemblyb->modules, MonoReflectionModuleBuilder*, i);
		if (file_module != moduleb) {
			if (!mono_image_fill_file_table (domain, (MonoReflectionModule*)file_module, assembly, error))
				return FALSE;
			module_index ++;
			if (file_module->types) {
				for (j = 0; j < file_module->num_types; ++j) {
					MonoReflectionTypeBuilder *tb = mono_array_get_internal (file_module->types, MonoReflectionTypeBuilder*, j);
					mono_image_fill_export_table (domain, tb, module_index, 0, assembly, error);
					return_val_if_nok (error, FALSE);
				}
			}
		}
	}
	if (assemblyb->loaded_modules) {
		for (i = 0; i < mono_array_length_internal (assemblyb->loaded_modules); ++i) {
			MonoReflectionModule *file_module = 
				mono_array_get_internal (assemblyb->loaded_modules, MonoReflectionModule*, i);
			if (!mono_image_fill_file_table (domain, file_module, assembly, error))
				return FALSE;
			module_index ++;
			mono_image_fill_export_table_from_module (domain, file_module, module_index, assembly);
		}
	}
	if (assemblyb->type_forwarders)
		mono_image_fill_export_table_from_type_forwarders (assemblyb, assembly);

	/* Emit MANIFESTRESOURCE table */
	module_index = 0;
	for (i = 0; i < mono_array_length_internal (assemblyb->modules); ++i) {
		int j;
		MonoReflectionModuleBuilder *file_module = 
			mono_array_get_internal (assemblyb->modules, MonoReflectionModuleBuilder*, i);
		/* The table for the main module is emitted later */
		if (file_module != moduleb) {
			module_index ++;
			if (file_module->resources) {
				int len = mono_array_length_internal (file_module->resources);
				for (j = 0; j < len; ++j) {
					MonoReflectionResource* res = (MonoReflectionResource*)mono_array_addr_internal (file_module->resources, MonoReflectionResource, j);
					if (!assembly_add_resource_manifest (file_module, assembly, res, MONO_IMPLEMENTATION_FILE | (module_index << MONO_IMPLEMENTATION_BITS), error))
						return FALSE;
				}
			}
		}
	}
	return TRUE;
}

#ifndef DISABLE_REFLECTION_EMIT_SAVE

/*
 * Insert into the metadata tables all the info about the TypeBuilder tb.
 * Data in the tables is inserted in a predefined order, since some tables need to be sorted.
 */
static gboolean
mono_image_get_type_info (MonoDomain *domain, MonoReflectionTypeBuilder *tb, MonoDynamicImage *assembly, MonoError *error)
{
	MonoDynamicTable *table;
	guint *values;
	int i, is_object = 0, is_system = 0;
	char *n;

	error_init (error);

	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	values = table->values + tb->table_idx * MONO_TYPEDEF_SIZE;
	values [MONO_TYPEDEF_FLAGS] = tb->attrs;
	n = mono_string_to_utf8_checked_internal (tb->name, error);
	return_val_if_nok (error, FALSE);
	if (strcmp (n, "Object") == 0)
		is_object++;
	values [MONO_TYPEDEF_NAME] = string_heap_insert (&assembly->sheap, n);
	g_free (n);
	n = mono_string_to_utf8_checked_internal (tb->nspace, error);
	return_val_if_nok (error, FALSE);
	if (strcmp (n, "System") == 0)
		is_system++;
	values [MONO_TYPEDEF_NAMESPACE] = string_heap_insert (&assembly->sheap, n);
	g_free (n);
	if (tb->parent && !(is_system && is_object) && 
			!(tb->attrs & TYPE_ATTRIBUTE_INTERFACE)) { /* interfaces don't have a parent */
		MonoType *parent_type = mono_reflection_type_get_handle ((MonoReflectionType*)tb->parent, error);
		return_val_if_nok (error, FALSE);
		values [MONO_TYPEDEF_EXTENDS] = mono_image_typedef_or_ref (assembly, parent_type);
	} else {
		values [MONO_TYPEDEF_EXTENDS] = 0;
	}
	values [MONO_TYPEDEF_FIELD_LIST] = assembly->tables [MONO_TABLE_FIELD].next_idx;
	values [MONO_TYPEDEF_METHOD_LIST] = assembly->tables [MONO_TABLE_METHOD].next_idx;

	/*
	 * if we have explicitlayout or sequentiallayouts, output data in the
	 * ClassLayout table.
	 */
	if (((tb->attrs & TYPE_ATTRIBUTE_LAYOUT_MASK) != TYPE_ATTRIBUTE_AUTO_LAYOUT) &&
			((tb->class_size > 0) || (tb->packing_size > 0))) {
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
		table->rows += mono_array_length_internal (tb->interfaces);
		alloc_table (table, table->rows);
		values = table->values + (i + 1) * MONO_INTERFACEIMPL_SIZE;
		for (i = 0; i < mono_array_length_internal (tb->interfaces); ++i) {
			MonoReflectionType* iface = (MonoReflectionType*) mono_array_get_internal (tb->interfaces, gpointer, i);
			MonoType *iface_type = mono_reflection_type_get_handle (iface, error);
			return_val_if_nok (error, FALSE);
			values [MONO_INTERFACEIMPL_CLASS] = tb->table_idx;
			values [MONO_INTERFACEIMPL_INTERFACE] = mono_image_typedef_or_ref (assembly, iface_type);
			values += MONO_INTERFACEIMPL_SIZE;
		}
	}

	/* handle fields */
	if (tb->fields) {
		table = &assembly->tables [MONO_TABLE_FIELD];
		table->rows += tb->num_fields;
		alloc_table (table, table->rows);
		for (i = 0; i < tb->num_fields; ++i) {
			mono_image_get_field_info (
				mono_array_get_internal (tb->fields, MonoReflectionFieldBuilder*, i), assembly, error);
			return_val_if_nok (error, FALSE);
		}
	}

	/* handle constructors */
	if (tb->ctors) {
		table = &assembly->tables [MONO_TABLE_METHOD];
		table->rows += mono_array_length_internal (tb->ctors);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length_internal (tb->ctors); ++i) {
			if (!mono_image_get_ctor_info (domain,
						       mono_array_get_internal (tb->ctors, MonoReflectionCtorBuilder*, i),
						       assembly, error))
				return FALSE;
		}
	}

	/* handle methods */
	if (tb->methods) {
		table = &assembly->tables [MONO_TABLE_METHOD];
		table->rows += tb->num_methods;
		alloc_table (table, table->rows);
		for (i = 0; i < tb->num_methods; ++i) {
			if (!mono_image_get_method_info (
				    mono_array_get_internal (tb->methods, MonoReflectionMethodBuilder*, i), assembly, error))
				return FALSE;
		}
	}

	/* Do the same with properties etc.. */
	if (tb->events && mono_array_length_internal (tb->events)) {
		table = &assembly->tables [MONO_TABLE_EVENT];
		table->rows += mono_array_length_internal (tb->events);
		alloc_table (table, table->rows);
		table = &assembly->tables [MONO_TABLE_EVENTMAP];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_EVENT_MAP_SIZE;
		values [MONO_EVENT_MAP_PARENT] = tb->table_idx;
		values [MONO_EVENT_MAP_EVENTLIST] = assembly->tables [MONO_TABLE_EVENT].next_idx;
		for (i = 0; i < mono_array_length_internal (tb->events); ++i) {
			mono_image_get_event_info (
				mono_array_get_internal (tb->events, MonoReflectionEventBuilder*, i), assembly, error);
			return_val_if_nok (error, FALSE);
		}
	}
	if (tb->properties && mono_array_length_internal (tb->properties)) {
		table = &assembly->tables [MONO_TABLE_PROPERTY];
		table->rows += mono_array_length_internal (tb->properties);
		alloc_table (table, table->rows);
		table = &assembly->tables [MONO_TABLE_PROPERTYMAP];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_PROPERTY_MAP_SIZE;
		values [MONO_PROPERTY_MAP_PARENT] = tb->table_idx;
		values [MONO_PROPERTY_MAP_PROPERTY_LIST] = assembly->tables [MONO_TABLE_PROPERTY].next_idx;
		for (i = 0; i < mono_array_length_internal (tb->properties); ++i) {
			mono_image_get_property_info (
				mono_array_get_internal (tb->properties, MonoReflectionPropertyBuilder*, i), assembly, error);
			return_val_if_nok (error, FALSE);
		}
	}

	/* handle generic parameters */
	if (tb->generic_params) {
		table = &assembly->tables [MONO_TABLE_GENERICPARAM];
		table->rows += mono_array_length_internal (tb->generic_params);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length_internal (tb->generic_params); ++i) {
			guint32 owner = MONO_TYPEORMETHOD_TYPE | (tb->table_idx << MONO_TYPEORMETHOD_BITS);

			mono_image_get_generic_param_info (
				mono_array_get_internal (tb->generic_params, MonoReflectionGenericParam*, i), owner, assembly);
		}
	}

	mono_image_add_decl_security (assembly, 
		mono_metadata_make_token (MONO_TABLE_TYPEDEF, tb->table_idx), tb->permissions);

	if (tb->subtypes) {
		MonoDynamicTable *ntable;
		
		ntable = &assembly->tables [MONO_TABLE_NESTEDCLASS];
		ntable->rows += mono_array_length_internal (tb->subtypes);
		alloc_table (ntable, ntable->rows);
		values = ntable->values + ntable->next_idx * MONO_NESTED_CLASS_SIZE;

		for (i = 0; i < mono_array_length_internal (tb->subtypes); ++i) {
			MonoReflectionTypeBuilder *subtype = mono_array_get_internal (tb->subtypes, MonoReflectionTypeBuilder*, i);

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

	return TRUE;
}


/*
 * mono_image_build_metadata() will fill the info in all the needed metadata tables
 * for the modulebuilder @moduleb.
 * At the end of the process, method and field tokens are fixed up and the 
 * on-disk compressed metadata representation is created.
 * Return TRUE on success, or FALSE on failure and sets @error
 */
gboolean
mono_image_build_metadata (MonoReflectionModuleBuilder *moduleb, MonoError *error)
{
	MonoDynamicTable *table;
	MonoDynamicImage *assembly;
	MonoReflectionAssemblyBuilder *assemblyb;
	MonoDomain *domain;
	MonoPtrArray types;
	guint32 *values;
	int i, j;

	error_init (error);

	assemblyb = moduleb->assemblyb;
	assembly = moduleb->dynamic_image;
	domain = mono_object_domain (assemblyb);

	if (assembly->text_rva)
		return TRUE;

	assembly->text_rva = START_TEXT_RVA;

	if (moduleb->is_main) {
		mono_image_emit_manifest (moduleb, error);
		return_val_if_nok (error, FALSE);
	}

	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	table->rows = 1; /* .<Module> */
	table->next_idx++;
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

	/* 
	 * handle global methods 
	 * FIXME: test what to do when global methods are defined in multiple modules.
	 */
	if (moduleb->global_methods) {
		table = &assembly->tables [MONO_TABLE_METHOD];
		table->rows += mono_array_length_internal (moduleb->global_methods);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length_internal (moduleb->global_methods); ++i) {
			if (!mono_image_get_method_info (
				    mono_array_get_internal (moduleb->global_methods, MonoReflectionMethodBuilder*, i), assembly, error))
				goto leave;
		}
	}
	if (moduleb->global_fields) {
		table = &assembly->tables [MONO_TABLE_FIELD];
		table->rows += mono_array_length_internal (moduleb->global_fields);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length_internal (moduleb->global_fields); ++i) {
			mono_image_get_field_info (
				mono_array_get_internal (moduleb->global_fields, MonoReflectionFieldBuilder*, i), assembly,
				error);
			goto_if_nok (error, leave);
		}
	}

	table = &assembly->tables [MONO_TABLE_MODULE];
	alloc_table (table, 1);
	mono_image_fill_module_table (domain, moduleb, assembly, error);
	goto_if_nok (error, leave);

	/* Collect all types into a list sorted by their table_idx */
	mono_ptr_array_init (types, moduleb->num_types, MONO_ROOT_SOURCE_REFLECTION, NULL, "Reflection Dynamic Image Type List");

	if (moduleb->types)
		for (i = 0; i < moduleb->num_types; ++i) {
			MonoReflectionTypeBuilder *type = mono_array_get_internal (moduleb->types, MonoReflectionTypeBuilder*, i);
			collect_types (&types, type);
		}

	mono_ptr_array_sort (types, (int (*)(const void *, const void *))compare_types_by_table_idx);
	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	table->rows += mono_ptr_array_size (types);
	alloc_table (table, table->rows);

	/*
	 * Emit type names + namespaces at one place inside the string heap,
	 * so load_class_names () needs to touch fewer pages.
	 */
	for (i = 0; i < mono_ptr_array_size (types); ++i) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mono_ptr_array_get (types, i);
		string_heap_insert_mstring (&assembly->sheap, tb->nspace, error);
		goto_if_nok (error, leave_types);
	}
	for (i = 0; i < mono_ptr_array_size (types); ++i) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mono_ptr_array_get (types, i);
		string_heap_insert_mstring (&assembly->sheap, tb->name, error);
		goto_if_nok (error, leave_types);
	}

	for (i = 0; i < mono_ptr_array_size (types); ++i) {
		MonoReflectionTypeBuilder *type = (MonoReflectionTypeBuilder *)mono_ptr_array_get (types, i);
		if (!mono_image_get_type_info (domain, type, assembly, error))
			goto leave_types;
	}

	/* 
	 * table->rows is already set above and in mono_image_fill_module_table.
	 */
	/* add all the custom attributes at the end, once all the indexes are stable */
	if (!mono_image_add_cattrs (assembly, 1, MONO_CUSTOM_ATTR_ASSEMBLY, assemblyb->cattrs, error))
		goto leave_types;

	/* CAS assembly permissions */
	if (assemblyb->permissions_minimum)
		mono_image_add_decl_security (assembly, mono_metadata_make_token (MONO_TABLE_ASSEMBLY, 1), assemblyb->permissions_minimum);
	if (assemblyb->permissions_optional)
		mono_image_add_decl_security (assembly, mono_metadata_make_token (MONO_TABLE_ASSEMBLY, 1), assemblyb->permissions_optional);
	if (assemblyb->permissions_refused)
		mono_image_add_decl_security (assembly, mono_metadata_make_token (MONO_TABLE_ASSEMBLY, 1), assemblyb->permissions_refused);

	if (!module_add_cattrs (assembly, moduleb, error))
		goto leave_types;

	/* fixup tokens */
	mono_g_hash_table_foreach (assembly->token_fixups, (GHFunc)fixup_method, assembly);

	/* Create the MethodImpl table.  We do this after emitting all methods so we already know
	 * the final tokens and don't need another fixup pass. */

	if (moduleb->global_methods) {
		for (i = 0; i < mono_array_length_internal (moduleb->global_methods); ++i) {
			MonoReflectionMethodBuilder *mb = mono_array_get_internal (
				moduleb->global_methods, MonoReflectionMethodBuilder*, i);
			if (!mono_image_add_methodimpl (assembly, mb, error))
				goto leave_types;
		}
	}

	for (i = 0; i < mono_ptr_array_size (types); ++i) {
		MonoReflectionTypeBuilder *type = (MonoReflectionTypeBuilder *)mono_ptr_array_get (types, i);
		if (type->methods) {
			for (j = 0; j < type->num_methods; ++j) {
				MonoReflectionMethodBuilder *mb = mono_array_get_internal (
					type->methods, MonoReflectionMethodBuilder*, j);

				if (!mono_image_add_methodimpl (assembly, mb, error))
					goto leave_types;
			}
		}
	}

	fixup_cattrs (assembly);

leave_types:
	mono_ptr_array_destroy (types);
leave:

	return is_ok (error);
}

#else /* DISABLE_REFLECTION_EMIT_SAVE */

gboolean
mono_image_build_metadata (MonoReflectionModuleBuilder *moduleb, MonoError *error)
{
	g_error ("This mono runtime was configured with --enable-minimal=reflection_emit_save, so saving of dynamic assemblies is not supported.");
}

#endif /* DISABLE_REFLECTION_EMIT_SAVE */

#ifndef DISABLE_REFLECTION_EMIT_SAVE

static int
calc_section_size (MonoDynamicImage *assembly)
{
	int nsections = 0;

	/* alignment constraints */
	mono_image_add_stream_zero (&assembly->code, 4 - (assembly->code.index % 4));
	g_assert ((assembly->code.index % 4) == 0);
	assembly->meta_size += 3;
	assembly->meta_size &= ~3;
	mono_image_add_stream_zero (&assembly->resources, 4 - (assembly->resources.index % 4));
	g_assert ((assembly->resources.index % 4) == 0);

	assembly->sections [MONO_SECTION_TEXT].size = assembly->meta_size + assembly->code.index + assembly->resources.index + assembly->strong_name_size;
	assembly->sections [MONO_SECTION_TEXT].attrs = SECT_FLAGS_HAS_CODE | SECT_FLAGS_MEM_EXECUTE | SECT_FLAGS_MEM_READ;
	nsections++;

	if (assembly->win32_res) {
		guint32 res_size = (assembly->win32_res_size + 3) & ~3;

		assembly->sections [MONO_SECTION_RSRC].size = res_size;
		assembly->sections [MONO_SECTION_RSRC].attrs = SECT_FLAGS_HAS_INITIALIZED_DATA | SECT_FLAGS_MEM_READ;
		nsections++;
	}

	assembly->sections [MONO_SECTION_RELOC].size = 12;
	assembly->sections [MONO_SECTION_RELOC].attrs = SECT_FLAGS_MEM_READ | SECT_FLAGS_MEM_DISCARDABLE | SECT_FLAGS_HAS_INITIALIZED_DATA;
	nsections++;

	return nsections;
}

typedef struct {
	guint32 id;
	guint32 offset;
	GSList *children;
	MonoReflectionWin32Resource *win32_res; /* Only for leaf nodes */
} ResTreeNode;

static int
resource_tree_compare_by_id (gconstpointer a, gconstpointer b)
{
	ResTreeNode *t1 = (ResTreeNode*)a;
	ResTreeNode *t2 = (ResTreeNode*)b;

	return t1->id - t2->id;
}

/*
 * resource_tree_create:
 *
 *  Organize the resources into a resource tree.
 */
static ResTreeNode *
resource_tree_create (MonoArray *win32_resources)
{
	ResTreeNode *tree, *res_node, *type_node, *lang_node;
	GSList *l;
	int i;

	tree = g_new0 (ResTreeNode, 1);
	
	for (i = 0; i < mono_array_length_internal (win32_resources); ++i) {
		MonoReflectionWin32Resource *win32_res =
			(MonoReflectionWin32Resource*)mono_array_addr_internal (win32_resources, MonoReflectionWin32Resource, i);

		/* Create node */

		/* FIXME: BUG: this stores managed references in unmanaged memory */
		lang_node = g_new0 (ResTreeNode, 1);
		lang_node->id = win32_res->lang_id;
		lang_node->win32_res = win32_res;

		/* Create type node if neccesary */
		type_node = NULL;
		for (l = tree->children; l; l = l->next)
			if (((ResTreeNode*)(l->data))->id == win32_res->res_type) {
				type_node = (ResTreeNode*)l->data;
				break;
			}

		if (!type_node) {
			type_node = g_new0 (ResTreeNode, 1);
			type_node->id = win32_res->res_type;

			/* 
			 * The resource types have to be sorted otherwise
			 * Windows Explorer can't display the version information.
			 */
			tree->children = g_slist_insert_sorted (tree->children, 
				type_node, resource_tree_compare_by_id);
		}

		/* Create res node if neccesary */
		res_node = NULL;
		for (l = type_node->children; l; l = l->next)
			if (((ResTreeNode*)(l->data))->id == win32_res->res_id) {
				res_node = (ResTreeNode*)l->data;
				break;
			}

		if (!res_node) {
			res_node = g_new0 (ResTreeNode, 1);
			res_node->id = win32_res->res_id;
			type_node->children = g_slist_append (type_node->children, res_node);
		}

		res_node->children = g_slist_append (res_node->children, lang_node);
	}

	return tree;
}

/*
 * resource_tree_encode:
 * 
 *   Encode the resource tree into the format used in the PE file.
 */
static void
resource_tree_encode (ResTreeNode *node, char *begin, char *p, char **endbuf)
{
	char *entries;
	MonoPEResourceDir dir;
	MonoPEResourceDirEntry dir_entry;
	MonoPEResourceDataEntry data_entry;
	GSList *l;
	guint32 res_id_entries;

	/*
	 * For the format of the resource directory, see the article
	 * "An In-Depth Look into the Win32 Portable Executable File Format" by
	 * Matt Pietrek
	 */

	memset (&dir, 0, sizeof (dir));
	memset (&dir_entry, 0, sizeof (dir_entry));
	memset (&data_entry, 0, sizeof (data_entry));

	g_assert (sizeof (dir) == 16);
	g_assert (sizeof (dir_entry) == 8);
	g_assert (sizeof (data_entry) == 16);

	node->offset = p - begin;

	/* IMAGE_RESOURCE_DIRECTORY */
	res_id_entries = g_slist_length (node->children);
	dir.res_id_entries = GUINT16_TO_LE (res_id_entries);

	memcpy (p, &dir, sizeof (dir));
	p += sizeof (dir);

	/* Reserve space for entries */
	entries = p;
	p += sizeof (dir_entry) * res_id_entries;

	/* Write children */
	for (l = node->children; l; l = l->next) {
		ResTreeNode *child = (ResTreeNode*)l->data;

		if (child->win32_res) {
			guint32 size;

			child->offset = p - begin;

			/* IMAGE_RESOURCE_DATA_ENTRY */
			data_entry.rde_data_offset = GUINT32_TO_LE (p - begin + sizeof (data_entry));
			size = mono_array_length_internal (child->win32_res->res_data);
			data_entry.rde_size = GUINT32_TO_LE (size);

			memcpy (p, &data_entry, sizeof (data_entry));
			p += sizeof (data_entry);

			memcpy (p, mono_array_addr_internal (child->win32_res->res_data, char, 0), size);
			p += size;
		} else {
			resource_tree_encode (child, begin, p, &p);
		}
	}

	/* IMAGE_RESOURCE_ENTRY */
	for (l = node->children; l; l = l->next) {
		ResTreeNode *child = (ResTreeNode*)l->data;

		MONO_PE_RES_DIR_ENTRY_SET_NAME (dir_entry, FALSE, child->id);
		MONO_PE_RES_DIR_ENTRY_SET_DIR (dir_entry, !child->win32_res, child->offset);

		memcpy (entries, &dir_entry, sizeof (dir_entry));
		entries += sizeof (dir_entry);
	}

	*endbuf = p;
}

static void
resource_tree_free (ResTreeNode * node)
{
	GSList * list;
	for (list = node->children; list; list = list->next)
		resource_tree_free ((ResTreeNode*)list->data);
	g_slist_free(node->children);
	g_free (node);
}

static void
assembly_add_win32_resources (MonoDynamicImage *assembly, MonoReflectionAssemblyBuilder *assemblyb)
{
	char *buf;
	char *p;
	guint32 size, i;
	MonoReflectionWin32Resource *win32_res;
	ResTreeNode *tree;

	if (!assemblyb->win32_resources)
		return;

	/*
	 * Resources are stored in a three level tree inside the PE file.
	 * - level one contains a node for each type of resource
	 * - level two contains a node for each resource
	 * - level three contains a node for each instance of a resource for a
	 *   specific language.
	 */

	tree = resource_tree_create (assemblyb->win32_resources);

	/* Estimate the size of the encoded tree */
	size = 0;
	for (i = 0; i < mono_array_length_internal (assemblyb->win32_resources); ++i) {
		win32_res = (MonoReflectionWin32Resource*)mono_array_addr_internal (assemblyb->win32_resources, MonoReflectionWin32Resource, i);
		size += mono_array_length_internal (win32_res->res_data);
	}
	/* Directory structure */
	size += mono_array_length_internal (assemblyb->win32_resources) * 256;
	p = buf = (char *)g_malloc (size);

	resource_tree_encode (tree, p, p, &p);

	g_assert (p - buf <= size);

	assembly->win32_res = (char *)g_malloc (p - buf);
	assembly->win32_res_size = p - buf;
	memcpy (assembly->win32_res, buf, p - buf);

	g_free (buf);
	resource_tree_free (tree);
}

static void
fixup_resource_directory (char *res_section, char *p, guint32 rva)
{
	MonoPEResourceDir *dir = (MonoPEResourceDir*)p;
	int i;

	p += sizeof (MonoPEResourceDir);
	for (i = 0; i < GUINT16_FROM_LE (dir->res_named_entries) + GUINT16_FROM_LE (dir->res_id_entries); ++i) {
		MonoPEResourceDirEntry *dir_entry = (MonoPEResourceDirEntry*)p;
		char *child = res_section + MONO_PE_RES_DIR_ENTRY_DIR_OFFSET (*dir_entry);
		if (MONO_PE_RES_DIR_ENTRY_IS_DIR (*dir_entry)) {
			fixup_resource_directory (res_section, child, rva);
		} else {
			MonoPEResourceDataEntry *data_entry = (MonoPEResourceDataEntry*)child;
			data_entry->rde_data_offset = GUINT32_TO_LE (GUINT32_FROM_LE (data_entry->rde_data_offset) + rva);
		}

		p += sizeof (MonoPEResourceDirEntry);
	}
}

static void
checked_write_file (HANDLE f, gconstpointer buffer, guint32 numbytes)
{
	guint32 dummy;
	gint32 win32error = 0;
	if (!mono_w32file_write (f, buffer, numbytes, &dummy, &win32error))
		g_error ("mono_w32file_write returned %d\n", win32error);
}

/*
 * mono_image_create_pefile:
 * @mb: a module builder object
 * 
 * This function creates the PE-COFF header, the image sections, the CLI header  * etc. all the data is written in
 * assembly->pefile where it can be easily retrieved later in chunks.
 */
gboolean
mono_image_create_pefile (MonoReflectionModuleBuilder *mb, HANDLE file, MonoError *error)
{
	MonoMSDOSHeader *msdos;
	MonoDotNetHeader *header;
	MonoSectionTable *section;
	MonoCLIHeader *cli_header;
	guint32 size, image_size, virtual_base, text_offset;
	guint32 header_start, section_start, file_offset, virtual_offset;
	MonoDynamicImage *assembly;
	MonoReflectionAssemblyBuilder *assemblyb;
	MonoDynamicStream pefile_stream = {0};
	MonoDynamicStream *pefile = &pefile_stream;
	int i, nsections;
	guint32 *rva, value;
	guchar *p;
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

	error_init (error);

	assemblyb = mb->assemblyb;

	mono_reflection_dynimage_basic_init (assemblyb, error);
	return_val_if_nok (error, FALSE);
	assembly = mb->dynamic_image;

	assembly->pe_kind = assemblyb->pe_kind;
	assembly->machine = assemblyb->machine;
	((MonoDynamicImage*)assemblyb->dynamic_assembly->assembly.image)->pe_kind = assemblyb->pe_kind;
	((MonoDynamicImage*)assemblyb->dynamic_assembly->assembly.image)->machine = assemblyb->machine;
	
	if (!mono_image_build_metadata (mb, error))
		return FALSE;
	

	if (mb->is_main && assemblyb->resources) {
		int len = mono_array_length_internal (assemblyb->resources);
		for (i = 0; i < len; ++i) {
			if (!assembly_add_resource (mb, assembly, (MonoReflectionResource*)mono_array_addr_internal (assemblyb->resources, MonoReflectionResource, i), error))
				return FALSE;
		}
	}

	if (mb->resources) {
		int len = mono_array_length_internal (mb->resources);
		for (i = 0; i < len; ++i) {
			if (!assembly_add_resource (mb, assembly, (MonoReflectionResource*)mono_array_addr_internal (mb->resources, MonoReflectionResource, i), error))
				return FALSE;
		}
	}

	if (!build_compressed_metadata (assembly, error))
		return FALSE;

	if (mb->is_main)
		assembly_add_win32_resources (assembly, assemblyb);

	nsections = calc_section_size (assembly);
	
	/* The DOS header and stub */
	g_assert (sizeof (MonoMSDOSHeader) == sizeof (msheader));
	mono_image_add_stream_data (pefile, (char*)msheader, sizeof (msheader));

	/* the dotnet header */
	header_start = mono_image_add_stream_zero (pefile, sizeof (MonoDotNetHeader));

	/* the section tables */
	section_start = mono_image_add_stream_zero (pefile, sizeof (MonoSectionTable) * nsections);

	file_offset = section_start + sizeof (MonoSectionTable) * nsections;
	virtual_offset = VIRT_ALIGN;
	image_size = 0;

	for (i = 0; i < MONO_SECTION_MAX; ++i) {
		if (!assembly->sections [i].size)
			continue;
		/* align offsets */
		file_offset += FILE_ALIGN - 1;
		file_offset &= ~(FILE_ALIGN - 1);
		virtual_offset += VIRT_ALIGN - 1;
		virtual_offset &= ~(VIRT_ALIGN - 1);

		assembly->sections [i].offset = file_offset;
		assembly->sections [i].rva = virtual_offset;

		file_offset += assembly->sections [i].size;
		virtual_offset += assembly->sections [i].size;
		image_size += (assembly->sections [i].size + VIRT_ALIGN - 1) & ~(VIRT_ALIGN - 1);
	}

	file_offset += FILE_ALIGN - 1;
	file_offset &= ~(FILE_ALIGN - 1);

	image_size += section_start + sizeof (MonoSectionTable) * nsections;

	/* back-patch info */
	msdos = (MonoMSDOSHeader*)pefile->data;
	msdos->pe_offset = GUINT32_FROM_LE (sizeof (MonoMSDOSHeader));

	header = (MonoDotNetHeader*)(pefile->data + header_start);
	header->pesig [0] = 'P';
	header->pesig [1] = 'E';
	
	header->coff.coff_machine = GUINT16_FROM_LE (assemblyb->machine);
	header->coff.coff_sections = GUINT16_FROM_LE (nsections);
	header->coff.coff_time = GUINT32_FROM_LE (time (NULL));
	header->coff.coff_opt_header_size = GUINT16_FROM_LE (sizeof (MonoDotNetHeader) - sizeof (MonoCOFFHeader) - 4);
	if (assemblyb->pekind == 1) {
		/* it's a dll */
		header->coff.coff_attributes = GUINT16_FROM_LE (0x210e);
	} else {
		/* it's an exe */
		header->coff.coff_attributes = GUINT16_FROM_LE (0x010e);
	}

	virtual_base = 0x400000; /* FIXME: 0x10000000 if a DLL */

	header->pe.pe_magic = GUINT16_FROM_LE (0x10B);
	header->pe.pe_major = 6;
	header->pe.pe_minor = 0;
	size = assembly->sections [MONO_SECTION_TEXT].size;
	size += FILE_ALIGN - 1;
	size &= ~(FILE_ALIGN - 1);
	header->pe.pe_code_size = GUINT32_FROM_LE(size);
	size = assembly->sections [MONO_SECTION_RSRC].size;
	size += FILE_ALIGN - 1;
	size &= ~(FILE_ALIGN - 1);
	header->pe.pe_data_size = GUINT32_FROM_LE(size);
	g_assert (START_TEXT_RVA == assembly->sections [MONO_SECTION_TEXT].rva);
	header->pe.pe_rva_code_base = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_TEXT].rva);
	header->pe.pe_rva_data_base = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_RSRC].rva);
	/* pe_rva_entry_point always at the beginning of the text section */
	header->pe.pe_rva_entry_point = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_TEXT].rva);

	header->nt.pe_image_base = GUINT32_FROM_LE (virtual_base);
	header->nt.pe_section_align = GUINT32_FROM_LE (VIRT_ALIGN);
	header->nt.pe_file_alignment = GUINT32_FROM_LE (FILE_ALIGN);
	header->nt.pe_os_major = GUINT16_FROM_LE (4);
	header->nt.pe_os_minor = GUINT16_FROM_LE (0);
	header->nt.pe_subsys_major = GUINT16_FROM_LE (4);
	size = section_start;
	size += FILE_ALIGN - 1;
	size &= ~(FILE_ALIGN - 1);
	header->nt.pe_header_size = GUINT32_FROM_LE (size);
	size = image_size;
	size += VIRT_ALIGN - 1;
	size &= ~(VIRT_ALIGN - 1);
	header->nt.pe_image_size = GUINT32_FROM_LE (size);

	/*
	// Translate the PEFileKind value to the value expected by the Windows loader
	*/
	{
		short kind;

		/*
		// PEFileKinds.Dll == 1
		// PEFileKinds.ConsoleApplication == 2
		// PEFileKinds.WindowApplication == 3
		//
		// need to get:
		//     IMAGE_SUBSYSTEM_WINDOWS_GUI 2 // Image runs in the Windows GUI subsystem.
                //     IMAGE_SUBSYSTEM_WINDOWS_CUI 3 // Image runs in the Windows character subsystem.
		*/
		if (assemblyb->pekind == 3)
			kind = 2;
		else
			kind = 3;
		
		header->nt.pe_subsys_required = GUINT16_FROM_LE (kind);
	}    
	header->nt.pe_stack_reserve = GUINT32_FROM_LE (0x00100000);
	header->nt.pe_stack_commit = GUINT32_FROM_LE (0x00001000);
	header->nt.pe_heap_reserve = GUINT32_FROM_LE (0x00100000);
	header->nt.pe_heap_commit = GUINT32_FROM_LE (0x00001000);
	header->nt.pe_loader_flags = GUINT32_FROM_LE (0);
	header->nt.pe_data_dir_count = GUINT32_FROM_LE (16);

	/* fill data directory entries */

	header->datadir.pe_resource_table.size = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_RSRC].size);
	header->datadir.pe_resource_table.rva = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_RSRC].rva);

	header->datadir.pe_reloc_table.size = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_RELOC].size);
	header->datadir.pe_reloc_table.rva = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_RELOC].rva);

	header->datadir.pe_cli_header.size = GUINT32_FROM_LE (72);
	header->datadir.pe_cli_header.rva = GUINT32_FROM_LE (assembly->text_rva + assembly->cli_header_offset);
	header->datadir.pe_iat.size = GUINT32_FROM_LE (8);
	header->datadir.pe_iat.rva = GUINT32_FROM_LE (assembly->text_rva + assembly->iat_offset);
	/* patch entrypoint name */
	if (assemblyb->pekind == 1)
		memcpy (assembly->code.data + assembly->imp_names_offset + 2, "_CorDllMain", 12);
	else
		memcpy (assembly->code.data + assembly->imp_names_offset + 2, "_CorExeMain", 12);
	/* patch imported function RVA name */
	rva = (guint32*)(assembly->code.data + assembly->iat_offset);
	*rva = GUINT32_FROM_LE (assembly->text_rva + assembly->imp_names_offset);

	/* the import table */
	header->datadir.pe_import_table.size = GUINT32_FROM_LE (79); /* FIXME: magic number? */
	header->datadir.pe_import_table.rva = GUINT32_FROM_LE (assembly->text_rva + assembly->idt_offset);
	/* patch imported dll RVA name and other entries in the dir */
	rva = (guint32*)(assembly->code.data + assembly->idt_offset + G_STRUCT_OFFSET (MonoIDT, name_rva));
	*rva = GUINT32_FROM_LE (assembly->text_rva + assembly->imp_names_offset + 14); /* 14 is hint+strlen+1 of func name */
	rva = (guint32*)(assembly->code.data + assembly->idt_offset + G_STRUCT_OFFSET (MonoIDT, import_address_table_rva));
	*rva = GUINT32_FROM_LE (assembly->text_rva + assembly->iat_offset);
	rva = (guint32*)(assembly->code.data + assembly->idt_offset + G_STRUCT_OFFSET (MonoIDT, import_lookup_table));
	*rva = GUINT32_FROM_LE (assembly->text_rva + assembly->ilt_offset);

	p = (guchar*)(assembly->code.data + assembly->ilt_offset);
	value = (assembly->text_rva + assembly->imp_names_offset);
	*p++ = (value) & 0xff;
	*p++ = (value >> 8) & (0xff);
	*p++ = (value >> 16) & (0xff);
	*p++ = (value >> 24) & (0xff);

	/* the CLI header info */
	cli_header = (MonoCLIHeader*)(assembly->code.data + assembly->cli_header_offset);
	cli_header->ch_size = GUINT32_FROM_LE (72);
	cli_header->ch_runtime_major = GUINT16_FROM_LE (2);
	cli_header->ch_runtime_minor = GUINT16_FROM_LE (5);
	cli_header->ch_flags = GUINT32_FROM_LE (assemblyb->pe_kind);
	if (assemblyb->entry_point) {
		guint32 table_idx = 0;
		if (!strcmp (m_class_get_name (mono_object_class (&assemblyb->entry_point->object)), "MethodBuilder")) {
			MonoReflectionMethodBuilder *methodb = (MonoReflectionMethodBuilder*)assemblyb->entry_point;
			table_idx = methodb->table_idx;
		} else {
			table_idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->method_to_table_idx, assemblyb->entry_point->method));
		}
		cli_header->ch_entry_point = GUINT32_FROM_LE (table_idx | MONO_TOKEN_METHOD_DEF);
	} else {
		cli_header->ch_entry_point = GUINT32_FROM_LE (0);
	}
	/* The embedded managed resources */
	text_offset = assembly->text_rva + assembly->code.index;
	cli_header->ch_resources.rva = GUINT32_FROM_LE (text_offset);
	cli_header->ch_resources.size = GUINT32_FROM_LE (assembly->resources.index);
	text_offset += assembly->resources.index;
	cli_header->ch_metadata.rva = GUINT32_FROM_LE (text_offset);
	cli_header->ch_metadata.size = GUINT32_FROM_LE (assembly->meta_size);
	text_offset += assembly->meta_size;
	if (assembly->strong_name_size) {
		cli_header->ch_strong_name.rva = GUINT32_FROM_LE (text_offset);
		cli_header->ch_strong_name.size = GUINT32_FROM_LE (assembly->strong_name_size);
		text_offset += assembly->strong_name_size;
	}

	/* write the section tables and section content */
	section = (MonoSectionTable*)(pefile->data + section_start);
	for (i = 0; i < MONO_SECTION_MAX; ++i) {
		static const char section_names [][7] = {
			".text", ".rsrc", ".reloc"
		};
		if (!assembly->sections [i].size)
			continue;
		strcpy (section->st_name, section_names [i]);
		/*g_print ("output section %s (%d), size: %d\n", section->st_name, i, assembly->sections [i].size);*/
		section->st_virtual_address = GUINT32_FROM_LE (assembly->sections [i].rva);
		section->st_virtual_size = GUINT32_FROM_LE (assembly->sections [i].size);
		section->st_raw_data_size = GUINT32_FROM_LE (GUINT32_TO_LE (section->st_virtual_size) + (FILE_ALIGN - 1));
		section->st_raw_data_size &= GUINT32_FROM_LE (~(FILE_ALIGN - 1));
		section->st_raw_data_ptr = GUINT32_FROM_LE (assembly->sections [i].offset);
		section->st_flags = GUINT32_FROM_LE (assembly->sections [i].attrs);
		section ++;
	}
	
	checked_write_file (file, pefile->data, pefile->index);
	
	mono_dynamic_stream_reset (pefile);
	
	for (i = 0; i < MONO_SECTION_MAX; ++i) {
		if (!assembly->sections [i].size)
			continue;
		
		if (mono_w32file_seek (file, assembly->sections [i].offset, NULL, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
			g_error ("mono_w32file_seek returned %d\n", mono_w32error_get_last ());
		
		switch (i) {
		case MONO_SECTION_TEXT:
			/* patch entry point */
			p = (guchar*)(assembly->code.data + 2);
			value = (virtual_base + assembly->text_rva + assembly->iat_offset);
			*p++ = (value) & 0xff;
			*p++ = (value >> 8) & 0xff;
			*p++ = (value >> 16) & 0xff;
			*p++ = (value >> 24) & 0xff;
		
			checked_write_file (file, assembly->code.data, assembly->code.index);
			checked_write_file (file, assembly->resources.data, assembly->resources.index);
			checked_write_file (file, assembly->image.raw_metadata, assembly->meta_size);
			checked_write_file (file, assembly->strong_name, assembly->strong_name_size);
				

			g_free (assembly->image.raw_metadata);
			break;
		case MONO_SECTION_RELOC: {
			struct {
				guint32 page_rva;
				guint32 block_size;
				guint16 type_and_offset;
				guint16 term;
			} reloc;
			
			g_assert (sizeof (reloc) == 12);
			
			reloc.page_rva = GUINT32_FROM_LE (assembly->text_rva);
			reloc.block_size = GUINT32_FROM_LE (12);
			
			/* 
			 * the entrypoint is always at the start of the text section 
			 * 3 is IMAGE_REL_BASED_HIGHLOW
			 * 2 is patch_size_rva - text_rva
			 */
			reloc.type_and_offset = GUINT16_FROM_LE ((3 << 12) + (2));
			reloc.term = 0;
			
			checked_write_file (file, &reloc, sizeof (reloc));
			
			break;
		}
		case MONO_SECTION_RSRC:
			if (assembly->win32_res) {

				/* Fixup the offsets in the IMAGE_RESOURCE_DATA_ENTRY structures */
				fixup_resource_directory (assembly->win32_res, assembly->win32_res, assembly->sections [i].rva);
				checked_write_file (file, assembly->win32_res, assembly->win32_res_size);
			}
			break;
		default:
			g_assert_not_reached ();
		}
	}
	
	/* check that the file is properly padded */
	if (mono_w32file_seek (file, file_offset, NULL, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
		g_error ("mono_w32file_seek returned %d\n", mono_w32error_get_last ());
	if (! mono_w32file_truncate (file))
		g_error ("mono_w32file_truncate returned %d\n", mono_w32error_get_last ());
	
	mono_dynamic_stream_reset (&assembly->code);
	mono_dynamic_stream_reset (&assembly->us);
	mono_dynamic_stream_reset (&assembly->blob);
	mono_dynamic_stream_reset (&assembly->guid);
	mono_dynamic_stream_reset (&assembly->sheap);

	g_hash_table_foreach (assembly->blob_cache, (GHFunc)g_free, NULL);
	g_hash_table_destroy (assembly->blob_cache);
	assembly->blob_cache = NULL;

	return TRUE;
}

#else /* DISABLE_REFLECTION_EMIT_SAVE */

gboolean
mono_image_create_pefile (MonoReflectionModuleBuilder *mb, HANDLE file, MonoError *error)
{
	g_assert_not_reached ();
}

#endif /* DISABLE_REFLECTION_EMIT_SAVE */

