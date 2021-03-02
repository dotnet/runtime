#include <mono/metadata/loader-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/reflection-cache.h>
#include <mono/metadata/mono-hash-internals.h>
#include <mono/metadata/debug-internals.h>

static LockFreeMempool*
lock_free_mempool_new (void)
{
	return g_new0 (LockFreeMempool, 1);
}

static void
lock_free_mempool_free (LockFreeMempool *mp)
{
	LockFreeMempoolChunk *chunk, *next;

	chunk = mp->chunks;
	while (chunk) {
		next = (LockFreeMempoolChunk *)chunk->prev;
		mono_vfree (chunk, mono_pagesize (), MONO_MEM_ACCOUNT_DOMAIN);
		chunk = next;
	}
	g_free (mp);
}

/*
 * This is async safe
 */
static LockFreeMempoolChunk*
lock_free_mempool_chunk_new (LockFreeMempool *mp, int len)
{
	LockFreeMempoolChunk *chunk, *prev;
	int size;

	size = mono_pagesize ();
	while (size - sizeof (LockFreeMempoolChunk) < len)
		size += mono_pagesize ();
	chunk = (LockFreeMempoolChunk *)mono_valloc (0, size, MONO_MMAP_READ|MONO_MMAP_WRITE, MONO_MEM_ACCOUNT_DOMAIN);
	g_assert (chunk);
	chunk->mem = (guint8 *)ALIGN_PTR_TO ((char*)chunk + sizeof (LockFreeMempoolChunk), 16);
	chunk->size = ((char*)chunk + size) - (char*)chunk->mem;
	chunk->pos = 0;

	/* Add to list of chunks lock-free */
	while (TRUE) {
		prev = mp->chunks;
		if (mono_atomic_cas_ptr ((volatile gpointer*)&mp->chunks, chunk, prev) == prev)
			break;
	}
	chunk->prev = prev;

	return chunk;
}

/*
 * This is async safe
 */
static gpointer
lock_free_mempool_alloc0 (LockFreeMempool *mp, guint size)
{
	LockFreeMempoolChunk *chunk;
	gpointer res;
	int oldpos;

	// FIXME: Free the allocator

	size = ALIGN_TO (size, 8);
	chunk = mp->current;
	if (!chunk) {
		chunk = lock_free_mempool_chunk_new (mp, size);
		mono_memory_barrier ();
		/* Publish */
		mp->current = chunk;
	}

	/* The code below is lock-free, 'chunk' is shared state */
	oldpos = mono_atomic_fetch_add_i32 (&chunk->pos, size);
	if (oldpos + size > chunk->size) {
		chunk = lock_free_mempool_chunk_new (mp, size);
		g_assert (chunk->pos + size <= chunk->size);
		res = chunk->mem;
		chunk->pos += size;
		mono_memory_barrier ();
		mp->current = chunk;
	} else {
		res = (char*)chunk->mem + oldpos;
	}

	return res;
}

static void
memory_manager_init (MonoMemoryManager *memory_manager, MonoDomain *domain, gboolean collectible)
{
	memory_manager->domain = domain;
	memory_manager->freeing = FALSE;

	mono_coop_mutex_init_recursive (&memory_manager->lock);

	memory_manager->mp = mono_mempool_new ();
	memory_manager->code_mp = mono_code_manager_new ();
	memory_manager->lock_free_mp = lock_free_mempool_new ();

	memory_manager->class_vtable_array = g_ptr_array_new ();

	// TODO: make these not linked to the domain for debugging
	memory_manager->type_hash = mono_g_hash_table_new_type_internal ((GHashFunc)mono_metadata_type_hash, (GCompareFunc)mono_metadata_type_equal, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Reflection Type Table");
	memory_manager->refobject_hash = mono_conc_g_hash_table_new_type (mono_reflected_hash, mono_reflected_equal, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Reflection Object Table");
	memory_manager->type_init_exception_hash = mono_g_hash_table_new_type_internal (mono_aligned_addr_hash, NULL, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Type Initialization Exception Table");

	if (mono_get_runtime_callbacks ()->init_mem_manager)
		mono_get_runtime_callbacks ()->init_mem_manager (memory_manager);
}

MonoSingletonMemoryManager *
mono_mem_manager_create_singleton (MonoAssemblyLoadContext *alc, MonoDomain *domain, gboolean collectible)
{
	MonoSingletonMemoryManager *mem_manager = g_new0 (MonoSingletonMemoryManager, 1);
	memory_manager_init ((MonoMemoryManager *)mem_manager, domain, collectible);

	mem_manager->memory_manager.is_generic = FALSE;
	mem_manager->alc = alc;

	return mem_manager;
}

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

// First phase of deletion
static void
memory_manager_delete_objects (MonoMemoryManager *memory_manager)
{
	memory_manager->freeing = TRUE;

	// Must be done before type_hash is freed
	for (int i = 0; i < memory_manager->class_vtable_array->len; i++)
		unregister_vtable_reflection_type ((MonoVTable *)g_ptr_array_index (memory_manager->class_vtable_array, i));

	g_ptr_array_free (memory_manager->class_vtable_array, TRUE);
	memory_manager->class_vtable_array = NULL;
	mono_g_hash_table_destroy (memory_manager->type_hash);
	memory_manager->type_hash = NULL;
	mono_conc_g_hash_table_foreach (memory_manager->refobject_hash, cleanup_refobject_hash, NULL);
	mono_conc_g_hash_table_destroy (memory_manager->refobject_hash);
	memory_manager->refobject_hash = NULL;
	mono_g_hash_table_destroy (memory_manager->type_init_exception_hash);
	memory_manager->type_init_exception_hash = NULL;
}

// Full deletion
static void
memory_manager_delete (MonoMemoryManager *memory_manager, gboolean debug_unload)
{
	// Scan here to assert no lingering references in vtables?

	if (mono_get_runtime_callbacks ()->free_mem_manager)
		mono_get_runtime_callbacks ()->free_mem_manager (memory_manager);

	if (memory_manager->debug_info) {
		mono_mem_manager_free_debug_info (memory_manager);
		memory_manager->debug_info = NULL;
	}

	if (!memory_manager->freeing)
		memory_manager_delete_objects (memory_manager);

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
}

void
mono_mem_manager_free_objects_singleton (MonoSingletonMemoryManager *memory_manager)
{
	g_assert (!memory_manager->memory_manager.freeing);

	memory_manager_delete_objects (&memory_manager->memory_manager);
}

void
mono_mem_manager_free_singleton (MonoSingletonMemoryManager *memory_manager, gboolean debug_unload)
{
	g_assert (!memory_manager->memory_manager.is_generic);

	memory_manager_delete (&memory_manager->memory_manager, debug_unload);
	g_free (memory_manager);
}

void
mono_mem_manager_lock (MonoMemoryManager *memory_manager)
{
	//mono_coop_mutex_lock (&memory_manager->lock);
	mono_domain_lock (memory_manager->domain);
}

void
mono_mem_manager_unlock (MonoMemoryManager *memory_manager)
{
	//mono_coop_mutex_unlock (&memory_manager->lock);
	mono_domain_unlock (memory_manager->domain);
}

void *
mono_mem_manager_alloc (MonoMemoryManager *memory_manager, guint size)
{
	void *res;

	mono_mem_manager_lock (memory_manager);
	res = mono_mem_manager_alloc_nolock (memory_manager, size);
	mono_mem_manager_unlock (memory_manager);

	return res;
}

void *
mono_mem_manager_alloc_nolock (MonoMemoryManager *memory_manager, guint size)
{
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_fetch_add_i32 (&mono_perfcounters->loader_bytes, size);
#endif
	return mono_mempool_alloc (memory_manager->mp, size);
}

void *
mono_mem_manager_alloc0 (MonoMemoryManager *memory_manager, guint size)
{
	void *res;

	mono_mem_manager_lock (memory_manager);
	res = mono_mem_manager_alloc0_nolock (memory_manager, size);
	mono_mem_manager_unlock (memory_manager);

	return res;
}

void *
mono_mem_manager_alloc0_nolock (MonoMemoryManager *memory_manager, guint size)
{
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_fetch_add_i32 (&mono_perfcounters->loader_bytes, size);
#endif
	return mono_mempool_alloc0 (memory_manager->mp, size);
}

char*
mono_mem_manager_strdup (MonoMemoryManager *memory_manager, const char *s)
{
	char *res;

	mono_mem_manager_lock (memory_manager);
	res = mono_mempool_strdup (memory_manager->mp, s);
	mono_mem_manager_unlock (memory_manager);

	return res;
}

void *
(mono_mem_manager_code_reserve) (MonoMemoryManager *memory_manager, int size)
{
	void *res;

	mono_mem_manager_lock (memory_manager);
	res = mono_code_manager_reserve (memory_manager->code_mp, size);
	mono_mem_manager_unlock (memory_manager);

	return res;
}

void *
mono_mem_manager_code_reserve_align (MonoMemoryManager *memory_manager, int size, int alignment)
{
	void *res;

	mono_mem_manager_lock (memory_manager);
	res = mono_code_manager_reserve_align (memory_manager->code_mp, size, alignment);
	mono_mem_manager_unlock (memory_manager);

	return res;
}

void
mono_mem_manager_code_commit (MonoMemoryManager *memory_manager, void *data, int size, int newsize)
{
	mono_mem_manager_lock (memory_manager);
	mono_code_manager_commit (memory_manager->code_mp, data, size, newsize);
	mono_mem_manager_unlock (memory_manager);
}

/*
 * mono_mem_manager_code_foreach:
 * Iterate over the code thunks of the code manager of @memory_manager.
 *
 * The @func callback MUST not take any locks. If it really needs to, it must respect
 * the locking rules of the runtime: http://www.mono-project.com/Mono:Runtime:Documentation:ThreadSafety
 * LOCKING: Acquires the memory manager lock.
 */
void
mono_mem_manager_code_foreach (MonoMemoryManager *memory_manager, MonoCodeManagerFunc func, void *user_data)
{
	mono_mem_manager_lock (memory_manager);
	mono_code_manager_foreach (memory_manager->code_mp, func, user_data);
	mono_mem_manager_unlock (memory_manager);
}

gpointer
(mono_mem_manager_alloc0_lock_free) (MonoMemoryManager *memory_manager, guint size)
{
	return lock_free_mempool_alloc0 (memory_manager->lock_free_mp, size);
}
