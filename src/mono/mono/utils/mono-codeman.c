/**
 * \file
 */

#include "config.h"

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <glib.h>

#include "mono-codeman.h"
#include "mono-mmap.h"
#include "mono-counters.h"
#if _WIN32
static void* mono_code_manager_heap;
#else
#define USE_DL_PREFIX 1
#include "dlmalloc.h"
#endif
#include <mono/metadata/profiler-private.h>
#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-tls.h>

static uintptr_t code_memory_used = 0;
static size_t dynamic_code_alloc_count;
static size_t dynamic_code_bytes_count;
static size_t dynamic_code_frees_count;
static const MonoCodeManagerCallbacks *code_manager_callbacks;

/*
 * AMD64 processors maintain icache coherency only for pages which are 
 * marked executable. Also, windows DEP requires us to obtain executable memory from
 * malloc when using dynamic code managers. The system malloc can't do this so we use a 
 * slighly modified version of Doug Lea's Malloc package for this purpose:
 * http://g.oswego.edu/dl/html/malloc.html
 *
 * Or on Windows, HeapCreate (HEAP_CREATE_ENABLE_EXECUTE).
 */

#define MIN_PAGES 16

#if defined(_WIN32) && (defined(_M_IX86) || defined(_M_X64)) // These are the same.
#define MIN_ALIGN MEMORY_ALLOCATION_ALIGNMENT
#elif defined(__x86_64__)
/*
 * We require 16 byte alignment on amd64 so the fp literals embedded in the code are 
 * properly aligned for SSE2.
 */
#define MIN_ALIGN 16
#else
#define MIN_ALIGN 8
#endif

/* if a chunk has less than this amount of free space it's considered full */
#define MAX_WASTAGE 32
#define MIN_BSIZE 32

#ifdef __x86_64__
#define ARCH_MAP_FLAGS MONO_MMAP_32BIT
#else
#define ARCH_MAP_FLAGS 0
#endif

#define MONO_PROT_RWX (MONO_MMAP_READ|MONO_MMAP_WRITE|MONO_MMAP_EXEC|MONO_MMAP_JIT)
#define MONO_PROT_RW (MONO_MMAP_READ|MONO_MMAP_WRITE)

typedef struct _CodeChunk CodeChunk;

enum {
	CODE_FLAG_MMAP,
	CODE_FLAG_MALLOC
};

struct _CodeChunk {
	char *data;
	CodeChunk *next;
	int pos;
	int size;
	unsigned int reserved: 8;
	/* this number of bytes is available to resolve addresses far in memory */
	unsigned int bsize: 24;
};

struct _MonoCodeManager {
	CodeChunk *current;
	CodeChunk *full;
	CodeChunk *last;
	int dynamic : 1;
	int read_only : 1;
	int no_exec : 1;
};

#define ALIGN_INT(val,alignment) (((val) + (alignment - 1)) & ~(alignment - 1))

#define VALLOC_FREELIST_SIZE 16

static mono_mutex_t valloc_mutex;
static GHashTable *valloc_freelists;
static MonoNativeTlsKey write_level_tls_id;

static void*
codechunk_valloc (void *preferred, guint32 size, gboolean no_exec)
{
	void *ptr;
	GSList *freelist;

	if (!valloc_freelists) {
		mono_os_mutex_init_recursive (&valloc_mutex);
		valloc_freelists = g_hash_table_new (NULL, NULL);
	}

	/*
	 * Keep a small freelist of memory blocks to decrease pressure on the kernel memory subsystem to avoid #3321.
	 */
	mono_os_mutex_lock (&valloc_mutex);
	freelist = (GSList *) g_hash_table_lookup (valloc_freelists, GUINT_TO_POINTER (size));
	if (freelist) {
		ptr = freelist->data;
		mono_codeman_enable_write ();
		memset (ptr, 0, size);
		mono_codeman_disable_write ();
		freelist = g_slist_delete_link (freelist, freelist);
		g_hash_table_insert (valloc_freelists, GUINT_TO_POINTER (size), freelist);
	} else {
		int prot;
		if (!no_exec)
			prot = MONO_PROT_RWX | ARCH_MAP_FLAGS;
		else
			prot = MONO_PROT_RW | ARCH_MAP_FLAGS;
		ptr = mono_valloc (preferred, size, prot, MONO_MEM_ACCOUNT_CODE);
		if (!ptr && preferred)
			ptr = mono_valloc (NULL, size, prot, MONO_MEM_ACCOUNT_CODE);
	}
	mono_os_mutex_unlock (&valloc_mutex);
	return ptr;
}

static void
codechunk_vfree (void *ptr, guint32 size)
{
	GSList *freelist;

	mono_os_mutex_lock (&valloc_mutex);
	freelist = (GSList *) g_hash_table_lookup (valloc_freelists, GUINT_TO_POINTER (size));
	if (!freelist || g_slist_length (freelist) < VALLOC_FREELIST_SIZE) {
		freelist = g_slist_prepend (freelist, ptr);
		g_hash_table_insert (valloc_freelists, GUINT_TO_POINTER (size), freelist);
	} else {
		mono_vfree (ptr, size, MONO_MEM_ACCOUNT_CODE);
	}
	mono_os_mutex_unlock (&valloc_mutex);
}		

static void
codechunk_cleanup (void)
{
	GHashTableIter iter;
	gpointer key, value;

	if (!valloc_freelists)
		return;
	g_hash_table_iter_init (&iter, valloc_freelists);
	while (g_hash_table_iter_next (&iter, &key, &value)) {
		GSList *freelist = (GSList *) value;
		GSList *l;

		for (l = freelist; l; l = l->next) {
			mono_vfree (l->data, GPOINTER_TO_UINT (key), MONO_MEM_ACCOUNT_CODE);
		}
		g_slist_free (freelist);
	}
	g_hash_table_destroy (valloc_freelists);
}

/* non-zero if we don't need to toggle write protection on individual threads */
static int
codeman_no_exec;

/**
 * mono_codeman_set_code_no_exec:
 *
 * If set to a non-zero value,
 * \c mono_codeman_enable_write and \c mono_codeman_disable_write turn into no-ops.
 *
 * The AOT compiler should do this if it is allocating RW (no X) memory for code.
 */
static void
mono_codeman_set_code_no_exec (int no_exec)
{
	codeman_no_exec = no_exec;
}

void
mono_code_manager_init (gboolean no_exec)
{
	mono_counters_register ("Dynamic code allocs", MONO_COUNTER_JIT | MONO_COUNTER_ULONG, &dynamic_code_alloc_count);
	mono_counters_register ("Dynamic code bytes", MONO_COUNTER_JIT | MONO_COUNTER_ULONG, &dynamic_code_bytes_count);
	mono_counters_register ("Dynamic code frees", MONO_COUNTER_JIT | MONO_COUNTER_ULONG, &dynamic_code_frees_count);

	mono_native_tls_alloc (&write_level_tls_id, NULL);

	mono_codeman_set_code_no_exec (no_exec);
}

void
mono_code_manager_cleanup (void)
{
	codechunk_cleanup ();
}

void
mono_code_manager_install_callbacks (const MonoCodeManagerCallbacks* callbacks)
{
	code_manager_callbacks = callbacks;
}

static
int
mono_codeman_allocation_type (MonoCodeManager const *cman)
{
#ifdef FORCE_MALLOC
	return CODE_FLAG_MALLOC;
#else
	return cman->dynamic ? CODE_FLAG_MALLOC : CODE_FLAG_MMAP;
#endif
}

enum CodeManagerType {
	MONO_CODEMAN_TYPE_JIT,
	MONO_CODEMAN_TYPE_DYNAMIC,
	MONO_CODEMAN_TYPE_AOT,
};

static gboolean
codeman_type_is_dynamic (int codeman_type)
{
	switch (codeman_type) {
	case MONO_CODEMAN_TYPE_DYNAMIC:
		return TRUE;
	default:
		return FALSE;
	}
}

static gboolean
codeman_type_is_aot (int codeman_type)
{
	switch (codeman_type) {
	case MONO_CODEMAN_TYPE_AOT:
		return TRUE;
	default:
		return FALSE;
	}
}

/**
 * mono_code_manager_new_internal
 *
 * Returns: the new code manager
 */
static
MonoCodeManager*
mono_code_manager_new_internal (int codeman_type)
{
	MonoCodeManager* cman = g_new0 (MonoCodeManager, 1);
	if (cman) {
		cman->dynamic = codeman_type_is_dynamic (codeman_type);
		cman->no_exec = codeman_type_is_aot (codeman_type);
#if _WIN32
		// It would seem the heap should live and die with the codemanager,
		// but that was failing, so try a global.
		if (mono_codeman_allocation_type (cman) == CODE_FLAG_MALLOC && !mono_code_manager_heap) {
			// This heap is leaked, similar to dlmalloc state.
			void* const heap = HeapCreate (HEAP_CREATE_ENABLE_EXECUTE, 0, 0);
			if (heap && mono_atomic_cas_ptr (&mono_code_manager_heap, heap, NULL))
				HeapDestroy (heap);
			if (!mono_code_manager_heap) {
				mono_code_manager_destroy (cman);
				cman = 0;
			}
		}
#endif
	}
	return cman;
}

/**
 * mono_code_manager_new:
 *
 * Creates a new code manager. A code manager can be used to allocate memory
 * suitable for storing native code that can be later executed.
 * A code manager allocates memory from the operating system in large chunks
 * (typically 64KB in size) so that many methods can be allocated inside them
 * close together, improving cache locality.
 *
 * Returns: the new code manager
 */
MonoCodeManager* 
mono_code_manager_new (void)
{
	return mono_code_manager_new_internal (MONO_CODEMAN_TYPE_JIT);
}

/**
 * mono_code_manager_new_dynamic:
 *
 * Creates a new code manager suitable for holding native code that can be
 * used for single or small methods that need to be deallocated independently
 * of other native code.
 *
 * Returns: the new code manager
 */
MonoCodeManager* 
mono_code_manager_new_dynamic (void)
{
	return mono_code_manager_new_internal (MONO_CODEMAN_TYPE_DYNAMIC);
}

/**
 * mono_code_manager_new_aot:
 *
 * Creates a new code manager that will hold code that is never
 * executed.  This can be used by the AOT compiler to allocate pages
 * on W^X platforms without asking for execute permission (which may
 * require additional entitlements, or AOT-time OS calls).
 */
MonoCodeManager*
mono_code_manager_new_aot (void)
{
	return mono_code_manager_new_internal (MONO_CODEMAN_TYPE_AOT);
}

static gpointer
mono_codeman_malloc (gsize n)
{
#if _WIN32
	void* const heap = mono_code_manager_heap;
	g_assert (heap);
	return HeapAlloc (heap, 0, n);
#else
	mono_codeman_enable_write ();
	gpointer res = dlmemalign (MIN_ALIGN, n);
	mono_codeman_disable_write ();
	return res;
#endif
}

static void
mono_codeman_free (gpointer p)
{
	if (!p)
		return;
#if _WIN32
	void* const heap = mono_code_manager_heap;
	g_assert (heap);
	HeapFree (heap, 0, p);
#else
	mono_codeman_enable_write ();
	dlfree (p);
	mono_codeman_disable_write ();
#endif
}

static void
free_chunklist (MonoCodeManager *cman, CodeChunk *chunk)
{
	CodeChunk *dead;
	
#if defined(HAVE_VALGRIND_MEMCHECK_H) && defined (VALGRIND_JIT_UNREGISTER_MAP)
	int valgrind_unregister = 0;
	if (RUNNING_ON_VALGRIND)
		valgrind_unregister = 1;
#define valgrind_unregister(x) do { if (valgrind_unregister) { VALGRIND_JIT_UNREGISTER_MAP(NULL,x); } } while (0) 
#else
#define valgrind_unregister(x)
#endif

	if (!chunk)
		return;

	const int flags = mono_codeman_allocation_type (cman);

	for (; chunk; ) {
		dead = chunk;
		MONO_PROFILER_RAISE (jit_chunk_destroyed, ((mono_byte *) dead->data));
		if (code_manager_callbacks)
			code_manager_callbacks->chunk_destroy (dead->data);
		chunk = chunk->next;
		if (flags == CODE_FLAG_MMAP) {
			codechunk_vfree (dead->data, dead->size);
			/* valgrind_unregister(dead->data); */
		} else if (flags == CODE_FLAG_MALLOC) {
			mono_codeman_free (dead->data);
		}
		code_memory_used -= dead->size;
		g_free (dead);
	}
}

/**
 * mono_code_manager_destroy:
 * \param cman a code manager
 * Free all the memory associated with the code manager \p cman.
 */
void
mono_code_manager_destroy (MonoCodeManager *cman)
{
	if (!cman)
		return;
	free_chunklist (cman, cman->full);
	free_chunklist (cman, cman->current);
	g_free (cman);
}

/**
 * mono_code_manager_invalidate:
 * \param cman a code manager
 * Fill all the memory with an invalid native code value
 * so that any attempt to execute code allocated in the code
 * manager \p cman will fail. This is used for debugging purposes.
 */
void
mono_code_manager_invalidate (MonoCodeManager *cman)
{
	CodeChunk *chunk;

#if defined(__i386__) || defined(_M_IX86) || defined(__x86_64__) || defined(_M_X64)
	int fill_value = 0xcc; /* x86 break */
#else
	int fill_value = 0x2a;
#endif

	for (chunk = cman->current; chunk; chunk = chunk->next)
		memset (chunk->data, fill_value, chunk->size);
	for (chunk = cman->full; chunk; chunk = chunk->next)
		memset (chunk->data, fill_value, chunk->size);
}

/**
 * mono_code_manager_set_read_only:
 * \param cman a code manager
 * Make the code manager read only, so further allocation requests cause an assert.
 */
void
mono_code_manager_set_read_only (MonoCodeManager *cman)
{
	cman->read_only = TRUE;
}

/**
 * mono_code_manager_foreach:
 * \param cman a code manager
 * \param func a callback function pointer
 * \param user_data additional data to pass to \p func
  * Invokes the callback \p func for each different chunk of memory allocated
 * in the code manager \p cman.
 */
void
mono_code_manager_foreach (MonoCodeManager *cman, MonoCodeManagerFunc func, void *user_data)
{
	CodeChunk *chunk;
	for (chunk = cman->current; chunk; chunk = chunk->next) {
		if (func (chunk->data, chunk->size, chunk->bsize, user_data))
			return;
	}
	for (chunk = cman->full; chunk; chunk = chunk->next) {
		if (func (chunk->data, chunk->size, chunk->bsize, user_data))
			return;
	}
}

/* BIND_ROOM is the divisor for the chunk of code size dedicated
 * to binding branches (branches not reachable with the immediate displacement)
 * bind_size = size/BIND_ROOM;
 * we should reduce it and make MIN_PAGES bigger for such systems
 */
#if defined(__ppc__) || defined(__powerpc__)
#define BIND_ROOM 4
#endif
#if defined(TARGET_ARM64)
#define BIND_ROOM 4
#endif

static CodeChunk*
new_codechunk (MonoCodeManager *cman, int size)
{
	CodeChunk * const last = cman->last;
	int const dynamic = cman->dynamic;
	int const no_exec = cman->no_exec;
	int chunk_size, bsize = 0;
	CodeChunk *chunk;
	void *ptr;

	const int flags = mono_codeman_allocation_type (cman);
	const int pagesize = mono_pagesize ();
	const int valloc_granule = mono_valloc_granule ();

	if (dynamic) {
		chunk_size = size;
	} else {
		const int minsize = MAX (pagesize * MIN_PAGES, valloc_granule);
		if (size < minsize)
			chunk_size = minsize;
		else {
			/* Allocate MIN_ALIGN-1 more than we need so we can still */
			/* guarantee MIN_ALIGN alignment for individual allocs	  */
			/* from mono_code_manager_reserve_align.		  */
			size += MIN_ALIGN - 1;
			size &= ~(MIN_ALIGN - 1);
			chunk_size = size;
			chunk_size += valloc_granule - 1;
			chunk_size &= ~ (valloc_granule - 1);
		}
	}
#ifdef BIND_ROOM
	if (dynamic)
		/* Reserve more space since there are no other chunks we might use if this one gets full */
		bsize = (chunk_size * 2) / BIND_ROOM;
	else
		bsize = chunk_size / BIND_ROOM;
	if (bsize < MIN_BSIZE)
		bsize = MIN_BSIZE;
	bsize += MIN_ALIGN -1;
	bsize &= ~ (MIN_ALIGN - 1);
	if (chunk_size - size < bsize) {
		chunk_size = size + bsize;
		if (!dynamic) {
			chunk_size += valloc_granule - 1;
			chunk_size &= ~ (valloc_granule - 1);
		}
	}
#endif

	if (flags == CODE_FLAG_MALLOC) {
		ptr = mono_codeman_malloc (chunk_size + MIN_ALIGN - 1);
		if (!ptr)
			return NULL;
	} else {
		/* Try to allocate code chunks next to each other to help the VM */
		ptr = NULL;
		if (last)
			ptr = codechunk_valloc ((guint8*)last->data + last->size, chunk_size, no_exec);
		if (!ptr)
			ptr = codechunk_valloc (NULL, chunk_size, no_exec);
		if (!ptr)
			return NULL;
	}

#ifdef BIND_ROOM
	if (flags == CODE_FLAG_MALLOC) {
		/* Make sure the thunks area is zeroed */
		mono_codeman_enable_write ();
		memset (ptr, 0, bsize);
		mono_codeman_disable_write ();
	}
#endif

	chunk = (CodeChunk *) g_malloc (sizeof (CodeChunk));
	if (!chunk) {
		if (flags == CODE_FLAG_MALLOC)
			mono_codeman_free (ptr);
		else
			mono_vfree (ptr, chunk_size, MONO_MEM_ACCOUNT_CODE);
		return NULL;
	}
	chunk->next = NULL;
	chunk->size = chunk_size;
	chunk->data = (char *) ptr;
	chunk->reserved = 0;
	chunk->pos = bsize;
	chunk->bsize = bsize;
	if (code_manager_callbacks)
		code_manager_callbacks->chunk_new (chunk->data, chunk->size);
	MONO_PROFILER_RAISE (jit_chunk_created, ((mono_byte *) chunk->data, chunk->size));

	code_memory_used += chunk_size;
	mono_runtime_resource_check_limit (MONO_RESOURCE_JIT_CODE, code_memory_used);
	/*printf ("code chunk at: %p\n", ptr);*/
	return chunk;
}

/**
 * mono_code_manager_reserve_align:
 * \param cman a code manager
 * \param size size of memory to allocate
 * \param alignment power of two alignment value
 * Allocates at least \p size bytes of memory inside the code manager \p cman.
 * \returns the pointer to the allocated memory or NULL on failure
 */
void*
mono_code_manager_reserve_align (MonoCodeManager *cman, int size, int alignment)
{
	CodeChunk *chunk, *prev;
	void *ptr;
	guint32 align_mask = alignment - 1;

	g_assert (!cman->read_only);

	/* eventually allow bigger alignments, but we need to fix the dynamic alloc code to
	 * handle this before
	 */
	g_assert (alignment <= MIN_ALIGN);

	if (cman->dynamic) {
		++dynamic_code_alloc_count;
		dynamic_code_bytes_count += size;
	}

	if (!cman->current) {
		cman->current = new_codechunk (cman, size);
		if (!cman->current)
			return NULL;
		cman->last = cman->current;
	}

	for (chunk = cman->current; chunk; chunk = chunk->next) {
		if (ALIGN_INT (chunk->pos, alignment) + size <= chunk->size) {
			chunk->pos = ALIGN_INT (chunk->pos, alignment);
			/* Align the chunk->data we add to chunk->pos */
			/* or we can't guarantee proper alignment     */
			ptr = (void*)((((uintptr_t)chunk->data + align_mask) & ~(uintptr_t)align_mask) + chunk->pos);
			chunk->pos = ((char*)ptr - chunk->data) + size;
			return ptr;
		}
	}
	/* 
	 * no room found, move one filled chunk to cman->full 
	 * to keep cman->current from growing too much
	 */
	prev = NULL;
	for (chunk = cman->current; chunk; prev = chunk, chunk = chunk->next) {
		if (chunk->pos + MIN_ALIGN * 4 <= chunk->size)
			continue;
		if (prev) {
			prev->next = chunk->next;
		} else {
			cman->current = chunk->next;
		}
		chunk->next = cman->full;
		cman->full = chunk;
		break;
	}
	chunk = new_codechunk (cman, size);
	if (!chunk)
		return NULL;
	chunk->next = cman->current;
	cman->current = chunk;
	cman->last = cman->current;
	chunk->pos = ALIGN_INT (chunk->pos, alignment);
	/* Align the chunk->data we add to chunk->pos */
	/* or we can't guarantee proper alignment     */
	ptr = (void*)((((uintptr_t)chunk->data + align_mask) & ~(uintptr_t)align_mask) + chunk->pos);
	chunk->pos = ((char*)ptr - chunk->data) + size;
	return ptr;
}

/**
 * mono_code_manager_reserve:
 * \param cman a code manager
 * \param size size of memory to allocate
 * Allocates at least \p size bytes of memory inside the code manager \p cman.
 * \returns the pointer to the allocated memory or NULL on failure
 */
void*
mono_code_manager_reserve (MonoCodeManager *cman, int size)
{
	return mono_code_manager_reserve_align (cman, size, MIN_ALIGN);
}

/**
 * mono_code_manager_commit:
 * \param cman a code manager
 * \param data the pointer returned by mono_code_manager_reserve ()
 * \param size the size requested in the call to mono_code_manager_reserve ()
 * \param newsize the new size to reserve
 * If we reserved too much room for a method and we didn't allocate
 * already from the code manager, we can get back the excess allocation
 * for later use in the code manager.
 */
void
mono_code_manager_commit (MonoCodeManager *cman, void *data, int size, int newsize)
{
	g_assert (newsize <= size);

	if (cman->current && (size != newsize) && (data == cman->current->data + cman->current->pos - size)) {
		cman->current->pos -= size - newsize;
	}
}

/**
 * mono_code_manager_size:
 * \param cman a code manager
 * \param used_size pointer to an integer for the result
 * This function can be used to get statistics about a code manager:
 * the integer pointed to by \p used_size will contain how much
 * memory is actually used inside the code managed \p cman.
 * \returns the amount of memory allocated in \p cman
 */
int
mono_code_manager_size (MonoCodeManager *cman, int *used_size)
{
	CodeChunk *chunk;
	guint32 size = 0;
	guint32 used = 0;
	for (chunk = cman->current; chunk; chunk = chunk->next) {
		size += chunk->size;
		used += chunk->pos;
	}
	for (chunk = cman->full; chunk; chunk = chunk->next) {
		size += chunk->size;
		used += chunk->pos;
	}
	if (used_size)
		*used_size = used;
	return size;
}

/*
 * mono_codeman_enable_write ():
 *
 *   Enable writing to code memory on the current thread on platforms that need it.
 * Calls can be nested.
 */
void
mono_codeman_enable_write (void)
{
	if (codeman_no_exec)
		return;
#ifdef HAVE_PTHREAD_JIT_WRITE_PROTECT_NP
	if (__builtin_available (macOS 11, *)) {
		int level = GPOINTER_TO_INT (mono_native_tls_get_value (write_level_tls_id));
		level ++;
		mono_native_tls_set_value (write_level_tls_id, GINT_TO_POINTER (level));
		pthread_jit_write_protect_np (0);
	}
#elif defined(HOST_MACCAT) && defined(__aarch64__)
	/* JITing in Catalyst apps is not allowed on Apple Silicon, so assume if we're here we don't really have executable pages. */
#endif
}

/*
 * mono_codeman_disable_write ():
 *
 *   Disable writing to code memory on the current thread on platforms that need it.
 * Calls can be nested.
 */
void
mono_codeman_disable_write (void)
{
	if (codeman_no_exec)
		return;
#ifdef HAVE_PTHREAD_JIT_WRITE_PROTECT_NP
	if (__builtin_available (macOS 11, *)) {
		int level = GPOINTER_TO_INT (mono_native_tls_get_value (write_level_tls_id));
		g_assert (level);
		level --;
		mono_native_tls_set_value (write_level_tls_id, GINT_TO_POINTER (level));
		if (level == 0)
			pthread_jit_write_protect_np (1);
	}
#elif defined(HOST_MACCAT) && defined(__aarch64__)
	/* JITing in Catalyst apps is not allowed on Apple Silicon, so assume if we're here we don't really have executable pages */
#endif
}
