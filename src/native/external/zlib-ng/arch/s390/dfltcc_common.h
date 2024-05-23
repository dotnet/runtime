#ifndef DFLTCC_COMMON_H
#define DFLTCC_COMMON_H

#include "zutil.h"

void Z_INTERNAL *PREFIX(dfltcc_alloc_window)(PREFIX3(streamp) strm, uInt items, uInt size);
void Z_INTERNAL PREFIX(dfltcc_copy_window)(void *dest, const void *src, size_t n);
void Z_INTERNAL PREFIX(dfltcc_free_window)(PREFIX3(streamp) strm, void *w);

#define ZFREE_STATE ZFREE

#define ZALLOC_WINDOW PREFIX(dfltcc_alloc_window)

#define ZCOPY_WINDOW PREFIX(dfltcc_copy_window)

#define ZFREE_WINDOW PREFIX(dfltcc_free_window)

#define TRY_FREE_WINDOW PREFIX(dfltcc_free_window)

#define DFLTCC_BLOCK_HEADER_BITS 3
#define DFLTCC_HLITS_COUNT_BITS 5
#define DFLTCC_HDISTS_COUNT_BITS 5
#define DFLTCC_HCLENS_COUNT_BITS 4
#define DFLTCC_MAX_HCLENS 19
#define DFLTCC_HCLEN_BITS 3
#define DFLTCC_MAX_HLITS 286
#define DFLTCC_MAX_HDISTS 30
#define DFLTCC_MAX_HLIT_HDIST_BITS 7
#define DFLTCC_MAX_SYMBOL_BITS 16
#define DFLTCC_MAX_EOBS_BITS 15
#define DFLTCC_MAX_PADDING_BITS 7

#define DEFLATE_BOUND_COMPLEN(source_len) \
    ((DFLTCC_BLOCK_HEADER_BITS + \
      DFLTCC_HLITS_COUNT_BITS + \
      DFLTCC_HDISTS_COUNT_BITS + \
      DFLTCC_HCLENS_COUNT_BITS + \
      DFLTCC_MAX_HCLENS * DFLTCC_HCLEN_BITS + \
      (DFLTCC_MAX_HLITS + DFLTCC_MAX_HDISTS) * DFLTCC_MAX_HLIT_HDIST_BITS + \
      (source_len) * DFLTCC_MAX_SYMBOL_BITS + \
      DFLTCC_MAX_EOBS_BITS + \
      DFLTCC_MAX_PADDING_BITS) >> 3)

#endif
