/**
* \file
*/

#ifndef _MONO_METADATA_LOADER_INTERNALS_H_
#define _MONO_METADATA_LOADER_INTERNALS_H_

#include <glib.h>
#include <mono/metadata/object-forward.h>
#include <mono/utils/mono-forward.h>
#include <mono/utils/mono-error.h>

typedef struct _MonoLoadedImages MonoLoadedImages;
typedef struct _MonoAssemblyLoadContext MonoAssemblyLoadContext;

#ifdef ENABLE_NETCORE
/* FIXME: this probably belongs somewhere else */
struct _MonoAssemblyLoadContext {
	MonoDomain *domain;
	MonoLoadedImages *loaded_images;
#if 0
	GSList *loaded_assemblies;
	MonoCoopMutex assemblies_lock;
#endif
};
#endif /* ENABLE_NETCORE */

gpointer
mono_lookup_pinvoke_call_internal (MonoMethod *method, MonoError *error);

#ifdef ENABLE_NETCORE
void
mono_set_pinvoke_search_directories (int dir_count, char **dirs);

void
mono_alc_init (MonoAssemblyLoadContext *alc, MonoDomain *domain);

void
mono_alc_cleanup (MonoAssemblyLoadContext *alc);

static inline MonoDomain *
mono_alc_domain (MonoAssemblyLoadContext *alc)
{
	return alc->domain;
}
#endif /* ENABLE_NETCORE */

MonoLoadedImages *
mono_alc_get_loaded_images (MonoAssemblyLoadContext *alc);

#endif
