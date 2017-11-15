/**
 * \file
 * Routines for creating an image at runtime
 * and related System.Reflection.Emit icalls
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
#include "mono/metadata/assembly.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/dynamic-image-internals.h"
#include "mono/metadata/dynamic-stream-internals.h"
#include "mono/metadata/exception.h"
#include "mono/metadata/gc-internals.h"
#include "mono/metadata/mono-ptr-array.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/profiler-private.h"
#include "mono/metadata/reflection-internals.h"
#include "mono/metadata/reflection-cache.h"
#include "mono/metadata/sre-internals.h"
#include "mono/metadata/custom-attrs-internals.h"
#include "mono/metadata/security-manager.h"
#include "mono/metadata/security-core-clr.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/tokentype.h"
#include "mono/utils/checked-build.h"
#include "mono/utils/mono-digest.h"
#include "mono/utils/w32api.h"

static GENERATE_GET_CLASS_WITH_CACHE (marshal_as_attribute, "System.Runtime.InteropServices", "MarshalAsAttribute");
static GENERATE_GET_CLASS_WITH_CACHE (module_builder, "System.Reflection.Emit", "ModuleBuilder");

static char* string_to_utf8_image_raw (MonoImage *image, MonoString *s, MonoError *error);

#ifndef DISABLE_REFLECTION_EMIT
static guint32 mono_image_get_sighelper_token (MonoDynamicImage *assembly, MonoReflectionSigHelperHandle helper, MonoError *error);
static gboolean ensure_runtime_vtable (MonoClass *klass, MonoError  *error);
static void reflection_methodbuilder_from_dynamic_method (ReflectionMethodBuilder *rmb, MonoReflectionDynamicMethod *mb);
static gboolean reflection_setup_internal_class (MonoReflectionTypeBuilderHandle tb, MonoError *error);
static gboolean reflection_init_generic_class (MonoReflectionTypeBuilderHandle tb, MonoError *error);
static gboolean reflection_setup_class_hierarchy (GHashTable *unparented, MonoError *error);


static gpointer register_assembly (MonoDomain *domain, MonoReflectionAssembly *res, MonoAssembly *assembly);
#endif

static char*   type_get_qualified_name (MonoType *type, MonoAssembly *ass);
static MonoReflectionTypeHandle mono_reflection_type_get_underlying_system_type (MonoReflectionTypeHandle t, MonoError *error);
static gboolean is_sre_array (MonoClass *klass);
static gboolean is_sre_byref (MonoClass *klass);
static gboolean is_sre_pointer (MonoClass *klass);
static gboolean is_sre_generic_instance (MonoClass *klass);
static gboolean is_sre_type_builder (MonoClass *klass);
static gboolean is_sre_method_builder (MonoClass *klass);
static gboolean is_sre_field_builder (MonoClass *klass);
static gboolean is_sre_gparam_builder (MonoClass *klass);
static gboolean is_sre_enum_builder (MonoClass *klass);
static gboolean is_sr_mono_method (MonoClass *klass);
static gboolean is_sr_mono_field (MonoClass *klass);

static guint32 mono_image_get_methodspec_token (MonoDynamicImage *assembly, MonoMethod *method);
static guint32 mono_image_get_inflated_method_token (MonoDynamicImage *assembly, MonoMethod *m);
static guint32 mono_image_create_method_token (MonoDynamicImage *assembly, MonoObjectHandle obj, MonoArrayHandle opt_param_types, MonoError *error);


#ifndef DISABLE_REFLECTION_EMIT
static MonoType* mono_type_array_get_and_resolve_raw (MonoArray* array, int idx, MonoError* error);
#endif

static gboolean mono_image_module_basic_init (MonoReflectionModuleBuilderHandle module, MonoError *error);

void
mono_reflection_emit_init (void)
{
	mono_dynamic_images_init ();
}

char*
string_to_utf8_image_raw (MonoImage *image, MonoString *s_raw, MonoError *error)
{
	/* FIXME all callers to string_to_utf8_image_raw should use handles */
	HANDLE_FUNCTION_ENTER ();
	char* result = NULL;
	error_init (error);
	MONO_HANDLE_DCL (MonoString, s);
	result = mono_string_to_utf8_image (image, s, error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static char*
type_get_fully_qualified_name (MonoType *type)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED);
}

static char*
type_get_qualified_name (MonoType *type, MonoAssembly *ass)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoClass *klass;
	MonoAssembly *ta;

	klass = mono_class_from_mono_type (type);
	if (!klass) 
		return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_REFLECTION);
	ta = klass->image->assembly;
	if (assembly_is_dynamic (ta) || (ta == ass)) {
		if (mono_class_is_ginst (klass) || mono_class_is_gtd (klass))
			/* For generic type definitions, we want T, while REFLECTION returns T<K> */
			return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_FULL_NAME);
		else
			return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_REFLECTION);
	}

	return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED);
}

#ifndef DISABLE_REFLECTION_EMIT
/**
 * mp_g_alloc:
 *
 * Allocate memory from the @image mempool if it is non-NULL. Otherwise, allocate memory
 * from the C heap.
 */
static gpointer
image_g_malloc (MonoImage *image, guint size)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	if (image)
		return mono_image_alloc (image, size);
	else
		return g_malloc (size);
}
#endif /* !DISABLE_REFLECTION_EMIT */

/**
 * image_g_alloc0:
 *
 * Allocate memory from the @image mempool if it is non-NULL. Otherwise, allocate memory
 * from the C heap.
 */
gpointer
mono_image_g_malloc0 (MonoImage *image, guint size)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	if (image)
		return mono_image_alloc0 (image, size);
	else
		return g_malloc0 (size);
}

/**
 * image_g_free:
 * @image: a MonoImage
 * @ptr: pointer
 *
 * If @image is NULL, free @ptr, otherwise do nothing.
 */
static void
image_g_free (MonoImage *image, gpointer ptr)
{
	if (image == NULL)
		g_free (ptr);
}

#ifndef DISABLE_REFLECTION_EMIT
static char*
image_strdup (MonoImage *image, const char *s)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	if (image)
		return mono_image_strdup (image, s);
	else
		return g_strdup (s);
}
#endif

#define image_g_new(image,struct_type, n_structs)		\
    ((struct_type *) image_g_malloc (image, ((gsize) sizeof (struct_type)) * ((gsize) (n_structs))))

#define image_g_new0(image,struct_type, n_structs)		\
    ((struct_type *) mono_image_g_malloc0 (image, ((gsize) sizeof (struct_type)) * ((gsize) (n_structs))))


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
mono_image_add_stream_data (MonoDynamicStream *stream, const char *data, guint32 len)
{
	return mono_dynstream_add_data (stream, data, len);
}

/*
 * Despite the name, we handle also TypeSpec (with the above helper).
 */
static guint32
mono_image_typedef_or_ref (MonoDynamicImage *assembly, MonoType *type)
{
	return mono_dynimage_encode_typedef_or_ref_full (assembly, type, TRUE);
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
mono_reflection_method_count_clauses (MonoReflectionILGen *ilgen)
{
	MONO_REQ_GC_UNSAFE_MODE;

	guint32 num_clauses = 0;
	int i;

	MonoILExceptionInfo *ex_info;
	for (i = 0; i < mono_array_length (ilgen->ex_handlers); ++i) {
		ex_info = (MonoILExceptionInfo*)mono_array_addr (ilgen->ex_handlers, MonoILExceptionInfo, i);
		if (ex_info->handlers)
			num_clauses += mono_array_length (ex_info->handlers);
		else
			num_clauses++;
	}

	return num_clauses;
}

#ifndef DISABLE_REFLECTION_EMIT
static MonoExceptionClause*
method_encode_clauses (MonoImage *image, MonoDynamicImage *assembly, MonoReflectionILGen *ilgen, guint32 num_clauses, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);

	MonoExceptionClause *clauses;
	MonoExceptionClause *clause;
	MonoILExceptionInfo *ex_info;
	MonoILExceptionBlock *ex_block;
	guint32 finally_start;
	int i, j, clause_index;;

	clauses = image_g_new0 (image, MonoExceptionClause, num_clauses);

	clause_index = 0;
	for (i = mono_array_length (ilgen->ex_handlers) - 1; i >= 0; --i) {
		ex_info = (MonoILExceptionInfo*)mono_array_addr (ilgen->ex_handlers, MonoILExceptionInfo, i);
		finally_start = ex_info->start + ex_info->len;
		if (!ex_info->handlers)
			continue;
		for (j = 0; j < mono_array_length (ex_info->handlers); ++j) {
			ex_block = (MonoILExceptionBlock*)mono_array_addr (ex_info->handlers, MonoILExceptionBlock, j);
			clause = &(clauses [clause_index]);

			clause->flags = ex_block->type;
			clause->try_offset = ex_info->start;

			if (ex_block->type == MONO_EXCEPTION_CLAUSE_FINALLY)
				clause->try_len = finally_start - ex_info->start;
			else
				clause->try_len = ex_info->len;
			clause->handler_offset = ex_block->start;
			clause->handler_len = ex_block->len;
			if (ex_block->extype) {
				MonoType *extype = mono_reflection_type_get_handle ((MonoReflectionType*)ex_block->extype, error);

				if (!is_ok (error)) {
					image_g_free (image, clauses);
					return NULL;
				}
				clause->data.catch_class = mono_class_from_mono_type (extype);
			} else {
				if (ex_block->type == MONO_EXCEPTION_CLAUSE_FILTER)
					clause->data.filter_offset = ex_block->filter_offset;
				else
					clause->data.filter_offset = 0;
			}
			finally_start = ex_block->start + ex_block->len;

			clause_index ++;
		}
	}

	return clauses;
}
#endif /* !DISABLE_REFLECTION_EMIT */

#ifndef DISABLE_REFLECTION_EMIT
/*
 * LOCKING: Acquires the loader lock. 
 */
static void
mono_save_custom_attrs (MonoImage *image, void *obj, MonoArray *cattrs)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoCustomAttrInfo *ainfo, *tmp;

	if (!cattrs || !mono_array_length (cattrs))
		return;

	ainfo = mono_custom_attrs_from_builders (image, image, cattrs);

	mono_loader_lock ();
	tmp = (MonoCustomAttrInfo *)mono_image_property_lookup (image, obj, MONO_PROP_DYNAMIC_CATTR);
	if (tmp)
		mono_custom_attrs_free (tmp);
	mono_image_property_insert (image, obj, MONO_PROP_DYNAMIC_CATTR, ainfo);
	mono_loader_unlock ();

}
#else
//FIXME some code compiled under DISABLE_REFLECTION_EMIT depends on this function, we should be more aggressively disabling things
static void
mono_save_custom_attrs (MonoImage *image, void *obj, MonoArray *cattrs)
{
}
#endif

guint32
mono_reflection_resolution_scope_from_image (MonoDynamicImage *assembly, MonoImage *image)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDynamicTable *table;
	guint32 token;
	guint32 *values;
	guint32 cols [MONO_ASSEMBLY_SIZE];
	const char *pubkey;
	guint32 publen;

	if ((token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, image))))
		return token;

	if (assembly_is_dynamic (image->assembly) && (image->assembly == assembly->image.assembly)) {
		table = &assembly->tables [MONO_TABLE_MODULEREF];
		token = table->next_idx ++;
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + token * MONO_MODULEREF_SIZE;
		values [MONO_MODULEREF_NAME] = string_heap_insert (&assembly->sheap, image->module_name);

		token <<= MONO_RESOLUTION_SCOPE_BITS;
		token |= MONO_RESOLUTION_SCOPE_MODULEREF;
		g_hash_table_insert (assembly->handleref, image, GUINT_TO_POINTER (token));

		return token;
	}
	
	if (assembly_is_dynamic (image->assembly))
		/* FIXME: */
		memset (cols, 0, sizeof (cols));
	else {
		/* image->assembly->image is the manifest module */
		image = image->assembly->image;
		mono_metadata_decode_row (&image->tables [MONO_TABLE_ASSEMBLY], 0, cols, MONO_ASSEMBLY_SIZE);
	}

	table = &assembly->tables [MONO_TABLE_ASSEMBLYREF];
	token = table->next_idx ++;
	table->rows ++;
	alloc_table (table, table->rows);
	values = table->values + token * MONO_ASSEMBLYREF_SIZE;
	values [MONO_ASSEMBLYREF_NAME] = string_heap_insert (&assembly->sheap, image->assembly_name);
	values [MONO_ASSEMBLYREF_MAJOR_VERSION] = cols [MONO_ASSEMBLY_MAJOR_VERSION];
	values [MONO_ASSEMBLYREF_MINOR_VERSION] = cols [MONO_ASSEMBLY_MINOR_VERSION];
	values [MONO_ASSEMBLYREF_BUILD_NUMBER] = cols [MONO_ASSEMBLY_BUILD_NUMBER];
	values [MONO_ASSEMBLYREF_REV_NUMBER] = cols [MONO_ASSEMBLY_REV_NUMBER];
	values [MONO_ASSEMBLYREF_FLAGS] = 0;
	values [MONO_ASSEMBLYREF_CULTURE] = 0;
	values [MONO_ASSEMBLYREF_HASH_VALUE] = 0;

	if (strcmp ("", image->assembly->aname.culture)) {
		values [MONO_ASSEMBLYREF_CULTURE] = string_heap_insert (&assembly->sheap,
				image->assembly->aname.culture);
	}

	if ((pubkey = mono_image_get_public_key (image, &publen))) {
		guchar pubtoken [9];
		pubtoken [0] = 8;
		mono_digest_get_public_token (pubtoken + 1, (guchar*)pubkey, publen);
		values [MONO_ASSEMBLYREF_PUBLIC_KEY] = mono_image_add_stream_data (&assembly->blob, (char*)pubtoken, 9);
	} else {
		values [MONO_ASSEMBLYREF_PUBLIC_KEY] = 0;
	}
	token <<= MONO_RESOLUTION_SCOPE_BITS;
	token |= MONO_RESOLUTION_SCOPE_ASSEMBLYREF;
	g_hash_table_insert (assembly->handleref, image, GUINT_TO_POINTER (token));
	return token;
}

#ifndef DISABLE_REFLECTION_EMIT
gboolean
mono_reflection_methodbuilder_from_method_builder (ReflectionMethodBuilder *rmb, MonoReflectionMethodBuilder *mb, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);
	memset (rmb, 0, sizeof (ReflectionMethodBuilder));

	rmb->ilgen = mb->ilgen;
	rmb->rtype = (MonoReflectionType*)mb->rtype;
	return_val_if_nok (error, FALSE);
	rmb->parameters = mb->parameters;
	rmb->generic_params = mb->generic_params;
	rmb->generic_container = mb->generic_container;
	rmb->opt_types = NULL;
	rmb->pinfo = mb->pinfo;
	rmb->attrs = mb->attrs;
	rmb->iattrs = mb->iattrs;
	rmb->call_conv = mb->call_conv;
	rmb->code = mb->code;
	rmb->type = mb->type;
	rmb->name = mb->name;
	rmb->table_idx = &mb->table_idx;
	rmb->init_locals = mb->init_locals;
	rmb->skip_visibility = FALSE;
	rmb->return_modreq = mb->return_modreq;
	rmb->return_modopt = mb->return_modopt;
	rmb->param_modreq = mb->param_modreq;
	rmb->param_modopt = mb->param_modopt;
	rmb->permissions = mb->permissions;
	rmb->mhandle = mb->mhandle;
	rmb->nrefs = 0;
	rmb->refs = NULL;

	if (mb->dll) {
		rmb->charset = mb->charset;
		rmb->extra_flags = mb->extra_flags;
		rmb->native_cc = mb->native_cc;
		rmb->dllentry = mb->dllentry;
		rmb->dll = mb->dll;
	}

	return TRUE;
}

gboolean
mono_reflection_methodbuilder_from_ctor_builder (ReflectionMethodBuilder *rmb, MonoReflectionCtorBuilder *mb, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	const char *name = mb->attrs & METHOD_ATTRIBUTE_STATIC ? ".cctor": ".ctor";

	error_init (error);

	memset (rmb, 0, sizeof (ReflectionMethodBuilder));

	rmb->ilgen = mb->ilgen;
	rmb->rtype = mono_type_get_object_checked (mono_domain_get (), &mono_defaults.void_class->byval_arg, error);
	return_val_if_nok (error, FALSE);
	rmb->parameters = mb->parameters;
	rmb->generic_params = NULL;
	rmb->generic_container = NULL;
	rmb->opt_types = NULL;
	rmb->pinfo = mb->pinfo;
	rmb->attrs = mb->attrs;
	rmb->iattrs = mb->iattrs;
	rmb->call_conv = mb->call_conv;
	rmb->code = NULL;
	rmb->type = mb->type;
	rmb->name = mono_string_new_checked (mono_domain_get (), name, error);
	return_val_if_nok (error, FALSE);
	rmb->table_idx = &mb->table_idx;
	rmb->init_locals = mb->init_locals;
	rmb->skip_visibility = FALSE;
	rmb->return_modreq = NULL;
	rmb->return_modopt = NULL;
	rmb->param_modreq = mb->param_modreq;
	rmb->param_modopt = mb->param_modopt;
	rmb->permissions = mb->permissions;
	rmb->mhandle = mb->mhandle;
	rmb->nrefs = 0;
	rmb->refs = NULL;

	return TRUE;
}

static void
reflection_methodbuilder_from_dynamic_method (ReflectionMethodBuilder *rmb, MonoReflectionDynamicMethod *mb)
{
	MONO_REQ_GC_UNSAFE_MODE;

	memset (rmb, 0, sizeof (ReflectionMethodBuilder));

	rmb->ilgen = mb->ilgen;
	rmb->rtype = mb->rtype;
	rmb->parameters = mb->parameters;
	rmb->generic_params = NULL;
	rmb->generic_container = NULL;
	rmb->opt_types = NULL;
	rmb->pinfo = NULL;
	rmb->attrs = mb->attrs;
	rmb->iattrs = 0;
	rmb->call_conv = mb->call_conv;
	rmb->code = NULL;
	rmb->type = (MonoObject *) mb->owner;
	rmb->name = mb->name;
	rmb->table_idx = NULL;
	rmb->init_locals = mb->init_locals;
	rmb->skip_visibility = mb->skip_visibility;
	rmb->return_modreq = NULL;
	rmb->return_modopt = NULL;
	rmb->param_modreq = NULL;
	rmb->param_modopt = NULL;
	rmb->permissions = NULL;
	rmb->mhandle = mb->mhandle;
	rmb->nrefs = 0;
	rmb->refs = NULL;
}	
#else /* DISABLE_REFLECTION_EMIT */
gboolean
mono_reflection_methodbuilder_from_method_builder (ReflectionMethodBuilder *rmb, MonoReflectionMethodBuilder *mb, MonoError *error) {
	g_assert_not_reached ();
	return FALSE;
}
gboolean
mono_reflection_methodbuilder_from_ctor_builder (ReflectionMethodBuilder *rmb, MonoReflectionCtorBuilder *mb, MonoError *error)
{
	g_assert_not_reached ();
	return FALSE;
}
#endif /* DISABLE_REFLECTION_EMIT */

#ifndef DISABLE_REFLECTION_EMIT
static guint32
mono_image_add_memberef_row (MonoDynamicImage *assembly, guint32 parent, const char *name, guint32 sig)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoDynamicTable *table;
	guint32 *values;
	guint32 token, pclass;

	switch (parent & MONO_TYPEDEFORREF_MASK) {
	case MONO_TYPEDEFORREF_TYPEREF:
		pclass = MONO_MEMBERREF_PARENT_TYPEREF;
		break;
	case MONO_TYPEDEFORREF_TYPESPEC:
		pclass = MONO_MEMBERREF_PARENT_TYPESPEC;
		break;
	case MONO_TYPEDEFORREF_TYPEDEF:
		pclass = MONO_MEMBERREF_PARENT_TYPEDEF;
		break;
	default:
		g_warning ("unknown typeref or def token 0x%08x for %s", parent, name);
		return 0;
	}
	/* extract the index */
	parent >>= MONO_TYPEDEFORREF_BITS;

	table = &assembly->tables [MONO_TABLE_MEMBERREF];

	if (assembly->save) {
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_MEMBERREF_SIZE;
		values [MONO_MEMBERREF_CLASS] = pclass | (parent << MONO_MEMBERREF_PARENT_BITS);
		values [MONO_MEMBERREF_NAME] = string_heap_insert (&assembly->sheap, name);
		values [MONO_MEMBERREF_SIGNATURE] = sig;
	}

	token = MONO_TOKEN_MEMBER_REF | table->next_idx;
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
mono_image_get_memberref_token (MonoDynamicImage *assembly, MonoType *type, const char *name, guint32 sig)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	guint32 parent = mono_image_typedef_or_ref (assembly, type);
	return mono_image_add_memberef_row (assembly, parent, name, sig);
}


guint32
mono_image_get_methodref_token (MonoDynamicImage *assembly, MonoMethod *method, gboolean create_typespec)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	guint32 token;
	MonoMethodSignature *sig;
	
	create_typespec = create_typespec && method->is_generic && method->klass->image != &assembly->image;

	if (create_typespec) {
		token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, GUINT_TO_POINTER (GPOINTER_TO_UINT (method) + 1)));
		if (token)
			return token;
	} 

	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, method));
	if (token && !create_typespec)
		return token;

	g_assert (!method->is_inflated);
	if (!token) {
		/*
		 * A methodref signature can't contain an unmanaged calling convention.
		 */
		sig = mono_metadata_signature_dup (mono_method_signature (method));
		if ((sig->call_convention != MONO_CALL_DEFAULT) && (sig->call_convention != MONO_CALL_VARARG))
			sig->call_convention = MONO_CALL_DEFAULT;
		token = mono_image_get_memberref_token (assembly, &method->klass->byval_arg,
			method->name,  mono_dynimage_encode_method_signature (assembly, sig));
		g_free (sig);
		g_hash_table_insert (assembly->handleref, method, GUINT_TO_POINTER(token));
	}

	if (create_typespec) {
		MonoDynamicTable *table = &assembly->tables [MONO_TABLE_METHODSPEC];
		g_assert (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF);
		token = (mono_metadata_token_index (token) << MONO_METHODDEFORREF_BITS) | MONO_METHODDEFORREF_METHODREF;

		if (assembly->save) {
			guint32 *values;

			alloc_table (table, table->rows + 1);
			values = table->values + table->next_idx * MONO_METHODSPEC_SIZE;
			values [MONO_METHODSPEC_METHOD] = token;
			values [MONO_METHODSPEC_SIGNATURE] = mono_dynimage_encode_generic_method_sig (assembly, &mono_method_get_generic_container (method)->context);
		}

		token = MONO_TOKEN_METHOD_SPEC | table->next_idx;
		table->next_idx ++;
		/*methodspec and memberef tokens are diferent, */
		g_hash_table_insert (assembly->handleref, GUINT_TO_POINTER (GPOINTER_TO_UINT (method) + 1), GUINT_TO_POINTER (token));
		return token;
	}
	return token;
}

static guint32
mono_image_get_varargs_method_token (MonoDynamicImage *assembly, guint32 original,
				     const gchar *name, guint32 sig)
{
	MonoDynamicTable *table;
	guint32 token;
	guint32 *values;
	
	table = &assembly->tables [MONO_TABLE_MEMBERREF];

	if (assembly->save) {
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_MEMBERREF_SIZE;
		values [MONO_MEMBERREF_CLASS] = original;
		values [MONO_MEMBERREF_NAME] = string_heap_insert (&assembly->sheap, name);
		values [MONO_MEMBERREF_SIGNATURE] = sig;
	}

	token = MONO_TOKEN_MEMBER_REF | table->next_idx;
	table->next_idx ++;

	return token;
}

#else /* DISABLE_REFLECTION_EMIT */

guint32
mono_image_get_methodref_token (MonoDynamicImage *assembly, MonoMethod *method, gboolean create_typespec)
{
	g_assert_not_reached ();
	return -1;
}
#endif

static gboolean
is_field_on_inst (MonoClassField *field)
{
	return mono_class_is_ginst (field->parent) && mono_class_get_generic_class (field->parent)->is_dynamic;
}

#ifndef DISABLE_REFLECTION_EMIT
static guint32
mono_image_get_fieldref_token (MonoDynamicImage *assembly, MonoClassField *field)
{
	MonoType *type;
	guint32 token;

	g_assert (field);
	g_assert (field->parent);

	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, field));
	if (token)
		return token;

	if (mono_class_is_ginst (field->parent) && mono_class_get_generic_class (field->parent)->container_class && mono_class_get_generic_class (field->parent)->container_class->fields) {
		int index = field - field->parent->fields;
		type = mono_field_get_type (&mono_class_get_generic_class (field->parent)->container_class->fields [index]);
	} else {
		type = mono_field_get_type (field);
	}
	token = mono_image_get_memberref_token (assembly, &field->parent->byval_arg,
											mono_field_get_name (field),
											mono_dynimage_encode_fieldref_signature (assembly, field->parent->image, type));
	g_hash_table_insert (assembly->handleref, field, GUINT_TO_POINTER(token));
	return token;
}

static guint32
method_encode_methodspec (MonoDynamicImage *assembly, MonoMethod *method)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 token, mtoken = 0, sig;
	MonoMethodInflated *imethod;
	MonoMethod *declaring;

	table = &assembly->tables [MONO_TABLE_METHODSPEC];

	g_assert (method->is_inflated);
	imethod = (MonoMethodInflated *) method;
	declaring = imethod->declaring;

	sig = mono_dynimage_encode_method_signature (assembly, mono_method_signature (declaring));
	mtoken = mono_image_get_memberref_token (assembly, &method->klass->byval_arg, declaring->name, sig);

	if (!mono_method_signature (declaring)->generic_param_count)
		return mtoken;

	switch (mono_metadata_token_table (mtoken)) {
	case MONO_TABLE_MEMBERREF:
		mtoken = (mono_metadata_token_index (mtoken) << MONO_METHODDEFORREF_BITS) | MONO_METHODDEFORREF_METHODREF;
		break;
	case MONO_TABLE_METHOD:
		mtoken = (mono_metadata_token_index (mtoken) << MONO_METHODDEFORREF_BITS) | MONO_METHODDEFORREF_METHODDEF;
		break;
	default:
		g_assert_not_reached ();
	}

	sig = mono_dynimage_encode_generic_method_sig (assembly, mono_method_get_context (method));

	if (assembly->save) {
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_METHODSPEC_SIZE;
		values [MONO_METHODSPEC_METHOD] = mtoken;
		values [MONO_METHODSPEC_SIGNATURE] = sig;
	}

	token = MONO_TOKEN_METHOD_SPEC | table->next_idx;
	table->next_idx ++;

	return token;
}

static guint32
mono_image_get_methodspec_token (MonoDynamicImage *assembly, MonoMethod *method)
{
	MonoMethodInflated *imethod;
	guint32 token;
	
	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, method));
	if (token)
		return token;

	g_assert (method->is_inflated);
	imethod = (MonoMethodInflated *) method;

	if (mono_method_signature (imethod->declaring)->generic_param_count) {
		token = method_encode_methodspec (assembly, method);
	} else {
		guint32 sig = mono_dynimage_encode_method_signature (
			assembly, mono_method_signature (imethod->declaring));
		token = mono_image_get_memberref_token (
			assembly, &method->klass->byval_arg, method->name, sig);
	}

	g_hash_table_insert (assembly->handleref, method, GUINT_TO_POINTER(token));
	return token;
}

static guint32
mono_image_get_inflated_method_token (MonoDynamicImage *assembly, MonoMethod *m)
{
	MonoMethodInflated *imethod = (MonoMethodInflated *) m;
	guint32 sig, token;

	sig = mono_dynimage_encode_method_signature (assembly, mono_method_signature (imethod->declaring));
	token = mono_image_get_memberref_token (
		assembly, &m->klass->byval_arg, m->name, sig);

	return token;
}

static guint32 
mono_image_get_sighelper_token (MonoDynamicImage *assembly, MonoReflectionSigHelperHandle helper, MonoError *error)
{
	guint32 idx;
	MonoDynamicTable *table;
	guint32 *values;

	error_init (error);

	table = &assembly->tables [MONO_TABLE_STANDALONESIG];
	idx = table->next_idx ++;
	table->rows ++;
	alloc_table (table, table->rows);
	values = table->values + idx * MONO_STAND_ALONE_SIGNATURE_SIZE;

	values [MONO_STAND_ALONE_SIGNATURE] =
		mono_dynimage_encode_reflection_sighelper (assembly, helper, error);
	return_val_if_nok (error, 0);
	
	return idx;
}

static int
reflection_cc_to_file (int call_conv) {
	switch (call_conv & 0x3) {
	case 0:
	case 1: return MONO_CALL_DEFAULT;
	case 2: return MONO_CALL_VARARG;
	default:
		g_assert_not_reached ();
	}
	return 0;
}
#endif /* !DISABLE_REFLECTION_EMIT */

struct _ArrayMethod {
	MonoType *parent;
	MonoMethodSignature *sig;
	char *name;
	guint32 token;
};

void
mono_sre_array_method_free (ArrayMethod *am)
{
	g_free (am->sig);
	g_free (am->name);
	g_free (am);
}

#ifndef DISABLE_REFLECTION_EMIT
static guint32
mono_image_get_array_token (MonoDynamicImage *assembly, MonoReflectionArrayMethodHandle m, MonoError *error)
{
	MonoMethodSignature *sig = NULL;
	char *name = NULL;

	error_init (error);

	MonoArrayHandle parameters = MONO_HANDLE_NEW_GET (MonoArray, m, parameters);
	guint32 nparams = mono_array_handle_length (parameters);
	sig = (MonoMethodSignature *)g_malloc0 (MONO_SIZEOF_METHOD_SIGNATURE + sizeof (MonoType*) * nparams);
	sig->hasthis = 1;
	sig->sentinelpos = -1;
	sig->call_convention = reflection_cc_to_file (MONO_HANDLE_GETVAL (m, call_conv));
	sig->param_count = nparams;
	MonoReflectionTypeHandle ret = MONO_HANDLE_NEW_GET (MonoReflectionType, m, ret);
	if (!MONO_HANDLE_IS_NULL (ret)) {
		sig->ret = mono_reflection_type_handle_mono_type (ret, error);
		goto_if_nok (error, fail);
	} else
		sig->ret = &mono_defaults.void_class->byval_arg;

	MonoReflectionTypeHandle parent = MONO_HANDLE_NEW_GET (MonoReflectionType, m, parent);
	MonoType *mtype = mono_reflection_type_handle_mono_type (parent, error);
	goto_if_nok (error, fail);

	for (int i = 0; i < nparams; ++i) {
		sig->params [i] = mono_type_array_get_and_resolve (parameters, i, error);
		goto_if_nok (error, fail);
	}

	MonoStringHandle mname = MONO_HANDLE_NEW_GET (MonoString, m, name);
	name = mono_string_handle_to_utf8 (mname, error);
	goto_if_nok (error, fail);

	ArrayMethod *am = NULL;
	for (GList *tmp = assembly->array_methods; tmp; tmp = tmp->next) {
		am = (ArrayMethod *)tmp->data;
		if (strcmp (name, am->name) == 0 && 
				mono_metadata_type_equal (am->parent, mtype) &&
				mono_metadata_signature_equal (am->sig, sig)) {
			g_free (name);
			g_free (sig);
			MONO_HANDLE_SETVAL (m, table_idx, guint32, am->token & 0xffffff);
			return am->token;
		}
	}
	am = g_new0 (ArrayMethod, 1);
	am->name = name;
	am->sig = sig;
	am->parent = mtype;
	am->token = mono_image_get_memberref_token (assembly, am->parent, name,
		mono_dynimage_encode_method_signature (assembly, sig));
	assembly->array_methods = g_list_prepend (assembly->array_methods, am);
	MONO_HANDLE_SETVAL (m, table_idx, guint32, am->token & 0xffffff);
	return am->token;
fail:
	g_free (name);
	g_free (sig);
	return 0;

}
#endif

#ifndef DISABLE_REFLECTION_EMIT

/*
 * mono_image_insert_string:
 * @module: module builder object
 * @str: a string
 *
 * Insert @str into the user string stream of @module.
 */
guint32
mono_image_insert_string (MonoReflectionModuleBuilderHandle ref_module, MonoStringHandle str, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	guint32 idx;
	char buf [16];
	char *b = buf;
	guint32 token = 0;

	MonoDynamicImage *assembly = MONO_HANDLE_GETVAL (ref_module, dynamic_image);
	if (!assembly) {
		if (!mono_image_module_basic_init (ref_module, error))
			goto leave;

		assembly = MONO_HANDLE_GETVAL (ref_module, dynamic_image);
	}
	g_assert (assembly != NULL);

	if (assembly->save) {
		int32_t length = mono_string_length (MONO_HANDLE_RAW (str));
		mono_metadata_encode_value (1 | (length * 2), b, &b);
		idx = mono_image_add_stream_data (&assembly->us, buf, b-buf);
		/* pinned */
		uint32_t gchandle = mono_gchandle_from_handle (MONO_HANDLE_CAST (MonoObject, str), TRUE);
		const char *p = (const char*)mono_string_chars (MONO_HANDLE_RAW (str));
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	{
		char *swapped = g_malloc (2 * length);

		swap_with_size (swapped, p, 2, length);
		mono_image_add_stream_data (&assembly->us, swapped, length * 2);
		g_free (swapped);
	}
#else
		mono_image_add_stream_data (&assembly->us, p, length * 2);
#endif
		mono_gchandle_free (gchandle);
		mono_image_add_stream_data (&assembly->us, "", 1);
	} else {
		idx = assembly->us.index ++;
	}

	token = MONO_TOKEN_STRING | idx;
	mono_dynamic_image_register_token (assembly, token, MONO_HANDLE_CAST (MonoObject, str), MONO_DYN_IMAGE_TOK_NEW);

leave:
	HANDLE_FUNCTION_RETURN_VAL (token);
}

static guint32
create_method_token (MonoDynamicImage *assembly, MonoMethod *method, MonoArrayHandle opt_param_types, MonoError *error)
{
	guint32 sig_token, parent;


	int nargs = mono_array_handle_length (opt_param_types);
	MonoMethodSignature *old = mono_method_signature (method);
	MonoMethodSignature *sig = mono_metadata_signature_alloc ( &assembly->image, old->param_count + nargs);

	sig->hasthis = old->hasthis;
	sig->explicit_this = old->explicit_this;
	sig->call_convention = old->call_convention;
	sig->generic_param_count = old->generic_param_count;
	sig->param_count = old->param_count + nargs;
	sig->sentinelpos = old->param_count;
	sig->ret = old->ret;

	for (int i = 0; i < old->param_count; i++)
		sig->params [i] = old->params [i];

	MonoReflectionTypeHandle rt = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	for (int i = 0; i < nargs; i++) {
		MONO_HANDLE_ARRAY_GETREF (rt, opt_param_types, i);
		sig->params [old->param_count + i] = mono_reflection_type_handle_mono_type (rt, error);
		goto_if_nok (error, fail);
	}

	parent = mono_image_typedef_or_ref (assembly, &method->klass->byval_arg);
	g_assert ((parent & MONO_TYPEDEFORREF_MASK) == MONO_MEMBERREF_PARENT_TYPEREF);
	parent >>= MONO_TYPEDEFORREF_BITS;

	parent <<= MONO_MEMBERREF_PARENT_BITS;
	parent |= MONO_MEMBERREF_PARENT_TYPEREF;

	sig_token = mono_dynimage_encode_method_signature (assembly, sig);
	guint32 token = mono_image_get_varargs_method_token (assembly, parent, method->name, sig_token);
	g_hash_table_insert (assembly->vararg_aux_hash, GUINT_TO_POINTER (token), sig);
	return token;
fail:
	return 0;
}

guint32
mono_image_create_method_token (MonoDynamicImage *assembly, MonoObjectHandle obj, MonoArrayHandle opt_param_types, MonoError *error)
{
	guint32 token = 0;

	error_init (error);

	MonoClass *klass = mono_handle_class (obj);
	if (strcmp (klass->name, "MonoMethod") == 0 || strcmp (klass->name, "MonoCMethod") == 0) {
		MonoReflectionMethodHandle ref_method = MONO_HANDLE_CAST (MonoReflectionMethod, obj);
		MonoMethod *method = MONO_HANDLE_GETVAL (ref_method, method);
		g_assert (!MONO_HANDLE_IS_NULL (opt_param_types) && (mono_method_signature (method)->sentinelpos >= 0));
		token = create_method_token (assembly, method, opt_param_types, error);
		goto_if_nok (error, fail);
	} else if (strcmp (klass->name, "MethodBuilder") == 0) {
		g_assert_not_reached ();
	} else {
		g_error ("requested method token for %s\n", klass->name);
	}

	mono_dynamic_image_register_token (assembly, token, obj, MONO_DYN_IMAGE_TOK_NEW);
	return token;
fail:
	g_assert (!mono_error_ok (error));
	return 0;
}

/*
 * mono_image_create_token:
 * @assembly: a dynamic assembly
 * @obj:
 * @register_token: Whenever to register the token in the assembly->tokens hash. 
 *
 * Get a token to insert in the IL code stream for the given MemberInfo.
 * The metadata emission routines need to pass FALSE as REGISTER_TOKEN, since by that time, 
 * the table_idx-es were recomputed, so registering the token would overwrite an existing 
 * entry.
 */
guint32
mono_image_create_token (MonoDynamicImage *assembly, MonoObjectHandle obj, 
			 gboolean create_open_instance, gboolean register_token,
			 MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	guint32 token = 0;

	error_init (error);

	MonoClass *klass = mono_handle_class (obj);
	MonoObjectHandle register_obj = MONO_HANDLE_NEW (MonoObject, NULL);
	MONO_HANDLE_ASSIGN (register_obj, obj);

	/* Check for user defined reflection objects */
	/* TypeDelegator is the only corlib type which doesn't look like a MonoReflectionType */
	if (klass->image != mono_defaults.corlib || (strcmp (klass->name, "TypeDelegator") == 0)) {
		mono_error_set_not_supported (error, "User defined subclasses of System.Type are not yet supported");
		goto leave;
	}

	/* This function is called from ModuleBuilder:getToken multiple times for the same objects */
	int how_collide = MONO_DYN_IMAGE_TOK_SAME_OK;

	if (strcmp (klass->name, "RuntimeType") == 0) {
		MonoType *type = mono_reflection_type_handle_mono_type (MONO_HANDLE_CAST (MonoReflectionType, obj), error);
		goto_if_nok (error, leave);
		MonoClass *mc = mono_class_from_mono_type (type);
		token = mono_metadata_token_from_dor (
			mono_dynimage_encode_typedef_or_ref_full (assembly, type, !mono_class_is_gtd (mc) || create_open_instance));
		/* If it's a RuntimeType now, we could have registered a
		 * TypeBuilder for it before, so replacing is okay. */
		how_collide = MONO_DYN_IMAGE_TOK_REPLACE;
	} else if (strcmp (klass->name, "MonoCMethod") == 0 ||
			   strcmp (klass->name, "MonoMethod") == 0) {
		MonoReflectionMethodHandle m = MONO_HANDLE_CAST (MonoReflectionMethod, obj);
		MonoMethod *method = MONO_HANDLE_GETVAL (m, method);
		if (method->is_inflated) {
			if (create_open_instance) {
				guint32 methodspec_token = mono_image_get_methodspec_token (assembly, method);
				MonoReflectionMethodHandle canonical_obj =
					mono_method_get_object_handle (MONO_HANDLE_DOMAIN (obj), method, NULL, error);
				goto_if_nok (error, leave);
				MONO_HANDLE_ASSIGN (register_obj, canonical_obj);
				token = methodspec_token;
			} else
				token = mono_image_get_inflated_method_token (assembly, method);
		} else if ((method->klass->image == &assembly->image) &&
			 !mono_class_is_ginst (method->klass)) {
			static guint32 method_table_idx = 0xffffff;
			if (method->klass->wastypebuilder) {
				/* we use the same token as the one that was assigned
				 * to the Methodbuilder.
				 * FIXME: do the equivalent for Fields.
				 */
				token = method->token;
				how_collide = MONO_DYN_IMAGE_TOK_REPLACE;
			} else {
				/*
				 * Each token should have a unique index, but the indexes are
				 * assigned by managed code, so we don't know about them. An
				 * easy solution is to count backwards...
				 */
				method_table_idx --;
				token = MONO_TOKEN_METHOD_DEF | method_table_idx;
				how_collide = MONO_DYN_IMAGE_TOK_NEW;
			}
		} else {
			guint32 methodref_token = mono_image_get_methodref_token (assembly, method, create_open_instance);
			/* We need to register a 'canonical' object.  The same
			 * MonoMethod could have been reflected via different
			 * classes so the MonoReflectionMethod:reftype could be
			 * different, and the object lookup in
			 * dynamic_image_register_token would assert assert. So
			 * we pick the MonoReflectionMethod object that has the
			 * reflected type as NULL (ie, take the declaring type
			 * of the method) */
			MonoReflectionMethodHandle canonical_obj =
				mono_method_get_object_handle (MONO_HANDLE_DOMAIN (obj), method, NULL, error);
			goto_if_nok (error, leave);
			MONO_HANDLE_ASSIGN (register_obj, canonical_obj);
			token = methodref_token;
		}
		/*g_print ("got token 0x%08x for %s\n", token, m->method->name);*/
	} else if (strcmp (klass->name, "MonoField") == 0) {
		MonoReflectionFieldHandle f = MONO_HANDLE_CAST (MonoReflectionField, obj);
		MonoClassField *field = MONO_HANDLE_GETVAL (f, field);
		if ((field->parent->image == &assembly->image) && !is_field_on_inst (field)) {
			static guint32 field_table_idx = 0xffffff;
			field_table_idx --;
			token = MONO_TOKEN_FIELD_DEF | field_table_idx;
			how_collide = MONO_DYN_IMAGE_TOK_NEW;
		} else {
			guint32 fieldref_token = mono_image_get_fieldref_token (assembly, field);
			/* Same as methodref: get a canonical object to
			 * register with the token. */
			MonoReflectionFieldHandle canonical_obj =
				mono_field_get_object_handle (MONO_HANDLE_DOMAIN (obj), field->parent, field, error);
			goto_if_nok (error, leave);
			MONO_HANDLE_ASSIGN (register_obj, canonical_obj);
			token = fieldref_token;
		}
		/*g_print ("got token 0x%08x for %s\n", token, f->field->name);*/
	} else if (strcmp (klass->name, "MonoArrayMethod") == 0) {
		MonoReflectionArrayMethodHandle m = MONO_HANDLE_CAST (MonoReflectionArrayMethod, obj);
		/* mono_image_get_array_token caches tokens by signature */
		guint32 array_token = mono_image_get_array_token (assembly, m, error);
		goto_if_nok (error, leave);
		token = array_token;
		/* ModuleBuilder:GetArrayMethod() always returns a fresh
		 * MonoArrayMethod instance even given the same method name and
		 * signature.  But they're all interchangeable, so it's okay to
		 * replace.
		 */
		how_collide = MONO_DYN_IMAGE_TOK_REPLACE;
	} else if (strcmp (klass->name, "SignatureHelper") == 0) {
		MonoReflectionSigHelperHandle s = MONO_HANDLE_CAST (MonoReflectionSigHelper, obj);
		/* always returns a fresh token */
		guint32 sig_token = MONO_TOKEN_SIGNATURE | mono_image_get_sighelper_token (assembly, s, error);
		goto_if_nok (error, leave);
		token = sig_token;
		how_collide = MONO_DYN_IMAGE_TOK_NEW;
	} else {
		g_error ("requested token for %s\n", klass->name);
	}

	if (register_token)
		mono_dynamic_image_register_token (assembly, token, register_obj, how_collide);

leave:
	HANDLE_FUNCTION_RETURN_VAL (token);
}


#endif

#ifndef DISABLE_REFLECTION_EMIT

static gboolean
assemblybuilderaccess_can_refonlyload (guint32 access)
{
	return (access & 0x4) != 0;
}

static gboolean
assemblybuilderaccess_can_run (guint32 access)
{
	return (access & MonoAssemblyBuilderAccess_Run) != 0;
}

static gboolean
assemblybuilderaccess_can_save (guint32 access)
{
	return (access & MonoAssemblyBuilderAccess_Save) != 0;
}


/*
 * mono_reflection_dynimage_basic_init:
 * @assembly: an assembly builder object
 *
 * Create the MonoImage that represents the assembly builder and setup some
 * of the helper hash table and the basic metadata streams.
 */
void
mono_reflection_dynimage_basic_init (MonoReflectionAssemblyBuilder *assemblyb)
{
	MonoError error;
	MonoDynamicAssembly *assembly;
	MonoDynamicImage *image;
	MonoDomain *domain = mono_object_domain (assemblyb);
	
	if (assemblyb->dynamic_assembly)
		return;

	assembly = assemblyb->dynamic_assembly = g_new0 (MonoDynamicAssembly, 1);

	MONO_PROFILER_RAISE (assembly_loading, (&assembly->assembly));
	
	assembly->assembly.ref_count = 1;
	assembly->assembly.dynamic = TRUE;
	assembly->assembly.corlib_internal = assemblyb->corlib_internal;
	assemblyb->assembly.assembly = (MonoAssembly*)assembly;
	assembly->assembly.basedir = mono_string_to_utf8_checked (assemblyb->dir, &error);
	if (mono_error_set_pending_exception (&error))
		return;
	if (assemblyb->culture) {
		assembly->assembly.aname.culture = mono_string_to_utf8_checked (assemblyb->culture, &error);
		if (mono_error_set_pending_exception (&error))
			return;
	} else
		assembly->assembly.aname.culture = g_strdup ("");

        if (assemblyb->version) {
			char *vstr = mono_string_to_utf8_checked (assemblyb->version, &error);
			if (mono_error_set_pending_exception (&error))
				return;
			char **version = g_strsplit (vstr, ".", 4);
			char **parts = version;
			assembly->assembly.aname.major = atoi (*parts++);
			assembly->assembly.aname.minor = atoi (*parts++);
			assembly->assembly.aname.build = *parts != NULL ? atoi (*parts++) : 0;
			assembly->assembly.aname.revision = *parts != NULL ? atoi (*parts) : 0;

			g_strfreev (version);
			g_free (vstr);
        } else {
			assembly->assembly.aname.major = 0;
			assembly->assembly.aname.minor = 0;
			assembly->assembly.aname.build = 0;
			assembly->assembly.aname.revision = 0;
        }

	assembly->assembly.ref_only = assemblybuilderaccess_can_refonlyload (assemblyb->access);
	assembly->run = assemblybuilderaccess_can_run (assemblyb->access);
	assembly->save = assemblybuilderaccess_can_save (assemblyb->access);
	assembly->domain = domain;

	char *assembly_name = mono_string_to_utf8_checked (assemblyb->name, &error);
	if (mono_error_set_pending_exception (&error))
		return;
	image = mono_dynamic_image_create (assembly, assembly_name, g_strdup ("RefEmit_YouForgotToDefineAModule"));
	image->initial_image = TRUE;
	assembly->assembly.aname.name = image->image.name;
	assembly->assembly.image = &image->image;
	if (assemblyb->pktoken && assemblyb->pktoken->max_length) {
		/* -1 to correct for the trailing NULL byte */
		if (assemblyb->pktoken->max_length != MONO_PUBLIC_KEY_TOKEN_LENGTH - 1) {
			g_error ("Public key token length invalid for assembly %s: %i", assembly->assembly.aname.name, assemblyb->pktoken->max_length);
		}
		memcpy (&assembly->assembly.aname.public_key_token, mono_array_addr (assemblyb->pktoken, guint8, 0), assemblyb->pktoken->max_length);		
	}

	mono_domain_assemblies_lock (domain);
	domain->domain_assemblies = g_slist_append (domain->domain_assemblies, assembly);
	mono_domain_assemblies_unlock (domain);

	register_assembly (mono_object_domain (assemblyb), &assemblyb->assembly, &assembly->assembly);
	
	MONO_PROFILER_RAISE (assembly_loaded, (&assembly->assembly));
	
	mono_assembly_invoke_load_hook ((MonoAssembly*)assembly);
}

#endif /* !DISABLE_REFLECTION_EMIT */

#ifndef DISABLE_REFLECTION_EMIT
static gpointer
register_assembly (MonoDomain *domain, MonoReflectionAssembly *res, MonoAssembly *assembly)
{
	return CACHE_OBJECT (MonoReflectionAssembly *, assembly, &res->object, NULL);
}

static MonoReflectionModuleBuilderHandle
register_module (MonoDomain *domain, MonoReflectionModuleBuilderHandle res, MonoDynamicImage *module)
{
	return CACHE_OBJECT_HANDLE (MonoReflectionModuleBuilderHandle, module, MONO_HANDLE_CAST (MonoObject, res), NULL);
}

static gboolean
image_module_basic_init (MonoReflectionModuleBuilderHandle moduleb, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (moduleb);
	MonoDynamicImage *image = MONO_HANDLE_GETVAL (moduleb, dynamic_image);
	MonoReflectionAssemblyBuilderHandle ab = MONO_HANDLE_NEW (MonoReflectionAssemblyBuilder, NULL);
	MONO_HANDLE_GET (ab, moduleb, assemblyb);
	if (!image) {
		/*
		 * FIXME: we already created an image in mono_reflection_dynimage_basic_init (), but
		 * we don't know which module it belongs to, since that is only 
		 * determined at assembly save time.
		 */
		/*image = (MonoDynamicImage*)ab->dynamic_assembly->assembly.image; */
		MonoStringHandle abname = MONO_HANDLE_NEW_GET (MonoString, ab, name);
		char *name = mono_string_handle_to_utf8 (abname, error);
		return_val_if_nok (error, FALSE);
		MonoStringHandle modfqname = MONO_HANDLE_NEW_GET (MonoString, MONO_HANDLE_CAST (MonoReflectionModule, moduleb), fqname);
		char *fqname = mono_string_handle_to_utf8 (modfqname, error);
		if (!is_ok (error)) {
			g_free (name);
			return FALSE;
		}
		MonoDynamicAssembly *dynamic_assembly = MONO_HANDLE_GETVAL (ab, dynamic_assembly);
		image = mono_dynamic_image_create (dynamic_assembly, name, fqname);

		MONO_HANDLE_SETVAL (MONO_HANDLE_CAST (MonoReflectionModule, moduleb), image, MonoImage*, &image->image);
		MONO_HANDLE_SETVAL (moduleb, dynamic_image, MonoDynamicImage*, image);
		register_module (domain, moduleb, image);

		/* register the module with the assembly */
		MonoImage *ass = dynamic_assembly->assembly.image;
		int module_count = ass->module_count;
		MonoImage **new_modules = g_new0 (MonoImage *, module_count + 1);

		if (ass->modules)
			memcpy (new_modules, ass->modules, module_count * sizeof (MonoImage *));
		new_modules [module_count] = &image->image;
		mono_image_addref (&image->image);

		g_free (ass->modules);
		ass->modules = new_modules;
		ass->module_count ++;
	}
	return TRUE;
}

static gboolean
mono_image_module_basic_init (MonoReflectionModuleBuilderHandle moduleb, MonoError *error)
{
	error_init (error);
	return image_module_basic_init (moduleb, error);
}

#endif

static gboolean
is_corlib_type (MonoClass *klass)
{
	return klass->image == mono_defaults.corlib;
}

#define check_corlib_type_cached(_class, _namespace, _name) do { \
	static MonoClass *cached_class; \
	if (cached_class) \
		return cached_class == _class; \
	if (is_corlib_type (_class) && !strcmp (_name, _class->name) && !strcmp (_namespace, _class->name_space)) { \
		cached_class = _class; \
		return TRUE; \
	} \
	return FALSE; \
} while (0) \


MonoType*
mono_type_array_get_and_resolve (MonoArrayHandle array, int idx, MonoError *error)
{
	HANDLE_FUNCTION_ENTER();
	error_init (error);
	MonoReflectionTypeHandle t = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	MONO_HANDLE_ARRAY_GETREF (t, array, idx);
	MonoType *result = mono_reflection_type_handle_mono_type (t, error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}


#ifndef DISABLE_REFLECTION_EMIT
static gboolean
is_sre_array (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "ArrayType");
}

static gboolean
is_sre_byref (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "ByRefType");
}

static gboolean
is_sre_pointer (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "PointerType");
}

static gboolean
is_sre_generic_instance (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "TypeBuilderInstantiation");
}

static gboolean
is_sre_type_builder (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "TypeBuilder");
}

static gboolean
is_sre_method_builder (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "MethodBuilder");
}

gboolean
mono_is_sre_ctor_builder (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "ConstructorBuilder");
}

static gboolean
is_sre_field_builder (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "FieldBuilder");
}

static gboolean
is_sre_gparam_builder (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "GenericTypeParameterBuilder");
}

static gboolean
is_sre_enum_builder (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "EnumBuilder");
}

gboolean
mono_is_sre_method_on_tb_inst (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "MethodOnTypeBuilderInst");
}

gboolean
mono_is_sre_ctor_on_tb_inst (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection.Emit", "ConstructorOnTypeBuilderInst");
}

static MonoReflectionTypeHandle
mono_reflection_type_get_underlying_system_type (MonoReflectionTypeHandle t, MonoError *error)
{
	static MonoMethod *method_get_underlying_system_type = NULL;
	HANDLE_FUNCTION_ENTER ();

	error_init (error);

	if (!method_get_underlying_system_type)
		method_get_underlying_system_type = mono_class_get_method_from_name (mono_defaults.systemtype_class, "get_UnderlyingSystemType", 0);

	MonoReflectionTypeHandle rt = MONO_HANDLE_NEW (MonoReflectionType, NULL);

	MonoMethod *usertype_method = mono_object_handle_get_virtual_method (MONO_HANDLE_CAST (MonoObject, t), method_get_underlying_system_type, error);
	goto_if_nok (error, leave);

	MONO_HANDLE_ASSIGN (rt, MONO_HANDLE_NEW (MonoReflectionType, mono_runtime_invoke_checked (usertype_method, MONO_HANDLE_RAW (t), NULL, error)));

leave:
	HANDLE_FUNCTION_RETURN_REF (MonoReflectionType, rt);
}

MonoType*
mono_reflection_type_get_handle (MonoReflectionType* ref_raw, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MONO_HANDLE_DCL (MonoReflectionType, ref);
	MonoType *result = mono_reflection_type_handle_mono_type (ref, error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static MonoType*
reflection_instance_handle_mono_type (MonoReflectionGenericClassHandle ref_gclass, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoType *result = NULL;
	MonoType **types = NULL;

	MonoArrayHandle typeargs = MONO_HANDLE_NEW_GET (MonoArray, ref_gclass, type_arguments);
	int count = mono_array_handle_length (typeargs);
	types = g_new0 (MonoType*, count);
	MonoReflectionTypeHandle t = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	for (int i = 0; i < count; ++i) {
		MONO_HANDLE_ARRAY_GETREF (t, typeargs, i);
		types [i] = mono_reflection_type_handle_mono_type (t, error);
		if (!types[i] || !is_ok (error)) {
			goto leave;
		}
	}
	/* Need to resolve the generic_type in order for it to create its generic context. */
	MonoReflectionTypeHandle ref_gtd = MONO_HANDLE_NEW_GET (MonoReflectionType, ref_gclass, generic_type);
	MonoType *gtd = mono_reflection_type_handle_mono_type (ref_gtd, error);
	goto_if_nok (error, leave);
	MonoClass *gtd_klass = mono_class_from_mono_type (gtd);
	if (is_sre_type_builder (mono_handle_class (ref_gtd))) {
		reflection_setup_internal_class (MONO_HANDLE_CAST (MonoReflectionTypeBuilder, ref_gtd), error);
		goto_if_nok (error, leave);
	}
	g_assert (count == 0 || mono_class_is_gtd (gtd_klass));
	result = mono_reflection_bind_generic_parameters (ref_gtd, count, types, error);
	goto_if_nok (error, leave);
	g_assert (result);
	MONO_HANDLE_SETVAL (MONO_HANDLE_CAST (MonoReflectionType, ref_gclass), type, MonoType*, result);
leave:
	g_free (types);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static MonoType*
reflection_param_handle_mono_type (MonoReflectionGenericParamHandle ref_gparam, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoType *result = NULL;


	MonoReflectionTypeBuilderHandle ref_tbuilder = MONO_HANDLE_NEW_GET (MonoReflectionTypeBuilder, ref_gparam, tbuilder);
	MonoReflectionModuleBuilderHandle ref_module = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, ref_tbuilder, module);
	MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (ref_module, dynamic_image);
	MonoImage *image = &dynamic_image->image;

	MonoGenericParamFull *param = mono_image_new0 (image, MonoGenericParamFull, 1);

	MonoStringHandle ref_name = MONO_HANDLE_NEW_GET (MonoString, ref_gparam, name);
	param->info.name = mono_string_to_utf8_image (image, ref_name, error);
	mono_error_assert_ok (error);
	param->param.num = MONO_HANDLE_GETVAL (ref_gparam, index);

	MonoReflectionMethodBuilderHandle ref_mbuilder = MONO_HANDLE_NEW_GET (MonoReflectionMethodBuilder, ref_gparam, mbuilder);
	if (!MONO_HANDLE_IS_NULL (ref_mbuilder)) {
		MonoGenericContainer *generic_container = MONO_HANDLE_GETVAL (ref_mbuilder, generic_container);
		if (!generic_container) {
			generic_container = (MonoGenericContainer *)mono_image_alloc0 (image, sizeof (MonoGenericContainer));
			generic_container->is_method = TRUE;
			/*
			 * Cannot set owner.method, since the MonoMethod is not created yet.
			 * Set the image field instead, so type_in_image () works.
			 */
			generic_container->is_anonymous = TRUE;
			generic_container->owner.image = image;
			MONO_HANDLE_SETVAL (ref_mbuilder, generic_container, MonoGenericContainer*, generic_container);
		}
		param->param.owner = generic_container;
	} else {
		MonoType *type = mono_reflection_type_handle_mono_type (MONO_HANDLE_CAST (MonoReflectionType, ref_tbuilder), error);
		goto_if_nok (error, leave);
		MonoClass *owner = mono_class_from_mono_type (type);
		g_assert (mono_class_is_gtd (owner));
		param->param.owner = mono_class_get_generic_container (owner);
	}

	MonoClass *pklass = mono_class_from_generic_parameter_internal ((MonoGenericParam *) param);

	result = &pklass->byval_arg;

	mono_class_set_ref_info (pklass, MONO_HANDLE_CAST (MonoObject, ref_gparam));
	mono_image_append_class_to_reflection_info_set (pklass);

	MONO_HANDLE_SETVAL (MONO_HANDLE_CAST (MonoReflectionType, ref_gparam), type, MonoType*, result);

leave:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static MonoType*
mono_type_array_get_and_resolve_raw (MonoArray* array_raw, int idx, MonoError *error)
{
	HANDLE_FUNCTION_ENTER(); /* FIXME callers of mono_type_array_get_and_resolve_raw should use handles */
	error_init (error);
	MONO_HANDLE_DCL (MonoArray, array);
	MonoType *result = mono_type_array_get_and_resolve (array, idx, error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

MonoType*
mono_reflection_type_handle_mono_type (MonoReflectionTypeHandle ref, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);

	MonoType* result = NULL;

	g_assert (ref);
	if (MONO_HANDLE_IS_NULL (ref))
		goto leave;
	MonoType *t = MONO_HANDLE_GETVAL (ref, type);
	if (t) {
		result = t;
		goto leave;
	}

	if (mono_reflection_is_usertype (ref)) {
		MONO_HANDLE_ASSIGN (ref, mono_reflection_type_get_underlying_system_type (ref, error));
		if (!is_ok (error) || MONO_HANDLE_IS_NULL (ref) || mono_reflection_is_usertype (ref))
			goto leave;
		t = MONO_HANDLE_GETVAL (ref, type);
		if (t) {
			result = t;
			goto leave;
		}
	}

	MonoClass *klass = mono_handle_class (ref);

	if (is_sre_array (klass)) {
		MonoReflectionArrayTypeHandle sre_array = MONO_HANDLE_CAST (MonoReflectionArrayType, ref);
		MonoReflectionTypeHandle ref_element = MONO_HANDLE_NEW_GET (MonoReflectionType, sre_array, element_type);
		MonoType *base = mono_reflection_type_handle_mono_type (ref_element, error);
		goto_if_nok (error, leave);
		g_assert (base);
		gint32 rank = MONO_HANDLE_GETVAL (sre_array, rank);
		MonoClass *eclass = mono_class_from_mono_type (base);
		result = mono_image_new0 (eclass->image, MonoType, 1);
		if (rank == 0)  {
			result->type = MONO_TYPE_SZARRAY;
			result->data.klass = eclass;
		} else {
			MonoArrayType *at = (MonoArrayType *)mono_image_alloc0 (eclass->image, sizeof (MonoArrayType));
			result->type = MONO_TYPE_ARRAY;
			result->data.array = at;
			at->eklass = eclass;
			at->rank = rank;
		}
		MONO_HANDLE_SETVAL (ref, type, MonoType*, result);
	} else if (is_sre_byref (klass)) {
		MonoReflectionDerivedTypeHandle sre_byref = MONO_HANDLE_CAST (MonoReflectionDerivedType, ref);
		MonoReflectionTypeHandle ref_element = MONO_HANDLE_NEW_GET (MonoReflectionType, sre_byref, element_type);
		MonoType *base = mono_reflection_type_handle_mono_type (ref_element, error);
		goto_if_nok (error, leave);
		g_assert (base);
		result = &mono_class_from_mono_type (base)->this_arg;
		MONO_HANDLE_SETVAL (ref, type, MonoType*, result);
	} else if (is_sre_pointer (klass)) {
		MonoReflectionDerivedTypeHandle sre_pointer = MONO_HANDLE_CAST (MonoReflectionDerivedType, ref);
		MonoReflectionTypeHandle ref_element = MONO_HANDLE_NEW_GET (MonoReflectionType, sre_pointer, element_type);
		MonoType *base = mono_reflection_type_handle_mono_type (ref_element, error);
		goto_if_nok (error, leave);
		g_assert (base);
		result = &mono_ptr_class_get (base)->byval_arg;
		MONO_HANDLE_SETVAL (ref, type, MonoType*, result);
	} else if (is_sre_generic_instance (klass)) {
		result = reflection_instance_handle_mono_type (MONO_HANDLE_CAST (MonoReflectionGenericClass, ref), error);
	} else if (is_sre_gparam_builder (klass)) {
		result = reflection_param_handle_mono_type (MONO_HANDLE_CAST (MonoReflectionGenericParam, ref), error);
	} else if (is_sre_enum_builder (klass)) {
		MonoReflectionEnumBuilderHandle ref_ebuilder = MONO_HANDLE_CAST (MonoReflectionEnumBuilder, ref);

		MonoReflectionTypeHandle ref_tb = MONO_HANDLE_NEW_GET (MonoReflectionType, ref_ebuilder, tb);
		result = mono_reflection_type_handle_mono_type (ref_tb, error);
	} else if (is_sre_type_builder (klass)) {
		MonoReflectionTypeBuilderHandle ref_tb = MONO_HANDLE_CAST (MonoReflectionTypeBuilder, ref);

		/* This happens when a finished type references an unfinished one. Have to create the minimal type */
		reflection_setup_internal_class (ref_tb, error);
		mono_error_assert_ok (error);
		result = MONO_HANDLE_GETVAL (ref, type);
	} else {
		g_error ("Cannot handle corlib user type %s", mono_type_full_name (&mono_object_class(ref)->byval_arg));
	}
leave:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

/**
 * LOCKING: Assumes the loader lock is held.
 */
static MonoMethodSignature*
parameters_to_signature (MonoImage *image, MonoArrayHandle parameters, MonoError *error) {
	MonoMethodSignature *sig;
	int count, i;

	error_init (error);

	count = MONO_HANDLE_IS_NULL (parameters) ? 0 : mono_array_handle_length (parameters);

	sig = (MonoMethodSignature *)mono_image_g_malloc0 (image, MONO_SIZEOF_METHOD_SIGNATURE + sizeof (MonoType*) * count);
	sig->param_count = count;
	sig->sentinelpos = -1; /* FIXME */
	for (i = 0; i < count; ++i) {
		sig->params [i] = mono_type_array_get_and_resolve (parameters, i, error);
		if (!is_ok (error)) {
			image_g_free (image, sig);
			return NULL;
		}
	}
	return sig;
}

/**
 * LOCKING: Assumes the loader lock is held.
 */
static MonoMethodSignature*
ctor_builder_to_signature (MonoImage *image, MonoReflectionCtorBuilderHandle ctor, MonoError *error) {
	MonoMethodSignature *sig;

	error_init (error);

	sig = parameters_to_signature (image, MONO_HANDLE_NEW_GET (MonoArray, ctor, parameters), error);
	return_val_if_nok (error, NULL);
	sig->hasthis = MONO_HANDLE_GETVAL (ctor, attrs) & METHOD_ATTRIBUTE_STATIC? 0: 1;
	sig->ret = &mono_defaults.void_class->byval_arg;
	return sig;
}

static MonoMethodSignature*
ctor_builder_to_signature_raw (MonoImage *image, MonoReflectionCtorBuilder* ctor_raw, MonoError *error) {
	HANDLE_FUNCTION_ENTER();
	MONO_HANDLE_DCL (MonoReflectionCtorBuilder, ctor);
	MonoMethodSignature *sig = ctor_builder_to_signature (image, ctor, error);
	HANDLE_FUNCTION_RETURN_VAL (sig);
}
/**
 * LOCKING: Assumes the loader lock is held.
 */
static MonoMethodSignature*
method_builder_to_signature (MonoImage *image, MonoReflectionMethodBuilderHandle method, MonoError *error) {
	MonoMethodSignature *sig;

	error_init (error);

	sig = parameters_to_signature (image, MONO_HANDLE_NEW_GET(MonoArray, method, parameters), error);
	return_val_if_nok (error, NULL);
	sig->hasthis = MONO_HANDLE_GETVAL (method, attrs) & METHOD_ATTRIBUTE_STATIC? 0: 1;
	MonoReflectionTypeHandle rtype = MONO_HANDLE_NEW_GET (MonoReflectionType, method, rtype);
	if (!MONO_HANDLE_IS_NULL (rtype)) {
		sig->ret = mono_reflection_type_handle_mono_type (rtype, error);
		if (!is_ok (error)) {
			image_g_free (image, sig);
			return NULL;
		}
	} else {
		sig->ret = &mono_defaults.void_class->byval_arg;
	}
	MonoArrayHandle generic_params = MONO_HANDLE_NEW_GET (MonoArray, method, generic_params);
	sig->generic_param_count = MONO_HANDLE_IS_NULL (generic_params) ? 0 : mono_array_handle_length (generic_params);
	return sig;
}

static MonoMethodSignature*
dynamic_method_to_signature (MonoReflectionDynamicMethodHandle method, MonoError *error) {
	HANDLE_FUNCTION_ENTER ();
	MonoMethodSignature *sig = NULL;

	error_init (error);

	sig = parameters_to_signature (NULL, MONO_HANDLE_NEW_GET (MonoArray, method, parameters), error);
	goto_if_nok (error, leave);
	sig->hasthis = MONO_HANDLE_GETVAL (method, attrs) & METHOD_ATTRIBUTE_STATIC? 0: 1;
	MonoReflectionTypeHandle rtype = MONO_HANDLE_NEW_GET (MonoReflectionType, method, rtype);
	if (!MONO_HANDLE_IS_NULL (rtype)) {
		sig->ret = mono_reflection_type_handle_mono_type (rtype, error);
		if (!is_ok (error)) {
			g_free (sig);
			sig = NULL;
			goto leave;
		}
	} else {
		sig->ret = &mono_defaults.void_class->byval_arg;
	}
	sig->generic_param_count = 0;
leave:
	HANDLE_FUNCTION_RETURN_VAL (sig);
}

static void
get_prop_name_and_type (MonoObject *prop, char **name, MonoType **type, MonoError *error)
{
	error_init (error);
	MonoClass *klass = mono_object_class (prop);
	if (strcmp (klass->name, "PropertyBuilder") == 0) {
		MonoReflectionPropertyBuilder *pb = (MonoReflectionPropertyBuilder *)prop;
		*name = mono_string_to_utf8_checked (pb->name, error);
		return_if_nok (error);
		*type = mono_reflection_type_get_handle ((MonoReflectionType*)pb->type, error);
	} else {
		MonoReflectionProperty *p = (MonoReflectionProperty *)prop;
		*name = g_strdup (p->property->name);
		if (p->property->get)
			*type = mono_method_signature (p->property->get)->ret;
		else
			*type = mono_method_signature (p->property->set)->params [mono_method_signature (p->property->set)->param_count - 1];
	}
}

static void
get_field_name_and_type (MonoObject *field, char **name, MonoType **type, MonoError *error)
{
	error_init (error);
	MonoClass *klass = mono_object_class (field);
	if (strcmp (klass->name, "FieldBuilder") == 0) {
		MonoReflectionFieldBuilder *fb = (MonoReflectionFieldBuilder *)field;
		*name = mono_string_to_utf8_checked (fb->name, error);
		return_if_nok (error);
		*type = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type, error);
	} else {
		MonoReflectionField *f = (MonoReflectionField *)field;
		*name = g_strdup (mono_field_get_name (f->field));
		*type = f->field->type;
	}
}

#else /* DISABLE_REFLECTION_EMIT */

static gboolean
is_sre_type_builder (MonoClass *klass)
{
	return FALSE;
}

static gboolean
is_sre_generic_instance (MonoClass *klass)
{
	return FALSE;
}

gboolean
mono_is_sre_ctor_builder (MonoClass *klass)
{
	return FALSE;
}

gboolean
mono_is_sre_method_on_tb_inst (MonoClass *klass)
{
	return FALSE;
}

gboolean
mono_is_sre_ctor_on_tb_inst (MonoClass *klass)
{
	return FALSE;
}

#endif /* !DISABLE_REFLECTION_EMIT */


static gboolean
is_sr_mono_field (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection", "MonoField");
}

gboolean
mono_is_sr_mono_property (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection", "MonoProperty");
}

static gboolean
is_sr_mono_method (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection", "MonoMethod");
}

gboolean
mono_is_sr_mono_cmethod (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection", "MonoCMethod");
}

gboolean
mono_class_is_reflection_method_or_constructor (MonoClass *klass)
{
	return is_sr_mono_method (klass) || mono_is_sr_mono_cmethod (klass);
}

gboolean
mono_is_sre_type_builder (MonoClass *klass)
{
	return is_sre_type_builder (klass);
}

gboolean
mono_is_sre_generic_instance (MonoClass *klass)
{
	return is_sre_generic_instance (klass);
}



/**
 * encode_cattr_value:
 * Encode a value in a custom attribute stream of bytes.
 * The value to encode is either supplied as an object in argument val
 * (valuetypes are boxed), or as a pointer to the data in the
 * argument argval.
 * @type represents the type of the value
 * @buffer is the start of the buffer
 * @p the current position in the buffer
 * @buflen contains the size of the buffer and is used to return the new buffer size
 * if this needs to be realloced.
 * @retbuffer and @retp return the start and the position of the buffer
 * @error set on error.
 */
static void
encode_cattr_value (MonoAssembly *assembly, char *buffer, char *p, char **retbuffer, char **retp, guint32 *buflen, MonoType *type, MonoObject *arg, char *argval, MonoError *error)
{
	MonoTypeEnum simple_type;
	
	error_init (error);
	if ((p-buffer) + 10 >= *buflen) {
		char *newbuf;
		*buflen *= 2;
		newbuf = (char *)g_realloc (buffer, *buflen);
		p = newbuf + (p-buffer);
		buffer = newbuf;
	}
	if (!argval)
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
	case MONO_TYPE_R8:
		swap_with_size (p, argval, 8, 1);
		p += 8;
		break;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		swap_with_size (p, argval, 8, 1);
		p += 8;
		break;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			simple_type = mono_class_enum_basetype (type->data.klass)->type;
			goto handle_enum;
		} else {
			g_warning ("generic valutype %s not handled in custom attr value decoding", type->data.klass->name);
		}
		break;
	case MONO_TYPE_STRING: {
		char *str;
		guint32 slen;
		if (!arg) {
			*p++ = 0xFF;
			break;
		}
		str = mono_string_to_utf8_checked ((MonoString*)arg, error);
		return_if_nok (error);
		slen = strlen (str);
		if ((p-buffer) + 10 + slen >= *buflen) {
			char *newbuf;
			*buflen *= 2;
			*buflen += slen;
			newbuf = (char *)g_realloc (buffer, *buflen);
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
		MonoType *arg_type;
		if (!arg) {
			*p++ = 0xFF;
			break;
		}
handle_type:
		arg_type = mono_reflection_type_get_handle ((MonoReflectionType*)arg, error);
		return_if_nok (error);

		str = type_get_qualified_name (arg_type, NULL);
		slen = strlen (str);
		if ((p-buffer) + 10 + slen >= *buflen) {
			char *newbuf;
			*buflen *= 2;
			*buflen += slen;
			newbuf = (char *)g_realloc (buffer, *buflen);
			p = newbuf + (p-buffer);
			buffer = newbuf;
		}
		mono_metadata_encode_value (slen, p, &p);
		memcpy (p, str, slen);
		p += slen;
		g_free (str);
		break;
	}
	case MONO_TYPE_SZARRAY: {
		int len, i;
		MonoClass *eclass, *arg_eclass;

		if (!arg) {
			*p++ = 0xff; *p++ = 0xff; *p++ = 0xff; *p++ = 0xff;
			break;
		}
		len = mono_array_length ((MonoArray*)arg);
		*p++ = len & 0xff;
		*p++ = (len >> 8) & 0xff;
		*p++ = (len >> 16) & 0xff;
		*p++ = (len >> 24) & 0xff;
		*retp = p;
		*retbuffer = buffer;
		eclass = type->data.klass;
		arg_eclass = mono_object_class (arg)->element_class;

		if (!eclass) {
			/* Happens when we are called from the MONO_TYPE_OBJECT case below */
			eclass = mono_defaults.object_class;
		}
		if (eclass == mono_defaults.object_class && arg_eclass->valuetype) {
			char *elptr = mono_array_addr ((MonoArray*)arg, char, 0);
			int elsize = mono_class_array_element_size (arg_eclass);
			for (i = 0; i < len; ++i) {
				encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, &arg_eclass->byval_arg, NULL, elptr, error);
				return_if_nok (error);
				elptr += elsize;
			}
		} else if (eclass->valuetype && arg_eclass->valuetype) {
			char *elptr = mono_array_addr ((MonoArray*)arg, char, 0);
			int elsize = mono_class_array_element_size (eclass);
			for (i = 0; i < len; ++i) {
				encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, &eclass->byval_arg, NULL, elptr, error);
				return_if_nok (error);
				elptr += elsize;
			}
		} else {
			for (i = 0; i < len; ++i) {
				encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, &eclass->byval_arg, mono_array_get ((MonoArray*)arg, MonoObject*, i), NULL, error);
				return_if_nok (error);
			}
		}
		break;
	}
	case MONO_TYPE_OBJECT: {
		MonoClass *klass;
		char *str;
		guint32 slen;

		/*
		 * The parameter type is 'object' but the type of the actual
		 * argument is not. So we have to add type information to the blob
		 * too. This is completely undocumented in the spec.
		 */

		if (arg == NULL) {
			*p++ = MONO_TYPE_STRING;	// It's same hack as MS uses
			*p++ = 0xFF;
			break;
		}
		
		klass = mono_object_class (arg);

		if (mono_object_isinst_checked (arg, mono_defaults.systemtype_class, error)) {
			*p++ = 0x50;
			goto handle_type;
		} else {
			return_if_nok (error);
		}

		if (klass->enumtype) {
			*p++ = 0x55;
		} else if (klass == mono_defaults.string_class) {
			simple_type = MONO_TYPE_STRING;
			*p++ = 0x0E;
			goto handle_enum;
		} else if (klass->rank == 1) {
			*p++ = 0x1D;
			if (klass->element_class->byval_arg.type == MONO_TYPE_OBJECT)
				/* See Partition II, Appendix B3 */
				*p++ = 0x51;
			else
				*p++ = klass->element_class->byval_arg.type;
			encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, &klass->byval_arg, arg, NULL, error);
			return_if_nok (error);
			break;
		} else if (klass->byval_arg.type >= MONO_TYPE_BOOLEAN && klass->byval_arg.type <= MONO_TYPE_R8) {
			*p++ = simple_type = klass->byval_arg.type;
			goto handle_enum;
		} else {
			g_error ("unhandled type in custom attr");
		}
		str = type_get_qualified_name (mono_class_get_type(klass), NULL);
		slen = strlen (str);
		if ((p-buffer) + 10 + slen >= *buflen) {
			char *newbuf;
			*buflen *= 2;
			*buflen += slen;
			newbuf = (char *)g_realloc (buffer, *buflen);
			p = newbuf + (p-buffer);
			buffer = newbuf;
		}
		mono_metadata_encode_value (slen, p, &p);
		memcpy (p, str, slen);
		p += slen;
		g_free (str);
		simple_type = mono_class_enum_basetype (klass)->type;
		goto handle_enum;
	}
	default:
		g_error ("type 0x%02x not yet supported in custom attr encoder", simple_type);
	}
	*retp = p;
	*retbuffer = buffer;
}

static void
encode_field_or_prop_type (MonoType *type, char *p, char **retp)
{
	if (type->type == MONO_TYPE_VALUETYPE && type->data.klass->enumtype) {
		char *str = type_get_qualified_name (type, NULL);
		int slen = strlen (str);

		*p++ = 0x55;
		/*
		 * This seems to be optional...
		 * *p++ = 0x80;
		 */
		mono_metadata_encode_value (slen, p, &p);
		memcpy (p, str, slen);
		p += slen;
		g_free (str);
	} else if (type->type == MONO_TYPE_OBJECT) {
		*p++ = 0x51;
	} else if (type->type == MONO_TYPE_CLASS) {
		/* it should be a type: encode_cattr_value () has the check */
		*p++ = 0x50;
	} else {
		mono_metadata_encode_value (type->type, p, &p);
		if (type->type == MONO_TYPE_SZARRAY)
			/* See the examples in Partition VI, Annex B */
			encode_field_or_prop_type (&type->data.klass->byval_arg, p, &p);
	}

	*retp = p;
}

#ifndef DISABLE_REFLECTION_EMIT
static void
encode_named_val (MonoReflectionAssembly *assembly, char *buffer, char *p, char **retbuffer, char **retp, guint32 *buflen, MonoType *type, char *name, MonoObject *value, MonoError *error)
{
	int len;

	error_init (error);

	/* Preallocate a large enough buffer */
	if (type->type == MONO_TYPE_VALUETYPE && type->data.klass->enumtype) {
		char *str = type_get_qualified_name (type, NULL);
		len = strlen (str);
		g_free (str);
	} else if (type->type == MONO_TYPE_SZARRAY && type->data.klass->enumtype) {
		char *str = type_get_qualified_name (&type->data.klass->byval_arg, NULL);
		len = strlen (str);
		g_free (str);
	} else {
		len = 0;
	}
	len += strlen (name);

	if ((p-buffer) + 20 + len >= *buflen) {
		char *newbuf;
		*buflen *= 2;
		*buflen += len;
		newbuf = (char *)g_realloc (buffer, *buflen);
		p = newbuf + (p-buffer);
		buffer = newbuf;
	}

	encode_field_or_prop_type (type, p, &p);

	len = strlen (name);
	mono_metadata_encode_value (len, p, &p);
	memcpy (p, name, len);
	p += len;
	encode_cattr_value (assembly->assembly, buffer, p, &buffer, &p, buflen, type, value, NULL, error);
	return_if_nok (error);
	*retp = p;
	*retbuffer = buffer;
}

/**
 * mono_reflection_get_custom_attrs_blob:
 * \param ctor custom attribute constructor
 * \param ctorArgs arguments o the constructor
 * \param properties
 * \param propValues
 * \param fields
 * \param fieldValues
 * Creates the blob of data that needs to be saved in the metadata and that represents
 * the custom attributed described by \p ctor, \p ctorArgs etc.
 * \returns a \c Byte array representing the blob of data.
 */
MonoArray*
mono_reflection_get_custom_attrs_blob (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *propValues, MonoArray *fields, MonoArray* fieldValues) 
{
	MonoError error;
	MonoArray *result = mono_reflection_get_custom_attrs_blob_checked (assembly, ctor, ctorArgs, properties, propValues, fields, fieldValues, &error);
	mono_error_cleanup (&error);
	return result;
}

/**
 * mono_reflection_get_custom_attrs_blob_checked:
 * \param ctor custom attribute constructor
 * \param ctorArgs arguments o the constructor
 * \param properties
 * \param propValues
 * \param fields
 * \param fieldValues
 * \param error set on error
 * Creates the blob of data that needs to be saved in the metadata and that represents
 * the custom attributed described by \p ctor, \p ctorArgs etc.
 * \returns a \c Byte array representing the blob of data.  On failure returns NULL and sets \p error.
 */
MonoArray*
mono_reflection_get_custom_attrs_blob_checked (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *propValues, MonoArray *fields, MonoArray* fieldValues, MonoError *error) 
{
	MonoArray *result = NULL;
	MonoMethodSignature *sig;
	MonoObject *arg;
	char *buffer, *p;
	guint32 buflen, i;

	error_init (error);

	if (strcmp (ctor->vtable->klass->name, "MonoCMethod")) {
		/* sig is freed later so allocate it in the heap */
		sig = ctor_builder_to_signature_raw (NULL, (MonoReflectionCtorBuilder*)ctor, error); /* FIXME use handles */
		if (!is_ok (error)) {
			g_free (sig);
			return NULL;
		}
	} else {
		sig = mono_method_signature (((MonoReflectionMethod*)ctor)->method);
	}

	g_assert (mono_array_length (ctorArgs) == sig->param_count);
	buflen = 256;
	p = buffer = (char *)g_malloc (buflen);
	/* write the prolog */
	*p++ = 1;
	*p++ = 0;
	for (i = 0; i < sig->param_count; ++i) {
		arg = mono_array_get (ctorArgs, MonoObject*, i);
		encode_cattr_value (assembly->assembly, buffer, p, &buffer, &p, &buflen, sig->params [i], arg, NULL, error);
		goto_if_nok (error, leave);
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

			prop = (MonoObject *)mono_array_get (properties, gpointer, i);
			get_prop_name_and_type (prop, &pname, &ptype, error);
			goto_if_nok (error, leave);
			*p++ = 0x54; /* PROPERTY signature */
			encode_named_val (assembly, buffer, p, &buffer, &p, &buflen, ptype, pname, (MonoObject*)mono_array_get (propValues, gpointer, i), error);
			g_free (pname);
			goto_if_nok (error, leave);
		}
	}

	if (fields) {
		MonoObject *field;
		for (i = 0; i < mono_array_length (fields); ++i) {
			MonoType *ftype;
			char *fname;

			field = (MonoObject *)mono_array_get (fields, gpointer, i);
			get_field_name_and_type (field, &fname, &ftype, error);
			goto_if_nok (error, leave);
			*p++ = 0x53; /* FIELD signature */
			encode_named_val (assembly, buffer, p, &buffer, &p, &buflen, ftype, fname, (MonoObject*)mono_array_get (fieldValues, gpointer, i), error);
			g_free (fname);
			goto_if_nok (error, leave);
		}
	}

	g_assert (p - buffer <= buflen);
	buflen = p - buffer;
	result = mono_array_new_checked (mono_domain_get (), mono_defaults.byte_class, buflen, error);
	goto_if_nok (error, leave);
	p = mono_array_addr (result, char, 0);
	memcpy (p, buffer, buflen);
leave:
	g_free (buffer);
	if (strcmp (ctor->vtable->klass->name, "MonoCMethod"))
		g_free (sig);
	return result;
}

static gboolean
reflection_setup_class_hierarchy (GHashTable *unparented, MonoError *error)
{
	error_init (error);

	mono_loader_lock ();

	MonoType *parent_type;
	MonoType *child_type;
	GHashTableIter iter;

	g_hash_table_iter_init (&iter, unparented);

	while (g_hash_table_iter_next (&iter, (gpointer *) &child_type, (gpointer *) &parent_type)) {
		MonoClass *child_class = mono_class_from_mono_type (child_type);
		if (parent_type != NULL) {
			MonoClass *parent_class = mono_class_from_mono_type (parent_type);
			child_class->parent = NULL;
			/* fool mono_class_setup_parent */
			child_class->supertypes = NULL;
			mono_class_setup_parent (child_class, parent_class);
		} else if (strcmp (child_class->name, "Object") == 0 && strcmp (child_class->name_space, "System") == 0) {
			const char *old_n = child_class->name;
			/* trick to get relative numbering right when compiling corlib */
			child_class->name = "BuildingObject";
			mono_class_setup_parent (child_class, mono_defaults.object_class);
			child_class->name = old_n;
		}
		mono_class_setup_mono_type (child_class);
		mono_class_setup_supertypes (child_class);
	}

	mono_loader_unlock ();
	return is_ok (error);
}

static gboolean
reflection_setup_internal_class_internal (MonoReflectionTypeBuilderHandle ref_tb, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);

	mono_loader_lock ();

	gint32 entering_state = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionTypeBuilder, ref_tb), state);
	if (entering_state != MonoTypeBuilderNew) {
		g_assert (MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionType, ref_tb), type));
		goto leave;
	}

	MONO_HANDLE_SETVAL (ref_tb, state, MonoTypeBuilderState, MonoTypeBuilderEntered);
	MonoReflectionModuleBuilderHandle module_ref = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, ref_tb, module);
	GHashTable *unparented_classes = MONO_HANDLE_GETVAL(module_ref, unparented_classes);

	// If this type is already setup, exit. We'll fix the parenting later
	MonoType *type = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionType, ref_tb), type);
	if (type)
		goto leave;

	MonoReflectionModuleBuilderHandle ref_module = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, ref_tb, module);
	MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (ref_module, dynamic_image);

	MonoStringHandle ref_name = MONO_HANDLE_NEW_GET (MonoString, ref_tb, name);
	MonoStringHandle ref_nspace = MONO_HANDLE_NEW_GET (MonoString, ref_tb, nspace);

	guint32 table_idx = MONO_HANDLE_GETVAL (ref_tb, table_idx);
	/*
	 * The size calculation here warrants some explaining. 
	 * reflection_setup_internal_class is called too early, well before we know whether the type will be a GTD or DEF,
	 * meaning we need to alloc enough space to morth a def into a gtd.
	 */
	MonoClass *klass = (MonoClass *)mono_image_alloc0 (&dynamic_image->image, MAX (sizeof (MonoClassDef), sizeof (MonoClassGtd)));
	klass->class_kind = MONO_CLASS_DEF;

	klass->image = &dynamic_image->image;

	klass->inited = 1; /* we lie to the runtime */
	klass->name = mono_string_to_utf8_image (klass->image, ref_name, error);
	goto_if_nok (error, leave);
	klass->name_space = mono_string_to_utf8_image (klass->image, ref_nspace, error);
	goto_if_nok (error, leave);
	klass->type_token = MONO_TOKEN_TYPE_DEF | table_idx;
	mono_class_set_flags (klass, MONO_HANDLE_GETVAL (ref_tb, attrs));
	
	MONO_PROFILER_RAISE (class_loading, (klass));

	klass->element_class = klass;

	g_assert (!mono_class_has_ref_info (klass));
	mono_class_set_ref_info (klass, MONO_HANDLE_CAST (MonoObject, ref_tb));

	MonoReflectionTypeHandle ref_nesting_type = MONO_HANDLE_NEW_GET (MonoReflectionType, ref_tb, nesting_type);
	/* Put into cache so mono_class_get_checked () will find it.
	   Skip nested types as those should not be available on the global scope. */
	if (MONO_HANDLE_IS_NULL (ref_nesting_type))
		mono_image_add_to_name_cache (klass->image, klass->name_space, klass->name, table_idx);

	/*
	  We must register all types as we cannot rely on the name_cache hashtable since we find the class
	  by performing a mono_class_get which does the full resolution.

	  Working around this semantics would require us to write a lot of code for no clear advantage.
	*/
	mono_image_append_class_to_reflection_info_set (klass);

	mono_dynamic_image_register_token (dynamic_image, MONO_TOKEN_TYPE_DEF | table_idx, MONO_HANDLE_CAST (MonoObject, ref_tb), MONO_DYN_IMAGE_TOK_NEW);

	if ((!strcmp (klass->name, "ValueType") && !strcmp (klass->name_space, "System")) ||
			(!strcmp (klass->name, "Object") && !strcmp (klass->name_space, "System")) ||
			(!strcmp (klass->name, "Enum") && !strcmp (klass->name_space, "System"))) {
		klass->instance_size = sizeof (MonoObject);
		klass->size_inited = 1;
		mono_class_setup_vtable_general (klass, NULL, 0, NULL);
	}

	mono_class_setup_mono_type (klass);

	/*
	 * FIXME: handle interfaces.
	 */
	MonoReflectionTypeHandle ref_tb_type = MONO_HANDLE_CAST (MonoReflectionType, ref_tb);
	MONO_HANDLE_SETVAL (ref_tb_type, type, MonoType*, &klass->byval_arg);
	MONO_HANDLE_SETVAL (ref_tb, state, gint32, MonoTypeBuilderFinished);

	reflection_init_generic_class (ref_tb, error);
	goto_if_nok (error, leave);

	// Do here so that the search inside of the parent can see the above type that's been set.
	MonoReflectionTypeHandle ref_parent = MONO_HANDLE_NEW_GET (MonoReflectionType, ref_tb, parent);
	MonoType *parent_type = NULL;
	if (!MONO_HANDLE_IS_NULL (ref_parent)) {
		MonoClass *parent_klass = mono_handle_class (ref_parent);
		gboolean recursive_init = TRUE;

		if (is_sre_type_builder (parent_klass)) {
			MonoTypeBuilderState parent_state = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionTypeBuilder, ref_parent), state);

			if (parent_state != MonoTypeBuilderNew) {
				// Initialize types reachable from parent recursively
				// We'll fix the type hierarchy later
				recursive_init = FALSE;
			}
		}

		if (recursive_init) {
			// If we haven't encountered a cycle, force the creation of ref_parent's type
			mono_reflection_type_handle_mono_type (ref_parent, error);
			goto_if_nok (error, leave);
		}

		parent_type = MONO_HANDLE_GETVAL (ref_parent, type);

		// If we failed to create the parent, fail the child
		if (!parent_type)
			goto leave;
	}

	// Push the child type and parent type to process later
	// Note: parent_type may be null.
	g_assert (!g_hash_table_lookup (unparented_classes, &klass->byval_arg));
	g_hash_table_insert (unparented_classes, &klass->byval_arg, parent_type);

	if (!MONO_HANDLE_IS_NULL (ref_nesting_type)) {
		if (!reflection_setup_internal_class (MONO_HANDLE_CAST (MonoReflectionTypeBuilder, ref_nesting_type), error))
			goto leave;

		MonoType *nesting_type = mono_reflection_type_handle_mono_type (ref_nesting_type, error);
		goto_if_nok (error, leave);
		klass->nested_in = mono_class_from_mono_type (nesting_type);
	}

	/*g_print ("setup %s as %s (%p)\n", klass->name, ((MonoObject*)tb)->vtable->klass->name, tb);*/

	MONO_PROFILER_RAISE (class_loaded, (klass));
	
leave:
	mono_loader_unlock ();
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

/**
 * reflection_init_generic_class:
 * @tb: a TypeBuilder object
 * @error: set on error
 *
 * Creates the generic class after all generic parameters have been added.
 * On success returns TRUE, on failure returns FALSE and sets @error.
 *
 * This assumes that reflection_setup_internal_class has already set up
 * ref_tb
 */
static gboolean
reflection_init_generic_class (MonoReflectionTypeBuilderHandle ref_tb, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	error_init (error);

	MonoTypeBuilderState ref_state = MONO_HANDLE_GETVAL (ref_tb, state);
	g_assert (ref_state == MonoTypeBuilderFinished);

	MonoType *type = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionType, ref_tb), type);
	MonoClass *klass = mono_class_from_mono_type (type);

	MonoArrayHandle generic_params = MONO_HANDLE_NEW_GET (MonoArray, ref_tb, generic_params);
	int count = MONO_HANDLE_IS_NULL (generic_params) ? 0 : mono_array_handle_length (generic_params);

	if (count == 0)
		goto leave;

	if (mono_class_try_get_generic_container (klass) != NULL)
		goto leave; /* already setup */

	MonoGenericContainer *generic_container = (MonoGenericContainer *)mono_image_alloc0 (klass->image, sizeof (MonoGenericContainer));

	generic_container->owner.klass = klass;
	generic_container->type_argc = count;
	generic_container->type_params = (MonoGenericParamFull *)mono_image_alloc0 (klass->image, sizeof (MonoGenericParamFull) * count);

	klass->class_kind = MONO_CLASS_GTD;
	mono_class_set_generic_container (klass, generic_container);


	MonoReflectionGenericParamHandle ref_gparam = MONO_HANDLE_NEW (MonoReflectionGenericParam, NULL);
	for (int i = 0; i < count; i++) {
		MONO_HANDLE_ARRAY_GETREF (ref_gparam, generic_params, i);
		MonoType *param_type = mono_reflection_type_handle_mono_type (MONO_HANDLE_CAST (MonoReflectionType, ref_gparam), error);
		goto_if_nok (error, leave);
		MonoGenericParamFull *param = (MonoGenericParamFull *) param_type->data.generic_param;
		generic_container->type_params [i] = *param;
		/*Make sure we are a diferent type instance */
		generic_container->type_params [i].param.owner = generic_container;
		generic_container->type_params [i].info.pklass = NULL;
		generic_container->type_params [i].info.flags = MONO_HANDLE_GETVAL (ref_gparam, attrs);

		g_assert (generic_container->type_params [i].param.owner);
	}

	generic_container->context.class_inst = mono_get_shared_generic_inst (generic_container);
	MonoGenericContext* context = &generic_container->context;
	MonoType *canonical_inst = &((MonoClassGtd*)klass)->canonical_inst;
	canonical_inst->type = MONO_TYPE_GENERICINST;
	canonical_inst->data.generic_class = mono_metadata_lookup_generic_class (klass, context->class_inst, FALSE);

leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

static MonoMarshalSpec*
mono_marshal_spec_from_builder (MonoImage *image, MonoAssembly *assembly,
				MonoReflectionMarshal *minfo, MonoError *error)
{
	MonoMarshalSpec *res;

	error_init (error);

	res = image_g_new0 (image, MonoMarshalSpec, 1);
	res->native = (MonoMarshalNative)minfo->type;

	switch (minfo->type) {
	case MONO_NATIVE_LPARRAY:
		res->data.array_data.elem_type = (MonoMarshalNative)minfo->eltype;
		if (minfo->has_size) {
			res->data.array_data.param_num = minfo->param_num;
			res->data.array_data.num_elem = minfo->count;
			res->data.array_data.elem_mult = minfo->param_num == -1 ? 0 : 1;
		}
		else {
			res->data.array_data.param_num = -1;
			res->data.array_data.num_elem = -1;
			res->data.array_data.elem_mult = -1;
		}
		break;

	case MONO_NATIVE_BYVALTSTR:
	case MONO_NATIVE_BYVALARRAY:
		res->data.array_data.num_elem = minfo->count;
		break;

	case MONO_NATIVE_CUSTOM:
		if (minfo->marshaltyperef) {
			MonoType *marshaltyperef = mono_reflection_type_get_handle ((MonoReflectionType*)minfo->marshaltyperef, error);
			if (!is_ok (error)) {
				image_g_free (image, res);
				return NULL;
			}
			res->data.custom_data.custom_name =
				type_get_fully_qualified_name (marshaltyperef);
		}
		if (minfo->mcookie) {
			res->data.custom_data.cookie = mono_string_to_utf8_checked (minfo->mcookie, error);
			if (!is_ok (error)) {
				image_g_free (image, res);
				return NULL;
			}
		}
		break;

	default:
		break;
	}

	return res;
}
#endif /* !DISABLE_REFLECTION_EMIT */

MonoReflectionMarshalAsAttributeHandle
mono_reflection_marshal_as_attribute_from_marshal_spec (MonoDomain *domain, MonoClass *klass,
							MonoMarshalSpec *spec, MonoError *error)
{
	error_init (error);
	
	MonoReflectionMarshalAsAttributeHandle minfo = MONO_HANDLE_NEW (MonoReflectionMarshalAsAttribute, mono_object_new_checked (domain, mono_class_get_marshal_as_attribute_class (), error));
	goto_if_nok (error, fail);
	guint32 utype = spec->native;
	MONO_HANDLE_SETVAL (minfo, utype, guint32, utype);

	switch (utype) {
	case MONO_NATIVE_LPARRAY:
		MONO_HANDLE_SETVAL (minfo, array_subtype, guint32, spec->data.array_data.elem_type);
		MONO_HANDLE_SETVAL (minfo, size_const, gint32, spec->data.array_data.num_elem);
		if (spec->data.array_data.param_num != -1)
			MONO_HANDLE_SETVAL (minfo, size_param_index, gint16, spec->data.array_data.param_num);
		break;

	case MONO_NATIVE_BYVALTSTR:
	case MONO_NATIVE_BYVALARRAY:
		MONO_HANDLE_SETVAL (minfo, size_const, gint32, spec->data.array_data.num_elem);
		break;

	case MONO_NATIVE_CUSTOM:
		if (spec->data.custom_data.custom_name) {
			MonoType *mtype = mono_reflection_type_from_name_checked (spec->data.custom_data.custom_name, klass->image, error);
			goto_if_nok (error, fail);

			if (mtype) {
				MonoReflectionTypeHandle rt = mono_type_get_object_handle (domain, mtype, error);
				goto_if_nok (error, fail);

				MONO_HANDLE_SET (minfo, marshal_type_ref, rt);
			}

			MonoStringHandle custom_name = mono_string_new_handle (domain, spec->data.custom_data.custom_name, error);
			goto_if_nok (error, fail);
			MONO_HANDLE_SET (minfo, marshal_type, custom_name);
		}
		if (spec->data.custom_data.cookie) {
			MonoStringHandle cookie = mono_string_new_handle (domain, spec->data.custom_data.cookie, error);
			goto_if_nok (error, fail);
			MONO_HANDLE_SET (minfo, marshal_cookie, cookie);
		}
		break;

	default:
		break;
	}

	return minfo;
fail:
	return MONO_HANDLE_NEW (MonoReflectionMarshalAsAttribute, NULL);
}

#ifndef DISABLE_REFLECTION_EMIT
static MonoMethod*
reflection_methodbuilder_to_mono_method (MonoClass *klass,
					 ReflectionMethodBuilder *rmb,
					 MonoMethodSignature *sig,
					 MonoError *error)
{
	MonoMethod *m;
	MonoMethodWrapper *wrapperm;
	MonoMarshalSpec **specs;
	MonoReflectionMethodAux *method_aux;
	MonoImage *image;
	gboolean dynamic;
	int i;

	error_init (error);
	/*
	 * Methods created using a MethodBuilder should have their memory allocated
	 * inside the image mempool, while dynamic methods should have their memory
	 * malloc'd.
	 */
	dynamic = rmb->refs != NULL;
	image = dynamic ? NULL : klass->image;

	if (!dynamic)
		g_assert (!mono_class_is_ginst (klass));

	mono_loader_lock ();

	if ((rmb->attrs & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
			(rmb->iattrs & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		m = (MonoMethod *)image_g_new0 (image, MonoMethodPInvoke, 1);
	else
		m = (MonoMethod *)image_g_new0 (image, MonoDynamicMethod, 1);

	wrapperm = (MonoMethodWrapper*)m;

	m->dynamic = dynamic;
	m->slot = -1;
	m->flags = rmb->attrs;
	m->iflags = rmb->iattrs;
	m->name = mono_string_to_utf8_image_ignore (image, rmb->name);
	m->klass = klass;
	m->signature = sig;
	m->sre_method = TRUE;
	m->skip_visibility = rmb->skip_visibility;
	if (rmb->table_idx)
		m->token = MONO_TOKEN_METHOD_DEF | (*rmb->table_idx);

	if (m->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		if (klass == mono_defaults.string_class && !strcmp (m->name, ".ctor"))
			m->string_ctor = 1;

		m->signature->pinvoke = 1;
	} else if (m->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		m->signature->pinvoke = 1;

		method_aux = image_g_new0 (image, MonoReflectionMethodAux, 1);

		method_aux->dllentry = rmb->dllentry ? string_to_utf8_image_raw (image, rmb->dllentry, error) : image_strdup (image, m->name);
		mono_error_assert_ok (error);
		method_aux->dll = string_to_utf8_image_raw (image, rmb->dll, error);
		mono_error_assert_ok (error);
		
		((MonoMethodPInvoke*)m)->piflags = (rmb->native_cc << 8) | (rmb->charset ? (rmb->charset - 1) * 2 : 0) | rmb->extra_flags;

		if (image_is_dynamic (klass->image))
			g_hash_table_insert (((MonoDynamicImage*)klass->image)->method_aux_hash, m, method_aux);

		mono_loader_unlock ();

		return m;
	} else if (!(m->flags & METHOD_ATTRIBUTE_ABSTRACT) &&
			   !(m->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		MonoMethodHeader *header;
		guint32 code_size;
		gint32 max_stack, i;
		gint32 num_locals = 0;
		gint32 num_clauses = 0;
		guint8 *code;

		if (rmb->ilgen) {
			code = mono_array_addr (rmb->ilgen->code, guint8, 0);
			code_size = rmb->ilgen->code_len;
			max_stack = rmb->ilgen->max_stack;
			num_locals = rmb->ilgen->locals ? mono_array_length (rmb->ilgen->locals) : 0;
			if (rmb->ilgen->ex_handlers)
				num_clauses = mono_reflection_method_count_clauses (rmb->ilgen);
		} else {
			if (rmb->code) {
				code = mono_array_addr (rmb->code, guint8, 0);
				code_size = mono_array_length (rmb->code);
				/* we probably need to run a verifier on the code... */
				max_stack = 8; 
			}
			else {
				code = NULL;
				code_size = 0;
				max_stack = 8;
			}
		}

		header = (MonoMethodHeader *)mono_image_g_malloc0 (image, MONO_SIZEOF_METHOD_HEADER + num_locals * sizeof (MonoType*));
		header->code_size = code_size;
		header->code = (const unsigned char *)image_g_malloc (image, code_size);
		memcpy ((char*)header->code, code, code_size);
		header->max_stack = max_stack;
		header->init_locals = rmb->init_locals;
		header->num_locals = num_locals;

		for (i = 0; i < num_locals; ++i) {
			MonoReflectionLocalBuilder *lb = 
				mono_array_get (rmb->ilgen->locals, MonoReflectionLocalBuilder*, i);

			header->locals [i] = image_g_new0 (image, MonoType, 1);
			MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)lb->type, error);
			mono_error_assert_ok (error);
			memcpy (header->locals [i], type, MONO_SIZEOF_TYPE);
		}

		header->num_clauses = num_clauses;
		if (num_clauses) {
			header->clauses = method_encode_clauses (image, (MonoDynamicImage*)klass->image,
								 rmb->ilgen, num_clauses, error);
			mono_error_assert_ok (error);
		}

		wrapperm->header = header;
		MonoDynamicMethod *dm = (MonoDynamicMethod*)wrapperm;
		dm->assembly = klass->image->assembly;
	}

	if (rmb->generic_params) {
		int count = mono_array_length (rmb->generic_params);
		MonoGenericContainer *container;

		container = (MonoGenericContainer *)mono_image_alloc0 (klass->image, sizeof (MonoGenericContainer));
		container->is_method = TRUE;
		container->is_anonymous = FALSE;
		container->type_argc = count;
		container->type_params = image_g_new0 (image, MonoGenericParamFull, count);
		container->owner.method = m;

		m->is_generic = TRUE;
		mono_method_set_generic_container (m, container);

		for (i = 0; i < count; i++) {
			MonoReflectionGenericParam *gp =
				mono_array_get (rmb->generic_params, MonoReflectionGenericParam*, i);
			MonoType *gp_type = mono_reflection_type_get_handle ((MonoReflectionType*)gp, error);
			mono_error_assert_ok (error);
			MonoGenericParamFull *param = (MonoGenericParamFull *) gp_type->data.generic_param;
			container->type_params [i] = *param;
			container->type_params [i].param.owner = container;

			gp->type.type->data.generic_param = (MonoGenericParam*)&container->type_params [i];

			MonoClass *gklass = mono_class_from_mono_type (gp_type);
			gklass->wastypebuilder = TRUE;
		}

		/*
		 * The method signature might have pointers to generic parameters that belong to other methods.
		 * This is a valid SRE case, but the resulting method signature must be encoded using the proper
		 * generic parameters.
		 */
		for (i = 0; i < m->signature->param_count; ++i) {
			MonoType *t = m->signature->params [i];
			if (t->type == MONO_TYPE_MVAR) {
				MonoGenericParam *gparam =  t->data.generic_param;
				if (gparam->num < count) {
					m->signature->params [i] = mono_metadata_type_dup (image, m->signature->params [i]);
					m->signature->params [i]->data.generic_param = mono_generic_container_get_param (container, gparam->num);
				}

			}
		}

		if (mono_class_is_gtd (klass)) {
			container->parent = mono_class_get_generic_container (klass);
			container->context.class_inst = mono_class_get_generic_container (klass)->context.class_inst;
		}
		container->context.method_inst = mono_get_shared_generic_inst (container);
	}

	if (rmb->refs) {
		MonoMethodWrapper *mw = (MonoMethodWrapper*)m;
		int i;
		void **data;

		m->wrapper_type = MONO_WRAPPER_DYNAMIC_METHOD;

		mw->method_data = data = image_g_new (image, gpointer, rmb->nrefs + 1);
		data [0] = GUINT_TO_POINTER (rmb->nrefs);
		for (i = 0; i < rmb->nrefs; ++i)
			data [i + 1] = rmb->refs [i];
	}

	method_aux = NULL;

	/* Parameter info */
	if (rmb->pinfo) {
		if (!method_aux)
			method_aux = image_g_new0 (image, MonoReflectionMethodAux, 1);
		method_aux->param_names = image_g_new0 (image, char *, mono_method_signature (m)->param_count + 1);
		for (i = 0; i <= m->signature->param_count; ++i) {
			MonoReflectionParamBuilder *pb;
			if ((pb = mono_array_get (rmb->pinfo, MonoReflectionParamBuilder*, i))) {
				if ((i > 0) && (pb->attrs)) {
					/* Make a copy since it might point to a shared type structure */
					m->signature->params [i - 1] = mono_metadata_type_dup (klass->image, m->signature->params [i - 1]);
					m->signature->params [i - 1]->attrs = pb->attrs;
				}

				if (pb->attrs & PARAM_ATTRIBUTE_HAS_DEFAULT) {
					MonoDynamicImage *assembly;
					guint32 idx, len;
					MonoTypeEnum def_type;
					char *p;
					const char *p2;

					if (!method_aux->param_defaults) {
						method_aux->param_defaults = image_g_new0 (image, guint8*, m->signature->param_count + 1);
						method_aux->param_default_types = image_g_new0 (image, guint32, m->signature->param_count + 1);
					}
					assembly = (MonoDynamicImage*)klass->image;
					idx = mono_dynimage_encode_constant (assembly, pb->def_value, &def_type);
					/* Copy the data from the blob since it might get realloc-ed */
					p = assembly->blob.data + idx;
					len = mono_metadata_decode_blob_size (p, &p2);
					len += p2 - p;
					method_aux->param_defaults [i] = (uint8_t *)image_g_malloc (image, len);
					method_aux->param_default_types [i] = def_type;
					memcpy ((gpointer)method_aux->param_defaults [i], p, len);
				}

				if (pb->name) {
					method_aux->param_names [i] = string_to_utf8_image_raw (image, pb->name, error);
					mono_error_assert_ok (error);
				}
				if (pb->cattrs) {
					if (!method_aux->param_cattr)
						method_aux->param_cattr = image_g_new0 (image, MonoCustomAttrInfo*, m->signature->param_count + 1);
					method_aux->param_cattr [i] = mono_custom_attrs_from_builders (image, klass->image, pb->cattrs);
				}
			}
		}
	}

	/* Parameter marshalling */
	specs = NULL;
	if (rmb->pinfo)		
		for (i = 0; i < mono_array_length (rmb->pinfo); ++i) {
			MonoReflectionParamBuilder *pb;
			if ((pb = mono_array_get (rmb->pinfo, MonoReflectionParamBuilder*, i))) {
				if (pb->marshal_info) {
					if (specs == NULL)
						specs = image_g_new0 (image, MonoMarshalSpec*, sig->param_count + 1);
					specs [pb->position] = 
						mono_marshal_spec_from_builder (image, klass->image->assembly, pb->marshal_info, error);
					if (!is_ok (error)) {
						mono_loader_unlock ();
						image_g_free (image, specs);
						/* FIXME: if image is NULL, this leaks all the other stuff we alloc'd in this function */
						return NULL;
					}
				}
			}
		}
	if (specs != NULL) {
		if (!method_aux)
			method_aux = image_g_new0 (image, MonoReflectionMethodAux, 1);
		method_aux->param_marshall = specs;
	}

	if (image_is_dynamic (klass->image) && method_aux)
		g_hash_table_insert (((MonoDynamicImage*)klass->image)->method_aux_hash, m, method_aux);

	mono_loader_unlock ();

	return m;
}	

static MonoMethod*
ctorbuilder_to_mono_method (MonoClass *klass, MonoReflectionCtorBuilder* mb, MonoError *error)
{
	ReflectionMethodBuilder rmb;
	MonoMethodSignature *sig;

	mono_loader_lock ();

	if (!mono_reflection_methodbuilder_from_ctor_builder (&rmb, mb, error))
		return NULL;

	g_assert (klass->image != NULL);
	sig = ctor_builder_to_signature_raw (klass->image, mb, error); /* FIXME use handles */
	mono_loader_unlock ();
	return_val_if_nok (error, NULL);

	mb->mhandle = reflection_methodbuilder_to_mono_method (klass, &rmb, sig, error);
	return_val_if_nok (error, NULL);
	mono_save_custom_attrs (klass->image, mb->mhandle, mb->cattrs);

	if (!((MonoDynamicImage*)(MonoDynamicImage*)klass->image)->save) {
		/* ilgen is no longer needed */
		mb->ilgen = NULL;
	}

	return mb->mhandle;
}

static MonoMethod*
methodbuilder_to_mono_method (MonoClass *klass, MonoReflectionMethodBuilderHandle ref_mb, MonoError *error)
{
	ReflectionMethodBuilder rmb;
	MonoMethodSignature *sig;

	error_init (error);

	mono_loader_lock ();

	MonoReflectionMethodBuilder *mb = MONO_HANDLE_RAW (ref_mb); /* FIXME use handles */
	if (!mono_reflection_methodbuilder_from_method_builder (&rmb, mb, error))
		return NULL;

	g_assert (klass->image != NULL);
	sig = method_builder_to_signature (klass->image, ref_mb, error);
	mono_loader_unlock ();
	return_val_if_nok (error, NULL);

	MonoMethod *method = reflection_methodbuilder_to_mono_method (klass, &rmb, sig, error);
	return_val_if_nok (error, NULL);
	MONO_HANDLE_SETVAL (ref_mb, mhandle, MonoMethod*, method);
	mono_save_custom_attrs (klass->image, method, mb->cattrs);

	if (!((MonoDynamicImage*)(MonoDynamicImage*)klass->image)->save)
		/* ilgen is no longer needed */
		mb->ilgen = NULL;
	return method;
}

static MonoMethod*
methodbuilder_to_mono_method_raw (MonoClass *klass, MonoReflectionMethodBuilder* mb_raw, MonoError *error)
{
	HANDLE_FUNCTION_ENTER (); /* FIXME change callers of methodbuilder_to_mono_method_raw to use handles */
	error_init (error);
	MONO_HANDLE_DCL (MonoReflectionMethodBuilder, mb);
	MonoMethod *result = methodbuilder_to_mono_method (klass, mb, error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

#endif

#ifndef DISABLE_REFLECTION_EMIT

/**
 * fix_partial_generic_class:
 * @klass: a generic instantiation MonoClass
 * @error: set on error
 *
 * Assumes that the generic container of @klass has its vtable
 * initialized, and updates the parent class, interfaces, methods and
 * fields of @klass by inflating the types using the generic context.
 *
 * On success returns TRUE, on failure returns FALSE and sets @error.
 *
 */
static gboolean
fix_partial_generic_class (MonoClass *klass, MonoError *error)
{
	MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;
	int i;

	error_init (error);

	if (klass->wastypebuilder)
		return TRUE;

	if (klass->parent != gklass->parent) {
		MonoType *parent_type = mono_class_inflate_generic_type_checked (&gklass->parent->byval_arg, &mono_class_get_generic_class (klass)->context, error);
		if (mono_error_ok (error)) {
			MonoClass *parent = mono_class_from_mono_type (parent_type);
			mono_metadata_free_type (parent_type);
			if (parent != klass->parent) {
				/*fool mono_class_setup_parent*/
				klass->supertypes = NULL;
				mono_class_setup_parent (klass, parent);
			}
		} else {
			if (gklass->wastypebuilder)
				klass->wastypebuilder = TRUE;
			return FALSE;
		}
	}

	if (!mono_class_get_generic_class (klass)->need_sync)
		return TRUE;

	int mcount = mono_class_get_method_count (klass);
	int gmcount = mono_class_get_method_count (gklass);
	if (mcount != gmcount) {
		mono_class_set_method_count (klass, gmcount);
		klass->methods = (MonoMethod **)mono_image_alloc (klass->image, sizeof (MonoMethod*) * (gmcount + 1));

		for (i = 0; i < gmcount; i++) {
			klass->methods [i] = mono_class_inflate_generic_method_full_checked (
				gklass->methods [i], klass, mono_class_get_context (klass), error);
			mono_error_assert_ok (error);
		}
	}

	if (klass->interface_count && klass->interface_count != gklass->interface_count) {
		klass->interface_count = gklass->interface_count;
		klass->interfaces = (MonoClass **)mono_image_alloc (klass->image, sizeof (MonoClass*) * gklass->interface_count);
		klass->interfaces_packed = NULL; /*make setup_interface_offsets happy*/

		for (i = 0; i < gklass->interface_count; ++i) {
			MonoType *iface_type = mono_class_inflate_generic_type_checked (&gklass->interfaces [i]->byval_arg, mono_class_get_context (klass), error);
			return_val_if_nok (error, FALSE);

			klass->interfaces [i] = mono_class_from_mono_type (iface_type);
			mono_metadata_free_type (iface_type);

			if (!ensure_runtime_vtable (klass->interfaces [i], error))
				return FALSE;
		}
		klass->interfaces_inited = 1;
	}

	int fcount = mono_class_get_field_count (klass);
	int gfcount = mono_class_get_field_count (gklass);
	if (fcount != gfcount) {
		mono_class_set_field_count (klass, gfcount);
		klass->fields = image_g_new0 (klass->image, MonoClassField, gfcount);

		for (i = 0; i < gfcount; i++) {
			klass->fields [i] = gklass->fields [i];
			klass->fields [i].parent = klass;
			klass->fields [i].type = mono_class_inflate_generic_type_checked (gklass->fields [i].type, mono_class_get_context (klass), error);
			return_val_if_nok (error, FALSE);
		}
	}

	/*We can only finish with this klass once it's parent has as well*/
	if (gklass->wastypebuilder)
		klass->wastypebuilder = TRUE;
	return TRUE;
}

/**
 * ensure_generic_class_runtime_vtable:
 * @klass a generic class
 * @error set on error
 *
 * Ensures that the generic container of @klass has a vtable and
 * returns TRUE on success.  On error returns FALSE and sets @error.
 */
static gboolean
ensure_generic_class_runtime_vtable (MonoClass *klass, MonoError *error)
{
	MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

	error_init (error);

	if (!ensure_runtime_vtable (gklass, error))
		return FALSE;

	return fix_partial_generic_class (klass, error);
}

/**
 * ensure_runtime_vtable:
 * @klass the class
 * @error set on error
 *
 * Ensures that @klass has a vtable and returns TRUE on success. On
 * error returns FALSE and sets @error.
 */
static gboolean
ensure_runtime_vtable (MonoClass *klass, MonoError *error)
{
	MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mono_class_get_ref_info_raw (klass); /* FIXME use handles */
	int i, num, j;

	error_init (error);

	if (!image_is_dynamic (klass->image) || (!tb && !mono_class_is_ginst (klass)) || klass->wastypebuilder)
		return TRUE;
	if (klass->parent)
		if (!ensure_runtime_vtable (klass->parent, error))
			return FALSE;

	if (tb) {
		num = tb->ctors? mono_array_length (tb->ctors): 0;
		num += tb->num_methods;
		mono_class_set_method_count (klass, num);
		klass->methods = (MonoMethod **)mono_image_alloc (klass->image, sizeof (MonoMethod*) * num);
		num = tb->ctors? mono_array_length (tb->ctors): 0;
		for (i = 0; i < num; ++i) {
			MonoMethod *ctor = ctorbuilder_to_mono_method (klass, mono_array_get (tb->ctors, MonoReflectionCtorBuilder*, i), error);
			if (!ctor)
				return FALSE;
			klass->methods [i] = ctor;
		}
		num = tb->num_methods;
		j = i;
		for (i = 0; i < num; ++i) {
			MonoMethod *meth = methodbuilder_to_mono_method_raw (klass, mono_array_get (tb->methods, MonoReflectionMethodBuilder*, i), error); /* FIXME use handles */
			if (!meth)
				return FALSE;
			klass->methods [j++] = meth;
		}
	
		if (tb->interfaces) {
			klass->interface_count = mono_array_length (tb->interfaces);
			klass->interfaces = (MonoClass **)mono_image_alloc (klass->image, sizeof (MonoClass*) * klass->interface_count);
			for (i = 0; i < klass->interface_count; ++i) {
				MonoType *iface = mono_type_array_get_and_resolve_raw (tb->interfaces, i, error); /* FIXME use handles */
				return_val_if_nok (error, FALSE);
				klass->interfaces [i] = mono_class_from_mono_type (iface);
				if (!ensure_runtime_vtable (klass->interfaces [i], error))
					return FALSE;
			}
			klass->interfaces_inited = 1;
		}
	} else if (mono_class_is_ginst (klass)){
		if (!ensure_generic_class_runtime_vtable (klass, error)) {
			mono_class_set_type_load_failure (klass, "Could not initialize vtable for generic class due to: %s", mono_error_get_message (error));
			return FALSE;
		}
	}

	if (mono_class_is_interface (klass) && !mono_class_is_ginst (klass)) {
		int slot_num = 0;
		int mcount = mono_class_get_method_count (klass);
		for (i = 0; i < mcount; ++i) {
			MonoMethod *im = klass->methods [i];
			if (!(im->flags & METHOD_ATTRIBUTE_STATIC))
				im->slot = slot_num++;
		}
		
		klass->interfaces_packed = NULL; /*make setup_interface_offsets happy*/
		mono_class_setup_interface_offsets (klass);
		mono_class_setup_interface_id (klass);
	}

	/*
	 * The generic vtable is needed even if image->run is not set since some
	 * runtime code like ves_icall_Type_GetMethodsByName depends on 
	 * method->slot being defined.
	 */

	/* 
	 * tb->methods could not be freed since it is used for determining 
	 * overrides during dynamic vtable construction.
	 */

	return TRUE;
}

static MonoMethod*
mono_reflection_method_get_handle (MonoObject *method, MonoError *error)
{
	error_init (error);
	MonoClass *klass = mono_object_class (method);
	if (is_sr_mono_method (klass)) {
		MonoReflectionMethod *sr_method = (MonoReflectionMethod*)method;
		return sr_method->method;
	}
	if (is_sre_method_builder (klass)) {
		MonoReflectionMethodBuilder *mb = (MonoReflectionMethodBuilder*)method;
		return mb->mhandle;
	}
	if (mono_is_sre_method_on_tb_inst (klass)) {
		MonoClass *handle_class;

		MonoMethod *result =  mono_reflection_resolve_object (NULL, method, &handle_class, NULL, error);
		return_val_if_nok (error, NULL);

		return result;
	}

	g_error ("Can't handle methods of type %s:%s", klass->name_space, klass->name);
	return NULL;
}

void
mono_reflection_get_dynamic_overrides (MonoClass *klass, MonoMethod ***overrides, int *num_overrides, MonoError *error)
{
	MonoReflectionTypeBuilder *tb;
	int i, j, onum;
	MonoReflectionMethod *m;

	error_init (error);
	*overrides = NULL;
	*num_overrides = 0;

	g_assert (image_is_dynamic (klass->image));

	if (!mono_class_has_ref_info (klass))
		return;

	tb = (MonoReflectionTypeBuilder*)mono_class_get_ref_info_raw (klass); /* FIXME use handles */
	g_assert (strcmp (mono_object_class (tb)->name, "TypeBuilder") == 0);

	onum = 0;
	if (tb->methods) {
		for (i = 0; i < tb->num_methods; ++i) {
			MonoReflectionMethodBuilder *mb = 
				mono_array_get (tb->methods, MonoReflectionMethodBuilder*, i);
			if (mb->override_methods)
				onum += mono_array_length (mb->override_methods);
		}
	}

	if (onum) {
		*overrides = g_new0 (MonoMethod*, onum * 2);

		onum = 0;
		for (i = 0; i < tb->num_methods; ++i) {
			MonoReflectionMethodBuilder *mb = 
				mono_array_get (tb->methods, MonoReflectionMethodBuilder*, i);
			if (mb->override_methods) {
				for (j = 0; j < mono_array_length (mb->override_methods); ++j) {
					m = mono_array_get (mb->override_methods, MonoReflectionMethod*, j);

					(*overrides) [onum * 2] = mono_reflection_method_get_handle ((MonoObject*)m, error);
					return_if_nok (error);
					(*overrides) [onum * 2 + 1] = mb->mhandle;

					g_assert (mb->mhandle);

					onum ++;
				}
			}
		}
	}

	*num_overrides = onum;
}

/* This initializes the same data as mono_class_setup_fields () */
static void
typebuilder_setup_fields (MonoClass *klass, MonoError *error)
{
	MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mono_class_get_ref_info_raw (klass); /* FIXME use handles */
	MonoReflectionFieldBuilder *fb;
	MonoClassField *field;
	MonoFieldDefaultValue *def_values;
	MonoImage *image = klass->image;
	const char *p, *p2;
	int i, instance_size, packing_size = 0;
	guint32 len, idx;

	if (klass->parent) {
		if (!klass->parent->size_inited)
			mono_class_init (klass->parent);
		instance_size = klass->parent->instance_size;
	} else {
		instance_size = sizeof (MonoObject);
	}

	int fcount = tb->num_fields;
	mono_class_set_field_count (klass, fcount);

	error_init (error);

	if (tb->class_size) {
		packing_size = tb->packing_size;
		instance_size += tb->class_size;
	}
	
	klass->fields = image_g_new0 (image, MonoClassField, fcount);
	def_values = image_g_new0 (image, MonoFieldDefaultValue, fcount);
	mono_class_set_field_def_values (klass, def_values);
	/*
	This is, guess what, a hack.
	The issue is that the runtime doesn't know how to setup the fields of a typebuider and crash.
	On the static path no field class is resolved, only types are built. This is the right thing to do
	but we suck.
	Setting size_inited is harmless because we're doing the same job as mono_class_setup_fields anyway.
	*/
	klass->size_inited = 1;

	for (i = 0; i < fcount; ++i) {
		MonoArray *rva_data;
		fb = (MonoReflectionFieldBuilder *)mono_array_get (tb->fields, gpointer, i);
		field = &klass->fields [i];
		field->parent = klass;
		field->name = string_to_utf8_image_raw (image, fb->name, error); /* FIXME use handles */
		if (!mono_error_ok (error))
			return;
		if (fb->attrs) {
			MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type, error);
			return_if_nok (error);
			field->type = mono_metadata_type_dup (klass->image, type);
			field->type->attrs = fb->attrs;
		} else {
			field->type = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type, error);
			return_if_nok (error);
		}

		if (!klass->enumtype && !mono_type_get_underlying_type (field->type)) {
			mono_class_set_type_load_failure (klass, "Field '%s' is an enum type with a bad underlying type", field->name);
			continue;
		}

		if ((fb->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA) && (rva_data = fb->rva_data)) {
			char *base = mono_array_addr (rva_data, char, 0);
			size_t size = mono_array_length (rva_data);
			char *data = (char *)mono_image_alloc (klass->image, size);
			memcpy (data, base, size);
			def_values [i].data = data;
		}
		if (fb->offset != -1)
			field->offset = fb->offset;
		fb->handle = field;
		mono_save_custom_attrs (klass->image, field, fb->cattrs);

		if (fb->def_value) {
			MonoDynamicImage *assembly = (MonoDynamicImage*)klass->image;
			field->type->attrs |= FIELD_ATTRIBUTE_HAS_DEFAULT;
			idx = mono_dynimage_encode_constant (assembly, fb->def_value, &def_values [i].def_type);
			/* Copy the data from the blob since it might get realloc-ed */
			p = assembly->blob.data + idx;
			len = mono_metadata_decode_blob_size (p, &p2);
			len += p2 - p;
			def_values [i].data = (const char *)mono_image_alloc (image, len);
			memcpy ((gpointer)def_values [i].data, p, len);
		}
	}

	if (!mono_class_has_failure (klass)) {
		mono_class_layout_fields (klass, instance_size, packing_size, TRUE);
	}
}

static void
typebuilder_setup_properties (MonoClass *klass, MonoError *error)
{
	MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mono_class_get_ref_info_raw (klass); /* FIXME use handles */
	MonoReflectionPropertyBuilder *pb;
	MonoImage *image = klass->image;
	MonoProperty *properties;
	MonoClassPropertyInfo *info;
	int i;

	error_init (error);

	info = mono_class_get_property_info (klass);
	if (!info) {
		info = mono_class_alloc0 (klass, sizeof (MonoClassPropertyInfo));
		mono_class_set_property_info (klass, info);
	}

	info->count = tb->properties ? mono_array_length (tb->properties) : 0;
	info->first = 0;

	properties = image_g_new0 (image, MonoProperty, info->count);
	info->properties = properties;
	for (i = 0; i < info->count; ++i) {
		pb = mono_array_get (tb->properties, MonoReflectionPropertyBuilder*, i);
		properties [i].parent = klass;
		properties [i].attrs = pb->attrs;
		properties [i].name = string_to_utf8_image_raw (image, pb->name, error); /* FIXME use handles */
		if (!mono_error_ok (error))
			return;
		if (pb->get_method)
			properties [i].get = pb->get_method->mhandle;
		if (pb->set_method)
			properties [i].set = pb->set_method->mhandle;

		mono_save_custom_attrs (klass->image, &properties [i], pb->cattrs);
		if (pb->def_value) {
			guint32 len, idx;
			const char *p, *p2;
			MonoDynamicImage *assembly = (MonoDynamicImage*)klass->image;
			if (!info->def_values)
				info->def_values = image_g_new0 (image, MonoFieldDefaultValue, info->count);
			properties [i].attrs |= PROPERTY_ATTRIBUTE_HAS_DEFAULT;
			idx = mono_dynimage_encode_constant (assembly, pb->def_value, &info->def_values [i].def_type);
			/* Copy the data from the blob since it might get realloc-ed */
			p = assembly->blob.data + idx;
			len = mono_metadata_decode_blob_size (p, &p2);
			len += p2 - p;
			info->def_values [i].data = (const char *)mono_image_alloc (image, len);
			memcpy ((gpointer)info->def_values [i].data, p, len);
		}
	}
}

static void
typebuilder_setup_events (MonoClass *klass, MonoError *error)
{
	MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mono_class_get_ref_info_raw (klass); /* FIXME use handles */
	MonoReflectionEventBuilder *eb;
	MonoImage *image = klass->image;
	MonoEvent *events;
	MonoClassEventInfo *info;
	int i;

	error_init (error);

	info = mono_class_alloc0 (klass, sizeof (MonoClassEventInfo));
	mono_class_set_event_info (klass, info);

	info->count = tb->events ? mono_array_length (tb->events) : 0;
	info->first = 0;

	events = image_g_new0 (image, MonoEvent, info->count);
	info->events = events;
	for (i = 0; i < info->count; ++i) {
		eb = mono_array_get (tb->events, MonoReflectionEventBuilder*, i);
		events [i].parent = klass;
		events [i].attrs = eb->attrs;
		events [i].name = string_to_utf8_image_raw (image, eb->name, error); /* FIXME use handles */
		if (!mono_error_ok (error))
			return;
		if (eb->add_method)
			events [i].add = eb->add_method->mhandle;
		if (eb->remove_method)
			events [i].remove = eb->remove_method->mhandle;
		if (eb->raise_method)
			events [i].raise = eb->raise_method->mhandle;

#ifndef MONO_SMALL_CONFIG
		if (eb->other_methods) {
			int j;
			events [i].other = image_g_new0 (image, MonoMethod*, mono_array_length (eb->other_methods) + 1);
			for (j = 0; j < mono_array_length (eb->other_methods); ++j) {
				MonoReflectionMethodBuilder *mb = 
					mono_array_get (eb->other_methods,
									MonoReflectionMethodBuilder*, j);
				events [i].other [j] = mb->mhandle;
			}
		}
#endif
		mono_save_custom_attrs (klass->image, &events [i], eb->cattrs);
	}
}

struct remove_instantiations_user_data
{
	MonoClass *klass;
	MonoError *error;
};

static gboolean
remove_instantiations_of_and_ensure_contents (gpointer key,
						  gpointer value,
						  gpointer user_data)
{
	struct remove_instantiations_user_data *data = (struct remove_instantiations_user_data*)user_data;
	MonoType *type = (MonoType*)key;
	MonoClass *klass = data->klass;
	gboolean already_failed = !is_ok (data->error);
	MonoError lerror;
	MonoError *error = already_failed ? &lerror : data->error;

	if ((type->type == MONO_TYPE_GENERICINST) && (type->data.generic_class->container_class == klass)) {
		MonoClass *inst_klass = mono_class_from_mono_type (type);
		//Ensure it's safe to use it.
		if (!fix_partial_generic_class (inst_klass, error)) {
			mono_class_set_type_load_failure (inst_klass, "Could not initialized generic type instance due to: %s", mono_error_get_message (error));
			// Marked the class with failure, but since some other instantiation already failed,
			// just report that one, and swallow the error from this one.
			if (already_failed)
				mono_error_cleanup (error);
		}
		return TRUE;
	} else
		return FALSE;
}

/**
 * reflection_setup_internal_class:
 * @tb: a TypeBuilder object
 * @error: set on error
 *
 * Creates a MonoClass that represents the TypeBuilder.
 * This is a trick that lets us simplify a lot of reflection code
 * (and will allow us to support Build and Run assemblies easier).
 *
 * Returns TRUE on success. On failure, returns FALSE and sets @error.
 */
static gboolean
reflection_setup_internal_class (MonoReflectionTypeBuilderHandle ref_tb, MonoError *error)
{
	MonoReflectionModuleBuilderHandle module_ref = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, ref_tb, module);
	GHashTable *unparented_classes = MONO_HANDLE_GETVAL(module_ref, unparented_classes);

	if (unparented_classes) {
		return reflection_setup_internal_class_internal (ref_tb, error);
	} else {
		// If we're not being called recursively
		unparented_classes = g_hash_table_new (NULL, NULL);
		MONO_HANDLE_SETVAL (module_ref, unparented_classes, GHashTable *, unparented_classes);

		gboolean ret_val = reflection_setup_internal_class_internal (ref_tb, error);
		mono_error_assert_ok (error);

		// Fix the relationship between the created classes and their parents
		reflection_setup_class_hierarchy (unparented_classes, error);
		mono_error_assert_ok (error);

		g_hash_table_destroy (unparented_classes);
		MONO_HANDLE_SETVAL (module_ref, unparented_classes, GHashTable *, NULL);

		return ret_val;
	}
}


MonoReflectionTypeHandle
ves_icall_TypeBuilder_create_runtime_class (MonoReflectionTypeBuilderHandle ref_tb, MonoError *error)
{
	error_init (error);

	reflection_setup_internal_class (ref_tb, error);
	mono_error_assert_ok (error);

	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_tb);
	MonoType *type = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionType, ref_tb), type);
	MonoClass *klass = mono_class_from_mono_type (type);

	MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, ref_tb, cattrs);
	mono_save_custom_attrs (klass->image, klass, MONO_HANDLE_RAW (cattrs)); /* FIXME use handles */

	/* 
	 * we need to lock the domain because the lock will be taken inside
	 * So, we need to keep the locking order correct.
	 */
	mono_loader_lock ();
	mono_domain_lock (domain);
	if (klass->wastypebuilder) {
		mono_domain_unlock (domain);
		mono_loader_unlock ();

		return mono_type_get_object_handle (domain, &klass->byval_arg, error);
	}
	/*
	 * Fields to set in klass:
	 * the various flags: delegate/unicode/contextbound etc.
	 */
	mono_class_set_flags (klass, MONO_HANDLE_GETVAL (ref_tb, attrs));
	klass->has_cctor = 1;

	mono_class_setup_parent (klass, klass->parent);
	/* fool mono_class_setup_supertypes */
	klass->supertypes = NULL;
	mono_class_setup_supertypes (klass);
	mono_class_setup_mono_type (klass);

	/* enums are done right away */
	if (!klass->enumtype)
		if (!ensure_runtime_vtable (klass, error))
			goto failure;

	MonoArrayHandle nested_types = MONO_HANDLE_NEW_GET (MonoArray, ref_tb, subtypes);
	if (!MONO_HANDLE_IS_NULL (nested_types)) {
		GList *nested = NULL;
		int num_nested = mono_array_handle_length (nested_types);
		MonoReflectionTypeHandle nested_tb = MONO_HANDLE_NEW (MonoReflectionType, NULL);
		for (int i = 0; i < num_nested; ++i) {
			MONO_HANDLE_ARRAY_GETREF (nested_tb, nested_types, i);

			if (MONO_HANDLE_GETVAL (nested_tb, type) == NULL) {
				reflection_setup_internal_class (MONO_HANDLE_CAST (MonoReflectionTypeBuilder, nested_tb), error);
				mono_error_assert_ok (error);
			}

			MonoType *subtype = mono_reflection_type_handle_mono_type (nested_tb, error);
			goto_if_nok (error, failure);
			nested = g_list_prepend_image (klass->image, nested, mono_class_from_mono_type (subtype));
		}
		mono_class_set_nested_classes_property (klass, nested);
	}

	klass->nested_classes_inited = TRUE;

	typebuilder_setup_fields (klass, error);
	goto_if_nok (error, failure);
	typebuilder_setup_properties (klass, error);
	goto_if_nok (error, failure);

	typebuilder_setup_events (klass, error);
	goto_if_nok (error, failure);

	klass->wastypebuilder = TRUE;

	MonoArrayHandle generic_params = MONO_HANDLE_NEW_GET (MonoArray, ref_tb, generic_params);
	if (!MONO_HANDLE_IS_NULL (generic_params)) {
		int num_params = mono_array_handle_length (generic_params);
		MonoReflectionTypeHandle ref_gparam = MONO_HANDLE_NEW (MonoReflectionType, NULL);
		for (int i = 0; i < num_params; i++) {
			MONO_HANDLE_ARRAY_GETREF (ref_gparam, generic_params, i);
			MonoType *param_type = mono_reflection_type_handle_mono_type (ref_gparam, error);
			goto_if_nok (error, failure);
			MonoClass *gklass = mono_class_from_mono_type (param_type);

			gklass->wastypebuilder = TRUE;
		}
	}

	/* 
	 * If we are a generic TypeBuilder, there might be instantiations in the type cache
	 * which have type System.Reflection.MonoGenericClass, but after the type is created, 
	 * we want to return normal System.MonoType objects, so clear these out from the cache.
	 *
	 * Together with this we must ensure the contents of all instances to match the created type.
	 */
	if (domain->type_hash && mono_class_is_gtd (klass)) {
		struct remove_instantiations_user_data data;
		data.klass = klass;
		data.error = error;
		mono_error_assert_ok (error);
		mono_g_hash_table_foreach_remove (domain->type_hash, remove_instantiations_of_and_ensure_contents, &data);
		goto_if_nok (error, failure);
	}

	mono_domain_unlock (domain);
	mono_loader_unlock ();

	if (klass->enumtype && !mono_class_is_valid_enum (klass)) {
		mono_class_set_type_load_failure (klass, "Not a valid enumeration");
		mono_error_set_type_load_class (error, klass, "Not a valid enumeration");
		goto failure_unlocked;
	}

	MonoReflectionTypeHandle res = mono_type_get_object_handle (domain, &klass->byval_arg, error);
	goto_if_nok (error, failure_unlocked);

	return res;

failure:
	mono_class_set_type_load_failure (klass, "TypeBuilder could not create runtime class due to: %s", mono_error_get_message (error));
	klass->wastypebuilder = TRUE;
	mono_domain_unlock (domain);
	mono_loader_unlock ();
failure_unlocked:
	return NULL;
}

typedef struct {
	MonoMethod *handle;
	MonoDomain *domain;
} DynamicMethodReleaseData;

/*
 * The runtime automatically clean up those after finalization.
*/	
static MonoReferenceQueue *dynamic_method_queue;

static void
free_dynamic_method (void *dynamic_method)
{
	DynamicMethodReleaseData *data = (DynamicMethodReleaseData *)dynamic_method;
	MonoDomain *domain = data->domain;
	MonoMethod *method = data->handle;
	guint32 dis_link;

	mono_domain_lock (domain);
	dis_link = (guint32)(size_t)g_hash_table_lookup (domain->method_to_dyn_method, method);
	g_hash_table_remove (domain->method_to_dyn_method, method);
	mono_domain_unlock (domain);
	g_assert (dis_link);
	mono_gchandle_free (dis_link);

	mono_runtime_free_method (domain, method);
	g_free (data);
}

static gboolean
reflection_create_dynamic_method (MonoReflectionDynamicMethodHandle ref_mb, MonoError *error)
{
	MonoReferenceQueue *queue;
	MonoMethod *handle;
	DynamicMethodReleaseData *release_data;
	ReflectionMethodBuilder rmb;
	MonoMethodSignature *sig;
	MonoClass *klass;
	MonoDomain *domain;
	GSList *l;
	int i;

	error_init (error);

	if (mono_runtime_is_shutting_down ()) {
		mono_error_set_generic_error (error, "System", "InvalidOperationException", "");
		return FALSE;
	}

	if (!(queue = dynamic_method_queue)) {
		mono_loader_lock ();
		if (!(queue = dynamic_method_queue))
			queue = dynamic_method_queue = mono_gc_reference_queue_new (free_dynamic_method);
		mono_loader_unlock ();
	}

	sig = dynamic_method_to_signature (ref_mb, error);
	return_val_if_nok (error, FALSE);

	MonoReflectionDynamicMethod *mb = MONO_HANDLE_RAW (ref_mb); /* FIXME convert reflection_create_dynamic_method to use handles */
	reflection_methodbuilder_from_dynamic_method (&rmb, mb);

	/*
	 * Resolve references.
	 */
	/* 
	 * Every second entry in the refs array is reserved for storing handle_class,
	 * which is needed by the ldtoken implementation in the JIT.
	 */
	rmb.nrefs = mb->nrefs;
	rmb.refs = g_new0 (gpointer, mb->nrefs + 1);
	for (i = 0; i < mb->nrefs; i += 2) {
		MonoClass *handle_class;
		gpointer ref;
		MonoObject *obj = mono_array_get (mb->refs, MonoObject*, i);

		if (strcmp (obj->vtable->klass->name, "DynamicMethod") == 0) {
			MonoReflectionDynamicMethod *method = (MonoReflectionDynamicMethod*)obj;
			/*
			 * The referenced DynamicMethod should already be created by the managed
			 * code, except in the case of circular references. In that case, we store
			 * method in the refs array, and fix it up later when the referenced 
			 * DynamicMethod is created.
			 */
			if (method->mhandle) {
				ref = method->mhandle;
			} else {
				/* FIXME: GC object stored in unmanaged memory */
				ref = method;

				/* FIXME: GC object stored in unmanaged memory */
				method->referenced_by = g_slist_append (method->referenced_by, mb);
			}
			handle_class = mono_defaults.methodhandle_class;
		} else {
			MonoException *ex = NULL;
			ref = mono_reflection_resolve_object (mb->module->image, obj, &handle_class, NULL, error);
			if (!is_ok  (error)) {
				g_free (rmb.refs);
				return FALSE;
			}
			if (!ref)
				ex = mono_get_exception_type_load (NULL, NULL);
			else if (mono_security_core_clr_enabled ())
				ex = mono_security_core_clr_ensure_dynamic_method_resolved_object (ref, handle_class);

			if (ex) {
				g_free (rmb.refs);
				mono_error_set_exception_instance (error, ex);
				return FALSE;
			}
		}

		rmb.refs [i] = ref; /* FIXME: GC object stored in unmanaged memory (change also resolve_object() signature) */
		rmb.refs [i + 1] = handle_class;
	}		

	MonoAssembly *ass = NULL;
	if (mb->owner) {
		MonoType *owner_type = mono_reflection_type_get_handle ((MonoReflectionType*)mb->owner, error);
		if (!is_ok (error)) {
			g_free (rmb.refs);
			return FALSE;
		}
		klass = mono_class_from_mono_type (owner_type);
		ass = klass->image->assembly;
	} else {
		klass = mono_defaults.object_class;
		ass = (mb->module && mb->module->image) ? mb->module->image->assembly : NULL;
	}

	mb->mhandle = handle = reflection_methodbuilder_to_mono_method (klass, &rmb, sig, error);
	((MonoDynamicMethod*)handle)->assembly = ass;
	g_free (rmb.refs);
	return_val_if_nok (error, FALSE);

	release_data = g_new (DynamicMethodReleaseData, 1);
	release_data->handle = handle;
	release_data->domain = mono_object_get_domain ((MonoObject*)mb);
	if (!mono_gc_reference_queue_add (queue, (MonoObject*)mb, release_data))
		g_free (release_data);

	/* Fix up refs entries pointing at us */
	for (l = mb->referenced_by; l; l = l->next) {
		MonoReflectionDynamicMethod *method = (MonoReflectionDynamicMethod*)l->data;
		MonoMethodWrapper *wrapper = (MonoMethodWrapper*)method->mhandle;
		gpointer *data;
		
		g_assert (method->mhandle);

		data = (gpointer*)wrapper->method_data;
		for (i = 0; i < GPOINTER_TO_UINT (data [0]); i += 2) {
			if ((data [i + 1] == mb) && (data [i + 1 + 1] == mono_defaults.methodhandle_class))
				data [i + 1] = mb->mhandle;
		}
	}
	g_slist_free (mb->referenced_by);

	/* ilgen is no longer needed */
	mb->ilgen = NULL;

	domain = mono_domain_get ();
	mono_domain_lock (domain);
	if (!domain->method_to_dyn_method)
		domain->method_to_dyn_method = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (domain->method_to_dyn_method, handle, (gpointer)(size_t)mono_gchandle_new_weakref ((MonoObject *)mb, TRUE));
	mono_domain_unlock (domain);

	return TRUE;
}

void
ves_icall_DynamicMethod_create_dynamic_method (MonoReflectionDynamicMethodHandle mb, MonoError *error)
{
	(void) reflection_create_dynamic_method (mb, error);
}

#endif /* DISABLE_REFLECTION_EMIT */

MonoMethodSignature *
mono_reflection_lookup_signature (MonoImage *image, MonoMethod *method, guint32 token, MonoError *error)
{
	MonoMethodSignature *sig;
	g_assert (image_is_dynamic (image));

	error_init (error);

	sig = (MonoMethodSignature *)g_hash_table_lookup (((MonoDynamicImage*)image)->vararg_aux_hash, GUINT_TO_POINTER (token));
	if (sig)
		return sig;

	return mono_method_signature_checked (method, error);
}

#ifndef DISABLE_REFLECTION_EMIT

/*
 * ensure_complete_type:
 *
 *   Ensure that KLASS is completed if it is a dynamic type, or references
 * dynamic types.
 */
static void
ensure_complete_type (MonoClass *klass, MonoError *error)
{
	error_init (error);

	if (image_is_dynamic (klass->image) && !klass->wastypebuilder && mono_class_has_ref_info (klass)) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mono_class_get_ref_info_raw (klass); /* FIXME use handles */

		mono_domain_try_type_resolve_checked (mono_domain_get (), NULL, (MonoObject*)tb, error);
		return_if_nok (error);

		// Asserting here could break a lot of code
		//g_assert (klass->wastypebuilder);
	}

	if (mono_class_is_ginst (klass)) {
		MonoGenericInst *inst = mono_class_get_generic_class (klass)->context.class_inst;
		int i;

		for (i = 0; i < inst->type_argc; ++i) {
			ensure_complete_type (mono_class_from_mono_type (inst->type_argv [i]), error);
			return_if_nok (error);
		}
	}
}

gpointer
mono_reflection_resolve_object (MonoImage *image, MonoObject *obj, MonoClass **handle_class, MonoGenericContext *context, MonoError *error)
{
	MonoClass *oklass = obj->vtable->klass;
	gpointer result = NULL;

	error_init (error);

	if (strcmp (oklass->name, "String") == 0) {
		result = mono_string_intern_checked ((MonoString*)obj, error);
		return_val_if_nok (error, NULL);
		*handle_class = mono_defaults.string_class;
		g_assert (result);
	} else if (strcmp (oklass->name, "RuntimeType") == 0) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)obj, error);
		return_val_if_nok (error, NULL);
		MonoClass *mc = mono_class_from_mono_type (type);
		if (!mono_class_init (mc)) {
			mono_error_set_for_class_failure (error, mc);
			return NULL;
		}

		if (context) {
			MonoType *inflated = mono_class_inflate_generic_type_checked (type, context, error);
			return_val_if_nok (error, NULL);

			result = mono_class_from_mono_type (inflated);
			mono_metadata_free_type (inflated);
		} else {
			result = mono_class_from_mono_type (type);
		}
		*handle_class = mono_defaults.typehandle_class;
		g_assert (result);
	} else if (strcmp (oklass->name, "MonoMethod") == 0 ||
			   strcmp (oklass->name, "MonoCMethod") == 0) {
		result = ((MonoReflectionMethod*)obj)->method;
		if (context) {
			result = mono_class_inflate_generic_method_checked ((MonoMethod *)result, context, error);
			mono_error_assert_ok (error);
		}
		*handle_class = mono_defaults.methodhandle_class;
		g_assert (result);
	} else if (strcmp (oklass->name, "MonoField") == 0) {
		MonoClassField *field = ((MonoReflectionField*)obj)->field;

		ensure_complete_type (field->parent, error);
		return_val_if_nok (error, NULL);

		if (context) {
			MonoType *inflated = mono_class_inflate_generic_type_checked (&field->parent->byval_arg, context, error);
			return_val_if_nok (error, NULL);

			MonoClass *klass = mono_class_from_mono_type (inflated);
			MonoClassField *inflated_field;
			gpointer iter = NULL;
			mono_metadata_free_type (inflated);
			while ((inflated_field = mono_class_get_fields (klass, &iter))) {
				if (!strcmp (field->name, inflated_field->name))
					break;
			}
			g_assert (inflated_field && !strcmp (field->name, inflated_field->name));
			result = inflated_field;
		} else {
			result = field;
		}
		*handle_class = mono_defaults.fieldhandle_class;
		g_assert (result);
	} else if (strcmp (oklass->name, "TypeBuilder") == 0) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder*)obj;
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)tb, error);
		return_val_if_nok (error, NULL);
		MonoClass *klass;

		klass = type->data.klass;
		if (klass->wastypebuilder) {
			/* Already created */
			result = klass;
		}
		else {
			mono_domain_try_type_resolve_checked (mono_domain_get (), NULL, (MonoObject*)tb, error);
			return_val_if_nok (error, NULL);
			result = type->data.klass;
			g_assert (result);
		}
		*handle_class = mono_defaults.typehandle_class;
	} else if (strcmp (oklass->name, "SignatureHelper") == 0) {
		MonoReflectionSigHelper *helper = (MonoReflectionSigHelper*)obj;
		MonoMethodSignature *sig;
		int nargs, i;

		if (helper->arguments)
			nargs = mono_array_length (helper->arguments);
		else
			nargs = 0;

		sig = mono_metadata_signature_alloc (image, nargs);
		sig->explicit_this = helper->call_conv & 64 ? 1 : 0;
		sig->hasthis = helper->call_conv & 32 ? 1 : 0;

		if (helper->unmanaged_call_conv) { /* unmanaged */
			sig->call_convention = helper->unmanaged_call_conv - 1;
			sig->pinvoke = TRUE;
		} else if (helper->call_conv & 0x02) {
			sig->call_convention = MONO_CALL_VARARG;
		} else {
			sig->call_convention = MONO_CALL_DEFAULT;
		}

		sig->param_count = nargs;
		/* TODO: Copy type ? */
		sig->ret = helper->return_type->type;
		for (i = 0; i < nargs; ++i) {
			sig->params [i] = mono_type_array_get_and_resolve_raw (helper->arguments, i, error); /* FIXME use handles */
			if (!is_ok (error)) {
				image_g_free (image, sig);
				return NULL;
			}
		}

		result = sig;
		*handle_class = NULL;
	} else if (strcmp (oklass->name, "DynamicMethod") == 0) {
		MonoReflectionDynamicMethod *method = (MonoReflectionDynamicMethod*)obj;
		/* Already created by the managed code */
		g_assert (method->mhandle);
		result = method->mhandle;
		*handle_class = mono_defaults.methodhandle_class;
	} else if (strcmp (oklass->name, "MonoArrayMethod") == 0) {
		MonoReflectionArrayMethod *m = (MonoReflectionArrayMethod*)obj;
		MonoType *mtype;
		MonoClass *klass;
		MonoMethod *method;
		gpointer iter;
		char *name;

		mtype = mono_reflection_type_get_handle (m->parent, error);
		return_val_if_nok (error, NULL);
		klass = mono_class_from_mono_type (mtype);

		/* Find the method */

		name = mono_string_to_utf8_checked (m->name, error);
		return_val_if_nok (error, NULL);
		iter = NULL;
		while ((method = mono_class_get_methods (klass, &iter))) {
			if (!strcmp (method->name, name))
				break;
		}
		g_free (name);

		// FIXME:
		g_assert (method);
		// FIXME: Check parameters/return value etc. match

		result = method;
		*handle_class = mono_defaults.methodhandle_class;
	} else if (is_sre_method_builder (oklass) ||
			   mono_is_sre_ctor_builder (oklass) ||
			   is_sre_field_builder (oklass) ||
			   is_sre_gparam_builder (oklass) ||
			   is_sre_generic_instance (oklass) ||
			   is_sre_array (oklass) ||
			   is_sre_byref (oklass) ||
			   is_sre_pointer (oklass) ||
			   !strcmp (oklass->name, "FieldOnTypeBuilderInst") ||
			   !strcmp (oklass->name, "MethodOnTypeBuilderInst") ||
			   !strcmp (oklass->name, "ConstructorOnTypeBuilderInst")) {
		static MonoMethod *resolve_method;
		if (!resolve_method) {
			MonoMethod *m = mono_class_get_method_from_name_flags (mono_class_get_module_builder_class (), "RuntimeResolve", 1, 0);
			g_assert (m);
			mono_memory_barrier ();
			resolve_method = m;
		}
		void *args [16];
		args [0] = obj;
		obj = mono_runtime_invoke_checked (resolve_method, NULL, args, error);
		mono_error_assert_ok (error);
		g_assert (obj);
		return mono_reflection_resolve_object (image, obj, handle_class, context, error);
	} else {
		g_print ("%s\n", obj->vtable->klass->name);
		g_assert_not_reached ();
	}
	return result;
}

#else /* DISABLE_REFLECTION_EMIT */

MonoArray*
mono_reflection_get_custom_attrs_blob (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *propValues, MonoArray *fields, MonoArray* fieldValues) 
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_reflection_dynimage_basic_init (MonoReflectionAssemblyBuilder *assemblyb)
{
	g_error ("This mono runtime was configured with --enable-minimal=reflection_emit, so System.Reflection.Emit is not supported.");
}

static gboolean
mono_image_module_basic_init (MonoReflectionModuleBuilderHandle moduleb, MonoError *error)
{
	g_assert_not_reached ();
	return FALSE;
}

guint32
mono_image_insert_string (MonoReflectionModuleBuilderHandle module, MonoStringHandle str, MonoError *error)
{
	g_assert_not_reached ();
	return 0;
}

guint32
mono_image_create_method_token (MonoDynamicImage *assembly, MonoObjectHandle obj, MonoArrayHandle opt_param_types, MonoError *error)
{
	g_assert_not_reached ();
	return 0;
}

guint32
mono_image_create_token (MonoDynamicImage *assembly, MonoObjectHandle obj, 
			 gboolean create_open_instance, gboolean register_token, MonoError *error)
{
	g_assert_not_reached ();
	return 0;
}

void
mono_reflection_get_dynamic_overrides (MonoClass *klass, MonoMethod ***overrides, int *num_overrides, MonoError *error)
{
	error_init (error);
	*overrides = NULL;
	*num_overrides = 0;
}

MonoReflectionTypeHandle
ves_icall_TypeBuilder_create_runtime_class (MonoReflectionTypeBuilderHandle tb, MonoError *error)
{
	g_assert_not_reached ();
	return NULL;
}

void 
ves_icall_DynamicMethod_create_dynamic_method (MonoReflectionDynamicMethodHandle mb, MonoError *error)
{
	error_init (error);
}

MonoType*
mono_reflection_type_get_handle (MonoReflectionType* ref, MonoError *error)
{
	error_init (error);
	if (!ref)
		return NULL;
	return ref->type;
}

MonoType*
mono_reflection_type_handle_mono_type (MonoReflectionTypeHandle ref, MonoError *error)
{
	error_init (error);
	if (MONO_HANDLE_IS_NULL (ref))
		return NULL;
	return MONO_HANDLE_GETVAL (ref, type);
}


#endif /* DISABLE_REFLECTION_EMIT */

void
mono_sre_generic_param_table_entry_free (GenericParamTableEntry *entry)
{
	MONO_GC_UNREGISTER_ROOT_IF_MOVING (entry->gparam);
	g_free (entry);
}

gint32
ves_icall_ModuleBuilder_getToken (MonoReflectionModuleBuilderHandle mb, MonoObjectHandle obj, gboolean create_open_instance, MonoError *error)
{
	error_init (error);
	if (MONO_HANDLE_IS_NULL (obj)) {
		mono_error_set_argument_null (error, "obj", "");
		return 0;
	}
	return mono_image_create_token (MONO_HANDLE_GETVAL (mb, dynamic_image), obj, create_open_instance, TRUE, error);
}

gint32
ves_icall_ModuleBuilder_getMethodToken (MonoReflectionModuleBuilderHandle mb,
					MonoReflectionMethodHandle method,
					MonoArrayHandle opt_param_types,
					MonoError *error)
{
	error_init (error);
	if (MONO_HANDLE_IS_NULL (method)) {
		mono_error_set_argument_null (error, "method", "");
		return 0;
	}

	return mono_image_create_method_token (MONO_HANDLE_GETVAL (mb, dynamic_image), MONO_HANDLE_CAST (MonoObject, method), opt_param_types, error);
}

void
ves_icall_ModuleBuilder_WriteToFile (MonoReflectionModuleBuilder *mb, HANDLE file)
{
	MonoError error;
	mono_image_create_pefile (mb, file, &error);
	mono_error_set_pending_exception (&error);
}

void
ves_icall_ModuleBuilder_build_metadata (MonoReflectionModuleBuilder *mb)
{
	MonoError error;
	mono_image_build_metadata (mb, &error);
	mono_error_set_pending_exception (&error);
}

void
ves_icall_ModuleBuilder_RegisterToken (MonoReflectionModuleBuilderHandle mb, MonoObjectHandle obj, guint32 token, MonoError *error)
{
	error_init (error);
	/* This function may be called by ModuleBuilder.FixupTokens to update
	 * an existing token, so replace is okay here. */
	mono_dynamic_image_register_token (MONO_HANDLE_GETVAL (mb, dynamic_image), token, obj, MONO_DYN_IMAGE_TOK_REPLACE);
}

MonoObjectHandle
ves_icall_ModuleBuilder_GetRegisteredToken (MonoReflectionModuleBuilderHandle mb, guint32 token, MonoError *error)
{
	error_init (error);
	MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (mb, dynamic_image);
	return mono_dynamic_image_get_registered_token (dynamic_image, token, error);
}

#ifndef DISABLE_REFLECTION_EMIT
MonoArray*
ves_icall_CustomAttributeBuilder_GetBlob (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *propValues, MonoArray *fields, MonoArray* fieldValues)
{
	MonoError error;
	MonoArray *result = mono_reflection_get_custom_attrs_blob_checked (assembly, ctor, ctorArgs, properties, propValues, fields, fieldValues, &error);
	mono_error_set_pending_exception (&error);
	return result;
}
#endif

void
ves_icall_AssemblyBuilder_basic_init (MonoReflectionAssemblyBuilder *assemblyb)
{
	mono_reflection_dynimage_basic_init (assemblyb);
}

void
ves_icall_AssemblyBuilder_UpdateNativeCustomAttributes (MonoReflectionAssemblyBuilderHandle assemblyb, MonoError *error)
{
	MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, assemblyb, cattrs);

	MonoReflectionAssemblyHandle assembly_handle = MONO_HANDLE_CAST (MonoReflectionAssembly, assemblyb);
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_handle, assembly);
	g_assert (assembly);

	mono_save_custom_attrs (assembly->image, assembly, MONO_HANDLE_RAW (cattrs));
}

void
ves_icall_EnumBuilder_setup_enum_type (MonoReflectionTypeHandle enumtype,
				       MonoReflectionTypeHandle t,
				       MonoError *error)
{
	error_init (error);
	MONO_HANDLE_SETVAL (enumtype, type, MonoType*, MONO_HANDLE_GETVAL (t, type));
}

void
ves_icall_ModuleBuilder_basic_init (MonoReflectionModuleBuilderHandle moduleb, MonoError *error)
{
	error_init (error);
	mono_image_module_basic_init (moduleb, error);
}

guint32
ves_icall_ModuleBuilder_getUSIndex (MonoReflectionModuleBuilderHandle module, MonoStringHandle str, MonoError *error)
{
	return mono_image_insert_string (module, str, error);
}

void
ves_icall_ModuleBuilder_set_wrappers_type (MonoReflectionModuleBuilderHandle moduleb, MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoDynamicImage *image = MONO_HANDLE_GETVAL (moduleb, dynamic_image);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	g_assert (type);
	image->wrappers_type = mono_class_from_mono_type (type);
}
