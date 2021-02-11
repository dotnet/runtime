/**
 * \file
 */

#ifndef _MONO_MEMPOOL_H_
#define _MONO_MEMPOOL_H_

#include <mono/utils/mono-publib.h>

typedef struct _MonoMemPool MonoMemPool;

MONO_API MonoMemPool *
mono_mempool_new           (void);

MONO_API MonoMemPool *
mono_mempool_new_size      (int initial_size);

MONO_API void
mono_mempool_destroy       (MonoMemPool *pool);

MONO_API void
mono_mempool_invalidate    (MonoMemPool *pool);

MONO_API void
mono_mempool_stats         (MonoMemPool *pool);

MONO_API void*
mono_mempool_alloc         (MonoMemPool *pool, unsigned int size);

#define mono_mempool_alloc(pool, size) (g_cast (mono_mempool_alloc ((pool), (size))))

MONO_API void*
mono_mempool_alloc0        (MonoMemPool *pool, unsigned int size);

#define mono_mempool_alloc0(pool, size) (g_cast (mono_mempool_alloc0 ((pool), (size))))

MONO_API mono_bool
mono_mempool_contains_addr (MonoMemPool *pool, void* addr);

MONO_API char*
mono_mempool_strdup        (MonoMemPool *pool, const char *s);

MONO_API uint32_t
mono_mempool_get_allocated (MonoMemPool *pool);

#endif
