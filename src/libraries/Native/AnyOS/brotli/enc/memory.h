/* Copyright 2016 Google Inc. All Rights Reserved.

   Distributed under MIT license.
   See file LICENSE for detail or copy at https://opensource.org/licenses/MIT
*/

/* Macros for memory management. */

#ifndef BROTLI_ENC_MEMORY_H_
#define BROTLI_ENC_MEMORY_H_

#include <brotli/types.h>
#include "./port.h"

#if defined(__cplusplus) || defined(c_plusplus)
extern "C" {
#endif

#if !defined(BROTLI_ENCODER_CLEANUP_ON_OOM) && \
    !defined(BROTLI_ENCODER_EXIT_ON_OOM)
#define BROTLI_ENCODER_EXIT_ON_OOM
#endif

typedef struct MemoryManager {
  brotli_alloc_func alloc_func;
  brotli_free_func free_func;
  void* opaque;
#if !defined(BROTLI_ENCODER_EXIT_ON_OOM)
  BROTLI_BOOL is_oom;
  size_t perm_allocated;
  size_t new_allocated;
  size_t new_freed;
  void* pointers[256];
#endif  /* BROTLI_ENCODER_EXIT_ON_OOM */
} MemoryManager;

BROTLI_INTERNAL void BrotliInitMemoryManager(
    MemoryManager* m, brotli_alloc_func alloc_func, brotli_free_func free_func,
    void* opaque);

BROTLI_INTERNAL void* BrotliAllocate(MemoryManager* m, size_t n);
#define BROTLI_ALLOC(M, T, N)                               \
  ((N) > 0 ? ((T*)BrotliAllocate((M), (N) * sizeof(T))) : NULL)

BROTLI_INTERNAL void BrotliFree(MemoryManager* m, void* p);
#define BROTLI_FREE(M, P) { \
  BrotliFree((M), (P));     \
  P = NULL;                 \
}

#if defined(BROTLI_ENCODER_EXIT_ON_OOM)
#define BROTLI_IS_OOM(M) (!!0)
#else  /* BROTLI_ENCODER_EXIT_ON_OOM */
#define BROTLI_IS_OOM(M) (!!(M)->is_oom)
#endif  /* BROTLI_ENCODER_EXIT_ON_OOM */

BROTLI_INTERNAL void BrotliWipeOutMemoryManager(MemoryManager* m);

#if defined(__cplusplus) || defined(c_plusplus)
}  /* extern "C" */
#endif

#endif  /* BROTLI_ENC_MEMORY_H_ */
