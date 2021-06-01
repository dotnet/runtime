// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_HOT_RELOAD_H
#define _MONO_COMPONENT_HOT_RELOAD_H

#include <glib.h>
#include "mono/metadata/object-forward.h"
#include "mono/utils/mono-error.h"
#include "mono/utils/mono-compiler.h"
#include "mono/component/component.h"

typedef struct _MonoComponentHotReload {
	MonoComponent component;
	void (*apply_changes) (MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, MonoError *error);
} MonoComponentHotReload;

#ifdef STATIC_COMPONENTS
MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentHotReload *
mono_component_hot_reload_init (void);
#endif

#endif/*_MONO_COMPONENT_HOT_RELOAD_H*/
