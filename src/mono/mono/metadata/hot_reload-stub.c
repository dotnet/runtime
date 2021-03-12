// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <glib.h>
#include "mono/component/component.h"
#include "mono/component/hot_reload.h"
#include "mono/utils/mono-error-internals.h"

static MonoComponentHotReload fn_table = {
	{ &hot_reload_stub_cleanup },
	&hot_reload_stub_apply_changes,
};

MonoComponentHotReload *
mono_component_hot_reload_stub_init (void)
{
	return &fn_table;
}

void
hot_reload_stub_cleanup (void)
{
}

void
hot_reload_stub_apply_changes (MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, MonoError *error)
{
	mono_error_set_not_supported (error, "Hot reload not supported in this runtime.");
}
