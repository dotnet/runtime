#ifndef _MONO_MEMPOOL_H_
#define _MONO_MEMPOOL_H_

G_BEGIN_DECLS

typedef struct _MonoMemPool MonoMemPool;

MonoMemPool *
mono_mempool_new           (void);

MonoMemPool *
mono_mempool_new_size      (int initial_size);

void
mono_mempool_destroy       (MonoMemPool *pool);

void
mono_mempool_invalidate    (MonoMemPool *pool);

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

gboolean
mono_mempool_contains_addr (MonoMemPool *pool,
			    gpointer addr);

char*
mono_mempool_strdup        (MonoMemPool *pool,
							const char *s);

guint32
mono_mempool_get_allocated (MonoMemPool *pool);

G_END_DECLS

#endif
