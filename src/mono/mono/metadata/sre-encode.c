/**
 * \file
 * Routines for encoding SRE builders into a
 * MonoDynamicImage and generating tokens.
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
#include "mono/metadata/object-internals.h"
#include "mono/metadata/reflection-internals.h"
#include "mono/metadata/sre-internals.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/tokentype.h"
#include "mono/utils/checked-build.h"
#include "icall-decl.h"

typedef struct {
	char *p;
	char *buf;
	char *end;
} SigBuffer;

static guint32 create_typespec (MonoDynamicImage *assembly, MonoType *type);

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
static guint32
mono_image_add_stream_data (MonoDynamicStream *stream, const char *data, guint32 len)
{
	return mono_dynstream_add_data (stream, data, len);
}
#endif

static void
alloc_table (MonoDynamicTable *table, guint nrows)
{
	mono_dynimage_alloc_table (table, nrows);
}

static void
sigbuffer_init (SigBuffer *buf, int size)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	buf->buf = (char *)g_malloc (size);
	buf->p = buf->buf;
	buf->end = buf->buf + size;
}

static void
sigbuffer_make_room (SigBuffer *buf, intptr_t size)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	if (buf->end - buf->p < size) {
		intptr_t new_size = buf->end - buf->buf + size + 32;
		char *p = (char *)g_realloc (buf->buf, new_size);
		size = buf->p - buf->buf;
		buf->buf = p;
		buf->p = p + size;
		buf->end = buf->buf + new_size;
	}
}

static void
sigbuffer_add_value (SigBuffer *buf, guint32 val)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	sigbuffer_make_room (buf, 6);
	mono_metadata_encode_value (val, buf->p, &buf->p);
}

static void
sigbuffer_add_byte (SigBuffer *buf, guint8 val)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	sigbuffer_make_room (buf, 1);
	buf->p [0] = val;
	buf->p++;
}

static void
sigbuffer_add_mem (SigBuffer *buf, char *p, guint32 size)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	sigbuffer_make_room (buf, size);
	memcpy (buf->p, p, size);
	buf->p += size;
}

static void
sigbuffer_free (SigBuffer *buf)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	g_free (buf->buf);
}

static guint32
sigbuffer_add_to_blob_cached (MonoDynamicImage *assembly, SigBuffer *buf)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	char blob_size [8];
	char *b = blob_size;
	guint32 size = GPTRDIFF_TO_UINT32 (buf->p - buf->buf);
	/* store length */
	g_assert (size <= GPTRDIFF_TO_UINT32 (buf->end - buf->buf));
	mono_metadata_encode_value (size, b, &b);
	return mono_dynamic_image_add_to_blob_cached (assembly, blob_size, GPTRDIFF_TO_INT (b - blob_size), buf->buf, size);
}

/*
 * Copy len * nelem bytes from val to dest, swapping bytes to LE if necessary.
 * dest may be misaligned.
 */
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
static void
swap_with_size (char *dest, const char* val, int len, int nelem)
{
	MONO_REQ_GC_NEUTRAL_MODE;
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
}
#endif


guint32
mono_dynimage_encode_constant (MonoDynamicImage *assembly, MonoObject *val, MonoTypeEnum *ret_type)
{
	MONO_REQ_GC_UNSAFE_MODE;

	char blob_size [64];
	char *b = blob_size;
	gpointer box_val;
	char* buf;
	guint32 idx = 0, len = 0, dummy = 0;

	buf = (char *)g_malloc (64);
	if (!val) {
		*ret_type = MONO_TYPE_CLASS;
		len = 4;
		box_val = &dummy;
	} else {
		box_val = mono_object_get_data (val);
		*ret_type = m_class_get_byval_arg (val->vtable->klass)->type;
	}
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
		len = 8;
		break;
	case MONO_TYPE_R8:
		len = 8;
		break;
	case MONO_TYPE_VALUETYPE: {
		MonoClass *klass = val->vtable->klass;

		if (m_class_is_enumtype (klass)) {
			*ret_type = mono_class_enum_basetype_internal (klass)->type;
			goto handle_enum;
		} else if (mono_is_corlib_image (m_class_get_image (klass)) && strcmp (m_class_get_name_space (klass), "System") == 0 && strcmp (m_class_get_name (klass), "DateTime") == 0) {
			len = 8;
		} else
			g_error ("we can't encode valuetypes, we should have never reached this line");
		break;
	}
	case MONO_TYPE_CLASS:
		break;
	case MONO_TYPE_STRING: {
		MonoString *str = (MonoString*)val;
		/* there is no signature */
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
		idx = mono_dynamic_image_add_to_blob_cached (assembly, blob_size, GPTRDIFF_TO_INT (b - blob_size), mono_string_chars_internal (str), len);
#endif

		g_free (buf);
		return idx;
	}
	case MONO_TYPE_GENERICINST:
		*ret_type = m_class_get_byval_arg (mono_class_get_generic_class (val->vtable->klass)->container_class)->type;
		goto handle_enum;
	default:
		g_error ("we don't encode constant type 0x%02x yet", *ret_type);
	}

	/* there is no signature */
	mono_metadata_encode_value (len, b, &b);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	swap_with_size (blob_size, (const char*)box_val, len, 1);
	mono_image_add_stream_data (&assembly->blob, blob_size, len);
#else
	idx = mono_dynamic_image_add_to_blob_cached (assembly, blob_size, GPTRDIFF_TO_INT (b - blob_size), box_val, len);
#endif

	g_free (buf);
	return idx;
}

static guint32
create_typespec (MonoDynamicImage *assembly, MonoType *type)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoDynamicTable *table;
	guint32 token;

	if ((token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->typespec, type))))
		return token;

	table = &assembly->tables [MONO_TABLE_TYPESPEC];
	token = MONO_TYPEDEFORREF_TYPESPEC | (table->next_idx << MONO_TYPEDEFORREF_BITS);
	g_hash_table_insert (assembly->typespec, type, GUINT_TO_POINTER (token));
	table->next_idx ++;
	return token;
}

guint32
mono_dynimage_encode_typedef_or_ref_full (MonoDynamicImage *assembly, MonoType *type, gboolean try_typespec)
{
	MONO_REQ_GC_UNSAFE_MODE;
	HANDLE_FUNCTION_ENTER ();

	MonoDynamicTable *table;
	guint32 token;
	MonoClass *klass;

	/* if the type requires a typespec, we must try that first*/
	if (try_typespec && (token = create_typespec (assembly, type)))
		goto leave;
	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->typeref, type));
	if (token)
		goto leave;
	klass = mono_class_from_mono_type_internal (type);

	MonoReflectionTypeBuilderHandle tb;
	tb = MONO_HANDLE_CAST (MonoReflectionTypeBuilder, mono_class_get_ref_info (klass));
	/*
	 * If it's in the same module and not a generic type parameter:
	 */
	if ((m_class_get_image (klass) == &assembly->image) && (type->type != MONO_TYPE_VAR) &&
			(type->type != MONO_TYPE_MVAR)) {
		token = MONO_TYPEDEFORREF_TYPEDEF | (MONO_HANDLE_GETVAL (tb, table_idx) << MONO_TYPEDEFORREF_BITS);
		/* This function is called multiple times from sre and sre-save, so same object is okay */
		mono_dynamic_image_register_token (assembly, token, MONO_HANDLE_CAST (MonoObject, tb), MONO_DYN_IMAGE_TOK_SAME_OK);
		goto leave;
	}

	if (m_class_get_nested_in (klass))
		mono_dynimage_encode_typedef_or_ref_full (assembly, m_class_get_byval_arg (m_class_get_nested_in (klass)), FALSE);
	table = &assembly->tables [MONO_TABLE_TYPEREF];
	token = MONO_TYPEDEFORREF_TYPEREF | (table->next_idx << MONO_TYPEDEFORREF_BITS); /* typeref */
	g_hash_table_insert (assembly->typeref, type, GUINT_TO_POINTER(token));
	table->next_idx ++;

	if (!MONO_HANDLE_IS_NULL (tb)) {
		/* This function is called multiple times from sre and sre-save, so same object is okay */
		mono_dynamic_image_register_token (assembly, token, MONO_HANDLE_CAST (MonoObject, tb), MONO_DYN_IMAGE_TOK_SAME_OK);
	}

leave:
	HANDLE_FUNCTION_RETURN_VAL (token);
}
