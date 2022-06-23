/**
 * \file
 */

#ifndef __MONO_METADATA_UPDATE_H__
#define __MONO_METADATA_UPDATE_H__

#include "mono/utils/mono-forward.h"
#include "mono/utils/bsearch.h"
#include "mono/metadata/loader-internals.h"
#include "mono/metadata/metadata-internals.h"

void
mono_metadata_update_init (void);

enum MonoModifiableAssemblies {
	/* modifiable assemblies are disabled */
	MONO_MODIFIABLE_ASSM_NONE = 0,
	/* assemblies with the Debug flag are modifiable */
	MONO_MODIFIABLE_ASSM_DEBUG = 1,
};

typedef MonoStreamHeader* (*MetadataHeapGetterFunc) (MonoImage*);

gboolean
mono_metadata_update_available (void);

gboolean
mono_metadata_update_enabled (int *modifiable_assemblies_out);

gboolean
mono_metadata_update_no_inline (MonoMethod *caller, MonoMethod *callee);

uint32_t
mono_metadata_update_thread_expose_published (void);

uint32_t
mono_metadata_update_get_thread_generation (void);

void
mono_metadata_update_cleanup_on_close (MonoImage *base_image);

void
mono_metadata_update_image_close_except_pools_all (MonoImage *base_image);

void
mono_metadata_update_image_close_all (MonoImage *base_image);

gpointer
mono_metadata_update_get_updated_method_rva (MonoImage *base_image, uint32_t idx);

gpointer
mono_metadata_update_get_updated_method_ppdb (MonoImage *base_image, uint32_t idx);

gboolean
mono_metadata_update_table_bounds_check (MonoImage *base_image, int table_index, int token_index);

gboolean
mono_metadata_update_delta_heap_lookup (MonoImage *base_image, MetadataHeapGetterFunc get_heap, uint32_t orig_index, MonoImage **image_out, uint32_t *index_out);

void*
mono_metadata_update_metadata_linear_search (MonoImage *base_image, MonoTableInfo *base_table, const void *key, BinarySearchComparer comparer);

MonoMethod*
mono_metadata_update_find_method_by_name (MonoClass *klass, const char *name, int param_count, int flags, MonoError *error);

uint32_t
mono_metadata_update_get_field_idx (MonoClassField *field);

MonoClassField *
mono_metadata_update_get_field (MonoClass *klass, uint32_t fielddef_token);

gboolean
mono_metadata_update_get_typedef_skeleton (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_method_idx, uint32_t *method_count,  uint32_t *first_field_idx, uint32_t *field_count);

gboolean
metadata_update_get_typedef_skeleton_properties (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_prop_idx, uint32_t *prop_count);

gboolean
metadata_update_get_typedef_skeleton_events (MonoImage *base_image, uint32_t typedef_token, uint32_t *first_event_idx, uint32_t *event_count);

gpointer
mono_metadata_update_get_static_field_addr (MonoClassField *field);

MonoMethod *
mono_metadata_update_added_methods_iter (MonoClass *klass, gpointer *iter);

MonoClassField *
mono_metadata_update_added_fields_iter (MonoClass *klass, gboolean lazy, gpointer *iter);

uint32_t
mono_metadata_update_get_num_fields_added (MonoClass *klass);
#endif /*__MONO_METADATA_UPDATE_H__*/
