/**
 * \file
 * Images created at runtime.
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
#include "mono/metadata/object.h"
#include "mono/metadata/dynamic-image-internals.h"
#include "mono/metadata/dynamic-stream-internals.h"
#include "mono/metadata/gc-internals.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/mono-hash-internals.h"
#include "mono/metadata/profiler-private.h"
#include "mono/metadata/reflection-internals.h"
#include "mono/metadata/sre-internals.h"
#include "mono/utils/checked-build.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-os-mutex.h"

// The dynamic images list is only needed to support the mempool reference tracking feature in checked-build.
static GPtrArray *dynamic_images;
static mono_mutex_t dynamic_images_mutex;

static void
dynamic_images_lock (void)
{
	mono_os_mutex_lock (&dynamic_images_mutex);
}

static void
dynamic_images_unlock (void)
{
	mono_os_mutex_unlock (&dynamic_images_mutex);
}

void
mono_dynamic_images_init (void)
{
	mono_os_mutex_init (&dynamic_images_mutex);
}

#ifndef DISABLE_REFLECTION_EMIT
static void
string_heap_init (MonoDynamicStream *sh)
{
	mono_dynstream_init (sh);
}
#endif

#ifndef DISABLE_REFLECTION_EMIT
static int
mono_blob_entry_hash (const char* str)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	guint len, h;
	const char *end;
	len = mono_metadata_decode_blob_size (str, &str);
	if (len > 0) {
		end = str + len;
		h = *str;
		for (str += 1; str < end; str++)
			h = (h << 5) - h + *str;
		return h;
	} else {
		return 0;
	}
}

static gboolean
mono_blob_entry_equal (const char *str1, const char *str2) {
	MONO_REQ_GC_NEUTRAL_MODE;

	int len, len2;
	const char *end1;
	const char *end2;
	len = mono_metadata_decode_blob_size (str1, &end1);
	len2 = mono_metadata_decode_blob_size (str2, &end2);
	if (len != len2)
		return 0;
	return memcmp (end1, end2, len) == 0;
}
#endif


/**
 * mono_find_dynamic_image_owner:
 *
 * Find the dynamic image, if any, which a given pointer is located in the memory of.
 */
MonoImage *
mono_find_dynamic_image_owner (void *ptr)
{
	MonoImage *owner = NULL;
	int i;

	dynamic_images_lock ();

	if (dynamic_images)
	{
		for (i = 0; !owner && i < dynamic_images->len; ++i) {
			MonoImage *image = (MonoImage *)g_ptr_array_index (dynamic_images, i);
			if (mono_mempool_contains_addr (image->mempool, ptr))
				owner = image;
		}
	}

	dynamic_images_unlock ();

	return owner;
}

static void
dynamic_image_lock (MonoDynamicImage *image)
{
	MONO_ENTER_GC_SAFE;
	mono_image_lock ((MonoImage*)image);
	MONO_EXIT_GC_SAFE;
}

static void
dynamic_image_unlock (MonoDynamicImage *image)
{
	mono_image_unlock ((MonoImage*)image);
}

#ifndef DISABLE_REFLECTION_EMIT
/*
 * mono_dynamic_image_register_token:
 *
 *   Register the TOKEN->OBJ mapping in the mapping table in ASSEMBLY. This is required for
 * the Module.ResolveXXXToken () methods to work.
 */
void
mono_dynamic_image_register_token (MonoDynamicImage *assembly, guint32 token, MonoObjectHandle obj, int how_collide)
{
	MONO_REQ_GC_UNSAFE_MODE;

	g_assert (!MONO_HANDLE_IS_NULL (obj));
	g_assert (strcmp (m_class_get_name (mono_handle_class (obj)), "EnumBuilder"));
	dynamic_image_lock (assembly);
	MonoObject *prev = (MonoObject *)mono_g_hash_table_lookup (assembly->tokens, GUINT_TO_POINTER (token));
	if (prev) {
		switch (how_collide) {
		case MONO_DYN_IMAGE_TOK_NEW:
			g_warning ("%s: Unexpected previous object when called with MONO_DYN_IMAGE_TOK_NEW", __func__);
			break;
		case MONO_DYN_IMAGE_TOK_SAME_OK:
			if (prev != MONO_HANDLE_RAW (obj)) {
				g_warning ("%s: condition `prev == MONO_HANDLE_RAW (obj)' not met", __func__);
			}
			break;
		case MONO_DYN_IMAGE_TOK_REPLACE:
			break;
		default:
			g_assert_not_reached ();
		}
	}
	mono_g_hash_table_insert_internal (assembly->tokens, GUINT_TO_POINTER (token), MONO_HANDLE_RAW (obj));
	dynamic_image_unlock (assembly);
}
#else
void
mono_dynamic_image_register_token (MonoDynamicImage *assembly, guint32 token, MonoObjectHandle obj, int how_collide)
{
}
#endif

static gboolean
lookup_dyn_token (MonoDynamicImage *assembly, guint32 token, MonoObjectHandle *object_handle)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoObject *obj;

	dynamic_image_lock (assembly);
	obj = (MonoObject *)mono_g_hash_table_lookup (assembly->tokens, GUINT_TO_POINTER (token));
	dynamic_image_unlock (assembly);

	if (object_handle)
		*object_handle = MONO_HANDLE_NEW (MonoObject, obj);

	return obj != NULL;
}

#ifndef DISABLE_REFLECTION_EMIT
MonoObjectHandle
mono_dynamic_image_get_registered_token (MonoDynamicImage *dynimage, guint32 token, MonoError *error)
{
	MonoObjectHandle obj;
	lookup_dyn_token (dynimage, token, &obj);
	return obj;
}
#else /* DISABLE_REFLECTION_EMIT */
MonoObjectHandle
mono_dynamic_image_get_registered_token (MonoDynamicImage *dynimage, guint32 token, MonoError *error)
{
	g_assert_not_reached ();
	return NULL_HANDLE;
}
#endif

/**
 * 
 * mono_dynamic_image_is_valid_token:
 * 
 * Returns TRUE if token is valid in the given image.
 * 
 */
gboolean
mono_dynamic_image_is_valid_token (MonoDynamicImage *image, guint32 token)
{
	return lookup_dyn_token (image, token, NULL);
}

#ifndef DISABLE_REFLECTION_EMIT

#endif /* DISABLE_REFLECTION_EMIT */

#ifndef DISABLE_REFLECTION_EMIT
/**
 * mono_reflection_lookup_dynamic_token:
 *
 * Finish the Builder object pointed to by TOKEN and return the corresponding
 * runtime structure. If HANDLE_CLASS is not NULL, it is set to the class required by 
 * mono_ldtoken. If valid_token is TRUE, assert if it is not found in the token->object
 * mapping table.
 *
 * LOCKING: Take the loader lock
 */
gpointer
mono_reflection_lookup_dynamic_token (MonoImage *image, guint32 token, gboolean valid_token, MonoClass **handle_class, MonoGenericContext *context, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoDynamicImage *assembly = (MonoDynamicImage*)image;
	MonoObjectHandle obj;
	MonoClass *klass;

	error_init (error);
	
	lookup_dyn_token (assembly, token, &obj);
	if (MONO_HANDLE_IS_NULL (obj)) {
		if (valid_token)
			g_error ("Could not find required dynamic token 0x%08x", token);
		else {
			mono_error_set_execution_engine (error, "Could not find dynamic token 0x%08x", token);
			return NULL;
		}
	}

	if (!handle_class)
		handle_class = &klass;
	gpointer const result = mono_reflection_resolve_object_handle (image, obj, handle_class, context, error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}
#else /* DISABLE_REFLECTION_EMIT */
gpointer
mono_reflection_lookup_dynamic_token (MonoImage *image, guint32 token, gboolean valid_token, MonoClass **handle_class, MonoGenericContext *context, MonoError *error)
{
	error_init (error);
	return NULL;
}
#endif /* DISABLE_REFLECTION_EMIT */

#ifndef DISABLE_REFLECTION_EMIT

static const unsigned char table_sizes [MONO_TABLE_NUM] = {
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
	MONO_METHODIMPL_SIZE,
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

	MONO_GENERICPARAM_SIZE,	/* 0x2A */
	MONO_METHODSPEC_SIZE,
	MONO_GENPARCONSTRAINT_SIZE
};

MonoDynamicImage*
mono_dynamic_image_create (MonoDynamicAssembly *assembly, char *assembly_name, char *module_name)
{
	static const guchar entrycode [16] = {0xff, 0x25, 0};
	MonoDynamicImage *image;
	int i;

	const char *version;

	if (!strcmp (mono_get_runtime_info ()->framework_version, "2.1"))
		version = "v2.0.50727"; /* HACK: SL 2 enforces the .net 2 metadata version */
	else
		version = mono_get_runtime_info ()->runtime_version;

	image = g_new0 (MonoDynamicImage, 1);

	MONO_PROFILER_RAISE (image_loading, (&image->image));
	
	/*g_print ("created image %p\n", image);*/
	/* keep in sync with image.c */
	image->image.name = assembly_name;
	image->image.assembly_name = image->image.name; /* they may be different */
	image->image.module_name = module_name;
	image->image.version = g_strdup (version);
	image->image.md_version_major = 1;
	image->image.md_version_minor = 1;
	image->image.dynamic = TRUE;

	image->image.references = g_new0 (MonoAssembly*, 1);
	image->image.references [0] = NULL;

	mono_image_init (&image->image);

	image->method_aux_hash = g_hash_table_new (NULL, NULL);
	image->vararg_aux_hash = g_hash_table_new (NULL, NULL);
	image->handleref = g_hash_table_new (NULL, NULL);
	image->tokens = mono_g_hash_table_new_type_internal (NULL, NULL, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_REFLECTION, NULL, "Reflection Dynamic Image Token Table");
	image->generic_def_objects = mono_g_hash_table_new_type_internal (NULL, NULL, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_REFLECTION, NULL, "Reflection Dynamic Image Generic Definition Table");
	image->typespec = g_hash_table_new ((GHashFunc)mono_metadata_type_hash, (GCompareFunc)mono_metadata_type_equal);
	image->typeref = g_hash_table_new ((GHashFunc)mono_metadata_type_hash, (GCompareFunc)mono_metadata_type_equal);
	image->blob_cache = g_hash_table_new ((GHashFunc)mono_blob_entry_hash, (GCompareFunc)mono_blob_entry_equal);

	/*g_print ("string heap create for image %p (%s)\n", image, module_name);*/
	string_heap_init (&image->sheap);
	mono_dynstream_add_data (&image->us, "", 1);
	mono_dynamic_image_add_to_blob_cached (image, "", 1, NULL, 0);
	/* import tables... */
	mono_dynstream_add_data (&image->code, entrycode, sizeof (entrycode));
	image->iat_offset = mono_dynstream_add_zero (&image->code, 8); /* two IAT entries */
	image->idt_offset = mono_dynstream_add_zero (&image->code, 2 * sizeof (MonoIDT)); /* two IDT entries */
	image->imp_names_offset = mono_dynstream_add_zero (&image->code, 2); /* flags for name entry */
	mono_dynstream_add_data (&image->code, "_CorExeMain", 12);
	mono_dynstream_add_data (&image->code, "mscoree.dll", 12);
	image->ilt_offset = mono_dynstream_add_zero (&image->code, 8); /* two ILT entries */
	mono_dynstream_data_align (&image->code);

	image->cli_header_offset = mono_dynstream_add_zero (&image->code, sizeof (MonoCLIHeader));

	for (i=0; i < MONO_TABLE_NUM; ++i) {
		image->tables [i].next_idx = 1;
		image->tables [i].columns = table_sizes [i];
	}

	image->image.assembly = (MonoAssembly*)assembly;
	image->pe_kind = 0x1; /* ILOnly */
	image->machine = 0x14c; /* I386 */
	
	MONO_PROFILER_RAISE (image_loaded, (&image->image));

	dynamic_images_lock ();

	if (!dynamic_images)
		dynamic_images = g_ptr_array_new ();

	g_ptr_array_add (dynamic_images, image);

	dynamic_images_unlock ();

	return image;
}
#else /* DISABLE_REFLECTION_EMIT */
MonoDynamicImage*
mono_dynamic_image_create (MonoDynamicAssembly *assembly, char *assembly_name, char *module_name)
{
	g_assert_not_reached ();
	return NULL;
}
#endif /* DISABLE_REFLECTION_EMIT */

guint32
mono_dynamic_image_add_to_blob_cached (MonoDynamicImage *assembly, gconstpointer b1, int s1, gconstpointer b2, int s2)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	guint32 idx;
	char *copy;
	gpointer oldkey, oldval;

	copy = (char *)g_malloc (s1+s2);
	memcpy (copy, b1, s1);
	memcpy (copy + s1, b2, s2);
	if (g_hash_table_lookup_extended (assembly->blob_cache, copy, &oldkey, &oldval)) {
		g_free (copy);
		idx = GPOINTER_TO_UINT (oldval);
	} else {
		idx = mono_dynstream_add_data (&assembly->blob, b1, s1);
		mono_dynstream_add_data (&assembly->blob, b2, s2);
		g_hash_table_insert (assembly->blob_cache, copy, GUINT_TO_POINTER (idx));
	}
	return idx;
}

void
mono_dynimage_alloc_table (MonoDynamicTable *table, guint nrows)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	table->rows = nrows;
	g_assert (table->columns);
	if (nrows + 1 >= table->alloc_rows) {
		while (nrows + 1 >= table->alloc_rows) {
			if (table->alloc_rows == 0)
				table->alloc_rows = 16;
			else
				table->alloc_rows *= 2;
		}

		table->values = (guint32 *)g_renew (guint32, table->values, (table->alloc_rows) * table->columns);
	}
}


static void
free_blob_cache_entry (gpointer key, gpointer val, gpointer user_data)
{
	g_free (key);
}

static void
release_hashtable (MonoGHashTable **hash)
{
	if (*hash) {
		mono_g_hash_table_destroy (*hash);
		*hash = NULL;
	}
}

void
mono_dynamic_image_release_gc_roots (MonoDynamicImage *image)
{
	release_hashtable (&image->tokens);
	release_hashtable (&image->generic_def_objects);
}

// Free dynamic image pass one: Free resources but not image itself
void
mono_dynamic_image_free (MonoDynamicImage *image)
{
	MonoDynamicImage *di = image;
	GList *list;
	int i;

	if (di->typespec)
		g_hash_table_destroy (di->typespec);
	if (di->typeref)
		g_hash_table_destroy (di->typeref);
	if (di->handleref)
		g_hash_table_destroy (di->handleref);
	if (di->tokens)
		mono_g_hash_table_destroy (di->tokens);
	if (di->generic_def_objects)
		mono_g_hash_table_destroy (di->generic_def_objects);
	if (di->blob_cache) {
		g_hash_table_foreach (di->blob_cache, free_blob_cache_entry, NULL);
		g_hash_table_destroy (di->blob_cache);
	}
	if (di->standalonesig_cache)
		g_hash_table_destroy (di->standalonesig_cache);
	for (list = di->array_methods; list; list = list->next) {
		ArrayMethod *am = (ArrayMethod *)list->data;
		mono_sre_array_method_free (am);
	}
	g_list_free (di->array_methods);
	if (di->method_aux_hash)
		g_hash_table_destroy (di->method_aux_hash);
	if (di->vararg_aux_hash)
		g_hash_table_destroy (di->vararg_aux_hash);
	g_free (di->strong_name);
	g_free (di->win32_res);
	if (di->public_key)
		g_free (di->public_key);

	/*g_print ("string heap destroy for image %p\n", di);*/
	mono_dynamic_stream_reset (&di->sheap);
	mono_dynamic_stream_reset (&di->code);
	mono_dynamic_stream_reset (&di->resources);
	mono_dynamic_stream_reset (&di->us);
	mono_dynamic_stream_reset (&di->blob);
	mono_dynamic_stream_reset (&di->tstream);
	mono_dynamic_stream_reset (&di->guid);
	for (i = 0; i < MONO_TABLE_NUM; ++i) {
		g_free (di->tables [i].values);
	}

	dynamic_images_lock ();

	if (dynamic_images)
		g_ptr_array_remove (dynamic_images, di);

	dynamic_images_unlock ();
}

// Free dynamic image pass two: Free image itself (might never get called in some debug modes)
void
mono_dynamic_image_free_image (MonoDynamicImage *image)
{
	g_free (image);
}
