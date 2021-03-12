// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <glib.h>

#include <mono/component/hot-reload.h>

#include <mono/utils/mono-compiler.h>

MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentHotReload *
mono_component_hot_reload_init (void);

static void
hot_reload_cleanup (void);

static void
hot_reload_apply_changes (MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, MonoError *error);

static MonoComponentHotReload fn_table = {
	{ &hot_reload_cleanup },
	&hot_reload_apply_changes,
};

MonoComponentHotReload *
mono_component_hot_reload_init (void);
{
	/* TODO: initialize things */
	
	return &fn_table;
}

static
hot_reload_cleanup (void)
{
	static gboolean cleaned = FALSE;
	if (cleaned)
		return;

	/* TODO: implement me */

	cleaned = TRUE;
}
