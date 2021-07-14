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
mono_image_effective_table_slow (const MonoTableInfo **t, int *idx)
{
	mono_component_hot_reload ()->effective_table_slow (t, idx);
}

int
mono_image_relative_delta_index (MonoImage *image_dmeta, int token)
{
	return mono_component_hot_reload ()->relative_delta_index (image_dmeta, token);
}

void
mono_image_load_enc_delta (MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, gconstpointer dpdb, uint32_t dpdb_len, MonoError *error)
{
	mono_component_hot_reload ()->apply_changes (base_image, dmeta, dmeta_len, dil, dil_len, dpdb, dpdb_len, error);
	if (is_ok (error)) {
		mono_component_debugger ()->send_enc_delta (base_image, dmeta, dmeta_len, dpdb, dpdb_len);
	}
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
