#include <mono/metadata/loader-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/reflection-cache.h>
#include <mono/metadata/mono-hash-internals.h>
#include <mono/metadata/debug-internals.h>
#include <mono/utils/unlocked.h>

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
		mono_vfree (chunk, mono_pagesize (), MONO_MEM_ACCOUNT_MEM_MANAGER);
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
	chunk = (LockFreeMempoolChunk *)mono_valloc (0, size, MONO_MMAP_READ|MONO_MMAP_WRITE, MONO_MEM_ACCOUNT_MEM_MANAGER);
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

MonoMemoryManager*
mono_mem_manager_new (MonoAssemblyLoadContext **alcs, int nalcs, gboolean collectible)
{
	MonoDomain *domain = mono_get_root_domain ();
	MonoMemoryManager *memory_manager;

	memory_manager = g_new0 (MonoMemoryManager, 1);
	memory_manager->collectible = collectible;
	memory_manager->n_alcs = nalcs;

	mono_coop_mutex_init_recursive (&memory_manager->lock);
	mono_os_mutex_init (&memory_manager->mp_mutex);

	memory_manager->_mp = mono_mempool_new ();
	if (mono_runtime_get_no_exec()) {
		memory_manager->code_mp = mono_code_manager_new_aot ();
	} else {
		memory_manager->code_mp = mono_code_manager_new ();
	}
	memory_manager->lock_free_mp = lock_free_mempool_new ();

	memory_manager->alcs = mono_mempool_alloc0 (memory_manager->_mp, sizeof (MonoAssemblyLoadContext *) * nalcs);
	memcpy (memory_manager->alcs, alcs, sizeof (MonoAssemblyLoadContext *) * nalcs);

	memory_manager->class_vtable_array = g_ptr_array_new ();

	// TODO: make these not linked to the domain for debugging
	memory_manager->type_hash = mono_g_hash_table_new_type_internal ((GHashFunc)mono_metadata_type_hash, (GCompareFunc)mono_metadata_type_equal, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Reflection Type Table");
	memory_manager->refobject_hash = mono_conc_g_hash_table_new_type (mono_reflected_hash, mono_reflected_equal, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Reflection Object Table");
	memory_manager->type_init_exception_hash = mono_g_hash_table_new_type_internal (mono_aligned_addr_hash, NULL, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Type Initialization Exception Table");

	if (mono_get_runtime_callbacks ()->init_mem_manager)
		mono_get_runtime_callbacks ()->init_mem_manager (memory_manager);

	return memory_manager;
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

	// FIXME: Free generics caches

	if (debug_unload) {
		mono_mempool_invalidate (memory_manager->_mp);
		mono_code_manager_invalidate (memory_manager->code_mp);
	} else {
		mono_mempool_destroy (memory_manager->_mp);
		memory_manager->_mp = NULL;
		mono_code_manager_destroy (memory_manager->code_mp);
		memory_manager->code_mp = NULL;
	}
}

void
mono_mem_manager_free_objects (MonoMemoryManager *memory_manager)
{
	g_assert (!memory_manager->freeing);

	memory_manager_delete_objects (memory_manager);
}

void
mono_mem_manager_free (MonoMemoryManager *memory_manager, gboolean debug_unload)
{
	g_assert (!memory_manager->is_generic);

	memory_manager_delete (memory_manager, debug_unload);
	g_free (memory_manager);
}

void
mono_mem_manager_lock (MonoMemoryManager *memory_manager)
{
	mono_locks_coop_acquire (&memory_manager->lock, MemoryManagerLock);
}

void
mono_mem_manager_unlock (MonoMemoryManager *memory_manager)
{
	mono_locks_coop_release (&memory_manager->lock, MemoryManagerLock);
}

static inline void
alloc_lock (MonoMemoryManager *memory_manager)
{
	mono_os_mutex_lock (&memory_manager->mp_mutex);
}

static inline void
alloc_unlock (MonoMemoryManager *memory_manager)
{
	mono_os_mutex_unlock (&memory_manager->mp_mutex);
}

void *
mono_mem_manager_alloc (MonoMemoryManager *memory_manager, guint size)
{
	void *res;

	alloc_lock (memory_manager);
	res = mono_mempool_alloc (memory_manager->_mp, size);
	alloc_unlock (memory_manager);

	return res;
}

void *
mono_mem_manager_alloc0 (MonoMemoryManager *memory_manager, guint size)
{
	void *res;

	alloc_lock (memory_manager);
	res = mono_mempool_alloc0 (memory_manager->_mp, size);
	alloc_unlock (memory_manager);

	return res;
}

char*
mono_mem_manager_strdup (MonoMemoryManager *memory_manager, const char *s)
{
	char *res;

	alloc_lock (memory_manager);
	res = mono_mempool_strdup (memory_manager->_mp, s);
	alloc_unlock (memory_manager);

	return res;
}

gboolean
mono_mem_manager_mp_contains_addr (MonoMemoryManager *memory_manager, gpointer addr)
{
	gboolean res;

	alloc_lock (memory_manager);
	res = mono_mempool_contains_addr (memory_manager->_mp, addr);
	alloc_unlock (memory_manager);
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

//107, 131, 163
#define HASH_TABLE_SIZE 163
static MonoMemoryManager *mem_manager_cache [HASH_TABLE_SIZE];
static gint32 mem_manager_cache_hit, mem_manager_cache_miss;

static guint32
mix_hash (uintptr_t source)
{
	unsigned int hash = source;

	// Actual hash
	hash = (((hash * 215497) >> 16) ^ ((hash * 1823231) + hash));

MONO_DISABLE_WARNING(4127) /* conditional expression is constant */
	// Mix in highest bits on 64-bit systems only
	if (sizeof (source) > 4)
		hash = hash ^ ((source >> 31) >> 1);
MONO_RESTORE_WARNING

	return hash;
}

static guint32
hash_alcs (MonoAssemblyLoadContext **alcs, int nalcs)
{
	guint32 res = 0;
	int i;
	for (i = 0; i < nalcs; ++i)
		res += mix_hash ((size_t)alcs [i]);

	return res;
}

static gboolean
match_mem_manager (MonoMemoryManager *mm, MonoAssemblyLoadContext **alcs, int nalcs)
{
	int j, k;

	if (mm->n_alcs != nalcs)
		return FALSE;
	/* The order might differ so check all pairs */
	for (j = 0; j < nalcs; ++j) {
		for (k = 0; k < nalcs; ++k)
			if (mm->alcs [k] == alcs [j])
				break;
		if (k == nalcs)
			/* Not found */
			break;
	}

	return j == nalcs;
}

static MonoMemoryManager*
mem_manager_cache_get (MonoAssemblyLoadContext **alcs, int nalcs)
{
	guint32 hash_code = hash_alcs (alcs, nalcs);
	int index = hash_code % HASH_TABLE_SIZE;
	MonoMemoryManager *mm = mem_manager_cache [index];
	if (!mm || !match_mem_manager (mm, alcs, nalcs)) {
		UnlockedIncrement (&mem_manager_cache_miss);
		return NULL;
	}
	UnlockedIncrement (&mem_manager_cache_hit);
	return mm;
}

static void
mem_manager_cache_add (MonoMemoryManager *mem_manager)
{
	guint32 hash_code = hash_alcs (mem_manager->alcs, mem_manager->n_alcs);
	int index = hash_code % HASH_TABLE_SIZE;
	mem_manager_cache [index] = mem_manager;
}

static MonoMemoryManager*
get_mem_manager_for_alcs (MonoAssemblyLoadContext **alcs, int nalcs)
{
	MonoAssemblyLoadContext *alc;
	GPtrArray *mem_managers;
	MonoMemoryManager *res;
	gboolean collectible;

	/* Can happen for dynamic images */
	if (nalcs == 0)
		return mono_alc_get_default ()->memory_manager;

	/* Common case */
	if (nalcs == 1)
		return alcs [0]->memory_manager;

	collectible = FALSE;
	for (int i = 0; i < nalcs; ++i)
		collectible |= alcs [i]->collectible;
	if (!collectible)
		/* Can use the default alc */
		return mono_alc_get_default ()->memory_manager;

	// Check in a lock free cache
	res = mem_manager_cache_get (alcs, nalcs);
	if (res)
		return res;

	/*
	 * Find an existing mem manager for these ALCs.
	 * This can exist even if the cache lookup fails since the cache is very simple.
	 */

	/* We can search any ALC in the list, use the first one for now */
	alc = alcs [0];

	mono_alc_memory_managers_lock (alc);

	mem_managers = alc->generic_memory_managers;

	res = NULL;
	for (int mindex = 0; mindex < mem_managers->len; ++mindex) {
		MonoMemoryManager *mm = (MonoMemoryManager*)g_ptr_array_index (mem_managers, mindex);

		if (match_mem_manager (mm, alcs, nalcs)) {
			res = mm;
			break;
		}
	}

	mono_alc_memory_managers_unlock (alc);

	if (res)
		return res;

	/* Create new mem manager */
	res = mono_mem_manager_new (alcs, nalcs, collectible);
	res->is_generic = TRUE;

	/* The hashes are lazily inited in metadata.c */

	/* Register it into its ALCs */
	for (int i = 0; i < nalcs; ++i) {
		mono_alc_memory_managers_lock (alcs [i]);
		g_ptr_array_add (alcs [i]->generic_memory_managers, res);
		mono_alc_memory_managers_unlock (alcs [i]);
	}

	mono_memory_barrier ();

	mem_manager_cache_add (res);

	return res;
}

/*
 * mono_mem_manager_get_generic:
 *
 *   Return a memory manager for allocating memory owned by the set of IMAGES.
 */
MonoMemoryManager*
mono_mem_manager_get_generic (MonoImage **images, int nimages)
{
	MonoAssemblyLoadContext **alcs = g_newa (MonoAssemblyLoadContext*, nimages);
	int nalcs, j;

	/* Collect the set of ALCs owning the images */
	nalcs = 0;
	for (int i = 0; i < nimages; ++i) {
		MonoAssemblyLoadContext *alc = mono_image_get_alc (images [i]);

		if (!alc)
			continue;

		/* O(n^2), but shouldn't be a problem in practice */
		for (j = 0; j < nalcs; ++j)
			if (alcs [j] == alc)
				break;
		if (j == nalcs)
			alcs [nalcs ++] = alc;
	}

	return get_mem_manager_for_alcs (alcs, nalcs);
}

/*
 * mono_mem_manager_merge:
 *
 *   Return a mem manager which depends on the ALCs of MM1/MM2.
 */
MonoMemoryManager*
mono_mem_manager_merge (MonoMemoryManager *mm1, MonoMemoryManager *mm2)
{
	MonoAssemblyLoadContext **alcs;

	// Common case
	if (mm1 == mm2)
		return mm1;

	alcs = g_newa (MonoAssemblyLoadContext*, mm1->n_alcs + mm2->n_alcs);

	memcpy (alcs, mm1->alcs, sizeof (MonoAssemblyLoadContext*) * mm1->n_alcs);

	int nalcs = mm1->n_alcs;
	/* O(n^2), but shouldn't be a problem in practice */
	for (int i = 0; i < mm2->n_alcs; ++i) {
		int j;
		for (j = 0; j < mm1->n_alcs; ++j) {
			if (mm2->alcs [i] == mm1->alcs [j])
				break;
		}
		if (j == mm1->n_alcs)
			alcs [nalcs ++] = mm2->alcs [i];
	}
	return get_mem_manager_for_alcs (alcs, nalcs);
}
