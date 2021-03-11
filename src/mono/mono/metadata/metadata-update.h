/**
 * \file
 */

#ifndef __MONO_METADATA_UPDATE_H__
#define __MONO_METADATA_UPDATE_H__

#include "mono/utils/mono-forward.h"
#include "mono/metadata/loader-internals.h"
#include "mono/metadata/metadata-internals.h"

#ifdef ENABLE_METADATA_UPDATE

enum MonoModifiableAssemblies {
	/* modifiable assemblies are disabled */
	MONO_MODIFIABLE_ASSM_NONE = 0,
	/* assemblies with the Debug flag are modifiable */
	MONO_MODIFIABLE_ASSM_DEBUG = 1,
};

gboolean
mono_metadata_update_enabled (int *modifiable_assemblies_out);

gboolean
mono_metadata_update_no_inline (MonoMethod *caller, MonoMethod *callee);

void
mono_metadata_update_init (void);

void
mono_metadata_update_cleanup (void);

gboolean
mono_metadata_update_available (void);

uint32_t
mono_metadata_update_thread_expose_published (void);

uint32_t
mono_metadata_update_get_thread_generation (void);

gboolean
mono_metadata_wait_for_update (uint32_t timeout_ms);

uint32_t
mono_metadata_update_prepare (void);

void
mono_metadata_update_publish (MonoAssemblyLoadContext *alc, uint32_t generation);

void
mono_metadata_update_cancel (uint32_t generation);

void
mono_metadata_update_cleanup_on_close (MonoImage *base_image);

MonoImage *
mono_table_info_get_base_image (const MonoTableInfo *t);


#endif /* ENABLE_METADATA_UPDATE */

#endif /*__MONO_METADATA_UPDATE_H__*/
