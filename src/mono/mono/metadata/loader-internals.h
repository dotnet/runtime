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
#include <mono/metadata/weak-hash.h>
#include <mono/metadata/object-forward.h>
#include <mono/utils/mono-codeman.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/mono-error.h>
#include <mono/utils/mono-forward.h>
#include <mono/utils/mono-conc-hashtable.h>

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

typedef struct {
	/*
	 * indexed by MonoMethodSignature
	 * Protected by the marshal lock
	 */
	GHashTable *delegate_invoke_cache;
	GHashTable *delegate_begin_invoke_cache;
	GHashTable *delegate_end_invoke_cache;
	GHashTable *runtime_invoke_signature_cache;
	GHashTable *runtime_invoke_sig_cache;

	/*
	 * indexed by SignaturePointerPair
	 */
	GHashTable *delegate_abstract_invoke_cache;
	GHashTable *delegate_bound_static_invoke_cache;

	/*
	 * indexed by MonoMethod pointers
	 * Protected by the marshal lock
	 */
	GHashTable *runtime_invoke_method_cache;
	GHashTable *managed_wrapper_cache;

	GHashTable *native_wrapper_cache;
	GHashTable *native_wrapper_aot_cache;
	GHashTable *native_wrapper_check_cache;
	GHashTable *native_wrapper_aot_check_cache;

	GHashTable *native_func_wrapper_aot_cache;
	GHashTable *native_func_wrapper_indirect_cache; /* Indexed by MonoMethodSignature. Protected by the marshal lock */
	GHashTable *synchronized_cache;
	GHashTable *unbox_wrapper_cache;
	GHashTable *cominterop_invoke_cache;
	GHashTable *cominterop_wrapper_cache; /* LOCKING: marshal lock */
	GHashTable *thunk_invoke_cache;
} MonoWrapperCaches;

/* Lock-free allocator */
typedef struct {
	guint8 *mem;
	gpointer prev;
	int size, pos;
} LockFreeMempoolChunk;

typedef struct {
	LockFreeMempoolChunk *current, *chunks;
} LockFreeMempool;

struct _MonoAssemblyLoadContext {
	MonoLoadedImages *loaded_images;
	GSList *loaded_assemblies;
	// If taking this with the domain assemblies_lock, always take this second
	MonoCoopMutex assemblies_lock;
	// Holds ALC-specific memory
	MonoMemoryManager *memory_manager;
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
	// The managed name, owned by this structure
	char *name;
};

struct _MonoMemoryManager {
	// Whether the MemoryManager can be unloaded; should only be set at creation
	gboolean collectible;
	// Whether the MemoryManager is in the process of being freed
	gboolean freeing;

	// If taking this with the loader lock, always take this second
	MonoCoopMutex lock;

	// Private, don't access directly
	MonoMemPool *_mp;
	MonoCodeManager *code_mp;
	LockFreeMempool *lock_free_mp;

	// Protects access to _mp
	// Non-coop, non-recursive
	mono_mutex_t mp_mutex;

	GPtrArray *class_vtable_array;
	GHashTable *generic_virtual_cases;

	/* Used to store offsets of thread static fields */
	GHashTable         *special_static_fields;

	/* Information maintained by mono-debug.c */
	gpointer debug_info;

	/* Information maintained by the execution engine */
	gpointer runtime_info;

	// Handles pointing to the corresponding LoaderAllocator object
	MonoGCHandle loader_allocator_handle;
	MonoGCHandle loader_allocator_weak_handle;

	// Hashtables for Reflection handles
	MonoGHashTable *type_hash;
	MonoConcGHashTable *refobject_hash;
	// Maps class -> type initialization exception object
	MonoGHashTable *type_init_exception_hash;
	// Maps delegate trampoline addr -> delegate object
	//MonoGHashTable *delegate_hash_table;

	/* Same hashes for collectible mem managers */
	MonoWeakHashTable *weak_type_hash;
	MonoWeakHashTable *weak_refobject_hash;
	MonoWeakHashTable *weak_type_init_exception_hash;

	/*
	 * Generic instances and aggregated custom modifiers depend on many alcs, and they need to be deleted if one
	 * of the alcs they depend on is unloaded. For example,
	 * List<Foo> depends on both List's alc and Foo's alc.
	 * A MemoryManager is the owner of all generic instances depending on the same set of
	 * alcs.
	 */
	// Parent ALCs
	int n_alcs;
	// Allocated from the mempool
	MonoAssemblyLoadContext **alcs;

	// Generic-specific caches
	GHashTable *ginst_cache, *gmethod_cache, *gsignature_cache;
	MonoConcurrentHashTable *gclass_cache;

	/* mirror caches of ones already on MonoImage. These ones contain generics */
	GHashTable *szarray_cache, *array_cache, *ptr_cache;

	MonoWrapperCaches wrapper_caches;

	GHashTable *aggregate_modifiers_cache;

	/* Indexed by MonoGenericParam pointers */
	GHashTable **gshared_types;
	/* The length of the above array */
	int gshared_types_len;
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
mono_set_pinvoke_search_directories (int dir_count, char **dirs);

void
mono_alcs_init (void);

void
mono_alc_create_default (MonoDomain *domain);

MonoAssemblyLoadContext *
mono_alc_create_individual (MonoGCHandle this_gchandle, gboolean collectible, MonoError *error);

void
mono_alc_assemblies_lock (MonoAssemblyLoadContext *alc);

void
mono_alc_assemblies_unlock (MonoAssemblyLoadContext *alc);

/*
 * This is below the loader lock in the locking hierarcy,
 * so when taking this with the loader lock, always take
 * this second.
 */
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

MONO_COMPONENT_API
MonoAssemblyLoadContext *
mono_alc_get_default (void);

static inline
MonoAssemblyLoadContext *
mono_alc_get_ambient (void)
{
	/*
	 * FIXME: All the callers of mono_alc_get_ambient () should get an ALC
	 * passed to them from their callers.
	 */
	return mono_alc_get_default ();
}

static inline MonoGCHandle
mono_alc_get_gchandle_for_resolving (MonoAssemblyLoadContext *alc)
{
	/* for the default ALC, pass NULL to ask for the Default ALC - see
	 * AssemblyLoadContext.GetAssemblyLoadContext(IntPtr gchManagedAssemblyLoadContext) - which
	 * will create the managed ALC object if it hasn't been created yet
	 */
	if (alc->gchandle == mono_alc_get_default ()->gchandle)
		return NULL;
	else
		return GUINT_TO_POINTER (alc->gchandle);
}

MonoAssemblyLoadContext *
mono_alc_from_gchandle (MonoGCHandle alc_gchandle);

MonoLoadedImages *
mono_alc_get_loaded_images (MonoAssemblyLoadContext *alc);

void
mono_alc_add_assembly (MonoAssemblyLoadContext *alc, MonoAssembly *ass);

MonoAssembly*
mono_alc_find_assembly (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname);

MONO_COMPONENT_API GPtrArray*
mono_alc_get_all_loaded_assemblies (void);

GPtrArray*
mono_alc_get_all (void);

MONO_API void
mono_loader_save_bundled_library (int fd, uint64_t offset, uint64_t size, const char *destfname);

MonoMemoryManager *
mono_mem_manager_new (MonoAssemblyLoadContext **alcs, int nalcs, gboolean collectible);

void
mono_mem_manager_free (MonoMemoryManager *memory_manager, gboolean debug_unload);

void
mono_mem_manager_free_objects (MonoMemoryManager *memory_manager);

MONO_COMPONENT_API void
mono_mem_manager_lock (MonoMemoryManager *memory_manager);

MONO_COMPONENT_API void
mono_mem_manager_unlock (MonoMemoryManager *memory_manager);

MONO_COMPONENT_API
void *
mono_mem_manager_alloc (MonoMemoryManager *memory_manager, guint size);

void *
mono_mem_manager_alloc0 (MonoMemoryManager *memory_manager, guint size);

gpointer
mono_mem_manager_alloc0_lock_free (MonoMemoryManager *memory_manager, guint size);

#define mono_mem_manager_alloc0_lock_free(memory_manager, size) (g_cast (mono_mem_manager_alloc0_lock_free ((memory_manager), (size))))

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

gboolean
mono_mem_manager_mp_contains_addr (MonoMemoryManager *memory_manager, gpointer addr);

MonoMemoryManager *
mono_mem_manager_get_generic (MonoImage **images, int nimages);

MonoMemoryManager*
mono_mem_manager_merge (MonoMemoryManager *mm1, MonoMemoryManager *mm2);

static inline GSList*
g_slist_prepend_mem_manager (MonoMemoryManager *memory_manager, GSList *list, gpointer data)
{
	GSList *new_list;

	new_list = (GSList *) mono_mem_manager_alloc (memory_manager, sizeof (GSList));
	new_list->data = data;
	new_list->next = list;

	return new_list;
}

MonoGCHandle
mono_mem_manager_get_loader_alloc (MonoMemoryManager *mem_manager);

void
mono_mem_manager_init_reflection_hashes (MonoMemoryManager *mem_manager);

void
mono_mem_manager_start_unload (MonoMemoryManager *mem_manager);

G_END_DECLS

#endif
