/*
 * mempool.c: efficient memory allocation
 *
 * MonoMemPool is for fast allocation of memory. We free
 * all memory when the pool is destroyed.
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include "mempool.h"

/*
 * MonoMemPool is for fast allocation of memory. We free
 * all memory when the pool is destroyed.
 */

#define MEM_ALIGN 8

#define MONO_MEMPOOL_PAGESIZE 8192

struct _MonoMemPool {
	MonoMemPool *next;
	gint rest;
	gpointer pos;
	union {
		double pad; /* to assure proper alignment */
		guint32 allocated;
	} d;
};

/**
 * mono_mempool_new:
 *
 * Returns: a new memory pool.
 */
MonoMemPool *
mono_mempool_new ()
{
	MonoMemPool *pool = g_malloc (MONO_MEMPOOL_PAGESIZE);

	pool->next = NULL;
	pool->pos = (char *)pool + sizeof (MonoMemPool);
	pool->rest = MONO_MEMPOOL_PAGESIZE - sizeof (MonoMemPool);
	pool->d.allocated = MONO_MEMPOOL_PAGESIZE;
	return pool;
}

/**
 * mono_mempool_destroy:
 * @pool: the momory pool to destroy
 *
 * Free all memory associated with this pool.
 */
void
mono_mempool_destroy (MonoMemPool *pool)
{
	MonoMemPool *p, *n;

	p = pool;
	while (p) {
		n = p->next;
		g_free (p);
		p = n;
	}
}

void
mono_mempool_empty (MonoMemPool *pool)
{
	pool->pos = (char *)pool + sizeof (MonoMemPool);
	pool->rest = MONO_MEMPOOL_PAGESIZE - sizeof (MonoMemPool);
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
	MonoMemPool *p, *n;
	int count = 0;
	guint32 still_free = 0;

	p = pool;
	while (p) {
		still_free += p->rest;
		n = p->next;
		p = n;
		count++;
	}
	if (pool) {
		g_print ("Mempool %p stats:\n", pool);
		g_print ("Total mem allocated: %d\n", pool->d.allocated);
		g_print ("Num chunks: %d\n", count);
		g_print ("Free memory: %d\n", still_free);
	}
}

/**
 * mono_mempool_alloc:
 * @pool: the momory pool to destroy
 * @size: size of the momory block
 *
 * Allocates a new block of memory in @pool. @size must 
 * be smaller than 256.
 *
 * Returns: the address of a newly allocated memory block.
 */
gpointer
mono_mempool_alloc (MonoMemPool *pool, guint size)
{
	gpointer rval;
	
	g_assert (pool != NULL);

	size = (size + MEM_ALIGN - 1) & ~(MEM_ALIGN - 1);

	if (pool->rest < size) {
		if (size >= 4096) {
			MonoMemPool *np = g_malloc (sizeof (MonoMemPool) + size);
			np->next = pool->next;
			pool->next = np;
			pool->d.allocated += sizeof (MonoMemPool) + size;
			return (char *)np + sizeof (MonoMemPool);
		} else {
			MonoMemPool *np = g_malloc (MONO_MEMPOOL_PAGESIZE);
			np->next = pool->next;
			pool->next = np;
			pool->pos = (char *)np + sizeof (MonoMemPool);
			pool->rest = MONO_MEMPOOL_PAGESIZE - sizeof (MonoMemPool);
			pool->d.allocated += MONO_MEMPOOL_PAGESIZE;
		}
	}

	rval = pool->pos;
	pool->rest -= size;
	pool->pos = (char *)pool->pos + size;

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
	gpointer rval = mono_mempool_alloc (pool, size);
	memset (rval, 0, size);
	return rval;
}

