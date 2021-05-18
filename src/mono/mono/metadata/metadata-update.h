/**
 * \file
 */

#ifndef __MONO_METADATA_UPDATE_H__
#define __MONO_METADATA_UPDATE_H__

#include "mono/utils/mono-forward.h"
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

#ifdef ENABLE_METADATA_UPDATE

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

#else /* ENABLE_METADATA_UPDATE */

static inline gboolean
mono_metadata_update_enabled (int *modifiable_assemblies_out)
{
        if (modifiable_assemblies_out)
                *modifiable_assemblies_out = 0;
        return FALSE;
}

static inline gboolean
mono_metadata_update_no_inline (MonoMethod *caller, MonoMethod *callee)
{
        return FALSE;
}

#endif /* ENABLE_METADATA_UPDATE */

#endif /*__MONO_METADATA_UPDATE_H__*/
