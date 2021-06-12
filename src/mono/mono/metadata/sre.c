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
#include "mono/metadata/assembly-internals.h"
#include "mono/metadata/class-init.h"
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
#include "mono/metadata/abi-details.h"
#include "mono/utils/checked-build.h"
#include "mono/utils/mono-digest.h"
#include "mono/utils/w32api.h"
#ifdef MONO_CLASS_DEF_PRIVATE
/* Rationale: Some of the code here does MonoClass construction.
 * FIXME: Move SRE class construction to class-init.c and unify with ordinary class construction.
 */
#define REALLY_INCLUDE_CLASS_DEF 1
#include <mono/metadata/class-private-definition.h>
#undef REALLY_INCLUDE_CLASS_DEF
#endif
#include "icall-decl.h"

/* Maps MonoMethod* to weak links to DynamicMethod objects */
static GHashTable *method_to_dyn_method;
static mono_mutex_t method_to_dyn_method_lock;

static inline void
dyn_methods_lock (void)
{
	mono_os_mutex_lock (&method_to_dyn_method_lock);
}

static inline void
dyn_methods_unlock (void)
{
	mono_os_mutex_unlock (&method_to_dyn_method_lock);
}

static GENERATE_GET_CLASS_WITH_CACHE (marshal_as_attribute, "System.Runtime.InteropServices", "MarshalAsAttribute");
#ifndef DISABLE_REFLECTION_EMIT
static GENERATE_GET_CLASS_WITH_CACHE (module_builder, "System.Reflection.Emit", "ModuleBuilder");
#endif

static char* string_to_utf8_image_raw (MonoImage *image, MonoString *s, MonoError *error);

#ifndef DISABLE_REFLECTION_EMIT
static guint32 mono_image_get_sighelper_token (MonoDynamicImage *assembly, MonoReflectionSigHelperHandle helper, MonoError *error);
static gboolean ensure_runtime_vtable (MonoClass *klass, MonoError  *error);
static void reflection_methodbuilder_from_dynamic_method (ReflectionMethodBuilder *rmb, MonoReflectionDynamicMethod *mb);
static gboolean reflection_setup_internal_class (MonoReflectionTypeBuilderHandle tb, MonoError *error);
static gboolean reflection_init_generic_class (MonoReflectionTypeBuilderHandle tb, MonoError *error);
static gboolean reflection_setup_class_hierarchy (GHashTable *unparented, MonoError *error);
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

	mono_os_mutex_init_recursive (&method_to_dyn_method_lock);
}

char*
string_to_utf8_image_raw (MonoImage *image, MonoString *s_raw, MonoError *error)
{
	/* FIXME all callers to string_to_utf8_image_raw should use handles */
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MONO_HANDLE_DCL (MonoString, s);
	char* const result = mono_string_to_utf8_image (image, s, error);
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

	klass = mono_class_from_mono_type_internal (type);
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
(mono_image_g_malloc0) (MonoImage *image, guint size)
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
swap_with_size (gpointer void_dest, gconstpointer void_val, int len, int nelem)
{
	char *dest = (char*)void_dest;
	const char* val = (const char*)void_val;
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
	for (i = 0; i < mono_array_length_internal (ilgen->ex_handlers); ++i) {
		ex_info = (MonoILExceptionInfo*)mono_array_addr_internal (ilgen->ex_handlers, MonoILExceptionInfo, i);
		if (ex_info->handlers)
			num_clauses += mono_array_length_internal (ex_info->handlers);
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
	int i, j, clause_index;

	clauses = image_g_new0 (image, MonoExceptionClause, num_clauses);

	clause_index = 0;
	for (i = mono_array_length_internal (ilgen->ex_handlers) - 1; i >= 0; --i) {
		ex_info = (MonoILExceptionInfo*)mono_array_addr_internal (ilgen->ex_handlers, MonoILExceptionInfo, i);
		finally_start = ex_info->start + ex_info->len;
		if (!ex_info->handlers)
			continue;
		for (j = 0; j < mono_array_length_internal (ex_info->handlers); ++j) {
			ex_block = (MonoILExceptionBlock*)mono_array_addr_internal (ex_info->handlers, MonoILExceptionBlock, j);
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
				clause->data.catch_class = mono_class_from_mono_type_internal (extype);
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

	if (!cattrs || !mono_array_length_internal (cattrs))
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
	MONO_HANDLE_PIN (rmb->ilgen);
	rmb->rtype = (MonoReflectionType*)mb->rtype;
	MONO_HANDLE_PIN (rmb->rtype);
	rmb->parameters = mb->parameters;
	MONO_HANDLE_PIN (rmb->parameters);
	rmb->generic_params = mb->generic_params;
	MONO_HANDLE_PIN (rmb->generic_params);
	rmb->generic_container = mb->generic_container;
	rmb->opt_types = NULL;
	rmb->pinfo = mb->pinfo;
	MONO_HANDLE_PIN (rmb->pinfo);
	rmb->attrs = mb->attrs;
	rmb->iattrs = mb->iattrs;
	rmb->call_conv = mb->call_conv;
	rmb->code = mb->code;
	MONO_HANDLE_PIN (rmb->code);
	rmb->type = mb->type;
	MONO_HANDLE_PIN (rmb->type);
	rmb->name = mb->name;
	MONO_HANDLE_PIN (rmb->name);
	rmb->table_idx = &mb->table_idx;
	rmb->init_locals = mb->init_locals;
	rmb->skip_visibility = FALSE;
	rmb->return_modreq = mb->return_modreq;
	MONO_HANDLE_PIN (rmb->return_modreq);
	rmb->return_modopt = mb->return_modopt;
	MONO_HANDLE_PIN (rmb->return_modopt);
	rmb->param_modreq = mb->param_modreq;
	MONO_HANDLE_PIN (rmb->param_modreq);
	rmb->param_modopt = mb->param_modopt;
	MONO_HANDLE_PIN (rmb->param_modopt);
	rmb->mhandle = mb->mhandle;
	rmb->nrefs = 0;
	rmb->refs = NULL;

	if (mb->dll) {
		rmb->charset = mb->charset;
		rmb->extra_flags = mb->extra_flags;
		rmb->native_cc = mb->native_cc;
		rmb->dllentry = mb->dllentry;
		MONO_HANDLE_PIN (rmb->dllentry);
		rmb->dll = mb->dll;
		MONO_HANDLE_PIN (rmb->dll);
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
	MONO_HANDLE_PIN (rmb->ilgen);
	rmb->rtype = mono_type_get_object_checked (mono_get_void_type (), error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_PIN (rmb->rtype);
	rmb->parameters = mb->parameters;
	MONO_HANDLE_PIN (rmb->parameters);
	rmb->generic_params = NULL;
	rmb->generic_container = NULL;
	rmb->opt_types = NULL;
	rmb->pinfo = mb->pinfo;
	MONO_HANDLE_PIN (rmb->pinfo);
	rmb->attrs = mb->attrs;
	rmb->iattrs = mb->iattrs;
	rmb->call_conv = mb->call_conv;
	rmb->code = NULL;
	rmb->type = mb->type;
	MONO_HANDLE_PIN (rmb->type);
	rmb->name = mono_string_new_checked (name, error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_PIN (rmb->name);
	rmb->table_idx = &mb->table_idx;
	rmb->init_locals = mb->init_locals;
	rmb->skip_visibility = FALSE;
	rmb->return_modreq = NULL;
	rmb->return_modopt = NULL;
	rmb->param_modreq = mb->param_modreq;
	MONO_HANDLE_PIN (rmb->param_modreq);
	rmb->param_modopt = mb->param_modopt;
	MONO_HANDLE_PIN (rmb->param_modopt);
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
	MONO_HANDLE_PIN (rmb->ilgen);
	rmb->rtype = mb->rtype;
	MONO_HANDLE_PIN (rmb->type);
	rmb->parameters = mb->parameters;
	MONO_HANDLE_PIN (rmb->parameters);
	rmb->generic_params = NULL;
	rmb->generic_container = NULL;
	rmb->opt_types = NULL;
	rmb->pinfo = NULL;
	rmb->attrs = mb->attrs;
	rmb->iattrs = 0;
	rmb->call_conv = mb->call_conv;
	rmb->code = NULL;
	rmb->type = (MonoObject *) mb->owner;
	MONO_HANDLE_PIN (rmb->type);
	rmb->name = mb->name;
	MONO_HANDLE_PIN (rmb->name);
	rmb->table_idx = NULL;
	rmb->init_locals = mb->init_locals;
	rmb->skip_visibility = mb->skip_visibility;
	rmb->return_modreq = NULL;
	rmb->return_modopt = NULL;
	rmb->param_modreq = NULL;
	rmb->param_modopt = NULL;
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
mono_image_add_memberef_row (MonoDynamicImage *assembly)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoDynamicTable *table;
	guint32 token;

	table = &assembly->tables [MONO_TABLE_MEMBERREF];
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
mono_image_get_memberref_token (MonoDynamicImage *assembly, MonoType *type)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	mono_image_typedef_or_ref (assembly, type);
	return mono_image_add_memberef_row (assembly);
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
		sig = mono_metadata_signature_dup (mono_method_signature_internal (method));
		if ((sig->call_convention != MONO_CALL_DEFAULT) && (sig->call_convention != MONO_CALL_VARARG))
			sig->call_convention = MONO_CALL_DEFAULT;
		token = mono_image_get_memberref_token (assembly, m_class_get_byval_arg (method->klass));
		g_free (sig);
		g_hash_table_insert (assembly->handleref, method, GUINT_TO_POINTER(token));
	}

	if (create_typespec) {
		MonoDynamicTable *table = &assembly->tables [MONO_TABLE_METHODSPEC];
		g_assert (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF);

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
	
	table = &assembly->tables [MONO_TABLE_MEMBERREF];
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

static gboolean
is_field_on_gtd (MonoClassField *field)
{
	return mono_class_is_gtd (field->parent);
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
		type = mono_field_get_type_internal (&mono_class_get_generic_class (field->parent)->container_class->fields [index]);
	} else {
		type = mono_field_get_type_internal (field);
	}
	token = mono_image_get_memberref_token (assembly, m_class_get_byval_arg (field->parent));
	g_hash_table_insert (assembly->handleref, field, GUINT_TO_POINTER(token));
	return token;
}

static guint32
method_encode_methodspec (MonoDynamicImage *assembly, MonoMethod *method)
{
	MonoDynamicTable *table;
	guint32 token, mtoken;
	MonoMethodInflated *imethod;
	MonoMethod *declaring;

	table = &assembly->tables [MONO_TABLE_METHODSPEC];

	g_assert (method->is_inflated);
	imethod = (MonoMethodInflated *) method;
	declaring = imethod->declaring;

	mtoken = mono_image_get_memberref_token (assembly, m_class_get_byval_arg (method->klass));

	if (!mono_method_signature_internal (declaring)->generic_param_count)
		return mtoken;

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

	if (mono_method_signature_internal (imethod->declaring)->generic_param_count) {
		token = method_encode_methodspec (assembly, method);
	} else {
		token = mono_image_get_memberref_token (assembly, m_class_get_byval_arg (method->klass));
	}

	g_hash_table_insert (assembly->handleref, method, GUINT_TO_POINTER(token));
	return token;
}

static guint32
mono_image_get_inflated_method_token (MonoDynamicImage *assembly, MonoMethod *m)
{
	return mono_image_get_memberref_token (assembly, m_class_get_byval_arg (m->klass));
}

static guint32 
mono_image_get_sighelper_token (MonoDynamicImage *assembly, MonoReflectionSigHelperHandle helper, MonoError *error)
{
	guint32 idx;
	MonoDynamicTable *table;

	error_init (error);

	table = &assembly->tables [MONO_TABLE_STANDALONESIG];
	idx = table->next_idx ++;
	table->rows ++;
	return idx;
}

static int
reflection_cc_to_file (int call_conv)
{
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
		sig->ret = mono_get_void_type ();

	MonoReflectionTypeHandle parent;
	parent = MONO_HANDLE_NEW_GET (MonoReflectionType, m, parent);
	MonoType *mtype;
	mtype = mono_reflection_type_handle_mono_type (parent, error);
	goto_if_nok (error, fail);

	for (int i = 0; i < nparams; ++i) {
		sig->params [i] = mono_type_array_get_and_resolve (parameters, i, error);
		goto_if_nok (error, fail);
	}

	MonoStringHandle mname;
	mname = MONO_HANDLE_NEW_GET (MonoString, m, name);
	name = mono_string_handle_to_utf8 (mname, error);
	goto_if_nok (error, fail);

	ArrayMethod *am;
	am = NULL;
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
	am->token = mono_image_get_memberref_token (assembly, am->parent);
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
	guint32 idx, token = 0;

	MonoDynamicImage *assembly = MONO_HANDLE_GETVAL (ref_module, dynamic_image);
	if (!assembly) {
		if (!mono_image_module_basic_init (ref_module, error))
			goto leave;

		assembly = MONO_HANDLE_GETVAL (ref_module, dynamic_image);
	}
	g_assert (assembly != NULL);

	idx = assembly->us.index ++;
	token = MONO_TOKEN_STRING | idx;
	mono_dynamic_image_register_token (assembly, token, MONO_HANDLE_CAST (MonoObject, str), MONO_DYN_IMAGE_TOK_NEW);

leave:
	HANDLE_FUNCTION_RETURN_VAL (token);
}

static guint32
create_method_token (MonoDynamicImage *assembly, MonoMethod *method, MonoArrayHandle opt_param_types, MonoError *error)
{
	guint32 parent;

	int nargs = mono_array_handle_length (opt_param_types);
	MonoMethodSignature *old = mono_method_signature_internal (method);
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

	parent = mono_image_typedef_or_ref (assembly, m_class_get_byval_arg (method->klass));
	g_assert ((parent & MONO_TYPEDEFORREF_MASK) == MONO_MEMBERREF_PARENT_TYPEREF);
	parent >>= MONO_TYPEDEFORREF_BITS;

	parent <<= MONO_MEMBERREF_PARENT_BITS;
	parent |= MONO_MEMBERREF_PARENT_TYPEREF;

	guint32 token = mono_image_get_varargs_method_token (assembly, parent, method->name, 0);
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
	if (strcmp (klass->name, "RuntimeMethodInfo") == 0 || strcmp (klass->name, "RuntimeConstructorInfo") == 0) {
		MonoReflectionMethodHandle ref_method = MONO_HANDLE_CAST (MonoReflectionMethod, obj);
		MonoMethod *method = MONO_HANDLE_GETVAL (ref_method, method);
		g_assert (!MONO_HANDLE_IS_NULL (opt_param_types) && (mono_method_signature_internal (method)->sentinelpos >= 0));
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
	g_assert (!is_ok (error));
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
	int how_collide;
	how_collide = MONO_DYN_IMAGE_TOK_SAME_OK;

	if (strcmp (klass->name, "RuntimeType") == 0) {
		MonoType *type = mono_reflection_type_handle_mono_type (MONO_HANDLE_CAST (MonoReflectionType, obj), error);
		goto_if_nok (error, leave);
		MonoClass *mc = mono_class_from_mono_type_internal (type);
		token = mono_metadata_token_from_dor (
			mono_dynimage_encode_typedef_or_ref_full (assembly, type, !mono_class_is_gtd (mc) || create_open_instance));
		/* If it's a RuntimeType now, we could have registered a
		 * TypeBuilder for it before, so replacing is okay. */
		how_collide = MONO_DYN_IMAGE_TOK_REPLACE;
	} else if (strcmp (klass->name, "RuntimeMethodInfo") == 0 ||
			   strcmp (klass->name, "RuntimeConstructorInfo") == 0) {
		MonoReflectionMethodHandle m = MONO_HANDLE_CAST (MonoReflectionMethod, obj);
		MonoMethod *method = MONO_HANDLE_GETVAL (m, method);
		if (method->is_inflated) {
			if (create_open_instance) {
				guint32 methodspec_token = mono_image_get_methodspec_token (assembly, method);
				MonoReflectionMethodHandle canonical_obj =
					mono_method_get_object_handle (method, NULL, error);
				goto_if_nok (error, leave);
				MONO_HANDLE_ASSIGN (register_obj, canonical_obj);
				token = methodspec_token;
			} else
				token = mono_image_get_inflated_method_token (assembly, method);
		} else if ((method->klass->image == &assembly->image) &&
			   !mono_class_is_ginst (method->klass) &&
			   !mono_class_is_gtd (method->klass)) {
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
				mono_method_get_object_handle (method, NULL, error);
			goto_if_nok (error, leave);
			MONO_HANDLE_ASSIGN (register_obj, canonical_obj);
			token = methodref_token;
		}
		/*g_print ("got token 0x%08x for %s\n", token, m->method->name);*/
	} else if (strcmp (klass->name, "RuntimeFieldInfo") == 0) {
		MonoReflectionFieldHandle f = MONO_HANDLE_CAST (MonoReflectionField, obj);
		MonoClassField *field = MONO_HANDLE_GETVAL (f, field);
		if ((field->parent->image == &assembly->image) &&
		    !is_field_on_gtd (field) &&
		    !is_field_on_inst (field)) {
			static guint32 field_table_idx = 0xffffff;
			field_table_idx --;
			token = MONO_TOKEN_FIELD_DEF | field_table_idx;
			g_assert (!mono_class_is_gtd (field->parent));
			how_collide = MONO_DYN_IMAGE_TOK_NEW;
		} else {
			guint32 fieldref_token = mono_image_get_fieldref_token (assembly, field);
			/* Same as methodref: get a canonical object to
			 * register with the token. */
			MonoReflectionFieldHandle canonical_obj =
				mono_field_get_object_handle (field->parent, field, error);
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

static gpointer
register_assembly (MonoReflectionAssembly *res, MonoAssembly *assembly)
{
	return CACHE_OBJECT (MonoReflectionAssembly *, mono_mem_manager_get_ambient (), assembly, &res->object, NULL);
}

static MonoReflectionModuleBuilderHandle
register_module (MonoReflectionModuleBuilderHandle res, MonoDynamicImage *module)
{
	return CACHE_OBJECT_HANDLE (MonoReflectionModuleBuilder, mono_mem_manager_get_ambient (), module, MONO_HANDLE_CAST (MonoObject, res), NULL);
}

/*
 * mono_reflection_dynimage_basic_init:
 * @assembly: an assembly builder object
 *
 * Create the MonoImage that represents the assembly builder and setup some
 * of the helper hash table and the basic metadata streams.
 */
void
mono_reflection_dynimage_basic_init (MonoReflectionAssemblyBuilder *assemblyb, MonoError *error)
{
	MonoDynamicAssembly *assembly;
	MonoDynamicImage *image;
	MonoAssemblyLoadContext *alc = mono_alc_get_default ();
	
	if (assemblyb->dynamic_assembly)
		return;

	assembly = assemblyb->dynamic_assembly = g_new0 (MonoDynamicAssembly, 1);

	MONO_PROFILER_RAISE (assembly_loading, (&assembly->assembly));
	
	assembly->assembly.ref_count = 1;
	assembly->assembly.dynamic = TRUE;
	assemblyb->assembly.assembly = (MonoAssembly*)assembly;
	assembly->assembly.basedir = NULL;
	return_if_nok (error);
	if (assemblyb->culture) {
		assembly->assembly.aname.culture = mono_string_to_utf8_checked_internal (assemblyb->culture, error);
		return_if_nok (error);
	} else
		assembly->assembly.aname.culture = g_strdup ("");

        if (assemblyb->version) {
			char *vstr = mono_string_to_utf8_checked_internal (assemblyb->version, error);
			if (mono_error_set_pending_exception (error))
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

	/* SRE assemblies are loaded into the individual loading context, ie,
	 * they only fire AssemblyResolve events, they don't cause probing for
	 * referenced assemblies to happen. */
	assembly->assembly.context.kind = MONO_ASMCTX_INDIVIDUAL;

	char *assembly_name = mono_string_to_utf8_checked_internal (assemblyb->name, error);
	return_if_nok (error);
	image = mono_dynamic_image_create (assembly, assembly_name, g_strdup ("RefEmit_YouForgotToDefineAModule"));
	image->initial_image = TRUE;
	assembly->assembly.aname.name = image->image.name;
	assembly->assembly.image = &image->image;

	mono_alc_add_assembly (alc, (MonoAssembly*)assembly);

	register_assembly (&assemblyb->assembly, &assembly->assembly);
	
	MONO_PROFILER_RAISE (assembly_loaded, (&assembly->assembly));
	
	mono_assembly_invoke_load_hook_internal (alc, (MonoAssembly*)assembly);
}

static gboolean
image_module_basic_init (MonoReflectionModuleBuilderHandle moduleb, MonoError *error)
{
	error_init (error);
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
		register_module (moduleb, image);

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

static MonoType *
add_custom_modifiers_to_type (MonoType *without_mods, MonoArrayHandle req_array, MonoArrayHandle opt_array, MonoImage *image, MonoError *error)
{
	HANDLE_FUNCTION_ENTER();
	error_init (error);

	int num_req_mods = 0;
	if (!MONO_HANDLE_IS_NULL (req_array))
		num_req_mods = mono_array_handle_length (req_array);

	int num_opt_mods = 0;
	if (!MONO_HANDLE_IS_NULL (opt_array))
		num_opt_mods = mono_array_handle_length (opt_array);

	const int total_mods = num_req_mods + num_opt_mods;
	if (total_mods == 0)
		return without_mods;

	MonoTypeWithModifiers *result;
	result = mono_image_g_malloc0 (image, mono_sizeof_type_with_mods (total_mods, FALSE));
	memcpy (result, without_mods, MONO_SIZEOF_TYPE);
	result->unmodified.has_cmods = 1;
	MonoCustomModContainer *cmods = mono_type_get_cmods ((MonoType *)result);
	g_assert (cmods);
	cmods->count = total_mods;
	cmods->image = image;

	g_assert (image_is_dynamic (image));
	MonoDynamicImage *allocator = (MonoDynamicImage *) image;

	g_assert (total_mods > 0);
	/* store cmods in reverse order from how the API supplies them.
	(Assemblies store modifiers in reverse order of IL syntax - and SRE
	follows the same order as IL syntax). */
	int modifier_index = total_mods - 1;

	MonoObjectHandle mod_handle = MONO_HANDLE_NEW (MonoObject, NULL);
	for (int i=0; i < num_req_mods; i++) {
		cmods->modifiers [modifier_index].required = TRUE;
		MONO_HANDLE_ARRAY_GETREF (mod_handle, req_array, i);
		cmods->modifiers [modifier_index].token = mono_image_create_token (allocator, mod_handle, FALSE, TRUE, error);
		modifier_index--;
	}

	for (int i=0; i < num_opt_mods; i++) {
		cmods->modifiers [modifier_index].required = FALSE;
		MONO_HANDLE_ARRAY_GETREF (mod_handle, opt_array, i);
		cmods->modifiers [modifier_index].token = mono_image_create_token (allocator, mod_handle, FALSE, TRUE, error);
		modifier_index--;
	}

	g_assert (modifier_index == -1);

	HANDLE_FUNCTION_RETURN_VAL ((MonoType *) result);
}

static
MonoType *
mono_type_array_get_and_resolve_with_modifiers (MonoArrayHandle types, MonoArrayHandle required_modifiers, MonoArrayHandle optional_modifiers, int idx, MonoImage *image, MonoError *error)
{
	HANDLE_FUNCTION_ENTER();
	error_init (error);
	MonoReflectionTypeHandle type = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	MonoArrayHandle req_mods_handle = MONO_HANDLE_NEW (MonoArray, NULL);
	MonoArrayHandle opt_mods_handle = MONO_HANDLE_NEW (MonoArray, NULL);

	if (!MONO_HANDLE_IS_NULL (required_modifiers))
		MONO_HANDLE_ARRAY_GETREF (req_mods_handle, required_modifiers, idx);

	if (!MONO_HANDLE_IS_NULL (optional_modifiers))
		MONO_HANDLE_ARRAY_GETREF (opt_mods_handle, optional_modifiers, idx);

	MONO_HANDLE_ARRAY_GETREF (type, types, idx);

	MonoType *result = mono_reflection_type_handle_mono_type (type, error);
	result = (MonoType *) add_custom_modifiers_to_type (result, req_mods_handle, opt_mods_handle, image, error);

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
	HANDLE_FUNCTION_ENTER ();

	error_init (error);

	MONO_STATIC_POINTER_INIT (MonoMethod, method_get_underlying_system_type)

		method_get_underlying_system_type = mono_class_get_method_from_name_checked (mono_defaults.systemtype_class, "get_UnderlyingSystemType", 0, 0, error);
		mono_error_assert_ok (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, method_get_underlying_system_type)

	MonoReflectionTypeHandle rt = MONO_HANDLE_NEW (MonoReflectionType, NULL);

	MonoMethod *usertype_method = mono_object_handle_get_virtual_method (MONO_HANDLE_CAST (MonoObject, t), method_get_underlying_system_type, error);
	goto_if_nok (error, leave);

	MONO_HANDLE_ASSIGN (rt, MONO_HANDLE_CAST (MonoReflectionType, MONO_HANDLE_NEW (MonoObject, mono_runtime_invoke_checked (usertype_method, MONO_HANDLE_RAW (t), NULL, error))));

leave:
	HANDLE_FUNCTION_RETURN_REF (MonoReflectionType, rt);
}

MonoType*
mono_reflection_type_get_handle (MonoReflectionType* ref_raw, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MONO_HANDLE_DCL (MonoReflectionType, ref);
	MonoType * const result = mono_reflection_type_handle_mono_type (ref, error);
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
	MonoReflectionTypeHandle ref_gtd;
	ref_gtd = MONO_HANDLE_NEW_GET (MonoReflectionType, ref_gclass, generic_type);
	MonoType *gtd;
	gtd = mono_reflection_type_handle_mono_type (ref_gtd, error);
	goto_if_nok (error, leave);
	MonoClass *gtd_klass;
	gtd_klass = mono_class_from_mono_type_internal (gtd);
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
	param->num = MONO_HANDLE_GETVAL (ref_gparam, index);

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
		param->owner = generic_container;
	} else {
		MonoType *type = mono_reflection_type_handle_mono_type (MONO_HANDLE_CAST (MonoReflectionType, ref_tbuilder), error);
		goto_if_nok (error, leave);
		MonoClass *owner;
		owner = mono_class_from_mono_type_internal (type);
		g_assert (mono_class_is_gtd (owner));
		param->owner = mono_class_get_generic_container (owner);
	}

	MonoClass *pklass;
	pklass = mono_class_create_generic_parameter ((MonoGenericParam *) param);

	result = m_class_get_byval_arg (pklass);

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
	MonoType * const result = mono_type_array_get_and_resolve (array, idx, error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

MonoType*
mono_reflection_type_handle_mono_type (MonoReflectionTypeHandle ref, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);

	MonoType* result = NULL;

	g_assert (!MONO_HANDLE_IS_NULL (ref));
	if (MONO_HANDLE_IS_NULL (ref))
		goto leave;
	MonoType *t;
	t = MONO_HANDLE_GETVAL (ref, type);
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

	MonoClass *klass;
	klass = mono_handle_class (ref);

	if (is_sre_array (klass)) {
		MonoReflectionArrayTypeHandle sre_array = MONO_HANDLE_CAST (MonoReflectionArrayType, ref);
		MonoReflectionTypeHandle ref_element = MONO_HANDLE_NEW_GET (MonoReflectionType, sre_array, element_type);
		MonoType *base = mono_reflection_type_handle_mono_type (ref_element, error);
		goto_if_nok (error, leave);
		g_assert (base);
		gint32 rank = MONO_HANDLE_GETVAL (sre_array, rank);
		MonoClass *eclass = mono_class_from_mono_type_internal (base);
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
		result = &mono_class_from_mono_type_internal (base)->this_arg;
		MONO_HANDLE_SETVAL (ref, type, MonoType*, result);
	} else if (is_sre_pointer (klass)) {
		MonoReflectionDerivedTypeHandle sre_pointer = MONO_HANDLE_CAST (MonoReflectionDerivedType, ref);
		MonoReflectionTypeHandle ref_element = MONO_HANDLE_NEW_GET (MonoReflectionType, sre_pointer, element_type);
		MonoType *base = mono_reflection_type_handle_mono_type (ref_element, error);
		goto_if_nok (error, leave);
		g_assert (base);
		result = m_class_get_byval_arg (mono_class_create_ptr (base));
		MONO_HANDLE_SETVAL (ref, type, MonoType*, result);
	} else if (is_sre_generic_instance (klass)) {
		result = reflection_instance_handle_mono_type (MONO_HANDLE_CAST (MonoReflectionGenericClass, ref), error);
	} else if (is_sre_gparam_builder (klass)) {
		result = reflection_param_handle_mono_type (MONO_HANDLE_CAST (MonoReflectionGenericParam, ref), error);
	} else if (is_sre_enum_builder (klass)) {
		MonoReflectionEnumBuilderHandle ref_ebuilder = MONO_HANDLE_CAST (MonoReflectionEnumBuilder, ref);

		MonoReflectionTypeHandle ref_tb = MONO_HANDLE_CAST (MonoReflectionType, MONO_HANDLE_NEW_GET (MonoReflectionTypeBuilder, ref_ebuilder, tb));
		result = mono_reflection_type_handle_mono_type (ref_tb, error);
	} else if (is_sre_type_builder (klass)) {
		MonoReflectionTypeBuilderHandle ref_tb = MONO_HANDLE_CAST (MonoReflectionTypeBuilder, ref);

		/* This happens when a finished type references an unfinished one. Have to create the minimal type */
		reflection_setup_internal_class (ref_tb, error);
		mono_error_assert_ok (error);
		result = MONO_HANDLE_GETVAL (ref, type);
	} else {
		g_error ("Cannot handle corlib user type %s", mono_type_full_name (m_class_get_byval_arg (mono_handle_class (ref))));
	}
leave:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

/**
 * LOCKING: Assumes the loader lock is held.
 */
static MonoMethodSignature*
parameters_to_signature (MonoImage *image, MonoArrayHandle parameters, MonoArrayHandle required_modifiers, MonoArrayHandle optional_modifiers, MonoError *error) {
	MonoMethodSignature *sig;
	int count, i;

	error_init (error);

	count = MONO_HANDLE_IS_NULL (parameters) ? 0 : mono_array_handle_length (parameters);

	sig = (MonoMethodSignature *)mono_image_g_malloc0 (image, MONO_SIZEOF_METHOD_SIGNATURE + sizeof (MonoType*) * count);
	sig->param_count = count;
	sig->sentinelpos = -1; /* FIXME */
	for (i = 0; i < count; ++i) {
		sig->params [i] = mono_type_array_get_and_resolve_with_modifiers (parameters, required_modifiers, optional_modifiers, i, image, error);
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
	MonoArrayHandle params = MONO_HANDLE_NEW_GET(MonoArray, ctor, parameters);
	MonoArrayHandle required_modifiers = MONO_HANDLE_NEW_GET(MonoArray, ctor, param_modreq);
	MonoArrayHandle optional_modifiers = MONO_HANDLE_NEW_GET(MonoArray, ctor, param_modopt);

	sig = parameters_to_signature (image, params, required_modifiers, optional_modifiers, error);
	return_val_if_nok (error, NULL);
	sig->hasthis = MONO_HANDLE_GETVAL (ctor, attrs) & METHOD_ATTRIBUTE_STATIC? 0: 1;
	sig->ret = mono_get_void_type ();
	return sig;
}

static MonoMethodSignature*
ctor_builder_to_signature_raw (MonoImage *image, MonoReflectionCtorBuilder* ctor_raw, MonoError *error) {
	HANDLE_FUNCTION_ENTER();
	MONO_HANDLE_DCL (MonoReflectionCtorBuilder, ctor);
	MonoMethodSignature * const sig = ctor_builder_to_signature (image, ctor, error);
	HANDLE_FUNCTION_RETURN_VAL (sig);
}
/**
 * LOCKING: Assumes the loader lock is held.
 */
static MonoMethodSignature*
method_builder_to_signature (MonoImage *image, MonoReflectionMethodBuilderHandle method, MonoError *error)
{
	MonoMethodSignature *sig;

	error_init (error);
	MonoArrayHandle params = MONO_HANDLE_NEW_GET(MonoArray, method, parameters);
	MonoArrayHandle required_modifiers = MONO_HANDLE_NEW_GET(MonoArray, method, param_modreq);
	MonoArrayHandle optional_modifiers = MONO_HANDLE_NEW_GET(MonoArray, method, param_modopt);
	MonoArrayHandle ret_req_modifiers = MONO_HANDLE_NEW_GET (MonoArray, method, return_modreq);
	MonoArrayHandle ret_opt_modifiers = MONO_HANDLE_NEW_GET (MonoArray, method, return_modopt);

	sig = parameters_to_signature (image, params, required_modifiers, optional_modifiers, error);
	return_val_if_nok (error, NULL);
	sig->hasthis = MONO_HANDLE_GETVAL (method, attrs) & METHOD_ATTRIBUTE_STATIC? 0: 1;
	MonoReflectionTypeHandle rtype;
	rtype = MONO_HANDLE_CAST (MonoReflectionType, MONO_HANDLE_NEW_GET (MonoObject, method, rtype));
	if (!MONO_HANDLE_IS_NULL (rtype)) {
		sig->ret = mono_reflection_type_handle_mono_type (rtype, error);
		sig->ret = add_custom_modifiers_to_type (sig->ret, ret_req_modifiers, ret_opt_modifiers, image, error);
		if (!is_ok (error)) {
			image_g_free (image, sig);
			return NULL;
		}
	} else {
		sig->ret = mono_get_void_type ();
	}
	MonoArrayHandle generic_params = MONO_HANDLE_NEW_GET (MonoArray, method, generic_params);
	sig->generic_param_count = MONO_HANDLE_IS_NULL (generic_params) ? 0 : mono_array_handle_length (generic_params);
	return sig;
}

static MonoMethodSignature*
dynamic_method_to_signature (MonoReflectionDynamicMethodHandle method, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoMethodSignature *sig = NULL;

	error_init (error);

	sig = parameters_to_signature (NULL, MONO_HANDLE_NEW_GET (MonoArray, method, parameters),
		MONO_HANDLE_CAST (MonoArray, NULL_HANDLE), MONO_HANDLE_CAST (MonoArray, NULL_HANDLE), error);
	goto_if_nok (error, leave);
	sig->hasthis = MONO_HANDLE_GETVAL (method, attrs) & METHOD_ATTRIBUTE_STATIC? 0: 1;
	MonoReflectionTypeHandle rtype;
	rtype = MONO_HANDLE_NEW_GET (MonoReflectionType, method, rtype);
	if (!MONO_HANDLE_IS_NULL (rtype)) {
		sig->ret = mono_reflection_type_handle_mono_type (rtype, error);
		if (!is_ok (error)) {
			g_free (sig);
			sig = NULL;
			goto leave;
		}
	} else {
		sig->ret = mono_get_void_type ();
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
		*name = mono_string_to_utf8_checked_internal (pb->name, error);
		return_if_nok (error);
		*type = mono_reflection_type_get_handle ((MonoReflectionType*)pb->type, error);
	} else {
		MonoReflectionProperty *p = (MonoReflectionProperty *)prop;
		*name = g_strdup (p->property->name);
		if (p->property->get)
			*type = mono_method_signature_internal (p->property->get)->ret;
		else
			*type = mono_method_signature_internal (p->property->set)->params [mono_method_signature_internal (p->property->set)->param_count - 1];
	}
}

static void
get_field_name_and_type (MonoObject *field, char **name, MonoType **type, MonoError *error)
{
	error_init (error);
	MonoClass *klass = mono_object_class (field);
	if (strcmp (klass->name, "FieldBuilder") == 0) {
		MonoReflectionFieldBuilder *fb = (MonoReflectionFieldBuilder *)field;
		*name = mono_string_to_utf8_checked_internal (fb->name, error);
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

gboolean
mono_is_sr_mono_property (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection", "RuntimePropertyInfo");
}

static gboolean
is_sr_mono_method (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection", "RuntimeMethodInfo");
}

gboolean
mono_is_sr_mono_cmethod (MonoClass *klass)
{
	check_corlib_type_cached (klass, "System.Reflection", "RuntimeConstructorInfo");
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
encode_cattr_value (MonoAssembly *assembly, char *buffer, char *p, char **retbuffer, char **retp, guint32 *buflen, MonoType *type, MonoObject *arg, gconstpointer void_argval, MonoError *error)
{
	const char *argval = (const char*)void_argval;
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
		argval = (const char*)mono_object_get_data (arg);
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
			simple_type = mono_class_enum_basetype_internal (type->data.klass)->type;
			goto handle_enum;
		} else {
			g_warning ("generic valuetype %s not handled in custom attr value decoding", type->data.klass->name);
		}
		break;
	case MONO_TYPE_STRING: {
		char *str;
		guint32 slen;
		if (!arg) {
MONO_DISABLE_WARNING(4309) // truncation of constant
			*p++ = 0xFF;
MONO_RESTORE_WARNING
			break;
		}
		str = mono_string_to_utf8_checked_internal ((MonoString*)arg, error);
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
MONO_DISABLE_WARNING(4309) // truncation of constant
			*p++ = 0xFF;
MONO_RESTORE_WARNING
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
MONO_DISABLE_WARNING(4309) // truncation of constant
			*p++ = 0xff; *p++ = 0xff; *p++ = 0xff; *p++ = 0xff;
MONO_RESTORE_WARNING
			break;
		}
		len = mono_array_length_internal ((MonoArray*)arg);
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
			char *elptr = mono_array_addr_internal ((MonoArray*)arg, char, 0);
			int elsize = mono_class_array_element_size (arg_eclass);
			for (i = 0; i < len; ++i) {
				encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, m_class_get_byval_arg (arg_eclass), NULL, elptr, error);
				return_if_nok (error);
				elptr += elsize;
			}
		} else if (eclass->valuetype && arg_eclass->valuetype) {
			char *elptr = mono_array_addr_internal ((MonoArray*)arg, char, 0);
			int elsize = mono_class_array_element_size (eclass);
			for (i = 0; i < len; ++i) {
				encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, m_class_get_byval_arg (eclass), NULL, elptr, error);
				return_if_nok (error);
				elptr += elsize;
			}
		} else {
			for (i = 0; i < len; ++i) {
				encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, m_class_get_byval_arg (eclass), mono_array_get_internal ((MonoArray*)arg, MonoObject*, i), NULL, error);
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
MONO_DISABLE_WARNING(4309) // truncation of constant
			*p++ = 0xFF;
MONO_RESTORE_WARNING
			break;
		}
		
		klass = mono_object_class (arg);

		if (mono_object_isinst_checked (arg, mono_defaults.systemtype_class, error)) {
			*p++ = 0x50;
			goto handle_type;
		} else {
			return_if_nok (error);
		}

		MonoType *klass_byval_arg = m_class_get_byval_arg (klass);
		if (klass->enumtype) {
			*p++ = 0x55;
		} else if (klass == mono_defaults.string_class) {
			simple_type = MONO_TYPE_STRING;
			*p++ = 0x0E;
			goto handle_enum;
		} else if (klass->rank == 1) {
			*p++ = 0x1D;
			if (m_class_get_byval_arg (m_class_get_element_class (klass))->type == MONO_TYPE_OBJECT)
				/* See Partition II, Appendix B3 */
				*p++ = 0x51;
			else
				*p++ = m_class_get_byval_arg (m_class_get_element_class (klass))->type;
			encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, klass_byval_arg, arg, NULL, error);
			return_if_nok (error);
			break;
		} else if (klass_byval_arg->type >= MONO_TYPE_BOOLEAN && klass_byval_arg->type <= MONO_TYPE_R8) {
			*p++ = simple_type = klass_byval_arg->type;
			goto handle_enum;
		} else {
			mono_error_set_not_supported (error, "unhandled type in custom attr");
			break;
		}
		str = type_get_qualified_name (m_class_get_byval_arg (klass), NULL);
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
		simple_type = mono_class_enum_basetype_internal (klass)->type;
		goto handle_enum;
	}
	default:
		mono_error_set_not_supported (error, "type 0x%02x not yet supported in custom attr encoder", simple_type);
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
			encode_field_or_prop_type (m_class_get_byval_arg (type->data.klass), p, &p);
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
		char *str = type_get_qualified_name (m_class_get_byval_arg (type->data.klass), NULL);
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
	HANDLE_FUNCTION_ENTER ();

	MonoArrayHandle result;

	MONO_ENTER_GC_UNSAFE;

	ERROR_DECL (error);

	MONO_HANDLE_NEW (MonoReflectionAssembly, assembly);
	MONO_HANDLE_NEW (MonoObject, ctor);
	MONO_HANDLE_NEW (MonoArray, ctorArgs);
	MONO_HANDLE_NEW (MonoArray, properties);
	MONO_HANDLE_NEW (MonoArray, propValues);
	MONO_HANDLE_NEW (MonoArray, fields);
	MONO_HANDLE_NEW (MonoArray, fieldValues);

	result = mono_reflection_get_custom_attrs_blob_checked (assembly, ctor, ctorArgs, properties, propValues, fields, fieldValues, error);

	mono_error_cleanup (error);

	MONO_EXIT_GC_UNSAFE;

	HANDLE_FUNCTION_RETURN_OBJ (result);
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
MonoArrayHandle
mono_reflection_get_custom_attrs_blob_checked (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *propValues, MonoArray *fields, MonoArray* fieldValues, MonoError *error)
{
	MonoArrayHandle result = NULL_HANDLE_INIT;
	MonoMethodSignature *sig;
	MonoMethodSignature *sig_free = NULL;
	MonoObject *arg;
	char *buffer = NULL;
	char *p;
	guint32 buflen, i;
	MonoObjectHandle h1 = NULL_HANDLE_INIT;
	MonoObjectHandle h2 = NULL_HANDLE_INIT;
	MonoObjectHandle argh = NULL_HANDLE_INIT;

	HANDLE_FUNCTION_ENTER ();

	if (strcmp (ctor->vtable->klass->name, "RuntimeConstructorInfo")) {
		/* sig is freed later so allocate it in the heap */
		sig = ctor_builder_to_signature_raw (NULL, (MonoReflectionCtorBuilder*)ctor, error); /* FIXME use handles */
		sig_free = sig;
		goto_if_nok (error, leave);
	} else {
		sig = mono_method_signature_internal (((MonoReflectionMethod*)ctor)->method);
	}

	g_assert (mono_array_length_internal (ctorArgs) == sig->param_count);
	buflen = 256;
	p = buffer = (char *)g_malloc (buflen);
	/* write the prolog */
	*p++ = 1;
	*p++ = 0;

	argh = MONO_HANDLE_NEW (MonoObject, NULL);

	for (i = 0; i < sig->param_count; ++i) {
		arg = mono_array_get_internal (ctorArgs, MonoObject*, i);
		MONO_HANDLE_ASSIGN_RAW (argh, arg);
		encode_cattr_value (assembly->assembly, buffer, p, &buffer, &p, &buflen, sig->params [i], arg, NULL, error);
		goto_if_nok (error, leave);
	}
	i = 0;
	if (properties)
		i += mono_array_length_internal (properties);
	if (fields)
		i += mono_array_length_internal (fields);
	*p++ = i & 0xff;
	*p++ = (i >> 8) & 0xff;

	if (properties || fields) {
		h1 = MONO_HANDLE_NEW (MonoObject, NULL);
		h2 = MONO_HANDLE_NEW (MonoObject, NULL);
	}

	if (properties) {

		MonoObject *prop;

		for (i = 0; i < mono_array_length_internal (properties); ++i) {
			MonoType *ptype;
			char *pname;

			prop = (MonoObject *)mono_array_get_internal (properties, gpointer, i);
			MONO_HANDLE_ASSIGN_RAW (h1, prop);

			get_prop_name_and_type (prop, &pname, &ptype, error);
			goto_if_nok (error, leave);
			*p++ = 0x54; /* PROPERTY signature */

			prop = (MonoObject*)mono_array_get_internal (propValues, gpointer, i);
			MONO_HANDLE_ASSIGN_RAW (h2, prop);

			encode_named_val (assembly, buffer, p, &buffer, &p, &buflen, ptype, pname, prop, error);
			g_free (pname);
			goto_if_nok (error, leave);
		}
	}

	if (fields) {

		MonoObject *field;

		for (i = 0; i < mono_array_length_internal (fields); ++i) {
			MonoType *ftype;
			char *fname;

			field = (MonoObject *)mono_array_get_internal (fields, gpointer, i);
			MONO_HANDLE_ASSIGN_RAW (h1, field);

			get_field_name_and_type (field, &fname, &ftype, error);
			goto_if_nok (error, leave);
			*p++ = 0x53; /* FIELD signature */

			field = (MonoObject*)mono_array_get_internal (fieldValues, gpointer, i);
			MONO_HANDLE_ASSIGN_RAW (h2, field);

			encode_named_val (assembly, buffer, p, &buffer, &p, &buflen, ftype, fname, field, error);
			g_free (fname);
			goto_if_nok (error, leave);
		}
	}

	g_assert (p - buffer <= buflen);
	buflen = p - buffer;
	result = mono_array_new_handle (mono_defaults.byte_class, buflen, error);
	goto_if_nok (error, leave);
	p = mono_array_addr_internal (MONO_HANDLE_RAW (result), char, 0);
	memcpy (p, buffer, buflen);
leave:
	g_free (buffer);
	g_free (sig_free);

	HANDLE_FUNCTION_RETURN_REF (MonoArray, result);
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
		MonoClass *child_class = mono_class_from_mono_type_internal (child_type);
		if (parent_type != NULL) {
			MonoClass *parent_class = mono_class_from_mono_type_internal (parent_type);
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

	MONO_HANDLE_SETVAL (ref_tb, state, gint32/*MonoTypeBuilderState*/, MonoTypeBuilderEntered);
	MonoReflectionModuleBuilderHandle module_ref;
	module_ref = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, ref_tb, module);
	GHashTable *unparented_classes;
	unparented_classes = MONO_HANDLE_GETVAL(module_ref, unparented_classes);

	// If this type is already setup, exit. We'll fix the parenting later
	MonoType *type;
	type = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionType, ref_tb), type);
	if (type)
		goto leave;

	MonoReflectionModuleBuilderHandle ref_module;
	ref_module = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, ref_tb, module);
	MonoDynamicImage *dynamic_image;
	dynamic_image = MONO_HANDLE_GETVAL (ref_module, dynamic_image);

	MonoStringHandle ref_name;
	ref_name = MONO_HANDLE_NEW_GET (MonoString, ref_tb, name);
	MonoStringHandle ref_nspace;
	ref_nspace = MONO_HANDLE_NEW_GET (MonoString, ref_tb, nspace);

	guint32 table_idx;
	table_idx = MONO_HANDLE_GETVAL (ref_tb, table_idx);
	/*
	 * The size calculation here warrants some explaining. 
	 * reflection_setup_internal_class is called too early, well before we know whether the type will be a GTD or DEF,
	 * meaning we need to alloc enough space to morth a def into a gtd.
	 */
	MonoClass *klass;
	klass = (MonoClass *)mono_image_alloc0 (&dynamic_image->image, MAX (sizeof (MonoClassDef), sizeof (MonoClassGtd)));
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

	MonoReflectionTypeHandle ref_nesting_type;
	ref_nesting_type = MONO_HANDLE_NEW_GET (MonoReflectionType, ref_tb, nesting_type);
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
		klass->instance_size = MONO_ABI_SIZEOF (MonoObject);
		klass->size_inited = 1;
		mono_class_setup_vtable_general (klass, NULL, 0, NULL);
	}

	mono_class_setup_mono_type (klass);

	/*
	 * FIXME: handle interfaces.
	 */
	MonoReflectionTypeHandle ref_tb_type;
	ref_tb_type = MONO_HANDLE_CAST (MonoReflectionType, ref_tb);
	MONO_HANDLE_SETVAL (ref_tb_type, type, MonoType*, m_class_get_byval_arg (klass));
	MONO_HANDLE_SETVAL (ref_tb, state, gint32, MonoTypeBuilderFinished);

	reflection_init_generic_class (ref_tb, error);
	goto_if_nok (error, leave);

	// Do here so that the search inside of the parent can see the above type that's been set.
	MonoReflectionTypeHandle ref_parent;
	ref_parent = MONO_HANDLE_CAST (MonoReflectionType, MONO_HANDLE_NEW_GET (MonoObject, ref_tb, parent));
	MonoType *parent_type;
	parent_type = NULL;
	if (!MONO_HANDLE_IS_NULL (ref_parent)) {
		MonoClass *parent_klass = mono_handle_class (ref_parent);
		gboolean recursive_init = TRUE;

		if (is_sre_type_builder (parent_klass)) {
			MonoTypeBuilderState parent_state = (MonoTypeBuilderState)MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionTypeBuilder, ref_parent), state);

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
	g_assert (!g_hash_table_lookup (unparented_classes, m_class_get_byval_arg (klass)));
	g_hash_table_insert (unparented_classes, m_class_get_byval_arg (klass), parent_type);

	if (!MONO_HANDLE_IS_NULL (ref_nesting_type)) {
		if (!reflection_setup_internal_class (MONO_HANDLE_CAST (MonoReflectionTypeBuilder, ref_nesting_type), error))
			goto leave;

		MonoType *nesting_type = mono_reflection_type_handle_mono_type (ref_nesting_type, error);
		goto_if_nok (error, leave);
		klass->nested_in = mono_class_from_mono_type_internal (nesting_type);
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

	MonoTypeBuilderState ref_state = (MonoTypeBuilderState)MONO_HANDLE_GETVAL (ref_tb, state);
	g_assert (ref_state == MonoTypeBuilderFinished);

	MonoType *type = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionType, ref_tb), type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);

	MonoArrayHandle generic_params = MONO_HANDLE_NEW_GET (MonoArray, ref_tb, generic_params);
	int count = MONO_HANDLE_IS_NULL (generic_params) ? 0 : mono_array_handle_length (generic_params);

	if (count == 0)
		goto leave;

	if (mono_class_try_get_generic_container (klass) != NULL)
		goto leave; /* already setup */

	MonoGenericContainer *generic_container;
	generic_container = (MonoGenericContainer *)mono_image_alloc0 (klass->image, sizeof (MonoGenericContainer));

	generic_container->owner.klass = klass;
	generic_container->type_argc = count;
	generic_container->type_params = (MonoGenericParamFull *)mono_image_alloc0 (klass->image, sizeof (MonoGenericParamFull) * count);

	klass->class_kind = MONO_CLASS_GTD;
	mono_class_set_generic_container (klass, generic_container);


	MonoReflectionGenericParamHandle ref_gparam;
	ref_gparam = MONO_HANDLE_NEW (MonoReflectionGenericParam, NULL);
	for (int i = 0; i < count; i++) {
		MONO_HANDLE_ARRAY_GETREF (ref_gparam, generic_params, i);
		MonoType *param_type = mono_reflection_type_handle_mono_type (MONO_HANDLE_CAST (MonoReflectionType, ref_gparam), error);
		goto_if_nok (error, leave);
		MonoGenericParamFull *param = (MonoGenericParamFull *) param_type->data.generic_param;
		generic_container->type_params [i] = *param;
		/*Make sure we are a diferent type instance */
		generic_container->type_params [i].owner = generic_container;
		generic_container->type_params [i].info.pklass = NULL;
		generic_container->type_params [i].info.flags = MONO_HANDLE_GETVAL (ref_gparam, attrs);

		g_assert (generic_container->type_params [i].owner);
	}

	generic_container->context.class_inst = mono_get_shared_generic_inst (generic_container);
	MonoGenericContext* context;
	context = &generic_container->context;
	MonoType *canonical_inst;
	canonical_inst = &((MonoClassGtd*)klass)->canonical_inst;
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
			res->data.custom_data.cookie = mono_string_to_utf8_checked_internal (minfo->mcookie, error);
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
mono_reflection_marshal_as_attribute_from_marshal_spec (MonoClass *klass,
							MonoMarshalSpec *spec, MonoError *error)
{
	error_init (error);
	
	MonoAssemblyLoadContext *alc = mono_alc_get_ambient ();
	MonoReflectionMarshalAsAttributeHandle minfo = MONO_HANDLE_CAST (MonoReflectionMarshalAsAttribute, mono_object_new_handle (mono_class_get_marshal_as_attribute_class (), error));
	goto_if_nok (error, fail);
	guint32 utype;
	utype = spec->native;
	MONO_HANDLE_SETVAL (minfo, utype, guint32, utype);

	switch (utype) {
	case MONO_NATIVE_LPARRAY:
		MONO_HANDLE_SETVAL (minfo, array_subtype, guint32, spec->data.array_data.elem_type);
		if (spec->data.array_data.num_elem != -1)
			MONO_HANDLE_SETVAL (minfo, size_const, gint32, spec->data.array_data.num_elem);
		if (spec->data.array_data.param_num != -1)
			MONO_HANDLE_SETVAL (minfo, size_param_index, gint16, spec->data.array_data.param_num);
		break;

	case MONO_NATIVE_BYVALTSTR:
	case MONO_NATIVE_BYVALARRAY:
		if (spec->data.array_data.num_elem != -1)
			MONO_HANDLE_SETVAL (minfo, size_const, gint32, spec->data.array_data.num_elem);
		break;

	case MONO_NATIVE_CUSTOM:
		if (spec->data.custom_data.custom_name) {
			MonoType *mtype = mono_reflection_type_from_name_checked (spec->data.custom_data.custom_name, alc, klass->image, error);
			goto_if_nok (error, fail);

			if (mtype) {
				MonoReflectionTypeHandle rt = mono_type_get_object_handle (mtype, error);
				goto_if_nok (error, fail);

				MONO_HANDLE_SET (minfo, marshal_type_ref, rt);
			}

			MonoStringHandle custom_name = mono_string_new_handle (spec->data.custom_data.custom_name, error);
			goto_if_nok (error, fail);
			MONO_HANDLE_SET (minfo, marshal_type, custom_name);
		}
		if (spec->data.custom_data.cookie) {
			MonoStringHandle cookie = mono_string_new_handle (spec->data.custom_data.cookie, error);
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
	MonoMarshalSpec **specs = NULL;
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
	m->name = string_to_utf8_image_raw (image, rmb->name, error);
	goto_if_nok (error, fail);
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

		goto leave;

	} else if (!(m->flags & METHOD_ATTRIBUTE_ABSTRACT) &&
			   !(m->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		MonoMethodHeader *header;
		guint32 code_size;
		gint32 max_stack, i;
		gint32 num_locals = 0;
		gint32 num_clauses = 0;
		guint8 *code;

		if (rmb->ilgen) {
			code = mono_array_addr_internal (rmb->ilgen->code, guint8, 0);
			code_size = rmb->ilgen->code_len;
			max_stack = rmb->ilgen->max_stack;
			num_locals = rmb->ilgen->locals ? mono_array_length_internal (rmb->ilgen->locals) : 0;
			if (rmb->ilgen->ex_handlers)
				num_clauses = mono_reflection_method_count_clauses (rmb->ilgen);
		} else {
			if (rmb->code) {
				code = mono_array_addr_internal (rmb->code, guint8, 0);
				code_size = mono_array_length_internal (rmb->code);
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
				mono_array_get_internal (rmb->ilgen->locals, MonoReflectionLocalBuilder*, i);

			header->locals [i] = image_g_new0 (image, MonoType, 1);
			MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)lb->type, error);
			mono_error_assert_ok (error);
			memcpy (header->locals [i], type, mono_sizeof_type (type));
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
		int count = mono_array_length_internal (rmb->generic_params);
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
				mono_array_get_internal (rmb->generic_params, MonoReflectionGenericParam*, i);
			MonoType *gp_type = mono_reflection_type_get_handle ((MonoReflectionType*)gp, error);
			mono_error_assert_ok (error);
			MonoGenericParamFull *param = (MonoGenericParamFull *) gp_type->data.generic_param;
			container->type_params [i] = *param;
			container->type_params [i].owner = container;

			gp->type.type->data.generic_param = (MonoGenericParam*)&container->type_params [i];

			MonoClass *gklass = mono_class_from_mono_type_internal (gp_type);
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
		method_aux->param_names = image_g_new0 (image, char *, mono_method_signature_internal (m)->param_count + 1);
		for (i = 0; i <= m->signature->param_count; ++i) {
			MonoReflectionParamBuilder *pb;
			if ((pb = mono_array_get_internal (rmb->pinfo, MonoReflectionParamBuilder*, i))) {
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
	if (rmb->pinfo)		
		for (i = 0; i < mono_array_length_internal (rmb->pinfo); ++i) {
			MonoReflectionParamBuilder *pb;
			if ((pb = mono_array_get_internal (rmb->pinfo, MonoReflectionParamBuilder*, i))) {
				if (pb->marshal_info) {
					if (specs == NULL)
						specs = image_g_new0 (image, MonoMarshalSpec*, sig->param_count + 1);
					specs [pb->position] = 
						mono_marshal_spec_from_builder (image, klass->image->assembly, pb->marshal_info, error);
					goto_if_nok (error, fail);
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

leave:
	mono_loader_unlock ();
	if (!m) // FIXME: This leaks if image is not NULL.
		image_g_free (image, specs);
	return m;

fail:
	 m = NULL;
	 goto leave;
}	

static MonoMethod*
ctorbuilder_to_mono_method (MonoClass *klass, MonoReflectionCtorBuilder* mb, MonoError *error)
{
	/* We need to clear handles for rmb fields created in mono_reflection_methodbuilder_from_ctor_builder */
	HANDLE_FUNCTION_ENTER ();
	ReflectionMethodBuilder rmb;
	MonoMethodSignature *sig;
	MonoMethod *ret;

	mono_loader_lock ();

	if (!mono_reflection_methodbuilder_from_ctor_builder (&rmb, mb, error)) {
		mono_loader_unlock ();
		goto exit_null;
	}

	g_assert (klass->image != NULL);
	sig = ctor_builder_to_signature_raw (klass->image, mb, error); /* FIXME use handles */
	mono_loader_unlock ();
	goto_if_nok (error, exit_null);

	mb->mhandle = reflection_methodbuilder_to_mono_method (klass, &rmb, sig, error);
	goto_if_nok (error, exit_null);
	mono_save_custom_attrs (klass->image, mb->mhandle, mb->cattrs);

	/* ilgen is no longer needed */
	mb->ilgen = NULL;
	ret = mb->mhandle;
	goto exit;
exit_null:
	ret = NULL;
exit:
	HANDLE_FUNCTION_RETURN_VAL (ret);
}

static MonoMethod*
methodbuilder_to_mono_method (MonoClass *klass, MonoReflectionMethodBuilderHandle ref_mb, MonoError *error)
{
	/* We need to clear handles for rmb fields created in mono_reflection_methodbuilder_from_method_builder */
	HANDLE_FUNCTION_ENTER ();
	ReflectionMethodBuilder rmb;
	MonoMethodSignature *sig;
	MonoMethod *ret, *method;

	error_init (error);

	mono_loader_lock ();

	MonoReflectionMethodBuilder *mb = MONO_HANDLE_RAW (ref_mb); /* FIXME use handles */
	if (!mono_reflection_methodbuilder_from_method_builder (&rmb, mb, error)) {
		mono_loader_unlock ();
		goto exit_null;
	}

	g_assert (klass->image != NULL);
	sig = method_builder_to_signature (klass->image, ref_mb, error);
	mono_loader_unlock ();
	goto_if_nok (error, exit_null);

	method = reflection_methodbuilder_to_mono_method (klass, &rmb, sig, error);
	goto_if_nok (error, exit_null);
	MONO_HANDLE_SETVAL (ref_mb, mhandle, MonoMethod*, method);
	mono_save_custom_attrs (klass->image, method, mb->cattrs);

	/* ilgen is no longer needed */
	mb->ilgen = NULL;
	ret = method;
	goto exit;
exit_null:
	ret = NULL;
exit:
	HANDLE_FUNCTION_RETURN_VAL (ret);
}

static MonoMethod*
methodbuilder_to_mono_method_raw (MonoClass *klass, MonoReflectionMethodBuilder* mb_raw, MonoError *error)
{
	HANDLE_FUNCTION_ENTER (); /* FIXME change callers of methodbuilder_to_mono_method_raw to use handles */
	error_init (error);
	MONO_HANDLE_DCL (MonoReflectionMethodBuilder, mb);
	MonoMethod * const result = methodbuilder_to_mono_method (klass, mb, error);
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
		MonoType *parent_type = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (m_class_get_parent (gklass)), &mono_class_get_generic_class (klass)->context, error);
		if (is_ok (error)) {
			MonoClass *parent = mono_class_from_mono_type_internal (parent_type);
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

		MonoClass **gklass_interfaces = m_class_get_interfaces (gklass);
		for (i = 0; i < gklass->interface_count; ++i) {
			MonoType *iface_type = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (gklass_interfaces [i]), mono_class_get_context (klass), error);
			return_val_if_nok (error, FALSE);

			klass->interfaces [i] = mono_class_from_mono_type_internal (iface_type);
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
	MonoReflectionTypeBuilder *tb = mono_class_get_ref_info_raw (klass); /* FIXME use handles */
	int i, num, j;

	error_init (error);

	if (!image_is_dynamic (klass->image) || (!tb && !mono_class_is_ginst (klass)) || klass->wastypebuilder)
		return TRUE;
	if (klass->parent)
		if (!ensure_runtime_vtable (klass->parent, error))
			return FALSE;

	if (tb) {
		num = tb->ctors? mono_array_length_internal (tb->ctors): 0;
		num += tb->num_methods;
		mono_class_set_method_count (klass, num);
		klass->methods = (MonoMethod **)mono_image_alloc (klass->image, sizeof (MonoMethod*) * num);
		num = tb->ctors? mono_array_length_internal (tb->ctors): 0;
		for (i = 0; i < num; ++i) {
			MonoMethod *ctor = ctorbuilder_to_mono_method (klass, mono_array_get_internal (tb->ctors, MonoReflectionCtorBuilder*, i), error);
			if (!ctor)
				return FALSE;
			klass->methods [i] = ctor;
		}
		num = tb->num_methods;
		j = i;
		for (i = 0; i < num; ++i) {
			MonoMethod *meth = methodbuilder_to_mono_method_raw (klass, mono_array_get_internal (tb->methods, MonoReflectionMethodBuilder*, i), error); /* FIXME use handles */
			if (!meth)
				return FALSE;
			klass->methods [j++] = meth;
		}
	
		if (tb->interfaces) {
			klass->interface_count = mono_array_length_internal (tb->interfaces);
			klass->interfaces = (MonoClass **)mono_image_alloc (klass->image, sizeof (MonoClass*) * klass->interface_count);
			for (i = 0; i < klass->interface_count; ++i) {
				MonoType *iface = mono_type_array_get_and_resolve_raw (tb->interfaces, i, error); /* FIXME use handles */
				return_val_if_nok (error, FALSE);
				klass->interfaces [i] = mono_class_from_mono_type_internal (iface);
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

		MonoMethod *result = (MonoMethod*)mono_reflection_resolve_object (NULL, method, &handle_class, NULL, error);
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

	tb = mono_class_get_ref_info_raw (klass); /* FIXME use handles */
	g_assert (strcmp (mono_object_class (tb)->name, "TypeBuilder") == 0);

	onum = 0;
	if (tb->methods) {
		for (i = 0; i < tb->num_methods; ++i) {
			MonoReflectionMethodBuilder *mb = 
				mono_array_get_internal (tb->methods, MonoReflectionMethodBuilder*, i);
			if (mb->override_methods)
				onum += mono_array_length_internal (mb->override_methods);
		}
	}

	if (onum) {
		*overrides = g_new0 (MonoMethod*, onum * 2);

		onum = 0;
		for (i = 0; i < tb->num_methods; ++i) {
			MonoReflectionMethodBuilder *mb = 
				mono_array_get_internal (tb->methods, MonoReflectionMethodBuilder*, i);
			if (mb->override_methods) {
				for (j = 0; j < mono_array_length_internal (mb->override_methods); ++j) {
					m = mono_array_get_internal (mb->override_methods, MonoReflectionMethod*, j);

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

static gint32
modulebuilder_get_next_table_index (MonoReflectionModuleBuilder *mb, gint32 table, gint32 num_fields, MonoError *error)
{
	error_init (error);

	if (mb->table_indexes == NULL) {
		MonoArray *arr = mono_array_new_checked (mono_defaults.int_class, 64, error);
		return_val_if_nok (error, 0);
		for (int i = 0; i < 64; i++) {
			mono_array_set_internal (arr, int, i, 1);
		}
		MONO_OBJECT_SETREF_INTERNAL (mb, table_indexes, arr);
	}
	gint32 index = mono_array_get_internal (mb->table_indexes, gint32, table);
	gint32 next_index = index + num_fields;
	mono_array_set_internal (mb->table_indexes, gint32, table, next_index);
	return index;
}

static void
typebuilder_setup_one_field (MonoDynamicImage *dynamic_image, MonoClass *klass, int32_t first_idx, MonoArray *tb_fields, int i, MonoFieldDefaultValue *def_value_out, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	{
		MonoImage *image = klass->image;
		MonoReflectionFieldBuilder *fb;
		MonoClassField *field;
		MonoArray *rva_data;

		fb = (MonoReflectionFieldBuilder *)mono_array_get_internal (tb_fields, gpointer, i);
		field = &klass->fields [i];
		field->parent = klass;
		field->name = string_to_utf8_image_raw (image, fb->name, error); /* FIXME use handles */
		goto_if_nok (error, leave);
		if (fb->attrs) {
			MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type, error);
			goto_if_nok (error, leave);
			field->type = mono_metadata_type_dup (klass->image, type);
			field->type->attrs = fb->attrs;
		} else {
			field->type = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type, error);
			goto_if_nok (error, leave);
		}
		if (klass->enumtype && strcmp (field->name, "value__") == 0) // used by enum classes to store the instance value
			field->type->attrs |= FIELD_ATTRIBUTE_RT_SPECIAL_NAME;

		if (!mono_type_get_underlying_type (field->type)) {
			if (!(klass->enumtype && mono_metadata_type_equal (field->type, m_class_get_byval_arg (klass)))) {
				mono_class_set_type_load_failure (klass, "Field '%s' is an enum type with a bad underlying type", field->name);
				goto leave;
			}
		}

		if ((fb->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA) && (rva_data = fb->rva_data)) {
			char *base = mono_array_addr_internal (rva_data, char, 0);
			size_t size = mono_array_length_internal (rva_data);
			char *data = (char *)mono_image_alloc (klass->image, size);
			memcpy (data, base, size);
			def_value_out->data = data;
		}
		if (fb->offset != -1)
			field->offset = fb->offset;
		fb->handle = field;
		mono_save_custom_attrs (klass->image, field, fb->cattrs);

		if (fb->def_value) {
			guint32 len, idx;
			const char *p, *p2;
			MonoDynamicImage *assembly = (MonoDynamicImage*)klass->image;
			field->type->attrs |= FIELD_ATTRIBUTE_HAS_DEFAULT;
			idx = mono_dynimage_encode_constant (assembly, fb->def_value, &def_value_out->def_type);
			/* Copy the data from the blob since it might get realloc-ed */
			p = assembly->blob.data + idx;
			len = mono_metadata_decode_blob_size (p, &p2);
			len += p2 - p;
			def_value_out->data = (const char *)mono_image_alloc (image, len);
			memcpy ((gpointer)def_value_out->data, p, len);
		}

		MonoObjectHandle field_builder_handle = MONO_HANDLE_CAST (MonoObject, MONO_HANDLE_NEW (MonoReflectionFieldBuilder, fb));
		mono_dynamic_image_register_token (dynamic_image, mono_metadata_make_token (MONO_TABLE_FIELD, first_idx + i), field_builder_handle, MONO_DYN_IMAGE_TOK_NEW);
	}
leave:
	HANDLE_FUNCTION_RETURN ();
}

/* This initializes the same data as mono_class_setup_fields () */
static void
typebuilder_setup_fields (MonoClass *klass, MonoError *error)
{
	MonoReflectionTypeBuilder *tb = mono_class_get_ref_info_raw (klass); /* FIXME use handles */
	MonoFieldDefaultValue *def_values;
	MonoImage *image = klass->image;
	int i, instance_size, packing_size = 0;

	error_init (error);

	if (klass->parent) {
		if (!klass->parent->size_inited)
			mono_class_init_internal (klass->parent);
		instance_size = klass->parent->instance_size;
	} else {
		instance_size = MONO_ABI_SIZEOF (MonoObject);
	}

	int fcount = tb->num_fields;
	mono_class_set_field_count (klass, fcount);

	gint32 first_idx = 0;
	if (tb->num_fields > 0) {
		first_idx = modulebuilder_get_next_table_index (tb->module, MONO_TABLE_FIELD, (gint32)tb->num_fields, error);
		return_if_nok (error);
	}
	mono_class_set_first_field_idx (klass, first_idx - 1); /* Why do we subtract 1? because mono_class_create_from_typedef does it, too. */

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
		typebuilder_setup_one_field (tb->module->dynamic_image, klass, first_idx, tb->fields, i, &def_values[i], error);
		if (!is_ok (error))
			return;
	}

	if (!mono_class_has_failure (klass))
		mono_class_layout_fields (klass, instance_size, packing_size, tb->class_size, TRUE);
}

static void
typebuilder_setup_properties (MonoClass *klass, MonoError *error)
{
	MonoReflectionTypeBuilder *tb = mono_class_get_ref_info_raw (klass); /* FIXME use handles */
	MonoReflectionPropertyBuilder *pb;
	MonoImage *image = klass->image;
	MonoProperty *properties;
	MonoClassPropertyInfo *info;
	int i;

	error_init (error);

	info = (MonoClassPropertyInfo*)mono_class_get_property_info (klass);
	if (!info) {
		info = mono_class_alloc0 (klass, sizeof (MonoClassPropertyInfo));
		mono_class_set_property_info (klass, info);
	}

	info->count = tb->properties ? mono_array_length_internal (tb->properties) : 0;
	info->first = 0;

	properties = image_g_new0 (image, MonoProperty, info->count);
	info->properties = properties;
	for (i = 0; i < info->count; ++i) {
		pb = mono_array_get_internal (tb->properties, MonoReflectionPropertyBuilder*, i);
		properties [i].parent = klass;
		properties [i].attrs = pb->attrs;
		properties [i].name = string_to_utf8_image_raw (image, pb->name, error); /* FIXME use handles */
		if (!is_ok (error))
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
	MonoReflectionTypeBuilder *tb = mono_class_get_ref_info_raw (klass); /* FIXME use handles */
	MonoReflectionEventBuilder *eb;
	MonoImage *image = klass->image;
	MonoEvent *events;
	MonoClassEventInfo *info;
	int i;

	error_init (error);

	info = mono_class_alloc0 (klass, sizeof (MonoClassEventInfo));
	mono_class_set_event_info (klass, info);

	info->count = tb->events ? mono_array_length_internal (tb->events) : 0;
	info->first = 0;

	events = image_g_new0 (image, MonoEvent, info->count);
	info->events = events;
	for (i = 0; i < info->count; ++i) {
		eb = mono_array_get_internal (tb->events, MonoReflectionEventBuilder*, i);
		events [i].parent = klass;
		events [i].attrs = eb->attrs;
		events [i].name = string_to_utf8_image_raw (image, eb->name, error); /* FIXME use handles */
		if (!is_ok (error))
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
			events [i].other = image_g_new0 (image, MonoMethod*, mono_array_length_internal (eb->other_methods) + 1);
			for (j = 0; j < mono_array_length_internal (eb->other_methods); ++j) {
				MonoReflectionMethodBuilder *mb = 
					mono_array_get_internal (eb->other_methods,
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
	ERROR_DECL (lerror);
	MonoError *error = already_failed ? lerror : data->error;

	if ((type->type == MONO_TYPE_GENERICINST) && (type->data.generic_class->container_class == klass)) {
		MonoClass *inst_klass = mono_class_from_mono_type_internal (type);
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
	HANDLE_FUNCTION_ENTER ();

	MonoReflectionModuleBuilderHandle module_ref = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, ref_tb, module);
	GHashTable *unparented_classes = MONO_HANDLE_GETVAL(module_ref, unparented_classes);
	gboolean ret_val;

	if (unparented_classes) {
		ret_val = reflection_setup_internal_class_internal (ref_tb, error);
	} else {
		// If we're not being called recursively
		unparented_classes = g_hash_table_new (NULL, NULL);
		MONO_HANDLE_SETVAL (module_ref, unparented_classes, GHashTable *, unparented_classes);

		ret_val = reflection_setup_internal_class_internal (ref_tb, error);
		mono_error_assert_ok (error);

		// Fix the relationship between the created classes and their parents
		reflection_setup_class_hierarchy (unparented_classes, error);
		mono_error_assert_ok (error);

		g_hash_table_destroy (unparented_classes);
		MONO_HANDLE_SETVAL (module_ref, unparented_classes, GHashTable *, NULL);
	}

	HANDLE_FUNCTION_RETURN_VAL (ret_val);
}

MonoReflectionTypeHandle
ves_icall_TypeBuilder_create_runtime_class (MonoReflectionTypeBuilderHandle ref_tb, MonoError *error)
{
	error_init (error);

	reflection_setup_internal_class (ref_tb, error);
	mono_error_assert_ok (error);

	MonoType *type = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionType, ref_tb), type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);

	MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, ref_tb, cattrs);
	mono_save_custom_attrs (klass->image, klass, MONO_HANDLE_RAW (cattrs)); /* FIXME use handles */

	mono_loader_lock ();

	if (klass->wastypebuilder) {
		mono_loader_unlock ();

		return mono_type_get_object_handle (m_class_get_byval_arg (klass), error);
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

	MonoArrayHandle nested_types;
	nested_types = MONO_HANDLE_NEW_GET (MonoArray, ref_tb, subtypes);
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
			nested = mono_g_list_prepend_image (klass->image, nested, mono_class_from_mono_type_internal (subtype));
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

	MonoArrayHandle generic_params;
	generic_params = MONO_HANDLE_NEW_GET (MonoArray, ref_tb, generic_params);
	if (!MONO_HANDLE_IS_NULL (generic_params)) {
		int num_params = mono_array_handle_length (generic_params);
		MonoReflectionTypeHandle ref_gparam = MONO_HANDLE_NEW (MonoReflectionType, NULL);
		for (int i = 0; i < num_params; i++) {
			MONO_HANDLE_ARRAY_GETREF (ref_gparam, generic_params, i);
			MonoType *param_type = mono_reflection_type_handle_mono_type (ref_gparam, error);
			goto_if_nok (error, failure);
			MonoClass *gklass = mono_class_from_mono_type_internal (param_type);

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
	if (mono_class_is_gtd (klass)) {
		MonoMemoryManager *memory_manager = mono_mem_manager_get_ambient ();
		struct remove_instantiations_user_data data;
		data.klass = klass;
		data.error = error;
		mono_error_assert_ok (error);
		mono_mem_manager_lock (memory_manager);
		mono_g_hash_table_foreach_remove (memory_manager->type_hash, remove_instantiations_of_and_ensure_contents, &data);
		mono_mem_manager_unlock (memory_manager);
		goto_if_nok (error, failure);
	}

	mono_loader_unlock ();

	if (klass->enumtype && !mono_class_is_valid_enum (klass)) {
		mono_class_set_type_load_failure (klass, "Not a valid enumeration");
		mono_error_set_type_load_class (error, klass, "Not a valid enumeration");
		goto failure_unlocked;
	}

	MonoReflectionTypeHandle res;
	res = mono_type_get_object_handle (m_class_get_byval_arg (klass), error);
	goto_if_nok (error, failure_unlocked);

	return res;

failure:
	mono_class_set_type_load_failure (klass, "TypeBuilder could not create runtime class due to: %s", mono_error_get_message (error));
	klass->wastypebuilder = TRUE;
	mono_loader_unlock ();
failure_unlocked:
	return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
}

typedef struct {
	MonoMethod *handle;
} DynamicMethodReleaseData;

/*
 * The runtime automatically clean up those after finalization.
*/	
static MonoReferenceQueue *dynamic_method_queue;

static void
free_dynamic_method (void *dynamic_method)
{
	DynamicMethodReleaseData *data = (DynamicMethodReleaseData *)dynamic_method;
	MonoMethod *method = data->handle;
	MonoGCHandle dis_link;

	dyn_methods_lock ();
	dis_link = g_hash_table_lookup (method_to_dyn_method, method);
	g_hash_table_remove (method_to_dyn_method, method);
	dyn_methods_unlock ();
	g_assert (dis_link);
	mono_gchandle_free_internal (dis_link);

	mono_runtime_free_method (method);
	g_free (data);
}

static gboolean
reflection_create_dynamic_method (MonoReflectionDynamicMethodHandle ref_mb, MonoError *error)
{
	/* We need to clear handles for rmb fields created in reflection_methodbuilder_from_dynamic_method */
	HANDLE_FUNCTION_ENTER ();
	MonoReferenceQueue *queue;
	MonoMethod *handle;
	DynamicMethodReleaseData *release_data;
	ReflectionMethodBuilder rmb;
	MonoMethodSignature *sig;
	MonoClass *klass;
	GSList *l;
	int i;
	gboolean ret = TRUE;
	MonoReflectionDynamicMethod *mb;
	MonoAssembly *ass = NULL;

	error_init (error);

	if (!(queue = dynamic_method_queue)) {
		mono_loader_lock ();
		if (!(queue = dynamic_method_queue))
			queue = dynamic_method_queue = mono_gc_reference_queue_new_internal (free_dynamic_method);
		mono_loader_unlock ();
	}

	sig = dynamic_method_to_signature (ref_mb, error);
	goto_if_nok (error, exit_false);

	mb = MONO_HANDLE_RAW (ref_mb); /* FIXME convert reflection_create_dynamic_method to use handles */
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
		MonoObject *obj = mono_array_get_internal (mb->refs, MonoObject*, i);
		MONO_HANDLE_PIN (obj);

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
				ref = method;

				method->referenced_by = g_slist_append (method->referenced_by, mb);
			}
			handle_class = mono_defaults.methodhandle_class;
		} else {
			MonoException *ex = NULL;
			ref = mono_reflection_resolve_object (mb->module->image, obj, &handle_class, NULL, error);
			/* ref should not be a reference. Otherwise we would need a handle for it */
			if (!is_ok  (error)) {
				g_free (rmb.refs);
				goto exit_false;
			}
			if (!ref)
				ex = mono_get_exception_type_load (NULL, NULL);
			if (ex) {
				g_free (rmb.refs);
				mono_error_set_exception_instance (error, ex);
				goto exit_false;
			}
		}

		rmb.refs [i] = ref;
		rmb.refs [i + 1] = handle_class;
	}		

	if (mb->owner) {
		MonoType *owner_type = mono_reflection_type_get_handle ((MonoReflectionType*)mb->owner, error);
		if (!is_ok (error)) {
			g_free (rmb.refs);
			goto exit_false;
		}
		klass = mono_class_from_mono_type_internal (owner_type);
		ass = klass->image->assembly;
	} else {
		klass = mono_defaults.object_class;
		ass = (mb->module && mb->module->image) ? mb->module->image->assembly : NULL;
	}

	mb->mhandle = handle = reflection_methodbuilder_to_mono_method (klass, &rmb, sig, error);
	((MonoDynamicMethod*)handle)->assembly = ass;
	g_free (rmb.refs);
	goto_if_nok (error, exit_false);

	release_data = g_new (DynamicMethodReleaseData, 1);
	release_data->handle = handle;
	if (!mono_gc_reference_queue_add_internal (queue, (MonoObject*)mb, release_data))
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

	dyn_methods_lock ();
	if (!method_to_dyn_method)
		method_to_dyn_method = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (method_to_dyn_method, handle, mono_gchandle_new_weakref_internal ((MonoObject *)mb, TRUE));
	dyn_methods_unlock ();

	goto exit;
exit_false:
	ret = FALSE;
exit:
	HANDLE_FUNCTION_RETURN_VAL (ret);
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
	HANDLE_FUNCTION_ENTER ();

	error_init (error);

	if (image_is_dynamic (klass->image) && !klass->wastypebuilder && mono_class_has_ref_info (klass)) {
		// TODO: make this work on netcore when working on SRE.TypeBuilder
		g_assert_not_reached ();
	}

	if (mono_class_is_ginst (klass)) {
		MonoGenericInst *inst = mono_class_get_generic_class (klass)->context.class_inst;
		int i;

		for (i = 0; i < inst->type_argc; ++i) {
			ensure_complete_type (mono_class_from_mono_type_internal (inst->type_argv [i]), error);
			goto_if_nok (error, exit);
		}
	}

exit:
	HANDLE_FUNCTION_RETURN ();
}

gpointer
mono_reflection_resolve_object (MonoImage *image, MonoObject *obj, MonoClass **handle_class, MonoGenericContext *context, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoClass *oklass = obj->vtable->klass;
	gpointer result = NULL;

	error_init (error);

	if (strcmp (oklass->name, "String") == 0) {
		result = MONO_HANDLE_RAW (mono_string_intern_checked (MONO_HANDLE_NEW (MonoString, (MonoString*)obj), error));
		goto_if_nok (error, return_null);
		*handle_class = mono_defaults.string_class;
		g_assert (result);
	} else if (strcmp (oklass->name, "RuntimeType") == 0) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)obj, error);
		goto_if_nok (error, return_null);
		MonoClass *mc = mono_class_from_mono_type_internal (type);
		if (!mono_class_init_internal (mc)) {
			mono_error_set_for_class_failure (error, mc);
			goto return_null;
		}

		if (context) {
			MonoType *inflated = mono_class_inflate_generic_type_checked (type, context, error);
			goto_if_nok (error, return_null);

			result = mono_class_from_mono_type_internal (inflated);
			mono_metadata_free_type (inflated);
		} else {
			result = mono_class_from_mono_type_internal (type);
		}
		*handle_class = mono_defaults.typehandle_class;
		g_assert (result);
	} else if (strcmp (oklass->name, "RuntimeMethodInfo") == 0 ||
			   strcmp (oklass->name, "RuntimeConstructorInfo") == 0) {
		result = ((MonoReflectionMethod*)obj)->method;
		if (context) {
			result = mono_class_inflate_generic_method_checked ((MonoMethod *)result, context, error);
			mono_error_assert_ok (error);
		}
		*handle_class = mono_defaults.methodhandle_class;
		g_assert (result);
	} else if (strcmp (oklass->name, "RuntimeFieldInfo") == 0) {
		MonoClassField *field = ((MonoReflectionField*)obj)->field;

		ensure_complete_type (field->parent, error);
		goto_if_nok (error, return_null);

		if (context) {
			MonoType *inflated = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (field->parent), context, error);
			goto_if_nok (error, return_null);

			MonoClass *klass;
			klass = mono_class_from_mono_type_internal (inflated);
			MonoClassField *inflated_field;
			gpointer iter = NULL;
			mono_metadata_free_type (inflated);
			while ((inflated_field = mono_class_get_fields_internal (klass, &iter))) {
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
		MonoReflectionTypeBuilderHandle tb = MONO_HANDLE_NEW (MonoReflectionTypeBuilder, (MonoReflectionTypeBuilder*)obj);
		MonoType *type = mono_reflection_type_get_handle (&MONO_HANDLE_RAW (tb)->type, error);
		goto_if_nok (error, return_null);
		MonoClass *klass;

		klass = type->data.klass;
		if (klass->wastypebuilder) {
			/* Already created */
			result = klass;
		} else {
			// TODO: make this work on netcore when working on SRE.TypeBuilder
			g_assert_not_reached();
		}

		*handle_class = mono_defaults.typehandle_class;
	} else if (strcmp (oklass->name, "SignatureHelper") == 0) {
		MonoReflectionSigHelper *helper = (MonoReflectionSigHelper*)obj;
		MonoMethodSignature *sig;
		int nargs, i;

		if (helper->arguments)
			nargs = mono_array_length_internal (helper->arguments);
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
				goto return_null;
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
		goto_if_nok (error, return_null);
		klass = mono_class_from_mono_type_internal (mtype);

		/* Find the method */

		name = mono_string_to_utf8_checked_internal (m->name, error);
		goto_if_nok (error, return_null);
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
			MonoMethod *m = mono_class_get_method_from_name_checked (mono_class_get_module_builder_class (), "RuntimeResolve", 1, 0, error);
			mono_error_assert_ok (error);
			g_assert (m);
			mono_memory_barrier ();
			resolve_method = m;
		}
		void *args [ ] = { obj };
		obj = mono_runtime_invoke_checked (resolve_method, NULL, args, error);
		goto_if_nok (error, return_null);
		g_assert (obj);
		result = mono_reflection_resolve_object (image, obj, handle_class, context, error);
		goto exit;
	} else {
		g_print ("%s\n", obj->vtable->klass->name);
		g_assert_not_reached ();
	}

	goto exit;
return_null:
	result = NULL;
	goto exit;
exit:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

gpointer
mono_reflection_resolve_object_handle (MonoImage *image, MonoObjectHandle obj, MonoClass **handle_class, MonoGenericContext *context, MonoError *error)
{
	return mono_reflection_resolve_object (image, MONO_HANDLE_RAW (obj), handle_class, context, error);
}

#else /* DISABLE_REFLECTION_EMIT */

MonoArray*
mono_reflection_get_custom_attrs_blob (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *propValues, MonoArray *fields, MonoArray* fieldValues)
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_reflection_dynimage_basic_init (MonoReflectionAssemblyBuilder *assemblyb, MonoError *error)
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
	return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
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

gint32
ves_icall_ModuleBuilder_getToken (MonoReflectionModuleBuilderHandle mb, MonoObjectHandle obj, MonoBoolean create_open_instance, MonoError *error)
{
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
	if (MONO_HANDLE_IS_NULL (method)) {
		mono_error_set_argument_null (error, "method", "");
		return 0;
	}

	return mono_image_create_method_token (MONO_HANDLE_GETVAL (mb, dynamic_image), MONO_HANDLE_CAST (MonoObject, method), opt_param_types, error);
}

void
ves_icall_ModuleBuilder_RegisterToken (MonoReflectionModuleBuilderHandle mb, MonoObjectHandle obj, guint32 token, MonoError *error)
{
	/* This function may be called by ModuleBuilder.FixupTokens to update
	 * an existing token, so replace is okay here. */
	mono_dynamic_image_register_token (MONO_HANDLE_GETVAL (mb, dynamic_image), token, obj, MONO_DYN_IMAGE_TOK_REPLACE);
}

MonoObjectHandle
ves_icall_ModuleBuilder_GetRegisteredToken (MonoReflectionModuleBuilderHandle mb, guint32 token, MonoError *error)
{
	MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (mb, dynamic_image);
	return mono_dynamic_image_get_registered_token (dynamic_image, token, error);
}

#ifndef DISABLE_REFLECTION_EMIT

MonoArrayHandle
ves_icall_CustomAttributeBuilder_GetBlob (MonoReflectionAssemblyHandle assembly, MonoObjectHandle ctor,
					  MonoArrayHandle ctorArgs, MonoArrayHandle properties,
					  MonoArrayHandle propValues, MonoArrayHandle fields,
					  MonoArrayHandle fieldValues, MonoError* error)
{
	return mono_reflection_get_custom_attrs_blob_checked (MONO_HANDLE_RAW (assembly), MONO_HANDLE_RAW (ctor),
							      MONO_HANDLE_RAW (ctorArgs), MONO_HANDLE_RAW (properties),
							      MONO_HANDLE_RAW (propValues), MONO_HANDLE_RAW (fields),
							      MONO_HANDLE_RAW (fieldValues), error);
}

#endif

void
ves_icall_AssemblyBuilder_basic_init (MonoReflectionAssemblyBuilderHandle assemblyb, MonoError *error)
{
	MonoGCHandle gchandle = mono_gchandle_from_handle (MONO_HANDLE_CAST (MonoObject, assemblyb), TRUE);
	mono_reflection_dynimage_basic_init (MONO_HANDLE_RAW (assemblyb), error);
	mono_gchandle_free_internal (gchandle);
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
	MONO_HANDLE_SETVAL (enumtype, type, MonoType*, MONO_HANDLE_GETVAL (t, type));
}

void
ves_icall_ModuleBuilder_basic_init (MonoReflectionModuleBuilderHandle moduleb, MonoError *error)
{
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
	MonoDynamicImage *image = MONO_HANDLE_GETVAL (moduleb, dynamic_image);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	g_assert (type);
	image->wrappers_type = mono_class_from_mono_type_internal (type);
}

MonoGCHandle
mono_method_to_dyn_method (MonoMethod *method)
{
	MonoGCHandle *handle;

	if (!method_to_dyn_method)
		return (MonoGCHandle)NULL;

	dyn_methods_lock ();
	handle = (MonoGCHandle*)g_hash_table_lookup (method_to_dyn_method, method);
	dyn_methods_unlock ();

	return handle;
}
