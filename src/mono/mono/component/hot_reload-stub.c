// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <glib.h>
#include "mono/component/component.h"
#include "mono/component/hot_reload.h"
#include "mono/metadata/components.h"
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-error-internals.h"

static bool
hot_reload_stub_available (void);

static void
hot_reload_stub_apply_changes (MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, MonoError *error);

static MonoComponentHotReload *
component_hot_reload_stub_init (void);

static MonoComponentHotReload fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &hot_reload_stub_available },
	&hot_reload_stub_apply_changes,
};

static bool
hot_reload_stub_available (void)
{
	return false;
}

static void
hot_reload_stub_apply_changes (MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, MonoError *error)
{
	mono_error_set_not_supported (error, "Hot reload not supported in this runtime.");
}

static MonoComponentHotReload *
component_hot_reload_stub_init (void)
{
	return &fn_table;
}

MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentHotReload *
mono_component_hot_reload_init (void)
{
	return component_hot_reload_stub_init ();
}
