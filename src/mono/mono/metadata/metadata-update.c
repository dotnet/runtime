/**
 * \file
 * Routines for publishing metadata updates
 *
 * Copyright 2020 Microsoft
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include "mono/utils/mono-compiler.h"

#include "mono/metadata/metadata-update.h"
#include "mono/metadata/components.h"
#include "mono/metadata/class-internals.h"
#include "mono/component/hot_reload.h"

gboolean
mono_metadata_update_available (void)
{
        return mono_component_hot_reload ()->component.available ();
}

MonoMetadataUpdateData mono_metadata_update_data_private;

void
mono_metadata_update_init (void)
{
	memset (&mono_metadata_update_data_private, 0, sizeof (mono_metadata_update_data_private));
	MonoComponentHotReload *comp = mono_component_hot_reload ();
	comp->set_fastpath_data (&mono_metadata_update_data_private);
}

gboolean
mono_metadata_update_enabled (int *modifiable_assemblies_out)
{
	return mono_component_hot_reload ()->update_enabled (modifiable_assemblies_out);
}

gboolean
mono_metadata_update_no_inline (MonoMethod *caller, MonoMethod *callee)
{
	return mono_component_hot_reload ()->no_inline (caller, callee);
}

uint32_t
mono_metadata_update_thread_expose_published (void)
{
	return mono_component_hot_reload ()->thread_expose_published ();
}

uint32_t
mono_metadata_update_get_thread_generation (void)
{
	return mono_component_hot_reload ()->get_thread_generation ();
}

void
mono_metadata_update_cleanup_on_close (MonoImage *base_image)
{
	mono_component_hot_reload ()->cleanup_on_close (base_image);
}

void
mono_image_effective_table_slow (const MonoTableInfo **t, uint32_t idx)
{
	mono_component_hot_reload ()->effective_table_slow (t, idx);
}

void
mono_image_load_enc_delta (int origin, MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, gconstpointer dpdb, uint32_t dpdb_len, MonoError *error)
{
	mono_component_hot_reload ()->apply_changes (origin, base_image, dmeta, dmeta_len, dil, dil_len, dpdb, dpdb_len, error);
	if (is_ok (error)) {
		mono_component_debugger ()->send_enc_delta (base_image, dmeta, dmeta_len, dpdb, dpdb_len);
	}
}

const char*
mono_enc_capabilities (void)
{
	return mono_component_hot_reload ()->get_capabilities();
}

static void
mono_image_close_except_pools_all_list (GList *images)
{
	for (GList *ptr = images; ptr; ptr = ptr->next) {
		MonoImage *image = (MonoImage *)ptr->data;
		if (image) {
			if (!mono_image_close_except_pools (image))
			    ptr->data = NULL;
		}
	}
}

void
mono_metadata_update_image_close_except_pools_all (MonoImage *base_image)
{
        mono_component_hot_reload ()->image_close_except_pools_all (base_image);
}

void
mono_metadata_update_image_close_all (MonoImage *base_image)
{
        mono_component_hot_reload ()->image_close_all (base_image);
}

gpointer
mono_metadata_update_get_updated_method_rva (MonoImage *base_image, uint32_t idx)
{
        return mono_component_hot_reload ()->get_updated_method_rva (base_image, idx);
}

gpointer
mono_metadata_update_get_updated_method_ppdb (MonoImage *base_image, uint32_t idx)
{
	return mono_component_hot_reload ()->get_updated_method_ppdb (base_image, idx);
}

gboolean
mono_metadata_update_table_bounds_check (MonoImage *base_image, int table_index, int token_index)
{
        return mono_component_hot_reload ()->table_bounds_check (base_image, table_index, token_index);
}

gboolean
mono_metadata_update_delta_heap_lookup (MonoImage *base_image, MetadataHeapGetterFunc get_heap, uint32_t orig_index, MonoImage **image_out, uint32_t *index_out)
{
        return mono_component_hot_reload ()->delta_heap_lookup (base_image, get_heap, orig_index, image_out, index_out);
}


gboolean
mono_metadata_update_has_modified_rows (const MonoTableInfo *table)
{
	return mono_component_hot_reload ()->has_modified_rows (table);
}

gboolean
mono_metadata_has_updates_api (void)
{
        return mono_metadata_has_updates ();
}

/**
 * mono_metadata_table_num_rows:
 *
 * Returns the number of rows from the specified table that the current thread can see.
 * If there's a EnC metadata update, this number may change.
 */
guint32
mono_metadata_table_num_rows_slow (MonoImage *base_image, int table_index)
{
	return mono_component_hot_reload()->table_num_rows_slow (base_image, table_index);
}

void*
mono_metadata_update_metadata_linear_search (MonoImage *base_image, MonoTableInfo *base_table, const void *key, BinarySearchComparer comparer)
{
	return mono_component_hot_reload()->metadata_linear_search (base_image, base_table, key, comparer);
}

/*
 * Returns the (1-based) table row index of the fielddef of the given field
 * (which must have m_field_is_from_update set).
 */
uint32_t
mono_metadata_update_get_field_idx (MonoClassField *field)
{
	return mono_component_hot_reload()->get_field_idx (field);
}

MonoClassField *
mono_metadata_update_get_field (MonoClass *klass, uint32_t fielddef_token)
{
	return mono_component_hot_reload()->get_field (klass, fielddef_token);
}

gpointer
mono_metadata_update_get_static_field_addr (MonoClassField *field)
{
	return mono_component_hot_reload()->get_static_field_addr (field);
}

MonoMethod *
mono_metadata_update_find_method_by_name (MonoClass *klass, const char *name, int param_count, int flags, MonoError *error)
{
	return mono_component_hot_reload()->find_method_by_name (klass, name, param_count, flags, error);
}

gboolean
mono_metadata_update_get_typedef_skeleton (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_method_idx, uint32_t *method_count,  uint32_t *first_field_idx, uint32_t *field_count)
{
	return mono_component_hot_reload()->get_typedef_skeleton (base_image, typedef_token, first_method_idx, method_count, first_field_idx, field_count);
}

gboolean
metadata_update_get_typedef_skeleton_properties (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_prop_idx, uint32_t *prop_count)
{
	return mono_component_hot_reload()->get_typedef_skeleton_properties (base_image, typedef_token, first_prop_idx, prop_count);
}

gboolean
metadata_update_get_typedef_skeleton_events (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_event_idx, uint32_t *event_count)
{
	return mono_component_hot_reload()->get_typedef_skeleton_events (base_image, typedef_token, first_event_idx, event_count);
}

MonoMethod *
mono_metadata_update_added_methods_iter (MonoClass *klass, gpointer *iter)
{
	return mono_component_hot_reload()->added_methods_iter (klass, iter);
}

MonoClassField *
mono_metadata_update_added_fields_iter (MonoClass *klass, gboolean lazy, gpointer *iter)
{
	return mono_component_hot_reload()->added_fields_iter (klass, lazy, iter);
}

uint32_t
mono_metadata_update_get_num_fields_added (MonoClass *klass)
{
	return mono_component_hot_reload()->get_num_fields_added (klass);
}

uint32_t
mono_metadata_update_get_num_methods_added (MonoClass *klass)
{
	return mono_component_hot_reload()->get_num_methods_added (klass);
}