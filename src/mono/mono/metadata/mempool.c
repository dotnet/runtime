/**
 * \file
 * efficient memory allocation
 *
 * MonoMemPool is for fast allocation of memory. We free
 * all memory when the pool is destroyed.
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc. (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include "mempool.h"
#include "mempool-internals.h"
#include "utils/unlocked.h"

/*
 * MonoMemPool is for fast allocation of memory. We free
 * all memory when the pool is destroyed.
 */

#define MEM_ALIGN 8
#define ALIGN_SIZE(s)	(((s) + MEM_ALIGN - 1) & ~(MEM_ALIGN - 1))

// Size of memory at start of mempool reserved for header
#define SIZEOF_MEM_POOL	(ALIGN_SIZE (sizeof (MonoMemPool)))

#if MONO_SMALL_CONFIG
#define MONO_MEMPOOL_PAGESIZE 4096
#define MONO_MEMPOOL_MINSIZE 256
#else
#define MONO_MEMPOOL_PAGESIZE 8192
#define MONO_MEMPOOL_MINSIZE 512
#endif

// The USE_MALLOC_FOR_MEMPOOLS debug-build flag causes mempools to be allocated in single-element blocks, so tools like Valgrind can run better.
#if USE_MALLOC_FOR_MEMPOOLS
#define INDIVIDUAL_ALLOCATIONS
#define MONO_MEMPOOL_PREFER_INDIVIDUAL_ALLOCATION_SIZE 0
#else
#define MONO_MEMPOOL_PREFER_INDIVIDUAL_ALLOCATION_SIZE MONO_MEMPOOL_PAGESIZE
#endif

#ifndef G_LIKELY
#define G_LIKELY(a) (a)
#define G_UNLIKELY(a) (a)
#endif

// A mempool is a linked list of memory blocks, each of which begins with this header structure.
// The initial block in the linked list is special, and tracks additional information.
struct _MonoMemPool {
	// Next block after this one in linked list
	MonoMemPool *next;

	// Size of this memory block only
	guint32 size;

	// Used in "initial block" only: Beginning of current free space in mempool (may be in some block other than the first one)
	guint8 *pos;

	// Used in "initial block" only: End of current free space in mempool (ie, the first byte following the end of usable space)
	guint8 *end;

	union {
		// Unused: Imposing floating point memory rules on _MonoMemPool's final field ensures proper alignment of whole header struct
		double pad;

		// Used in "initial block" only: Number of bytes so far allocated (whether used or not) in the whole mempool
		guint32 allocated;
	} d;
};

static gint64 total_bytes_allocated = 0;

/**
 * mono_mempool_new:
 *
 * Returns: a new memory pool.
 */
MonoMemPool *
mono_mempool_new (void)
{
	return mono_mempool_new_size (MONO_MEMPOOL_PAGESIZE);
}

/**
 * mono_mempool_new_size:
 * \param initial_size the amount of memory to initially reserve for the memory pool.
 * \returns a new memory pool with a specific initial memory reservation.
 */
MonoMemPool *
mono_mempool_new_size (int initial_size)
{
	MonoMemPool *pool;

#ifdef INDIVIDUAL_ALLOCATIONS
	// In individual allocation mode, create initial block with zero storage space.
	initial_size = SIZEOF_MEM_POOL;
#else
	if (initial_size < MONO_MEMPOOL_MINSIZE)
		initial_size = MONO_MEMPOOL_MINSIZE;
#endif

	pool = (MonoMemPool *)g_malloc (initial_size);

	pool->next = NULL;
	pool->pos = (guint8*)pool + SIZEOF_MEM_POOL; // Start after header
	pool->end = (guint8*)pool + initial_size;    // End at end of allocated space
	pool->d.allocated = pool->size = initial_size;
	UnlockedAdd64 (&total_bytes_allocated, initial_size);
	return pool;
}

/**
 * mono_mempool_destroy:
 * \param pool the memory pool to destroy
 *
 * Free all memory associated with this pool.
 */
void
mono_mempool_destroy (MonoMemPool *pool)
{
	MonoMemPool *p, *n;

	UnlockedSubtract64 (&total_bytes_allocated, pool->d.allocated);

	p = pool;
	while (p) {
		n = p->next;
		g_free (p);
		p = n;
	}
}

/**
 * mono_mempool_invalidate:
 * \param pool the memory pool to invalidate
 *
 * Fill the memory associated with this pool to 0x2a (42). Useful for debugging.
 */
void
mono_mempool_invalidate (MonoMemPool *pool)
{
	MonoMemPool *p, *n;

	p = pool;
	while (p) {
		n = p->next;
		memset (p, 42, p->size);
		p = n;
	}
}

/**
 * mono_mempool_stats:
 * \param pool the memory pool we need stats for
 *
 * Print a few stats about the mempool:
 * - Total memory allocated (malloced) by mem pool
 * - Number of chunks/blocks memory is allocated in
 * - How much memory is available to dispense before a new malloc must occur?
 */
void
mono_mempool_stats (MonoMemPool *pool)
{
	MonoMemPool *p;
	int count = 0;
	guint32 still_free;

	p = pool;
	while (p) {
		p = p->next;
		count++;
	}
	if (pool) {
		still_free = pool->end - pool->pos;
		g_print ("Mempool %p stats:\n", pool);
		g_print ("Total mem allocated: %d\n", pool->d.allocated);
		g_print ("Num chunks: %d\n", count);
		g_print ("Free memory: %d\n", still_free);
	}
}

#ifdef TRACE_ALLOCATIONS
#include <execinfo.h>
#include "metadata/appdomain.h"
#include "metadata/metadata-internals.h"

static mono_mutex_t mempool_tracing_lock;
#define BACKTRACE_DEPTH 7
static void
mono_backtrace (int size)
{
	void *array[BACKTRACE_DEPTH];
	char **names;
	int i, symbols;
	static gboolean inited;

	if (!inited) {
		mono_os_mutex_init_recursive (&mempool_tracing_lock);
		inited = TRUE;
	}

	mono_os_mutex_lock (&mempool_tracing_lock);
	g_print ("Allocating %d bytes\n", size);
	MONO_ENTER_GC_SAFE;
	symbols = backtrace (array, BACKTRACE_DEPTH);
	names = backtrace_symbols (array, symbols);
	MONO_EXIT_GC_SAFE;
	for (i = 1; i < symbols; ++i) {
		g_print ("\t%s\n", names [i]);
	}
	g_free (names);
	mono_os_mutex_unlock (&mempool_tracing_lock);
}

#endif

/**
 * get_next_size:
 * @pool: the memory pool to use
 * @size: size of the memory entity we are trying to allocate
 *
 * A mempool is growing; give a recommended size for the next block.
 * Each block in a mempool should be about 150% bigger than the previous one,
 * or bigger if it is necessary to include the new entity.
 *
 * Returns: the recommended size.
 */
static guint
get_next_size (MonoMemPool *pool, int size)
{
	int target = pool->next? pool->next->size: pool->size;
	size += SIZEOF_MEM_POOL;
	/* increase the size */
	target += target / 2;
	while (target < size) {
		target += target / 2;
	}
	if (target > MONO_MEMPOOL_PAGESIZE && size <= MONO_MEMPOOL_PAGESIZE)
		target = MONO_MEMPOOL_PAGESIZE;
	return target;
}

/**
 * mono_mempool_alloc:
 * \param pool the memory pool to use
 * \param size size of the memory block
 *
 * Allocates a new block of memory in \p pool .
 *
 * \returns the address of a newly allocated memory block.
 */
gpointer
(mono_mempool_alloc) (MonoMemPool *pool, guint size)
{
	gpointer rval = pool->pos; // Return value

	// Normal case: Just bump up pos pointer and we are done
	size = ALIGN_SIZE (size);
	pool->pos = (guint8*)rval + size;

#ifdef TRACE_ALLOCATIONS
	if (pool == mono_get_corlib ()->mempool) {
		mono_backtrace (size);
	}
#endif

	// If we have just overflowed the current block, we need to back up and try again.
	if (G_UNLIKELY (pool->pos >= pool->end)) {
		pool->pos -= size;  // Back out

		// For large objects, allocate the object into its own block.
		// (In individual allocation mode, the constant will be 0 and this path will always be taken)
		if (size >= MONO_MEMPOOL_PREFER_INDIVIDUAL_ALLOCATION_SIZE) {
			guint new_size = SIZEOF_MEM_POOL + size;
			MonoMemPool *np = (MonoMemPool *)g_malloc (new_size);

			np->next = pool->next;
			np->size = new_size;
			pool->next = np;
			pool->d.allocated += new_size;
			UnlockedAdd64 (&total_bytes_allocated, new_size);

			rval = (guint8*)np + SIZEOF_MEM_POOL;
		} else {
			// Notice: any unused memory at the end of the old head becomes simply abandoned in this case until the mempool is freed (see Bugzilla #35136)
			guint new_size = get_next_size (pool, size);
			MonoMemPool *np = (MonoMemPool *)g_malloc (new_size);

			np->next = pool->next;
			np->size = new_size;
			pool->next = np;
			pool->pos = (guint8*)np + SIZEOF_MEM_POOL;
			pool->end = (guint8*)np + new_size;
			pool->d.allocated += new_size;
			UnlockedAdd64 (&total_bytes_allocated, new_size);

			rval = pool->pos;
			pool->pos += size;
		}
	}

	return rval;
}

/**
 * mono_mempool_alloc0:
 *
 * same as \c mono_mempool_alloc, but fills memory with zero.
 */
gpointer
(mono_mempool_alloc0) (MonoMemPool *pool, guint size)
{
	size = ALIGN_SIZE (size);
	const gpointer rval = mono_mempool_alloc (pool, size);
	if (rval)
		memset (rval, 0, size);
	return rval;
}

/**
 * mono_mempool_contains_addr:
 *
 * Determines whether \p addr is inside the memory used by the mempool.
 */
gboolean
mono_mempool_contains_addr (MonoMemPool *pool,
							gpointer addr)
{
	MonoMemPool *p = pool;

	while (p) {
		if (addr >= (gpointer)p && addr < (gpointer)((guint8*)p + p->size))
			return TRUE;
		p = p->next;
	}

	return FALSE;
}

/**
 * mono_mempool_strdup:
 *
 * Same as strdup, but allocates memory from the mempool.
 * Returns: a pointer to the newly allocated string data inside the mempool.
 */
char*
mono_mempool_strdup (MonoMemPool *pool,
					 const char *s)
{
	int l;
	char *res;

	if (s == NULL)
		return NULL;

	l = strlen (s);
	res = (char *)mono_mempool_alloc (pool, l + 1);
	memcpy (res, s, l + 1);

	return res;
}

char*
mono_mempool_strdup_vprintf (MonoMemPool *pool, const char *format, va_list args)
{
	size_t buflen;
	char *buf;
	va_list args2;
	va_copy (args2, args);
	int len = vsnprintf (NULL, 0, format, args2);
	va_end (args2);

	if (len >= 0 && (buf = (char*)mono_mempool_alloc (pool, (buflen = (size_t) (len + 1)))) != NULL) {
		vsnprintf (buf, buflen, format, args);
	} else {
		buf = NULL;
	}
	return buf;
}

char*
mono_mempool_strdup_printf (MonoMemPool *pool, const char *format, ...)
{
	char *buf;
	va_list args;
	va_start (args, format);
	buf = mono_mempool_strdup_vprintf (pool, format, args);
	va_end (args);
	return buf;
}

/**
 * mono_mempool_get_allocated:
 *
 * Return the amount of memory allocated for this mempool.
 */
guint32
mono_mempool_get_allocated (MonoMemPool *pool)
{
	return pool->d.allocated;
}

/**
 * mono_mempool_get_bytes_allocated:
 *
 * Return the number of bytes currently allocated for mempools.
 */
long
mono_mempool_get_bytes_allocated (void)
{
	return UnlockedRead64 (&total_bytes_allocated);
}
