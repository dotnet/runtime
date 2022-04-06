// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <glib.h>
#include "mono/component/component.h"
#include "mono/component/hot_reload.h"
#include "mono/metadata/components.h"
#include "mono/metadata/metadata-update.h"
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-error-internals.h"

static bool
hot_reload_stub_available (void);

static void
hot_reload_stub_apply_changes (int origin, MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, gconstpointer dpdb_bytes_orig, uint32_t dpdb_length, MonoError *error);

static MonoComponentHotReload *
component_hot_reload_stub_init (void);

static void
hot_reload_stub_set_fastpath_data (MonoMetadataUpdateData *ptr);

static gboolean
hot_reload_stub_update_enabled (int *modifiable_assemblies_out);

static gboolean
hot_reload_stub_no_inline (MonoMethod *caller, MonoMethod *callee);

static uint32_t
hot_reload_stub_thread_expose_published (void);

static uint32_t
hot_reload_stub_get_thread_generation (void);

static void
hot_reload_stub_cleanup_on_close (MonoImage *image);

static void
hot_reload_stub_effective_table_slow (const MonoTableInfo **t, int idx);

static void
hot_reload_stub_close_except_pools_all (MonoImage *base_image);

static void
hot_reload_stub_close_all (MonoImage *base_image);

static gpointer
hot_reload_stub_get_updated_method_rva (MonoImage *base_image, uint32_t idx);

static gboolean
hot_reload_stub_table_bounds_check (MonoImage *base_image, int table_index, int token_index);

static gboolean
hot_reload_stub_delta_heap_lookup (MonoImage *base_image, MetadataHeapGetterFunc get_heap, uint32_t orig_index, MonoImage **image_out, uint32_t *index_out);

static gpointer
hot_reload_stub_get_updated_method_ppdb (MonoImage *base_image, uint32_t idx);

static gboolean
hot_reload_stub_has_modified_rows (const MonoTableInfo *table);

static int
hot_reload_stub_table_num_rows_slow (MonoImage *image, int table_index);

static uint32_t
hot_reload_stub_method_parent (MonoImage *image, uint32_t method_index);

static void*
hot_reload_stub_metadata_linear_search (MonoImage *base_image, MonoTableInfo *base_table, const void *key, BinarySearchComparer comparer);

static uint32_t
hot_reload_stub_field_parent (MonoImage *image, uint32_t field_index);

static uint32_t
hot_reload_stub_get_field_idx (MonoClassField *field);

static MonoClassField *
hot_reload_stub_get_field (MonoClass *klass, uint32_t fielddef_token);

static gpointer
hot_reload_stub_get_static_field_addr (MonoClassField *field);

static MonoMethod *
hot_reload_stub_find_method_by_name (MonoClass *klass, const char *name, int param_count, int flags, MonoError *error);

static gboolean
hot_reload_stub_get_typedef_skeleton (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_method_idx, uint32_t *method_count,  uint32_t *first_field_idx, uint32_t *field_count);

static gboolean
hot_reload_stub_get_typedef_skeleton_properties (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_prop_idx, uint32_t *prop_count);

static gboolean
hot_reload_stub_get_typedef_skeleton_events (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_event_idx, uint32_t *event_count);

static MonoMethod *
hot_reload_stub_added_methods_iter (MonoClass *klass, gpointer *iter);

static MonoClassField *
hot_reload_stub_added_fields_iter (MonoClass *klass, gboolean lazy, gpointer *iter);

static MonoComponentHotReload fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &hot_reload_stub_available },
	&hot_reload_stub_set_fastpath_data,
	&hot_reload_stub_update_enabled,
	&hot_reload_stub_no_inline,
	&hot_reload_stub_thread_expose_published,
	&hot_reload_stub_get_thread_generation,
	&hot_reload_stub_cleanup_on_close,
	&hot_reload_stub_effective_table_slow,
	&hot_reload_stub_apply_changes,
	&hot_reload_stub_close_except_pools_all,
	&hot_reload_stub_close_all,
	&hot_reload_stub_get_updated_method_rva,
	&hot_reload_stub_table_bounds_check,
	&hot_reload_stub_delta_heap_lookup,
	&hot_reload_stub_get_updated_method_ppdb,
	&hot_reload_stub_has_modified_rows,
	&hot_reload_stub_table_num_rows_slow,
	&hot_reload_stub_method_parent,
	&hot_reload_stub_metadata_linear_search,
	&hot_reload_stub_field_parent,
	&hot_reload_stub_get_field_idx,
	&hot_reload_stub_get_field,
	&hot_reload_stub_get_static_field_addr,
	&hot_reload_stub_find_method_by_name,
	&hot_reload_stub_get_typedef_skeleton,
	&hot_reload_stub_get_typedef_skeleton_properties,
	&hot_reload_stub_get_typedef_skeleton_events,
	&hot_reload_stub_added_methods_iter,
	&hot_reload_stub_added_fields_iter,
};

static bool
hot_reload_stub_available (void)
{
	return false;
}

static MonoComponentHotReload *
component_hot_reload_stub_init (void)
{
	return &fn_table;
}

void
hot_reload_stub_set_fastpath_data (MonoMetadataUpdateData *ptr)
{
}

gboolean
hot_reload_stub_update_enabled (int *modifiable_assemblies_out)
{
	if (modifiable_assemblies_out)
		*modifiable_assemblies_out = MONO_MODIFIABLE_ASSM_NONE;
	return false;
}

static gboolean
hot_reload_stub_no_inline (MonoMethod *caller, MonoMethod *callee)
{
	return false;
}


static uint32_t
hot_reload_stub_thread_expose_published (void)
{
	return 0;
}

uint32_t
hot_reload_stub_get_thread_generation (void)
{
	return 0;
}

void
hot_reload_stub_cleanup_on_close (MonoImage *image)
{
}

void
hot_reload_stub_effective_table_slow (const MonoTableInfo **t, int idx)
{
	g_assert_not_reached ();
}

void
hot_reload_stub_apply_changes (int origin, MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, gconstpointer dpdb_bytes_orig, uint32_t dpdb_length, MonoError *error)
{
	mono_error_set_not_supported (error, "Hot reload not supported in this runtime.");
}

static void
hot_reload_stub_close_except_pools_all (MonoImage *base_image)
{
}

static void
hot_reload_stub_close_all (MonoImage *base_image)
{
}

gpointer
hot_reload_stub_get_updated_method_rva (MonoImage *base_image, uint32_t idx)
{
	g_assert_not_reached ();
}

gboolean
hot_reload_stub_table_bounds_check (MonoImage *base_image, int table_index, int token_index)
{
	g_assert_not_reached ();
}

static gboolean
hot_reload_stub_delta_heap_lookup (MonoImage *base_image, MetadataHeapGetterFunc get_heap, uint32_t orig_index, MonoImage **image_out, uint32_t *index_out)
{
	g_assert_not_reached ();
}

static gpointer
hot_reload_stub_get_updated_method_ppdb (MonoImage *base_image, uint32_t idx)
{
	g_assert_not_reached ();
}

static gboolean
hot_reload_stub_has_modified_rows (const MonoTableInfo *table)
{
	return FALSE;
}

static int
hot_reload_stub_table_num_rows_slow (MonoImage *image, int table_index)
{
	g_assert_not_reached (); /* should always take the fast path */
}

static uint32_t
hot_reload_stub_method_parent (MonoImage *image, uint32_t method_index)
{
	return 0;
}


static void*
hot_reload_stub_metadata_linear_search (MonoImage *base_image, MonoTableInfo *base_table, const void *key, BinarySearchComparer comparer)
{
	return NULL;
}

static uint32_t
hot_reload_stub_field_parent (MonoImage *image, uint32_t field_index)
{
	return 0;
}

static uint32_t
hot_reload_stub_get_field_idx (MonoClassField *field)
{
	return 0;
}

static MonoClassField *
hot_reload_stub_get_field (MonoClass *klass, uint32_t fielddef_token)
{
	return NULL;
}

static gpointer
hot_reload_stub_get_static_field_addr (MonoClassField *field)
{
	return NULL;
}

static MonoMethod *
hot_reload_stub_find_method_by_name (MonoClass *klass, const char *name, int param_count, int flags, MonoError *error)
{
	return NULL;
}

static gboolean
hot_reload_stub_get_typedef_skeleton (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_method_idx, uint32_t *method_count,  uint32_t *first_field_idx, uint32_t *field_count)
{
	return FALSE;
}

static gboolean
hot_reload_stub_get_typedef_skeleton_properties (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_prop_idx, uint32_t *prop_count)
{
	return FALSE;
}

static gboolean
hot_reload_stub_get_typedef_skeleton_events (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_event_idx, uint32_t *event_count)
{
	return FALSE;
}

static MonoMethod *
hot_reload_stub_added_methods_iter (MonoClass *klass, gpointer *iter)
{
	return NULL;
}

static MonoClassField *
hot_reload_stub_added_fields_iter (MonoClass *klass, gboolean lazy, gpointer *iter)
{
	return NULL;
}


MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentHotReload *
mono_component_hot_reload_init (void)
{
	return component_hot_reload_stub_init ();
}
