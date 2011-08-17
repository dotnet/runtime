/*
 * mempool.c: efficient memory allocation
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
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include "mempool.h"
#include "mempool-internals.h"

#if USE_MALLOC_FOR_MEMPOOLS
#define MALLOC_ALLOCATION
#endif

/*
 * MonoMemPool is for fast allocation of memory. We free
 * all memory when the pool is destroyed.
 */

#define MEM_ALIGN 8

#if MONO_SMALL_CONFIG
#define MONO_MEMPOOL_PAGESIZE 4096
#define MONO_MEMPOOL_MINSIZE 256
#else
#define MONO_MEMPOOL_PAGESIZE 8192
#define MONO_MEMPOOL_MINSIZE 512
#endif

#ifndef G_LIKELY
#define G_LIKELY(a) (a)
#define G_UNLIKELY(a) (a)
#endif

#ifdef MALLOC_ALLOCATION
typedef struct _Chunk {
	struct _Chunk *next;
	guint32 size;
} Chunk;

struct _MonoMemPool {
	Chunk *chunks;
	guint32 allocated;
};
#else
struct _MonoMemPool {
	MonoMemPool *next;
	gint rest;
	guint8 *pos, *end;
	guint32 size;
	union {
		double pad; /* to assure proper alignment */
		guint32 allocated;
	} d;
};
#endif

static long total_bytes_allocated = 0;

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

MonoMemPool *
mono_mempool_new_size (int initial_size)
{
#ifdef MALLOC_ALLOCATION
	return g_new0 (MonoMemPool, 1);
#else
	MonoMemPool *pool;
	if (initial_size < MONO_MEMPOOL_MINSIZE)
		initial_size = MONO_MEMPOOL_MINSIZE;
	pool = g_malloc (initial_size);

	pool->next = NULL;
	pool->pos = (guint8*)pool + sizeof (MonoMemPool);
	pool->end = pool->pos + initial_size - sizeof (MonoMemPool);
	pool->d.allocated = pool->size = initial_size;
	total_bytes_allocated += initial_size;
	return pool;
#endif
}

/**
 * mono_mempool_destroy:
 * @pool: the memory pool to destroy
 *
 * Free all memory associated with this pool.
 */
void
mono_mempool_destroy (MonoMemPool *pool)
{
#ifdef MALLOC_ALLOCATION
	mono_mempool_empty (pool);

	g_free (pool);
#else
	MonoMemPool *p, *n;

	total_bytes_allocated -= pool->d.allocated;

	p = pool;
	while (p) {
		n = p->next;
		g_free (p);
		p = n;
	}
#endif
}

/**
 * mono_mempool_invalidate:
 * @pool: the memory pool to invalidate
 *
 * Fill the memory associated with this pool to 0x2a (42). Useful for debugging.
 */
void
mono_mempool_invalidate (MonoMemPool *pool)
{
#ifdef MALLOC_ALLOCATION
	g_assert_not_reached ();
#else
	MonoMemPool *p, *n;

	p = pool;
	while (p) {
		n = p->next;
		memset (p, 42, p->size);
		p = n;
	}
#endif
}

void
mono_mempool_empty (MonoMemPool *pool)
{
#ifdef MALLOC_ALLOCATION
	Chunk *p, *n;

	p = pool->chunks;
	pool->chunks = NULL;
	while (p) {
		n = p->next;
		g_free (p);
		p = n;
	}

	pool->allocated = 0;
#else
	pool->pos = (guint8*)pool + sizeof (MonoMemPool);
	pool->end = pool->pos + pool->size - sizeof (MonoMemPool);
#endif
}

/**
 * mono_mempool_stats:
 * @pool: the momory pool we need stats for
 *
 * Print a few stats about the mempool
 */
void
mono_mempool_stats (MonoMemPool *pool)
{
#ifdef MALLOC_ALLOCATION
	g_assert_not_reached ();
#else
	MonoMemPool *p;
	int count = 0;
	guint32 still_free = 0;

	p = pool;
	while (p) {
		still_free += p->end - p->pos;
		p = p->next;
		count++;
	}
	if (pool) {
		g_print ("Mempool %p stats:\n", pool);
		g_print ("Total mem allocated: %d\n", pool->d.allocated);
		g_print ("Num chunks: %d\n", count);
		g_print ("Free memory: %d\n", still_free);
	}
#endif
}

#ifndef MALLOC_ALLOCATION
#ifdef TRACE_ALLOCATIONS
#include <execinfo.h>
#include "metadata/appdomain.h"
#include "metadata/metadata-internals.h"

static CRITICAL_SECTION mempool_tracing_lock;
#define BACKTRACE_DEPTH 7
static void
mono_backtrace (int size)
{
        void *array[BACKTRACE_DEPTH];
        char **names;
        int i, symbols;
        static gboolean inited;

        if (!inited) {
            InitializeCriticalSection (&mempool_tracing_lock);
            inited = TRUE;
        }

        EnterCriticalSection (&mempool_tracing_lock);
        g_print ("Allocating %d bytes\n", size);
        symbols = backtrace (array, BACKTRACE_DEPTH);
        names = backtrace_symbols (array, symbols);
        for (i = 1; i < symbols; ++i) {
                g_print ("\t%s\n", names [i]);
        }
        free (names);
        LeaveCriticalSection (&mempool_tracing_lock);
}

#endif

static int
get_next_size (MonoMemPool *pool, int size)
{
	int target = pool->next? pool->next->size: pool->size;
	size += sizeof (MonoMemPool);
	/* increase the size */
	target += target / 2;
	while (target < size) {
		target += target / 2;
	}
	if (target > MONO_MEMPOOL_PAGESIZE && size <= MONO_MEMPOOL_PAGESIZE)
		target = MONO_MEMPOOL_PAGESIZE;
	return target;
}
#endif

/**
 * mono_mempool_alloc:
 * @pool: the momory pool to use
 * @size: size of the momory block
 *
 * Allocates a new block of memory in @pool.
 *
 * Returns: the address of a newly allocated memory block.
 */
gpointer
mono_mempool_alloc (MonoMemPool *pool, guint size)
{
	gpointer rval;
	
	size = (size + MEM_ALIGN - 1) & ~(MEM_ALIGN - 1);

#ifdef MALLOC_ALLOCATION
	{
		Chunk *c = g_malloc (size);

		c->next = pool->chunks;
		pool->chunks = c;
		c->size = size - sizeof(Chunk);

		pool->allocated += size;

		rval = ((guint8*)c) + sizeof (Chunk);
	}
#else
	rval = pool->pos;
	pool->pos = (guint8*)rval + size;

#ifdef TRACE_ALLOCATIONS
	if (pool == mono_get_corlib ()->mempool) {
		mono_backtrace (size);
	}
#endif
	if (G_UNLIKELY (pool->pos >= pool->end)) {
		pool->pos -= size;
		if (size >= 4096) {
			MonoMemPool *np = g_malloc (sizeof (MonoMemPool) + size);
			np->next = pool->next;
			pool->next = np;
			np->pos = (guint8*)np + sizeof (MonoMemPool);
			np->size = sizeof (MonoMemPool) + size;
			np->end = np->pos + np->size - sizeof (MonoMemPool);
			pool->d.allocated += sizeof (MonoMemPool) + size;
			total_bytes_allocated += sizeof (MonoMemPool) + size;
			return (guint8*)np + sizeof (MonoMemPool);
		} else {
			int new_size = get_next_size (pool, size);
			MonoMemPool *np = g_malloc (new_size);
			np->next = pool->next;
			pool->next = np;
			pool->pos = (guint8*)np + sizeof (MonoMemPool);
			np->pos = (guint8*)np + sizeof (MonoMemPool);
			np->size = new_size;
			np->end = np->pos;
			pool->end = pool->pos + new_size - sizeof (MonoMemPool);
			pool->d.allocated += new_size;
			total_bytes_allocated += new_size;

			rval = pool->pos;
			pool->pos += size;
		}
	}
#endif

	return rval;
}

/**
 * mono_mempool_alloc0:
 *
 * same as mono_mempool_alloc, but fills memory with zero.
 */
gpointer
mono_mempool_alloc0 (MonoMemPool *pool, guint size)
{
	gpointer rval;

#ifdef MALLOC_ALLOCATION
	rval = mono_mempool_alloc (pool, size);
#else
	size = (size + MEM_ALIGN - 1) & ~(MEM_ALIGN - 1);

	rval = pool->pos;
	pool->pos = (guint8*)rval + size;

	if (G_UNLIKELY (pool->pos >= pool->end)) {
		rval = mono_mempool_alloc (pool, size);
	}
#ifdef TRACE_ALLOCATIONS
	else if (pool == mono_get_corlib ()->mempool) {
		mono_backtrace (size);
	}
#endif
#endif

	memset (rval, 0, size);
	return rval;
}

/**
 * mono_mempool_contains_addr:
 *
 *  Determines whenever ADDR is inside the memory used by the mempool.
 */
gboolean
mono_mempool_contains_addr (MonoMemPool *pool,
							gpointer addr)
{
#ifdef MALLOC_ALLOCATION
	Chunk *c;

	c = pool->chunks;
	while (c) {
		guint8 *p = ((guint8*)c) + sizeof (Chunk);

		if (addr >= (gpointer)p && addr < (gpointer)(p + c->size))
			return TRUE;

		c = c->next;
	}
#else
	MonoMemPool *p;

	p = pool;
	while (p) {
		if (addr > (gpointer)p && addr <= (gpointer)((guint8*)p + p->size))
			return TRUE;
		p = p->next;
	}
#endif

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
	res = mono_mempool_alloc (pool, l + 1);
	memcpy (res, s, l + 1);

	return res;
}

/**
 * mono_mempool_get_allocated:
 *
 * Return the amount of memory allocated for this mempool.
 */
guint32
mono_mempool_get_allocated (MonoMemPool *pool)
{
#ifdef MALLOC_ALLOCATION
	return pool->allocated;
#else
	return pool->d.allocated;
#endif
}

/**
 * mono_mempool_get_bytes_allocated:
 *
 * Return the number of bytes currently allocated for mempools.
 */
long
mono_mempool_get_bytes_allocated (void)
{
	return total_bytes_allocated;
}
