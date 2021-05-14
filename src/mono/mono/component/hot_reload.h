// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_HOT_RELOAD_H
#define _MONO_COMPONENT_HOT_RELOAD_H

#include <glib.h>
#include "mono/metadata/object-forward.h"
#include "mono/metadata/metadata-internals.h"
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
	void (*apply_changes) (MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, MonoError *error);
} MonoComponentHotReload;

MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentHotReload *
mono_component_hot_reload_init (void);

#endif/*_MONO_COMPONENT_HOT_RELOAD_H*/
