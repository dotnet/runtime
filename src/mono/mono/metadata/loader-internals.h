/**
* \file
*/

#ifndef _MONO_METADATA_LOADER_INTERNALS_H_
#define _MONO_METADATA_LOADER_INTERNALS_H_

#include <glib.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/image.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/mono-conc-hash.h>
#include <mono/metadata/mono-hash.h>
#include <mono/metadata/object-forward.h>
#include <mono/utils/mono-codeman.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/mono-error.h>
#include <mono/utils/mono-forward.h>

#if defined(TARGET_OSX)
#define MONO_LOADER_LIBRARY_NAME "libcoreclr.dylib"
#elif defined(TARGET_ANDROID)
#define MONO_LOADER_LIBRARY_NAME "libmonodroid.so"
#else
#define MONO_LOADER_LIBRARY_NAME "libcoreclr.so"
#endif

G_BEGIN_DECLS

typedef struct _MonoLoadedImages MonoLoadedImages;
typedef struct _MonoAssemblyLoadContext MonoAssemblyLoadContext;
typedef struct _MonoMemoryManager MonoMemoryManager;
typedef struct _MonoSingletonMemoryManager MonoSingletonMemoryManager;
typedef struct _MonoGenericMemoryManager MonoGenericMemoryManager;

struct _MonoBundledSatelliteAssembly {
	const char *name;
	const char *culture;
	const unsigned char *data;
	unsigned int size;
};

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
	// Handle of the corresponding managed object.  If the ALC is
	// collectible, the handle is weak, otherwise it's strong.
	MonoGCHandle gchandle;
	// Whether the ALC can be unloaded; should only be set at creation
	gboolean collectible;
	// Set to TRUE when the unloading process has begun
	gboolean unloading;
	// Used in native-library.c for the hash table below; do not access anywhere else
	MonoCoopMutex pinvoke_lock;
	// Maps malloc-ed char* pinvoke scope -> MonoDl*
	GHashTable *pinvoke_scopes;
};

struct _MonoMemoryManager {
	MonoDomain *domain;
	// Whether the MemoryManager can be unloaded on netcore; should only be set at creation
	gboolean collectible;
	// Whether this is a singleton or generic MemoryManager
	gboolean is_generic;
	// Whether the MemoryManager is in the process of being freed
	gboolean freeing;

	// If taking this with the loader lock, always take this second
	// Currently unused, we take the domain lock instead
	MonoCoopMutex lock;

	MonoMemPool *mp;
	MonoCodeManager *code_mp;

	GPtrArray *class_vtable_array;

	/* Information maintained by mono-debug.c */
	gpointer debug_info;

	/* Information maintained by the execution engine */
	gpointer runtime_info;

	// !!! REGISTERED AS GC ROOTS !!!
	// Hashtables for Reflection handles
	MonoGHashTable *type_hash;
	MonoConcGHashTable *refobject_hash;
	// Maps class -> type initializaiton exception object
	MonoGHashTable *type_init_exception_hash;
	// Maps delegate trampoline addr -> delegate object
	//MonoGHashTable *delegate_hash_table;
	// End of GC roots
};

struct _MonoSingletonMemoryManager {
	MonoMemoryManager memory_manager;

	// Parent ALC, NULL on framework
	MonoAssemblyLoadContext *alc;
};

struct _MonoGenericMemoryManager {
	MonoMemoryManager memory_manager;

	// Parent ALCs
	int n_alcs;
	MonoAssemblyLoadContext **alcs;
};

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

void
mono_set_pinvoke_search_directories (int dir_count, char **dirs);

void
mono_alc_create_default (MonoDomain *domain);

MonoAssemblyLoadContext *
mono_alc_create_individual (MonoDomain *domain, MonoGCHandle this_gchandle, gboolean collectible, MonoError *error);

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

static inline MonoDomain *
mono_alc_domain (MonoAssemblyLoadContext *alc)
{
	return alc->domain;
}

MonoLoadedImages *
mono_alc_get_loaded_images (MonoAssemblyLoadContext *alc);

MONO_API void
mono_loader_save_bundled_library (int fd, uint64_t offset, uint64_t size, const char *destfname);

MonoSingletonMemoryManager *
mono_mem_manager_create_singleton (MonoAssemblyLoadContext *alc, MonoDomain *domain, gboolean collectible);

void
mono_mem_manager_free_singleton (MonoSingletonMemoryManager *memory_manager, gboolean debug_unload);

void
mono_mem_manager_free_objects_singleton (MonoSingletonMemoryManager *memory_manager);

void
mono_mem_manager_lock (MonoMemoryManager *memory_manager);

void
mono_mem_manager_unlock (MonoMemoryManager *memory_manager);

void *
mono_mem_manager_alloc (MonoMemoryManager *memory_manager, guint size);

void *
mono_mem_manager_alloc_nolock (MonoMemoryManager *memory_manager, guint size);

void *
mono_mem_manager_alloc0 (MonoMemoryManager *memory_manager, guint size);

void *
mono_mem_manager_alloc0_nolock (MonoMemoryManager *memory_manager, guint size);

void *
mono_mem_manager_code_reserve (MonoMemoryManager *memory_manager, int size);

#define mono_mem_manager_code_reserve(mem_manager, size) (g_cast (mono_mem_manager_code_reserve ((mem_manager), (size))))

void *
mono_mem_manager_code_reserve_align (MonoMemoryManager *memory_manager, int size, int newsize);

void
mono_mem_manager_code_commit (MonoMemoryManager *memory_manager, void *data, int size, int newsize);

void
mono_mem_manager_code_foreach (MonoMemoryManager *memory_manager, MonoCodeManagerFunc func, void *user_data);

char*
mono_mem_manager_strdup (MonoMemoryManager *memory_manager, const char *s);

void
mono_mem_manager_free_debug_info (MonoMemoryManager *memory_manager);

G_END_DECLS

#endif
