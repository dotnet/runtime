// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <glib.h>

#include <mono/component/hot_reload.h>

#include <mono/utils/mono-compiler.h>

#ifndef STATIC_COMPONENTS
MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentHotReload *
mono_component_hot_reload_init (void);
#endif

static bool
hot_reload_available (void);

static void
hot_reload_apply_changes (MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, MonoError *error);

static MonoComponentHotReload fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &hot_reload_available },
	&hot_reload_apply_changes,
};

MonoComponentHotReload *
mono_component_hot_reload_init (void)
{
	/* TODO: implement me */
	return &fn_table;
}

static bool
hot_reload_available (void)
{
	return true;
}

static void
hot_reload_apply_changes (MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, MonoError *error)
{
	/* TODO: implement me */
}
