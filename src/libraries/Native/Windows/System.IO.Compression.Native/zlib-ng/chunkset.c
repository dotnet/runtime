/* chunkset.c -- inline functions to copy small data chunks.
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include "zbuild.h"
#include "zutil.h"

// We need sizeof(chunk_t) to be 8, no matter what.
#if defined(UNALIGNED64_OK)
typedef uint64_t chunk_t;
#elif defined(UNALIGNED_OK)
typedef struct chunk_t { uint32_t u32[2]; } chunk_t;
#else
typedef struct chunk_t { uint8_t u8[8]; } chunk_t;
#endif

#define CHUNK_SIZE 8

#define HAVE_CHUNKMEMSET_1
#define HAVE_CHUNKMEMSET_4
#define HAVE_CHUNKMEMSET_8

static inline void chunkmemset_1(uint8_t *from, chunk_t *chunk) {
#if defined(UNALIGNED64_OK)
    *chunk = 0x0101010101010101 * (uint8_t)*from;
#elif defined(UNALIGNED_OK)
    chunk->u32[0] = 0x01010101 * (uint8_t)*from;
    chunk->u32[1] = chunk->u32[0];
#else
    memset(chunk, *from, sizeof(chunk_t));
#endif
}

static inline void chunkmemset_4(uint8_t *from, chunk_t *chunk) {
#if defined(UNALIGNED64_OK)
    uint32_t half_chunk;
    half_chunk = *(uint32_t *)from;
    *chunk = 0x0000000100000001 * (uint64_t)half_chunk;
#elif defined(UNALIGNED_OK)
    chunk->u32[0] = *(uint32_t *)from;
    chunk->u32[1] = chunk->u32[0];
#else
    uint8_t *chunkptr = (uint8_t *)chunk;
    memcpy(chunkptr, from, 4);
    memcpy(chunkptr+4, from, 4);
#endif
}

static inline void chunkmemset_8(uint8_t *from, chunk_t *chunk) {
#if defined(UNALIGNED64_OK)
    *chunk = *(uint64_t *)from;
#elif defined(UNALIGNED_OK)
    uint32_t* p = (uint32_t *)from;
    chunk->u32[0] = p[0];
    chunk->u32[1] = p[1];
#else
    memcpy(chunk, from, sizeof(chunk_t));
#endif
}

static inline void loadchunk(uint8_t const *s, chunk_t *chunk) {
    chunkmemset_8((uint8_t *)s, chunk);
}

static inline void storechunk(uint8_t *out, chunk_t *chunk) {
#if defined(UNALIGNED64_OK)
    *(uint64_t *)out = *chunk;
#elif defined(UNALIGNED_OK)
    ((uint32_t *)out)[0] = chunk->u32[0];
    ((uint32_t *)out)[1] = chunk->u32[1];
#else
    memcpy(out, chunk, sizeof(chunk_t));
#endif
}

#define CHUNKSIZE        chunksize_c
#define CHUNKCOPY        chunkcopy_c
#define CHUNKCOPY_SAFE   chunkcopy_safe_c
#define CHUNKUNROLL      chunkunroll_c
#define CHUNKMEMSET      chunkmemset_c
#define CHUNKMEMSET_SAFE chunkmemset_safe_c

#include "chunkset_tpl.h"
