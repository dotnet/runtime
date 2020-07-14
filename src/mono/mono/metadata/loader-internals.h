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
#include <mono/utils/mono-codeman.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/mono-hash.h>
#include <mono/metadata/mono-conc-hash.h>

#ifdef ENABLE_NETCORE
#if defined(TARGET_OSX)
#define MONO_LOADER_LIBRARY_NAME "libcoreclr.dylib"
#elif defined(TARGET_ANDROID)
#define MONO_LOADER_LIBRARY_NAME "libmonodroid.so"
#else
#define MONO_LOADER_LIBRARY_NAME "libcoreclr.so"
#endif
#endif

typedef struct _MonoLoadedImages MonoLoadedImages;
typedef struct _MonoAssemblyLoadContext MonoAssemblyLoadContext;
typedef struct _MonoMemoryManager MonoMemoryManager;
typedef struct _MonoSingletonMemoryManager MonoSingletonMemoryManager;
typedef struct _MonoGenericMemoryManager MonoGenericMemoryManager;

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
struct _MonoAssemblyLoadContext {
	MonoDomain *domain;
	MonoLoadedImages *loaded_images;
	GSList *loaded_assemblies;
	// If taking this with the domain assemblies_lock, always take this second
	MonoCoopMutex assemblies_lock;
	// Holds ALC-specific memory
	MonoSingletonMemoryManager *memory_manager;
	GPtrArray *generic_memory_managers;
	// Protects generic_memory_managers; if taking this with the domain alcs_lock, always take this second
	MonoCoopMutex memory_managers_lock;
	// Handle of the corresponding managed object
	// If the ALC is collectible, the handle is weak, otherwise it's strong
	MonoGCHandle gchandle;
	// Handle of the corresponding managed ReferenceTracker, only set if collectible
	MonoGCHandle ref_tracker;
	// Whether the ALC can be unloaded; should only be set at creation
	gboolean collectible;
	// Set to TRUE when the unloading process has begun
	gboolean unloading;
	// Used in native-library.c for the hash table below; do not access anywhere else
	MonoCoopMutex pinvoke_lock;
	// Maps malloc-ed char* pinvoke scope -> MonoDl*
	GHashTable *pinvoke_scopes;
};
#endif /* ENABLE_NETCORE */

// Add comment about type punning
struct _MonoMemoryManager {
	// Whether the MemoryManager can be unloaded on netcore; should only be set at creation
	gboolean collectible;
	// Whether this is a singleton or generic memory manager
	gboolean is_generic;
	// Whether the MemoryManager is in the process of being free'd
	gboolean freeing;

	// Entries moved over from the domain:

	// If taking this with the loader lock, always take this second
	// On legacy, this does *not* protect mp/code_mp, which are covered by the domain lock
	MonoCoopMutex lock;

	MonoMemPool *mp;
	MonoCodeManager *code_mp;

	GPtrArray *class_vtable_array;

	// !!! REGISTERED AS GC ROOTS !!!
	// Hashtables for Reflection handles
	MonoGHashTable *type_hash;
	MonoConcGHashTable *refobject_hash;
	// Maps class -> type initialization exception object
	MonoGHashTable *type_init_exception_hash;
	// Maps delegate trampoline addr -> delegate object
	//MonoGHashTable *delegate_hash_table;
	// End of gc roots

	// This must be a GHashTable, since these objects can't be finalized
	// if the hashtable contains a GC visible reference to them.
	GHashTable *finalizable_objects_hash;
	mono_mutex_t finalizable_objects_hash_lock; // TODO: must this be an os lock?
};

struct _MonoSingletonMemoryManager {
	MonoMemoryManager memory_manager;

	// Parent ALC
	MonoAssemblyLoadContext *alc;
};

#ifdef ENABLE_NETCORE
struct _MonoGenericMemoryManager {
	MonoMemoryManager memory_manager;

	// Parent ALCs
	int n_alcs;
	MonoAssemblyLoadContext **alcs;
};
#endif

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

void
mono_alc_memory_managers_lock (MonoAssemblyLoadContext *alc);

void
mono_alc_memory_managers_unlock (MonoAssemblyLoadContext *alc);

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

MonoSingletonMemoryManager *
mono_memory_manager_create_singleton (MonoAssemblyLoadContext *alc, gboolean collectible);

void
mono_memory_manager_free_singleton (MonoSingletonMemoryManager *memory_manager, gboolean debug_unload);

#ifdef ENABLE_NETCORE
MonoGenericMemoryManager *
mono_memory_manager_get_generic (MonoAssemblyLoadContext **alcs, int n_alcs);

void
mono_memory_manager_free_generic (MonoGenericMemoryManager *memory_manager, gboolean debug_unload);
#endif

static inline void
mono_memory_manager_lock (MonoMemoryManager *memory_manager)
{
	mono_coop_mutex_lock (&memory_manager->lock);
}

static inline void
mono_memory_manager_unlock (MonoMemoryManager *memory_manager)
{
	mono_coop_mutex_unlock (&memory_manager->lock);
}

MonoMemoryManager *
mono_memory_manager_from_class (MonoDomain *domain, MonoClass *klass);

MonoMemoryManager *
mono_memory_manager_from_method (MonoDomain *domain, MonoMethod *method);

gpointer
mono_memory_manager_alloc (MonoMemoryManager *memory_manager, guint size);

gpointer
mono_memory_manager_alloc0 (MonoMemoryManager *memory_manager, guint size);

void*
mono_memory_manager_code_reserve (MonoMemoryManager *memory_manager, int size);

void*
mono_memory_manager_code_reserve_align (MonoMemoryManager *memory_manager, int size, int alignment);

void
mono_memory_manager_code_commit (MonoMemoryManager *memory_manager, void *data, int size, int newsize);

void
mono_memory_manager_code_foreach (MonoMemoryManager *memory_manager, MonoCodeManagerFunc func, void *user_data);

// Uses the domain on legacy and the method's MemoryManager on netcore
void *
mono_method_alloc_code (MonoDomain *domain, MonoMethod *method, int size);

void *
mono_method_alloc0_code (MonoDomain *domain, MonoMethod *method, int size);

// Uses the domain on legacy and the method's MemoryManager on netcore
void *
mono_class_alloc_code (MonoDomain *domain, MonoClass *klass, int size);

void *
mono_class_alloc0_code (MonoDomain *domain, MonoClass *klass, int size);
#endif
