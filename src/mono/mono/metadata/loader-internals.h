/**
* \file
*/

#ifndef _MONO_METADATA_LOADER_INTERNALS_H_
#define _MONO_METADATA_LOADER_INTERNALS_H_

#include <glib.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/image.h>
#include <mono/metadata/object-forward.h>
#include <mono/utils/mono-forward.h>
#include <mono/utils/mono-error.h>
#include <mono/utils/mono-coop-mutex.h>

#ifdef ENABLE_NETCORE
#if defined(TARGET_OSX)
#define MONO_LOADER_LIBRARY_NAME "libcoreclr.dylib"
#elif defined(TARGET_ANDROID)
#define MONO_LOADER_LIBRARY_NAME "libruntime-android.so"
#else
#define MONO_LOADER_LIBRARY_NAME "libcoreclr.so"
#endif
#endif

typedef struct _MonoLoadedImages MonoLoadedImages;
typedef struct _MonoAssemblyLoadContext MonoAssemblyLoadContext;

#ifndef DISABLE_DLLMAP
typedef struct _MonoDllMap MonoDllMap;
struct _MonoDllMap {
	char *dll;
	char *target;
	char *func;
	char *target_func;
	MonoDllMap *next;
};
#endif

#ifdef ENABLE_NETCORE
/* FIXME: this probably belongs somewhere else */
struct _MonoAssemblyLoadContext {
	MonoDomain *domain;
	MonoLoadedImages *loaded_images;
	GSList *loaded_assemblies;
	// If taking this with the domain assemblies_lock, always take this second
	MonoCoopMutex assemblies_lock;
	/* Handle of the corresponding managed object.  If the ALC is
	 * collectible, the handle is weak, otherwise it's strong.
	 */
	MonoGCHandle gchandle;
	// Whether the ALC can be unloaded; should only be set at creation
	gboolean collectible;
	// Set to TRUE when the unloading process has begun, ensures nothing else will use that ALC
	// Maybe remove this? for now, should be helpful for debugging
	// Alternatively, check for it in the various ALC functions and error if it's true when calling them
	gboolean unloading;
	// Used in native-library.c for the hash table below; do not access anywhere else
	MonoCoopMutex pinvoke_lock;
	// Maps malloc-ed char* pinvoke scope -> MonoDl*
	GHashTable *pinvoke_scopes;
};
#endif /* ENABLE_NETCORE */

void
mono_global_loader_data_lock (void);

void
mono_global_loader_data_unlock (void);

gpointer
mono_lookup_pinvoke_call_internal (MonoMethod *method, MonoError *error);

#ifndef DISABLE_DLLMAP
void
mono_dllmap_insert_internal (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc);

void
mono_global_dllmap_cleanup (void);
#endif

void
mono_global_loader_cache_init (void);

void
mono_global_loader_cache_cleanup (void);

#ifdef ENABLE_NETCORE
void
mono_set_pinvoke_search_directories (int dir_count, char **dirs);

void
mono_alc_init (MonoAssemblyLoadContext *alc, MonoDomain *domain, gboolean collectible);

void
mono_alc_cleanup (MonoAssemblyLoadContext *alc);

void
mono_alc_assemblies_lock (MonoAssemblyLoadContext *alc);

void
mono_alc_assemblies_unlock (MonoAssemblyLoadContext *alc);

gboolean
mono_alc_is_default (MonoAssemblyLoadContext *alc);

MonoAssembly*
mono_alc_invoke_resolve_using_load_nofail (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname);

MonoAssembly*
mono_alc_invoke_resolve_using_resolving_event_nofail (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname);

MonoAssembly*
mono_alc_invoke_resolve_using_resolve_satellite_nofail (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname);

MonoAssemblyLoadContext *
mono_alc_from_gchandle (MonoGCHandle alc_gchandle);

#endif /* ENABLE_NETCORE */

static inline MonoDomain *
mono_alc_domain (MonoAssemblyLoadContext *alc)
{
#ifdef ENABLE_NETCORE
	return alc->domain;
#else
	return mono_domain_get ();
#endif
}

MonoLoadedImages *
mono_alc_get_loaded_images (MonoAssemblyLoadContext *alc);

MONO_API void
mono_loader_save_bundled_library (int fd, uint64_t offset, uint64_t size, const char *destfname);

#endif
