#ifndef _MONO_MEMPOOL_H_
#define _MONO_MEMPOOL_H_

typedef struct _MonoMemPool MonoMemPool;

MonoMemPool *
mono_mempool_new           (void);

void
mono_mempool_destroy       (MonoMemPool *pool);

void
mono_mempool_empty         (MonoMemPool *pool);

void
mono_mempool_stats         (MonoMemPool *pool);

gpointer
mono_mempool_alloc         (MonoMemPool *pool, 
			    guint        size);

gpointer
mono_mempool_alloc0        (MonoMemPool *pool, 
			    guint        size);

#endif
