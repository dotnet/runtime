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

typedef struct {
	char *p;
	char *buf;
	char *end;
} SigBuffer;

static guint32 create_typespec (MonoDynamicImage *assembly, MonoType *type);
static void    encode_type (MonoDynamicImage *assembly, MonoType *type, SigBuffer *buf);
static guint32 mono_image_typedef_or_ref (MonoDynamicImage *assembly, MonoType *type);

static guint32
mono_image_add_stream_data (MonoDynamicStream *stream, const char *data, guint32 len)
{
	return mono_dynstream_add_data (stream, data, len);
}

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
sigbuffer_make_room (SigBuffer *buf, int size)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	if (buf->end - buf->p < size) {
		int new_size = buf->end - buf->buf + size + 32;
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
	guint32 size = buf->p - buf->buf;
	/* store length */
	g_assert (size <= (buf->end - buf->buf));
	mono_metadata_encode_value (size, b, &b);
	return mono_dynamic_image_add_to_blob_cached (assembly, blob_size, b-blob_size, buf->buf, size);
}


static void
encode_generic_class (MonoDynamicImage *assembly, MonoGenericClass *gclass, SigBuffer *buf)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	int i;
	MonoGenericInst *class_inst;
	MonoClass *klass;

	g_assert (gclass);

	class_inst = gclass->context.class_inst;

	sigbuffer_add_value (buf, MONO_TYPE_GENERICINST);
	klass = gclass->container_class;
	sigbuffer_add_value (buf, klass->byval_arg.type);
	sigbuffer_add_value (buf, mono_dynimage_encode_typedef_or_ref_full (assembly, &klass->byval_arg, FALSE));

	sigbuffer_add_value (buf, class_inst->type_argc);
	for (i = 0; i < class_inst->type_argc; ++i)
		encode_type (assembly, class_inst->type_argv [i], buf);

}

static void
encode_type (MonoDynamicImage *assembly, MonoType *type, SigBuffer *buf)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	if (!type) {
		g_assert_not_reached ();
		return;
	}
		
	if (type->byref)
		sigbuffer_add_value (buf, MONO_TYPE_BYREF);

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
		sigbuffer_add_value (buf, type->type);
		break;
	case MONO_TYPE_PTR:
		sigbuffer_add_value (buf, type->type);
		encode_type (assembly, type->data.type, buf);
		break;
	case MONO_TYPE_SZARRAY:
		sigbuffer_add_value (buf, type->type);
		encode_type (assembly, &type->data.klass->byval_arg, buf);
		break;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS: {
		MonoClass *k = mono_class_from_mono_type (type);

		if (mono_class_is_gtd (k)) {
			MonoGenericClass *gclass = mono_metadata_lookup_generic_class (k, mono_class_get_generic_container (k)->context.class_inst, TRUE);
			encode_generic_class (assembly, gclass, buf);
		} else {
			/*
			 * Make sure we use the correct type.
			 */
			sigbuffer_add_value (buf, k->byval_arg.type);
			/*
			 * ensure only non-byref gets passed to mono_image_typedef_or_ref(),
			 * otherwise two typerefs could point to the same type, leading to
			 * verification errors.
			 */
			sigbuffer_add_value (buf, mono_image_typedef_or_ref (assembly, &k->byval_arg));
		}
		break;
	}
	case MONO_TYPE_ARRAY:
		sigbuffer_add_value (buf, type->type);
		encode_type (assembly, &type->data.array->eklass->byval_arg, buf);
		sigbuffer_add_value (buf, type->data.array->rank);
		sigbuffer_add_value (buf, 0); /* FIXME: set to 0 for now */
		sigbuffer_add_value (buf, 0);
		break;
	case MONO_TYPE_GENERICINST:
		encode_generic_class (assembly, type->data.generic_class, buf);
		break;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		sigbuffer_add_value (buf, type->type);
		sigbuffer_add_value (buf, mono_type_get_generic_param_num (type));
		break;
	default:
		g_error ("need to encode type %x", type->type);
	}
}

static void
encode_reflection_type (MonoDynamicImage *assembly, MonoReflectionTypeHandle type, SigBuffer *buf, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);

	if (!type) {
		sigbuffer_add_value (buf, MONO_TYPE_VOID);
		return;
	}

	MonoType *t = mono_reflection_type_handle_mono_type (type, error);
	return_if_nok (error);
	encode_type (assembly, t, buf);
}

static void
encode_reflection_type_raw (MonoDynamicImage *assembly, MonoReflectionType* type_raw, SigBuffer *buf, MonoError *error)
{
	HANDLE_FUNCTION_ENTER (); /* FIXME callers of encode_reflection_type_raw should use handles */
	error_init (error);
	MONO_HANDLE_DCL (MonoReflectionType, type);
	encode_reflection_type (assembly, type, buf, error);
	HANDLE_FUNCTION_RETURN ();
}


static void
encode_custom_modifiers (MonoDynamicImage *assembly, MonoArrayHandle modreq, MonoArrayHandle modopt, SigBuffer *buf, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MONO_REQ_GC_UNSAFE_MODE;

	int i;

	error_init (error);

	if (!MONO_HANDLE_IS_NULL (modreq)) {
		for (i = 0; i < mono_array_handle_length (modreq); ++i) {
			MonoType *mod = mono_type_array_get_and_resolve (modreq, i, error);
			goto_if_nok (error, leave);
			sigbuffer_add_byte (buf, MONO_TYPE_CMOD_REQD);
			sigbuffer_add_value (buf, mono_image_typedef_or_ref (assembly, mod));
		}
	}
	if (!MONO_HANDLE_IS_NULL (modopt)) {
		for (i = 0; i < mono_array_handle_length (modopt); ++i) {
			MonoType *mod = mono_type_array_get_and_resolve (modopt, i, error);
			goto_if_nok (error, leave);
			sigbuffer_add_byte (buf, MONO_TYPE_CMOD_OPT);
			sigbuffer_add_value (buf, mono_image_typedef_or_ref (assembly, mod));
		}
	}
leave:
	HANDLE_FUNCTION_RETURN ();
}

static void
encode_custom_modifiers_raw (MonoDynamicImage *assembly, MonoArray *modreq_raw, MonoArray *modopt_raw, SigBuffer *buf, MonoError *error)
{
	HANDLE_FUNCTION_ENTER (); /* FIXME callers of encode_custom_modifiers_raw should use handles */
	error_init (error);
	MONO_HANDLE_DCL (MonoArray, modreq);
	MONO_HANDLE_DCL (MonoArray, modopt);
	encode_custom_modifiers (assembly, modreq, modopt, buf, error);
	HANDLE_FUNCTION_RETURN ();
}


#ifndef DISABLE_REFLECTION_EMIT
guint32
mono_dynimage_encode_method_signature (MonoDynamicImage *assembly, MonoMethodSignature *sig)
{
	MONO_REQ_GC_UNSAFE_MODE;

	SigBuffer buf;
	int i;
	guint32 nparams =  sig->param_count;
	guint32 idx;

	if (!assembly->save)
		return 0;

	sigbuffer_init (&buf, 32);
	/*
	 * FIXME: vararg, explicit_this, differenc call_conv values...
	 */
	idx = sig->call_convention;
	if (sig->hasthis)
		idx |= 0x20; /* hasthis */
	if (sig->generic_param_count)
		idx |= 0x10; /* generic */
	sigbuffer_add_byte (&buf, idx);
	if (sig->generic_param_count)
		sigbuffer_add_value (&buf, sig->generic_param_count);
	sigbuffer_add_value (&buf, nparams);
	encode_type (assembly, sig->ret, &buf);
	for (i = 0; i < nparams; ++i) {
		if (i == sig->sentinelpos)
			sigbuffer_add_byte (&buf, MONO_TYPE_SENTINEL);
		encode_type (assembly, sig->params [i], &buf);
	}
	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}
#else /* DISABLE_REFLECTION_EMIT */
guint32
mono_dynimage_encode_method_signature (MonoDynamicImage *assembly, MonoMethodSignature *sig)
{
	g_assert_not_reached ();
	return 0;
}
#endif

guint32
mono_dynimage_encode_method_builder_signature (MonoDynamicImage *assembly, ReflectionMethodBuilder *mb, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);

	/*
	 * FIXME: reuse code from method_encode_signature().
	 */
	SigBuffer buf;
	int i;
	guint32 nparams =  mb->parameters ? mono_array_length (mb->parameters): 0;
	guint32 ngparams = mb->generic_params ? mono_array_length (mb->generic_params): 0;
	guint32 notypes = mb->opt_types ? mono_array_length (mb->opt_types): 0;
	guint32 idx;

	sigbuffer_init (&buf, 32);
	/* LAMESPEC: all the call conv spec is foobared */
	idx = mb->call_conv & 0x60; /* has-this, explicit-this */
	if (mb->call_conv & 2)
		idx |= 0x5; /* vararg */
	if (!(mb->attrs & METHOD_ATTRIBUTE_STATIC))
		idx |= 0x20; /* hasthis */
	if (ngparams)
		idx |= 0x10; /* generic */
	sigbuffer_add_byte (&buf, idx);
	if (ngparams)
		sigbuffer_add_value (&buf, ngparams);
	sigbuffer_add_value (&buf, nparams + notypes);
	encode_custom_modifiers_raw (assembly, mb->return_modreq, mb->return_modopt, &buf, error);
	goto_if_nok (error, leave);
	encode_reflection_type_raw (assembly, mb->rtype, &buf, error);
	goto_if_nok (error, leave);
	for (i = 0; i < nparams; ++i) {
		MonoArray *modreq = NULL;
		MonoArray *modopt = NULL;
		MonoReflectionType *pt;

		if (mb->param_modreq && (i < mono_array_length (mb->param_modreq)))
			modreq = mono_array_get (mb->param_modreq, MonoArray*, i);
		if (mb->param_modopt && (i < mono_array_length (mb->param_modopt)))
			modopt = mono_array_get (mb->param_modopt, MonoArray*, i);
		encode_custom_modifiers_raw (assembly, modreq, modopt, &buf, error);
		goto_if_nok (error, leave);
		pt = mono_array_get (mb->parameters, MonoReflectionType*, i);
		encode_reflection_type_raw (assembly, pt, &buf, error);
		goto_if_nok (error, leave);
	}
	if (notypes)
		sigbuffer_add_byte (&buf, MONO_TYPE_SENTINEL);
	for (i = 0; i < notypes; ++i) {
		MonoReflectionType *pt;

		pt = mono_array_get (mb->opt_types, MonoReflectionType*, i);
		encode_reflection_type_raw (assembly, pt, &buf, error);
		goto_if_nok (error, leave);
	}

	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
leave:
	sigbuffer_free (&buf);
	return idx;
}

guint32
mono_dynimage_encode_locals (MonoDynamicImage *assembly, MonoReflectionILGen *ilgen, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);

	MonoDynamicTable *table;
	guint32 *values;
	guint32 idx, sig_idx;
	guint nl = mono_array_length (ilgen->locals);
	SigBuffer buf;
	int i;

	sigbuffer_init (&buf, 32);
	sigbuffer_add_value (&buf, 0x07);
	sigbuffer_add_value (&buf, nl);
	for (i = 0; i < nl; ++i) {
		MonoReflectionLocalBuilder *lb = mono_array_get (ilgen->locals, MonoReflectionLocalBuilder*, i);
		
		if (lb->is_pinned)
			sigbuffer_add_value (&buf, MONO_TYPE_PINNED);
		
		encode_reflection_type_raw (assembly, (MonoReflectionType*)lb->type, &buf, error);
		if (!is_ok (error)) {
			sigbuffer_free (&buf);
			return 0;
		}
	}
	sig_idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);

	if (assembly->standalonesig_cache == NULL)
		assembly->standalonesig_cache = g_hash_table_new (NULL, NULL);
	idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->standalonesig_cache, GUINT_TO_POINTER (sig_idx)));
	if (idx)
		return idx;

	table = &assembly->tables [MONO_TABLE_STANDALONESIG];
	idx = table->next_idx ++;
	table->rows ++;
	alloc_table (table, table->rows);
	values = table->values + idx * MONO_STAND_ALONE_SIGNATURE_SIZE;

	values [MONO_STAND_ALONE_SIGNATURE] = sig_idx;

	g_hash_table_insert (assembly->standalonesig_cache, GUINT_TO_POINTER (sig_idx), GUINT_TO_POINTER (idx));

	return idx;
}


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


guint32
mono_dynimage_encode_constant (MonoDynamicImage *assembly, MonoObject *val, MonoTypeEnum *ret_type)
{
	MONO_REQ_GC_UNSAFE_MODE;

	char blob_size [64];
	char *b = blob_size;
	char *box_val;
	char* buf;
	guint32 idx = 0, len = 0, dummy = 0;

	buf = (char *)g_malloc (64);
	if (!val) {
		*ret_type = MONO_TYPE_CLASS;
		len = 4;
		box_val = (char*)&dummy;
	} else {
		box_val = ((char*)val) + sizeof (MonoObject);
		*ret_type = val->vtable->klass->byval_arg.type;
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
		
		if (klass->enumtype) {
			*ret_type = mono_class_enum_basetype (klass)->type;
			goto handle_enum;
		} else if (mono_is_corlib_image (klass->image) && strcmp (klass->name_space, "System") == 0 && strcmp (klass->name, "DateTime") == 0) {
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
			char *swapped = g_malloc (2 * mono_string_length (str));
			const char *p = (const char*)mono_string_chars (str);

			swap_with_size (swapped, p, 2, mono_string_length (str));
			idx = mono_dynamic_image_add_to_blob_cached (assembly, blob_size, b-blob_size, swapped, len);
			g_free (swapped);
		}
#else
		idx = mono_dynamic_image_add_to_blob_cached (assembly, blob_size, b-blob_size, (char*)mono_string_chars (str), len);
#endif

		g_free (buf);
		return idx;
	}
	case MONO_TYPE_GENERICINST:
		*ret_type = mono_class_get_generic_class (val->vtable->klass)->container_class->byval_arg.type;
		goto handle_enum;
	default:
		g_error ("we don't encode constant type 0x%02x yet", *ret_type);
	}

	/* there is no signature */
	mono_metadata_encode_value (len, b, &b);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	swap_with_size (blob_size, box_val, len, 1);
	mono_image_add_stream_data (&assembly->blob, blob_size, len);
#else
	idx = mono_dynamic_image_add_to_blob_cached (assembly, blob_size, b-blob_size, box_val, len);
#endif

	g_free (buf);
	return idx;
}

guint32
mono_dynimage_encode_field_signature (MonoDynamicImage *assembly, MonoReflectionFieldBuilder *fb, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);

	SigBuffer buf;
	guint32 idx;
	guint32 typespec = 0;
	MonoType *type;
	MonoClass *klass;

	type = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type, error);
	return_val_if_nok (error, 0);
	klass = mono_class_from_mono_type (type);

	sigbuffer_init (&buf, 32);
	
	sigbuffer_add_value (&buf, 0x06);
	encode_custom_modifiers_raw (assembly, fb->modreq, fb->modopt, &buf, error);
	goto_if_nok (error, fail);
	/* encode custom attributes before the type */

	if (mono_class_is_gtd (klass))
		typespec = create_typespec (assembly, type);

	if (typespec) {
		MonoGenericClass *gclass;
		gclass = mono_metadata_lookup_generic_class (klass, mono_class_get_generic_container (klass)->context.class_inst, TRUE);
		encode_generic_class (assembly, gclass, &buf);
	} else {
		encode_type (assembly, type, &buf);
	}
	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
fail:
	sigbuffer_free (&buf);
	return 0;
}

#ifndef DISABLE_REFLECTION_EMIT
/*field_image is the image to which the eventual custom mods have been encoded against*/
guint32
mono_dynimage_encode_fieldref_signature (MonoDynamicImage *assembly, MonoImage *field_image, MonoType *type)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	SigBuffer buf;
	guint32 idx, i, token;

	if (!assembly->save)
		return 0;

	sigbuffer_init (&buf, 32);
	
	sigbuffer_add_value (&buf, 0x06);
	/* encode custom attributes before the type */
	if (type->num_mods) {
		for (i = 0; i < type->num_mods; ++i) {
			if (field_image) {
				MonoError error;
				MonoClass *klass = mono_class_get_checked (field_image, type->modifiers [i].token, &error);
				g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */

				token = mono_image_typedef_or_ref (assembly, &klass->byval_arg);
			} else {
				token = type->modifiers [i].token;
			}

			if (type->modifiers [i].required)
				sigbuffer_add_byte (&buf, MONO_TYPE_CMOD_REQD);
			else
				sigbuffer_add_byte (&buf, MONO_TYPE_CMOD_OPT);

			sigbuffer_add_value (&buf, token);
		}
	}
	encode_type (assembly, type, &buf);
	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}
#else /* DISABLE_REFLECTION_EMIT */
guint32
mono_dynimage_encode_fieldref_signature (MonoDynamicImage *assembly, MonoImage *field_image, MonoType *type)
{
	g_assert_not_reached ();
	return 0;
}
#endif /* DISABLE_REFLECTION_EMIT */

static guint32
create_typespec (MonoDynamicImage *assembly, MonoType *type)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoDynamicTable *table;
	guint32 *values;
	guint32 token;
	SigBuffer buf;

	if ((token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->typespec, type))))
		return token;

	sigbuffer_init (&buf, 32);
	switch (type->type) {
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
	case MONO_TYPE_GENERICINST:
		encode_type (assembly, type, &buf);
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {
		MonoClass *k = mono_class_from_mono_type (type);
		if (!k || !mono_class_is_gtd (k)) {
			sigbuffer_free (&buf);
			return 0;
		}
		encode_type (assembly, type, &buf);
		break;
	}
	default:
		sigbuffer_free (&buf);
		return 0;
	}

	table = &assembly->tables [MONO_TABLE_TYPESPEC];
	if (assembly->save) {
		token = sigbuffer_add_to_blob_cached (assembly, &buf);
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_TYPESPEC_SIZE;
		values [MONO_TYPESPEC_SIGNATURE] = token;
	}
	sigbuffer_free (&buf);

	token = MONO_TYPEDEFORREF_TYPESPEC | (table->next_idx << MONO_TYPEDEFORREF_BITS);
	g_hash_table_insert (assembly->typespec, type, GUINT_TO_POINTER(token));
	table->next_idx ++;
	return token;
}

guint32
mono_dynimage_encode_typedef_or_ref_full (MonoDynamicImage *assembly, MonoType *type, gboolean try_typespec)
{
	MONO_REQ_GC_UNSAFE_MODE;
	HANDLE_FUNCTION_ENTER ();

	MonoDynamicTable *table;
	guint32 *values;
	guint32 token, scope, enclosing;
	MonoClass *klass;

	/* if the type requires a typespec, we must try that first*/
	if (try_typespec && (token = create_typespec (assembly, type)))
		goto leave;
	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->typeref, type));
	if (token)
		goto leave;
	klass = mono_class_from_mono_type (type);

	MonoReflectionTypeBuilderHandle tb = MONO_HANDLE_CAST (MonoReflectionTypeBuilder, mono_class_get_ref_info (klass));
	/*
	 * If it's in the same module and not a generic type parameter:
	 */
	if ((klass->image == &assembly->image) && (type->type != MONO_TYPE_VAR) && 
			(type->type != MONO_TYPE_MVAR)) {
		token = MONO_TYPEDEFORREF_TYPEDEF | (MONO_HANDLE_GETVAL (tb, table_idx) << MONO_TYPEDEFORREF_BITS);
		/* This function is called multiple times from sre and sre-save, so same object is okay */
		mono_dynamic_image_register_token (assembly, token, MONO_HANDLE_CAST (MonoObject, tb), MONO_DYN_IMAGE_TOK_SAME_OK);
		goto leave;
	}

	if (klass->nested_in) {
		enclosing = mono_dynimage_encode_typedef_or_ref_full (assembly, &klass->nested_in->byval_arg, FALSE);
		/* get the typeref idx of the enclosing type */
		enclosing >>= MONO_TYPEDEFORREF_BITS;
		scope = (enclosing << MONO_RESOLUTION_SCOPE_BITS) | MONO_RESOLUTION_SCOPE_TYPEREF;
	} else {
		scope = mono_reflection_resolution_scope_from_image (assembly, klass->image);
	}
	table = &assembly->tables [MONO_TABLE_TYPEREF];
	if (assembly->save) {
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_TYPEREF_SIZE;
		values [MONO_TYPEREF_SCOPE] = scope;
		values [MONO_TYPEREF_NAME] = mono_dynstream_insert_string (&assembly->sheap, klass->name);
		values [MONO_TYPEREF_NAMESPACE] = mono_dynstream_insert_string (&assembly->sheap, klass->name_space);
	}
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

/*
 * Despite the name, we handle also TypeSpec (with the above helper).
 */
static guint32
mono_image_typedef_or_ref (MonoDynamicImage *assembly, MonoType *type)
{
	return mono_dynimage_encode_typedef_or_ref_full (assembly, type, TRUE);
}

guint32
mono_dynimage_encode_generic_method_sig (MonoDynamicImage *assembly, MonoGenericContext *context)
{
	SigBuffer buf;
	int i;
	guint32 nparams = context->method_inst->type_argc;
	guint32 idx;

	if (!assembly->save)
		return 0;

	sigbuffer_init (&buf, 32);
	/*
	 * FIXME: vararg, explicit_this, differenc call_conv values...
	 */
	sigbuffer_add_value (&buf, 0xa); /* FIXME FIXME FIXME */
	sigbuffer_add_value (&buf, nparams);

	for (i = 0; i < nparams; i++)
		encode_type (assembly, context->method_inst->type_argv [i], &buf);

	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}

#ifndef DISABLE_REFLECTION_EMIT
static gboolean
encode_sighelper_arg (MonoDynamicImage *assembly, int i, MonoArrayHandle helper_arguments, MonoArrayHandle helper_modreqs, MonoArrayHandle helper_modopts, SigBuffer* buf, MonoError *error)
{
	HANDLE_FUNCTION_ENTER();
	error_init (error);
	MonoArrayHandle modreqs = MONO_HANDLE_NEW (MonoArray, NULL);
	MonoArrayHandle modopts = MONO_HANDLE_NEW (MonoArray, NULL);

	if (!MONO_HANDLE_IS_NULL (helper_modreqs) && (i < mono_array_handle_length (helper_modreqs)))
		MONO_HANDLE_ARRAY_GETREF (modreqs, helper_modreqs, i);
	if (!MONO_HANDLE_IS_NULL (helper_modopts) && (i < mono_array_handle_length (helper_modopts)))
		MONO_HANDLE_ARRAY_GETREF (modopts, helper_modopts, i);

	encode_custom_modifiers (assembly, modreqs, modopts, buf, error);
	goto_if_nok (error, leave);
	MonoReflectionTypeHandle pt = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	MONO_HANDLE_ARRAY_GETREF (pt, helper_arguments, i);
	encode_reflection_type (assembly, pt, buf, error);
	goto_if_nok (error, leave);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

guint32
mono_dynimage_encode_reflection_sighelper (MonoDynamicImage *assembly, MonoReflectionSigHelperHandle helper, MonoError *error)
{
	SigBuffer buf;
	guint32 nargs;
	guint32 i, idx;

	error_init (error);

	if (!assembly->save)
		return 0;

	/* FIXME: this means SignatureHelper.SignatureHelpType.HELPER_METHOD */
	g_assert (MONO_HANDLE_GETVAL (helper, type) == 2);

	MonoArrayHandle arguments = MONO_HANDLE_NEW_GET (MonoArray, helper, arguments);
	if (!MONO_HANDLE_IS_NULL (arguments))
		nargs = mono_array_handle_length (arguments);
	else
		nargs = 0;

	sigbuffer_init (&buf, 32);

	/* Encode calling convention */
	/* Change Any to Standard */
	if ((MONO_HANDLE_GETVAL (helper, call_conv) & 0x03) == 0x03)
		MONO_HANDLE_SETVAL (helper, call_conv, guint32, 0x01);
	/* explicit_this implies has_this */
	if (MONO_HANDLE_GETVAL (helper, call_conv) & 0x40)
		MONO_HANDLE_SETVAL (helper, call_conv, guint32, MONO_HANDLE_GETVAL (helper, call_conv) & 0x20);

	if (MONO_HANDLE_GETVAL (helper, call_conv) == 0) { /* Unmanaged */
		idx = MONO_HANDLE_GETVAL (helper, unmanaged_call_conv) - 1;
	} else {
		/* Managed */
		idx = MONO_HANDLE_GETVAL (helper, call_conv) & 0x60; /* has_this + explicit_this */
		if (MONO_HANDLE_GETVAL (helper, call_conv) & 0x02) /* varargs */
			idx += 0x05;
	}

	sigbuffer_add_byte (&buf, idx);
	sigbuffer_add_value (&buf, nargs);
	encode_reflection_type (assembly, MONO_HANDLE_NEW_GET (MonoReflectionType, helper, return_type), &buf, error);
	goto_if_nok (error, fail);
	MonoArrayHandle modreqs = MONO_HANDLE_NEW_GET (MonoArray, helper, modreqs);
	MonoArrayHandle modopts = MONO_HANDLE_NEW_GET (MonoArray, helper, modopts);
	for (i = 0; i < nargs; ++i) {
		if (!encode_sighelper_arg (assembly, i, arguments, modreqs, modopts, &buf, error))
			goto fail;
	}
	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);

	return idx;
fail:
	sigbuffer_free (&buf);
	return 0;
}
#else /* DISABLE_REFLECTION_EMIT */
guint32
mono_dynimage_encode_reflection_sighelper (MonoDynamicImage *assembly, MonoReflectionSigHelperHandle helper, MonoError *error)
{
	g_assert_not_reached ();
	return 0;
}
#endif /* DISABLE_REFLECTION_EMIT */

static gboolean
encode_reflection_types (MonoDynamicImage *assembly, MonoArrayHandle sig_arguments, int i, SigBuffer *buf, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoReflectionTypeHandle type = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	MONO_HANDLE_ARRAY_GETREF (type, sig_arguments, i);
	encode_reflection_type (assembly, type, buf, error);
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

static MonoArrayHandle
reflection_sighelper_get_signature_local (MonoReflectionSigHelperHandle sig, MonoError *error)
{
	MonoReflectionModuleBuilderHandle module = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, sig, module);
	MonoDynamicImage *assembly = MONO_HANDLE_IS_NULL (module) ? NULL : MONO_HANDLE_GETVAL (module, dynamic_image);
	MonoArrayHandle sig_arguments = MONO_HANDLE_NEW_GET (MonoArray, sig, arguments);
	guint32 na = MONO_HANDLE_IS_NULL (sig_arguments) ? 0 : mono_array_handle_length (sig_arguments);
	guint32 buflen, i;
	SigBuffer buf;

	error_init (error);

	sigbuffer_init (&buf, 32);

	sigbuffer_add_value (&buf, 0x07);
	sigbuffer_add_value (&buf, na);
	if (assembly != NULL){
		for (i = 0; i < na; ++i) {
			if (!encode_reflection_types (assembly, sig_arguments, i, &buf, error))
				goto fail;
		}
	}

	buflen = buf.p - buf.buf;
	MonoArrayHandle result = mono_array_new_handle (mono_domain_get (), mono_defaults.byte_class, buflen, error);
	goto_if_nok (error, fail);
	uint32_t gchandle;
	void *base = MONO_ARRAY_HANDLE_PIN (result, char, 0, &gchandle);
	memcpy (base, buf.buf, buflen);
	sigbuffer_free (&buf);
	mono_gchandle_free (gchandle);
	return result;
fail:
	sigbuffer_free (&buf);
	return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
}

static MonoArrayHandle
reflection_sighelper_get_signature_field (MonoReflectionSigHelperHandle sig, MonoError *error)
{
	MonoReflectionModuleBuilderHandle module = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, sig, module);
	MonoDynamicImage *assembly = MONO_HANDLE_GETVAL (module, dynamic_image);
	MonoArrayHandle sig_arguments = MONO_HANDLE_NEW_GET (MonoArray, sig, arguments);
	guint32 na = MONO_HANDLE_IS_NULL (sig_arguments) ? 0 : mono_array_handle_length (sig_arguments);
	guint32 buflen, i;
	SigBuffer buf;

	error_init (error);

	sigbuffer_init (&buf, 32);

	sigbuffer_add_value (&buf, 0x06);
	for (i = 0; i < na; ++i) {
		if (! encode_reflection_types (assembly, sig_arguments, i, &buf, error))
			goto fail;
	}

	buflen = buf.p - buf.buf;
	MonoArrayHandle result = mono_array_new_handle (mono_domain_get (), mono_defaults.byte_class, buflen, error);
	goto_if_nok (error, fail);
	uint32_t gchandle;
	void *base = MONO_ARRAY_HANDLE_PIN (result, char, 0, &gchandle);
	memcpy (base, buf.buf, buflen);
	sigbuffer_free (&buf);
	mono_gchandle_free (gchandle);

	return result;
fail:
	sigbuffer_free (&buf);
	return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
}

static char*
type_get_fully_qualified_name (MonoType *type)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED);
}

#ifndef DISABLE_REFLECTION_EMIT_SAVE
guint32
mono_dynimage_save_encode_marshal_blob (MonoDynamicImage *assembly, MonoReflectionMarshal *minfo, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);

	char *str;
	SigBuffer buf;
	guint32 idx, len;

	sigbuffer_init (&buf, 32);

	sigbuffer_add_value (&buf, minfo->type);

	switch (minfo->type) {
	case MONO_NATIVE_BYVALTSTR:
	case MONO_NATIVE_BYVALARRAY:
		sigbuffer_add_value (&buf, minfo->count);
		break;
	case MONO_NATIVE_LPARRAY:
		if (minfo->eltype || minfo->has_size) {
			sigbuffer_add_value (&buf, minfo->eltype);
			if (minfo->has_size) {
				sigbuffer_add_value (&buf, minfo->param_num != -1? minfo->param_num: 0);
				sigbuffer_add_value (&buf, minfo->count != -1? minfo->count: 0);

				/* LAMESPEC: ElemMult is undocumented */
				sigbuffer_add_value (&buf, minfo->param_num != -1? 1: 0);
			}
		}
		break;
	case MONO_NATIVE_SAFEARRAY:
		if (minfo->eltype)
			sigbuffer_add_value (&buf, minfo->eltype);
		break;
	case MONO_NATIVE_CUSTOM:
		if (minfo->guid) {
			str = mono_string_to_utf8_checked (minfo->guid, error);
			if (!is_ok (error)) {
				sigbuffer_free (&buf);
				return 0;
			}
			len = strlen (str);
			sigbuffer_add_value (&buf, len);
			sigbuffer_add_mem (&buf, str, len);
			g_free (str);
		} else {
			sigbuffer_add_value (&buf, 0);
		}
		/* native type name */
		sigbuffer_add_value (&buf, 0);
		/* custom marshaler type name */
		if (minfo->marshaltype || minfo->marshaltyperef) {
			if (minfo->marshaltyperef) {
				MonoType *marshaltype = mono_reflection_type_get_handle ((MonoReflectionType*)minfo->marshaltyperef, error);
				if (!is_ok (error)) {
					sigbuffer_free (&buf);
					return 0;
				}
				str = type_get_fully_qualified_name (marshaltype);
			} else {
				str = mono_string_to_utf8_checked (minfo->marshaltype, error);
				if (!is_ok (error)) {
					sigbuffer_free (&buf);
					return 0;
				}
			}
			len = strlen (str);
			sigbuffer_add_value (&buf, len);
			sigbuffer_add_mem (&buf, str, len);
			g_free (str);
		} else {
			/* FIXME: Actually a bug, since this field is required.  Punting for now ... */
			sigbuffer_add_value (&buf, 0);
		}
		if (minfo->mcookie) {
			str = mono_string_to_utf8_checked (minfo->mcookie, error);
			if (!is_ok (error)) {
				sigbuffer_free (&buf);
				return 0;
			}
			len = strlen (str);
			sigbuffer_add_value (&buf, len);
			sigbuffer_add_mem (&buf, str, len);
			g_free (str);
		} else {
			sigbuffer_add_value (&buf, 0);
		}
		break;
	default:
		break;
	}
	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}

guint32
mono_dynimage_save_encode_property_signature (MonoDynamicImage *assembly, MonoReflectionPropertyBuilder *fb, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);

	SigBuffer buf;
	guint32 nparams = 0;
	MonoReflectionMethodBuilder *mb = fb->get_method;
	MonoReflectionMethodBuilder *smb = fb->set_method;
	guint32 idx, i;

	if (mb && mb->parameters)
		nparams = mono_array_length (mb->parameters);
	if (!mb && smb && smb->parameters)
		nparams = mono_array_length (smb->parameters) - 1;
	sigbuffer_init (&buf, 32);
	if (fb->call_conv & 0x20)
		sigbuffer_add_byte (&buf, 0x28);
	else
		sigbuffer_add_byte (&buf, 0x08);
	sigbuffer_add_value (&buf, nparams);
	if (mb) {
		encode_reflection_type_raw (assembly, (MonoReflectionType*)mb->rtype, &buf, error);
		if (!is_ok (error))
			goto fail;
		for (i = 0; i < nparams; ++i) {
			MonoReflectionType *pt = mono_array_get (mb->parameters, MonoReflectionType*, i);
			encode_reflection_type_raw (assembly, pt, &buf, error);
			if (!is_ok (error))
				goto fail;
		}
	} else if (smb && smb->parameters) {
		/* the property type is the last param */
		encode_reflection_type_raw (assembly, mono_array_get (smb->parameters, MonoReflectionType*, nparams), &buf, error);
		if (!is_ok (error))
			goto fail;

		for (i = 0; i < nparams; ++i) {
			MonoReflectionType *pt = mono_array_get (smb->parameters, MonoReflectionType*, i);
			encode_reflection_type_raw (assembly, pt, &buf, error);
			if (!is_ok (error))
				goto fail;
		}
	} else {
		encode_reflection_type_raw (assembly, (MonoReflectionType*)fb->type, &buf, error);
		if (!is_ok (error))
			goto fail;
	}

	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
fail:
	sigbuffer_free (&buf);
	return 0;
}


#else /*DISABLE_REFLECTION_EMIT_SAVE*/
guint32
mono_dynimage_save_encode_marshal_blob (MonoDynamicImage *assembly, MonoReflectionMarshal *minfo, MonoError *error)
{
	g_assert_not_reached ();
	return 0;
}

guint32
mono_dynimage_save_encode_property_signature (MonoDynamicImage *assembly, MonoReflectionPropertyBuilder *fb, MonoError *error)
{
	g_assert_not_reached ();
	return 0;
}
#endif /*DISABLE_REFLECTION_EMIT_SAVE*/

#ifndef DISABLE_REFLECTION_EMIT
MonoArrayHandle
ves_icall_SignatureHelper_get_signature_local (MonoReflectionSigHelperHandle sig, MonoError *error)
{
	error_init (error);
	return reflection_sighelper_get_signature_local (sig, error);
}

MonoArrayHandle
ves_icall_SignatureHelper_get_signature_field (MonoReflectionSigHelperHandle sig, MonoError *error)
{
	error_init (error);
	return reflection_sighelper_get_signature_field (sig, error);
}
#else /* DISABLE_REFLECTION_EMIT */
MonoArrayHandle
ves_icall_SignatureHelper_get_signature_local (MonoReflectionSigHelperHandle sig, MonoError *error)
{
	error_init (error);
	g_assert_not_reached ();
	return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
}

MonoArrayHandle
ves_icall_SignatureHelper_get_signature_field (MonoReflectionSigHelperHandle sig, MonoError *error)
{
	error_init (error);
	g_assert_not_reached ();
	return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
}

#endif /* DISABLE_REFLECTION_EMIT */
