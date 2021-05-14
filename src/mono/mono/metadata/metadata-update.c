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

#ifdef ENABLE_METADATA_UPDATE

#include <glib.h>
#include "mono/metadata/assembly-internals.h"
#include "mono/metadata/components.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/metadata-update.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/tokentype.h"
#include "mono/utils/mono-coop-mutex.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-lazy-init.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/mono-path.h"

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
mono_image_load_enc_delta (MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, MonoError *error)
{
	mono_component_hot_reload ()->apply_changes (base_image, dmeta, dmeta_len, dil, dil_len, error);
}

#else /* ENABLE_METADATA_UPDATE */

void
mono_metadata_update_init (void)
{
	MonoComponentHotReload *comp = mono_component_hot_reload ();
	comp->set_fastpath_data (NULL);
}

#endif /* ENABLE_METADATA_UPDATE */

