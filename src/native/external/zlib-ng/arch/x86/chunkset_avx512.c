/* chunkset_avx512.c -- AVX512 inline functions to copy small data chunks.
 * For conditions of distribution and use, see copyright notice in zlib.h
 */
#include "zbuild.h"
#include "zmemory.h"

#ifdef X86_AVX512

#include "avx2_tables.h"
#include <immintrin.h>
#include "x86_intrins.h"

typedef __m256i chunk_t;
typedef __m128i halfchunk_t;
typedef __mmask32 mask_t;
typedef __mmask16 halfmask_t;

#define HAVE_CHUNKMEMSET_2
#define HAVE_CHUNKMEMSET_4
#define HAVE_CHUNKMEMSET_8
#define HAVE_CHUNKMEMSET_16
#define HAVE_CHUNK_MAG
#define HAVE_HALF_CHUNK
#define HAVE_MASKED_READWRITE
#define HAVE_CHUNKCOPY
#define HAVE_HALFCHUNKCOPY

static inline halfmask_t gen_half_mask(unsigned len) {
   return (halfmask_t)_bzhi_u32(0xFFFF, len);
}

static inline mask_t gen_mask(unsigned len) {
   return (mask_t)_bzhi_u32(0xFFFFFFFF, len);
}

static inline void chunkmemset_2(uint8_t *from, chunk_t *chunk) {
    *chunk = _mm256_set1_epi16(zng_memread_2(from));
}

static inline void chunkmemset_4(uint8_t *from, chunk_t *chunk) {
    *chunk = _mm256_set1_epi32(zng_memread_4(from));
}

static inline void chunkmemset_8(uint8_t *from, chunk_t *chunk) {
    *chunk = _mm256_set1_epi64x(zng_memread_8(from));
}

static inline void chunkmemset_16(uint8_t *from, chunk_t *chunk) {
    /* Unfortunately there seems to be a compiler bug in Visual Studio 2015 where
     * the load is dumped to the stack with an aligned move for this memory-register
     * broadcast. The vbroadcasti128 instruction is 2 fewer cycles and this dump to
     * stack doesn't exist if compiled with optimizations. For the sake of working
     * properly in a debugger, let's take the 2 cycle penalty */
#if defined(_MSC_VER) && _MSC_VER <= 1900
    halfchunk_t half = _mm_loadu_si128((__m128i*)from);
    *chunk = _mm256_inserti128_si256(_mm256_castsi128_si256(half), half, 1);
#else
    *chunk = _mm256_broadcastsi128_si256(_mm_loadu_si128((__m128i*)from));
#endif
}

static inline void loadchunk(uint8_t const *s, chunk_t *chunk) {
    *chunk = _mm256_loadu_si256((__m256i *)s);
}

static inline void storechunk(uint8_t *out, chunk_t *chunk) {
    _mm256_storeu_si256((__m256i *)out, *chunk);
}

static inline uint8_t* CHUNKCOPY(uint8_t *out, uint8_t const *from, unsigned len) {
    Assert(len > 0, "chunkcopy should never have a length 0");

    chunk_t chunk;
    uint32_t rem = len % sizeof(chunk_t);

    if (len < sizeof(chunk_t)) {
        mask_t rem_mask = gen_mask(rem);
        chunk = _mm256_maskz_loadu_epi8(rem_mask, from);
        _mm256_mask_storeu_epi8(out, rem_mask, chunk);
        return out + rem;
    }

    loadchunk(from, &chunk);
    rem = (rem == 0) ? sizeof(chunk_t) : rem;
    storechunk(out, &chunk);
    out += rem;
    from += rem;
    len -= rem;

    while (len > 0) {
        loadchunk(from, &chunk);
        storechunk(out, &chunk);
        out += sizeof(chunk_t);
        from += sizeof(chunk_t);
        len -= sizeof(chunk_t);
    }

    return out;
}

static inline chunk_t GET_CHUNK_MAG(uint8_t *buf, uint32_t *chunk_rem, uint32_t dist) {
    lut_rem_pair lut_rem = perm_idx_lut[dist - 3];
    __m256i ret_vec;
    *chunk_rem = lut_rem.remval;

    /* See the AVX2 implementation for more detailed comments. This is that + some masked
     * loads to avoid an out of bounds read on the heap */

    if (dist < 16) {
        const __m256i permute_xform =
            _mm256_setr_epi8(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                             16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16);
        __m256i perm_vec = _mm256_load_si256((__m256i*)(permute_table+lut_rem.idx));
        halfmask_t load_mask = gen_half_mask(dist);
        __m128i ret_vec0 = _mm_maskz_loadu_epi8(load_mask, buf);
        perm_vec = _mm256_add_epi8(perm_vec, permute_xform);
        ret_vec = _mm256_inserti128_si256(_mm256_castsi128_si256(ret_vec0), ret_vec0, 1);
        ret_vec = _mm256_shuffle_epi8(ret_vec, perm_vec);
    }  else {
        halfmask_t load_mask = gen_half_mask(dist - 16);
        __m128i ret_vec0 = _mm_loadu_si128((__m128i*)buf);
        __m128i ret_vec1 = _mm_maskz_loadu_epi8(load_mask, (__m128i*)(buf + 16));
        __m128i perm_vec1 = _mm_load_si128((__m128i*)(permute_table + lut_rem.idx));
        halfmask_t xlane_mask = _mm_cmp_epi8_mask(perm_vec1, _mm_set1_epi8(15), _MM_CMPINT_LE);
        __m128i latter_half = _mm_mask_shuffle_epi8(ret_vec1, xlane_mask, ret_vec0, perm_vec1);
        ret_vec = _mm256_inserti128_si256(_mm256_castsi128_si256(ret_vec0), latter_half, 1);
    }

    return ret_vec;
}

static inline void storehalfchunk(uint8_t *out, halfchunk_t *chunk) {
    _mm_storeu_si128((__m128i *)out, *chunk);
}

static inline chunk_t halfchunk2whole(halfchunk_t *chunk) {
    /* We zero extend mostly to appease some memory sanitizers. These bytes are ultimately
     * unlikely to be actually written or read from */
    return _mm256_zextsi128_si256(*chunk);
}

static inline halfchunk_t GET_HALFCHUNK_MAG(uint8_t *buf, uint32_t *chunk_rem, uint32_t dist) {
    lut_rem_pair lut_rem = perm_idx_lut[dist - 3];
    __m128i perm_vec, ret_vec;
    halfmask_t load_mask = gen_half_mask(dist);
    ret_vec = _mm_maskz_loadu_epi8(load_mask, buf);
    *chunk_rem = half_rem_vals[dist - 3];

    perm_vec = _mm_load_si128((__m128i*)(permute_table + lut_rem.idx));
    ret_vec = _mm_shuffle_epi8(ret_vec, perm_vec);

    return ret_vec;
}

static inline uint8_t* HALFCHUNKCOPY(uint8_t *out, uint8_t const *from, unsigned len) {
    Assert(len > 0, "chunkcopy should never have a length 0");
    halfchunk_t chunk;

    uint32_t rem = len % sizeof(halfchunk_t);
    if (rem == 0) {
        rem = sizeof(halfchunk_t);
    }

    halfmask_t rem_mask = gen_half_mask(rem);
    chunk = _mm_maskz_loadu_epi8(rem_mask, from);
    _mm_mask_storeu_epi8(out, rem_mask, chunk);

    return out + rem;
}

#define CHUNKSIZE        chunksize_avx512
#define CHUNKUNROLL      chunkunroll_avx512
#define CHUNKMEMSET      chunkmemset_avx512
#define CHUNKMEMSET_SAFE chunkmemset_safe_avx512

#include "chunkset_tpl.h"

#define INFLATE_FAST     inflate_fast_avx512

#include "inffast_tpl.h"

#endif
