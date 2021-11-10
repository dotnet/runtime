// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_HOT_RELOAD_H
#define _MONO_COMPONENT_HOT_RELOAD_H

#include <glib.h>
#include "mono/metadata/object-forward.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/metadata-update.h"
#include "mono/utils/mono-error.h"
#include "mono/utils/mono-compiler.h"
#include "mono/component/component.h"

typedef struct _MonoComponentHotReload {
	MonoComponent component;
	void (*set_fastpath_data) (MonoMetadataUpdateData *data);
	gboolean (*update_enabled) (int *modifiable_assemblies_out);
	gboolean (*no_inline) (MonoMethod *caller, MonoMethod *callee);
	uint32_t (*thread_expose_published) (void);
	uint32_t (*get_thread_generation) (void);
	void (*cleanup_on_close) (MonoImage *image);
	void (*effective_table_slow) (const MonoTableInfo **t, int *idx);
	int (*relative_delta_index) (MonoImage *image_dmeta, int token);
	void (*apply_changes) (int origin, MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, gconstpointer dpdb_bytes_orig, uint32_t dpdb_length, MonoError *error);
	void (*image_close_except_pools_all) (MonoImage *base_image);
	void (*image_close_all) (MonoImage *base_image);
	gpointer (*get_updated_method_rva) (MonoImage *base_image, uint32_t idx);
	gboolean (*table_bounds_check) (MonoImage *base_image, int table_index, int token_index);
	gboolean (*delta_heap_lookup) (MonoImage *base_image, MetadataHeapGetterFunc get_heap, uint32_t orig_index, MonoImage **image_out, uint32_t *index_out);
	gpointer (*get_updated_method_ppdb) (MonoImage *base_image, uint32_t idx);
	gboolean (*has_modified_rows) (const MonoTableInfo *table);
} MonoComponentHotReload;

MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentHotReload *
mono_component_hot_reload_init (void);

#endif/*_MONO_COMPONENT_HOT_RELOAD_H*/
