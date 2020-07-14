#include <mono/metadata/loader-internals.h>
#include <mono/metadata/reflection-cache.h>
#include <mono/metadata/mono-hash-internals.h>

static void
memory_manager_init (MonoMemoryManager *memory_manager, gboolean collectible)
{
	MonoDomain *domain = mono_domain_get (); // this is quite possibly wrong on legacy?

	memory_manager->freeing = FALSE;

	mono_coop_mutex_init_recursive (&memory_manager->lock);

	memory_manager->mp = mono_mempool_new ();
	memory_manager->code_mp = mono_code_manager_new ();

	memory_manager->class_vtable_array = g_ptr_array_new ();

	// TODO: make these not linked to the domain for debugging
	memory_manager->type_hash = mono_g_hash_table_new_type_internal ((GHashFunc)mono_metadata_type_hash, (GCompareFunc)mono_metadata_type_equal, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Reflection Type Table");
	memory_manager->refobject_hash = mono_conc_g_hash_table_new_type (mono_reflected_hash, mono_reflected_equal, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Reflection Object Table");
	memory_manager->type_init_exception_hash = mono_g_hash_table_new_type_internal (mono_aligned_addr_hash, NULL, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Type Initialization Exception Table");

	memory_manager->finalizable_objects_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	mono_os_mutex_init_recursive (&memory_manager->finalizable_objects_hash_lock);
}

MonoSingletonMemoryManager *
mono_memory_manager_create_singleton (MonoAssemblyLoadContext *alc, gboolean collectible)
{
	MonoSingletonMemoryManager *mem_manager = g_new0 (MonoSingletonMemoryManager, 1);
	memory_manager_init ((MonoMemoryManager *)mem_manager, collectible);

	mem_manager->memory_manager.is_generic = FALSE;
	mem_manager->alc = alc;

	return mem_manager;
}

#ifdef ENABLE_NETCORE
static MonoGenericMemoryManager *
memory_manager_create_generic (MonoAssemblyLoadContext **alcs, int n_alcs)
{
	gboolean collectible = FALSE;

	MonoGenericMemoryManager *mem_manager = g_new0 (MonoGenericMemoryManager, 1);
	mem_manager->memory_manager.is_generic = TRUE;

	for (int i = 0; i < n_alcs; i++) {
		MonoAssemblyLoadContext *alc = alcs [i];
		g_assert (!alc->unloading);
		if (alc->collectible) {
			collectible = TRUE;
			break;
		}
	}

	memory_manager_init ((MonoMemoryManager *)mem_manager, collectible);
	mem_manager->n_alcs = n_alcs;
	mem_manager->alcs = (MonoAssemblyLoadContext **)g_new (MonoAssemblyLoadContext *, n_alcs);
	memcpy (mem_manager->alcs, alcs, n_alcs * sizeof (MonoAssemblyLoadContext *));

	for (int i = 0; i < n_alcs; i++) {
		MonoAssemblyLoadContext *alc = alcs [i];
		mono_alc_memory_managers_lock (alc);
		g_ptr_array_add (alc->generic_memory_managers, mem_manager);
		mono_alc_memory_managers_unlock (alc);
	}

	return mem_manager;
}

static gboolean
compare_memory_manager (MonoGenericMemoryManager *memory_manager, MonoAssemblyLoadContext **alcs, int n_alcs)
{
	int i, j;

	if (memory_manager->n_alcs != n_alcs)
		return FALSE;

	for (i = 0; i < n_alcs; i++) {
		for (j = 0; j < n_alcs; j++) {
			if (memory_manager->alcs [j] == alcs [i])
				break; // break on match
		}

		if (j == n_alcs)
			break; // break on not found
	}

	// If we made it all the way through, all items were found
	return i == n_alcs;
}

MonoGenericMemoryManager *
mono_memory_manager_get_generic (MonoAssemblyLoadContext **alcs, int n_alcs)
{
	g_assert (n_alcs > 0);
	g_assert (alcs [0]);

	// TODO: consider a cache

	// TODO: maybe we need a global lock here

	// Look through first ALC's memory managers for a match
	MonoAssemblyLoadContext *alc = alcs [0];

	mono_alc_memory_managers_lock (alc);

	GPtrArray *generic_mms = alc->generic_memory_managers;
	for (int i = 0; i < generic_mms->len; i++) {
		MonoGenericMemoryManager *test_memory_manager = (MonoGenericMemoryManager *)generic_mms->pdata [i];
		if (compare_memory_manager (test_memory_manager, alcs, n_alcs)) {
			mono_alc_memory_managers_unlock (alc);
			return test_memory_manager;
		}
	}

	mono_alc_memory_managers_unlock (alc);

	// Not found, create it
	return memory_manager_create_generic (alcs, n_alcs);
}
#endif

static void
cleanup_refobject_hash (gpointer key, gpointer value, gpointer user_data)
{
	free_reflected_entry ((ReflectedEntry *)key);
}

static void
unregister_vtable_reflection_type (MonoVTable *vtable)
{
	MonoObject *type = (MonoObject *)vtable->type;

	if (type->vtable->klass != mono_defaults.runtimetype_class)
		MONO_GC_UNREGISTER_ROOT_IF_MOVING (vtable->type);
}

static void
memory_manager_delete (MonoMemoryManager *memory_manager, gboolean debug_unload)
{
	// Scan here to assert no lingering references in vtables?

	mono_coop_mutex_destroy (&memory_manager->lock);

	if (debug_unload) {
		mono_mempool_invalidate (memory_manager->mp);
		mono_code_manager_invalidate (memory_manager->code_mp);
	} else {
#ifndef DISABLE_PERFCOUNTERS
		/* FIXME: use an explicit subtraction method as soon as it's available */
		mono_atomic_fetch_add_i32 (&mono_perfcounters->loader_bytes, -1 * mono_mempool_get_allocated (memory_manager->mp));
#endif
		mono_mempool_destroy (memory_manager->mp);
		memory_manager->mp = NULL;
		mono_code_manager_destroy (memory_manager->code_mp);
		memory_manager->code_mp = NULL;
	}

	g_ptr_array_free (memory_manager->class_vtable_array, TRUE);
	memory_manager->class_vtable_array = NULL;

	// Must be done before type_hash is freed
	for (int i = 0; i < memory_manager->class_vtable_array->len; i++)
		unregister_vtable_reflection_type ((MonoVTable *)g_ptr_array_index (memory_manager->class_vtable_array, i));

	mono_g_hash_table_destroy (memory_manager->type_hash);
	memory_manager->type_hash = NULL;
	mono_conc_g_hash_table_foreach (memory_manager->refobject_hash, cleanup_refobject_hash, NULL);
	mono_conc_g_hash_table_destroy (memory_manager->refobject_hash);
	memory_manager->refobject_hash = NULL;
	mono_g_hash_table_destroy (memory_manager->type_init_exception_hash);
	memory_manager->type_init_exception_hash = NULL;

	g_hash_table_destroy (memory_manager->finalizable_objects_hash);
	memory_manager->finalizable_objects_hash = NULL;
	mono_os_mutex_destroy (&memory_manager->finalizable_objects_hash_lock);
}

void
mono_memory_manager_free_singleton (MonoSingletonMemoryManager *memory_manager, gboolean debug_unload)
{
	g_assert (!memory_manager->memory_manager.is_generic);

	memory_manager_delete ((MonoMemoryManager *)memory_manager, debug_unload);
	g_free (memory_manager);
}

#ifdef ENABLE_NETCORE
void
mono_memory_manager_free_generic (MonoGenericMemoryManager *memory_manager, gboolean debug_unload)
{
	g_assert (memory_manager->memory_manager.is_generic);

	// This should be an atomic read/write, but I'm sure it's fine
	if (memory_manager->memory_manager.freeing)
		return;

	memory_manager->memory_manager.freeing = TRUE;

	// Remove from composing ALCs
	for (int i = 0; i < memory_manager->n_alcs; i++) {
		MonoAssemblyLoadContext *alc = memory_manager->alcs [i];
		mono_alc_memory_managers_lock (alc);
		g_ptr_array_remove (alc->generic_memory_managers, memory_manager);
		mono_alc_memory_managers_unlock (alc);
	}
	g_free (memory_manager->alcs);

	memory_manager_delete ((MonoMemoryManager *)memory_manager, debug_unload);
	g_free (memory_manager);
}
#endif

gpointer
mono_memory_manager_alloc (MonoMemoryManager *memory_manager, guint size)
{
	gpointer res;

	mono_memory_manager_lock (memory_manager);
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_fetch_add_i32 (&mono_perfcounters->loader_bytes, size);
#endif
	res = mono_mempool_alloc (memory_manager->mp, size);
	mono_memory_manager_unlock (memory_manager);

	return res;
}

gpointer
mono_memory_manager_alloc0 (MonoMemoryManager *memory_manager, guint size)
{
	gpointer res;

	mono_memory_manager_lock (memory_manager);
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_fetch_add_i32 (&mono_perfcounters->loader_bytes, size);
#endif
	res = mono_mempool_alloc0 (memory_manager->mp, size);
	mono_memory_manager_unlock (memory_manager);

	return res;
}

void*
mono_memory_manager_code_reserve (MonoMemoryManager *memory_manager, int size)
{
	gpointer res;

	mono_memory_manager_lock (memory_manager);
	res = mono_code_manager_reserve (memory_manager->code_mp, size);
	mono_memory_manager_unlock (memory_manager);

	return res;
}

void*
mono_memory_manager_code_reserve_align (MonoMemoryManager *memory_manager, int size, int alignment)
{
	gpointer res;

	mono_memory_manager_lock (memory_manager);
	res = mono_code_manager_reserve_align (memory_manager->code_mp, size, alignment);
	mono_memory_manager_unlock (memory_manager);

	return res;
}

void
mono_memory_manager_code_commit (MonoMemoryManager *memory_manager, void *data, int size, int newsize)
{
	mono_memory_manager_lock (memory_manager);
	mono_code_manager_commit (memory_manager->code_mp, data, size, newsize);
	mono_memory_manager_unlock (memory_manager);
}

void
mono_memory_manager_code_foreach (MonoMemoryManager *memory_manager, MonoCodeManagerFunc func, void *user_data)
{
	mono_memory_manager_lock (memory_manager);
	mono_code_manager_foreach (memory_manager->code_mp, func, user_data);
	mono_memory_manager_unlock (memory_manager);
}

void *
mono_method_alloc_code (MonoDomain *domain, MonoMethod *method, int size)
{
	MonoMemoryManager *memory_manager = mono_memory_manager_from_method (domain, method);
	return mono_memory_manager_alloc (memory_manager, size);
}

void *
mono_method_alloc0_code (MonoDomain *domain, MonoMethod *method, int size)
{
	MonoMemoryManager *memory_manager = mono_memory_manager_from_method (domain, method);
	return mono_memory_manager_alloc0 (memory_manager, size);
}

void *
mono_class_alloc_code (MonoDomain *domain, MonoClass *klass, int size)
{
	MonoMemoryManager *memory_manager = mono_memory_manager_from_class (domain, klass);
	return mono_memory_manager_alloc (memory_manager, size);
}

void *
mono_class_alloc0_code (MonoDomain *domain, MonoClass *klass, int size)
{
	MonoMemoryManager *memory_manager = mono_memory_manager_from_class (domain, klass);
	return mono_memory_manager_alloc0 (memory_manager, size);
}

static MonoAssemblyLoadContext **
get_alcs_from_image_set (MonoImageSet *set, int *n_alcs)
{
	GPtrArray *alcs_dym = g_ptr_array_new ();
	for (int i = 0; i < set->nimages; i++) {
		MonoImage *image = set->images [i];
		MonoAssemblyLoadContext *alc = image->alc;
		if (alc && !g_ptr_array_find (alcs_dym, alc, NULL))
			g_ptr_array_add (alcs_dym, alc);
	}
	MonoAssemblyLoadContext **alcs = (MonoAssemblyLoadContext **)alcs_dym->pdata;
	*n_alcs = alcs_dym->len;
	g_ptr_array_free (alcs_dym, FALSE);
	return alcs;
}

static MonoMemoryManager *
memory_manager_from_set (MonoImageSet *set)
{
	if (set->nimages == 1)
		return (MonoMemoryManager *)set->images [0]->alc->memory_manager;

	int n_alcs;
	MonoAssemblyLoadContext **alcs = get_alcs_from_image_set (set, &n_alcs);
	MonoMemoryManager *memory_manager = (MonoMemoryManager *)mono_memory_manager_get_generic (alcs, n_alcs);
	g_free (alcs);
	return memory_manager;
}

MonoMemoryManager *
mono_memory_manager_from_class (MonoDomain *domain, MonoClass *klass)
{
#if defined(ENABLE_NETCORE) && !defined(DISABLE_ALC_UNLOADABILITY)
	if (klass == NULL) // this happens in startup, apparently
		return (MonoMemoryManager *)mono_domain_default_alc (domain)->memory_manager;
	// TODO: blatant hack, this needs to be way cheaper, probably by setting the MemoryManager in the vtable
	MonoImageSet *set = mono_metadata_get_image_set_for_class (klass);
	return memory_manager_from_set (set);
#else
	return domain->memory_manager;
#endif
}

MonoMemoryManager *
mono_memory_manager_from_method (MonoDomain *domain, MonoMethod *method)
{
#if defined(ENABLE_NETCORE) && !defined(DISABLE_ALC_UNLOADABILITY)
	// TODO: blatant hack, this needs to be way cheaper, probably by setting the MemoryManager in the vtable
	// TODO: actually get inflated method properly
	MonoImageSet *set = mono_metadata_get_image_set_for_method ((MonoMethodInflated *)method);
	return memory_manager_from_set (set);
#else
	return domain->memory_manager;
#endif
}

