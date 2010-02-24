#ifndef _MONO_MEMPOOL_H_
#define _MONO_MEMPOOL_H_

#include <mono/utils/mono-publib.h>

MONO_BEGIN_DECLS

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

void*
mono_mempool_alloc         (MonoMemPool *pool, unsigned int size);

void*
mono_mempool_alloc0        (MonoMemPool *pool, unsigned int size);

mono_bool
mono_mempool_contains_addr (MonoMemPool *pool, void* addr);

char*
mono_mempool_strdup        (MonoMemPool *pool, const char *s);

uint32_t
mono_mempool_get_allocated (MonoMemPool *pool);

MONO_END_DECLS

#endif
