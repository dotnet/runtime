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

#include "jit.h"

/*
 * MonoMemPool is for fast allocation of memory. We free
 * all memory when the pool is destroyed.
 */

#if SIZEOF_VOID_P > SIZEOF_LONG
#define MEM_ALIGN     SIZEOF_VOID_P
#else
#define MEM_ALIGN     SIZEOF_LONG
#endif

#define MONO_MEMPOOL_PAGESIZE 8192

struct _MonoMemPool {
	MonoMemPool *next;
	gint rest;
	gpointer pos;
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
	pool->pos = (gpointer)pool + sizeof (MonoMemPool);
	pool->rest = MONO_MEMPOOL_PAGESIZE - sizeof (MonoMemPool);
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
	g_assert (size < 256);

	size = (size + MEM_ALIGN - 1) & ~(MEM_ALIGN - 1);

	if (pool->rest < size) {
		MonoMemPool *np = g_malloc (MONO_MEMPOOL_PAGESIZE);
		np->next = pool->next;
		pool->next = np;
		pool->pos = (gpointer)np + sizeof (MonoMemPool);
		pool->rest = MONO_MEMPOOL_PAGESIZE - sizeof (MonoMemPool);
	}

	rval = pool->pos;
	pool->rest -= size;
	pool->pos += size;

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

