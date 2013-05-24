#include "config.h"

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <glib.h>

/* For dlmalloc.h */
#define USE_DL_PREFIX 1

#include "mono-codeman.h"
#include "mono-mmap.h"
#include "mono-counters.h"
#include "dlmalloc.h"
#include <mono/metadata/class-internals.h>
#include <mono/metadata/profiler-private.h>
#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#if defined(__native_client_codegen__) && defined(__native_client__)
#include <malloc.h>
#include <nacl/nacl_dyncode.h>
#include <mono/mini/mini.h>
#endif

static uintptr_t code_memory_used = 0;

/*
 * AMD64 processors maintain icache coherency only for pages which are 
 * marked executable. Also, windows DEP requires us to obtain executable memory from
 * malloc when using dynamic code managers. The system malloc can't do this so we use a 
 * slighly modified version of Doug Lea's Malloc package for this purpose:
 * http://g.oswego.edu/dl/html/malloc.html
 */

#define MIN_PAGES 16

#if defined(__ia64__) || defined(__x86_64__)
/*
 * We require 16 byte alignment on amd64 so the fp literals embedded in the code are 
 * properly aligned for SSE2.
 */
#define MIN_ALIGN 16
#else
#define MIN_ALIGN 8
#endif
#ifdef __native_client_codegen__
/* For Google Native Client, all targets of indirect control flow need to    */
/* be aligned to bundle boundary. 16 bytes on ARM, 32 bytes on x86.
 * MIN_ALIGN was updated to force alignment for calls from
 * tramp-<arch>.c to mono_global_codeman_reserve()     */
/* and mono_domain_code_reserve().                                           */
#undef MIN_ALIGN
#define MIN_ALIGN kNaClBundleSize

#endif

/* if a chunk has less than this amount of free space it's considered full */
#define MAX_WASTAGE 32
#define MIN_BSIZE 32

#ifdef __x86_64__
#define ARCH_MAP_FLAGS MONO_MMAP_32BIT
#else
#define ARCH_MAP_FLAGS 0
#endif

#define MONO_PROT_RWX (MONO_MMAP_READ|MONO_MMAP_WRITE|MONO_MMAP_EXEC)

typedef struct _CodeChunck CodeChunk;

enum {
	CODE_FLAG_MMAP,
	CODE_FLAG_MALLOC
};

struct _CodeChunck {
	char *data;
	int pos;
	int size;
	CodeChunk *next;
	unsigned int flags: 8;
	/* this number of bytes is available to resolve addresses far in memory */
	unsigned int bsize: 24;
};

struct _MonoCodeManager {
	int dynamic;
	int read_only;
	CodeChunk *current;
	CodeChunk *full;
#if defined(__native_client_codegen__) && defined(__native_client__)
	GHashTable *hash;
#endif
};

#define ALIGN_INT(val,alignment) (((val) + (alignment - 1)) & ~(alignment - 1))

#if defined(__native_client_codegen__) && defined(__native_client__)
/* End of text segment, set by linker. 
 * Dynamic text starts on the next allocated page.
 */
extern char etext[];
char *next_dynamic_code_addr = NULL;

/*
 * This routine gets the next available bundle aligned
 * pointer in the dynamic code section.  It does not check
 * for the section end, this error will be caught in the
 * service runtime.
 */
void*
allocate_code(intptr_t increment)
{
	char *addr;
	if (increment < 0) return NULL;
	increment = increment & kNaClBundleMask ? (increment & ~kNaClBundleMask) + kNaClBundleSize : increment;
	addr = next_dynamic_code_addr;
	next_dynamic_code_addr += increment;
	return addr;
}

int
nacl_is_code_address (void *target)
{
	return (char *)target < next_dynamic_code_addr;
}

/* Fill code buffer with arch-specific NOPs. */
void
mono_nacl_fill_code_buffer (guint8 *data, int size);

#ifndef USE_JUMP_TABLES
const int kMaxPatchDepth = 32;
__thread unsigned char **patch_source_base = NULL;
__thread unsigned char **patch_dest_base = NULL;
__thread int *patch_alloc_size = NULL;
__thread int patch_current_depth = -1;
__thread int allow_target_modification = 1;

static void
nacl_jit_check_init ()
{
	if (patch_source_base == NULL) {
		patch_source_base = g_malloc (kMaxPatchDepth * sizeof(unsigned char *));
		patch_dest_base = g_malloc (kMaxPatchDepth * sizeof(unsigned char *));
		patch_alloc_size = g_malloc (kMaxPatchDepth * sizeof(int));
	}
}
#endif

void
nacl_allow_target_modification (int val)
{
#ifndef USE_JUMP_TABLES
        allow_target_modification = val;
#endif /* USE_JUMP_TABLES */
}

/* Given a patch target, modify the target such that patching will work when
 * the code is copied to the data section.
 */
void*
nacl_modify_patch_target (unsigned char *target)
{
	/*
	 * There's no need in patch tricks for jumptables,
	 * as we always patch same jumptable.
	 */
#ifndef USE_JUMP_TABLES
	/* This seems like a bit of an ugly way to do this but the advantage
	 * is we don't have to worry about all the conditions in
	 * mono_resolve_patch_target, and it can be used by all the bare uses
	 * of <arch>_patch.
	 */
	unsigned char *sb;
	unsigned char *db;

	if (!allow_target_modification) return target;

	nacl_jit_check_init ();
	sb = patch_source_base[patch_current_depth];
	db = patch_dest_base[patch_current_depth];

	if (target >= sb && (target < sb + patch_alloc_size[patch_current_depth])) {
		/* Do nothing.  target is in the section being generated.
		 * no need to modify, the disp will be the same either way.
		 */
	} else {
		int target_offset = target - db;
		target = sb + target_offset;
	}
#endif
	return target;
}

void*
nacl_inverse_modify_patch_target (unsigned char *target)
{
	/*
	 * There's no need in patch tricks for jumptables,
	 * as we always patch same jumptable.
	 */
#ifndef USE_JUMP_TABLES
	unsigned char *sb;
	unsigned char *db;
	int target_offset;

	if (!allow_target_modification) return target;

	nacl_jit_check_init ();
	sb = patch_source_base[patch_current_depth];
	db = patch_dest_base[patch_current_depth];

	target_offset = target - sb;
	target = db + target_offset;
#endif
	return target;
}


#endif /* __native_client_codegen && __native_client__ */

#define VALLOC_FREELIST_SIZE 16

static CRITICAL_SECTION valloc_mutex;
static GHashTable *valloc_freelists;

static void*
codechunk_valloc (guint32 size)
{
	void *ptr;
	GSList *freelist;

	if (!valloc_freelists) {
		InitializeCriticalSection (&valloc_mutex);
		valloc_freelists = g_hash_table_new (NULL, NULL);
	}

	/*
	 * Keep a small freelist of memory blocks to decrease pressure on the kernel memory subsystem to avoid #3321.
	 */
	EnterCriticalSection (&valloc_mutex);
	freelist = g_hash_table_lookup (valloc_freelists, GUINT_TO_POINTER (size));
	if (freelist) {
		ptr = freelist->data;
		memset (ptr, 0, size);
		freelist = g_slist_remove_link (freelist, freelist);
		g_hash_table_insert (valloc_freelists, GUINT_TO_POINTER (size), freelist);
	} else {
		ptr = mono_valloc (NULL, size + MIN_ALIGN - 1, MONO_PROT_RWX | ARCH_MAP_FLAGS);
	}
	LeaveCriticalSection (&valloc_mutex);
	return ptr;
}

static void
codechunk_vfree (void *ptr, guint32 size)
{
	GSList *freelist;

	EnterCriticalSection (&valloc_mutex);
	freelist = g_hash_table_lookup (valloc_freelists, GUINT_TO_POINTER (size));
	if (!freelist || g_slist_length (freelist) < VALLOC_FREELIST_SIZE) {
		freelist = g_slist_prepend (freelist, ptr);
		g_hash_table_insert (valloc_freelists, GUINT_TO_POINTER (size), freelist);
	} else {
		mono_vfree (ptr, size);
	}
	LeaveCriticalSection (&valloc_mutex);
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
		GSList *freelist = value;
		GSList *l;

		for (l = freelist; l; l = l->next) {
			mono_vfree (l->data, GPOINTER_TO_UINT (key));
		}
		g_slist_free (freelist);
	}
	g_hash_table_destroy (valloc_freelists);
}

void
mono_code_manager_init (void)
{
}

void
mono_code_manager_cleanup (void)
{
	codechunk_cleanup ();
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
	MonoCodeManager *cman = malloc (sizeof (MonoCodeManager));
	if (!cman)
		return NULL;
	cman->current = NULL;
	cman->full = NULL;
	cman->dynamic = 0;
	cman->read_only = 0;
#if defined(__native_client_codegen__) && defined(__native_client__)
	if (next_dynamic_code_addr == NULL) {
		const guint kPageMask = 0xFFFF; /* 64K pages */
		next_dynamic_code_addr = (uintptr_t)(etext + kPageMask) & ~kPageMask;
#if defined (__GLIBC__)
		/* TODO: For now, just jump 64MB ahead to avoid dynamic libraries. */
		next_dynamic_code_addr += (uintptr_t)0x4000000;
#else
		/* Workaround bug in service runtime, unable to allocate */
		/* from the first page in the dynamic code section.    */
		next_dynamic_code_addr += (uintptr_t)0x10000;
#endif
	}
	cman->hash =  g_hash_table_new (NULL, NULL);
# ifndef USE_JUMP_TABLES
	if (patch_source_base == NULL) {
		patch_source_base = g_malloc (kMaxPatchDepth * sizeof(unsigned char *));
		patch_dest_base = g_malloc (kMaxPatchDepth * sizeof(unsigned char *));
		patch_alloc_size = g_malloc (kMaxPatchDepth * sizeof(int));
	}
# endif
#endif
	return cman;
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
	MonoCodeManager *cman = mono_code_manager_new ();
	cman->dynamic = 1;
	return cman;
}


static void
free_chunklist (CodeChunk *chunk)
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

	for (; chunk; ) {
		dead = chunk;
		mono_profiler_code_chunk_destroy ((gpointer) dead->data);
		chunk = chunk->next;
		if (dead->flags == CODE_FLAG_MMAP) {
			codechunk_vfree (dead->data, dead->size);
			/* valgrind_unregister(dead->data); */
		} else if (dead->flags == CODE_FLAG_MALLOC) {
			dlfree (dead->data);
		}
		code_memory_used -= dead->size;
		free (dead);
	}
}

/**
 * mono_code_manager_destroy:
 * @cman: a code manager
 *
 * Free all the memory associated with the code manager @cman.
 */
void
mono_code_manager_destroy (MonoCodeManager *cman)
{
	free_chunklist (cman->full);
	free_chunklist (cman->current);
	free (cman);
}

/**
 * mono_code_manager_invalidate:
 * @cman: a code manager
 *
 * Fill all the memory with an invalid native code value
 * so that any attempt to execute code allocated in the code
 * manager @cman will fail. This is used for debugging purposes.
 */
void             
mono_code_manager_invalidate (MonoCodeManager *cman)
{
	CodeChunk *chunk;

#if defined(__i386__) || defined(__x86_64__)
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
 * @cman: a code manager
 *
 * Make the code manager read only, so further allocation requests cause an assert.
 */
void             
mono_code_manager_set_read_only (MonoCodeManager *cman)
{
	cman->read_only = TRUE;
}

/**
 * mono_code_manager_foreach:
 * @cman: a code manager
 * @func: a callback function pointer
 * @user_data: additional data to pass to @func
 *
 * Invokes the callback @func for each different chunk of memory allocated
 * in the code manager @cman.
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

/* BIND_ROOM is the divisor for the chunck of code size dedicated
 * to binding branches (branches not reachable with the immediate displacement)
 * bind_size = size/BIND_ROOM;
 * we should reduce it and make MIN_PAGES bigger for such systems
 */
#if defined(__ppc__) || defined(__powerpc__)
#define BIND_ROOM 4
#endif
#if defined(__arm__)
#define BIND_ROOM 8
#endif

static CodeChunk*
new_codechunk (int dynamic, int size)
{
	int minsize, flags = CODE_FLAG_MMAP;
	int chunk_size, bsize = 0;
	int pagesize;
	CodeChunk *chunk;
	void *ptr;

#ifdef FORCE_MALLOC
	flags = CODE_FLAG_MALLOC;
#endif

	pagesize = mono_pagesize ();

	if (dynamic) {
		chunk_size = size;
		flags = CODE_FLAG_MALLOC;
	} else {
		minsize = pagesize * MIN_PAGES;
		if (size < minsize)
			chunk_size = minsize;
		else {
			chunk_size = size;
			chunk_size += pagesize - 1;
			chunk_size &= ~ (pagesize - 1);
		}
	}
#ifdef BIND_ROOM
	bsize = chunk_size / BIND_ROOM;
	if (bsize < MIN_BSIZE)
		bsize = MIN_BSIZE;
	bsize += MIN_ALIGN -1;
	bsize &= ~ (MIN_ALIGN - 1);
	if (chunk_size - size < bsize) {
		chunk_size = size + bsize;
		chunk_size += pagesize - 1;
		chunk_size &= ~ (pagesize - 1);
	}
#endif

	if (flags == CODE_FLAG_MALLOC) {
		ptr = dlmemalign (MIN_ALIGN, chunk_size + MIN_ALIGN - 1);
		if (!ptr)
			return NULL;
	} else {
		/* Allocate MIN_ALIGN-1 more than we need so we can still */
		/* guarantee MIN_ALIGN alignment for individual allocs    */
		/* from mono_code_manager_reserve_align.                  */
		ptr = codechunk_valloc (chunk_size);
		if (!ptr)
			return NULL;
	}

	if (flags == CODE_FLAG_MALLOC) {
#ifdef BIND_ROOM
		/* Make sure the thunks area is zeroed */
		memset (ptr, 0, bsize);
#endif
	}

	chunk = malloc (sizeof (CodeChunk));
	if (!chunk) {
		if (flags == CODE_FLAG_MALLOC)
			dlfree (ptr);
		else
			mono_vfree (ptr, chunk_size);
		return NULL;
	}
	chunk->next = NULL;
	chunk->size = chunk_size;
	chunk->data = ptr;
	chunk->flags = flags;
	chunk->pos = bsize;
	chunk->bsize = bsize;
	mono_profiler_code_chunk_new((gpointer) chunk->data, chunk->size);

	code_memory_used += chunk_size;
	mono_runtime_resource_check_limit (MONO_RESOURCE_JIT_CODE, code_memory_used);
	/*printf ("code chunk at: %p\n", ptr);*/
	return chunk;
}

/**
 * mono_code_manager_reserve:
 * @cman: a code manager
 * @size: size of memory to allocate
 * @alignment: power of two alignment value
 *
 * Allocates at least @size bytes of memory inside the code manager @cman.
 *
 * Returns: the pointer to the allocated memory or #NULL on failure
 */
void*
mono_code_manager_reserve_align (MonoCodeManager *cman, int size, int alignment)
{
#if !defined(__native_client__) || !defined(__native_client_codegen__)
	CodeChunk *chunk, *prev;
	void *ptr;
	guint32 align_mask = alignment - 1;

	g_assert (!cman->read_only);

	/* eventually allow bigger alignments, but we need to fix the dynamic alloc code to
	 * handle this before
	 */
	g_assert (alignment <= MIN_ALIGN);

	if (cman->dynamic) {
		++mono_stats.dynamic_code_alloc_count;
		mono_stats.dynamic_code_bytes_count += size;
	}

	if (!cman->current) {
		cman->current = new_codechunk (cman->dynamic, size);
		if (!cman->current)
			return NULL;
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
	chunk = new_codechunk (cman->dynamic, size);
	if (!chunk)
		return NULL;
	chunk->next = cman->current;
	cman->current = chunk;
	chunk->pos = ALIGN_INT (chunk->pos, alignment);
	/* Align the chunk->data we add to chunk->pos */
	/* or we can't guarantee proper alignment     */
	ptr = (void*)((((uintptr_t)chunk->data + align_mask) & ~(uintptr_t)align_mask) + chunk->pos);
	chunk->pos = ((char*)ptr - chunk->data) + size;
	return ptr;
#else
	unsigned char *temp_ptr, *code_ptr;
	/* Round up size to next bundle */
	alignment = kNaClBundleSize;
	size = (size + kNaClBundleSize) & (~kNaClBundleMask);
	/* Allocate a temp buffer */
	temp_ptr = memalign (alignment, size);
	g_assert (((uintptr_t)temp_ptr & kNaClBundleMask) == 0);
	/* Allocate code space from the service runtime */
	code_ptr = allocate_code (size);
	/* Insert pointer to code space in hash, keyed by buffer ptr */
	g_hash_table_insert (cman->hash, temp_ptr, code_ptr);

#ifndef USE_JUMP_TABLES
	nacl_jit_check_init ();

	patch_current_depth++;
	patch_source_base[patch_current_depth] = temp_ptr;
	patch_dest_base[patch_current_depth] = code_ptr;
	patch_alloc_size[patch_current_depth] = size;
	g_assert (patch_current_depth < kMaxPatchDepth);
#endif

	return temp_ptr;
#endif
}

/**
 * mono_code_manager_reserve:
 * @cman: a code manager
 * @size: size of memory to allocate
 *
 * Allocates at least @size bytes of memory inside the code manager @cman.
 *
 * Returns: the pointer to the allocated memory or #NULL on failure
 */
void*
mono_code_manager_reserve (MonoCodeManager *cman, int size)
{
	return mono_code_manager_reserve_align (cman, size, MIN_ALIGN);
}

/**
 * mono_code_manager_commit:
 * @cman: a code manager
 * @data: the pointer returned by mono_code_manager_reserve ()
 * @size: the size requested in the call to mono_code_manager_reserve ()
 * @newsize: the new size to reserve
 *
 * If we reserved too much room for a method and we didn't allocate
 * already from the code manager, we can get back the excess allocation
 * for later use in the code manager.
 */
void
mono_code_manager_commit (MonoCodeManager *cman, void *data, int size, int newsize)
{
#if !defined(__native_client__) || !defined(__native_client_codegen__)
	g_assert (newsize <= size);

	if (cman->current && (size != newsize) && (data == cman->current->data + cman->current->pos - size)) {
		cman->current->pos -= size - newsize;
	}
#else
	unsigned char *code;
	int status;
	g_assert (NACL_BUNDLE_ALIGN_UP(newsize) <= size);
	code = g_hash_table_lookup (cman->hash, data);
	g_assert (code != NULL);
	mono_nacl_fill_code_buffer ((uint8_t*)data + newsize, size - newsize);
	newsize = NACL_BUNDLE_ALIGN_UP(newsize);
	g_assert ((GPOINTER_TO_UINT (data) & kNaClBundleMask) == 0);
	g_assert ((newsize & kNaClBundleMask) == 0);
	status = nacl_dyncode_create (code, data, newsize);
	if (status != 0) {
		unsigned char *codep;
		fprintf(stderr, "Error creating Native Client dynamic code section attempted to be\n"
		                "emitted at %p (hex dissasembly of code follows):\n", code);
		for (codep = data; codep < data + newsize; codep++)
			fprintf(stderr, "%02x ", *codep);
		fprintf(stderr, "\n");
		g_assert_not_reached ();
	}
	g_hash_table_remove (cman->hash, data);
# ifndef USE_JUMP_TABLES
	g_assert (data == patch_source_base[patch_current_depth]);
	g_assert (code == patch_dest_base[patch_current_depth]);
	patch_current_depth--;
	g_assert (patch_current_depth >= -1);
# endif
	free (data);
#endif
}

#if defined(__native_client_codegen__) && defined(__native_client__)
void *
nacl_code_manager_get_code_dest (MonoCodeManager *cman, void *data)
{
	return g_hash_table_lookup (cman->hash, data);
}
#endif

/**
 * mono_code_manager_size:
 * @cman: a code manager
 * @used_size: pointer to an integer for the result
 *
 * This function can be used to get statistics about a code manager:
 * the integer pointed to by @used_size will contain how much
 * memory is actually used inside the code managed @cman.
 *
 * Returns: the amount of memory allocated in @cman
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

#ifdef __native_client_codegen__
# if defined(TARGET_ARM)
/* Fill empty space with UDF instruction used as halt on ARM. */
void
mono_nacl_fill_code_buffer (guint8 *data, int size)
{
        guint32* data32 = (guint32*)data;
        int i;
        g_assert(size % 4 == 0);
        for (i = 0; i < size / 4; i++)
                data32[i] = 0xE7FEDEFF;
}
# elif (defined(TARGET_X86) || defined(TARGET_AMD64))
/* Fill empty space with HLT instruction */
void
mono_nacl_fill_code_buffer(guint8 *data, int size)
{
        memset (data, 0xf4, size);
}
# else
#  error "Not ported"
# endif
#endif
